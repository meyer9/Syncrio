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
using SyncrioCommon;
using MessageStream2;

namespace SyncrioServer.Messages
{
    public class FlagSync
    {
        public static void HandleFlagSync(ClientObject client, byte[] messageData)
        {
            string flagPath = Path.Combine(Server.ScenarioDirectory, "Flags");
            using (MessageReader mr = new MessageReader(messageData))
            {
                FlagMessageType messageType = (FlagMessageType)mr.Read<int>();
                string playerName = mr.Read<string>();
                if (playerName != client.playerName)
                {
                    Messages.ConnectionEnd.SendConnectionEnd(client, "Kicked for sending a flag for another player");
                    return;
                }
                switch (messageType)
                {
                    case FlagMessageType.LIST:
                        {
                            //Send the list back
                            List<string> serverFlagFileNames = new List<string>();
                            List<string> serverFlagOwners = new List<string>();
                            List<string> serverFlagShaSums = new List<string>();

                            string[] clientFlags = mr.Read<string[]>();
                            string[] clientFlagShas = mr.Read<string[]>();
                            string[] serverFlags = Directory.GetFiles(flagPath, "*", SearchOption.AllDirectories);
                            foreach (string serverFlag in serverFlags)
                            {
                                string trimmedName = Path.GetFileName(serverFlag);
                                string flagOwnerPath = Path.GetDirectoryName(serverFlag);
                                string flagOwner = flagOwnerPath.Substring(Path.GetDirectoryName(flagOwnerPath).Length + 1);
                                bool isMatched = false;
                                bool shaDifferent = false;
                                for (int i = 0; i < clientFlags.Length; i++)
                                {
                                    if (clientFlags[i].ToLower() == trimmedName.ToLower())
                                    {
                                        isMatched = true;
                                        shaDifferent = (Common.CalculateSHA256Hash(serverFlag) != clientFlagShas[i]);
                                    }
                                }
                                if (!isMatched || shaDifferent)
                                {
                                    if (flagOwner == client.playerName)
                                    {
                                        SyncrioLog.Debug("Deleting flag " + trimmedName);
                                        File.Delete(serverFlag);
                                        ServerMessage newMessage = new ServerMessage();
                                        newMessage.type = ServerMessageType.FLAG_SYNC;
                                        using (MessageWriter mw = new MessageWriter())
                                        {
                                            mw.Write<int>((int)FlagMessageType.DELETE_FILE);
                                            mw.Write<string>(trimmedName);
                                            newMessage.data = mw.GetMessageBytes();
                                            ClientHandler.SendToAll(client, newMessage, false);
                                        }
                                        if (Directory.GetFiles(flagOwnerPath).Length == 0)
                                        {
                                            Directory.Delete(flagOwnerPath);
                                        }
                                    }
                                    else
                                    {
                                        SyncrioLog.Debug("Sending flag " + serverFlag + " from " + flagOwner + " to " + client.playerName);
                                        ServerMessage newMessage = new ServerMessage();
                                        newMessage.type = ServerMessageType.FLAG_SYNC;
                                        using (MessageWriter mw = new MessageWriter())
                                        {
                                            mw.Write<int>((int)FlagMessageType.FLAG_DATA);
                                            mw.Write<string>(flagOwner);
                                            mw.Write<string>(trimmedName);
                                            mw.Write<byte[]>(File.ReadAllBytes(serverFlag));
                                            newMessage.data = mw.GetMessageBytes();
                                            ClientHandler.SendToClient(client, newMessage, false);
                                        }
                                    }
                                }
                                //Don't tell the client we have a different copy of the flag so it is reuploaded
                                if (File.Exists(serverFlag))
                                {
                                    serverFlagFileNames.Add(trimmedName);
                                    serverFlagOwners.Add(flagOwner);
                                    serverFlagShaSums.Add(Common.CalculateSHA256Hash(serverFlag));
                                }
                            }
                            ServerMessage listMessage = new ServerMessage();
                            listMessage.type = ServerMessageType.FLAG_SYNC;
                            using (MessageWriter mw2 = new MessageWriter())
                            {
                                mw2.Write<int>((int)FlagMessageType.LIST);
                                mw2.Write<string[]>(serverFlagFileNames.ToArray());
                                mw2.Write<string[]>(serverFlagOwners.ToArray());
                                mw2.Write<string[]>(serverFlagShaSums.ToArray());
                                listMessage.data = mw2.GetMessageBytes();
                            }
                            ClientHandler.SendToClient(client, listMessage, false);
                        }
                        break;
                    case FlagMessageType.DELETE_FILE:
                        {
                            string flagName = mr.Read<string>();
                            string playerFlagPath = Path.Combine(flagPath, client.playerName);
                            if (Directory.Exists(playerFlagPath))
                            {
                                string flagFile = Path.Combine(playerFlagPath, flagName);
                                if (File.Exists(flagFile))
                                {
                                    File.Delete(flagFile);
                                }
                                if (Directory.GetFiles(playerFlagPath).Length == 0)
                                {
                                    Directory.Delete(playerFlagPath);
                                }
                            }
                            ServerMessage newMessage = new ServerMessage();
                            newMessage.type = ServerMessageType.FLAG_SYNC;
                            using (MessageWriter mw = new MessageWriter())
                            {
                                mw.Write<int>((int)FlagMessageType.DELETE_FILE);
                                mw.Write<string>(flagName);
                                newMessage.data = mw.GetMessageBytes();
                            }
                            ClientHandler.SendToAll(client, newMessage, false);
                        }
                        break;
                    case FlagMessageType.UPLOAD_FILE:
                        {
                            string flagName = mr.Read<string>();
                            byte[] flagData = mr.Read<byte[]>();
                            string playerFlagPath = Path.Combine(flagPath, client.playerName);
                            if (!Directory.Exists(playerFlagPath))
                            {
                                Directory.CreateDirectory(playerFlagPath);
                            }
                            SyncrioLog.Debug("Saving flag " + flagName + " from " + client.playerName);
                            File.WriteAllBytes(Path.Combine(playerFlagPath, flagName), flagData);
                            ServerMessage newMessage = new ServerMessage();
                            newMessage.type = ServerMessageType.FLAG_SYNC;
                            using (MessageWriter mw = new MessageWriter())
                            {
                                mw.Write<int>((int)FlagMessageType.FLAG_DATA);
                                mw.Write<string>(client.playerName);
                                mw.Write<string>(flagName);
                                mw.Write<byte[]>(flagData);
                            }
                            ClientHandler.SendToAll(client, newMessage, false);
                        }
                        break;
                }
            }
        }
    }
}

