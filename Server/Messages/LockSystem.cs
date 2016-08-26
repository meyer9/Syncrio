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
    public class LockSystem
    {
        public static void SendAllLocks(ClientObject client)
        {
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.LOCK_SYSTEM;
            //Send the dictionary as 2 string[]'s.
            Dictionary<string,string> lockList = SyncrioServer.LockSystem.fetch.GetLockList();
            List<string> lockKeys = new List<string>(lockList.Keys);
            List<string> lockValues = new List<string>(lockList.Values);
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write((int)LockMessageType.LIST);
                mw.Write<string[]>(lockKeys.ToArray());
                mw.Write<string[]>(lockValues.ToArray());
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToClient(client, newMessage, true);
        }

        public static void HandleLockSystemMessage(ClientObject client, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                //All of the messages need replies, let's create a message for it.
                ServerMessage newMessage = new ServerMessage();
                newMessage.type = ServerMessageType.LOCK_SYSTEM;
                //Read the lock-system message type
                LockMessageType lockMessageType = (LockMessageType)mr.Read<int>();
                switch (lockMessageType)
                {
                    case LockMessageType.ACQUIRE:
                        {
                            string playerName = mr.Read<string>();
                            string lockName = mr.Read<string>();
                            bool force = mr.Read<bool>();
                            if (playerName != client.playerName)
                            {
                                Messages.ConnectionEnd.SendConnectionEnd(client, "Kicked for sending a lock message for another player");
                            }
                            bool lockResult = SyncrioServer.LockSystem.fetch.AcquireLock(lockName, playerName, force);
                            using (MessageWriter mw = new MessageWriter())
                            {
                                mw.Write((int)LockMessageType.ACQUIRE);
                                mw.Write(playerName);
                                mw.Write(lockName);
                                mw.Write(lockResult);
                                newMessage.data = mw.GetMessageBytes();
                            }
                            //Send to all clients
                            ClientHandler.SendToAll(null, newMessage, true);
                            if (lockResult)
                            {
                                SyncrioLog.Debug(playerName + " acquired lock " + lockName);
                            }
                            else
                            {
                                SyncrioLog.Debug(playerName + " failed to acquire lock " + lockName);
                            }
                        }
                        break;
                    case LockMessageType.RELEASE:
                        {
                            string playerName = mr.Read<string>();
                            string lockName = mr.Read<string>();
                            if (playerName != client.playerName)
                            {
                                Messages.ConnectionEnd.SendConnectionEnd(client, "Kicked for sending a lock message for another player");
                            }
                            bool lockResult = SyncrioServer.LockSystem.fetch.ReleaseLock(lockName, playerName);
                            if (!lockResult)
                            {
                                Messages.ConnectionEnd.SendConnectionEnd(client, "Kicked for releasing a lock you do not own");
                            }
                            else
                            {
                                using (MessageWriter mw = new MessageWriter())
                                {
                                    mw.Write((int)LockMessageType.RELEASE);
                                    mw.Write(playerName);
                                    mw.Write(lockName);
                                    mw.Write(lockResult);
                                    newMessage.data = mw.GetMessageBytes();
                                }
                                //Send to all clients
                                ClientHandler.SendToAll(null, newMessage, true);
                            }
                            if (lockResult)
                            {
                                SyncrioLog.Debug(playerName + " released lock " + lockName);
                            }
                            else
                            {
                                SyncrioLog.Debug(playerName + " failed to release lock " + lockName);
                            }
                        }
                        break;
                }
            }
        }
    }
}

