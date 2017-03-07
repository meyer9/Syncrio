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

        public int currentSubspace
        {
            get;
            private set;
        }

        public bool locked
        {
            get
            {
                return lockedSubspace != null;
            }
        }

        public Subspace lockedSubspace
        {
            get;
            private set;
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

        public float requestedRate
        {
            get;
            private set;
        }

        private const float MAX_CLOCK_SKEW = 5f;
        private const float MAX_SUBSPACE_RATE = 1f;
        private const float MIN_SUBSPACE_RATE = 0.3f;
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
        private List<float> requestedRatesList = new List<float>();
        private Dictionary<int, Subspace> subspaces = new Dictionary<int, Subspace>();
        private static TimeSyncer singleton;

        public TimeSyncer()
        {
            currentSubspace = -1;
            requestedRate = 1f;
        }

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
                if (NetworkWorker.fetch.state != ClientState.DISCONNECTING)
                {
                    NetworkWorker.fetch.SendTimeSync();
                }
            }

            //Mod API to disable the time syncer
            if (disabled)
            {
                return;
            }

            if (WarpWorker.fetch.warpMode == WarpMode.SUBSPACE)
            {
                if (Settings.fetch.DarkMultiPlayerCoopMode)
                {
                    VesselWorker.fetch.DetectDMPSync();
                }
            }

            if (locked)
            {
                if (!Settings.fetch.DarkMultiPlayerCoopMode)
                {
                    if (WarpWorker.fetch.warpMode == WarpMode.SUBSPACE)
                    {
                        VesselWorker.fetch.DetectReverting();
                    }
                    //Set the Universe time here
                    SyncTime();
                }
            }
        }

        //Skew or set the clock
        private void SyncTime()
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT)
            {
                if (requestedRatesList.Count > 0)
                {
                    requestedRatesList.Clear();
                    requestedRate = 1f;
                }
            }

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
                    double targetTime = GetUniverseTime();
                    double currentError = GetCurrentError();
                    if (Math.Abs(currentError) > MAX_CLOCK_SKEW)
                    {
                        StepClock(targetTime);
                    }
                    else
                    {
                        SkewClock(currentError);
                    }
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
            Planetarium.SetUniversalTime(targetTick);
        }

        private void SkewClock(double currentError)
        {
            float timeWarpRate = (float)Math.Pow(2, -currentError);
            if (timeWarpRate > MAX_CLOCK_RATE)
            {
                timeWarpRate = MAX_CLOCK_RATE;
            }
            if (timeWarpRate < MIN_CLOCK_RATE)
            {
                timeWarpRate = MIN_CLOCK_RATE;
            }
            //Request how fast we *think* we can run (The reciporical of the current warp rate)
            float tempRequestedRate = lockedSubspace.subspaceSpeed * (1 / timeWarpRate);
            if (tempRequestedRate > MAX_SUBSPACE_RATE)
            {
                tempRequestedRate = MAX_SUBSPACE_RATE;
            }
            requestedRatesList.Add(tempRequestedRate);
            //Delete entries if there are too many
            while (requestedRatesList.Count > 50)
            {
                requestedRatesList.RemoveAt(0);
            }
            //Set the average requested rate
            float requestedRateTotal = 0f;
            foreach (float requestedRateEntry in requestedRatesList)
            {
                requestedRateTotal += requestedRateEntry;
            }
            requestedRate = requestedRateTotal / requestedRatesList.Count;

            //Set the physwarp rate
            Time.timeScale = timeWarpRate;
        }

        public void AddNewSubspace(int subspaceID, long serverTime, double planetariumTime, float subspaceSpeed)
        {
            Subspace newSubspace = new Subspace();
            newSubspace.serverClock = serverTime;
            newSubspace.planetTime = planetariumTime;
            newSubspace.subspaceSpeed = subspaceSpeed;
            subspaces[subspaceID] = newSubspace;
            if (currentSubspace == subspaceID)
            {
                LockSubspace(currentSubspace);
            }
            SyncrioLog.Debug("Subspace " + subspaceID + " locked to server, time: " + planetariumTime);
        }

        public void LockTemporarySubspace(long serverClock, double planetTime, float subspaceSpeed)
        {
            Subspace tempSubspace = new Subspace();
            tempSubspace.serverClock = serverClock;
            tempSubspace.planetTime = planetTime;
            tempSubspace.subspaceSpeed = subspaceSpeed;
            lockedSubspace = tempSubspace;
        }

        public void LockSubspace(int subspaceID)
        {
            if (subspaces.ContainsKey(subspaceID))
            {
                TimeWarp.SetRate(0, true);
                lockedSubspace = subspaces[subspaceID];
                SyncrioLog.Debug("Locked to subspace " + subspaceID + ", time: " + GetUniverseTime());
                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<int>((int)WarpMessageType.CHANGE_SUBSPACE);
                    mw.Write<int>(subspaceID);
                    NetworkWorker.fetch.SendWarpMessage(mw.GetMessageBytes());
                }
            }
            currentSubspace = subspaceID;
        }

        public void UnlockSubspace()
        {
            currentSubspace = -1;
            lockedSubspace = null;
            Time.timeScale = 1f;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)WarpMessageType.CHANGE_SUBSPACE);
                mw.Write<int>(currentSubspace);
                NetworkWorker.fetch.SendWarpMessage(mw.GetMessageBytes());
            }
        }

        public void RelockSubspace(int subspaceID, long serverClock, double planetTime, float subspaceSpeed)
        {
            if (subspaces.ContainsKey(subspaceID))
            {
                subspaces[subspaceID].serverClock = serverClock;
                subspaces[subspaceID].planetTime = planetTime;
                subspaces[subspaceID].subspaceSpeed = subspaceSpeed;
            }
            else
            {
                SyncrioLog.Debug("Failed to relock non-existant subspace " + subspaceID);
            }
        }

        public long GetServerClock()
        {
            if (synced)
            {
                return DateTime.UtcNow.Ticks + clockOffsetAverage;
            }
            return 0;
        }

        public double GetUniverseTime()
        {
            if (synced && locked)
            {
                return GetUniverseTime(lockedSubspace);
            }
            return 0;
        }

        public double GetUniverseTime(int subspace)
        {
            if (subspaces.ContainsKey(subspace))
            {
                return GetUniverseTime(subspaces[subspace]);
            }
            else
            {
                return 0;
            }
        }

        public double GetUniverseTime(Subspace subspace)
        {
            long realTimeSinceLock = GetServerClock() - subspace.serverClock;
            double realTimeSinceLockSeconds = realTimeSinceLock / 10000000d;
            double adjustedTimeSinceLockSeconds = realTimeSinceLockSeconds * subspace.subspaceSpeed;
            return subspace.planetTime + adjustedTimeSinceLockSeconds;
        }

        public double GetCurrentError()
        {
            if (synced && locked)
            {
                double currentTime = Planetarium.GetUniversalTime();
                double targetTime = GetUniverseTime();
                return (currentTime - targetTime);
            }
            return 0;
        }

        public bool SubspaceExists(int subspaceID)
        {
            return subspaces.ContainsKey(subspaceID);
        }

        public Subspace GetSubspace(int subspaceID)
        {
            Subspace ss = new Subspace();
            if (subspaces.ContainsKey(subspaceID))
            {
                ss.serverClock = subspaces[subspaceID].serverClock;
                ss.planetTime = subspaces[subspaceID].planetTime;
                ss.subspaceSpeed = subspaces[subspaceID].subspaceSpeed;
            }
            return ss;
        }

        public int GetMostAdvancedSubspace()
        {
            double highestTime = double.NegativeInfinity;
            int retVal = -1;
            foreach (int subspaceID in subspaces.Keys)
            {
                double testTime = GetUniverseTime(subspaceID);
                if (testTime > highestTime)
                {
                    highestTime = testTime;
                    retVal = subspaceID;
                }
            }
            return retVal;
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

