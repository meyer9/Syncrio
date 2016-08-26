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
using System.Threading;
using System.Collections.Generic;

namespace SyncrioServer
{
    public class CommandHandler
    {
        private static Dictionary<string, Command> commands = new Dictionary<string, Command>();

        public static void ThreadMain()
        {
            try
            {
                //Register commands
                CommandHandler.RegisterCommand("help", CommandHandler.DisplayHelp, "Displays this help");
                CommandHandler.RegisterCommand("say", CommandHandler.Say, "Broadcasts a message to clients");
                CommandHandler.RegisterCommand("listclients", ListClients, "Lists connected clients");
                CommandHandler.RegisterCommand("countclients", CountClients, "Counts connected clients");
                CommandHandler.RegisterCommand("connectionstats", ConnectionStats, "Displays network traffic usage");

                //Main loop
                while (Server.serverRunning)
                {
                    string input = "";
                    try
                    {
                        input = Console.ReadLine();
                        if (input == null)
                        {
                            SyncrioLog.Debug("Terminal may be not attached or broken, Exiting out of command handler");
                            return;
                        }
                    }
                    catch
                    {
                        if (Server.serverRunning)
                        {
                            SyncrioLog.Debug("Ignored mono Console.ReadLine() bug");
                        }
                        Thread.Sleep(500);
                    }
                    SyncrioLog.Normal("Command input: " + input);
                    if (input.StartsWith("/"))
                    {
                        HandleServerInput(input.Substring(1));
                    }
                    else
                    {
                        if (input != "")
                        {
                            commands["say"].func(input);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (Server.serverRunning)
                {
                    SyncrioLog.Fatal("Error in command handler thread, Exception: " + e);
                    throw;
                }
            }
        }

        public static void HandleServerInput(string input)
        {
            string commandPart = input;
            string argumentPart = "";
            if (commandPart.Contains(" "))
            {
                if (commandPart.Length > commandPart.IndexOf(' ') + 1)
                {
                    argumentPart = commandPart.Substring(commandPart.IndexOf(' ') + 1);
                }
                commandPart = commandPart.Substring(0, commandPart.IndexOf(' '));
            }
            if (commandPart.Length > 0)
            {
                if (commands.ContainsKey(commandPart))
                {
                    try
                    {
                        commands[commandPart].func(argumentPart);
                    }
                    catch (Exception e)
                    {
                        SyncrioLog.Error("Error handling server command " + commandPart + ", Exception " + e);
                    }
                }
                else
                {
                    SyncrioLog.Normal("Unknown server command: " + commandPart);
                }
            }
        }

        public static void RegisterCommand(string command, Action<string> func, string description)
        {
            Command cmd = new Command(command, func, description);
            if (!commands.ContainsKey(command))
            {
                commands.Add(command, cmd);
            }
        }

        private static void DisplayHelp(string commandArgs)
        {
            List<Command> commands = new List<Command>();
            int longestName = 0;
            foreach (Command cmd in CommandHandler.commands.Values)
            {
                commands.Add(cmd);
                if (cmd.name.Length > longestName)
                {
                    longestName = cmd.name.Length;
                }
            }
            foreach (Command cmd in commands)
            {
                SyncrioLog.Normal(cmd.name.PadRight(longestName) + " - " + cmd.description);
            }
        }

        private static void Say(string sayText)
        {
            SyncrioLog.Normal("Broadcasting " + sayText);
            Messages.Chat.SendChatMessageToAll(sayText);
        }

        private static void ListClients(string commandArgs)
        {
            if (Server.players != "")
            {
                SyncrioLog.Normal("Online players: " + Server.players);
            }
            else
            {
                SyncrioLog.Normal("No clients connected");
            }
        }

        private static void CountClients(string commandArgs)
        {
            SyncrioLog.Normal("Online players: " + Server.playerCount);
        }

        private static void ConnectionStats(string commandArgs)
        {
            //Do some shit here.
            long bytesQueuedOutTotal = 0;
            long bytesSentTotal = 0;
            long bytesReceivedTotal = 0;
            SyncrioLog.Normal("Connection stats:");
            foreach (ClientObject client in ClientHandler.GetClients())
            {
                if (client.authenticated)
                {
                    bytesQueuedOutTotal += client.bytesQueuedOut;
                    bytesSentTotal += client.bytesSent;
                    bytesReceivedTotal += client.bytesReceived;
                    SyncrioLog.Normal("Player '" + client.playerName + "', queued out: " + client.bytesQueuedOut + ", sent: " + client.bytesSent + ", received: " + client.bytesReceived);
                }
            }
            SyncrioLog.Normal("Server, queued out: " + bytesQueuedOutTotal + ", sent: " + bytesSentTotal + ", received: " + bytesReceivedTotal);
        }

        private class Command
        {
            public string name;
            public Action<string> func;
            public string description;

            public Command(string name, Action<string> func, string description)
            {
                this.name = name;
                this.func = func;
                this.description = description;
            }
        }
    }
}

