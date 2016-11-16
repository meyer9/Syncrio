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

namespace SyncrioClientSide
{
    class ScenarioWindow
    {
        private static ScenarioWindow singleton;
        public bool workerEnabled = false;
        public bool display = false;
        private bool initialized = false;
        private bool isWindowLocked = false;
        private bool resetScenario = false;
        private bool sync = false;
        //GUI
        private GUIStyle windowStyle;
        private GUIStyle buttonStyle;
        private GUILayoutOption[] labelOptions;
        private GUILayoutOption[] labelOptionsTwo;
        private GUILayoutOption[] layoutOptions;
        private Rect windowRect;
        private Rect moveRect;
        private const float WINDOW_HEIGHT = 150;
        private const float WINDOW_WIDTH = 200;

        public static ScenarioWindow fetch
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
                CheckWindowLock();
            }
        }

        private void Draw()
        {
            if (display)
            {
                if (!initialized)
                {
                    initialized = true;
                    InitGUI();
                }
                windowRect = SyncrioGuiUtil.PreventOffscreenWindow(GUILayout.Window(7717 + Client.WINDOW_OFFSET, windowRect, DrawContent, "Syncrio - Scenario Sync", windowStyle, layoutOptions));
            }
        }

        private void InitGUI()
        {
            //Setup GUI stuff
            windowRect = new Rect(Screen.width * 0.5f - WINDOW_WIDTH, Screen.height / 2f - WINDOW_HEIGHT / 2f, WINDOW_WIDTH, WINDOW_HEIGHT);
            moveRect = new Rect(0, 0, 10000, 20);
            windowStyle = new GUIStyle(GUI.skin.window);
            buttonStyle = new GUIStyle(GUI.skin.button);
            layoutOptions = new GUILayoutOption[4];
            layoutOptions[0] = GUILayout.MinWidth(WINDOW_WIDTH);
            layoutOptions[1] = GUILayout.MaxWidth(WINDOW_WIDTH);
            layoutOptions[2] = GUILayout.MinHeight(WINDOW_HEIGHT);
            layoutOptions[3] = GUILayout.MaxHeight(WINDOW_HEIGHT);

            labelOptions = new GUILayoutOption[1];
            labelOptions[0] = GUILayout.Width(100);

            labelOptionsTwo = new GUILayoutOption[1];
            labelOptionsTwo[0] = GUILayout.Width(180);
        }

        private void DrawContent(int windowID)
        {
            bool isInGroup = GroupSystem.playerGroupAssigned;
            GUILayout.BeginVertical();
            GUI.DragWindow(moveRect);
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            if (isInGroup)
            {
                sync = GUILayout.Toggle(sync, "Sync", buttonStyle);

                if (sync)
                {
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();

                    GUILayout.Label("Sync with server:", labelOptions);

                    if (GUILayout.Button("Yes", buttonStyle))
                    {
                        ScenarioWorker.fetch.ScenarioSync(isInGroup, true, true, false);
                        sync = false;
                    }
                    if (GUILayout.Button("No", buttonStyle))
                    {
                        sync = false;
                    }
                }
            }
            else
            {
                if (ScenarioWorker.fetch.nonGroupScenarios)
                {
                    if (GUILayout.Button("Sync", buttonStyle))
                    {
                        ScenarioWorker.fetch.ScenarioSync(isInGroup, false, true, false);
                    }
                }
                else
                {
                    GUILayout.Label("Join a Group to Sync", labelOptions);
                }
            }

            if (ScenarioWorker.fetch.canResetScenario)
            {
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                resetScenario = GUILayout.Toggle(resetScenario, "Reset Scenario!", buttonStyle);
                if (resetScenario)
                {
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("WARNING: You are about to reset your scenario to the default settings", labelOptionsTwo);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    if (GUILayout.Button("Continue", buttonStyle))
                    {
                        ScenarioWorker.fetch.ResetScenatio(isInGroup);
                    }
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Cancel", buttonStyle))
                    {
                        resetScenario = false;
                    }
                    GUILayout.Space(20);
                }
                
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
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

            if (display)
            {
                Vector2 mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;

                bool shouldLock = windowRect.Contains(mousePos);

                if (shouldLock && !isWindowLocked)
                {
                    InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "Syncrio_ScenarioWindowLock");
                    isWindowLocked = true;
                }
                if (!shouldLock && isWindowLocked)
                {
                    RemoveWindowLock();
                }
            }

            if (!display && isWindowLocked)
            {
                RemoveWindowLock();
            }
        }

        private void RemoveWindowLock()
        {
            if (isWindowLocked)
            {
                isWindowLocked = false;
                InputLockManager.RemoveControlLock("Syncrio_ScenarioWindowLock");
            }
        }

        public static void Reset()
        {
            lock (Client.eventLock)
            {
                if (singleton != null)
                {
                    singleton.workerEnabled = false;
                    Client.drawEvent.Remove(singleton.Draw);
                    Client.updateEvent.Remove(singleton.Update);
                }
                singleton = new ScenarioWindow();
                Client.drawEvent.Add(singleton.Draw);
                Client.updateEvent.Add(singleton.Update);
            }
        }
    }
}
