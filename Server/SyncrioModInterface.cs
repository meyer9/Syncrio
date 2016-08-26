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

namespace SyncrioServer
{
    /// <summary>
    /// Syncrio message callback.
    /// client - The client that has sent the message
    /// modData - The mod byte[] payload
    /// </summary>
    public delegate void SyncrioMessageCallback(ClientObject client,byte[] modData);
    public class SyncrioModInterface
    {
        private static Dictionary<string, SyncrioMessageCallback> registeredMods = new Dictionary<string, SyncrioMessageCallback>();
        private static object eventLock = new object();

        /// <summary>
        /// Registers a mod handler function that will be called as soon as the message is received.
        /// </summary>
        /// <param name="modName">Mod name.</param>
        /// <param name="handlerFunction">Handler function.</param>
        public static bool RegisterModHandler(string modName, SyncrioMessageCallback handlerFunction)
        {
            lock (eventLock)
            {
                if (registeredMods.ContainsKey(modName))
                {
                    SyncrioLog.Debug("Failed to register mod handler for " + modName + ", mod already registered");
                    return false;
                }
                SyncrioLog.Debug("Registered mod handler for " + modName);
                registeredMods.Add(modName, handlerFunction);
            }
            return true;
        }

        /// <summary>
        /// Unregisters a mod handler.
        /// </summary>
        /// <returns><c>true</c> if a mod handler was unregistered</returns>
        /// <param name="modName">Mod name.</param>
        public static bool UnregisterModHandler(string modName)
        {
            bool unregistered = false;
            lock (eventLock)
            {
                if (registeredMods.ContainsKey(modName))
                {
                    registeredMods.Remove(modName);
                    unregistered = true;
                }
            }
            return unregistered;
        }

        public static void SendSyncrioModMessageToClient(ClientObject client, string modName, byte[] messageData, bool highPriority)
        {
            if (modName == null)
            {
                //Now that's just being silly :)
                return;
            }
            if (messageData == null)
            {
                SyncrioLog.Debug(modName + " attemped to send a null message");
                return;
            }
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.MOD_DATA;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string>(modName);
                mw.Write<byte[]>(messageData);
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToClient(client, newMessage , highPriority);
        }

        public static void SendSyncrioModMessageToAll(ClientObject excludeClient, string modName, byte[] messageData, bool highPriority)
        {
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.MOD_DATA;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string>(modName);
                mw.Write<byte[]>(messageData);
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToAll(excludeClient, newMessage, highPriority);
        }
       

        /// <summary>
        /// Internal use only - Called when a mod message is received from ClientHandler.
        /// </summary>
        public static void OnModMessageReceived(ClientObject client, string modName, byte[] modData)
        {
            if (registeredMods.ContainsKey(modName))
            {
                registeredMods[modName](client, modData);
            }
        }
    }
}
