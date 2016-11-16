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
using UnityEngine;

namespace SyncrioClientSide
{
    public class OptionsWindow
    {
        private static OptionsWindow singleton = new OptionsWindow();
        public bool loadEventHandled = true;
        public bool display;
        private bool isWindowLocked = false;
        private bool safeDisplay;
        private bool initialized;
        //GUI Layout
        private Rect windowRect;
        private Rect moveRect;
        private GUILayoutOption[] layoutOptions;
        private GUILayoutOption[] smallOption;
        //Styles
        private GUIStyle windowStyle;
        private GUIStyle buttonStyle;
        //const
        private const float WINDOW_HEIGHT = 400;
        private const float WINDOW_WIDTH = 300;
        //TempColour
        private Color tempColor = new Color(1f, 1f, 1f, 1f);
        private GUIStyle tempColorLabelStyle;
        //Cache size
        private string newCacheSize = "";
        //Keybindings
        private bool settingChat;
        private bool settingScreenshot;
        private string toolbarMode;

        public OptionsWindow()
        {
            Client.updateEvent.Add(this.Update);
            Client.drawEvent.Add(this.Draw);
        }

        public static OptionsWindow fetch
        {
            get
            {
                return singleton;
            }
        }

        private void InitGUI()
        {
            //Setup GUI stuff
            windowRect = new Rect(Screen.width / 2f - WINDOW_WIDTH / 2f, Screen.height / 2f - WINDOW_HEIGHT / 2f, WINDOW_WIDTH, WINDOW_HEIGHT);
            moveRect = new Rect(0, 0, 10000, 20);

            windowStyle = new GUIStyle(GUI.skin.window);
            buttonStyle = new GUIStyle(GUI.skin.button);

            layoutOptions = new GUILayoutOption[4];
            layoutOptions[0] = GUILayout.Width(WINDOW_WIDTH);
            layoutOptions[1] = GUILayout.Height(WINDOW_HEIGHT);
            layoutOptions[2] = GUILayout.ExpandWidth(true);
            layoutOptions[3] = GUILayout.ExpandHeight(true);

            smallOption = new GUILayoutOption[2];
            smallOption[0] = GUILayout.Width(100);
            smallOption[1] = GUILayout.ExpandWidth(false);

            tempColor = new Color();
            tempColorLabelStyle = new GUIStyle(GUI.skin.label);
            UpdateToolbarString();
        }

        private void UpdateToolbarString()
        {
            switch (Settings.fetch.toolbarType)
            {
                case SyncrioToolbarType.DISABLED:
                    toolbarMode = "Disabled";
                    break;
                case SyncrioToolbarType.FORCE_STOCK:
                    toolbarMode = "Stock";
                    break;
                case SyncrioToolbarType.BLIZZY_IF_INSTALLED:
                    toolbarMode = "Blizzy if installed";
                    break;
                case SyncrioToolbarType.BOTH_IF_INSTALLED:
                    toolbarMode = "Both if installed";
                    break;
            }
        }

        private void Update()
        {
            safeDisplay = display;
        }

        private void Draw()
        {
            if (!initialized)
            {
                initialized = true;
                InitGUI();
            }
            if (safeDisplay)
            {
                windowRect = SyncrioGuiUtil.PreventOffscreenWindow(GUILayout.Window(7711 + Client.WINDOW_OFFSET, windowRect, DrawContent, "Syncrio - Options", windowStyle, layoutOptions));
            }
            CheckWindowLock();
        }

