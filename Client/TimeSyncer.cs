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
using UnityEngine;
using SyncrioCommon;
using MessageStream2;

namespace SyncrioClientSide
{
    public class TimeSyncer
    {
        public bool disabled
        {
            get;
            set;
        }

        public bool synced
        {
            get;
            private set;
        }

        public bool workerEnabled;

        public bool locked
        {
            get
            {
                return true;
            }
        }

        public long clockOffsetAverage
        {
            get;
            private set;
        }

        public long networkLatencyAverage
        {
            get;
            private set;
        }

        public long serverLag
        {
            get;
            private set;
        }

        private const float MAX_CLOCK_SKEW = 5f;
        private const float MIN_CLOCK_RATE = 0.3f;
        private const float MAX_CLOCK_RATE = 1.5f;
        private const float SYNC_TIME_INTERVAL = 30f;
        private const float CLOCK_SET_INTERVAL = .1f;
        private const int SYNC_TIME_VALID = 4;
        private const int SYNC_TIME_MAX = 10;
        private float lastSyncTime = 0f;
        private float lastClockSkew = 0f;
        private List<long> clockOffset = new List<long>();
        private List<long> networkLatency = new List<long>();
        private static TimeSyncer singleton;

        public static TimeSyncer fetch
        {
            get
            {
                return singleton;
            }
        }

        public void FixedUpdate()
        {
            if (!workerEnabled)
            {
                return;
            }

            if (!synced)
            {
                return;
            }

            if ((UnityEngine.Time.realtimeSinceStartup - lastSyncTime) > SYNC_TIME_INTERVAL)
            {
                lastSyncTime = UnityEngine.Time.realtimeSinceStartup;
                NetworkWorker.fetch.SendTimeSync();
            }

            //Mod API to disable the time syncer
            if (disabled)
            {
                return;
            }
            
            if (locked)
            {
                //Set the Scenario time here
                SyncTime();
            }
        }

        //Skew or set the clock
        private void SyncTime()
        {
            if (Time.timeSinceLevelLoad < 1f)
            {
                return;
            }

            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                if (!FlightGlobals.ready)
                {
                    return;
                }
                if (FlightGlobals.fetch.activeVessel == null)
                {
                    return;
                }
            }

            if ((UnityEngine.Time.realtimeSinceStartup - lastClockSkew) > CLOCK_SET_INTERVAL)
            {
                lastClockSkew = UnityEngine.Time.realtimeSinceStartup;
                if (CanSyncTime())
                {
                    //No Subspace Timewarp!
                }
                else
                {
                    Time.timeScale = 1f;
                }
            }
        }

