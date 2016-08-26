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
        private object scenarioLock = new object();
        private Dictionary<string,string> checkData = new Dictionary<string, string>();
        private Queue<ScenarioEntry> scenarioQueue = new Queue<ScenarioEntry>();
        private bool blockScenarioDataSends = false;
        private float lastScenarioSendTime = 0f;
        private const float SEND_SCENARIO_DATA_INTERVAL = 30f;
        public bool autoSendScenarios;
        public bool nonGroupScenarios;
        public bool canResetScenario;
        public string[] currentScenarioName;
        public byte[][] currentScenarioData;
        public List<string> scenarioFundsHistory = new List<string>();
        public List<string> scenarioRepHistory = new List<string>();
        public List<string> scenarioSciHistory = new List<string>();
        private string scenarioFundsHistoryFile = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Path.Combine(KSPUtil.ApplicationRootPath, "GameData"), "Syncrio"), "Plugins"), "Data"), "SVH_funds.txt");
        private string scenarioRepHistoryFile = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Path.Combine(KSPUtil.ApplicationRootPath, "GameData"), "Syncrio"), "Plugins"), "Data"), "SVH_rep.txt");
        private string scenarioSciHistoryFile = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Path.Combine(KSPUtil.ApplicationRootPath, "GameData"), "Syncrio"), "Plugins"), "Data"), "SVH_sci.txt");
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

        private void Update()
        {
            if (workerEnabled && !blockScenarioDataSends && autoSendScenarios)
            {
                if ((UnityEngine.Time.realtimeSinceStartup - lastScenarioSendTime) > SEND_SCENARIO_DATA_INTERVAL)
                {
                    lastScenarioSendTime = UnityEngine.Time.realtimeSinceStartup;
                    scenarioSync(GroupSystem.playerGroupAssigned, true, false);
                }
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
            //Blacklist asteroid module from every game mode
            if (scenarioName == "ScenarioDiscoverableObjects")
            {
                //We hijack this and enable / disable it if we need to.
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
                            pcm.name = kerbalName;
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
                        pcm.name = kerbalName;
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
            pcm.name = kerbalName;
            pcm.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
            //Create protovessel
            uint newPartID = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
            CelestialBody contractBody = FlightGlobals.Bodies[bodyID];
            //Atmo: 10km above atmo, to half the planets radius out.
            //Non-atmo: 30km above ground, to half the planets radius out.
            double minAltitude = FinePrint.Utilities.CelestialUtilities.GetMinimumOrbitalAltitude(contractBody, 1.1f);
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
                                MethodInfo addMemberToCrewRosterMethod = typeof(KerbalRoster).GetMethod("AddCrewMember", BindingFlags.NonPublic | BindingFlags.Instance);
                                AddCrewMemberToRoster = (AddCrewMemberToRosterDelegate)Delegate.CreateDelegate(typeof(AddCrewMemberToRosterDelegate), HighLogic.CurrentGame.CrewRoster, addMemberToCrewRosterMethod);
                            }
                            if (AddCrewMemberToRoster == null)
                            {
                                throw new Exception("Failed to initialize AddCrewMemberToRoster for #172 ProgressTracking fix.");
                            }
                            SyncrioLog.Debug("Generating missing kerbal from ProgressTracking: " + kerbalName);
                            ProtoCrewMember pcm = CrewGenerator.RandomCrewMemberPrototype(ProtoCrewMember.KerbalType.Crew);
                            pcm.name = kerbalName;
                            AddCrewMemberToRoster(pcm);
                            //Also send it off to the server
                            VesselWorker.fetch.SendKerbalIfDifferent(pcm);
                        }
                    }
                }
            }
        }

        //If the scene field is blank, KSP will throw an error while starting the game, meaning players will be unable to join the server.
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

        public void UpgradeTheAstronautComplexSoTheGameDoesntBugOut()
        {
            ProtoScenarioModule sm = HighLogic.CurrentGame.scenarios.Find(psm => psm.moduleName == "ScenarioUpgradeableFacilities");
            if (sm != null)
            {
                if (ScenarioUpgradeableFacilities.protoUpgradeables.ContainsKey("SpaceCenter/AstronautComplex"))
                {
                    foreach (Upgradeables.UpgradeableFacility uf in ScenarioUpgradeableFacilities.protoUpgradeables["SpaceCenter/AstronautComplex"].facilityRefs)
                    {
                        SyncrioLog.Debug("Setting astronaut complex to max level");
                        uf.SetLevel(uf.MaxLevel);
                    }
                }
            }
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

        public void LoadScenarioData(ScenarioEntry entry)
        {
            lock (scenarioLock)
            {
                if (!IsScenarioModuleAllowed(entry.scenarioName))
                {
                    SyncrioLog.Debug("Skipped '" + entry.scenarioName + "' scenario data  in " + Client.fetch.gameMode + " mode");
                    return;
                }

                //Load data from Syncrio
                if (entry.scenarioNode == null)
                {
                    SyncrioLog.Debug(entry.scenarioName + " scenario data failed to create a ConfigNode!");
                    blockScenarioDataSends = true;
                    return;
                }

                //Load data into game
                if (DidScenarioChange(entry))
                {
                    bool loaded = false;
                    List<ProtoScenarioModule> psmLocked = HighLogic.CurrentGame.scenarios;
                    for (int i = 0; i < HighLogic.CurrentGame.scenarios.Count; i++)
                    {
                        if (psmLocked[i].moduleName == entry.scenarioName)
                        {
                            SyncrioLog.Debug("Loading existing " + entry.scenarioName + " scenario module");
                            try
                            {
                                if (entry.scenarioName == "Funding")
                                {
                                    double fundsAmount = Convert.ToDouble(entry.scenarioNode.GetValue("funds"));
                                    double fundsAmountDiff = fundsAmount - Funding.Instance.Funds;

                                    Funding.Instance.AddFunds(fundsAmountDiff, TransactionReasons.None);
                                }
                                if (entry.scenarioName == "Reputation")
                                {
                                    float repAmount = Convert.ToSingle(entry.scenarioNode.GetValue("rep"));
                                    float repAmountDiff = repAmount - Reputation.Instance.reputation;

                                    Reputation.Instance.AddReputation(repAmountDiff, TransactionReasons.None);
                                }
                                if (entry.scenarioName == "ResearchAndDevelopment")
                                {
                                    float sciAmount = Convert.ToSingle(entry.scenarioNode.GetValue("sci"));
                                    float sciAmountDiff = sciAmount - ResearchAndDevelopment.Instance.Science;

                                    ResearchAndDevelopment.Instance.AddScience(sciAmountDiff, TransactionReasons.None);
                                }
                                List<ScenarioModule> loadedModules = ScenarioRunner.GetLoadedModules();
                                if (loadedModules.Contains(psmLocked[i].moduleRef))
                                {
                                    ScenarioRunner.RemoveModule(psmLocked[i].moduleRef);
                                }
                                psmLocked[i].moduleRef = ScenarioRunner.fetch.AddModule(entry.scenarioNode);
                                psmLocked[i].moduleRef.targetScenes = psmLocked[i].targetScenes;

                                HighLogic.CurrentGame.Updated();
                            }
                            catch (Exception e)
                            {
                                SyncrioLog.Debug("Error loading " + entry.scenarioName + " scenario module, Exception: " + e);
                                blockScenarioDataSends = true;
                            }
                            loaded = true;
                        }
                        HighLogic.CurrentGame.scenarios = psmLocked;
                    }
                    if (!loaded)
                    {
                        SyncrioLog.Debug("Loading new " + entry.scenarioName + " scenario module");
                        LoadNewScenarioData(entry.scenarioNode);
                    }
                }
            }
        }
        
        public void resetScenatio(bool isInGroup)
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

        public void LoadNewScenarioData(ConfigNode newScenarioData)
        {
            ProtoScenarioModule newModule = new ProtoScenarioModule(newScenarioData);
            try
            {
                HighLogic.CurrentGame.scenarios.Add(newModule);
                newModule.Load(ScenarioRunner.fetch);
            }
            catch
            {
                SyncrioLog.Debug("Error loading scenario data!");
                blockScenarioDataSends = true;
            }
        }

        public void QueueScenarioData(string scenarioName, ConfigNode scenarioData)
        {
            ScenarioEntry entry = new ScenarioEntry();
            entry.scenarioName = scenarioName;
            entry.scenarioNode = scenarioData;
            if (Client.fetch.gameRunning)
            {
                LoadScenarioData(entry);
            }
            else
            {
                scenarioQueue.Enqueue(entry);
            }
        }

        public void scenarioSync(bool isInGroup, bool toServer, bool highPriority)
        {
            string[] scenarioName;
            byte[][] scenarioData;
            if (toServer)
            {
                GetCurrentScenarioModules();
                if (currentScenarioName != null)
                {
                    scenarioName = currentScenarioName;
                }
                else
                {
                    SyncrioLog.Debug("Error during sync scenario: 'currentScenarioName' is null");
                    scenarioName = null;
                }
                if (currentScenarioData != null)
                {
                    scenarioData = currentScenarioData;
                }
                else
                {
                    SyncrioLog.Debug("Error during sync scenario: 'currentScenarioData' is null");
                    scenarioData = null;
                }
            }
            else
            {
                scenarioName = null;
                scenarioData = null;
            }
            if (isInGroup)
            {
                if (scenarioName != null && scenarioData != null)
                {
                    ScenatioVersionHistoryToGroup(scenarioName, scenarioData);
                }
                string groupName = GroupSystem.playerGroupName;
                if (!string.IsNullOrEmpty(groupName))
                {
                    byte[] messageBytes;
                    ClientMessage newMessage = new ClientMessage();
                    newMessage.handled = false;
                    newMessage.type = ClientMessageType.SYNC_SCENARIO_REQUEST;
                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(toServer);
                        mw.Write<bool>(isInGroup);
                        mw.Write<string>(groupName);
                        if (toServer)
                        {
                            mw.Write<string[]>(scenarioName);
                            mw.Write<string[]>(scenarioFundsHistory.ToArray());
                            mw.Write<string[]>(scenarioRepHistory.ToArray());
                            mw.Write<string[]>(scenarioSciHistory.ToArray());
                            foreach (byte[] scenarioBytes in scenarioData)
                            {
                                mw.Write<byte[]>(Compression.CompressIfNeeded(scenarioBytes));
                            }
                        }
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
                newMessage.type = ClientMessageType.SYNC_SCENARIO_REQUEST;
                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<bool>(toServer);
                    mw.Write<bool>(isInGroup);
                    mw.Write<string[]>(scenarioName);
                    foreach (byte[] scenarioBytes in scenarioData)
                    {
                        mw.Write<byte[]>(Compression.CompressIfNeeded(scenarioBytes));
                    }
                    messageBytes = mw.GetMessageBytes();
                }
                newMessage.data = messageBytes;
                NetworkWorker.fetch.SendScenarioCommand(newMessage, false);
            }
        }

        public void GetCurrentScenarioModules()
        {
            List<string> scenarioName = new List<string>();
            List<byte[]> scenarioData = new List<byte[]>();

            foreach (ScenarioModule sm in ScenarioRunner.GetLoadedModules())
            {
                string scenarioType = sm.GetType().Name;
                if (!IsScenarioModuleAllowed(scenarioType))
                {
                    continue;
                }
                ConfigNode scenarioNode = new ConfigNode();
                sm.Save(scenarioNode);
                byte[] scenarioBytes = ConfigNodeSerializer.fetch.Serialize(scenarioNode);
                string scenarioHash = Common.CalculateSHA256Hash(scenarioBytes);
                if (scenarioBytes.Length == 0)
                {
                    SyncrioLog.Debug("Error writing scenario data for " + scenarioType);
                    continue;
                }
                if (checkData.ContainsKey(scenarioType) ? (checkData[scenarioType] == scenarioHash) : false)
                {
                    //Data is the same since last time - Skip it.
                    continue;
                }
                else
                {
                    checkData[scenarioType] = scenarioHash;
                }
                if (scenarioBytes != null)
                {
                    scenarioName.Add(scenarioType);
                    scenarioData.Add(scenarioBytes);
                }
            }
            currentScenarioName = scenarioName.ToArray();
            currentScenarioData = scenarioData.ToArray();
        }

        public bool DidScenarioChange(ScenarioEntry scenarioEntry)
        {
            string previousScenarioHash = null;
            string currentScenarioHash = Common.CalculateSHA256Hash(ConfigNodeSerializer.fetch.Serialize(scenarioEntry.scenarioNode));
            if (checkData.TryGetValue(scenarioEntry.scenarioName, out previousScenarioHash))
            {
                return previousScenarioHash != currentScenarioHash;
            }
            return true;
        }

        public void ScenatioVersionHistoryToGroup(string[] scenarioName, byte[][] scenarioData)
        {
            scenarioFundsHistory.Clear();
            scenarioFundsHistory = new List<string>(File.ReadAllLines(scenarioFundsHistoryFile));
            scenarioRepHistory.Clear();
            scenarioRepHistory = new List<string>(File.ReadAllLines(scenarioRepHistoryFile));
            scenarioSciHistory.Clear();
            scenarioSciHistory = new List<string>(File.ReadAllLines(scenarioSciHistoryFile));

            for (int i = 0; i < scenarioName.Length; i++)
            {
                byte[] scenarioBytes = scenarioData[i];
                ConfigNode scenarioDataConfigNode = ConfigNodeSerializer.fetch.Deserialize(scenarioBytes);
                if (scenarioDataConfigNode != null)
                {
                    ScenarioEntry entry = new ScenarioEntry();
                    entry.scenarioName = scenarioName[i];
                    entry.scenarioNode = scenarioDataConfigNode;

                    if (entry.scenarioName == "Funding")
                    {
                        double fundsAmount = Convert.ToDouble(entry.scenarioNode.GetValue("funds"));
                        string fundsAmountDiffString = string.Empty;
                        if (scenarioFundsHistory.Contains("funds"))
                        {
                            int lastFunds = scenarioFundsHistory.LastIndexOf("funds") + 1;
                            double lastFundsAmount = Convert.ToDouble(scenarioFundsHistory[lastFunds].ToString());
                            double fundsAmountDiff = fundsAmount - lastFundsAmount;
                            fundsAmountDiffString = Convert.ToString(fundsAmountDiff);
                        }
                        else
                        {
                            fundsAmountDiffString = Convert.ToString(fundsAmount);
                        }

                        scenarioFundsHistory.Add("funds");
                        scenarioFundsHistory.Add(fundsAmountDiffString);
                    }
                    if (entry.scenarioName == "Reputation")
                    {
                        float repAmount = Convert.ToSingle(entry.scenarioNode.GetValue("rep"));
                        string repAmountDiffString = string.Empty;
                        if (scenarioRepHistory.Contains("rep"))
                        {
                            int lastRep = scenarioRepHistory.LastIndexOf("rep") + 1;
                            float lastRepAmount = Convert.ToSingle(scenarioRepHistory[lastRep].ToString());
                            float repAmountDiff = repAmount - lastRepAmount;
                            repAmountDiffString = Convert.ToString(repAmountDiff);
                        }
                        else
                        {
                            repAmountDiffString = Convert.ToString(repAmount);
                        }

                        scenarioRepHistory.Add("rep");
                        scenarioRepHistory.Add(repAmountDiffString);
                    }
                    if (entry.scenarioName == "ResearchAndDevelopment")
                    {
                        float sciAmount = Convert.ToSingle(entry.scenarioNode.GetValue("sci"));
                        string sciAmountDiffString = string.Empty;
                        if (scenarioSciHistory.Contains("sci"))
                        {
                            int lastSci = scenarioSciHistory.LastIndexOf("sci") + 1;
                            float lastSciAmount = Convert.ToSingle(scenarioSciHistory[lastSci].ToString());
                            float sciAmountDiff = sciAmount - lastSciAmount;
                            sciAmountDiffString = Convert.ToString(sciAmountDiff);
                        }
                        else
                        {
                            sciAmountDiffString = Convert.ToString(sciAmount);
                        }

                        scenarioSciHistory.Add("sci");
                        scenarioSciHistory.Add(sciAmountDiffString);
                    }
                }
            }
            File.WriteAllLines(scenarioFundsHistoryFile, scenarioFundsHistory.ToArray());
            File.WriteAllLines(scenarioRepHistoryFile, scenarioRepHistory.ToArray());
            File.WriteAllLines(scenarioSciHistoryFile, scenarioSciHistory.ToArray());
        }

        public void ScenatioVersionHistoryFromGroup(string[] serverScenatioFundsVersionHistory, string[] serverScenatioRepVersionHistory, string[] serverScenatioSciVersionHistory)
        {
            scenarioFundsHistory.Clear();
            scenarioFundsHistory = new List<string>(serverScenatioFundsVersionHistory);
            File.WriteAllLines(scenarioFundsHistoryFile, scenarioFundsHistory.ToArray());
            scenarioRepHistory.Clear();
            scenarioRepHistory = new List<string>(serverScenatioRepVersionHistory);
            File.WriteAllLines(scenarioRepHistoryFile, scenarioRepHistory.ToArray());
            scenarioSciHistory.Clear();
            scenarioSciHistory = new List<string>(serverScenatioSciVersionHistory);
            File.WriteAllLines(scenarioSciHistoryFile, scenarioSciHistory.ToArray());
        }

        public static void Reset()
        {
            lock (Client.eventLock)
            {
                if (singleton != null)
                {
                    singleton.workerEnabled = false;
                    Client.updateEvent.Remove(singleton.Update);
                }
                singleton = new ScenarioWorker();
                Client.updateEvent.Add(singleton.Update);
            }
        }
    }

    public class ScenarioEntry
    {
        public string scenarioName;
        public ConfigNode scenarioNode;
    }
}

