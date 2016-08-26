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

namespace SyncrioClientSide
{
    public class DebugWindow
    {
        public bool display = false;
        private bool safeDisplay = false;
        private bool initialized = false;
        private bool isWindowLocked = false;
        private static DebugWindow singleton;
        //private parts
        private bool displayFast;
        private bool displayVectors;
        private bool displayNTP;
        private bool displayConnectionQueue;
        private bool displayDynamicTickStats;
        private bool displayRequestedRates;
        private bool displayProfilerStatistics;
        private string vectorText = "";
        private string ntpText = "";
        private string connectionText = "";
        private string dynamicTickText = "";
        private string requestedRateText = "";
        private string profilerText = "";
        private float lastUpdateTime;
        //GUI Layout
        private Rect windowRect;
        private Rect moveRect;
        private GUILayoutOption[] layoutOptions;
        private GUILayoutOption[] textAreaOptions;
        private GUIStyle windowStyle;
        private GUIStyle buttonStyle;
        private GUIStyle labelStyle;
        //const
        private const float WINDOW_HEIGHT = 400;
        private const float WINDOW_WIDTH = 350;
        private const float DISPLAY_UPDATE_INTERVAL = .2f;

        public static DebugWindow fetch
        {
            get
            {
                return singleton;
            }
        }

        private void InitGUI()
        {
            //Setup GUI stuff
            windowRect = new Rect(Screen.width - (WINDOW_WIDTH + 50), (Screen.height / 2f) - (WINDOW_HEIGHT / 2f), WINDOW_WIDTH, WINDOW_HEIGHT);
            moveRect = new Rect(0, 0, 10000, 20);

            layoutOptions = new GUILayoutOption[4];
            layoutOptions[0] = GUILayout.MinWidth(WINDOW_WIDTH);
            layoutOptions[1] = GUILayout.MaxWidth(WINDOW_WIDTH);
            layoutOptions[2] = GUILayout.MinHeight(WINDOW_HEIGHT);
            layoutOptions[3] = GUILayout.MaxHeight(WINDOW_HEIGHT);

            windowStyle = new GUIStyle(GUI.skin.window);
            buttonStyle = new GUIStyle(GUI.skin.button);

            textAreaOptions = new GUILayoutOption[1];
            textAreaOptions[0] = GUILayout.ExpandWidth(true);

            labelStyle = new GUIStyle(GUI.skin.label);
        }

        public void Draw()
        {
            if (safeDisplay)
            {
                if (!initialized)
                {
                    initialized = true;
                    InitGUI();
                }
                windowRect = SyncrioGuiUtil.PreventOffscreenWindow(GUILayout.Window(7705 + Client.WINDOW_OFFSET, windowRect, DrawContent, "Syncrio - Debug", windowStyle, layoutOptions));
            }
            CheckWindowLock();
        }

        private void DrawContent(int windowID)
        {
            GUILayout.BeginVertical();
            GUI.DragWindow(moveRect);
            GameEvents.debugEvents = GUILayout.Toggle(GameEvents.debugEvents, "Debug GameEvents", buttonStyle);
            displayFast = GUILayout.Toggle(displayFast, "Fast debug update", buttonStyle);
            displayVectors = GUILayout.Toggle(displayVectors, "Display vessel vectors", buttonStyle);
            if (displayVectors)
            {
                GUILayout.Label(vectorText, labelStyle);
            }
            displayNTP = GUILayout.Toggle(displayNTP, "Display NTP statistics", buttonStyle);
            if (displayNTP)
            {
                GUILayout.Label(ntpText, labelStyle);
            }
            displayConnectionQueue = GUILayout.Toggle(displayConnectionQueue, "Display connection statistics", buttonStyle);
            if (displayConnectionQueue)
            {
                GUILayout.Label(connectionText, labelStyle);
            }
            displayDynamicTickStats = GUILayout.Toggle(displayDynamicTickStats, "Display dynamic tick statistics", buttonStyle);
            if (displayDynamicTickStats)
            {
                GUILayout.Label(dynamicTickText, labelStyle);
            }
            displayRequestedRates = GUILayout.Toggle(displayRequestedRates, "Display requested rates", buttonStyle);
            if (displayRequestedRates)
            {
                GUILayout.Label(requestedRateText, labelStyle);
            }
            displayProfilerStatistics = GUILayout.Toggle(displayProfilerStatistics, "Display Profiler Statistics", buttonStyle);
            if (displayProfilerStatistics)
            {
                if (System.Diagnostics.Stopwatch.IsHighResolution)
                {
                    if (GUILayout.Button("Reset Profiler history", buttonStyle))
                    {
                        Profiler.updateData = new ProfilerData();
                        Profiler.fixedUpdateData = new ProfilerData();
                        Profiler.guiData = new ProfilerData();
                    }
                    GUILayout.Label("Timer resolution: " + System.Diagnostics.Stopwatch.Frequency + " hz", labelStyle);
                    GUILayout.Label(profilerText, labelStyle);
                }
                else
                {
                    GUILayout.Label("Timer resolution: " + System.Diagnostics.Stopwatch.Frequency + " hz", labelStyle);
                    GUILayout.Label("Profiling statistics unavailable without a high resolution timer");
                }
            }
            GUILayout.EndVertical();
        }

