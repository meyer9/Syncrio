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
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.Text;
using SyncrioCommon;
using SettingsParser;

namespace SyncrioServer
{
    public class Server
    {
        public static bool serverRunning;
        public static bool serverStarting;
        public static bool serverRestarting;
        public static string ScenarioDirectory;
        public static string configDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        public static Stopwatch serverClock;
        public static HttpListener httpListener;
        private static long ctrlCTime;
        public static int playerCount = 0;
        public static string players = "";
        public static long lastPlayerActivity;
        public static object ScenarioSizeLock = new object();
        private static int day;
        private static bool syncing = false;
        private static int numberOfClientsInGroupsAtSync = 0;

        public static void Main()
        {
            #if !DEBUG
            try
            {
            #endif
                //Start the server clock
                serverClock = new Stopwatch();
                serverClock.Start();

                Settings.Reset();

                //Set the last player activity time to server start
                lastPlayerActivity = serverClock.ElapsedMilliseconds;

                //Periodic garbage collection
                long lastGarbageCollect = 0;
                
                //Periodic screenshot check
                long lastScreenshotExpiredCheck = 0;

                //Periodic log check
                long lastLogExpiredCheck = 0;

                //Periodic day check
                long lastDayCheck = 0;

                //Periodic scenario sync
                long lastScenarioSendTime = 0;

                long autoSyncScenariosWaitInterval = Settings.settingsStore.autoSyncScenariosWaitInterval * 60000;

                //Set Scenario directory and modfile path
                ScenarioDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scenarios");

                if (!Directory.Exists(configDirectory))
                {
                    Directory.CreateDirectory(configDirectory);
                }

                string oldSettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SyncrioServerSettings.txt");
                string newSettingsFile = Path.Combine(Server.configDirectory, "Settings.txt");
                string oldGameplayFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SyncrioGameplaySettings.txt");
                string newGameplayFile = Path.Combine(Server.configDirectory, "GameplaySettings.txt");

                //Register the server commands
                CommandHandler.RegisterCommand("exit", Server.ShutDown, "Shuts down the server");
                CommandHandler.RegisterCommand("quit", Server.ShutDown, "Shuts down the server");
                CommandHandler.RegisterCommand("shutdown", Server.ShutDown, "Shuts down the server");
                CommandHandler.RegisterCommand("restart", Server.Restart, "Restarts the server");
                CommandHandler.RegisterCommand("kick", KickCommand.KickPlayer, "Kicks a player from the server");
                CommandHandler.RegisterCommand("ban", BanSystem.fetch.BanPlayer, "Bans a player from the server");
                CommandHandler.RegisterCommand("banip", BanSystem.fetch.BanIP, "Bans an IP Address from the server");
                CommandHandler.RegisterCommand("bankey", BanSystem.fetch.BanPublicKey, "Bans a Guid from the server");
                CommandHandler.RegisterCommand("pm", PMCommand.HandleCommand, "Sends a message to a player");
                CommandHandler.RegisterCommand("admin", AdminCommand.HandleCommand, "Sets a player as admin/removes admin from the player");
                CommandHandler.RegisterCommand("group", GroupCommand.HandleCommand, "Modify player groups");
                CommandHandler.RegisterCommand("whitelist", WhitelistCommand.HandleCommand, "Change the server whitelist");
                //Register the ctrl+c event
                Console.CancelKeyPress += new ConsoleCancelEventHandler(CatchExit);
                serverStarting = true;
                
                if (System.Net.Sockets.Socket.OSSupportsIPv6)
                {
                    Settings.settingsStore.address = "::";
                }

                SyncrioLog.Debug("Loading settings...");
                Settings.Load();
                if (Settings.settingsStore.gameDifficulty == GameDifficulty.CUSTOM)
                {
                    GameplaySettings.Reset();
                    GameplaySettings.Load();
                }

                //Test compression
                if (Settings.settingsStore.compressionEnabled)
                {
                    long testTime = Compression.TestSysIOCompression();
                    Compression.compressionEnabled = true;
                    SyncrioLog.Debug("System.IO compression works: " + Compression.sysIOCompressionWorks + ", test time: " + testTime + " ms.");
                }

                //Set day for log change
                day = DateTime.Now.Day;

                //Load plugins
                SyncrioPluginHandler.LoadPlugins();

                Console.Title = "SyncrioServer " + Common.PROGRAM_VERSION + ", protocol " + Common.PROTOCOL_VERSION;

                while (serverStarting || serverRestarting)
                {
                    if (serverRestarting)
                    {
                        SyncrioLog.Debug("Reloading settings...");
                        Settings.Reset();
                        Settings.Load();
                        if (Settings.settingsStore.gameDifficulty == GameDifficulty.CUSTOM)
                        {
                            SyncrioLog.Debug("Reloading gameplay settings...");
                            GameplaySettings.Reset();
                            GameplaySettings.Load();
                        }
                    }

                    serverRestarting = false;
                    SyncrioLog.Normal("Starting SyncrioServer " + Common.PROGRAM_VERSION + ", protocol " + Common.PROTOCOL_VERSION);

                    if (Settings.settingsStore.gameDifficulty == GameDifficulty.CUSTOM)
                    {
                        //Generate the config file by accessing the object.
                        SyncrioLog.Debug("Loading gameplay settings...");
                        GameplaySettings.Load();
                    }

                    //Load Scenario
                    SyncrioLog.Normal("Loading Scenario... ");
                    CheckScenario();

                    SyncrioLog.Normal("Starting Syncrio Server on port " + Settings.settingsStore.port + "... ");

                    serverRunning = true;
                    Thread commandThread = new Thread(new ThreadStart(CommandHandler.ThreadMain));
                    Thread clientThread = new Thread(new ThreadStart(ClientHandler.ThreadMain));
                    commandThread.Start();
                    clientThread.Start();
                    while (serverStarting)
                    {
                        Thread.Sleep(500);
                    }

                    StartHTTPServer();

                    GroupSystem.fetch.ServerStarting();
                    while (!GroupSystem.fetch.groupsLoaded)
                    {
                        Thread.Sleep(500);
                    }

                    SyncrioLog.Normal("Ready!");
                    SyncrioPluginHandler.FireOnServerStart();
                    while (serverRunning)
                    {
                        //Run a garbage collection every 30 seconds.
                        if ((serverClock.ElapsedMilliseconds - lastGarbageCollect) > 30000)
                        {
                            lastGarbageCollect = serverClock.ElapsedMilliseconds;
                            GC.Collect();
                        }
                        //Run the screenshot expire function every 10 minutes
                        if ((serverClock.ElapsedMilliseconds - lastScreenshotExpiredCheck) > 600000)
                        {
                            lastScreenshotExpiredCheck = serverClock.ElapsedMilliseconds;
                            ScreenshotExpire.ExpireScreenshots();
                        }
                        //Run the log expire function every 10 minutes
                        if ((serverClock.ElapsedMilliseconds - lastLogExpiredCheck) > 600000)
                        {
                            lastLogExpiredCheck = serverClock.ElapsedMilliseconds;
                            LogExpire.ExpireLogs();
                        }
                        // Check if the day has changed, every minute
                        if ((serverClock.ElapsedMilliseconds - lastDayCheck) > 60000)
                        {
                            lastDayCheck = serverClock.ElapsedMilliseconds;
                            if (day != DateTime.Now.Day)
                            {
                                SyncrioLog.LogFilename = Path.Combine(SyncrioLog.LogFolder, "Syncrioserver " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".log");
                                SyncrioLog.WriteToLog("Continued from logfile " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".log");
                                day = DateTime.Now.Day;
                            }
                        }
                        //Auto Sync Scenarios
                        if (Settings.settingsStore.autoSyncScenarios && autoSyncScenariosWaitInterval != 0)
                        {
                            if ((serverClock.ElapsedMilliseconds - lastScenarioSendTime) > autoSyncScenariosWaitInterval)
                            {
                                lastScenarioSendTime = serverClock.ElapsedMilliseconds;
                                ScenarioSystem.fetch.numberOfPlayersSyncing = 0;
                                numberOfClientsInGroupsAtSync = 0;
                                foreach (ClientObject client in ClientHandler.GetClients())
                                {
                                    if (GroupSystem.fetch.PlayerIsInGroup(client.playerName))
                                    {
                                        ScenarioSystem.fetch.SendAutoSyncScenarioRequest(client);
                                        ScenarioSystem.fetch.numberOfPlayersSyncing += 1;
                                        numberOfClientsInGroupsAtSync += 1;
                                    }
                                }
                                syncing = true;
                            }

                            if (syncing)
                            {
                                int playersInGroups = GroupSystem.fetch.GetNumberOfPlayersInAllGroups();

                                if (numberOfClientsInGroupsAtSync > playersInGroups)
                                {
                                    int lostPlayers = numberOfClientsInGroupsAtSync - playersInGroups;
                                    numberOfClientsInGroupsAtSync -= lostPlayers;
                                    ScenarioSystem.fetch.numberOfPlayersSyncing -= lostPlayers;
                                }
                            }
                        }
                        if (syncing && ScenarioSystem.fetch.numberOfPlayersSyncing == 0)
                        {
                            syncing = false;
                            foreach (ClientObject client in ClientHandler.GetClients())
                            {
                                if (GroupSystem.fetch.PlayerIsInGroup(client.playerName))
                                {
                                    Messages.ScenarioData.SendScenarioGroupModules(client, GroupSystem.fetch.GetPlayerGroup(client.playerName));
                                }
                                else
                                {
                                    //Don't care
                                }
                            }
                        }

                        Thread.Sleep(500);
                    }
                    SyncrioPluginHandler.FireOnServerStop();
                    commandThread.Abort();
                    clientThread.Join();
                }
                SyncrioLog.Normal("Goodbye!");
                Environment.Exit(0);
            #if !DEBUG
            }
            catch (Exception e)
            {
                SyncrioLog.Fatal("Error in main server thread, Exception: " + e);
                throw;
            }
            #endif
        }

