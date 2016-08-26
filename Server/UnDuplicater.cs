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
    class UnDuplicater
    {
        public static List<string> StringDuplicateRemover(string[] stringArray)
        {
            Regex wordRegex = new Regex(@"^[\w_]+$", RegexOptions.None);// matches a single word on a line by itself

            // Split the text in lines and trim each line
            List<string> lines = stringArray.ToList();

            for (int i = 0; i < lines.Count(); i++)
                lines[i] = lines[i].Trim();

            // Remove comment lines
            lines.RemoveAll(l => l.StartsWith("//"));

            List<string> result = new List<string>(lines);

            result[0] = lines[0];
            result[1] = lines[1];

            int cursor = 2;
            while (cursor < lines.Count())
            {
                // Find a single word with a single "{" on the next line
                // e.g: "PART \n {"
                // this should be the opening of a new child string
                if (wordRegex.IsMatch(lines[cursor]) && (lines[cursor + 1] == "{"))
                {
                    int matchBracketIdx = FindMatchingBracket(lines, cursor + 1);
                    KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                    // Remove the child string from the file and fix that too
                    List<string> childStringLines = lines.GetRange(range.Key, range.Value);
                    lines.RemoveRange(range.Key, range.Value);

                    result.AddRange(RemoveStringDuplicates(String.Join(Environment.NewLine, childStringLines.ToArray())));
                }
                else
                {
                    // Only increment if a string was not removed
                    cursor++;
                }
            }

            result.Add(lines.Last());
            return result;
        }

        public static List<string> RemoveStringDuplicates(string stringToUnDuplicate)
        {
            Regex wordRegex = new Regex(@"^[\w_]+$", RegexOptions.None);// matches a single word on a line by itself

            // Split the text in lines and trim each line
            List<string> lines = stringToUnDuplicate.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

            for (int i = 0; i < lines.Count(); i++)
                lines[i] = lines[i].Trim();

            // Remove comment lines
            lines.RemoveAll(l => l.StartsWith("//"));

            // Sanity checks
            if (!wordRegex.IsMatch(lines[0])) throw new ArgumentException("Invalid node name!");
            if (lines[1] != "{") throw new ArgumentException("Invalid node format!");
            if (lines.Last() != "}") throw new ArgumentException("Invalid node format!");

            List<string> result = new List<string>(lines);

            result[0] = lines[0];
            result[1] = lines[1];

            int preResultNumber = 0;
            string[] preResult = new string[lines.Count];
            int cursor = 2;
            while (cursor < lines.Count())
            {
                // Find a single word with a single "{" on the next line
                // e.g: "PART \n {"
                // this should be the opening of a new child string
                if (wordRegex.IsMatch(lines[cursor]) && (lines[cursor + 1] == "{"))
                {
                    int matchBracketIdx = FindMatchingBracket(lines, cursor + 1);
                    KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                    // Remove the child string from the file and fix that too
                    List<string> childStringLines = lines.GetRange(range.Key, range.Value);
                    lines.RemoveRange(range.Key, range.Value);

                    preResult[preResultNumber] = RemoveStringDuplicates(String.Join(Environment.NewLine, childStringLines.ToArray())).ToString();
                    preResultNumber++;
                }
                else
                {
                    // Only increment if a string was not removed
                    cursor++;
                }
            }

            for (int i = 0; i < preResult.Length; i++)
            {
                for (int i2 = 0; i2 < preResult.Length; i2++)
                {
                    if (i != i2)
                    {
                        if (preResult[i] != null && preResult[i2] != null)
                        {
                            if (preResult[i] == preResult[i2])
                            {
                                preResult[i2] = null;
                            }
                        }
                    }
                }
            }
            preResult = preResult.Where(x => !string.IsNullOrEmpty(x)).ToArray();

            string tempPreResult = preResult.ToString();

            preResult = tempPreResult.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            result.AddRange(preResult.ToList());

            result.Add(lines.Last());

            return result;
        }


        static int FindMatchingBracket(List<string> lines, int startFrom)
        {
            int brackets = 0;
            for (int i = startFrom; i < lines.Count(); i++)
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
