/*   Syncrio License
 *   
 *   Copyright © 2016 Caleb Huyck
 *   
 *   This file is part of Syncrio.
 *   
 *   Syncrio is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *   
 *   Syncrio is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *   
 *   You should have received a copy of the GNU General Public License
 *   along with Syncrio.  If not, see <http://www.gnu.org/licenses/>.
 */

/*   DarkMultiPlayer License
 * 
 *   Copyright (c) 2014-2016 Christopher Andrews, Alexandre Oliveira, Joshua Blake, William Donaldson.
 *
 *   Permission is hereby granted, free of charge, to any person obtaining a copy
 *   of this software and associated documentation files (the "Software"), to deal
 *   in the Software without restriction, including without limitation the rights
 *   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *   copies of the Software, and to permit persons to whom the Software is
 *   furnished to do so, subject to the following conditions:
 *
 *   The above copyright notice and this permission notice shall be included in all
 *   copies or substantial portions of the Software.
 *
 *   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *   SOFTWARE.
 */


using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using MessageStream2;
using System.IO;
using SyncrioCommon;

namespace SyncrioServer
{
    public class ClientHandler
    {
        //No point support IPv6 until KSP enables it on their windows builds.
        private static TcpListener TCPServer;
        private static ReadOnlyCollection<ClientObject> clients = new List<ClientObject>().AsReadOnly();

        //When a client hits 100kb on the send queue, SyncrioServer will throw out old duplicate messages
        private const int OPTIMIZE_QUEUE_LIMIT = 100 * 1024;

        public static void ThreadMain()
        {
            try
            {
                clients = new List<ClientObject>().AsReadOnly();

                Messages.WarpControl.Reset();
                Messages.Chat.Reset();
                Messages.ScreenshotLibrary.Reset();

                SetupTCPServer();

                while (Server.serverRunning)
                {
                    //Process current clients
                    foreach (ClientObject client in clients)
                    {
                        Messages.Heartbeat.CheckHeartBeat(client);
                    }
                    //Check timers
                    Messages.WarpControl.CheckTimer();
                    //Run plugin update
                    SyncrioPluginHandler.FireOnUpdate();
                    Thread.Sleep(10);
                }
            }
            catch (Exception e)
            {
                SyncrioLog.Error("Fatal error thrown, exception: " + e);
                Server.ShutDown("Crashed!");
            }
            try
            {
                long disconnectTime = DateTime.UtcNow.Ticks;
                bool sendingHighPriotityMessages = true;
                while (sendingHighPriotityMessages)
                {
                    if ((DateTime.UtcNow.Ticks - disconnectTime) > 50000000)
                    {
                        SyncrioLog.Debug("Shutting down with " + Server.playerCount + " players, " + clients.Count + " connected clients");
                        break;
                    }
                    sendingHighPriotityMessages = false;
                    foreach (ClientObject client in clients)
                    {
                        if (client.authenticated && (client.sendMessageQueueHigh.Count > 0))
                        {
                            sendingHighPriotityMessages = true;
                        }
                    }
                    Thread.Sleep(10);
                }
                ShutdownTCPServer();
            }
            catch (Exception e)
            {
                SyncrioLog.Fatal("Fatal error thrown during shutdown, exception: " + e);
                throw;
            }
        }

        private static void SetupTCPServer()
        {
            try
            {
                IPAddress bindAddress = IPAddress.Parse(Settings.settingsStore.address);
                TCPServer = new TcpListener(new IPEndPoint(bindAddress, Settings.settingsStore.port));
                try
                {
                    if (System.Net.Sockets.Socket.OSSupportsIPv6)
                    {
                        //Windows defaults to v6 only, but this option does not exist in mono so it has to be in a try/catch block along with the casted int.
                        if (Environment.OSVersion.Platform != PlatformID.MacOSX && Environment.OSVersion.Platform != PlatformID.Unix)
                        {
                            TCPServer.Server.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, 0);
                        }
                    }
                }
                catch
                {
                    //Don't care - On linux and mac this throws because it's already set, and on windows it just works.
                }
                TCPServer.Start(4);
                TCPServer.BeginAcceptTcpClient(new AsyncCallback(NewClientCallback), null);
            }
            catch (Exception e)
            {
                SyncrioLog.Normal("Error setting up server, Exception: " + e);
                Server.serverRunning = false;
            }
            Server.serverStarting = false;
        }

