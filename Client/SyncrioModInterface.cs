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

namespace SyncrioClientSide
{
    public delegate void SyncrioMessageCallback (byte[] messageData);

    public class QueuedSyncrioMessage
    {
        public string modName;
        public byte[] messageData;
    }

    public class SyncrioModInterface
    {
        private static SyncrioModInterface singleton = new SyncrioModInterface();
        //Registered methods
        private Dictionary<string, SyncrioMessageCallback> registeredRawMods = new Dictionary<string, SyncrioMessageCallback>();
        private Dictionary<string, SyncrioMessageCallback> registeredUpdateMods = new Dictionary<string, SyncrioMessageCallback>();
        private Dictionary<string, SyncrioMessageCallback> registeredFixedUpdateMods = new Dictionary<string, SyncrioMessageCallback>();
        //Delay queues - Apparently ConcurrentQueue isn't supported in .NET 3.5 :(
        private Dictionary<string, Queue<byte[]>> updateQueue = new Dictionary<string, Queue<byte[]>>();
        private Dictionary<string, Queue<byte[]>> fixedUpdateQueue = new Dictionary<string, Queue<byte[]>>();
        //Protect against threaded access
        private object eventLock = new object();

        public SyncrioModInterface()
        {
            lock (Client.eventLock)
            {
                Client.updateEvent.Add(this.Update);
                Client.fixedUpdateEvent.Add(this.FixedUpdate);
            }
        }

        public static SyncrioModInterface fetch
        {
            get
            {
                return singleton;
            }
        }

        /// <summary>
        /// Unregisters a mod handler.
        /// </summary>
        /// <returns><c>true</c>, if mod handler was unregistered, <c>false</c> otherwise.</returns>
        /// <param name="modName">Mod name.</param>
        public bool UnregisterModHandler(string modName)
        {
            bool unregistered = false;
            lock (eventLock)
            {
                if (registeredRawMods.ContainsKey(modName))
                {
                    registeredRawMods.Remove(modName);
                    unregistered = true;
                }
                if (registeredUpdateMods.ContainsKey(modName))
                {
                    registeredUpdateMods.Remove(modName);
                    updateQueue.Remove(modName);
                    unregistered = true;
                }
                if (registeredFixedUpdateMods.ContainsKey(modName))
                {
                    registeredFixedUpdateMods.Remove(modName);
                    fixedUpdateQueue.Remove(modName);
                    unregistered = true;
                }
            }
            return unregistered;
        }

        /// <summary>
        /// Registers a mod handler function that will be called as soon as the message is received.
        /// This is called from the networking thread, so you should avoid interacting with KSP directly here as Unity is not thread safe.
        /// </summary>
        /// <param name="modName">Mod name.</param>
        /// <param name="handlerFunction">Handler function.</param>
        public bool RegisterRawModHandler(string modName, SyncrioMessageCallback handlerFunction)
        {
            lock (eventLock)
            {
                if (registeredRawMods.ContainsKey(modName))
                {
                    SyncrioLog.Debug("Failed to register raw mod handler for " + modName + ", mod already registered");
                    return false;
                }
                SyncrioLog.Debug("Registered raw mod handler for " + modName);
                registeredRawMods.Add(modName, handlerFunction);
            }
            return true;
        }

        /// <summary>
        /// Registers a mod handler function that will be called on every Update.
        /// </summary>
        /// <param name="modName">Mod name.</param>
        /// <param name="handlerFunction">Handler function.</param>
        public bool RegisterUpdateModHandler(string modName, SyncrioMessageCallback handlerFunction)
        {
            lock (eventLock)
            {
                if (registeredUpdateMods.ContainsKey(modName))
                {
                    SyncrioLog.Debug("Failed to register Update mod handler for " + modName + ", mod already registered");
                    return false;
                }
                SyncrioLog.Debug("Registered Update mod handler for " + modName);
                registeredUpdateMods.Add(modName, handlerFunction);
                updateQueue.Add(modName, new Queue<byte[]>());
            }
            return true;
        }

        /// <summary>
        /// Registers a mod handler function that will be called on every FixedUpdate.
        /// </summary>
        /// <param name="modName">Mod name.</param>
        /// <param name="handlerFunction">Handler function.</param>
        public bool RegisterFixedUpdateModHandler(string modName, SyncrioMessageCallback handlerFunction)
        {
            lock (eventLock)
            {
                if (registeredFixedUpdateMods.ContainsKey(modName))
                {
                    SyncrioLog.Debug("Failed to register FixedUpdate mod handler for " + modName + ", mod already registered");
                    return false;
                }
                SyncrioLog.Debug("Registered FixedUpdate mod handler for " + modName);
                registeredFixedUpdateMods.Add(modName, handlerFunction);
                fixedUpdateQueue.Add(modName, new Queue<byte[]>());
            }
            return true;
        }

        /// <summary>
        /// Sends a Syncrio mod message.
        /// </summary>
        /// <param name="modName">Mod name</param>
        /// <param name="messageData">The message payload (MessageWriter can make this easier)</param>
        /// <param name="relay">If set to <c>true</c>, The server will relay the message to all other authenticated clients</param>
        /// <param name="highPriority">If set to <c>true</c>, Syncrio will send this in the high priority queue (Which will send before all vessel updates and screenshots)</param>
        public void SendSyncrioModMessage(string modName, byte[] messageData, bool relay, bool highPriority)
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
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string>(modName);
                mw.Write<bool>(relay);
                mw.Write<bool>(highPriority);
                mw.Write<byte[]>(messageData);
                NetworkWorker.fetch.SendModMessage(mw.GetMessageBytes(), highPriority);
            }
        }

        /// <summary>
        /// Internal use only - Called when a mod message is received from NetworkWorker.
        /// </summary>
        public void HandleModData(byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                string modName = mr.Read<string>();
                byte[] modData = mr.Read<byte[]>();
                OnModMessageReceived(modName, modData);
            }
        }

        /// <summary>
        /// Internal use only
        /// </summary>
        private void OnModMessageReceived(string modName, byte[] modData)
        {
            lock (eventLock)
            {
                if (updateQueue.ContainsKey(modName))
                {
                    updateQueue[modName].Enqueue(modData);
                }

                if (fixedUpdateQueue.ContainsKey(modName))
                {
                   fixedUpdateQueue[modName].Enqueue(modData);
                }

                if (registeredRawMods.ContainsKey(modName))
                {
                    registeredRawMods[modName](modData);
                }
            }
        }

        /// <summary>
        /// Internal use only
        /// </summary>
        private void Update()
        {
            lock (eventLock)
            {
                foreach (KeyValuePair<string, Queue<byte[]>> currentModQueue in updateQueue)
                {
                    while (currentModQueue.Value.Count > 0)
                    {
                        registeredUpdateMods[currentModQueue.Key](currentModQueue.Value.Dequeue());
                    }
                }
            }
        }

        /// <summary>
        /// Internal use only
        /// </summary>
        private void FixedUpdate()
        {
            lock (eventLock)
            {
                foreach (KeyValuePair<string, Queue<byte[]>> currentModQueue in fixedUpdateQueue)
                {
                    while (currentModQueue.Value.Count > 0)
                    {
                        registeredFixedUpdateMods[currentModQueue.Key](currentModQueue.Value.Dequeue());
                    }
                }
            }
        }
    }
}

