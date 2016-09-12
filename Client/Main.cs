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
using System.IO;
using UnityEngine;
using SyncrioCommon;
using System.Reflection;
using SyncrioClientSide.Utilities;

namespace SyncrioClientSide
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class Client : MonoBehaviour
    {
        private static Client singleton;
        //Global state vars
        public string status;
        public bool startGame;
        public bool forceQuit;
        public bool showGUI = true;
        public bool toolbarShowGUI = true;
        public bool modDisabled = false;
        public bool SyncrioSaveChecked = false;
        public string assemblyPath;
        //Game running is directly set from NetworkWorker.fetch after a successful connection
        public bool gameRunning;
        public bool fireReset;
        public GameMode gameMode;
        public bool serverAllowCheats = true;
        //Disconnect message
        public bool displayDisconnectMessage;
        private ScreenMessage disconnectMessage;
        private float lastDisconnectMessageCheck;
        public static List<Action> updateEvent = new List<Action>();
        public static List<Action> fixedUpdateEvent = new List<Action>();
        public static List<Action> drawEvent = new List<Action>();
        public static List<Action> resetEvent = new List<Action>();
        public static object eventLock = new object();
        //Chosen by a 2147483647 sided dice roll. Guaranteed to be random.
        public const int WINDOW_OFFSET = 1664952404;
        //Hack gravity fix.
        private Dictionary<CelestialBody, double> bodiesGees = new Dictionary<CelestialBody,double>();
        //Command line connect
        public static ServerEntry commandLineConnect;

        // Server setting
        public GameDifficulty serverDifficulty;
        public GameParameters serverParameters;

        public Client()
        {
            singleton = this;
        }

        public static Client fetch
        {
            get
            {
                return singleton;
            }
        }

        public void Awake()
        {
            Profiler.SyncrioReferenceTime.Start();
            GameObject.DontDestroyOnLoad(this);
            assemblyPath = new DirectoryInfo(Assembly.GetExecutingAssembly().Location).FullName;
            string kspPath = new DirectoryInfo(KSPUtil.ApplicationRootPath).FullName;
            //I find my abuse of Path.Combine distrubing.
            UnityEngine.Debug.Log("KSP installed at " + kspPath);
            UnityEngine.Debug.Log("Syncrio installed at " + assemblyPath);
            //Prevents symlink warning for development.
            if (Settings.fetch.disclaimerAccepted != 1)
            {
                modDisabled = true;
                DisclaimerWindow.Enable();
            }
            if (!CompatibilityChecker.IsCompatible())
            {
                modDisabled = true;
            }
            if (!InstallChecker.IsCorrectlyInstalled())
            {
                modDisabled = true;
            }
            SetupDirectoriesIfNeeded();
            //ScenarioSyncCache needs to run expiry here
            ScenarioSyncCache.fetch.ExpireCache();
            //Register events needed to bootstrap the workers.
            lock (eventLock)
            {
                resetEvent.Add(LockSystem.Reset);
                resetEvent.Add(AdminSystem.Reset);
                resetEvent.Add(GroupSystem.Reset);
                resetEvent.Add(ChatWorker.Reset);
                resetEvent.Add(CraftLibraryWorker.Reset);
                resetEvent.Add(DebugWindow.Reset);
                resetEvent.Add(DynamicTickWorker.Reset);
                resetEvent.Add(GroupWindow.Reset);
                resetEvent.Add(FlagSyncer.Reset);
                resetEvent.Add(KerbalReassigner.Reset);
                resetEvent.Add(PlayerColorWorker.Reset);
                resetEvent.Add(PlayerStatusWindow.Reset);
                resetEvent.Add(PlayerStatusWorker.Reset);
                resetEvent.Add(ScenarioWorker.Reset);
                resetEvent.Add(ScenarioWindow.Reset);
                resetEvent.Add(ScreenshotWorker.Reset);
                resetEvent.Add(TimeSyncer.Reset);
                resetEvent.Add(ToolbarSupport.Reset);
                resetEvent.Add(VesselWorker.Reset);
                GameEvents.onHideUI.Add(() =>
                {
                    showGUI = false;
                });
                GameEvents.onShowUI.Add(() =>
                {
                    showGUI = true;
                });
            }
            FireResetEvent();
            HandleCommandLineArgs();
            long testTime = Compression.TestSysIOCompression();
            SyncrioLog.Debug("System.IO compression works: " + Compression.sysIOCompressionWorks + ", test time: " + testTime + " ms.");
            SyncrioLog.Debug("Syncrio " + Common.PROGRAM_VERSION + ", protocol " + Common.PROTOCOL_VERSION + " Initialized!");
        }

        private void HandleCommandLineArgs()
        {
            bool nextLineIsAddress = false;
            bool valid = false;
            string address = null;
            int port = 7776;
            foreach (string commandLineArg in Environment.GetCommandLineArgs())
            {
                //Supporting IPv6 is FUN!
                if (nextLineIsAddress)
                {
                    valid = true;
                    nextLineIsAddress = false;
                    if (commandLineArg.Contains("Syncrio://"))
                    {
                        if (commandLineArg.Contains("[") && commandLineArg.Contains("]"))
                        {
                            //IPv6 literal
                            address = commandLineArg.Substring("Syncrio://[".Length);
                            address = address.Substring(0, address.LastIndexOf("]"));
                            if (commandLineArg.Contains("]:"))
                            {
                                //With port
                                string portString = commandLineArg.Substring(commandLineArg.LastIndexOf("]:") + 1);
                                if (!Int32.TryParse(portString, out port))
                                {
                                    valid = false;
                                }
                            }
                        }
                        else
                        {
                            //IPv4 literal or hostname
                            if (commandLineArg.Substring("Syncrio://".Length).Contains(":"))
                            {
                                //With port
                                address = commandLineArg.Substring("Syncrio://".Length);
                                address = address.Substring(0, address.LastIndexOf(":"));
                                string portString = commandLineArg.Substring(commandLineArg.LastIndexOf(":") + 1);
                                if (!Int32.TryParse(portString, out port))
                                {
                                    valid = false;
                                }
                            }
                            else
                            {
                                //Without port
                                address = commandLineArg.Substring("Syncrio://".Length);
                            }
                        }
                    }
                    else
                    {
                        valid = false;
                    }
                }

                if (commandLineArg == "-Syncrio")
                {
                    nextLineIsAddress = true;
                }
            }
            if (valid)
            {
                commandLineConnect = new ServerEntry();
                commandLineConnect.address = address;
                commandLineConnect.port = port;
                SyncrioLog.Debug("Connecting via command line to: " + address + ", port: " + port);
            }
            else
            {
                SyncrioLog.Debug("Command line address is invalid: " + address + ", port: " + port);
            }
        }

        public void Update()
        {
            long startClock = Profiler.SyncrioReferenceTime.ElapsedTicks;
            SyncrioLog.Update();

            if (modDisabled)
            {
                return;
            }
            try
            {
                if (HighLogic.LoadedScene == GameScenes.MAINMENU)
                {
                    if (!ModWorker.fetch.dllListBuilt)
                    {
                        ModWorker.fetch.dllListBuilt = true;
                        ModWorker.fetch.BuildDllFileList();
                    }
                    if (!SyncrioSaveChecked)
                    {
                        SyncrioSaveChecked = true;
                        SetupBlankGameIfNeeded();
                    }
                }

                //Handle GUI events
                if (!PlayerStatusWindow.fetch.disconnectEventHandled)
                {
                    PlayerStatusWindow.fetch.disconnectEventHandled = true;
                    forceQuit = true;
                    ScenarioWorker.fetch.scenarioSync(GroupSystem.playerGroupAssigned, false, true, false); // Send scenario modules before disconnecting
                    NetworkWorker.fetch.SendDisconnect("Quit");
                }
                if (!ConnectionWindow.fetch.renameEventHandled)
                {
                    PlayerStatusWorker.fetch.myPlayerStatus.playerName = Settings.fetch.playerName;
                    Settings.fetch.SaveSettings();
                    ConnectionWindow.fetch.renameEventHandled = true;
                }
                if (!ConnectionWindow.fetch.addEventHandled)
                {
                    Settings.fetch.servers.Add(ConnectionWindow.fetch.addEntry);
                    ConnectionWindow.fetch.addEntry = null;
                    Settings.fetch.SaveSettings();
                    ConnectionWindow.fetch.addingServer = false;
                    ConnectionWindow.fetch.addEventHandled = true;
                }
                if (!ConnectionWindow.fetch.editEventHandled)
                {
                    Settings.fetch.servers[ConnectionWindow.fetch.selected].name = ConnectionWindow.fetch.editEntry.name;
                    Settings.fetch.servers[ConnectionWindow.fetch.selected].address = ConnectionWindow.fetch.editEntry.address;
                    Settings.fetch.servers[ConnectionWindow.fetch.selected].port = ConnectionWindow.fetch.editEntry.port;
                    ConnectionWindow.fetch.editEntry = null;
                    Settings.fetch.SaveSettings();
                    ConnectionWindow.fetch.addingServer = false;
                    ConnectionWindow.fetch.editEventHandled = true;
                }
                if (!ConnectionWindow.fetch.removeEventHandled)
                {
                    Settings.fetch.servers.RemoveAt(ConnectionWindow.fetch.selected);
                    ConnectionWindow.fetch.selected = -1;
                    Settings.fetch.SaveSettings();
                    ConnectionWindow.fetch.removeEventHandled = true;
                }
                if (!ConnectionWindow.fetch.connectEventHandled)
                {
                    ConnectionWindow.fetch.connectEventHandled = true;
                    NetworkWorker.fetch.ConnectToServer(Settings.fetch.servers[ConnectionWindow.fetch.selected].address, Settings.fetch.servers[ConnectionWindow.fetch.selected].port);
                }
                if (commandLineConnect != null && HighLogic.LoadedScene == GameScenes.MAINMENU && Time.timeSinceLevelLoad > 1f)
                {
                    NetworkWorker.fetch.ConnectToServer(commandLineConnect.address, commandLineConnect.port);
                    commandLineConnect = null;
                }

                if (!ConnectionWindow.fetch.disconnectEventHandled)
                {
                    ConnectionWindow.fetch.disconnectEventHandled = true;
                    gameRunning = false;
                    fireReset = true;
                    if (NetworkWorker.fetch.state == ClientState.CONNECTING)
                    {
                        NetworkWorker.fetch.Disconnect("Cancelled connection to server");
                    }
                    else
                    {
                        NetworkWorker.fetch.SendDisconnect("Quit during initial sync");
                    }
                }

                foreach (Action updateAction in updateEvent)
                {
                    try
                    {
                        updateAction();
                    }
                    catch (Exception e)
                    {
                        SyncrioLog.Debug("Threw in UpdateEvent, exception: " + e);
                        if (NetworkWorker.fetch.state != ClientState.RUNNING)
                        {
                            if (NetworkWorker.fetch.state != ClientState.DISCONNECTED)
                            {
                                NetworkWorker.fetch.SendDisconnect("Unhandled error while syncing!");
                            }
                            else
                            {
                                NetworkWorker.fetch.Disconnect("Unhandled error while syncing!");
                            }
                        }
                    }
                }
                //Force quit
                if (forceQuit)
                {
                    forceQuit = false;
                    gameRunning = false;
                    fireReset = true;
                    StopGame();
                }

                if (displayDisconnectMessage)
                {
                    if (HighLogic.LoadedScene != GameScenes.MAINMENU)
                    {
                        if ((UnityEngine.Time.realtimeSinceStartup - lastDisconnectMessageCheck) > 1f)
                        {
                            lastDisconnectMessageCheck = UnityEngine.Time.realtimeSinceStartup;
                            if (disconnectMessage != null)
                            {
                                disconnectMessage.duration = 0;
                            }
                            disconnectMessage = ScreenMessages.PostScreenMessage("You have been disconnected!", 2f, ScreenMessageStyle.UPPER_CENTER);
                        }
                    }
                    else
                    {
                        displayDisconnectMessage = false;
                    }
                }

                //Normal quit
                if (gameRunning)
                {
                    if (!Settings.fetch.DarkMultiPlayerCoopMode)
                    {
                        if (HighLogic.LoadedScene == GameScenes.MAINMENU)
                        {
                            gameRunning = false;
                            fireReset = true;
                            NetworkWorker.fetch.SendDisconnect("Quit to main menu");
                        }

                        if (ScreenshotWorker.fetch.uploadScreenshot)
                        {
                            ScreenshotWorker.fetch.uploadScreenshot = false;
                            StartCoroutine(UploadScreenshot());
                        }

                        if (HighLogic.CurrentGame.flagURL != Settings.fetch.selectedFlag)
                        {
                            SyncrioLog.Debug("Saving selected flag");
                            Settings.fetch.selectedFlag = HighLogic.CurrentGame.flagURL;
                            Settings.fetch.SaveSettings();
                            FlagSyncer.fetch.flagChangeEvent = true;
                        }
                    }

                    // save every GeeASL from each body in FlightGlobals
                    if (HighLogic.LoadedScene == GameScenes.FLIGHT && bodiesGees.Count == 0)
                    {
                        foreach (CelestialBody body in FlightGlobals.fetch.bodies)
                        {
                            bodiesGees.Add(body, body.GeeASL);
                        }
                    }

                    //handle use of cheats
                    if (!serverAllowCheats)
                    {
                        CheatOptions.InfinitePropellant = false;
                        CheatOptions.NoCrashDamage = false;

                        foreach (KeyValuePair<CelestialBody, double> gravityEntry in bodiesGees)
                        {
                            gravityEntry.Key.GeeASL = gravityEntry.Value;
                        }
                    }

                    if (HighLogic.LoadedScene == GameScenes.FLIGHT && FlightGlobals.ready)
                    {
                        HighLogic.CurrentGame.Parameters.Flight.CanLeaveToSpaceCenter = Settings.fetch.revertEnabled || (PauseMenu.canSaveAndExit == ClearToSaveStatus.CLEAR);
                    }
                    else
                    {
                        HighLogic.CurrentGame.Parameters.Flight.CanLeaveToSpaceCenter = true;
                    }
                }

                if (fireReset)
                {
                    fireReset = false;
                    FireResetEvent();
                }

                if (startGame)
                {
                    startGame = false;
                    StartGame();
                }
            }
            catch (Exception e)
            {
                SyncrioLog.Debug("Threw in Update, state " + NetworkWorker.fetch.state.ToString() + ", exception " + e);
                if (NetworkWorker.fetch.state != ClientState.RUNNING)
                {
                    if (NetworkWorker.fetch.state != ClientState.DISCONNECTED)
                    {
                        NetworkWorker.fetch.SendDisconnect("Unhandled error while syncing!");
                    }
                    else
                    {
                        NetworkWorker.fetch.Disconnect("Unhandled error while syncing!");
                    }
                }
            }
            Profiler.updateData.ReportTime(startClock);
        }

        public IEnumerator<WaitForEndOfFrame> UploadScreenshot()
        {
            yield return new WaitForEndOfFrame();
            ScreenshotWorker.fetch.SendScreenshot();
            ScreenshotWorker.fetch.screenshotTaken = true;
        }

        public void FixedUpdate()
        {
            long startClock = Profiler.SyncrioReferenceTime.ElapsedTicks;
            if (modDisabled)
            {
                return;
            }
            foreach (Action fixedUpdateAction in fixedUpdateEvent)
            {
                try
                {
                    fixedUpdateAction();
                }
                catch (Exception e)
                {
                    SyncrioLog.Debug("Threw in FixedUpdate event, exception: " + e);
                    if (NetworkWorker.fetch.state != ClientState.RUNNING)
                    {
                        if (NetworkWorker.fetch.state != ClientState.DISCONNECTED)
                        {
                            NetworkWorker.fetch.SendDisconnect("Unhandled error while syncing!");
                        }
                        else
                        {
                            NetworkWorker.fetch.Disconnect("Unhandled error while syncing!");
                        }
                    }
                }
            }
            Profiler.fixedUpdateData.ReportTime(startClock);
        }

        public void OnGUI()
        {
            //Window ID's - Doesn't include "random" offset.
            //Minimized Status window: 7701
            //Connection window: 7702
            //Status window: 7703
            //Chat window: 7704
            //Debug window: 7705
            //Mod window: 7706
            //Craft library window: 7707
            //Craft upload window: 7708
            //Screenshot window: 7710
            //Options window: 7711
            //Converter window: 7712
            //Disclaimer window: 7713
            //Group window: 7714
            //Group Become Leader window: 7715
            //Group Invite Player window: 7716
            //Scenario window: 7717
            long startClock = Profiler.SyncrioReferenceTime.ElapsedTicks;
            if (showGUI && toolbarShowGUI)
            {
                foreach (Action drawAction in drawEvent)
                {
                    try
                    {
                        drawAction();
                    }
                    catch (Exception e)
                    {
                        SyncrioLog.Debug("Threw in OnGUI event, exception: " + e);
                    }
                }
            }
            Profiler.guiData.ReportTime(startClock);
        }

        private void StartGame()
        {
            //Create new game object for our Syncrio session.
            HighLogic.CurrentGame = CreateBlankGame();

            //Set the game mode
            HighLogic.CurrentGame.Mode = ConvertGameMode(gameMode);

            //Set difficulty
            HighLogic.CurrentGame.Parameters = serverParameters;
            
            //Load Syncrio stuff
            VesselWorker.fetch.LoadKerbalsIntoGame();

            //Load the scenarios from the server
            ScenarioWorker.fetch.LoadScenarioDataIntoGame();

            //Load the missing scenarios as well (Eg, Contracts and stuff for career mode
            ScenarioWorker.fetch.LoadMissingScenarioDataIntoGame();

            //This only makes KSP complain
            HighLogic.CurrentGame.CrewRoster.ValidateAssignments(HighLogic.CurrentGame);
            SyncrioLog.Debug("Starting " + gameMode + " game...");

            //.Start() seems to stupidly .Load() somewhere - Let's overwrite it so it loads correctly.
            GamePersistence.SaveGame(HighLogic.CurrentGame, "persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
            HighLogic.CurrentGame.Start();
            ChatWorker.fetch.display = true;
            SyncrioLog.Debug("Started!");
        }

        private void StopGame()
        {
            HighLogic.SaveFolder = "Syncrio";
            if (HighLogic.LoadedScene != GameScenes.MAINMENU)
            {
                HighLogic.LoadScene(GameScenes.MAINMENU);
            }
            //HighLogic.CurrentGame = null; This is no bueno
            bodiesGees.Clear();
        }

        public Game.Modes ConvertGameMode(GameMode inputMode)
        {
            if (inputMode == GameMode.SANDBOX)
            {
                return Game.Modes.SANDBOX;
            }
            if (inputMode == GameMode.SCIENCE)
            {
                return Game.Modes.SCIENCE_SANDBOX;
            }
            if (inputMode == GameMode.CAREER)
            {
                return Game.Modes.CAREER;
            }
            return Game.Modes.SANDBOX;
        }

        private void FireResetEvent()
        {
            foreach (Action resetAction in resetEvent)
            {
                try
                {
                    resetAction();
                }
                catch (Exception e)
                {
                    SyncrioLog.Debug("Threw in FireResetEvent, exception: " + e);
                }
            }
        }

        private void OnApplicationQuit()
        {
            if (gameRunning && NetworkWorker.fetch.state == ClientState.RUNNING)
            {
                Application.CancelQuit();
                ScenarioWorker.fetch.scenarioSync(GroupSystem.playerGroupAssigned, false, true, false);
                HighLogic.LoadScene(GameScenes.MAINMENU);
            }
        }

        private void SetupDirectoriesIfNeeded()
        {
            string SyncrioSavesDirectory = Path.Combine(Path.Combine(KSPUtil.ApplicationRootPath, "saves"), "Syncrio");
            CreateIfNeeded(SyncrioSavesDirectory);
            CreateIfNeeded(Path.Combine(SyncrioSavesDirectory, "Ships"));
            CreateIfNeeded(Path.Combine(SyncrioSavesDirectory, Path.Combine("Ships", "VAB")));
            CreateIfNeeded(Path.Combine(SyncrioSavesDirectory, Path.Combine("Ships", "SPH")));
            CreateIfNeeded(Path.Combine(SyncrioSavesDirectory, "Subassemblies"));
            string SyncrioCacheDirectory = Path.Combine(Path.Combine(Path.Combine(KSPUtil.ApplicationRootPath, "GameData"), "Syncrio"), "Cache");
            CreateIfNeeded(SyncrioCacheDirectory);
            string SyncrioIncomingCacheDirectory = Path.Combine(Path.Combine(Path.Combine(Path.Combine(KSPUtil.ApplicationRootPath, "GameData"), "Syncrio"), "Cache"), "Incoming");
            CreateIfNeeded(SyncrioIncomingCacheDirectory);
            string SyncrioFlagsDirectory = Path.Combine(Path.Combine(Path.Combine(KSPUtil.ApplicationRootPath, "GameData"), "Syncrio"), "Flags");
            CreateIfNeeded(SyncrioFlagsDirectory);
        }

        private void SetupBlankGameIfNeeded()
        {
            string persistentFile = Path.Combine(Path.Combine(Path.Combine(KSPUtil.ApplicationRootPath, "saves"), "Syncrio"), "persistent.sfs");
            if (!File.Exists(persistentFile))
            {
                SyncrioLog.Debug("Creating new blank persistent.sfs file");
                Game blankGame = CreateBlankGame();
                HighLogic.SaveFolder = "Syncrio";
                GamePersistence.SaveGame(blankGame, "persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
            }
        }

        private Game CreateBlankGame()
        {
            Game returnGame = new Game();
            //KSP complains about a missing message system if we don't do this.
            returnGame.additionalSystems = new ConfigNode();
            returnGame.additionalSystems.AddNode("MESSAGESYSTEM");

            //Flightstate is null on new Game();
            returnGame.flightState = new FlightState();
            if (returnGame.flightState.mapViewFilterState == 0)
            {
                returnGame.flightState.mapViewFilterState = -1026;
            }

            //Syncrio stuff
            returnGame.startScene = GameScenes.SPACECENTER;
            returnGame.flagURL = Settings.fetch.selectedFlag;
            returnGame.Title = "Syncrio";

            HighLogic.SaveFolder = "Syncrio";

            return returnGame;
        }

        private void CreateIfNeeded(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}

