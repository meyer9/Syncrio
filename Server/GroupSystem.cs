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
using System.Xml;
using System.Collections.Generic;
using SyncrioCommon;
using MessageStream2;

namespace SyncrioServer
{
    public class GroupSystem
    {
        private static GroupSystem singleton;
        //Data structure
        private Dictionary<string, GroupObject> groups;
        //Groups loaded
        public bool groupsLoaded = false;
        //Number of Groups
        public static int groupCount = 0;
        //Kick Player Votes
        private Dictionary<string, KickPlayerVotes> kickPlayerVotes = new Dictionary<string, KickPlayerVotes>();
        //Directories
        public string groupDirectory
        {
            private set;
            get;
        }

        public string groupScenariosDirectory
        {
            private set;
            get;
        }

        public string playerDirectory
        {
            private set;
            get;
        }

        public GroupSystem()
        {
            groupDirectory = Path.Combine(Server.ScenarioDirectory, "GroupData", "Groups");
            groupScenariosDirectory = Path.Combine(Server.ScenarioDirectory, "GroupData", "GroupScenarios");
            playerDirectory = Path.Combine(Server.ScenarioDirectory, "Players");
            LoadGroups();
        }

        public static GroupSystem fetch
        {
            get
            {
                if (singleton == null)
                {
                    singleton = new GroupSystem();
                }
                return singleton;
            }
        }

        public void ServerStarting()
        {
            //Yes this is blank, it is only here to get "fetch" called
        }

        public Dictionary<string, GroupObject> GetCopy()
        {
            Dictionary<string, GroupObject> returnDictionary = new Dictionary<string, GroupObject>();
            lock (groups)
            {
                foreach (KeyValuePair<string, GroupObject> kvp in groups)
                {
                    GroupObject newGroupObject = new GroupObject();
                    newGroupObject.passwordSalt = kvp.Value.passwordSalt;
                    newGroupObject.passwordHash = kvp.Value.passwordHash;
                    newGroupObject.privacy = kvp.Value.privacy;
                    newGroupObject.members = new List<string>(kvp.Value.members);
                    newGroupObject.settings.inviteAvailable = kvp.Value.settings.inviteAvailable;
                    returnDictionary.Add(kvp.Key, newGroupObject);
                }
            }
            return returnDictionary;
        }

        private void LoadGroups()
        {
            SyncrioLog.Debug("Loading groups");
            groups = new Dictionary<string, GroupObject>();
            string[] groupPaths = Directory.GetDirectories(groupDirectory);
            foreach (string groupPath in groupPaths)
            {
                string groupName = Path.GetFileName(groupPath);
                GroupObject newGroup = new GroupObject();
                string thisGroupDirectory = Path.Combine(groupDirectory, groupName);
                string groupScenariosThisGroupDirectory = Path.Combine(groupScenariosDirectory, groupName);
                if (!Directory.Exists(groupScenariosThisGroupDirectory))
                {
                    Directory.CreateDirectory(groupScenariosThisGroupDirectory);
                }

                string thisGroupScenarioDirectory = Path.Combine(groupScenariosThisGroupDirectory, "Scenario");
                if (!Directory.Exists(thisGroupScenarioDirectory))
                {
                    Directory.CreateDirectory(thisGroupScenarioDirectory);
                }

                string membersFile = Path.Combine(thisGroupDirectory, "members.txt");
                string settingsFile = Path.Combine(thisGroupDirectory, "settings.txt");
                if (!File.Exists(membersFile))
                {
                    SyncrioLog.Error("Group " + groupName + " is broken (members file), skipping!");
                    continue;
                }
                if (!File.Exists(settingsFile))
                {
                    SyncrioLog.Error("Group " + groupName + " is broken (settings file), skipping!");
                    continue;
                }
                using (StreamReader sr = new StreamReader(membersFile))
                {
                    string currentLine = null;
                    while ((currentLine = sr.ReadLine()) != null)
                    {
                        newGroup.members.Add(currentLine);
                    }
                }
                int lineIndex = 0;
                using (StreamReader sr = new StreamReader(settingsFile))
                {
                    string currentLine = null;
                    while ((currentLine = sr.ReadLine()) != null)
                    {
                        switch (lineIndex)
                        {
                            case 0:
                                newGroup.privacy = (GroupPrivacy)Enum.Parse(typeof(GroupPrivacy), currentLine);
                                break;
                            case 1:
                                newGroup.settings.inviteAvailable = Convert.ToBoolean(currentLine, ScenarioSystem.english);
                                break;
                            case 2:
                                newGroup.passwordSalt = currentLine;
                                break;
                            case 3:
                                newGroup.passwordHash = currentLine;
                                break;
                        }
                        lineIndex++;
                    }
                }
                if (newGroup.members.Count > 0)
                {
                    groups.Add(groupName, newGroup);
                }
                else
                {
                    SyncrioLog.Error("Group " + groupName + " is broken (no members), skipping!");
                }
            }
            groupsLoaded = true;
            groupCount = groups.Count;
            if (groupCount > 0)
            {
                SyncrioLog.Debug(groupCount + " Groups Loaded");
            }
            else
            {
                SyncrioLog.Debug("No Groups Loaded");
            }
            SetKickPlayerVotes();
        }

