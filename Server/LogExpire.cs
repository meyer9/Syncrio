﻿/*   Syncrio License
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
using System.IO;

namespace SyncrioServer
{
    public class LogExpire
    {
        private static string logDirectory
        {
            get
            {
                return Path.Combine(Server.ScenarioDirectory, SyncrioLog.LogFolder);
            }
        }

        public static void ExpireLogs()
        {
            if (!Directory.Exists(logDirectory))
            {
                //Screenshot directory is missing so there will be no screenshots to delete.
                return;
            }
            string[] logFiles = Directory.GetFiles(logDirectory);
            foreach (string logFile in logFiles)
            {
                //Check if the expireScreenshots setting is enabled
                if (Settings.settingsStore.expireLogs > 0)
                {
                    //If the file is older than a day, delete it
                    if (File.GetCreationTime(logFile).AddDays(Settings.settingsStore.expireLogs) < DateTime.Now)
                    {
                        SyncrioLog.Debug("Deleting saved log '" + logFile + "', reason: Expired!");
                        try
                        {
                            File.Delete(logFile);
                        }
                        catch (Exception e)
                        {
                            SyncrioLog.Error("Exception while trying to delete '" + logFile + "'!, Exception: " + e.Message);
                        }
                    }
                }
            }
        }
    }
}