        // Check Scenario folder size
        public static long GetScenarioSize()
        {
            lock (ScenarioSizeLock)
            {
                long directorySize = 0;
                string[] kerbals = Directory.GetFiles(Path.Combine(ScenarioDirectory, "Kerbals"), "*.*");
                string[] vessels = Directory.GetFiles(Path.Combine(ScenarioDirectory, "Vessels"), "*.*");

                foreach (string kerbal in kerbals)
                {
                    FileInfo kInfo = new FileInfo(kerbal);
                    directorySize += kInfo.Length;
                }

                foreach (string vessel in vessels)
                {
                    FileInfo vInfo = new FileInfo(vessel);
                    directorySize += vInfo.Length;
                }

                return directorySize;
            }
        }
        //Get last disconnect time
        public static long GetLastPlayerActivity()
        {
            if (playerCount > 0)
            {
                return 0;
            }
            return (serverClock.ElapsedMilliseconds - lastPlayerActivity) / 1000;
        }
        //Create Scenario directories
        private static void CheckScenario()
        {
            if (!Directory.Exists(ScenarioDirectory))
            {
                Directory.CreateDirectory(ScenarioDirectory);
            }
            if (!Directory.Exists(Path.Combine(ScenarioDirectory, "Crafts")))
            {
                Directory.CreateDirectory(Path.Combine(ScenarioDirectory, "Crafts"));
            }
            if (!Directory.Exists(Path.Combine(ScenarioDirectory, "Flags")))
            {
                Directory.CreateDirectory(Path.Combine(ScenarioDirectory, "Flags"));
            }
            if (!Directory.Exists(Path.Combine(ScenarioDirectory, "Kerbals")))
            {
                Directory.CreateDirectory(Path.Combine(ScenarioDirectory, "Kerbals"));
            }
            if (!Directory.Exists(Path.Combine(ScenarioDirectory, "Groups")))
            {
                Directory.CreateDirectory(Path.Combine(ScenarioDirectory, "Groups"));
            }
            if (!Directory.Exists(Path.Combine(ScenarioDirectory, "Groups", "Initial")))
            {
                Directory.CreateDirectory(Path.Combine(ScenarioDirectory, "Groups", "Initial"));
            }
            if (!Directory.Exists(Path.Combine(ScenarioDirectory, "Players")))
            {
                Directory.CreateDirectory(Path.Combine(ScenarioDirectory, "Players"));
            }
            if (!Directory.Exists(Path.Combine(ScenarioDirectory, "Players", "Initial")))
            {
                Directory.CreateDirectory(Path.Combine(ScenarioDirectory, "Players", "Initial"));
            }
        }
        //Shutdown
        public static void ShutDown(string commandArgs)
        {
            if (commandArgs != "")
            {
                SyncrioLog.Normal("Shutting down - " + commandArgs);
                Messages.ConnectionEnd.SendConnectionEndToAll("Server is shutting down - " + commandArgs);
            }
            else
            {
                SyncrioLog.Normal("Shutting down");
                Messages.ConnectionEnd.SendConnectionEndToAll("Server is shutting down");
            }
            serverStarting = false;
            serverRunning = false;
            StopHTTPServer();
        }
        //Restart
        private static void Restart(string commandArgs)
        {
            if (commandArgs != "")
            {
                SyncrioLog.Normal("Restarting - " + commandArgs);
                Messages.ConnectionEnd.SendConnectionEndToAll("Server is restarting - " + commandArgs);
            }
            else
            {
                SyncrioLog.Normal("Restarting");
                Messages.ConnectionEnd.SendConnectionEndToAll("Server is restarting");
            }
            serverRestarting = true;
            serverStarting = false;
            serverRunning = false;
            ForceStopHTTPServer();
        }
        //Gracefully shut down
        private static void CatchExit(object sender, ConsoleCancelEventArgs args)
        {
            //If control+c not pressed within 5 seconds, catch it and shutdown gracefully.
            if ((DateTime.UtcNow.Ticks - ctrlCTime) > 50000000)
            {
                ctrlCTime = DateTime.UtcNow.Ticks;
                args.Cancel = true;
                ShutDown("Caught Ctrl+C");
            }
            else
            {
                SyncrioLog.Debug("Terminating!");
            }
        }

