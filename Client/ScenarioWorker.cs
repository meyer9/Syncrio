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
        private object scenarioSyncLock = new object();
        private Dictionary<string,string> checkData = new Dictionary<string, string>();
        private Queue<ScenarioEntry> scenarioQueue = new Queue<ScenarioEntry>();
        private List<KeyValuePair<string, byte[]>> activeScenarioQueue = new List<KeyValuePair<string, byte[]>>();
        public bool autoSendScenarios;
        public bool nonGroupScenarios;
        public bool canResetScenario;
        public string[] currentScenarioCheckpoint;
        public List<string> scenarioChangeList = new List<string>();
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

        public void LoadScenarioData()
        {
            lock (scenarioLock)
            {
                List<KeyValuePair<string, byte[]>> scenarioListToAdd = new List<KeyValuePair<string, byte[]>>();
                bool changed = false;

                changed = DidScenarioChangeBetweenSyncs();

                if (activeScenarioQueue != null)
                {
                    if (changed)
                    {
                        foreach (KeyValuePair<string, byte[]> kvp in activeScenarioQueue)
                        {
                            if (scenarioChangeList != null)
                            {
                                if (!scenarioChangeList.Contains(kvp.Key))
                                {
                                    scenarioListToAdd.Add(kvp);
                                }
                            }
                            else
                            {
                                SyncrioLog.Debug("'scenarioChangeList' is null and it should not be null!");
                                break;
                            }
                        }
                    }
                    else
                    {
                        scenarioListToAdd = activeScenarioQueue;
                    }
                }
                else
                {
                    SyncrioLog.Debug("'activeScenarioQueue' is null!");
                    return;
                }

                if (scenarioListToAdd == null)
                {
                    return;
                }

                List<ProtoScenarioModule> psmLocked = HighLogic.CurrentGame.scenarios;
                List<ScenarioModule> loadedModules = ScenarioRunner.GetLoadedModules();
                bool loaded = false;
                bool loadedAll = true;
                List<ScenarioEntry> listOfNewScenarioModulesToAdd = new List<ScenarioEntry>();

                for (int v = 0; v < scenarioListToAdd.Count; v++)
                {
                    ScenarioEntry entry = new ScenarioEntry();
                    entry.scenarioName = scenarioListToAdd[v].Key;
                    entry.scenarioNode = ConfigNodeSerializer.fetch.Deserialize(scenarioListToAdd[v].Value);

                    if (!IsScenarioModuleAllowed(entry.scenarioName))
                    {
                        SyncrioLog.Debug("Skipped '" + entry.scenarioName + "' scenario data  in " + Client.fetch.gameMode + " mode");
                        continue;
                    }

                    //Load data from Syncrio
                    if (entry.scenarioNode == null)
                    {
                        SyncrioLog.Debug(entry.scenarioName + " scenario data failed to create a ConfigNode!");
                        continue;
                    }

                    //Load data into game
                    for (int i = 0; i < psmLocked.Count; i++)
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
                                if (loadedModules.Contains(psmLocked[i].moduleRef))
                                {
                                    ScenarioRunner.RemoveModule(psmLocked[i].moduleRef);
                                }
                                psmLocked[i].moduleRef = ScenarioRunner.Instance.AddModule(entry.scenarioNode);
                                psmLocked[i].moduleRef.targetScenes = psmLocked[i].targetScenes;
                            }
                            catch (Exception e)
                            {
                                SyncrioLog.Debug("Error loading " + entry.scenarioName + " scenario module, Exception: " + e);
                            }
                            loaded = true;
                        }
                    }
                    if (!loaded)
                    {
                        listOfNewScenarioModulesToAdd.Add(entry);
                        loadedAll = false;
                    }
                }
                HighLogic.CurrentGame.scenarios = psmLocked;
                if (!loadedAll)
                {
                    SyncrioLog.Debug("Loading " + listOfNewScenarioModulesToAdd.Count + " new scenario module(s)");
                    LoadNewScenarioData(listOfNewScenarioModulesToAdd);
                }
                HighLogic.CurrentGame.Updated();

                scenarioListToAdd.Clear();
                activeScenarioQueue.Clear();

                if (changed)
                {
                    ScenarioSync(GroupSystem.playerGroupAssigned, false, true, false);
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

        public void LoadNewScenarioData(List<ScenarioEntry> newScenarioDataList)
        {
            for (int i = 0; i < newScenarioDataList.Count; i++)
            {
                SyncrioLog.Debug("Loading new " + newScenarioDataList[i].scenarioName + " scenario module");
                ProtoScenarioModule newModule = new ProtoScenarioModule(newScenarioDataList[i].scenarioNode);
                try
                {
                    HighLogic.CurrentGame.scenarios.Add(newModule);
                    newModule.Load(ScenarioRunner.Instance);
                }
                catch
                {
                    SyncrioLog.Debug("Error loading scenario data!");
                }
            }
        }

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

        public void ScenarioSync(bool isInGroup, bool isTwoWay, bool toServer, bool isAutoReply)
        {
            lock (scenarioSyncLock)
            {
                if (!Settings.fetch.DarkMultiPlayerCoopMode)
                {
                    VesselWorker.fetch.SendVessels();
                }

                string[] scenarioName;
                byte[][] scenarioData;

                KeyValuePair<string[], byte[][]> currentScenario = GetCurrentScenarioModules(false);
                scenarioName = currentScenario.Key;
                scenarioData = currentScenario.Value;

                if (isAutoReply)
                {
                    if (scenarioName.Length > 0 && scenarioData.Length > 0)
                    {
                        List<byte[]> entryList = new List<byte[]>();
                        for (int i = 0; i < scenarioName.Length; i++)
                        {
                            entryList.Add(scenarioData[i]);
                        }
                        SetScenarioChangeCheckpoint(entryList);
                    }
                }

                if (isInGroup)
                {
                    string groupName = GroupSystem.playerGroupName;
                    if (!string.IsNullOrEmpty(groupName))
                    {
                        byte[] messageBytes;
                        ClientMessage newMessage = new ClientMessage();
                        newMessage.handled = false;
                        newMessage.type = ClientMessageType.SYNC_SCENARIO_REQUEST;
                        using (MessageWriter mw = new MessageWriter())
                        {
                            mw.Write<bool>(isAutoReply);
                            mw.Write<bool>(toServer);
                            mw.Write<bool>(isInGroup);
                            mw.Write<string>(groupName);
                            if (toServer)
                            {
                                mw.Write<string[]>(scenarioName);
                                foreach (byte[] scenarioBytes in scenarioData)
                                {
                                    mw.Write<byte[]>(Compression.CompressIfNeeded(scenarioBytes));
                                }
                            }
                            messageBytes = mw.GetMessageBytes();
                        }
                        newMessage.data = messageBytes;
                        NetworkWorker.fetch.SendScenarioCommand(newMessage, false);
                        if (toServer && isTwoWay)
                        {
                            ScenarioSync(true, true, false, false);
                        }
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
                        mw.Write<bool>(isAutoReply);
                        mw.Write<bool>(true);
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
        }

        public KeyValuePair<string[], byte[][]> GetCurrentScenarioModules(bool includeChanged)
        {
            KeyValuePair<string[], byte[][]> returnList = new KeyValuePair<string[], byte[][]>();
            int numberOfScenarioModulesLoaded = 0;

            List<string> scenarioName = new List<string>();
            List<byte[]> scenarioData = new List<byte[]>();

            if (HighLogic.CurrentGame != null)
            {
                foreach (ScenarioModule sm in ScenarioRunner.GetLoadedModules())
                {
                    string scenarioType = sm.GetType().Name;
                    if (!IsScenarioModuleAllowed(scenarioType))
                    {
                        continue;
                    }
                    if (scenarioType != "ContractSystem" && scenarioType != "Funding" && scenarioType != "PartUpgradeManager" && scenarioType != "ProgressTracking" && scenarioType != "Reputation" && scenarioType != "ResearchAndDevelopment" && scenarioType != "ResourceScenario" && scenarioType != "ScenarioCustomWaypoints" && scenarioType != "ScenarioDestructibles" && scenarioType != "ScenarioUpgradeableFacilities" && scenarioType != "StrategySystem")
                    {
                        continue;
                    }
                    ConfigNode scenarioNode = new ConfigNode();
                    sm.Save(scenarioNode);
                    byte[] scenarioBytes = ConfigNodeSerializer.fetch.Serialize(scenarioNode);
                    string scenarioHash = Common.CalculateSHA256Hash(scenarioBytes);

                    if (scenarioBytes != null)
                    {
                        if (scenarioBytes.Length == 0)
                        {
                            SyncrioLog.Debug("Error writing scenario data for " + scenarioType);
                            continue;
                        }
                        if (!includeChanged)
                        {
                            if (checkData.ContainsKey(scenarioType) ? (checkData[scenarioType] == scenarioHash) : false)
                            {
                                //Data is the same since last time - Skip it.
                                continue;
                            }
                            else
                            {
                                if (!checkData.ContainsKey(scenarioType))
                                {
                                    checkData.Add(scenarioType, scenarioHash);
                                }
                                else
                                {
                                    checkData[scenarioType] = scenarioHash;
                                }
                            }
                        }
                        scenarioName.Add(scenarioType);
                        scenarioData.Add(scenarioBytes);
                        numberOfScenarioModulesLoaded += 1;
                    }
                    else
                    {
                        SyncrioLog.Debug("Error writing scenario data for " + scenarioType + ", and/or the data is null!");
                        continue;
                    }
                }
            }
            else
            {
                throw new Exception("HighLogic.CurrentGame is Null!");
            }

            if (scenarioName.Count < 0 || scenarioData.Count < 0)
            {
                throw new Exception("We have receved no scenario data from KSP!");
            }

            SyncrioLog.Debug(numberOfScenarioModulesLoaded + " Scenario Modules loaded from KSP.");

            returnList = new KeyValuePair<string[], byte[][]>(scenarioName.ToArray(), scenarioData.ToArray());

            return returnList;
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

        public bool DidScenarioChangeBetweenSyncs()
        {
            scenarioChangeList.Clear();
            KeyValuePair<string[], byte[][]> currentScenario = GetCurrentScenarioModules(true);
            string[] scenarioName;
            byte[][] scenarioData;
            
            scenarioName = currentScenario.Key;
            scenarioData = currentScenario.Value;

            bool didItChange = false;

            if (currentScenarioCheckpoint == null)
            {
                didItChange = false;
                List<byte[]> entryList = new List<byte[]>();
                for (int i = 0; i < scenarioName.Length; i++)
                {
                    entryList.Add(scenarioData[i]);
                }
                SetScenarioChangeCheckpoint(entryList);
            }
            else
            {
                for (int i = 0; i < scenarioName.Length; i++)
                {
                    ScenarioEntry entry = new ScenarioEntry();
                    entry.scenarioName = scenarioName[i];

                    string currentScenarioHash = Common.CalculateSHA256Hash(scenarioData[i]);
                    bool changed = true;
                    if (currentScenarioCheckpoint.Length > i)
                    {
                        changed = (currentScenarioCheckpoint[i] != currentScenarioHash);
                    }
                    if (changed)
                    {
                        didItChange = true;
                        scenarioChangeList.Add(entry.scenarioName);
                    }
                }
            }

            return didItChange;
        }

        public void SetScenarioChangeCheckpoint(List<byte[]> checkpointList)
        {
            currentScenarioCheckpoint = new string[checkpointList.Count];
            for (int i = 0; i < checkpointList.Count; i++)
            {
                currentScenarioCheckpoint[i] = Common.CalculateSHA256Hash(checkpointList[i]);
            }
        }

        public void AutoSendScenariosReply()
        {
            if (HighLogic.LoadedScene != GameScenes.LOADING && HighLogic.LoadedScene != GameScenes.MAINMENU)
            {
                if (GroupSystem.playerGroupAssigned)
                {
                    ScenarioSync(true, false, true, true);
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

