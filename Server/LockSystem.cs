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

namespace SyncrioServer
{
    public class LockSystem
    {
        private static LockSystem instance = new LockSystem();
        private Dictionary<string, string> playerLocks;
        //Lock types
        //control-vessel-(vesselid) - Replaces the old "inUse" messages, the active pilot will have the control-vessel lock.
        //update-vessel-(vesselid) - Replaces the "only the closest player can update a vessel" code, Now you acquire locks to update crafts around you.
        //asteroid-spawn - Held by the player that can spawn asteroids into the game.

        public LockSystem()
        {
            playerLocks = new Dictionary<string, string>();
        }

        public static LockSystem fetch
        {
            get
            {
                return instance;
            }
        }

        public bool AcquireLock(string lockName, string playerName, bool force)
        {
            lock (playerLocks)
            {
                if (force || !playerLocks.ContainsKey(lockName))
                {
                    playerLocks[lockName] = playerName;
                    return true;
                }
                return false;
            }
        }

        public bool ReleaseLock(string lockName, string playerName)
        {
            lock (playerLocks)
            {
                if (playerLocks.ContainsKey(lockName))
                {
                    if (playerLocks[lockName] == playerName)
                    {
                        playerLocks.Remove(lockName);
                        return true;
                    }
                }
                return false;
            }
        }

        public void ReleasePlayerLocks(string playerName)
        {
            lock (playerLocks)
            {
                List<string> removeList = new List<string>();
                foreach (KeyValuePair<string,string> kvp in playerLocks)
                {
                    if (kvp.Value == playerName)
                    {
                        removeList.Add(kvp.Key);
                    }
                }
                foreach (string removeValue in removeList)
                {
                    playerLocks.Remove(removeValue);
                }
            }
        }

        public Dictionary<string,string> GetLockList()
        {
            lock (playerLocks)
            {
                //Return a copy.
                return new Dictionary<string, string>(playerLocks);
            }
        }

        public static void Reset()
        {
            lock (fetch.playerLocks)
            {
                fetch.playerLocks.Clear();
            }
        }
    }
}

