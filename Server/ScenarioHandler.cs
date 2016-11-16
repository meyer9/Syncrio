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


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MessageStream2;
using SyncrioCommon;
using SyncrioUtil;

namespace SyncrioServer
{
    class ScenarioHandler
    {
        public static List<GroupSubspaces> AllGroupSubspaces = new List<GroupSubspaces>();
        private static List<GroupProgress> AllGroupsProgress = new List<GroupProgress>();

        public static void HandleScenarioModuleData(ClientObject client, byte[] messageData)
        {
            lock (Messages.ScenarioData.scenarioDataLock)
            {
                using (MessageReader mr = new MessageReader(messageData))
                {
                    string[] scenarioName = mr.Read<string[]>();
                    SyncrioLog.Debug("Saving " + scenarioName.Length + " scenario modules from " + client.playerName);

                    for (int i = 0; i < scenarioName.Length; i++)
                    {
                        byte[] scenarioData = Compression.DecompressIfNeeded(mr.Read<byte[]>());
                        FileHandler.WriteToFile(scenarioData, Path.Combine(Server.ScenarioDirectory, "Players", client.playerName, scenarioName[i] + ".txt"));
                    }
                }
            }
        }
        
        public static void HandleAllGroupScenarioData(string groupName, Dictionary<string, List<byte[]>> scenarioList, int subSpace)
        {
            int numberOfScenatioTypesSaved = 0;

            foreach (string currentScenarioModuleType in scenarioList.Keys)
            {
                if (currentScenarioModuleType == "ContractSystem")
                {
                    List<ScenarioDataTypes.ContractSystem> convertedScenarioModules = new List<ScenarioDataTypes.ContractSystem>();
                    for (int i = 0; i < scenarioList[currentScenarioModuleType].Count; i++)
                    {
                        ScenarioDataTypes.ContractSystem newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ContractSystem());
                        convertedScenarioModules.Add(ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioList[currentScenarioModuleType][i]), newScenarioModule));
                    }
                    try
                    {
                        HandleConvertedScenarioModules(convertedScenarioModules, groupName, subSpace);
                    }
                    catch (Exception e)
                    {
                        SyncrioLog.Debug("Error in 'HandleAllGroupScenarioData' while trying to handle a 'ContractSystem'! Error: " + e);
                    }

                    numberOfScenatioTypesSaved += 1;

                    continue;
                }

