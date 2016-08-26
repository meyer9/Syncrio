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
using System.Net;
using MessageStream2;

namespace SyncrioServer
{
    public class BanSystem
    {
        private static BanSystem instance;
        private static string banlistFile;
        private static string ipBanlistFile;
        private static string publicKeyBanlistFile;
        private List<string> bannedNames = new List<string>();
        private List<IPAddress> bannedIPs = new List<IPAddress>();
        private List<string> bannedPublicKeys = new List<string>();

        public BanSystem()
        {
            LoadBans();
        }

        public static BanSystem fetch
        {
            get
            {
                //Lazy loading
                if (instance == null)
                {
                    banlistFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "SyncrioPlayerBans.txt");
                    ipBanlistFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "SyncrioIPBans.txt");
                    publicKeyBanlistFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "SyncrioKeyBans.txt");
                    instance = new BanSystem();
                }
                return instance;
            }
        }

        public void BanPlayer(string commandArgs)
        {
            string playerName = commandArgs;
            string reason = "";

            if (commandArgs.Contains(" "))
            {
                playerName = commandArgs.Substring(0, commandArgs.IndexOf(" "));
                reason = commandArgs.Substring(commandArgs.IndexOf(" ") + 1);
            }

            if (playerName != "")
            {

                ClientObject player = ClientHandler.GetClientByName(playerName);

                if (reason == "")
                {
                    reason = "no reason specified";
                }

                if (player != null)
                {
                    Messages.ConnectionEnd.SendConnectionEnd(player, "You were banned from the server!");
                }

                SyncrioLog.Normal("Player '" + playerName + "' was banned from the server");
                bannedNames.Add(playerName);
                SaveBans();
            }

        }

        public void BanIP(string commandArgs)
        {
            string ip = commandArgs;
            string reason = "";

            if (commandArgs.Contains(" "))
            {
                ip = commandArgs.Substring(0, commandArgs.IndexOf(" "));
                reason = commandArgs.Substring(commandArgs.IndexOf(" ") + 1);
            }

            IPAddress ipAddress;
            if (IPAddress.TryParse(ip, out ipAddress))
            {

                ClientObject player = ClientHandler.GetClientByIP(ipAddress);

                if (player != null)
                {
                    Messages.ConnectionEnd.SendConnectionEnd(player, "You were banned from the server!");
                }
                bannedIPs.Add(ipAddress);
                SaveBans();

                SyncrioLog.Normal("IP Address '" + ip + "' was banned from the server: " + reason);
            }
            else
            {
                SyncrioLog.Normal(ip + " is not a valid IP address");
            }

        }

        public void BanPublicKey(string commandArgs)
        {
            string publicKey = commandArgs;
            string reason = "";

            if (commandArgs.Contains(" "))
            {
                publicKey = commandArgs.Substring(0, commandArgs.IndexOf(" "));
                reason = commandArgs.Substring(commandArgs.IndexOf(" ") + 1);
            }

            ClientObject player = ClientHandler.GetClientByPublicKey(publicKey);

            if (reason == "")
            {
                reason = "no reason specified";
            }

            if (player != null)
            {
                Messages.ConnectionEnd.SendConnectionEnd(player, "You were banned from the server!");
            }
            bannedPublicKeys.Add(publicKey);
            SaveBans();

            SyncrioLog.Normal("Public key '" + publicKey + "' was banned from the server: " + reason);

        }

        public bool IsPlayerNameBanned(string playerName)
        {
            return bannedNames.Contains(playerName);
        }

        public bool IsIPBanned(IPAddress address)
        {
            return bannedIPs.Contains(address);
        }

        public bool IsPublicKeyBanned(string publicKey)
        {
            return bannedPublicKeys.Contains(publicKey);
        }

        public void SaveBans()
        {
            try
            {
                if (File.Exists(banlistFile))
                {
                    File.SetAttributes(banlistFile, FileAttributes.Normal);
                }

                using (StreamWriter sw = new StreamWriter(banlistFile))
                {

                    foreach (string name in bannedNames)
                    {
                        sw.WriteLine("{0}", name);
                    }
                }

                using (StreamWriter sw = new StreamWriter(ipBanlistFile))
                {
                    foreach (IPAddress ip in bannedIPs)
                    {
                        sw.WriteLine("{0}", ip);
                    }
                }

                using (StreamWriter sw = new StreamWriter(publicKeyBanlistFile))
                {
                    foreach (string publicKey in bannedPublicKeys)
                    {
                        sw.WriteLine("{0}", publicKey);
                    }
                }
            }
            catch (Exception e)
            {
                SyncrioLog.Error("Error saving bans!, Exception: " + e);
            }
        }

        public void LoadBans()
        {
            bannedNames.Clear();
            bannedIPs.Clear();
            bannedPublicKeys.Clear();

            if (File.Exists(banlistFile))
            {
                foreach (string line in File.ReadAllLines(banlistFile))
                {
                    if (!bannedNames.Contains(line))
                    {
                        bannedNames.Add(line);
                    }
                }
            }
            else
            {
                File.Create(banlistFile);
            }

            if (File.Exists(ipBanlistFile))
            {
                foreach (string line in File.ReadAllLines(ipBanlistFile))
                {
                    IPAddress banIPAddr = null;
                    if (IPAddress.TryParse(line, out banIPAddr))
                    {
                        if (!bannedIPs.Contains(banIPAddr))
                        {
                            bannedIPs.Add(banIPAddr);
                        }
                    }
                    else
                    {
                        SyncrioLog.Error("Error in IP ban list file, " + line + " is not an IP address");
                    }
                }
            }
            else
            {
                File.Create(ipBanlistFile);
            }

            if (File.Exists(publicKeyBanlistFile))
            {
                foreach (string bannedPublicKey in File.ReadAllLines(publicKeyBanlistFile))
                {
                    if (!bannedPublicKeys.Contains(bannedPublicKey))
                    {
                        bannedPublicKeys.Add(bannedPublicKey);
                    }
                }
            }
            else
            {
                File.Create(publicKeyBanlistFile);
            }
        }
    }
}

