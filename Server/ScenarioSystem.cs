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
using System.Globalization;
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
        private Dictionary<ScenarioDataType, Dictionary<string, Dictionary<ClientObject, long>>> dataSendQueue = new Dictionary<ScenarioDataType, Dictionary<string, Dictionary<ClientObject, long>>>();//<Data type, <Group name, <Player sending data, Last data send time>>>
        private object scenarioQueueLock = new object();
        private Dictionary<string, object> scenarioHandlerLocks = new Dictionary<string, object>();
        private Dictionary<string, Dictionary<string, long>> playersInFlight = new Dictionary<string, Dictionary<string, long>>();//<Group name (empty string if not in a group), <Player name, Time at which the player entered flight>>
        private List<string> flightChangeLogs = new List<string>();//<Group name>
        private List<string> playerDataChangeLogs = new List<string>();//<Player name>

        public static CultureInfo english = new CultureInfo("en-US");

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

            if (!Directory.Exists(playerFolder))
            {
                Directory.CreateDirectory(playerFolder);
            }

            string filePath = Path.Combine(playerFolder, "ResourceScenario.txt");

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

        public void CheckScenarioQueue()
        {
            lock (scenarioQueueLock)
            {
                Dictionary<ScenarioDataType, Dictionary<string, List<ClientObject>>> clientsToRemove = new Dictionary<ScenarioDataType, Dictionary<string, List<ClientObject>>>();

                foreach (ScenarioDataType type in dataSendQueue.Keys)
                {
                    foreach (string group in dataSendQueue[type].Keys)
                    {
                        foreach (ClientObject player in dataSendQueue[type][group].Keys)
                        {
                            if ((Server.serverClock.ElapsedMilliseconds - dataSendQueue[type][group][player]) > 1000)
                            {
                                ScenarioSendData(group, type, player);
                                
                                if (!clientsToRemove.ContainsKey(type))
                                {
                                    clientsToRemove.Add(type, new Dictionary<string, List<ClientObject>>() { { group, new List<ClientObject>() } });
                                }
                                else
                                {
                                    if (!clientsToRemove[type].ContainsKey(group))
                                    {
                                        clientsToRemove[type].Add(group, new List<ClientObject>());
                                    }
                                }

                                clientsToRemove[type][group].Add(player);
                            }
                        }
                    }
                }

                foreach (ScenarioDataType type in clientsToRemove.Keys)
                {
                    foreach (string group in clientsToRemove[type].Keys)
                    {
                        foreach (ClientObject player in clientsToRemove[type][group])
                        {
                            dataSendQueue[type][group].Remove(player);
                        }

                        if (clientsToRemove[type][group].Count == 0)
                        {
                            clientsToRemove[type].Remove(group);
                        }
                    }

                    if (clientsToRemove[type].Count == 0)
                    {
                        clientsToRemove.Remove(type);
                    }
                }
            }
        }

        public void QueueData(ScenarioDataType type, string groupName, ClientObject callingClient)
        {
            lock (scenarioQueueLock)
            {
                if (!dataSendQueue.ContainsKey(type))
                {
                    dataSendQueue.Add(type, new Dictionary<string, Dictionary<ClientObject, long>>() { { groupName, new Dictionary<ClientObject, long>() } });
                }
                else
                {
                    if (!dataSendQueue[type].ContainsKey(groupName))
                    {
                        dataSendQueue[type].Add(groupName, new Dictionary<ClientObject, long>());
                    }
                }

                dataSendQueue[type][groupName][callingClient] = Server.serverClock.ElapsedMilliseconds;
            }
        }

        public void EnterFlight(ClientObject callingClient, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                bool isInGroup = mr.Read<bool>();
                string groupName = string.Empty;

                if (isInGroup)
                {
                    groupName = mr.Read<string>();

                    if (!playersInFlight.ContainsKey(groupName))
                    {
                        playersInFlight.Add(groupName, new Dictionary<string, long>() { { callingClient.playerName, Server.serverClock.ElapsedMilliseconds } });

                        if (!flightChangeLogs.Contains(groupName))
                        {
                            flightChangeLogs.Add(groupName);

                            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                            string scenarioFolder = Path.Combine(groupFolder, "Scenario");

                            if (Directory.Exists(scenarioFolder))
                            {
                                string changeLogFolder = Path.Combine(groupFolder, "DataLogs");

                                if (!Directory.Exists(changeLogFolder))
                                {
                                    Directory.CreateDirectory(changeLogFolder);
                                }
                                else
                                {
                                    SyncrioUtil.FileHandler.DeleteDirectory(changeLogFolder);

                                    Directory.CreateDirectory(changeLogFolder);
                                }

                                foreach (string file in Directory.GetFiles(scenarioFolder))
                                {
                                    File.Copy(file, Path.Combine(changeLogFolder, Path.GetFileName(file)));
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!playersInFlight[groupName].ContainsKey(callingClient.playerName))
                        {
                            playersInFlight[groupName].Add(callingClient.playerName, Server.serverClock.ElapsedMilliseconds);

                            if (!flightChangeLogs.Contains(groupName))
                            {
                                flightChangeLogs.Add(groupName);

                                string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                                string scenarioFolder = Path.Combine(groupFolder, "Scenario");

                                if (Directory.Exists(scenarioFolder))
                                {
                                    string changeLogFolder = Path.Combine(groupFolder, "DataLogs");

                                    if (!Directory.Exists(changeLogFolder))
                                    {
                                        Directory.CreateDirectory(changeLogFolder);
                                    }
                                    else
                                    {
                                        SyncrioUtil.FileHandler.DeleteDirectory(changeLogFolder);

                                        Directory.CreateDirectory(changeLogFolder);
                                    }

                                    foreach (string file in Directory.GetFiles(scenarioFolder))
                                    {
                                        File.Copy(file, Path.Combine(changeLogFolder, Path.GetFileName(file)));
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (!playersInFlight.ContainsKey(string.Empty))
                    {
                        playersInFlight.Add(string.Empty, new Dictionary<string, long>() { { callingClient.playerName, Server.serverClock.ElapsedMilliseconds } });

                        if (!playerDataChangeLogs.Contains(callingClient.playerName))
                        {
                            playerDataChangeLogs.Add(callingClient.playerName);
                        }
                    }
                    else
                    {
                        if (!playersInFlight[string.Empty].ContainsKey(callingClient.playerName))
                        {
                            playersInFlight[string.Empty].Add(callingClient.playerName, Server.serverClock.ElapsedMilliseconds);

                            if (!playerDataChangeLogs.Contains(callingClient.playerName))
                            {
                                playerDataChangeLogs.Add(callingClient.playerName);
                            }
                        }
                    }
                }
            }
        }

        public void ExitFlight(ClientObject callingClient, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                bool isInGroup = mr.Read<bool>();
                string groupName = string.Empty;

                if (isInGroup)
                {
                    groupName = mr.Read<string>();

                    if (playersInFlight.ContainsKey(groupName))
                    {
                        if (playersInFlight[groupName].ContainsKey(callingClient.playerName))
                        {
                            playersInFlight[groupName].Remove(callingClient.playerName);

                            if (playersInFlight[groupName].Count == 0)
                            {
                                if (flightChangeLogs.Contains(groupName))
                                {
                                    flightChangeLogs.Remove(groupName);

                                    string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                                    string changeLogFolder = Path.Combine(groupFolder, "DataLogs");

                                    if (Directory.Exists(changeLogFolder))
                                    {
                                        SyncrioUtil.FileHandler.DeleteDirectory(changeLogFolder);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (playersInFlight.ContainsKey(string.Empty))
                    {
                        if (playersInFlight[string.Empty].ContainsKey(callingClient.playerName))
                        {
                            playersInFlight[string.Empty].Remove(callingClient.playerName);
                            
                            if (playerDataChangeLogs.Contains(callingClient.playerName))
                            {
                                playerDataChangeLogs.Remove(callingClient.playerName);

                                string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, callingClient.playerName);
                                string filePath = Path.Combine(playerFolder, "FlightLog.txt");

                                if (File.Exists(filePath))
                                {
                                    byte[] flightData = SyncrioUtil.FileHandler.ReadFromFile(filePath);

                                    SyncFlightLog(callingClient, flightData);

                                    SyncrioUtil.FileHandler.DeleteFile(filePath);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void RevertFlight(ClientObject callingClient, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                bool isInGroup = mr.Read<bool>();
                string groupName = string.Empty;

                if (isInGroup)
                {
                    groupName = mr.Read<string>();

                    if (playersInFlight.ContainsKey(groupName))
                    {
                        if (playersInFlight[groupName].ContainsKey(callingClient.playerName))
                        {
                            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                            string changeLogFolder = Path.Combine(groupFolder, "DataLogs");
                            string filePath = Path.Combine(changeLogFolder, "FlightLog.txt");

                            if (File.Exists(filePath))
                            {
                                List<string> flightData = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                                int looped = flightData.Count - 1;
                                while (looped >= 0)
                                {
                                    if (flightData[looped] == string.Format(english, "CHANGE_NODE : {0}", callingClient.playerName))
                                    {
                                        if (Convert.ToInt64(flightData[looped + 3]) > playersInFlight[groupName][callingClient.playerName])
                                        {
                                            int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(flightData, looped + 1);
                                            KeyValuePair<int, int> range = new KeyValuePair<int, int>(looped, (matchBracketIdx - looped) + 1);

                                            flightData.RemoveRange(range.Key, range.Value);
                                        }
                                    }

                                    looped--;
                                }

                                RebuildScenarioData(groupName, flightData);
                            }
                        }
                    }
                }
                else
                {
                    if (playersInFlight.ContainsKey(string.Empty))
                    {
                        if (playersInFlight[string.Empty].ContainsKey(callingClient.playerName))
                        {
                            string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, callingClient.playerName);
                            string filePath = Path.Combine(playerFolder, "FlightLog.txt");

                            if (File.Exists(filePath))
                            {
                                SyncrioUtil.FileHandler.DeleteFile(filePath);
                            }
                        }
                    }
                }
            }
        }

        public void SyncScenario(ClientObject callingClient, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                ScenarioDataType type = (ScenarioDataType)mr.Read<int>();

                SyncrioLog.Debug(callingClient.playerName + " sent data type: " + type.ToString());

                bool isInGroup = mr.Read<bool>();
                string groupName = string.Empty;
                byte[] subData;
                
                bool dontSync = false;

                if (isInGroup)
                {
                    groupName = mr.Read<string>();

                    subData = mr.Read<byte[]>();

                    if (flightChangeLogs.Contains(groupName))
                    {
                        string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                        string changeLogFolder = Path.Combine(groupFolder, "DataLogs");
                        string filePath = Path.Combine(changeLogFolder, "FlightLog.txt");

                        if (!Directory.Exists(changeLogFolder))
                        {
                            Directory.CreateDirectory(changeLogFolder);
                        }

                        List<string> data;

                        if (File.Exists(filePath))
                        {
                            data = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));
                        }
                        else
                        {
                            data = new List<string>();
                        }

                        data.Add(string.Format(english, "CHANGE_NODE : {0}", callingClient.playerName));
                        data.Add("{");

                        data.Add(Convert.ToString((int)type));

                        data.Add(Server.serverClock.ElapsedMilliseconds.ToString());

                        data.Add("DATA_NODE");
                        data.Add("{");

                        data.AddRange(SyncrioUtil.ByteArraySerializer.Deserialize(subData));

                        data.Add("}");
                        data.Add("}");

                        SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(data), filePath);
                    }
                }
                else
                {
                    subData = mr.Read<byte[]>();

                    if (playerDataChangeLogs.Contains(callingClient.playerName))
                    {
                        dontSync = true;

                        string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, callingClient.playerName);
                        string filePath = Path.Combine(playerFolder, "FlightLog.txt");

                        if (!Directory.Exists(playerFolder))
                        {
                            Directory.CreateDirectory(playerFolder);
                        }

                        List<string> data;

                        if (File.Exists(filePath))
                        {
                            data = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));
                        }
                        else
                        {
                            data = new List<string>();
                        }

                        data.Add("CHANGE_NODE");
                        data.Add("{");
                        
                        data.Add(Convert.ToString((int)type));

                        data.Add("DATA_NODE");
                        data.Add("{");

                        data.AddRange(SyncrioUtil.ByteArraySerializer.Deserialize(subData));

                        data.Add("}");
                        data.Add("}");

                        SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(data), filePath);
                    }
                }

                try
                {
                    if (isInGroup)
                    {
                        if (!scenarioHandlerLocks.ContainsKey(groupName))
                        {
                            scenarioHandlerLocks.Add(groupName, new object());
                        }

                        lock (scenarioHandlerLocks[groupName])
                        {
                            HandleScenarioData(callingClient, subData, type, isInGroup, groupName);
                        }
                    }
                    else
                    {
                        if (!dontSync)
                        {
                            HandleScenarioData(callingClient, subData, type, false, string.Empty);
                        }
                    }
                }
                catch (Exception e)
                {
                    SyncrioLog.Debug("Error syncing data, type: " + type.ToString() + ", error: " + e);
                }
            }
        }

        private void SyncFlightLog(ClientObject callingClient, byte[] messageData)
        {
            List<string> flightLog = SyncrioUtil.ByteArraySerializer.Deserialize(messageData);

            int looped = 0;
            while (looped < flightLog.Count)
            {
                if (flightLog[looped] == "CHANGE_NODE")
                {
                    int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(flightLog, looped + 1);
                    KeyValuePair<int, int> range = new KeyValuePair<int, int>(looped, (matchBracketIdx - looped) + 1);

                    ScenarioDataType type = (ScenarioDataType)Convert.ToInt32(flightLog[looped + 2]);

                    if (flightLog[looped + 3] == "DATA_NODE")
                    {
                        int matchBracketIdx2 = SyncrioUtil.DataCleaner.FindMatchingBracket(flightLog, looped + 4);
                        KeyValuePair<int, int> range2 = new KeyValuePair<int, int>(looped + 3, (matchBracketIdx2 - (looped + 3)) + 1);

                        byte[] data = SyncrioUtil.ByteArraySerializer.Serialize(flightLog.GetRange(range2.Key, range2.Value));

                        try
                        {
                            HandleScenarioData(callingClient, data, type, false, string.Empty);
                        }
                        catch (Exception e)
                        {
                            SyncrioLog.Debug("Error applying synced data, type: " + type.ToString() + ", error: " + e);
                        }
                    }

                    flightLog.RemoveRange(range.Key, range.Value);
                }
                else
                {
                    looped++;
                }
            }
        }

        private void RebuildScenarioData(string groupName, List<string> flightLog)
        {
            int looped = 0;
            while (looped < flightLog.Count)
            {
                if (flightLog[looped].StartsWith("CHANGE_NODE : "))
                {
                    string[] split = flightLog[looped].Split(':');
                    string start = split[0];
                    string playerName = split[1];

                    int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(flightLog, looped + 1);
                    KeyValuePair<int, int> range = new KeyValuePair<int, int>(looped, (matchBracketIdx - looped) + 1);

                    ScenarioDataType type = (ScenarioDataType)Convert.ToInt32(flightLog[looped + 2]);

                    if (flightLog[looped + 4] == "DATA_NODE")
                    {
                        int matchBracketIdx2 = SyncrioUtil.DataCleaner.FindMatchingBracket(flightLog, looped + 5);
                        KeyValuePair<int, int> range2 = new KeyValuePair<int, int>(looped + 4, (matchBracketIdx2 - (looped + 4)) + 1);

                        byte[] data = SyncrioUtil.ByteArraySerializer.Serialize(flightLog.GetRange(range2.Key, range2.Value));

                        try
                        {
                            if (!scenarioHandlerLocks.ContainsKey(groupName))
                            {
                                scenarioHandlerLocks.Add(groupName, new object());
                            }

                            lock (scenarioHandlerLocks[groupName])
                            {
                                string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                                string changeLogFolder = Path.Combine(groupFolder, "DataLogs");
                                string scenarioFolder = Path.Combine(groupFolder, "Scenario");

                                if (Directory.Exists(changeLogFolder))
                                {
                                    if (!Directory.Exists(scenarioFolder))
                                    {
                                        Directory.CreateDirectory(scenarioFolder);
                                    }
                                    else
                                    {
                                        SyncrioUtil.FileHandler.DeleteDirectory(scenarioFolder);

                                        Directory.CreateDirectory(scenarioFolder);
                                    }

                                    foreach (string file in Directory.GetFiles(changeLogFolder))
                                    {
                                        if (Path.GetFileName(file) != "FlightLog.txt")
                                        {
                                            File.Copy(file, Path.Combine(scenarioFolder, Path.GetFileName(file)));
                                        }
                                    }
                                }

                                HandleSpecialScenarioData(data, type, groupName);
                            }
                        }
                        catch (Exception e)
                        {
                            SyncrioLog.Debug("Error applying rebuilt sync data, type: " + type.ToString() + ", error: " + e);
                        }
                    }

                    flightLog.RemoveRange(range.Key, range.Value);
                }
                else
                {
                    looped++;
                }
            }

            Dictionary<string, GroupObject> groups = GroupSystem.fetch.GetCopy();

            foreach (ClientObject player in ClientHandler.GetClients())
            {
                if (groups[groupName].members.Contains(player.playerName))
                {
                    ScenarioSendAllData(groupName, player);
                }
            }
        }

        private void HandleScenarioData(ClientObject callingClient, byte[] data, ScenarioDataType type, bool isInGroup, string groupName)
        {
            using (MessageReader mr = new MessageReader(data))
            {
                switch (type)
                {
                    case ScenarioDataType.CONTRACT_UPDATED:
                        {
                            ContractUpdateType contractType = (ContractUpdateType)mr.Read<int>();

                            byte[] cnData = mr.Read<byte[]>();
                            List<string> cnLines = SyncrioUtil.ByteArraySerializer.Deserialize(cnData);

                            cnLines = SyncrioUtil.DataCleaner.BasicClean(cnLines);

                            if (isInGroup)
                            {
                                if (GroupSystem.fetch.GroupExists(groupName))
                                {
                                    ScenarioUpdateContract(cnLines, groupName, contractType);

                                    int number = mr.Read<int>();

                                    if (number != 0)
                                    {
                                        List<string> weightsList = new List<string>();

                                        for (int i = 0; i < number; i++)
                                        {
                                            string weight = mr.Read<string>();
                                            int amount = mr.Read<int>();

                                            weightsList.Add(string.Format(english, "{0}{1}{2}", weight, " : ", amount.ToString()));
                                        }

                                        ScenarioSetWeights(weightsList, groupName);
                                    }

                                    QueueData(type, groupName, callingClient);
                                }
                            }
                            else
                            {
                                ScenarioUpdateContract(cnLines, callingClient);

                                int number = mr.Read<int>();

                                if (number != 0)
                                {
                                    List<string> weightsList = new List<string>();

                                    for (int i = 0; i < number; i++)
                                    {
                                        string weight = mr.Read<string>();
                                        int amount = mr.Read<int>();

                                        weightsList.Add(string.Format(english, "{0}{1}{2}", weight, " : ", amount.ToString()));
                                    }

                                    ScenarioSetWeights(weightsList, callingClient);
                                }
                            }
                        }
                        break;
                    case ScenarioDataType.CONTRACT_OFFERED:
                        {
                            byte[] cnData = mr.Read<byte[]>();
                            List<string> cnLines = SyncrioUtil.ByteArraySerializer.Deserialize(cnData);

                            cnLines = SyncrioUtil.DataCleaner.BasicClean(cnLines);

                            if (isInGroup)
                            {
                                if (GroupSystem.fetch.GroupExists(groupName))
                                {
                                    ScenarioAddContract(cnLines, groupName);

                                    QueueData(type, groupName, callingClient);
                                }
                            }
                            else
                            {
                                ScenarioAddContract(cnLines, callingClient);
                            }
                        }
                        break;
                    case ScenarioDataType.REMOVE_CONTRACT:
                        {
                            string cnGUID = mr.Read<string>();

                            if (isInGroup)
                            {
                                if (GroupSystem.fetch.GroupExists(groupName))
                                {
                                    ScenarioRemoveContract(cnGUID, groupName);
                                }
                            }
                            else
                            {
                                ScenarioRemoveContract(cnGUID, callingClient);
                            }
                        }
                        break;
                    case ScenarioDataType.CUSTOM_WAYPOINT_LOAD:
                        {
                            string wpName = mr.Read<string>();
                            byte[] wpData = mr.Read<byte[]>();
                            List<string> wpLines = SyncrioUtil.ByteArraySerializer.Deserialize(wpData);

                            wpLines = SyncrioUtil.DataCleaner.BasicClean(wpLines);

                            if (isInGroup)
                            {
                                if (GroupSystem.fetch.GroupExists(groupName))
                                {
                                    ScenarioSaveLoadedWaypoint(wpName, wpLines, groupName);

                                    QueueData(type, groupName, callingClient);
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
                            string wpName = mr.Read<string>();
                            byte[] wpData = mr.Read<byte[]>();
                            List<string> wpLines = SyncrioUtil.ByteArraySerializer.Deserialize(wpData);

                            wpLines = SyncrioUtil.DataCleaner.BasicClean(wpLines);

                            if (isInGroup)
                            {
                                if (GroupSystem.fetch.GroupExists(groupName))
                                {
                                    ScenarioSaveWaypoint(wpName, wpLines, groupName);

                                    QueueData(type, groupName, callingClient);
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
                            KSPTransactionReasons reason = (KSPTransactionReasons)mr.Read<int>();

                            double value = mr.Read<double>();

                            if (isInGroup)
                            {
                                if (GroupSystem.fetch.GroupExists(groupName))
                                {
                                    ScenarioChangeCurrency(value, groupName, reason);

                                    QueueData(type, groupName, callingClient);
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
                            KSPTransactionReasons reason = (KSPTransactionReasons)mr.Read<int>();

                            float value = mr.Read<float>();

                            if (isInGroup)
                            {
                                if (GroupSystem.fetch.GroupExists(groupName))
                                {
                                    ScenarioChangeCurrency(1, value, groupName, reason);

                                    QueueData(type, groupName, callingClient);
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
                            KSPTransactionReasons reason = (KSPTransactionReasons)mr.Read<int>();

                            float value = mr.Read<float>();

                            if (isInGroup)
                            {
                                if (GroupSystem.fetch.GroupExists(groupName))
                                {
                                    ScenarioChangeCurrency(2, value, groupName, reason);

                                    QueueData(type, groupName, callingClient);
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
                            string facilityID = mr.Read<string>();
                            int level = mr.Read<int>();

                            if (isInGroup)
                            {
                                if (GroupSystem.fetch.GroupExists(groupName))
                                {
                                    ScenarioBuildingUpgrade(facilityID, level, groupName);

                                    QueueData(type, groupName, callingClient);
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
                            string buildingID = mr.Read<string>();

                            if (isInGroup)
                            {
                                if (GroupSystem.fetch.GroupExists(groupName))
                                {
                                    ScenarioBuildingBreak(buildingID, groupName);

                                    QueueData(type, groupName, callingClient);
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
                            string buildingID = mr.Read<string>();

                            if (isInGroup)
                            {
                                if (GroupSystem.fetch.GroupExists(groupName))
                                {
                                    ScenarioBuildingFix(buildingID, groupName);

                                    QueueData(type, groupName, callingClient);
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
                            string partID = mr.Read<string>();
                            string techNeededID = mr.Read<string>();

                            if (isInGroup)
                            {
                                if (GroupSystem.fetch.GroupExists(groupName))
                                {
                                    ScenarioAddPart(partID, techNeededID, groupName);

                                    QueueData(type, groupName, callingClient);
                                }
                            }
                            else
                            {
                                ScenarioAddPart(partID, techNeededID, callingClient);
                            }
                        }
                        break;
                    case ScenarioDataType.REMOVE_PART:
                        {
                            string partID = mr.Read<string>();
                            string techNeededID = mr.Read<string>();

                            if (isInGroup)
                            {
                                if (GroupSystem.fetch.GroupExists(groupName))
                                {
                                    ScenarioRemovePart(partID, techNeededID, groupName);

                                    //Send data back as a part purchased
                                    QueueData(ScenarioDataType.PART_PURCHASED, groupName, callingClient);
                                }
                            }
                            else
                            {
                                ScenarioRemovePart(partID, techNeededID, callingClient);
                            }
                        }
                        break;
                    case ScenarioDataType.PART_UPGRADE_PURCHASED:
                        {
                            string upgradeID = mr.Read<string>();
                            string techNeededID = mr.Read<string>();

                            if (isInGroup)
                            {
                                if (GroupSystem.fetch.GroupExists(groupName))
                                {
                                    ScenarioAddUpgrade(upgradeID, techNeededID, groupName);

                                    QueueData(type, groupName, callingClient);
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
                            string progressID = mr.Read<string>();
                            byte[] pnData = mr.Read<byte[]>();
                            List<string> pnLines = SyncrioUtil.ByteArraySerializer.Deserialize(pnData);

                            pnLines = SyncrioUtil.DataCleaner.BasicClean(pnLines);

                            if (isInGroup)
                            {
                                if (GroupSystem.fetch.GroupExists(groupName))
                                {
                                    ScenarioAddProgress(progressID, pnLines, groupName);

                                    QueueData(type, groupName, callingClient);
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
                            string techID = mr.Read<string>();
                            List<string> techNode = SyncrioUtil.ByteArraySerializer.Deserialize(mr.Read<byte[]>());

                            int numberOfParts = mr.Read<int>();

                            List<string> parts = new List<string>();

                            for (int i = 0; i < numberOfParts; i++)
                            {
                                parts.Add(mr.Read<string>());
                            }

                            if (isInGroup)
                            {
                                if (GroupSystem.fetch.GroupExists(groupName))
                                {
                                    ScenarioAddTech(techID, techNode, parts, groupName);

                                    QueueData(type, groupName, callingClient);
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
                            string sciID = mr.Read<string>();
                            float dataValue = mr.Read<float>();
                            List<string> sciNode = SyncrioUtil.ByteArraySerializer.Deserialize(mr.Read<byte[]>());

                            if (isInGroup)
                            {
                                if (GroupSystem.fetch.GroupExists(groupName))
                                {
                                    ScenarioScienceRecieved(sciID, dataValue, sciNode, groupName);

                                    QueueData(type, groupName, callingClient);
                                }
                            }
                            else
                            {
                                ScenarioScienceRecieved(sciID, dataValue, sciNode, callingClient);
                            }
                        }
                        break;
                    case ScenarioDataType.EXPERIMENT_DEPLOYED:
                        {
                            string experimentName = mr.Read<string>();

                            List<string> experimentNode = SyncrioUtil.ByteArraySerializer.Deserialize(mr.Read<byte[]>());

                            if (isInGroup)
                            {
                                if (GroupSystem.fetch.GroupExists(groupName))
                                {
                                    ScenarioExperimentDeployed(experimentName, experimentNode, groupName);

                                    QueueData(type, groupName, callingClient);
                                }
                            } else
                            {
                                ScenarioExperimentDeployed(experimentName, experimentNode, callingClient);
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

                                    SyncrioUtil.FileHandler.WriteToFile(mr.Read<byte[]>(), filePath);

                                    QueueData(type, groupName, callingClient);
                                }
                            }
                            else
                            {
                                string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, callingClient.playerName);
                                string filePath = Path.Combine(playerFolder, "ResourceScenario.txt");

                                SyncrioUtil.FileHandler.WriteToFile(mr.Read<byte[]>(), filePath);
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

                                    SyncrioUtil.FileHandler.WriteToFile(mr.Read<byte[]>(), filePath);

                                    QueueData(type, groupName, callingClient);
                                }
                            }
                            else
                            {
                                string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, callingClient.playerName);
                                string filePath = Path.Combine(playerFolder, "StrategySystem.txt");

                                SyncrioUtil.FileHandler.WriteToFile(mr.Read<byte[]>(), filePath);
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

        private void HandleSpecialScenarioData(byte[] data, ScenarioDataType type, string groupName)
        {
            if (GroupSystem.fetch.GroupExists(groupName))
            {
                using (MessageReader mr = new MessageReader(data))
                {
                    switch (type)
                    {
                        case ScenarioDataType.CONTRACT_UPDATED:
                            {
                                ContractUpdateType contractType = (ContractUpdateType)mr.Read<int>();

                                byte[] cnData = mr.Read<byte[]>();
                                List<string> cnLines = SyncrioUtil.ByteArraySerializer.Deserialize(cnData);

                                cnLines = SyncrioUtil.DataCleaner.BasicClean(cnLines);

                                ScenarioUpdateContract(cnLines, groupName, contractType, true);

                                int number = mr.Read<int>();

                                if (number != 0)
                                {
                                    List<string> weightsList = new List<string>();

                                    for (int i = 0; i < number; i++)
                                    {
                                        string weight = mr.Read<string>();
                                        int amount = mr.Read<int>();

                                        weightsList.Add(string.Format(english, "{0}{1}{2}", weight, " : ", amount.ToString()));
                                    }

                                    ScenarioSetWeights(weightsList, groupName, true);
                                }
                            }
                            break;
                        case ScenarioDataType.CONTRACT_OFFERED:
                            {
                                byte[] cnData = mr.Read<byte[]>();
                                List<string> cnLines = SyncrioUtil.ByteArraySerializer.Deserialize(cnData);

                                cnLines = SyncrioUtil.DataCleaner.BasicClean(cnLines);

                                ScenarioAddContract(cnLines, groupName, true);
                            }
                            break;
                        case ScenarioDataType.REMOVE_CONTRACT:
                            {
                                string cnGUID = mr.Read<string>();

                                ScenarioRemoveContract(cnGUID, groupName, true);
                            }
                            break;
                        case ScenarioDataType.CUSTOM_WAYPOINT_LOAD:
                            {
                                string wpName = mr.Read<string>();
                                byte[] wpData = mr.Read<byte[]>();
                                List<string> wpLines = SyncrioUtil.ByteArraySerializer.Deserialize(wpData);

                                wpLines = SyncrioUtil.DataCleaner.BasicClean(wpLines);

                                ScenarioSaveLoadedWaypoint(wpName, wpLines, groupName, true);
                            }
                            break;
                        case ScenarioDataType.CUSTOM_WAYPOINT_SAVE:
                            {
                                string wpName = mr.Read<string>();
                                byte[] wpData = mr.Read<byte[]>();
                                List<string> wpLines = SyncrioUtil.ByteArraySerializer.Deserialize(wpData);

                                wpLines = SyncrioUtil.DataCleaner.BasicClean(wpLines);

                                ScenarioSaveWaypoint(wpName, wpLines, groupName, true);
                            }
                            break;
                        case ScenarioDataType.FUNDS_CHANGED:
                            {
                                KSPTransactionReasons reason = (KSPTransactionReasons)mr.Read<int>();

                                double value = mr.Read<double>();

                                ScenarioChangeCurrency(value, groupName, reason, true);
                            }
                            break;
                        case ScenarioDataType.REPUTATION_CHANGED:
                            {
                                KSPTransactionReasons reason = (KSPTransactionReasons)mr.Read<int>();

                                float value = mr.Read<float>();

                                ScenarioChangeCurrency(1, value, groupName, reason, true);
                            }
                            break;
                        case ScenarioDataType.SCIENCE_CHANGED:
                            {
                                KSPTransactionReasons reason = (KSPTransactionReasons)mr.Read<int>();

                                float value = mr.Read<float>();

                                ScenarioChangeCurrency(2, value, groupName, reason, true);
                            }
                            break;
                        case ScenarioDataType.KSC_FACILITY_UPGRADED:
                            {
                                string facilityID = mr.Read<string>();
                                int level = mr.Read<int>();

                                ScenarioBuildingUpgrade(facilityID, level, groupName, true);
                            }
                            break;
                        case ScenarioDataType.KSC_STRUCTURE_COLLAPSED:
                            {
                                string buildingID = mr.Read<string>();

                                ScenarioBuildingBreak(buildingID, groupName, true);
                            }
                            break;
                        case ScenarioDataType.KSC_STRUCTURE_REPAIRED:
                            {
                                string buildingID = mr.Read<string>();

                                ScenarioBuildingFix(buildingID, groupName, true);
                            }
                            break;
                        case ScenarioDataType.PART_PURCHASED:
                            {
                                string partID = mr.Read<string>();
                                string techNeededID = mr.Read<string>();

                                ScenarioAddPart(partID, techNeededID, groupName, true);
                            }
                            break;
                        case ScenarioDataType.REMOVE_PART:
                            {
                                string partID = mr.Read<string>();
                                string techNeededID = mr.Read<string>();

                                ScenarioRemovePart(partID, techNeededID, groupName, true);
                            }
                            break;
                        case ScenarioDataType.PART_UPGRADE_PURCHASED:
                            {
                                string upgradeID = mr.Read<string>();
                                string techNeededID = mr.Read<string>();

                                ScenarioAddUpgrade(upgradeID, techNeededID, groupName, true);
                            }
                            break;
                        case ScenarioDataType.PROGRESS_UPDATED:
                            {
                                string progressID = mr.Read<string>();
                                byte[] pnData = mr.Read<byte[]>();
                                List<string> pnLines = SyncrioUtil.ByteArraySerializer.Deserialize(pnData);

                                pnLines = SyncrioUtil.DataCleaner.BasicClean(pnLines);

                                ScenarioAddProgress(progressID, pnLines, groupName, true);
                            }
                            break;
                        case ScenarioDataType.TECHNOLOGY_RESEARCHED:
                            {
                                string techID = mr.Read<string>();
                                List<string> techNode = SyncrioUtil.ByteArraySerializer.Deserialize(mr.Read<byte[]>());

                                int numberOfParts = mr.Read<int>();

                                List<string> parts = new List<string>();

                                for (int i = 0; i < numberOfParts; i++)
                                {
                                    parts.Add(mr.Read<string>());
                                }

                                ScenarioAddTech(techID, techNode, parts, groupName, true);
                            }
                            break;
                        case ScenarioDataType.SCIENCE_RECIEVED:
                            {
                                string sciID = mr.Read<string>();
                                float dataValue = mr.Read<float>();
                                List<string> sciNode = SyncrioUtil.ByteArraySerializer.Deserialize(mr.Read<byte[]>());

                                ScenarioScienceRecieved(sciID, dataValue, sciNode, groupName, true);
                            }
                            break;
                        case ScenarioDataType.EXPERIMENT_DEPLOYED:
                            {
                                string experimentID = mr.Read<string>();
                                List<string> expNode = SyncrioUtil.ByteArraySerializer.Deserialize(mr.Read<byte[]>());

                                ScenarioExperimentDeployed(experimentID, expNode, groupName);
                            }
                            break;
                        case ScenarioDataType.RESOURCE_SCENARIO:
                            {
                                string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                                string scenarioFolder = Path.Combine(groupFolder, "Scenario");
                                string filePath = Path.Combine(scenarioFolder, "ResourceScenario.txt");

                                SyncrioUtil.FileHandler.WriteToFile(mr.Read<byte[]>(), filePath);
                            }
                            break;
                        case ScenarioDataType.STRATEGY_SYSTEM:
                            {
                                string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                                string scenarioFolder = Path.Combine(groupFolder, "Scenario");
                                string filePath = Path.Combine(scenarioFolder, "StrategySystem.txt");

                                SyncrioUtil.FileHandler.WriteToFile(mr.Read<byte[]>(), filePath);
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
                case ScenarioDataType.EXPERIMENT_DEPLOYED:
                    {
                        string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                        string scenarioFolder = Path.Combine(groupFolder, "Scenario");
                        string filePath = Path.Combine(scenarioFolder, "Experiments.txt");

                        if (!Directory.Exists(scenarioFolder))
                        {
                            Directory.CreateDirectory(scenarioFolder);
                        }

                        if (File.Exists(filePath))
                        {
                            List<string> tempList = new List<string>();
                            tempList.Add("Experiments");
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

            string filePath15 = Path.Combine(scenarioFolder, "Experiments.txt");

            if (File.Exists(filePath15))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Experiments");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath15));
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

            if (!Directory.Exists(playerFolder))
            {
                Directory.CreateDirectory(playerFolder);
            }

            string filePath = Path.Combine(playerFolder, "Contracts.txt");

            if (File.Exists(filePath))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Contracts");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath));
            }

            string filePath2 = Path.Combine(playerFolder, "Waypoints.txt");

            if (File.Exists(filePath2))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Waypoints");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath2));
            }

            string filePath3 = Path.Combine(playerFolder, "Currency.txt");

            if (File.Exists(filePath3))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Currency");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath3));
            }

            string filePath4 = Path.Combine(playerFolder, "BuildingLevel.txt");

            if (File.Exists(filePath4))
            {
                List<string> tempList = new List<string>();
                tempList.Add("BuildingLevel");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath4));
            }

            string filePath5 = Path.Combine(playerFolder, "BuildingDead.txt");

            if (File.Exists(filePath5))
            {
                List<string> tempList = new List<string>();
                tempList.Add("BuildingDead");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath5));
            }

            string filePath6 = Path.Combine(playerFolder, "BuildingAlive.txt");

            if (File.Exists(filePath6))
            {
                List<string> tempList = new List<string>();
                tempList.Add("BuildingAlive");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath6));
            }

            string filePath7 = Path.Combine(playerFolder, "Progress.txt");

            if (File.Exists(filePath7))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Progress");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath7));
            }

            string filePath8 = Path.Combine(playerFolder, "Tech.txt");

            if (File.Exists(filePath8))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Tech");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath8));
            }

            string filePath9 = Path.Combine(playerFolder, "ScienceRecieved.txt");

            if (File.Exists(filePath9))
            {
                List<string> tempList = new List<string>();
                tempList.Add("ScienceRecieved");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath9));
            }

            string filePath10 = Path.Combine(playerFolder, "Parts.txt");

            if (File.Exists(filePath10))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Parts");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath10));
            }

            string filePath11 = Path.Combine(playerFolder, "Upgrades.txt");

            if (File.Exists(filePath11))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Upgrades");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath11));
            }

            string filePath12 = Path.Combine(playerFolder, "Weights.txt");

            if (File.Exists(filePath12))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Weights");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath12));
            }

            string filePath13 = Path.Combine(playerFolder, "ResourceScenario.txt");

            if (File.Exists(filePath13))
            {
                List<string> tempList = new List<string>();
                tempList.Add("ResourceScenario");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath13));
            }

            string filePath14 = Path.Combine(playerFolder, "StrategySystem.txt");

            if (File.Exists(filePath14))
            {
                List<string> tempList = new List<string>();
                tempList.Add("StrategySystem");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath14));
            }

            string filePath15 = Path.Combine(playerFolder, "Experiments.txt");

            if (File.Exists(filePath15))
            {
                List<string> tempList = new List<string>();
                tempList.Add("Experiments");
                data.Add(SyncrioUtil.ByteArraySerializer.Serialize(tempList));
                data.Add(SyncrioUtil.FileHandler.ReadFromFile(filePath15));
            }

            Messages.ScenarioData.SendScenarioModules(player, data);
        }

        private void ScenarioSetWeights(List<string> weights, string groupName, bool altFunction = false)
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

        private void ScenarioUpdateContract(List<string> cnLines, string groupName, ContractUpdateType contractType, bool altFunction = false)
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

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

                        if (oldList[oldIndex - looped] == "ContractNode" && oldList[(oldIndex - looped) + 1] == "{" && oldList[(oldIndex - looped) + 2] == "CONTRACT")
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
                    newList.Add("ContractNode");
                    newList.Add("{");
                    newList.AddRange(cnLines);
                    newList.Add("}");
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

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

                        if (oldList[oldIndex - looped] == "ContractNode" && oldList[(oldIndex - looped) + 1] == "{" && oldList[(oldIndex - looped) + 2] == "CONTRACT")
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
                    newList.Add("ContractNode");
                    newList.Add("{");
                    newList.AddRange(cnLines);
                    newList.Add("}");
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioAddContract(List<string> cnLines, string groupName, bool altFunction = false)
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

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
                    newList.Add("ContractNode");
                    newList.Add("{");
                    newList.AddRange(cnLines);
                    newList.Add("}");
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

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
                    newList.Add("ContractNode");
                    newList.Add("{");
                    newList.AddRange(cnLines);
                    newList.Add("}");
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioRemoveContract(string cnGUID, string groupName, bool altFunction = false)
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                string cnID = string.Format(english, "guid = {0}", cnGUID);

                if (oldList.Any(i => i == cnID))
                {
                    int oldIndex = oldList.FindIndex(i => i == cnID);
                    
                    int looped = 0;
                    while (oldList[oldIndex - looped] != "ContractNode" && looped <= 20)
                    {
                        looped++;
                    }

                    if (oldList[oldIndex - looped] == "ContractNode" && oldList[(oldIndex - looped) + 1] == "{" && oldList[(oldIndex - looped) + 2] == "CONTRACT")
                    {
                        int tempIndex = oldIndex - looped;
                        int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(oldList, tempIndex + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(tempIndex, (matchBracketIdx - tempIndex) + 1);
                        
                        oldList.RemoveRange(range.Key, range.Value);
                    }
                }
                else
                {
                    return;
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
        }

        private void ScenarioRemoveContract(string cnGUID, ClientObject client)
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                string cnID = string.Format(english, "guid = {0}", cnGUID);

                if (oldList.Any(i => i == cnID))
                {
                    int oldIndex = oldList.FindIndex(i => i == cnID);

                    int looped = 0;
                    while (oldList[oldIndex - looped] != "ContractNode" && looped <= 20)
                    {
                        looped++;
                    }

                    if (oldList[oldIndex - looped] == "ContractNode" && oldList[(oldIndex - looped) + 1] == "{" && oldList[(oldIndex - looped) + 2] == "CONTRACT")
                    {
                        int tempIndex = oldIndex - looped;
                        int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(oldList, tempIndex + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(tempIndex, (matchBracketIdx - tempIndex) + 1);

                        oldList.RemoveRange(range.Key, range.Value);
                    }
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
        }

        private void ScenarioSaveLoadedWaypoint(string wpID, List<string> wpLines, string groupName, bool altFunction = false)
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                if (!oldList.Any(i => i == string.Format(english, "Waypoint : {0}", wpID)))
                {
                    oldList.Add(string.Format(english, "Waypoint : {0}", wpID));
                    oldList.Add("{");
                    oldList.AddRange(wpLines);
                    oldList.Add("}");
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add(string.Format(english, "Waypoint : {0}", wpID));
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                if (!oldList.Any(i => i == string.Format(english, "Waypoint : {0}", wpID)))
                {
                    oldList.Add(string.Format(english, "Waypoint : {0}", wpID));
                    oldList.Add("{");
                    oldList.AddRange(wpLines);
                    oldList.Add("}");
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add(string.Format(english, "Waypoint : {0}", wpID));
                newList.Add("{");
                newList.AddRange(wpLines);
                newList.Add("}");

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioSaveWaypoint(string wpID, List<string> wpLines, string groupName, bool altFunction = false)
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                int cursor = oldList.FindIndex(i => i == string.Format(english, "Waypoint : {0}", wpID));

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

                newList.Add(string.Format(english, "Waypoint : {0}", wpID));
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                int cursor = oldList.FindIndex(i => i == string.Format(english, "Waypoint : {0}", wpID));

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

                newList.Add(string.Format(english, "Waypoint : {0}", wpID));
                newList.Add("{");
                newList.AddRange(wpLines);
                newList.Add("}");

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        /// <summary>
        /// This version is for funds only.
        /// </summary>
        private void ScenarioChangeCurrency(double value, string groupName, KSPTransactionReasons reason, bool altFunction = false)
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                oldList[0] = Convert.ToString(value, english);

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add(Convert.ToString(0, english));
                newList.Add(Convert.ToString(0, english));
                newList.Add(Convert.ToString(0, english));

                newList[0] = Convert.ToString(Convert.ToDouble(newList[0], english) + value, english);

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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                oldList[0] = Convert.ToString(value, english);

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add(Convert.ToString(0, english));
                newList.Add(Convert.ToString(0, english));
                newList.Add(Convert.ToString(0, english));

                newList[0] = Convert.ToString(Convert.ToDouble(newList[0], english) + value, english);

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        /// <summary>
        /// Set type to either 1 or 2. 1 == Reputation. 2 == Science.
        /// </summary>
        private void ScenarioChangeCurrency(int type, float value, string groupName, KSPTransactionReasons reason, bool altFunction = false)
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                oldList[type] = Convert.ToString(value, english);

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add(Convert.ToString(0, english));
                newList.Add(Convert.ToString(0, english));
                newList.Add(Convert.ToString(0, english));

                newList[type] = Convert.ToString(Convert.ToSingle(newList[type], english) + value, english);

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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                oldList[type] = Convert.ToString(value, english);

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add(Convert.ToString(0, english));
                newList.Add(Convert.ToString(0, english));
                newList.Add(Convert.ToString(0, english));

                newList[type] = Convert.ToString(Convert.ToSingle(newList[type], english) + value, english);

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }
        
        private void ScenarioBuildingUpgrade(string buliding, int level, string groupName, bool altFunction = false)
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                string startString = buliding + " = ";

                int cursor = oldList.FindIndex(i => i.StartsWith(startString));

                if (cursor != -1)
                {
                    if (Convert.ToInt32(oldList[cursor].Substring(startString.Length), english) < level)
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
                
                newList.Add(buliding + " = " + level.ToString());

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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                string startString = buliding + " = ";

                int cursor = oldList.FindIndex(i => i.StartsWith(startString));

                if (cursor != -1)
                {
                    if (Convert.ToInt32(oldList[cursor].Substring(startString.Length), english) < level)
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

                newList.Add(buliding + " = " + level.ToString());
                
                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }
        
        private void ScenarioBuildingBreak(string buliding, string groupName, bool altFunction = false)
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

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
                
                newList.Add(buliding);

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }


            string filePath2 = Path.Combine(scenarioFolder, "BuildingAlive.txt");

            if (File.Exists(filePath2))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath2));

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

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

                newList.Add(buliding);

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }


            string filePath2 = Path.Combine(playerFolder, "BuildingAlive.txt");

            if (File.Exists(filePath2))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath2));

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

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
                
                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath2);
            }
        }
        
        private void ScenarioBuildingFix(string buliding, string groupName, bool altFunction = false)
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

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

                newList.Add(buliding);

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }


            string filePath2 = Path.Combine(scenarioFolder, "BuildingDead.txt");

            if (File.Exists(filePath2))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath2));

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

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

                newList.Add(buliding);

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }


            string filePath2 = Path.Combine(playerFolder, "BuildingDead.txt");

            if (File.Exists(filePath2))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath2));

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

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
                
                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath2);
            }
        }

        private void ScenarioAddPart(string part, string techNeeded, string groupName, bool altFunction = false)
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                if (!oldList.Any(i => i == string.Format(english, "{0}{1}{2}",part, " : ", techNeeded)))
                {
                    oldList.Add(string.Format(english, "{0}{1}{2}", part, " : ", techNeeded));
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add(string.Format(english, "{0}{1}{2}", part, " : ", techNeeded));

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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                if (!oldList.Any(i => i == string.Format(english, "{0}{1}{2}", part, " : ", techNeeded)))
                {
                    oldList.Add(string.Format(english, "{0}{1}{2}", part, " : ", techNeeded));
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add(string.Format(english, "{0}{1}{2}", part, " : ", techNeeded));

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioRemovePart(string part, string techNeeded, string groupName, bool altFunction = false)
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                int index = oldList.FindIndex(i => i == string.Format(english, "{0}{1}{2}", part, " : ", techNeeded));

                if (index != -1)
                {
                    oldList.RemoveAt(index);
                }
                else
                {
                    return;
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
        }

        private void ScenarioRemovePart(string part, string techNeeded, ClientObject client)
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                int index = oldList.FindIndex(i => i == string.Format(english, "{0}{1}{2}", part, " : ", techNeeded));

                if (index != -1)
                {
                    oldList.RemoveAt(index);
                }
                else
                {
                    return;
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
        }

        private void ScenarioAddUpgrade(string upgrade, string techNeeded, string groupName, bool altFunction = false)
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                if (!oldList.Any(i => i == string.Format(english, "{0}{1}{2}", upgrade, " : ", techNeeded)))
                {
                    oldList.Add(string.Format(english, "{0}{1}{2}", upgrade, " : ", techNeeded));
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add(string.Format(english, "{0}{1}{2}", upgrade, " : ", techNeeded));

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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                if (!oldList.Any(i => i == string.Format(english, "{0}{1}{2}", upgrade, " : ", techNeeded)))
                {
                    oldList.Add(string.Format(english, "{0}{1}{2}", upgrade, " : ", techNeeded));
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add(string.Format(english, "{0}{1}{2}", upgrade, " : ", techNeeded));

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioAddProgress(string progressID, List<string> progressLines, string groupName, bool altFunction = false)
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                if (!oldList.Contains(string.Format(english, "ProgressNode : {0}", progressID)))
                {
                    oldList.Add(string.Format(english, "ProgressNode : {0}", progressID));
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

                newList.Add(string.Format(english, "ProgressNode : {0}", progressID));
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                if (!oldList.Contains(string.Format(english, "ProgressNode : {0}", progressID)))
                {
                    oldList.Add(string.Format(english, "ProgressNode : {0}", progressID));
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

                newList.Add(string.Format(english, "ProgressNode : {0}", progressID));
                newList.Add("{");
                newList.AddRange(progressLines);
                newList.Add("}");

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioAddTech(string techID, List<string> techNode, List<string> parts, string groupName, bool altFunction = false)
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                if (!oldList.Contains(string.Format(english, "Tech : {0}", techID)))
                {
                    oldList.Add(string.Format(english, "Tech : {0}", techID));

                    oldList.Add("TechNode");
                    oldList.Add("{");
                    oldList.AddRange(techNode);
                    oldList.Add("}");

                    oldList.Add("TechParts");
                    oldList.Add("{");
                    for (int i = 0; i < parts.Count; i++)
                    {
                        oldList.Add(string.Format(english, "Part : {0}", parts[i]));
                    }
                    oldList.Add("}");

                    SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
                }
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add(string.Format(english, "Tech : {0}", techID));

                newList.Add("TechNode");
                newList.Add("{");
                newList.AddRange(techNode);
                newList.Add("}");

                newList.Add("TechParts");
                newList.Add("{");
                for (int i = 0; i < parts.Count; i++)
                {
                    newList.Add(string.Format(english, "Part : {0}", parts[i]));
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                if (!oldList.Contains(string.Format(english, "Tech : {0}", techID)))
                {
                    oldList.Add(string.Format(english, "Tech : {0}", techID));

                    oldList.Add("TechNode");
                    oldList.Add("{");
                    oldList.AddRange(techNode);
                    oldList.Add("}");

                    oldList.Add("TechParts");
                    oldList.Add("{");
                    for (int i = 0; i < parts.Count; i++)
                    {
                        oldList.Add(string.Format(english, "Part : {0}", parts[i]));
                    }
                    oldList.Add("}");

                    SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
                }
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add(string.Format(english, "Tech : {0}", techID));

                newList.Add("TechNode");
                newList.Add("{");
                newList.AddRange(techNode);
                newList.Add("}");

                newList.Add("TechParts");
                newList.Add("{");
                for (int i = 0; i < parts.Count; i++)
                {
                    newList.Add(string.Format(english, "Part : {0}", parts[i]));
                }
                newList.Add("}");

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void HandleExperimentFile(string filePath, string experimentID, List<string> expNode)
        {

            if (File.Exists(filePath))
            {
                List<string> oldList = SyncrioUtil.ByteArraySerializer.Deserialize(SyncrioUtil.FileHandler.ReadFromFile(filePath));

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                if (!oldList.Contains(string.Format(english, "Experiment : {0}", experimentID)))
                {
                    oldList.Add(string.Format(english, "Experiment : {0}", experimentID));

                    oldList.Add("ExpNode");
                    oldList.Add("{");
                    oldList.AddRange(expNode);
                    oldList.Add("}");

                    SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
                }
                else
                {
                    int index = oldList.FindIndex(i => i == string.Format(english, "Experiment : {0}", experimentID));

                    int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(oldList, index + 2);
                    KeyValuePair<int, int> range = new KeyValuePair<int, int>(index, (matchBracketIdx - index) + 1);

                    oldList.RemoveRange(range.Key + 4, range.Value - 5);

                    oldList.InsertRange(range.Key + 4, expNode);

                    SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
                }
            }
            else
            {
                List<string> newList = new List<string>();

                newList.Add(string.Format(english, "Experiment : {0}", experimentID));

                newList.Add("ExpNode");
                newList.Add("{");
                newList.AddRange(expNode);
                newList.Add("}");

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }

        private void ScenarioExperimentDeployed(string experimentID, List<string> expNode, string groupName)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "Experiments.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            HandleExperimentFile(filePath, experimentID, expNode);
        }

        private void ScenarioExperimentDeployed(string experimentID, List<string> expNode, ClientObject callingClient)
        {
            string playerFolder = Path.Combine(ScenarioSystem.fetch.playerDirectory, callingClient.playerName);
            string filePath = Path.Combine(playerFolder, "Experiments.txt");

            if (!Directory.Exists(playerFolder))
            {
                Directory.CreateDirectory(playerFolder);
            }

            HandleExperimentFile(filePath, experimentID, expNode);
        }

        private void ScenarioScienceRecieved(string sciID, float dataValue, List<string> sciNode, string groupName, bool altFunction = false)
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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                if (!oldList.Contains(string.Format(english, "Sci : {0}", sciID)))
                {
                    oldList.Add(string.Format(english, "Sci : {0}", sciID));

                    oldList.Add(string.Format(english, "Value : {0}", Convert.ToString(dataValue, english)));

                    oldList.Add("SciNode");
                    oldList.Add("{");
                    oldList.AddRange(sciNode);
                    oldList.Add("}");

                    SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
                }
                else
                {
                    int index = oldList.FindIndex(i => i == string.Format(english, "Sci : {0}", sciID));

                    oldList[index + 1] = string.Format(english, "Value : {0}", Convert.ToString(dataValue, english));

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

                newList.Add(string.Format(english, "Sci : {0}", sciID));

                newList.Add(string.Format(english, "Value : {0}", Convert.ToString(dataValue, english)));

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

                if (oldList == null)
                {
                    oldList = new List<string>();
                }

                if (!oldList.Contains(string.Format(english, "Sci : {0}", sciID)))
                {
                    oldList.Add(string.Format(english, "Sci : {0}", sciID));

                    oldList.Add(string.Format(english, "Value : {0}", Convert.ToString(dataValue, english)));

                    oldList.Add("SciNode");
                    oldList.Add("{");
                    oldList.AddRange(sciNode);
                    oldList.Add("}");

                    SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(oldList), filePath);
                }
                else
                {
                    int index = oldList.FindIndex(i => i == string.Format(english, "Sci : {0}", sciID));

                    oldList[index + 1] = string.Format(english, "Value : {0}", Convert.ToString(dataValue, english));

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

                newList.Add(string.Format(english, "Sci : {0}", sciID));

                newList.Add(string.Format(english, "Value : {0}", Convert.ToString(dataValue, english)));

                newList.Add("SciNode");
                newList.Add("{");
                newList.AddRange(sciNode);
                newList.Add("}");

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(newList), filePath);
            }
        }
    }
}
