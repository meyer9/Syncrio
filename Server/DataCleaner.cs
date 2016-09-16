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
                        result.Add(lines[0]);
                        result.Add(lines[1]);
                    }
                    break;
                case "name = ContractSystem":
                    {
                        result.Add(lines[0]);
                        result.Add(lines[1]);
                        result.Add(lines[2]);
                        result.Add(lines[3]);
                    }
                    break;
                case "name = Funding":
                    {
                        result.Add(lines[0]);
                        result.Add(lines[1]);
                        result.Add(lines[2]);
                    }
                    break;
                case "name = PartUpgradeManager":
                    {
                        result.Add(lines[0]);
                        result.Add(lines[1]);
                    }
                    break;
                case "name = ProgressTracking":
                    {
                        result.Add(lines[0]);
                        result.Add(lines[1]);
                    }
                    break;
                case "name = Reputation":
                    {
                        result.Add(lines[0]);
                        result.Add(lines[1]);
                        result.Add(lines[2]);
                    }
                    break;
                case "name = ResearchAndDevelopment":
                    {
                        result.Add(lines[0]);
                        result.Add(lines[1]);
                        result.Add(lines[2]);
                    }
                    break;
                case "name = ResourceScenario":
                    {
                        result.Add(lines[0]);
                        result.Add(lines[1]);
                    }
                    break;
                case "name = ScenarioAchievements":
                    {
                        result.Add(lines[0]);
                        result.Add(lines[1]);
                    }
                    break;
                case "name = ScenarioContractEvents":
                    {
                        result.Add(lines[0]);
                        result.Add(lines[1]);
                        result.Add(lines[2]);
                    }
                    break;
                case "name = ScenarioCustomWaypoints":
                    {
                        result.Add(lines[0]);
                        result.Add(lines[1]);
                    }
                    break;
                case "name = ScenarioDestructibles":
                    {
                        result.Add(lines[0]);
                        result.Add(lines[1]);
                    }
                    break;
                case "name = ScenarioNewGameIntro":
                    {
                        result.Add(lines[0]);
                        result.Add(lines[1]);
                        result.Add(lines[2]);
                        result.Add(lines[3]);
                        result.Add(lines[4]);
                    }
                    break;
                case "name = ScenarioUpgradeableFacilities":
                    {
                        result.Add(lines[0]);
                        result.Add(lines[1]);
                    }
                    break;
                case "name = StrategySystem":
                    {
                        result.Add(lines[0]);
                        result.Add(lines[1]);
                    }
                    break;
                case "name = VesselRecovery":
                    {
                        result.Add(lines[0]);
                        result.Add(lines[1]);
                    }
                    break;
                default:
                    {
                        SyncrioLog.Debug("Unknowen Scenario Data Type!");
                    }
                    break;
            }

            int cursor = 2;
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
                        if (wordRegex.IsMatch(lines[1]) && wordRegex.IsMatch(lines[2]))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ContractSystem":
                    {
                        if (wordRegex.IsMatch(lines[1]) && wordRegex.IsMatch(lines[2]) && wordRegex.IsMatch(lines[3]))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = Funding":
                    {
                        if (wordRegex.IsMatch(lines[1]) && wordRegex.IsMatch(lines[2]))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = PartUpgradeManager":
                    {
                        if (wordRegex.IsMatch(lines[1]) && wordRegex.IsMatch(lines[2]))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ProgressTracking":
                    {
                        if (wordRegex.IsMatch(lines[1]))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = Reputation":
                    {
                        if (wordRegex.IsMatch(lines[1]) && wordRegex.IsMatch(lines[2]))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ResearchAndDevelopment":
                    {
                        if (wordRegex.IsMatch(lines[1]) && wordRegex.IsMatch(lines[2]))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ResourceScenario":
                    {
                        if (wordRegex.IsMatch(lines[1]))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ScenarioAchievements":
                    {
                        if (wordRegex.IsMatch(lines[1]))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ScenarioContractEvents":
                    {
                        if (wordRegex.IsMatch(lines[1]) && wordRegex.IsMatch(lines[2]))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ScenarioCustomWaypoints":
                    {
                        if (wordRegex.IsMatch(lines[1]) && wordRegex.IsMatch(lines[2]))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ScenarioDestructibles":
                    {
                        if (wordRegex.IsMatch(lines[1]))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ScenarioNewGameIntro":
                    {
                        if (wordRegex.IsMatch(lines[1]) && wordRegex.IsMatch(lines[2]) && wordRegex.IsMatch(lines[3]) && wordRegex.IsMatch(lines[4]))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = ScenarioUpgradeableFacilities":
                    {
                        if (wordRegex.IsMatch(lines[1]))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = StrategySystem":
                    {
                        if (wordRegex.IsMatch(lines[1]))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                case "name = VesselRecovery":
                    {
                        if (wordRegex.IsMatch(lines[1]))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                default:
                    {
                        return false;
                    }
            }
        }
    }
}
