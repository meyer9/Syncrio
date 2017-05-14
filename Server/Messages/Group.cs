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
using MessageStream2;
using SyncrioCommon;

namespace SyncrioServer.Messages
{
    public class Group
    {
        public static void CreateGroupResponse(ClientObject client, string groupName, bool inviteAvailable)
        {
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string>(groupName);
                mw.Write<bool>(inviteAvailable);
                ServerMessage newMessage = new ServerMessage();
                newMessage.type = ServerMessageType.CREATE_GROUP_REPLY;
                newMessage.data = mw.GetMessageBytes();
                ClientHandler.SendToClient(client, newMessage, true);
            }
        }
        public static void ErrorCreatingGroup(ClientObject client, string error)
        {
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string>(error);
                ServerMessage newMessage = new ServerMessage();
                newMessage.type = ServerMessageType.CREATE_GROUP_ERROR;
                newMessage.data = mw.GetMessageBytes();
                ClientHandler.SendToClient(client, newMessage, true);
            }
        }
        public static void ChangeGroupLeaderRequest(ClientObject client, byte[] messagedata)
        {
            string groupLeader;
            string groupName;
            string requestedLeader;
            ClientObject targetLeader;
            using (MessageReader mr = new MessageReader(messagedata))
            {
                groupLeader = mr.Read<string>();
                groupName = mr.Read<string>();
                requestedLeader = mr.Read<string>();
            }
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string>(groupLeader);
                mw.Write<string>(groupName);
                mw.Write<string>(requestedLeader);
                targetLeader = ClientHandler.GetClientByName(requestedLeader);
                ServerMessage newMessage = new ServerMessage();
                newMessage.type = ServerMessageType.CHANGE_LEADER_REQUEST_RELAY;
                newMessage.data = mw.GetMessageBytes();
                ClientHandler.SendToClient(targetLeader, newMessage, true);
            }
        }
        public static void PlayerInviteRequest(ClientObject client, byte[] messagedata)
        {
            string invitedPlayer = "";
            string groupName = "";
            string sender = "";
            ClientObject targetPlayer;
            bool isReply;
            using (MessageReader mr = new MessageReader(messagedata))
            {
                isReply = mr.Read<bool>();
                if (!isReply)
                {
                    invitedPlayer = mr.Read<string>();
                    groupName = mr.Read<string>();
                    sender = mr.Read<string>();
                }
                else
                {
                    byte[] replyMessage;
                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(mr.Read<bool>());
                        mw.Write<string>(mr.Read<string>());
                        mw.Write<string>(mr.Read<string>());
                        mw.Write<string>(mr.Read<string>());
                        replyMessage = mw.GetMessageBytes();
                    }
                    GroupSystem.fetch.InvitePlayer(client, replyMessage);
                }
            }
            if (!isReply)
            {
                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<string>(invitedPlayer);
                    mw.Write<string>(groupName);
                    mw.Write<string>(sender);
                    targetPlayer = ClientHandler.GetClientByName(invitedPlayer);
                    ServerMessage newMessage = new ServerMessage();
                    newMessage.type = ServerMessageType.INVITE_PLAYER_REQUEST_RELAY;
                    newMessage.data = mw.GetMessageBytes();
                    ClientHandler.SendToClient(targetPlayer, newMessage, true);
                    Messages.Chat.SendChatMessageToClient(targetPlayer, "You were invited to " + groupName);
                }
            }
        }
        public static void SendAllGroupsToAllClients()
        {
            Dictionary<string, GroupObject> groupState = GroupSystem.fetch.GetCopy();
            foreach (KeyValuePair<string, GroupObject> kvp in groupState)
            {
                ServerMessage newMessage = new ServerMessage();
                newMessage.type = ServerMessageType.GROUP_SYSTEM;
                newMessage.data = GetGroupBytes(kvp.Key, kvp.Value);
                ClientHandler.SendToAll(null, newMessage, true);
            }
        }
        public static void SendAllGroupsToClient(ClientObject client)
        {
            Dictionary<string, GroupObject> groupState = GroupSystem.fetch.GetCopy();
            foreach (KeyValuePair<string, GroupObject> kvp in groupState)
            {
                ServerMessage newMessage = new ServerMessage();
                newMessage.type = ServerMessageType.GROUP_SYSTEM;
                newMessage.data = GetGroupBytes(kvp.Key, kvp.Value);
                ClientHandler.SendToClient(client, newMessage, true);
            }
        }

        public static void SendGroupToAll(string groupName, GroupObject group)
        {
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.GROUP_SYSTEM;
            newMessage.data = GetGroupBytes(groupName, group);
            ClientHandler.SendToAll(null, newMessage, true);
        }

        private static byte[] GetGroupBytes(string groupName, GroupObject group)
        {
            byte[] returnBytes;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)GroupMessageType.SET);
                mw.Write<string>(groupName);
                mw.Write<int>((int)group.privacy);
                if (group.privacy == GroupPrivacy.PRIVATE_PASSWORD)
                {
                    bool passwordSet = (group.passwordHash != null);
                    mw.Write<bool>(passwordSet);
                    if (passwordSet)
                    {
                        //Send the salt so the user can send the correct hash back
                        mw.Write<string>(group.passwordSalt);
                    }
                }
                mw.Write<string[]>(group.members.ToArray());
                mw.Write<bool>(group.settings.inviteAvailable);
                returnBytes = mw.GetMessageBytes();
            }
            return returnBytes;
        }

        public static void RemoveGroup(string groupName)
        {
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.GROUP_SYSTEM;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)GroupMessageType.REMOVE);
                mw.Write<string>(groupName);
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToAll(null, newMessage, true);
        }
    }
}
