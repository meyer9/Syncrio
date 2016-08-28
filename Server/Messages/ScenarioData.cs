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
using System.IO;
using System.Linq;
using MessageStream2;
using SyncrioCommon;

namespace SyncrioServer.Messages
{
    public class ScenarioData
    {
        public static void SendScenarioModules(ClientObject client)
        {
            int numberOfScenarioModules = Directory.GetFiles(Path.Combine(Server.ScenarioDirectory, "Players", client.playerName)).Length;
            int currentScenarioModule = 0;
            string[] scenarioNames = new string[numberOfScenarioModules];
            byte[][] scenarioDataArray = new byte[numberOfScenarioModules][];
            foreach (string file in Directory.GetFiles(Path.Combine(Server.ScenarioDirectory, "Players", client.playerName)))
            {
                //Remove the .txt part for the name
                scenarioNames[currentScenarioModule] = Path.GetFileNameWithoutExtension(file);
                scenarioDataArray[currentScenarioModule] = File.ReadAllBytes(file);
                currentScenarioModule++;
            }
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.SCENARIO_DATA;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string[]>(scenarioNames);
                mw.Write<string[]>(new string[0]);
                mw.Write<string[]>(new string[0]);
                mw.Write<string[]>(new string[0]);
                foreach (byte[] scenarioData in scenarioDataArray)
                {
                    if (client.compressionEnabled)
                    {
                        mw.Write<byte[]>(Compression.CompressIfNeeded(scenarioData));
                    }
                    else
                    {
                        mw.Write<byte[]>(Compression.AddCompressionHeader(scenarioData, false));
                    }
                }
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToClient(client, newMessage, true);
        }

