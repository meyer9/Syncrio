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
using SyncrioCommon;
using MessageStream2;

namespace SyncrioServer.Messages
{
    public class ScreenshotLibrary
    {
        private static Dictionary<string, int> playerUploadedScreenshotIndex = new Dictionary<string, int>();
        private static Dictionary<string, Dictionary<string,int>> playerDownloadedScreenshotIndex = new Dictionary<string, Dictionary<string, int>>();
        private static Dictionary<string, string> playerWatchScreenshot = new Dictionary<string, string>();

        public static void HandleScreenshotLibrary(ClientObject client, byte[] messageData)
        {
            string screenshotDirectory = Path.Combine(Server.ScenarioDirectory, "Screenshots");
            if (Settings.settingsStore.screenshotDirectory != "")
            {
                if (Directory.Exists(Settings.settingsStore.screenshotDirectory))
                {
                    screenshotDirectory = Settings.settingsStore.screenshotDirectory;
                }
            }
            if (!Directory.Exists(screenshotDirectory))
            {
                Directory.CreateDirectory(screenshotDirectory);
            }
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.SCREENSHOT_LIBRARY;
            using (MessageReader mr = new MessageReader(messageData))
            {
                ScreenshotMessageType messageType = (ScreenshotMessageType)mr.Read<int>();
                string fromPlayer = mr.Read<string>();
                switch (messageType)
                {
                    case ScreenshotMessageType.SCREENSHOT:
                        {
                            if (Settings.settingsStore.screenshotsPerPlayer > -1)
                            {
                                string playerScreenshotDirectory = Path.Combine(screenshotDirectory, fromPlayer);
                                if (!Directory.Exists(playerScreenshotDirectory))
                                {
                                    Directory.CreateDirectory(playerScreenshotDirectory);
                                }
                                string screenshotFile = Path.Combine(playerScreenshotDirectory, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".png");
                                SyncrioLog.Debug("Saving screenshot from " + fromPlayer);

                                byte[] screenshotData = mr.Read<byte[]>();

                                File.WriteAllBytes(screenshotFile, screenshotData);
                                if (Settings.settingsStore.screenshotsPerPlayer != 0)
                                {
                                    while (Directory.GetFiles(playerScreenshotDirectory).Length > Settings.settingsStore.screenshotsPerPlayer)
                                    {
                                        string[] currentFiles = Directory.GetFiles(playerScreenshotDirectory);
                                        string deleteFile = currentFiles[0];
                                        //Find oldest file
                                        foreach (string testFile in currentFiles)
                                        {
                                            if (File.GetCreationTime(testFile) < File.GetCreationTime(deleteFile))
                                            {
                                                deleteFile = testFile;
                                            }
                                        }
                                        File.Delete(deleteFile);
                                        SyncrioLog.Debug("Removing old screenshot " + Path.GetFileName(deleteFile));
                                    }
                                }

                                //Notify players that aren't watching that there's a new screenshot availabe. This only works if there's a file available on the server.
                                //The server does not keep the screenshots in memory.
                                ServerMessage notifyMessage = new ServerMessage();
                                notifyMessage.type = ServerMessageType.SCREENSHOT_LIBRARY;
                                using (MessageWriter mw = new MessageWriter())
                                {
                                    mw.Write<int>((int)ScreenshotMessageType.NOTIFY);
                                    mw.Write(fromPlayer);
                                    notifyMessage.data = mw.GetMessageBytes();
                                    ClientHandler.SendToAll(client, notifyMessage, false);
                                }
                            }
                            if (!playerUploadedScreenshotIndex.ContainsKey(fromPlayer))
                            {
                                playerUploadedScreenshotIndex.Add(fromPlayer, 0);
                            }
                            else
                            {
                                playerUploadedScreenshotIndex[fromPlayer]++;
                            }
                            if (!playerDownloadedScreenshotIndex.ContainsKey(fromPlayer))
                            {
                                playerDownloadedScreenshotIndex.Add(fromPlayer, new Dictionary<string, int>());
                            }
                            if (!playerDownloadedScreenshotIndex[fromPlayer].ContainsKey(fromPlayer))
                            {
                                playerDownloadedScreenshotIndex[fromPlayer].Add(fromPlayer, playerUploadedScreenshotIndex[fromPlayer]);
                            }
                            else
                            {
                                playerDownloadedScreenshotIndex[fromPlayer][fromPlayer] = playerUploadedScreenshotIndex[fromPlayer];
                            }
                            newMessage.data = messageData;
                            foreach (KeyValuePair<string, string> entry in playerWatchScreenshot)
                            {
                                if (entry.Key != fromPlayer)
                                {
                                    if (entry.Value == fromPlayer && entry.Key != client.playerName)
                                    {
                                        ClientObject toClient = ClientHandler.GetClientByName(entry.Key);
                                        if (toClient != null && toClient != client)
                                        {
                                            if (!playerDownloadedScreenshotIndex.ContainsKey(entry.Key))
                                            {
                                                playerDownloadedScreenshotIndex.Add(entry.Key, new Dictionary<string, int>());
                                            }
                                            if (!playerDownloadedScreenshotIndex[entry.Key].ContainsKey(fromPlayer))
                                            {
                                                playerDownloadedScreenshotIndex[entry.Key].Add(fromPlayer, 0);
                                            }
                                            playerDownloadedScreenshotIndex[entry.Key][fromPlayer] = playerUploadedScreenshotIndex[fromPlayer];
                                            SyncrioLog.Debug("Sending screenshot from " + fromPlayer + " to " + entry.Key);
                                            using (MessageWriter mw = new MessageWriter())
                                            {
                                                ServerMessage sendStartMessage = new ServerMessage();
                                                sendStartMessage.type = ServerMessageType.SCREENSHOT_LIBRARY;
                                                mw.Write<int>((int)ScreenshotMessageType.SEND_START_NOTIFY);
                                                mw.Write<string>(fromPlayer);
                                                sendStartMessage.data = mw.GetMessageBytes();
                                                ClientHandler.SendToClient(toClient, sendStartMessage, true);
                                            }
                                            ClientHandler.SendToClient(toClient, newMessage, false);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case ScreenshotMessageType.WATCH:
                        {
                            newMessage.data = messageData;
                            string watchPlayer = mr.Read<string>();
                            if (watchPlayer == "")
                            {
                                if (playerWatchScreenshot.ContainsKey(fromPlayer))
                                {
                                    SyncrioLog.Debug(fromPlayer + " is no longer watching screenshots from " + playerWatchScreenshot[fromPlayer]);
                                    playerWatchScreenshot.Remove(fromPlayer);
                                }
                            }
                            else
                            {
                                SyncrioLog.Debug(fromPlayer + " is watching screenshots from " + watchPlayer);
                                playerWatchScreenshot[fromPlayer] = watchPlayer;
                                if (!playerDownloadedScreenshotIndex.ContainsKey(fromPlayer))
                                {
                                    playerDownloadedScreenshotIndex.Add(fromPlayer, new Dictionary<string, int>());
                                }
                                string watchPlayerScreenshotDirectory = Path.Combine(screenshotDirectory, watchPlayer);
                                //Find latest screenshot
                                string sendFile = null;
                                if (Directory.Exists(watchPlayerScreenshotDirectory))
                                {
                                    string[] playerScreenshots = Directory.GetFiles(watchPlayerScreenshotDirectory);
                                    if (playerScreenshots.Length > 0)
                                    {
                                        sendFile = playerScreenshots[0];
                                        foreach (string testFile in playerScreenshots)
                                        {
                                            if (File.GetCreationTime(testFile) > File.GetCreationTime(sendFile))
                                            {
                                                sendFile = testFile;
                                            }
                                        }
                                        if (!playerUploadedScreenshotIndex.ContainsKey(watchPlayer))
                                        {
                                            playerUploadedScreenshotIndex.Add(watchPlayer, 0);
                                        }
                                    }
                                }
                                //Send screenshot if needed
                                if (sendFile != null)
                                {
                                    bool sendScreenshot = false;
                                    if (!playerDownloadedScreenshotIndex[fromPlayer].ContainsKey(watchPlayer))
                                    {
                                        playerDownloadedScreenshotIndex[fromPlayer].Add(watchPlayer, playerUploadedScreenshotIndex[watchPlayer]);
                                        sendScreenshot = true;
                                    }
                                    else
                                    {
                                        if (playerDownloadedScreenshotIndex[fromPlayer][watchPlayer] != playerUploadedScreenshotIndex[watchPlayer])
                                        {
                                            sendScreenshot = true;
                                            playerDownloadedScreenshotIndex[fromPlayer][watchPlayer] = playerUploadedScreenshotIndex[watchPlayer];
                                        }
                                    }
                                    if (sendScreenshot)
                                    {
                                        ServerMessage sendStartMessage = new ServerMessage();
                                        sendStartMessage.type = ServerMessageType.SCREENSHOT_LIBRARY;
                                        using (MessageWriter mw = new MessageWriter())
                                        {
                                            mw.Write<int>((int)ScreenshotMessageType.SEND_START_NOTIFY);
                                            mw.Write<string>(fromPlayer);
                                            sendStartMessage.data = mw.GetMessageBytes();
                                        }
                                        ServerMessage screenshotMessage = new ServerMessage();
                                        screenshotMessage.type = ServerMessageType.SCREENSHOT_LIBRARY;
                                        using (MessageWriter mw = new MessageWriter())
                                        {
                                            mw.Write<int>((int)ScreenshotMessageType.SCREENSHOT);
                                            mw.Write<string>(watchPlayer);
                                            mw.Write<byte[]>(File.ReadAllBytes(sendFile));
                                            screenshotMessage.data = mw.GetMessageBytes();
                                        }
                                        ClientObject toClient = ClientHandler.GetClientByName(fromPlayer);
                                        if (toClient != null)
                                        {
                                            SyncrioLog.Debug("Sending saved screenshot from " + watchPlayer + " to " + fromPlayer);
                                            ClientHandler.SendToClient(toClient, sendStartMessage, false);
                                            ClientHandler.SendToClient(toClient, screenshotMessage, false);
                                        }
                                    }
                                }
                            }
                            //Relay the message
                            ClientHandler.SendToAll(client, newMessage, false);
                        }
                        break;
                }
            }
        }

        public static void RemovePlayer(string playerName)
        {
            if (playerDownloadedScreenshotIndex.ContainsKey(playerName))
            {
                playerDownloadedScreenshotIndex.Remove(playerName);
            }
            if (playerUploadedScreenshotIndex.ContainsKey(playerName))
            {
                playerUploadedScreenshotIndex.Remove(playerName);
            }
            if (playerWatchScreenshot.ContainsKey(playerName))
            {
                playerWatchScreenshot.Remove(playerName);
            }
        }

        public static void Reset()
        {
            playerUploadedScreenshotIndex.Clear();
            playerDownloadedScreenshotIndex.Clear();
            playerWatchScreenshot.Clear();
        }
    }
}

