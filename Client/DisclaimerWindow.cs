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
    //This disclaimer exists because I was contacted by a moderator pointing me to the addon posting rules.
    public class DisclaimerWindow
    {
        public static DisclaimerWindow singleton;
        private const int WINDOW_WIDTH = 500;
        private const int WINDOW_HEIGHT = 300;
        private Rect windowRect;
        private Rect moveRect;
        private bool initialized;
        private bool display;
        private GUILayoutOption[] layoutOptions;

        public static DisclaimerWindow fetch
        {
            get
            {
                return singleton;
            }
        }

        private void InitGUI()
        {
            //Setup GUI stuff
            windowRect = new Rect((Screen.width / 2f) - (WINDOW_WIDTH / 2), (Screen.height / 2f) - WINDOW_HEIGHT, WINDOW_WIDTH, WINDOW_HEIGHT);
            moveRect = new Rect(0, 0, 10000, 20);

            layoutOptions = new GUILayoutOption[2];
            layoutOptions[0] = GUILayout.ExpandWidth(true);
            layoutOptions[1] = GUILayout.ExpandHeight(true);
        }

        private void Draw()
        {
            if (!initialized)
            {
                initialized = true;
                InitGUI();
            }
            if (display)
            {
                windowRect = SyncrioGuiUtil.PreventOffscreenWindow(GUILayout.Window(7713 + Client.WINDOW_OFFSET, windowRect, DrawContent, "Syncrio - Disclaimer", layoutOptions));
            }
        }

        private void DrawContent(int windowID)
        {
            GUILayout.BeginVertical();
            GUI.DragWindow(moveRect);
            string disclaimerText = "Syncrio shares the following possibly personally identifiable information with any server you connect to.\n";
            disclaimerText += "a) Your player name you connect with.\n";
            disclaimerText += "b) Your player token (A randomly generated string to authenticate you).\n";
            disclaimerText += "c) Your IP address is logged on the server console.\n";
            disclaimerText += "\n";
            disclaimerText += "Syncrio does not contact any other computer than the server you are connecting to.\n";
            disclaimerText += "In order to use Syncrio, you must allow Syncrio to use this info\n";
            disclaimerText += "\n";
            disclaimerText += "For more information - see the KSP addon rules\n";
            GUILayout.Label(disclaimerText);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Open the KSP Addon rules in the browser"))
            {
                Application.OpenURL("http://forum.kerbalspaceprogram.com/threads/87841-Add-on-Posting-Rules-July-24th-2014-going-into-effect-August-21st-2014!");
            }
            if (GUILayout.Button("I accept - Enable Syncrio"))
            {
                SyncrioLog.Debug("User accepted disclaimer - Enabling Syncrio");
                display = false;
                Settings.fetch.disclaimerAccepted = 1;
                Client.fetch.modDisabled = false;
                Settings.fetch.SaveSettings();
            }
            if (GUILayout.Button("I decline - Disable Syncrio"))
            {
                SyncrioLog.Debug("User declined disclaimer - Disabling Syncrio");
                display = false;
            }
            GUILayout.EndVertical();
        }

        public static void Enable()
        {
            singleton = new DisclaimerWindow();
            lock (Client.eventLock) {
                Client.drawEvent.Add(singleton.Draw);
            }
            singleton.display = true;
        }
    }
}

