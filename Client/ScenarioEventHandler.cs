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
        private Dictionary<string, ConfigNode> lastPrograssData = new Dictionary<string, ConfigNode>();//<Progress Id, Progress Data>
        private float lastSync = 0.0f;
        private float syncCooldown = 0.2f;
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

                GameEvents.onProgressNodeSave.Add(OnProgressNodeSave);

                GameEvents.OnTechnologyResearched.Add(OnTechnologyResearched);
                GameEvents.OnScienceRecieved.Add(OnScienceRecieved);
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
            }
            catch (Exception e)
            {
                SyncrioLog.Debug("Error unregistering  scenario events! Error: " + e);
            }
        }
        
        private void OnContractUpdatedWithWeights(Contracts.Contract contract)
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
                lastPrograssData.Add(dataNode.from.Id, dataNode.to);

                send = true;
            }
            else
            {
                if (dataNode.to != null)
                {
                    if (dataNode.to != lastPrograssData[dataNode.from.Id])
                    {
                        lastPrograssData[dataNode.from.Id] = dataNode.to;
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
                singleton.delaySync = true;
            }
        }
    }
}
