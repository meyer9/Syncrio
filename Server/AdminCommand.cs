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
using System.IO;
using SyncrioCommon;
using MessageStream2;

namespace SyncrioServer
{
    public class AdminCommand
    {
        public static void HandleCommand(string commandArgs)
        {
            string func = "";
            string playerName = "";

            func = commandArgs;
            if (commandArgs.Contains(" "))
            {
                func = commandArgs.Substring(0, commandArgs.IndexOf(" "));
                if (commandArgs.Substring(func.Length).Contains(" "))
                {
                    playerName = commandArgs.Substring(func.Length + 1);
                }
            }

            switch (func)
            {
                default:
                    SyncrioLog.Normal("Undefined function. Usage: /admin [add|del] playername or /admin show");
                    break;
                case "add":
                    if (File.Exists(Path.Combine(Server.ScenarioDirectory, "Players", playerName + ".txt")))
                    {
                        if (!AdminSystem.fetch.IsAdmin(playerName))
                        {
                            SyncrioLog.Debug("Added '" + playerName + "' to admin list.");
                            AdminSystem.fetch.AddAdmin(playerName);
                            //Notify all players an admin has been added
                            ServerMessage newMessage = new ServerMessage();
                            newMessage.type = ServerMessageType.ADMIN_SYSTEM;
                            using (MessageWriter mw = new MessageWriter())
                            {
                                mw.Write<int>((int)AdminMessageType.ADD);
                                mw.Write<string>(playerName);
                                newMessage.data = mw.GetMessageBytes();
                            }
                            ClientHandler.SendToAll(null, newMessage, true);
                        }
                        else
                        {
                            SyncrioLog.Normal("'" + playerName + "' is already an admin.");
                        }

                    }
                    else
                    {
                        SyncrioLog.Normal("'" + playerName + "' does not exist.");
                    }
                    break;
                case "del":
                    if (AdminSystem.fetch.IsAdmin(playerName))
                    {
                        SyncrioLog.Normal("Removed '" + playerName + "' from the admin list.");
                        AdminSystem.fetch.RemoveAdmin(playerName);
                        //Notify all players an admin has been removed
                        ServerMessage newMessage = new ServerMessage();
                        newMessage.type = ServerMessageType.ADMIN_SYSTEM;
                        using (MessageWriter mw = new MessageWriter())
                        {
                            mw.Write<int>((int)AdminMessageType.REMOVE);
                            mw.Write<string>(playerName);
                            newMessage.data = mw.GetMessageBytes();
                        }
                        ClientHandler.SendToAll(null, newMessage, true);

                    }
                    else
                    {
                        SyncrioLog.Normal("'" + playerName + "' is not an admin.");
                    }
                    break;
                case "show":
                    foreach (string player in AdminSystem.fetch.GetAdmins())
                    {
                        SyncrioLog.Normal(player);
                    }
                    break;
            }
        }
    }
}

