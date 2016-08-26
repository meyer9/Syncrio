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
using System.Collections.Generic;
using SyncrioCommon;
using MessageStream2;

namespace SyncrioServer.Messages
{
    public class Chat
    {
        private static Dictionary<string, List<string>> playerChatChannels = new Dictionary<string, List<string>>();

        public static void SendChatMessageToClient(ClientObject client, string messageText)
        {
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.CHAT_MESSAGE;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)ChatMessageType.PRIVATE_MESSAGE);
                mw.Write<string>(Settings.settingsStore.consoleIdentifier);
                mw.Write<string>(client.playerName);
                mw.Write(messageText);
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToClient(client, newMessage, true);
        }

        public static void SendChatMessageToAll(string messageText)
        {
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.CHAT_MESSAGE;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)ChatMessageType.CHANNEL_MESSAGE);
                mw.Write<string>(Settings.settingsStore.consoleIdentifier);
                //Global channel
                mw.Write<string>("");
                mw.Write(messageText);
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToAll(null, newMessage, true);
        }

        public static void SendChatMessageToChannel(string channel, string messageText)
        {
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.CHAT_MESSAGE;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)ChatMessageType.CHANNEL_MESSAGE);
                mw.Write<string>(Settings.settingsStore.consoleIdentifier);
                // Channel
                mw.Write<string>(channel);
                mw.Write(messageText);
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToAll(null, newMessage, true);
        }

        public static void SendConsoleMessageToClient(ClientObject client, string message)
        {
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.CHAT_MESSAGE;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)ChatMessageType.CONSOLE_MESSAGE);
                mw.Write<string>(message);
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToClient(client, newMessage, false);
        }

        public static void SendConsoleMessageToAdmins(string message)
        {
            foreach (ClientObject client in ClientHandler.GetClients())
            {
                if (client.authenticated && SyncrioServer.AdminSystem.fetch.IsAdmin(client.playerName))
                {
                    SendConsoleMessageToClient(client, message);
                }
            }
        }

        public static void HandleChatMessage(ClientObject client, byte[] messageData)
        {
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.CHAT_MESSAGE;
            newMessage.data = messageData;
            using (MessageReader mr = new MessageReader(messageData))
            {
                ChatMessageType messageType = (ChatMessageType)mr.Read<int>();
                string fromPlayer = mr.Read<string>();
                if (fromPlayer != client.playerName)
                {
                    Messages.ConnectionEnd.SendConnectionEnd(client, "Kicked for sending a chat message for another player");
                    return;
                }
                switch (messageType)
                {
                    case ChatMessageType.JOIN:
                        {
                            string joinChannel = mr.Read<string>();
                            if (!playerChatChannels.ContainsKey(fromPlayer))
                            {
                                playerChatChannels.Add(fromPlayer, new List<string>());
                            }
                            if (!playerChatChannels[fromPlayer].Contains(joinChannel))
                            {
                                playerChatChannels[fromPlayer].Add(joinChannel);
                            }
                            SyncrioLog.Debug(fromPlayer + " joined channel: " + joinChannel);
                        }
                        ClientHandler.SendToAll(client, newMessage, true);
                        break;
                    case ChatMessageType.LEAVE:
                        {
                            string leaveChannel = mr.Read<string>();
                            if (playerChatChannels.ContainsKey(fromPlayer))
                            {
                                if (playerChatChannels[fromPlayer].Contains(leaveChannel))
                                {
                                    playerChatChannels[fromPlayer].Remove(leaveChannel);
                                }
                                if (playerChatChannels[fromPlayer].Count == 0)
                                {
                                    playerChatChannels.Remove(fromPlayer);
                                }
                            }
                            SyncrioLog.Debug(fromPlayer + " left channel: " + leaveChannel);
                        }
                        ClientHandler.SendToAll(client, newMessage, true);
                        break;
                    case ChatMessageType.CHANNEL_MESSAGE:
                        {
                            string channel = mr.Read<string>();
                            string message = mr.Read<string>();
                            if (channel != "")
                            {
                                foreach (KeyValuePair<string, List<string>> playerEntry in playerChatChannels)
                                {
                                    if (playerEntry.Value.Contains(channel))
                                    {
                                        ClientObject findClient = ClientHandler.GetClientByName(playerEntry.Key);
                                        if (findClient != null)
                                        {
                                            ClientHandler.SendToClient(findClient, newMessage, true);
                                        }
                                    }
                                }
                                SyncrioLog.ChatMessage(fromPlayer + " -> #" + channel + ": " + message);
                            }
                            else
                            {
                                ClientHandler.SendToClient(client, newMessage, true);
                                ClientHandler.SendToAll(client, newMessage, true);
                                SyncrioLog.ChatMessage(fromPlayer + " -> #Global: " + message);
                            }
                        }
                        break;
                    case ChatMessageType.PRIVATE_MESSAGE:
                        {
                            string toPlayer = mr.Read<string>();
                            string message = mr.Read<string>();
                            if (toPlayer != Settings.settingsStore.consoleIdentifier)
                            {
                                ClientObject findClient = ClientHandler.GetClientByName(toPlayer);
                                if (findClient != null)
                                {
                                    ClientHandler.SendToClient(client, newMessage, true);
                                    ClientHandler.SendToClient(findClient, newMessage, true);
                                    SyncrioLog.ChatMessage(fromPlayer + " -> @" + toPlayer + ": " + message);
                                }
                                {
                                    SyncrioLog.ChatMessage(fromPlayer + " -X-> @" + toPlayer + ": " + message);
                                }
                            }
                            else
                            {
                                ClientHandler.SendToClient(client, newMessage, true);
                                SyncrioLog.ChatMessage(fromPlayer + " -> @" + toPlayer + ": " + message);
                            }
                        }
                        break;
                    case ChatMessageType.CONSOLE_MESSAGE:
                        {
                            string message = mr.Read<string>();
                            if (client.authenticated && SyncrioServer.AdminSystem.fetch.IsAdmin(client.playerName))
                            {
                                CommandHandler.HandleServerInput(message);
                            }
                            else
                            {
                                Messages.ConnectionEnd.SendConnectionEnd(client, "Kicked for sending a console command as a non-admin player.");
                            }
                        }
                        break;
                }
            }
        }

        public static void SendPlayerChatChannels(ClientObject client)
        {
            List<string> playerList = new List<string>();
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)ChatMessageType.LIST);
                foreach (KeyValuePair<string, List<string>> playerEntry in playerChatChannels)
                {
                    playerList.Add(playerEntry.Key);
                }
                mw.Write<string[]>(playerList.ToArray());
                foreach (string player in playerList)
                {
                    mw.Write<string[]>(playerChatChannels[player].ToArray());
                }
                ServerMessage newMessage = new ServerMessage();
                newMessage.type = ServerMessageType.CHAT_MESSAGE;
                newMessage.data = mw.GetMessageBytes();
                ClientHandler.SendToClient(client, newMessage, true);
            }
        }

        public static void RemovePlayer(string playerName)
        {
            if (playerChatChannels.ContainsKey(playerName))
            {
                playerChatChannels.Remove(playerName);
            }
        }

        public static void Reset()
        {
            playerChatChannels.Clear();
        }
    }
}

