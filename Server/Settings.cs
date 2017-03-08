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
using System.Net;
using SyncrioCommon;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using SettingsParser;

namespace SyncrioServer
{
    public class Settings
    {
        private static ConfigParser<SettingsStore> serverSettings;
        private static ConfigParser<SpecialSettingsStore> specialSettings;
        public static SettingsStore settingsStore
        {
            get
            {
                return serverSettings.Settings;
            }
        }
        public static SpecialSettingsStore specialSettingsStore
        {
            get
            {
                return specialSettings.Settings;
            }
        }

        public static void Reset()
        {
            serverSettings = new ConfigParser<SettingsStore>(new SettingsStore(), Path.Combine(Server.configDirectory, "Settings.txt"));
            specialSettings = new ConfigParser<SpecialSettingsStore>(new SpecialSettingsStore(), Path.Combine(Server.configDirectory, "SpecialSettings.txt"));
        }

        public static void Load()
        {
            serverSettings.LoadSettings();
            specialSettings.LoadSettings();
        }

        public static void Save()
        {
            serverSettings.SaveSettings();
            specialSettings.SaveSettings();
        }

        private static void CheckForCareerMode()
        {
            if (serverSettings.Settings.gameMode == GameMode.CAREER)
            {
                serverSettings.Settings.numberOfKerbals = 0;
                Save();
            }
        }
    }

    public class SettingsStore
    {
        [Description("The address the server listens on.\n# WARNING: You do not need to change this unless you are running 2 servers on the same port.\n# Changing this setting from 0.0.0.0 will only give you trouble if you aren't running multiple servers.\n# Change this setting to :: to listen on IPv4 and IPv6.")]
        public string address = "0.0.0.0";
        [Description("The port the server listens on.")]
        public int port = 7776;
        [Description("Specify the warp type.")]
        public WarpMode warpMode = WarpMode.SUBSPACE;
        [Description("Specify the game type.")]
        public GameMode gameMode = GameMode.CAREER;
        [Description("Specify the gameplay difficulty of the server.")]
        public GameDifficulty gameDifficulty = GameDifficulty.NORMAL;
        [Description("Enable white-listing.")]
        public bool whitelisted = false;
        [Description("Enable mod control.\n# WARNING: Only consider turning off mod control for private servers and you know all players will have the same mods installed.\n# The game will constantly complain about missing parts if there are missing mods.")]
        public ModControlMode modControl = ModControlMode.ENABLED_STOP_INVALID_PART_JOIN;
        [Description("Specify if the the server Scenario 'ticks' while nobody is connected or the server is shut down.")]
        public bool keepTickingWhileOffline = true;
        [Description("If true, sends the player to the latest subspace upon connecting. If false, sends the player to the previous subspace they were in.\n# NOTE: This may cause time-paradoxes, and will not work across server restarts.")]
        public bool sendPlayerToLatestSubspace = true;
        [Description("Use UTC instead of system time in the log.")]
        public bool useUTCTimeInLog = false;
        [Description("Minimum log level.")]
        public SyncrioLog.LogLevels logLevel = SyncrioLog.LogLevels.DEBUG;
        [Description("Specify maximum number of screenshots to save per player. -1 = None, 0 = Unlimited")]
        public int screenshotsPerPlayer = 20;
        [Description("Specify vertical resolution of screenshots.")]
        public int screenshotHeight = 720;
        [Description("Enable use of cheats in-game.")]
        public bool cheats = false;
        [Description("HTTP port for server status. 0 = Disabled")]
        public int httpPort = 0;
        [Description("Name of the server.")]
        public string serverName = "Syncrio Server";
        [Description("Base number of kerbals Syncrio generates. 0 = Disabled.\n# -Note! If DarkMultiPlayer Cooperative Mode is on then this will do nothing.\n# -Note! If the game mode is set to career then this is automaticly set to 0.")]
        public int numberOfKerbals = 0;
        [Description("Maximum amount of players that can join the server.")]
        public int maxPlayers = 20;
        [Description("Maximum amount of player scenario groups that can be exist on the server.")]
        public int maxGroups = 15;
        [Description("Specify a custom screenshot directory.\n#This directory must exist in order to be used. Leave blank to store it in Scenario Folder.")]
        public string screenshotDirectory = string.Empty;
        [Description("Specify the name that will appear when you send a message using the server's console.")]
        public string consoleIdentifier = "Server";
        [Description("Specify the server's MOTD (message of the day).")]
        public string serverMotd = "Welcome, %name%!";
        [Description("Specify the amount of days a screenshot should be considered as expired and deleted. 0 = Disabled")]
        public double expireScreenshots = 0;
        [Description("Specify whether to enable compression. Decreases bandwidth usage but increases CPU usage. 0 = Disabled")]
        public bool compressionEnabled = true;
        [Description("Specify the amount of days a log file should be considered as expired and deleted. 0 = Disabled")]
        public double expireLogs = 0;
    }

    public class SpecialSettingsStore
    {
        [Description("Enable DarkMultiPlayer Cooperative Mode.\n# WARNING: If disabled Syncrio will not work with DarkMultiPlayer!\n# WARNING: If enabled it Must be enabled on the client side as well!\n# This mode will turn off the functions (on the Syncrio side) that both Syncrio and DarkMultiPlayer have.\n# This speeds up the KSP and the Syncrio server by removing the duplicate functions.")]
        public bool DarkMultiPlayerCoopMode = false;
        [Description("Specify if groups with public progress are to show all progress or just the progress count.")]
        public bool showAllGroupProgress = false;
        [Description("Minimum % of kick votes that has to be sent to kick a player from a group.\n# -Note! Any keep votes sent will lower the % of kick votes sent.\n# -Note! The leader of the group has ultimate power over the vote system and can instantly kick a player, but can't vote to keep the player and is excluded from the vote count.\n# -Note! Enter a number from 0 to 100, 100 is ok to enter, but 0 is not. Do not enter 0!\n# -Note! The number will be divided by 100.")]
        public int groupKickPlayerVotesThreshold = 75;
        [Description("Specify if players can have saved scenarios when they're not in a group.\n# -Note! If false, any player who is not in a group will not have their scenario saved.")]
        public bool nonGroupScenarios = true;
    }
}