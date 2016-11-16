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
    public class PlayerStatusWindow
    {
        public bool display = false;
        public bool displayEnable = false;
        public bool disconnectEventHandled = true;
        public bool colorEventHandled = true;
        private bool isWindowLocked = false;
        //private parts
        private static PlayerStatusWindow singleton;
        private bool initialized;
        private Vector2 scrollPosition;
        public bool minmized;
        private bool safeMinimized;
        //GUI Layout
        private bool calculatedMinSize;
        private Rect windowRect;
        private Rect minWindowRect;
        private Rect moveRect;
        private GUILayoutOption[] layoutOptions;
        private GUILayoutOption[] minLayoutOptions;
        //Styles
        private GUIStyle windowStyle;
        private GUIStyle subspaceStyle;
        private GUIStyle buttonStyle;
        private GUIStyle highlightStyle;
        private GUIStyle scrollStyle;
        private Dictionary<string, GUIStyle> playerNameStyle;
        private GUIStyle stateTextStyle;
        //Player status dictionaries
        private SubspaceDisplayEntry[] subspaceDisplay;
        private double lastStatusUpdate;
        //const
        private const float WINDOW_HEIGHT = 400;
        private const float WINDOW_WIDTH = 300;
        private const float UPDATE_STATUS_INTERVAL = .2f;

        public static PlayerStatusWindow fetch
        {
            get
            {
                return singleton;
            }
        }

        private void InitGUI()
        {
            //Setup GUI stuff
            windowRect = new Rect(Screen.width * 0.7f - WINDOW_WIDTH, Screen.height / 2f - WINDOW_HEIGHT / 2f, WINDOW_WIDTH, WINDOW_HEIGHT);
            minWindowRect = new Rect(float.NegativeInfinity, float.NegativeInfinity, 0, 0);
            moveRect = new Rect(0, 0, 10000, 20);

            windowStyle = new GUIStyle(GUI.skin.window);
            buttonStyle = new GUIStyle(GUI.skin.button);
            highlightStyle = new GUIStyle(GUI.skin.button);
            highlightStyle.normal.textColor = Color.red;
            highlightStyle.active.textColor = Color.red;
            highlightStyle.hover.textColor = Color.red;
            scrollStyle = new GUIStyle(GUI.skin.scrollView);
            subspaceStyle = new GUIStyle();
            subspaceStyle.normal.background = new Texture2D(1, 1);
            subspaceStyle.normal.background.SetPixel(0, 0, Color.black);
            subspaceStyle.normal.background.Apply();

            layoutOptions = new GUILayoutOption[4];
            layoutOptions[0] = GUILayout.MinWidth(WINDOW_WIDTH);
            layoutOptions[1] = GUILayout.MaxWidth(WINDOW_WIDTH);
            layoutOptions[2] = GUILayout.MinHeight(WINDOW_HEIGHT);
            layoutOptions[3] = GUILayout.MaxHeight(WINDOW_HEIGHT);

            minLayoutOptions = new GUILayoutOption[4];
            minLayoutOptions[0] = GUILayout.MinWidth(0);
            minLayoutOptions[1] = GUILayout.MinHeight(0);
            minLayoutOptions[2] = GUILayout.ExpandHeight(true);
            minLayoutOptions[3] = GUILayout.ExpandWidth(true);

            //Adapted from KMP.
            playerNameStyle = new Dictionary<string, GUIStyle>();

            stateTextStyle = new GUIStyle(GUI.skin.label);
            stateTextStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f);
            stateTextStyle.hover.textColor = stateTextStyle.normal.textColor;
            stateTextStyle.active.textColor = stateTextStyle.normal.textColor;
            stateTextStyle.fontStyle = FontStyle.Normal;
            stateTextStyle.fontSize = 12;
            stateTextStyle.stretchWidth = true;

            subspaceDisplay = new SubspaceDisplayEntry[0];
        }

        private void Update()
        {
            if (!Settings.fetch.DarkMultiPlayerCoopMode)
            {
                display = Client.fetch.gameRunning;
            }
            else
            {
                if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
                {
                    if (PlayerStatusWorker.fetch.workerEnabled)
                    {
                        displayEnable = true;
                    }
                }
                else
                {
                    if (HighLogic.LoadedScene == GameScenes.MAINMENU)
                    {
                        displayEnable = false;
                    }
                }
                display = displayEnable;
            }
            if (display)
            {
                safeMinimized = minmized;
                if (!calculatedMinSize && minWindowRect.width != 0 && minWindowRect.height != 0)
                {
                    calculatedMinSize = true;
                }
                if ((UnityEngine.Time.realtimeSinceStartup - lastStatusUpdate) > UPDATE_STATUS_INTERVAL)
                {
                    lastStatusUpdate = UnityEngine.Time.realtimeSinceStartup;
                    subspaceDisplay = WarpWorker.fetch.GetSubspaceDisplayEntries();
                }
            }
        }

        private void Draw()
        {
            if (!colorEventHandled)
            {
                playerNameStyle = new Dictionary<string, GUIStyle>();
                colorEventHandled = true;
            }
            if (!initialized)
            {
                initialized = true;
                InitGUI();
            }
            if (display)
            {
                //Calculate the minimum size of the minimize window by drawing it off the screen
                if (!calculatedMinSize)
                {
                    minWindowRect = GUILayout.Window(7701 + Client.WINDOW_OFFSET, minWindowRect, DrawMaximize, "Syncrio", windowStyle, minLayoutOptions);
                }
                if (!safeMinimized)
                {
                    windowRect = SyncrioGuiUtil.PreventOffscreenWindow(GUILayout.Window(7703 + Client.WINDOW_OFFSET, windowRect, DrawContent, "Syncrio - Status", windowStyle, layoutOptions));
                }
                else
                {
                    minWindowRect = SyncrioGuiUtil.PreventOffscreenWindow(GUILayout.Window(7703 + Client.WINDOW_OFFSET, minWindowRect, DrawMaximize, "Syncrio", windowStyle, minLayoutOptions));
                }
            }
            CheckWindowLock();
        }

        private void DrawContent(int windowID)
        {
            GUILayout.BeginVertical();
            GUI.DragWindow(moveRect);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUIStyle chatButtonStyle = buttonStyle;
            if (!Settings.fetch.DarkMultiPlayerCoopMode)
            {
                if (ChatWorker.fetch.chatButtonHighlighted)
                {
                    chatButtonStyle = highlightStyle;
                }
                ChatWorker.fetch.display = GUILayout.Toggle(ChatWorker.fetch.display, "Chat", chatButtonStyle);
                CraftLibraryWorker.fetch.display = GUILayout.Toggle(CraftLibraryWorker.fetch.display, "Craft", buttonStyle);
            }
            DebugWindow.fetch.display = GUILayout.Toggle(DebugWindow.fetch.display, "Debug", buttonStyle);
            GroupWindow.fetch.display = GUILayout.Toggle(GroupWindow.fetch.display, "Group", buttonStyle);
            if (!Settings.fetch.DarkMultiPlayerCoopMode)
            {
                GUIStyle screenshotButtonStyle = buttonStyle;
                if (ScreenshotWorker.fetch.screenshotButtonHighlighted)
                {
                    screenshotButtonStyle = highlightStyle;
                }
                ScreenshotWorker.fetch.display = GUILayout.Toggle(ScreenshotWorker.fetch.display, "Screenshot", screenshotButtonStyle);
            }
            if (GUILayout.Button("-", buttonStyle))
            {
                minmized = true;
                minWindowRect.x = windowRect.xMax - minWindowRect.width;
                minWindowRect.y = windowRect.y;
            }
            GUILayout.EndHorizontal();
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, scrollStyle);
            if (!Settings.fetch.DarkMultiPlayerCoopMode)
            {
                //Draw subspaces
                double ourTime = TimeSyncer.fetch.locked ? TimeSyncer.fetch.GetUniverseTime() : Planetarium.GetUniversalTime();
                long serverClock = TimeSyncer.fetch.GetServerClock();
                foreach (SubspaceDisplayEntry currentEntry in subspaceDisplay)
                {
                    double currentTime = 0;
                    double diffTime = 0;
                    string diffState = "Unknown";
                    if (!currentEntry.isUs)
                    {
                        if (!currentEntry.isWarping)
                        {
                            //Subspace entry
                            if (currentEntry.subspaceEntry != null)
                            {
                                long serverClockDiff = serverClock - currentEntry.subspaceEntry.serverClock;
                                double secondsDiff = serverClockDiff / 10000000d;
                                currentTime = currentEntry.subspaceEntry.planetTime + (currentEntry.subspaceEntry.subspaceSpeed * secondsDiff);
                                diffTime = currentTime - ourTime;
                                diffState = (diffTime > 0) ? SecondsToVeryShortString((int)diffTime) + " in the future" : SecondsToVeryShortString(-(int)diffTime) + " in the past";
                            }
                        }
                        else
                        {
                            //Warp entry
                            if (currentEntry.warpingEntry != null)
                            {
                                float[] warpRates = TimeWarp.fetch.warpRates;
                                if (currentEntry.warpingEntry.isPhysWarp)
                                {
                                    warpRates = TimeWarp.fetch.physicsWarpRates;
                                }
                                long serverClockDiff = serverClock - currentEntry.warpingEntry.serverClock;
                                double secondsDiff = serverClockDiff / 10000000d;
                                currentTime = currentEntry.warpingEntry.planetTime + (warpRates[currentEntry.warpingEntry.rateIndex] * secondsDiff);
                                diffTime = currentTime - ourTime;
                                diffState = (diffTime > 0) ? SecondsToVeryShortString((int)diffTime) + " in the future" : SecondsToVeryShortString(-(int)diffTime) + " in the past";
                            }
                        }
                    }
                    else
                    {
                        currentTime = ourTime;
                        diffState = "NOW";
                    }

                    //Draw the subspace black bar.
                    GUILayout.BeginHorizontal(subspaceStyle);
                    GUILayout.Label("T+ " + SecondsToShortString((int)currentTime) + " - " + diffState);
                    GUILayout.FlexibleSpace();
                    //Draw the sync button if needed
                    if ((WarpWorker.fetch.warpMode == WarpMode.SUBSPACE) && !currentEntry.isUs && !currentEntry.isWarping && (currentEntry.subspaceEntry != null) && (diffTime > 0))
                    {
                        if (GUILayout.Button("Sync", buttonStyle))
                        {
                            TimeSyncer.fetch.LockSubspace(currentEntry.subspaceID);
                        }
                    }
                    GUILayout.EndHorizontal();

                    foreach (string currentPlayer in currentEntry.players)
                    {
                        if (currentPlayer == Settings.fetch.playerName)
                        {
                            DrawPlayerEntry(PlayerStatusWorker.fetch.myPlayerStatus);
                        }
                        else
                        {
                            DrawPlayerEntry(PlayerStatusWorker.fetch.GetPlayerStatus(currentPlayer));
                        }
                    }
                }
            }
            else
            {
                DrawPlayerEntry(PlayerStatusWorker.fetch.myPlayerStatus);
                foreach (PlayerStatus currentPlayer in PlayerStatusWorker.fetch.playerStatusList)
                {
                    DrawPlayerEntry(currentPlayer);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            ScenarioWindow.fetch.display = GUILayout.Toggle(ScenarioWindow.fetch.display, "Scenario Sync", buttonStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Disconnect", buttonStyle))
            {
                disconnectEventHandled = false;
            }
            OptionsWindow.fetch.display = GUILayout.Toggle(OptionsWindow.fetch.display, "Options", buttonStyle);
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

                bool shouldLock = (minmized ? minWindowRect.Contains(mousePos) : windowRect.Contains(mousePos));

                if (shouldLock && !isWindowLocked)
                {
                    InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS,  "Syncrio_PlayerStatusLock");
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
                InputLockManager.RemoveControlLock( "Syncrio_PlayerStatusLock");
            }
        }

        private string SecondsToLongString(int time)
        {
            //Every month is feburary ok?
            int years = time / (60 * 60 * 24 * 7 * 4 * 12);
            time -= years * (60 * 60 * 24 * 7 * 4 * 12);
            int months = time / (60 * 60 * 24 * 7 * 4);
            time -= months * (60 * 60 * 24 * 7 * 4);
            int weeks = time / (60 * 60 * 24 * 7);
            time -= weeks * (60 * 60 * 24 * 7);
            int days = time / (60 * 60 * 24);
            time -= days * (60 * 60 * 24);
            int hours = time / (60 * 60);
            time -= hours * (60 * 60);
            int minutes = time / 60;
            time -= minutes * 60;
            int seconds = time;
            string returnString = "";
            if (years > 0)
            {
                if (years == 1)
                {
                    returnString += "1 year";
                }
                else
                {
                    returnString += years + " years";
                }
            }
            if (returnString != "")
            {
                returnString += ", ";
            }
            if (months > 0)
            {
                if (months == 1)
                {
                    returnString += "1 month";
                }
                else
                {
                    returnString += months + " month";
                }
            }
            if (returnString != "")
            {
                returnString += ", ";
            }
            if (weeks > 0)
            {
                if (weeks == 1)
                {
                    returnString += "1 week";
                }
                else
                {
                    returnString += weeks + " weeks";
                }
            }
            if (returnString != "")
            {
                returnString += ", ";
            }
            if (days > 0)
            {
                if (days == 1)
                {
                    returnString += "1 day";
                }
                else
                {
                    returnString += days + " days";
                }
            }
            if (returnString != "")
            {
                returnString += ", ";
            }
            if (hours > 0)
            {
                if (hours == 1)
                {
                    returnString += "1 hour";
                }
                else
                {
                    returnString += hours + " hours";
                }
            }
            if (returnString != "")
            {
                returnString += ", ";
            }
            if (minutes > 0)
            {
                if (minutes == 1)
                {
                    returnString += "1 minute";
                }
                else
                {
                    returnString += minutes + " minutes";
                }
            }
            if (returnString != "")
            {
                returnString += ", ";
            }
            if (seconds == 1)
            {
                returnString += "1 second";
            }
            else
            {
                returnString += seconds + " seconds";
            }
            return returnString;
        }

        private string SecondsToShortString(int time)
        {
            int years = time / (60 * 60 * 24 * 7 * 4 * 12);
            time -= years * (60 * 60 * 24 * 7 * 4 * 12);
            int months = time / (60 * 60 * 24 * 7 * 4);
            time -= months * (60 * 60 * 24 * 7 * 4);
            int weeks = time / (60 * 60 * 24 * 7);
            time -= weeks * (60 * 60 * 24 * 7);
            int days = time / (60 * 60 * 24);
            time -= days * (60 * 60 * 24);
            int hours = time / (60 * 60);
            time -= hours * (60 * 60);
            int minutes = time / 60;
            time -= minutes * 60;
            int seconds = time;
            string returnString = "";
            if (years > 0)
            {
                if (years == 1)
                {
                    returnString += "1y, ";
                }
                else
                {
                    returnString += years + "y, ";
                }
            }
            if (months > 0)
            {
                if (months == 1)
                {
                    returnString += "1m, ";
                }
                else
                {
                    returnString += months + "m, ";
                }
            }
            if (weeks > 0)
            {
                if (weeks == 1)
                {
                    returnString += "1w, ";
                }
                else
                {
                    returnString += weeks + "w, ";
                }
            }
            if (days > 0)
            {
                if (days == 1)
                {
                    returnString += "1d, ";
                }
                else
                {
                    returnString += days + "d, ";
                }
            }
            returnString += hours.ToString("00") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");
            return returnString;
        }

        private string SecondsToVeryShortString(int time)
        {
            int years = time / (60 * 60 * 24 * 7 * 4 * 12);
            time -= years * (60 * 60 * 24 * 7 * 4 * 12);
            int months = time / (60 * 60 * 24 * 7 * 4);
            time -= months * (60 * 60 * 24 * 7 * 4);
            int weeks = time / (60 * 60 * 24 * 7);
            time -= weeks * (60 * 60 * 24 * 7);
            int days = time / (60 * 60 * 24);
            time -= days * (60 * 60 * 24);
            int hours = time / (60 * 60);
            time -= hours * (60 * 60);
            int minutes = time / 60;
            time -= minutes * 60;
            int seconds = time;
            if (years > 0)
            {
                if (years == 1)
                {
                    return "1 year";
                }
                else
                {
                    return years + " years";
                }
            }
            if (months > 0)
            {
                if (months == 1)
                {
                    return "1 month";
                }
                else
                {
                    return months + " months";
                }
            }
            if (weeks > 0)
            {
                if (weeks == 1)
                {
                    return "1 week";
                }
                else
                {
                    return weeks + " weeks";
                }
            }
            if (days > 0)
            {
                if (days == 1)
                {
                    return "1 day";
                }
                else
                {
                    return days + " days";
                }
            }
            if (hours > 0)
            {
                if (hours == 1)
                {
                    return "1 hour";
                }
                else
                {
                    return hours + " hours";
                }
            }
            if (minutes > 0)
            {
                if (minutes == 1)
                {
                    return "1 minute";
                }
                else
                {
                    return minutes + " minutes";
                }
            }
            if (seconds == 1)
            {
                return "1 second";
            }
            else
            {
                return seconds + " seconds";
            }
        }

        private void DrawMaximize(int windowID)
        {
            GUI.DragWindow(moveRect);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (!Settings.fetch.DarkMultiPlayerCoopMode)
            {
                GUIStyle chatButtonStyle = buttonStyle;
                if (ChatWorker.fetch.chatButtonHighlighted)
                {
                    chatButtonStyle = highlightStyle;
                }
                ChatWorker.fetch.display = GUILayout.Toggle(ChatWorker.fetch.display, "C", chatButtonStyle);
            }
            DebugWindow.fetch.display = GUILayout.Toggle(DebugWindow.fetch.display, "D", buttonStyle);
            GroupWindow.fetch.display = GUILayout.Toggle(GroupWindow.fetch.display, "G", buttonStyle);
            if (!Settings.fetch.DarkMultiPlayerCoopMode)
            {
                GUIStyle screenshotButtonStyle = buttonStyle;
                if (ScreenshotWorker.fetch.screenshotButtonHighlighted)
                {
                    screenshotButtonStyle = highlightStyle;
                }
                ScreenshotWorker.fetch.display = GUILayout.Toggle(ScreenshotWorker.fetch.display, "S", screenshotButtonStyle);
            }
            OptionsWindow.fetch.display = GUILayout.Toggle(OptionsWindow.fetch.display, "O", buttonStyle);
            ScenarioWindow.fetch.display = GUILayout.Toggle(ScenarioWindow.fetch.display, "SS", buttonStyle);
            if (GUILayout.Button("+", buttonStyle))
            {
                windowRect.xMax = minWindowRect.xMax;
                windowRect.yMin = minWindowRect.yMin;
                windowRect.xMin = minWindowRect.xMax - WINDOW_WIDTH;
                windowRect.yMax = minWindowRect.yMin + WINDOW_HEIGHT;
                minmized = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawPlayerEntry(PlayerStatus playerStatus)
        {
            if (playerStatus == null)
            {
                //Just connected or disconnected.
                return;
            }
            GUILayout.BeginHorizontal();
            if (!playerNameStyle.ContainsKey(playerStatus.playerName))
            {
                playerNameStyle[playerStatus.playerName] = new GUIStyle(GUI.skin.label);
                playerNameStyle[playerStatus.playerName].normal.textColor = PlayerColorWorker.fetch.GetPlayerColor(playerStatus.playerName);
                playerNameStyle[playerStatus.playerName].hover.textColor = PlayerColorWorker.fetch.GetPlayerColor(playerStatus.playerName);
                playerNameStyle[playerStatus.playerName].active.textColor = PlayerColorWorker.fetch.GetPlayerColor(playerStatus.playerName);
                playerNameStyle[playerStatus.playerName].fontStyle = FontStyle.Bold;
                playerNameStyle[playerStatus.playerName].stretchWidth = true;
                playerNameStyle[playerStatus.playerName].wordWrap = false;
            }
            GUILayout.Label(playerStatus.playerName, playerNameStyle[playerStatus.playerName]);
            GUILayout.FlexibleSpace();
            GUILayout.Label(playerStatus.statusText, stateTextStyle);
            GUILayout.EndHorizontal();
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
                singleton = new PlayerStatusWindow();
                Client.updateEvent.Add(singleton.Update);
                Client.drawEvent.Add(singleton.Draw);
            }
        }
    }
}

