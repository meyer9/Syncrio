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
using System.Linq;
using System.IO;
using System.Threading;
using UnityEngine;
using SyncrioCommon;
using MessageStream2;
using System.Reflection;

namespace SyncrioClientSide
{
    public class ScenarioWorker
    {
        public bool workerEnabled = false;
        private static ScenarioWorker singleton;
        public bool isSyncing = false;
        public bool stopSync = false;
        private object scenarioLock = new object();
        public bool nonGroupScenarios;
        public List<byte[]> baseData;
        public List<byte[]> startData;
        public int baseDataLoadAttempts = 0;
        public bool loadBaseData = false;
        public float lastBaseDataLoadAttempt = 0f;
        private delegate ScienceSubject getScienceSubjectDelegate(ResearchAndDevelopment r, ScienceSubject subject);
        private getScienceSubjectDelegate getScienceSubjectThunk;

        public static ScenarioWorker fetch
        {
            get
            {
                return singleton;
            }
        }

        public ScenarioWorker()
        {
            Type rAndDType = typeof(ResearchAndDevelopment);
            MethodInfo getScienceSubjectMethodInfo = rAndDType.GetMethod("getScienceSubject", BindingFlags.NonPublic | BindingFlags.Instance);
            getScienceSubjectThunk = (getScienceSubjectDelegate)Delegate.CreateDelegate(typeof(getScienceSubjectDelegate), null, getScienceSubjectMethodInfo);
        }

        public void LoadMissingScenarioDataIntoGame()
        {
            List<KSPScenarioType> validScenarios = KSPScenarioType.GetAllScenarioTypesInAssemblies();
            foreach (KSPScenarioType validScenario in validScenarios)
            {
                if (HighLogic.CurrentGame.scenarios.Exists(psm => psm.moduleName == validScenario.ModuleType.Name))
                {
                    continue;
                }
                bool loadModule = false;
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                {
                    loadModule = validScenario.ScenarioAttributes.HasCreateOption(ScenarioCreationOptions.AddToNewCareerGames);
                }
                if (HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
                {
                    loadModule = validScenario.ScenarioAttributes.HasCreateOption(ScenarioCreationOptions.AddToNewScienceSandboxGames);
                }
                if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX)
                {
                    loadModule = validScenario.ScenarioAttributes.HasCreateOption(ScenarioCreationOptions.AddToNewSandboxGames);
                }
                if (loadModule)
                {
                    SyncrioLog.Debug("Creating new scenario module " + validScenario.ModuleType.Name);
                    HighLogic.CurrentGame.AddProtoScenarioModule(validScenario.ModuleType, validScenario.ScenarioAttributes.TargetScenes);
                }
            }
        }

        public bool LoadStartScenarioData()
        {
            bool returnVal = false;

            if (startData != null)
            {
                if (startData.Count > 0)
                {
                    for (int i = 0; i < startData.Count; i += 2)
                    {
                        if (i + 1 >= startData.Count)
                        {
                            break;
                        }

                        List<string> name = SyncrioUtil.ByteArraySerializer.Deserialize(startData[i]);

                        if (name[0] == "ResourceScenario")
                        {
                            returnVal = true;
                        }

                        ProtoScenarioModule psm = new ProtoScenarioModule(ConfigNodeSerializer.fetch.Deserialize(startData[i + 1]));

                        if (psm != null)
                        {
                            HighLogic.CurrentGame.scenarios.Add(psm);
                        }
                    }
                }
            }

            return returnVal;
        }

        public bool LoadBaseScenarioData()
        {
            if (ResearchAndDevelopment.Instance == null || ProgressTracking.Instance == null || (HighLogic.CurrentGame.Mode == Game.Modes.CAREER && Contracts.ContractSystem.Instance == null))
            {
                return false;
            }

            if (baseData != null)
            {
                if (baseData.Count > 0)
                {
                    LoadScenarioData(baseData);
                    baseData = null;
                }
            }

            ScenarioEventHandler.fetch.delaySync = false;

            return true;
        }

