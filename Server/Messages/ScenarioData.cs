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
        private static object scenarioDataLock = new object();

        public static void SendScenarioModules(ClientObject client)
        {
            lock (scenarioDataLock)
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
        }

        public static void SendScenarioGroupModules(ClientObject client, string groupName)
        {
            lock (scenarioDataLock)
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
        }

        public static void HandleScenarioModuleData(ClientObject client, byte[] messageData)
        {
            lock (scenarioDataLock)
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
        }

        public static void HandleGroupScenarioModuleData(ClientObject client, byte[] messageData)
        {
            lock (scenarioDataLock)
            {
                using (MessageReader mr = new MessageReader(messageData))
                {
                    string[] scenarioName = mr.Read<string[]>();
                    List<string> playerScenarioFundsHistory = mr.Read<string[]>().ToList();
                    List<string> playerScenarioRepHistory = mr.Read<string[]>().ToList();
                    List<string> playerScenarioSciHistory = mr.Read<string[]>().ToList();
                    string groupName = mr.Read<string>();

                    List<string> groupScenarioFundsHistory = ScenarioSystem.fetch.GetScenatioFundsVersionHistory(groupName).ToList();
                    List<string> groupScenarioRepHistory = ScenarioSystem.fetch.GetScenatioRepVersionHistory(groupName).ToList();
                    List<string> groupScenarioSciHistory = ScenarioSystem.fetch.GetScenatioSciVersionHistory(groupName).ToList();

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
                                float fundsValueDff = 0;
                                float repValueDff = 0;
                                float sciValueDff = 0;
                                if (scenarioName[i] != "Funding" && scenarioName[i] != "Reputation" && scenarioName[i] != "ResearchAndDevelopment")
                                {
                                    List<string> scenarioDataStringList = ByteArraySerializer.fetch.Deserialize(scenarioData);
                                    List<string> oldScenarioDataStringList = ByteArraySerializer.fetch.Deserialize(oldScenarioData);
                                    List<string> cleanedScenarioDataStringList = new List<string>();
                                    if (DataCleaner.IsDataGood(oldScenarioDataStringList))
                                    {
                                        List<string> mergedScenarioDataStringList = new List<string>(scenarioDataStringList);
                                        mergedScenarioDataStringList.AddRange(RemoveHeader.HeaderRemover(oldScenarioDataStringList));
                                        List<string> noDuplicatesScenarioDataStringList = UnDuplicater.StringDuplicateRemover(mergedScenarioDataStringList);
                                        cleanedScenarioDataStringList = DataCleaner.CleanData(noDuplicatesScenarioDataStringList);
                                    }
                                    else
                                    {
                                        List<string> noDuplicatesScenarioDataStringList = UnDuplicater.StringDuplicateRemover(scenarioDataStringList);
                                        cleanedScenarioDataStringList = DataCleaner.CleanData(noDuplicatesScenarioDataStringList);
                                    }
                                    byte[] mergedScenarioDataSerialized = ByteArraySerializer.fetch.Serialize(cleanedScenarioDataStringList);
                                    File.WriteAllBytes(filePath, mergedScenarioDataSerialized);
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
                                                float finalValueDff = 0;

                                                bool stopThisStep = false;
                                                bool stopNextStep = false;
                                                bool addPlayerHistory = false;

                                                for (int v = 1; v < playerScenarioFundsHistory.Count; v += 2)
                                                {
                                                    if ((v - 1) < playerScenarioFundsHistory.Count())
                                                    {
                                                        if (playerScenarioFundsHistory[v - 1].ToString() != "funds")
                                                        {
                                                            stopThisStep = true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        stopThisStep = true;
                                                    }
                                                    if ((v - 1) < groupScenarioFundsHistory.Count())
                                                    {
                                                        if (groupScenarioFundsHistory[v - 1].ToString() != "funds")
                                                        {
                                                            stopThisStep = true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if ((v - 1) < playerScenarioFundsHistory.Count())
                                                        {
                                                            addPlayerHistory = true;
                                                        }
                                                        else
                                                        {
                                                            stopThisStep = true;
                                                        }
                                                    }
                                                    if (!addPlayerHistory)
                                                    {
                                                        if (!stopThisStep)
                                                        {
                                                            if (!stopNextStep)
                                                            {
                                                                float currentPlayerFundsValue = Convert.ToSingle(playerScenarioFundsHistory[v].ToString());

                                                                float currentGroupFundsValue = Convert.ToSingle(groupScenarioFundsHistory[v].ToString());

                                                                if (currentPlayerFundsValue != currentGroupFundsValue)
                                                                {
                                                                    float lastPlayerFundsValue = Convert.ToSingle(playerScenarioFundsHistory[v - 2].ToString());
                                                                    float playerFundsValueDff = currentPlayerFundsValue -= lastPlayerFundsValue;

                                                                    float lastGroupFundsValue = Convert.ToSingle(groupScenarioFundsHistory[v - 2].ToString());
                                                                    float groupFundsValueDff = currentGroupFundsValue -= lastGroupFundsValue;

                                                                    float valueDff = playerFundsValueDff + groupFundsValueDff;
                                                                    finalValueDff += valueDff;
                                                                    mergedScenarioFundsHistory.Add("funds");
                                                                    mergedScenarioFundsHistory.Add(Convert.ToString(valueDff));
                                                                }
                                                                if ((v + 1) < playerScenarioFundsHistory.Count())
                                                                {
                                                                    if (playerScenarioFundsHistory[v + 1].ToString() != "funds")
                                                                    {
                                                                        stopNextStep = true;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    stopNextStep = true;
                                                                }
                                                                if ((v + 1) < groupScenarioFundsHistory.Count())
                                                                {
                                                                    if (groupScenarioFundsHistory[v + 1].ToString() != "funds")
                                                                    {
                                                                        stopNextStep = true;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    if ((v + 1) < playerScenarioFundsHistory.Count())
                                                                    {
                                                                        addPlayerHistory = true;
                                                                    }
                                                                    else
                                                                    {
                                                                        stopNextStep = true;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        int x = v;
                                                        while ((x + 1) < playerScenarioFundsHistory.Count())
                                                        {
                                                            float currentPlayerFundsValue = Convert.ToSingle(playerScenarioFundsHistory[x].ToString());

                                                            float lastPlayerFundsValue = Convert.ToSingle(playerScenarioFundsHistory[x - 2].ToString());
                                                            float valueDff = currentPlayerFundsValue -= lastPlayerFundsValue;

                                                            finalValueDff += valueDff;

                                                            mergedScenarioFundsHistory.Add("funds");
                                                            mergedScenarioFundsHistory.Add(Convert.ToString(valueDff));
                                                            x += 2;
                                                        }
                                                        break;
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

                                                bool stopThisStep = false;
                                                bool stopNextStep = false;
                                                bool addPlayerHistory = false;

                                                for (int v = 1; v < playerScenarioRepHistory.Count; v += 2)
                                                {
                                                    if ((v - 1) < playerScenarioRepHistory.Count())
                                                    {
                                                        if (playerScenarioRepHistory[v - 1].ToString() != "rep")
                                                        {
                                                            stopThisStep = true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        stopThisStep = true;
                                                    }
                                                    if ((v - 1) < groupScenarioRepHistory.Count())
                                                    {
                                                        if (groupScenarioRepHistory[v - 1].ToString() != "rep")
                                                        {
                                                            stopThisStep = true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if ((v - 1) < playerScenarioRepHistory.Count())
                                                        {
                                                            addPlayerHistory = true;
                                                        }
                                                        else
                                                        {
                                                            stopThisStep = true;
                                                        }
                                                    }
                                                    if (!addPlayerHistory)
                                                    {
                                                        if (!stopThisStep)
                                                        {
                                                            if (!stopNextStep)
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
                                                                if ((v + 1) < playerScenarioRepHistory.Count())
                                                                {
                                                                    if (playerScenarioRepHistory[v + 1].ToString() != "rep")
                                                                    {
                                                                        stopNextStep = true;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    stopNextStep = true;
                                                                }
                                                                if ((v + 1) < groupScenarioRepHistory.Count())
                                                                {
                                                                    if (groupScenarioRepHistory[v + 1].ToString() != "rep")
                                                                    {
                                                                        stopNextStep = true;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    if ((v - 1) < playerScenarioRepHistory.Count())
                                                                    {
                                                                        addPlayerHistory = true;
                                                                    }
                                                                    else
                                                                    {
                                                                        stopNextStep = true;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        int x = v;
                                                        while ((x + 1) < playerScenarioRepHistory.Count())
                                                        {
                                                            float currentPlayerRepValue = Convert.ToSingle(playerScenarioRepHistory[x].ToString());

                                                            float lastPlayerRepValue = Convert.ToSingle(playerScenarioRepHistory[x - 2].ToString());
                                                            float valueDff = currentPlayerRepValue -= lastPlayerRepValue;

                                                            finalValueDff += valueDff;

                                                            mergedScenarioRepHistory.Add("rep");
                                                            mergedScenarioRepHistory.Add(Convert.ToString(valueDff));
                                                            x += 2;
                                                        }
                                                        break;
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

                                                bool stopThisStep = false;
                                                bool stopNextStep = false;
                                                bool addPlayerHistory = false;

                                                for (int v = 1; v < playerScenarioSciHistory.Count; v += 2)
                                                {
                                                    if ((v - 1) < playerScenarioSciHistory.Count())
                                                    {
                                                        if (playerScenarioSciHistory[v - 1].ToString() != "sci")
                                                        {
                                                            stopThisStep = true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        stopThisStep = true;
                                                    }
                                                    if ((v - 1) < groupScenarioSciHistory.Count())
                                                    {
                                                        if (groupScenarioSciHistory[v - 1].ToString() != "sci")
                                                        {
                                                            stopThisStep = true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if ((v - 1) < playerScenarioSciHistory.Count())
                                                        {
                                                            addPlayerHistory = true;
                                                        }
                                                        else
                                                        {
                                                            stopThisStep = true;
                                                        }
                                                    }
                                                    if (!addPlayerHistory)
                                                    {
                                                        if (!stopThisStep)
                                                        {
                                                            if (!stopNextStep)
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
                                                                if ((v + 1) < playerScenarioSciHistory.Count())
                                                                {
                                                                    if (playerScenarioSciHistory[v + 1].ToString() != "sci")
                                                                    {
                                                                        stopNextStep = true;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    stopNextStep = true;
                                                                }
                                                                if ((v + 1) < groupScenarioSciHistory.Count())
                                                                {
                                                                    if (groupScenarioSciHistory[v + 1].ToString() != "sci")
                                                                    {
                                                                        stopNextStep = true;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    if ((v - 1) < playerScenarioSciHistory.Count())
                                                                    {
                                                                        addPlayerHistory = true;
                                                                    }
                                                                    else
                                                                    {
                                                                        stopNextStep = true;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        int x = v;
                                                        while ((x + 1) < playerScenarioSciHistory.Count())
                                                        {
                                                            float currentPlayerSciValue = Convert.ToSingle(playerScenarioSciHistory[x].ToString());

                                                            float lastPlayerSciValue = Convert.ToSingle(playerScenarioSciHistory[x - 2].ToString());
                                                            float valueDff = currentPlayerSciValue -= lastPlayerSciValue;

                                                            finalValueDff += valueDff;

                                                            mergedScenarioSciHistory.Add("sci");
                                                            mergedScenarioSciHistory.Add(Convert.ToString(valueDff));
                                                            x += 2;
                                                        }
                                                        break;
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
                                    List<string> scenarioDataStringList = ByteArraySerializer.fetch.Deserialize(scenarioData);
                                    List<string> oldScenarioDataStringList = ByteArraySerializer.fetch.Deserialize(oldScenarioData);
                                    List<string> cleanedScenarioDataStringList = new List<string>();
                                    if (DataCleaner.IsDataGood(oldScenarioDataStringList))
                                    {
                                        List<string> mergedScenarioDataStringList = new List<string>(scenarioDataStringList);
                                        mergedScenarioDataStringList.AddRange(RemoveHeader.HeaderRemover(oldScenarioDataStringList));
                                        List<string> noDuplicatesScenarioDataStringList = UnDuplicater.StringDuplicateRemover(mergedScenarioDataStringList);
                                        cleanedScenarioDataStringList = DataCleaner.CleanData(noDuplicatesScenarioDataStringList);
                                    }
                                    else
                                    {
                                        List<string> noDuplicatesScenarioDataStringList = UnDuplicater.StringDuplicateRemover(scenarioDataStringList);
                                        cleanedScenarioDataStringList = DataCleaner.CleanData(noDuplicatesScenarioDataStringList);
                                    }
                                    if (cleanedScenarioDataStringList.Any(s => s.Contains("funds")))
                                    {
                                        for (int v = 0; v < cleanedScenarioDataStringList.Count; v++)
                                        {
                                            if (cleanedScenarioDataStringList[v].Contains("funds"))
                                            {
                                                cleanedScenarioDataStringList[v].Replace("funds = ", "");
                                                float newFunds = Convert.ToSingle(cleanedScenarioDataStringList[v]) + fundsValueDff;
                                                cleanedScenarioDataStringList[v] = "funds = " + Convert.ToString(newFunds);
                                                break;
                                            }
                                        }
                                    }
                                    if (cleanedScenarioDataStringList.Any(s => s.Contains("rep")))
                                    {
                                        for (int v = 0; v < cleanedScenarioDataStringList.Count; v++)
                                        {
                                            if (cleanedScenarioDataStringList[v].Contains("rep"))
                                            {
                                                cleanedScenarioDataStringList[v].Replace("rep = ", "");
                                                float newRep = Convert.ToSingle(cleanedScenarioDataStringList[v]) + repValueDff;
                                                cleanedScenarioDataStringList[v] = "rep = " + Convert.ToString(newRep);
                                                break;
                                            }
                                        }
                                    }
                                    if (cleanedScenarioDataStringList.Any(s => s.Contains("sci")))
                                    {
                                        for (int v = 0; v < cleanedScenarioDataStringList.Count; v++)
                                        {
                                            if (cleanedScenarioDataStringList[v] == "sci")
                                            {
                                                cleanedScenarioDataStringList[v].Replace("sci = ", "");
                                                float newSci = Convert.ToSingle(cleanedScenarioDataStringList[v]) + sciValueDff;
                                                cleanedScenarioDataStringList[v] = "sci = " + Convert.ToString(newSci);
                                                break;
                                            }
                                        }
                                    }
                                    byte[] mergedScenarioDataSerialized = ByteArraySerializer.fetch.Serialize(cleanedScenarioDataStringList);
                                    File.WriteAllBytes(filePath, mergedScenarioDataSerialized);
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
}

