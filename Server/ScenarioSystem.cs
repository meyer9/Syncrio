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
using System.Linq;
using SyncrioCommon;
using MessageStream2;

namespace SyncrioServer
{
    class ScenarioSystem
    {
        private static ScenarioSystem singleton;
        public int numberOfPlayersSyncing = 0;

        public static SubspacesList subspaceList = new SubspacesList();
        public static object subspaceListLock = new object();
        
        //Directories
        public string groupDirectory
        {
            private set;
            get;
        }

        public string groupInitialScenarioDirectory
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

        public string playerInitialScenarioDirectory
        {
            private set;
            get;
        }

        public ScenarioSystem()
        {
            groupDirectory = Path.Combine(Server.ScenarioDirectory, "GroupData", "Groups");
            groupInitialScenarioDirectory = Path.Combine(Server.ScenarioDirectory, "GroupData", "InitialGroup");
            groupScenariosDirectory = Path.Combine(Server.ScenarioDirectory, "GroupData", "GroupScenarios");
            playerDirectory = Path.Combine(Server.ScenarioDirectory, "Players");
            playerInitialScenarioDirectory = Path.Combine(playerDirectory, "Initial");
        }

        public static ScenarioSystem fetch
        {
            get
            {
                if (singleton == null)
                {
                    singleton = new ScenarioSystem();
                }
                return singleton;
            }
        }

        public static void CreateDefaultScenario()
        {
            if (!File.Exists(Path.Combine(ScenarioSystem.fetch.groupInitialScenarioDirectory, "ScenarioNewGameIntro.txt")))
            {
                List<string> newScenario = new List<string>();

                newScenario.Add("name = ScenarioNewGameIntro");
                newScenario.Add("scene = 5, 6, 8");
                newScenario.Add("kscComplete = True");
                newScenario.Add("editorComplete = True");
                newScenario.Add("tsComplete = True");

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newScenario), Path.Combine(ScenarioSystem.fetch.groupInitialScenarioDirectory, "ScenarioNewGameIntro.txt"));

