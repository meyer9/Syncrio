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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyncrioCommon;
using MessageStream2;

namespace SyncrioServer.Messages
{
    class Vessel
    {
        public static string VesselsDirectory = Path.Combine(Server.ScenarioDirectory, "Vessels");

        public static void HandleVessels(ClientObject client, byte[] messageData)
        {
            string playerFolder = Path.Combine(VesselsDirectory, client.playerName);
            if (!Directory.Exists(playerFolder))
            {
                Directory.CreateDirectory(playerFolder);
            }

            using (MessageReader mr = new MessageReader(messageData))
            {
                List<string> VesselList = new List<string>();
                int numberOfVessels = mr.Read<int>();

                for (int i = 0; i < numberOfVessels; i++)
                {
                    VesselList.Add("Vessel");
                    VesselList.Add("{");

                    VesselList.AddRange(SyncrioUtil.ByteArraySerializer.Deserialize(mr.Read<byte[]>()));

                    VesselList.Add("}");
                }

                SyncrioUtil.FileHandler.WriteToFile(SyncrioUtil.ByteArraySerializer.Serialize(VesselList), Path.Combine(playerFolder, "Vessels.txt"));
            }
        }

        public static void SendPlayerVessels(ClientObject client)
        {
            string playerFolder = Path.Combine(VesselsDirectory, client.playerName);
            if (Directory.Exists(playerFolder))
            {
                if (File.Exists(Path.Combine(playerFolder, "Vessels.txt")))
                {
                    byte[] vesselBytes = SyncrioUtil.FileHandler.ReadFromFile(Path.Combine(playerFolder, "Vessels.txt"));

                    List<string> vesselList = SyncrioUtil.ByteArraySerializer.Deserialize(vesselBytes);

                    using (MessageWriter mw = new MessageWriter())
                    {
                        List<byte[]> listToWrite = new List<byte[]>();

                        int cursor = 0;
                        while (cursor < vesselList.Count)
                        {
                            if (vesselList[cursor] == "Vessel" && vesselList[cursor + 1] == "{")
                            {
                                int matchBracketIdx = SyncrioUtil.DataCleaner.FindMatchingBracket(vesselList, cursor + 1);
                                KeyValuePair<int, int> range = new KeyValuePair<int, int>(cursor, (matchBracketIdx - cursor + 1));

                                if (range.Key + 2 < vesselList.Count && range.Value - 3 > 0)
                                {
                                    listToWrite.Add(SyncrioUtil.ByteArraySerializer.Serialize(vesselList.GetRange(range.Key + 2, range.Value - 3)));//Use Key + 2 and Value - 3 because that way you will only get the inside of the node.
                                    vesselList.RemoveRange(range.Key, range.Value);
                                }
                                else
                                {
                                    vesselList.RemoveRange(range.Key, range.Value);
                                }
                            }
                            else
                            {
                                cursor++;
                            }
                        }

                        mw.Write<int>(listToWrite.Count);

                        for (int i = 0; i < listToWrite.Count; i++)
                        {
                            mw.Write<byte[]>(listToWrite[i]);
                        }

                        ServerMessage newMessage = new ServerMessage();

                        newMessage.type = ServerMessageType.SEND_VESSELS;

                        newMessage.data = mw.GetMessageBytes();

                        ClientHandler.SendToClient(client, newMessage, true);
                    }
                }
            }
        }
    }
}
