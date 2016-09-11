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
    class ScenarioSystem
    {
        private static ScenarioSystem singleton;
        public int numberOfPlayersSyncing = 0;
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
            groupDirectory = Path.Combine(Server.ScenarioDirectory, "Groups");
            groupInitialScenarioDirectory = Path.Combine(groupDirectory, "Initial");
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
        public void ScenarioInitialSync(ClientObject callingClient, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                string playerName = mr.Read<string>();
                if (GroupSystem.fetch.PlayerIsInGroup(playerName))
                {
                    string groupName = GroupSystem.fetch.GetPlayerGroup(playerName);
                    Messages.ScenarioData.SendScenarioGroupModules(callingClient, groupName);
                }
                else
                {
                    Messages.ScenarioData.SendScenarioModules(callingClient);
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
                        string groupName = mr.Read<string>();
                        if (toServer)
                        {
                            string[] scenarioName = mr.Read<string[]>();
                            string[] scenarioFundsHistory = mr.Read<string[]>();
                            string[] scenarioRepHistory = mr.Read<string[]>();
                            string[] scenarioSciHistory = mr.Read<string[]>();
                            mw.Write<string[]>(scenarioName);
                            mw.Write<string[]>(scenarioFundsHistory);
                            mw.Write<string[]>(scenarioRepHistory);
                            mw.Write<string[]>(scenarioSciHistory);
                            mw.Write<string>(groupName);
                            for (int i = 0; i < scenarioName.Length; i++)
                            {
                                mw.Write<byte[]>(mr.Read<byte[]>());
                            }
                            Messages.ScenarioData.HandleGroupScenarioModuleData(callingClient, mw.GetMessageBytes());
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
                            Messages.ScenarioData.HandleScenarioModuleData(callingClient, mw.GetMessageBytes());
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
                    string thisGroupScenarioDirectory = Path.Combine(thisGroupDirectory, "Scenario");
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

        public string[] GetScenatioFundsVersionHistory(string groupName)
        {
            string thisGroupDirectory = Path.Combine(GroupSystem.fetch.groupDirectory, groupName);
            string thisGroupScenarioFundsVersionHistory = Path.Combine(thisGroupDirectory, "SVH_funds.txt");
            if (!File.Exists(thisGroupScenarioFundsVersionHistory))
            {
                File.Create(thisGroupScenarioFundsVersionHistory).Close();
            }

            return File.ReadAllLines(thisGroupScenarioFundsVersionHistory);
        }

        public void SetScenatioFundsVersionHistory(string groupName, string[] newFundsVersionHistory)
        {
            string thisGroupDirectory = Path.Combine(GroupSystem.fetch.groupDirectory, groupName);
            string thisGroupScenarioFundsVersionHistory = Path.Combine(thisGroupDirectory, "SVH_funds.txt");

            File.WriteAllLines(thisGroupScenarioFundsVersionHistory, newFundsVersionHistory);
        }

        public string[] GetScenatioRepVersionHistory(string groupName)
        {
            string thisGroupDirectory = Path.Combine(GroupSystem.fetch.groupDirectory, groupName);
            string thisGroupScenarioRepVersionHistory = Path.Combine(thisGroupDirectory, "SVH_rep.txt");
            if (!File.Exists(thisGroupScenarioRepVersionHistory))
            {
                File.Create(thisGroupScenarioRepVersionHistory).Close();
            }

            return File.ReadAllLines(thisGroupScenarioRepVersionHistory);
        }

        public void SetScenatioRepVersionHistory(string groupName, string[] newRepVersionHistory)
        {
            string thisGroupDirectory = Path.Combine(GroupSystem.fetch.groupDirectory, groupName);
            string thisGroupScenarioRepVersionHistory = Path.Combine(thisGroupDirectory, "SVH_rep.txt");

            File.WriteAllLines(thisGroupScenarioRepVersionHistory, newRepVersionHistory);
        }

        public string[] GetScenatioSciVersionHistory(string groupName)
        {
            string thisGroupDirectory = Path.Combine(GroupSystem.fetch.groupDirectory, groupName);
            string thisGroupScenarioSciVersionHistory = Path.Combine(thisGroupDirectory, "SVH_sci.txt");
            if (!File.Exists(thisGroupScenarioSciVersionHistory))
            {
                File.Create(thisGroupScenarioSciVersionHistory).Close();
            }

            return File.ReadAllLines(thisGroupScenarioSciVersionHistory);
        }

        public void SetScenatioSciVersionHistory(string groupName, string[] newSciVersionHistory)
        {
            string thisGroupDirectory = Path.Combine(GroupSystem.fetch.groupDirectory, groupName);
            string thisGroupScenarioSciVersionHistory = Path.Combine(thisGroupDirectory, "SVH_sci.txt");

            File.WriteAllLines(thisGroupScenarioSciVersionHistory, newSciVersionHistory);
        }

        public static void Reset()
        {
            singleton = null;
        }
    }
}
