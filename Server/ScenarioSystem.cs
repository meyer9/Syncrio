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

        public void ScenarioSendStartData(ClientObject client)
        {
            List<byte[]> data = new List<byte[]>();

            List<string> newScenarioName = new List<string>();

            newScenarioName.Add("ScenarioNewGameIntro");

            data.Add(SyncrioUtil.ByteArraySerializer.Serialize(newScenarioName));

            List<string> newScenario = new List<string>();

            newScenario.Add("name = ScenarioNewGameIntro");
            newScenario.Add("scene = 5, 6, 8");
            newScenario.Add("kscComplete = True");
            newScenario.Add("editorComplete = True");
            newScenario.Add("tsComplete = True");

            data.Add(SyncrioUtil.ByteArraySerializer.Serialize(newScenario));
            
            string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, client.playerName);
            string scenarioFolder = Path.Combine(playerFolder, "Scenario");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            string filePath = Path.Combine(scenarioFolder, "ResourceScenario.txt");

            if (File.Exists(filePath))
            {
                List<string> tempList = new List<string>();
                tempList.Add("ResourceScenario");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath));
            }

            Messages.ScenarioData.SendStartData(client, data);
        }

        public void ScenarioSendStartData(string groupName, ClientObject client)
        {
            List<byte[]> data = new List<byte[]>();

            List<string> newScenarioName = new List<string>();

            newScenarioName.Add("ScenarioNewGameIntro");

            data.Add(SyncrioUtil.ByteArraySerializer.Serialize(newScenarioName));

            List<string> newScenario = new List<string>();

            newScenario.Add("name = ScenarioNewGameIntro");
            newScenario.Add("scene = 5, 6, 8");
            newScenario.Add("kscComplete = True");
            newScenario.Add("editorComplete = True");
            newScenario.Add("tsComplete = True");

            data.Add(SyncrioUtil.ByteArraySerializer.Serialize(newScenario));

            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Scenario");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            string filePath = Path.Combine(scenarioFolder, "ResourceScenario.txt");

            if (File.Exists(filePath))
            {
                List<string> tempList = new List<string>();
                tempList.Add("ResourceScenario");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath));
            }

            Messages.ScenarioData.SendStartData(client, data);
        }

        /*
        public void RevertScenario(ClientObject callingClient, byte[] messageData)
        {

        }
        */

        public void SyncScenario(ClientObject callingClient, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                ScenarioDataType type = (ScenarioDataType)mr.Read<int>();

                SyncrioLog.Debug(callingClient.playerName + " sent data type: " + type.ToString());

                try
                {
                    byte[] subData = mr.Read<byte[]>();
                    using (MessageReader subDataReader = new MessageReader(subData))
                    {
                        bool isInGroup = subDataReader.Read<bool>();
                        string groupName = string.Empty;

                        if (isInGroup)
                        {
                            groupName = subDataReader.Read<string>();
                        }

                        switch (type)
                        {
                            case ScenarioDataType.CONTRACT_UPDATED:
                                {
                                    byte[] cnData = subDataReader.Read<byte[]>();
                                    List<string> cnLines = SyncrioUtil.ByteArraySerializer.Deserialize(cnData);

                                    cnLines = SyncrioUtil.DataCleaner.BasicClean(cnLines);

                                    if (isInGroup)
                                    {
                                        if (GroupSystem.fetch.GroupExists(groupName))
                                        {
                                            ScenarioUpdateContract(cnLines, groupName);

                                            int number = subDataReader.Read<int>();

                                            if (number != 0)
                                            {
                                                List<string> weightsList = new List<string>();

                                                for (int i = 0; i < number; i++)
                                                {
                                                    string weight = subDataReader.Read<string>();
                                                    int amount = subDataReader.Read<int>();

                                                    weightsList.Add(weight + " : " + amount.ToString());
                                                }

                                                ScenarioSetWeights(weightsList, groupName);
                                            }

                                            ScenarioSendData(groupName, ScenarioDataType.CONTRACT_UPDATED, callingClient);
                                        }
                                    }
                                    else
                                    {
                                        ScenarioUpdateContract(cnLines, callingClient);

                                        int number = subDataReader.Read<int>();

                                        if (number != 0)
                                        {
                                            List<string> weightsList = new List<string>();

                                            for (int i = 0; i < number; i++)
                                            {
                                                string weight = subDataReader.Read<string>();
                                                int amount = subDataReader.Read<int>();

                                                weightsList.Add(weight + " : " + amount.ToString());
                                            }

                                            ScenarioSetWeights(weightsList, callingClient);
                                        }
                                    }
                                }
                                break;
                            case ScenarioDataType.CONTRACT_OFFERED:
                                {
                                    byte[] cnData = subDataReader.Read<byte[]>();
                                    List<string> cnLines = SyncrioUtil.ByteArraySerializer.Deserialize(cnData);

                                    cnLines = SyncrioUtil.DataCleaner.BasicClean(cnLines);

                                    if (isInGroup)
                                    {
                                        if (GroupSystem.fetch.GroupExists(groupName))
                                        {
                                            ScenarioAddContract(cnLines, groupName);

                                            ScenarioSendData(groupName, ScenarioDataType.CONTRACT_OFFERED, callingClient);
                                        }
                                    }
                                    else
                                    {
                                        ScenarioAddContract(cnLines, callingClient);
                                    }
                                }
                                break;
                            case ScenarioDataType.CUSTOM_WAYPOINT_LOAD:
                                {
                                    string wpName = subDataReader.Read<string>();
                                    byte[] wpData = subDataReader.Read<byte[]>();
                                    List<string> wpLines = SyncrioUtil.ByteArraySerializer.Deserialize(wpData);

                                    wpLines = SyncrioUtil.DataCleaner.BasicClean(wpLines);

                                    if (isInGroup)
                                    {
                                        if (GroupSystem.fetch.GroupExists(groupName))
                                        {
                                            ScenarioSaveLoadedWaypoint(wpName, wpLines, groupName);

                                            ScenarioSendData(groupName, ScenarioDataType.CUSTOM_WAYPOINT_LOAD, callingClient);
                                        }
                                    }
                                    else
                                    {
                                        ScenarioSaveLoadedWaypoint(wpName, wpLines, callingClient);
                                    }
                                }
                                break;
                            case ScenarioDataType.CUSTOM_WAYPOINT_SAVE:
                                {
                                    string wpName = subDataReader.Read<string>();
                                    byte[] wpData = subDataReader.Read<byte[]>();
                                    List<string> wpLines = SyncrioUtil.ByteArraySerializer.Deserialize(wpData);

                                    wpLines = SyncrioUtil.DataCleaner.BasicClean(wpLines);

                                    if (isInGroup)
                                    {
                                        if (GroupSystem.fetch.GroupExists(groupName))
                                        {
                                            ScenarioSaveWaypoint(wpName, wpLines, groupName);

                                            ScenarioSendData(groupName, ScenarioDataType.CUSTOM_WAYPOINT_SAVE, callingClient);
                                        }
                                    }
                                    else
                                    {
                                        ScenarioSaveWaypoint(wpName, wpLines, callingClient);
                                    }
                                }
                                break;
                            case ScenarioDataType.FUNDS_CHANGED:
                                {
                                    double value = subDataReader.Read<double>();

                                    if (isInGroup)
                                    {
                                        if (GroupSystem.fetch.GroupExists(groupName))
                                        {
                                            ScenarioChangeCurrency(value, groupName);

                                            ScenarioSendData(groupName, ScenarioDataType.FUNDS_CHANGED, callingClient);
                                        }
                                    }
                                    else
                                    {
                                        ScenarioChangeCurrency(value, callingClient);
                                    }
                                }
                                break;
                            case ScenarioDataType.REPUTATION_CHANGED:
                                {
                                    float value = subDataReader.Read<float>();

                                    if (isInGroup)
                                    {
                                        if (GroupSystem.fetch.GroupExists(groupName))
                                        {
                                            if (callingClient.subspace != -1)
                                            {
                                                ScenarioChangeCurrency(1, value, groupName);
                                            }
                                            else
                                            {
                                                if (callingClient.lastSubspace != -1)
                                                {
                                                    ScenarioChangeCurrency(1, value, groupName);
                                                }
                                            }

                                            ScenarioSendData(groupName, ScenarioDataType.REPUTATION_CHANGED, callingClient);
                                        }
                                    }
                                    else
                                    {
                                        ScenarioChangeCurrency(1, value, callingClient);
                                    }
                                }
                                break;
                            case ScenarioDataType.SCIENCE_CHANGED:
                                {
                                    float value = subDataReader.Read<float>();

                                    if (isInGroup)
                                    {
                                        if (GroupSystem.fetch.GroupExists(groupName))
                                        {
                                            ScenarioChangeCurrency(2, value, groupName);

                                            ScenarioSendData(groupName, ScenarioDataType.SCIENCE_CHANGED, callingClient);
                                        }
                                    }
                                    else
                                    {
                                        ScenarioChangeCurrency(2, value, callingClient);
                                    }
                                }
                                break;
                            case ScenarioDataType.KSC_FACILITY_UPGRADED:
                                {
                                    string facilityID = subDataReader.Read<string>();
                                    int level = subDataReader.Read<int>();

                                    if (isInGroup)
                                    {
                                        if (GroupSystem.fetch.GroupExists(groupName))
                                        {
                                            ScenarioBuildingUpgrade(facilityID, level, groupName);

                                            ScenarioSendData(groupName, ScenarioDataType.KSC_FACILITY_UPGRADED, callingClient);
                                        }
                                    }
                                    else
                                    {
                                        ScenarioBuildingUpgrade(facilityID, level, callingClient);
                                    }
                                }
                                break;
                            case ScenarioDataType.KSC_STRUCTURE_COLLAPSED:
                                {
                                    string buildingID = subDataReader.Read<string>();

                                    if (isInGroup)
                                    {
                                        if (GroupSystem.fetch.GroupExists(groupName))
                                        {
                                            ScenarioBuildingBreak(buildingID, groupName);

                                            ScenarioSendData(groupName, ScenarioDataType.KSC_STRUCTURE_COLLAPSED, callingClient);
                                        }
                                    }
                                    else
                                    {
                                        ScenarioBuildingBreak(buildingID, callingClient);
                                    }
                                }
                                break;
                            case ScenarioDataType.KSC_STRUCTURE_REPAIRED:
                                {
                                    string buildingID = subDataReader.Read<string>();

                                    if (isInGroup)
                                    {
                                        if (GroupSystem.fetch.GroupExists(groupName))
                                        {
                                            ScenarioBuildingFix(buildingID, groupName);

                                            ScenarioSendData(groupName, ScenarioDataType.KSC_STRUCTURE_REPAIRED, callingClient);
                                        }
                                    }
                                    else
                                    {
                                        ScenarioBuildingFix(buildingID, callingClient);
                                    }
                                }
                                break;
                            case ScenarioDataType.PART_PURCHASED:
                                {
                                    string partID = subDataReader.Read<string>();
                                    string techNeededID = subDataReader.Read<string>();

                                    if (isInGroup)
                                    {
                                        if (GroupSystem.fetch.GroupExists(groupName))
                                        {
                                            ScenarioAddPart(partID, techNeededID, groupName);

                                            ScenarioSendData(groupName, ScenarioDataType.PART_PURCHASED, callingClient);
                                        }
                                    }
                                    else
                                    {
                                        ScenarioAddPart(partID, techNeededID, callingClient);
                                    }
                                }
                                break;
                            case ScenarioDataType.PART_UPGRADE_PURCHASED:
                                {
                                    string upgradeID = subDataReader.Read<string>();
                                    string techNeededID = subDataReader.Read<string>();

                                    if (isInGroup)
                                    {
                                        if (GroupSystem.fetch.GroupExists(groupName))
                                        {
                                            ScenarioAddUpgrade(upgradeID, techNeededID, groupName);

                                            ScenarioSendData(groupName, ScenarioDataType.PART_UPGRADE_PURCHASED, callingClient);
                                        }
                                    }
                                    else
                                    {
                                        ScenarioAddUpgrade(upgradeID, techNeededID, callingClient);
                                    }
                                }
                                break;
                            case ScenarioDataType.PROGRESS_UPDATED:
                                {
                                    string progressID = subDataReader.Read<string>();
                                    byte[] pnData = subDataReader.Read<byte[]>();
                                    List<string> pnLines = SyncrioUtil.ByteArraySerializer.Deserialize(pnData);

                                    pnLines = SyncrioUtil.DataCleaner.BasicClean(pnLines);

                                    if (isInGroup)
                                    {
                                        if (GroupSystem.fetch.GroupExists(groupName))
                                        {
                                            ScenarioAddProgress(progressID, pnLines, groupName);

                                            ScenarioSendData(groupName, ScenarioDataType.PROGRESS_UPDATED, callingClient);
                                        }
                                    }
                                    else
                                    {
                                        ScenarioAddProgress(progressID, pnLines, callingClient);
                                    }
                                }
                                break;
                            case ScenarioDataType.TECHNOLOGY_RESEARCHED:
                                {
                                    string techID = subDataReader.Read<string>();
                                    List<string> techNode = SyncrioUtil.ByteArraySerializer.Deserialize(subDataReader.Read<byte[]>());

                                    int numberOfParts = subDataReader.Read<int>();

                                    List<string> parts = new List<string>();

                                    for (int i = 0; i < numberOfParts; i++)
                                    {
                                        parts.Add(subDataReader.Read<string>());
                                    }

                                    if (isInGroup)
                                    {
                                        if (GroupSystem.fetch.GroupExists(groupName))
                                        {
                                            ScenarioAddTech(techID, techNode, parts, groupName);

                                            ScenarioSendData(groupName, ScenarioDataType.TECHNOLOGY_RESEARCHED, callingClient);
                                        }
                                    }
                                    else
                                    {
                                        ScenarioAddTech(techID, techNode, parts, callingClient);
                                    }
                                }
                                break;
                            case ScenarioDataType.SCIENCE_RECIEVED:
                                {
                                    string sciID = subDataReader.Read<string>();
                                    float dataValue = subDataReader.Read<float>();
                                    List<string> sciNode = SyncrioUtil.ByteArraySerializer.Deserialize(subDataReader.Read<byte[]>());

                                    if (isInGroup)
                                    {
                                        if (GroupSystem.fetch.GroupExists(groupName))
                                        {
                                            ScenarioScienceRecieved(sciID, dataValue, sciNode, groupName);

                                            ScenarioSendData(groupName, ScenarioDataType.SCIENCE_RECIEVED, callingClient);
                                        }
                                    }
                                    else
                                    {
                                        ScenarioScienceRecieved(sciID, dataValue, sciNode, callingClient);
                                    }
                                }
                                break;
                            case ScenarioDataType.RESOURCE_SCENARIO:
                                {
                                    if (isInGroup)
                                    {
                                        if (GroupSystem.fetch.GroupExists(groupName))
                                        {
                                            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                                            string scenarioFolder = Path.Combine(groupFolder, "Scenario");
                                            string filePath = Path.Combine(scenarioFolder, "ResourceScenario.txt");

                                            SyncrioUtil.FileHandler.WriteToFile(subDataReader.Read<byte[]>(), filePath);

                                            ScenarioSendData(groupName, ScenarioDataType.RESOURCE_SCENARIO, callingClient);
                                        }
                                    }
                                    else
                                    {
                                        string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, callingClient.playerName);
                                        string filePath = Path.Combine(playerFolder, "ResourceScenario.txt");

                                        SyncrioUtil.FileHandler.WriteToFile(subDataReader.Read<byte[]>(), filePath);
                                    }
                                }
                                break;
                            case ScenarioDataType.STRATEGY_SYSTEM:
                                {
                                    if (isInGroup)
                                    {
                                        if (GroupSystem.fetch.GroupExists(groupName))
                                        {
                                            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                                            string scenarioFolder = Path.Combine(groupFolder, "Scenario");
                                            string filePath = Path.Combine(scenarioFolder, "StrategySystem.txt");

                                            SyncrioUtil.FileHandler.WriteToFile(subDataReader.Read<byte[]>(), filePath);

                                            ScenarioSendData(groupName, ScenarioDataType.STRATEGY_SYSTEM, callingClient);
                                        }
                                    }
                                    else
                                    {
                                        string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, callingClient.playerName);
                                        string filePath = Path.Combine(playerFolder, "StrategySystem.txt");

                                        SyncrioUtil.FileHandler.WriteToFile(subDataReader.Read<byte[]>(), filePath);
                                    }
                                }
                                break;
                            default:
                                {
                                    //Nothing for now.
                                }
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    SyncrioLog.Debug("Error syncing data type: " + type.ToString() + ", error: " + e);
                }
            }
        }

        public void ScenarioSendData(string groupName, ScenarioDataType type, ClientObject excludedClient)
        {
            List<byte[]> data = new List<byte[]>();
            switch (type)
            {
                case ScenarioDataType.CONTRACT_UPDATED:
                case ScenarioDataType.CONTRACT_OFFERED:
                    {
                        string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                        string scenarioFolder = Path.Combine(groupFolder, "Scenario");
                        string filePath = Path.Combine(scenarioFolder, "Contracts.txt");
                        string filePath2 = Path.Combine(scenarioFolder, "Weights.txt");

                        if (!Directory.Exists(scenarioFolder))
                        {
                            Directory.CreateDirectory(scenarioFolder);
                        }

                        if (File.Exists(filePath))
                        {
                            List<string> tempList = new List<string>();
                            tempList.Add("Contracts");
                            data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                            data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath));
                        }

                        if (File.Exists(filePath2))
                        {
                            List<string> tempList = new List<string>();
                            tempList.Add("Weights");
                            data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                            data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath));
                        }
                    }
                    break;
                case ScenarioDataType.CUSTOM_WAYPOINT_LOAD:
                case ScenarioDataType.CUSTOM_WAYPOINT_SAVE:
                    {
                        string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                        string scenarioFolder = Path.Combine(groupFolder, "Scenario");
                        string filePath = Path.Combine(scenarioFolder, "Waypoints.txt");

                        if (!Directory.Exists(scenarioFolder))
                        {
                            Directory.CreateDirectory(scenarioFolder);
                        }

                        if (File.Exists(filePath))
                        {
                            List<string> tempList = new List<string>();
                            tempList.Add("Waypoints");
                            data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                            data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath));
                        }
                    }
                    break;
                case ScenarioDataType.FUNDS_CHANGED:
                case ScenarioDataType.REPUTATION_CHANGED:
                case ScenarioDataType.SCIENCE_CHANGED:
                    {
                        string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                        string scenarioFolder = Path.Combine(groupFolder, "Scenario");
                        string filePath = Path.Combine(scenarioFolder, "Currency.txt");

                        if (!Directory.Exists(scenarioFolder))
                        {
                            Directory.CreateDirectory(scenarioFolder);
                        }

                        if (File.Exists(filePath))
                        {
                            List<string> tempList = new List<string>();
                            tempList.Add("Currency");
                            data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                            data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath));
                        }
                    }
                    break;
                case ScenarioDataType.KSC_FACILITY_UPGRADED:
                    {
                        string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                        string scenarioFolder = Path.Combine(groupFolder, "Scenario");
                        string filePath = Path.Combine(scenarioFolder, "BuildingLevel.txt");

                        if (!Directory.Exists(scenarioFolder))
                        {
                            Directory.CreateDirectory(scenarioFolder);
                        }

                        if (File.Exists(filePath))
                        {
                            List<string> tempList = new List<string>();
                            tempList.Add("BuildingLevel");
                            data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                            data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath));
                        }
                    }
                    break;
                case ScenarioDataType.KSC_STRUCTURE_COLLAPSED:
                case ScenarioDataType.KSC_STRUCTURE_REPAIRED:
                    {
                        string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                        string scenarioFolder = Path.Combine(groupFolder, "Scenario");
                        string filePath = Path.Combine(scenarioFolder, "BuildingDead.txt");

                        if (!Directory.Exists(scenarioFolder))
                        {
                            Directory.CreateDirectory(scenarioFolder);
                        }

                        if (File.Exists(filePath))
                        {
                            List<string> tempList = new List<string>();
                            tempList.Add("BuildingDead");
                            data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                            data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath));
                        }

                        string filePath2 = Path.Combine(scenarioFolder, "BuildingAlive.txt");

                        if (File.Exists(filePath2))
                        {
                            List<string> tempList = new List<string>();
                            tempList.Add("BuildingAlive");
                            data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                            data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath2));
                        }
                    }
                    break;
                case ScenarioDataType.PART_PURCHASED:
                    {
                        string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                        string scenarioFolder = Path.Combine(groupFolder, "Scenario");
                        string filePath = Path.Combine(scenarioFolder, "Parts.txt");

                        if (!Directory.Exists(scenarioFolder))
                        {
                            Directory.CreateDirectory(scenarioFolder);
                        }

                        if (File.Exists(filePath))
                        {
                            List<string> tempList = new List<string>();
                            tempList.Add("Parts");
                            data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                            data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath));
                        }
                    }
                    break;
                case ScenarioDataType.PART_UPGRADE_PURCHASED:
                    {
                        string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                        string scenarioFolder = Path.Combine(groupFolder, "Scenario");
                        string filePath = Path.Combine(scenarioFolder, "Upgrades.txt");

                        if (!Directory.Exists(scenarioFolder))
                        {
                            Directory.CreateDirectory(scenarioFolder);
                        }

                        if (File.Exists(filePath))
                        {
                            List<string> tempList = new List<string>();
                            tempList.Add("Upgrades");
                            data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                            data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath));
                        }
                    }
                    break;
                case ScenarioDataType.PROGRESS_UPDATED:
                    {
                        string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                        string scenarioFolder = Path.Combine(groupFolder, "Scenario");
                        string filePath = Path.Combine(scenarioFolder, "Progress.txt");

                        if (!Directory.Exists(scenarioFolder))
                        {
                            Directory.CreateDirectory(scenarioFolder);
                        }

                        if (File.Exists(filePath))
                        {
                            List<string> tempList = new List<string>();
                            tempList.Add("Progress");
                            data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                            data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath));
                        }
                    }
                    break;
                case ScenarioDataType.TECHNOLOGY_RESEARCHED:
                    {
                        string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                        string scenarioFolder = Path.Combine(groupFolder, "Scenario");
                        string filePath = Path.Combine(scenarioFolder, "Tech.txt");

                        if (!Directory.Exists(scenarioFolder))
                        {
                            Directory.CreateDirectory(scenarioFolder);
                        }

                        if (File.Exists(filePath))
                        {
                            List<string> tempList = new List<string>();
                            tempList.Add("Tech");
                            data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                            data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath));
                        }
                    }
                    break;
                case ScenarioDataType.SCIENCE_RECIEVED:
                    {
                        string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                        string scenarioFolder = Path.Combine(groupFolder, "Scenario");
                        string filePath = Path.Combine(scenarioFolder, "ScienceRecieved.txt");

                        if (!Directory.Exists(scenarioFolder))
                        {
                            Directory.CreateDirectory(scenarioFolder);
                        }

                        if (File.Exists(filePath))
                        {
                            List<string> tempList = new List<string>();
                            tempList.Add("ScienceRecieved");
                            data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                            data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath));
                        }
                    }
                    break;
                case ScenarioDataType.RESOURCE_SCENARIO:
                    {
                        string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                        string scenarioFolder = Path.Combine(groupFolder, "Scenario");
                        string filePath = Path.Combine(scenarioFolder, "ResourceScenario.txt");

                        if (!Directory.Exists(scenarioFolder))
                        {
                            Directory.CreateDirectory(scenarioFolder);
                        }

                        if (File.Exists(filePath))
                        {
                            List<string> tempList = new List<string>();
                            tempList.Add("ResourceScenario");
                            data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                            data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath));
                        }
                    }
                    break;
                case ScenarioDataType.STRATEGY_SYSTEM:
                    {
                        string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                        string scenarioFolder = Path.Combine(groupFolder, "Scenario");
                        string filePath = Path.Combine(scenarioFolder, "StrategySystem.txt");

                        if (!Directory.Exists(scenarioFolder))
                        {
                            Directory.CreateDirectory(scenarioFolder);
                        }

                        if (File.Exists(filePath))
                        {
                            List<string> tempList = new List<string>();
                            tempList.Add("StrategySystem");
                            data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                            data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath));
                        }
                    }
                    break;
                default:
                    {

                    }
                    break;
            }

            Dictionary<string, GroupObject> groups = GroupSystem.fetch.GetCopy();

            foreach (ClientObject client in ClientHandler.GetClients())
            {
                if (client != excludedClient)
                {
                    if (groups[groupName].members.Contains(client.playerName))
                    {
                        Messages.ScenarioData.SendScenarioModules(client, data);
                    }
                }
            }
        }

        public void ScenarioSendAllData(string groupName, ClientObject player)
        {
            List<byte[]> data = new List<byte[]>();

            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Scenario");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            string filePath = Path.Combine(scenarioFolder, "Contracts.txt");

            if (File.Exists(filePath))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Contracts");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath));
            }

            string filePath2 = Path.Combine(scenarioFolder, "Waypoints.txt");

            if (File.Exists(filePath2))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Waypoints");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath2));
            }

            string filePath3 = Path.Combine(scenarioFolder, "Currency.txt");

            if (File.Exists(filePath3))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Currency");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath3));
            }

            string filePath4 = Path.Combine(scenarioFolder, "BuildingLevel.txt");

            if (File.Exists(filePath4))
            {
                List<string> tempList = new List<string>();
                tempList.Add("BuildingLevel");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath4));
            }

            string filePath5 = Path.Combine(scenarioFolder, "BuildingDead.txt");

            if (File.Exists(filePath5))
            {
                List<string> tempList = new List<string>();
                tempList.Add("BuildingDead");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath5));
            }

            string filePath6 = Path.Combine(scenarioFolder, "BuildingAlive.txt");

            if (File.Exists(filePath6))
            {
                List<string> tempList = new List<string>();
                tempList.Add("BuildingAlive");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath6));
            }

            string filePath7 = Path.Combine(scenarioFolder, "Progress.txt");

            if (File.Exists(filePath7))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Progress");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath7));
            }

            string filePath8 = Path.Combine(scenarioFolder, "Tech.txt");

            if (File.Exists(filePath8))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Tech");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath8));
            }

            string filePath9 = Path.Combine(scenarioFolder, "ScienceRecieved.txt");

            if (File.Exists(filePath9))
            {
                List<string> tempList = new List<string>();
                tempList.Add("ScienceRecieved");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath9));
            }

            string filePath10 = Path.Combine(scenarioFolder, "Parts.txt");

            if (File.Exists(filePath10))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Parts");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath10));
            }

            string filePath11 = Path.Combine(scenarioFolder, "Upgrades.txt");

            if (File.Exists(filePath11))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Upgrades");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath11));
            }

            string filePath12 = Path.Combine(scenarioFolder, "Weights.txt");

            if (File.Exists(filePath12))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Weights");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath12));
            }

            string filePath13 = Path.Combine(scenarioFolder, "ResourceScenario.txt");

            if (File.Exists(filePath13))
            {
                List<string> tempList = new List<string>();
                tempList.Add("ResourceScenario");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath13));
            }

            string filePath14 = Path.Combine(scenarioFolder, "StrategySystem.txt");

            if (File.Exists(filePath14))
            {
                List<string> tempList = new List<string>();
                tempList.Add("StrategySystem");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath14));
            }

            Dictionary<string, GroupObject> groups = GroupSystem.fetch.GetCopy();

            if (groups[groupName].members.Contains(player.playerName))
            {
                Messages.ScenarioData.SendScenarioModules(player, data);
            }
        }

        public void ScenarioSendAllData(ClientObject player)
        {
            List<byte[]> data = new List<byte[]>();

            string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, player.playerName);
            string scenarioFolder = Path.Combine(playerFolder, "Scenario");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            string filePath = Path.Combine(scenarioFolder, "Contracts.txt");

            if (File.Exists(filePath))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Contracts");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath));
            }

            string filePath2 = Path.Combine(scenarioFolder, "Waypoints.txt");

            if (File.Exists(filePath2))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Waypoints");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath2));
            }

            string filePath3 = Path.Combine(scenarioFolder, "Currency.txt");

            if (File.Exists(filePath3))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Currency");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath3));
            }

            string filePath4 = Path.Combine(scenarioFolder, "BuildingLevel.txt");

            if (File.Exists(filePath4))
            {
                List<string> tempList = new List<string>();
                tempList.Add("BuildingLevel");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath4));
            }

            string filePath5 = Path.Combine(scenarioFolder, "BuildingDead.txt");

            if (File.Exists(filePath5))
            {
                List<string> tempList = new List<string>();
                tempList.Add("BuildingDead");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath5));
            }

            string filePath6 = Path.Combine(scenarioFolder, "BuildingAlive.txt");

            if (File.Exists(filePath6))
            {
                List<string> tempList = new List<string>();
                tempList.Add("BuildingAlive");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath6));
            }

            string filePath7 = Path.Combine(scenarioFolder, "Progress.txt");

            if (File.Exists(filePath7))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Progress");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath7));
            }

            string filePath8 = Path.Combine(scenarioFolder, "Tech.txt");

            if (File.Exists(filePath8))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Tech");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath8));
            }

            string filePath9 = Path.Combine(scenarioFolder, "ScienceRecieved.txt");

            if (File.Exists(filePath9))
            {
                List<string> tempList = new List<string>();
                tempList.Add("ScienceRecieved");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath9));
            }

            string filePath10 = Path.Combine(scenarioFolder, "Parts.txt");

            if (File.Exists(filePath10))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Parts");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath10));
            }

            string filePath11 = Path.Combine(scenarioFolder, "Upgrades.txt");

            if (File.Exists(filePath11))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Upgrades");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath11));
            }

            string filePath12 = Path.Combine(scenarioFolder, "Weights.txt");

            if (File.Exists(filePath12))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Weights");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath12));
            }

            string filePath13 = Path.Combine(scenarioFolder, "ResourceScenario.txt");

            if (File.Exists(filePath13))
            {
                List<string> tempList = new List<string>();
                tempList.Add("ResourceScenario");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath13));
            }

            string filePath14 = Path.Combine(scenarioFolder, "StrategySystem.txt");

            if (File.Exists(filePath14))
            {
                List<string> tempList = new List<string>();
                tempList.Add("StrategySystem");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath14));
            }

            Messages.ScenarioData.SendScenarioModules(player, data);
        }

        private void ScenarioSetWeights(List<string> weights, string groupName)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "Weights.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(weights), filePath);
        }

        private void ScenarioSetWeights(List<string> weights, ClientObject client)
        {
            string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, client.playerName);
            string filePath = Path.Combine(playerFolder, "Weights.txt");

            if (!Directory.Exists(playerFolder))
            {
                Directory.CreateDirectory(playerFolder);
            }

            SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(weights), filePath);
        }

        private void ScenarioUpdateContract(List<string> cnLines, string groupName)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "Contracts.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                if (cnLines.Any(i => i.StartsWith("guid")))
                {
                    int cnIndex = cnLines.FindIndex(i => i.StartsWith("guid"));

                    if (oldList.Any(i => i == cnLines[cnIndex]))
                    {
                        int oldIndex = oldList.FindIndex(i => i == cnLines[cnIndex]);

                        int looped = 0;
                        while (oldList[oldIndex - looped] != "ContractNode" && looped <= 20)
                        {
                            looped++;
                        }

                        if (oldList[oldIndex - looped] == "ContractNode")
                        {
                            int tempIndex = oldIndex - looped;
                            int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(oldList, tempIndex + 1);
                            KeyValuePair<int, int> range = new KeyValuePair<int, int>(tempIndex, (matchBracketIdx - tempIndex) + 1);

                            if (range.Key + 2 < oldList.Count && range.Value - 3 > 0)
                            {
                                oldList.RemoveRange(range.Key + 2, range.Value - 3);

                                oldList.InsertRange(range.Key + 2, cnLines);
                            }
                        }
                    }
                    else
                    {
                        oldList.Add("ContractNode");
                        oldList.Add("{");
                        oldList.AddRange(cnLines);
                        oldList.Add("}");
                    }
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                if (cnLines.Any(i => i.StartsWith("guid")))
                {
                    int cnIndex = cnLines.FindIndex(i => i.StartsWith("guid"));

                    if (newList.Any(i => i == cnLines[cnIndex]))
                    {
                        int oldIndex = newList.FindIndex(i => i == cnLines[cnIndex]);

                        int looped = 0;
                        while (newList[oldIndex - looped] != "ContractNode" && looped <= 20)
                        {
                            looped++;
                        }

                        if (newList[oldIndex - looped] == "ContractNode")
                        {
                            int tempIndex = oldIndex - looped;
                            int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(newList, tempIndex + 1);
                            KeyValuePair<int, int> range = new KeyValuePair<int, int>(tempIndex, (matchBracketIdx - tempIndex) + 1);

                            if (range.Key + 2 < newList.Count && range.Value - 3 > 0)
                            {
                                newList.RemoveRange(range.Key + 2, range.Value - 3);

                                newList.InsertRange(range.Key + 2, cnLines);
                            }
                        }
                    }
                    else
                    {
                        newList.Add("ContractNode");
                        newList.Add("{");
                        newList.AddRange(cnLines);
                        newList.Add("}");
                    }
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioUpdateContract(List<string> cnLines, ClientObject client)
        {
            string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, client.playerName);
            string filePath = Path.Combine(playerFolder, "Contracts.txt");

            if (!Directory.Exists(playerFolder))
            {
                Directory.CreateDirectory(playerFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                if (cnLines.Any(i => i.StartsWith("guid")))
                {
                    int cnIndex = cnLines.FindIndex(i => i.StartsWith("guid"));

                    if (oldList.Any(i => i == cnLines[cnIndex]))
                    {
                        int oldIndex = oldList.FindIndex(i => i == cnLines[cnIndex]);

                        int looped = 0;
                        while (oldList[oldIndex - looped] != "ContractNode" && looped <= 20)
                        {
                            looped++;
                        }

                        if (oldList[oldIndex - looped] == "ContractNode")
                        {
                            int tempIndex = oldIndex - looped;
                            int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(oldList, tempIndex + 1);
                            KeyValuePair<int, int> range = new KeyValuePair<int, int>(tempIndex, (matchBracketIdx - tempIndex) + 1);

                            if (range.Key + 2 < oldList.Count && range.Value - 3 > 0)
                            {
                                oldList.RemoveRange(range.Key + 2, range.Value - 3);

                                oldList.InsertRange(range.Key + 2, cnLines);
                            }
                        }
                    }
                    else
                    {
                        oldList.Add("ContractNode");
                        oldList.Add("{");
                        oldList.AddRange(cnLines);
                        oldList.Add("}");
                    }
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                if (cnLines.Any(i => i.StartsWith("guid")))
                {
                    int cnIndex = cnLines.FindIndex(i => i.StartsWith("guid"));

                    if (newList.Any(i => i == cnLines[cnIndex]))
                    {
                        int oldIndex = newList.FindIndex(i => i == cnLines[cnIndex]);

                        int looped = 0;
                        while (newList[oldIndex - looped] != "ContractNode" && looped <= 20)
                        {
                            looped++;
                        }

                        if (newList[oldIndex - looped] == "ContractNode")
                        {
                            int tempIndex = oldIndex - looped;
                            int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(newList, tempIndex + 1);
                            KeyValuePair<int, int> range = new KeyValuePair<int, int>(tempIndex, (matchBracketIdx - tempIndex) + 1);

                            if (range.Key + 2 < newList.Count && range.Value - 3 > 0)
                            {
                                newList.RemoveRange(range.Key + 2, range.Value - 3);

                                newList.InsertRange(range.Key + 2, cnLines);
                            }
                        }
                    }
                    else
                    {
                        newList.Add("ContractNode");
                        newList.Add("{");
                        newList.AddRange(cnLines);
                        newList.Add("}");
                    }
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioAddContract(List<string> cnLines, string groupName)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "Contracts.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                if (cnLines.Any(i => i.StartsWith("guid")))
                {
                    int cnIndex = cnLines.FindIndex(i => i.StartsWith("guid"));

                    if (!oldList.Any(i => i == cnLines[cnIndex]))
                    {
                        oldList.Add("ContractNode");
                        oldList.Add("{");
                        oldList.AddRange(cnLines);
                        oldList.Add("}");
                    }
                    else
                    {
                        return;
                    }
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                if (cnLines.Any(i => i.StartsWith("guid")))
                {
                    int cnIndex = cnLines.FindIndex(i => i.StartsWith("guid"));

                    if (!newList.Any(i => i == cnLines[cnIndex]))
                    {
                        newList.Add("ContractNode");
                        newList.Add("{");
                        newList.AddRange(cnLines);
                        newList.Add("}");
                    }
                    else
                    {
                        return;
                    }
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioAddContract(List<string> cnLines, ClientObject client)
        {
            string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, client.playerName);
            string filePath = Path.Combine(playerFolder, "Contracts.txt");

            if (!Directory.Exists(playerFolder))
            {
                Directory.CreateDirectory(playerFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                if (cnLines.Any(i => i.StartsWith("guid")))
                {
                    int cnIndex = cnLines.FindIndex(i => i.StartsWith("guid"));

                    if (!oldList.Any(i => i == cnLines[cnIndex]))
                    {
                        oldList.Add("ContractNode");
                        oldList.Add("{");
                        oldList.AddRange(cnLines);
                        oldList.Add("}");
                    }
                    else
                    {
                        return;
                    }
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                if (cnLines.Any(i => i.StartsWith("guid")))
                {
                    int cnIndex = cnLines.FindIndex(i => i.StartsWith("guid"));

                    if (!newList.Any(i => i == cnLines[cnIndex]))
                    {
                        newList.Add("ContractNode");
                        newList.Add("{");
                        newList.AddRange(cnLines);
                        newList.Add("}");
                    }
                    else
                    {
                        return;
                    }
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioSaveLoadedWaypoint(string wpID, List<string> wpLines, string groupName)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "Waypoints.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                if (!oldList.Any(i => i == "Waypoint : " + wpID))
                {
                    oldList.Add("Waypoint : " + wpID);
                    oldList.Add("{");
                    oldList.AddRange(wpLines);
                    oldList.Add("}");
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add("Waypoint : " + wpID);
                newList.Add("{");
                newList.AddRange(wpLines);
                newList.Add("}");

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioSaveLoadedWaypoint(string wpID, List<string> wpLines, ClientObject client)
        {
            string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, client.playerName);
            string filePath = Path.Combine(playerFolder, "Waypoints.txt");

            if (!Directory.Exists(playerFolder))
            {
                Directory.CreateDirectory(playerFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                if (!oldList.Any(i => i == "Waypoint : " + wpID))
                {
                    oldList.Add("Waypoint : " + wpID);
                    oldList.Add("{");
                    oldList.AddRange(wpLines);
                    oldList.Add("}");
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add("Waypoint : " + wpID);
                newList.Add("{");
                newList.AddRange(wpLines);
                newList.Add("}");

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioSaveWaypoint(string wpID, List<string> wpLines, string groupName)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "Waypoints.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                int cursor = oldList.FindIndex(i => i == "Waypoint : " + wpID);

                if (cursor != -1 && oldList[cursor + 1] == "{")
                {
                    List<string> newWaypointLines = new List<string>();
                    int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(oldList, cursor + 1);
                    KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor) + 1);

                    oldList.RemoveRange(range.Key, range.Value);

                    oldList.InsertRange(range.Key, wpLines);
                }
                else
                {
                    oldList.Add("Waypoint : " + wpID);
                    oldList.Add("{");
                    oldList.AddRange(wpLines);
                    oldList.Add("}");
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add("Waypoint : " + wpID);
                newList.Add("{");
                newList.AddRange(wpLines);
                newList.Add("}");

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }
        
        private void ScenarioSaveWaypoint(string wpID, List<string> wpLines, ClientObject client)
        {
            string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, client.playerName);
            string filePath = Path.Combine(playerFolder, "Waypoints.txt");

            if (!Directory.Exists(playerFolder))
            {
                Directory.CreateDirectory(playerFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                int cursor = oldList.FindIndex(i => i == "Waypoint : " + wpID);

                if (cursor != -1 && oldList[cursor + 1] == "{")
                {
                    List<string> newWaypointLines = new List<string>();
                    int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(oldList, cursor + 1);
                    KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor) + 1);

                    oldList.RemoveRange(range.Key, range.Value);

                    oldList.InsertRange(range.Key, wpLines);
                }
                else
                {
                    oldList.Add("Waypoint : " + wpID);
                    oldList.Add("{");
                    oldList.AddRange(wpLines);
                    oldList.Add("}");
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add("Waypoint : " + wpID);
                newList.Add("{");
                newList.AddRange(wpLines);
                newList.Add("}");

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        /// <summary>
        /// This version is for funds only.
        /// </summary>
        private void ScenarioChangeCurrency(double value, string groupName)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "Currency.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                oldList[0] = Convert.ToString(value);

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add(Convert.ToString(0));
                newList.Add(Convert.ToString(0));
                newList.Add(Convert.ToString(0));

                newList[0] = Convert.ToString(Convert.ToDouble(newList[0]) + value);

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        /// <summary>
        /// This version is for funds only.
        /// </summary>
        private void ScenarioChangeCurrency(double value, ClientObject client)
        {
            string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, client.playerName);
            string filePath = Path.Combine(playerFolder, "Currency.txt");

            if (!Directory.Exists(playerFolder))
            {
                Directory.CreateDirectory(playerFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                oldList[0] = Convert.ToString(value);

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add(Convert.ToString(0));
                newList.Add(Convert.ToString(0));
                newList.Add(Convert.ToString(0));

                newList[0] = Convert.ToString(Convert.ToDouble(newList[0]) + value);

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        /// <summary>
        /// Set type to either 1 or 2. 1 == Reputation. 2 == Science.
        /// </summary>
        private void ScenarioChangeCurrency(int type, float value, string groupName)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "Currency.txt");

            if (type != 1 && type != 2)
            {
                return;
            }

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                oldList[type] = Convert.ToString(value);

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add(Convert.ToString(0));
                newList.Add(Convert.ToString(0));
                newList.Add(Convert.ToString(0));

                newList[type] = Convert.ToString(Convert.ToSingle(newList[type]) + value);

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }
        
        /// <summary>
        /// Set type to either 1 or 2. 1 == Reputation. 2 == Science.
        /// </summary>
        private void ScenarioChangeCurrency(int type, float value, ClientObject client)
        {
            string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, client.playerName);
            string filePath = Path.Combine(playerFolder, "Currency.txt");

            if (type != 1 && type != 2)
            {
                return;
            }

            if (!Directory.Exists(playerFolder))
            {
                Directory.CreateDirectory(playerFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                oldList[type] = Convert.ToString(value);

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add(Convert.ToString(0));
                newList.Add(Convert.ToString(0));
                newList.Add(Convert.ToString(0));

                newList[type] = Convert.ToString(Convert.ToSingle(newList[type]) + value);

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }
        
        private void ScenarioBuildingUpgrade(string buliding, int level, string groupName)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "BuildingLevel.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                string startString = buliding + " = ";

                int cursor = oldList.FindIndex(i => i.StartsWith(startString));

                if (cursor != -1)
                {
                    if (Convert.ToInt32(oldList[cursor].Substring(startString.Length)) < level)
                    {
                        oldList[cursor] = startString + level.ToString();
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    oldList.Add(startString + level.ToString());
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                string startString = buliding + " = ";

                int cursor = newList.FindIndex(i => i.StartsWith(startString));

                if (cursor != -1)
                {
                    if (Convert.ToInt32(newList[cursor].Substring(startString.Length)) < level)
                    {
                        newList[cursor] = startString + level.ToString();
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    newList.Add(startString + level.ToString());
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }
        
        private void ScenarioBuildingUpgrade(string buliding, int level, ClientObject client)
        {
            string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, client.playerName);
            string filePath = Path.Combine(playerFolder, "BuildingLevel.txt");

            if (!Directory.Exists(playerFolder))
            {
                Directory.CreateDirectory(playerFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                string startString = buliding + " = ";

                int cursor = oldList.FindIndex(i => i.StartsWith(startString));

                if (cursor != -1)
                {
                    if (Convert.ToInt32(oldList[cursor].Substring(startString.Length)) < level)
                    {
                        oldList[cursor] = startString + level.ToString();
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    oldList.Add(startString + level.ToString());
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                string startString = buliding + " = ";

                int cursor = newList.FindIndex(i => i.StartsWith(startString));

                if (cursor != -1)
                {
                    if (Convert.ToInt32(newList[cursor].Substring(startString.Length)) < level)
                    {
                        newList[cursor] = startString + level.ToString();
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    newList.Add(startString + level.ToString());
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }
        
        private void ScenarioBuildingBreak(string buliding, string groupName)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "BuildingDead.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                int cursor = oldList.FindIndex(i => i == buliding);

                if (cursor == -1)
                {
                    oldList.Add(buliding);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                int cursor = newList.FindIndex(i => i == buliding);

                if (cursor == -1)
                {
                    newList.Add(buliding);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }


            string filePath2 = Path.Combine(scenarioFolder, "BuildingAlive.txt");

            if (File.Exists(filePath2))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath2));

                int cursor = oldList.FindIndex(i => i == buliding);

                if (cursor != -1)
                {
                    oldList.RemoveAt(cursor);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath2);
            }
            else
            {
                List<string> newList = new List<string>();

                int cursor = newList.FindIndex(i => i == buliding);

                if (cursor != -1)
                {
                    newList.RemoveAt(cursor);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath2);
            }
        }
        
        private void ScenarioBuildingBreak(string buliding, ClientObject client)
        {
            string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, client.playerName);
            string filePath = Path.Combine(playerFolder, "BuildingDead.txt");

            if (!Directory.Exists(playerFolder))
            {
                Directory.CreateDirectory(playerFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                int cursor = oldList.FindIndex(i => i == buliding);

                if (cursor == -1)
                {
                    oldList.Add(buliding);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                int cursor = newList.FindIndex(i => i == buliding);

                if (cursor == -1)
                {
                    newList.Add(buliding);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }


            string filePath2 = Path.Combine(playerFolder, "BuildingAlive.txt");

            if (File.Exists(filePath2))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath2));

                int cursor = oldList.FindIndex(i => i == buliding);

                if (cursor != -1)
                {
                    oldList.RemoveAt(cursor);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath2);
            }
            else
            {
                List<string> newList = new List<string>();

                int cursor = newList.FindIndex(i => i == buliding);

                if (cursor != -1)
                {
                    newList.RemoveAt(cursor);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath2);
            }
        }
        
        private void ScenarioBuildingFix(string buliding, string groupName)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "BuildingAlive.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                int cursor = oldList.FindIndex(i => i == buliding);

                if (cursor == -1)
                {
                    oldList.Add(buliding);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                int cursor = newList.FindIndex(i => i == buliding);

                if (cursor == -1)
                {
                    newList.Add(buliding);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }


            string filePath2 = Path.Combine(scenarioFolder, "BuildingDead.txt");

            if (File.Exists(filePath2))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath2));

                int cursor = oldList.FindIndex(i => i == buliding);

                if (cursor != -1)
                {
                    oldList.RemoveAt(cursor);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath2);
            }
            else
            {
                List<string> newList = new List<string>();

                int cursor = newList.FindIndex(i => i == buliding);

                if (cursor != -1)
                {
                    newList.RemoveAt(cursor);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath2);
            }
        }
        
        private void ScenarioBuildingFix(string buliding, ClientObject client)
        {
            string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, client.playerName);
            string filePath = Path.Combine(playerFolder, "BuildingAlive.txt");

            if (!Directory.Exists(playerFolder))
            {
                Directory.CreateDirectory(playerFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                int cursor = oldList.FindIndex(i => i == buliding);

                if (cursor == -1)
                {
                    oldList.Add(buliding);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                int cursor = newList.FindIndex(i => i == buliding);

                if (cursor == -1)
                {
                    newList.Add(buliding);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }


            string filePath2 = Path.Combine(playerFolder, "BuildingDead.txt");

            if (File.Exists(filePath2))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath2));

                int cursor = oldList.FindIndex(i => i == buliding);

                if (cursor != -1)
                {
                    oldList.RemoveAt(cursor);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath2);
            }
            else
            {
                List<string> newList = new List<string>();

                int cursor = newList.FindIndex(i => i == buliding);

                if (cursor != -1)
                {
                    newList.RemoveAt(cursor);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath2);
            }
        }

        private void ScenarioAddPart(string part, string techNeeded, string groupName)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "Parts.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                if (!oldList.Any(i => i == part + " : " + techNeeded))
                {
                    oldList.Add(part + " : " + techNeeded);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                if (!newList.Any(i => i == part + " : " + techNeeded))
                {
                    newList.Add(part + " : " + techNeeded);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioAddPart(string part, string techNeeded, ClientObject client)
        {
            string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, client.playerName);
            string filePath = Path.Combine(playerFolder, "Parts.txt");

            if (!Directory.Exists(playerFolder))
            {
                Directory.CreateDirectory(playerFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                if (!oldList.Any(i => i == part + " : " + techNeeded))
                {
                    oldList.Add(part + " : " + techNeeded);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                if (!newList.Any(i => i == part + " : " + techNeeded))
                {
                    newList.Add(part + " : " + techNeeded);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioAddUpgrade(string upgrade, string techNeeded, string groupName)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "Upgrades.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                if (!oldList.Any(i => i == upgrade + " : " + techNeeded))
                {
                    oldList.Add(upgrade + " : " + techNeeded);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                if (!newList.Any(i => i == upgrade + " : " + techNeeded))
                {
                    newList.Add(upgrade + " : " + techNeeded);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioAddUpgrade(string upgrade, string techNeeded, ClientObject client)
        {
            string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, client.playerName);
            string filePath = Path.Combine(playerFolder, "Upgrades.txt");

            if (!Directory.Exists(playerFolder))
            {
                Directory.CreateDirectory(playerFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                if (!oldList.Any(i => i == upgrade + " : " + techNeeded))
                {
                    oldList.Add(upgrade + " : " + techNeeded);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                if (!newList.Any(i => i == upgrade + " : " + techNeeded))
                {
                    newList.Add(upgrade + " : " + techNeeded);
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioAddProgress(string progressID, List<string> progressLines, string groupName)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "Progress.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                if (!oldList.Contains("ProgressNode : " + progressID))
                {
                    oldList.Add("ProgressNode : " + progressID);
                    oldList.Add("{");
                    oldList.AddRange(progressLines);
                    oldList.Add("}");

                    SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
                }
                else
                {
                    int index = oldList.FindIndex(i => i == "ProgressNode : " + progressID);

                    if (oldList[index + 1] == "{")
                    {
                        int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(oldList, index + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(index, (matchBracketIdx - index) + 1);

                        oldList.RemoveRange(range.Key + 2, range.Value - 3);

                        oldList.InsertRange(range.Key + 2, progressLines);

                        SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
                    }
                }
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add("ProgressNode : " + progressID);
                newList.Add("{");
                newList.AddRange(progressLines);
                newList.Add("}");

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioAddProgress(string progressID, List<string> progressLines, ClientObject client)
        {
            string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, client.playerName);
            string filePath = Path.Combine(playerFolder, "Progress.txt");

            if (!Directory.Exists(playerFolder))
            {
                Directory.CreateDirectory(playerFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                if (!oldList.Contains("ProgressNode : " + progressID))
                {
                    oldList.Add("ProgressNode : " + progressID);
                    oldList.Add("{");
                    oldList.AddRange(progressLines);
                    oldList.Add("}");

                    SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
                }
                else
                {
                    int index = oldList.FindIndex(i => i == "ProgressNode : " + progressID);

                    if (oldList[index + 1] == "{")
                    {
                        int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(oldList, index + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(index, (matchBracketIdx - index) + 1);

                        oldList.RemoveRange(range.Key + 2, range.Value - 3);

                        oldList.InsertRange(range.Key + 2, progressLines);

                        SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
                    }
                }
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add("ProgressNode : " + progressID);
                newList.Add("{");
                newList.AddRange(progressLines);
                newList.Add("}");

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioAddTech(string techID, List<string> techNode, List<string> parts, string groupName)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "Tech.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                if (!oldList.Contains("Tech : " + techID))
                {
                    oldList.Add("Tech : " + techID);

                    oldList.Add("TechNode");
                    oldList.Add("{");
                    oldList.AddRange(techNode);
                    oldList.Add("}");

                    oldList.Add("TechParts");
                    oldList.Add("{");
                    for (int i = 0; i < parts.Count; i++)
                    {
                        oldList.Add("Part : " + parts[i]);
                    }
                    oldList.Add("}");

                    SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
                }
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add("Tech : " + techID);

                newList.Add("TechNode");
                newList.Add("{");
                newList.AddRange(techNode);
                newList.Add("}");

                newList.Add("TechParts");
                newList.Add("{");
                for (int i = 0; i < parts.Count; i++)
                {
                    newList.Add("Part : " + parts[i]);
                }
                newList.Add("}");


                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioAddTech(string techID, List<string> techNode, List<string> parts, ClientObject client)
        {
            string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, client.playerName);
            string filePath = Path.Combine(playerFolder, "Tech.txt");

            if (!Directory.Exists(playerFolder))
            {
                Directory.CreateDirectory(playerFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                if (!oldList.Contains("Tech : " + techID))
                {
                    oldList.Add("Tech : " + techID);

                    oldList.Add("TechNode");
                    oldList.Add("{");
                    oldList.AddRange(techNode);
                    oldList.Add("}");

                    oldList.Add("TechParts");
                    oldList.Add("{");
                    for (int i = 0; i < parts.Count; i++)
                    {
                        oldList.Add("Part : " + parts[i]);
                    }
                    oldList.Add("}");

                    SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
                }
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add("Tech : " + techID);

                newList.Add("TechNode");
                newList.Add("{");
                newList.AddRange(techNode);
                newList.Add("}");

                newList.Add("TechParts");
                newList.Add("{");
                for (int i = 0; i < parts.Count; i++)
                {
                    newList.Add("Part : " + parts[i]);
                }
                newList.Add("}");

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioScienceRecieved(string sciID, float dataValue, List<string> sciNode, string groupName)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "ScienceRecieved.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                if (!oldList.Contains("Sci : " + sciID))
                {
                    oldList.Add("Sci : " + sciID);

                    oldList.Add("Value : " + Convert.ToString(dataValue));

                    oldList.Add("SciNode");
                    oldList.Add("{");
                    oldList.AddRange(sciNode);
                    oldList.Add("}");

                    SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
                }
                else
                {
                    int index = oldList.FindIndex(i => i == "Sci : " + sciID);

                    oldList[index + 1] = "Value : " + Convert.ToString(dataValue);

                    int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(oldList, index + 3);
                    KeyValuePair<int, int> range = new KeyValuePair<int, int>(index, (matchBracketIdx - index) + 1);

                    oldList.RemoveRange(range.Key + 4, range.Value - 5);

                    oldList.InsertRange(range.Key + 4, sciNode);

                    SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
                }
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add("Sci : " + sciID);

                newList.Add("Value : " + Convert.ToString(dataValue));

                newList.Add("SciNode");
                newList.Add("{");
                newList.AddRange(sciNode);
                newList.Add("}");


                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioScienceRecieved(string sciID, float dataValue, List<string> sciNode, ClientObject client)
        {
            string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, client.playerName);
            string filePath = Path.Combine(playerFolder, "ScienceRecieved.txt");

            if (!Directory.Exists(playerFolder))
            {
                Directory.CreateDirectory(playerFolder);
            }

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                if (!oldList.Contains("Sci : " + sciID))
                {
                    oldList.Add("Sci : " + sciID);

                    oldList.Add("Value : " + Convert.ToString(dataValue));

                    oldList.Add("SciNode");
                    oldList.Add("{");
                    oldList.AddRange(sciNode);
                    oldList.Add("}");

                    SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
                }
                else
                {
                    int index = oldList.FindIndex(i => i == "Sci : " + sciID);

                    oldList[index + 1] = "Value : " + Convert.ToString(dataValue);

                    int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(oldList, index + 3);
                    KeyValuePair<int, int> range = new KeyValuePair<int, int>(index, (matchBracketIdx - index) + 1);

                    oldList.RemoveRange(range.Key + 4, range.Value - 5);

                    oldList.InsertRange(range.Key + 4, sciNode);

                    SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
                }
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add("Sci : " + sciID);

                newList.Add("Value : " + Convert.ToString(dataValue));

                newList.Add("SciNode");
                newList.Add("{");
                newList.AddRange(sciNode);
                newList.Add("}");

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }
    }
}
