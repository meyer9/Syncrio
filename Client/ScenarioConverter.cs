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
using SyncrioCommon;

namespace SyncrioClientSide
{
    public class ScenarioConverter
    {
        private static string savesFolder = Path.Combine(KSPUtil.ApplicationRootPath, "saves");

        public static void GenerateScenario(string saveName)
        {
            string ScenarioFolder = Path.Combine(KSPUtil.ApplicationRootPath, "Generated Scenario");
            if (Directory.Exists(ScenarioFolder))
            {
                Directory.Delete(ScenarioFolder, true);
            }

            string saveFolder = Path.Combine(savesFolder, saveName);
            if (!Directory.Exists(saveFolder))
            {
                SyncrioLog.Debug("Failed to generate a Syncrio Scenario for '" + saveName + "', Save directory doesn't exist");
                ScreenMessages.PostScreenMessage("Failed to generate a Syncrio Scenario for '" + saveName + "', Save directory doesn't exist", 5f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            string persistentFile = Path.Combine(saveFolder, "persistent.sfs");
            if (!File.Exists(persistentFile))
            {
                SyncrioLog.Debug("Failed to generate a Syncrio Scenario for '" + saveName + "', persistent.sfs doesn't exist");
                ScreenMessages.PostScreenMessage("Failed to generate a Syncrio Scenario for '" + saveName + "', persistent.sfs doesn't exist", 5f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            Directory.CreateDirectory(ScenarioFolder);
            string vesselFolder = Path.Combine(ScenarioFolder, "Vessels");
            Directory.CreateDirectory(vesselFolder);
            string playersFolder = Path.Combine(ScenarioFolder, "Players");
            Directory.CreateDirectory(playersFolder);
            string playerScenarioFolder = Path.Combine(playersFolder, Settings.fetch.playerName);
            Directory.CreateDirectory(playerScenarioFolder);
            string kerbalFolder = Path.Combine(ScenarioFolder, "Kerbals");
            Directory.CreateDirectory(kerbalFolder);

            //Load game data
            ConfigNode persistentData = ConfigNode.Load(persistentFile);
            if (persistentData == null)
            {
                SyncrioLog.Debug("Failed to generate a Syncrio Scenario for '" + saveName + "', failed to load persistent data");
                ScreenMessages.PostScreenMessage("Failed to generate a Syncrio Scenario for '" + saveName + "', failed to load persistent data", 5f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            ConfigNode gameData = persistentData.GetNode("GAME");
            if (gameData == null)
            {
                SyncrioLog.Debug("Failed to generate a Syncrio Scenario for '" + saveName + "', failed to load game data");
                ScreenMessages.PostScreenMessage("Failed to generate a Syncrio Scenario for '" + saveName + "', failed to load game data", 5f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            //Save vessels
            ConfigNode flightState = gameData.GetNode("FLIGHTSTATE");
            if (flightState == null)
            {
                SyncrioLog.Debug("Failed to generate a Syncrio Scenario for '" + saveName + "', failed to load flight state data");
                ScreenMessages.PostScreenMessage("Failed to generate a Syncrio Scenario for '" + saveName + "', failed to load flight state data", 5f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            ConfigNode[] vesselNodes = flightState.GetNodes("VESSEL");
            if (vesselNodes != null)
            {
                foreach (ConfigNode cn in vesselNodes)
                {
                    string vesselID = Common.ConvertConfigStringToGUIDString(cn.GetValue("pid"));
                    SyncrioLog.Debug("Saving vessel " + vesselID + ", name: " + cn.GetValue("name"));
                    cn.Save(Path.Combine(vesselFolder, vesselID + ".txt"));
                }
            }
            //Save scenario data
            ConfigNode[] scenarioNodes = gameData.GetNodes("SCENARIO");
            if (scenarioNodes != null)
            {
                foreach (ConfigNode cn in scenarioNodes)
                {
                    string scenarioName = cn.GetValue("name");
                    SyncrioLog.Debug("Saving scenario: " + scenarioName);
                    cn.Save(Path.Combine(playerScenarioFolder, scenarioName + ".txt"));
                }
            }
            //Save kerbal data
            ConfigNode[] kerbalNodes = gameData.GetNode("ROSTER").GetNodes("CREW");
            if (kerbalNodes != null)
            {
                int kerbalIndex = 0;
                foreach (ConfigNode cn in kerbalNodes)
                {
                    SyncrioLog.Debug("Saving kerbal " + kerbalIndex + ", name: " + cn.GetValue("name"));
                    cn.Save(Path.Combine(kerbalFolder, kerbalIndex + ".txt"));
                    kerbalIndex++;
                }
            }
            SyncrioLog.Debug("Generated KSP_folder/Scenario from " + saveName);
            ScreenMessages.PostScreenMessage("Generated KSP_folder/Scenario from " + saveName, 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        public static string[] GetSavedNames()
        {
            List<string> returnList = new List<string>();
            string[] possibleSaves = Directory.GetDirectories(savesFolder);
            foreach (string saveDirectory in possibleSaves)
            {
                string trimmedDirectory = saveDirectory;
                //Cut the trailing path character off if we need to
                if (saveDirectory[saveDirectory.Length - 1] == Path.DirectorySeparatorChar)
                {
                    trimmedDirectory = saveDirectory.Substring(0, saveDirectory.Length - 2);
                }
                string saveName = trimmedDirectory.Substring(trimmedDirectory.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                if (saveName.ToLower() != "training" && saveName.ToLower() != "scenarios")
                {
                    if (File.Exists(Path.Combine(saveDirectory, "persistent.sfs")))
                    {
                        returnList.Add(saveName);
                    }
                }
            }
            return returnList.ToArray();
        }
    }
}

