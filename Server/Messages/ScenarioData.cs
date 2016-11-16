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
using System.IO;
using System.Linq;
using MessageStream2;
using SyncrioCommon;

namespace SyncrioServer.Messages
{
    public class ScenarioData
    {
        public static object scenarioDataLock = new object();

        public static void SendScenarioModules(ClientObject client)
        {
            lock (scenarioDataLock)
            {
                int numberOfScenarioModules = Directory.GetFiles(Path.Combine(Server.ScenarioDirectory, "Players", client.playerName)).Length;
                int currentScenarioModule = 0;
                string[] scenarioNames = new string[numberOfScenarioModules];
                byte[][] scenarioDataArray = new byte[numberOfScenarioModules][];
                foreach (string file in Directory.GetFiles(Path.Combine(Server.ScenarioDirectory, "Players", client.playerName)))
                {
                    //Remove the .txt part for the name
                    scenarioNames[currentScenarioModule] = Path.GetFileNameWithoutExtension(file);
                    scenarioDataArray[currentScenarioModule] = File.ReadAllBytes(file);
                    currentScenarioModule++;
                }
                ServerMessage newMessage = new ServerMessage();
                newMessage.type = ServerMessageType.SCENARIO_DATA;
                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<string[]>(scenarioNames);
                    foreach (byte[] scenarioData in scenarioDataArray)
                    {
                        if (client.compressionEnabled)
                        {
                            mw.Write<byte[]>(Compression.CompressIfNeeded(scenarioData));
                        }
                        else
                        {
                            mw.Write<byte[]>(Compression.AddCompressionHeader(scenarioData, false));
                        }
                    }
                    newMessage.data = mw.GetMessageBytes();
                }
                ClientHandler.SendToClient(client, newMessage, true);
            }
        }

        public static void SendResetScenarioGroupModules(ClientObject client, string groupName)
        {
            if (!Directory.Exists(Path.Combine(Server.ScenarioDirectory, "GroupData", "GroupScenarios", groupName, "Subspace0", "Scenario")))
            {
                return;
            }
            int numberOfScenarioModules = Directory.GetFiles(Path.Combine(Server.ScenarioDirectory, "GroupData", "GroupScenarios", groupName, "Subspace0", "Scenario")).Length;
            int currentScenarioModule = 0;
            string[] scenarioNames = new string[numberOfScenarioModules];
            byte[][] scenarioDataArray = new byte[numberOfScenarioModules][];
            foreach (string file in Directory.GetFiles(Path.Combine(Server.ScenarioDirectory, "GroupData", "GroupScenarios", groupName, "Subspace0", "Scenario")))
            {
                //Remove the .txt part for the name
                scenarioNames[currentScenarioModule] = Path.GetFileNameWithoutExtension(file);
                scenarioDataArray[currentScenarioModule] = File.ReadAllBytes(file);
                currentScenarioModule++;
            }

            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.SCENARIO_DATA;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string[]>(scenarioNames);
                foreach (byte[] scenarioData in scenarioDataArray)
                {
                    if (client.compressionEnabled)
                    {
                        mw.Write<byte[]>(Compression.CompressIfNeeded(scenarioData));
                    }
                    else
                    {
                        mw.Write<byte[]>(Compression.AddCompressionHeader(scenarioData, false));
                    }
                }
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToClient(client, newMessage, true);
        }

        public static void SendScenarioGroupModules(ClientObject client, string groupName)
        {
            int subSpace = client.subspace;
            if (!Directory.Exists(Path.Combine(Server.ScenarioDirectory, "GroupData", "GroupScenarios", groupName, "Subspace" + subSpace, "Scenario")))
            {
                return;
            }
            int numberOfScenarioModules = Directory.GetFiles(Path.Combine(Server.ScenarioDirectory, "GroupData", "GroupScenarios", groupName, "Subspace" + subSpace, "Scenario")).Length;
            int currentScenarioModule = 0;
            string[] scenarioNames = new string[numberOfScenarioModules];
            byte[][] scenarioDataArray = new byte[numberOfScenarioModules][];
            foreach (string file in Directory.GetFiles(Path.Combine(Server.ScenarioDirectory, "GroupData", "GroupScenarios", groupName, "Subspace" + subSpace, "Scenario")))
            {
                //Remove the .txt part for the name
                scenarioNames[currentScenarioModule] = Path.GetFileNameWithoutExtension(file);
                scenarioDataArray[currentScenarioModule] = File.ReadAllBytes(file);
                currentScenarioModule++;
            }

            KeyValuePair<int, int> keys = ScenarioHandler.GetKeys(subSpace, groupName);

            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.SCENARIO_DATA;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string[]>(scenarioNames);

                foreach (byte[] scenarioData in scenarioDataArray)
                {
                    if (client.compressionEnabled)
                    {
                        mw.Write<byte[]>(Compression.CompressIfNeeded(scenarioData));
                    }
                    else
                    {
                        mw.Write<byte[]>(Compression.AddCompressionHeader(scenarioData, false));
                    }
                }
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToClient(client, newMessage, true);
        }
    }
}
