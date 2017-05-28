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
    public delegate void AcquireEvent(string playerName,string lockName,bool lockResult);
    public delegate void ReleaseEvent(string playerName,string lockName);
    public class LockSystem
    {
        private static LockSystem singleton;
        private Dictionary<string, string> serverLocks = new Dictionary<string, string>();
        private List<AcquireEvent> lockAcquireEvents = new List<AcquireEvent>();
        private List<ReleaseEvent> lockReleaseEvents = new List<ReleaseEvent>();
        private Dictionary<string, double> lastAcquireTime = new Dictionary<string, double>();
        private object lockObject = new object();

        public static LockSystem fetch
        {
            get
            {
                return singleton;
            }
        }

        public void AcquireLock(string lockName, bool force)
        {
            lock (lockObject)
            {
                using (MessageWriter mw = new MessageWriter())
                {
                    SyncrioLog.Debug("Acquiring '" + lockName + "' lock");
                    mw.Write<int>((int)LockMessageType.ACQUIRE);
                    mw.Write<string>(Settings.fetch.playerName);
                    mw.Write<string>(lockName);
                    mw.Write<bool>(force);
                    NetworkWorker.fetch.SendLockSystemMessage(mw.GetMessageBytes());
                }
            }
        }

        public void ReleaseLock(string lockName)
        {
            lock (lockObject)
            {
                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<int>((int)LockMessageType.RELEASE);
                    mw.Write<string>(Settings.fetch.playerName);
                    mw.Write<string>(lockName);
                    NetworkWorker.fetch.SendLockSystemMessage(mw.GetMessageBytes());
                }
                if (LockIsOurs(lockName))
                {
                    serverLocks.Remove(lockName);
                }
            }
        }

        public void ReleasePlayerLocks(string playerName)
        {
            lock (lockObject)
            {
                List<string> removeList = new List<string>();
                foreach (KeyValuePair<string,string> kvp in serverLocks)
                {
                    if (kvp.Value == playerName)
                    {
                        removeList.Add(kvp.Key);
                    }
                }
                foreach (string removeValue in removeList)
                {
                    serverLocks.Remove(removeValue);
                    FireReleaseEvent(playerName, removeValue);
                }
            }
        }

        public void ReleasePlayerLocksWithPrefix(string playerName, string prefix)
        {
            SyncrioLog.Debug("Releasing lock with prefix " + prefix + " for " + playerName);
            lock (lockObject)
            {
                List<string> removeList = new List<string>();
                foreach (KeyValuePair<string,string> kvp in serverLocks)
                {
                    if (kvp.Key.StartsWith(prefix) && kvp.Value == playerName)
                    {
                        removeList.Add(kvp.Key);
                    }
                }
                foreach (string removeValue in removeList)
                {
                    if (playerName == Settings.fetch.playerName)
                    {
                        SyncrioLog.Debug("Releasing lock " + removeValue);
                        ReleaseLock(removeValue);
                    }
                    else
                    {
                        serverLocks.Remove(removeValue);
                        FireReleaseEvent(playerName, removeValue);
                    }
                }
            }
        }

        public void HandleLockMessage(byte[] messageData)
        {
            lock (lockObject)
            {
                using (MessageReader mr = new MessageReader(messageData))
                {
                    LockMessageType lockMessageType = (LockMessageType)mr.Read<int>();
                    switch (lockMessageType)
                    {
                        case LockMessageType.LIST:
                            {
                                //We shouldn't need to clear this as LIST is only sent once, but better safe than sorry.
                                serverLocks.Clear();
                                string[] lockKeys = mr.Read<string[]>();
                                string[] lockValues = mr.Read<string[]>();
                                for (int i = 0; i < lockKeys.Length; i++)
                                {
                                    serverLocks.Add(lockKeys[i], lockValues[i]);
                                }
                            }
                            break;
                        case LockMessageType.ACQUIRE:
                            {
                                string playerName = mr.Read<string>();
                                string lockName = mr.Read<string>();
                                bool lockResult = mr.Read<bool>();
                                if (lockResult)
                                {
                                    serverLocks[lockName] = playerName;
                                }
                                FireAcquireEvent(playerName, lockName, lockResult);
                            }
                            break;
                        case LockMessageType.RELEASE:
                            {
                                string playerName = mr.Read<string>();
                                string lockName = mr.Read<string>();
                                if (serverLocks.ContainsKey(lockName))
                                {
                                    serverLocks.Remove(lockName);
                                }
                                FireReleaseEvent(playerName, lockName);
                            }
                            break;
                    }
                }
            }
        }

        public void RegisterAcquireHook(AcquireEvent methodObject)
        {
            lockAcquireEvents.Add(methodObject);
        }

        public void UnregisterAcquireHook(AcquireEvent methodObject)
        {
            if (lockAcquireEvents.Contains(methodObject))
            {
                lockAcquireEvents.Remove(methodObject);
            }
        }

        public void RegisterReleaseHook(ReleaseEvent methodObject)
        {
            lockReleaseEvents.Add(methodObject);
        }

        public void UnregisterReleaseHook(ReleaseEvent methodObject)
        {
            if (lockReleaseEvents.Contains(methodObject))
            {
                lockReleaseEvents.Remove(methodObject);
            }
        }

        private void FireAcquireEvent(string playerName, string lockName, bool lockResult)
        {
            foreach (AcquireEvent methodObject in lockAcquireEvents)
            {
                try
                {
                    methodObject(playerName, lockName, lockResult);
                }
                catch (Exception e)
                {
                    SyncrioLog.Debug("Error thrown in acquire lock event, exception " + e);
                }
            }
        }

        private void FireReleaseEvent(string playerName, string lockName)
        {
            foreach (ReleaseEvent methodObject in lockReleaseEvents)
            {
                try
                {
                    methodObject(playerName, lockName);
                }
                catch (Exception e)
                {
                    SyncrioLog.Debug("Error thrown in release lock event, exception " + e);
                }
            } 
        }

        public bool LockIsOurs(string lockName)
        {
            lock (lockObject)
            {
                if (serverLocks.ContainsKey(lockName))
                {
                    if (serverLocks[lockName] == Settings.fetch.playerName)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool LockExists(string lockName)
        {
            lock (lockObject)
            {
                return serverLocks.ContainsKey(lockName);
            }
        }

        public string LockOwner(string lockName)
        {
            lock (lockObject)
            {
                if (serverLocks.ContainsKey(lockName))
                {
                    return serverLocks[lockName];
                }
                return "";
            }
        }

        public static void Reset()
        {
            lock (Client.eventLock)
            {
                singleton = new LockSystem();
            }
        }
    }
}

