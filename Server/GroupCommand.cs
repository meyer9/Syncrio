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
using SyncrioCommon;

namespace SyncrioServer
{
    public class GroupCommand
    {
        public static void HandleCommand(string commandArgs)
        {
            string func = "";
            string argument1 = "";
            string argument2 = "";

            func = commandArgs;
            if (commandArgs.Contains(" "))
            {
                func = commandArgs.Substring(0, commandArgs.IndexOf(" "));
                if (commandArgs.Substring(func.Length).Contains(" "))
                {
                    string parseString = commandArgs.Substring(func.Length + 1);
                    //First argument
                    if (parseString.StartsWith("\""))
                    {
                        parseString = parseString.Substring(1);
                        argument1 = parseString.Substring(0, parseString.IndexOf("\""));
                        parseString = parseString.Substring(argument1.Length + 1);
                        if (parseString.StartsWith(" "))
                        {
                            parseString = parseString.Substring(1);
                        }
                    }
                    else
                    {
                        if (parseString.Contains(" "))
                        {
                            argument1 = parseString.Substring(0, parseString.IndexOf(" "));
                            parseString = parseString.Substring(argument1.Length + 1);
                        }
                        else
                        {
                            argument1 = parseString.Substring(0, parseString.Length);
                            parseString = "";
                        }

                    }
                    //Second argument
                    if (parseString.Length > 0)
                    {
                        if (parseString.StartsWith("\""))
                        {
                            argument2 = parseString.Substring(1, parseString.Length - 1);
                        }
                        else
                        {
                            argument2 = parseString.Substring(0, parseString.Length);
                        }
                    }
                }
            }

            switch (func)
            {
                default:
                    SyncrioLog.Debug("Undefined function. Usage: /group [create|join] groupname leader, [leave] playername, [remove] groupname, or /group show");
                    break;
                case "create":
                    GroupSystem.fetch.CreateGroupServerCommand(argument1, argument2);
                    break;
                case "remove":
                    GroupSystem.fetch.RemoveGroupServerCommand(null, argument1);
                    break;
                case "join":
                    GroupSystem.fetch.JoinGroupServerCommand(null, argument1, argument2);
                    break;
                case "leave":
                    GroupSystem.fetch.LeaveGroupServerCommand(null, argument1);
                    break;
                case "show":
                    foreach (KeyValuePair<string, GroupObject> kvp in GroupSystem.fetch.GetCopy())
                    {
                        SyncrioLog.Debug(kvp.Key + " (" + kvp.Value.privacy + ")");
                        bool printedLeader = false;
                        foreach (string member in kvp.Value.members)
                        {
                            if (!printedLeader)
                            {
                                SyncrioLog.Debug("  @" + member);
                                printedLeader = true;
                            }
                            else
                            {
                                SyncrioLog.Debug("  +" + member);
                            }
                        }
                    }
                    break;
            }
        }
    }
}
