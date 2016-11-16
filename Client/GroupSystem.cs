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

namespace SyncrioClientSide
{
    public class GroupSystem
    {
        private static GroupSystem singleton;
        public Dictionary<string, GroupObject> groups = new Dictionary<string, GroupObject>();
        public Dictionary<string, Dictionary<int, GroupProgress>> allGroupProgressList = new Dictionary<string, Dictionary<int, GroupProgress>>();
        private object groupLock = new object();
        public static string playerGroupName = string.Empty;
        public static bool playerGroupAssigned = false;
        public static bool playerCreatingGroup = false;
        public static bool playerIsGroupLeader = false;
        public static bool groupFreeToInvite = false;
        public static bool setIfGroupIsFreeToInvite = true;
        public static bool kickPlayerButtons = false;
        public static bool invitePlayerButtons = false;
        public static bool groupProgressButtons = false;
        public static bool newGroupPrivacy_PUBLIC = false;
        public static bool newGroupPrivacy_PASSWORD = false;
        public static bool newGroupPrivacy_INVITE_ONLY = false;
        public static string groupCreationError = string.Empty;
        public static bool isGroupCreationResponseError = false;
        public static bool joinGroupPrivacy_PASSWORD = false;
        public static bool disbandGroupButtonPressed = false;
        public static string groupLeaderChange = string.Empty;
        public static string groupnameChange = string.Empty;
        public static string requestedLeader = string.Empty;
        public static bool groupChangeLeaderWindowDisplay = false;
        public static bool stepDownButton = false;
        public static string inviteSender = string.Empty;
        public static string groupnameInvite = string.Empty;
        public static string invitedPlayer = string.Empty;
        public static bool displayInvite = false;
        public static bool renameGroupButtonPressed = false;
        public static bool changeGroupPrivacyButtonPressed = false;

        public static GroupSystem fetch
        {
            get
            {
                return singleton;
            }
        }

        public void CreateGroup()
        {
            string gn = GroupWindow.groupNameCreate;
            string gpass = null;
            string gp;
            bool gpSet = false;
            string groupCreator = Settings.fetch.playerName;
            if (newGroupPrivacy_PUBLIC)
            {
                gp = GroupPrivacy.PUBLIC.ToString();
                gpSet = true;
            }
            else
            {
                if (newGroupPrivacy_PASSWORD)
                {
                    gp = GroupPrivacy.PRIVATE_PASSWORD.ToString();
                    gpSet = true;
                    gpass = GroupWindow.groupPasswordCreate;
                }
                else
                {
                    if (newGroupPrivacy_INVITE_ONLY)
                    {
                        gp = GroupPrivacy.PRIVATE_INVITE_ONLY.ToString();
                        gpSet = true;
                    }
                    else
                    {
                        SyncrioLog.Debug("No group privacy set!");
                        gp = GroupPrivacy.PUBLIC.ToString();//This is here to fix a compiler error, DONT remove me!!!
                    }
                }
            }
            if (gn != null)
            {
                if (gpSet)
                {
                    if (gp == GroupPrivacy.PRIVATE_PASSWORD.ToString())
                    {
                        if (gpass != null)
                        {
                            byte[] messageBytes;
                            ClientMessage newMessage = new ClientMessage();
                            newMessage.handled = false;
                            newMessage.type = ClientMessageType.CREATE_GROUP_REQUEST;
                            using (MessageWriter mw = new MessageWriter())
                            {
                                mw.Write<string>(groupCreator);
                                mw.Write<string>(gn);
                                mw.Write<string>(gp);
                                mw.Write<string>(gpass);
                                mw.Write<bool>(setIfGroupIsFreeToInvite);
                                messageBytes = mw.GetMessageBytes();
                            }
                            newMessage.data = messageBytes;
                            NetworkWorker.fetch.SendGroupCommand(newMessage, true);
                        }
                        else
                        {
                            SyncrioLog.Debug("No group password set!");
                        }
                    }
                    else
                    {
                        byte[] messageBytes;
                        ClientMessage newMessage = new ClientMessage();
                        newMessage.handled = false;
                        newMessage.type = ClientMessageType.CREATE_GROUP_REQUEST;
                        using (MessageWriter mw = new MessageWriter())
                        {
                            mw.Write<string>(groupCreator);
                            mw.Write<string>(gn);
                            mw.Write<string>(gp);
                            mw.Write<bool>(setIfGroupIsFreeToInvite);
                            messageBytes = mw.GetMessageBytes();
                        }
                        newMessage.data = messageBytes;
                        NetworkWorker.fetch.SendGroupCommand(newMessage, true);
                    }
                }
            }
            else
            {
                SyncrioLog.Debug("No group name set!");
            }
        }

