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
using System.IO;

namespace SyncrioServer
{
    public class AdminSystem
    {
        private static AdminSystem instance;
        private static string adminListFile;
        private List<string> serverAdmins = new List<string>();

        public static AdminSystem fetch
        {
            get
            {
                //Lazy loading
                if (instance == null)
                {
                    adminListFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "SyncrioAdmins.txt");
                    instance = new AdminSystem();
                    instance.LoadAdmins();
                }
                return instance;
            }
        }

        private void LoadAdmins()
        {
            SyncrioLog.Debug("Loading admins");
            serverAdmins.Clear();

            if (File.Exists(adminListFile))
            {
                serverAdmins.AddRange(File.ReadAllLines(adminListFile));
            }
            else
            {
                SaveAdmins();
            }
        }

        private void SaveAdmins()
        {
            SyncrioLog.Debug("Saving admins");
            try
            {
                if (File.Exists(adminListFile))
                {
                    File.SetAttributes(adminListFile, FileAttributes.Normal);
                }

                using (StreamWriter sw = new StreamWriter(adminListFile))
                {
                    foreach (string user in serverAdmins)
                    {
                        sw.WriteLine(user);
                    }
                }
            }
            catch (Exception e)
            {
                SyncrioLog.Error("Error saving admin list!, Exception: " + e);
            }
        }

        public void AddAdmin(string playerName)
        {
            lock (serverAdmins)
            {
                if (!serverAdmins.Contains(playerName))
                {
                    serverAdmins.Add(playerName);
                    SaveAdmins();
                }
            }
        }

        public void RemoveAdmin(string playerName)
        {
            lock (serverAdmins)
            {
                if (serverAdmins.Contains(playerName))
                {
                    serverAdmins.Remove(playerName);
                    SaveAdmins();
                }
            }
        }

        public bool IsAdmin(string playerName)
        {
            lock (serverAdmins)
            {
                return serverAdmins.Contains(playerName);
            }
        }

        public string[] GetAdmins()
        {
            lock (serverAdmins)
            {
                return serverAdmins.ToArray();
            }
        }
    }
}

