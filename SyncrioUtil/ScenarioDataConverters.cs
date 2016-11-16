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
    public class ScenarioDataConverters
    {
        /// <summary>
        /// Converts the the given data and imputs it in to the given scenario data type, then returns it.
        /// </summary>
        public static ScenarioDataTypes.ContractSystem ConvertToScenarioDataType(List<string> inputData, ScenarioDataTypes.ContractSystem outputScenario)
        {
            List<string> data = DataCleaner.BasicClean(inputData);
            List<string> headerLines = new List<string>();
            List<string> weightsLines = new List<string>();
            List<string> contractsToHandle = new List<string>();

            if (DataCleaner.IsDataGood(data))
            {
                headerLines.Add(data[0]);
                headerLines.Add(data[1]);
                headerLines.Add(data[2]);
                headerLines.Add(data[3]);

                int cursor = 4;//Yes this starts at 4
                while (cursor < data.Count())
                {
                    bool increment = true;

                    if (data[cursor] == "WEIGHTS" && (data[cursor + 1] == "{"))
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            weightsLines = data.GetRange(range.Key + 2, range.Value - 3);//Use Key + 2 and Value - 3 because that way you will only get the inside of the node.
                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "CONTRACTS" && (data[cursor + 1] == "{"))
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> rangeToRemove = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (rangeToRemove.Key + 2 < data.Count && rangeToRemove.Value - 3 > 0)
                        {
                            KeyValuePair<int, int> childRange = new KeyValuePair<int, int>(cursor + 2, (matchBracketIdx - cursor - 2));

                            List<string> childStringLines = data.GetRange(childRange.Key, childRange.Value);
                            data.RemoveRange(rangeToRemove.Key, rangeToRemove.Value);

                            contractsToHandle.AddRange(childStringLines);
                        }
                        else
                        {
                            data.RemoveRange(rangeToRemove.Key, rangeToRemove.Value);
                        }
                    }

                    if (increment)
                    {
                        cursor++;
                    }
                }

                List<ScenarioDataTypes.Contract>[] handledContracts = ConvertToContracts(contractsToHandle);

                outputScenario.header = headerLines;
                outputScenario.weights = weightsLines;
                outputScenario.contracts = handledContracts[0];
                outputScenario.finishedContracts = handledContracts[1];
            }

            return outputScenario;
        }
        /// <summary>
        /// Converts the the given string list into a ScenarioDataTypes.Contract list.
        /// </summary>
        private static List<ScenarioDataTypes.Contract>[] ConvertToContracts(List<string> inputString)
        {
            List<string> data = inputString;
            List<ScenarioDataTypes.Contract> contractsList = new List<ScenarioDataTypes.Contract>();
            List<ScenarioDataTypes.Contract> finishedContractsList = new List<ScenarioDataTypes.Contract>();

            int cursor = 0;
            while (cursor < data.Count())
            {
                bool increment = true;

                if (data[cursor] == "CONTRACT" && data[cursor + 1] == "{" && data[cursor + 2].StartsWith("guid"))
                {
                    increment = false;

                    int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                    KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                    List<string> contractStringLines = data.GetRange(range.Key, range.Value);

                    string contractGUID = data[cursor + 2];
                    List<int> usedNodeNumbers = new List<int>();

                    List<string> contractLinesToAdd = new List<string>();

                    List<ScenarioDataTypes.Param> parametersToAdd = new List<ScenarioDataTypes.Param>();

                    int cursorTwo = 3;//Yes this starts at three
                    while (cursorTwo < contractStringLines.Count - 1)
                    {
                        if (contractStringLines[cursorTwo] == "PARAM" && contractStringLines[cursorTwo + 1] == "{")
                        {
                            int paramMatchBracketIdx = DataCleaner.FindMatchingBracket(contractStringLines, cursorTwo + 1);
                            KeyValuePair<int, int> paramRange = new KeyValuePair<int, int>(cursorTwo, (paramMatchBracketIdx - cursorTwo + 1));

                            if (paramRange.Key + 2 < contractStringLines.Count && paramRange.Value - 3 > 0)
                            {
                                KeyValuePair<List<int>, ScenarioDataTypes.Param> paramKVP = ConvertToParam(contractStringLines.GetRange(paramRange.Key + 2, paramRange.Value - 3), usedNodeNumbers);

                                usedNodeNumbers = paramKVP.Key;

                                ScenarioDataTypes.Param newParam = paramKVP.Value;

                                parametersToAdd.Add(newParam);

                                contractStringLines.RemoveRange(paramRange.Key, paramRange.Value);
                            }
                            else
                            {
                                contractStringLines.RemoveRange(paramRange.Key, paramRange.Value);
                            }
                        }
                        else
                        {
                            contractLinesToAdd.Add(contractStringLines[cursorTwo]);
                            cursorTwo++;
                        }
                    }

                    ScenarioDataTypes.Contract contractToAdd = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Contract());

                    contractToAdd.guid = contractGUID;
                    contractToAdd.contractDataLines = contractLinesToAdd;
                    contractToAdd.usedNodeNumbers = usedNodeNumbers;
                    contractToAdd.parameters = parametersToAdd;

                    contractsList.Add(contractToAdd);

                    data.RemoveRange(range.Key, range.Value);
                }

                if (cursor > data.Count() - 1)
                {
                    break;
                }
                
                if (data[cursor] == "CONTRACT_FINISHED" && data[cursor + 1] == "{" && data[cursor + 2].StartsWith("guid"))
                {
                    increment = false;

                    int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                    KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                    List<string> contractStringLines = data.GetRange(range.Key, range.Value);

                    string contractGUID = data[cursor + 2];
                    List<int> usedNodeNumbers = new List<int>();

                    List<string> contractLinesToAdd = new List<string>();

                    List<ScenarioDataTypes.Param> parametersToAdd = new List<ScenarioDataTypes.Param>();

                    int cursorTwo = 3;//Yes this starts at three
                    while (cursorTwo < contractStringLines.Count - 1)
                    {
                        if (contractStringLines[cursorTwo] == "PARAM" && contractStringLines[cursorTwo + 1] == "{")
                        {
                            int paramMatchBracketIdx = DataCleaner.FindMatchingBracket(contractStringLines, cursorTwo + 1);
                            KeyValuePair<int, int> paramRange = new KeyValuePair<int, int>(cursorTwo, (paramMatchBracketIdx - cursorTwo + 1));

                            if (paramRange.Key + 2 < contractStringLines.Count && paramRange.Value - 3 > 0)
                            {
                                KeyValuePair<List<int>, ScenarioDataTypes.Param> paramKVP = ConvertToParam(contractStringLines.GetRange(paramRange.Key + 2, paramRange.Value - 3), usedNodeNumbers);

                                usedNodeNumbers = paramKVP.Key;

                                ScenarioDataTypes.Param newParam = paramKVP.Value;

                                parametersToAdd.Add(newParam);

                                contractStringLines.RemoveRange(paramRange.Key, paramRange.Value);
                            }
                            else
                            {
                                contractStringLines.RemoveRange(paramRange.Key, paramRange.Value);
                            }
                        }
                        else
                        {
                            contractLinesToAdd.Add(contractStringLines[cursorTwo]);
                            cursorTwo++;
                        }
                    }

                    ScenarioDataTypes.Contract contractToAdd = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Contract());

                    contractToAdd.guid = contractGUID;
                    contractToAdd.contractDataLines = contractLinesToAdd;
                    contractToAdd.usedNodeNumbers = usedNodeNumbers;
                    contractToAdd.parameters = parametersToAdd;

                    finishedContractsList.Add(contractToAdd);

                    data.RemoveRange(range.Key, range.Value);
                }

                if (increment)
                {
                    cursor++;
                }
            }

            List<ScenarioDataTypes.Contract>[] retrunArray = new List<ScenarioDataTypes.Contract>[2];//Yes this needs to be two

            retrunArray[0] = contractsList;
            retrunArray[1] = finishedContractsList;

            return retrunArray;
        }
        /// <summary>
        /// Converts the the given string list into a ScenarioDataTypes.Param.
        /// </summary>
        private static KeyValuePair<List<int>, ScenarioDataTypes.Param> ConvertToParam(List<string> inputString, List<int> usedNodeNumbers)
        {
            List<string> data = inputString;
            ScenarioDataTypes.Param newParam = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Param());
            List<string> newParamLines = new List<string>();
            List<ScenarioDataTypes.SubParam> subParamList = new List<ScenarioDataTypes.SubParam>();

            if (usedNodeNumbers.Count > 0)
            {
                int lastUsedNumber = usedNodeNumbers.Last();

                newParam.nodeNumber = lastUsedNumber + 1;

                usedNodeNumbers.Add(lastUsedNumber + 1);
            }
            else
            {
                newParam.nodeNumber = 0;

                usedNodeNumbers.Add(0);
            }

            int cursor = 0;
            while (cursor < data.Count())
            {
                if (data[cursor] == "PARAM" && data[cursor + 1] == "{")
                {
                    int paramMatchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                    KeyValuePair<int, int> paramRange = new KeyValuePair<int, int>(cursor, (paramMatchBracketIdx - cursor + 1));

                    if (paramRange.Key + 2 < data.Count && paramRange.Value - 3 > 0)
                    {
                        List<string> subParamLines = data.GetRange(paramRange.Key + 2, paramRange.Value - 3);

                        data.RemoveRange(paramRange.Key, paramRange.Value);

                        KeyValuePair<List<int>, List<ScenarioDataTypes.SubParam>> subKVP = ConvertToSubParam(subParamLines, usedNodeNumbers, newParam.nodeNumber);

                        usedNodeNumbers = subKVP.Key;

                        subParamList.AddRange(subKVP.Value);
                    }
                    else
                    {
                        data.RemoveRange(paramRange.Key, paramRange.Value);
                    }
                }
                else
                {
                    newParamLines.Add(data[cursor]);
                    cursor++;
                }
            }

            newParam.paramLines = newParamLines;
            newParam.subParameters = subParamList;

            KeyValuePair<List<int>, ScenarioDataTypes.Param> returnKVP = new KeyValuePair<List<int>, ScenarioDataTypes.Param>(usedNodeNumbers, newParam);

            return returnKVP;
        }
        /// <summary>
        /// Converts the the given string list into a ScenarioDataTypes.SubParam list.
        /// </summary>
        private static KeyValuePair<List<int>, List<ScenarioDataTypes.SubParam>> ConvertToSubParam(List<string> inputString, List<int> usedNodeNumbers, int parentNodeNumber)
        {
            List<string> data = inputString;
            List<ScenarioDataTypes.SubParam> subParamList = new List<ScenarioDataTypes.SubParam>();
            ScenarioDataTypes.SubParam newSubParam = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.SubParam());
            List<string> newSubParamLines = new List<string>();

            int lastUsedNumber = usedNodeNumbers.Last();

            newSubParam.nodeNumber = lastUsedNumber + 1;
            newSubParam.parentNodeNumber = parentNodeNumber;

            usedNodeNumbers.Add(lastUsedNumber + 1);

            int cursor = 0;
            while (cursor < data.Count())
            {
                if (data[cursor] == "PARAM" && data[cursor + 1] == "{")
                {
                    int paramMatchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                    KeyValuePair<int, int> paramRange = new KeyValuePair<int, int>(cursor, (paramMatchBracketIdx - cursor + 1));

                    if (paramRange.Key + 2 < data.Count && paramRange.Value - 3 > 0)
                    {
                        List<string> subParamLines = data.GetRange(paramRange.Key + 2, paramRange.Value - 3);

                        data.RemoveRange(paramRange.Key, paramRange.Value);

                        KeyValuePair<List<int>, List<ScenarioDataTypes.SubParam>> subKVP = ConvertToSubParam(subParamLines, usedNodeNumbers, newSubParam.nodeNumber);

                        usedNodeNumbers = subKVP.Key;

                        subParamList.AddRange(subKVP.Value);
                    }
                    else
                    {
                        data.RemoveRange(paramRange.Key, paramRange.Value);
                    }
                }
                else
                {
                    newSubParamLines.Add(data[cursor]);
                    cursor++;
                }
            }

            newSubParam.subParamLines = newSubParamLines;

            subParamList.Add(newSubParam);

            KeyValuePair<List<int>, List<ScenarioDataTypes.SubParam>> returnKVP = new KeyValuePair<List<int>, List<ScenarioDataTypes.SubParam>>(usedNodeNumbers, subParamList);

            return returnKVP;
        }
        /// <summary>
        /// Converts the the given data and imputs it in to the given scenario data type, then returns it.
        /// </summary>
        public static ScenarioDataTypes.Funding ConvertToScenarioDataType(List<string> inputData, ScenarioDataTypes.Funding outputScenario)
        {
            List<string> data = DataCleaner.BasicClean(inputData);
            List<string> headerLines = new List<string>();
            string fundsLine = "";

            if (DataCleaner.IsDataGood(data))
            {
                headerLines.Add(data[0]);
                headerLines.Add(data[1]);

                fundsLine = data[2];

                outputScenario.header = headerLines;
                outputScenario.fundsLine = fundsLine;
            }

            return outputScenario;
        }
        /// <summary>
        /// Converts the the given data and imputs it in to the given scenario data type, then returns it.
        /// </summary>
        public static ScenarioDataTypes.PartUpgradeManager ConvertToScenarioDataType(List<string> inputData, ScenarioDataTypes.PartUpgradeManager outputScenario)
        {
            List<string> data = DataCleaner.BasicClean(inputData);
            List<string> headerLines = new List<string>();
            List<string> unlocksLines = new List<string>();
            List<string> enabledsLines = new List<string>();

            if (DataCleaner.IsDataGood(data))
            {
                headerLines.Add(data[0]);
                headerLines.Add(data[1]);

                int cursor = 2;//Yes this starts at two
                while (cursor < data.Count)
                {
                    if (data[cursor] == "UPGRADES" && data[cursor + 1] == "{")
                    {
                        List<string> subData = new List<string>();

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        subData = data.GetRange(range.Key, range.Value);

                        bool unlocksLinesFound = false;
                        bool enabledsLinesFound = false;

                        int cursorTwo = 2;//Yes this starts at two
                        while (cursorTwo < subData.Count - 1)
                        {
                            bool increment = true;

                            if (subData[cursorTwo] == "Unlocks" && subData[cursorTwo + 1] == "{")
                            {
                                unlocksLinesFound = true;
                                increment = false;

                                int subMatchBracketIdx = DataCleaner.FindMatchingBracket(subData, cursorTwo + 1);
                                KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                if (subRange.Key + 2 < subData.Count && subRange.Value - 3 > 0)
                                {
                                    unlocksLines = subData.GetRange(subRange.Key + 2, subRange.Value - 3);

                                    subData.RemoveRange(subRange.Key, subRange.Value);
                                }
                                else
                                {
                                    subData.RemoveRange(subRange.Key, subRange.Value);
                                }
                            }

                            if (subData[cursorTwo] == "Enableds" && subData[cursorTwo + 1] == "{")
                            {
                                enabledsLinesFound = true;
                                increment = false;

                                int subMatchBracketIdx = DataCleaner.FindMatchingBracket(subData, cursorTwo + 1);
                                KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                if (subRange.Key + 2 < subData.Count && subRange.Value - 3 > 0)
                                {
                                    enabledsLines = subData.GetRange(subRange.Key + 2, subRange.Value - 3);

                                    subData.RemoveRange(subRange.Key, subRange.Value);
                                }
                                else
                                {
                                    subData.RemoveRange(subRange.Key, subRange.Value);
                                }
                            }

                            if (unlocksLinesFound && enabledsLinesFound)
                            {
                                break;
                            }

                            if (increment)
                            {
                                cursorTwo++;
                            }
                        }

                        data.RemoveRange(range.Key, range.Value);

                        break;
                    }
                    else
                    {
                        cursor++;
                    }
                }

                outputScenario.header = headerLines;
                outputScenario.upgrades.unlocks = unlocksLines;
                outputScenario.upgrades.enableds = enabledsLines;
            }

            return outputScenario;
        }
        /// <summary>
        /// Converts the the given data and imputs it in to the given scenario data type, then returns it.
        /// </summary>
        public static ScenarioDataTypes.ProgressTracking ConvertToScenarioDataType(List<string> inputData, ScenarioDataTypes.ProgressTracking outputScenario)
        {
            Regex wordRegex = new Regex(@"^[\w_]+$", RegexOptions.None);// matches a single word on a line by itself

            List<string> data = DataCleaner.BasicClean(inputData);
            List<string> headerLines = new List<string>();
            ScenarioDataTypes.BasicProgress basicProgress = outputScenario.basicProgress;
            List<ScenarioDataTypes.CelestialProgress> celestialProgressList = outputScenario.celestialProgress;
            ScenarioDataTypes.Secrets secrets = outputScenario.secrets;

            if (DataCleaner.IsDataGood(data))
            {
                headerLines.Add(data[0]);
                headerLines.Add(data[1]);

                int cursor = 4;//Yes this starts at four
                while (cursor < data.Count)
                {
                    bool increment = true;

                    //Basic Progress
                    if (data[cursor] == "FirstLaunch" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            basicProgress.firstLaunch = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "FirstCrewToSurvive" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            basicProgress.firstCrewToSurvive = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "ReachedSpace" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            basicProgress.reachSpace = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "RecordsAltitude" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            basicProgress.altitudeRecord = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "RecordsDepth" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            basicProgress.depthRecord = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "RecordsSpeed" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            basicProgress.speedRecord = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "RecordsDistance" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            basicProgress.distanceRecord = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "KSCLanding" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            basicProgress.KSCLanding = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "LaunchpadLanding" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            basicProgress.launchpadLanding = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "RunwayLanding" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            basicProgress.runwayLanding = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "TowerBuzz" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            basicProgress.towerBuzz = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    //Celestial Progress
                    if (wordRegex.IsMatch(data[cursor]) && data[cursor + 1] == "{" && data[cursor + 2].StartsWith("reached"))
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            List<string> newCelestialProgressLines = data.GetRange(range.Key + 2, range.Value - 3);

                            ScenarioDataTypes.CelestialProgress newCelestialProgress = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.CelestialProgress());

                            newCelestialProgress.celestialBody = data[cursor];
                            newCelestialProgress.reached = data[cursor + 2];

                            int cursorTwo = 1;//Yes this starts at two
                            while (cursorTwo < newCelestialProgressLines.Count)
                            {
                                bool subIncrement = true;

                                if (newCelestialProgressLines[cursorTwo] == "BaseConstruction" && newCelestialProgressLines[cursorTwo + 1] == "{")
                                {
                                    subIncrement = false;

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(newCelestialProgressLines, cursorTwo + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                    if (subRange.Key + 2 < newCelestialProgressLines.Count && subRange.Value - 3 > 0)
                                    {
                                        newCelestialProgress.baseConstruction = newCelestialProgressLines.GetRange(subRange.Key + 2, subRange.Value - 3);

                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                    else
                                    {
                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                }

                                if (cursorTwo > newCelestialProgressLines.Count() - 1)
                                {
                                    break;
                                }

                                if (newCelestialProgressLines[cursorTwo] == "CrewTransfer" && newCelestialProgressLines[cursorTwo + 1] == "{")
                                {
                                    subIncrement = false;

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(newCelestialProgressLines, cursorTwo + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                    if (subRange.Key + 2 < newCelestialProgressLines.Count && subRange.Value - 3 > 0)
                                    {
                                        newCelestialProgress.crewTransfer = newCelestialProgressLines.GetRange(subRange.Key + 2, subRange.Value - 3);

                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                    else
                                    {
                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                }

                                if (cursorTwo > newCelestialProgressLines.Count() - 1)
                                {
                                    break;
                                }

                                if (newCelestialProgressLines[cursorTwo] == "Docking" && newCelestialProgressLines[cursorTwo + 1] == "{")
                                {
                                    subIncrement = false;

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(newCelestialProgressLines, cursorTwo + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                    if (subRange.Key + 2 < newCelestialProgressLines.Count && subRange.Value - 3 > 0)
                                    {
                                        newCelestialProgress.docking = newCelestialProgressLines.GetRange(subRange.Key + 2, subRange.Value - 3);

                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                    else
                                    {
                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                }

                                if (cursorTwo > newCelestialProgressLines.Count() - 1)
                                {
                                    break;
                                }

                                if (newCelestialProgressLines[cursorTwo] == "Escape" && newCelestialProgressLines[cursorTwo + 1] == "{")
                                {
                                    subIncrement = false;

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(newCelestialProgressLines, cursorTwo + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                    if (subRange.Key + 2 < newCelestialProgressLines.Count && subRange.Value - 3 > 0)
                                    {
                                        newCelestialProgress.escape = newCelestialProgressLines.GetRange(subRange.Key + 2, subRange.Value - 3);

                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                    else
                                    {
                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                }

                                if (cursorTwo > newCelestialProgressLines.Count() - 1)
                                {
                                    break;
                                }

                                if (newCelestialProgressLines[cursorTwo] == "FlagPlant" && newCelestialProgressLines[cursorTwo + 1] == "{")
                                {
                                    subIncrement = false;

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(newCelestialProgressLines, cursorTwo + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                    if (subRange.Key + 2 < newCelestialProgressLines.Count && subRange.Value - 3 > 0)
                                    {
                                        newCelestialProgress.flagPlant = newCelestialProgressLines.GetRange(subRange.Key + 2, subRange.Value - 3);

                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                    else
                                    {
                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                }

                                if (cursorTwo > newCelestialProgressLines.Count() - 1)
                                {
                                    break;
                                }

                                if (newCelestialProgressLines[cursorTwo] == "Flight" && newCelestialProgressLines[cursorTwo + 1] == "{")
                                {
                                    subIncrement = false;

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(newCelestialProgressLines, cursorTwo + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                    if (subRange.Key + 2 < newCelestialProgressLines.Count && subRange.Value - 3 > 0)
                                    {
                                        newCelestialProgress.flight = newCelestialProgressLines.GetRange(subRange.Key + 2, subRange.Value - 3);

                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                    else
                                    {
                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                }

                                if (cursorTwo > newCelestialProgressLines.Count() - 1)
                                {
                                    break;
                                }

                                if (newCelestialProgressLines[cursorTwo] == "FlyBy" && newCelestialProgressLines[cursorTwo + 1] == "{")
                                {
                                    subIncrement = false;

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(newCelestialProgressLines, cursorTwo + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                    if (subRange.Key + 2 < newCelestialProgressLines.Count && subRange.Value - 3 > 0)
                                    {
                                        newCelestialProgress.flyBy = newCelestialProgressLines.GetRange(subRange.Key + 2, subRange.Value - 3);

                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                    else
                                    {
                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                }

                                if (cursorTwo > newCelestialProgressLines.Count() - 1)
                                {
                                    break;
                                }

                                if (newCelestialProgressLines[cursorTwo] == "Landing" && newCelestialProgressLines[cursorTwo + 1] == "{")
                                {
                                    subIncrement = false;

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(newCelestialProgressLines, cursorTwo + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                    if (subRange.Key + 2 < newCelestialProgressLines.Count && subRange.Value - 3 > 0)
                                    {
                                        newCelestialProgress.landing = newCelestialProgressLines.GetRange(subRange.Key + 2, subRange.Value - 3);

                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                    else
                                    {
                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                }

                                if (cursorTwo > newCelestialProgressLines.Count() - 1)
                                {
                                    break;
                                }

                                if (newCelestialProgressLines[cursorTwo] == "Orbit" && newCelestialProgressLines[cursorTwo + 1] == "{")
                                {
                                    subIncrement = false;

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(newCelestialProgressLines, cursorTwo + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                    if (subRange.Key + 2 < newCelestialProgressLines.Count && subRange.Value - 3 > 0)
                                    {
                                        newCelestialProgress.orbit = newCelestialProgressLines.GetRange(subRange.Key + 2, subRange.Value - 3);

                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                    else
                                    {
                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                }

                                if (cursorTwo > newCelestialProgressLines.Count() - 1)
                                {
                                    break;
                                }

                                if (newCelestialProgressLines[cursorTwo] == "Rendezvous" && newCelestialProgressLines[cursorTwo + 1] == "{")
                                {
                                    subIncrement = false;

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(newCelestialProgressLines, cursorTwo + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                    if (subRange.Key + 2 < newCelestialProgressLines.Count && subRange.Value - 3 > 0)
                                    {
                                        newCelestialProgress.rendezvous = newCelestialProgressLines.GetRange(subRange.Key + 2, subRange.Value - 3);

                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                    else
                                    {
                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                }

                                if (cursorTwo > newCelestialProgressLines.Count() - 1)
                                {
                                    break;
                                }

                                if (newCelestialProgressLines[cursorTwo] == "ReturnFromFlyby" && newCelestialProgressLines[cursorTwo + 1] == "{")
                                {
                                    subIncrement = false;

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(newCelestialProgressLines, cursorTwo + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                    if (subRange.Key + 2 < newCelestialProgressLines.Count && subRange.Value - 3 > 0)
                                    {
                                        newCelestialProgress.returnFromFlyby = newCelestialProgressLines.GetRange(subRange.Key + 2, subRange.Value - 3);

                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                    else
                                    {
                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                }

                                if (cursorTwo > newCelestialProgressLines.Count() - 1)
                                {
                                    break;
                                }

                                if (newCelestialProgressLines[cursorTwo] == "ReturnFromOrbit" && newCelestialProgressLines[cursorTwo + 1] == "{")
                                {
                                    subIncrement = false;

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(newCelestialProgressLines, cursorTwo + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                    if (subRange.Key + 2 < newCelestialProgressLines.Count && subRange.Value - 3 > 0)
                                    {
                                        newCelestialProgress.returnFromOrbit = newCelestialProgressLines.GetRange(subRange.Key + 2, subRange.Value - 3);

                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                    else
                                    {
                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                }

                                if (cursorTwo > newCelestialProgressLines.Count() - 1)
                                {
                                    break;
                                }

                                if (newCelestialProgressLines[cursorTwo] == "ReturnFromSurface" && newCelestialProgressLines[cursorTwo + 1] == "{")
                                {
                                    subIncrement = false;

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(newCelestialProgressLines, cursorTwo + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                    if (subRange.Key + 2 < newCelestialProgressLines.Count && subRange.Value - 3 > 0)
                                    {
                                        newCelestialProgress.returnFromSurface = newCelestialProgressLines.GetRange(subRange.Key + 2, subRange.Value - 3);

                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                    else
                                    {
                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                }

                                if (cursorTwo > newCelestialProgressLines.Count() - 1)
                                {
                                    break;
                                }

                                if (newCelestialProgressLines[cursorTwo] == "Science" && newCelestialProgressLines[cursorTwo + 1] == "{")
                                {
                                    subIncrement = false;

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(newCelestialProgressLines, cursorTwo + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                    if (subRange.Key + 2 < newCelestialProgressLines.Count && subRange.Value - 3 > 0)
                                    {
                                        newCelestialProgress.science = newCelestialProgressLines.GetRange(subRange.Key + 2, subRange.Value - 3);

                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                    else
                                    {
                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                }

                                if (cursorTwo > newCelestialProgressLines.Count() - 1)
                                {
                                    break;
                                }

                                if (newCelestialProgressLines[cursorTwo] == "Spacewalk" && newCelestialProgressLines[cursorTwo + 1] == "{")
                                {
                                    subIncrement = false;

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(newCelestialProgressLines, cursorTwo + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                    if (subRange.Key + 2 < newCelestialProgressLines.Count && subRange.Value - 3 > 0)
                                    {
                                        newCelestialProgress.spacewalk = newCelestialProgressLines.GetRange(subRange.Key + 2, subRange.Value - 3);

                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                    else
                                    {
                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                }

                                if (cursorTwo > newCelestialProgressLines.Count() - 1)
                                {
                                    break;
                                }

                                if (newCelestialProgressLines[cursorTwo] == "Splashdown" && newCelestialProgressLines[cursorTwo + 1] == "{")
                                {
                                    subIncrement = false;

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(newCelestialProgressLines, cursorTwo + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                    if (subRange.Key + 2 < newCelestialProgressLines.Count && subRange.Value - 3 > 0)
                                    {
                                        newCelestialProgress.splashdown = newCelestialProgressLines.GetRange(subRange.Key + 2, subRange.Value - 3);

                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                    else
                                    {
                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                }

                                if (cursorTwo > newCelestialProgressLines.Count() - 1)
                                {
                                    break;
                                }

                                if (newCelestialProgressLines[cursorTwo] == "StationConstruction" && newCelestialProgressLines[cursorTwo + 1] == "{")
                                {
                                    subIncrement = false;

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(newCelestialProgressLines, cursorTwo + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                    if (subRange.Key + 2 < newCelestialProgressLines.Count && subRange.Value - 3 > 0)
                                    {
                                        newCelestialProgress.stationConstruction = newCelestialProgressLines.GetRange(subRange.Key + 2, subRange.Value - 3);

                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                    else
                                    {
                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                }

                                if (cursorTwo > newCelestialProgressLines.Count() - 1)
                                {
                                    break;
                                }

                                if (newCelestialProgressLines[cursorTwo] == "Suborbit" && newCelestialProgressLines[cursorTwo + 1] == "{")
                                {
                                    subIncrement = false;

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(newCelestialProgressLines, cursorTwo + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                    if (subRange.Key + 2 < newCelestialProgressLines.Count && subRange.Value - 3 > 0)
                                    {
                                        newCelestialProgress.suborbit = newCelestialProgressLines.GetRange(subRange.Key + 2, subRange.Value - 3);

                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                    else
                                    {
                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                }

                                if (cursorTwo > newCelestialProgressLines.Count() - 1)
                                {
                                    break;
                                }

                                if (newCelestialProgressLines[cursorTwo] == "SurfaceEVA" && newCelestialProgressLines[cursorTwo + 1] == "{")
                                {
                                    subIncrement = false;

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(newCelestialProgressLines, cursorTwo + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                    if (subRange.Key + 2 < newCelestialProgressLines.Count && subRange.Value - 3 > 0)
                                    {
                                        newCelestialProgress.surfaceEVA = newCelestialProgressLines.GetRange(subRange.Key + 2, subRange.Value - 3);

                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                    else
                                    {
                                        newCelestialProgressLines.RemoveRange(subRange.Key, subRange.Value);
                                    }
                                }

                                if (cursorTwo > newCelestialProgressLines.Count() - 1)
                                {
                                    break;
                                }

                                if (subIncrement)
                                {
                                    cursorTwo++;
                                }
                            }

                            celestialProgressList.Add(newCelestialProgress);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    //Secrets --- AKA "Spoilers!!!"
                    /*                            *\
                     * -------------------------- *
                     * Alert!!!!!!!!!!!!!!!!!!!!!!*
                     * Spoilers!!!!!!!!!!!!!!!!!!!*
                     * Ahead!!!!!!!!!!!!!!!!!!!!!!*
                     * -------------------------- *
                    \*                            */

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIBopDeadKraken" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIBopDeadKraken = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIBopRandolith" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIBopRandolith = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIDresRandolith" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIDresRandolith = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIDunaFace" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIDunaFace = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIDunaMSL" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIDunaMSL = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIDunaPyramid" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIDunaPyramid = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIDunaRandolith" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIDunaRandolith = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIEelooRandolith" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIEelooRandolith = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIEveRandolith" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIEveRandolith = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIGillyRandolith" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIGillyRandolith = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIIkeRandolith" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIIkeRandolith = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIKerbinIslandAirfield" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIKerbinIslandAirfield = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIKerbinKSC2" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIKerbinKSC2 = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIKerbinMonolith00" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIKerbinMonolith00 = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIKerbinMonolith01" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIKerbinMonolith01 = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIKerbinMonolith02" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIKerbinMonolith02 = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIKerbinPyramids" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIKerbinPyramids = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIKerbinRandolith" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIKerbinRandolith = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIKerbinUFO" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIKerbinUFO = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POILaytheRandolith" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POILaytheRandolith = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIMinmusMonolith00" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIMinmusMonolith00 = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIMinmusRandolith" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIMinmusRandolith = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIMohoRandolith" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIMohoRandolith = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIMunArmstrongMemorial" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIMunArmstrongMemorial = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIMunMonolith00" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIMunMonolith00 = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIMunMonolith01" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIMunMonolith01 = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIMunMonolith02" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIMunMonolith02 = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIMunRandolith" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIMunRandolith = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIMunRockArch00" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIMunRockArch00 = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIMunRockArch01" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIMunRockArch01 = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIMunRockArch02" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));
                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIMunRockArch02 = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIMunUFO" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIMunUFO = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIPolRandolith" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIPolRandolith = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POITyloCave" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POITyloCave = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POITyloRandolith" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POITyloRandolith = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIVallIcehenge" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIVallIcehenge = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "POIVallRandolith" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            secrets.POIVallRandolith = data.GetRange(range.Key + 2, range.Value - 3);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }
                    /*                            *\
                     * -------------------------- *
                     * End!!!!!!!!!!!!!!!!!!!!!!!!*
                     * Of!!!!!!!!!!!!!!!!!!!!!!!!!*
                     * Spoilers!!!!!!!!!!!!!!!!!!!*
                     * -------------------------- *
                    \*                            */

                    if (increment)
                    {
                        cursor++;
                    }
                }

                outputScenario.header = headerLines;
                outputScenario.basicProgress = basicProgress;
                outputScenario.celestialProgress = celestialProgressList;
                outputScenario.secrets = secrets;
            }

            return outputScenario;
        }
        /// <summary>
        /// Converts the the given data and imputs it in to the given scenario data type, then returns it.
        /// </summary>
        public static ScenarioDataTypes.Reputation ConvertToScenarioDataType(List<string> inputData, ScenarioDataTypes.Reputation outputScenario)
        {
            List<string> data = DataCleaner.BasicClean(inputData);
            List<string> headerLines = new List<string>();
            string repLine = "";

            if (DataCleaner.IsDataGood(data))
            {
                headerLines.Add(data[0]);
                headerLines.Add(data[1]);

                repLine = data[2];

                outputScenario.header = headerLines;
                outputScenario.repLine = repLine;
            }

            return outputScenario;
        }
        /// <summary>
        /// Converts the the given data and imputs it in to the given scenario data type, then returns it.
        /// </summary>
        public static ScenarioDataTypes.ResearchAndDevelopment ConvertToScenarioDataType(List<string> inputData, ScenarioDataTypes.ResearchAndDevelopment outputScenario)
        {
            List<string> data = DataCleaner.BasicClean(inputData);
            List<string> headerLines = new List<string>();
            List<ScenarioDataTypes.Tech> techList = new List<ScenarioDataTypes.Tech>();
            List<List<string>> scienceList = new List<List<string>>();
            string sciLine = "";

            if (DataCleaner.IsDataGood(data))
            {
                headerLines.Add(data[0]);
                headerLines.Add(data[1]);

                sciLine = data[2];

                int cursor = 3;//Yes this starts at three
                while (cursor < data.Count)
                {
                    bool increment = true;

                    if (data[cursor] == "Tech" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            List<string> techLines = data.GetRange(range.Key, range.Value);

                            ScenarioDataTypes.Tech newTech = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Tech());
                            newTech.parts = new List<string>();

                            newTech.idLine = techLines[2];
                            newTech.stateLine = techLines[3];
                            newTech.costLine = techLines[4];

                            int cursorTwo = 5;//Yes this starts at five
                            while (cursorTwo < techLines.Count - 1)
                            {
                                newTech.parts.Add(techLines[cursorTwo]);
                                cursorTwo++;
                            }

                            techList.Add(newTech);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "Science" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            List<string> scienceLines = data.GetRange(range.Key + 2, range.Value - 3);//Use Key + 2 and Value - 3 because that way you will only get the inside of the node.

                            scienceList.Add(scienceLines);

                            data.RemoveRange(range.Key, range.Value);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (increment)
                    {
                        cursor++;
                    }
                }

                outputScenario.header = headerLines;
                outputScenario.sciLine = sciLine;
                outputScenario.techList = techList;
                outputScenario.scienceList = scienceList;
            }

            return outputScenario;
        }
        /// <summary>
        /// Converts the the given data and imputs it in to the given scenario data type, then returns it.
        /// </summary>
        public static ScenarioDataTypes.ResourceScenario ConvertToScenarioDataType(List<string> inputData, ScenarioDataTypes.ResourceScenario outputScenario)
        {
            List<string> data = DataCleaner.BasicClean(inputData);
            List<string> headerLines = new List<string>();
            ScenarioDataTypes.ResourceSettings resourceSettings = outputScenario.resourceSettings;

            if (DataCleaner.IsDataGood(data))
            {
                headerLines.Add(data[0]);
                headerLines.Add(data[1]);

                int cursor = 2;//Yes this starts at two
                while (cursor < data.Count)
                {
                    if (data[cursor] == "RESOURCE_SETTINGS" && data[cursor + 1] == "{")
                    {
                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value != 0)
                        {
                            List<string> resourceLines = data.GetRange(range.Key + 2, range.Value - 3);//Use Key + 2 and Value - 3 because that way you will only get the inside of the node.

                            List<ScenarioDataTypes.PlanetScanData> planetScanData = new List<ScenarioDataTypes.PlanetScanData>();

                            int cursorTwo = 0;
                            while (cursorTwo < resourceLines.Count)
                            {
                                if (resourceLines[cursorTwo] == "PLANET_SCAN_DATA" && resourceLines[cursorTwo + 1] == "{")
                                {
                                    ScenarioDataTypes.PlanetScanData scanData = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.PlanetScanData());

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(resourceLines, cursorTwo + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursorTwo, (subMatchBracketIdx - cursorTwo + 1));

                                    scanData.scanDataLine = resourceLines[cursorTwo + 2];

                                    planetScanData.Add(scanData);

                                    resourceLines.RemoveRange(subRange.Key, subRange.Value);
                                }
                                else
                                {
                                    cursorTwo++;
                                }
                            }

                            resourceSettings.resourceLines = resourceLines;

                            resourceSettings.scanDataList = planetScanData;
                        }

                        break;
                    }
                    else
                    {
                        cursor++;
                    }
                }

                outputScenario.header = headerLines;
                outputScenario.resourceSettings = resourceSettings;
            }

            return outputScenario;
        }
        /// <summary>
        /// Converts the the given data and imputs it in to the given scenario data type, then returns it.
        /// </summary>
        public static ScenarioDataTypes.ScenarioCustomWaypoints ConvertToScenarioDataType(List<string> inputData, ScenarioDataTypes.ScenarioCustomWaypoints outputScenario)
        {
            List<string> data = DataCleaner.BasicClean(inputData);
            List<string> headerLines = new List<string>();
            List<ScenarioDataTypes.Waypoint> waypoints = new List<ScenarioDataTypes.Waypoint>();

            if (DataCleaner.IsDataGood(data))
            {
                headerLines.Add(data[0]);
                headerLines.Add(data[1]);

                int cursor = 2;//Yes this starts at two
                while (cursor < data.Count)
                {
                    if (data[cursor] == "WAYPOINT" && data[cursor + 1] == "{")
                    {
                        List<string> newWaypointLines = new List<string>();
                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            ScenarioDataTypes.Waypoint newWaypoint = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Waypoint());

                            newWaypointLines = data.GetRange(range.Key + 2, range.Value - 3);//Use Key + 2 and Value - 3 because that way you will only get the inside of the node.

                            data.RemoveRange(range.Key, range.Value);

                            newWaypoint.waypointLines = newWaypointLines;

                            waypoints.Add(newWaypoint);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }
                    else
                    {
                        cursor++;
                    }
                }

                outputScenario.header = headerLines;
                outputScenario.waypoints = waypoints;
            }

            return outputScenario;
        }
        /// <summary>
        /// Converts the the given data and imputs it in to the given scenario data type, then returns it.
        /// </summary>
        public static ScenarioDataTypes.ScenarioDestructibles ConvertToScenarioDataType(List<string> inputData, ScenarioDataTypes.ScenarioDestructibles outputScenario)
        {
            List<string> data = DataCleaner.BasicClean(inputData);
            List<string> headerLines = new List<string>();
            List<ScenarioDataTypes.Destructibles> destructibles = new List<ScenarioDataTypes.Destructibles>();

            if (DataCleaner.IsDataGood(data))
            {
                headerLines.Add(data[0]);
                headerLines.Add(data[1]);

                int cursor = 2;//Yes this starts at two
                while (cursor < data.Count)
                {
                    bool increment = true;

                    if (data[cursor] == "SpaceCenter/Administration/Facility/Building" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());
                        
                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/FlagPole/Facility" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/AstronautComplex/Facility/mainBuilding" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/LaunchPad/Facility/Flag" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/MissionControl/Facility/mainBuilding" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/ResearchAndDevelopment/Facility/mainBuilding" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/ResearchAndDevelopment/Facility/ForeverAlone" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/SpaceplaneHangar/Facility/Building" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/TrackingStation/Facility/building" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/TrackingStation/Facility/OuterDish" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/VehicleAssemblyBuilding/Facility/VAB2" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/AstronautComplex/Facility/building" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/LaunchPad/Facility/LaunchPadMedium/ksp_pad_cylTank" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/LaunchPad/Facility/LaunchPadMedium/ksp_pad_launchPad" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/LaunchPad/Facility/LaunchPadMedium/ksp_pad_sphereTank" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/LaunchPad/Facility/LaunchPadMedium/ksp_pad_waterTower" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/LaunchPad/Facility/LaunchPadMedium/KSCFlagPoleLaunchPad" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/Runway/Facility/model_runway_new_v43/runway_light_SE" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/Runway/Facility/model_runway_new_v43/runway_light_NE" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/Runway/Facility/model_runway_new_v43/runway_light_NW" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/Runway/Facility/model_runway_new_v43/runway_light_SW" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/Runway/Facility/End09" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/Runway/Facility/End27" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/Runway/Facility/Section1" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/Runway/Facility/Section2" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/Runway/Facility/Section3" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/Runway/Facility/Section4" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/VehicleAssemblyBuilding/Facility/ksp_pad_cylTank" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/VehicleAssemblyBuilding/Facility/Tank" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/VehicleAssemblyBuilding/Facility/mainBuilding" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/VehicleAssemblyBuilding/Facility/PodMemorial" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/SpaceplaneHangar/Facility/Tank" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/SpaceplaneHangar/Facility/ksp_pad_cylTank" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/SpaceplaneHangar/Facility/ksp_pad_waterTower" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/SpaceplaneHangar/Facility/mainBuilding" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/TrackingStation/Facility/dish_array/dish_south" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/TrackingStation/Facility/dish_array/dish_north" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/TrackingStation/Facility/dish_array/dish_east" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/TrackingStation/Facility/MainBuilding" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/MissionControl/Facility/building" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/ResearchAndDevelopment/Facility/ksp_pad_cylTank" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/ResearchAndDevelopment/Facility/SmallLab" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/ResearchAndDevelopment/Facility/CentralBuilding" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/ResearchAndDevelopment/Facility/MainBuilding" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/ResearchAndDevelopment/Facility/CornerLab" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/ResearchAndDevelopment/Facility/WindTunnel" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/ResearchAndDevelopment/Facility/Observatory" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/ResearchAndDevelopment/Facility/SideLab" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/Administration/Facility/mainBuilding" && data[cursor + 1] == "{")
                    {
                        ScenarioDataTypes.Destructibles newdestructible = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Destructibles());

                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            newdestructible.id = data[cursor];

                            newdestructible.infoLine = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            destructibles.Add(newdestructible);
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (increment)
                    {
                        cursor++;
                    }
                }

                outputScenario.header = headerLines;
                outputScenario.destructibles = destructibles;
            }

            return outputScenario;
        }
        /// <summary>
        /// Converts the the given data and imputs it in to the given scenario data type, then returns it.
        /// </summary>
        public static ScenarioDataTypes.ScenarioUpgradeableFacilities ConvertToScenarioDataType(List<string> inputData, ScenarioDataTypes.ScenarioUpgradeableFacilities outputScenario)
        {
            List<string> data = DataCleaner.BasicClean(inputData);
            List<string> headerLines = new List<string>();
            ScenarioDataTypes.UpgradeableBuildings buildings = outputScenario.buildings;

            if (DataCleaner.IsDataGood(data))
            {
                headerLines.Add(data[0]);
                headerLines.Add(data[1]);

                int cursor = 2;//Yes this starts at two
                while (cursor < data.Count)
                {
                    bool increment = true;

                    if (data[cursor] == "SpaceCenter/Administration" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        string building = "";
                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            building = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            buildings.administration = building;
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/AstronautComplex" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        string building = "";
                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            building = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            buildings.astronautComplex = building;
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/FlagPole" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        string building = "";
                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            building = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            buildings.flagPole = building;
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/LaunchPad" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        string building = "";
                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            building = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            buildings.launchPad = building;
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/MissionControl" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        string building = "";
                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            building = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            buildings.missionControl = building;
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/ResearchAndDevelopment" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        string building = "";
                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            building = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            buildings.researchAndDevelopment = building;
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/Runway" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        string building = "";
                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            building = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            buildings.runway = building;
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/SpaceplaneHangar" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        string building = "";
                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            building = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            buildings.spaceplaneHangar = building;
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/TrackingStation" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        string building = "";
                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            building = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            buildings.trackingStation = building;
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (cursor > data.Count() - 1)
                    {
                        break;
                    }

                    if (data[cursor] == "SpaceCenter/VehicleAssemblyBuilding" && data[cursor + 1] == "{")
                    {
                        increment = false;

                        string building = "";
                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            building = data[cursor + 2];

                            data.RemoveRange(range.Key, range.Value);

                            buildings.vehicleAssemblyBuilding = building;
                        }
                        else
                        {
                            data.RemoveRange(range.Key, range.Value);
                        }
                    }

                    if (increment)
                    {
                        cursor++;
                    }
                }

                outputScenario.header = headerLines;
                outputScenario.buildings = buildings;
            }

            return outputScenario;
        }
        /// <summary>
        /// Converts the the given data and imputs it in to the given scenario data type, then returns it.
        /// </summary>
        public static ScenarioDataTypes.StrategySystem ConvertToScenarioDataType(List<string> inputData, ScenarioDataTypes.StrategySystem outputScenario)
        {
            List<string> data = DataCleaner.BasicClean(inputData);
            List<string> headerLines = new List<string>();
            List<ScenarioDataTypes.Strategy> strategyList = new List<ScenarioDataTypes.Strategy>();

            if (DataCleaner.IsDataGood(data))
            {
                headerLines.Add(data[0]);
                headerLines.Add(data[1]);

                int cursor = 2;//Yes this starts at two
                while (cursor < data.Count)
                {
                    if (data[cursor] == "STRATEGIES" && data[cursor + 1] == "{")
                    {
                        int matchBracketIdx = DataCleaner.FindMatchingBracket(data, cursor + 1);
                        KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                        if (range.Key + 2 < data.Count && range.Value - 3 > 0)
                        {
                            List<string> strategyLines = data.GetRange(range.Key + 2, range.Value - 3);//Use Key + 2 and Value - 3 because that way you will only get the inside of the node.

                            int cursorTwo = 0;
                            while (cursorTwo < strategyLines.Count)
                            {
                                bool subIncrement = true;

                                if (strategyLines[cursor] == "STRATEGY" && strategyLines[cursor + 1] == "{")
                                {
                                    subIncrement = false;

                                    int subMatchBracketIdx = DataCleaner.FindMatchingBracket(strategyLines, cursor + 1);
                                    KeyValuePair<int, int> subRange = new KeyValuePair<int, int>(cursor, (subMatchBracketIdx - cursor + 1));

                                    ScenarioDataTypes.Strategy newStrategy = ScenarioDataConstructor.ConstructSubData(new ScenarioDataTypes.Strategy());

                                    List<string> newStrategyLines = strategyLines.GetRange(subRange.Key + 2, subRange.Value - 3);//Use Key + 2 and Value - 3 because that way you will only get the inside of the node.

                                    newStrategy.strategy = newStrategyLines;

                                    strategyList.Add(newStrategy);

                                    strategyLines.RemoveRange(range.Key, range.Value);
                                }

                                if (subIncrement)
                                {
                                    cursorTwo++;
                                }
                            }
                        }

                        break;
                    }
                    else
                    {
                        cursor++;
                    }
                }

                outputScenario.header = headerLines;
                outputScenario.strategies = strategyList;
            }

            return outputScenario;
        }

        /// <summary>
        /// Converts the the given data and returns a string list.
        /// </summary>
        public static List<string> ConvertToStringList(ScenarioDataTypes.ContractSystem inputData)
        {
            List<string> outputList = new List<string>();

            outputList.AddRange(inputData.header);

            outputList.Add("WEIGHTS");
            outputList.Add("{");

            outputList.AddRange(inputData.weights);

            outputList.Add("}");

            outputList.Add("CONTRACTS");
            outputList.Add("{");

            for (int i = 0; i < inputData.contracts.Count; i++)
            {
                outputList.Add("CONTRACT");
                outputList.Add("{");

                outputList.Add(inputData.contracts[i].guid);

                outputList.AddRange(inputData.contracts[i].contractDataLines);

                outputList.AddRange(ConvertParamsToStringList(inputData.contracts[i].parameters));

                outputList.Add("}");
            }

            for (int i = 0; i < inputData.finishedContracts.Count; i++)
            {
                outputList.Add("CONTRACT_FINISHED");
                outputList.Add("{");

                outputList.Add(inputData.finishedContracts[i].guid);

                outputList.AddRange(inputData.finishedContracts[i].contractDataLines);

                outputList.AddRange(ConvertParamsToStringList(inputData.finishedContracts[i].parameters));

                outputList.Add("}");
            }

            outputList.Add("}");

            return outputList;
        }

        /// <summary>
        /// Converts the the given params and returns a string list.
        /// </summary>
        public static List<string> ConvertParamsToStringList(List<ScenarioDataTypes.Param> inputData)
        {
            List<string> outputList = new List<string>();

            for (int i = 0; i < inputData.Count; i++)
            {
                outputList.Add("PARAM");
                outputList.Add("{");

                outputList.AddRange(inputData[i].paramLines);

                if (inputData[i].subParameters.Any(v => v.parentNodeNumber == inputData[i].nodeNumber))
                {
                    List<ScenarioDataTypes.SubParam> childNodes = inputData[i].subParameters.Where(v => v.parentNodeNumber == inputData[i].nodeNumber).ToList();

                    inputData[i].subParameters.RemoveAll(v => v.parentNodeNumber == inputData[i].nodeNumber);

                    KeyValuePair<List<ScenarioDataTypes.SubParam>, List<string>> subKVP = ConvertSubParamsToStringList(childNodes, inputData[i].subParameters);

                    inputData[i].subParameters.Clear();
                    inputData[i].subParameters.AddRange(subKVP.Key);

                    outputList.AddRange(subKVP.Value);
                }

                outputList.Add("}");
            }

            return outputList;
        }

        /// <summary>
        /// Converts the the given sub params and returns a Key Value Pair of a list of unused child nodes and a string list.
        /// </summary>
        public static KeyValuePair<List<ScenarioDataTypes.SubParam>, List<string>> ConvertSubParamsToStringList(List<ScenarioDataTypes.SubParam> inputData, List<ScenarioDataTypes.SubParam> listOfPotentialChildNodes)
        {
            List<string> outputList = new List<string>();

            for (int i = 0; i < inputData.Count; i++)
            {
                outputList.Add("PARAM");
                outputList.Add("{");

                outputList.AddRange(inputData[i].subParamLines);

                if (listOfPotentialChildNodes.Any(v => v.parentNodeNumber == inputData[i].nodeNumber))
                {
                    List<ScenarioDataTypes.SubParam> childNodes = listOfPotentialChildNodes.Where(v => v.parentNodeNumber == inputData[i].nodeNumber).ToList();

                    listOfPotentialChildNodes.RemoveAll(v => v.parentNodeNumber == inputData[i].nodeNumber);

                    KeyValuePair<List<ScenarioDataTypes.SubParam>, List<string>> subKVP = ConvertSubParamsToStringList(childNodes, listOfPotentialChildNodes);

                    listOfPotentialChildNodes.Clear();
                    listOfPotentialChildNodes.AddRange(subKVP.Key);

                    outputList.AddRange(subKVP.Value);
                }

                outputList.Add("}");
            }

            KeyValuePair<List<ScenarioDataTypes.SubParam>, List<string>> output = new KeyValuePair<List<ScenarioDataTypes.SubParam>, List<string>>(listOfPotentialChildNodes, outputList);

            return output;
        }

        /// <summary>
        /// Converts the the given data and returns a string list.
        /// </summary>
        public static List<string> ConvertToStringList(ScenarioDataTypes.Funding inputData)
        {
            List<string> outputList = new List<string>();

            outputList.AddRange(inputData.header);

            outputList.Add(inputData.fundsLine);

            return outputList;
        }

        /// <summary>
        /// Converts the the given data and returns a string list.
        /// </summary>
        public static List<string> ConvertToStringList(ScenarioDataTypes.PartUpgradeManager inputData)
        {
            List<string> outputList = new List<string>();

            outputList.AddRange(inputData.header);

            outputList.Add("UPGRADES");
            outputList.Add("{");

            outputList.Add("Unlocks");
            outputList.Add("{");

            outputList.AddRange(inputData.upgrades.unlocks);

            outputList.Add("}");

            outputList.Add("Enableds");
            outputList.Add("{");

            outputList.AddRange(inputData.upgrades.enableds);

            outputList.Add("}");

            outputList.Add("}");

            return outputList;
        }

        /// <summary>
        /// Converts the the given data and returns a string list.
        /// </summary>
        public static List<string> ConvertToStringList(ScenarioDataTypes.ProgressTracking inputData)
        {
            List<string> outputList = new List<string>();

            outputList.AddRange(inputData.header);

            outputList.Add("Progress");
            outputList.Add("{");

            //Basic Progress
            if (inputData.basicProgress.firstLaunch.Count > 0)
            {
                outputList.Add("FirstLaunch");
                outputList.Add("{");

                outputList.AddRange(inputData.basicProgress.firstLaunch);

                outputList.Add("}");
            }

            if (inputData.basicProgress.firstCrewToSurvive.Count > 0)
            {
                outputList.Add("FirstCrewToSurvive");
                outputList.Add("{");

                outputList.AddRange(inputData.basicProgress.firstCrewToSurvive);

                outputList.Add("}");
            }

            if (inputData.basicProgress.altitudeRecord.Count > 0)
            {
                outputList.Add("RecordsAltitude");
                outputList.Add("{");

                outputList.AddRange(inputData.basicProgress.altitudeRecord);

                outputList.Add("}");
            }

            if (inputData.basicProgress.depthRecord.Count > 0)
            {
                outputList.Add("RecordsDepth");
                outputList.Add("{");

                outputList.AddRange(inputData.basicProgress.depthRecord);

                outputList.Add("}");
            }

            if (inputData.basicProgress.distanceRecord.Count > 0)
            {
                outputList.Add("RecordsDistance");
                outputList.Add("{");

                outputList.AddRange(inputData.basicProgress.distanceRecord);

                outputList.Add("}");
            }

            if (inputData.basicProgress.speedRecord.Count > 0)
            {
                outputList.Add("RecordsSpeed");
                outputList.Add("{");

                outputList.AddRange(inputData.basicProgress.speedRecord);

                outputList.Add("}");
            }

            if (inputData.basicProgress.reachSpace.Count > 0)
            {
                outputList.Add("ReachedSpace");
                outputList.Add("{");

                outputList.AddRange(inputData.basicProgress.reachSpace);

                outputList.Add("}");
            }

            if (inputData.basicProgress.KSCLanding.Count > 0)
            {
                outputList.Add("KSCLanding");
                outputList.Add("{");

                outputList.AddRange(inputData.basicProgress.KSCLanding);

                outputList.Add("}");
            }

            if (inputData.basicProgress.launchpadLanding.Count > 0)
            {
                outputList.Add("LaunchpadLanding");
                outputList.Add("{");

                outputList.AddRange(inputData.basicProgress.launchpadLanding);

                outputList.Add("}");
            }

            if (inputData.basicProgress.runwayLanding.Count > 0)
            {
                outputList.Add("RunwayLanding");
                outputList.Add("{");

                outputList.AddRange(inputData.basicProgress.runwayLanding);

                outputList.Add("}");
            }

            if (inputData.basicProgress.towerBuzz.Count > 0)
            {
                outputList.Add("TowerBuzz");
                outputList.Add("{");

                outputList.AddRange(inputData.basicProgress.towerBuzz);

                outputList.Add("}");
            }

            //Celestial Progress
            for (int i = 0; i < inputData.celestialProgress.Count; i++)
            {
                outputList.Add(inputData.celestialProgress[i].celestialBody);
                outputList.Add("{");

                outputList.Add(inputData.celestialProgress[i].reached);

                if (inputData.celestialProgress[i].baseConstruction.Count > 0)
                {
                    outputList.Add("BaseConstruction");
                    outputList.Add("{");

                    outputList.AddRange(inputData.celestialProgress[i].baseConstruction);

                    outputList.Add("}");
                }

                if (inputData.celestialProgress[i].crewTransfer.Count > 0)
                {
                    outputList.Add("CrewTransfer");
                    outputList.Add("{");

                    outputList.AddRange(inputData.celestialProgress[i].crewTransfer);

                    outputList.Add("}");
                }

                if (inputData.celestialProgress[i].docking.Count > 0)
                {
                    outputList.Add("Docking");
                    outputList.Add("{");

                    outputList.AddRange(inputData.celestialProgress[i].docking);

                    outputList.Add("}");
                }

                if (inputData.celestialProgress[i].escape.Count > 0)
                {
                    outputList.Add("Escape");
                    outputList.Add("{");

                    outputList.AddRange(inputData.celestialProgress[i].escape);

                    outputList.Add("}");
                }

                if (inputData.celestialProgress[i].flagPlant.Count > 0)
                {
                    outputList.Add("FlagPlant");
                    outputList.Add("{");

                    outputList.AddRange(inputData.celestialProgress[i].flagPlant);

                    outputList.Add("}");
                }

                if (inputData.celestialProgress[i].flight.Count > 0)
                {
                    outputList.Add("Flight");
                    outputList.Add("{");

                    outputList.AddRange(inputData.celestialProgress[i].flight);

                    outputList.Add("}");
                }

                if (inputData.celestialProgress[i].flyBy.Count > 0)
                {
                    outputList.Add("FlyBy");
                    outputList.Add("{");

                    outputList.AddRange(inputData.celestialProgress[i].flyBy);

                    outputList.Add("}");
                }

                if (inputData.celestialProgress[i].landing.Count > 0)
                {
                    outputList.Add("Landing");
                    outputList.Add("{");

                    outputList.AddRange(inputData.celestialProgress[i].landing);

                    outputList.Add("}");
                }

                if (inputData.celestialProgress[i].orbit.Count > 0)
                {
                    outputList.Add("Orbit");
                    outputList.Add("{");

                    outputList.AddRange(inputData.celestialProgress[i].orbit);

                    outputList.Add("}");
                }

                if (inputData.celestialProgress[i].rendezvous.Count > 0)
                {
                    outputList.Add("Rendezvous");
                    outputList.Add("{");

                    outputList.AddRange(inputData.celestialProgress[i].rendezvous);

                    outputList.Add("}");
                }

                if (inputData.celestialProgress[i].returnFromFlyby.Count > 0)
                {
                    outputList.Add("ReturnFromFlyby");
                    outputList.Add("{");

                    outputList.AddRange(inputData.celestialProgress[i].returnFromFlyby);

                    outputList.Add("}");
                }

                if (inputData.celestialProgress[i].returnFromOrbit.Count > 0)
                {
                    outputList.Add("ReturnFromOrbit");
                    outputList.Add("{");

                    outputList.AddRange(inputData.celestialProgress[i].returnFromOrbit);

                    outputList.Add("}");
                }

                if (inputData.celestialProgress[i].returnFromSurface.Count > 0)
                {
                    outputList.Add("ReturnFromSurface");
                    outputList.Add("{");

                    outputList.AddRange(inputData.celestialProgress[i].returnFromSurface);

                    outputList.Add("}");
                }

                if (inputData.celestialProgress[i].science.Count > 0)
                {
                    outputList.Add("Science");
                    outputList.Add("{");

                    outputList.AddRange(inputData.celestialProgress[i].science);

                    outputList.Add("}");
                }

                if (inputData.celestialProgress[i].spacewalk.Count > 0)
                {
                    outputList.Add("Spacewalk");
                    outputList.Add("{");

                    outputList.AddRange(inputData.celestialProgress[i].spacewalk);

                    outputList.Add("}");
                }

                if (inputData.celestialProgress[i].splashdown.Count > 0)
                {
                    outputList.Add("Splashdown");
                    outputList.Add("{");

                    outputList.AddRange(inputData.celestialProgress[i].splashdown);

                    outputList.Add("}");
                }

                if (inputData.celestialProgress[i].stationConstruction.Count > 0)
                {
                    outputList.Add("StationConstruction");
                    outputList.Add("{");

                    outputList.AddRange(inputData.celestialProgress[i].stationConstruction);

                    outputList.Add("}");
                }

                if (inputData.celestialProgress[i].suborbit.Count > 0)
                {
                    outputList.Add("Suborbit");
                    outputList.Add("{");

                    outputList.AddRange(inputData.celestialProgress[i].suborbit);

                    outputList.Add("}");
                }

                if (inputData.celestialProgress[i].surfaceEVA.Count > 0)
                {
                    outputList.Add("SurfaceEVA");
                    outputList.Add("{");

                    outputList.AddRange(inputData.celestialProgress[i].surfaceEVA);

                    outputList.Add("}");
                }

                outputList.Add("}");
            }
            
            //Secrets --- AKA "Spoilers!!!"
            /*                            *\
             * -------------------------- *
             * Alert!!!!!!!!!!!!!!!!!!!!!!*
             * Spoilers!!!!!!!!!!!!!!!!!!!*
             * Ahead!!!!!!!!!!!!!!!!!!!!!!*
             * -------------------------- *
            \*                            */
            if (inputData.secrets.POIBopDeadKraken.Count > 0)
            {
                outputList.Add("POIBopDeadKraken");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIBopDeadKraken);

                outputList.Add("}");
            }

            if (inputData.secrets.POIBopRandolith.Count > 0)
            {
                outputList.Add("POIBopRandolith");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIBopRandolith);

                outputList.Add("}");
            }

            if (inputData.secrets.POIDresRandolith.Count > 0)
            {
                outputList.Add("POIDresRandolith");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIDresRandolith);

                outputList.Add("}");
            }

            if (inputData.secrets.POIDunaFace.Count > 0)
            {
                outputList.Add("POIDunaFace");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIDunaFace);

                outputList.Add("}");
            }

            if (inputData.secrets.POIDunaMSL.Count > 0)
            {
                outputList.Add("POIDunaMSL");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIDunaMSL);

                outputList.Add("}");
            }

            if (inputData.secrets.POIDunaPyramid.Count > 0)
            {
                outputList.Add("POIDunaPyramid");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIDunaPyramid);

                outputList.Add("}");
            }

            if (inputData.secrets.POIDunaRandolith.Count > 0)
            {
                outputList.Add("POIDunaRandolith");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIDunaRandolith);

                outputList.Add("}");
            }

            if (inputData.secrets.POIEelooRandolith.Count > 0)
            {
                outputList.Add("POIEelooRandolith");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIEelooRandolith);

                outputList.Add("}");
            }

            if (inputData.secrets.POIEveRandolith.Count > 0)
            {
                outputList.Add("POIEveRandolith");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIEveRandolith);

                outputList.Add("}");
            }

            if (inputData.secrets.POIGillyRandolith.Count > 0)
            {
                outputList.Add("POIGillyRandolith");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIGillyRandolith);

                outputList.Add("}");
            }

            if (inputData.secrets.POIIkeRandolith.Count > 0)
            {
                outputList.Add("POIIkeRandolith");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIIkeRandolith);

                outputList.Add("}");
            }

            if (inputData.secrets.POIKerbinIslandAirfield.Count > 0)
            {
                outputList.Add("POIKerbinIslandAirfield");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIKerbinIslandAirfield);

                outputList.Add("}");
            }

            if (inputData.secrets.POIKerbinKSC2.Count > 0)
            {
                outputList.Add("POIKerbinKSC2");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIKerbinKSC2);

                outputList.Add("}");
            }

            if (inputData.secrets.POIKerbinMonolith00.Count > 0)
            {
                outputList.Add("POIKerbinMonolith00");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIKerbinMonolith00);

                outputList.Add("}");
            }

            if (inputData.secrets.POIKerbinMonolith01.Count > 0)
            {
                outputList.Add("POIKerbinMonolith01");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIKerbinMonolith01);

                outputList.Add("}");
            }

            if (inputData.secrets.POIKerbinMonolith02.Count > 0)
            {
                outputList.Add("POIKerbinMonolith02");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIKerbinMonolith02);

                outputList.Add("}");
            }

            if (inputData.secrets.POIKerbinPyramids.Count > 0)
            {
                outputList.Add("POIKerbinPyramids");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIKerbinPyramids);

                outputList.Add("}");
            }

            if (inputData.secrets.POIKerbinRandolith.Count > 0)
            {
                outputList.Add("POIKerbinRandolith");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIKerbinRandolith);

                outputList.Add("}");
            }

            if (inputData.secrets.POIKerbinUFO.Count > 0)
            {
                outputList.Add("POIKerbinUFO");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIKerbinUFO);

                outputList.Add("}");
            }

            if (inputData.secrets.POILaytheRandolith.Count > 0)
            {
                outputList.Add("POILaytheRandolith");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POILaytheRandolith);

                outputList.Add("}");
            }

            if (inputData.secrets.POIMinmusMonolith00.Count > 0)
            {
                outputList.Add("POIMinmusMonolith00");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIMinmusMonolith00);

                outputList.Add("}");
            }

            if (inputData.secrets.POIMinmusRandolith.Count > 0)
            {
                outputList.Add("POIMinmusRandolith");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIMinmusRandolith);

                outputList.Add("}");
            }

            if (inputData.secrets.POIMohoRandolith.Count > 0)
            {
                outputList.Add("POIMohoRandolith");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIMohoRandolith);

                outputList.Add("}");
            }

            if (inputData.secrets.POIMunArmstrongMemorial.Count > 0)
            {
                outputList.Add("POIMunArmstrongMemorial");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIMunArmstrongMemorial);

                outputList.Add("}");
            }

            if (inputData.secrets.POIMunMonolith00.Count > 0)
            {
                outputList.Add("POIMunMonolith00");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIMunMonolith00);

                outputList.Add("}");
            }

            if (inputData.secrets.POIMunMonolith01.Count > 0)
            {
                outputList.Add("POIMunMonolith01");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIMunMonolith01);

                outputList.Add("}");
            }

            if (inputData.secrets.POIMunMonolith02.Count > 0)
            {
                outputList.Add("POIMunMonolith02");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIMunMonolith02);

                outputList.Add("}");
            }

            if (inputData.secrets.POIMunRandolith.Count > 0)
            {
                outputList.Add("POIMunRandolith");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIMunRandolith);

                outputList.Add("}");
            }

            if (inputData.secrets.POIMunRockArch00.Count > 0)
            {
                outputList.Add("POIMunRockArch00");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIMunRockArch00);

                outputList.Add("}");
            }

            if (inputData.secrets.POIMunRockArch01.Count > 0)
            {
                outputList.Add("POIMunRockArch01");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIMunRockArch01);

                outputList.Add("}");
            }

            if (inputData.secrets.POIMunRockArch02.Count > 0)
            {
                outputList.Add("POIMunRockArch02");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIMunRockArch02);

                outputList.Add("}");
            }

            if (inputData.secrets.POIMunUFO.Count > 0)
            {
                outputList.Add("POIMunUFO");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIMunUFO);

                outputList.Add("}");
            }

            if (inputData.secrets.POIPolRandolith.Count > 0)
            {
                outputList.Add("POIPolRandolith");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIPolRandolith);

                outputList.Add("}");
            }

            if (inputData.secrets.POITyloCave.Count > 0)
            {
                outputList.Add("POITyloCave");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POITyloCave);

                outputList.Add("}");
            }

            if (inputData.secrets.POITyloRandolith.Count > 0)
            {
                outputList.Add("POITyloRandolith");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POITyloRandolith);

                outputList.Add("}");
            }

            if (inputData.secrets.POIVallIcehenge.Count > 0)
            {
                outputList.Add("POIVallIcehenge");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIVallIcehenge);

                outputList.Add("}");
            }

            if (inputData.secrets.POIVallRandolith.Count > 0)
            {
                outputList.Add("POIVallRandolith");
                outputList.Add("{");

                outputList.AddRange(inputData.secrets.POIVallRandolith);

                outputList.Add("}");
            }
            /*                            *\
             * -------------------------- *
             * End!!!!!!!!!!!!!!!!!!!!!!!!*
             * Of!!!!!!!!!!!!!!!!!!!!!!!!!*
             * Spoilers!!!!!!!!!!!!!!!!!!!*
             * -------------------------- *
            \*                            */

            outputList.Add("}");

            return outputList;
        }

        /// <summary>
        /// Converts the the given data and returns a string list.
        /// </summary>
        public static List<string> ConvertToStringList(ScenarioDataTypes.Reputation inputData)
        {
            List<string> outputList = new List<string>();

            outputList.AddRange(inputData.header);

            outputList.Add(inputData.repLine);

            return outputList;
        }

        /// <summary>
        /// Converts the the given data and returns a string list.
        /// </summary>
        public static List<string> ConvertToStringList(ScenarioDataTypes.ResearchAndDevelopment inputData)
        {
            List<string> outputList = new List<string>();

            outputList.AddRange(inputData.header);

            outputList.Add(inputData.sciLine);

            for (int i = 0; i < inputData.techList.Count; i++)
            {
                outputList.Add("Tech");
                outputList.Add("{");

                outputList.Add(inputData.techList[i].idLine);

                outputList.Add(inputData.techList[i].stateLine);

                outputList.Add(inputData.techList[i].costLine);

                outputList.AddRange(inputData.techList[i].parts);

                outputList.Add("}");
            }

            for (int i = 0; i < inputData.scienceList.Count; i++)
            {
                outputList.Add("Science");
                outputList.Add("{");

                outputList.AddRange(inputData.scienceList[i]);

                outputList.Add("}");
            }

            return outputList;
        }

        /// <summary>
        /// Converts the the given data and returns a string list.
        /// </summary>
        public static List<string> ConvertToStringList(ScenarioDataTypes.ResourceScenario inputData)
        {
            List<string> outputList = new List<string>();

            outputList.AddRange(inputData.header);

            outputList.Add("RESOURCE_SETTINGS");
            outputList.Add("{");

            outputList.AddRange(inputData.resourceSettings.resourceLines);

            for (int i = 0; i < inputData.resourceSettings.scanDataList.Count; i++)
            {
                outputList.Add("PLANET_SCAN_DATA");
                outputList.Add("{");

                outputList.Add(inputData.resourceSettings.scanDataList[i].scanDataLine);

                outputList.Add("}");
            }

            outputList.Add("}");

            return outputList;
        }

        /// <summary>
        /// Converts the the given data and returns a string list.
        /// </summary>
        public static List<string> ConvertToStringList(ScenarioDataTypes.ScenarioCustomWaypoints inputData)
        {
            List<string> outputList = new List<string>();

            outputList.AddRange(inputData.header);

            for (int i = 0; i < inputData.waypoints.Count; i++)
            {
                outputList.Add("WAYPOINT");
                outputList.Add("{");

                outputList.AddRange(inputData.waypoints[i].waypointLines);

                outputList.Add("}");
            }
            
            return outputList;
        }

        /// <summary>
        /// Converts the the given data and returns a string list.
        /// </summary>
        public static List<string> ConvertToStringList(ScenarioDataTypes.ScenarioDestructibles inputData)
        {
            List<string> outputList = new List<string>();

            outputList.AddRange(inputData.header);

            for (int i = 0; i < inputData.destructibles.Count; i++)
            {
                outputList.Add(inputData.destructibles[i].id);
                outputList.Add("{");

                outputList.Add(inputData.destructibles[i].infoLine);

                outputList.Add("}");
            }
            
            return outputList;
        }

        /// <summary>
        /// Converts the the given data and returns a string list.
        /// </summary>
        public static List<string> ConvertToStringList(ScenarioDataTypes.ScenarioUpgradeableFacilities inputData)
        {
            List<string> outputList = new List<string>();

            outputList.AddRange(inputData.header);

            outputList.Add("SpaceCenter/Administration");
            outputList.Add("{");

            outputList.Add(inputData.buildings.administration);

            outputList.Add("}");

            outputList.Add("SpaceCenter/AstronautComplex");
            outputList.Add("{");

            outputList.Add(inputData.buildings.astronautComplex);

            outputList.Add("}");

            outputList.Add("SpaceCenter/FlagPole");
            outputList.Add("{");

            outputList.Add(inputData.buildings.flagPole);

            outputList.Add("}");

            outputList.Add("SpaceCenter/LaunchPad");
            outputList.Add("{");

            outputList.Add(inputData.buildings.launchPad);

            outputList.Add("}");

            outputList.Add("SpaceCenter/MissionControl");
            outputList.Add("{");

            outputList.Add(inputData.buildings.missionControl);

            outputList.Add("}");

            outputList.Add("SpaceCenter/ResearchAndDevelopment");
            outputList.Add("{");

            outputList.Add(inputData.buildings.researchAndDevelopment);

            outputList.Add("}");

            outputList.Add("SpaceCenter/Runway");
            outputList.Add("{");

            outputList.Add(inputData.buildings.runway);

            outputList.Add("}");

            outputList.Add("SpaceCenter/SpaceplaneHangar");
            outputList.Add("{");

            outputList.Add(inputData.buildings.spaceplaneHangar);

            outputList.Add("}");

            outputList.Add("SpaceCenter/TrackingStation");
            outputList.Add("{");

            outputList.Add(inputData.buildings.trackingStation);

            outputList.Add("}");

            outputList.Add("SpaceCenter/VehicleAssemblyBuilding");
            outputList.Add("{");

            outputList.Add(inputData.buildings.vehicleAssemblyBuilding);

            outputList.Add("}");

            return outputList;
        }

        /// <summary>
        /// Converts the the given data and returns a string list.
        /// </summary>
        public static List<string> ConvertToStringList(ScenarioDataTypes.StrategySystem inputData)
        {
            List<string> outputList = new List<string>();

            outputList.AddRange(inputData.header);

            outputList.Add("STRATEGIES");
            outputList.Add("{");

            for (int i = 0; i < inputData.strategies.Count; i++)
            {
                outputList.Add("STRATEGY");
                outputList.Add("{");

                outputList.AddRange(inputData.strategies[i].strategy);

                outputList.Add("}");
            }

            outputList.Add("}");

            return outputList;
        }
    }
}
