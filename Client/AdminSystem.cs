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
using System.Collections.ObjectModel;
using SyncrioCommon;
using MessageStream2;

namespace SyncrioClientSide
{
    public class AdminSystem
    {
        private static AdminSystem singleton;
        private List<string> serverAdmins = new List<string>();
        private object adminLock = new object();

        public static AdminSystem fetch
        {
            get
            {
                return singleton;
            }
        }

        public void HandleAdminMessage(byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                AdminMessageType messageType = (AdminMessageType)mr.Read<int>();
                switch (messageType)
                {
                    case AdminMessageType.LIST:
                        {
                            string[] adminNames = mr.Read<string[]>();
                            foreach (string adminName in adminNames)
                            {
                                RegisterServerAdmin(adminName);
                            }
                        }
                        break;
                    case AdminMessageType.ADD:
                        {
                            string adminName = mr.Read<string>();
                            RegisterServerAdmin(adminName);
                        }
                        break;
                    case AdminMessageType.REMOVE:
                        {
                            string adminName = mr.Read<string>();
                            UnregisterServerAdmin(adminName);
                        }
                        break;
                }
            }
        }

        private void RegisterServerAdmin(string adminName)
        {
            lock (adminLock)
            {
                if (!serverAdmins.Contains(adminName))
                {
                    serverAdmins.Add(adminName);
                }
            }
        }

        private void UnregisterServerAdmin(string adminName)
        {
            lock (adminLock)
            {
                if (serverAdmins.Contains(adminName))
                {
                    serverAdmins.Remove(adminName);
                }
            }
        }

        /// <summary>
        /// Check wether the current player is an admin on the server
        /// </summary>
        /// <returns><c>true</c> if the current player is admin; otherwise, <c>false</c>.</returns>
        public bool IsAdmin()
        {
            return IsAdmin(Settings.fetch.playerName);
        }

        /// <summary>
        /// Check wether the specified player is an admin on the server
        /// </summary>
        /// <returns><c>true</c> if the specified player is admin; otherwise, <c>false</c>.</returns>
        /// <param name="playerName">Player name to check for admin.</param>
        public bool IsAdmin(string playerName)
        {
            lock (adminLock)
            {
                return serverAdmins.Contains(playerName);
            }
        }

        public static void Reset()
        {
            lock (Client.eventLock)
            {
                singleton = new AdminSystem();
            }
        }
    }
}