        private static void StartHTTPServer()
        {
            string OS = Environment.OSVersion.Platform.ToString();
            if (Settings.settingsStore.httpPort > 0)
            {
                SyncrioLog.Normal("Starting HTTP server...");
                httpListener = new HttpListener();
                try
                {
                    if (Settings.settingsStore.address != "0.0.0.0" && Settings.settingsStore.address != "::")
                    {
                        string listenAddress = Settings.settingsStore.address;
                        if (listenAddress.Contains(":"))
                        {
                            //Sorry
                            SyncrioLog.Error("Error: The server status port does not support specific IPv6 addresses. Sorry.");
                            //listenAddress = "[" + listenAddress + "]";
                            return;

                        }

                        httpListener.Prefixes.Add("http://" + listenAddress + ":" + Settings.settingsStore.httpPort + '/');
                    }
                    else
                    {
                        httpListener.Prefixes.Add("http://*:" + Settings.settingsStore.httpPort + '/');
                    }
                    httpListener.Start();
                    httpListener.BeginGetContext(asyncHTTPCallback, httpListener);
                }
                catch (HttpListenerException e)
                {
                    if (OS == "Win32NT" || OS == "Win32S" || OS == "Win32Windows" || OS == "WinCE") // if OS is Windows
                    {
                        if (e.ErrorCode == 5) // Access Denied
                        {
                            SyncrioLog.Debug("HTTP Server: access denied.");
                            SyncrioLog.Debug("Prompting user to switch to administrator mode.");

                            ProcessStartInfo startInfo = new ProcessStartInfo("SyncrioServer.exe") { Verb = "runas" };
                            Process.Start(startInfo);

                            Environment.Exit(0);
                        }
                    }
                    else
                    {
                        SyncrioLog.Fatal("Error while starting HTTP server.\n" + e);
                    }
                    throw;
                }
            }
        }

