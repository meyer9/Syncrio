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
using System.Diagnostics;

namespace SyncrioClientSide
{
    //The lamest profiler in the world!
    public class Profiler
    {
        public static Stopwatch SyncrioReferenceTime = new Stopwatch();
        public static ProfilerData fixedUpdateData = new ProfilerData();
        public static ProfilerData updateData = new ProfilerData();
        public static ProfilerData guiData = new ProfilerData();
        public long FPS;
    }

    public class ProfilerData
    {
        //Tick time is how long the method takes to run.
        public long tickMinTime = long.MaxValue;
        public long tickMaxTime = long.MinValue;
        public long tickTime;
        List<long> tickHistory = new List<long>();
        public long tickAverage;
        //Delta time is how long it takes inbetween the method runs.
        public long deltaMinTime = long.MaxValue;
        public long deltaMaxTime = long.MinValue;
        public long lastDeltaTime;
        public long deltaTime;
        List<long> deltaHistory = new List<long>();
        public long deltaAverage;

        public void ReportTime(long startClock)
        {
            long currentClock = Profiler.SyncrioReferenceTime.ElapsedTicks;
            tickTime = currentClock - startClock;
            deltaTime = startClock - lastDeltaTime;
            lastDeltaTime = currentClock;
            if (tickTime < tickMinTime)
            {
                tickMinTime = tickTime;
            }
            if (tickTime > tickMaxTime)
            {
                tickMaxTime = tickTime;
            }
            //Ignore the first delta as it will be incorrect on reset.
            if (deltaHistory.Count != 0)
            {
                if (deltaTime < deltaMinTime)
                {
                    deltaMinTime = deltaTime;
                }
                if (deltaTime > deltaMaxTime)
                {
                    deltaMaxTime = deltaTime;
                }
            }
            tickHistory.Add(tickTime);
            if (tickHistory.Count > 300)
            {
                tickHistory.RemoveAt(0);
            }
            tickAverage = 0;
            foreach (long entry in tickHistory)
            {
                tickAverage += entry;
            }
            tickAverage /= tickHistory.Count;
            deltaHistory.Add(deltaTime);
            if (deltaHistory.Count > 300)
            {
                deltaHistory.RemoveAt(0);
            }
            deltaAverage = 0;
            foreach (long entry in deltaHistory)
            {
                deltaAverage += entry;
            }
            deltaAverage /= deltaHistory.Count;
        }

        public override string ToString()
        {
            double tickMS = Math.Round(tickTime / (double)(Stopwatch.Frequency / 1000), 3);
            double tickMinMS = Math.Round(tickMinTime / (double)(Stopwatch.Frequency / 1000), 3);
            double tickMaxMS = Math.Round(tickMaxTime / (double)(Stopwatch.Frequency / 1000), 3);
            double tickAverageMS = Math.Round(tickAverage / (double)(Stopwatch.Frequency / 1000), 3);
            double deltaMS = Math.Round(deltaTime / (double)(Stopwatch.Frequency / 1000), 3);
            double deltaMinMS = Math.Round(deltaMinTime / (double)(Stopwatch.Frequency / 1000), 3);
            double deltaMaxMS = Math.Round(deltaMaxTime / (double)(Stopwatch.Frequency / 1000), 3);
            double deltaAverageMS = Math.Round(deltaAverage / (double)(Stopwatch.Frequency / 1000), 3);
            string returnString = "tick: " + tickMS + " (min/max/avg) " + tickMinMS + "/" + tickMaxMS + "/" + tickAverageMS + "\n";
            returnString += "delta: " + deltaMS + " (min/max/avg) " + deltaMinMS + "/" + deltaMaxMS + "/" + deltaAverageMS + "\n";
            return returnString;
        }
    }
}