                if (currentScenarioModuleType == "Funding")
                {
                    List<ScenarioDataTypes.Funding> convertedScenarioModules = new List<ScenarioDataTypes.Funding>();
                    for (int i = 0; i < scenarioList[currentScenarioModuleType].Count; i++)
                    {
                        ScenarioDataTypes.Funding newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.Funding());
                        convertedScenarioModules.Add(ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioList[currentScenarioModuleType][i]), newScenarioModule));
                    }
                    try
                    {
                        HandleConvertedScenarioModules(convertedScenarioModules, groupName, subSpace);
                    }
                    catch (Exception e)
                    {
                        SyncrioLog.Debug("Error in 'HandleAllGroupScenarioData' while trying to handle a 'Funding'! Error: " + e);
                    }

                    numberOfScenatioTypesSaved += 1;

                    continue;
                }

                if (currentScenarioModuleType == "PartUpgradeManager")
                {
                    List<ScenarioDataTypes.PartUpgradeManager> convertedScenarioModules = new List<ScenarioDataTypes.PartUpgradeManager>();
                    for (int i = 0; i < scenarioList[currentScenarioModuleType].Count; i++)
                    {
                        ScenarioDataTypes.PartUpgradeManager newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.PartUpgradeManager());
                        convertedScenarioModules.Add(ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioList[currentScenarioModuleType][i]), newScenarioModule));
                    }
                    try
                    {
                        HandleConvertedScenarioModules(convertedScenarioModules, groupName, subSpace);
                    }
                    catch (Exception e)
                    {
                        SyncrioLog.Debug("Error in 'HandleAllGroupScenarioData' while trying to handle a 'PartUpgradeManager'! Error: " + e);
                    }

                    numberOfScenatioTypesSaved += 1;

                    continue;
                }

                if (currentScenarioModuleType == "ProgressTracking")
                {
                    List<ScenarioDataTypes.ProgressTracking> convertedScenarioModules = new List<ScenarioDataTypes.ProgressTracking>();
                    for (int i = 0; i < scenarioList[currentScenarioModuleType].Count; i++)
                    {
                        ScenarioDataTypes.ProgressTracking newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ProgressTracking());
                        convertedScenarioModules.Add(ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioList[currentScenarioModuleType][i]), newScenarioModule));
                    }
                    try
                    {
                        HandleConvertedScenarioModules(convertedScenarioModules, groupName, subSpace);
                    }
                    catch (Exception e)
                    {
                        SyncrioLog.Debug("Error in 'HandleAllGroupScenarioData' while trying to handle a 'ProgressTracking'! Error: " + e);
                    }

                    numberOfScenatioTypesSaved += 1;

                    continue;
                }

                if (currentScenarioModuleType == "Reputation")
                {
                    List<ScenarioDataTypes.Reputation> convertedScenarioModules = new List<ScenarioDataTypes.Reputation>();
                    for (int i = 0; i < scenarioList[currentScenarioModuleType].Count; i++)
                    {
                        ScenarioDataTypes.Reputation newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.Reputation());
                        convertedScenarioModules.Add(ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioList[currentScenarioModuleType][i]), newScenarioModule));
                    }
                    try
                    {
                        HandleConvertedScenarioModules(convertedScenarioModules, groupName, subSpace);
                    }
                    catch (Exception e)
                    {
                        SyncrioLog.Debug("Error in 'HandleAllGroupScenarioData' while trying to handle a 'Reputation'! Error: " + e);
                    }

                    numberOfScenatioTypesSaved += 1;

                    continue;
                }

                if (currentScenarioModuleType == "ResearchAndDevelopment")
                {
                    List<ScenarioDataTypes.ResearchAndDevelopment> convertedScenarioModules = new List<ScenarioDataTypes.ResearchAndDevelopment>();
                    for (int i = 0; i < scenarioList[currentScenarioModuleType].Count; i++)
                    {
                        ScenarioDataTypes.ResearchAndDevelopment newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ResearchAndDevelopment());
                        convertedScenarioModules.Add(ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioList[currentScenarioModuleType][i]), newScenarioModule));
                    }
                    try
                    {
                        HandleConvertedScenarioModules(convertedScenarioModules, groupName, subSpace);
                    }
                    catch (Exception e)
                    {
                        SyncrioLog.Debug("Error in 'HandleAllGroupScenarioData' while trying to handle a 'ResearchAndDevelopment'! Error: " + e);
                    }

                    numberOfScenatioTypesSaved += 1;

                    continue;
                }

                if (currentScenarioModuleType == "ResourceScenario")
                {
                    List<ScenarioDataTypes.ResourceScenario> convertedScenarioModules = new List<ScenarioDataTypes.ResourceScenario>();
                    for (int i = 0; i < scenarioList[currentScenarioModuleType].Count; i++)
                    {
                        ScenarioDataTypes.ResourceScenario newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ResourceScenario());
                        convertedScenarioModules.Add(ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioList[currentScenarioModuleType][i]), newScenarioModule));
                    }
                    try
                    {
                        HandleConvertedScenarioModules(convertedScenarioModules, groupName, subSpace);
                    }
                    catch (Exception e)
                    {
                        SyncrioLog.Debug("Error in 'HandleAllGroupScenarioData' while trying to handle a 'ResourceScenario'! Error: " + e);
                    }

                    numberOfScenatioTypesSaved += 1;

                    continue;
                }

                if (currentScenarioModuleType == "ScenarioCustomWaypoints")
                {
                    List<ScenarioDataTypes.ScenarioCustomWaypoints> convertedScenarioModules = new List<ScenarioDataTypes.ScenarioCustomWaypoints>();
                    for (int i = 0; i < scenarioList[currentScenarioModuleType].Count; i++)
                    {
                        ScenarioDataTypes.ScenarioCustomWaypoints newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ScenarioCustomWaypoints());
                        convertedScenarioModules.Add(ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioList[currentScenarioModuleType][i]), newScenarioModule));
                    }
                    try
                    {
                        HandleConvertedScenarioModules(convertedScenarioModules, groupName, subSpace);
                    }
                    catch (Exception e)
                    {
                        SyncrioLog.Debug("Error in 'HandleAllGroupScenarioData' while trying to handle a 'ScenarioCustomWaypoints'! Error: " + e);
                    }

                    numberOfScenatioTypesSaved += 1;

                    continue;
                }

                if (currentScenarioModuleType == "ScenarioDestructibles")
                {
                    List<ScenarioDataTypes.ScenarioDestructibles> convertedScenarioModules = new List<ScenarioDataTypes.ScenarioDestructibles>();
                    for (int i = 0; i < scenarioList[currentScenarioModuleType].Count; i++)
                    {
                        ScenarioDataTypes.ScenarioDestructibles newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ScenarioDestructibles());
                        convertedScenarioModules.Add(ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioList[currentScenarioModuleType][i]), newScenarioModule));
                    }
                    try
                    {
                        HandleConvertedScenarioModules(convertedScenarioModules, groupName, subSpace);
                    }
                    catch (Exception e)
                    {
                        SyncrioLog.Debug("Error in 'HandleAllGroupScenarioData' while trying to handle a 'ScenarioDestructibles'! Error: " + e);
                    }

                    numberOfScenatioTypesSaved += 1;

                    continue;
                }

                if (currentScenarioModuleType == "ScenarioUpgradeableFacilities")
                {
                    List<ScenarioDataTypes.ScenarioUpgradeableFacilities> convertedScenarioModules = new List<ScenarioDataTypes.ScenarioUpgradeableFacilities>();
                    for (int i = 0; i < scenarioList[currentScenarioModuleType].Count; i++)
                    {
                        ScenarioDataTypes.ScenarioUpgradeableFacilities newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ScenarioUpgradeableFacilities());
                        convertedScenarioModules.Add(ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioList[currentScenarioModuleType][i]), newScenarioModule));
                    }
                    try
                    {
                        HandleConvertedScenarioModules(convertedScenarioModules, groupName, subSpace);
                    }
                    catch (Exception e)
                    {
                        SyncrioLog.Debug("Error in 'HandleAllGroupScenarioData' while trying to handle a 'ScenarioUpgradeableFacilities'! Error: " + e);
                    }

                    numberOfScenatioTypesSaved += 1;

                    continue;
                }

                if (currentScenarioModuleType == "StrategySystem")
                {
                    List<ScenarioDataTypes.StrategySystem> convertedScenarioModules = new List<ScenarioDataTypes.StrategySystem>();
                    for (int i = 0; i < scenarioList[currentScenarioModuleType].Count; i++)
                    {
                        ScenarioDataTypes.StrategySystem newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.StrategySystem());
                        convertedScenarioModules.Add(ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioList[currentScenarioModuleType][i]), newScenarioModule));
                    }
                    try
                    {
                        HandleConvertedScenarioModules(convertedScenarioModules, groupName, subSpace);
                    }
                    catch (Exception e)
                    {
                        SyncrioLog.Debug("Error in 'HandleAllGroupScenarioData' while trying to handle a 'StrategySystem'! Error: " + e);
                    }

                    numberOfScenatioTypesSaved += 1;

                    continue;
                }
            }
            SyncrioLog.Debug("Saved " + numberOfScenatioTypesSaved + " scenario group modules to subspace " + subSpace + " of " + groupName);
        }

        /// <summary>
        /// Merges the given list of scenario modules and writes it to the group file, at the given subspace.
        /// </summary>
        public static void HandleConvertedScenarioModules(List<ScenarioDataTypes.ContractSystem> inputList, string groupName, int subspace)
        {
            Regex wordRegex = new Regex(@"^[\w_]+", RegexOptions.None);// matches a single word

            ScenarioDataTypes.ContractSystem oldData = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ContractSystem());
            oldData = GetOldScenarioData(oldData, groupName, subspace);
            Dictionary<string, int> weightDifferences = new Dictionary<string, int>();
            List<string> mergedWeights = new List<string>();

            foreach (ScenarioDataTypes.ContractSystem cs in inputList)
            {
                if (oldData.header.Count == 0)
                {
                    if (cs.header.Count != 0)
                    {
                        oldData.header = cs.header;
                    }
                }
                else
                {
                    if (cs.header.Count != 0)
                    {
                        if (cs.header[2].Substring(0, 6) == "update")
                        {
                            double newContractSystemHeader = Convert.ToDouble(cs.header[2].Substring(9));

                            if (oldData.header[2].Substring(0, 6) == "update")
                            {
                                double oldContractSystemHeader = Convert.ToDouble(oldData.header[2].Substring(9));

                                if (newContractSystemHeader > oldContractSystemHeader)
                                {
                                    oldData.header = cs.header;
                                }
                            }
                            else
                            {
                                oldData.header = cs.header;
                            }
                        }
                    }
                }

                //Weight Handling
                if (oldData.weights.Count > 0)
                {
                    if (cs.weights.Count > 0)
                    {
                        for (int cursor = 0; cursor < oldData.weights.Count; cursor++)
                        {
                            if (cursor < cs.weights.Count)
                            {
                                string oldWeightName = wordRegex.Match(oldData.weights[cursor]).ToString();
                                KeyValuePair<string, int> oldWeightData = new KeyValuePair<string, int>(oldWeightName, Convert.ToInt32(oldData.weights[cursor].Substring(oldWeightName.Length + 3)));

                                string newWeightName = wordRegex.Match(cs.weights[cursor]).ToString();
                                KeyValuePair<string, int> newWeightData = new KeyValuePair<string, int>(newWeightName, Convert.ToInt32(cs.weights[cursor].Substring(newWeightName.Length + 3)));

                                if (newWeightData.Key == oldWeightData.Key)
                                {
                                    if (newWeightData.Value != oldWeightData.Value)
                                    {
                                        if (weightDifferences.ContainsKey(oldWeightData.Key))
                                        {
                                            weightDifferences[oldWeightData.Key] += newWeightData.Value - oldWeightData.Value;
                                        }
                                        else
                                        {
                                            weightDifferences.Add(oldWeightData.Key, newWeightData.Value);
                                        }
                                    }
                                    else
                                    {
                                        if (!weightDifferences.ContainsKey(oldWeightData.Key))
                                        {
                                            weightDifferences.Add(oldWeightData.Key, newWeightData.Value);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (cs.weights.Count > 0)
                    {
                        oldData.weights = cs.weights;
                    }
                }
                //Contract Handling
                List<ScenarioDataTypes.Contract> contractList = new List<ScenarioDataTypes.Contract>();

                if (oldData.contracts.Count > 0)
                {
                    if (cs.contracts.Count > 0)
                    {
                        for (int cursor = 0; cursor < oldData.contracts.Count; cursor++)
                        {
                            if (cursor < cs.contracts.Count)
                            {
                                ScenarioDataTypes.Contract oldContract = oldData.contracts[cursor];

                                ScenarioDataTypes.Contract newContract = cs.contracts[cursor];

                                if (newContract.guid == oldContract.guid)
                                {
                                    if (oldContract.contractDataLines.Any(i => i.StartsWith("state")))
                                    {
                                        string oldStateLine = oldContract.contractDataLines.FirstOrDefault(i => i.StartsWith("state"));

                                        if (newContract.contractDataLines.Any(i => i.StartsWith("state")))
                                        {
                                            string newStateLine = newContract.contractDataLines.FirstOrDefault(i => i.StartsWith("state"));

                                            if (newStateLine != oldStateLine)
                                            {
                                                if (newStateLine == "state = Active")
                                                {
                                                    contractList.Add(newContract);
                                                }
                                                else
                                                {
                                                    if (oldStateLine == "state = Active")
                                                    {
                                                        contractList.Add(oldContract);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (newStateLine == "state = Active")
                                                {
                                                    ScenarioDataTypes.Contract editedContract = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Contract());
                                                    List<ScenarioDataTypes.Param> editedContractParams = new List<ScenarioDataTypes.Param>();

                                                    editedContract.guid = newContract.guid;
                                                    editedContract.contractDataLines = newContract.contractDataLines;
                                                    editedContract.usedNodeNumbers = newContract.usedNodeNumbers;

                                                    for (int i = 0; i < newContract.parameters.Count; i++)
                                                    {
                                                        string newParamStateLine = newContract.parameters[i].paramLines.FirstOrDefault(v => v.StartsWith("state"));

                                                        if (newParamStateLine == "state = Complete" || newParamStateLine == "state = Failed")
                                                        {
                                                            editedContractParams.Add(newContract.parameters[i]);
                                                        }
                                                        else
                                                        {
                                                            for (int v = 0; v < newContract.parameters[i].subParameters.Count; v++)
                                                            {
                                                                ScenarioDataTypes.Param editedContractParameter = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Param());

                                                                editedContractParameter.nodeNumber = newContract.parameters[i].nodeNumber;
                                                                editedContractParameter.paramLines = newContract.parameters[i].paramLines;

                                                                editedContractParameter.subParameters.Add(newContract.parameters[i].subParameters[v]);

                                                                editedContractParams.Add(editedContractParameter);
                                                            }
                                                        }
                                                    }

                                                    editedContract.parameters.AddRange(editedContractParams);

                                                    contractList.Add(editedContract);
                                                }
                                                else
                                                {
                                                    contractList.Add(oldContract);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            contractList.Add(oldContract);
                                        }
                                    }
                                    else
                                    {
                                        if (newContract.contractDataLines.Any(i => i.StartsWith("state")))
                                        {
                                            contractList.Add(newContract);
                                        }
                                    }
                                }
                                else
                                {
                                    List<ScenarioDataTypes.Contract> newFinishedContracts = new List<ScenarioDataTypes.Contract>();
                                    if (cs.finishedContracts.Count > 0)
                                    {
                                        newFinishedContracts = cs.finishedContracts.Where(i => i.guid == oldContract.guid).ToList();
                                    }

                                    if (newFinishedContracts.Count == 0)
                                    {
                                        int cursorTwo = -1;
                                        while (newContract.guid != oldContract.guid)
                                        {
                                            if (cursorTwo + 1 < cs.contracts.Count)
                                            {
                                                cursorTwo++;
                                            }
                                            else
                                            {
                                                break;
                                            }
                                            newContract = cs.contracts[cursorTwo];
                                        }

                                        if (newContract.guid == oldContract.guid)
                                        {
                                            if (oldContract.contractDataLines.Any(i => i.StartsWith("state")))
                                            {
                                                string oldStateLine = oldContract.contractDataLines.FirstOrDefault(i => i.StartsWith("state"));

                                                if (newContract.contractDataLines.Any(i => i.StartsWith("state")))
                                                {
                                                    string newStateLine = newContract.contractDataLines.FirstOrDefault(i => i.StartsWith("state"));

                                                    if (newStateLine != oldStateLine)
                                                    {
                                                        if (newStateLine == "state = Active")
                                                        {
                                                            contractList.Add(newContract);
                                                        }
                                                        else
                                                        {
                                                            if (oldStateLine == "state = Active")
                                                            {
                                                                contractList.Add(oldContract);
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (newStateLine == "state = Active")
                                                        {
                                                            ScenarioDataTypes.Contract editedContract = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Contract());
                                                            List<ScenarioDataTypes.Param> editedContractParams = new List<ScenarioDataTypes.Param>();

                                                            editedContract.guid = newContract.guid;
                                                            editedContract.contractDataLines = newContract.contractDataLines;
                                                            editedContract.usedNodeNumbers = newContract.usedNodeNumbers;

                                                            for (int i = 0; i < newContract.parameters.Count; i++)
                                                            {
                                                                string newParamStateLine = newContract.parameters[i].paramLines.FirstOrDefault(v => v.StartsWith("state"));

                                                                if (newParamStateLine == "state = Complete" || newParamStateLine == "state = Failed")
                                                                {
                                                                    editedContractParams.Add(newContract.parameters[i]);
                                                                }
                                                                else
                                                                {
                                                                    for (int v = 0; v < newContract.parameters[i].subParameters.Count; v++)
                                                                    {
                                                                        ScenarioDataTypes.Param editedContractParameter = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Param());

                                                                        editedContractParameter.nodeNumber = newContract.parameters[i].nodeNumber;
                                                                        editedContractParameter.paramLines = newContract.parameters[i].paramLines;

                                                                        editedContractParameter.subParameters.Add(newContract.parameters[i].subParameters[v]);

                                                                        editedContractParams.Add(editedContractParameter);
                                                                    }
                                                                }
                                                            }

                                                            editedContract.parameters.AddRange(editedContractParams);

                                                            contractList.Add(editedContract);
                                                        }
                                                        else
                                                        {
                                                            contractList.Add(oldContract);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    contractList.Add(oldContract);
                                                }
                                            }
                                            else
                                            {
                                                if (newContract.contractDataLines.Any(i => i.StartsWith("state")))
                                                {
                                                    contractList.Add(newContract);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            contractList.Add(oldContract);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (cs.contracts.Count > 0)
                    {
                        contractList = cs.contracts;
                    }
                }
                //Finished Contract Handling
                List<ScenarioDataTypes.Contract> finishedContractList = new List<ScenarioDataTypes.Contract>();

                if (oldData.finishedContracts.Count > 0)
                {
                    if (cs.finishedContracts.Count > 0)
                    {
                        for (int cursor = 0; cursor < oldData.finishedContracts.Count; cursor++)
                        {
                            if (cursor < cs.finishedContracts.Count)
                            {
                                ScenarioDataTypes.Contract oldContract = oldData.finishedContracts[cursor];

                                ScenarioDataTypes.Contract newContract = cs.finishedContracts[cursor];

                                if (newContract.guid == oldContract.guid)
                                {
                                    finishedContractList.Add(newContract);
                                }
                                else
                                {
                                    int cursorTwo = -1;
                                    while (newContract.guid != oldContract.guid)
                                    {
                                        if (cursorTwo + 1 < cs.contracts.Count)
                                        {
                                            cursorTwo++;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                        newContract = cs.contracts[cursorTwo];
                                    }

                                    if (newContract.guid != oldContract.guid)
                                    {
                                        finishedContractList.Add(newContract);
                                    }
                                    else
                                    {
                                        finishedContractList.Add(oldContract);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (cs.finishedContracts.Count > 0)
                    {
                        finishedContractList = cs.finishedContracts;
                    }
                }

                oldData.contracts = contractList;
                oldData.finishedContracts = finishedContractList;
            }

            foreach (KeyValuePair<string, int> kvp in weightDifferences)
            {
                mergedWeights.Add(kvp.Key + " = " + kvp.Value);
            }

            oldData.weights = mergedWeights;

            SetScenarioData(oldData, groupName, subspace);
        }

        /// <summary>
        /// Merges the given list of scenario modules and writes it to the group file, at the given subspace.
        /// </summary>
        public static void HandleConvertedScenarioModules(List<ScenarioDataTypes.Funding> inputList, string groupName, int subspace)
        {
            GroupProgress newGP = new GroupProgress();

            newGP.GroupName = groupName;
            newGP.GroupSubspace = subspace;

            ScenarioDataTypes.Funding oldData = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.Funding());
            oldData = GetOldScenarioData(oldData, groupName, subspace);

            double oldFunds = 0;

            if (!string.IsNullOrEmpty(oldData.fundsLine))
            {
                oldFunds = Convert.ToDouble(oldData.fundsLine.Substring(8));
            }

            double endFunds = oldFunds;

            foreach (ScenarioDataTypes.Funding funding in inputList)
            {
                if (oldData.header.Count == 0)
                {
                    if (funding.header.Count != 0)
                    {
                        oldData.header = funding.header;
                    }
                }

                double newFunds = 0;

                if (funding.fundsLine.Length > 0)
                {
                    newFunds = Convert.ToDouble(funding.fundsLine.Substring(8));
                }

                if (newFunds != oldFunds)
                {
                    endFunds += newFunds - oldFunds;
                }
            }

            newGP.Funds = Convert.ToString(endFunds);

            oldData.fundsLine = "funds = " + Convert.ToString(endFunds);

            SetScenarioData(oldData, groupName, subspace);

            SetGroupProgress(groupName, subspace, newGP);
        }

        /// <summary>
        /// Merges the given list of scenario modules and writes it to the group file, at the given subspace.
        /// </summary>
        public static void HandleConvertedScenarioModules(List<ScenarioDataTypes.PartUpgradeManager> inputList, string groupName, int subspace)
        {
            ScenarioDataTypes.PartUpgradeManager oldData = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.PartUpgradeManager());
            oldData = GetOldScenarioData(oldData, groupName, subspace);

            oldData = inputList[0];//This is only temporary; Until I get better data on this scenario type.-----------------------------------------

            SetScenarioData(oldData, groupName, subspace);
        }

        /// <summary>
        /// Merges the given list of scenario modules and writes it to the group file, at the given subspace.
        /// </summary>
        public static void HandleConvertedScenarioModules(List<ScenarioDataTypes.ProgressTracking> inputList, string groupName, int subspace)
        {
            GroupProgress newGP = new GroupProgress();

            newGP.GroupName = groupName;
            newGP.GroupSubspace = subspace;

            ScenarioDataTypes.ProgressTracking oldData = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ProgressTracking());
            oldData = GetOldScenarioData(oldData, groupName, subspace);
            Dictionary<string, int> celestialBodyList = new Dictionary<string, int>();

            for (int i = 0; i < oldData.celestialProgress.Count; i++)
            {
                if (!celestialBodyList.ContainsKey(oldData.celestialProgress[i].celestialBody))
                {
                    celestialBodyList.Add(oldData.celestialProgress[i].celestialBody, i);
                }
            }

            foreach (ScenarioDataTypes.ProgressTracking pt in inputList)
            {
                if (oldData.header.Count == 0)
                {
                    if (pt.header.Count != 0)
                    {
                        oldData.header = pt.header;
                    }
                }

                ScenarioDataTypes.ProgressTracking editingProgress = oldData;

                //Basic Progress
                if (editingProgress.basicProgress.altitudeRecord.Count == 0)
                {
                    editingProgress.basicProgress.altitudeRecord = pt.basicProgress.altitudeRecord;

                    if (pt.basicProgress.altitudeRecord.Count > 0)
                    {
                        newGP.Progress.Add("altitudeRecord");
                    }
                }

                if (editingProgress.basicProgress.depthRecord.Count == 0)
                {
                    editingProgress.basicProgress.depthRecord = pt.basicProgress.depthRecord;

                    if (pt.basicProgress.depthRecord.Count > 0)
                    {
                        newGP.Progress.Add("depthRecord");
                    }
                }

                if (editingProgress.basicProgress.distanceRecord.Count == 0)
                {
                    editingProgress.basicProgress.distanceRecord = pt.basicProgress.distanceRecord;

                    if (pt.basicProgress.distanceRecord.Count > 0)
                    {
                        newGP.Progress.Add("distanceRecord");
                    }
                }

                if (editingProgress.basicProgress.firstCrewToSurvive.Count == 0)
                {
                    editingProgress.basicProgress.firstCrewToSurvive = pt.basicProgress.firstCrewToSurvive;

                    if (pt.basicProgress.firstCrewToSurvive.Count > 0)
                    {
                        newGP.Progress.Add("firstCrewToSurvive");
                    }
                }

                if (editingProgress.basicProgress.firstLaunch.Count == 0)
                {
                    editingProgress.basicProgress.firstLaunch = pt.basicProgress.firstLaunch;

                    if (pt.basicProgress.firstLaunch.Count > 0)
                    {
                        newGP.Progress.Add("firstLaunch");
                    }
                }

                if (editingProgress.basicProgress.KSCLanding.Count == 0)
                {
                    editingProgress.basicProgress.KSCLanding = pt.basicProgress.KSCLanding;

                    if (pt.basicProgress.KSCLanding.Count > 0)
                    {
                        newGP.Progress.Add("KSCLanding");
                    }
                }

                if (editingProgress.basicProgress.launchpadLanding.Count == 0)
                {
                    editingProgress.basicProgress.launchpadLanding = pt.basicProgress.launchpadLanding;

                    if (pt.basicProgress.launchpadLanding.Count > 0)
                    {
                        newGP.Progress.Add("launchpadLanding");
                    }
                }

                if (editingProgress.basicProgress.reachSpace.Count == 0)
                {
                    editingProgress.basicProgress.reachSpace = pt.basicProgress.reachSpace;

                    if (pt.basicProgress.reachSpace.Count > 0)
                    {
                        newGP.Progress.Add("reachSpace");
                    }
                }

                if (editingProgress.basicProgress.runwayLanding.Count == 0)
                {
                    editingProgress.basicProgress.runwayLanding = pt.basicProgress.runwayLanding;

                    if (pt.basicProgress.runwayLanding.Count > 0)
                    {
                        newGP.Progress.Add("runwayLanding");
                    }
                }

                if (editingProgress.basicProgress.speedRecord.Count == 0)
                {
                    editingProgress.basicProgress.speedRecord = pt.basicProgress.speedRecord;

                    if (pt.basicProgress.speedRecord.Count > 0)
                    {
                        newGP.Progress.Add("speedRecord");
                    }
                }

                if (editingProgress.basicProgress.towerBuzz.Count == 0)
                {
                    editingProgress.basicProgress.towerBuzz = pt.basicProgress.towerBuzz;

                    if (pt.basicProgress.towerBuzz.Count > 0)
                    {
                        newGP.Progress.Add("towerBuzz");
                    }
                }

                //Celestial Progress
                for (int i = 0; i < pt.celestialProgress.Count; i++)
                {
                    if (celestialBodyList.ContainsKey(pt.celestialProgress[i].celestialBody))
                    {
                        int key = -1;

                        celestialBodyList.TryGetValue(pt.celestialProgress[i].celestialBody, out key);

                        if (key != -1)
                        {
                            List<string> newCP = new List<string>();

                            ScenarioDataTypes.CelestialProgress editingCelestialProgress = editingProgress.celestialProgress[key];

                            newCP.Add(editingCelestialProgress.celestialBody);

                            newCP.Add(editingCelestialProgress.reached);

                            if (editingCelestialProgress.baseConstruction.Count == 0)
                            {
                                editingCelestialProgress.baseConstruction = pt.celestialProgress[i].baseConstruction;

                                if (pt.celestialProgress[i].baseConstruction.Count > 0)
                                {
                                    newCP.Add("baseConstruction");
                                }
                            }

                            if (editingCelestialProgress.crewTransfer.Count == 0)
                            {
                                editingCelestialProgress.crewTransfer = pt.celestialProgress[i].crewTransfer;

                                if (pt.celestialProgress[i].crewTransfer.Count > 0)
                                {
                                    newCP.Add("crewTransfer");
                                }
                            }

                            if (editingCelestialProgress.docking.Count == 0)
                            {
                                editingCelestialProgress.docking = pt.celestialProgress[i].docking;

                                if (pt.celestialProgress[i].docking.Count > 0)
                                {
                                    newCP.Add("docking");
                                }
                            }

                            if (editingCelestialProgress.escape.Count == 0)
                            {
                                editingCelestialProgress.escape = pt.celestialProgress[i].escape;

                                if (pt.celestialProgress[i].escape.Count > 0)
                                {
                                    newCP.Add("escape");
                                }
                            }

                            if (editingCelestialProgress.flagPlant.Count == 0)
                            {
                                editingCelestialProgress.flagPlant = pt.celestialProgress[i].flagPlant;

                                if (pt.celestialProgress[i].flagPlant.Count > 0)
                                {
                                    newCP.Add("flagPlant");
                                }
                            }

                            if (editingCelestialProgress.flight.Count == 0)
                            {
                                editingCelestialProgress.flight = pt.celestialProgress[i].flight;

                                if (pt.celestialProgress[i].flight.Count > 0)
                                {
                                    newCP.Add("flight");
                                }
                            }

                            if (editingCelestialProgress.flyBy.Count == 0)
                            {
                                editingCelestialProgress.flyBy = pt.celestialProgress[i].flyBy;

                                if (pt.celestialProgress[i].flyBy.Count > 0)
                                {
                                    newCP.Add("flyBy");
                                }
                            }

                            if (editingCelestialProgress.landing.Count == 0)
                            {
                                editingCelestialProgress.landing = pt.celestialProgress[i].landing;

                                if (pt.celestialProgress[i].landing.Count > 0)
                                {
                                    newCP.Add("landing");
                                }
                            }

                            if (editingCelestialProgress.orbit.Count == 0)
                            {
                                editingCelestialProgress.orbit = pt.celestialProgress[i].orbit;

                                if (pt.celestialProgress[i].orbit.Count > 0)
                                {
                                    newCP.Add("orbit");
                                }
                            }

                            if (editingCelestialProgress.rendezvous.Count == 0)
                            {
                                editingCelestialProgress.rendezvous = pt.celestialProgress[i].rendezvous;

                                if (pt.celestialProgress[i].rendezvous.Count > 0)
                                {
                                    newCP.Add("rendezvous");
                                }
                            }

                            if (editingCelestialProgress.returnFromFlyby.Count == 0)
                            {
                                editingCelestialProgress.returnFromFlyby = pt.celestialProgress[i].returnFromFlyby;

                                if (pt.celestialProgress[i].returnFromFlyby.Count > 0)
                                {
                                    newCP.Add("returnFromFlyby");
                                }
                            }

                            if (editingCelestialProgress.returnFromOrbit.Count == 0)
                            {
                                editingCelestialProgress.returnFromOrbit = pt.celestialProgress[i].returnFromOrbit;

                                if (pt.celestialProgress[i].returnFromOrbit.Count > 0)
                                {
                                    newCP.Add("returnFromOrbit");
                                }
                            }

                            if (editingCelestialProgress.returnFromSurface.Count == 0)
                            {
                                editingCelestialProgress.returnFromSurface = pt.celestialProgress[i].returnFromSurface;

                                if (pt.celestialProgress[i].returnFromSurface.Count > 0)
                                {
                                    newCP.Add("returnFromSurface");
                                }
                            }

                            if (editingCelestialProgress.science.Count == 0)
                            {
                                editingCelestialProgress.science = pt.celestialProgress[i].science;

                                if (pt.celestialProgress[i].science.Count > 0)
                                {
                                    newCP.Add("science");
                                }
                            }

                            if (editingCelestialProgress.spacewalk.Count == 0)
                            {
                                editingCelestialProgress.spacewalk = pt.celestialProgress[i].spacewalk;

                                if (pt.celestialProgress[i].spacewalk.Count > 0)
                                {
                                    newCP.Add("spacewalk");
                                }
                            }

                            if (editingCelestialProgress.splashdown.Count == 0)
                            {
                                editingCelestialProgress.splashdown = pt.celestialProgress[i].splashdown;

                                if (pt.celestialProgress[i].splashdown.Count > 0)
                                {
                                    newCP.Add("splashdown");
                                }
                            }

                            if (editingCelestialProgress.stationConstruction.Count == 0)
                            {
                                editingCelestialProgress.stationConstruction = pt.celestialProgress[i].stationConstruction;

                                if (pt.celestialProgress[i].stationConstruction.Count > 0)
                                {
                                    newCP.Add("stationConstruction");
                                }
                            }

                            if (editingCelestialProgress.suborbit.Count == 0)
                            {
                                editingCelestialProgress.suborbit = pt.celestialProgress[i].suborbit;

                                if (pt.celestialProgress[i].suborbit.Count > 0)
                                {
                                    newCP.Add("suborbit");
                                }
                            }

                            if (editingCelestialProgress.surfaceEVA.Count == 0)
                            {
                                editingCelestialProgress.surfaceEVA = pt.celestialProgress[i].surfaceEVA;

                                if (pt.celestialProgress[i].surfaceEVA.Count > 0)
                                {
                                    newCP.Add("surfaceEVA");
                                }
                            }

                            editingProgress.celestialProgress[key] = editingCelestialProgress;

                            newGP.CelestialProgress.Add(newCP);
                        }
                    }
                    else
                    {
                        editingProgress.celestialProgress.Add(pt.celestialProgress[i]);

                        celestialBodyList.Add(pt.celestialProgress[i].celestialBody, celestialBodyList.Count);
                    }
                }

                //Secrets --- AKA "Spoilers!!!"
                /*                            *\
                 * -------------------------- *
                 * Alert!!!!!!!!!!!!!!!!!!!!!!*
                 * Spoilers!!!!!!!!!!!!!!!!!!!*
                 * Ahead!!!!!!!!!!!!!!!!!!!!!!*
                 * -------------------------- *
                \*                            */
                if (editingProgress.secrets.POIBopDeadKraken.Count == 0)
                {
                    editingProgress.secrets.POIBopDeadKraken = pt.secrets.POIBopDeadKraken;

                    if (pt.secrets.POIBopDeadKraken.Count > 0)
                    {
                        newGP.Secrets.Add("POIBopDeadKraken");
                    }
                }

                if (editingProgress.secrets.POIBopRandolith.Count == 0)
                {
                    editingProgress.secrets.POIBopRandolith = pt.secrets.POIBopRandolith;

                    if (pt.secrets.POIBopRandolith.Count > 0)
                    {
                        newGP.Secrets.Add("POIBopRandolith");
                    }
                }

                if (editingProgress.secrets.POIDresRandolith.Count == 0)
                {
                    editingProgress.secrets.POIDresRandolith = pt.secrets.POIDresRandolith;

                    if (pt.secrets.POIDresRandolith.Count > 0)
                    {
                        newGP.Secrets.Add("POIDresRandolith");
                    }
                }

                if (editingProgress.secrets.POIDunaFace.Count == 0)
                {
                    editingProgress.secrets.POIDunaFace = pt.secrets.POIDunaFace;

                    if (pt.secrets.POIDunaFace.Count > 0)
                    {
                        newGP.Secrets.Add("POIDunaFace");
                    }
                }

                if (editingProgress.secrets.POIDunaMSL.Count == 0)
                {
                    editingProgress.secrets.POIDunaMSL = pt.secrets.POIDunaMSL;

                    if (pt.secrets.POIDunaMSL.Count > 0)
                    {
                        newGP.Secrets.Add("POIDunaMSL");
                    }
                }

                if (editingProgress.secrets.POIDunaPyramid.Count == 0)
                {
                    editingProgress.secrets.POIDunaPyramid = pt.secrets.POIDunaPyramid;

                    if (pt.secrets.POIDunaPyramid.Count > 0)
                    {
                        newGP.Secrets.Add("POIDunaPyramid");
                    }
                }

                if (editingProgress.secrets.POIDunaRandolith.Count == 0)
                {
                    editingProgress.secrets.POIDunaRandolith = pt.secrets.POIDunaRandolith;

                    if (pt.secrets.POIDunaRandolith.Count > 0)
                    {
                        newGP.Secrets.Add("POIDunaRandolith");
                    }
                }

                if (editingProgress.secrets.POIEelooRandolith.Count == 0)
                {
                    editingProgress.secrets.POIEelooRandolith = pt.secrets.POIEelooRandolith;

                    if (pt.secrets.POIEelooRandolith.Count > 0)
                    {
                        newGP.Secrets.Add("POIEelooRandolith");
                    }
                }

                if (editingProgress.secrets.POIEveRandolith.Count == 0)
                {
                    editingProgress.secrets.POIEveRandolith = pt.secrets.POIEveRandolith;

                    if (pt.secrets.POIEveRandolith.Count > 0)
                    {
                        newGP.Secrets.Add("POIEveRandolith");
                    }
                }

                if (editingProgress.secrets.POIGillyRandolith.Count == 0)
                {
                    editingProgress.secrets.POIGillyRandolith = pt.secrets.POIGillyRandolith;

                    if (pt.secrets.POIGillyRandolith.Count > 0)
                    {
                        newGP.Secrets.Add("POIGillyRandolith");
                    }
                }

                if (editingProgress.secrets.POIIkeRandolith.Count == 0)
                {
                    editingProgress.secrets.POIIkeRandolith = pt.secrets.POIIkeRandolith;

                    if (pt.secrets.POIIkeRandolith.Count > 0)
                    {
                        newGP.Secrets.Add("POIIkeRandolith");
                    }
                }

                if (editingProgress.secrets.POIKerbinIslandAirfield.Count == 0)
                {
                    editingProgress.secrets.POIKerbinIslandAirfield = pt.secrets.POIKerbinIslandAirfield;

                    if (pt.secrets.POIKerbinIslandAirfield.Count > 0)
                    {
                        newGP.Secrets.Add("POIKerbinIslandAirfield");
                    }
                }

                if (editingProgress.secrets.POIKerbinKSC2.Count == 0)
                {
                    editingProgress.secrets.POIKerbinKSC2 = pt.secrets.POIKerbinKSC2;

                    if (pt.secrets.POIKerbinKSC2.Count > 0)
                    {
                        newGP.Secrets.Add("POIKerbinKSC2");
                    }
                }

                if (editingProgress.secrets.POIKerbinMonolith00.Count == 0)
                {
                    editingProgress.secrets.POIKerbinMonolith00 = pt.secrets.POIKerbinMonolith00;

                    if (pt.secrets.POIKerbinMonolith00.Count > 0)
                    {
                        newGP.Secrets.Add("POIKerbinMonolith00");
                    }
                }

                if (editingProgress.secrets.POIKerbinMonolith01.Count == 0)
                {
                    editingProgress.secrets.POIKerbinMonolith01 = pt.secrets.POIKerbinMonolith01;

                    if (pt.secrets.POIKerbinMonolith01.Count > 0)
                    {
                        newGP.Secrets.Add("POIKerbinMonolith01");
                    }
                }

                if (editingProgress.secrets.POIKerbinMonolith02.Count == 0)
                {
                    editingProgress.secrets.POIKerbinMonolith02 = pt.secrets.POIKerbinMonolith02;

                    if (pt.secrets.POIKerbinMonolith02.Count > 0)
                    {
                        newGP.Secrets.Add("POIKerbinMonolith02");
                    }
                }

                if (editingProgress.secrets.POIKerbinPyramids.Count == 0)
                {
                    editingProgress.secrets.POIKerbinPyramids = pt.secrets.POIKerbinPyramids;

                    if (pt.secrets.POIKerbinPyramids.Count > 0)
                    {
                        newGP.Secrets.Add("POIKerbinPyramids");
                    }
                }

                if (editingProgress.secrets.POIKerbinRandolith.Count == 0)
                {
                    editingProgress.secrets.POIKerbinRandolith = pt.secrets.POIKerbinRandolith;

                    if (pt.secrets.POIKerbinRandolith.Count > 0)
                    {
                        newGP.Secrets.Add("POIKerbinRandolith");
                    }
                }

                if (editingProgress.secrets.POIKerbinUFO.Count == 0)
                {
                    editingProgress.secrets.POIKerbinUFO = pt.secrets.POIKerbinUFO;

                    if (pt.secrets.POIKerbinUFO.Count > 0)
                    {
                        newGP.Secrets.Add("POIKerbinUFO");
                    }
                }

                if (editingProgress.secrets.POILaytheRandolith.Count == 0)
                {
                    editingProgress.secrets.POILaytheRandolith = pt.secrets.POILaytheRandolith;

                    if (pt.secrets.POILaytheRandolith.Count > 0)
                    {
                        newGP.Secrets.Add("POILaytheRandolith");
                    }
                }

                if (editingProgress.secrets.POIMinmusMonolith00.Count == 0)
                {
                    editingProgress.secrets.POIMinmusMonolith00 = pt.secrets.POIMinmusMonolith00;

                    if (pt.secrets.POIMinmusMonolith00.Count > 0)
                    {
                        newGP.Secrets.Add("POIMinmusMonolith00");
                    }
                }

                if (editingProgress.secrets.POIMinmusRandolith.Count == 0)
                {
                    editingProgress.secrets.POIMinmusRandolith = pt.secrets.POIMinmusRandolith;

                    if (pt.secrets.POIMinmusRandolith.Count > 0)
                    {
                        newGP.Secrets.Add("POIMinmusRandolith");
                    }
                }

                if (editingProgress.secrets.POIMohoRandolith.Count == 0)
                {
                    editingProgress.secrets.POIMohoRandolith = pt.secrets.POIMohoRandolith;

                    if (pt.secrets.POIMohoRandolith.Count > 0)
                    {
                        newGP.Secrets.Add("POIMohoRandolith");
                    }
                }

                if (editingProgress.secrets.POIMunArmstrongMemorial.Count == 0)
                {
                    editingProgress.secrets.POIMunArmstrongMemorial = pt.secrets.POIMunArmstrongMemorial;

                    if (pt.secrets.POIMunArmstrongMemorial.Count > 0)
                    {
                        newGP.Secrets.Add("POIMunArmstrongMemorial");
                    }
                }

                if (editingProgress.secrets.POIMunMonolith00.Count == 0)
                {
                    editingProgress.secrets.POIMunMonolith00 = pt.secrets.POIMunMonolith00;

                    if (pt.secrets.POIMunMonolith00.Count > 0)
                    {
                        newGP.Secrets.Add("POIMunMonolith00");
                    }
                }

                if (editingProgress.secrets.POIMunMonolith01.Count == 0)
                {
                    editingProgress.secrets.POIMunMonolith01 = pt.secrets.POIMunMonolith01;

                    if (pt.secrets.POIMunMonolith01.Count > 0)
                    {
                        newGP.Secrets.Add("POIMunMonolith01");
                    }
                }

                if (editingProgress.secrets.POIMunMonolith02.Count == 0)
                {
                    editingProgress.secrets.POIMunMonolith02 = pt.secrets.POIMunMonolith02;

                    if (pt.secrets.POIMunMonolith02.Count > 0)
                    {
                        newGP.Secrets.Add("POIMunMonolith02");
                    }
                }

                if (editingProgress.secrets.POIMunRandolith.Count == 0)
                {
                    editingProgress.secrets.POIMunRandolith = pt.secrets.POIMunRandolith;

                    if (pt.secrets.POIMunRandolith.Count > 0)
                    {
                        newGP.Secrets.Add("POIMunRandolith");
                    }
                }

                if (editingProgress.secrets.POIMunRockArch00.Count == 0)
                {
                    editingProgress.secrets.POIMunRockArch00 = pt.secrets.POIMunRockArch00;

                    if (pt.secrets.POIMunRockArch00.Count > 0)
                    {
                        newGP.Secrets.Add("POIMunRockArch00");
                    }
                }

                if (editingProgress.secrets.POIMunRockArch01.Count == 0)
                {
                    editingProgress.secrets.POIMunRockArch01 = pt.secrets.POIMunRockArch01;

                    if (pt.secrets.POIMunRockArch01.Count > 0)
                    {
                        newGP.Secrets.Add("POIMunRockArch01");
                    }
                }

                if (editingProgress.secrets.POIMunRockArch02.Count == 0)
                {
                    editingProgress.secrets.POIMunRockArch02 = pt.secrets.POIMunRockArch02;

                    if (pt.secrets.POIMunRockArch02.Count > 0)
                    {
                        newGP.Secrets.Add("POIMunRockArch02");
                    }
                }

                if (editingProgress.secrets.POIMunUFO.Count == 0)
                {
                    editingProgress.secrets.POIMunUFO = pt.secrets.POIMunUFO;

                    if (pt.secrets.POIMunUFO.Count > 0)
                    {
                        newGP.Secrets.Add("POIMunUFO");
                    }
                }

                if (editingProgress.secrets.POIPolRandolith.Count == 0)
                {
                    editingProgress.secrets.POIPolRandolith = pt.secrets.POIPolRandolith;

                    if (pt.secrets.POIPolRandolith.Count > 0)
                    {
                        newGP.Secrets.Add("POIPolRandolith");
                    }
                }

                if (editingProgress.secrets.POITyloCave.Count == 0)
                {
                    editingProgress.secrets.POITyloCave = pt.secrets.POITyloCave;

                    if (pt.secrets.POITyloCave.Count > 0)
                    {
                        newGP.Secrets.Add("POITyloCave");
                    }
                }

                if (editingProgress.secrets.POITyloRandolith.Count == 0)
                {
                    editingProgress.secrets.POITyloRandolith = pt.secrets.POITyloRandolith;

                    if (pt.secrets.POITyloRandolith.Count > 0)
                    {
                        newGP.Secrets.Add("POITyloRandolith");
                    }
                }

                if (editingProgress.secrets.POIVallIcehenge.Count == 0)
                {
                    editingProgress.secrets.POIVallIcehenge = pt.secrets.POIVallIcehenge;

                    if (pt.secrets.POIVallIcehenge.Count > 0)
                    {
                        newGP.Secrets.Add("POIVallIcehenge");
                    }
                }

                if (editingProgress.secrets.POIVallRandolith.Count == 0)
                {
                    editingProgress.secrets.POIVallRandolith = pt.secrets.POIVallRandolith;

                    if (pt.secrets.POIVallRandolith.Count > 0)
                    {
                        newGP.Secrets.Add("POIVallRandolith");
                    }
                }
                /*                            *\
                 * -------------------------- *
                 * End!!!!!!!!!!!!!!!!!!!!!!!!*
                 * Of!!!!!!!!!!!!!!!!!!!!!!!!!*
                 * Spoilers!!!!!!!!!!!!!!!!!!!*
                 * -------------------------- *
                \*                            */

                oldData = editingProgress;
            }

            SetScenarioData(oldData, groupName, subspace);

            SetGroupProgress(groupName, subspace, newGP);
        }

        /// <summary>
        /// Merges the given list of scenario modules and writes it to the group file, at the given subspace.
        /// </summary>
        public static void HandleConvertedScenarioModules(List<ScenarioDataTypes.Reputation> inputList, string groupName, int subspace)
        {
            GroupProgress newGP = new GroupProgress();

            newGP.GroupName = groupName;
            newGP.GroupSubspace = subspace;

            ScenarioDataTypes.Reputation oldData = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.Reputation());
            oldData = GetOldScenarioData(oldData, groupName, subspace);

            float oldRep = 0;

            if (!string.IsNullOrEmpty(oldData.repLine))
            {
                oldRep = Convert.ToSingle(oldData.repLine.Substring(6));
            }

            float endRep = oldRep;

            foreach (ScenarioDataTypes.Reputation reputation in inputList)
            {
                if (oldData.header.Count == 0)
                {
                    if (reputation.header.Count != 0)
                    {
                        oldData.header = reputation.header;
                    }
                }

                float newFunds = 0;

                if (reputation.repLine.Length > 0)
                {
                    newFunds = Convert.ToSingle(reputation.repLine.Substring(6));
                }

                if (newFunds != oldRep)
                {
                    endRep += newFunds - oldRep;
                }
            }

            newGP.Rep = Convert.ToString(endRep);

            oldData.repLine = "rep = " + Convert.ToString(endRep);

            SetScenarioData(oldData, groupName, subspace);

            SetGroupProgress(groupName, subspace, newGP);
        }

        /// <summary>
        /// Merges the given list of scenario modules and writes it to the group file, at the given subspace.
        /// </summary>
        public static void HandleConvertedScenarioModules(List<ScenarioDataTypes.ResearchAndDevelopment> inputList, string groupName, int subspace)
        {
            GroupProgress newGP = new GroupProgress();

            newGP.GroupName = groupName;
            newGP.GroupSubspace = subspace;

            ScenarioDataTypes.ResearchAndDevelopment oldData = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ResearchAndDevelopment());
            oldData = GetOldScenarioData(oldData, groupName, subspace);
            List<ScenarioDataTypes.Tech> techList = new List<ScenarioDataTypes.Tech>();
            List<string> addedTechsList = new List<string>();
            List<List<string>> scienceList = new List<List<string>>();
            List<string> addedSciencesList = new List<string>();

            float oldSci = 0;

            if (!string.IsNullOrEmpty(oldData.sciLine))
            {
                oldSci = Convert.ToSingle(oldData.sciLine.Substring(6));
            }

            float endSci = oldSci;

            foreach (ScenarioDataTypes.ResearchAndDevelopment RnD in inputList)
            {
                if (oldData.header.Count == 0)
                {
                    if (RnD.header.Count != 0)
                    {
                        oldData.header = RnD.header;
                    }
                }

                float newFunds = 0;

                if (RnD.sciLine.Length > 0)
                {
                    newFunds = Convert.ToSingle(RnD.sciLine.Substring(6));
                }

                if (newFunds != oldSci)
                {
                    endSci += newFunds - oldSci;
                }

                for (int i = 0; i < RnD.techList.Count; i++)
                {
                    if (!addedTechsList.Contains(RnD.techList[i].idLine))
                    {
                        techList.Add(RnD.techList[i]);
                        addedTechsList.Add(RnD.techList[i].idLine);
                    }
                }

                for (int i = 0; i < oldData.techList.Count; i++)
                {
                    if (!addedTechsList.Contains(oldData.techList[i].idLine))
                    {
                        techList.Add(oldData.techList[i]);
                        addedTechsList.Add(oldData.techList[i].idLine);
                    }
                }

                for (int i = 0; i < RnD.scienceList.Count; i++)
                {
                    if (!addedSciencesList.Contains(RnD.scienceList[i][0]))
                    {
                        scienceList.Add(RnD.scienceList[i]);
                        addedSciencesList.Add(RnD.scienceList[i][0]);
                    }
                }

                for (int i = 0; i < oldData.scienceList.Count; i++)
                {
                    if (!addedSciencesList.Contains(oldData.scienceList[i][0]))
                    {
                        scienceList.Add(oldData.scienceList[i]);
                        addedSciencesList.Add(oldData.scienceList[i][0]);
                    }
                }
            }

            newGP.Sci = Convert.ToString(endSci);

            newGP.Techs = addedTechsList;

            oldData.sciLine = "sci = " + Convert.ToString(endSci);

            oldData.techList = techList;

            oldData.scienceList = scienceList;

            SetScenarioData(oldData, groupName, subspace);

            SetGroupProgress(groupName, subspace, newGP);
        }

        /// <summary>
        /// Merges the given list of scenario modules and writes it to the group file, at the given subspace.
        /// </summary>
        public static void HandleConvertedScenarioModules(List<ScenarioDataTypes.ResourceScenario> inputList, string groupName, int subspace)
        {
            ScenarioDataTypes.ResourceScenario oldData = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ResourceScenario());
            oldData = GetOldScenarioData(oldData, groupName, subspace);
            List<string> listOfAlreadyAddedPlanetScanData = new List<string>();

            for (int i = 0; i < oldData.resourceSettings.scanDataList.Count; i++)
            {
                listOfAlreadyAddedPlanetScanData.Add(oldData.resourceSettings.scanDataList[i].scanDataLine);
            }

            foreach (ScenarioDataTypes.ResourceScenario rs in inputList)
            {
                if (oldData.header.Count == 0)
                {
                    if (rs.header.Count != 0)
                    {
                        oldData.header = rs.header;
                    }
                }

                for (int i = 0; i < rs.resourceSettings.scanDataList.Count; i++)
                {
                    if (!listOfAlreadyAddedPlanetScanData.Contains(rs.resourceSettings.scanDataList[i].scanDataLine))
                    {
                        oldData.resourceSettings.scanDataList.Add(rs.resourceSettings.scanDataList[i]);

                        listOfAlreadyAddedPlanetScanData.Add(rs.resourceSettings.scanDataList[i].scanDataLine);
                    }
                }
            }

            SetScenarioData(oldData, groupName, subspace);
        }

        /// <summary>
        /// Merges the given list of scenario modules and writes it to the group file, at the given subspace.
        /// </summary>
        public static void HandleConvertedScenarioModules(List<ScenarioDataTypes.ScenarioCustomWaypoints> inputList, string groupName, int subspace)
        {
            ScenarioDataTypes.ScenarioCustomWaypoints oldData = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ScenarioCustomWaypoints());
            oldData = GetOldScenarioData(oldData, groupName, subspace);
            List<string> listOfAlreadyAddedWaypoints = new List<string>();

            for (int i = 0; i < oldData.waypoints.Count; i++)
            {
                listOfAlreadyAddedWaypoints.Add(oldData.waypoints[i].waypointLines[0]);
            }

            foreach (ScenarioDataTypes.ScenarioCustomWaypoints scw in inputList)
            {
                if (oldData.header.Count == 0)
                {
                    if (scw.header.Count != 0)
                    {
                        oldData.header = scw.header;
                    }
                }

                for (int i = 0; i < scw.waypoints.Count; i++)
                {
                    if (!listOfAlreadyAddedWaypoints.Contains(scw.waypoints[i].waypointLines[0]))
                    {
                        oldData.waypoints.Add(scw.waypoints[i]);

                        listOfAlreadyAddedWaypoints.Add(scw.waypoints[i].waypointLines[0]);
                    }
                }
            }

            SetScenarioData(oldData, groupName, subspace);
        }

        /// <summary>
        /// Merges the given list of scenario modules and writes it to the group file, at the given subspace.
        /// </summary>
        public static void HandleConvertedScenarioModules(List<ScenarioDataTypes.ScenarioDestructibles> inputList, string groupName, int subspace)
        {
            ScenarioDataTypes.ScenarioDestructibles oldData = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ScenarioDestructibles());
            oldData = GetOldScenarioData(oldData, groupName, subspace);
            List<string> hasChanged = new List<string>();

            foreach (ScenarioDataTypes.ScenarioDestructibles sd in inputList)
            {
                if (oldData.header.Count == 0)
                {
                    if (sd.header.Count != 0)
                    {
                        oldData.header = sd.header;
                    }
                }

                for (int i = 0; i < oldData.destructibles.Count; i++)
                {
                    string currentID = oldData.destructibles[i].id;

                    if (!hasChanged.Contains(currentID))
                    {
                        ScenarioDataTypes.Destructibles newDestructible = sd.destructibles.Where(v => v.id == currentID).ToList()[0];

                        if (newDestructible.infoLine != oldData.destructibles[i].infoLine)
                        {
                            oldData.destructibles[i] = newDestructible;

                            hasChanged.Add(currentID);
                        }
                    }
                }
            }

            SetScenarioData(oldData, groupName, subspace);
        }
        
        /// <summary>
        /// Merges the given list of scenario modules and writes it to the group file, at the given subspace.
        /// </summary>
        public static void HandleConvertedScenarioModules(List<ScenarioDataTypes.ScenarioUpgradeableFacilities> inputList, string groupName, int subspace)
        {
            ScenarioDataTypes.ScenarioUpgradeableFacilities oldData = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ScenarioUpgradeableFacilities());
            oldData = GetOldScenarioData(oldData, groupName, subspace);

            foreach (ScenarioDataTypes.ScenarioUpgradeableFacilities suf in inputList)
            {
                if (oldData.header.Count == 0)
                {
                    if (suf.header.Count != 0)
                    {
                        oldData.header = suf.header;
                    }
                }

                if (!string.IsNullOrEmpty(suf.buildings.administration))
                {
                    if (!string.IsNullOrEmpty(oldData.buildings.administration))
                    {
                        if (Convert.ToSingle(suf.buildings.administration.Substring(6)) > Convert.ToSingle(oldData.buildings.administration.Substring(6)))
                        {
                            oldData.buildings.administration = suf.buildings.administration;
                        }
                    }
                    else
                    {
                        oldData.buildings.administration = suf.buildings.administration;
                    }
                }

                if (!string.IsNullOrEmpty(suf.buildings.astronautComplex))
                {
                    if (!string.IsNullOrEmpty(oldData.buildings.astronautComplex))
                    {
                        if (Convert.ToSingle(suf.buildings.astronautComplex.Substring(6)) > Convert.ToSingle(oldData.buildings.astronautComplex.Substring(6)))
                        {
                            oldData.buildings.astronautComplex = suf.buildings.astronautComplex;
                        }
                    }
                    else
                    {
                        oldData.buildings.astronautComplex = suf.buildings.astronautComplex;
                    }
                }

                if (!string.IsNullOrEmpty(suf.buildings.flagPole))
                {
                    if (!string.IsNullOrEmpty(oldData.buildings.flagPole))
                    {
                        if (Convert.ToSingle(suf.buildings.flagPole.Substring(6)) > Convert.ToSingle(oldData.buildings.flagPole.Substring(6)))
                        {
                            oldData.buildings.flagPole = suf.buildings.flagPole;
                        }
                    }
                    else
                    {
                        oldData.buildings.flagPole = suf.buildings.flagPole;
                    }
                }

                if (!string.IsNullOrEmpty(suf.buildings.launchPad))
                {
                    if (!string.IsNullOrEmpty(oldData.buildings.launchPad))
                    {
                        if (Convert.ToSingle(suf.buildings.launchPad.Substring(6)) > Convert.ToSingle(oldData.buildings.launchPad.Substring(6)))
                        {
                            oldData.buildings.launchPad = suf.buildings.launchPad;
                        }
                    }
                    else
                    {
                        oldData.buildings.launchPad = suf.buildings.launchPad;
                    }
                }

                if (!string.IsNullOrEmpty(suf.buildings.missionControl))
                {
                    if (!string.IsNullOrEmpty(oldData.buildings.missionControl))
                    {
                        if (Convert.ToSingle(suf.buildings.missionControl.Substring(6)) > Convert.ToSingle(oldData.buildings.missionControl.Substring(6)))
                        {
                            oldData.buildings.missionControl = suf.buildings.missionControl;
                        }
                    }
                    else
                    {
                        oldData.buildings.missionControl = suf.buildings.missionControl;
                    }
                }

                if (!string.IsNullOrEmpty(suf.buildings.researchAndDevelopment))
                {
                    if (!string.IsNullOrEmpty(oldData.buildings.researchAndDevelopment))
                    {
                        if (Convert.ToSingle(suf.buildings.researchAndDevelopment.Substring(6)) > Convert.ToSingle(oldData.buildings.researchAndDevelopment.Substring(6)))
                        {
                            oldData.buildings.researchAndDevelopment = suf.buildings.researchAndDevelopment;
                        }
                    }
                    else
                    {
                        oldData.buildings.researchAndDevelopment = suf.buildings.researchAndDevelopment;
                    }
                }

                if (!string.IsNullOrEmpty(suf.buildings.runway))
                {
                    if (!string.IsNullOrEmpty(oldData.buildings.runway))
                    {
                        if (Convert.ToSingle(suf.buildings.runway.Substring(6)) > Convert.ToSingle(oldData.buildings.runway.Substring(6)))
                        {
                            oldData.buildings.runway = suf.buildings.runway;
                        }
                    }
                    else
                    {
                        oldData.buildings.runway = suf.buildings.runway;
                    }
                }

                if (!string.IsNullOrEmpty(suf.buildings.spaceplaneHangar))
                {
                    if (!string.IsNullOrEmpty(oldData.buildings.spaceplaneHangar))
                    {
                        if (Convert.ToSingle(suf.buildings.spaceplaneHangar.Substring(6)) > Convert.ToSingle(oldData.buildings.spaceplaneHangar.Substring(6)))
                        {
                            oldData.buildings.spaceplaneHangar = suf.buildings.spaceplaneHangar;
                        }
                    }
                    else
                    {
                        oldData.buildings.spaceplaneHangar = suf.buildings.spaceplaneHangar;
                    }
                }

                if (!string.IsNullOrEmpty(suf.buildings.trackingStation))
                {
                    if (!string.IsNullOrEmpty(oldData.buildings.trackingStation))
                    {
                        if (Convert.ToSingle(suf.buildings.trackingStation.Substring(6)) > Convert.ToSingle(oldData.buildings.trackingStation.Substring(6)))
                        {
                            oldData.buildings.trackingStation = suf.buildings.trackingStation;
                        }
                    }
                    else
                    {
                        oldData.buildings.trackingStation = suf.buildings.trackingStation;
                    }
                }

                if (!string.IsNullOrEmpty(suf.buildings.vehicleAssemblyBuilding))
                {
                    if (!string.IsNullOrEmpty(oldData.buildings.vehicleAssemblyBuilding))
                    {
                        if (Convert.ToSingle(suf.buildings.vehicleAssemblyBuilding.Substring(6)) > Convert.ToSingle(oldData.buildings.vehicleAssemblyBuilding.Substring(6)))
                        {
                            oldData.buildings.vehicleAssemblyBuilding = suf.buildings.vehicleAssemblyBuilding;
                        }
                    }
                    else
                    {
                        oldData.buildings.vehicleAssemblyBuilding = suf.buildings.vehicleAssemblyBuilding;
                    }
                }
            }

            SetScenarioData(oldData, groupName, subspace);
        }
        
        /// <summary>
        /// Merges the given list of scenario modules and writes it to the group file, at the given subspace.
        /// </summary>
        public static void HandleConvertedScenarioModules(List<ScenarioDataTypes.StrategySystem> inputList, string groupName, int subspace)
        {
            ScenarioDataTypes.StrategySystem oldData = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.StrategySystem());
            oldData = GetOldScenarioData(oldData, groupName, subspace);
            List<string> listOfAlreadyAddedStrategies = new List<string>();

            for (int i = 0; i < oldData.strategies.Count; i++)
            {
                if (oldData.strategies[i].strategy.Count > 0)
                {
                    listOfAlreadyAddedStrategies.Add(oldData.strategies[i].strategy[0]);
                }
            }

            foreach (ScenarioDataTypes.StrategySystem ss in inputList)
            {
                if (oldData.header.Count == 0)
                {
                    if (ss.header.Count != 0)
                    {
                        oldData.header = ss.header;
                    }
                }

                for (int i = 0; i < ss.strategies.Count; i++)
                {
                    if (ss.strategies[i].strategy.Count > 0)
                    {
                        if (!listOfAlreadyAddedStrategies.Contains(ss.strategies[i].strategy[0]))
                        {
                            oldData.strategies.Add(ss.strategies[i]);

                            listOfAlreadyAddedStrategies.Add(ss.strategies[i].strategy[0]);
                        }
                    }
                }
            }

            SetScenarioData(oldData, groupName, subspace);
        }

        /// <summary>
        /// Returns the groups subspace scenario data of the given type.
        /// </summary>
        public static ScenarioDataTypes.ContractSystem GetOldScenarioData(ScenarioDataTypes.ContractSystem imputType, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "ContractSystem.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                imputType = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(FileHandler.ReadFromFile(filePath)), imputType);
            }

            return imputType;
        }

        /// <summary>
        /// Sets the groups subspace scenario data of the given data type.
        /// </summary>
        public static void SetScenarioData(ScenarioDataTypes.ContractSystem data, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "ContractSystem.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            byte[] convertedData = ByteArraySerializer.Serialize(ScenarioDataConverters.ConvertToStringList(data));

            FileHandler.WriteToFile(convertedData, filePath);
        }

        /// <summary>
        /// Returns the groups subspace scenario data of the given type.
        /// </summary>
        public static ScenarioDataTypes.Funding GetOldScenarioData(ScenarioDataTypes.Funding imputType, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "Funding.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                imputType = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(FileHandler.ReadFromFile(filePath)), imputType);
            }

            return imputType;
        }

        /// <summary>
        /// Sets the groups subspace scenario data of the given data type.
        /// </summary>
        public static void SetScenarioData(ScenarioDataTypes.Funding data, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "Funding.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            byte[] convertedData = ByteArraySerializer.Serialize(ScenarioDataConverters.ConvertToStringList(data));

            FileHandler.WriteToFile(convertedData, filePath);
        }

        /// <summary>
        /// Returns the groups subspace scenario data of the given type.
        /// </summary>
        public static ScenarioDataTypes.PartUpgradeManager GetOldScenarioData(ScenarioDataTypes.PartUpgradeManager imputType, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "PartUpgradeManager.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                imputType = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(FileHandler.ReadFromFile(filePath)), imputType);
            }

            return imputType;
        }

        /// <summary>
        /// Sets the groups subspace scenario data of the given data type.
        /// </summary>
        public static void SetScenarioData(ScenarioDataTypes.PartUpgradeManager data, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "PartUpgradeManager.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            byte[] convertedData = ByteArraySerializer.Serialize(ScenarioDataConverters.ConvertToStringList(data));

            FileHandler.WriteToFile(convertedData, filePath);
        }

        /// <summary>
        /// Returns the groups subspace scenario data of the given type.
        /// </summary>
        public static ScenarioDataTypes.ProgressTracking GetOldScenarioData(ScenarioDataTypes.ProgressTracking imputType, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "ProgressTracking.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                imputType = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(FileHandler.ReadFromFile(filePath)), imputType);
            }

            return imputType;
        }

        /// <summary>
        /// Sets the groups subspace scenario data of the given data type.
        /// </summary>
        public static void SetScenarioData(ScenarioDataTypes.ProgressTracking data, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "ProgressTracking.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            byte[] convertedData = ByteArraySerializer.Serialize(ScenarioDataConverters.ConvertToStringList(data));

            FileHandler.WriteToFile(convertedData, filePath);
        }

        /// <summary>
        /// Returns the groups subspace scenario data of the given type.
        /// </summary>
        public static ScenarioDataTypes.Reputation GetOldScenarioData(ScenarioDataTypes.Reputation imputType, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "Reputation.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                imputType = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(FileHandler.ReadFromFile(filePath)), imputType);
            }

            return imputType;
        }

        /// <summary>
        /// Sets the groups subspace scenario data of the given data type.
        /// </summary>
        public static void SetScenarioData(ScenarioDataTypes.Reputation data, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "Reputation.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            byte[] convertedData = ByteArraySerializer.Serialize(ScenarioDataConverters.ConvertToStringList(data));

            FileHandler.WriteToFile(convertedData, filePath);
        }

        /// <summary>
        /// Returns the groups subspace scenario data of the given type.
        /// </summary>
        public static ScenarioDataTypes.ResearchAndDevelopment GetOldScenarioData(ScenarioDataTypes.ResearchAndDevelopment imputType, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "ResearchAndDevelopment.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                imputType = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(FileHandler.ReadFromFile(filePath)), imputType);
            }

            return imputType;
        }

        /// <summary>
        /// Sets the groups subspace scenario data of the given data type.
        /// </summary>
        public static void SetScenarioData(ScenarioDataTypes.ResearchAndDevelopment data, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "ResearchAndDevelopment.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            byte[] convertedData = ByteArraySerializer.Serialize(ScenarioDataConverters.ConvertToStringList(data));

            FileHandler.WriteToFile(convertedData, filePath);
        }

        /// <summary>
        /// Returns the groups subspace scenario data of the given type.
        /// </summary>
        public static ScenarioDataTypes.ResourceScenario GetOldScenarioData(ScenarioDataTypes.ResourceScenario imputType, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "ResourceScenario.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                imputType = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(FileHandler.ReadFromFile(filePath)), imputType);
            }

            return imputType;
        }

        /// <summary>
        /// Sets the groups subspace scenario data of the given data type.
        /// </summary>
        public static void SetScenarioData(ScenarioDataTypes.ResourceScenario data, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "ResourceScenario.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            byte[] convertedData = ByteArraySerializer.Serialize(ScenarioDataConverters.ConvertToStringList(data));

            FileHandler.WriteToFile(convertedData, filePath);
        }

        /// <summary>
        /// Returns the groups subspace scenario data of the given type.
        /// </summary>
        public static ScenarioDataTypes.ScenarioCustomWaypoints GetOldScenarioData(ScenarioDataTypes.ScenarioCustomWaypoints imputType, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "ScenarioCustomWaypoints.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                imputType = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(FileHandler.ReadFromFile(filePath)), imputType);
            }

            return imputType;
        }

        /// <summary>
        /// Sets the groups subspace scenario data of the given data type.
        /// </summary>
        public static void SetScenarioData(ScenarioDataTypes.ScenarioCustomWaypoints data, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "ScenarioCustomWaypoints.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            byte[] convertedData = ByteArraySerializer.Serialize(ScenarioDataConverters.ConvertToStringList(data));

            FileHandler.WriteToFile(convertedData, filePath);
        }

        /// <summary>
        /// Returns the groups subspace scenario data of the given type.
        /// </summary>
        public static ScenarioDataTypes.ScenarioDestructibles GetOldScenarioData(ScenarioDataTypes.ScenarioDestructibles imputType, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "ScenarioDestructibles.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                imputType = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(FileHandler.ReadFromFile(filePath)), imputType);
            }

            return imputType;
        }

        /// <summary>
        /// Sets the groups subspace scenario data of the given data type.
        /// </summary>
        public static void SetScenarioData(ScenarioDataTypes.ScenarioDestructibles data, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "ScenarioDestructibles.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            byte[] convertedData = ByteArraySerializer.Serialize(ScenarioDataConverters.ConvertToStringList(data));

            FileHandler.WriteToFile(convertedData, filePath);
        }

        /// <summary>
        /// Returns the groups subspace scenario data of the given type.
        /// </summary>
        public static ScenarioDataTypes.ScenarioUpgradeableFacilities GetOldScenarioData(ScenarioDataTypes.ScenarioUpgradeableFacilities imputType, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "ScenarioUpgradeableFacilities.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                imputType = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(FileHandler.ReadFromFile(filePath)), imputType);
            }

            return imputType;
        }

        /// <summary>
        /// Sets the groups subspace scenario data of the given data type.
        /// </summary>
        public static void SetScenarioData(ScenarioDataTypes.ScenarioUpgradeableFacilities data, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "ScenarioUpgradeableFacilities.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            byte[] convertedData = ByteArraySerializer.Serialize(ScenarioDataConverters.ConvertToStringList(data));

            FileHandler.WriteToFile(convertedData, filePath);
        }

        /// <summary>
        /// Returns the groups subspace scenario data of the given type.
        /// </summary>
        public static ScenarioDataTypes.StrategySystem GetOldScenarioData(ScenarioDataTypes.StrategySystem imputType, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "StrategySystem.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            if (File.Exists(filePath))
            {
                imputType = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(FileHandler.ReadFromFile(filePath)), imputType);
            }

            return imputType;
        }

        /// <summary>
        /// Sets the groups subspace scenario data of the given data type.
        /// </summary>
        public static void SetScenarioData(ScenarioDataTypes.StrategySystem data, string groupName, int subspace)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string scenarioFolder = Path.Combine(groupFolder, "Subspace" + subspace, "Scenario");
            string filePath = Path.Combine(scenarioFolder, "StrategySystem.txt");

            if (!Directory.Exists(scenarioFolder))
            {
                Directory.CreateDirectory(scenarioFolder);
            }

            byte[] convertedData = ByteArraySerializer.Serialize(ScenarioDataConverters.ConvertToStringList(data));

            FileHandler.WriteToFile(convertedData, filePath);
        }

        public static KeyValuePair<int, int> GetKeys(int subSpace, string groupName)
        {
            List<GroupSubspaces> threadSafeList = AllGroupSubspaces;
            int subSpaceKey = threadSafeList.FindIndex(s => s.SubspaceNumber == subSpace);

            int groupKey = -1;
            if (subSpaceKey != -1)
            {
                groupKey = threadSafeList[subSpaceKey].GroupNames.FindIndex(s => s == groupName);
                if (groupKey == -1)
                {
                    AllGroupSubspaces[subSpaceKey].GroupNames.Add(groupName);
                    groupKey = AllGroupSubspaces[subSpaceKey].GroupNames.Count - 1;
                }
            }
            else
            {
                AllGroupSubspaces.Add(new GroupSubspaces());
                subSpaceKey = AllGroupSubspaces.Count - 1;
                AllGroupSubspaces[subSpaceKey].GroupNames.Add(groupName);
                groupKey = AllGroupSubspaces[subSpaceKey].GroupNames.Count - 1;
            }

            return new KeyValuePair<int, int>(subSpaceKey, groupKey);
        }

        public static void MergeEmptySubSpaceWithNewestOne(int emptySubspaceNumber)
        {
            int subSpace = Messages.WarpControl.GetLatestSubspace();

            if (subSpace != emptySubspaceNumber)
            {
                List<string> completedGroups = new List<string>();

                ScenarioSystem.SubspacesList subspaceListCopy = ScenarioSystem.CopySubspacesList(ScenarioSystem.subspaceList);

                foreach (ScenarioSystem.Subspace subSpaceFromList in subspaceListCopy.Subspaces)
                {
                    foreach (ScenarioSystem.Group group in subSpaceFromList.Groups)
                    {
                        string groupName = group.GroupName;

                        if (completedGroups.Contains(groupName))
                        {
                            continue;
                        }
                        else
                        {
                            completedGroups.Add(groupName);
                        }

                        bool addAllFiles = false;

                        string currentGroupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
                        string oldSubspaceFolder = Path.Combine(currentGroupFolder, "Subspace" + emptySubspaceNumber);

                        if (!Directory.Exists(oldSubspaceFolder))
                        {
                            SyncrioLog.Debug("Can not find the listed group's folder!");
                            continue;
                        }

                        if (!Directory.Exists(Path.Combine(oldSubspaceFolder, "Scenario")))
                        {
                            SyncrioLog.Debug("Can not find the listed group's scenario folder!");
                            continue;
                        }

                        string newSubspaceFolder = Path.Combine(currentGroupFolder, "Subspace" + subSpace);

                        if (!Directory.Exists(newSubspaceFolder))
                        {
                            Directory.CreateDirectory(newSubspaceFolder);
                            addAllFiles = true;
                        }

                        if (!Directory.Exists(Path.Combine(newSubspaceFolder, "Scenario")))
                        {
                            Directory.CreateDirectory(Path.Combine(newSubspaceFolder, "Scenario"));
                            addAllFiles = true;
                        }

                        if (!addAllFiles)
                        {
                            foreach (string file in Directory.GetFiles(Path.Combine(oldSubspaceFolder, "Scenario")))
                            {
                                string currentScenarioModuleType = Path.GetFileNameWithoutExtension(file);

                                byte[] scenarioData1ByteArray = FileHandler.ReadFromFile(file);

                                string newFilePath = Path.Combine(newSubspaceFolder, "Scenario", currentScenarioModuleType + ".txt");
                                if (File.Exists(newFilePath))
                                {
                                    byte[] scenarioData2ByteArray = FileHandler.ReadFromFile(newFilePath);

                                    if (currentScenarioModuleType == "ContractSystem")
                                    {
                                        ScenarioDataTypes.ContractSystem newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ContractSystem());
                                        newScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData2ByteArray), newScenarioModule);

                                        ScenarioDataTypes.ContractSystem oldScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ContractSystem());
                                        oldScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), oldScenarioModule);

                                        MergeConvertedScenarioModule(newScenarioModule, oldScenarioModule, groupName, subSpace);
                                    }

                                    if (currentScenarioModuleType == "Funding")
                                    {
                                        ScenarioDataTypes.Funding newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.Funding());
                                        newScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData2ByteArray), newScenarioModule);

                                        ScenarioDataTypes.Funding oldScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.Funding());
                                        oldScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), oldScenarioModule);

                                        MergeConvertedScenarioModule(newScenarioModule, oldScenarioModule, groupName, subSpace);
                                    }

                                    if (currentScenarioModuleType == "PartUpgradeManager")
                                    {
                                        ScenarioDataTypes.PartUpgradeManager newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.PartUpgradeManager());
                                        newScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData2ByteArray), newScenarioModule);

                                        ScenarioDataTypes.PartUpgradeManager oldScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.PartUpgradeManager());
                                        oldScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), oldScenarioModule);

                                        MergeConvertedScenarioModule(newScenarioModule, oldScenarioModule, groupName, subSpace);
                                    }

                                    if (currentScenarioModuleType == "ProgressTracking")
                                    {
                                        ScenarioDataTypes.ProgressTracking newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ProgressTracking());
                                        newScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData2ByteArray), newScenarioModule);

                                        ScenarioDataTypes.ProgressTracking oldScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ProgressTracking());
                                        oldScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), oldScenarioModule);

                                        MergeConvertedScenarioModule(newScenarioModule, oldScenarioModule, groupName, subSpace);
                                    }

                                    if (currentScenarioModuleType == "Reputation")
                                    {
                                        ScenarioDataTypes.Reputation newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.Reputation());
                                        newScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData2ByteArray), newScenarioModule);

                                        ScenarioDataTypes.Reputation oldScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.Reputation());
                                        oldScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), oldScenarioModule);

                                        MergeConvertedScenarioModule(newScenarioModule, oldScenarioModule, groupName, subSpace);
                                    }

                                    if (currentScenarioModuleType == "ResearchAndDevelopment")
                                    {
                                        ScenarioDataTypes.ResearchAndDevelopment newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ResearchAndDevelopment());
                                        newScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData2ByteArray), newScenarioModule);

                                        ScenarioDataTypes.ResearchAndDevelopment oldScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ResearchAndDevelopment());
                                        oldScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), oldScenarioModule);

                                        MergeConvertedScenarioModule(newScenarioModule, oldScenarioModule, groupName, subSpace);
                                    }

                                    if (currentScenarioModuleType == "ResourceScenario")
                                    {
                                        ScenarioDataTypes.ResourceScenario newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ResourceScenario());
                                        newScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData2ByteArray), newScenarioModule);

                                        ScenarioDataTypes.ResourceScenario oldScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ResourceScenario());
                                        oldScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), oldScenarioModule);

                                        MergeConvertedScenarioModule(newScenarioModule, oldScenarioModule, groupName, subSpace);
                                    }

                                    if (currentScenarioModuleType == "ScenarioCustomWaypoints")
                                    {
                                        ScenarioDataTypes.ScenarioCustomWaypoints newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ScenarioCustomWaypoints());
                                        newScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData2ByteArray), newScenarioModule);

                                        ScenarioDataTypes.ScenarioCustomWaypoints oldScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ScenarioCustomWaypoints());
                                        oldScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), oldScenarioModule);

                                        MergeConvertedScenarioModule(newScenarioModule, oldScenarioModule, groupName, subSpace);
                                    }

                                    if (currentScenarioModuleType == "ScenarioDestructibles")
                                    {
                                        ScenarioDataTypes.ScenarioDestructibles newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ScenarioDestructibles());
                                        newScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData2ByteArray), newScenarioModule);

                                        ScenarioDataTypes.ScenarioDestructibles oldScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ScenarioDestructibles());
                                        oldScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), oldScenarioModule);

                                        MergeConvertedScenarioModule(newScenarioModule, oldScenarioModule, groupName, subSpace);
                                    }

                                    if (currentScenarioModuleType == "ScenarioUpgradeableFacilities")
                                    {
                                        ScenarioDataTypes.ScenarioUpgradeableFacilities newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ScenarioUpgradeableFacilities());
                                        newScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData2ByteArray), newScenarioModule);

                                        ScenarioDataTypes.ScenarioUpgradeableFacilities oldScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ScenarioUpgradeableFacilities());
                                        oldScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), oldScenarioModule);

                                        MergeConvertedScenarioModule(newScenarioModule, oldScenarioModule, groupName, subSpace);
                                    }

                                    if (currentScenarioModuleType == "StrategySystem")
                                    {
                                        ScenarioDataTypes.StrategySystem newScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.StrategySystem());
                                        newScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData2ByteArray), newScenarioModule);

                                        ScenarioDataTypes.StrategySystem oldScenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.StrategySystem());
                                        oldScenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), oldScenarioModule);

                                        MergeConvertedScenarioModule(newScenarioModule, oldScenarioModule, groupName, subSpace);
                                    }
                                }
                                else
                                {
                                    if (currentScenarioModuleType == "ContractSystem")
                                    {
                                        ScenarioDataTypes.ContractSystem scenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ContractSystem());
                                        scenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), scenarioModule);

                                        SetScenarioData(scenarioModule, groupName, subSpace);
                                    }

                                    if (currentScenarioModuleType == "Funding")
                                    {
                                        ScenarioDataTypes.Funding scenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.Funding());
                                        scenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), scenarioModule);

                                        SetScenarioData(scenarioModule, groupName, subSpace);
                                    }

                                    if (currentScenarioModuleType == "PartUpgradeManager")
                                    {
                                        ScenarioDataTypes.PartUpgradeManager scenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.PartUpgradeManager());
                                        scenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), scenarioModule);

                                        SetScenarioData(scenarioModule, groupName, subSpace);
                                    }

                                    if (currentScenarioModuleType == "ProgressTracking")
                                    {
                                        ScenarioDataTypes.ProgressTracking scenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ProgressTracking());
                                        scenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), scenarioModule);

                                        SetScenarioData(scenarioModule, groupName, subSpace);
                                    }

                                    if (currentScenarioModuleType == "Reputation")
                                    {
                                        ScenarioDataTypes.Reputation scenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.Reputation());
                                        scenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), scenarioModule);

                                        SetScenarioData(scenarioModule, groupName, subSpace);
                                    }

                                    if (currentScenarioModuleType == "ResearchAndDevelopment")
                                    {
                                        ScenarioDataTypes.ResearchAndDevelopment scenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ResearchAndDevelopment());
                                        scenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), scenarioModule);

                                        SetScenarioData(scenarioModule, groupName, subSpace);
                                    }

                                    if (currentScenarioModuleType == "ResourceScenario")
                                    {
                                        ScenarioDataTypes.ResourceScenario scenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ResourceScenario());
                                        scenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), scenarioModule);

                                        SetScenarioData(scenarioModule, groupName, subSpace);
                                    }

                                    if (currentScenarioModuleType == "ScenarioCustomWaypoints")
                                    {
                                        ScenarioDataTypes.ScenarioCustomWaypoints scenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ScenarioCustomWaypoints());
                                        scenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), scenarioModule);

                                        SetScenarioData(scenarioModule, groupName, subSpace);
                                    }

                                    if (currentScenarioModuleType == "ScenarioDestructibles")
                                    {
                                        ScenarioDataTypes.ScenarioDestructibles scenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ScenarioDestructibles());
                                        scenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), scenarioModule);

                                        SetScenarioData(scenarioModule, groupName, subSpace);
                                    }

                                    if (currentScenarioModuleType == "ScenarioUpgradeableFacilities")
                                    {
                                        ScenarioDataTypes.ScenarioUpgradeableFacilities scenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.ScenarioUpgradeableFacilities());
                                        scenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), scenarioModule);

                                        SetScenarioData(scenarioModule, groupName, subSpace);
                                    }

                                    if (currentScenarioModuleType == "StrategySystem")
                                    {
                                        ScenarioDataTypes.StrategySystem scenarioModule = ScenarioDataConstructor.ConstructData(new ScenarioDataTypes.StrategySystem());
                                        scenarioModule = ScenarioDataConverters.ConvertToScenarioDataType(ByteArraySerializer.Deserialize(scenarioData1ByteArray), scenarioModule);

                                        SetScenarioData(scenarioModule, groupName, subSpace);
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (string file in Directory.GetFiles(Path.Combine(oldSubspaceFolder, "Scenario")))
                            {
                                File.Copy(file, Path.Combine(Path.Combine(newSubspaceFolder, "Scenario"), Path.GetFileName(file)));
                            }
                        }
                        RemoveEmptySubspaceFolders(emptySubspaceNumber, groupName);
                    }
                }
            }
        }

        public static void RemoveEmptySubspaceFolders(int emptySubspaceNumber, string groupName)
        {
            string groupFolder = Path.Combine(ScenarioSystem.fetch.groupScenariosDirectory, groupName);
            string subspaceFolder = Path.Combine(groupFolder, "Subspace" + emptySubspaceNumber);

            if (Directory.Exists(subspaceFolder))
            {
                FileHandler.DeleteDirectory(subspaceFolder);
            }
        }

        /// <summary>
        /// Merges two converted scenario modules and writes the merged scenario module to the specified subspace group file.
        /// </summary>
        public static void MergeConvertedScenarioModule(ScenarioDataTypes.ContractSystem newSM, ScenarioDataTypes.ContractSystem oldSM, string groupName, int subSpace)
        {
            Regex wordRegex = new Regex(@"^[\w_]+", RegexOptions.None);// matches a single word
            
            ScenarioDataTypes.ContractSystem mergedSM = oldSM;
            Dictionary<string, int> weightDifferences = new Dictionary<string, int>();
            List<string> mergedWeights = new List<string>();

            if (oldSM.header.Count == 0)
            {
                if (newSM.header.Count != 0)
                {
                    mergedSM.header = newSM.header;
                }
            }
            else
            {
                if (newSM.header.Count != 0)
                {
                    if (newSM.header[2].Substring(0, 6) == "update")
                    {
                        double newContractSystemHeader = Convert.ToDouble(newSM.header[2].Substring(9));

                        if (oldSM.header[2].Substring(0, 6) == "update")
                        {
                            double oldContractSystemHeader = Convert.ToDouble(oldSM.header[2].Substring(9));

                            if (newContractSystemHeader > oldContractSystemHeader)
                            {
                                mergedSM.header = newSM.header;
                            }
                        }
                        else
                        {
                            mergedSM.header = newSM.header;
                        }
                    }
                }
            }

            //Weight Handling
            if (oldSM.weights.Count > 0)
            {
                if (newSM.weights.Count > 0)
                {
                    for (int cursor = 0; cursor < oldSM.weights.Count; cursor++)
                    {
                        if (cursor < newSM.weights.Count)
                        {
                            string oldWeightName = wordRegex.Match(oldSM.weights[cursor]).ToString();
                            KeyValuePair<string, int> oldWeightData = new KeyValuePair<string, int>(oldWeightName, Convert.ToInt32(oldSM.weights[cursor].Substring(oldWeightName.Length + 3)));

                            string newWeightName = wordRegex.Match(newSM.weights[cursor]).ToString();
                            KeyValuePair<string, int> newWeightData = new KeyValuePair<string, int>(newWeightName, Convert.ToInt32(newSM.weights[cursor].Substring(newWeightName.Length + 3)));

                            if (newWeightData.Key == oldWeightData.Key)
                            {
                                if (newWeightData.Value != oldWeightData.Value)
                                {
                                    if (weightDifferences.ContainsKey(oldWeightData.Key))
                                    {
                                        weightDifferences[oldWeightData.Key] += newWeightData.Value - oldWeightData.Value;
                                    }
                                    else
                                    {
                                        weightDifferences.Add(oldWeightData.Key, newWeightData.Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (newSM.weights.Count > 0)
                {
                    mergedSM.weights = newSM.weights;
                }
            }
            //Contract Handling
            List<ScenarioDataTypes.Contract> contractList = new List<ScenarioDataTypes.Contract>();

            if (oldSM.contracts.Count > 0)
            {
                if (newSM.contracts.Count > 0)
                {
                    for (int cursor = 0; cursor < oldSM.contracts.Count; cursor++)
                    {
                        if (cursor < newSM.contracts.Count)
                        {
                            ScenarioDataTypes.Contract oldContract = oldSM.contracts[cursor];

                            ScenarioDataTypes.Contract newContract = newSM.contracts[cursor];

                            if (newContract.guid == oldContract.guid)
                            {
                                if (oldContract.contractDataLines.Any(i => i.StartsWith("state")))
                                {
                                    string oldStateLine = oldContract.contractDataLines.FirstOrDefault(i => i.StartsWith("state"));

                                    if (newContract.contractDataLines.Any(i => i.StartsWith("state")))
                                    {
                                        string newStateLine = newContract.contractDataLines.FirstOrDefault(i => i.StartsWith("state"));

                                        if (newStateLine != oldStateLine)
                                        {
                                            if (newStateLine == "state = Active")
                                            {
                                                contractList.Add(newContract);
                                            }
                                            else
                                            {
                                                if (oldStateLine == "state = Active")
                                                {
                                                    contractList.Add(oldContract);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (newStateLine == "state = Active")
                                            {
                                                ScenarioDataTypes.Contract editedContract = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Contract());
                                                List<ScenarioDataTypes.Param> editedContractParams = new List<ScenarioDataTypes.Param>();

                                                editedContract.guid = newContract.guid;
                                                editedContract.contractDataLines = newContract.contractDataLines;
                                                editedContract.usedNodeNumbers = newContract.usedNodeNumbers;

                                                for (int i = 0; i < newContract.parameters.Count; i++)
                                                {
                                                    string newParamStateLine = newContract.parameters[i].paramLines.FirstOrDefault(v => v.StartsWith("state"));

                                                    if (newParamStateLine == "state = Complete" || newParamStateLine == "state = Failed")
                                                    {
                                                        editedContractParams.Add(newContract.parameters[i]);
                                                    }
                                                    else
                                                    {
                                                        for (int v = 0; v < newContract.parameters[i].subParameters.Count; v++)
                                                        {
                                                            ScenarioDataTypes.Param editedContractParameter = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Param());

                                                            editedContractParameter.nodeNumber = newContract.parameters[i].nodeNumber;
                                                            editedContractParameter.paramLines = newContract.parameters[i].paramLines;

                                                            editedContractParameter.subParameters.Add(newContract.parameters[i].subParameters[v]);

                                                            editedContractParams.Add(editedContractParameter);
                                                        }
                                                    }
                                                }

                                                editedContract.parameters.AddRange(editedContractParams);

                                                contractList.Add(editedContract);
                                            }
                                            else
                                            {
                                                contractList.Add(oldContract);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        contractList.Add(oldContract);
                                    }
                                }
                                else
                                {
                                    if (newContract.contractDataLines.Any(i => i.StartsWith("state")))
                                    {
                                        contractList.Add(newContract);
                                    }
                                }
                            }
                            else
                            {
                                List<ScenarioDataTypes.Contract> newFinishedContracts = new List<ScenarioDataTypes.Contract>();
                                if (newSM.finishedContracts.Count > 0)
                                {
                                    newFinishedContracts = newSM.finishedContracts.Where(i => i.guid == oldContract.guid).ToList();
                                }

                                if (newFinishedContracts.Count == 0)
                                {
                                    int cursorTwo = -1;
                                    while (newContract.guid != oldContract.guid)
                                    {
                                        if (cursorTwo + 1 < newSM.contracts.Count)
                                        {
                                            cursorTwo++;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                        newContract = newSM.contracts[cursorTwo];
                                    }

                                    if (newContract.guid == oldContract.guid)
                                    {
                                        if (oldContract.contractDataLines.Any(i => i.StartsWith("state")))
                                        {
                                            string oldStateLine = oldContract.contractDataLines.FirstOrDefault(i => i.StartsWith("state"));

                                            if (newContract.contractDataLines.Any(i => i.StartsWith("state")))
                                            {
                                                string newStateLine = newContract.contractDataLines.FirstOrDefault(i => i.StartsWith("state"));

                                                if (newStateLine != oldStateLine)
                                                {
                                                    if (newStateLine == "state = Active")
                                                    {
                                                        contractList.Add(newContract);
                                                    }
                                                    else
                                                    {
                                                        if (oldStateLine == "state = Active")
                                                        {
                                                            contractList.Add(oldContract);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (newStateLine == "state = Active")
                                                    {
                                                        ScenarioDataTypes.Contract editedContract = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Contract());
                                                        List<ScenarioDataTypes.Param> editedContractParams = new List<ScenarioDataTypes.Param>();

                                                        editedContract.guid = newContract.guid;
                                                        editedContract.contractDataLines = newContract.contractDataLines;
                                                        editedContract.usedNodeNumbers = newContract.usedNodeNumbers;

                                                        for (int i = 0; i < newContract.parameters.Count; i++)
                                                        {
                                                            string newParamStateLine = newContract.parameters[i].paramLines.FirstOrDefault(v => v.StartsWith("state"));

                                                            if (newParamStateLine == "state = Complete" || newParamStateLine == "state = Failed")
                                                            {
                                                                editedContractParams.Add(newContract.parameters[i]);
                                                            }
                                                            else
                                                            {
                                                                for (int v = 0; v < newContract.parameters[i].subParameters.Count; v++)
                                                                {
                                                                    ScenarioDataTypes.Param editedContractParameter = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Param());

                                                                    editedContractParameter.nodeNumber = newContract.parameters[i].nodeNumber;
                                                                    editedContractParameter.paramLines = newContract.parameters[i].paramLines;

                                                                    string newSubParamStateLine = newContract.parameters[i].paramLines.FirstOrDefault(x => x.StartsWith("state"));

                                                                    if (newSubParamStateLine == "state = Complete" || newSubParamStateLine == "state = Failed")
                                                                    {
                                                                        editedContractParameter.subParameters.Add(newContract.parameters[i].subParameters[v]);
                                                                    }
                                                                    else
                                                                    {
                                                                        editedContractParameter.subParameters.Add(oldContract.parameters[i].subParameters[v]);
                                                                    }

                                                                    editedContractParams.Add(editedContractParameter);
                                                                }
                                                            }
                                                        }

                                                        editedContract.parameters.AddRange(editedContractParams);

                                                        contractList.Add(editedContract);
                                                    }
                                                    else
                                                    {
                                                        contractList.Add(oldContract);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                contractList.Add(oldContract);
                                            }
                                        }
                                        else
                                        {
                                            if (newContract.contractDataLines.Any(i => i.StartsWith("state")))
                                            {
                                                contractList.Add(newContract);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        contractList.Add(oldContract);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (newSM.contracts.Count > 0)
                {
                    contractList = newSM.contracts;
                }
            }
            //Finished Contract Handling
            List<ScenarioDataTypes.Contract> finishedContractList = new List<ScenarioDataTypes.Contract>();

            if (oldSM.finishedContracts.Count > 0)
            {
                if (newSM.finishedContracts.Count > 0)
                {
                    for (int cursor = 0; cursor < oldSM.finishedContracts.Count; cursor++)
                    {
                        if (cursor < newSM.finishedContracts.Count)
                        {
                            ScenarioDataTypes.Contract oldContract = oldSM.finishedContracts[cursor];

                            ScenarioDataTypes.Contract newContract = newSM.finishedContracts[cursor];

                            if (newContract.guid == oldContract.guid)
                            {
                                finishedContractList.Add(newContract);
                            }
                            else
                            {
                                int cursorTwo = -1;
                                while (newContract.guid != oldContract.guid)
                                {
                                    if (cursorTwo + 1 < newSM.contracts.Count)
                                    {
                                        cursorTwo++;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                    newContract = newSM.contracts[cursorTwo];
                                }

                                if (newContract.guid != oldContract.guid)
                                {
                                    finishedContractList.Add(newContract);
                                }
                                else
                                {
                                    finishedContractList.Add(oldContract);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (newSM.finishedContracts.Count > 0)
                {
                    finishedContractList = newSM.finishedContracts;
                }
            }

            mergedSM.contracts = contractList;
            mergedSM.finishedContracts = finishedContractList;

            foreach (KeyValuePair<string, int> kvp in weightDifferences)
            {
                mergedWeights.Add(kvp.Key + " = " + kvp.Value);
            }

            mergedSM.weights = mergedWeights;

            SetScenarioData(mergedSM, groupName, subSpace);
        }

        /// <summary>
        /// Merges two converted scenario modules and writes the merged scenario module to the specified subspace group file.
        /// </summary>
        public static void MergeConvertedScenarioModule(ScenarioDataTypes.Funding newSM, ScenarioDataTypes.Funding oldSM, string groupName, int subSpace)
        {
            ScenarioDataTypes.Funding mergedSM = oldSM;

            double oldFunds = 0;

            if (!string.IsNullOrEmpty(oldSM.fundsLine))
            {
                oldFunds = Convert.ToDouble(oldSM.fundsLine.Substring(8));
            }

            double endFunds = oldFunds;
            
            if (oldSM.header.Count == 0)
            {
                if (newSM.header.Count != 0)
                {
                    mergedSM.header = newSM.header;
                }
            }

            double newFunds = 0;

            if (newSM.fundsLine.Length > 0)
            {
                newFunds = Convert.ToDouble(newSM.fundsLine.Substring(8));
            }

            if (newFunds != oldFunds)
            {
                endFunds += newFunds - oldFunds;
            }

            mergedSM.fundsLine = "funds = " + Convert.ToString(endFunds);

            SetScenarioData(mergedSM, groupName, subSpace);
        }

        /// <summary>
        /// Merges two converted scenario modules and writes the merged scenario module to the specified subspace group file.
        /// </summary>
        public static void MergeConvertedScenarioModule(ScenarioDataTypes.PartUpgradeManager newSM, ScenarioDataTypes.PartUpgradeManager oldSM, string groupName, int subSpace)
        {
            ScenarioDataTypes.PartUpgradeManager mergedSM = oldSM;

            //This is only temporary; Until I get better data on this scenario type.-----------------------------------------

            SetScenarioData(mergedSM, groupName, subSpace);
        }

        /// <summary>
        /// Merges two converted scenario modules and writes the merged scenario module to the specified subspace group file.
        /// </summary>
        public static void MergeConvertedScenarioModule(ScenarioDataTypes.ProgressTracking newSM, ScenarioDataTypes.ProgressTracking oldSM, string groupName, int subSpace)
        {
            ScenarioDataTypes.ProgressTracking mergedSM = oldSM;
            Dictionary<string, int> celestialBodyList = new Dictionary<string, int>();

            for (int i = 0; i < oldSM.celestialProgress.Count; i++)
            {
                if (!celestialBodyList.ContainsKey(oldSM.celestialProgress[i].celestialBody))
                {
                    celestialBodyList.Add(oldSM.celestialProgress[i].celestialBody, i);
                }
            }

            if (oldSM.header.Count == 0)
            {
                if (newSM.header.Count != 0)
                {
                    mergedSM.header = newSM.header;
                }
            }

            ScenarioDataTypes.ProgressTracking editingProgress = mergedSM;

            //Basic Progress
            if (editingProgress.basicProgress.altitudeRecord.Count == 0)
            {
                editingProgress.basicProgress.altitudeRecord = newSM.basicProgress.altitudeRecord;
            }

            if (editingProgress.basicProgress.depthRecord.Count == 0)
            {
                editingProgress.basicProgress.depthRecord = newSM.basicProgress.depthRecord;
            }

            if (editingProgress.basicProgress.distanceRecord.Count == 0)
            {
                editingProgress.basicProgress.distanceRecord = newSM.basicProgress.distanceRecord;
            }

            if (editingProgress.basicProgress.firstCrewToSurvive.Count == 0)
            {
                editingProgress.basicProgress.firstCrewToSurvive = newSM.basicProgress.firstCrewToSurvive;
            }

            if (editingProgress.basicProgress.firstLaunch.Count == 0)
            {
                editingProgress.basicProgress.firstLaunch = newSM.basicProgress.firstLaunch;
            }

            if (editingProgress.basicProgress.KSCLanding.Count == 0)
            {
                editingProgress.basicProgress.KSCLanding = newSM.basicProgress.KSCLanding;
            }

            if (editingProgress.basicProgress.launchpadLanding.Count == 0)
            {
                editingProgress.basicProgress.launchpadLanding = newSM.basicProgress.launchpadLanding;
            }

            if (editingProgress.basicProgress.reachSpace.Count == 0)
            {
                editingProgress.basicProgress.reachSpace = newSM.basicProgress.reachSpace;
            }

            if (editingProgress.basicProgress.runwayLanding.Count == 0)
            {
                editingProgress.basicProgress.runwayLanding = newSM.basicProgress.runwayLanding;
            }

            if (editingProgress.basicProgress.speedRecord.Count == 0)
            {
                editingProgress.basicProgress.speedRecord = newSM.basicProgress.speedRecord;
            }

            if (editingProgress.basicProgress.towerBuzz.Count == 0)
            {
                editingProgress.basicProgress.towerBuzz = newSM.basicProgress.towerBuzz;
            }

            //Celestial Progress
            for (int i = 0; i < newSM.celestialProgress.Count; i++)
            {
                if (celestialBodyList.ContainsKey(newSM.celestialProgress[i].celestialBody))
                {
                    int key = -1;

                    celestialBodyList.TryGetValue(newSM.celestialProgress[i].celestialBody, out key);

                    if (key != -1)
                    {
                        ScenarioDataTypes.CelestialProgress editingCelestialProgress = editingProgress.celestialProgress[key];

                        if (editingCelestialProgress.baseConstruction.Count == 0)
                        {
                            editingCelestialProgress.baseConstruction = newSM.celestialProgress[i].baseConstruction;
                        }

                        if (editingCelestialProgress.crewTransfer.Count == 0)
                        {
                            editingCelestialProgress.crewTransfer = newSM.celestialProgress[i].crewTransfer;
                        }

                        if (editingCelestialProgress.docking.Count == 0)
                        {
                            editingCelestialProgress.docking = newSM.celestialProgress[i].docking;
                        }

                        if (editingCelestialProgress.escape.Count == 0)
                        {
                            editingCelestialProgress.escape = newSM.celestialProgress[i].escape;
                        }

                        if (editingCelestialProgress.flagPlant.Count == 0)
                        {
                            editingCelestialProgress.flagPlant = newSM.celestialProgress[i].flagPlant;
                        }

                        if (editingCelestialProgress.flight.Count == 0)
                        {
                            editingCelestialProgress.flight = newSM.celestialProgress[i].flight;
                        }

                        if (editingCelestialProgress.flyBy.Count == 0)
                        {
                            editingCelestialProgress.flyBy = newSM.celestialProgress[i].flyBy;
                        }

                        if (editingCelestialProgress.landing.Count == 0)
                        {
                            editingCelestialProgress.landing = newSM.celestialProgress[i].landing;
                        }

                        if (editingCelestialProgress.orbit.Count == 0)
                        {
                            editingCelestialProgress.orbit = newSM.celestialProgress[i].orbit;
                        }

                        if (editingCelestialProgress.rendezvous.Count == 0)
                        {
                            editingCelestialProgress.rendezvous = newSM.celestialProgress[i].rendezvous;
                        }

                        if (editingCelestialProgress.returnFromFlyby.Count == 0)
                        {
                            editingCelestialProgress.returnFromFlyby = newSM.celestialProgress[i].returnFromFlyby;
                        }

                        if (editingCelestialProgress.returnFromOrbit.Count == 0)
                        {
                            editingCelestialProgress.returnFromOrbit = newSM.celestialProgress[i].returnFromOrbit;
                        }

                        if (editingCelestialProgress.returnFromSurface.Count == 0)
                        {
                            editingCelestialProgress.returnFromSurface = newSM.celestialProgress[i].returnFromSurface;
                        }

                        if (editingCelestialProgress.science.Count == 0)
                        {
                            editingCelestialProgress.science = newSM.celestialProgress[i].science;
                        }

                        if (editingCelestialProgress.spacewalk.Count == 0)
                        {
                            editingCelestialProgress.spacewalk = newSM.celestialProgress[i].spacewalk;
                        }

                        if (editingCelestialProgress.splashdown.Count == 0)
                        {
                            editingCelestialProgress.splashdown = newSM.celestialProgress[i].splashdown;
                        }

                        if (editingCelestialProgress.stationConstruction.Count == 0)
                        {
                            editingCelestialProgress.stationConstruction = newSM.celestialProgress[i].stationConstruction;
                        }

                        if (editingCelestialProgress.suborbit.Count == 0)
                        {
                            editingCelestialProgress.suborbit = newSM.celestialProgress[i].suborbit;
                        }

                        if (editingCelestialProgress.surfaceEVA.Count == 0)
                        {
                            editingCelestialProgress.surfaceEVA = newSM.celestialProgress[i].surfaceEVA;
                        }

                        editingProgress.celestialProgress[key] = editingCelestialProgress;
                    }
                }
                else
                {
                    editingProgress.celestialProgress.Add(newSM.celestialProgress[i]);

                    celestialBodyList.Add(newSM.celestialProgress[i].celestialBody, celestialBodyList.Count);
                }
            }

            //Secrets --- AKA "Spoilers!!!"
            /*                            *\
                * -------------------------- *
                * Alert!!!!!!!!!!!!!!!!!!!!!!*
                * Spoilers!!!!!!!!!!!!!!!!!!!*
                * Ahead!!!!!!!!!!!!!!!!!!!!!!*
                * -------------------------- *
            \*                            */
            if (editingProgress.secrets.POIBopDeadKraken.Count == 0)
            {
                editingProgress.secrets.POIBopDeadKraken = newSM.secrets.POIBopDeadKraken;
            }

            if (editingProgress.secrets.POIBopRandolith.Count == 0)
            {
                editingProgress.secrets.POIBopRandolith = newSM.secrets.POIBopRandolith;
            }

            if (editingProgress.secrets.POIDresRandolith.Count == 0)
            {
                editingProgress.secrets.POIDresRandolith = newSM.secrets.POIDresRandolith;
            }

            if (editingProgress.secrets.POIDunaFace.Count == 0)
            {
                editingProgress.secrets.POIDunaFace = newSM.secrets.POIDunaFace;
            }

            if (editingProgress.secrets.POIDunaMSL.Count == 0)
            {
                editingProgress.secrets.POIDunaMSL = newSM.secrets.POIDunaMSL;
            }

            if (editingProgress.secrets.POIDunaPyramid.Count == 0)
            {
                editingProgress.secrets.POIDunaPyramid = newSM.secrets.POIDunaPyramid;
            }

            if (editingProgress.secrets.POIDunaRandolith.Count == 0)
            {
                editingProgress.secrets.POIDunaRandolith = newSM.secrets.POIDunaRandolith;
            }

            if (editingProgress.secrets.POIEelooRandolith.Count == 0)
            {
                editingProgress.secrets.POIEelooRandolith = newSM.secrets.POIEelooRandolith;
            }

            if (editingProgress.secrets.POIEveRandolith.Count == 0)
            {
                editingProgress.secrets.POIEveRandolith = newSM.secrets.POIEveRandolith;
            }

            if (editingProgress.secrets.POIGillyRandolith.Count == 0)
            {
                editingProgress.secrets.POIGillyRandolith = newSM.secrets.POIGillyRandolith;
            }

            if (editingProgress.secrets.POIIkeRandolith.Count == 0)
            {
                editingProgress.secrets.POIIkeRandolith = newSM.secrets.POIIkeRandolith;
            }

            if (editingProgress.secrets.POIKerbinIslandAirfield.Count == 0)
            {
                editingProgress.secrets.POIKerbinIslandAirfield = newSM.secrets.POIKerbinIslandAirfield;
            }

            if (editingProgress.secrets.POIKerbinKSC2.Count == 0)
            {
                editingProgress.secrets.POIKerbinKSC2 = newSM.secrets.POIKerbinKSC2;
            }

            if (editingProgress.secrets.POIKerbinMonolith00.Count == 0)
            {
                editingProgress.secrets.POIKerbinMonolith00 = newSM.secrets.POIKerbinMonolith00;
            }

            if (editingProgress.secrets.POIKerbinMonolith01.Count == 0)
            {
                editingProgress.secrets.POIKerbinMonolith01 = newSM.secrets.POIKerbinMonolith01;
            }

            if (editingProgress.secrets.POIKerbinMonolith02.Count == 0)
            {
                editingProgress.secrets.POIKerbinMonolith02 = newSM.secrets.POIKerbinMonolith02;
            }

            if (editingProgress.secrets.POIKerbinPyramids.Count == 0)
            {
                editingProgress.secrets.POIKerbinPyramids = newSM.secrets.POIKerbinPyramids;
            }

            if (editingProgress.secrets.POIKerbinRandolith.Count == 0)
            {
                editingProgress.secrets.POIKerbinRandolith = newSM.secrets.POIKerbinRandolith;
            }

            if (editingProgress.secrets.POIKerbinUFO.Count == 0)
            {
                editingProgress.secrets.POIKerbinUFO = newSM.secrets.POIKerbinUFO;
            }

            if (editingProgress.secrets.POILaytheRandolith.Count == 0)
            {
                editingProgress.secrets.POILaytheRandolith = newSM.secrets.POILaytheRandolith;
            }

            if (editingProgress.secrets.POIMinmusMonolith00.Count == 0)
            {
                editingProgress.secrets.POIMinmusMonolith00 = newSM.secrets.POIMinmusMonolith00;
            }

            if (editingProgress.secrets.POIMinmusRandolith.Count == 0)
            {
                editingProgress.secrets.POIMinmusRandolith = newSM.secrets.POIMinmusRandolith;
            }

            if (editingProgress.secrets.POIMohoRandolith.Count == 0)
            {
                editingProgress.secrets.POIMohoRandolith = newSM.secrets.POIMohoRandolith;
            }

            if (editingProgress.secrets.POIMunArmstrongMemorial.Count == 0)
            {
                editingProgress.secrets.POIMunArmstrongMemorial = newSM.secrets.POIMunArmstrongMemorial;
            }

            if (editingProgress.secrets.POIMunMonolith00.Count == 0)
            {
                editingProgress.secrets.POIMunMonolith00 = newSM.secrets.POIMunMonolith00;
            }

            if (editingProgress.secrets.POIMunMonolith01.Count == 0)
            {
                editingProgress.secrets.POIMunMonolith01 = newSM.secrets.POIMunMonolith01;
            }

            if (editingProgress.secrets.POIMunMonolith02.Count == 0)
            {
                editingProgress.secrets.POIMunMonolith02 = newSM.secrets.POIMunMonolith02;
            }

            if (editingProgress.secrets.POIMunRandolith.Count == 0)
            {
                editingProgress.secrets.POIMunRandolith = newSM.secrets.POIMunRandolith;
            }

            if (editingProgress.secrets.POIMunRockArch00.Count == 0)
            {
                editingProgress.secrets.POIMunRockArch00 = newSM.secrets.POIMunRockArch00;
            }

            if (editingProgress.secrets.POIMunRockArch01.Count == 0)
            {
                editingProgress.secrets.POIMunRockArch01 = newSM.secrets.POIMunRockArch01;
            }

            if (editingProgress.secrets.POIMunRockArch02.Count == 0)
            {
                editingProgress.secrets.POIMunRockArch02 = newSM.secrets.POIMunRockArch02;
            }

            if (editingProgress.secrets.POIMunUFO.Count == 0)
            {
                editingProgress.secrets.POIMunUFO = newSM.secrets.POIMunUFO;
            }

            if (editingProgress.secrets.POIPolRandolith.Count == 0)
            {
                editingProgress.secrets.POIPolRandolith = newSM.secrets.POIPolRandolith;
            }

            if (editingProgress.secrets.POITyloCave.Count == 0)
            {
                editingProgress.secrets.POITyloCave = newSM.secrets.POITyloCave;
            }

            if (editingProgress.secrets.POITyloRandolith.Count == 0)
            {
                editingProgress.secrets.POITyloRandolith = newSM.secrets.POITyloRandolith;
            }

            if (editingProgress.secrets.POIVallIcehenge.Count == 0)
            {
                editingProgress.secrets.POIVallIcehenge = newSM.secrets.POIVallIcehenge;
            }

            if (editingProgress.secrets.POIVallRandolith.Count == 0)
            {
                editingProgress.secrets.POIVallRandolith = newSM.secrets.POIVallRandolith;
            }
            /*                            *\
                * -------------------------- *
                * End!!!!!!!!!!!!!!!!!!!!!!!!*
                * Of!!!!!!!!!!!!!!!!!!!!!!!!!*
                * Spoilers!!!!!!!!!!!!!!!!!!!*
                * -------------------------- *
            \*                            */

            mergedSM = editingProgress;

            SetScenarioData(mergedSM, groupName, subSpace);
        }

        /// <summary>
        /// Merges two converted scenario modules and writes the merged scenario module to the specified subspace group file.
        /// </summary>
        public static void MergeConvertedScenarioModule(ScenarioDataTypes.Reputation newSM, ScenarioDataTypes.Reputation oldSM, string groupName, int subSpace)
        {
            ScenarioDataTypes.Reputation mergedSM = oldSM;

            float oldRep = 0;

            if (!string.IsNullOrEmpty(oldSM.repLine))
            {
                oldRep = Convert.ToSingle(oldSM.repLine.Substring(6));
            }

            float endRep = oldRep;
            
            if (oldSM.header.Count == 0)
            {
                if (newSM.header.Count != 0)
                {
                    mergedSM.header = newSM.header;
                }
            }

            float newFunds = 0;

            if (newSM.repLine.Length > 0)
            {
                newFunds = Convert.ToSingle(newSM.repLine.Substring(6));
            }

            if (newFunds != oldRep)
            {
                endRep += newFunds - oldRep;
            }

            mergedSM.repLine = "rep = " + Convert.ToString(endRep);

            SetScenarioData(mergedSM, groupName, subSpace);
        }

        /// <summary>
        /// Merges two converted scenario modules and writes the merged scenario module to the specified subspace group file.
        /// </summary>
        public static void MergeConvertedScenarioModule(ScenarioDataTypes.ResearchAndDevelopment newSM, ScenarioDataTypes.ResearchAndDevelopment oldSM, string groupName, int subSpace)
        {
            ScenarioDataTypes.ResearchAndDevelopment mergedSM = oldSM;
            List<ScenarioDataTypes.Tech> techList = new List<ScenarioDataTypes.Tech>();
            List<string> addedTechsList = new List<string>();
            List<List<string>> scienceList = new List<List<string>>();
            List<string> addedSciencesList = new List<string>();

            float oldSci = 0;

            if (!string.IsNullOrEmpty(oldSM.sciLine))
            {
                oldSci = Convert.ToSingle(oldSM.sciLine.Substring(6));
            }

            float endSci = oldSci;
            
            if (oldSM.header.Count == 0)
            {
                if (newSM.header.Count != 0)
                {
                    mergedSM.header = newSM.header;
                }
            }

            float newFunds = 0;

            if (newSM.sciLine.Length > 0)
            {
                newFunds = Convert.ToSingle(newSM.sciLine.Substring(6));
            }

            if (newFunds != oldSci)
            {
                endSci += newFunds - oldSci;
            }

            for (int i = 0; i < newSM.techList.Count; i++)
            {
                if (!addedTechsList.Contains(newSM.techList[i].idLine))
                {
                    techList.Add(newSM.techList[i]);
                    addedTechsList.Add(newSM.techList[i].idLine);
                }
            }

            for (int i = 0; i < oldSM.techList.Count; i++)
            {
                if (!addedTechsList.Contains(oldSM.techList[i].idLine))
                {
                    techList.Add(oldSM.techList[i]);
                    addedTechsList.Add(oldSM.techList[i].idLine);
                }
            }

            for (int i = 0; i < newSM.scienceList.Count; i++)
            {
                if (!addedSciencesList.Contains(newSM.scienceList[i][0]))
                {
                    scienceList.Add(newSM.scienceList[i]);
                    addedSciencesList.Add(newSM.scienceList[i][0]);
                }
            }

            for (int i = 0; i < oldSM.scienceList.Count; i++)
            {
                if (!addedSciencesList.Contains(oldSM.scienceList[i][0]))
                {
                    scienceList.Add(oldSM.scienceList[i]);
                    addedSciencesList.Add(oldSM.scienceList[i][0]);
                }
            }

            mergedSM.sciLine = "sci = " + Convert.ToString(endSci);

            mergedSM.techList = techList;

            mergedSM.scienceList = scienceList;

            SetScenarioData(mergedSM, groupName, subSpace);
        }

        /// <summary>
        /// Merges two converted scenario modules and writes the merged scenario module to the specified subspace group file.
        /// </summary>
        public static void MergeConvertedScenarioModule(ScenarioDataTypes.ResourceScenario newSM, ScenarioDataTypes.ResourceScenario oldSM, string groupName, int subSpace)
        {
            ScenarioDataTypes.ResourceScenario mergedSM = oldSM;
            List<string> listOfAlreadyAddedPlanetScanData = new List<string>();

            for (int i = 0; i < oldSM.resourceSettings.scanDataList.Count; i++)
            {
                listOfAlreadyAddedPlanetScanData.Add(oldSM.resourceSettings.scanDataList[i].scanDataLine);
            }
            
            if (oldSM.header.Count == 0)
            {
                if (newSM.header.Count != 0)
                {
                    mergedSM.header = newSM.header;
                }
            }

            for (int i = 0; i < newSM.resourceSettings.scanDataList.Count; i++)
            {
                if (!listOfAlreadyAddedPlanetScanData.Contains(newSM.resourceSettings.scanDataList[i].scanDataLine))
                {
                    mergedSM.resourceSettings.scanDataList.Add(newSM.resourceSettings.scanDataList[i]);

                    listOfAlreadyAddedPlanetScanData.Add(newSM.resourceSettings.scanDataList[i].scanDataLine);
                }
            }

            SetScenarioData(mergedSM, groupName, subSpace);
        }

        /// <summary>
        /// Merges two converted scenario modules and writes the merged scenario module to the specified subspace group file.
        /// </summary>
        public static void MergeConvertedScenarioModule(ScenarioDataTypes.ScenarioCustomWaypoints newSM, ScenarioDataTypes.ScenarioCustomWaypoints oldSM, string groupName, int subSpace)
        {
            ScenarioDataTypes.ScenarioCustomWaypoints mergedSM = oldSM;
            List<string> listOfAlreadyAddedWaypoints = new List<string>();

            for (int i = 0; i < oldSM.waypoints.Count; i++)
            {
                listOfAlreadyAddedWaypoints.Add(oldSM.waypoints[i].waypointLines[0]);
            }
            
            if (oldSM.header.Count == 0)
            {
                if (newSM.header.Count != 0)
                {
                    mergedSM.header = newSM.header;
                }
            }

            for (int i = 0; i < newSM.waypoints.Count; i++)
            {
                if (!listOfAlreadyAddedWaypoints.Contains(newSM.waypoints[i].waypointLines[0]))
                {
                    mergedSM.waypoints.Add(newSM.waypoints[i]);

                    listOfAlreadyAddedWaypoints.Add(newSM.waypoints[i].waypointLines[0]);
                }
            }

            SetScenarioData(mergedSM, groupName, subSpace);
        }

        /// <summary>
        /// Merges two converted scenario modules and writes the merged scenario module to the specified subspace group file.
        /// </summary>
        public static void MergeConvertedScenarioModule(ScenarioDataTypes.ScenarioDestructibles newSM, ScenarioDataTypes.ScenarioDestructibles oldSM, string groupName, int subSpace)
        {
            ScenarioDataTypes.ScenarioDestructibles mergedSM = oldSM;
            List<string> hasChanged = new List<string>();
            
            if (oldSM.header.Count == 0)
            {
                if (newSM.header.Count != 0)
                {
                    mergedSM.header = newSM.header;
                }
            }

            for (int i = 0; i < oldSM.destructibles.Count; i++)
            {
                string currentID = oldSM.destructibles[i].id;

                if (!hasChanged.Contains(currentID))
                {
                    ScenarioDataTypes.Destructibles newDestructible = newSM.destructibles.Where(v => v.id == currentID).ToList()[0];

                    if (newDestructible.infoLine != oldSM.destructibles[i].infoLine)
                    {
                        mergedSM.destructibles[i] = newDestructible;

                        hasChanged.Add(currentID);
                    }
                }
            }

            SetScenarioData(mergedSM, groupName, subSpace);
        }

        /// <summary>
        /// Merges two converted scenario modules and writes the merged scenario module to the specified subspace group file.
        /// </summary>
        public static void MergeConvertedScenarioModule(ScenarioDataTypes.ScenarioUpgradeableFacilities newSM, ScenarioDataTypes.ScenarioUpgradeableFacilities oldSM, string groupName, int subSpace)
        {
            ScenarioDataTypes.ScenarioUpgradeableFacilities mergedSM = oldSM;
            
            if (oldSM.header.Count == 0)
            {
                if (newSM.header.Count != 0)
                {
                    mergedSM.header = newSM.header;
                }
            }

            if (!string.IsNullOrEmpty(newSM.buildings.administration))
            {
                if (!string.IsNullOrEmpty(oldSM.buildings.administration))
                {
                    if (Convert.ToSingle(newSM.buildings.administration.Substring(6)) > Convert.ToSingle(mergedSM.buildings.administration.Substring(6)))
                    {
                        mergedSM.buildings.administration = newSM.buildings.administration;
                    }
                }
                else
                {
                    mergedSM.buildings.administration = newSM.buildings.administration;
                }
            }

            if (!string.IsNullOrEmpty(newSM.buildings.astronautComplex))
            {
                if (!string.IsNullOrEmpty(oldSM.buildings.astronautComplex))
                {
                    if (Convert.ToSingle(newSM.buildings.astronautComplex.Substring(6)) > Convert.ToSingle(mergedSM.buildings.astronautComplex.Substring(6)))
                    {
                        mergedSM.buildings.astronautComplex = newSM.buildings.astronautComplex;
                    }
                }
                else
                {
                    mergedSM.buildings.astronautComplex = newSM.buildings.astronautComplex;
                }
            }

            if (!string.IsNullOrEmpty(newSM.buildings.flagPole))
            {
                if (!string.IsNullOrEmpty(oldSM.buildings.flagPole))
                {
                    if (Convert.ToSingle(newSM.buildings.flagPole.Substring(6)) > Convert.ToSingle(mergedSM.buildings.flagPole.Substring(6)))
                    {
                        mergedSM.buildings.flagPole = newSM.buildings.flagPole;
                    }
                }
                else
                {
                    mergedSM.buildings.flagPole = newSM.buildings.flagPole;
                }
            }

            if (!string.IsNullOrEmpty(newSM.buildings.launchPad))
            {
                if (!string.IsNullOrEmpty(oldSM.buildings.launchPad))
                {
                    if (Convert.ToSingle(newSM.buildings.launchPad.Substring(6)) > Convert.ToSingle(mergedSM.buildings.launchPad.Substring(6)))
                    {
                        mergedSM.buildings.launchPad = newSM.buildings.launchPad;
                    }
                }
                else
                {
                    mergedSM.buildings.launchPad = newSM.buildings.launchPad;
                }
            }

            if (!string.IsNullOrEmpty(newSM.buildings.missionControl))
            {
                if (!string.IsNullOrEmpty(oldSM.buildings.missionControl))
                {
                    if (Convert.ToSingle(newSM.buildings.missionControl.Substring(6)) > Convert.ToSingle(mergedSM.buildings.missionControl.Substring(6)))
                    {
                        mergedSM.buildings.missionControl = newSM.buildings.missionControl;
                    }
                }
                else
                {
                    mergedSM.buildings.missionControl = newSM.buildings.missionControl;
                }
            }

            if (!string.IsNullOrEmpty(newSM.buildings.researchAndDevelopment))
            {
                if (!string.IsNullOrEmpty(oldSM.buildings.researchAndDevelopment))
                {
                    if (Convert.ToSingle(newSM.buildings.researchAndDevelopment.Substring(6)) > Convert.ToSingle(mergedSM.buildings.researchAndDevelopment.Substring(6)))
                    {
                        mergedSM.buildings.researchAndDevelopment = newSM.buildings.researchAndDevelopment;
                    }
                }
                else
                {
                    mergedSM.buildings.researchAndDevelopment = newSM.buildings.researchAndDevelopment;
                }
            }

            if (!string.IsNullOrEmpty(newSM.buildings.runway))
            {
                if (!string.IsNullOrEmpty(oldSM.buildings.runway))
                {
                    if (Convert.ToSingle(newSM.buildings.runway.Substring(6)) > Convert.ToSingle(mergedSM.buildings.runway.Substring(6)))
                    {
                        mergedSM.buildings.runway = newSM.buildings.runway;
                    }
                }
                else
                {
                    mergedSM.buildings.runway = newSM.buildings.runway;
                }
            }

            if (!string.IsNullOrEmpty(newSM.buildings.spaceplaneHangar))
            {
                if (!string.IsNullOrEmpty(oldSM.buildings.spaceplaneHangar))
                {
                    if (Convert.ToSingle(newSM.buildings.spaceplaneHangar.Substring(6)) > Convert.ToSingle(mergedSM.buildings.spaceplaneHangar.Substring(6)))
                    {
                        mergedSM.buildings.spaceplaneHangar = newSM.buildings.spaceplaneHangar;
                    }
                }
                else
                {
                    mergedSM.buildings.spaceplaneHangar = newSM.buildings.spaceplaneHangar;
                }
            }

            if (!string.IsNullOrEmpty(newSM.buildings.trackingStation))
            {
                if (!string.IsNullOrEmpty(oldSM.buildings.trackingStation))
                {
                    if (Convert.ToSingle(newSM.buildings.trackingStation.Substring(6)) > Convert.ToSingle(mergedSM.buildings.trackingStation.Substring(6)))
                    {
                        mergedSM.buildings.trackingStation = newSM.buildings.trackingStation;
                    }
                }
                else
                {
                    mergedSM.buildings.trackingStation = newSM.buildings.trackingStation;
                }
            }

            if (!string.IsNullOrEmpty(newSM.buildings.vehicleAssemblyBuilding))
            {
                if (!string.IsNullOrEmpty(oldSM.buildings.vehicleAssemblyBuilding))
                {
                    if (Convert.ToSingle(newSM.buildings.vehicleAssemblyBuilding.Substring(6)) > Convert.ToSingle(mergedSM.buildings.vehicleAssemblyBuilding.Substring(6)))
                    {
                        mergedSM.buildings.vehicleAssemblyBuilding = newSM.buildings.vehicleAssemblyBuilding;
                    }
                }
                else
                {
                    mergedSM.buildings.vehicleAssemblyBuilding = newSM.buildings.vehicleAssemblyBuilding;
                }
            }

            SetScenarioData(mergedSM, groupName, subSpace);
        }

        /// <summary>
        /// Merges two converted scenario modules and writes the merged scenario module to the specified subspace group file.
        /// </summary>
        public static void MergeConvertedScenarioModule(ScenarioDataTypes.StrategySystem newSM, ScenarioDataTypes.StrategySystem oldSM, string groupName, int subSpace)
        {
            ScenarioDataTypes.StrategySystem mergedSM = oldSM;
            List<string> listOfAlreadyAddedStrategies = new List<string>();

            for (int i = 0; i < oldSM.strategies.Count; i++)
            {
                if (oldSM.strategies[i].strategy.Count > 0)
                {
                    listOfAlreadyAddedStrategies.Add(oldSM.strategies[i].strategy[0]);
                }
            }
            
            if (oldSM.header.Count == 0)
            {
                if (newSM.header.Count != 0)
                {
                    mergedSM.header = newSM.header;
                }
            }

            for (int i = 0; i < newSM.strategies.Count; i++)
            {
                if (newSM.strategies[i].strategy.Count > 0)
                {
                    if (!listOfAlreadyAddedStrategies.Contains(newSM.strategies[i].strategy[0]))
                    {
                        mergedSM.strategies.Add(newSM.strategies[i]);

                        listOfAlreadyAddedStrategies.Add(newSM.strategies[i].strategy[0]);
                    }
                }
            }

            SetScenarioData(mergedSM, groupName, subSpace);
        }

        public static void SaveAllGroupsProgress(List<List<string>> input)
        {
            List<string> output = new List<string>();
            foreach (List<string> list in input)
            {
                output.Add("GroupProgress");
                output.Add("{");

                output.AddRange(list);

                output.Add("}");
            }
            FileHandler.WriteToFile(ByteArraySerializer.Serialize(output), Path.Combine(Server.ScenarioDirectory, "GroupData", "AllGroupProgress.txt"));
        }

        public static List<List<string>> LoadAllGroupsProgress()
        {
            List<List<string>> output = new List<List<string>>();

            if (File.Exists(Path.Combine(Server.ScenarioDirectory, "GroupData", "AllGroupProgress.txt")))
            {
                List<string> subOutput = ByteArraySerializer.Deserialize(FileHandler.ReadFromFile(Path.Combine(Server.ScenarioDirectory, "GroupData", "AllGroupProgress.txt")));

                if (subOutput != null)
                {
                    int cursor = 0;
                    while (cursor < subOutput.Count)
                    {
                        if (subOutput[cursor] == "GroupProgress" && subOutput[cursor + 1] == "{")
                        {
                            int matchBracketIdx = DataCleaner.FindMatchingBracket(subOutput, cursor + 1);
                            KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                            if (range.Key + 2 < subOutput.Count && range.Value - 3 > 0)
                            {
                                output.Add(subOutput.GetRange(range.Key + 2, range.Value - 3));

                                subOutput.RemoveRange(range.Key, range.Value);
                            }
                            else
                            {
                                subOutput.RemoveRange(range.Key, range.Value);
                            }
                        }
                        else
                        {
                            cursor++;
                        }
                    }
                }
                else
                {
                    output = GetAllGroupsProgress();

                    SaveAllGroupsProgress(output);
                }
            }
            else
            {
                output = GetAllGroupsProgress();

                SaveAllGroupsProgress(output);
            }

            return output;
        }

        public static void SetAllGroupsProgress(List<List<string>> input)
        {
            List<GroupProgress> result = new List<GroupProgress>();

            foreach (List<string> groupProgressList in input)
            {
                GroupProgress newGP = new GroupProgress();

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

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(groupProgressList, cursor + 1);
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

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(groupProgressList, cursor + 1);
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

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(groupProgressList, cursor + 1);
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
                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(celestialProgressLines, subCursor + 1);
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

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(groupProgressList, cursor + 1);
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
                newGP.GroupName = groupName;
                newGP.GroupSubspace = groupSubspace;

                newGP.Funds = funds;
                newGP.Rep = rep;
                newGP.Sci = sci;

                newGP.Techs = techs;
                newGP.Progress = progress;
                newGP.CelestialProgress = celestialProgress;
                newGP.Secrets = secrets;

                result.Add(newGP);
            }

            AllGroupsProgress = result;
        }

        public static List<List<string>> GetAllGroupsProgress()
        {
            List<List<string>> output = new List<List<string>>();
            foreach (GroupProgress gp in AllGroupsProgress)
            {
                List<string> subOutput = new List<string>();
                subOutput.Add(gp.GroupName);
                subOutput.Add(Convert.ToString(gp.GroupSubspace));
                subOutput.Add(gp.Funds);
                subOutput.Add(gp.Rep);
                subOutput.Add(gp.Sci);
                subOutput.Add("Techs");
                subOutput.Add("{");
                subOutput.AddRange(gp.Techs);
                subOutput.Add("}");
                subOutput.Add("Progress");
                subOutput.Add("{");
                subOutput.AddRange(gp.Progress);
                subOutput.Add("}");
                subOutput.Add("CelestialProgressList");
                subOutput.Add("{");
                foreach (List<string> cp in gp.CelestialProgress)
                {
                    subOutput.Add("CelestialProgress");
                    subOutput.Add("{");
                    subOutput.AddRange(cp);
                    subOutput.Add("}");
                }
                subOutput.Add("}");
                subOutput.Add("Secrets");
                subOutput.Add("{");
                subOutput.AddRange(gp.Secrets);
                subOutput.Add("}");

                output.Add(subOutput);
            }

            SaveAllGroupsProgress(output);

            return output;
        }

        public static GroupProgress GetGroupProgress(string groupName, int groupSubspace)
        {
            GroupProgress result = AllGroupsProgress.FirstOrDefault(i => i.GroupName == groupName && i.GroupSubspace == groupSubspace);

            return result;
        }

        public static void SetGroupProgress(string groupName, int groupSubspace, GroupProgress gpToDataToWrite)
        {
            if (AllGroupsProgress.Any(i => i.GroupName == groupName && i.GroupSubspace == groupSubspace))
            {
                int index = AllGroupsProgress.FindIndex(i => i.GroupName == groupName && i.GroupSubspace == groupSubspace);

                if (!string.IsNullOrEmpty(gpToDataToWrite.Funds))
                {
                    AllGroupsProgress[index].Funds = gpToDataToWrite.Funds;
                }

                if (!string.IsNullOrEmpty(gpToDataToWrite.Rep))
                {
                    AllGroupsProgress[index].Rep = gpToDataToWrite.Rep;
                }

                if (!string.IsNullOrEmpty(gpToDataToWrite.Sci))
                {
                    AllGroupsProgress[index].Sci = gpToDataToWrite.Sci;
                }

                if (gpToDataToWrite.Techs.Count > 0)
                {
                    AllGroupsProgress[index].Techs = gpToDataToWrite.Techs;
                }

                if (gpToDataToWrite.Progress.Count > 0)
                {
                    AllGroupsProgress[index].Progress = gpToDataToWrite.Progress;
                }

                if (gpToDataToWrite.CelestialProgress.Count > 0)
                {
                    AllGroupsProgress[index].CelestialProgress = gpToDataToWrite.CelestialProgress;
                }

                if (gpToDataToWrite.Secrets.Count > 0)
                {
                    AllGroupsProgress[index].Secrets = gpToDataToWrite.Secrets;
                }
            }
            else
            {
                AllGroupsProgress.Add(gpToDataToWrite);
            }
        }

        public class GroupSubspaces
        {
            public int SubspaceNumber = 0;
            public List<string> GroupNames = new List<string>();
        }

        public class GroupProgress
        {
            public string GroupName = string.Empty;
            public int GroupSubspace = 0;
            public string Funds = string.Empty;
            public string Rep = string.Empty;
            public string Sci = string.Empty;
            public List<string> Techs = new List<string>();
            public List<string> Progress = new List<string>();
            public List<List<string>> CelestialProgress = new List<List<string>>();
            public List<string> Secrets = new List<string>();
        }
    }
}
