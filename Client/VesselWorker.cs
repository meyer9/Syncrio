/*   Syncrio License
 *   
 *   Copyright � 2016 Caleb Huyck
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
using System.Reflection;
using UnityEngine;
using SyncrioCommon;

namespace SyncrioClientSide
{
    public class VesselWorker
    {
        public bool workerEnabled;
        //Hooks enabled
        private static VesselWorker singleton;
        //Update frequency
        private const float VESSEL_PROTOVESSEL_UPDATE_INTERVAL = 30f;
        public float safetyBubbleDistance = 100f;
        //Spectate stuff
        private const string Syncrio_SPECTATE_LOCK = "Syncrio_Spectating";
        private const float UPDATE_SCREEN_MESSAGE_INTERVAL = 1f;
        //Incoming queue
        private object updateQueueLock = new object();
        private Dictionary<string, Queue<KerbalEntry>> kerbalProtoQueue = new Dictionary<string, Queue<KerbalEntry>>();
        //Incoming revert support
        private Dictionary<string, List<KerbalEntry>> kerbalProtoHistory = new Dictionary<string, List<KerbalEntry>>();
        private Dictionary<string, double> kerbalProtoHistoryTime = new Dictionary<string, double>();
        //Vessel tracking
        private HashSet<Guid> serverVessels = new HashSet<Guid>();
        private Dictionary<Guid, bool> vesselPartsOk = new Dictionary<Guid, bool>();
        //Vessel state tracking
        private Dictionary<Guid, int> vesselPartCount = new Dictionary<Guid, int>();
        private Dictionary<Guid, string> vesselNames = new Dictionary<Guid, string>();
        private Dictionary<Guid, VesselType> vesselTypes = new Dictionary<Guid, VesselType>();
        private Dictionary<Guid, Vessel.Situations> vesselSituations = new Dictionary<Guid, Vessel.Situations>();
        //Known kerbals
        private Dictionary<string, string> serverKerbals = new Dictionary<string, string>();
        //Known vessels and last send/receive time
        private Dictionary<Guid, float> serverVesselsProtoUpdate = new Dictionary<Guid, float>();
        private Dictionary<Guid, float> serverVesselsPositionUpdate = new Dictionary<Guid, float>();
        //Track when the vessel was last controlled.
        private Dictionary<Guid, double> latestVesselUpdate = new Dictionary<Guid, double>();
        private Dictionary<Guid, double> latestUpdateSent = new Dictionary<Guid, double>();
        //KillVessel tracking
        private Dictionary<Guid, double> lastKillVesselDestroy = new Dictionary<Guid, double>();
        private Dictionary<Guid, double> lastLoadVessel = new Dictionary<Guid, double>();
        private List<Vessel> delayKillVessels = new List<Vessel>();
        //System.Reflection hackiness for loading kerbals into the crew roster:
        private delegate bool AddCrewMemberToRosterDelegate(ProtoCrewMember pcm);

        private AddCrewMemberToRosterDelegate AddCrewMemberToRoster;

        public static VesselWorker fetch
        {
            get
            {
                return singleton;
            }
        }

        public void SendKerbalIfDifferent(ProtoCrewMember pcm)
        {
            if (pcm.type == ProtoCrewMember.KerbalType.Tourist)
            {
                //Don't send tourists
                SyncrioLog.Debug("Skipping sending of tourist: " + pcm.name);
                return;
            }
            ConfigNode kerbalNode = new ConfigNode();
            pcm.Save(kerbalNode);
            byte[] kerbalBytes = ConfigNodeSerializer.fetch.Serialize(kerbalNode);
            if (kerbalBytes == null || kerbalBytes.Length == 0)
            {
                SyncrioLog.Debug("VesselWorker: Error sending kerbal - bytes are null or 0");
                return;
            }
            string kerbalHash = Common.CalculateSHA256Hash(kerbalBytes);
            bool kerbalDifferent = false;
            if (!serverKerbals.ContainsKey(pcm.name))
            {
                //New kerbal
                SyncrioLog.Debug("Found new kerbal, sending...");
                kerbalDifferent = true;
            }
            else if (serverKerbals[pcm.name] != kerbalHash)
            {
                SyncrioLog.Debug("Found changed kerbal (" + pcm.name + "), sending...");
                kerbalDifferent = true;
            }
            if (kerbalDifferent)
            {
                serverKerbals[pcm.name] = kerbalHash;
                NetworkWorker.fetch.SendKerbalProtoMessage(pcm.name, kerbalBytes);
            }
        }
        //Called from main
        public void LoadKerbalsIntoGame()
        {
            SyncrioLog.Debug("Loading kerbals into game");
            MethodInfo addMemberToCrewRosterMethod = typeof(KerbalRoster).GetMethod("AddCrewMember", BindingFlags.Public | BindingFlags.Instance);
            AddCrewMemberToRoster = (AddCrewMemberToRosterDelegate)Delegate.CreateDelegate(typeof(AddCrewMemberToRosterDelegate), HighLogic.CurrentGame.CrewRoster, addMemberToCrewRosterMethod);
            if (AddCrewMemberToRoster == null)
            {
                throw new Exception("Failed to load AddCrewMember delegate!");
            }

            foreach (KeyValuePair<string, Queue<KerbalEntry>> kerbalQueue in kerbalProtoQueue)
            {
                while (kerbalQueue.Value.Count > 0)
                {
                    KerbalEntry kerbalEntry = kerbalQueue.Value.Dequeue();
                    LoadKerbal(kerbalEntry.kerbalNode);
                }
            }

            if (serverKerbals.Count == 0)
            {
                KerbalRoster newRoster = KerbalRoster.GenerateInitialCrewRoster(HighLogic.CurrentGame.Mode);
                foreach (ProtoCrewMember pcm in newRoster.Crew)
                {
                    AddCrewMemberToRoster(pcm);
                    SendKerbalIfDifferent(pcm);
                }
            }

            int generateKerbals = 0;
            if (serverKerbals.Count < 20)
            {
                generateKerbals = 20 - serverKerbals.Count;
                SyncrioLog.Debug("Generating " + generateKerbals + " new kerbals");
            }

            while (generateKerbals > 0)
            {
                ProtoCrewMember protoKerbal = HighLogic.CurrentGame.CrewRoster.GetNewKerbal(ProtoCrewMember.KerbalType.Crew);
                SendKerbalIfDifferent(protoKerbal);
                generateKerbals--;
            }
            SyncrioLog.Debug("Kerbals loaded");
        }

        private void LoadKerbal(ConfigNode crewNode)
        {
            if (crewNode == null)
            {
                SyncrioLog.Debug("crewNode is null!");
                return;
            }
            ProtoCrewMember protoCrew = new ProtoCrewMember(HighLogic.CurrentGame.Mode, crewNode);
            if (protoCrew == null)
            {
                SyncrioLog.Debug("protoCrew is null!");
                return;
            }
            if (String.IsNullOrEmpty(protoCrew.name))
            {
                SyncrioLog.Debug("protoName is blank!");
                return;
            }
            protoCrew.type = ProtoCrewMember.KerbalType.Crew;
            if (!HighLogic.CurrentGame.CrewRoster.Exists(protoCrew.name))
            {
                AddCrewMemberToRoster(protoCrew);
                ConfigNode kerbalNode = new ConfigNode();
                protoCrew.Save(kerbalNode);
                byte[] kerbalBytes = ConfigNodeSerializer.fetch.Serialize(kerbalNode);
                if (kerbalBytes != null && kerbalBytes.Length != 0)
                {
                    serverKerbals[protoCrew.name] = Common.CalculateSHA256Hash(kerbalBytes);
                }
            }
            else
            {
                ConfigNode careerLogNode = crewNode.GetNode("CAREER_LOG");
                if (careerLogNode != null)
                {
                    //Insert wolf howling at the moon here
                    HighLogic.CurrentGame.CrewRoster[protoCrew.name].careerLog.Entries.Clear();
                    HighLogic.CurrentGame.CrewRoster[protoCrew.name].careerLog.Load(careerLogNode);
                }
                else
                {
                    SyncrioLog.Debug("Career log node for " + protoCrew.name + " is empty!");
                }

                ConfigNode flightLogNode = crewNode.GetNode("FLIGHT_LOG");
                if (flightLogNode != null)
                {
                    //And here. Someone "cannot into" lists and how to protect them.
                    HighLogic.CurrentGame.CrewRoster[protoCrew.name].careerLog.Entries.Clear();
                    HighLogic.CurrentGame.CrewRoster[protoCrew.name].careerLog.Load(careerLogNode);
                }
                HighLogic.CurrentGame.CrewRoster[protoCrew.name].courage = protoCrew.courage;
                HighLogic.CurrentGame.CrewRoster[protoCrew.name].experience = protoCrew.experience;
                HighLogic.CurrentGame.CrewRoster[protoCrew.name].experienceLevel = protoCrew.experienceLevel;
                HighLogic.CurrentGame.CrewRoster[protoCrew.name].gender = protoCrew.gender;
                HighLogic.CurrentGame.CrewRoster[protoCrew.name].hasToured = protoCrew.hasToured;
                HighLogic.CurrentGame.CrewRoster[protoCrew.name].isBadass = protoCrew.isBadass;
                HighLogic.CurrentGame.CrewRoster[protoCrew.name].rosterStatus = protoCrew.rosterStatus;
                HighLogic.CurrentGame.CrewRoster[protoCrew.name].seat = protoCrew.seat;
                HighLogic.CurrentGame.CrewRoster[protoCrew.name].seatIdx = protoCrew.seatIdx;
                HighLogic.CurrentGame.CrewRoster[protoCrew.name].stupidity = protoCrew.stupidity;
                HighLogic.CurrentGame.CrewRoster[protoCrew.name].UTaR = protoCrew.UTaR;
            }
        }

        private void DodgeVesselLandedStatus(ConfigNode vesselNode)
        {
            if (vesselNode != null)
            {
                string situation = vesselNode.GetValue("sit");
                switch (situation)
                {
                    case "LANDED":
                        vesselNode.SetValue("landed", "True");
                        vesselNode.SetValue("splashed", "False");
                        break;
                    case "SPLASHED":
                        vesselNode.SetValue("splashed", "True");
                        vesselNode.SetValue("landed", "False");
                        break;
                }
            }
        }

        private string DodgeValueIfNeeded(string input)
        {
            string boolValue = input.Substring(0, input.IndexOf(", "));
            string timeValue = input.Substring(input.IndexOf(", ") + 1);
            double vesselPlanetTime = Double.Parse(timeValue);
            double currentPlanetTime = Planetarium.GetUniversalTime();
            if (vesselPlanetTime > currentPlanetTime)
            {
                return boolValue + ", " + currentPlanetTime;
            }
            return input;
        }

        public void SendKerbalsInVessel(ProtoVessel vessel)
        {
            if (vessel == null)
            {
                return;
            }
            if (vessel.protoPartSnapshots == null)
            {
                return;
            }
            foreach (ProtoPartSnapshot part in vessel.protoPartSnapshots)
            {
                if (part == null)
                {
                    continue;
                }
                foreach (ProtoCrewMember pcm in part.protoModuleCrew)
                {
                    SendKerbalIfDifferent(pcm);
                }
            }
        }

        public void SendKerbalsInVessel(Vessel vessel)
        {
            if (vessel == null)
            {
                return;
            }
            if (vessel.parts == null)
            {
                return;
            }
            foreach (Part part in vessel.parts)
            {
                if (part == null)
                {
                    continue;
                }
                foreach (ProtoCrewMember pcm in part.protoModuleCrew)
                {
                    SendKerbalIfDifferent(pcm);
                }
            }
        }
        //Called from networkWorker
        public void QueueKerbal(double planetTime, string kerbalName, ConfigNode kerbalNode)
        {
            lock (updateQueueLock)
            {
                KerbalEntry newEntry = new KerbalEntry();
                newEntry.planetTime = planetTime;
                newEntry.kerbalNode = kerbalNode;
                if (!kerbalProtoQueue.ContainsKey(kerbalName))
                {
                    kerbalProtoQueue.Add(kerbalName, new Queue<KerbalEntry>());
                }

                Queue<KerbalEntry> keQueue = kerbalProtoQueue[kerbalName];
                if (kerbalProtoHistoryTime.ContainsKey(kerbalName))
                {
                    //If we get a remove older than the current queue peek, then someone has gone back in time and the timeline needs to be fixed.
                    if (planetTime < kerbalProtoHistoryTime[kerbalName])
                    {
                        SyncrioLog.Debug("Kerbal " + kerbalName + " went back in time - rewriting the remove history for it.");
                        Queue<KerbalEntry> newQueue = new Queue<KerbalEntry>();
                        while (keQueue.Count > 0)
                        {
                            KerbalEntry oldKe = keQueue.Dequeue();
                            //Save the updates from before the revert
                            if (oldKe.planetTime < planetTime)
                            {
                                newQueue.Enqueue(oldKe);
                            }
                        }
                        keQueue = newQueue;
                        kerbalProtoQueue[kerbalName] = newQueue;
                        //Clean the history too
                        if (Settings.fetch.revertEnabled)
                        {
                            if (kerbalProtoHistory.ContainsKey(kerbalName))
                            {
                                List<KerbalEntry> keh = kerbalProtoHistory[kerbalName];
                                foreach (KerbalEntry oldKe in keh.ToArray())
                                {
                                    if (oldKe.planetTime > planetTime)
                                    {
                                        keh.Remove(oldKe);
                                    }
                                }
                            }
                        }
                    }
                }

                keQueue.Enqueue(newEntry);
                if (Settings.fetch.revertEnabled)
                {
                    if (!kerbalProtoHistory.ContainsKey(kerbalName))
                    {
                        kerbalProtoHistory.Add(kerbalName, new List<KerbalEntry>());
                    }
                    kerbalProtoHistory[kerbalName].Add(newEntry);
                }
                kerbalProtoHistoryTime[kerbalName] = planetTime;
            }
        }
        public static void Reset()
        {
            lock (Client.eventLock)
            {
                singleton = new VesselWorker();
            }
        }
    }
    class KerbalEntry
    {
        public double planetTime;
        public ConfigNode kerbalNode;
    }
}

