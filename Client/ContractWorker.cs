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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncrioClientSide
{
    class ContractWorker
    {
        //singleton
        private static ContractWorker singleton;
        public bool workerEnabled;
        private bool sentRequest = false;
        private float lastRequest = 0.0f;
        private float requestWaitTime = 1.0f;

        public static ContractWorker fetch
        {
            get
            {
                return singleton;
            }
        }

        private void Update()
        {
            if (workerEnabled)
            {
                if (GroupSystem.playerGroupAssigned)
                {
                    string groupName = GroupSystem.playerGroupName;
                    if (!string.IsNullOrEmpty(groupName))
                    {
                        //Try to acquire the contract-spawn lock for our group if nobody else has it.
                        if (!LockSystem.fetch.LockExists("contract-spawn-" + groupName) && !sentRequest)
                        {
                            sentRequest = true;
                            lastRequest = UnityEngine.Time.realtimeSinceStartup;

                            LockSystem.fetch.AcquireLock("contract-spawn-" + groupName, false);
                        }

                        if (sentRequest)
                        {
                            if (UnityEngine.Time.realtimeSinceStartup > lastRequest + requestWaitTime)
                            {
                                sentRequest = false;
                            }
                        }
                    }
                }
            }
        }

        public static void Reset()
        {
            lock (Client.eventLock)
            {
                if (singleton != null)
                {
                    singleton.workerEnabled = false;
                    Client.updateEvent.Remove(singleton.Update);
                }
                singleton = new ContractWorker();
                Client.updateEvent.Add(singleton.Update);
            }
        }
    }
}
