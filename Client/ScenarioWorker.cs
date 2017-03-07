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
        private object scenarioSyncLock = new object();
        private Dictionary<string,string> checkData = new Dictionary<string, string>();
        public bool nonGroupScenarios;
        public bool canResetScenario;
        public List<byte[]> baseData = new List<byte[]>();
        //ScenarioType list to check.
        private Dictionary<string, Type> allScenarioTypesInAssemblies;
        //System.Reflection hackiness for loading kerbals into the crew roster:
        private delegate bool AddCrewMemberToRosterDelegate(ProtoCrewMember pcm);

        private AddCrewMemberToRosterDelegate AddCrewMemberToRoster;

        public static ScenarioWorker fetch
        {
            get
            {
                return singleton;
            }
        }

        private void LoadScenarioTypes()
        {
            allScenarioTypesInAssemblies = new Dictionary<string, Type>();
            foreach (AssemblyLoader.LoadedAssembly something in AssemblyLoader.loadedAssemblies)
            {
                foreach (Type scenarioType in something.assembly.GetTypes())
                {
                    if (scenarioType.IsSubclassOf(typeof(ScenarioModule)))
                    {
                        if (!allScenarioTypesInAssemblies.ContainsKey(scenarioType.Name))
                        {
                            allScenarioTypesInAssemblies.Add(scenarioType.Name, scenarioType);
                        }
                    }
                }
            }
        }

        private bool IsScenarioModuleAllowed(string scenarioName)
        {
            if (scenarioName == null)
            {
                return false;
            }
            if (allScenarioTypesInAssemblies == null)
            {
                //Load type dictionary on first use
                LoadScenarioTypes();
            }
            if (!allScenarioTypesInAssemblies.ContainsKey(scenarioName))
            {
                //Module missing
                return false;
            }
            Type scenarioType = allScenarioTypesInAssemblies[scenarioName];
            KSPScenario[] scenarioAttributes = (KSPScenario[])scenarioType.GetCustomAttributes(typeof(KSPScenario), true);
            if (scenarioAttributes.Length > 0)
            {
                KSPScenario attribute = scenarioAttributes[0];
                bool protoAllowed = false;
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                {
                    protoAllowed = protoAllowed || attribute.HasCreateOption(ScenarioCreationOptions.AddToExistingCareerGames);
                    protoAllowed = protoAllowed || attribute.HasCreateOption(ScenarioCreationOptions.AddToNewCareerGames);
                }
                if (HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
                {
                    protoAllowed = protoAllowed || attribute.HasCreateOption(ScenarioCreationOptions.AddToExistingScienceSandboxGames);
                    protoAllowed = protoAllowed || attribute.HasCreateOption(ScenarioCreationOptions.AddToNewScienceSandboxGames);
                }
                if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX)
                {
                    protoAllowed = protoAllowed || attribute.HasCreateOption(ScenarioCreationOptions.AddToExistingSandboxGames);
                    protoAllowed = protoAllowed || attribute.HasCreateOption(ScenarioCreationOptions.AddToNewSandboxGames);
                }
                return protoAllowed;
            }
            //Scenario is not marked with KSPScenario - let's load it anyway.
            return true;
        }

        /*
        public void LoadScenarioDataIntoGame()
        {
            while (scenarioQueue.Count > 0)
            {
                ScenarioEntry scenarioEntry = scenarioQueue.Dequeue();
                if (scenarioEntry.scenarioName == "ContractSystem")
                {
                    SpawnStrandedKerbalsForRescueMissions(scenarioEntry.scenarioNode);
                    CreateMissingTourists(scenarioEntry.scenarioNode);
                }
                if (scenarioEntry.scenarioName == "ProgressTracking")
                {
                    CreateMissingKerbalsInProgressTrackingSoTheGameDoesntBugOut(scenarioEntry.scenarioNode);
                }
                CheckForBlankSceneSoTheGameDoesntBugOut(scenarioEntry);
                ProtoScenarioModule psm = new ProtoScenarioModule(scenarioEntry.scenarioNode);
                if (psm != null)
                {
                    if (IsScenarioModuleAllowed(psm.moduleName))
                    {
                        SyncrioLog.Debug("Loading " + psm.moduleName + " scenario data");
                        HighLogic.CurrentGame.scenarios.Add(psm);
                    }
                    else
                    {
                        SyncrioLog.Debug("Skipping " + psm.moduleName + " scenario data in " + Client.fetch.gameMode + " mode");
                    }
                }
            }
        }
        */

        private void CreateMissingTourists(ConfigNode contractSystemNode)
        {
            ConfigNode contractsNode = contractSystemNode.GetNode("CONTRACTS");
            foreach (ConfigNode contractNode in contractsNode.GetNodes("CONTRACT"))
            {
                if (contractNode.GetValue("type") == "TourismContract" && contractNode.GetValue("state") == "Active")
                {
                    foreach (ConfigNode paramNode in contractNode.GetNodes("PARAM"))
                    {
                        foreach (string kerbalName in paramNode.GetValues("kerbalName"))
                        {
                            SyncrioLog.Debug("Spawning missing tourist (" + kerbalName + ") for active tourism contract");
                            ProtoCrewMember pcm = HighLogic.CurrentGame.CrewRoster.GetNewKerbal(ProtoCrewMember.KerbalType.Tourist);
                            pcm.ChangeName(kerbalName);
                        }
                    }
                }
            }
        }

        //Defends against bug #172
        private void SpawnStrandedKerbalsForRescueMissions(ConfigNode contractSystemNode)
        {
            ConfigNode contractsNode = contractSystemNode.GetNode("CONTRACTS");
            foreach (ConfigNode contractNode in contractsNode.GetNodes("CONTRACT"))
            {
                if ((contractNode.GetValue("type") == "RescueKerbal") && (contractNode.GetValue("state") == "Offered"))
                {
                    string kerbalName = contractNode.GetValue("kerbalName");
                    if (!HighLogic.CurrentGame.CrewRoster.Exists(kerbalName))
                    {
                        SyncrioLog.Debug("Spawning missing kerbal (" + kerbalName + ") for offered KerbalRescue contract");
                        ProtoCrewMember pcm = HighLogic.CurrentGame.CrewRoster.GetNewKerbal(ProtoCrewMember.KerbalType.Unowned);
                        pcm.ChangeName(kerbalName);
                    }
                }
                if ((contractNode.GetValue("type") == "RescueKerbal") && (contractNode.GetValue("state") == "Active"))
                {

                    string kerbalName = contractNode.GetValue("kerbalName");
                    SyncrioLog.Debug("Spawning stranded kerbal (" + kerbalName + ") for active KerbalRescue contract");
                    int bodyID = Int32.Parse(contractNode.GetValue("body"));
                    if (!HighLogic.CurrentGame.CrewRoster.Exists(kerbalName))
                    {
                        GenerateStrandedKerbal(bodyID, kerbalName);
                    }
                }
            }
        }

        private void GenerateStrandedKerbal(int bodyID, string kerbalName)
        {
            //Add kerbal to crew roster.
            SyncrioLog.Debug("Spawning missing kerbal, name: " + kerbalName);
            ProtoCrewMember pcm = HighLogic.CurrentGame.CrewRoster.GetNewKerbal(ProtoCrewMember.KerbalType.Unowned);
            pcm.ChangeName(kerbalName);
            pcm.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
            //Create protovessel
            uint newPartID = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
            CelestialBody contractBody = FlightGlobals.Bodies[bodyID];
            //Atmo: 10km above atmo, to half the planets radius out.
            //Non-atmo: 30km above ground, to half the planets radius out.
            double minAltitude = FinePrint.Utilities.CelestialUtilities.GetMinimumOrbitalDistance(contractBody, 1.1f);
            double maxAltitude = minAltitude + contractBody.Radius * 0.5;
            Orbit strandedOrbit = Orbit.CreateRandomOrbitAround(FlightGlobals.Bodies[bodyID], minAltitude, maxAltitude);
            ConfigNode[] kerbalPartNode = new ConfigNode[1];
            ProtoCrewMember[] partCrew = new ProtoCrewMember[1];
            partCrew[0] = pcm;
            kerbalPartNode[0] = ProtoVessel.CreatePartNode("kerbalEVA", newPartID, partCrew);
            ConfigNode protoVesselNode = ProtoVessel.CreateVesselNode(kerbalName, VesselType.EVA, strandedOrbit, 0, kerbalPartNode);
            ConfigNode discoveryNode = ProtoVessel.CreateDiscoveryNode(DiscoveryLevels.Unowned, UntrackedObjectClass.A, double.PositiveInfinity, double.PositiveInfinity);
            ProtoVessel protoVessel = new ProtoVessel(protoVesselNode, HighLogic.CurrentGame);
            protoVessel.discoveryInfo = discoveryNode;
            //It's not supposed to be infinite, but you're crazy if you think I'm going to decipher the values field of the rescue node.
            HighLogic.CurrentGame.flightState.protoVessels.Add(protoVessel);
        }
        //Defends against bug #172
        private void CreateMissingKerbalsInProgressTrackingSoTheGameDoesntBugOut(ConfigNode progressTrackingNode)
        {
            foreach (ConfigNode possibleNode in progressTrackingNode.nodes)
            {
                //Recursion (noun): See Recursion.
                CreateMissingKerbalsInProgressTrackingSoTheGameDoesntBugOut(possibleNode);
            }
            //The kerbals are kept in a ConfigNode named 'crew', with 'crews' as a comma space delimited array of names.
            if (progressTrackingNode.name == "crew")
            {
                string kerbalNames = progressTrackingNode.GetValue("crews");
                if (!String.IsNullOrEmpty(kerbalNames))
                {
                    string[] kerbalNamesSplit = kerbalNames.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string kerbalName in kerbalNamesSplit)
                    {
                        if (!HighLogic.CurrentGame.CrewRoster.Exists(kerbalName))
                        {
                            if (AddCrewMemberToRoster == null)
                            {
                                MethodInfo addMemberToCrewRosterMethod = typeof(KerbalRoster).GetMethod("AddCrewMember", BindingFlags.Public | BindingFlags.Instance);
                                AddCrewMemberToRoster = (AddCrewMemberToRosterDelegate)Delegate.CreateDelegate(typeof(AddCrewMemberToRosterDelegate), HighLogic.CurrentGame.CrewRoster, addMemberToCrewRosterMethod);
                            }
                            if (AddCrewMemberToRoster == null)
                            {
                                throw new Exception("Failed to initialize AddCrewMemberToRoster for #172 ProgressTracking fix.");
                            }
                            SyncrioLog.Debug("Generating missing kerbal from ProgressTracking: " + kerbalName);
                            ProtoCrewMember pcm = CrewGenerator.RandomCrewMemberPrototype(ProtoCrewMember.KerbalType.Crew);
                            pcm.ChangeName(kerbalName);
                            AddCrewMemberToRoster(pcm);
                            //Also send it off to the server
                            VesselWorker.fetch.SendKerbalIfDifferent(pcm);
                        }
                    }
                }
            }
        }

        //If the scene field is blank, KSP will throw an error while starting the game, meaning players will be unable to join the server.
        /*
        private void CheckForBlankSceneSoTheGameDoesntBugOut(ScenarioEntry scenarioEntry)
        {
            if (scenarioEntry.scenarioNode.GetValue("scene") == string.Empty)
            {
                string nodeName = scenarioEntry.scenarioName;
                ScreenMessages.PostScreenMessage(nodeName + " is badly behaved!");
                SyncrioLog.Debug(nodeName + " is badly behaved!");
                scenarioEntry.scenarioNode.SetValue("scene", "7, 8, 5, 6, 9");
            }
        }
        */

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

        public void LoadBaseScenarioData()
        {
            if (baseData.Count > 0)
            {
                LoadScenarioData(baseData);
                baseData = new List<byte[]>();
            }
        }

        public void LoadScenarioData(List<byte[]> data)
        {
            lock (scenarioLock)
            {
                if (Client.fetch.gameRunning && HighLogic.LoadedScene != GameScenes.LOADING)
                {
                    isSyncing = true;
                    ScenarioEventHandler.fetch.startCooldown = true;
                    ScenarioEventHandler.fetch.cooldown = true;

                    for (int v = 0; v < data.Count; v += 2)
                    {
                        List<string> name = SyncrioUtil.ByteArraySerializer.Deserialize(data[v]);
                        List<string> dataList = SyncrioUtil.ByteArraySerializer.Deserialize(data[v + 1]);

                        try
                        {
                            if (name[0] == "Contracts")
                            {
                                List<Contracts.Contract> realContracts = new List<Contracts.Contract>();
                                List<Contracts.Contract> completeContracts = new List<Contracts.Contract>();

                                int looped = 0;
                                while (looped < dataList.Count)
                                {
                                    if (dataList[looped] == "ContractNode")
                                    {
                                        int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(dataList, looped + 1);
                                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(looped, (matchBracketIdx - looped));

                                        if (range.Key + 2 < dataList.Count && range.Value - 3 > 0)
                                        {
                                            List<string> contract = dataList.GetRange(range.Key + 2, range.Value - 3);

                                            dataList.RemoveRange(range.Key, range.Value);

                                            ConfigNode contractCFG = ConfigNodeSerializer.fetch.Deserialize(SyncrioUtil.ByteArraySerializer.Serialize(contract));

                                            Contracts.Contract trueContract = new Contracts.Contract();

                                            Contracts.Contract.Load(trueContract, contractCFG);

                                            if (!trueContract.Complete())
                                            {
                                                realContracts.Add(trueContract);
                                            }
                                            else
                                            {
                                                completeContracts.Add(trueContract);
                                            }
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

                                Contracts.ContractSystem.Instance.ClearContractsCurrent();
                                Contracts.ContractSystem.Instance.ClearContractsFinished();

                                Contracts.ContractSystem.Instance.Contracts.AddRange(realContracts);
                                Contracts.ContractSystem.Instance.ContractsFinished.AddRange(completeContracts);
                            }

                            if (name[0] == "Waypoints")
                            {
                                
                            }

                            if (name[0] == "Currency")
                            {
                                if (Client.fetch.gameMode == GameMode.CAREER)
                                {
                                    double fundsAmount = Convert.ToDouble(dataList[0]);
                                    double fundsAmountDiff = fundsAmount - Funding.Instance.Funds;
                                    Funding.Instance.AddFunds(fundsAmountDiff, TransactionReasons.None);

                                    float repAmount = Convert.ToSingle(dataList[1]);
                                    float repAmountDiff = repAmount - Reputation.Instance.reputation;
                                    Reputation.Instance.AddReputation(repAmountDiff, TransactionReasons.None);

                                    float sciAmount = Convert.ToSingle(dataList[2]);
                                    float sciAmountDiff = sciAmount - ResearchAndDevelopment.Instance.Science;
                                    ResearchAndDevelopment.Instance.AddScience(sciAmountDiff, TransactionReasons.None);
                                }
                                else
                                {
                                    if (Client.fetch.gameMode == GameMode.SCIENCE)
                                    {
                                        float sciAmount = Convert.ToSingle(dataList[2]);
                                        float sciAmountDiff = sciAmount - ResearchAndDevelopment.Instance.Science;
                                        ResearchAndDevelopment.Instance.AddScience(sciAmountDiff, TransactionReasons.None);
                                    }
                                }
                            }

                            if (name[0] == "BuildingLevel")
                            {
                                foreach (ScenarioUpgradeableFacilities.ProtoUpgradeable proto in ScenarioUpgradeableFacilities.protoUpgradeables.Values)
                                {
                                    foreach (Upgradeables.UpgradeableFacility facility in proto.facilityRefs)
                                    {
                                        if (dataList.Any(i => i.StartsWith(facility.id)))
                                        {
                                            int index = dataList.FindIndex(i => i.StartsWith(facility.id));

                                            string[] subData = dataList[index].Split('=');

                                            string id = subData[0].Trim();
                                            string level = subData[1].Trim();

                                            facility.SetLevel(Convert.ToInt32(level));
                                        }
                                    }
                                }
                            }

                            if (name[0] == "BuildingDead")
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

                            if (name[0] == "BuildingAlive")
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

                            if (name[0] == "Parts")
                            {
                                foreach (string part in dataList)
                                {
                                    string[] split = part.Split(':');
                                    string partName = split[0].Trim();
                                    string partTech = split[1].Trim();

                                    AvailablePart truePart = PartLoader.getPartInfoByName(partName);

                                    if (ResearchAndDevelopment.GetTechnologyState(partTech) != RDTech.State.Unavailable)
                                    {
                                        if (!ResearchAndDevelopment.PartTechAvailable(truePart))
                                        {
                                            ProtoTechNode ptNode = ResearchAndDevelopment.Instance.GetTechState(partTech);

                                            ptNode.partsPurchased.Add(truePart);

                                            ResearchAndDevelopment.Instance.SetTechState(partTech, ptNode);
                                        }
                                        else
                                        {
                                            if (ResearchAndDevelopment.IsExperimentalPart(truePart))
                                            {
                                                ResearchAndDevelopment.RemoveExperimentalPart(truePart);

                                                ProtoTechNode ptNode = ResearchAndDevelopment.Instance.GetTechState(partTech);

                                                ptNode.partsPurchased.Add(truePart);

                                                ResearchAndDevelopment.Instance.SetTechState(partTech, ptNode);
                                            }
                                        }
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
                                int looped = 0;
                                while (looped < dataList.Count)
                                {
                                    if (dataList[looped].StartsWith("ProgressNode : "))
                                    {
                                        string[] split = dataList[looped].Split(':');
                                        string start = split[0].Trim();
                                        string progressId = split[1].Trim();

                                        int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(dataList, looped + 1);
                                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(looped, (matchBracketIdx - looped));

                                        if (range.Key + 2 < dataList.Count && range.Value - 3 > 0)
                                        {
                                            List<string> progress = dataList.GetRange(range.Key + 2, range.Value - 3);

                                            dataList.RemoveRange(range.Key, range.Value);

                                            ConfigNode progressCFG = ConfigNodeSerializer.fetch.Deserialize(SyncrioUtil.ByteArraySerializer.Serialize(progress));

                                            ProgressNode target = ProgressTracking.Instance.FindNode(progressId);

                                            if (target != null)
                                            {
                                                target.Load(progressCFG);
                                            }
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

                            if (name[0] == "Tech")
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
                                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(looped, (matchBracketIdx - looped));

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
                                                    KeyValuePair<int, int> range2 = new KeyValuePair<int, int>(loop2, (matchBracketIdx2 - loop2));

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

                                                ResearchAndDevelopment.RefreshTechTreeUI();
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

                            if (name[0] == "ScienceRecieved")
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
                                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(index, (matchBracketIdx - index));

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
                                        float dataAmount = Convert.ToSingle(split2[1].Trim());

                                        int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(dataList, looped + 3);
                                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(looped, (matchBracketIdx - looped));

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

                                int idx = HighLogic.CurrentGame.scenarios.FindIndex(i => i.moduleName == "ResearchAndDevelopment");

                                if (ScenarioRunner.GetLoadedModules().Contains(HighLogic.CurrentGame.scenarios[idx].moduleRef))
                                {
                                    ScenarioRunner.RemoveModule(HighLogic.CurrentGame.scenarios[idx].moduleRef);
                                }

                                HighLogic.CurrentGame.scenarios[idx].moduleRef = ScenarioRunner.Instance.AddModule(RnDCFG);
                                HighLogic.CurrentGame.scenarios[idx].moduleRef.targetScenes = HighLogic.CurrentGame.scenarios[idx].targetScenes;

                                //ResearchAndDevelopment.Instance.Load(RnDCFG);
                            }
                        }
                        catch (Exception e)
                        {
                            SyncrioLog.Debug("Error handling: " + name[0] + " data, error: " + e);
                        }
                    }

                    isSyncing = false;
                }
            }
        }
        
        public void ResetScenatio(bool isInGroup)
        {
            if (isInGroup)
            {
                string groupName = GroupSystem.playerGroupName;
                if (!string.IsNullOrEmpty(groupName))
                {
                    byte[] messageBytes;
                    ClientMessage newMessage = new ClientMessage();
                    newMessage.handled = false;
                    newMessage.type = ClientMessageType.RESET_SCENARIO;
                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(isInGroup);
                        mw.Write<string>(groupName);
                        messageBytes = mw.GetMessageBytes();
                    }
                    newMessage.data = messageBytes;
                    NetworkWorker.fetch.SendScenarioCommand(newMessage, false);
                }
            }
            else
            {
                byte[] messageBytes;
                ClientMessage newMessage = new ClientMessage();
                newMessage.handled = false;
                newMessage.type = ClientMessageType.RESET_SCENARIO;
                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<bool>(isInGroup);
                    messageBytes = mw.GetMessageBytes();
                }
                newMessage.data = messageBytes;
                NetworkWorker.fetch.SendScenarioCommand(newMessage, false);
            }
        }
        
        /*
        public void QueueScenarioData(string scenarioName, ConfigNode scenarioData)
        {
            ScenarioEntry entry = new ScenarioEntry();
            entry.scenarioName = scenarioName;
            entry.scenarioNode = scenarioData;
            if (Client.fetch.gameRunning)
            {
                KeyValuePair<string, byte[]> kvp = new KeyValuePair<string, byte[]>(scenarioName, ConfigNodeSerializer.fetch.Serialize(scenarioData));
                activeScenarioQueue.Add(kvp);
            }
            else
            {
                scenarioQueue.Enqueue(entry);
            }
        }
        */

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

    /*
    public class ScenarioEntry
    {
        public string scenarioName;
        public ConfigNode scenarioNode;
    }
    */
}