        private static void ShutdownTCPServer()
        {
            TCPServer.Stop();
        }

        private static void NewClientCallback(IAsyncResult ar)
        {
            if (Server.serverRunning)
            {
                try
                {
                    TcpClient newClient = TCPServer.EndAcceptTcpClient(ar);
                    SetupClient(newClient);
                    SyncrioLog.Normal("New client connection from " + newClient.Client.RemoteEndPoint);
                }
                catch
                {
                    SyncrioLog.Normal("Error accepting client!");
                }
                TCPServer.BeginAcceptTcpClient(new AsyncCallback(NewClientCallback), null);
            }
        }

        private static void SetupClient(TcpClient newClientConnection)
        {
            ClientObject newClientObject = new ClientObject();
            newClientObject.subspace = Messages.WarpControl.GetLatestSubspace();
            newClientObject.playerStatus = new PlayerStatus();
            newClientObject.connectionStatus = ConnectionStatus.CONNECTED;
            newClientObject.endpoint = newClientConnection.Client.RemoteEndPoint.ToString();
            newClientObject.ipAddress = (newClientConnection.Client.RemoteEndPoint as IPEndPoint).Address;
            //Keep the connection reference
            newClientObject.connection = newClientConnection;
            StartReceivingIncomingMessages(newClientObject);
            StartSendingOutgoingMessages(newClientObject);
            SyncrioPluginHandler.FireOnClientConnect(newClientObject);
            Messages.Handshake.SendHandshakeChallange(newClientObject);
            lock (clients)
            {
                List<ClientObject> newList = new List<ClientObject>(clients);
                newList.Add(newClientObject);
                clients = newList.AsReadOnly();
                Server.playerCount = GetActiveClientCount();
                Server.players = GetActivePlayerNames();
                SyncrioLog.Debug("Online players is now: " + Server.playerCount + ", connected: " + clients.Count);
            }
        }

        public static int GetActiveClientCount()
        {
            int authenticatedCount = 0;
            foreach (ClientObject client in clients)
            {
                if (client.authenticated)
                {
                    authenticatedCount++;
                }
            }
            return authenticatedCount;
        }

        public static string GetActivePlayerNames()
        {
            string playerString = "";
            foreach (ClientObject client in clients)
            {
                if (client.authenticated)
                {
                    if (playerString != "")
                    {
                        playerString += ", ";
                    }
                    playerString += client.playerName;
                }
            }
            return playerString;
        }



        private static void StartSendingOutgoingMessages(ClientObject client)
        {
            Thread clientSendThread = new Thread(new ParameterizedThreadStart(SendOutgoingMessages));
            clientSendThread.IsBackground = true;
            clientSendThread.Start(client);
        }
        //ParameterizedThreadStart takes an object
        private static void SendOutgoingMessages(object client)
        {
            SendOutgoingMessages((ClientObject)client);
        }

        private static void SendOutgoingMessages(ClientObject client)
        {
            while (client.connectionStatus == ConnectionStatus.CONNECTED)
            {
                ServerMessage message = null;
                if (message == null && client.sendMessageQueueHigh.Count > 0)
                {
                    client.sendMessageQueueHigh.TryDequeue(out message);
                }
                //Don't send low or split during server shutdown.
                if (Server.serverRunning)
                {
                    if (message == null && client.sendMessageQueueSplit.Count > 0)
                    {
                        client.sendMessageQueueSplit.TryDequeue(out message);
                    }
                    if (message == null && client.sendMessageQueueLow.Count > 0)
                    {
                        client.sendMessageQueueLow.TryDequeue(out message);
                        //Splits large messages to higher priority messages can get into the queue faster
                        SplitAndRewriteMessage(client, ref message);
                    }
                }
                if (message != null)
                {
                    SendNetworkMessage(client, message);
                }
                else
                {
                    //Give the chance for the thread to terminate
                    client.sendEvent.WaitOne(1000);
                }
            }
        }