        public static void SendScenarioGroupModules(ClientObject client, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                string groupName = mr.Read<string>();
                int numberOfScenarioModules = Directory.GetFiles(Path.Combine(Server.ScenarioDirectory, "Groups", groupName, "Scenario")).Length;
                int currentScenarioModule = 0;
                string[] scenarioNames = new string[numberOfScenarioModules];
                byte[][] scenarioDataArray = new byte[numberOfScenarioModules][];
                foreach (string file in Directory.GetFiles(Path.Combine(Server.ScenarioDirectory, "Groups", groupName, "Scenario")))
                {
                    //Remove the .txt part for the name
                    scenarioNames[currentScenarioModule] = Path.GetFileNameWithoutExtension(file);
                    scenarioDataArray[currentScenarioModule] = File.ReadAllBytes(file);
                    currentScenarioModule++;
                }
                ServerMessage newMessage = new ServerMessage();
                newMessage.type = ServerMessageType.SCENARIO_DATA;
                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<string[]>(scenarioNames);
                    mw.Write<string[]>(ScenarioSystem.fetch.GetScenatioFundsVersionHistory(groupName));
                    mw.Write<string[]>(ScenarioSystem.fetch.GetScenatioRepVersionHistory(groupName));
                    mw.Write<string[]>(ScenarioSystem.fetch.GetScenatioSciVersionHistory(groupName));
                    foreach (byte[] scenarioData in scenarioDataArray)
                    {
                        if (client.compressionEnabled)
                        {
                            mw.Write<byte[]>(Compression.CompressIfNeeded(scenarioData));
                        }
                        else
                        {
                            mw.Write<byte[]>(Compression.AddCompressionHeader(scenarioData, false));
                        }
                    }
                    newMessage.data = mw.GetMessageBytes();
                }
                ClientHandler.SendToClient(client, newMessage, true);
            }
        }

        public static void SendScenarioGroupModulesStringGroupName(ClientObject client, string groupName)
        {
            int numberOfScenarioModules = Directory.GetFiles(Path.Combine(Server.ScenarioDirectory, "Groups", groupName, "Scenario")).Length;
            int currentScenarioModule = 0;
            string[] scenarioNames = new string[numberOfScenarioModules];
            byte[][] scenarioDataArray = new byte[numberOfScenarioModules][];
            foreach (string file in Directory.GetFiles(Path.Combine(Server.ScenarioDirectory, "Groups", groupName, "Scenario")))
            {
                //Remove the .txt part for the name
                scenarioNames[currentScenarioModule] = Path.GetFileNameWithoutExtension(file);
                scenarioDataArray[currentScenarioModule] = File.ReadAllBytes(file);
                currentScenarioModule++;
            }
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.SCENARIO_DATA;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string[]>(scenarioNames);
                mw.Write<string[]>(ScenarioSystem.fetch.GetScenatioFundsVersionHistory(groupName));
                mw.Write<string[]>(ScenarioSystem.fetch.GetScenatioRepVersionHistory(groupName));
                mw.Write<string[]>(ScenarioSystem.fetch.GetScenatioSciVersionHistory(groupName));
                foreach (byte[] scenarioData in scenarioDataArray)
                {
                    if (client.compressionEnabled)
                    {
                        mw.Write<byte[]>(Compression.CompressIfNeeded(scenarioData));
                    }
                    else
                    {
                        mw.Write<byte[]>(Compression.AddCompressionHeader(scenarioData, false));
                    }
                }
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToClient(client, newMessage, true);
        }

        public static void HandleScenarioModuleData(ClientObject client, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                string[] scenarioName = mr.Read<string[]>();
                SyncrioLog.Debug("Saving " + scenarioName.Length + " scenario modules from " + client.playerName);

                for (int i = 0; i < scenarioName.Length; i++)
                {
                    byte[] scenarioData = Compression.DecompressIfNeeded(mr.Read<byte[]>());
                    File.WriteAllBytes(Path.Combine(Server.ScenarioDirectory, "Players", client.playerName, scenarioName[i] + ".txt"), scenarioData);
                }
            }
        }

        public static void HandleGroupScenarioModuleData(ClientObject client, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                string[] scenarioName = mr.Read<string[]>();
                List<string> playerScenarioFundsHistory = new List<string>(mr.Read<string[]>());
                List<string> playerScenarioRepHistory = new List<string>(mr.Read<string[]>());
                List<string> playerScenarioSciHistory = new List<string>(mr.Read<string[]>());
                string groupName = mr.Read<string>();

                List<string> groupScenarioFundsHistory = new List<string>(ScenarioSystem.fetch.GetScenatioFundsVersionHistory(groupName));
                List<string> groupScenarioRepHistory = new List<string>(ScenarioSystem.fetch.GetScenatioRepVersionHistory(groupName));
                List<string> groupScenarioSciHistory = new List<string>(ScenarioSystem.fetch.GetScenatioSciVersionHistory(groupName));

                SyncrioLog.Debug("Saving " + scenarioName.Length + " scenario group modules from " + client.playerName + " to " + groupName);

                for (int i = 0; i < scenarioName.Length; i++)
                {
                    byte[] scenarioData = Compression.DecompressIfNeeded(mr.Read<byte[]>());
                    string filePath = Path.Combine(Server.ScenarioDirectory, "Groups", groupName, "Scenario", scenarioName[i] + ".txt");
                    if (File.Exists(filePath))
                    {
                        byte[] oldScenarioData = File.ReadAllBytes(filePath);
                        if (scenarioData != oldScenarioData)
                        {
                            double fundsValueDff = 0;
                            float repValueDff = 0;
                            float sciValueDff = 0;
                            if (scenarioName[i] != "Funding" && scenarioName[i] != "Reputation" && scenarioName[i] != "ResearchAndDevelopment")
                            {
                                List<string> scenarioDataStringList = ByteArraySerializer.fetch.Deserialize(scenarioData);
                                List<string> oldScenarioDataStringList = ByteArraySerializer.fetch.Deserialize(oldScenarioData);
                                List<string> mergedScenarioDataStringList = oldScenarioDataStringList.Union(scenarioDataStringList).ToList<string>();
                                List<string> noDuplicatesScenarioDataStringList = UnDuplicater.StringDuplicateRemover(mergedScenarioDataStringList.ToArray());
                                byte[] mergedScenarioDataSerializedStringList = ByteArraySerializer.fetch.Serialize(noDuplicatesScenarioDataStringList);
                                File.WriteAllBytes(filePath, mergedScenarioDataSerializedStringList);
                            }
                            else
                            {
                                if (scenarioName[i] == "Funding")
                                {
                                    if (playerScenarioFundsHistory != groupScenarioFundsHistory)
                                    {
                                        if (playerScenarioFundsHistory.Contains("funds") && groupScenarioFundsHistory.Contains("funds"))
                                        {
                                            List<string> mergedScenarioFundsHistory = new List<string>(groupScenarioFundsHistory);
                                            double finalValueDff = 0;

                                            for (int v = 1; v < playerScenarioFundsHistory.Count; v += 2)
                                            {
                                                double currentPlayerFundsValue = Convert.ToDouble(playerScenarioFundsHistory[v].ToString());

                                                double currentGroupFundsValue = Convert.ToDouble(groupScenarioFundsHistory[v].ToString());

                                                if (currentPlayerFundsValue != currentGroupFundsValue)
                                                {
                                                    double lastPlayerFundsValue = Convert.ToDouble(playerScenarioFundsHistory[v - 2].ToString());
                                                    double playerFundsValueDff = currentPlayerFundsValue -= lastPlayerFundsValue;

                                                    double lastGroupFundsValue = Convert.ToDouble(groupScenarioFundsHistory[v - 2].ToString());
                                                    double groupFundsValueDff = currentGroupFundsValue -= lastGroupFundsValue;

                                                    double valueDff = playerFundsValueDff + groupFundsValueDff;
                                                    finalValueDff += valueDff;
                                                    mergedScenarioFundsHistory.Add("funds");
                                                    mergedScenarioFundsHistory.Add(Convert.ToString(valueDff));
                                                }
                                            }
                                            fundsValueDff += finalValueDff;
                                            ScenarioSystem.fetch.SetScenatioFundsVersionHistory(groupName, mergedScenarioFundsHistory.ToArray());
                                        }
                                        else
                                        {
                                            ScenarioSystem.fetch.SetScenatioFundsVersionHistory(groupName, playerScenarioFundsHistory.ToArray());
                                        }
                                    }
                                }
                                if (scenarioName[i] == "Reputation")
                                {
                                    if (playerScenarioRepHistory != groupScenarioRepHistory)
                                    {
                                        if (playerScenarioRepHistory.Contains("rep") && groupScenarioRepHistory.Contains("rep"))
                                        {
                                            List<string> mergedScenarioRepHistory = new List<string>(groupScenarioRepHistory);
                                            float finalValueDff = 0;

                                            for (int v = 1; v < playerScenarioRepHistory.Count; v += 2)
                                            {
                                                float currentPlayerRepValue = Convert.ToSingle(playerScenarioRepHistory[v].ToString());

                                                float currentGroupRepValue = Convert.ToSingle(groupScenarioRepHistory[v].ToString());

                                                if (currentPlayerRepValue != currentGroupRepValue)
                                                {
                                                    float lastPlayerRepValue = Convert.ToSingle(playerScenarioRepHistory[v - 2].ToString());
                                                    float playerRepValueDff = currentPlayerRepValue -= lastPlayerRepValue;

                                                    float lastGroupRepValue = Convert.ToSingle(groupScenarioRepHistory[v - 2].ToString());
                                                    float groupRepValueDff = currentGroupRepValue -= lastGroupRepValue;

                                                    float valueDff = playerRepValueDff + groupRepValueDff;
                                                    finalValueDff += valueDff;
                                                    mergedScenarioRepHistory.Add("rep");
                                                    mergedScenarioRepHistory.Add(Convert.ToString(valueDff));
                                                }
                                            }
                                            repValueDff += finalValueDff;
                                            ScenarioSystem.fetch.SetScenatioRepVersionHistory(groupName, mergedScenarioRepHistory.ToArray());
                                        }
                                        else
                                        {
                                            ScenarioSystem.fetch.SetScenatioRepVersionHistory(groupName, playerScenarioRepHistory.ToArray());
                                        }
                                    }
                                }
                                if (scenarioName[i] == "ResearchAndDevelopment")
                                {
                                    if (playerScenarioSciHistory != groupScenarioSciHistory)
                                    {
                                        if (playerScenarioSciHistory.Contains("sci") && groupScenarioSciHistory.Contains("sci"))
                                        {
                                            List<string> mergedScenarioSciHistory = new List<string>(groupScenarioSciHistory);
                                            float finalValueDff = 0;

                                            for (int v = 1; v < playerScenarioSciHistory.Count; v += 2)
                                            {
                                                float currentPlayerSciValue = Convert.ToSingle(playerScenarioSciHistory[v].ToString());

                                                float currentGroupSciValue = Convert.ToSingle(groupScenarioSciHistory[v].ToString());

                                                if (currentPlayerSciValue != currentGroupSciValue)
                                                {
                                                    float lastPlayerSciValue = Convert.ToSingle(playerScenarioSciHistory[v - 2].ToString());
                                                    float playerSciValueDff = currentPlayerSciValue -= lastPlayerSciValue;

                                                    float lastGroupSciValue = Convert.ToSingle(groupScenarioSciHistory[v - 2].ToString());
                                                    float groupSciValueDff = currentGroupSciValue -= lastGroupSciValue;

                                                    float valueDff = playerSciValueDff + groupSciValueDff;
                                                    finalValueDff += valueDff;
                                                    mergedScenarioSciHistory.Add("sci");
                                                    mergedScenarioSciHistory.Add(Convert.ToString(valueDff));
                                                }
                                            }
                                            sciValueDff += finalValueDff;
                                            ScenarioSystem.fetch.SetScenatioSciVersionHistory(groupName, mergedScenarioSciHistory.ToArray());
                                        }
                                        else
                                        {
                                            ScenarioSystem.fetch.SetScenatioSciVersionHistory(groupName, playerScenarioSciHistory.ToArray());
                                        }
                                    }
                                }
                                /*
                                ConfigNode scenarioDataConfigNode = ConfigNodeSerializer.fetch.Deserialize(scenarioData);
                                ConfigNode oldScenarioDataConfigNode = ConfigNodeSerializer.fetch.Deserialize(oldScenarioData);
                                ConfigNode.Merge(oldScenarioDataConfigNode, scenarioDataConfigNode);
                                */
                                List<string> scenarioDataStringList = ByteArraySerializer.fetch.Deserialize(scenarioData);
                                List<string> oldScenarioDataStringList = ByteArraySerializer.fetch.Deserialize(oldScenarioData);
                                List<string> mergedScenarioDataStringList = oldScenarioDataStringList.Union(scenarioDataStringList).ToList<string>();
                                List<string> noDuplicatesScenarioDataStringList = UnDuplicater.StringDuplicateRemover(mergedScenarioDataStringList.ToArray());
                                if (noDuplicatesScenarioDataStringList.Any(s => s.Contains("funds")))
                                {
                                    for (int v = 0; v < noDuplicatesScenarioDataStringList.Count; v++)
                                    {
                                        if (noDuplicatesScenarioDataStringList[v].Contains("funds"))
                                        {
                                            noDuplicatesScenarioDataStringList[v].Replace("funds = ", "");
                                            double newFunds = Convert.ToDouble(noDuplicatesScenarioDataStringList[v]) + fundsValueDff;
                                            noDuplicatesScenarioDataStringList[v] = Convert.ToString(newFunds);
                                            noDuplicatesScenarioDataStringList[v] = "funds = " + Convert.ToString(noDuplicatesScenarioDataStringList[v]);
                                            break;
                                        }
                                    }
                                }
                                if (noDuplicatesScenarioDataStringList.Any(s => s.Contains("rep")))
                                {
                                    for (int v = 0; v < noDuplicatesScenarioDataStringList.Count; v++)
                                    {
                                        if (noDuplicatesScenarioDataStringList[v].Contains("rep"))
                                        {
                                            noDuplicatesScenarioDataStringList[v].Replace("rep = ", "");
                                            float newRep = Convert.ToSingle(noDuplicatesScenarioDataStringList[v]) + repValueDff;
                                            noDuplicatesScenarioDataStringList[v] = Convert.ToString(newRep);
                                            noDuplicatesScenarioDataStringList[v] = "rep = " + Convert.ToString(noDuplicatesScenarioDataStringList[v]);
                                            break;
                                        }
                                    }
                                }
                                if (noDuplicatesScenarioDataStringList.Any(s => s.Contains("sci")))
                                {
                                    for (int v = 0; v < noDuplicatesScenarioDataStringList.Count; v++)
                                    {
                                        if (noDuplicatesScenarioDataStringList[v] == "sci")
                                        {
                                            noDuplicatesScenarioDataStringList[v].Replace("sci = ", "");
                                            float newSci = Convert.ToSingle(noDuplicatesScenarioDataStringList[v]) + sciValueDff;
                                            noDuplicatesScenarioDataStringList[v] = Convert.ToString(newSci);
                                            noDuplicatesScenarioDataStringList[v] = "sci = " + Convert.ToString(noDuplicatesScenarioDataStringList[v]);
                                            break;
                                        }
                                    }
                                }
                                /*
                                byte[] oldScenarioDataSerializedConfigNode = ConfigNodeSerializer.fetch.Serialize(oldScenarioDataConfigNode);
                                File.WriteAllBytes(filePath, oldScenarioDataSerializedConfigNode);
                                */
                                byte[] mergedScenarioDataSerializedStringList = ByteArraySerializer.fetch.Serialize(noDuplicatesScenarioDataStringList);
                                File.WriteAllBytes(filePath, mergedScenarioDataSerializedStringList);
                            }
                        }
                    }
                    else
                    {
                        File.WriteAllBytes(filePath, scenarioData);
                    }
                }
            }
        }
    }
}

