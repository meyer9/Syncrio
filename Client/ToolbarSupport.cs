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
using KSP.UI.Screens;
using UnityEngine;

namespace SyncrioClientSide
{
    public class ToolbarSupport
    {
        //State
        private bool registered;
        private bool stockDelayRegister;
        private bool blizzyRegistered;
        private bool stockRegistered;
        private Texture2D buttonTexture;
		private ApplicationLauncherButton stockSyncrioButton;
        private IButton blizzyButton;
        //Singleton
        private static ToolbarSupport singleton;

        public static ToolbarSupport fetch
        {
            get
            {
                return singleton;
            }
        }

        public void DetectSettingsChange()
        {
            if (registered)
            {
                DisableToolbar();
                EnableToolbar();
            }
        }

        public void EnableToolbar()
        {
            buttonTexture = GameDatabase.Instance.GetTexture("Syncrio/Button/SyncrioButton", false);
            if (registered)
            {
                SyncrioLog.Debug("Cannot re-register toolbar");
                return;
            }
            registered = true;
            if (Settings.fetch.toolbarType == SyncrioToolbarType.DISABLED)
            {
                //Nothing!
            }
            if (Settings.fetch.toolbarType == SyncrioToolbarType.FORCE_STOCK)
            {
                EnableStockToolbar();
            }
            if (Settings.fetch.toolbarType == SyncrioToolbarType.BLIZZY_IF_INSTALLED)
            {
                if (ToolbarManager.ToolbarAvailable)
                {
                    EnableBlizzyToolbar();
                }
                else
                {
                    EnableStockToolbar();
                }
            }
            if (Settings.fetch.toolbarType == SyncrioToolbarType.BOTH_IF_INSTALLED)
            {
                if (ToolbarManager.ToolbarAvailable)
                {
                    EnableBlizzyToolbar();
                }
                EnableStockToolbar();
            }
        }

        public void DisableToolbar()
        {
            registered = false;
            if (blizzyRegistered)
            {
                DisableBlizzyToolbar();
            }
            if (stockRegistered)
            {
                DisableStockToolbar();
            }
        }

        private void EnableBlizzyToolbar()
        {
            blizzyRegistered = true;
            blizzyButton = ToolbarManager.Instance.add("Syncrio", "GUIButton");
            blizzyButton.OnClick += OnBlizzyClick;
            blizzyButton.ToolTip = "Toggle Syncrio windows";
            blizzyButton.TexturePath = "Syncrio/Button/SyncrioButtonLow";
            blizzyButton.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION);
            SyncrioLog.Debug("Registered blizzy toolbar");
        }

        private void DisableBlizzyToolbar()
        {
            blizzyRegistered = false;
            if (blizzyButton != null)
            {
                blizzyButton.Destroy();
            }
            SyncrioLog.Debug("Unregistered blizzy toolbar");
        }

        private void EnableStockToolbar()
        {
            stockRegistered = true;
            if (ApplicationLauncher.Ready)
            {
                EnableStockForRealsies();
            }
            else
            {
                stockDelayRegister = true;
                GameEvents.onGUIApplicationLauncherReady.Add(EnableStockForRealsies);
            }
            SyncrioLog.Debug("Registered stock toolbar");
        }

        private void EnableStockForRealsies()
        {
            if (stockDelayRegister)
            {
                stockDelayRegister = false;
                GameEvents.onGUIApplicationLauncherReady.Remove(EnableStockForRealsies);
            }
            stockSyncrioButton = ApplicationLauncher.Instance.AddModApplication(HandleButtonClick, HandleButtonClick, DoNothing, DoNothing, DoNothing, DoNothing, ApplicationLauncher.AppScenes.ALWAYS, buttonTexture);
        }

        private void DisableStockToolbar()
        {
            stockRegistered = false;
            if (stockDelayRegister)
            {
                stockDelayRegister = false;
                GameEvents.onGUIApplicationLauncherReady.Remove(EnableStockForRealsies);
            }
            if (stockSyncrioButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(stockSyncrioButton);
            }
            SyncrioLog.Debug("Unregistered stock toolbar");
        }

        private void OnBlizzyClick(ClickEvent clickArgs)
        {
            HandleButtonClick();
        }

        private void HandleButtonClick()
        {
            Client.fetch.toolbarShowGUI = !Client.fetch.toolbarShowGUI;
        }

        private void DoNothing()
        {
        }

        public static void Reset()
        {
            lock (Client.eventLock)
            {
                if (singleton != null)
                {
                    singleton.DisableToolbar();
                }
                singleton = new ToolbarSupport();
            }
        }
    }

    public enum SyncrioToolbarType
    {
        DISABLED,
        FORCE_STOCK,
        BLIZZY_IF_INSTALLED,
        BOTH_IF_INSTALLED
    }
}

