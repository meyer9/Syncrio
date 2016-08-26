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
using SyncrioCommon;

namespace SyncrioClientSide
{
    public class PlayerStatusWorker
    {
        public bool workerEnabled;
        private static PlayerStatusWorker singleton;
        private Queue<PlayerStatus> addStatusQueue = new Queue<PlayerStatus>();
        private Queue<string> removeStatusQueue = new Queue<string>();
        public PlayerStatus myPlayerStatus;
        private PlayerStatus lastPlayerStatus = new PlayerStatus();
        public List<PlayerStatus> playerStatusList = new List<PlayerStatus>();
        private const float PLAYER_STATUS_CHECK_INTERVAL = .2f;
        private const float PLAYER_STATUS_SEND_THROTTLE = 1f;
        private float lastPlayerStatusSend = 0f;
        private float lastPlayerStatusCheck = 0f;

        public PlayerStatusWorker()
        {
            myPlayerStatus = new PlayerStatus();
            myPlayerStatus.playerName = Settings.fetch.playerName;
            myPlayerStatus.statusText = "Syncing";
        }

        public static PlayerStatusWorker fetch
        {
            get
            {
                return singleton;
            }
        }

        private void Update()
        {
            if (workerEnabled)
            {
                if ((UnityEngine.Time.realtimeSinceStartup - lastPlayerStatusCheck) > PLAYER_STATUS_CHECK_INTERVAL)
                {
                    lastPlayerStatusCheck = UnityEngine.Time.realtimeSinceStartup;
                    myPlayerStatus.vesselText = "";
                    myPlayerStatus.statusText = "";
                    if (HighLogic.LoadedSceneIsFlight)
                    {
                        //Send vessel+status update
                        if (FlightGlobals.ActiveVessel != null)
                        {
                            myPlayerStatus.vesselText = FlightGlobals.ActiveVessel.vesselName;
                            string bodyName = FlightGlobals.ActiveVessel.mainBody.bodyName;
                            switch (FlightGlobals.ActiveVessel.situation)
                            {
                                case (Vessel.Situations.DOCKED):
                                    myPlayerStatus.statusText = "Docked above " + bodyName;
                                    break;
                                case (Vessel.Situations.ESCAPING):
                                    if (FlightGlobals.ActiveVessel.orbit.timeToPe < 0)
                                    {
                                        myPlayerStatus.statusText = "Escaping " + bodyName;
                                    }
                                    else
                                    {
                                        myPlayerStatus.statusText = "Encountering " + bodyName;
                                    }
                                    break;
                                case (Vessel.Situations.FLYING):
                                        myPlayerStatus.statusText = "Flying above " + bodyName;
                                    break;
                                case (Vessel.Situations.LANDED):
                                        myPlayerStatus.statusText = "Landed on " + bodyName;
                                    break;
                                case (Vessel.Situations.ORBITING):
                                    myPlayerStatus.statusText = "Orbiting " + bodyName;
                                    break;
                                case (Vessel.Situations.PRELAUNCH):
                                        myPlayerStatus.statusText = "Launching from " + bodyName;
                                    break;
                                case (Vessel.Situations.SPLASHED):
                                    myPlayerStatus.statusText = "Splashed on " + bodyName;
                                    break;
                                case (Vessel.Situations.SUB_ORBITAL):
                                    if (FlightGlobals.ActiveVessel.verticalSpeed > 0)
                                    {
                                        myPlayerStatus.statusText = "Ascending from " + bodyName;
                                    }
                                    else
                                    {
                                        myPlayerStatus.statusText = "Descending to " + bodyName;
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            myPlayerStatus.statusText = "Loading";
                        }
                    }
                    else
                    {
                        //Send status update
                        switch (HighLogic.LoadedScene)
                        {
                            case (GameScenes.EDITOR):
                                myPlayerStatus.statusText = "Building";
                                if (EditorDriver.editorFacility == EditorFacility.VAB)
                                {
                                    myPlayerStatus.statusText = "Building in VAB";
                                }
                                if (EditorDriver.editorFacility == EditorFacility.SPH)
                                {
                                    myPlayerStatus.statusText = "Building in SPH";
                                }
                                break;
                            case (GameScenes.SPACECENTER):
                                myPlayerStatus.statusText = "At Space Center";
                                break;
                            case (GameScenes.TRACKSTATION):
                                myPlayerStatus.statusText = "At Tracking Station";
                                break;
                            case (GameScenes.LOADING):
                                myPlayerStatus.statusText = "Loading";
                                break;

                        }
                    }
                }

                bool statusDifferent = false;
                statusDifferent = statusDifferent || (myPlayerStatus.vesselText != lastPlayerStatus.vesselText);
                statusDifferent = statusDifferent || (myPlayerStatus.statusText != lastPlayerStatus.statusText);
                if (statusDifferent && ((UnityEngine.Time.realtimeSinceStartup - lastPlayerStatusSend) > PLAYER_STATUS_SEND_THROTTLE))
                {
                    lastPlayerStatusSend = UnityEngine.Time.realtimeSinceStartup;
                    lastPlayerStatus.vesselText = myPlayerStatus.vesselText;
                    lastPlayerStatus.statusText = myPlayerStatus.statusText;
                    NetworkWorker.fetch.SendPlayerStatus(myPlayerStatus);
                }

                while (addStatusQueue.Count > 0)
                {
                    PlayerStatus newStatusEntry = addStatusQueue.Dequeue();
                    bool found = false;
                    foreach (PlayerStatus playerStatusEntry in playerStatusList)
                    {
                        if (playerStatusEntry.playerName == newStatusEntry.playerName)
                        {
                            found = true;
                            playerStatusEntry.vesselText = newStatusEntry.vesselText;
                            playerStatusEntry.statusText = newStatusEntry.statusText;
                        }
                    }
                    if (!found)
                    {
                        playerStatusList.Add(newStatusEntry);
                        SyncrioLog.Debug("Added " + newStatusEntry.playerName + " to status list");
                    }
                }

                while (removeStatusQueue.Count > 0)
                {
                    string removeStatusString = removeStatusQueue.Dequeue();
                    PlayerStatus removeStatus = null;
                    foreach (PlayerStatus currentStatus in playerStatusList)
                    {
                        if (currentStatus.playerName == removeStatusString)
                        {
                            removeStatus = currentStatus;
                        }
                    }
                    if (removeStatus != null)
                    {
                        playerStatusList.Remove(removeStatus);

                        SyncrioLog.Debug("Removed " + removeStatusString + " from status list");
                    }
                    else
                    {
                        SyncrioLog.Debug("Cannot remove non-existant player " + removeStatusString);
                    }
                }
            }
        }

        public void AddPlayerStatus(PlayerStatus playerStatus)
        {
            addStatusQueue.Enqueue(playerStatus);
        }

        public void RemovePlayerStatus(string playerName)
        {
            removeStatusQueue.Enqueue(playerName);
        }

        public int GetPlayerCount()
        {
            return playerStatusList.Count;
        }

        public PlayerStatus GetPlayerStatus(string playerName)
        {
            PlayerStatus returnStatus = null;
            foreach (PlayerStatus ps in playerStatusList)
            {
                if (ps.playerName == playerName)
                {
                    returnStatus = ps;
                    break;
                }
            }
            return returnStatus;
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
                singleton = new PlayerStatusWorker();
                Client.updateEvent.Add(singleton.Update);
            }
        }
    }
}