        private void StepClock(double targetTick)
        {
            if (HighLogic.LoadedScene == GameScenes.LOADING)
            {
                SyncrioLog.Debug("Skipping StepClock in loading screen");
                return;
            }
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (FlightGlobals.fetch.activeVessel == null || !FlightGlobals.ready)
                {
                    SyncrioLog.Debug("Skipping StepClock (active vessel is null or not ready)");
                    return;
                }
                try
                {
                    OrbitPhysicsManager.HoldVesselUnpack(5);
                }
                catch
                {
                    SyncrioLog.Debug("Failed to hold vessel unpack");
                    return;
                }
                foreach (Vessel v in FlightGlobals.fetch.vessels)
                {
                    if (!v.packed)
                    {
                        if (v != FlightGlobals.fetch.activeVessel)
                        {
                            try
                            {
                                v.GoOnRails();
                            }
                            catch
                            {
                                SyncrioLog.Debug("Error packing vessel " + v.id.ToString());
                            }
                        }
                        if (v == FlightGlobals.fetch.activeVessel)
                        {
                            if (SafeToStepClock(v, targetTick))
                            {
                                try
                                {
                                    v.GoOnRails();
                                }
                                catch
                                {
                                    SyncrioLog.Debug("Error packing active vessel " + v.id.ToString());
                                }
                            }
                        }
                    }
                }
            }
            Planetarium.SetUniversalTime(targetTick);
        }

        private bool SafeToStepClock(Vessel checkVessel, double targetTick)
        {
            switch (checkVessel.situation)
            {
                case Vessel.Situations.LANDED:
                case Vessel.Situations.PRELAUNCH:
                case Vessel.Situations.SPLASHED:
                    return (checkVessel.srf_velocity.magnitude < 2);
                case Vessel.Situations.ORBITING:
                case Vessel.Situations.ESCAPING:
                    return true;
                case Vessel.Situations.SUB_ORBITAL:
                    double altitudeAtUT = checkVessel.orbit.getRelativePositionAtUT(targetTick).magnitude;
                    return (altitudeAtUT > checkVessel.mainBody.Radius + 10000 && checkVessel.altitude > 10000);
                default :
                    return false;
            }
        }

        private bool SituationIsGrounded(Vessel.Situations situation)
        {
            switch (situation)
            {
                case Vessel.Situations.LANDED:
                case Vessel.Situations.PRELAUNCH:
                case Vessel.Situations.SPLASHED:
                    return true;
            }
            return false;
        }

        public long GetServerClock()
        {
            if (synced)
            {
                return DateTime.UtcNow.Ticks + clockOffsetAverage;
            }
            return 0;
        }

        private bool CanSyncTime()
        {
            bool canSync;
            switch (HighLogic.LoadedScene)
            {
                case GameScenes.TRACKSTATION:
                case GameScenes.FLIGHT:
                case GameScenes.SPACECENTER:
                    canSync = true;
                    break;
                default :
                    canSync = false;
                    break;
            }
            return canSync;
        }

        public void HandleSyncTime(long clientSend, long serverReceive, long serverSend)
        {
            long clientReceive = DateTime.UtcNow.Ticks;
            long clientLatency = (clientReceive - clientSend) - (serverSend - serverReceive);
            long clientOffset = ((serverReceive - clientSend) + (serverSend - clientReceive)) / 2;
            clockOffset.Add(clientOffset);
            networkLatency.Add(clientLatency);
            serverLag = serverSend - serverReceive;
            if (clockOffset.Count > SYNC_TIME_MAX)
            {
                clockOffset.RemoveAt(0);
            }
            if (networkLatency.Count > SYNC_TIME_MAX)
            {
                networkLatency.RemoveAt(0);
            }
            long clockOffsetTotal = 0;
            //Calculate the average for the offset and latency.
            foreach (long currentOffset in clockOffset)
            {
                clockOffsetTotal += currentOffset;
            }
            clockOffsetAverage = clockOffsetTotal / clockOffset.Count;

            long networkLatencyTotal = 0;
            foreach (long currentLatency in networkLatency)
            {
                networkLatencyTotal += currentLatency;
            }
            networkLatencyAverage = networkLatencyTotal / networkLatency.Count;

            //Check if we are now synced
            if ((clockOffset.Count > SYNC_TIME_VALID) && !synced)
            {
                synced = true;
                float clockOffsetAverageMs = clockOffsetAverage / 10000f;
                float networkLatencyMs = networkLatencyAverage / 10000f;
                SyncrioLog.Debug("Initial clock syncronized, offset " + clockOffsetAverageMs + "ms, latency " + networkLatencyMs + "ms");
            }

            //Ask for another time sync if we aren't synced yet.
            if (!synced)
            {
                lastSyncTime = UnityEngine.Time.realtimeSinceStartup;
                NetworkWorker.fetch.SendTimeSync();
            }
        }

        public static void Reset()
        {
            lock (Client.eventLock)
            {
                if (singleton != null)
                {
                    singleton.workerEnabled = false;
                    Client.fixedUpdateEvent.Remove(singleton.FixedUpdate);
                    Time.timeScale = 1f;
                }
                singleton = new TimeSyncer();
                Client.fixedUpdateEvent.Add(singleton.FixedUpdate);
            }
        }
    }
}