        public void LoadScenarioData(List<byte[]> data)
        {
            lock (scenarioLock)
            {
                if (Client.fetch.gameRunning)
                {
                    if (HighLogic.LoadedScene != GameScenes.LOADING)
                    {
                        isSyncing = true;
                        ScenarioEventHandler.fetch.startCooldown = true;
                        ScenarioEventHandler.fetch.cooldown = true;

                        for (int v = 0; v < data.Count; v += 2)
                        {
                            if (v + 1 >= data.Count)
                            {
                                break;
                            }

                            List<string> name = SyncrioUtil.ByteArraySerializer.Deserialize(data[v]);
                            List<string> dataList = SyncrioUtil.ByteArraySerializer.Deserialize(data[v + 1]);

                            try
                            {
                                if (name[0] == "Contracts" && HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                                {
                                    if (!ScenarioEventHandler.fetch.MissionControlOpen && (HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.EDITOR))
                                    {
                                        List<Contracts.Contract> contracts = Contracts.ContractSystem.Instance.Contracts;

                                        for (int i = 0; i < contracts.Count; i++)
                                        {
                                            if (contracts[i].ContractState == Contracts.Contract.State.Active)
                                            {
                                                contracts[i].Unregister();
                                            }
                                        }

                                        ConfigNode ContractCFG = new ConfigNode();

                                        Contracts.ContractSystem.Instance.Save(ContractCFG);

                                        List<string> contractData = SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(ContractCFG));

                                        int index = 0;
                                        while (index < contractData.Count)
                                        {
                                            if (contractData[index] == "CONTRACTS" && contractData[index + 1] == "{")
                                            {
                                                int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(contractData, index + 1);
                                                KeyValuePair<int, int> range = new KeyValuePair<int, int>(index, (matchBracketIdx - index) + 1);

                                                contractData.RemoveRange(range.Key, range.Value);
                                            }
                                            else
                                            {
                                                index++;
                                            }
                                        }

                                        contractData.Add("CONTRACTS");
                                        contractData.Add("{");

                                        int looped = 0;
                                        while (looped < dataList.Count)
                                        {
                                            if (dataList[looped] == "ContractNode")
                                            {
                                                int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(dataList, looped + 1);
                                                KeyValuePair<int, int> range = new KeyValuePair<int, int>(looped, (matchBracketIdx - looped) + 1);

                                                if (range.Key + 2 < dataList.Count && range.Value - 3 > 0)
                                                {
                                                    contractData.AddRange(dataList.GetRange(range.Key + 2, range.Value - 3));

                                                    dataList.RemoveRange(range.Key, range.Value);
                                                }
                                                else
                                                {
                                                    looped++;
                                                }
                                            }
                                            else
                                            {
                                                looped++;
                                            }
                                        }

                                        contractData.Add("}");

                                        ContractCFG = ConfigNodeSerializer.fetch.Deserialize(SyncrioUtil.ByteArraySerializer.Serialize(contractData));

                                        List<ProtoScenarioModule> psmLocked = HighLogic.CurrentGame.scenarios;

                                        int idx = psmLocked.FindIndex(i => i.moduleName == "ContractSystem");

                                        if (idx != -1)
                                        {
                                            if (ScenarioRunner.GetLoadedModules().Contains(psmLocked[idx].moduleRef))
                                            {
                                                ScenarioRunner.RemoveModule(psmLocked[idx].moduleRef);
                                            }

                                            psmLocked[idx].moduleRef = ScenarioRunner.Instance.AddModule(ContractCFG);
                                            psmLocked[idx].moduleRef.targetScenes = HighLogic.CurrentGame.scenarios[idx].targetScenes;

                                            HighLogic.CurrentGame.scenarios = psmLocked;
                                        }
                                        else
                                        {
                                            ScenarioModule sm = ScenarioRunner.Instance.AddModule(ContractCFG);

                                            HighLogic.CurrentGame.AddProtoScenarioModule(sm.GetType(), new GameScenes[4] { GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER, GameScenes.EDITOR });

                                            psmLocked = HighLogic.CurrentGame.scenarios;

                                            idx = psmLocked.FindIndex(i => i.moduleName == "ContractSystem");

                                            psmLocked[idx].moduleRef = sm;
                                            psmLocked[idx].moduleRef.targetScenes = HighLogic.CurrentGame.scenarios[idx].targetScenes;

                                            HighLogic.CurrentGame.scenarios = psmLocked;
                                        }
                                        
                                        List<Contracts.Contract> newContracts = Contracts.ContractSystem.Instance.Contracts;

                                        for (int i = 0; i < newContracts.Count; i++)
                                        {
                                            if (newContracts[i].ContractState == Contracts.Contract.State.Active)
                                            {
                                                newContracts[i].Register();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        List<byte[]> dataBacklog = new List<byte[]>();

                                        dataBacklog.Add(data[v]);
                                        dataBacklog.Add(data[v + 1]);

                                        ScenarioEventHandler.fetch.scenarioBacklog.Add(new KeyValuePair<string, List<byte[]>>("Contract", dataBacklog));
                                    }
                                }

                                if (name[0] == "Weights" && HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                                {
                                    foreach (string weight in dataList)
                                    {
                                        if (!string.IsNullOrEmpty(weight))
                                        {
                                            if (weight.Contains(":"))
                                            {
                                                string[] split = weight.Split(':');
                                                string weightName = split[0].Trim();
                                                int weightAmount = Convert.ToInt32(split[1].Trim());

                                                if (Contracts.ContractSystem.ContractWeights.ContainsKey(weightName))
                                                {
                                                    Contracts.ContractSystem.ContractWeights[weightName] = weightAmount;
                                                }
                                            }
                                        }
                                    }
                                }

                                if (name[0] == "Waypoints")
                                {
                                    if (HighLogic.LoadedScene != GameScenes.TRACKSTATION && (HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.EDITOR))
                                    {
                                        ConfigNode WaypointCFG = new ConfigNode();

                                        ScenarioCustomWaypoints.Instance.Save(WaypointCFG);

                                        List<string> waypointData = SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(WaypointCFG));

                                        int index = 0;
                                        while (index < waypointData.Count)
                                        {
                                            if (waypointData[index] == "WAYPOINT" && waypointData[index + 1] == "{")
                                            {
                                                int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(waypointData, index + 1);
                                                KeyValuePair<int, int> range = new KeyValuePair<int, int>(index, (matchBracketIdx - index) + 1);

                                                waypointData.RemoveRange(range.Key, range.Value);
                                            }
                                            else
                                            {
                                                index++;
                                            }
                                        }

                                        int looped = 0;
                                        while (looped < dataList.Count)
                                        {
                                            if (dataList[looped].StartsWith("Waypoint : ") && dataList[looped + 1] == "{")
                                            {
                                                int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(dataList, looped + 1);
                                                KeyValuePair<int, int> range = new KeyValuePair<int, int>(looped, (matchBracketIdx - looped) + 1);

                                                if (range.Key + 1 < dataList.Count && range.Value - 1 > 0)
                                                {
                                                    waypointData.Add("WAYPOINT");

                                                    waypointData.AddRange(dataList.GetRange(range.Key + 1, range.Value - 1));

                                                    dataList.RemoveRange(range.Key, range.Value);
                                                }
                                                else
                                                {
                                                    looped++;
                                                }
                                            }
                                            else
                                            {
                                                looped++;
                                            }
                                        }

                                        WaypointCFG = ConfigNodeSerializer.fetch.Deserialize(SyncrioUtil.ByteArraySerializer.Serialize(waypointData));

                                        List<ProtoScenarioModule> psmLocked = HighLogic.CurrentGame.scenarios;

                                        int idx = psmLocked.FindIndex(i => i.moduleName == "ScenarioCustomWaypoints");

                                        if (idx != -1)
                                        {
                                            if (ScenarioRunner.GetLoadedModules().Contains(psmLocked[idx].moduleRef))
                                            {
                                                ScenarioRunner.RemoveModule(psmLocked[idx].moduleRef);
                                            }

                                            psmLocked[idx].moduleRef = ScenarioRunner.Instance.AddModule(WaypointCFG);
                                            psmLocked[idx].moduleRef.targetScenes = HighLogic.CurrentGame.scenarios[idx].targetScenes;

                                            HighLogic.CurrentGame.scenarios = psmLocked;
                                        }
                                        else
                                        {
                                            ScenarioModule sm = ScenarioRunner.Instance.AddModule(WaypointCFG);

                                            HighLogic.CurrentGame.AddProtoScenarioModule(sm.GetType(), new GameScenes[2] { GameScenes.FLIGHT, GameScenes.TRACKSTATION });

                                            psmLocked = HighLogic.CurrentGame.scenarios;

                                            idx = psmLocked.FindIndex(i => i.moduleName == "ScenarioCustomWaypoints");

                                            psmLocked[idx].moduleRef = sm;
                                            psmLocked[idx].moduleRef.targetScenes = HighLogic.CurrentGame.scenarios[idx].targetScenes;

                                            HighLogic.CurrentGame.scenarios = psmLocked;
                                        }
                                    }
                                    else
                                    {
                                        List<byte[]> dataBacklog = new List<byte[]>();

                                        dataBacklog.Add(data[v]);
                                        dataBacklog.Add(data[v + 1]);

                                        ScenarioEventHandler.fetch.scenarioBacklog.Add(new KeyValuePair<string, List<byte[]>>("Waypoint", dataBacklog));
                                    }
                                }

                                if (name[0] == "Currency")
                                {
                                    if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                                    {
                                        double fundsAmount = Convert.ToDouble(dataList[0], Client.english);
                                        double fundsAmountDiff = fundsAmount - Funding.Instance.Funds;
                                        Funding.Instance.AddFunds(fundsAmountDiff, TransactionReasons.None);

                                        float repAmount = Convert.ToSingle(dataList[1], Client.english);
                                        float repAmountDiff = repAmount - Reputation.Instance.reputation;
                                        Reputation.Instance.AddReputation(repAmountDiff, TransactionReasons.None);

                                        float sciAmount = Convert.ToSingle(dataList[2], Client.english);
                                        float sciAmountDiff = sciAmount - ResearchAndDevelopment.Instance.Science;
                                        ResearchAndDevelopment.Instance.AddScience(sciAmountDiff, TransactionReasons.None);
                                    }
                                    else
                                    {
                                        if (HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
                                        {
                                            float sciAmount = Convert.ToSingle(dataList[2]);
                                            float sciAmountDiff = sciAmount - ResearchAndDevelopment.Instance.Science;
                                            ResearchAndDevelopment.Instance.AddScience(sciAmountDiff, TransactionReasons.None);
                                        }
                                    }
                                }

                                if (name[0] == "BuildingLevel")
                                {
                                    if (HighLogic.LoadedScene == GameScenes.SPACECENTER && !ScenarioEventHandler.fetch.RnDOpen && !ScenarioEventHandler.fetch.MissionControlOpen && !ScenarioEventHandler.fetch.AdministrationOpen)
                                    {
                                        ConfigNode cfg = new ConfigNode();

                                        ScenarioUpgradeableFacilities.Instance.Save(cfg);

                                        List<string> cfgData = SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(cfg));

                                        foreach (string building in dataList)
                                        {
                                            string[] subData = building.Split('=');

                                            string id = subData[0].Trim();
                                            int level = Convert.ToInt32(subData[1].Trim(), Client.english);

                                            float realLevel;

                                            if (level == 2)
                                            {
                                                realLevel = 1f;
                                            }
                                            else
                                            {
                                                if (level == 1)
                                                {
                                                    realLevel = 0.5f;
                                                }
                                                else
                                                {
                                                    realLevel = 0;
                                                }
                                            }

                                            if (cfgData.Contains(id))
                                            {
                                                int index = cfgData.FindIndex(i => i == id);

                                                cfgData[index + 2] = string.Format(Client.english, "lvl = {0}", realLevel);
                                            }
                                        }

                                        cfg = ConfigNodeSerializer.fetch.Deserialize(SyncrioUtil.ByteArraySerializer.Serialize(cfgData));

                                        List<ProtoScenarioModule> psmLocked = HighLogic.CurrentGame.scenarios;

                                        int idx = psmLocked.FindIndex(i => i.moduleName == "ScenarioUpgradeableFacilities");

                                        if (idx != -1)
                                        {
                                            if (ScenarioRunner.GetLoadedModules().Contains(psmLocked[idx].moduleRef))
                                            {
                                                ScenarioRunner.RemoveModule(psmLocked[idx].moduleRef);
                                            }

                                            psmLocked[idx].moduleRef = ScenarioRunner.Instance.AddModule(cfg);
                                            psmLocked[idx].moduleRef.targetScenes = HighLogic.CurrentGame.scenarios[idx].targetScenes;

                                            HighLogic.CurrentGame.scenarios = psmLocked;
                                        }
                                        else
                                        {
                                            ScenarioModule sm = ScenarioRunner.Instance.AddModule(cfg);

                                            HighLogic.CurrentGame.AddProtoScenarioModule(sm.GetType(), new GameScenes[4] { GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION });

                                            psmLocked = HighLogic.CurrentGame.scenarios;

                                            idx = psmLocked.FindIndex(i => i.moduleName == "ScenarioUpgradeableFacilities");

                                            psmLocked[idx].moduleRef = sm;
                                            psmLocked[idx].moduleRef.targetScenes = HighLogic.CurrentGame.scenarios[idx].targetScenes;

                                            HighLogic.CurrentGame.scenarios = psmLocked;
                                        }
                                    }
                                    else
                                    {
                                        List<byte[]> dataBacklog = new List<byte[]>();

                                        dataBacklog.Add(data[v]);
                                        dataBacklog.Add(data[v + 1]);

                                        ScenarioEventHandler.fetch.scenarioBacklog.Add(new KeyValuePair<string, List<byte[]>>("Building", dataBacklog));
                                    }
                                }

                                if (name[0] == "BuildingDead")
                                {
                                    if (HighLogic.LoadedScene == GameScenes.SPACECENTER && !ScenarioEventHandler.fetch.RnDOpen && !ScenarioEventHandler.fetch.MissionControlOpen && !ScenarioEventHandler.fetch.AdministrationOpen)
                                    {
                                        foreach (ScenarioDestructibles.ProtoDestructible protoD in ScenarioDestructibles.protoDestructibles.Values)
                                        {
                                            foreach (DestructibleBuilding building in protoD.dBuildingRefs)
                                            {
                                                if (dataList.Any(i => i == building.id))
                                                {
                                                    int index = dataList.FindIndex(i => i == building.id);

                                                    if (!building.IsDestroyed)
                                                    {
                                                        building.Demolish();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        List<byte[]> dataBacklog = new List<byte[]>();

                                        dataBacklog.Add(data[v]);
                                        dataBacklog.Add(data[v + 1]);

                                        ScenarioEventHandler.fetch.scenarioBacklog.Add(new KeyValuePair<string, List<byte[]>>("Building", dataBacklog));
                                    }
                                }

                                if (name[0] == "BuildingAlive")
                                {
                                    if (HighLogic.LoadedScene == GameScenes.SPACECENTER && !ScenarioEventHandler.fetch.RnDOpen && !ScenarioEventHandler.fetch.MissionControlOpen && !ScenarioEventHandler.fetch.AdministrationOpen)
                                    {
                                        foreach (ScenarioDestructibles.ProtoDestructible protoD in ScenarioDestructibles.protoDestructibles.Values)
                                        {
                                            foreach (DestructibleBuilding building in protoD.dBuildingRefs)
                                            {
                                                if (dataList.Any(i => i == building.id))
                                                {
                                                    int index = dataList.FindIndex(i => i == building.id);

                                                    if (!building.IsIntact)
                                                    {
                                                        building.Repair();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        List<byte[]> dataBacklog = new List<byte[]>();

                                        dataBacklog.Add(data[v]);
                                        dataBacklog.Add(data[v + 1]);

                                        ScenarioEventHandler.fetch.scenarioBacklog.Add(new KeyValuePair<string, List<byte[]>>("Building", dataBacklog));
                                    }
                                }

                                if (name[0] == "Parts")
                                {
                                    if (!ScenarioEventHandler.fetch.RnDOpen && HighLogic.LoadedScene != GameScenes.EDITOR)
                                    {
                                        List<AvailablePart> allParts = PartLoader.Instance.loadedParts;

                                        for (int i = 0; i < allParts.Count; i++)
                                        {
                                            ProtoTechNode ptNode = ResearchAndDevelopment.Instance.GetTechState(allParts[i].TechRequired);

                                            RDTech.State state;

                                            if (ptNode != null)
                                            {
                                                state = ptNode.state;
                                            }
                                            else
                                            {
                                                state = RDTech.State.Unavailable;
                                            }

                                            if (state == RDTech.State.Unavailable)
                                            {
                                                if (ResearchAndDevelopment.PartTechAvailable(allParts[i]))
                                                {
                                                    ResearchAndDevelopment.RemoveExperimentalPart(allParts[i]);
                                                }
                                            }
                                        }

                                        foreach (string part in dataList)
                                        {
                                            string[] split = part.Split(':');
                                            string partName = split[0].Trim();
                                            string partTech = split[1].Trim();

                                            AvailablePart truePart = PartLoader.getPartInfoByName(partName);

                                            ProtoTechNode ptNode = ResearchAndDevelopment.Instance.GetTechState(partTech);

                                            RDTech.State state;

                                            if (ptNode != null)
                                            {
                                                state = ptNode.state;
                                            }
                                            else
                                            {
                                                state = RDTech.State.Unavailable;
                                            }

                                            if (state != RDTech.State.Unavailable)
                                            {
                                                if (!ResearchAndDevelopment.PartTechAvailable(truePart))
                                                {
                                                    ptNode.partsPurchased.Add(truePart);

                                                    ResearchAndDevelopment.Instance.SetTechState(partTech, ptNode);
                                                }
                                                /*
                                                else
                                                {
                                                    if (ResearchAndDevelopment.IsExperimentalPart(truePart))
                                                    {
                                                        ResearchAndDevelopment.RemoveExperimentalPart(truePart);
                                                        
                                                        ptNode.partsPurchased.Add(truePart);

                                                        ResearchAndDevelopment.Instance.SetTechState(partTech, ptNode);
                                                    }
                                                }
                                                */
                                            }
                                            else
                                            {
                                                if (!ResearchAndDevelopment.PartTechAvailable(truePart))
                                                {
                                                    ResearchAndDevelopment.AddExperimentalPart(truePart);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        List<byte[]> dataBacklog = new List<byte[]>();

                                        dataBacklog.Add(data[v]);
                                        dataBacklog.Add(data[v + 1]);

                                        ScenarioEventHandler.fetch.scenarioBacklog.Add(new KeyValuePair<string, List<byte[]>>("Tech", dataBacklog));//This is a "Tech" because it's needs are same as tech needs
                                    }
                                }

                                if (name[0] == "Upgrades")
                                {
                                    foreach (string upgrade in dataList)
                                    {
                                        string[] split = upgrade.Split(':');
                                        string upgradeName = split[0].Trim();
                                        string upgradeTech = split[1].Trim();

                                        foreach (PartUpgradeHandler.Upgrade partUpgrade in PartUpgradeManager.Handler.GetUpgradesForTech(upgradeTech))
                                        {
                                            if (partUpgrade.name == upgradeName)
                                            {
                                                PartUpgradeManager.Handler.SetUnlocked(partUpgrade.name, true);
                                            }
                                        }
                                    }
                                }

                                if (name[0] == "Progress")
                                {
                                    ConfigNode ProgressCFG = new ConfigNode();

                                    ProgressTracking.Instance.Save(ProgressCFG);

                                    List<string> progressData = SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(ProgressCFG));

                                    int index = 0;
                                    while (index < progressData.Count)
                                    {
                                        if (progressData[index] == "Progress" && progressData[index + 1] == "{")
                                        {
                                            int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(progressData, index + 1);
                                            KeyValuePair<int, int> range = new KeyValuePair<int, int>(index, (matchBracketIdx - index) + 1);

                                            progressData.RemoveRange(range.Key, range.Value);
                                        }
                                        else
                                        {
                                            index++;
                                        }
                                    }

                                    progressData.Add("Progress");
                                    progressData.Add("{");

                                    int looped = 0;
                                    while (looped < dataList.Count)
                                    {
                                        if (dataList[looped].StartsWith("ProgressNode : ") && dataList[looped + 1] == "{")
                                        {
                                            string[] split = dataList[looped].Split(':');
                                            string start = split[0].Trim();
                                            string progressId = split[1].Trim();

                                            int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(dataList, looped + 1);
                                            KeyValuePair<int, int> range = new KeyValuePair<int, int>(looped, (matchBracketIdx - looped) + 1);

                                            if (range.Key + 1 < dataList.Count && range.Value - 1 > 0)
                                            {
                                                progressData.Add(progressId);

                                                progressData.AddRange(dataList.GetRange(range.Key + 1, range.Value - 1));

                                                dataList.RemoveRange(range.Key, range.Value);
                                            }
                                            else
                                            {
                                                looped++;
                                            }
                                        }
                                        else
                                        {
                                            looped++;
                                        }
                                    }

                                    progressData.Add("}");

                                    ProgressCFG = ConfigNodeSerializer.fetch.Deserialize(SyncrioUtil.ByteArraySerializer.Serialize(progressData));

                                    List<ProtoScenarioModule> psmLocked = HighLogic.CurrentGame.scenarios;

                                    int idx = psmLocked.FindIndex(i => i.moduleName == "ProgressTracking");

                                    if (idx != -1)
                                    {
                                        if (ScenarioRunner.GetLoadedModules().Contains(psmLocked[idx].moduleRef))
                                        {
                                            ScenarioRunner.RemoveModule(psmLocked[idx].moduleRef);
                                        }

                                        psmLocked[idx].moduleRef = ScenarioRunner.Instance.AddModule(ProgressCFG);
                                        psmLocked[idx].moduleRef.targetScenes = HighLogic.CurrentGame.scenarios[idx].targetScenes;

                                        HighLogic.CurrentGame.scenarios = psmLocked;
                                    }
                                    else
                                    {
                                        ScenarioModule sm = ScenarioRunner.Instance.AddModule(ProgressCFG);

                                        HighLogic.CurrentGame.AddProtoScenarioModule(sm.GetType(), new GameScenes[3] { GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER });

                                        psmLocked = HighLogic.CurrentGame.scenarios;

                                        idx = psmLocked.FindIndex(i => i.moduleName == "ProgressTracking");

                                        psmLocked[idx].moduleRef = sm;
                                        psmLocked[idx].moduleRef.targetScenes = HighLogic.CurrentGame.scenarios[idx].targetScenes;

                                        HighLogic.CurrentGame.scenarios = psmLocked;
                                    }
                                }

                                if (name[0] == "Tech")
                                {
                                    if (!ScenarioEventHandler.fetch.RnDOpen && HighLogic.LoadedScene != GameScenes.EDITOR && (HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER))
                                    {
                                        int looped = 0;
                                        while (looped < dataList.Count)
                                        {
                                            if (dataList[looped].StartsWith("Tech : ") && dataList[looped + 1] == "TechNode" && dataList[looped + 2] == "{")
                                            {
                                                string[] split = dataList[looped].Split(':');
                                                string start = split[0].Trim();
                                                string tech = split[1].Trim();

                                                int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(dataList, looped + 2);
                                                KeyValuePair<int, int> range = new KeyValuePair<int, int>(looped, (matchBracketIdx - looped) + 1);

                                                if (range.Key + 3 < dataList.Count && range.Value - 4 > 0)
                                                {
                                                    ConfigNode techCFG = ConfigNodeSerializer.fetch.Deserialize(SyncrioUtil.ByteArraySerializer.Serialize(dataList.GetRange(range.Key + 3, range.Value - 4)));

                                                    ProtoTechNode ptNode = ResearchAndDevelopment.Instance.GetTechState(tech);

                                                    if (ptNode == null)
                                                    {
                                                        List<AvailablePart> parts = new List<AvailablePart>();

                                                        int loop2 = matchBracketIdx + 1;
                                                        while (dataList[loop2] != "TechParts" && !dataList[loop2].StartsWith("Tech : ") && loop2 < dataList.Count)
                                                        {
                                                            loop2++;
                                                        }

                                                        if (dataList[loop2] == "TechParts" && dataList[loop2 + 1] == "{")
                                                        {
                                                            int matchBracketIdx2 = SyncrioUtil.DataCleaner.FindMatchingBracket(dataList, loop2 + 1);
                                                            KeyValuePair<int, int> range2 = new KeyValuePair<int, int>(loop2, (matchBracketIdx2 - loop2) + 1);

                                                            int loop3 = loop2 + 2;
                                                            while (loop3 < matchBracketIdx2 && loop3 < dataList.Count)
                                                            {
                                                                if (dataList[loop3].StartsWith("Part : "))
                                                                {
                                                                    string[] split2 = dataList[loop3].Split(':');
                                                                    string start2 = split2[0].Trim();
                                                                    string partName = split2[1].Trim();

                                                                    AvailablePart part = PartLoader.getPartInfoByName(partName);

                                                                    if (ResearchAndDevelopment.IsExperimentalPart(part))
                                                                    {
                                                                        ResearchAndDevelopment.RemoveExperimentalPart(part);
                                                                    }

                                                                    parts.Add(part);
                                                                }

                                                                loop3++;
                                                            }
                                                            dataList.RemoveRange(range2.Key, range2.Value);
                                                        }

                                                        ptNode = new ProtoTechNode();

                                                        RDTech RnD = new RDTech();

                                                        RnD.Load(techCFG);

                                                        ptNode.partsPurchased = parts;

                                                        ptNode.scienceCost = RnD.scienceCost;

                                                        ptNode.techID = RnD.techID;

                                                        ptNode.state = RDTech.State.Available;

                                                        ResearchAndDevelopment.Instance.UnlockProtoTechNode(ptNode);

                                                        ResearchAndDevelopment.Instance.SetTechState(tech, ptNode);
                                                    }

                                                    dataList.RemoveRange(range.Key, range.Value);
                                                }
                                                else
                                                {
                                                    looped++;
                                                }
                                            }
                                            else
                                            {
                                                looped++;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        List<byte[]> dataBacklog = new List<byte[]>();

                                        dataBacklog.Add(data[v]);
                                        dataBacklog.Add(data[v + 1]);

                                        ScenarioEventHandler.fetch.scenarioBacklog.Add(new KeyValuePair<string, List<byte[]>>("Tech", dataBacklog));
                                    }
                                }

                                if (name[0] == "ScienceRecieved")
                                {
                                    if (!ScenarioEventHandler.fetch.RnDOpen && (HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.EDITOR))
                                    {
                                        ConfigNode RnDCFG = new ConfigNode();

                                        ResearchAndDevelopment.Instance.Save(RnDCFG);

                                        List<string> sciData = SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(RnDCFG));

                                        int index = 0;
                                        while (index < sciData.Count)
                                        {
                                            if (sciData[index] == "Science" && sciData[index + 1] == "{")
                                            {
                                                int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(sciData, index + 1);
                                                KeyValuePair<int, int> range = new KeyValuePair<int, int>(index, (matchBracketIdx - index) + 1);

                                                sciData.RemoveRange(range.Key, range.Value);
                                            }
                                            else
                                            {
                                                index++;
                                            }
                                        }

                                        int looped = 0;
                                        while (looped < dataList.Count)
                                        {
                                            if (dataList[looped].StartsWith("Sci : ") && dataList[looped + 1].StartsWith("Value : ") && dataList[looped + 2] == "SciNode" && dataList[looped + 3] == "{")
                                            {
                                                string[] split = dataList[looped].Split(':');
                                                string start = split[0].Trim();
                                                string sciID = split[1].Trim();

                                                string[] split2 = dataList[looped + 1].Split(':');
                                                string start2 = split2[0].Trim();
                                                float dataAmount = Convert.ToSingle(split2[1].Trim(), Client.english);

                                                int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(dataList, looped + 3);
                                                KeyValuePair<int, int> range = new KeyValuePair<int, int>(looped, (matchBracketIdx - looped) + 1);

                                                if (range.Key + 4 < dataList.Count && range.Value - 5 > 0)
                                                {
                                                    sciData.Add("Science");
                                                    sciData.Add("{");

                                                    sciData.AddRange(dataList.GetRange(range.Key + 4, range.Value - 5));

                                                    sciData.Add("}");

                                                    dataList.RemoveRange(range.Key, range.Value);
                                                }
                                                else
                                                {
                                                    looped++;
                                                }
                                            }
                                            else
                                            {
                                                looped++;
                                            }
                                        }

                                        RnDCFG = ConfigNodeSerializer.fetch.Deserialize(SyncrioUtil.ByteArraySerializer.Serialize(sciData));

                                        List<ProtoScenarioModule> psmLocked = HighLogic.CurrentGame.scenarios;

                                        int idx = psmLocked.FindIndex(i => i.moduleName == "ResearchAndDevelopment");

                                        if (idx != -1)
                                        {
                                            if (ScenarioRunner.GetLoadedModules().Contains(psmLocked[idx].moduleRef))
                                            {
                                                ScenarioRunner.RemoveModule(psmLocked[idx].moduleRef);
                                            }

                                            psmLocked[idx].moduleRef = ScenarioRunner.Instance.AddModule(RnDCFG);
                                            psmLocked[idx].moduleRef.targetScenes = HighLogic.CurrentGame.scenarios[idx].targetScenes;

                                            HighLogic.CurrentGame.scenarios = psmLocked;
                                        }
                                        else
                                        {
                                            ScenarioModule sm = ScenarioRunner.Instance.AddModule(RnDCFG);

                                            HighLogic.CurrentGame.AddProtoScenarioModule(sm.GetType(), new GameScenes[4] { GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER, GameScenes.EDITOR });

                                            psmLocked = HighLogic.CurrentGame.scenarios;

                                            idx = psmLocked.FindIndex(i => i.moduleName == "ResearchAndDevelopment");

                                            psmLocked[idx].moduleRef = sm;
                                            psmLocked[idx].moduleRef.targetScenes = HighLogic.CurrentGame.scenarios[idx].targetScenes;

                                            HighLogic.CurrentGame.scenarios = psmLocked;
                                        }
                                    }
                                    else
                                    {
                                        List<byte[]> dataBacklog = new List<byte[]>();

                                        dataBacklog.Add(data[v]);
                                        dataBacklog.Add(data[v + 1]);

                                        ScenarioEventHandler.fetch.scenarioBacklog.Add(new KeyValuePair<string, List<byte[]>>("Science", dataBacklog));
                                    }
                                }

                                if (name[0] == "ResourceScenario")
                                {
                                    ConfigNode ResourceCfg = ConfigNodeSerializer.fetch.Deserialize(SyncrioUtil.ByteArraySerializer.Serialize(dataList));

                                    ResourceScenario.Instance.Load(ResourceCfg);

                                    ScenarioEventHandler.fetch.lastResourceScenarioModule = SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(ResourceCfg));
                                }

                                if (name[0] == "StrategySystem" && HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                                {
                                    if (!ScenarioEventHandler.fetch.AdministrationOpen)
                                    {
                                        ConfigNode StrategyCfg = ConfigNodeSerializer.fetch.Deserialize(SyncrioUtil.ByteArraySerializer.Serialize(dataList));

                                        Strategies.StrategySystem.Instance.Load(StrategyCfg);

                                        ScenarioEventHandler.fetch.lastStrategySystemModule = SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(StrategyCfg));
                                    }
                                    else
                                    {
                                        List<byte[]> dataBacklog = new List<byte[]>();

                                        dataBacklog.Add(data[v]);
                                        dataBacklog.Add(data[v + 1]);

                                        ScenarioEventHandler.fetch.scenarioBacklog.Add(new KeyValuePair<string, List<byte[]>>("Strategy", dataBacklog));
                                    }
                                }

                                if (name[0] == "Experiments")
                                {
                                    List<ConfigNode> subjectNodes = new List<ConfigNode>();
                                    
                                    int looped = 0;
                                    while (looped < dataList.Count)
                                    {
                                        if (dataList[looped].StartsWith("Experiment : ") && dataList[looped + 1] == "ExpNode" && dataList[looped + 2] == "{")
                                        {
                                            int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(dataList, looped + 2);

                                            List<string> subjectNodeRaw = dataList.GetRange(looped + 3, (matchBracketIdx - looped) - 3);

                                            subjectNodes.Add(ConfigNodeSerializer.fetch.Deserialize(SyncrioUtil.ByteArraySerializer.Serialize(subjectNodeRaw)));
                                            looped++;
                                        }
                                        else
                                        {
                                            looped++;
                                        }
                                    }
                                    List<ScienceSubject> subjects = new List<ScienceSubject>();

                                    foreach (ConfigNode subjectNode in subjectNodes)
                                    {
                                        subjects.Add(new ScienceSubject(subjectNode));
                                    }
                                    foreach (ScienceSubject subj in subjects)
                                    {
                                        getScienceSubjectThunk(ResearchAndDevelopment.Instance, subj);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                SyncrioLog.Debug("Error handling: " + name[0] + " data, error: " + e);
                            }
                        }

                        isSyncing = false;
                    }
                    else
                    {
                        ScenarioEventHandler.fetch.scenarioBacklog.Add(new KeyValuePair<string, List<byte[]>>("Loading", data));
                    }
                }
            }
        }

        public static void Reset()
        {
            lock (Client.eventLock)
            {
                if (singleton != null)
                {
                    singleton.workerEnabled = false;
                }
                singleton = new ScenarioWorker();
            }
        }
    }
}

