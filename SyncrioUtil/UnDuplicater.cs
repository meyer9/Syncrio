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
    public class UnDuplicater
    {
        public static List<string> StringDuplicateRemover(List<string> stringList)
        {
            Regex wordRegex = new Regex(@"^[\w_]+$", RegexOptions.None);// matches a single word on a line by itself
            
            List<string> lines = new List<string>(stringList);

            List<string> result = new List<string>(lines);

            List<string> preResult = new List<string>();

            List<KeyValuePair<int, int>> ranges = new List<KeyValuePair<int, int>>();

            int cursor = 2;//KEEP ME AT 2!!! Or you Will get a "stack overflow exception"!
            while (cursor < lines.Count())
            {
                // Find a single word with a single "{" on the next line
                // e.g: "PART \n {"
                // this should be the opening of a new child string
                if (wordRegex.IsMatch(lines[cursor]) && (lines[cursor + 1] == "{"))
                {
                    int matchBracketIdx = DataCleaner.FindMatchingBracket(lines, cursor + 1);
                    KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                    // Remove the child string from the file and fix that too
                    List<string> childStringLines = lines.GetRange(range.Key, range.Value);
                    lines.RemoveRange(range.Key, range.Value);

                    result.RemoveRange(range.Key, range.Value);

                    List<string> childLinesToAdd = RemoveStringDuplicates(childStringLines);

                    KeyValuePair<int, int> rangeToAdd = new KeyValuePair<int, int>(preResult.Count, childLinesToAdd.Count);

                    ranges.Add(rangeToAdd);

                    preResult.AddRange(childLinesToAdd);
                }
                else
                {
                    // Only increment if a string was not removed
                    cursor++;
                }
            }

            List<string> tempResult = NodeDuplicateRemover(preResult, ranges);

            result.AddRange(tempResult);

            return result;
        }

        public static List<string> RemoveStringDuplicates(List<string> stringListToUnDuplicate)
        {
            Regex wordRegex = new Regex(@"^[\w_]+$", RegexOptions.None);// matches a single word on a line by itself

            List<string> lines = new List<string>(stringListToUnDuplicate);

            // Sanity checks
            if (!wordRegex.IsMatch(lines[0])) throw new ArgumentException("Invalid node name!");
            if (lines[1] != "{") throw new ArgumentException("Invalid node format!");
            if (lines.Last() != "}") throw new ArgumentException("Invalid node format!");

            List<string> result = new List<string>();

            result.Add(lines[0]);
            result.Add(lines[1]);

            int preResultNumber = 0;
            string[] preResult = new string[lines.Count];
            int cursor = 2;//KEEP ME AT 2!!! Or you Will get a "stack overflow exception"!
            while (cursor < lines.Count())
            {
                // Find a single word with a single "{" on the next line
                // e.g: "PART \n {"
                // this should be the opening of a new child string
                if (wordRegex.IsMatch(lines[cursor]) && (lines[cursor + 1] == "{"))
                {
                    int matchBracketIdx = DataCleaner.FindMatchingBracket(lines, cursor + 1);
                    KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                    // Remove the child string from the file and fix that too
                    List<string> childStringLines = lines.GetRange(range.Key, range.Value);
                    lines.RemoveRange(range.Key, range.Value);

                    string preResultToAdd = string.Join(Environment.NewLine, RemoveStringDuplicates(childStringLines));

                    if (!preResult.Contains(preResultToAdd))
                    {
                        preResult[preResultNumber] = preResultToAdd;
                        preResultNumber++;
                    }
                }
                else
                {
                    preResult[preResultNumber] = lines[cursor];
                    preResultNumber++;
                    // Only increment if a string was not removed
                    cursor++;
                }
            }
            preResult = preResult.Where(x => !string.IsNullOrEmpty(x)).ToArray();

            result.AddRange(preResult);

            result.Add(lines.Last());

            return result;
        }

        public static List<string> NodeDuplicateRemover(List<string> nodeListToUnDuplicate, List<KeyValuePair<int, int>> nodeRangesToCheck)
        {
            List<string> preResult = nodeListToUnDuplicate;

            string[] ranges = new string[nodeRangesToCheck.Count];

            for (int i = 0; i < nodeRangesToCheck.Count; i++)
            {
                ranges[i] = string.Join(Environment.NewLine, preResult.GetRange(nodeRangesToCheck[i].Key, nodeRangesToCheck[i].Value));
            }

            List<string> distinctRanges = ranges.Distinct().ToList();

            preResult.Clear();

            for (int i = 0; i < distinctRanges.Count; i++)
            {
                preResult.AddRange(distinctRanges[i].Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None));
            }

            List<string> result = preResult;

            return result;
        }
    }
}