        private void SaveGroup(string groupName)
        {
            if (!GroupExists(groupName))
            {
                Console.WriteLine("Cannot save group " + groupName + ", doesn't exist");
                return;
            }
            SyncrioLog.Debug("Saving " + groupName);
            GroupObject saveGroup = groups[groupName];
            string thisGroupDirectory = Path.Combine(groupDirectory, groupName);
            string groupScenariosThisGroupDirectory = Path.Combine(groupScenariosDirectory, groupName);
            if (!Directory.Exists(thisGroupDirectory))
            {
                Directory.CreateDirectory(thisGroupDirectory);
            }
            if (!Directory.Exists(groupScenariosThisGroupDirectory))
            {
                Directory.CreateDirectory(groupScenariosThisGroupDirectory);
            }
            
            string thisGroupScenarioDirectory = Path.Combine(groupScenariosThisGroupDirectory, "Scenario");
            if (!Directory.Exists(thisGroupScenarioDirectory))
            {
                Directory.CreateDirectory(thisGroupScenarioDirectory);
            }

            string membersFile = Path.Combine(thisGroupDirectory, "members.txt");
            string settingsFile = Path.Combine(thisGroupDirectory, "settings.txt");
            using (StreamWriter sw = new StreamWriter(membersFile + ".new"))
            {
                foreach (string member in saveGroup.members)
                {
                    sw.WriteLine(member);
                }
            }
            File.Copy(membersFile + ".new", membersFile, true);
            File.Delete(membersFile + ".new");
            using (StreamWriter sw = new StreamWriter(settingsFile + ".new"))
            {
                sw.WriteLine(saveGroup.privacy.ToString());
                sw.WriteLine(saveGroup.settings.inviteAvailable);
                if (saveGroup.passwordSalt != null)
                {
                    sw.WriteLine(saveGroup.passwordSalt);
                    if (saveGroup.passwordHash != null)
                    {
                        sw.WriteLine(saveGroup.passwordHash);
                    }
                    else
                    {
                        sw.WriteLine(Environment.NewLine);
                    }
                }
                else
                {
                    sw.WriteLine(Environment.NewLine);
                    sw.WriteLine(Environment.NewLine);
                }
            }
            File.Copy(settingsFile + ".new", settingsFile, true);
            File.Delete(settingsFile + ".new");
            SetKickPlayerVotes();
        }

        public bool GroupExists(string groupName)
        {
            lock (groups)
            {
                return groups.ContainsKey(groupName);
            }
        }

