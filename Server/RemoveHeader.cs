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
    class RemoveHeader
    {
        public static List<string> HeaderRemover(List<string> stringList)
        {
            Regex wordRegex = new Regex(@"^[\w_]+$", RegexOptions.None);// matches a single word on a line by itself

            // Split the text in lines and trim each line
            List<string> lines = stringList;

            for (int i = 0; i < lines.Count(); i++)
                lines[i] = lines[i].Trim();

            // Remove comment lines
            lines.RemoveAll(l => l.StartsWith("//"));
            
            List<string> result = new List<string>();

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
    }
}
