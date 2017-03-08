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
using System.IO;
using SyncrioCommon;
using MessageStream2;

namespace SyncrioServer.Messages
{
    public class KerbalsRequest
    {
        public static void HandleKerbalsRequest(ClientObject client)
        {
            //The time sensitive SYNC_TIME is over by this point.
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string>(client.playerName);
                ServerMessage joinMessage = new ServerMessage();
                joinMessage.type = ServerMessageType.PLAYER_JOIN;
                joinMessage.data = mw.GetMessageBytes();
                ClientHandler.SendToAll(client, joinMessage, true);
            }
            Messages.ServerSettings.SendServerSettings(client);
            Messages.WarpControl.SendSetSubspace(client);
            Messages.WarpControl.SendAllSubspaces(client);
            Messages.PlayerColor.SendAllPlayerColors(client);
            Messages.PlayerStatus.SendAllPlayerStatus(client);
            if (!GroupSystem.fetch.PlayerIsInGroup(client.playerName))
            {
                ScenarioSystem.fetch.ScenarioSendAllData(client);
            }
            else
            {
                string groupname = GroupSystem.fetch.GetPlayerGroup(client.playerName);
                ScenarioSystem.fetch.ScenarioSendAllData(groupname, client);
            }
            Messages.WarpControl.SendAllReportedSkewRates(client);
            Messages.CraftLibrary.SendCraftList(client);
            Messages.Chat.SendPlayerChatChannels(client);
            Messages.LockSystem.SendAllLocks(client);
            Messages.AdminSystem.SendAllAdmins(client);
            Messages.Group.SendAllGroupsToClient(client);
            Messages.Vessel.SendPlayerVessels(client);//This one is non DMP co-op mode only
            //Send kerbals
            lock (Server.ScenarioSizeLock)
            {
                string[] kerbalFiles = Directory.GetFiles(Path.Combine(Server.ScenarioDirectory, "Kerbals"));
                foreach (string kerbalFile in kerbalFiles)
                {
                    string kerbalName = Path.GetFileNameWithoutExtension(kerbalFile);
                    byte[] kerbalData = File.ReadAllBytes(kerbalFile);
                    SendKerbal(client, kerbalName, kerbalData);
                }
                SyncrioLog.Debug("Sending " + client.playerName + " " + kerbalFiles.Length + " kerbals...");
            }
            SendKerbalsComplete(client);
        }

        private static void SendKerbal(ClientObject client, string kerbalName, byte[] kerbalData)
        {
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.KERBAL_REPLY;
            using (MessageWriter mw = new MessageWriter())
            {
                //Send the vessel with a send time of 0 so it instantly loads on the client.
                mw.Write<double>(0);
                mw.Write<string>(kerbalName);
                mw.Write<byte[]>(kerbalData);
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToClient(client, newMessage, false);
        }

        private static void SendKerbalsComplete(ClientObject client)
        {
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.KERBAL_COMPLETE;
            ClientHandler.SendToClient(client, newMessage, false);
        }
    }
}

