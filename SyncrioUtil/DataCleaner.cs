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

namespace SyncrioUtil
{
    public class DataCleaner
    {
        public static List<string> CleanData(List<string> stringList)
        {
            Regex wordRegex = new Regex(@"^[\w_]+$", RegexOptions.None);// matches a single word on a line by itself
            
            List<string> lines = new List<string>(stringList);

            List<string> result = new List<string>();

            switch (lines[0])
            {
                case "name = ContractSystem":
                    {
                        if (lines.Count > 2)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                            result.Add(lines[2]);
                            result.Add(lines[3]);
                        }
                    }
                    break;
                case "name = Funding":
                    {
                        if (lines.Count > 1)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                            result.Add(lines[2]);
                        }
                    }
                    break;
                case "name = PartUpgradeManager":
                    {
                        if (lines.Count > 0)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                        }
                    }
                    break;
                case "name = ProgressTracking":
                    {
                        if (lines.Count > 0)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                        }
                    }
                    break;
                case "name = Reputation":
                    {
                        if (lines.Count > 1)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                            result.Add(lines[2]);
                        }
                    }
                    break;
                case "name = ResearchAndDevelopment":
                    {
                        if (lines.Count > 1)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                            result.Add(lines[2]);
                        }
                    }
                    break;
                case "name = ResourceScenario":
                    {
                        if (lines.Count > 0)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                        }
                    }
                    break;
                case "name = ScenarioCustomWaypoints":
                    {
                        if (lines.Count > 0)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                        }
                    }
                    break;
                case "name = ScenarioDestructibles":
                    {
                        if (lines.Count > 0)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                        }
                    }
                    break;
                case "name = ScenarioUpgradeableFacilities":
                    {
                        if (lines.Count > 0)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                        }
                    }
                    break;
                case "name = StrategySystem":
                    {
                        if (lines.Count > 0)
                        {
                            result.Add(lines[0]);
                            result.Add(lines[1]);
                        }
                    }
                    break;
                default:
                    {
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
                    int matchBracketIdx = FindMatchingBracket(lines, cursor + 1);
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
            if (stringList == null || stringList.Count == 0)
            {
                return false;
            }

            Regex wordRegex = new Regex(@"^[\w_]+", RegexOptions.None);// matches a single word on a line by itself

            List<string> lines = BasicClean(stringList);

            switch (lines[0])
            {
                case "name = ContractSystem":
                    {
                        if (lines.Count > 2)
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
                        else
                        {
                            return false;
                        }
                    }
                case "name = Funding":
                    {
                        if (lines.Count > 1)
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
                        else
                        {
                            return false;
                        }
                    }
                case "name = PartUpgradeManager":
                    {
                        if (lines.Count > 0)
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
                        else
                        {
                            return false;
                        }
                    }
                case "name = ProgressTracking":
                    {
                        if (lines.Count > 0)
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
                        else
                        {
                            return false;
                        }
                    }
                case "name = Reputation":
                    {
                        if (lines.Count > 1)
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
                        else
                        {
                            return false;
                        }
                    }
                case "name = ResearchAndDevelopment":
                    {
                        if (lines.Count > 1)
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
                        else
                        {
                            return false;
                        }
                    }
                case "name = ResourceScenario":
                    {
                        if (lines.Count > 0)
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
                        else
                        {
                            return false;
                        }
                    }
                case "name = ScenarioCustomWaypoints":
                    {
                        if (lines.Count > 0)
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
                        else
                        {
                            return false;
                        }
                    }
                case "name = ScenarioDestructibles":
                    {
                        if (lines.Count > 0)
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
                        else
                        {
                            return false;
                        }
                    }
                case "name = ScenarioUpgradeableFacilities":
                    {
                        if (lines.Count > 0)
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
                        else
                        {
                            return false;
                        }
                    }
                case "name = StrategySystem":
                    {
                        if (lines.Count > 0)
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

        public static List<string> BasicClean(List<string> stringList)
        {
            if (stringList == null || stringList.Count == 0)
            {
                return new List<string>();
            }

            //Copy the text into a new lines var and trim each line
            List<string> lines = new List<string>(stringList);

            for (int i = 0; i < lines.Count(); i++)
            {
                lines[i] = lines[i].Trim();
            }

            //Remove comment lines
            lines.RemoveAll(l => l.StartsWith("//"));

            return lines;
        }

        public static int FindMatchingBracket(List<string> lines, int startFrom)
        {
            int brackets = 0;
            for (int i = startFrom; i < lines.Count; i++)
            {
                if (lines[i].Trim() == "{") brackets++;
                if (lines[i].Trim() == "}") brackets--;

                if (brackets == 0)
                    return i;
            }

            throw new ArgumentOutOfRangeException("Could not find a matching bracket!");
        }
    }
}
