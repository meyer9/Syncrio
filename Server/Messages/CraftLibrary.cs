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

namespace SyncrioServer.Messages
{
    public class CraftLibrary
    {
        public static void SendCraftList(ClientObject client)
        {
            int numberOfCrafts = 0;
            string craftDirectory = Path.Combine(Server.ScenarioDirectory, "Crafts");
            if (!Directory.Exists(craftDirectory))
            {
                Directory.CreateDirectory(craftDirectory);
            }
            string[] players = Directory.GetDirectories(craftDirectory);
            for (int i = 0; i < players.Length; i++)
            {
                players[i] = players[i].Substring(players[i].LastIndexOf(Path.DirectorySeparatorChar) + 1);
            }
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.CRAFT_LIBRARY;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)CraftMessageType.LIST);
                mw.Write<string[]>(players);
                foreach (string player in players)
                {
                    string playerPath = Path.Combine(craftDirectory, player);
                    string vabPath = Path.Combine(playerPath, "VAB");
                    string sphPath = Path.Combine(playerPath, "SPH");
                    string subassemblyPath = Path.Combine(playerPath, "SUBASSEMBLY");
                    bool vabExists = Directory.Exists(vabPath);
                    bool sphExists = Directory.Exists(sphPath);
                    bool subassemblyExists = Directory.Exists(subassemblyPath);
                    mw.Write<bool>(vabExists);
                    mw.Write<bool>(sphExists);
                    mw.Write<bool>(subassemblyExists);
                    if (vabExists)
                    {
                        string[] vabCraftNames = Directory.GetFiles(vabPath);
                        for (int i = 0; i < vabCraftNames.Length; i++)
                        {
                            //We only want the craft names
                            vabCraftNames[i] = Path.GetFileNameWithoutExtension(vabCraftNames[i]);
                            numberOfCrafts++;
                        }
                        mw.Write<string[]>(vabCraftNames);
                    }

                    if (sphExists)
                    {
                        string[] sphCraftNames = Directory.GetFiles(sphPath);
                        for (int i = 0; i < sphCraftNames.Length; i++)
                        {
                            //We only want the craft names
                            sphCraftNames[i] = Path.GetFileNameWithoutExtension(sphCraftNames[i]);
                            numberOfCrafts++;
                        }
                        mw.Write<string[]>(sphCraftNames);
                    }

                    if (subassemblyExists)
                    {
                        string[] subassemblyCraftNames = Directory.GetFiles(subassemblyPath);
                        for (int i = 0; i < subassemblyCraftNames.Length; i++)
                        {
                            //We only want the craft names
                            subassemblyCraftNames[i] = Path.GetFileNameWithoutExtension(subassemblyCraftNames[i]);
                            numberOfCrafts++;
                        }
                        mw.Write<string[]>(subassemblyCraftNames);
                    }
                }
                newMessage.data = mw.GetMessageBytes();
                ClientHandler.SendToClient(client, newMessage, true);
                SyncrioLog.Debug("Sending " + client.playerName + " " + numberOfCrafts + " craft library entries");
            }
        }

        public static void HandleCraftLibrary(ClientObject client, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                CraftMessageType craftMessageType = (CraftMessageType)mr.Read<int>();
                string fromPlayer = mr.Read<string>();
                if (fromPlayer != client.playerName)
                {
                    Messages.ConnectionEnd.SendConnectionEnd(client, "Kicked for sending an craft library message for another player");
                    return;
                }
                switch (craftMessageType)
                {

                    case CraftMessageType.UPLOAD_FILE:
                        {
                            CraftType uploadType = (CraftType)mr.Read<int>();
                            string uploadName = mr.Read<string>();
                            byte[] uploadData = mr.Read<byte[]>();
                            string playerPath = Path.Combine(Path.Combine(Server.ScenarioDirectory, "Crafts"), fromPlayer);
                            if (!Directory.Exists(playerPath))
                            {
                                Directory.CreateDirectory(playerPath);
                            }
                            string typePath = Path.Combine(playerPath, uploadType.ToString());
                            if (!Directory.Exists(typePath))
                            {
                                Directory.CreateDirectory(typePath);
                            }
                            string craftFile = Path.Combine(typePath, uploadName + ".craft");
                            File.WriteAllBytes(craftFile, uploadData);
                            SyncrioLog.Debug("Saving " + uploadName + ", type: " + uploadType.ToString() + " from " + fromPlayer);
                            using (MessageWriter mw = new MessageWriter())
                            {
                                ServerMessage newMessage = new ServerMessage();
                                newMessage.type = ServerMessageType.CRAFT_LIBRARY;
                                mw.Write<int>((int)CraftMessageType.ADD_FILE);
                                mw.Write<string>(fromPlayer);
                                mw.Write<int>((int)uploadType);
                                mw.Write<string>(uploadName);
                                newMessage.data = mw.GetMessageBytes();
                                ClientHandler.SendToAll(client, newMessage, false);
                            }
                        }
                        break;
                    case CraftMessageType.REQUEST_FILE:
                        {
                            string craftOwner = mr.Read<string>();
                            CraftType requestedType = (CraftType)mr.Read<int>();
                            bool hasCraft = false;
                            string requestedName = mr.Read<string>();
                            string playerPath = Path.Combine(Path.Combine(Server.ScenarioDirectory, "Crafts"), craftOwner);
                            string typePath = Path.Combine(playerPath, requestedType.ToString());
                            string craftFile = Path.Combine(typePath, requestedName + ".craft");
                            if (Directory.Exists(playerPath))
                            {
                                if (Directory.Exists(typePath))
                                {
                                    if (File.Exists(craftFile))
                                    {
                                        hasCraft = true;
                                    }
                                }
                            }
                            ServerMessage newMessage = new ServerMessage();
                            newMessage.type = ServerMessageType.CRAFT_LIBRARY;
                            using (MessageWriter mw = new MessageWriter())
                            {
                                mw.Write<int>((int)CraftMessageType.RESPOND_FILE);
                                mw.Write<string>(craftOwner);
                                mw.Write<int>((int)requestedType);
                                mw.Write<string>(requestedName);
                                mw.Write<bool>(hasCraft);
                                if (hasCraft)
                                {
                                    mw.Write<byte[]>(File.ReadAllBytes(craftFile));
                                    SyncrioLog.Debug("Sending " + fromPlayer + " " + requestedName + " from " + craftOwner);
                                }
                                newMessage.data = mw.GetMessageBytes();
                            }
                            ClientHandler.SendToClient(client, newMessage, false);
                        }
                        break;
                    case CraftMessageType.DELETE_FILE:
                        {
                            CraftType craftType = (CraftType)mr.Read<int>();
                            string craftName = mr.Read<string>();
                            string playerPath = Path.Combine(Path.Combine(Server.ScenarioDirectory, "Crafts"), fromPlayer);
                            string typePath = Path.Combine(playerPath, craftType.ToString());
                            string craftFile = Path.Combine(typePath, craftName + ".craft");
                            if (Directory.Exists(playerPath))
                            {
                                if (Directory.Exists(typePath))
                                {
                                    if (File.Exists(craftFile))
                                    {
                                        File.Delete(craftFile);
                                        SyncrioLog.Debug("Removing " + craftName + ", type: " + craftType.ToString() + " from " + fromPlayer);
                                    }
                                }
                            }
                            if (Directory.Exists(playerPath))
                            {
                                if (Directory.GetFiles(typePath).Length == 0)
                                {
                                    Directory.Delete(typePath);
                                }
                            }
                            if (Directory.GetDirectories(playerPath).Length == 0)
                            {
                                Directory.Delete(playerPath);
                            }
                            //Relay the delete message to other clients
                            ServerMessage newMessage = new ServerMessage();
                            newMessage.type = ServerMessageType.CRAFT_LIBRARY;
                            newMessage.data = messageData;
                            ClientHandler.SendToAll(client, newMessage, false);
                        }
                        break;
                }
            }
        }
    }
}