        public bool PlayerExists(string playerName)
        {
            return File.Exists(Path.Combine(playerDirectory, playerName + ".txt"));
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

        public List<string> GetPlayersInGroup(string groupName)
        {
            if (GroupExists(groupName))
            {
                return new List<string>(groups[groupName].members);
            }
            return null;
        }

        public string GetGroupOwner(string groupName)
        {
            if (!GroupExists(groupName))
            {
                return null;
            }
            return groups[groupName].members[0];
        }

        public int GetNumberOfPlayersInAllGroups()
        {
            int returnValue = 0;
            foreach (ClientObject client in ClientHandler.GetClients())
            {
                if (PlayerIsInGroup(client.playerName))
                {
                    returnValue += 1;
                }
            }
            return returnValue;
        }

        /// <summary>
        /// Creates the group. Returns true if successful.
        /// </summary>
        public bool CreateGroup(ClientObject callingClient, byte[] messageData)
        {
            lock (groups)
            {
                if (groupCount <= Settings.settingsStore.maxGroups)
                {
                    using (MessageReader mr = new MessageReader(messageData))
                    {
                        string ownerName = mr.Read<string>();
                        string groupName = mr.Read<string>();
                        string groupPrivacyString = mr.Read<string>();
                        GroupPrivacy groupPrivacy = GroupPrivacy.PUBLIC;
                        if (groupPrivacyString == GroupPrivacy.PUBLIC.ToString())
                        {
                            groupPrivacy = GroupPrivacy.PUBLIC;
                        }
                        else
                        {
                            if (groupPrivacyString == GroupPrivacy.PRIVATE_PASSWORD.ToString())
                            {
                                groupPrivacy = GroupPrivacy.PRIVATE_PASSWORD;
                            }
                            else
                            {
                                if (groupPrivacyString == GroupPrivacy.PRIVATE_INVITE_ONLY.ToString())
                                {
                                    groupPrivacy = GroupPrivacy.PRIVATE_INVITE_ONLY;
                                }
                            }
                        }
                        string groupPassword = string.Empty;
                        if (groupPrivacy == GroupPrivacy.PRIVATE_PASSWORD)
                        {
                            groupPassword = mr.Read<string>();
                        }
                        bool isGroupFreeToInvite = mr.Read<bool>();

                        if (GroupExists(groupName))
                        {
                            string errorText = "Cannot create group " + groupName + ", Group already exists";
                            SyncrioLog.Debug(errorText);
                            Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                            Messages.Group.ErrorCreatingGroup(callingClient, "Group Name Already Taken!");
                            return false;
                        }
                        if (PlayerIsInGroup(ownerName))
                        {
                            string errorText = "Cannot create group " + groupName + ", " + ownerName + " already belongs to a group";
                            SyncrioLog.Debug(errorText);
                            Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                            Messages.Group.ErrorCreatingGroup(callingClient, "You Already Belong To A Group!");
                            return false;
                        }
                        if (!PlayerExists(ownerName))
                        {
                            string errorText = "Cannot create group " + groupName + ", " + ownerName + " does not exist";
                            SyncrioLog.Debug(errorText);
                            Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                            Messages.Group.ErrorCreatingGroup(callingClient, "You Dont Exist! ;)");
                            return false;
                        }
                        GroupObject go = new GroupObject();
                        go.members.Add(ownerName);
                        go.privacy = groupPrivacy;
                        go.settings.inviteAvailable = isGroupFreeToInvite;
                        groups.Add(groupName, go);
                        SyncrioLog.Debug(ownerName + " created group " + groupName);
                        Messages.Chat.SendChatMessageToClient(callingClient, "You created " + groupName);
                        Messages.Group.SendGroupToAll(groupName, go);
                        SaveGroup(groupName);
                        SetGroupPasswordRaw(callingClient, groupName, groupPassword);
                        Messages.Group.CreateGroupResponse(callingClient, groupName, isGroupFreeToInvite);
                        groupCount = groups.Count;
                        return true;
                    }
                }
                else
                {
                    SyncrioLog.Debug("The Max # of Groups has been reached!");
                    Messages.Group.ErrorCreatingGroup(callingClient, "Max # of Groups reached!");
                    return false;
                }
            }
        }

        /// <summary>
        /// Creates the group from the server command system. Returns true if successful.
        /// </summary>
        public bool CreateGroupServerCommand(string groupName, string ownerName)
        {
            lock (groups)
            {
                if (groupCount <= Settings.settingsStore.maxGroups)
                {
                    if (GroupExists(groupName))
                    {
                        string errorText = "Cannot create group " + groupName + ", Group already exists";
                        SyncrioLog.Debug(errorText);
                        return false;
                    }
                    if (PlayerIsInGroup(ownerName))
                    {
                        string errorText = "Cannot create group " + groupName + ", " + ownerName + " already belongs to a group";
                        SyncrioLog.Debug(errorText);
                        return false;
                    }
                    if (!PlayerExists(ownerName))
                    {
                        string errorText = "Cannot create group " + groupName + ", " + ownerName + " does not exist";
                        SyncrioLog.Debug(errorText);
                        return false;
                    }
                    GroupObject go = new GroupObject();
                    go.members.Add(ownerName);
                    go.privacy = GroupPrivacy.PUBLIC;
                    go.settings.inviteAvailable = true;
                    groups.Add(groupName, go);
                    SyncrioLog.Debug(ownerName + " created group " + groupName);
                    Messages.Group.SendGroupToAll(groupName, go);
                    SaveGroup(groupName);
                    groupCount = groups.Count;
                    return true;
                }
                else
                {
                    SyncrioLog.Debug("The Max # of Groups has been reached!");
                    return false;
                }
            }
        }

        /// <summary>
        /// Make a player join the group. Returns true if the group was joined.
        /// </summary>
        public bool JoinGroup(ClientObject callingClient, byte[] messagedata)
        {
            lock (groups)
            {
                using (MessageReader mr = new MessageReader(messagedata))
                {
                    string groupName = mr.Read<string>();
                    string playerName = mr.Read<string>();
                    string groupPassword = mr.Read<string>();
                    bool passwordIncorrect = CheckGroupPasswordRaw(groupName, groupPassword);
                    if (!GroupExists(groupName))
                    {
                        string errorText = "Cannot join group " + groupName + ", Group does not exist";
                        SyncrioLog.Debug(errorText);
                        Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                        return false;
                    }
                    if (PlayerIsInGroup(playerName))
                    {
                        string errorText = "Cannot join group " + groupName + ", " + playerName + " already belongs to a group";
                        SyncrioLog.Debug(errorText);
                        Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                        return false;
                    }
                    if (!PlayerExists(playerName))
                    {
                        string errorText = "Cannot join group " + groupName + ", " + playerName + " doesn't exist";
                        SyncrioLog.Debug(errorText);
                        Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                        return false;
                    }
                    if (passwordIncorrect)
                    {
                        string errorText = "Incorrect Password!";
                        SyncrioLog.Debug(errorText);
                        Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                        return false;
                    }
                    GroupObject go = groups[groupName];
                    go.members.Add(playerName);
                    SyncrioLog.Debug(playerName + " joined " + groupName);
                    Messages.Chat.SendChatMessageToClient(callingClient, "You joined " + groupName);
                    Messages.Group.SendGroupToAll(groupName, go);
                    SaveGroup(groupName);
                    ScenarioSystem.fetch.ScenarioSendAllData(groupName, callingClient);
                    return true;
                }
            }
        }

        /// <summary>
        /// Make a player join the group from the server command system. Returns true if the group was joined.
        /// </summary>
        public bool JoinGroupServerCommand(ClientObject callingClient, string groupName, string playerName)
        {
            lock (groups)
            {
                if (!GroupExists(groupName))
                {
                    string errorText = "Cannot join group " + groupName + ", Group does not exist";
                    SyncrioLog.Debug(errorText);
                    Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                    return false;
                }
                if (PlayerIsInGroup(playerName))
                {
                    string errorText = "Cannot join group " + groupName + ", " + playerName + " already belongs to a group";
                    SyncrioLog.Debug(errorText);
                    Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                    return false;
                }
                if (!PlayerExists(playerName))
                {
                    string errorText = "Cannot join group " + groupName + ", " + playerName + " doesn't exist";
                    SyncrioLog.Debug(errorText);
                    Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                    return false;
                }
                GroupObject go = groups[groupName];
                go.members.Add(playerName);
                SyncrioLog.Debug(playerName + " joined " + groupName);
                Messages.Chat.SendChatMessageToClient(callingClient, "You joined " + groupName);
                Messages.Group.SendGroupToAll(groupName, go);
                SaveGroup(groupName);
                ScenarioSystem.fetch.ScenarioSendAllData(groupName, callingClient);
                return true;
            }
        }

        /// <summary>
        /// Make a player leave the group. Returns true if the group was left.
        /// </summary>
        public bool LeaveGroup(ClientObject callingClient, byte[] messagedata)
        {
            lock (groups)
            {
                using (MessageReader mr = new MessageReader(messagedata))
                {
                    string playerName = mr.Read<string>();
                    if (!PlayerExists(playerName))
                    {
                        string errorText = "Cannot leave group, " + playerName + " doesn't exist";
                        SyncrioLog.Debug(errorText);
                        Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                        return false;
                    }
                    string playerGroupName = GetPlayerGroup(playerName);
                    if (playerGroupName == null)
                    {
                        string errorText = "Cannot leave group, " + playerName + " does not belong to any a group";
                        SyncrioLog.Debug(errorText);
                        Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                        return false;
                    }
                    if (playerName == GetGroupOwner(playerGroupName))
                    {
                        if (GetPlayersInGroup(playerGroupName).Count == 1)
                        {
                            return RemoveGroupServerCommand(callingClient, playerGroupName);
                        }
                        else
                        {
                            string errorText = "Cannot leave group, " + playerName + " is the owner";
                            SyncrioLog.Debug(errorText);
                            Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                            return false;
                        }
                    }
                    GroupObject go = groups[playerGroupName];
                    go.members.Remove(playerName);
                    SyncrioLog.Debug(playerName + " left " + playerGroupName);
                    Messages.Chat.SendChatMessageToClient(callingClient, "You left " + playerGroupName);
                    Messages.Group.SendGroupToAll(playerGroupName, go);
                    SaveGroup(playerGroupName);
                    return true;
                }
            }
        }

        /// <summary>
        /// Make a player leave the group from the server command system. Returns true if the group was left.
        /// </summary>
        public bool LeaveGroupServerCommand(ClientObject callingClient, string playerName)
        {
            lock (groups)
            {
                if (!PlayerExists(playerName))
                {
                    string errorText = "Cannot leave group, " + playerName + " doesn't exist";
                    SyncrioLog.Debug(errorText);
                    Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                    return false;
                }
                string playerGroupName = GetPlayerGroup(playerName);
                if (playerGroupName == null)
                {
                    string errorText = "Cannot leave group, " + playerName + " does not belong to any a group";
                    SyncrioLog.Debug(errorText);
                    Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                    return false;
                }
                if (playerName == GetGroupOwner(playerGroupName))
                {
                    if (GetPlayersInGroup(playerGroupName).Count == 1)
                    {
                        return RemoveGroupServerCommand(callingClient, playerGroupName);
                    }
                    else
                    {
                        string errorText = "Cannot leave group, " + playerName + " is the owner";
                        SyncrioLog.Debug(errorText);
                        Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                        return false;
                    }
                }
                GroupObject go = groups[playerGroupName];
                go.members.Remove(playerName);
                SyncrioLog.Debug(playerName + " left " + playerGroupName);
                Messages.Chat.SendChatMessageToClient(callingClient, "You left " + playerGroupName);
                Messages.Group.SendGroupToAll(playerGroupName, go);
                SaveGroup(playerGroupName);
                return true;
            }
        }

        public bool RemoveGroup(ClientObject callingClient, byte[] messagedata)
        {
            lock (groups)
            {
                using (MessageReader mr = new MessageReader(messagedata))
                {
                    string groupName = mr.Read<string>();
                    if (!groups.ContainsKey(groupName))
                    {
                        string errorText = "Cannot remove group, " + groupName + " doesn't exist";
                        SyncrioLog.Debug(errorText);
                        Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                        return false;
                    }
                    GroupObject go = groups[groupName];
                    string thisGroupDirectory = Path.Combine(groupDirectory, groupName);
                    string thisGroupScenarioDirectory = Path.Combine(groupScenariosDirectory, groupName);
                    SyncrioUtil.FileHandler.DeleteDirectory(thisGroupDirectory);
                    SyncrioUtil.FileHandler.DeleteDirectory(thisGroupScenarioDirectory);
                    groups.Remove(groupName);
                    SyncrioLog.Debug("Deleted group " + groupName);
                    Messages.Chat.SendChatMessageToClient(callingClient, "You deleted " + groupName);
                    Messages.Group.RemoveGroup(groupName);
                    return true;
                }
            }
        }

        public bool RemoveGroupServerCommand(ClientObject callingClient, string groupName)
        {
            lock (groups)
            {
                if (!groups.ContainsKey(groupName))
                {
                    string errorText = "Cannot remove group, " + groupName + " doesn't exist";
                    SyncrioLog.Debug(errorText);
                    Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                    return false;
                }
                GroupObject go = groups[groupName];
                string thisGroupDirectory = Path.Combine(groupDirectory, groupName);
                string thisGroupScenarioDirectory = Path.Combine(groupScenariosDirectory, groupName);
                SyncrioUtil.FileHandler.DeleteDirectory(thisGroupDirectory);
                SyncrioUtil.FileHandler.DeleteDirectory(thisGroupScenarioDirectory);
                groups.Remove(groupName);
                SyncrioLog.Debug("Deleted group " + groupName);
                Messages.Chat.SendChatMessageToClient(callingClient, "You deleted " + groupName);
                Messages.Group.RemoveGroup(groupName);
                return true;
            }
        }

        /// <summary>
        /// Sets the group owner. If the group or player does not exist, or the player already belongs to a different group, this method returns false
        /// </summary>
        public bool SetGroupOwner(ClientObject callingClient, byte[] messagedata)
        {
            lock (groups)
            {
                using (MessageReader mr = new MessageReader(messagedata))
                {
                    bool setLeaderYesOrNo = mr.Read<bool>();
                    string groupLeaderName = mr.Read<string>();
                    string groupName = mr.Read<string>();
                    string playerName = mr.Read<string>();
                    ClientObject targetPlayer;
                    if (setLeaderYesOrNo)
                    {
                        if (!GroupExists(groupName))
                        {
                            string errorText = "Cannot set group owner, " + groupName + " doesn't exist";
                            SyncrioLog.Debug(errorText);
                            Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                            return false;
                        }
                        if (!PlayerExists(playerName))
                        {
                            string errorText = "Cannot set group owner, " + playerName + " does not exist";
                            SyncrioLog.Debug(errorText);
                            Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                            return false;
                        }
                        if (PlayerIsInGroup(playerName))
                        {
                            if (GetPlayerGroup(playerName) != groupName)
                            {
                                string errorText = "Cannot set group owner, " + playerName + " already belongs to another group";
                                SyncrioLog.Debug(errorText);
                                Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                                return false;
                            }
                        }
                        else
                        {
                            if (!JoinGroupServerCommand(callingClient, groupName, playerName))
                            {
                                string errorText = "Cannot set group owner, " + playerName + " failed to join the group";
                                SyncrioLog.Debug(errorText);
                                Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                                return false;
                            }
                        }
                        GroupObject go = groups[groupName];
                        go.members.Remove(playerName);
                        go.members.Insert(0, playerName);
                        SyncrioLog.Debug("Leader of group " + groupName + " changed to " + playerName);
                        Messages.Chat.SendChatMessageToClient(callingClient, "You became leader of " + groupName);
                        Messages.Group.SendGroupToAll(groupName, go);
                        SaveGroup(groupName);
                        return true;
                    }
                    else
                    {
                        targetPlayer = ClientHandler.GetClientByName(groupLeaderName);
                        Messages.Chat.SendChatMessageToClient(targetPlayer, playerName + " declined your offer to become Leader.");
                        return true;
                    }
                }
            }
        }

        /// <summary>
        /// Sets the group owner from the server command system. If the group or player does not exist, or the player already belongs to a different group, this method returns false
        /// </summary>
        public bool SetGroupOwnerServerCommand(ClientObject callingClient, string groupName, string playerName)
        {
            lock (groups)
            {
                if (!GroupExists(groupName))
                {
                    string errorText = "Cannot set group owner, " + groupName + " doesn't exist";
                    SyncrioLog.Debug(errorText);
                    Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                    return false;
                }
                if (!PlayerExists(playerName))
                {
                    string errorText = "Cannot set group owner, " + playerName + " does not exist";
                    SyncrioLog.Debug(errorText);
                    Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                    return false;
                }
                if (PlayerIsInGroup(playerName))
                {
                    if (GetPlayerGroup(playerName) != groupName)
                    {
                        string errorText = "Cannot set group owner, " + playerName + " already belongs to another group";
                        SyncrioLog.Debug(errorText);
                        Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                        return false;
                    }
                }
                else
                {
                    if (!JoinGroupServerCommand(callingClient, groupName, playerName))
                    {
                        string errorText = "Cannot set group owner, " + playerName + " failed to join the group";
                        SyncrioLog.Debug(errorText);
                        Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                        return false;
                    }
                }
                GroupObject go = groups[groupName];
                go.members.Remove(playerName);
                go.members.Insert(0, playerName);
                SyncrioLog.Debug("Leader of group " + groupName + " changed to " + playerName);
                Messages.Chat.SendChatMessageToClient(callingClient, "You became leader of " + groupName);
                Messages.Group.SendGroupToAll(groupName, go);
                SaveGroup(groupName);
                return true;
            }
        }

        /// <summary>
        /// Make a player leave the group. Returns true if the group was left.
        /// </summary>
        public bool KickPlayer(ClientObject callingClient, byte[] messagedata)
        {
            lock (groups)
            {
                using (MessageReader mr = new MessageReader(messagedata))
                {
                    bool LeaderKickingPlayer = mr.Read<bool>();
                    if (LeaderKickingPlayer)
                    {
                        string playerName = mr.Read<string>();
                        if (!PlayerExists(playerName))
                        {
                            string errorText = "Cannot kick from the group, " + playerName + " doesn't exist";
                            SyncrioLog.Debug(errorText);
                            Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                            return false;
                        }
                        string playerGroupName = GetPlayerGroup(playerName);
                        if (playerGroupName == null)
                        {
                            string errorText = "Cannot kick from the group, " + playerName + " does not belong to any a group";
                            SyncrioLog.Debug(errorText);
                            Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                            return false;
                        }
                        if (playerName == GetGroupOwner(playerGroupName))
                        {
                            string errorText = "Cannot kick from the group, " + playerName + " is the owner";
                            SyncrioLog.Debug(errorText);
                            Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                            return false;
                        }
                        ClientObject kickedPlayer;
                        kickedPlayer = ClientHandler.GetClientByName(playerName);
                        GroupObject go = groups[playerGroupName];
                        go.members.Remove(playerName);
                        SyncrioLog.Debug(playerName + " was kick from " + playerGroupName);
                        if (ClientHandler.ClientConnected(kickedPlayer))
                        {
                            Messages.Chat.SendChatMessageToClient(kickedPlayer, "You were kick from " + playerGroupName);
                        }
                        Messages.Group.SendGroupToAll(playerGroupName, go);
                        SaveGroup(playerGroupName);
                        return true;
                    }
                    else
                    {
                        string playerName = mr.Read<string>();
                        int PlayerVoteYesOrNo = mr.Read<int>();
                        string playerGroupName = GetPlayerGroup(playerName);
                        kickPlayerVotes[playerName].playerVotedCounter += 1;
                        kickPlayerVotes[playerName].currentVotes += PlayerVoteYesOrNo;
                        kickPlayerVotes[playerName].votesPrecent = (kickPlayerVotes[playerName].currentVotes / (groups[playerGroupName].members.Count - 1));
                        double Threshold = (Settings.settingsStore.groupKickPlayerVotesThreshold / 100);
                        if (kickPlayerVotes[playerName].votesPrecent > Threshold)
                        {
                            kickPlayerVotes[playerName].currentVotes = 0;
                            kickPlayerVotes[playerName].votesPrecent = 0;
                            kickPlayerVotes[playerName].playerVotedCounter = 0;
                            ClientObject kickedPlayer;
                            kickedPlayer = ClientHandler.GetClientByName(playerName);
                            GroupObject go = groups[playerGroupName];
                            go.members.Remove(playerName);
                            SyncrioLog.Debug(playerName + " was kick from " + playerGroupName);
                            if (ClientHandler.ClientConnected(kickedPlayer))
                            {
                                Messages.Chat.SendChatMessageToClient(kickedPlayer, "You were kick from " + playerGroupName);
                            }
                            Messages.Group.SendGroupToAll(playerGroupName, go);
                            SaveGroup(playerGroupName);
                            return true;
                        }
                        else
                        {
                            if (kickPlayerVotes[playerName].playerVotedCounter == groups[playerGroupName].members.Count - 1)
                            {
                                kickPlayerVotes[playerName].currentVotes = 0;
                                kickPlayerVotes[playerName].votesPrecent = 0;
                                kickPlayerVotes[playerName].playerVotedCounter = 0;
                                ClientObject theNotkickedPlayer;
                                theNotkickedPlayer = ClientHandler.GetClientByName(playerName);
                                using (MessageWriter mw = new MessageWriter())
                                {
                                    ServerMessage newMessage = new ServerMessage();
                                    newMessage.type = ServerMessageType.KICK_PLAYER_REPLY;
                                    mw.Write<bool>(false);
                                    mw.Write<string>(playerName);
                                    newMessage.data = mw.GetMessageBytes();
                                    ClientHandler.SendToAll(theNotkickedPlayer, newMessage, true);
                                }
                            }
                            else
                            {
                                ClientObject thePlayerBeingkicked;
                                thePlayerBeingkicked = ClientHandler.GetClientByName(playerName);
                                using (MessageWriter mw = new MessageWriter())
                                {
                                    ServerMessage newMessage = new ServerMessage();
                                    newMessage.type = ServerMessageType.KICK_PLAYER_REPLY;
                                    mw.Write<bool>(true);
                                    mw.Write<string>(playerName);
                                    mw.Write<int>(kickPlayerVotes[playerName].playerVotedCounter);
                                    newMessage.data = mw.GetMessageBytes();
                                    ClientHandler.SendToAll(thePlayerBeingkicked, newMessage, true);
                                }
                            }
                            return false;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Invite a player to join the group. Returns true if the group was Joined.
        /// </summary>
        public bool InvitePlayer(ClientObject callingClient, byte[] messagedata)
        {
            lock (groups)
            {
                using (MessageReader mr = new MessageReader(messagedata))
                {
                    bool joinGroupYesOrNo = mr.Read<bool>();
                    string playerName = mr.Read<string>();
                    string senderGroup = mr.Read<string>();
                    string senderName = mr.Read<string>();
                    ClientObject targetPlayer;
                    if (joinGroupYesOrNo)
                    {
                        if (!PlayerExists(playerName))
                        {
                            string errorText = "Cannot invite to the group, " + playerName + " doesn't exist";
                            SyncrioLog.Debug(errorText);
                            Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                            return false;
                        }
                        string playerGroupName = GetPlayerGroup(playerName);
                        if (playerGroupName != null)
                        {
                            string errorText = "Cannot invite to the group, " + playerName + " belongs to a group";
                            SyncrioLog.Debug(errorText);
                            Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                            return false;
                        }
                        ClientObject invitePlayer;
                        invitePlayer = ClientHandler.GetClientByName(playerName);
                        GroupObject go = groups[senderGroup];
                        go.members.Add(playerName);
                        SyncrioLog.Debug(playerName + " was add to " + senderGroup);
                        Messages.Group.SendGroupToAll(senderGroup, go);
                        SaveGroup(senderGroup);
                        return true;
                    }
                    else
                    {
                        targetPlayer = ClientHandler.GetClientByName(senderName);
                        Messages.Chat.SendChatMessageToClient(targetPlayer, playerName + " declined your offer to join the group.");
                        return true;
                    }
                }
            }
        }

        /// <summary>
        /// Rename group. Returns true if the group was renamed.
        /// </summary>
        public bool RenameGroupRequest(ClientObject callingClient, byte[] messagedata)
        {
            lock (groups)
            {
                using (MessageReader mr = new MessageReader(messagedata))
                {
                    string oldGroupName = mr.Read<string>();
                    string newGroupName = mr.Read<string>();
                    if (!GroupExists(oldGroupName))
                    {
                        string errorText = "Cannot rename the group, " + oldGroupName + " doesn't exist";
                        SyncrioLog.Debug(errorText);
                        Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                        return false;
                    }
                    GroupObject go = new GroupObject();
                    GroupObject goOld = groups[oldGroupName];
                    go.members = goOld.members;
                    go.privacy = goOld.privacy;
                    go.passwordHash = goOld.passwordHash;
                    go.passwordSalt = goOld.passwordSalt;
                    go.settings = goOld.settings;
                    string thisGroupDirectory = Path.Combine(groupDirectory, oldGroupName);
                    Directory.Delete(thisGroupDirectory, true);
                    groups.Remove(oldGroupName);
                    Messages.Group.RemoveGroup(oldGroupName);
                    groups.Add(newGroupName, go);
                    Messages.Group.SendGroupToAll(newGroupName, go);
                    SyncrioLog.Debug(oldGroupName + " was renamed to " + newGroupName);
                    Messages.Chat.SendChatMessageToClient(callingClient, "You renamed " + oldGroupName + " to " + newGroupName);
                    SaveGroup(newGroupName);
                    return true;
                }
            }
        }
        
        /// <summary>
        /// Change group privacy. Returns true if the group privacy was changed.
        /// </summary>
        public bool ChangeGroupPrivacyRequest(ClientObject callingClient, byte[] messageData)
        {
            lock (groups)
            {
                using (MessageReader mr = new MessageReader(messageData))
                {
                    string groupName = mr.Read<string>();
                    string groupPrivacyString = mr.Read<string>();
                    GroupPrivacy groupPrivacy = GroupPrivacy.PUBLIC;
                    if (groupPrivacyString == GroupPrivacy.PUBLIC.ToString())
                    {
                        groupPrivacy = GroupPrivacy.PUBLIC;
                    }
                    else
                    {
                        if (groupPrivacyString == GroupPrivacy.PRIVATE_PASSWORD.ToString())
                        {
                            groupPrivacy = GroupPrivacy.PRIVATE_PASSWORD;
                        }
                        else
                        {
                            if (groupPrivacyString == GroupPrivacy.PRIVATE_INVITE_ONLY.ToString())
                            {
                                groupPrivacy = GroupPrivacy.PRIVATE_INVITE_ONLY;
                            }
                        }
                    }
                    string groupPassword = string.Empty;
                    if (groupPrivacy == GroupPrivacy.PRIVATE_PASSWORD)
                    {
                        groupPassword = mr.Read<string>();
                    }

                    if (!GroupExists(groupName))
                    {
                        string errorText = "Cannot change group privacy, " + groupName + " does not exist";
                        SyncrioLog.Debug(errorText);
                        Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                        return false;
                    }
                    GroupObject go = groups[groupName];
                    go.privacy = groupPrivacy;
                    SyncrioLog.Debug(groupName + "'s privacy changed to " + groupPrivacyString);
                    Messages.Chat.SendChatMessageToClient(callingClient, "You changed " + groupName + "'s privacy level");
                    Messages.Group.SendGroupToAll(groupName, go);
                    SaveGroup(groupName);
                    SetGroupPasswordRaw(callingClient, groupName, groupPassword);
                    return true;
                }
            }
        }

        /// <summary>
        /// Sets the group password, with a raw, unencrypted password. Set to null or empty string to remove the password. Returns true on success.
        /// </summary>
        public bool SetGroupPasswordRaw(ClientObject callingClient, string groupName, string password)
        {
            lock (groups)
            {
                if (!GroupExists(groupName))
                {
                    SyncrioLog.Debug("Cannot set group password, " + groupName + " doesn't exist");
                    return false;
                }
                GroupObject go = groups[groupName];
                if (password == null || password == "")
                {
                    go.passwordSalt = null;
                    go.passwordHash = null;
                    Messages.Group.SendGroupToAll(groupName, go);
                    SaveGroup(groupName);
                    return true;
                }
                return SetGroupPassword(callingClient, groupName, Common.CalculateSHA256HashFromString(password));
            }
        }

        /// <summary>
        /// Sets the group password, with an unsalted SHA256 password. Set to null or empty string to remove the password. Returns true on success.
        /// </summary>
        public bool SetGroupPassword(ClientObject callingClient, string groupName, string passwordSHA256)
        {
            lock (groups)
            {
                if (!GroupExists(groupName))
                {
                    string errorText = "Cannot set group password, " + groupName + " doesn't exist";
                    SyncrioLog.Debug(errorText);
                    Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                    return false;
                }
                GroupObject go = groups[groupName];
                if (passwordSHA256 == null || passwordSHA256 == "")
                {
                    go.passwordSalt = null;
                    go.passwordHash = null;
                    Messages.Group.SendGroupToAll(groupName, go);
                    SaveGroup(groupName);
                    return true;
                }
                //The salt is generated by the SHA256Sum of the current tick time
                string salt = Common.CalculateSHA256HashFromString(DateTime.UtcNow.Ticks.ToString());
                string saltedPassword = Common.CalculateSHA256HashFromString(salt + passwordSHA256);
                return SetGroupPassword(callingClient, groupName, salt, saltedPassword);
            }
        }

        /// <summary>
        /// Sets the group password, with a specified salt and salted password. Returns true on success
        /// </summary>
        public bool SetGroupPassword(ClientObject callingClient, string groupName, string saltSHA256, string saltedPasswordSHA256)
        {
            lock (groups)
            {
                if (!GroupExists(groupName))
                {
                    string errorText = "Cannot set group password, " + groupName + " doesn't exist";
                    SyncrioLog.Debug(errorText);
                    Messages.Chat.SendChatMessageToClient(callingClient, errorText);
                    return false;
                }
                GroupObject go = groups[groupName];
                if (saltedPasswordSHA256 == null || saltedPasswordSHA256 == "")
                {
                    go.passwordSalt = null;
                    go.passwordHash = null;
                    Messages.Group.SendGroupToAll(groupName, go);
                    SaveGroup(groupName);
                    return true;
                }
                go.passwordSalt = saltSHA256;
                go.passwordHash = saltedPasswordSHA256;
                SyncrioLog.Debug("Password of group " + groupName + " changed");
                Messages.Chat.SendChatMessageToClient(callingClient, "You changed the password of " + groupName);
                Messages.Group.SendGroupToAll(groupName, go);
                SaveGroup(groupName);
                return true;
            }
        }

        /// <summary>
        /// Checks the group password for a match (Raw password). Returns true on success. Always returns false if the group password is not set.
        /// </summary>
        public bool CheckGroupPasswordRaw(string groupName, string rawPassword)
        {
            lock (groups)
            {
                if (!GroupExists(groupName))
                {
                    return false;
                }
                GroupObject go = groups[groupName];
                if (go.passwordSalt == null)
                {
                    return false;
                }
                if (go.passwordHash == null)
                {
                    return false;
                }
                return CheckGroupPassword(groupName, Common.CalculateSHA256HashFromString(rawPassword));
            }
        }

        /// <summary>
        /// Checks the group password for a match (Raw password). Returns true on success. Always returns false if the group password is not set.
        /// </summary>
        public bool CheckGroupPassword(string groupName, string shaPassword)
        {
            lock (groups)
            {
                if (!GroupExists(groupName))
                {
                    return false;
                }
                GroupObject go = groups[groupName];
                if (go.passwordSalt == null)
                {
                    return false;
                }
                if (go.passwordHash == null)
                {
                    return false;
                }
                string checkPassword = Common.CalculateSHA256HashFromString(go.passwordSalt + shaPassword);
                return CheckGroupPassword(groupName, go.passwordSalt, checkPassword);
            }
        }

        /// <summary>
        /// Checks the group password for a match (Raw password). Returns true on success. Always returns false if the group password is not set.
        /// </summary>
        public bool CheckGroupPassword(string groupName, string saltSHA256, string saltedPasswordSHA256)
        {
            lock (groups)
            {
                if (!GroupExists(groupName))
                {
                    return false;
                }
                GroupObject go = groups[groupName];
                if (go.passwordSalt == null)
                {
                    return false;
                }
                if (go.passwordHash == null)
                {
                    return false;
                }
                return (go.passwordSalt == saltSHA256 && go.passwordHash == saltedPasswordSHA256);
            }
        }

        /// <summary>
        /// Returns if the group member is online. If the group does not exist or the member is not in the group, returns false
        /// </summary>
        public bool IsGroupMemberOnline(string groupName, string memberName)
        {
            lock (groups)
            {
                if (GroupExists(groupName))
                {
                    if (groups[groupName].members.Contains(memberName))
                    {
                        if (ClientHandler.ClientConnectedStringClientName(memberName))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public static void Reset()
        {
            singleton = null;
        }

        public class KickPlayerVotes
        {
            //Number of kick player votes - keep player votes
            public int currentVotes = 0;
            //Number of currentVotes divided by the number of players in the group excluding the leader
            public double votesPrecent = 0;
            //Number of player votes
            public int playerVotedCounter = 0;
        }

        public void SetKickPlayerVotes()
        {
            lock (groups)
            {
                foreach (KeyValuePair<string, GroupObject> kvp in groups)
                {
                    foreach (string member in kvp.Value.members)
                    {
                        if (!kickPlayerVotes.ContainsKey(member))
                        {
                            kickPlayerVotes.Add(member, new KickPlayerVotes());
                        }
                        else
                        {
                            //Keep Me Empty.
                        }
                    }
                }
            }
        }
    }
}
