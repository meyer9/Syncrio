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