        public void JoinGroup()
        {
            string gn = GroupWindow.groupNameJoin;
            string joiningPlayer = Settings.fetch.playerName;
            string gpass = string.Empty;
            if (joinGroupPrivacy_PASSWORD)
            {
                gpass = GroupWindow.groupPasswordJoin;
            }
            if (gn != null)
            {
                byte[] messageBytes;
                ClientMessage newMessage = new ClientMessage();
                newMessage.handled = false;
                newMessage.type = ClientMessageType.JOIN_GROUP_REQUEST;
                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<string>(gn);
                    mw.Write<string>(joiningPlayer);
                    mw.Write<string>(gpass);
                    messageBytes = mw.GetMessageBytes();
                }
                newMessage.data = messageBytes;
                NetworkWorker.fetch.SendGroupCommand(newMessage, true);
            }
            else
            {
                SyncrioLog.Debug("No group selected!");
            }
        }

        public void LeaveGroup()
        {
            string leavingPlayer = Settings.fetch.playerName;
            byte[] messageBytes;
            ClientMessage newMessage = new ClientMessage();
            newMessage.handled = false;
            newMessage.type = ClientMessageType.LEAVE_GROUP;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string>(leavingPlayer);
                messageBytes = mw.GetMessageBytes();
            }
            newMessage.data = messageBytes;
            NetworkWorker.fetch.SendGroupCommand(newMessage, true);
        }

        public void RemoveGroup()
        {
            if (!string.IsNullOrEmpty(playerGroupName))
            {
                byte[] messageBytes;
                ClientMessage newMessage = new ClientMessage();
                newMessage.handled = false;
                newMessage.type = ClientMessageType.REMOVE_GROUP_REQUEST;
                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<string>(playerGroupName);
                    messageBytes = mw.GetMessageBytes();
                }
                newMessage.data = messageBytes;
                NetworkWorker.fetch.SendGroupCommand(newMessage, true);
            }
        }

        public void StepDown()
        {
            string gl = Settings.fetch.playerName;
            string gn = playerGroupName;
            string rp = GroupWindow.chosenPlayer;

            if (rp != null)
            {
                byte[] messageBytes;
                ClientMessage newMessage = new ClientMessage();
                newMessage.handled = false;
                newMessage.type = ClientMessageType.CHANGE_LEADER_REQUEST;
                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<string>(gl);
                    mw.Write<string>(gn);
                    mw.Write<string>(rp);
                    messageBytes = mw.GetMessageBytes();
                }
                newMessage.data = messageBytes;
                NetworkWorker.fetch.SendGroupCommand(newMessage, true);
            }
            else
            {
                SyncrioLog.Debug("No player selected!");
            }
        }

        public void KickPlayer()
        {
            bool lk = GroupWindow.isLeaderKickingPlayer;
            string kp = GroupWindow.chosenPlayerToKick;

            if (kp != null)
            {
                if (lk)
                {
                    byte[] messageBytes;
                    ClientMessage newMessage = new ClientMessage();
                    newMessage.handled = false;
                    newMessage.type = ClientMessageType.KICK_PLAYER_REQUEST;
                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(lk);
                        mw.Write<string>(kp);
                        messageBytes = mw.GetMessageBytes();
                    }
                    newMessage.data = messageBytes;
                    NetworkWorker.fetch.SendGroupCommand(newMessage, true);
                }
                else
                {
                    byte[] messageBytes;
                    ClientMessage newMessage = new ClientMessage();
                    newMessage.handled = false;
                    newMessage.type = ClientMessageType.KICK_PLAYER_REQUEST;
                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(lk);
                        mw.Write<string>(kp);
                        if (GroupWindow.voteKickPlayer)
                        {
                            mw.Write<int>(1);
                        }
                        else
                        {
                            if (GroupWindow.voteKeepPlayer)
                            {
                                mw.Write<int>(-1);
                            }
                            else
                            {
                                mw.Write<int>(0);
                            }
                        }
                        messageBytes = mw.GetMessageBytes();
                    }
                    newMessage.data = messageBytes;
                    NetworkWorker.fetch.SendGroupCommand(newMessage, true);
                }
            }
            else
            {
                SyncrioLog.Debug("No player selected!");
            }
        }

        public void InvitePlayer()
        {
            string pi = GroupWindow.chosenPlayerToInvite;
            string senderGroup = playerGroupName;
            string sender = Settings.fetch.playerName;
            if (pi != null)
            {
                if (!string.IsNullOrEmpty(senderGroup))
                {
                    byte[] messageBytes;
                    ClientMessage newMessage = new ClientMessage();
                    newMessage.handled = false;
                    newMessage.type = ClientMessageType.INVITE_PLAYER;
                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(false);//Is Reply
                        mw.Write<string>(pi);
                        mw.Write<string>(senderGroup);
                        mw.Write<string>(sender);
                        messageBytes = mw.GetMessageBytes();
                    }
                    newMessage.data = messageBytes;
                    NetworkWorker.fetch.SendGroupCommand(newMessage, true);
                }
                else
                {
                    SyncrioLog.Debug("Not in a group, can not invite!");
                }
            }
            else
            {
                SyncrioLog.Debug("No player selected!");
            }
        }

        public void RenameGroup()
        {
            string oldGroupName = playerGroupName;
            string newGroupName = GroupWindow.groupNameRename;
            if (!string.IsNullOrEmpty(oldGroupName))
            {
                byte[] messageBytes;
                ClientMessage newMessage = new ClientMessage();
                newMessage.handled = false;
                newMessage.type = ClientMessageType.RENAME_GROUP_REQUEST;
                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<string>(oldGroupName);
                    mw.Write<string>(newGroupName);
                    messageBytes = mw.GetMessageBytes();
                }
                newMessage.data = messageBytes;
                NetworkWorker.fetch.SendGroupCommand(newMessage, true);
            }
            else
            {
                //Don't care
            }
        }

        public void ChangeGroupPrivacy()
        {
            string groupName = playerGroupName;
            string gpass = null;
            string gp;
            bool gpSet = false;
            if (newGroupPrivacy_PUBLIC)
            {
                gp = GroupPrivacy.PUBLIC.ToString();
                gpSet = true;
            }
            else
            {
                if (newGroupPrivacy_PASSWORD)
                {
                    gp = GroupPrivacy.PRIVATE_PASSWORD.ToString();
                    gpSet = true;
                    gpass = GroupWindow.groupPasswordSet;
                }
                else
                {
                    if (newGroupPrivacy_INVITE_ONLY)
                    {
                        gp = GroupPrivacy.PRIVATE_INVITE_ONLY.ToString();
                        gpSet = true;
                    }
                    else
                    {
                        SyncrioLog.Debug("No group privacy set!");
                        gp = GroupPrivacy.PUBLIC.ToString();//This is here to fix a compiler error, DONT remove me!!!
                    }
                }
            }
            if (gpSet)
            {
                if (gp == GroupPrivacy.PRIVATE_PASSWORD.ToString())
                {
                    if (gpass != null)
                    {
                        byte[] messageBytes;
                        ClientMessage newMessage = new ClientMessage();
                        newMessage.handled = false;
                        newMessage.type = ClientMessageType.CHANGE_GROUP_PRIVACY_REQUEST;
                        using (MessageWriter mw = new MessageWriter())
                        {
                            mw.Write<string>(groupName);
                            mw.Write<string>(gp);
                            mw.Write<string>(gpass);
                            messageBytes = mw.GetMessageBytes();
                        }
                        newMessage.data = messageBytes;
                        NetworkWorker.fetch.SendGroupCommand(newMessage, true);
                    }
                    else
                    {
                        SyncrioLog.Debug("No group password set!");
                    }
                }
                else
                {
                    byte[] messageBytes;
                    ClientMessage newMessage = new ClientMessage();
                    newMessage.handled = false;
                    newMessage.type = ClientMessageType.CHANGE_GROUP_PRIVACY_REQUEST;
                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<string>(groupName);
                        mw.Write<string>(gp);
                        messageBytes = mw.GetMessageBytes();
                    }
                    newMessage.data = messageBytes;
                    NetworkWorker.fetch.SendGroupCommand(newMessage, true);
                }
            }
        }

        public void HandleGroupCreationComplete(byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                playerGroupName = mr.Read<string>();
                PlayerStatusWorker.fetch.myPlayerStatus.groupName = playerGroupName;
                groupFreeToInvite = mr.Read<bool>();
                playerCreatingGroup = false;
                playerGroupAssigned = true;
                playerIsGroupLeader = true;
            }
        }

        public void HandleGroupCreationError(byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                groupCreationError = mr.Read<string>();
                isGroupCreationResponseError = true;
            }
        }

        public void HandleChangeGroupLeaderRequest(byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                groupLeaderChange = mr.Read<string>();
                groupnameChange = mr.Read<string>();
                requestedLeader = mr.Read<string>();
                groupChangeLeaderWindowDisplay = true;
            }
        }

        public void HandleInvitePlayerRequest(byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                invitedPlayer = mr.Read<string>();
                groupnameInvite = mr.Read<string>();
                inviteSender = mr.Read<string>();
                displayInvite = true;
            }
        }

        /// <summary>
        /// Set True for Yes and False for No.
        /// </summary>
        public void ChangeGroupLeaderResponse(bool yesOrNo)
        {
            byte[] messageBytes;
            ClientMessage newMessage = new ClientMessage();
            newMessage.handled = false;
            newMessage.type = ClientMessageType.SET_LEADER;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<bool>(yesOrNo);
                mw.Write<string>(groupLeaderChange);
                mw.Write<string>(groupnameChange);
                mw.Write<string>(requestedLeader);
                messageBytes = mw.GetMessageBytes();
            }
            newMessage.data = messageBytes;
            NetworkWorker.fetch.SendGroupCommand(newMessage, true);
        }

        /// <summary>
        /// Set True for Yes and False for No.
        /// </summary>
        public void InviteResponse(bool yesOrNo)
        {
            byte[] messageBytes;
            ClientMessage newMessage = new ClientMessage();
            newMessage.handled = false;
            newMessage.type = ClientMessageType.INVITE_PLAYER;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<bool>(true);//Is Reply
                mw.Write<bool>(yesOrNo);
                mw.Write<string>(invitedPlayer);
                mw.Write<string>(groupnameInvite);
                mw.Write<string>(inviteSender);
                messageBytes = mw.GetMessageBytes();
            }
            newMessage.data = messageBytes;
            NetworkWorker.fetch.SendGroupCommand(newMessage, true);
        }

        public void HandleKickPlayerReply(byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                bool isPlayerStillBeingKicked = mr.Read<bool>();
                string playerBeingKicked = mr.Read<string>();
                if (GroupWindow.fetch.KickingPlayer.ContainsKey(playerBeingKicked))
                {
                    if (isPlayerStillBeingKicked)
                    {
                        GroupWindow.fetch.KickingPlayer[playerBeingKicked].BeingKicked = true;
                        GroupWindow.fetch.KickingPlayer[playerBeingKicked].numberOfVotes = mr.Read<int>();
                    }
                    else
                    {
                        GroupWindow.fetch.KickingPlayer[playerBeingKicked].BeingKicked = false;
                        GroupWindow.fetch.KickingPlayer[playerBeingKicked].numberOfVotes = 0;
                    }
                }
                else
                {
                    SyncrioLog.Debug("Can not find" + playerBeingKicked);
                }
            }
        }

        public bool PlayerIsInGroup(string playerName)
        {
            return (GetPlayerGroup(playerName) != null);
        }

        public string GetPlayerGroup(string playerName)
        {
            string returnGroup = null;
            lock (groups)
            {
                foreach (KeyValuePair<string, GroupObject> kvp in groups)
                {
                    if (kvp.Value.members.Contains(playerName))
                    {
                        returnGroup = kvp.Key;
                        break;
                    }
                }
            }
            return returnGroup;
        }

        public void HandleGroupProgress(byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                int numberOfProgess = mr.Read<int>();

                for (int i = 0; i < numberOfProgess; i++)
                {
                    List<string> groupProgressList = new List<string>(mr.Read<string[]>());

                    string groupName = groupProgressList[0];
                    int groupSubspace = Convert.ToInt32(groupProgressList[1]);

                    string funds = groupProgressList[2];
                    string rep = groupProgressList[3];
                    string sci = groupProgressList[4];

                    List<string> techs = new List<string>();
                    List<string> progress = new List<string>();
                    List<List<string>> celestialProgress = new List<List<string>>();
                    List<string> secrets = new List<string>();

                    int cursor = 2;
                    while (cursor < groupProgressList.Count)
                    {
                        bool increment = true;

                        if (groupProgressList[cursor] == "Techs" && (groupProgressList[cursor + 1] == "{"))
                        {
                            increment = false;

                            int matchBracketIdx = FindMatchingBracket(groupProgressList, cursor + 1);
                            KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                            if (range.Key + 2 < groupProgressList.Count && range.Value - 3 > 0)
                            {
                                techs = groupProgressList.GetRange(range.Key + 2, range.Value - 3);//Use Key + 2 and Value - 3 because that way you will only get the inside of the node.
                                groupProgressList.RemoveRange(range.Key, range.Value);
                            }
                            else
                            {
                                groupProgressList.RemoveRange(range.Key, range.Value);
                            }
                        }

                        if (cursor > groupProgressList.Count - 1)
                        {
                            break;
                        }

                        if (groupProgressList[cursor] == "Progress" && (groupProgressList[cursor + 1] == "{"))
                        {
                            increment = false;

                            int matchBracketIdx = FindMatchingBracket(groupProgressList, cursor + 1);
                            KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                            if (range.Key + 2 < groupProgressList.Count && range.Value - 3 > 0)
                            {
                                progress = groupProgressList.GetRange(range.Key + 2, range.Value - 3);//Use Key + 2 and Value - 3 because that way you will only get the inside of the node.
                                groupProgressList.RemoveRange(range.Key, range.Value);
                            }
                            else
                            {
                                groupProgressList.RemoveRange(range.Key, range.Value);
                            }
                        }

                        if (cursor > groupProgressList.Count - 1)
                        {
                            break;
                        }

                        if (groupProgressList[cursor] == "CelestialProgressList" && (groupProgressList[cursor + 1] == "{"))
                        {
                            increment = false;

                            int matchBracketIdx = FindMatchingBracket(groupProgressList, cursor + 1);
                            KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                            if (range.Key + 2 < groupProgressList.Count && range.Value - 3 > 0)
                            {
                                List<string> celestialProgressLines = groupProgressList.GetRange(range.Key + 2, range.Value - 3);//Use Key + 2 and Value - 3 because that way you will only get the inside of the node.
                                groupProgressList.RemoveRange(range.Key, range.Value);

                                int subCursor = 0;
                                while (subCursor < celestialProgressLines.Count)
                                {
                                    if (celestialProgressLines[subCursor] == "CelestialProgress" && (celestialProgressLines[subCursor + 1] == "{"))
                                    {
                                        int subMatchBracketIdx = FindMatchingBracket(celestialProgressLines, subCursor + 1);
                                        KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(subCursor, (subMatchBracketIdx - subCursor + 1));

                                        if (subRange.Key + 2 < celestialProgressLines.Count && subRange.Value - 3 > 0)
                                        {
                                            celestialProgress.Add(celestialProgressLines.GetRange(subRange.Key + 2, subRange.Value - 3));//Use Key + 2 and Value - 3 because that way you will only get the inside of the node.
                                            celestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                        }
                                        else
                                        {
                                            celestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                groupProgressList.RemoveRange(range.Key, range.Value);
                            }
                        }

                        if (cursor > groupProgressList.Count - 1)
                        {
                            break;
                        }

                        if (groupProgressList[cursor] == "Secrets" && (groupProgressList[cursor + 1] == "{"))
                        {
                            increment = false;

                            int matchBracketIdx = FindMatchingBracket(groupProgressList, cursor + 1);
                            KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                            if (range.Key + 2 < groupProgressList.Count && range.Value - 3 > 0)
                            {
                                secrets = groupProgressList.GetRange(range.Key + 2, range.Value - 3);//Use Key + 2 and Value - 3 because that way you will only get the inside of the node.
                                groupProgressList.RemoveRange(range.Key, range.Value);
                            }
                            else
                            {
                                groupProgressList.RemoveRange(range.Key, range.Value);
                            }
                        }

                        if (increment)
                        {
                            cursor++;
                        }
                    }

                    if (allGroupProgressList.ContainsKey(groupName))
                    {
                        if (allGroupProgressList[groupName].ContainsKey(groupSubspace))
                        {
                            GroupProgress editGroupProgress = allGroupProgressList[groupName][groupSubspace];

                            editGroupProgress.Funds = funds;
                            editGroupProgress.Rep = rep;
                            editGroupProgress.Sci = sci;

                            editGroupProgress.Techs = techs;
                            editGroupProgress.Progress = progress;
                            editGroupProgress.CelestialProgress = celestialProgress;
                            editGroupProgress.Secrets = secrets;

                            allGroupProgressList[groupName][groupSubspace] = editGroupProgress;
                        }
                        else
                        {
                            GroupProgress newGroupProgress = new GroupProgress();

                            newGroupProgress.GroupName = groupName;
                            newGroupProgress.GroupSubspace = groupSubspace;

                            newGroupProgress.Funds = funds;
                            newGroupProgress.Rep = rep;
                            newGroupProgress.Sci = sci;

                            newGroupProgress.Techs = techs;
                            newGroupProgress.Progress = progress;
                            newGroupProgress.CelestialProgress = celestialProgress;
                            newGroupProgress.Secrets = secrets;

                            allGroupProgressList[groupName].Add(groupSubspace, newGroupProgress);
                        }
                    }
                    else
                    {
                        GroupProgress newGroupProgress = new GroupProgress();

                        newGroupProgress.GroupName = groupName;
                        newGroupProgress.GroupSubspace = groupSubspace;

                        newGroupProgress.Funds = funds;
                        newGroupProgress.Rep = rep;
                        newGroupProgress.Sci = sci;

                        newGroupProgress.Techs = techs;
                        newGroupProgress.Progress = progress;
                        newGroupProgress.CelestialProgress = celestialProgress;
                        newGroupProgress.Secrets = secrets;

                        allGroupProgressList.Add(groupName, new Dictionary<int, GroupProgress>());

                        allGroupProgressList[groupName].Add(groupSubspace, newGroupProgress);
                    }
                }
            }
        }

        public void HandleGroupMessage(byte[] messageData)
        {
            lock (groupLock)
            {
                using (MessageReader mr = new MessageReader(messageData))
                {
                    GroupMessageType messageType = (GroupMessageType)mr.Read<int>();
                    switch (messageType)
                    {
                        case GroupMessageType.SET:
                            {
                                string groupName = mr.Read<string>();
                                GroupPrivacy groupPrivate = (GroupPrivacy)mr.Read<int>();
                                string groupSalt = null;
                                if (groupPrivate == GroupPrivacy.PRIVATE_PASSWORD)
                                {
                                    bool passwordSet = mr.Read<bool>();
                                    {
                                        if (passwordSet)
                                        {
                                            groupSalt = mr.Read<string>();
                                        }
                                    }
                                }
                                string[] groupMembers = mr.Read<string[]>();
                                if (!groups.ContainsKey(groupName))
                                {
                                    groups.Add(groupName, new GroupObject());
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(playerGroupName))
                                    {
                                        if (groupName == playerGroupName)
                                        {
                                            List<string> groupMembersList = new List<string>(groupMembers);
                                            List<string> kickingPlayerList = new List<string>(GroupWindow.fetch.KickingPlayer.Keys);
                                            List<string> kickOrSelectPlayerList = new List<string>(GroupWindow.fetch.kickOrSelectPlayerButton.Keys);
                                            bool runKickingPlayerOncePreMessage = false;
                                            bool runKickOrSelectPlayerOncePreMessage = false;
                                            foreach (string member in groupMembersList)
                                            {
                                                if (!GroupWindow.fetch.KickingPlayer.ContainsKey(member))
                                                {
                                                    GroupWindow.fetch.KickingPlayer.Add(member, new GroupWindow.KickingPlayerStatus());
                                                }
                                                else
                                                {
                                                    if (!runKickingPlayerOncePreMessage)
                                                    {
                                                        foreach (string playerName in kickingPlayerList)
                                                        {
                                                            if (!groupMembersList.Contains(playerName))
                                                            {
                                                                GroupWindow.fetch.KickingPlayer.Remove(playerName);
                                                            }
                                                        }
                                                        runKickingPlayerOncePreMessage = true;
                                                    }
                                                }

                                                if (!GroupWindow.fetch.kickOrSelectPlayerButton.ContainsKey(member))
                                                {
                                                    GroupWindow.fetch.kickOrSelectPlayerButton.Add(member, new GroupWindow.PickPlayerOrGroupButtons());
                                                }
                                                else
                                                {
                                                    if (!runKickOrSelectPlayerOncePreMessage)
                                                    {
                                                        foreach (string playerName in kickOrSelectPlayerList)
                                                        {
                                                            if (!groupMembersList.Contains(playerName))
                                                            {
                                                                GroupWindow.fetch.kickOrSelectPlayerButton.Remove(playerName);
                                                            }
                                                        }
                                                        runKickOrSelectPlayerOncePreMessage = true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                groups[groupName].members = new List<string>(groupMembers);
                                groups[groupName].privacy = groupPrivate;
                                groups[groupName].passwordSalt = groupSalt;
                                if (groupName != playerGroupName)
                                {
                                    groups[groupName].settings.inviteAvailable = mr.Read<bool>();
                                }
                                else
                                {
                                    groupFreeToInvite = mr.Read<bool>();
                                    groups[groupName].settings.inviteAvailable = groupFreeToInvite;
                                }
                                GroupWindow.fetch.AssignPlayerGroup();
                                GroupWindow.fetch.SetJoinGroupButton();
                                GroupWindow.fetch.SetInvitePlayerButton();
                                GroupWindow.fetch.CheckInvitePlayerButton();
                                SyncrioLog.Debug("Group " + groupName + " updated");
                            }
                            break;
                        case GroupMessageType.REMOVE:
                            {
                                string groupName = mr.Read<string>();
                                if (groups.ContainsKey(groupName))
                                {
                                    groups.Remove(groupName);
                                    GroupWindow.fetch.AssignPlayerGroup();
                                    GroupWindow.fetch.SetJoinGroupButton();
                                    GroupWindow.fetch.SetInvitePlayerButton();
                                    GroupWindow.fetch.SetKickOrSelectPlayerButton();
                                    GroupWindow.fetch.CheckInvitePlayerButton();
                                    SyncrioLog.Debug("Group " + groupName + " removed");
                                }
                            }
                            break;
                        default:
                            SyncrioLog.Debug("Unknown group message type: " + messageType);
                            break;
                    }
                }
            }
        }

        private static void ResetGroupValues()
        {
            GroupSystem.fetch.groups = new Dictionary<string, GroupObject>();
            GroupSystem.fetch.allGroupProgressList = new Dictionary<string, Dictionary<int, GroupProgress>>();
            playerGroupName = string.Empty;
            playerGroupAssigned = false;
            playerCreatingGroup = false;
            playerIsGroupLeader = false;
            groupFreeToInvite = false;
            setIfGroupIsFreeToInvite = true;
            kickPlayerButtons = false;
            invitePlayerButtons = false;
            groupProgressButtons = false;
            newGroupPrivacy_PUBLIC = false;
            newGroupPrivacy_PASSWORD = false;
            newGroupPrivacy_INVITE_ONLY = false;
            groupCreationError = string.Empty;
            isGroupCreationResponseError = false;
            joinGroupPrivacy_PASSWORD = false;
            disbandGroupButtonPressed = false;
            groupLeaderChange = string.Empty;
            groupnameChange = string.Empty;
            requestedLeader = string.Empty;
            groupChangeLeaderWindowDisplay = false;
            stepDownButton = false;
            inviteSender = string.Empty;
            groupnameInvite = string.Empty;
            invitedPlayer = string.Empty;
            displayInvite = false;
            renameGroupButtonPressed = false;
            changeGroupPrivacyButtonPressed = false;
        }

        public static void Reset()
        {
            lock (Client.eventLock)
            {
                singleton = new GroupSystem();
                ResetGroupValues();
            }
        }

        public static int FindMatchingBracket(List<string> lines, int startFrom)
        {
            int brackets = 0;
            for (int i = startFrom; i < lines.Count; i++)
            {
                if (lines[i].Trim() == "{") brackets++;
                if (lines[i].Trim() == "}") brackets--;

                if (brackets == 0)
                    return i;
            }

            throw new ArgumentOutOfRangeException("Could not find a matching bracket!");
        }

        public struct GroupProgress
        {
            public string GroupName;
            public int GroupSubspace;
            public string Funds;
            public string Rep;
            public string Sci;
            public List<string> Techs;
            public List<string> Progress;
            public List<List<string>> CelestialProgress;
            public List<string> Secrets;
        }
    }
}
