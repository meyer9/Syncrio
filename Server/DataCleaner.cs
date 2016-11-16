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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SyncrioServer
{
    class DataCleaner
    {
        public static List<string> CleanData(List<string> stringList)
        {
            Regex wordRegex = new Regex(@"^[\w_]+$", RegexOptions.None);// matches a single word on a line by itself

            // Split the text in lines and trim each line
            List<string> lines = new List<string>(stringList);

            for (int i = 0; i < lines.Count(); i++)
            {
                lines[i] = lines[i].Trim();
            }

            // Remove comment lines
            lines.RemoveAll(l => l.StartsWith("//"));

            List<string> result = new List<string>();

            switch (lines[0])
            {
                case "name = CommNetScenario":
                    {
                        if (lines.Count > 1)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                        }
                        else
                        {
                            SyncrioLog.Debug("'CommNetScenario' Scenario Data is bad!");
                        }
                    }
                    break;
                case "name = ContractSystem":
                    {
                        if (lines.Count > 3)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                            result.Add(lines[2]);
                            result.Add(lines[3]);
                        }
                        else
                        {
                            SyncrioLog.Debug("'ContractSystem' Scenario Data is bad!");
                        }
                    }
                    break;
                case "name = Funding":
                    {
                        if (lines.Count > 2)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                            result.Add(lines[2]);
                        }
                        else
                        {
                            SyncrioLog.Debug("'Funding' Scenario Data is bad!");
                        }
                    }
                    break;
                case "name = PartUpgradeManager":
                    {
                        if (lines.Count > 1)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                        }
                        else
                        {
                            SyncrioLog.Debug("'PartUpgradeManager' Scenario Data is bad!");
                        }
                    }
                    break;
                case "name = ProgressTracking":
                    {
                        if (lines.Count > 1)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                        }
                        else
                        {
                            SyncrioLog.Debug("'ProgressTracking' Scenario Data is bad!");
                        }
                    }
                    break;
                case "name = Reputation":
                    {
                        if (lines.Count > 2)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                            result.Add(lines[2]);
                        }
                        else
                        {
                            SyncrioLog.Debug("'Reputation' Scenario Data is bad!");
                        }
                    }
                    break;
                case "name = ResearchAndDevelopment":
                    {
                        if (lines.Count > 2)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                            result.Add(lines[2]);
                        }
                        else
                        {
                            SyncrioLog.Debug("'ResearchAndDevelopment' Scenario Data is bad!");
                        }
                    }
                    break;
                case "name = ResourceScenario":
                    {
                        if (lines.Count > 1)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                        }
                        else
                        {
                            SyncrioLog.Debug("'ResourceScenario' Scenario Data is bad!");
                        }
                    }
                    break;
                case "name = ScenarioAchievements":
                    {
                        if (lines.Count > 1)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                        }
                        else
                        {
                            SyncrioLog.Debug("'ScenarioAchievements' Scenario Data is bad!");
                        }
                    }
                    break;
                case "name = ScenarioContractEvents":
                    {
                        if (lines.Count > 2)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                            result.Add(lines[2]);
                        }
                        else
                        {
                            SyncrioLog.Debug("'ScenarioContractEvents' Scenario Data is bad!");
                        }
                    }
                    break;
                case "name = ScenarioCustomWaypoints":
                    {
                        if (lines.Count > 1)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                        }
                        else
                        {
                            SyncrioLog.Debug("'ScenarioCustomWaypoints' Scenario Data is bad!");
                        }
                    }
                    break;
                case "name = ScenarioDestructibles":
                    {
                        if (lines.Count > 1)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                        }
                        else
                        {
                            SyncrioLog.Debug("'ScenarioDestructibles' Scenario Data is bad!");
                        }
                    }
                    break;
                case "name = ScenarioDiscoverableObjects":
                    {
                        if (lines.Count > 2)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                            result.Add(lines[2]);
                        }
                        else
                        {
                            SyncrioLog.Debug("'ScenarioDiscoverableObjects' Scenario Data is bad!");
                        }
                    }
                    break;
                case "name = ScenarioNewGameIntro":
                    {
                        if (lines.Count > 4)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                            result.Add(lines[2]);
                            result.Add(lines[3]);
                            result.Add(lines[4]);
                        }
                        else
                        {
                            SyncrioLog.Debug("'ScenarioNewGameIntro' Scenario Data is bad!");
                        }
                    }
                    break;
                case "name = ScenarioUpgradeableFacilities":
                    {
                        if (lines.Count > 1)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                        }
                        else
                        {
                            SyncrioLog.Debug("'ScenarioUpgradeableFacilities' Scenario Data is bad!");
                        }
                    }
                    break;
                case "name = StrategySystem":
                    {
                        if (lines.Count > 1)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                        }
                        else
                        {
                            SyncrioLog.Debug("'StrategySystem' Scenario Data is bad!");
                        }
                    }
                    break;
                case "name = VesselRecovery":
                    {
                        if (lines.Count > 1)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                        }
                        else
                        {
                            SyncrioLog.Debug("'VesselRecovery' Scenario Data is bad!");
                        }
                    }
                    break;
                default:
                    {
                        SyncrioLog.Debug("Unknowen Scenario Data Type! Returning the whole list.");
                        return stringList;
                    }
            }

            int cursor = 2;//KEEP ME AT 2!!! Or you Will get a "stack overflow exception"!
            while (cursor < lines.Count())
            {
                // Find a single word with a single "{" on the next line
                // e.g: "PART \n {"
                // this should be the opening of a new child string
                if (wordRegex.IsMatch(lines[cursor]) && (lines[cursor + 1] == "{"))
                {
                    int matchBracketIdx = UnDuplicater.FindMatchingBracket(lines, cursor + 1);
                    KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));
                    result.AddRange(lines.GetRange(range.Key, range.Value));
                    lines.RemoveRange(range.Key, range.Value);
                }
                else
                {
                    // Only increment if a string was not removed
                    cursor++;
                }
            }

            return result;
        }

        public static bool IsDataGood(List<string> stringList)
        {
            if (stringList == null)
            {
                return false;
            }

            Regex wordRegex = new Regex(@"^[\w_]+$", RegexOptions.None);// matches a single word on a line by itself

            // Split the text in lines and trim each line
            List<string> lines = new List<string>(stringList);

            for (int i = 0; i < lines.Count(); i++)
            {
                lines[i] = lines[i].Trim();
            }

            // Remove comment lines
            lines.RemoveAll(l => l.StartsWith("//"));

            switch (lines[0])
            {
                case "name = CommNetScenario":
                    {
                        if (lines.Count > 1)
                        {
                            if (!wordRegex.IsMatch(lines[1]))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ContractSystem":
                    {
                        if (lines.Count > 3)
                        {
                            if (!wordRegex.IsMatch(lines[1]) && !wordRegex.IsMatch(lines[2]) && !wordRegex.IsMatch(lines[3]))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = Funding":
                    {
                        if (lines.Count > 2)
                        {
                            if (!wordRegex.IsMatch(lines[1]) && !wordRegex.IsMatch(lines[2]))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = PartUpgradeManager":
                    {
                        if (lines.Count > 1)
                        {
                            if (!wordRegex.IsMatch(lines[1]))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ProgressTracking":
                    {
                        if (lines.Count > 1)
                        {
                            if (!wordRegex.IsMatch(lines[1]))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = Reputation":
                    {
                        if (lines.Count > 2)
                        {
                            if (!wordRegex.IsMatch(lines[1]) && !wordRegex.IsMatch(lines[2]))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ResearchAndDevelopment":
                    {
                        if (lines.Count > 2)
                        {
                            if (!wordRegex.IsMatch(lines[1]) && !wordRegex.IsMatch(lines[2]))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ResourceScenario":
                    {
                        if (lines.Count > 1)
                        {
                            if (!wordRegex.IsMatch(lines[1]))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ScenarioAchievements":
                    {
                        if (lines.Count > 1)
                        {
                            if (!wordRegex.IsMatch(lines[1]))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ScenarioContractEvents":
                    {
                        if (lines.Count > 2)
                        {
                            if (!wordRegex.IsMatch(lines[1]) && !wordRegex.IsMatch(lines[2]))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ScenarioCustomWaypoints":
                    {
                        if (lines.Count > 1)
                        {
                            if (!wordRegex.IsMatch(lines[1]))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ScenarioDestructibles":
                    {
                        if (lines.Count > 1)
                        {
                            if (!wordRegex.IsMatch(lines[1]))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ScenarioDiscoverableObjects":
                    {
                        if (lines.Count > 2)
                        {
                            if (!wordRegex.IsMatch(lines[1]) && !wordRegex.IsMatch(lines[2]))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ScenarioNewGameIntro":
                    {
                        if (lines.Count > 4)
                        {
                            if (!wordRegex.IsMatch(lines[1]) && !wordRegex.IsMatch(lines[2]) && !wordRegex.IsMatch(lines[3]) && !wordRegex.IsMatch(lines[4]))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ScenarioUpgradeableFacilities":
                    {
                        if (lines.Count > 1)
                        {
                            if (!wordRegex.IsMatch(lines[1]))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = StrategySystem":
                    {
                        if (lines.Count > 1)
                        {
                            if (!wordRegex.IsMatch(lines[1]))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = VesselRecovery":
                    {
                        if (lines.Count > 1)
                        {
                            if (!wordRegex.IsMatch(lines[1]))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                default:
                    {
                        SyncrioLog.Debug("Unknowen Scenario Data Type!");
                        return false;
                    }
            }
        }
    }
}
