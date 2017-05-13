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
        //private ScenarioEvents scenarioEvents = new ScenarioEvents();
        private float lastSync = 0.0f;
        private float syncCooldown = 0.2f;
        public bool cooldown = false;
        public bool startCooldown = false;

        public static ScenarioEventHandler fetch
        {
            get
            {
                return singleton;
            }
        }

        private void Update()
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

            if (!registered && !ScenarioWorker.fetch.stopSync)
            {
                RegisterEvents();
            }
            else
            {
                if (ScenarioWorker.fetch.stopSync)
                {
                    UnregisterEvents();
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

                GameEvents.OnProgressComplete.Add(OnProgressComplete);

                GameEvents.OnTechnologyResearched.Add(OnTechnologyResearched);
                GameEvents.OnScienceRecieved.Add(OnScienceRecieved);

                /*
                scenarioEvents.onContractAccepted = GameEvents.FindEvent<EventData<Contracts.Contract>>("Contract.Accepted");
                if (scenarioEvents.onContractAccepted != null)
                {
                    scenarioEvents.onContractAccepted.Add(OnContractUpdatedWithWeights);
                }

                scenarioEvents.onContractCancelled = GameEvents.FindEvent<EventData<Contracts.Contract>>("Contract.Cancelled");
                if (scenarioEvents.onContractCancelled != null)
                {
                    scenarioEvents.onContractCancelled.Add(OnContractUpdated);
                }

                scenarioEvents.onContractCompleted = GameEvents.FindEvent<EventData<Contracts.Contract>>("Contract.Completed");
                if (scenarioEvents.onContractCompleted != null)
                {
                    scenarioEvents.onContractCompleted.Add(OnContractUpdated);
                }

                scenarioEvents.onContractDeclined = GameEvents.FindEvent<EventData<Contracts.Contract>>("Contract.Declined");
                if (scenarioEvents.onContractDeclined != null)
                {
                    scenarioEvents.onContractDeclined.Add(OnContractUpdated);
                }

                scenarioEvents.onContractFailed = GameEvents.FindEvent<EventData<Contracts.Contract>>("Contract.Failed");
                if (scenarioEvents.onContractFailed != null)
                {
                    scenarioEvents.onContractFailed.Add(OnContractUpdated);
                }

                scenarioEvents.onContractFinished = GameEvents.FindEvent<EventData<Contracts.Contract>>("Contract.Finished");
                if (scenarioEvents.onContractFinished != null)
                {
                    scenarioEvents.onContractFinished.Add(OnContractUpdated);
                }

                scenarioEvents.onContractOffered = GameEvents.FindEvent<EventData<Contracts.Contract>>("Contract.Generated");
                if (scenarioEvents.onContractOffered != null)
                {
                    scenarioEvents.onContractOffered.Add(OnContractOffered);
                }

                scenarioEvents.onContractParameterChange = GameEvents.FindEvent<EventData<Contracts.Contract, Contracts.ContractParameter>>("Contract.ParameterChange");
                if (scenarioEvents.onContractParameterChange != null)
                {
                    scenarioEvents.onContractParameterChange.Add(OnContractParameterChange);
                }

                scenarioEvents.onContractRead = GameEvents.FindEvent<EventData<Contracts.Contract>>("Contract.Read");
                if (scenarioEvents.onContractRead != null)
                {
                    scenarioEvents.onContractRead.Add(OnContractUpdated);
                }

                scenarioEvents.onContractSeen = GameEvents.FindEvent<EventData<Contracts.Contract>>("Contract.Seen");
                if (scenarioEvents.onContractSeen != null)
                {
                    scenarioEvents.onContractSeen.Add(OnContractUpdated);
                }

                scenarioEvents.onCustomWaypointLoad = GameEvents.FindEvent<EventData<GameEvents.FromToAction<FinePrint.Waypoint, ConfigNode>>>("onCustomWaypointLoad");
                if (scenarioEvents.onCustomWaypointLoad != null)
                {
                    scenarioEvents.onCustomWaypointLoad.Add(OnCustomWaypointLoad);
                }

                scenarioEvents.onCustomWaypointSave = GameEvents.FindEvent<EventData<GameEvents.FromToAction<FinePrint.Waypoint, ConfigNode>>>("onCustomWaypointSave");
                if (scenarioEvents.onCustomWaypointSave != null)
                {
                    scenarioEvents.onCustomWaypointSave.Add(OnCustomWaypointSave);
                }

                scenarioEvents.OnFundsChanged = GameEvents.FindEvent<EventData<double, TransactionReasons>>("OnFundsChanged");
                if (scenarioEvents.OnFundsChanged != null)
                {
                    scenarioEvents.OnFundsChanged.Add(OnFundsChanged);
                }

                scenarioEvents.OnReputationChanged = GameEvents.FindEvent<EventData<float, TransactionReasons>>("OnReputationChanged");
                if (scenarioEvents.OnReputationChanged != null)
                {
                    scenarioEvents.OnReputationChanged.Add(OnReputationChanged);
                }

                scenarioEvents.OnScienceChanged = GameEvents.FindEvent<EventData<float, TransactionReasons>>("OnScienceChanged");
                if (scenarioEvents.OnScienceChanged != null)
                {
                    scenarioEvents.OnScienceChanged.Add(OnScienceChanged);
                }

                scenarioEvents.OnKSCFacilityUpgraded = GameEvents.FindEvent<EventData<Upgradeables.UpgradeableFacility, int>>("OnKSCFacilityUpgraded");
                if (scenarioEvents.OnKSCFacilityUpgraded != null)
                {
                    scenarioEvents.OnKSCFacilityUpgraded.Add(OnKSCFacilityUpgraded);
                }

                scenarioEvents.OnKSCStructureCollapsed = GameEvents.FindEvent<EventData<DestructibleBuilding>>("OnKSCStructureCollapsed");
                if (scenarioEvents.OnKSCStructureCollapsed != null)
                {
                    scenarioEvents.OnKSCStructureCollapsed.Add(OnKSCStructureCollapsed);
                }

                scenarioEvents.OnKSCStructureRepaired = GameEvents.FindEvent<EventData<DestructibleBuilding>>("OnKSCStructureRepaired");
                if (scenarioEvents.OnKSCStructureRepaired != null)
                {
                    scenarioEvents.OnKSCStructureRepaired.Add(OnKSCStructureRepaired);
                }

                scenarioEvents.OnPartPurchased = GameEvents.FindEvent<EventData<AvailablePart>>("OnPartPurchased");
                if (scenarioEvents.OnPartPurchased != null)
                {
                    scenarioEvents.OnPartPurchased.Add(OnPartPurchased);
                }

                scenarioEvents.OnPartUpgradePurchased = GameEvents.FindEvent<EventData<PartUpgradeHandler.Upgrade>>("OnPartUpgradePurchased");
                if (scenarioEvents.OnPartUpgradePurchased != null)
                {
                    scenarioEvents.OnPartUpgradePurchased.Add(OnPartUpgradePurchased);
                }

                scenarioEvents.OnProgressComplete = GameEvents.FindEvent<EventData<ProgressNode>>("OnProgressComplete");
                if (scenarioEvents.OnProgressComplete != null)
                {
                    scenarioEvents.OnProgressComplete.Add(OnProgressComplete);
                }

                scenarioEvents.OnTechnologyResearched = GameEvents.FindEvent<EventData<GameEvents.HostTargetAction<RDTech, RDTech.OperationResult>>>("OnTechnologyResearched");
                if (scenarioEvents.OnTechnologyResearched != null)
                {
                    scenarioEvents.OnTechnologyResearched.Add(OnTechnologyResearched);
                }

                scenarioEvents.OnScienceRecieved = GameEvents.FindEvent<EventData<float, ScienceSubject, ProtoVessel, bool>>("OnScienceRecieved");
                if (scenarioEvents.OnScienceRecieved != null)
                {
                    scenarioEvents.OnScienceRecieved.Add(OnScienceRecieved);
                }
                */
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

                GameEvents.OnProgressComplete.Remove(OnProgressComplete);

                GameEvents.OnTechnologyResearched.Remove(OnTechnologyResearched);
                GameEvents.OnScienceRecieved.Remove(OnScienceRecieved);
                
                /*
                if (scenarioEvents.onContractAccepted != null)
                {
                    scenarioEvents.onContractAccepted.Remove(OnContractUpdatedWithWeights);
                }

                if (scenarioEvents.onContractCancelled != null)
                {
                    scenarioEvents.onContractCancelled.Remove(OnContractUpdated);
                }

                if (scenarioEvents.onContractCompleted != null)
                {
                    scenarioEvents.onContractCompleted.Remove(OnContractUpdated);
                }

                if (scenarioEvents.onContractDeclined != null)
                {
                    scenarioEvents.onContractDeclined.Remove(OnContractUpdated);
                }

                if (scenarioEvents.onContractFailed != null)
                {
                    scenarioEvents.onContractFailed.Remove(OnContractUpdated);
                }

                if (scenarioEvents.onContractFinished != null)
                {
                    scenarioEvents.onContractFinished.Remove(OnContractUpdated);
                }

                if (scenarioEvents.onContractOffered != null)
                {
                    scenarioEvents.onContractOffered.Remove(OnContractOffered);
                }

                if (scenarioEvents.onContractParameterChange != null)
                {
                    scenarioEvents.onContractParameterChange.Remove(OnContractParameterChange);
                }

                if (scenarioEvents.onContractRead != null)
                {
                    scenarioEvents.onContractRead.Remove(OnContractUpdated);
                }

                if (scenarioEvents.onContractSeen != null)
                {
                    scenarioEvents.onContractSeen.Remove(OnContractUpdated);
                }

                if (scenarioEvents.onCustomWaypointLoad != null)
                {
                    scenarioEvents.onCustomWaypointLoad.Remove(OnCustomWaypointLoad);
                }

                if (scenarioEvents.onCustomWaypointSave != null)
                {
                    scenarioEvents.onCustomWaypointSave.Remove(OnCustomWaypointSave);
                }

                if (scenarioEvents.OnFundsChanged != null)
                {
                    scenarioEvents.OnFundsChanged.Remove(OnFundsChanged);
                }

                if (scenarioEvents.OnReputationChanged != null)
                {
                    scenarioEvents.OnReputationChanged.Remove(OnReputationChanged);
                }

                if (scenarioEvents.OnScienceChanged != null)
                {
                    scenarioEvents.OnScienceChanged.Remove(OnScienceChanged);
                }

                if (scenarioEvents.OnKSCFacilityUpgraded != null)
                {
                    scenarioEvents.OnKSCFacilityUpgraded.Remove(OnKSCFacilityUpgraded);
                }

                if (scenarioEvents.OnKSCStructureCollapsed != null)
                {
                    scenarioEvents.OnKSCStructureCollapsed.Remove(OnKSCStructureCollapsed);
                }

                if (scenarioEvents.OnKSCStructureRepaired != null)
                {
                    scenarioEvents.OnKSCStructureRepaired.Remove(OnKSCStructureRepaired);
                }

                if (scenarioEvents.OnPartPurchased != null)
                {
                    scenarioEvents.OnPartPurchased.Remove(OnPartPurchased);
                }

                if (scenarioEvents.OnPartUpgradePurchased != null)
                {
                    scenarioEvents.OnPartUpgradePurchased.Remove(OnPartUpgradePurchased);
                }

                if (scenarioEvents.OnProgressComplete != null)
                {
                    scenarioEvents.OnProgressComplete.Remove(OnProgressComplete);
                }

                if (scenarioEvents.OnTechnologyResearched != null)
                {
                    scenarioEvents.OnTechnologyResearched.Remove(OnTechnologyResearched);
                }

                if (scenarioEvents.OnScienceRecieved != null)
                {
                    scenarioEvents.OnScienceRecieved.Remove(OnScienceRecieved);
                }
                */
            }
            catch (Exception e)
            {
                SyncrioLog.Debug("Error unregistering  scenario events! Error: " + e);
            }
        }
        
        private void OnContractUpdatedWithWeights(Contracts.Contract contract)
        {
            if (cooldown)
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
            }
        }

        private void OnContractUpdated(Contracts.Contract contract)
        {
            if (cooldown)
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

        private void OnContractOffered(Contracts.Contract contract)
        {
            if (cooldown)
            {
                return;
            }

            if (GroupSystem.playerGroupAssigned)
            {
                string groupName = GroupSystem.playerGroupName;

                if (!string.IsNullOrEmpty(groupName))
                {
                    if (!LockSystem.fetch.LockIsOurs("contract-spawn-" + groupName))
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
            if (cooldown)
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
            if (cooldown)
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
            if (cooldown)
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

            if (cooldown)
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

            if (cooldown)
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

            if (cooldown)
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
            if (cooldown)
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
            if (cooldown)
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
            if (cooldown)
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
            if (cooldown)
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
            if (cooldown)
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

        private void OnProgressComplete(ProgressNode pn)
        {
            if (cooldown)
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

                        mw.Write<string>(pn.Id);
                        
                        ConfigNode pnCFG = new ConfigNode();
                        pn.Save(pnCFG);
                        byte[] pnData = ConfigNodeSerializer.fetch.Serialize(pnCFG);

                        mw.Write<byte[]>(pnData);

                        data = mw.GetMessageBytes();
                    }

                    SendData((int)ScenarioDataType.PROGRESS_COMPLETE, data);
                }
            }
            else
            {
                byte[] data;

                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<bool>(false);//In group

                    mw.Write<string>(pn.Id);

                    ConfigNode pnCFG = new ConfigNode();
                    pn.Save(pnCFG);
                    byte[] pnData = ConfigNodeSerializer.fetch.Serialize(pnCFG);

                    mw.Write<byte[]>(pnData);

                    data = mw.GetMessageBytes();
                }

                SendData((int)ScenarioDataType.PROGRESS_COMPLETE, data);
            }
        }

        private void OnTechnologyResearched(GameEvents.HostTargetAction<RDTech, RDTech.OperationResult> tech)
        {
            if (cooldown)
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
            if (cooldown)
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

        private void SendData(int type, byte[] data)
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
            }
        }

        /*
        class ScenarioEvents
        {
            //Contracts
            public EventData<Contracts.Contract> onContractAccepted;//"Contract.Accepted"
            public EventData<Contracts.Contract> onContractCancelled;//"Contract.Cancelled"
            public EventData<Contracts.Contract> onContractCompleted;//"Contract.Completed"
            public EventData<Contracts.Contract> onContractDeclined;//"Contract.Declined"
            public EventData<Contracts.Contract> onContractFailed;//"Contract.Failed"
            public EventData<Contracts.Contract> onContractFinished;//"Contract.Finished"
            public EventData<Contracts.Contract> onContractOffered;//"Contract.Generated"
            public EventData<Contracts.Contract, Contracts.ContractParameter> onContractParameterChange;//"Contract.ParameterChange"
            public EventData<Contracts.Contract> onContractRead;//"Contract.Read"
            public EventData<Contracts.Contract> onContractSeen;//"Contract.Seen"
            //Custom Waypoints
            public EventData<GameEvents.FromToAction<FinePrint.Waypoint, ConfigNode>> onCustomWaypointLoad;//"onCustomWaypointLoad"
            public EventData<GameEvents.FromToAction<FinePrint.Waypoint, ConfigNode>> onCustomWaypointSave;//"onCustomWaypointSave"
            //Currency
            public EventData<double, TransactionReasons> OnFundsChanged;//"OnFundsChanged"
            public EventData<float, TransactionReasons> OnReputationChanged;//"OnReputationChanged"
            public EventData<float, TransactionReasons> OnScienceChanged;//"OnScienceChanged"
            //Facilities
            public EventData<Upgradeables.UpgradeableFacility, int> OnKSCFacilityUpgraded;//"OnKSCFacilityUpgraded"
            public EventData<DestructibleBuilding> OnKSCStructureCollapsed;//"OnKSCStructureCollapsed"
            public EventData<DestructibleBuilding> OnKSCStructureRepaired;//"OnKSCStructureRepaired"
            //Parts
            public EventData<AvailablePart> OnPartPurchased;//"OnPartPurchased"
            public EventData<PartUpgradeHandler.Upgrade> OnPartUpgradePurchased;//"OnPartUpgradePurchased"
            //Progress
            public EventData<ProgressNode> OnProgressComplete;//"OnProgressComplete"
            //Technology
            public EventData<GameEvents.HostTargetAction<RDTech, RDTech.OperationResult>> OnTechnologyResearched;//"OnTechnologyResearched"
            //Science
            public EventData<float, ScienceSubject, ProtoVessel, bool> OnScienceRecieved;//"OnScienceRecieved"
        }
        */
    }
}
