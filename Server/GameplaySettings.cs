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
    public class GameplaySettings
    {
        private static ConfigParser<GameplaySettingsStore> gameplaySettings;
        public static GameplaySettingsStore settingsStore
        {
            get
            {
                if (gameplaySettings == null)
                {
                    return null;
                }
                return gameplaySettings.Settings;
            }
        }

        public static void Reset()
        {
            gameplaySettings = new ConfigParser<GameplaySettingsStore>(new GameplaySettingsStore(), Path.Combine(Server.configDirectory, "GameplaySettings.txt"));
        }

        public static void Load()
        {
            gameplaySettings.LoadSettings();
        }

        public static void Save()
        {
            gameplaySettings.SaveSettings();
        }
    }

    public class GameplaySettingsStore
    {
        // General Options
        [Description("Allow Stock Vessels")]
        public bool allowStockVessels = false;
        [Description("Auto-Hire Crewmemebers before Flight")]
        public bool autoHireCrews = true;
        [Description("No Entry Purchase Required on Research")]
        public bool bypassEntryPurchaseAfterResearch = true;
        [Description("Indestructible Facilities")]
        public bool indestructibleFacilities = false;
        [Description("Missing Crews Respawn")]
        public bool missingCrewsRespawn = true;
        [Description("Re-Entry Heating")]
        public float reentryHeatScale = 1.0f;
        [Description("Resource Abundance")]
        public float resourceAbundance = 1.0f;
        [Description("Allow Quickloading and Reverting Flights\nNote that if set to true and warp mode isn't SUBSPACE, it will have no effect")]
        public bool canQuickLoad = true;
        [Description("Enable Comm Network")]
        public bool commNetwork = true;
        [Description("Crew Respawn Time")]
        public float respawnTime = 2f;
        // Career Options
        [Description("Funds Rewards")]
        public float fundsGainMultiplier = 1.0f;
        [Description("Funds Penalties")]
        public float fundsLossMultiplier = 1.0f;
        [Description("Reputation Rewards")]
        public float repGainMultiplier = 1.0f;
        [Description("Reputation Penalties")]
        public float repLossMultiplier = 1.0f;
        [Description("Decline Penalty")]
        public float repLossDeclined = 1.0f;
        [Description("Science Rewards")]
        public float scienceGainMultiplier = 1.0f;
        [Description("Starting Funds")]
        public float startingFunds = 25000.0f;
        [Description("Starting Reputation")]
        public float startingReputation = 0.0f;
        [Description("Starting Science")]
        public float startingScience = 0.0f;
        // Advanced Options
        [Description("Enable Kerbal Exp")]
        public bool kerbalExp = true;
        [Description("Kerbals Level Up Immediately")]
        public bool immediateLevelUp = false;
        [Description("Allow Negative Currency")]
        public bool allowNegativeCurrency = false;
        [Description("Part Pressure Limits")]
        public bool partPressureLimit = false;
        [Description("Part G-Force Limits")]
        public bool partGForceLimit = false;
        [Description("Kerbal G-Force Limits")]
        public bool kerbalGForceLimit = false;
        [Description("Kerbal G-Force Tolerance")]
        public float kerbalGForceTolerance = 1.0f;
        [Description("Obey Crossfeed Rules")]
        public bool obeyCrossfeedRules = false;
        [Description("Always Allow Action Groups")]
        public bool alwaysAllowActionGroups = false;
        [Description("Building Damage Multiplier")]
        public float buildingDamageMultiplier = 0.05f;
        [Description("Part Upgrades")]
        public bool partUpgrades = true;
        // CommNet Options
        [Description("Require Signal for Control")]
        public bool requireSignalForControl = false;
        [Description("Plasma Blackout")]
        public bool plasmaBlackout = false;
        [Description("Range Modifier")]
        public float rangeModifier = 1.0f;
        [Description("DSN Modifier")]
        public float dsnModifier = 1.0f;
        [Description("Occlusion Modifier, Vac")]
        public float occlusionModifierVac = 0.9f;
        [Description("Occlusion Modifier, Atm")]
        public float occlusionModifierAtm = 0.75f;
        [Description("Enable Extra Groundstations")]
        public bool extraGroundstations = true;
    }
}