        private void Update()
        {
            safeDisplay = display;
            if (display)
            {
                if (((UnityEngine.Time.realtimeSinceStartup - lastUpdateTime) > DISPLAY_UPDATE_INTERVAL) || displayFast)
                {
                    lastUpdateTime = UnityEngine.Time.realtimeSinceStartup;
                    //Vector text
                    if (HighLogic.LoadedScene == GameScenes.FLIGHT && FlightGlobals.ready && FlightGlobals.fetch.activeVessel != null) {
                        Vessel ourVessel = FlightGlobals.fetch.activeVessel;
                        vectorText = "Forward vector: " + ourVessel.GetFwdVector() + "\n";
                        vectorText += "Up vector: " + (Vector3)ourVessel.upAxis + "\n";
                        vectorText += "Srf Rotation: " + ourVessel.srfRelRotation + "\n";
                        vectorText += "Vessel Rotation: " + ourVessel.transform.rotation + "\n";
                        vectorText += "Vessel Local Rotation: " + ourVessel.transform.localRotation + "\n";
                        vectorText += "mainBody Rotation: " + (Quaternion)ourVessel.mainBody.rotation + "\n";
                        vectorText += "mainBody Transform Rotation: " + (Quaternion)ourVessel.mainBody.bodyTransform.rotation + "\n";
                        vectorText += "Surface Velocity: " + ourVessel.GetSrfVelocity() + ", |v|: " + ourVessel.GetSrfVelocity().magnitude + "\n";
                        vectorText += "Orbital Velocity: " + ourVessel.GetObtVelocity() + ", |v|: " + ourVessel.GetObtVelocity().magnitude + "\n";
                        if (ourVessel.orbitDriver != null && ourVessel.orbitDriver.orbit != null)
                        {
                            vectorText += "Frame Velocity: " + (Vector3)ourVessel.orbitDriver.orbit.GetFrameVel() + ", |v|: " + ourVessel.orbitDriver.orbit.GetFrameVel().magnitude + "\n";
                        }
                        vectorText += "CoM offset vector: " + ourVessel.orbitDriver.CoMoffset + "\n";
                        vectorText += "Angular Velocity: " + ourVessel.angularVelocity + ", |v|: " + ourVessel.angularVelocity.magnitude + "\n";
                        vectorText += "World Pos: " + (Vector3)ourVessel.GetWorldPos3D() + ", |pos|: " + ourVessel.GetWorldPos3D().magnitude + "\n";
                    }
                    else {
                        vectorText = "You have to be in flight";
                    }

                    //Connection queue text
                    connectionText = "Last send time: " + NetworkWorker.fetch.GetStatistics("LastSendTime") + "ms.\n";
                    connectionText += "Last receive time: " + NetworkWorker.fetch.GetStatistics("LastReceiveTime") + "ms.\n";
                    connectionText += "Queued outgoing messages (High): " + NetworkWorker.fetch.GetStatistics("HighPriorityQueueLength") + ".\n";
                    connectionText += "Queued outgoing messages (Split): " + NetworkWorker.fetch.GetStatistics("SplitPriorityQueueLength") + ".\n";
                    connectionText += "Queued outgoing messages (Low): " + NetworkWorker.fetch.GetStatistics("LowPriorityQueueLength") + ".\n";
                    connectionText += "Queued out bytes: " + NetworkWorker.fetch.GetStatistics("QueuedOutBytes") + ".\n";
                    connectionText += "Sent bytes: " + NetworkWorker.fetch.GetStatistics("SentBytes") + ".\n";
                    connectionText += "Received bytes: " + NetworkWorker.fetch.GetStatistics("ReceivedBytes") + ".\n";

                    //Dynamic tick text
                    dynamicTickText = "Current tick rate: " + DynamicTickWorker.fetch.sendTickRate + "hz.\n";
                    dynamicTickText += "Current max secondry vessels: " + DynamicTickWorker.fetch.maxSecondryVesselsPerTick + ".\n";

                    profilerText = "Update: \n" + Profiler.updateData;
                    profilerText += "Fixed Update: \n" + Profiler.fixedUpdateData;
                    profilerText += "GUI: \n" + Profiler.guiData;
                }
            }
        }

        private void CheckWindowLock()
        {
            if (!Client.fetch.gameRunning)
            {
                RemoveWindowLock();
                return;
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                RemoveWindowLock();
                return;
            }

            if (safeDisplay)
            {
                Vector2 mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;

                bool shouldLock = windowRect.Contains(mousePos);

                if (shouldLock && !isWindowLocked)
                {
                    InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "Syncrio_DebugLock");
                    isWindowLocked = true;
                }
                if (!shouldLock && isWindowLocked)
                {
                    RemoveWindowLock();
                }
            }

            if (!safeDisplay && isWindowLocked)
            {
                RemoveWindowLock();
            }
        }

        private void RemoveWindowLock()
        {
            if (isWindowLocked)
            {
                isWindowLocked = false;
                InputLockManager.RemoveControlLock("Syncrio_DebugLock");
            }
        }

        public static void Reset()
        {
            lock (Client.eventLock)
            {
                if (singleton != null)
                {
                    singleton.display = false;
                    singleton.RemoveWindowLock();
                    Client.updateEvent.Remove(singleton.Update);
                    Client.drawEvent.Remove(singleton.Draw);
                }
                singleton = new DebugWindow();
                Client.updateEvent.Add(singleton.Update);
                Client.drawEvent.Add(singleton.Draw);
            }
        }
    }
}

