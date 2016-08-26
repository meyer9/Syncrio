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
using MessageStream2;
using SyncrioCommon;
using UnityEngine;

namespace SyncrioClientSide
{
    public class FlagSyncer
    {
        //Singleton
        private static FlagSyncer singleton;
        //Public
        public bool workerEnabled;
        public bool flagChangeEvent;
        public bool syncComplete;
        //Private
        private string flagPath;
        private Dictionary<string, FlagInfo> serverFlags = new Dictionary<string, FlagInfo>();
        private Queue<FlagRespondMessage> newFlags = new Queue<FlagRespondMessage>();

        public FlagSyncer()
        {
            flagPath = Path.Combine(Path.Combine(Path.Combine(KSPUtil.ApplicationRootPath, "GameData"), "Syncrio"), "Flags");
        }

        public static FlagSyncer fetch
        {
            get
            {
                return singleton;
            }
        }

        public void SendFlagList()
        {
            string[] SyncrioFlags = Directory.GetFiles(flagPath);
            string[] SyncrioSha = new string[SyncrioFlags.Length];
            for (int i=0; i < SyncrioFlags.Length; i++)
            {
                SyncrioSha[i] = Common.CalculateSHA256Hash(SyncrioFlags[i]);
                SyncrioFlags[i] = Path.GetFileName(SyncrioFlags[i]);
            }
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)FlagMessageType.LIST);
                mw.Write<string>(Settings.fetch.playerName);
                mw.Write<string[]>(SyncrioFlags);
                mw.Write<string[]>(SyncrioSha);
                NetworkWorker.fetch.SendFlagMessage(mw.GetMessageBytes());
            }
        }

        public void HandleMessage(byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                FlagMessageType messageType = (FlagMessageType)mr.Read<int>();
                switch (messageType)
                {
                    case FlagMessageType.LIST:
                        {
                            //List code
                            string[] serverFlagFiles = mr.Read<string[]>();
                            string[] serverFlagOwners = mr.Read<string[]>();
                            string[] serverFlagShaSums = mr.Read<string[]>();
                            for (int i = 0; i < serverFlagFiles.Length; i++)
                            {
                                FlagInfo fi = new FlagInfo();
                                fi.owner = serverFlagOwners[i];
                                fi.shaSum = serverFlagShaSums[i];
                                serverFlags[Path.GetFileNameWithoutExtension(serverFlagFiles[i])] = fi;
                            }
                            syncComplete = true;
                            //Check if we need to upload the flag
                            flagChangeEvent = true;
                        }
                        break;
                    case FlagMessageType.FLAG_DATA:
                        {
                            FlagRespondMessage frm = new FlagRespondMessage();
                            frm.flagInfo.owner = mr.Read<string>();
                            frm.flagName = mr.Read<string>();
                            frm.flagData = mr.Read<byte[]>();
                            frm.flagInfo.shaSum = Common.CalculateSHA256Hash(frm.flagData);
                            newFlags.Enqueue(frm);
                        }
                        break;
                    case FlagMessageType.DELETE_FILE:
                        {
                            string flagName = mr.Read<string>();
                            string flagFile = Path.Combine(flagPath, flagName);
                            if (File.Exists(flagFile))
                            {
                                try
                                {

                                    if (File.Exists(flagFile))
                                    {
                                        SyncrioLog.Debug("Deleting flag " + flagFile);
                                        File.Delete(flagFile);
                                    }
                                }
                                catch (Exception e)
                                {
                                    SyncrioLog.Debug("Error deleting flag " + flagFile + ", exception: " + e);
                                }
                            }
                        }
                        break;
                }
            }
        }

        private void Update()
        {
            if (workerEnabled && syncComplete && (HighLogic.CurrentGame != null ? HighLogic.CurrentGame.flagURL != null : false))
            {
                if (flagChangeEvent)
                {
                    flagChangeEvent = false;
                    HandleFlagChangeEvent();
                }
                while (newFlags.Count > 0)
                {
                    HandleFlagRespondMessage(newFlags.Dequeue());
                }
            }
        }

        private void HandleFlagChangeEvent()
        {
            string flagURL = HighLogic.CurrentGame.flagURL;
            if (!flagURL.ToLower().StartsWith("Syncrio/flags/"))
            {
                //If it's not a Syncrio flag don't sync it.
                return;
            }
            string flagName = flagURL.Substring("Syncrio/Flags/".Length);
            if (serverFlags.ContainsKey(flagName) ? serverFlags[flagName].owner != Settings.fetch.playerName : false)
            {
                //If the flag is owned by someone else don't sync it
                return;
            }
            string flagFile = "";

            string[] flagFiles = Directory.GetFiles(flagPath, "*", SearchOption.TopDirectoryOnly);
            foreach (string possibleMatch in flagFiles)
            {
                if (flagName.ToLower() == Path.GetFileNameWithoutExtension(possibleMatch).ToLower())
                {
                    flagFile = possibleMatch;
                }
            }
            //Sanity check to make sure we found the file
            if (flagFile != "" ? File.Exists(flagFile) : false)
            {
                string shaSum = Common.CalculateSHA256Hash(flagFile);
                if (serverFlags.ContainsKey(flagName) ? serverFlags[flagName].shaSum == shaSum : false)
                {
                    //Don't send the flag when the SHA sum already matches
                    return;
                }
                SyncrioLog.Debug("Uploading " + Path.GetFileName(flagFile));
                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<int>((int)FlagMessageType.UPLOAD_FILE);
                    mw.Write<string>(Settings.fetch.playerName);
                    mw.Write<string>(Path.GetFileName(flagFile));
                    mw.Write<byte[]>(File.ReadAllBytes(flagFile));
                    NetworkWorker.fetch.SendFlagMessage(mw.GetMessageBytes());
                }
                FlagInfo fi = new FlagInfo();
                fi.owner = Settings.fetch.playerName;
                fi.shaSum = Common.CalculateSHA256Hash(flagFile);
                serverFlags[flagName] = fi;
            }

        }

        private void HandleFlagRespondMessage(FlagRespondMessage flagRespondMessage)
        {
            serverFlags[flagRespondMessage.flagName] = flagRespondMessage.flagInfo;
            string flagFile = Path.Combine(flagPath, flagRespondMessage.flagName);
            Texture2D flagTexture = new Texture2D(4, 4);
            if (flagTexture.LoadImage(flagRespondMessage.flagData))
            {
                flagTexture.name = "Syncrio/Flags/" + Path.GetFileNameWithoutExtension(flagRespondMessage.flagName);
                File.WriteAllBytes(flagFile, flagRespondMessage.flagData);
                GameDatabase.TextureInfo ti = new GameDatabase.TextureInfo(null, flagTexture, false, true, false);
                ti.name = flagTexture.name;
                bool containsTexture = false;
                foreach (GameDatabase.TextureInfo databaseTi in GameDatabase.Instance.databaseTexture)
                {
                    if (databaseTi.name == ti.name)
                    {
                        containsTexture = true;
                    }
                }
                if (!containsTexture)
                {
                    GameDatabase.Instance.databaseTexture.Add(ti);
                }
                else
                {
                    GameDatabase.Instance.ReplaceTexture(ti.name, ti);
                }
                SyncrioLog.Debug("Loaded " + flagTexture.name);
            }
            else
            {
                SyncrioLog.Debug("Failed to load flag " + flagRespondMessage.flagName);
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
                singleton = new FlagSyncer();
                Client.updateEvent.Add(singleton.Update);
            }
        }
    }

    public class FlagRespondMessage
    {
        public string flagName;
        public byte[] flagData;
        public FlagInfo flagInfo = new FlagInfo();
    }

    public class FlagInfo
    {
        public string shaSum;
        public string owner;
    }
}

