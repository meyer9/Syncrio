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
using UnityEngine;

namespace SyncrioClientSide
{
    //This class rate limits the update frequency, and asks the server to send us less messages
    public class DynamicTickWorker
    {
        public bool workerEnabled;
        private static DynamicTickWorker singleton;
        private float lastDynamicTickRateCheck;
        private const float DYNAMIC_TICK_RATE_CHECK_INTERVAL = 1f;
        //Twiddle these knobs
        private const int MASTER_MIN_TICKS_PER_SECOND = 1;
        private const int MASTER_MAX_TICKS_PER_SECOND = 5;
        private const int MASTER_MIN_SECONDARY_VESSELS = 1;
        private const int MASTER_MAX_SECONDARY_VESSELS = 15;
        //500KB
        private const int MASTER_TICK_SCALING = 100 * 1024;
        //1000KB
        private const int MASTER_SECONDARY_VESSELS_SCALING = 200 * 1024;

        public static DynamicTickWorker fetch
        {
            get
            {
                return singleton;
            }
        }
        //Restricted access variables
        public int maxSecondryVesselsPerTick
        {
            private set;
            get;
        }

        public int sendTickRate
        {
            private set;
            get;
        }

        private void Update()
        {
            if (workerEnabled)
            {
                if ((UnityEngine.Time.realtimeSinceStartup - lastDynamicTickRateCheck) > DYNAMIC_TICK_RATE_CHECK_INTERVAL)
                {
                    lastDynamicTickRateCheck = UnityEngine.Time.realtimeSinceStartup;
                    CalculateRates();
                }
            }
        }

        private void CalculateRates()
        {
            long currentQueuedBytes = NetworkWorker.fetch.GetStatistics("QueuedOutBytes");

            //Tick Rate math - Clamp to minimum value.
            long newTickRate = MASTER_MAX_TICKS_PER_SECOND - (currentQueuedBytes / (MASTER_TICK_SCALING / (MASTER_MAX_TICKS_PER_SECOND - MASTER_MIN_TICKS_PER_SECOND)));
            sendTickRate = newTickRate > MASTER_MIN_TICKS_PER_SECOND ? (int)newTickRate : MASTER_MIN_TICKS_PER_SECOND;

            //Secondary vessel math - Clamp to minimum value
            long newSecondryVesselsPerTick = MASTER_MAX_SECONDARY_VESSELS - (currentQueuedBytes / (MASTER_SECONDARY_VESSELS_SCALING / (MASTER_MAX_SECONDARY_VESSELS - MASTER_MIN_SECONDARY_VESSELS)));
            maxSecondryVesselsPerTick = newSecondryVesselsPerTick > MASTER_MIN_SECONDARY_VESSELS ? (int)newSecondryVesselsPerTick : MASTER_MIN_SECONDARY_VESSELS;
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
                singleton = new DynamicTickWorker();
                singleton.maxSecondryVesselsPerTick = MASTER_MAX_SECONDARY_VESSELS;
                singleton.sendTickRate = MASTER_MAX_TICKS_PER_SECOND;
                Client.updateEvent.Add(singleton.Update);
            }
        }
    }
}