        private static void StopHTTPServer()
        {
            if (Settings.settingsStore.httpPort > 0)
            {
                SyncrioLog.Normal("Stopping HTTP server...");
                httpListener.Stop();
            }
        }

        private static void ForceStopHTTPServer()
        {
            if (Settings.settingsStore.httpPort > 0)
            {
                SyncrioLog.Normal("Force stopping HTTP server...");
                if (httpListener != null)
                {
                    try
                    {
                        httpListener.Abort();
                    }
                    catch (Exception e)
                    {
                        SyncrioLog.Fatal("Error trying to shutdown HTTP server: " + e);
                        throw;
                    }
                }
            }
        }

        private static void asyncHTTPCallback(IAsyncResult result)
        {
            try
            {
                HttpListener listener = (HttpListener)result.AsyncState;

                HttpListenerContext context = listener.EndGetContext(result);
                string responseText = "";
                bool handled = false;
                
                if (!handled)
                {
                    responseText = new ServerInfo(Settings.settingsStore).GetJSON();
                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseText);
                context.Response.ContentLength64 = buffer.LongLength;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();

                listener.BeginGetContext(asyncHTTPCallback, listener);
            }
            catch (Exception e)
            {
                //Ignore the EngGetContext throw while shutting down the HTTP server.
                if (serverRunning)
                {
                    SyncrioLog.Error("Exception while listening to HTTP server!, Exception:\n" + e);
                    Thread.Sleep(1000);
                    httpListener.BeginGetContext(asyncHTTPCallback, httpListener);
                }
            }
        }
    }
}