        private static void SplitAndRewriteMessage(ClientObject client, ref ServerMessage message)
        {
            if (message == null)
            {
                return;
            }
            if (message.data == null)
            {
                return;
            }
            if (message.data.Length > Common.SPLIT_MESSAGE_LENGTH)
            {
                ServerMessage newSplitMessage = new ServerMessage();
                newSplitMessage.type = ServerMessageType.SPLIT_MESSAGE;
                int splitBytesLeft = message.data.Length;
                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<int>((int)message.type);
                    mw.Write<int>(message.data.Length);
                    byte[] firstSplit = new byte[Common.SPLIT_MESSAGE_LENGTH];
                    Array.Copy(message.data, 0, firstSplit, 0, Common.SPLIT_MESSAGE_LENGTH);
                    mw.Write<byte[]>(firstSplit);
                    splitBytesLeft -= Common.SPLIT_MESSAGE_LENGTH;
                    newSplitMessage.data = mw.GetMessageBytes();
                    //SPLIT_MESSAGE metadata header length.
                    client.bytesQueuedOut += 8;
                    client.sendMessageQueueSplit.Enqueue(newSplitMessage);
                }


                while (splitBytesLeft > 0)
                {
                    ServerMessage currentSplitMessage = new ServerMessage();
                    currentSplitMessage.type = ServerMessageType.SPLIT_MESSAGE;
                    currentSplitMessage.data = new byte[Math.Min(splitBytesLeft, Common.SPLIT_MESSAGE_LENGTH)];
                    Array.Copy(message.data, message.data.Length - splitBytesLeft, currentSplitMessage.data, 0, currentSplitMessage.data.Length);
                    splitBytesLeft -= currentSplitMessage.data.Length;
                    //SPLIT_MESSAGE network frame header length.
                    client.bytesQueuedOut += 8;
                    client.sendMessageQueueSplit.Enqueue(currentSplitMessage);
                }
                client.sendMessageQueueSplit.TryDequeue(out message);
            }
        }

        private static void SendNetworkMessage(ClientObject client, ServerMessage message)
        {
            //Write the send times down in SYNC_TIME_REPLY packets
            if (message.type == ServerMessageType.SYNC_TIME_REPLY)
            {
                try
                {
                    using (MessageWriter mw = new MessageWriter())
                    {
                        using (MessageReader mr = new MessageReader(message.data))
                        {
                            client.bytesQueuedOut += 8;
                            //Client send time
                            mw.Write<long>(mr.Read<long>());
                            //Server receive time
                            mw.Write<long>(mr.Read<long>());
                            //Server send time
                            mw.Write<long>(DateTime.UtcNow.Ticks);
                            message.data = mw.GetMessageBytes();
                        }
                    }
                }
                catch (Exception e)
                {
                    SyncrioLog.Debug("Error rewriting SYNC_TIME packet, Exception " + e);
                }
            }
            //Continue sending
            byte[] messageBytes = Common.PrependNetworkFrame((int)message.type, message.data);
            client.lastSendTime = Server.serverClock.ElapsedMilliseconds;
            client.bytesQueuedOut -= messageBytes.Length;
            client.bytesSent += messageBytes.Length;
            if (client.connectionStatus == ConnectionStatus.CONNECTED)
            {
                try
                {
                    client.connection.GetStream().Write(messageBytes, 0, messageBytes.Length);
                }
                catch (Exception e)
                {
                    HandleDisconnectException("Send Network Message", client, e);
                    return;
                }
            }
            SyncrioPluginHandler.FireOnMessageSent(client, message);
            if (message.type == ServerMessageType.CONNECTION_END)
            {
                using (MessageReader mr = new MessageReader(message.data))
                {
                    string reason = mr.Read<string>();
                    SyncrioLog.Normal("Disconnecting client " + client.playerName + ", sent CONNECTION_END (" + reason + ") to endpoint " + client.endpoint);
                    client.disconnectClient = true;
                    DisconnectClient(client);
                }
            }
            if (message.type == ServerMessageType.HANDSHAKE_REPLY)
            {
                using (MessageReader mr = new MessageReader(message.data))
                {
                    int response = mr.Read<int>();
                    string reason = mr.Read<string>();
                    if (response != 0)
                    {
                        SyncrioLog.Normal("Disconnecting client " + client.playerName + ", sent HANDSHAKE_REPLY (" + reason + ") to endpoint " + client.endpoint);
                        client.disconnectClient = true;
                        DisconnectClient(client);
                    }
                }
            }
        }

        private static void StartReceivingIncomingMessages(ClientObject client)
        {
            client.lastReceiveTime = Server.serverClock.ElapsedMilliseconds;
            //Allocate byte for header
            client.receiveMessage = new ClientMessage();
            client.receiveMessage.data = new byte[8];
            client.receiveMessageBytesLeft = client.receiveMessage.data.Length;
            try
            {
                client.connection.GetStream().BeginRead(client.receiveMessage.data, client.receiveMessage.data.Length - client.receiveMessageBytesLeft, client.receiveMessageBytesLeft, new AsyncCallback(ReceiveCallback), client);
            }
            catch (Exception e)
            {
                HandleDisconnectException("Start Receive", client, e);
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            ClientObject client = (ClientObject)ar.AsyncState;
            int bytesRead = 0;
            try
            {
                bytesRead = client.connection.GetStream().EndRead(ar);
            }
            catch (Exception e)
            {
                HandleDisconnectException("ReceiveCallback", client, e);
                return;
            }
            client.bytesReceived += bytesRead;
            client.receiveMessageBytesLeft -= bytesRead;
            if (client.receiveMessageBytesLeft == 0)
            {
                //We either have the header or the message data, let's do something
                if (!client.isReceivingMessage)
                {
                    //We have the header
                    using (MessageReader mr = new MessageReader(client.receiveMessage.data))
                    {

                        int messageType = mr.Read<int>();
                        int messageLength = mr.Read<int>();
                        if (messageType < 0 || messageType > (Enum.GetNames(typeof(ClientMessageType)).Length - 1))
                        {
                            //Malformed message, most likely from a non Syncrio-client.
                            Messages.ConnectionEnd.SendConnectionEnd(client, "Invalid Syncrio message. Disconnected.");
                            SyncrioLog.Normal("Invalid Syncrio message from " + client.endpoint);
                            //Returning from ReceiveCallback will break the receive loop and stop processing any further messages.
                            return;
                        }
                        client.receiveMessage.type = (ClientMessageType)messageType;
                        if (messageLength == 0)
                        {
                            //Null message, handle it.
                            client.receiveMessage.data = null;
                            HandleMessage(client, client.receiveMessage);
                            client.receiveMessage.type = 0;
                            client.receiveMessage.data = new byte[8];
                            client.receiveMessageBytesLeft = client.receiveMessage.data.Length;
                        }
                        else
                        {
                            if (messageLength > 0 && messageLength < Common.MAX_MESSAGE_SIZE)
                            {
                                client.isReceivingMessage = true;
                                client.receiveMessage.data = new byte[messageLength];
                                client.receiveMessageBytesLeft = client.receiveMessage.data.Length;
                            }
                            else
                            {
                                //Malformed message, most likely from a non Syncrio-client.
                                Messages.ConnectionEnd.SendConnectionEnd(client, "Invalid Syncrio message. Disconnected.");
                                SyncrioLog.Normal("Invalid Syncrio message from " + client.endpoint);
                                //Returning from ReceiveCallback will break the receive loop and stop processing any further messages.
                                return;
                            }
                        }
                    }
                }
                else
                {
                    //We have the message data to a non-null message, handle it
                    client.isReceivingMessage = false;
                    #if !DEBUG
                    try
                    {
                    #endif
                        HandleMessage(client, client.receiveMessage);
                    #if !DEBUG
                    }
                    catch (Exception e)
                    {
                        HandleDisconnectException("ReceiveCallback", client, e);
                        return;
                    }
                    #endif
                    client.receiveMessage.type = 0;
                    client.receiveMessage.data = new byte[8];
                    client.receiveMessageBytesLeft = client.receiveMessage.data.Length;
                }
            }
            if (client.connectionStatus == ConnectionStatus.CONNECTED)
            {
                client.lastReceiveTime = Server.serverClock.ElapsedMilliseconds;
                try
                {
                    client.connection.GetStream().BeginRead(client.receiveMessage.data, client.receiveMessage.data.Length - client.receiveMessageBytesLeft, client.receiveMessageBytesLeft, new AsyncCallback(ReceiveCallback), client);
                }
                catch (Exception e)
                {
                    HandleDisconnectException("ReceiveCallback", client, e);
                    return;
                }
            }
        }

        private static void HandleDisconnectException(string location, ClientObject client, Exception e)
        {
            lock (client.disconnectLock)
            {
                if (!client.disconnectClient && client.connectionStatus != ConnectionStatus.DISCONNECTED)
                {
                    if (e.InnerException != null)
                    {
                        SyncrioLog.Normal("Client " + client.playerName + " disconnected in " + location + ", endpoint " + client.endpoint + ", error: " + e.Message + " (" + e.InnerException.Message + ")");
                    }
                    else
                    {
                        SyncrioLog.Normal("Client " + client.playerName + " disconnected in " + location + ", endpoint " + client.endpoint + ", error: " + e.Message);
                    }
                }
                DisconnectClient(client);
            }
        }

        internal static void DisconnectClient(ClientObject client)
        {
            lock (client.disconnectLock)
            {
                //Remove clients from list
                if (clients.Contains(client))
                {
                    List<ClientObject> newList = new List<ClientObject>(clients);
                    newList.Remove(client);
                    clients = newList.AsReadOnly();
                    Server.playerCount = GetActiveClientCount();
                    Server.players = GetActivePlayerNames();
                    SyncrioLog.Debug("Online players is now: " + Server.playerCount + ", connected: " + clients.Count);
                    if (!Settings.settingsStore.keepTickingWhileOffline && clients.Count == 0)
                    {
                        Messages.WarpControl.HoldSubspace();
                    }
                    Messages.WarpControl.DisconnectPlayer(client.playerName);
                }
                //Disconnect
                if (client.connectionStatus != ConnectionStatus.DISCONNECTED)
                {
                    SyncrioPluginHandler.FireOnClientDisconnect(client);
                    if (client.playerName != null)
                    {
                        Messages.Chat.RemovePlayer(client.playerName);
                    }
                    client.connectionStatus = ConnectionStatus.DISCONNECTED;
                    if (client.authenticated)
                    {
                        ServerMessage newMessage = new ServerMessage();
                        newMessage.type = ServerMessageType.PLAYER_DISCONNECT;
                        using (MessageWriter mw = new MessageWriter())
                        {
                            mw.Write<string>(client.playerName);
                            newMessage.data = mw.GetMessageBytes();
                        }
                        SendToAll(client, newMessage, true);
                        LockSystem.fetch.ReleasePlayerLocks(client.playerName);
                    }
                    try
                    {
                        if (client.connection != null)
                        {
                            client.connection.GetStream().Close();
                            client.connection.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        SyncrioLog.Debug("Error closing client connection: " + e.Message);
                    }
                    Server.lastPlayerActivity = Server.serverClock.ElapsedMilliseconds;
                }
            }
        }

        internal static void HandleMessage(ClientObject client, ClientMessage message)
        {
            //Prevent plugins from dodging SPLIT_MESSAGE. If they are modified, every split message will be broken.
            if (message.type != ClientMessageType.SPLIT_MESSAGE)
            {
                SyncrioPluginHandler.FireOnMessageReceived(client, message);

                if (message.handled)
                {
                    //a plugin has handled this message and requested suppression of the default Syncrio behavior
                    return;
                }
            }

            //Clients can only send HEARTBEATS, HANDSHAKE_REQUEST or CONNECTION_END's until they are authenticated.
            if (!client.authenticated && !(message.type == ClientMessageType.HEARTBEAT || message.type == ClientMessageType.HANDSHAKE_RESPONSE || message.type == ClientMessageType.CONNECTION_END))
            {
                Messages.ConnectionEnd.SendConnectionEnd(client, "You must authenticate before attempting to send a " + message.type.ToString() + " message");
                return;
            }

            #if !DEBUG
            try
            {
                #endif
                switch (message.type)
                {
                    case ClientMessageType.HEARTBEAT:
                        //Don't do anything for heartbeats, they just keep the connection alive
                        break;
                    case ClientMessageType.HANDSHAKE_RESPONSE:
                        Messages.Handshake.HandleHandshakeResponse(client, message.data);
                        break;
                    case ClientMessageType.CHAT_MESSAGE:
                        Messages.Chat.HandleChatMessage(client, message.data);
                        break;
                    case ClientMessageType.PLAYER_STATUS:
                        Messages.PlayerStatus.HandlePlayerStatus(client, message.data);
                        break;
                    case ClientMessageType.PLAYER_COLOR:
                        Messages.PlayerColor.HandlePlayerColor(client, message.data);
                        break;
                    case ClientMessageType.SYNC_TIME_REQUEST:
                        Messages.SyncTimeRequest.HandleSyncTimeRequest(client, message.data);
                        break;
                    case ClientMessageType.KERBALS_REQUEST:
                        Messages.KerbalsRequest.HandleKerbalsRequest(client);
                        break;
                    case ClientMessageType.KERBAL_PROTO:
                        Messages.KerbalProto.HandleKerbalProto(client, message.data);
                        break;
                    case ClientMessageType.CRAFT_LIBRARY:
                        Messages.CraftLibrary.HandleCraftLibrary(client, message.data);
                        break;
                    case ClientMessageType.SCREENSHOT_LIBRARY:
                        Messages.ScreenshotLibrary.HandleScreenshotLibrary(client, message.data);
                        break;
                    case ClientMessageType.FLAG_SYNC:
                        Messages.FlagSync.HandleFlagSync(client, message.data);
                        break;
                    case ClientMessageType.PING_REQUEST:
                        Messages.PingRequest.HandlePingRequest(client, message.data);
                        break;
                    case ClientMessageType.MOTD_REQUEST:
                        Messages.MotdRequest.HandleMotdRequest(client);  
                        break;
                    case ClientMessageType.WARP_CONTROL:
                        Messages.WarpControl.HandleWarpControl(client, message.data);
                        break;
                    case ClientMessageType.LOCK_SYSTEM:
                        Messages.LockSystem.HandleLockSystemMessage(client, message.data);
                        break;
                    case ClientMessageType.CREATE_GROUP_REQUEST:
                        GroupSystem.fetch.CreateGroup(client, message.data);
                        break;
                    case ClientMessageType.JOIN_GROUP_REQUEST:
                        GroupSystem.fetch.JoinGroup(client, message.data);
                        break;
                    case ClientMessageType.LEAVE_GROUP:
                        GroupSystem.fetch.LeaveGroup(client, message.data);
                        break;
                    case ClientMessageType.REMOVE_GROUP_REQUEST:
                        GroupSystem.fetch.RemoveGroup(client, message.data);
                        break;
                    case ClientMessageType.CHANGE_LEADER_REQUEST:
                        Messages.Group.ChangeGroupLeaderRequest(client, message.data);
                        break;
                    case ClientMessageType.SET_LEADER:
                        GroupSystem.fetch.SetGroupOwner(client, message.data);
                        break;
                    case ClientMessageType.KICK_PLAYER_REQUEST:
                        GroupSystem.fetch.KickPlayer(client, message.data);
                        break;
                    case ClientMessageType.INVITE_PLAYER:
                        Messages.Group.PlayerInviteRequest(client, message.data);
                        break;
                    case ClientMessageType.RENAME_GROUP_REQUEST:
                        GroupSystem.fetch.RenameGroupRequest(client, message.data);
                        break;
                    case ClientMessageType.CHANGE_GROUP_PRIVACY_REQUEST:
                        GroupSystem.fetch.ChangeGroupPrivacyRequest(client, message.data);
                        break;
                    case ClientMessageType.SCENARIO_DATA:
                        ScenarioSystem.fetch.SyncScenario(client, message.data);
                        break;
                    case ClientMessageType.SEND_VESSELS:
                        Messages.Vessel.HandleVessels(client, message.data);
                        break;
                    case ClientMessageType.MOD_DATA:
                        Messages.ModData.HandleModDataMessage(client, message.data);
                        break;
                        case ClientMessageType.KERBAL_REMOVE:
                        Messages.KerbalRemove.HandleKerbalRemoval(client, message.data);
                        break;
                    case ClientMessageType.SPLIT_MESSAGE:
                        Messages.SplitMessage.HandleSplitMessage(client, message.data);
                        break;
                    case ClientMessageType.CONNECTION_END:
                        Messages.ConnectionEnd.HandleConnectionEnd(client, message.data);
                        break;
                    default:
                        SyncrioLog.Debug("Unhandled message type " + message.type);
                        Messages.ConnectionEnd.SendConnectionEnd(client, "Unhandled message type " + message.type);
                        #if DEBUG
                        throw new NotImplementedException("Message type not implemented");
                        #else
                        break;
                        #endif
                }
                #if !DEBUG
            }
            catch (Exception e)
            {
                SyncrioLog.Debug("Error handling " + message.type + " from " + client.playerName + ", exception: " + e);
                Messages.ConnectionEnd.SendConnectionEnd(client, "Server failed to process " + message.type + " message");
            }
            #endif
        }

        //Call with null client to send to all clients.
        public static void SendToAll(ClientObject ourClient, ServerMessage message, bool highPriority)
        {
            foreach (ClientObject otherClient in clients)
            {
                if (ourClient != otherClient)
                {
                    SendToClient(otherClient, message, highPriority);
                }
            }
        }

        //Call with null client to send to all clients. Auto selects wether to use the compressed or decompressed message.
        public static void SendToAllAutoCompressed(ClientObject ourClient, ServerMessage compressed, ServerMessage decompressed, bool highPriority)
        {
            foreach (ClientObject otherClient in clients)
            {
                if (ourClient != otherClient)
                {
                    if (otherClient.compressionEnabled && (compressed != null))
                    {
                        SendToClient(otherClient, compressed, highPriority);
                    }
                    else
                    {
                        SendToClient(otherClient, decompressed, highPriority);
                    }
                }
            }
        }

        public static void SendToClient(ClientObject client, ServerMessage message, bool highPriority)
        {
            //Because we dodge the queue, we need to lock it up again...
            lock (client.sendLock)
            {
                if (message == null)
                {
                    return;
                }
                //All messages have an 8 byte header
                client.bytesQueuedOut += 8;
                if (message.data != null)
                {
                    //Count the payload if we have one.
                    client.bytesQueuedOut += message.data.Length;
                }
                if (highPriority)
                {
                    client.sendMessageQueueHigh.Enqueue(message);
                }
                else
                {
                    client.sendMessageQueueLow.Enqueue(message);
                    //If we need to optimize
                    if (client.bytesQueuedOut > OPTIMIZE_QUEUE_LIMIT)
                    {
                        //And we haven't optimized in the last 5 seconds
                        long currentTime = DateTime.UtcNow.Ticks;
                        long optimizedBytes = 0;
                        if ((currentTime - client.lastQueueOptimizeTime) > 50000000)
                        {
                            client.lastQueueOptimizeTime = currentTime;
                            SyncrioLog.Debug("Optimizing " + client.playerName + " (" + client.bytesQueuedOut + " bytes queued)");

                            //Create a temporary filter list
                            List<ServerMessage> oldClientMessagesToSend = new List<ServerMessage>();
                            List<ServerMessage> newClientMessagesToSend = new List<ServerMessage>();
                            //Steal all the messages from the queue and put them into a list
                            ServerMessage stealMessage = null;
                            while (client.sendMessageQueueLow.TryDequeue(out stealMessage))
                            {
                                oldClientMessagesToSend.Add(stealMessage);
                            }
                            //Clear the client send queue
                            List<string> seenProtovesselUpdates = new List<string>();
                            List<string> seenPositionUpdates = new List<string>();
                            //Iterate backwards over the list
                            oldClientMessagesToSend.Reverse();
                            foreach (ServerMessage currentMessage in oldClientMessagesToSend)
                            {
                                //Message isn't proto or position, don't skip it.
                                newClientMessagesToSend.Add(currentMessage);
                            }
                            //Flip it back to the right order
                            newClientMessagesToSend.Reverse();
                            foreach (ServerMessage putBackMessage in newClientMessagesToSend)
                            {
                                client.sendMessageQueueLow.Enqueue(putBackMessage);
                            }
                            float optimizeTime = (DateTime.UtcNow.Ticks - currentTime) / 10000f;
                            client.bytesQueuedOut -= optimizedBytes;
                            SyncrioLog.Debug("Optimized " + optimizedBytes + " bytes in " + Math.Round(optimizeTime, 3) + " ms.");
                        }
                    }
                }
                client.sendEvent.Set();
            }
        }

        public static ClientObject[] GetClients()
        {
            List<ClientObject> returnArray = new List<ClientObject>(clients);
            return returnArray.ToArray();
        }

        public static bool ClientConnected(ClientObject client)
        {
            return clients.Contains(client);
        }

        public static bool ClientConnectedStringClientName(string clientName)
        {
            foreach (ClientObject testClient in clients)
            {
                if (testClient.authenticated && testClient.playerName == clientName)
                {
                    return true;
                }
            }
            return false;
        }

        public static ClientObject GetClientByName(string playerName)
        {
            ClientObject findClient = null;
            foreach (ClientObject testClient in clients)
            {
                if (testClient.authenticated && testClient.playerName == playerName)
                {
                    findClient = testClient;
                    break;
                }
            }
            return findClient;
        }

        public static ClientObject GetClientByIP(IPAddress ipAddress)
        {
            ClientObject findClient = null;
            foreach (ClientObject testClient in clients)
            {
                if (testClient.authenticated && testClient.ipAddress == ipAddress)
                {
                    findClient = testClient;
                    break;
                }
            }
            return findClient;
        }

        public static ClientObject GetClientByPublicKey(string publicKey)
        {
            ClientObject findClient = null;
            foreach (ClientObject testClient in clients)
            {
                if (testClient.authenticated && testClient.publicKey == publicKey)
                {
                    findClient = testClient;
                    break;
                }
            }
            return findClient;
        }
    }

    public class ClientObject
    {
        public bool authenticated;
        public byte[] challange;
        public string playerName = "Unknown";
        public string clientVersion;
        public bool isBanned;
        public IPAddress ipAddress;
        public string publicKey;
        //subspace tracking
        public int subspace = -1;
        public int lastSubspace = -1;
        public float subspaceRate = 1f;
        //vessel tracking
        public string activeVessel = "";
        //connection
        public string endpoint;
        public TcpClient connection;
        //Send buffer
        public long lastSendTime;
        public ConcurrentQueue<ServerMessage> sendMessageQueueHigh = new ConcurrentQueue<ServerMessage>();
        public ConcurrentQueue<ServerMessage> sendMessageQueueSplit = new ConcurrentQueue<ServerMessage>();
        public ConcurrentQueue<ServerMessage> sendMessageQueueLow = new ConcurrentQueue<ServerMessage>();
        public long lastReceiveTime;
        public bool disconnectClient;
        //Receive buffer
        public bool isReceivingMessage;
        public int receiveMessageBytesLeft;
        public ClientMessage receiveMessage;
        //Receive split buffer
        public bool isReceivingSplitMessage;
        public int receiveSplitMessageBytesLeft;
        public ClientMessage receiveSplitMessage;
        //State tracking
        public ConnectionStatus connectionStatus;
        public PlayerStatus playerStatus;
        public float[] playerColor;
        //Network traffic tracking
        public long bytesQueuedOut = 0;
        public long bytesSent = 0;
        public long bytesReceived = 0;
        public long lastQueueOptimizeTime = 0;
        //Send lock
        public AutoResetEvent sendEvent = new AutoResetEvent(false);
        public object sendLock = new object();
        public object disconnectLock = new object();
        //Compression
        public bool compressionEnabled = false;
    }
}

