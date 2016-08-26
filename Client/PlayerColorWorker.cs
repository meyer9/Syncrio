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
using MessageStream2;
using SyncrioCommon;

namespace SyncrioClientSide
{
    //Damn you americans - You're making me spell 'colour' wrong!
    public class PlayerColorWorker
    {
        //As this worker is entirely event based, we need to register and unregister hooks in the workerEnabled accessor.
        private bool privateWorkerEnabled;

        public bool workerEnabled
        {
            get
            {
                return privateWorkerEnabled;
            }
            set
            {
                if (!privateWorkerEnabled && value)
                {
                    GameEvents.onVesselCreate.Add(this.SetVesselColor);
                    LockSystem.fetch.RegisterAcquireHook(this.OnLockAcquire);
                    LockSystem.fetch.RegisterReleaseHook(this.OnLockRelease);
                }
                if (privateWorkerEnabled && !value)
                {
                    GameEvents.onVesselCreate.Remove(this.SetVesselColor);
                    LockSystem.fetch.UnregisterAcquireHook(this.OnLockAcquire);
                    LockSystem.fetch.UnregisterReleaseHook(this.OnLockRelease);
                }
                privateWorkerEnabled = value;
            }
        }

        private static PlayerColorWorker singleton;
        private Dictionary<string,Color> playerColors = new Dictionary<string, Color>();
        private object playerColorLock = new object();
        //Can't declare const - But no touchy.
        public readonly Color DEFAULT_COLOR = Color.grey;

        public static PlayerColorWorker fetch
        {
            get
            {
                return singleton;
            }
        }

        private void SetVesselColor(Vessel colorVessel)
        {
            if (workerEnabled)
            {
                if (LockSystem.fetch.LockExists("control-" + colorVessel.id.ToString()) && !LockSystem.fetch.LockIsOurs("control-" + colorVessel.id.ToString()))
                {
                    string vesselOwner = LockSystem.fetch.LockOwner("control-" + colorVessel.id.ToString());
                    SyncrioLog.Debug("Vessel " + colorVessel.id.ToString() + " owner is " + vesselOwner);
                    colorVessel.orbitDriver.orbitColor = GetPlayerColor(vesselOwner);
                }
                else
                {
                    colorVessel.orbitDriver.orbitColor = DEFAULT_COLOR;
                }
            }
        }

        private void OnLockAcquire(string playerName, string lockName, bool result)
        {
            if (workerEnabled)
            {
                UpdateVesselColorsFromLockName(lockName);
            }
        }

        private void OnLockRelease(string playerName, string lockName)
        {
            if (workerEnabled)
            {
                UpdateVesselColorsFromLockName(lockName);
            }
        }

        private void UpdateVesselColorsFromLockName(string lockName)
        {
            if (lockName.StartsWith("control-"))
            {
                string vesselID = lockName.Substring(8);
                foreach (Vessel findVessel in FlightGlobals.fetch.vessels)
                {
                    if (findVessel.id.ToString() == vesselID)
                    {
                        SetVesselColor(findVessel);
                    }
                }
            }
        }

        private void UpdateAllVesselColors()
        {
            foreach (Vessel updateVessel in FlightGlobals.fetch.vessels)
            {
                SetVesselColor(updateVessel);
            }
        }

        public Color GetPlayerColor(string playerName)
        {
            lock (playerColorLock)
            {
                if (playerName == Settings.fetch.playerName)
                {
                    return Settings.fetch.playerColor;
                }
                if (playerColors.ContainsKey(playerName))
                {
                    return playerColors[playerName];
                }
                return DEFAULT_COLOR;
            }
        }

        public void HandlePlayerColorMessage(byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                PlayerColorMessageType messageType = (PlayerColorMessageType)mr.Read<int>();
                switch (messageType)
                {
                    case PlayerColorMessageType.LIST:
                        {
                            int numOfEntries = mr.Read<int>();
                            lock (playerColorLock)
                            {
                                playerColors = new Dictionary<string, Color>();
                                for (int i = 0; i < numOfEntries; i++)
                                {

                                    string playerName = mr.Read<string>();
                                    Color playerColor = ConvertFloatArrayToColor(mr.Read<float[]>());
                                    playerColors.Add(playerName, playerColor);
                                    PlayerStatusWindow.fetch.colorEventHandled = false;
                                }
                            }
                        }
                        break;
                    case PlayerColorMessageType.SET:
                        {
                            lock (playerColorLock)
                            {
                                string playerName = mr.Read<string>();
                                Color playerColor = ConvertFloatArrayToColor(mr.Read<float[]>());
                                SyncrioLog.Debug("Color message, name: " + playerName + " , color: " + playerColor.ToString());
                                playerColors[playerName] = playerColor;
                                UpdateAllVesselColors();
                                PlayerStatusWindow.fetch.colorEventHandled = false;
                            }
                        }
                        break;
                }
            }
        }

        public void SendPlayerColorToServer()
        {
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)PlayerColorMessageType.SET);
                mw.Write<string>(Settings.fetch.playerName);
                mw.Write<float[]>(ConvertColorToFloatArray(Settings.fetch.playerColor));
                NetworkWorker.fetch.SendPlayerColorMessage(mw.GetMessageBytes());
            }
        }
        //Helpers
        public static float[] ConvertColorToFloatArray(Color convertColour)
        {
            float[] returnArray = new float[3];
            returnArray[0] = convertColour.r;
            returnArray[1] = convertColour.g;
            returnArray[2] = convertColour.b;
            return returnArray;
        }

        public static Color ConvertFloatArrayToColor(float[] convertArray)
        {
            return new Color(convertArray[0], convertArray[1], convertArray[2]);
        }
        //Adapted from KMP
        public static Color GenerateRandomColor()
        {
            System.Random rand = new System.Random();
            int seed = rand.Next();
            Color returnColor = Color.white;
            switch (seed % 17)
            {
                case 0:
                    return Color.red;
                case 1:
                    return new Color(1, 0, 0.5f, 1); //Rosy pink
                case 2:
                    return new Color(0.6f, 0, 0.5f, 1); //OU Crimson
                case 3:
                    return new Color(1, 0.5f, 0, 1); //Orange
                case 4:
                    return Color.yellow;
                case 5:
                    return new Color(1, 0.84f, 0, 1); //Gold
                case 6:
                    return Color.green;
                case 7:
                    return new Color(0, 0.651f, 0.576f, 1); //Persian Green
                case 8:
                    return new Color(0, 0.651f, 0.576f, 1); //Persian Green
                case 9:
                    return new Color(0, 0.659f, 0.420f, 1); //Jade
                case 10:
                    return new Color(0.043f, 0.855f, 0.318f, 1); //Malachite
                case 11:
                    return Color.cyan;  
                case 12:
                    return new Color(0.537f, 0.812f, 0.883f, 1); //Baby blue;
                case 13:
                    return new Color(0, 0.529f, 0.741f, 1); //NCS blue
                case 14:
                    return new Color(0.255f, 0.412f, 0.882f, 1); //Royal Blue
                case 15:
                    return new Color(0.5f, 0, 1, 1); //Violet
                default:
                    return Color.magenta;
            }
        }

        public static void Reset()
        {
            lock (Client.eventLock)
            {
                if (singleton != null)
                {
                    singleton.workerEnabled = false;
                }
                singleton = new PlayerColorWorker();
            }
        }
    }
}

