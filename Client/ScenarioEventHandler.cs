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
using System.Linq;
using SyncrioCommon;
using MessageStream2;
using System.Text;

namespace SyncrioClientSide
{
    class ScenarioEventHandler
    {
        public bool enabled = false;
        private static ScenarioEventHandler singleton;
        private bool registered = false;
        private Dictionary<string, List<string>> lastPrograssData = new Dictionary<string, List<string>>();//<Progress Id, Progress Data>
        public List<string> lastResourceScenarioModule;
        public List<string> lastStrategySystemModule;
        private float lastScenarioCheck = 0f;
        private float scenarioCheckInterval = 4f;
        private float lastSync = 0f;
        private float syncCooldown = 1f;
        private bool wasInFlight = false;
        public List<KeyValuePair<string, List<byte[]>>> scenarioBacklog = new List<KeyValuePair<string, List<byte[]>>>();
        public bool RnDOpen = false;
        public bool MissionControlOpen = false;
        public bool AdministrationOpen = false;
        public bool cooldown = false;
        public bool startCooldown = false;
        public bool delaySync = true;

        public static ScenarioEventHandler fetch
        {
            get
            {
                return singleton;
            }
        }

        private void Update()
        {
            if (Client.fetch.gameRunning)
            {
                if (ScenarioWorker.fetch.isSyncing || startCooldown)
                {
                    lastSync = UnityEngine.Time.realtimeSinceStartup;
                    cooldown = true;
                    startCooldown = false;
                }
                else
                {
                    if (UnityEngine.Time.realtimeSinceStartup > lastSync + syncCooldown)
                    {
                        cooldown = false;
                    }
                }

                if (!ScenarioWorker.fetch.stopSync)
                {
                    if (scenarioBacklog.Count > 0)
                    {
                        List<int> dataToRemove = new List<int>();

                        for (int i = 0; i < scenarioBacklog.Count; i++)
                        {
                            if (scenarioBacklog[i].Key == "Loading" && HighLogic.LoadedScene != GameScenes.LOADING)
                            {
                                ScenarioWorker.fetch.LoadScenarioData(scenarioBacklog[i].Value);

                                dataToRemove.Add(i);

                                continue;
                            }

                            if (scenarioBacklog[i].Key == "Tech" && !RnDOpen && HighLogic.LoadedScene != GameScenes.EDITOR && (HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER))
                            {
                                ScenarioWorker.fetch.LoadScenarioData(scenarioBacklog[i].Value);

                                dataToRemove.Add(i);

                                continue;
                            }

                            if (scenarioBacklog[i].Key == "Science" && !RnDOpen && (HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.EDITOR))
                            {
                                ScenarioWorker.fetch.LoadScenarioData(scenarioBacklog[i].Value);

                                dataToRemove.Add(i);

                                continue;
                            }

                            if (scenarioBacklog[i].Key == "Contract" && !MissionControlOpen && (HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.EDITOR))
                            {
                                ScenarioWorker.fetch.LoadScenarioData(scenarioBacklog[i].Value);

                                dataToRemove.Add(i);

                                continue;
                            }

                            if (scenarioBacklog[i].Key == "Strategy" && !AdministrationOpen)
                            {
                                ScenarioWorker.fetch.LoadScenarioData(scenarioBacklog[i].Value);

                                dataToRemove.Add(i);

                                continue;
                            }

                            if (scenarioBacklog[i].Key == "Waypoint" && HighLogic.LoadedScene != GameScenes.TRACKSTATION)
                            {
                                ScenarioWorker.fetch.LoadScenarioData(scenarioBacklog[i].Value);

                                dataToRemove.Add(i);

                                continue;
                            }

                            dataToRemove.Add(i);
                        }

                        if (dataToRemove.Count > 0)
                        {
                            for (int i = dataToRemove.Count; i > 0; i--)
                            {
                                scenarioBacklog.RemoveAt(dataToRemove[i - 1]);
                            }
                        }
                    }

                    /*
                    if (HighLogic.LoadedSceneIsFlight)
                    {
                        if (!wasInFlight)
                        {
                            wasInFlight = true;

                            byte[] messageData;

                            ClientMessage newMessage = new ClientMessage();

                            newMessage.type = ClientMessageType.ENTER_FLIGHT;
                            newMessage.handled = false;

                            using (MessageWriter mw = new MessageWriter())
                            {
                                if (GroupSystem.playerGroupAssigned)
                                {
                                    string groupName = GroupSystem.playerGroupName;

                                    if (!string.IsNullOrEmpty(groupName))
                                    {
                                        mw.Write<bool>(true);//In group
                                        mw.Write<string>(groupName);
                                    }
                                    else
                                    {
                                        return;
                                    }
                                }
                                else
                                {
                                    mw.Write<bool>(false);//In group
                                }

                                messageData = mw.GetMessageBytes();
                            }

                            newMessage.data = messageData;

                            NetworkWorker.fetch.SendScenarioCommand(newMessage, false);
                        }
                    }
                    else
                    {
                        if (wasInFlight)
                        {
                            wasInFlight = false;

                            byte[] messageData;

                            ClientMessage newMessage = new ClientMessage();

                            newMessage.type = ClientMessageType.EXIT_FLIGHT;
                            newMessage.handled = false;

                            using (MessageWriter mw = new MessageWriter())
                            {
                                if (GroupSystem.playerGroupAssigned)
                                {
                                    string groupName = GroupSystem.playerGroupName;

                                    if (!string.IsNullOrEmpty(groupName))
                                    {
                                        mw.Write<bool>(true);//In group
                                        mw.Write<string>(groupName);
                                    }
                                    else
                                    {
                                        return;
                                    }
                                }
                                else
                                {
                                    mw.Write<bool>(false);//In group
                                }

                                messageData = mw.GetMessageBytes();
                            }

                            newMessage.data = messageData;

                            NetworkWorker.fetch.SendScenarioCommand(newMessage, false);
                        }
                    }
                    */

                    if (!registered)
                    {
                        RegisterEvents();
                    }
                    else
                    {
                        if ((UnityEngine.Time.realtimeSinceStartup - lastScenarioCheck) > scenarioCheckInterval)
                        {
                            lastScenarioCheck = UnityEngine.Time.realtimeSinceStartup;

                            if (lastResourceScenarioModule != null)
                            {
                                ConfigNode currentResourceScenarioModule = new ConfigNode();

                                ResourceScenario.Instance.Save(currentResourceScenarioModule);

                                List<string> newData = SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(currentResourceScenarioModule));

                                bool dataIsAMatch = false;

                                if (newData == lastResourceScenarioModule)
                                {
                                    dataIsAMatch = true;
                                }
                                else
                                {
                                    if (newData.Count == lastResourceScenarioModule.Count)
                                    {
                                        int loop = 0;

                                        bool subDataIsAMatch = true;

                                        while (loop < newData.Count)
                                        {
                                            if (newData[loop].Trim() != lastResourceScenarioModule[loop].Trim())
                                            {
                                                subDataIsAMatch = false;

                                                break;
                                            }

                                            loop++;
                                        }

                                        dataIsAMatch = subDataIsAMatch;
                                    }
                                }

                                if (!dataIsAMatch)
                                {
                                    lastResourceScenarioModule = newData;

                                    byte[] data = ConfigNodeSerializer.fetch.Serialize(currentResourceScenarioModule);

                                    byte[] messageData;

                                    using (MessageWriter mw = new MessageWriter())
                                    {
                                        if (GroupSystem.playerGroupAssigned)
                                        {
                                            string groupName = GroupSystem.playerGroupName;

                                            if (!string.IsNullOrEmpty(groupName))
                                            {
                                                mw.Write<bool>(true);//In group
                                                mw.Write<string>(groupName);
                                            }
                                            else
                                            {
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            mw.Write<bool>(false);//In group
                                        }

                                        mw.Write<byte[]>(data);

                                        messageData = mw.GetMessageBytes();
                                    }

                                    SendData((int)ScenarioDataType.RESOURCE_SCENARIO, messageData);
                                }
                            }
                            else
                            {
                                ConfigNode module = new ConfigNode();

                                ResourceScenario.Instance.Save(module);

                                lastResourceScenarioModule = SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(module));
                            }

                            if (lastStrategySystemModule != null)
                            {
                                ConfigNode currentStrategySystemModule = new ConfigNode();

                                Strategies.StrategySystem.Instance.Save(currentStrategySystemModule);

                                List<string> newData = SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(currentStrategySystemModule));

                                bool dataIsAMatch = false;

                                if (newData == lastStrategySystemModule)
                                {
                                    dataIsAMatch = true;
                                }
                                else
                                {
                                    if (newData.Count == lastStrategySystemModule.Count)
                                    {
                                        int loop = 0;

                                        bool subDataIsAMatch = true;

                                        while (loop < newData.Count)
                                        {
                                            if (newData[loop].Trim() != lastStrategySystemModule[loop].Trim())
                                            {
                                                subDataIsAMatch = false;

                                                break;
                                            }

                                            loop++;
                                        }

                                        dataIsAMatch = subDataIsAMatch;
                                    }
                                }

                                if (!dataIsAMatch)
                                {
                                    lastStrategySystemModule = newData;

                                    byte[] data = ConfigNodeSerializer.fetch.Serialize(currentStrategySystemModule);

                                    byte[] messageData;

                                    using (MessageWriter mw = new MessageWriter())
                                    {
                                        if (GroupSystem.playerGroupAssigned)
                                        {
                                            string groupName = GroupSystem.playerGroupName;

                                            if (!string.IsNullOrEmpty(groupName))
                                            {
                                                mw.Write<bool>(true);//In group
                                                mw.Write<string>(groupName);
                                            }
                                            else
                                            {
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            mw.Write<bool>(false);//In group
                                        }

                                        mw.Write<byte[]>(data);

                                        messageData = mw.GetMessageBytes();
                                    }

                                    SendData((int)ScenarioDataType.STRATEGY_SYSTEM, messageData);
                                }
                            }
                            else
                            {
                                ConfigNode module = new ConfigNode();

                                Strategies.StrategySystem.Instance.Save(module);

                                lastStrategySystemModule = SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(module));
                            }
                        }
                    }
                }
                else
                {
                    if (registered)
                    {
                        UnregisterEvents();
                    }
                }
            }
        }

        private void RegisterEvents()
        {
            try
            {
                GameEvents.Contract.onAccepted.Add(OnContractUpdatedWithWeights);
                GameEvents.Contract.onCancelled.Add(OnContractUpdated);
                GameEvents.Contract.onCompleted.Add(OnContractUpdated);
                GameEvents.Contract.onDeclined.Add(OnContractUpdated);
                GameEvents.Contract.onFailed.Add(OnContractUpdated);
                GameEvents.Contract.onFinished.Add(OnContractUpdated);
                GameEvents.Contract.onOffered.Add(OnContractOffered);
                GameEvents.Contract.onParameterChange.Add(OnContractParameterChange);
                GameEvents.Contract.onRead.Add(OnContractUpdated);
                GameEvents.Contract.onSeen.Add(OnContractUpdated);

                GameEvents.onCustomWaypointLoad.Add(OnCustomWaypointLoad);
                GameEvents.onCustomWaypointSave.Add(OnCustomWaypointSave);

                GameEvents.OnFundsChanged.Add(OnFundsChanged);
                GameEvents.OnReputationChanged.Add(OnReputationChanged);
                GameEvents.OnScienceChanged.Add(OnScienceChanged);

                GameEvents.OnKSCFacilityUpgraded.Add(OnKSCFacilityUpgraded);
                GameEvents.OnKSCStructureCollapsed.Add(OnKSCStructureCollapsed);
                GameEvents.OnKSCStructureRepaired.Add(OnKSCStructureRepaired);

                GameEvents.OnPartPurchased.Add(OnPartPurchased);
                GameEvents.OnPartUpgradePurchased.Add(OnPartUpgradePurchased);

                GameEvents.onProgressNodeSave.Add(OnProgressNodeSave);

                GameEvents.OnTechnologyResearched.Add(OnTechnologyResearched);
                GameEvents.OnScienceRecieved.Add(OnScienceRecieved);

                GameEvents.onGUIRnDComplexSpawn.Add(OnGUIRnDComplexSpawn);
                GameEvents.onGUIRnDComplexDespawn.Add(OnGUIRnDComplexDespawn);

                GameEvents.onGUIMissionControlSpawn.Add(OnGUIMissionControlSpawn);
                GameEvents.onGUIMissionControlDespawn.Add(OnGUIMissionControlDespawn);

                GameEvents.onGUIAdministrationFacilitySpawn.Add(OnGUIAdministrationFacilitySpawn);
                GameEvents.onGUIAdministrationFacilityDespawn.Add(OnGUIAdministrationFacilityDespawn);
            }
            catch (Exception e)
            {
                SyncrioLog.Debug("Error registering scenario events! Error: " + e);
            }
            registered = true;
        }

        private void UnregisterEvents()
        {
            registered = false;
            try
            {
                GameEvents.Contract.onAccepted.Remove(OnContractUpdatedWithWeights);
                GameEvents.Contract.onCancelled.Remove(OnContractUpdated);
                GameEvents.Contract.onCompleted.Remove(OnContractUpdated);
                GameEvents.Contract.onDeclined.Remove(OnContractUpdated);
                GameEvents.Contract.onFailed.Remove(OnContractUpdated);
                GameEvents.Contract.onFinished.Remove(OnContractUpdated);
                GameEvents.Contract.onOffered.Remove(OnContractOffered);
                GameEvents.Contract.onParameterChange.Remove(OnContractParameterChange);
                GameEvents.Contract.onRead.Remove(OnContractUpdated);
                GameEvents.Contract.onSeen.Remove(OnContractUpdated);

                GameEvents.onCustomWaypointLoad.Remove(OnCustomWaypointLoad);
                GameEvents.onCustomWaypointSave.Remove(OnCustomWaypointSave);

                GameEvents.OnFundsChanged.Remove(OnFundsChanged);
                GameEvents.OnReputationChanged.Remove(OnReputationChanged);
                GameEvents.OnScienceChanged.Remove(OnScienceChanged);

                GameEvents.OnKSCFacilityUpgraded.Remove(OnKSCFacilityUpgraded);
                GameEvents.OnKSCStructureCollapsed.Remove(OnKSCStructureCollapsed);
                GameEvents.OnKSCStructureRepaired.Remove(OnKSCStructureRepaired);

                GameEvents.OnPartPurchased.Remove(OnPartPurchased);
                GameEvents.OnPartUpgradePurchased.Remove(OnPartUpgradePurchased);

                GameEvents.onProgressNodeSave.Remove(OnProgressNodeSave);

                GameEvents.OnTechnologyResearched.Remove(OnTechnologyResearched);
                GameEvents.OnScienceRecieved.Remove(OnScienceRecieved);

                GameEvents.onGUIRnDComplexSpawn.Remove(OnGUIRnDComplexSpawn);
                GameEvents.onGUIRnDComplexDespawn.Remove(OnGUIRnDComplexDespawn);

                GameEvents.onGUIMissionControlSpawn.Remove(OnGUIMissionControlSpawn);
                GameEvents.onGUIMissionControlDespawn.Remove(OnGUIMissionControlDespawn);

                GameEvents.onGUIAdministrationFacilitySpawn.Remove(OnGUIAdministrationFacilitySpawn);
                GameEvents.onGUIAdministrationFacilityDespawn.Remove(OnGUIAdministrationFacilityDespawn);
            }
            catch (Exception e)
            {
                SyncrioLog.Debug("Error unregistering  scenario events! Error: " + e);
            }
        }

        private void OnGUIRnDComplexSpawn()
        {
            RnDOpen = true;
        }

        private void OnGUIRnDComplexDespawn()
        {
            RnDOpen = false;
        }

        private void OnGUIMissionControlSpawn()
        {
            MissionControlOpen = true;
        }

        private void OnGUIMissionControlDespawn()
        {
            MissionControlOpen = false;
        }

        private void OnGUIAdministrationFacilitySpawn()
        {
            AdministrationOpen = true;
        }

        private void OnGUIAdministrationFacilityDespawn()
        {
            AdministrationOpen = false;
        }

        private void OnContractUpdatedWithWeights(Contracts.Contract contract)//onAccepted only
        {
            if (cooldown || delaySync)
            {
                return;
            }

            if (GroupSystem.playerGroupAssigned)
            {
                string groupName = GroupSystem.playerGroupName;

                if (!string.IsNullOrEmpty(groupName))
                {
                    byte[] data;

                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(true);//In group
                        mw.Write<string>(groupName);

                        ConfigNode cn = new ConfigNode();
                        contract.Save(cn);

                        List<string> cnList = SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(cn));
                        
                        cnList.Insert(0, "CONTRACT");
                        cnList.Insert(1, "{");
                        cnList.Add("}");

                        byte[] cnData = SyncrioUtil.ByteArraySerializer.Serialize(cnList);

                        mw.Write<byte[]>(cnData);
                        
                        //Also send the contract weights
                        mw.Write<int>(Contracts.ContractSystem.ContractWeights.Count);

                        foreach (string key in Contracts.ContractSystem.ContractWeights.Keys)
                        {
                            mw.Write<string>(key);
                            mw.Write<int>(Contracts.ContractSystem.ContractWeights[key]);
                        }

                        data = mw.GetMessageBytes();
                    }

                    SendData((int)ScenarioDataType.CONTRACT_UPDATED, data);

                    foreach (Contracts.ContractParameter _param in contract.AllParameters)
                    {
                        if (_param.GetType() == typeof(Contracts.Parameters.PartTest))
                        {
                            Contracts.Parameters.PartTest trueParam = (Contracts.Parameters.PartTest)_param;
                            
                            byte[] data2;

                            using (MessageWriter mw = new MessageWriter())
                            {
                                mw.Write<bool>(true);//In group
                                mw.Write<string>(groupName);

                                mw.Write<string>(trueParam.tgtPartInfo.name);
                                mw.Write<string>(trueParam.tgtPartInfo.TechRequired);

                                data2 = mw.GetMessageBytes();
                            }

                            SendData((int)ScenarioDataType.PART_PURCHASED, data2);
                        }
                    }
                }
            }
            else
            {
                byte[] data;

                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<bool>(false);//In group

                    ConfigNode cn = new ConfigNode();
                    contract.Save(cn);

                    List<string> cnList = SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(cn));
                    
                    cnList.Insert(0, "CONTRACT");
                    cnList.Insert(1, "{");
                    cnList.Add("}");

                    byte[] cnData = SyncrioUtil.ByteArraySerializer.Serialize(cnList);

                    mw.Write<byte[]>(cnData);

                    //Also send the contract weights
                    mw.Write<int>(Contracts.ContractSystem.ContractWeights.Count);

                    foreach (string key in Contracts.ContractSystem.ContractWeights.Keys)
                    {
                        mw.Write<string>(key);
                        mw.Write<int>(Contracts.ContractSystem.ContractWeights[key]);
                    }

                    data = mw.GetMessageBytes();
                }

                SendData((int)ScenarioDataType.CONTRACT_UPDATED, data);

                foreach (Contracts.ContractParameter _param in contract.AllParameters)
                {
                    if (_param.GetType() == typeof(Contracts.Parameters.PartTest))
                    {
                        Contracts.Parameters.PartTest trueParam = (Contracts.Parameters.PartTest)_param;

                        byte[] data2;

                        using (MessageWriter mw = new MessageWriter())
                        {
                            mw.Write<bool>(false);//In group

                            mw.Write<string>(trueParam.tgtPartInfo.name);
                            mw.Write<string>(trueParam.tgtPartInfo.TechRequired);

                            data2 = mw.GetMessageBytes();
                        }

                        SendData((int)ScenarioDataType.PART_PURCHASED, data2);
                    }
                }
            }
        }

        private void OnContractUpdated(Contracts.Contract contract)
        {
            if (cooldown || delaySync)
            {
                return;
            }

            if (contract.ContractState != Contracts.Contract.State.Withdrawn && contract.ContractState != Contracts.Contract.State.OfferExpired)
            {
                if (GroupSystem.playerGroupAssigned)
                {
                    string groupName = GroupSystem.playerGroupName;

                    if (!string.IsNullOrEmpty(groupName))
                    {
                        byte[] data;

                        using (MessageWriter mw = new MessageWriter())
                        {
                            mw.Write<bool>(true);//In group
                            mw.Write<string>(groupName);

                            ConfigNode cn = new ConfigNode();
                            contract.Save(cn);

                            List<string> cnList = SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(cn));

                            if (contract.IsFinished())
                            {
                                cnList.Insert(0, "CONTRACT_FINISHED");
                                cnList.Insert(1, "{");
                                cnList.Add("}");
                            }
                            else
                            {
                                cnList.Insert(0, "CONTRACT");
                                cnList.Insert(1, "{");
                                cnList.Add("}");
                            }

                            byte[] cnData = SyncrioUtil.ByteArraySerializer.Serialize(cnList);

                            mw.Write<byte[]>(cnData);

                            mw.Write<int>(0);

                            data = mw.GetMessageBytes();
                        }

                        SendData((int)ScenarioDataType.CONTRACT_UPDATED, data);

                        if (contract.IsFinished())
                        {
                            foreach (Contracts.ContractParameter _param in contract.AllParameters)
                            {
                                if (_param.GetType() == typeof(Contracts.Parameters.PartTest))
                                {
                                    Contracts.Parameters.PartTest trueParam = (Contracts.Parameters.PartTest)_param;

                                    byte[] data2;

                                    using (MessageWriter mw = new MessageWriter())
                                    {
                                        mw.Write<bool>(true);//In group
                                        mw.Write<string>(groupName);

                                        mw.Write<string>(trueParam.tgtPartInfo.name);
                                        mw.Write<string>(trueParam.tgtPartInfo.TechRequired);

                                        data2 = mw.GetMessageBytes();
                                    }

                                    SendData((int)ScenarioDataType.REMOVE_PART, data2);
                                }
                            }
                        }
                    }
                }
                else
                {
                    byte[] data;

                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(false);//In group

                        ConfigNode cn = new ConfigNode();
                        contract.Save(cn);

                        List<string> cnList = SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(cn));

                        if (contract.IsFinished())
                        {
                            cnList.Insert(0, "CONTRACT_FINISHED");
                            cnList.Insert(1, "{");
                            cnList.Add("}");
                        }
                        else
                        {
                            cnList.Insert(0, "CONTRACT");
                            cnList.Insert(1, "{");
                            cnList.Add("}");
                        }

                        byte[] cnData = SyncrioUtil.ByteArraySerializer.Serialize(cnList);

                        mw.Write<byte[]>(cnData);

                        mw.Write<int>(0);

                        data = mw.GetMessageBytes();
                    }

                    SendData((int)ScenarioDataType.CONTRACT_UPDATED, data);

                    if (contract.IsFinished())
                    {
                        foreach (Contracts.ContractParameter _param in contract.AllParameters)
                        {
                            if (_param.GetType() == typeof(Contracts.Parameters.PartTest))
                            {
                                Contracts.Parameters.PartTest trueParam = (Contracts.Parameters.PartTest)_param;

                                byte[] data2;

                                using (MessageWriter mw = new MessageWriter())
                                {
                                    mw.Write<bool>(false);//In group

                                    mw.Write<string>(trueParam.tgtPartInfo.name);
                                    mw.Write<string>(trueParam.tgtPartInfo.TechRequired);

                                    data2 = mw.GetMessageBytes();
                                }

                                SendData((int)ScenarioDataType.REMOVE_PART, data2);
                            }
                        }
                    }
                }
            }
            else
            {
                if (GroupSystem.playerGroupAssigned)
                {
                    string groupName = GroupSystem.playerGroupName;

                    if (!string.IsNullOrEmpty(groupName))
                    {
                        byte[] data;

                        using (MessageWriter mw = new MessageWriter())
                        {
                            mw.Write<bool>(true);//In group
                            mw.Write<string>(groupName);
                            
                            mw.Write<string>(contract.ContractGuid.ToString());

                            data = mw.GetMessageBytes();
                        }

                        SendData((int)ScenarioDataType.REMOVE_CONTRACT, data);
                        
                        foreach (Contracts.ContractParameter _param in contract.AllParameters)
                        {
                            if (_param.GetType() == typeof(Contracts.Parameters.PartTest))
                            {
                                Contracts.Parameters.PartTest trueParam = (Contracts.Parameters.PartTest)_param;

                                byte[] data2;

                                using (MessageWriter mw = new MessageWriter())
                                {
                                    mw.Write<bool>(true);//In group
                                    mw.Write<string>(groupName);

                                    mw.Write<string>(trueParam.tgtPartInfo.name);
                                    mw.Write<string>(trueParam.tgtPartInfo.TechRequired);

                                    data2 = mw.GetMessageBytes();
                                }

                                SendData((int)ScenarioDataType.REMOVE_PART, data2);
                            }
                        }
                    }
                }
                else
                {
                    byte[] data;

                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(false);//In group

                        mw.Write<string>(contract.ContractGuid.ToString());

                        data = mw.GetMessageBytes();
                    }

                    SendData((int)ScenarioDataType.REMOVE_CONTRACT, data);

                    foreach (Contracts.ContractParameter _param in contract.AllParameters)
                    {
                        if (_param.GetType() == typeof(Contracts.Parameters.PartTest))
                        {
                            Contracts.Parameters.PartTest trueParam = (Contracts.Parameters.PartTest)_param;

                            byte[] data2;

                            using (MessageWriter mw = new MessageWriter())
                            {
                                mw.Write<bool>(false);//In group

                                mw.Write<string>(trueParam.tgtPartInfo.name);
                                mw.Write<string>(trueParam.tgtPartInfo.TechRequired);

                                data2 = mw.GetMessageBytes();
                            }

                            SendData((int)ScenarioDataType.REMOVE_PART, data2);
                        }
                    }
                }
            }
        }

        private void OnContractOffered(Contracts.Contract contract)
        {
            if (cooldown || delaySync)
            {
                return;
            }

            if (GroupSystem.playerGroupAssigned)
            {
                string groupName = GroupSystem.playerGroupName;

                if (!string.IsNullOrEmpty(groupName))
                {
                    if (!LockSystem.fetch.LockIsOurs(string.Format(Client.english, "contract-spawn-{0}", groupName)))
                    {
                        contract.Kill();
                    }
                    else
                    {
                        byte[] data;

                        using (MessageWriter mw = new MessageWriter())
                        {
                            mw.Write<bool>(true);//In group
                            mw.Write<string>(groupName);

                            ConfigNode cn = new ConfigNode();
                            contract.Save(cn);

                            List<string> cnList = SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(cn));

                            if (contract.IsFinished())
                            {
                                cnList.Insert(0, "CONTRACT_FINISHED");
                                cnList.Insert(1, "{");
                                cnList.Add("}");
                            }
                            else
                            {
                                cnList.Insert(0, "CONTRACT");
                                cnList.Insert(1, "{");
                                cnList.Add("}");
                            }

                            byte[] cnData = SyncrioUtil.ByteArraySerializer.Serialize(cnList);

                            mw.Write<byte[]>(cnData);

                            data = mw.GetMessageBytes();
                        }

                        SendData((int)ScenarioDataType.CONTRACT_OFFERED, data);
                    }
                }
            }
            else
            {
                byte[] data;

                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<bool>(false);//In group

                    ConfigNode cn = new ConfigNode();
                    contract.Save(cn);

                    List<string> cnList = SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(cn));

                    if (contract.IsFinished())
                    {
                        cnList.Insert(0, "CONTRACT_FINISHED");
                        cnList.Insert(1, "{");
                        cnList.Add("}");
                    }
                    else
                    {
                        cnList.Insert(0, "CONTRACT");
                        cnList.Insert(1, "{");
                        cnList.Add("}");
                    }

                    byte[] cnData = SyncrioUtil.ByteArraySerializer.Serialize(cnList);

                    mw.Write<byte[]>(cnData);

                    data = mw.GetMessageBytes();
                }

                SendData((int)ScenarioDataType.CONTRACT_OFFERED, data);
            }
        }

        private void OnContractParameterChange(Contracts.Contract contract, Contracts.ContractParameter param)
        {
            if (cooldown || delaySync)
            {
                return;
            }

            if (GroupSystem.playerGroupAssigned)
            {
                string groupName = GroupSystem.playerGroupName;

                if (!string.IsNullOrEmpty(groupName))
                {
                    byte[] data;

                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(true);//In group
                        mw.Write<string>(groupName);

                        ConfigNode cn = new ConfigNode();
                        contract.Save(cn);

                        List<string> cnList = SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(cn));

                        if (contract.IsFinished())
                        {
                            cnList.Insert(0, "CONTRACT_FINISHED");
                            cnList.Insert(1, "{");
                            cnList.Add("}");
                        }
                        else
                        {
                            cnList.Insert(0, "CONTRACT");
                            cnList.Insert(1, "{");
                            cnList.Add("}");
                        }

                        byte[] cnData = SyncrioUtil.ByteArraySerializer.Serialize(cnList);

                        mw.Write<byte[]>(cnData);

                        mw.Write<int>(0);

                        data = mw.GetMessageBytes();
                    }

                    SendData((int)ScenarioDataType.CONTRACT_UPDATED, data);

                }
            }
            else
            {
                byte[] data;

                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<bool>(false);//In group

                    ConfigNode cn = new ConfigNode();
                    contract.Save(cn);

                    List<string> cnList = SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(cn));

                    if (contract.IsFinished())
                    {
                        cnList.Insert(0, "CONTRACT_FINISHED");
                        cnList.Insert(1, "{");
                        cnList.Add("}");
                    }
                    else
                    {
                        cnList.Insert(0, "CONTRACT");
                        cnList.Insert(1, "{");
                        cnList.Add("}");
                    }

                    byte[] cnData = SyncrioUtil.ByteArraySerializer.Serialize(cnList);

                    mw.Write<byte[]>(cnData);

                    mw.Write<int>(0);

                    data = mw.GetMessageBytes();
                }

                SendData((int)ScenarioDataType.CONTRACT_UPDATED, data);
            }
        }

        private void OnCustomWaypointLoad(GameEvents.FromToAction<FinePrint.Waypoint, ConfigNode> waypoint)
        {
            if (cooldown || delaySync)
            {
                return;
            }

            if (GroupSystem.playerGroupAssigned)
            {
                string groupName = GroupSystem.playerGroupName;

                if (!string.IsNullOrEmpty(groupName))
                {
                    byte[] data;

                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(true);//In group
                        mw.Write<string>(groupName);

                        mw.Write<string>(waypoint.from.FullName);

                        ConfigNode wp = new ConfigNode();
                        waypoint.to.CopyTo(wp);
                        byte[] wpData = ConfigNodeSerializer.fetch.Serialize(wp);

                        mw.Write<byte[]>(wpData);

                        data = mw.GetMessageBytes();
                    }

                    SendData((int)ScenarioDataType.CUSTOM_WAYPOINT_LOAD, data);
                }
            }
            else
            {
                byte[] data;

                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<bool>(false);//In group

                    mw.Write<string>(waypoint.from.FullName);

                    ConfigNode wp = new ConfigNode();
                    waypoint.to.CopyTo(wp);
                    byte[] wpData = ConfigNodeSerializer.fetch.Serialize(wp);

                    mw.Write<byte[]>(wpData);

                    data = mw.GetMessageBytes();
                }

                SendData((int)ScenarioDataType.CUSTOM_WAYPOINT_LOAD, data);
            }
        }

        private void OnCustomWaypointSave(GameEvents.FromToAction<FinePrint.Waypoint, ConfigNode> waypoint)
        {
            if (cooldown || delaySync)
            {
                return;
            }

            if (GroupSystem.playerGroupAssigned)
            {
                string groupName = GroupSystem.playerGroupName;

                if (!string.IsNullOrEmpty(groupName))
                {
                    byte[] data;

                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(true);//In group
                        mw.Write<string>(groupName);

                        mw.Write<string>(waypoint.from.FullName);

                        ConfigNode wp = new ConfigNode();
                        waypoint.to.CopyTo(wp);
                        byte[] wpData = ConfigNodeSerializer.fetch.Serialize(wp);

                        mw.Write<byte[]>(wpData);

                        data = mw.GetMessageBytes();
                    }

                    SendData((int)ScenarioDataType.CUSTOM_WAYPOINT_SAVE, data);
                }
            }
            else
            {
                byte[] data;

                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<bool>(false);//In group

                    mw.Write<string>(waypoint.from.FullName);

                    ConfigNode wp = new ConfigNode();
                    waypoint.to.CopyTo(wp);
                    byte[] wpData = ConfigNodeSerializer.fetch.Serialize(wp);

                    mw.Write<byte[]>(wpData);

                    data = mw.GetMessageBytes();
                }

                SendData((int)ScenarioDataType.CUSTOM_WAYPOINT_SAVE, data);
            }
        }

        private void OnFundsChanged(double value, TransactionReasons reason)
        {
            if (reason == TransactionReasons.Cheating)
            {
                if (!Client.fetch.serverAllowCheats)
                {
                    return;
                }
            }

            if (cooldown || delaySync)
            {
                return;
            }

            if (GroupSystem.playerGroupAssigned)
            {
                string groupName = GroupSystem.playerGroupName;

                if (!string.IsNullOrEmpty(groupName))
                {
                    byte[] data;

                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(true);//In group
                        mw.Write<string>(groupName);

                        mw.Write<double>(value);

                        data = mw.GetMessageBytes();
                    }

                    SendData((int)ScenarioDataType.FUNDS_CHANGED, data);
                }
            }
            else
            {
                byte[] data;

                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<bool>(false);//In group

                    mw.Write<double>(value);

                    data = mw.GetMessageBytes();
                }

                SendData((int)ScenarioDataType.FUNDS_CHANGED, data);
            }
        }

        private void OnReputationChanged(float value, TransactionReasons reason)
        {
            if (reason == TransactionReasons.Cheating)
            {
                if (!Client.fetch.serverAllowCheats)
                {
                    return;
                }
            }

            if (cooldown || delaySync)
            {
                return;
            }

            if (GroupSystem.playerGroupAssigned)
            {
                string groupName = GroupSystem.playerGroupName;

                if (!string.IsNullOrEmpty(groupName))
                {
                    byte[] data;

                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(true);//In group
                        mw.Write<string>(groupName);

                        mw.Write<float>(value);

                        data = mw.GetMessageBytes();
                    }

                    SendData((int)ScenarioDataType.REPUTATION_CHANGED, data);
                }
            }
            else
            {
                byte[] data;

                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<bool>(false);//In group

                    mw.Write<float>(value);

                    data = mw.GetMessageBytes();
                }

                SendData((int)ScenarioDataType.REPUTATION_CHANGED, data);
            }
        }

        private void OnScienceChanged(float value, TransactionReasons reason)
        {
            if (reason == TransactionReasons.Cheating)
            {
                if (!Client.fetch.serverAllowCheats)
                {
                    return;
                }
            }

            if (cooldown || delaySync)
            {
                return;
            }

            if (GroupSystem.playerGroupAssigned)
            {
                string groupName = GroupSystem.playerGroupName;

                if (!string.IsNullOrEmpty(groupName))
                {
                    byte[] data;

                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(true);//In group
                        mw.Write<string>(groupName);

                        mw.Write<float>(value);

                        data = mw.GetMessageBytes();
                    }

                    SendData((int)ScenarioDataType.SCIENCE_CHANGED, data);
                }
            }
            else
            {
                byte[] data;

                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<bool>(false);//In group

                    mw.Write<float>(value);

                    data = mw.GetMessageBytes();
                }

                SendData((int)ScenarioDataType.SCIENCE_CHANGED, data);
            }
        }

        private void OnKSCFacilityUpgraded(Upgradeables.UpgradeableFacility facility, int level)
        {
            if (cooldown || delaySync)
            {
                return;
            }

            if (GroupSystem.playerGroupAssigned)
            {
                string groupName = GroupSystem.playerGroupName;

                if (!string.IsNullOrEmpty(groupName))
                {
                    byte[] data;

                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(true);//In group
                        mw.Write<string>(groupName);

                        mw.Write<string>(facility.id);

                        mw.Write<int>(level);

                        data = mw.GetMessageBytes();
                    }

                    SendData((int)ScenarioDataType.KSC_FACILITY_UPGRADED, data);
                }
            }
            else
            {
                byte[] data;

                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<bool>(false);//In group

                    mw.Write<string>(facility.id);

                    mw.Write<int>(level);

                    data = mw.GetMessageBytes();
                }

                SendData((int)ScenarioDataType.KSC_FACILITY_UPGRADED, data);
            }
        }

        private void OnKSCStructureCollapsed(DestructibleBuilding building)
        {
            if (cooldown || delaySync)
            {
                return;
            }

            if (GroupSystem.playerGroupAssigned)
            {
                string groupName = GroupSystem.playerGroupName;

                if (!string.IsNullOrEmpty(groupName))
                {
                    byte[] data;

                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(true);//In group
                        mw.Write<string>(groupName);

                        mw.Write<string>(building.id);

                        data = mw.GetMessageBytes();
                    }

                    SendData((int)ScenarioDataType.KSC_STRUCTURE_COLLAPSED, data);
                }
            }
            else
            {
                byte[] data;

                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<bool>(false);//In group

                    mw.Write<string>(building.id);

                    data = mw.GetMessageBytes();
                }

                SendData((int)ScenarioDataType.KSC_STRUCTURE_COLLAPSED, data);
            }
        }

        private void OnKSCStructureRepaired(DestructibleBuilding building)
        {
            if (cooldown || delaySync)
            {
                return;
            }

            if (GroupSystem.playerGroupAssigned)
            {
                string groupName = GroupSystem.playerGroupName;

                if (!string.IsNullOrEmpty(groupName))
                {
                    byte[] data;

                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(true);//In group
                        mw.Write<string>(groupName);

                        mw.Write<string>(building.id);

                        data = mw.GetMessageBytes();
                    }

                    SendData((int)ScenarioDataType.KSC_STRUCTURE_REPAIRED, data);
                }
            }
            else
            {
                byte[] data;

                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<bool>(false);//In group

                    mw.Write<string>(building.id);

                    data = mw.GetMessageBytes();
                }

                SendData((int)ScenarioDataType.KSC_STRUCTURE_REPAIRED, data);
            }
        }

        private void OnPartPurchased(AvailablePart part)
        {
            if (cooldown || delaySync)
            {
                return;
            }

            if (GroupSystem.playerGroupAssigned)
            {
                string groupName = GroupSystem.playerGroupName;

                if (!string.IsNullOrEmpty(groupName))
                {
                    byte[] data;

                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(true);//In group
                        mw.Write<string>(groupName);

                        mw.Write<string>(part.name);
                        mw.Write<string>(part.TechRequired);

                        data = mw.GetMessageBytes();
                    }

                    SendData((int)ScenarioDataType.PART_PURCHASED, data);
                }
            }
            else
            {
                byte[] data;

                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<bool>(false);//In group

                    mw.Write<string>(part.name);
                    mw.Write<string>(part.TechRequired);

                    data = mw.GetMessageBytes();
                }

                SendData((int)ScenarioDataType.PART_PURCHASED, data);
            }
        }

        private void OnPartUpgradePurchased(PartUpgradeHandler.Upgrade upgrade)
        {
            if (cooldown || delaySync)
            {
                return;
            }

            if (GroupSystem.playerGroupAssigned)
            {
                string groupName = GroupSystem.playerGroupName;

                if (!string.IsNullOrEmpty(groupName))
                {
                    byte[] data;

                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(true);//In group
                        mw.Write<string>(groupName);

                        mw.Write<string>(upgrade.name);
                        mw.Write<string>(upgrade.techRequired);

                        data = mw.GetMessageBytes();
                    }

                    SendData((int)ScenarioDataType.PART_UPGRADE_PURCHASED, data);
                }
            }
            else
            {
                byte[] data;

                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<bool>(false);//In group

                    mw.Write<string>(upgrade.name);
                    mw.Write<string>(upgrade.techRequired);

                    data = mw.GetMessageBytes();
                }

                SendData((int)ScenarioDataType.PART_UPGRADE_PURCHASED, data);
            }
        }

        private void OnProgressNodeSave(GameEvents.FromToAction<ProgressNode, ConfigNode> dataNode)
        {
            if (cooldown || delaySync)
            {
                return;
            }

            if (dataNode.from.Id == "BaseConstruction" || dataNode.from.Id == "CrewTransfer" || dataNode.from.Id == "Docking" || dataNode.from.Id == "Escape" || dataNode.from.Id == "FlagPlant" || dataNode.from.Id == "Flight" || dataNode.from.Id == "FlyBy" || dataNode.from.Id == "Landing" || dataNode.from.Id == "Orbit" || dataNode.from.Id == "Rendezvous" || dataNode.from.Id == "ReturnFromFlyby" || dataNode.from.Id == "ReturnFromOrbit" || dataNode.from.Id == "ReturnFromSurface" || dataNode.from.Id == "Science" || dataNode.from.Id == "Spacewalk" || dataNode.from.Id == "Splashdown" || dataNode.from.Id == "StationConstruction" || dataNode.from.Id == "Suborbit" || dataNode.from.Id == "SurfaceEVA")
            {
                return;
            }

            bool send = false;
            
            if (!lastPrograssData.ContainsKey(dataNode.from.Id))
            {
                lastPrograssData.Add(dataNode.from.Id, SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(dataNode.to)));

                send = true;
            }
            else
            {
                if (dataNode.to != null)
                {
                    List<string> newData = SyncrioUtil.ByteArraySerializer.Deserialize(ConfigNodeSerializer.fetch.Serialize(dataNode.to));

                    bool dataIsAMatch = false;

                    if (newData == lastPrograssData[dataNode.from.Id])
                    {
                        dataIsAMatch = true;
                    }
                    else
                    {
                        if (newData.Count == lastPrograssData[dataNode.from.Id].Count)
                        {
                            int loop = 0;

                            bool subDataIsAMatch = true;

                            while (loop < newData.Count)
                            {
                                if (newData[loop].Trim() != lastPrograssData[dataNode.from.Id][loop].Trim())
                                {
                                    subDataIsAMatch = false;

                                    break;
                                }

                                loop++;
                            }

                            dataIsAMatch = subDataIsAMatch;
                        }
                    }

                    if (!dataIsAMatch)
                    {
                        lastPrograssData[dataNode.from.Id] = newData;
                        send = true;
                    }
                }
            }

            if (send)
            {
                if (GroupSystem.playerGroupAssigned)
                {
                    string groupName = GroupSystem.playerGroupName;

                    if (!string.IsNullOrEmpty(groupName))
                    {
                        byte[] data;

                        using (MessageWriter mw = new MessageWriter())
                        {
                            mw.Write<bool>(true);//In group
                            mw.Write<string>(groupName);

                            mw.Write<string>(dataNode.from.Id);

                            byte[] pnData = ConfigNodeSerializer.fetch.Serialize(dataNode.to);

                            mw.Write<byte[]>(pnData);

                            data = mw.GetMessageBytes();
                        }

                        SendData((int)ScenarioDataType.PROGRESS_UPDATED, data);
                    }
                }
                else
                {
                    byte[] data;

                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(false);//In group

                        mw.Write<string>(dataNode.from.Id);

                        byte[] pnData = ConfigNodeSerializer.fetch.Serialize(dataNode.to);

                        mw.Write<byte[]>(pnData);

                        data = mw.GetMessageBytes();
                    }

                    SendData((int)ScenarioDataType.PROGRESS_UPDATED, data);
                }
            }
        }

        private void OnTechnologyResearched(GameEvents.HostTargetAction<RDTech, RDTech.OperationResult> tech)
        {
            if (cooldown || delaySync)
            {
                return;
            }

            if (tech.target == RDTech.OperationResult.Successful)
            {
                if (GroupSystem.playerGroupAssigned)
                {
                    string groupName = GroupSystem.playerGroupName;

                    if (!string.IsNullOrEmpty(groupName))
                    {
                        byte[] data;

                        using (MessageWriter mw = new MessageWriter())
                        {
                            mw.Write<bool>(true);//In group
                            mw.Write<string>(groupName);

                            mw.Write<string>(tech.host.techID);

                            ConfigNode techCFG = new ConfigNode();
                            tech.host.Save(techCFG);
                            byte[] techData = ConfigNodeSerializer.fetch.Serialize(techCFG);

                            mw.Write<byte[]>(techData);

                            mw.Write<int>(tech.host.partsPurchased.Count);

                            for (int i = 0; i < tech.host.partsPurchased.Count; i++)
                            {
                                mw.Write<string>(tech.host.partsPurchased[i].name);
                            }

                            data = mw.GetMessageBytes();
                        }

                        SendData((int)ScenarioDataType.TECHNOLOGY_RESEARCHED, data);
                    }
                }
                else
                {
                    byte[] data;

                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(false);//In group

                        mw.Write<string>(tech.host.techID);

                        ConfigNode techCFG = new ConfigNode();
                        tech.host.Save(techCFG);
                        byte[] techData = ConfigNodeSerializer.fetch.Serialize(techCFG);

                        mw.Write<byte[]>(techData);

                        mw.Write<int>(tech.host.partsPurchased.Count);

                        for (int i = 0; i < tech.host.partsPurchased.Count; i++)
                        {
                            mw.Write<string>(tech.host.partsPurchased[i].name);
                        }

                        data = mw.GetMessageBytes();
                    }

                    SendData((int)ScenarioDataType.TECHNOLOGY_RESEARCHED, data);
                }
            }
        }

        private void OnScienceRecieved(float dataValue, ScienceSubject subject, ProtoVessel vessel, bool reverseEngineered)
        {
            if (cooldown || delaySync)
            {
                return;
            }
            
            if (GroupSystem.playerGroupAssigned)
            {
                string groupName = GroupSystem.playerGroupName;

                if (!string.IsNullOrEmpty(groupName))
                {
                    byte[] data;

                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(true);//In group
                        mw.Write<string>(groupName);

                        mw.Write<string>(subject.id);

                        float dataAmount = subject.science / subject.subjectValue;

                        mw.Write<float>(dataAmount * subject.dataScale);

                        ConfigNode sciCFG = new ConfigNode();
                        subject.Save(sciCFG);
                        byte[] sciData = ConfigNodeSerializer.fetch.Serialize(sciCFG);

                        mw.Write<byte[]>(sciData);

                        data = mw.GetMessageBytes();
                    }

                    SendData((int)ScenarioDataType.SCIENCE_RECIEVED, data);
                }
            }
            else
            {
                byte[] data;

                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<bool>(false);//In group

                    mw.Write<string>(subject.id);

                    float dataAmount = subject.science / subject.subjectValue;

                    mw.Write<float>(dataAmount * subject.dataScale);

                    ConfigNode sciCFG = new ConfigNode();
                    subject.Save(sciCFG);
                    byte[] sciData = ConfigNodeSerializer.fetch.Serialize(sciCFG);

                    mw.Write<byte[]>(sciData);

                    data = mw.GetMessageBytes();
                }

                SendData((int)ScenarioDataType.SCIENCE_RECIEVED, data);
            }
        }

        internal void SendData(int type, byte[] data)
        {
            byte[] messageBytes;
            ClientMessage newMessage = new ClientMessage();
            newMessage.handled = false;
            newMessage.type = ClientMessageType.SCENARIO_DATA;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>(type);
                mw.Write<byte[]>(data);

                messageBytes = mw.GetMessageBytes();
            }
            newMessage.data = messageBytes;
            NetworkWorker.fetch.SendScenarioCommand(newMessage, false);
        }

        public static void Reset()
        {
            lock (Client.eventLock)
            {
                if (singleton != null)
                {
                    Client.updateEvent.Remove(singleton.Update);
                    singleton.enabled = false;
                    if (singleton.registered)
                    {
                        singleton.UnregisterEvents();
                    }
                }
                singleton = new ScenarioEventHandler();
                Client.updateEvent.Add(singleton.Update);
                singleton.delaySync = true;
            }
        }
    }
}