        private void DrawContent(int windowID)
        {
            if (!loadEventHandled)
            {
                loadEventHandled = true;
                tempColor = Settings.fetch.playerColor;
                newCacheSize = Settings.fetch.cacheSize.ToString();
            }
            //Player color
            GUILayout.BeginVertical();
            GUI.DragWindow(moveRect);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Player name color: ");
            GUILayout.Label(Settings.fetch.playerName, tempColorLabelStyle);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("R: ");
            tempColor.r = GUILayout.HorizontalScrollbar(tempColor.r, 0, 0, 1);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("G: ");
            tempColor.g = GUILayout.HorizontalScrollbar(tempColor.g, 0, 0, 1);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("B: ");
            tempColor.b = GUILayout.HorizontalScrollbar(tempColor.b, 0, 0, 1);
            GUILayout.EndHorizontal();
            tempColorLabelStyle.active.textColor = tempColor;
            tempColorLabelStyle.normal.textColor = tempColor;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Random", buttonStyle))
            {
                tempColor = PlayerColorWorker.GenerateRandomColor();
            }
            if (GUILayout.Button("Set", buttonStyle))
            {
                PlayerStatusWindow.fetch.colorEventHandled = false;
                Settings.fetch.playerColor = tempColor;
                Settings.fetch.SaveSettings();
                if (NetworkWorker.fetch.state == SyncrioCommon.ClientState.RUNNING)
                {
                    PlayerColorWorker.fetch.SendPlayerColorToServer();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            //Cache
            GUILayout.Label("Cache size");
            GUILayout.Label("Current size: " + Math.Round((ScenarioSyncCache.fetch.currentCacheSize / (float)(1024 * 1024)), 3) + "MB.");
            GUILayout.Label("Max size: " + Settings.fetch.cacheSize + "MB.");
            newCacheSize = GUILayout.TextArea(newCacheSize);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Set", buttonStyle))
            {
                int tempCacheSize;
                if (Int32.TryParse(newCacheSize, out tempCacheSize))
                {
                    if (tempCacheSize < 1)
                    {
                        tempCacheSize = 1;
                        newCacheSize = tempCacheSize.ToString();
                    }
                    if (tempCacheSize > 1000)
                    {
                        tempCacheSize = 1000;
                        newCacheSize = tempCacheSize.ToString();
                    }
                    Settings.fetch.cacheSize = tempCacheSize;
                    Settings.fetch.SaveSettings();
                }
                else
                {
                    newCacheSize = Settings.fetch.cacheSize.ToString();
                }
            }
            if (GUILayout.Button("Expire cache"))
            {
                ScenarioSyncCache.fetch.ExpireCache();
            }
            if (GUILayout.Button("Delete cache"))
            {
                ScenarioSyncCache.fetch.DeleteCache();
            }
            GUILayout.EndHorizontal();
            //Key bindings
            GUILayout.Space(10);
            string chatDescription = "Set chat key (current: " + Settings.fetch.chatKey + ")";
            if (settingChat)
            {
                chatDescription = "Setting chat key (click to cancel)...";
                if (Event.current.isKey)
                {
                    if (Event.current.keyCode != KeyCode.Escape)
                    {
                        Settings.fetch.chatKey = Event.current.keyCode;
                        Settings.fetch.SaveSettings();
                        settingChat = false;
                    }
                    else
                    {
                        settingChat = false;
                    }
                }
            }
            if (GUILayout.Button(chatDescription))
            {
                settingChat = !settingChat;
            }
            string screenshotDescription = "Set screenshot key (current: " + Settings.fetch.screenshotKey.ToString() + ")";
            if (settingScreenshot)
            {
                screenshotDescription = "Setting screenshot key (click to cancel)...";
                if (Event.current.isKey)
                {
                    if (Event.current.keyCode != KeyCode.Escape)
                    {
                        Settings.fetch.screenshotKey = Event.current.keyCode;
                        Settings.fetch.SaveSettings();
                        settingScreenshot = false;
                    }
                    else
                    {
                        settingScreenshot = false;
                    }
                }
            }
            if (GUILayout.Button(screenshotDescription))
            {
                settingScreenshot = !settingScreenshot;
            }
            GUILayout.Space(10);
            ScenarioConverterWindow.fetch.display = GUILayout.Toggle(ScenarioConverterWindow.fetch.display, "Generate Scenario from saved game", buttonStyle);
            if (GUILayout.Button("Reset disclaimer"))
            {
                Settings.fetch.disclaimerAccepted = 0;
                Settings.fetch.SaveSettings();
            }
            bool settingdarkmultiplayercoopmode = GUILayout.Toggle(Settings.fetch.DarkMultiPlayerCoopMode, "Enable DMP Co-op Mode", buttonStyle);
            if (settingdarkmultiplayercoopmode != Settings.fetch.DarkMultiPlayerCoopMode)
            {
                Settings.fetch.DarkMultiPlayerCoopMode = settingdarkmultiplayercoopmode;
                Settings.fetch.SaveSettings();
            }
            bool settingCompression = GUILayout.Toggle(Settings.fetch.compressionEnabled, "Enable compression", buttonStyle);
            if (settingCompression != Settings.fetch.compressionEnabled)
            {
                Settings.fetch.compressionEnabled = settingCompression;
                Settings.fetch.SaveSettings();
            }
            bool settingRevert = GUILayout.Toggle(Settings.fetch.revertEnabled, "Enable revert", buttonStyle);
            if (settingRevert != Settings.fetch.revertEnabled)
            {
                Settings.fetch.revertEnabled = settingRevert;
                Settings.fetch.SaveSettings();
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Toolbar:", smallOption);
            if (GUILayout.Button(toolbarMode, buttonStyle))
            {
                int newSetting = (int)Settings.fetch.toolbarType + 1;
                //Overflow to 0
                if (!Enum.IsDefined(typeof(SyncrioToolbarType), newSetting))
                {
                    newSetting = 0;
                }
                Settings.fetch.toolbarType = (SyncrioToolbarType)newSetting;
                Settings.fetch.SaveSettings();
                UpdateToolbarString();
                ToolbarSupport.fetch.DetectSettingsChange();
            }
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", buttonStyle))
            {
                display = false;
            }
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

            if (safeDisplay)
            {
                Vector2 mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;

                bool shouldLock = windowRect.Contains(mousePos);

                if (shouldLock && !isWindowLocked)
                {
                    InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "Syncrio_OptionsLock");
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
                InputLockManager.RemoveControlLock("Syncrio_OptionsLock");
            }
        }
    }
}