                foreach (Subspace subSpace in subspaceList.Subspaces)
                {
                    foreach (Group group in subSpace.Groups)
                    {
                        string path = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, group.GroupName, "Subspace" + subSpace.SubspaceNumber, "Scenario", "ScenarioNewGameIntro.txt");

                        if (!File.Exists(path))
                        {
                            File.Copy(Path.Combine(ScenarioSystem.fetch.groupInitialScenarioDirectory, "ScenarioNewGameIntro.txt"), path);
                        }
                    }
                }
            }
        }

        public void SyncScenario(ClientObject callingClient, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                bool isAutoReply = mr.Read<bool>();
                if (isAutoReply)
                {
                    numberOfPlayersSyncing -= 1;
                }
                bool toServer = mr.Read<bool>();//If we are syncing the server's copy(true) of the scenario or the player's copy(false) of the scenario.
                bool isInGroup = mr.Read<bool>();
                using (MessageWriter mw = new MessageWriter())
                {
                    if (isInGroup)
                    {
                        CheckSubspacesForPlayers();

                        string groupName = mr.Read<string>();
                        if (toServer)
                        {
                            int subSpace = callingClient.subspace;
                            if (subSpace == -1)
                            {
                                subSpace = Messages.WarpControl.GetLatestSubspace();
                            }
                            string[] scenarioName = mr.Read<string[]>();
                            for (int i = 0; i < scenarioName.Length; i++)
                            {
                                mw.Write<byte[]>(mr.Read<byte[]>());
                            }
                            int subSpaceIndex = subspaceList.Subspaces.FindIndex(s => s.SubspaceNumber == subSpace);
                            if (subSpaceIndex == -1)
                            {
                                lock (subspaceListLock)
                                {
                                    subspaceList.Subspaces.Add(new Subspace());
                                    subSpaceIndex = subspaceList.Subspaces.Count - 1;
                                    subspaceList.Subspaces[subSpaceIndex].SubspaceNumber = subSpace;
                                }

                                //Save the new subspace
                                SaveGroupSubspaceFile();

                                //Creat the subspace folders
                                foreach (string groupFolder in Directory.GetFiles(groupScenariosDirectory))
                                {
                                    string currentGroupFolder = Path.Combine(groupScenariosDirectory, groupFolder);
                                    string newSubspaceFolder = Path.Combine(currentGroupFolder, "Subspace" + subSpace);
                                    if (!Directory.Exists(newSubspaceFolder))
                                    {
                                        Directory.CreateDirectory(newSubspaceFolder);
                                        Directory.CreateDirectory(Path.Combine(newSubspaceFolder, "Scenario"));
                                    }
                                }
                            }
                            int groupIndex = subspaceList.Subspaces[subSpaceIndex].Groups.FindIndex(s => s.GroupName == groupName);
                            if (groupIndex == -1)
                            {
                                subspaceList.Subspaces[subSpaceIndex].Groups.Add(new Group());
                                groupIndex = subspaceList.Subspaces[subSpaceIndex].Groups.Count - 1;
                                subspaceList.Subspaces[subSpaceIndex].Groups[groupIndex].GroupName = groupName;
                            }

                            EnqueueScenarioData(scenarioName, mw.GetMessageBytes(), groupIndex, subSpaceIndex);
                            if (!isAutoReply)
                            {
                                DequeueScenarioData(groupIndex, subSpaceIndex);
                            }
                        }
                        else
                        {
                            Messages.ScenarioData.SendScenarioGroupModules(callingClient, groupName);
                        }
                    }
                    else
                    {
                        if (toServer)
                        {
                            string[] scenarioName = mr.Read<string[]>();
                            mw.Write<string[]>(scenarioName);
                            for (int i = 0; i < scenarioName.Length; i++)
                            {
                                mw.Write<byte[]>(mr.Read<byte[]>());
                            }
                            ScenarioHandler.HandleScenarioModuleData(callingClient, mw.GetMessageBytes());
                        }
                        else
                        {
                            //Dont care
                        }
                    }
                }
            }
        }

        public void ResetScenario(ClientObject callingClient, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                bool isInGroup = mr.Read<bool>();
                if (isInGroup)
                {
                    string groupName = mr.Read<string>();

                    string thisGroupDirectory = Path.Combine(groupDirectory, groupName);
                    string groupScenariosThisGroupDirectory = Path.Combine(groupScenariosDirectory, groupName);
                    string thisGroupScenarioDirectory = Path.Combine(groupScenariosThisGroupDirectory, "Subspace0", "Scenario");
                    if (Directory.Exists(thisGroupDirectory))
                    {
                        if (Directory.Exists(thisGroupScenarioDirectory))
                        {
                            if (Directory.GetFiles(thisGroupScenarioDirectory).Length > 0)
                            {
                                foreach (string file in Directory.GetFiles(thisGroupScenarioDirectory))
                                {
                                    File.Delete(file);
                                }
                            }
                            if (Directory.GetFiles(groupInitialScenarioDirectory).Length > 0)
                            {
                                foreach (string file in Directory.GetFiles(groupInitialScenarioDirectory))
                                {
                                    File.Copy(file, Path.Combine(thisGroupScenarioDirectory, Path.GetFileName(file)));
                                }
                                GroupSystem.fetch.sendResetGroupScenario(groupName, callingClient);
                            }
                            else
                            {
                                SyncrioLog.Debug("Group Initial Scenario directory is empty, can not reset the scenario!");
                            }
                        }
                        else
                        {
                            SyncrioLog.Debug("Group Scenario directory doesn't exist, can not reset the scenario!");
                        }
                    }
                    else
                    {
                        SyncrioLog.Debug("Group directory doesn't exist, can not reset the scenario!");
                    }
                }
                else
                {
                    string thisPlayerDirectory = Path.Combine(playerDirectory, callingClient.playerName);
                    if (Directory.Exists(thisPlayerDirectory))
                    {
                        if (Directory.GetFiles(thisPlayerDirectory).Length > 0)
                        {
                            foreach (string file in Directory.GetFiles(thisPlayerDirectory))
                            {
                                File.Delete(file);
                            }
                        }
                        if (Directory.GetFiles(playerInitialScenarioDirectory).Length > 0)
                        {
                            foreach (string file in Directory.GetFiles(playerInitialScenarioDirectory))
                            {
                                File.Copy(file, Path.Combine(thisPlayerDirectory, Path.GetFileName(file)));
                            }
                            Messages.ScenarioData.SendScenarioModules(callingClient);
                            Messages.Chat.SendChatMessageToClient(callingClient, "You have reset your scenario!");
                        }
                        else
                        {
                            SyncrioLog.Debug("Player Initial Scenario directory is empty, can not reset the scenario!");
                        }
                    }
                    else
                    {
                        SyncrioLog.Debug("Player directory doesn't exist, can not reset the scenario!");
                    }
                }
            }
        }

        public void SendAutoSyncScenarioRequest(ClientObject targetClient)
        {
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.AUTO_SYNC_SCENARIO_REQUEST;
            newMessage.data = null;
            ClientHandler.SendToClient(targetClient, newMessage, true);
        }

        public void EnqueueScenarioData(string[] scenarioName, byte[] scenarioDataPile, int groupIndex, int subSpaceIndex)
        {
            using (MessageReader mr = new MessageReader(scenarioDataPile))
            {
                for (int i = 0; i < scenarioName.Length; i++)
                {
                    byte[] scenarioData = Compression.DecompressIfNeeded(mr.Read<byte[]>());

                    if (scenarioData != null)
                    {
                        if (subspaceList.Subspaces[subSpaceIndex].Groups[groupIndex].GroupQueuedScenarioData.ContainsKey(scenarioName[i]))
                        {
                            subspaceList.Subspaces[subSpaceIndex].Groups[groupIndex].GroupQueuedScenarioData[scenarioName[i]].Add(scenarioData);
                        }
                        else
                        {
                            subspaceList.Subspaces[subSpaceIndex].Groups[groupIndex].GroupQueuedScenarioData.Add(scenarioName[i], new List<byte[]>());

                            subspaceList.Subspaces[subSpaceIndex].Groups[groupIndex].GroupQueuedScenarioData[scenarioName[i]].Add(scenarioData);
                        }
                    }
                }
            }
        }
        
        public bool DequeueScenarioData(int groupIndex, int subSpaceIndex)
        {
            lock (subspaceListLock)
            {
                Dictionary<string, List<byte[]>> dequeuedScenarioDataList = new Dictionary<string, List<byte[]>>();

                if (subspaceList.Subspaces[subSpaceIndex].Groups[groupIndex].GroupQueuedScenarioData.Count > 0)
                {
                    dequeuedScenarioDataList = subspaceList.Subspaces[subSpaceIndex].Groups[groupIndex].GroupQueuedScenarioData;
                    subspaceList.Subspaces[subSpaceIndex].Groups[groupIndex].GroupQueuedScenarioData = new Dictionary<string, List<byte[]>>();
                }
                else
                {
                    string groupName = subspaceList.Subspaces[subSpaceIndex].Groups[groupIndex].GroupName;
                    int subSpaceNumber = subspaceList.Subspaces[subSpaceIndex].SubspaceNumber;
                    SyncrioLog.Debug("The queue for group: " + groupName + ", in subspace: " + subSpaceNumber + ", is empty.");
                    return false;
                }
                
                ScenarioHandler.HandleAllGroupScenarioData(subspaceList.Subspaces[subSpaceIndex].Groups[groupIndex].GroupName, dequeuedScenarioDataList, subspaceList.Subspaces[subSpaceIndex].SubspaceNumber);

                return true;
            }
        }
        
        public bool DequeueAllScenarioData()
        {
            lock (subspaceListLock)
            {
                foreach (Subspace subSpace in subspaceList.Subspaces)
                {
                    foreach (Group group in subSpace.Groups)
                    {
                        Dictionary<string, List<byte[]>> dequeuedScenarioDataList = new Dictionary<string, List<byte[]>>();

                        if (group.GroupQueuedScenarioData.Count > 0)
                        {
                            dequeuedScenarioDataList = group.GroupQueuedScenarioData;
                            group.GroupQueuedScenarioData = new Dictionary<string, List<byte[]>>();
                        }
                        else
                        {
                            string groupName = group.GroupName;
                            int subSpaceNumber = subSpace.SubspaceNumber;
                            SyncrioLog.Debug("The queue for group: " + groupName + ", in subspace: " + subSpaceNumber + ", is empty.");
                            return false;
                        }
                        
                        ScenarioHandler.HandleAllGroupScenarioData(group.GroupName, dequeuedScenarioDataList, subSpace.SubspaceNumber);
                    }
                }

                return true;
            }
        }

        public static void CheckSubspacesForPlayers()
        {
            lock (subspaceListLock)
            {
                SubspacesList endResult = CopySubspacesList(subspaceList);

                foreach (int key in Messages.WarpControl.playersInSubspaces.Keys)
                {
                    int playersInThisSubspace = 0;
                    Messages.WarpControl.playersInSubspaces.TryGetValue(key, out playersInThisSubspace);
                    if (playersInThisSubspace <= 0)
                    {
                        for (int i = endResult.Subspaces.Count - 1; i >= 0; i--)
                        {
                            if (endResult.Subspaces[i].SubspaceNumber == key)
                            {
                                ScenarioHandler.MergeEmptySubSpaceWithNewestOne(key);
                                endResult.Subspaces.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }

                subspaceList = CopySubspacesList(endResult);
            }

            SaveGroupSubspaceFile();
        }

        public static void LoadGroupSubspaceFile()
        {
            string gsFile = Path.Combine(Server.ScenarioDirectory, "GroupData", "GroupSubspaces.txt");

            lock (subspaceListLock)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(gsFile))
                    {
                        //Ignore the comment line.
                        string firstLine = "";
                        while (firstLine.StartsWith("#") || String.IsNullOrEmpty(firstLine))
                        {
                            if (sr.EndOfStream)
                            {
                                throw new Exception();
                            }
                            firstLine = sr.ReadLine().Trim();
                        }
                        Subspace firstLineSubspace = new Subspace();
                        firstLineSubspace.SubspaceNumber = Int32.Parse(firstLine);
                        subspaceList.Subspaces.Add(firstLineSubspace);
                        Messages.WarpControl.playersInSubspaces.Add(firstLineSubspace.SubspaceNumber, 0);
                        while (!sr.EndOfStream)
                        {
                            Subspace newSubspace = new Subspace();
                            newSubspace.SubspaceNumber = Int32.Parse(sr.ReadLine().Trim());
                            subspaceList.Subspaces.Add(newSubspace);
                            Messages.WarpControl.playersInSubspaces.Add(newSubspace.SubspaceNumber, 0);
                        }
                    }
                }
                catch
                {
                    Subspace newSubspace = new Subspace();
                    newSubspace.SubspaceNumber = 0;
                    subspaceList.Subspaces.Add(newSubspace);
                    SaveGroupSubspaceFile();
                }
            }
        }

        public static void SaveGroupSubspaceFile()
        {
            string gsFile = Path.Combine(Server.ScenarioDirectory, "GroupData", "GroupSubspaces.txt");

            lock (subspaceListLock)
            {
                using (StreamWriter sw = new StreamWriter(gsFile))
                {
                    sw.WriteLine("#Incorrectly editing this file will cause the server to crash.");
                    sw.WriteLine("#This file can only be edited if the server is stopped.");
                    for (int i = 0; i < subspaceList.Subspaces.Count; i++)
                    {
                        sw.WriteLine(subspaceList.Subspaces[i].SubspaceNumber);
                    }
                }
            }
        }

        public static SubspacesList CopySubspacesList(SubspacesList imputlist)
        {
            SubspacesList outputList = new SubspacesList();

            foreach (Subspace ss in imputlist.Subspaces)
            {
                Subspace newSS = new Subspace();

                newSS.SubspaceNumber = ss.SubspaceNumber;

                foreach (Group group in ss.Groups)
                {
                    Group newGroup = new Group();

                    newGroup.GroupName = group.GroupName;

                    newGroup.GroupQueuedScenarioData = group.GroupQueuedScenarioData;

                    newSS.Groups.Add(newGroup);
                }

                outputList.Subspaces.Add(newSS);
            }

            return outputList;
        }

        public class SubspacesList
        {
            public List<Subspace> Subspaces = new List<Subspace>();
        }
        public class Subspace
        {
            public int SubspaceNumber = 0;
            public List<Group> Groups = new List<Group>();
        }
        public class Group
        {
            public string GroupName = "";
            public Dictionary<string, List<byte[]>> GroupQueuedScenarioData = new Dictionary<string, List<byte[]>>();
        }
    }
}
