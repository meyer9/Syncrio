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
using MessageStream2;

namespace SyncrioServer.Messages
{
    public class PlayerColor
    {
        public static void SendAllPlayerColors(ClientObject client)
        {
            Dictionary<string,float[]> sendColors = new Dictionary<string, float[]>();
            foreach (ClientObject otherClient in ClientHandler.GetClients())
            {
                if (otherClient.authenticated && otherClient.playerColor != null)
                {
                    if (otherClient != client)
                    {
                        sendColors[otherClient.playerName] = otherClient.playerColor;
                    }
                }
            }
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.PLAYER_COLOR;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)PlayerColorMessageType.LIST);
                mw.Write<int>(sendColors.Count);
                foreach (KeyValuePair<string, float[]> kvp in sendColors)
                {
                    mw.Write<string>(kvp.Key);
                    mw.Write<float[]>(kvp.Value);
                }
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToClient(client, newMessage, true);
        }

        public static void HandlePlayerColor(ClientObject client, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                PlayerColorMessageType messageType = (PlayerColorMessageType)mr.Read<int>();
                switch (messageType)
                {
                    case PlayerColorMessageType.SET:
                        {
                            string playerName = mr.Read<string>();
                            if (playerName != client.playerName)
                            {
                                SyncrioLog.Debug(client.playerName + " tried to send a color update for " + playerName + ", kicking.");
                                Messages.ConnectionEnd.SendConnectionEnd(client, "Kicked for sending a color update for another player");
                                return;
                            }
                            client.playerColor = mr.Read<float[]>();
                            //Relay the message
                            ServerMessage newMessage = new ServerMessage();
                            newMessage.type = ServerMessageType.PLAYER_COLOR;
                            newMessage.data = messageData;
                            ClientHandler.SendToAll(client, newMessage, true);
                        }
                        break;
                }
            }
        }
    }
}

