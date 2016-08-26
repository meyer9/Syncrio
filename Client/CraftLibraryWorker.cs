/*   Syncrio License
 *   
 *   Copyright � 2016 Caleb Huyck
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
using UnityEngine;
using SyncrioCommon;
using System.IO;
using MessageStream2;

namespace SyncrioClientSide
{
    public class CraftLibraryWorker
    {
        //Public
        private static CraftLibraryWorker singleton;
        public bool display;
        public bool workerEnabled;
        //Private
        private Queue<CraftChangeEntry> craftAddQueue = new Queue<CraftChangeEntry>();
        private Queue<CraftChangeEntry> craftDeleteQueue = new Queue<CraftChangeEntry>();
        private Queue<CraftResponseEntry> craftResponseQueue = new Queue<CraftResponseEntry>();
        private bool safeDisplay;
        private bool initialized;
        private bool showUpload;
        private bool isWindowLocked = false;
        private string selectedPlayer;
        private List<string> playersWithCrafts = new List<string>();
        //Player -> Craft type -> Craft name
        private Dictionary<string, Dictionary<CraftType, List<string>>> playerList = new Dictionary<string, Dictionary<CraftType, List<string>>>();
        //Craft type -> Craft name
        private Dictionary<CraftType, List<string>> uploadList = new Dictionary<CraftType, List<string>>();
        //GUI Layout
        private Rect playerWindowRect;
        private Rect libraryWindowRect;
        private Rect moveRect;
        private GUILayoutOption[] playerLayoutOptions;
        private GUILayoutOption[] libraryLayoutOptions;
        private GUILayoutOption[] textAreaOptions;
        private GUIStyle windowStyle;
        private GUIStyle buttonStyle;
        private GUIStyle labelStyle;
        private GUIStyle scrollStyle;
        private Vector2 playerScrollPos;
        private Vector2 libraryScrollPos;
        //save paths
        private string savePath;
        private string vabPath;
        private string sphPath;
        private string subassemblyPath;
        //upload event
        private CraftType uploadCraftType;
        private string uploadCraftName;
        //download event
        private CraftType downloadCraftType;
        private string downloadCraftName;
        //delete event
        private CraftType deleteCraftType;
        private string deleteCraftName;
        //Screen message
        private bool displayCraftUploadingMessage = false;
        public bool finishedUploadingCraft = false;
        private float lastCraftMessageCheck;
        ScreenMessage craftUploadMessage;
        //const
        private const float PLAYER_WINDOW_HEIGHT = 300;
        private const float PLAYER_WINDOW_WIDTH = 200;
        private const float LIBRARY_WINDOW_HEIGHT = 400;
        private const float LIBRARY_WINDOW_WIDTH = 300;
        private const float CRAFT_MESSAGE_CHECK_INTERVAL = 0.2f;

        public CraftLibraryWorker()
        {
            savePath = Path.Combine(Path.Combine(KSPUtil.ApplicationRootPath, "saves"), "Syncrio");
            vabPath = Path.Combine(Path.Combine(savePath, "Ships"), "VAB");
            sphPath = Path.Combine(Path.Combine(savePath, "Ships"), "SPH");
            subassemblyPath = Path.Combine(savePath, "Subassemblies");
        }

        public static CraftLibraryWorker fetch
        {
            get
            {
                return singleton;
            }
        }

        private void Update()
        {
            safeDisplay = display;
            if (workerEnabled)
            {
                while (craftAddQueue.Count > 0)
                {
                    CraftChangeEntry cce = craftAddQueue.Dequeue();
                    AddCraftEntry(cce.playerName, cce.craftType, cce.craftName);
                }

                while (craftDeleteQueue.Count > 0)
                {
                    CraftChangeEntry cce = craftDeleteQueue.Dequeue();
                    DeleteCraftEntry(cce.playerName, cce.craftType, cce.craftName);
                }

                while (craftResponseQueue.Count > 0)
                {
                    CraftResponseEntry cre = craftResponseQueue.Dequeue();
                    SaveCraftFile(cre.craftType, cre.craftName, cre.craftData);
                }

                if (uploadCraftName != null)
                {
                    UploadCraftFile(uploadCraftType, uploadCraftName);
                    uploadCraftName = null;
                    uploadCraftType = CraftType.VAB;
                }

                if (downloadCraftName != null)
                {
                    DownloadCraftFile(selectedPlayer, downloadCraftType, downloadCraftName);
                    downloadCraftName = null;
                    downloadCraftType = CraftType.VAB;
                }

                if (deleteCraftName != null)
                {
                    DeleteCraftEntry(Settings.fetch.playerName, deleteCraftType, deleteCraftName);
                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<int>((int)CraftMessageType.DELETE_FILE);
                        mw.Write<string>(Settings.fetch.playerName);
                        mw.Write<int>((int)deleteCraftType);
                        mw.Write<string>(deleteCraftName);
                        NetworkWorker.fetch.SendCraftLibraryMessage(mw.GetMessageBytes());
                    }
                    deleteCraftName = null;
                    deleteCraftType = CraftType.VAB;
                }

                if (displayCraftUploadingMessage && ((UnityEngine.Time.realtimeSinceStartup - lastCraftMessageCheck) > CRAFT_MESSAGE_CHECK_INTERVAL))
                {
                    lastCraftMessageCheck = UnityEngine.Time.realtimeSinceStartup;
                    if (craftUploadMessage != null)
                    {
                        craftUploadMessage.duration = 0f;
                    }
                    if (finishedUploadingCraft)
                    {
                        displayCraftUploadingMessage = false;
                        craftUploadMessage = ScreenMessages.PostScreenMessage("Craft uploaded!", 2f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    else
                    {
                        craftUploadMessage = ScreenMessages.PostScreenMessage("Uploading craft...", 1f, ScreenMessageStyle.UPPER_CENTER);
                    }
                }

            }
        }

        private void UploadCraftFile(CraftType type, string name)
        {
            string uploadPath = "";
            switch (uploadCraftType)
            {
                case CraftType.VAB:
                    uploadPath = vabPath;
                    break;
                case CraftType.SPH:
                    uploadPath = sphPath;
                    break;
                case CraftType.SUBASSEMBLY:
                    uploadPath = subassemblyPath;
                    break;
            }
            string filePath = Path.Combine(uploadPath, name + ".craft");
            if (File.Exists(filePath))
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<int>((int)CraftMessageType.UPLOAD_FILE);
                    mw.Write<string>(Settings.fetch.playerName);
                    mw.Write<int>((int)type);
                    mw.Write<string>(name);
                    mw.Write<byte[]>(fileData);
                    NetworkWorker.fetch.SendCraftLibraryMessage(mw.GetMessageBytes());
                    AddCraftEntry(Settings.fetch.playerName, uploadCraftType, uploadCraftName);
                    displayCraftUploadingMessage = true;
                }
            }
            else
            {
                SyncrioLog.Debug("Cannot upload file, " + filePath + " does not exist!");
            }

        }

        private void DownloadCraftFile(string playerName, CraftType craftType, string craftName)
        {

            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)CraftMessageType.REQUEST_FILE);
                mw.Write<string>(Settings.fetch.playerName);
                mw.Write<string>(playerName);
                mw.Write<int>((int)craftType);
                mw.Write<string>(craftName);
                NetworkWorker.fetch.SendCraftLibraryMessage(mw.GetMessageBytes());
            }
        }

        private void AddCraftEntry(string playerName, CraftType craftType, string craftName)
        {
            if (!playersWithCrafts.Contains(playerName))
            {
                playersWithCrafts.Add(playerName);
            }
            if (!playerList.ContainsKey(playerName))
            {
                playerList.Add(playerName, new Dictionary<CraftType, List<string>>());
            }
            if (!playerList[playerName].ContainsKey(craftType))
            {
                playerList[playerName].Add(craftType, new List<string>());
            }
            if (!playerList[playerName][craftType].Contains(craftName))
            {
                SyncrioLog.Debug("Adding " + craftName + ", type: " + craftType.ToString() + " from " + playerName);
                playerList[playerName][craftType].Add(craftName);
            }
        }

        private void DeleteCraftEntry(string playerName, CraftType craftType, string craftName)
        {
            if (playerList.ContainsKey(playerName))
            {
                if (playerList[playerName].ContainsKey(craftType))
                {
                    if (playerList[playerName][craftType].Contains(craftName))
                    {
                        playerList[playerName][craftType].Remove(craftName);
                        if (playerList[playerName][craftType].Count == 0)
                        {
                            playerList[playerName].Remove(craftType);
                        }
                        if (playerList[playerName].Count == 0)
                        {
                            if (playerName != Settings.fetch.playerName)
                            {
                                playerList.Remove(playerName);
                                if (playersWithCrafts.Contains(playerName))
                                {
                                    playersWithCrafts.Remove(playerName);
                                }
                            }
                        }
                    }
                    else
                    {
                        SyncrioLog.Debug("Cannot remove craft entry " + craftName + " for player " + playerName + ", craft does not exist");
                    }
                }
                else
                {
                    SyncrioLog.Debug("Cannot remove craft entry " + craftName + " for player " + playerName + ", player does not have any " + craftType + " entries");
                }

            }
            else
            {
                SyncrioLog.Debug("Cannot remove craft entry " + craftName + " for player " + playerName + ", no player entry");
            }
        }

        private void SaveCraftFile(CraftType craftType, string craftName, byte[] craftData)
        {
            string savePath = "";
            switch (craftType)
            {
                case CraftType.VAB:
                    savePath = vabPath;
                    break;
                case CraftType.SPH:
                    savePath = sphPath;
                    break;
                case CraftType.SUBASSEMBLY:
                    savePath = subassemblyPath;
                    break;
            }
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            string craftFile = Path.Combine(savePath, craftName + ".craft");
            File.WriteAllBytes(craftFile, craftData);
            ScreenMessages.PostScreenMessage("Craft " + craftName + " saved!", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        private void InitGUI()
        {
            //Setup GUI stuff
            //left 50, middle height
            playerWindowRect = new Rect(50, (Screen.height / 2f) - (PLAYER_WINDOW_HEIGHT / 2f), PLAYER_WINDOW_WIDTH, PLAYER_WINDOW_HEIGHT);
            //middle of the screen
            libraryWindowRect = new Rect((Screen.width / 2f) - (LIBRARY_WINDOW_WIDTH / 2f), (Screen.height / 2f) - (LIBRARY_WINDOW_HEIGHT / 2f), LIBRARY_WINDOW_WIDTH, LIBRARY_WINDOW_HEIGHT);
            moveRect = new Rect(0, 0, 10000, 20);

            playerLayoutOptions = new GUILayoutOption[4];
            playerLayoutOptions[0] = GUILayout.MinWidth(PLAYER_WINDOW_WIDTH);
            playerLayoutOptions[1] = GUILayout.MaxWidth(PLAYER_WINDOW_WIDTH);
            playerLayoutOptions[2] = GUILayout.MinHeight(PLAYER_WINDOW_HEIGHT);
            playerLayoutOptions[3] = GUILayout.MaxHeight(PLAYER_WINDOW_HEIGHT);

            libraryLayoutOptions = new GUILayoutOption[4];
            libraryLayoutOptions[0] = GUILayout.MinWidth(LIBRARY_WINDOW_WIDTH);
            libraryLayoutOptions[1] = GUILayout.MaxWidth(LIBRARY_WINDOW_WIDTH);
            libraryLayoutOptions[2] = GUILayout.MinHeight(LIBRARY_WINDOW_HEIGHT);
            libraryLayoutOptions[3] = GUILayout.MaxHeight(LIBRARY_WINDOW_HEIGHT);

            windowStyle = new GUIStyle(GUI.skin.window);
            buttonStyle = new GUIStyle(GUI.skin.button);
            labelStyle = new GUIStyle(GUI.skin.label);
            scrollStyle = new GUIStyle(GUI.skin.scrollView);

            textAreaOptions = new GUILayoutOption[2];
            textAreaOptions[0] = GUILayout.ExpandWidth(false);
            textAreaOptions[1] = GUILayout.ExpandWidth(false);
        }

        public void Draw()
        {
            if (!initialized)
            {
                initialized = true;
                InitGUI();
            }
            if (safeDisplay)
            {
                playerWindowRect = SyncrioGuiUtil.PreventOffscreenWindow(GUILayout.Window(7707 + Client.WINDOW_OFFSET, playerWindowRect, DrawPlayerContent, "Syncrio - Craft Library", windowStyle, playerLayoutOptions));
            }
            if (safeDisplay && selectedPlayer != null)
            {
                //Sanity check
                if (playersWithCrafts.Contains(selectedPlayer) || selectedPlayer == Settings.fetch.playerName)
                {
                    libraryWindowRect = SyncrioGuiUtil.PreventOffscreenWindow(GUILayout.Window(7708 + Client.WINDOW_OFFSET, libraryWindowRect, DrawLibraryContent, "Syncrio - " + selectedPlayer + " Craft Library", windowStyle, libraryLayoutOptions));
                }
                else
                {
                    selectedPlayer = null;
                }
            }
            CheckWindowLock();
        }

        private void DrawPlayerContent(int windowID)
        {
            GUILayout.BeginVertical();
            GUI.DragWindow(moveRect);
            //Draw the player buttons
            playerScrollPos = GUILayout.BeginScrollView(playerScrollPos, scrollStyle);
            DrawPlayerButton(Settings.fetch.playerName);
            foreach (string playerName in playersWithCrafts)
            {
                if (playerName != Settings.fetch.playerName)
                {
                    DrawPlayerButton(playerName);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawPlayerButton(string playerName)
        {
            bool buttonSelected = GUILayout.Toggle(selectedPlayer == playerName, playerName, buttonStyle);
            if (buttonSelected && selectedPlayer != playerName)
            {
                //Select
                selectedPlayer = playerName;
            }
            if (!buttonSelected && selectedPlayer == playerName)
            {
                //Unselect
                selectedPlayer = null;
            }
        }

        private void DrawLibraryContent(int windowID)
        {
            GUILayout.BeginVertical();
            GUI.DragWindow(moveRect);
            bool newShowUpload = false;
            if (selectedPlayer == Settings.fetch.playerName)
            {
                newShowUpload = GUILayout.Toggle(showUpload, "Upload", buttonStyle);
            }
            if (newShowUpload && !showUpload)
            {
                //Build list when the upload button is pressed.
                BuildUploadList();
            }
            showUpload = newShowUpload;
            libraryScrollPos = GUILayout.BeginScrollView(libraryScrollPos, scrollStyle);
            if (showUpload)
            {
                //Draw upload screen
                DrawUploadScreen();
            }
            else
            {
                //Draw download screen
                DrawDownloadScreen();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void CheckWindowLock()
        {
            if (!Client.fetch.gameRunning)
            {
                RemoveWindowLock();
                return;
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                RemoveWindowLock();
                return;
            }

            if (safeDisplay)
            {
                Vector2 mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;

                bool shouldLock = (playerWindowRect.Contains(mousePos) || libraryWindowRect.Contains(mousePos));

                if (shouldLock && !isWindowLocked)
                {
                    InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "Syncrio_CraftLock");
                    isWindowLocked = true;
                }
                if (!shouldLock && isWindowLocked)
                {
                    RemoveWindowLock();
                }
            }

            if (!safeDisplay && isWindowLocked)
            {
                RemoveWindowLock();
            }
        }

        private void RemoveWindowLock()
        {
            if (isWindowLocked)
            {
                isWindowLocked = false;
                InputLockManager.RemoveControlLock("Syncrio_CraftLock");
            }
        }

        private void DrawUploadScreen()
        {
            foreach (KeyValuePair<CraftType, List<string>> entryType in uploadList)
            {
                GUILayout.Label(entryType.Key.ToString(), labelStyle);
                foreach (string entryName in entryType.Value)
                {
                    if (playerList.ContainsKey(Settings.fetch.playerName))
                    {
                        if (playerList[Settings.fetch.playerName].ContainsKey(entryType.Key))
                        {
                            if (playerList[Settings.fetch.playerName][entryType.Key].Contains(entryName))
                            {
                                GUI.enabled = false;
                            }
                        }
                    }
                    if (GUILayout.Button(entryName, buttonStyle))
                    {
                        uploadCraftType = entryType.Key;
                        uploadCraftName = entryName;
                    }
                    GUI.enabled = true;
                }
            }
        }

        private void BuildUploadList()
        {
            uploadList = new Dictionary<CraftType, List<string>>();
            if (Directory.Exists(vabPath))
            {
                uploadList.Add(CraftType.VAB, new List<string>());
                string[] craftFiles = Directory.GetFiles(vabPath);
                foreach (string craftFile in craftFiles)
                {
                    string craftName = Path.GetFileNameWithoutExtension(craftFile);
                    uploadList[CraftType.VAB].Add(craftName);
                }
            }
            if (Directory.Exists(sphPath))
            {
                uploadList.Add(CraftType.SPH, new List<string>());
                string[] craftFiles = Directory.GetFiles(sphPath);
                foreach (string craftFile in craftFiles)
                {
                    string craftName = Path.GetFileNameWithoutExtension(craftFile);
                    uploadList[CraftType.SPH].Add(craftName);
                }
            }
            if (Directory.Exists(vabPath))
            {
                uploadList.Add(CraftType.SUBASSEMBLY, new List<string>());
                string[] craftFiles = Directory.GetFiles(subassemblyPath);
                foreach (string craftFile in craftFiles)
                {
                    string craftName = Path.GetFileNameWithoutExtension(craftFile);
                    uploadList[CraftType.SUBASSEMBLY].Add(craftName);
                }
            }
        }

        private void DrawDownloadScreen()
        {
            if (playerList.ContainsKey(selectedPlayer))
            {
                foreach (KeyValuePair<CraftType, List<string>> entry in playerList[selectedPlayer])
                {
                    GUILayout.Label(entry.Key.ToString(), labelStyle);
                    foreach (string craftName in entry.Value)
                    {
                        if (selectedPlayer == Settings.fetch.playerName)
                        {
                            //Also draw remove button on player screen
                            GUILayout.BeginHorizontal();
                            if (GUILayout.Button(craftName, buttonStyle))
                            {
                                downloadCraftType = entry.Key;
                                downloadCraftName = craftName;
                            }
                            if (GUILayout.Button("Remove", buttonStyle))
                            {
                                deleteCraftType = entry.Key;
                                deleteCraftName = craftName;
                            }
                            GUILayout.EndHorizontal();
                        }
                        else
                        {
                            if (GUILayout.Button(craftName, buttonStyle))
                            {
                                downloadCraftType = entry.Key;
                                downloadCraftName = craftName;
                            }
                        }
                    }
                }
            }
        }

        public void QueueCraftAdd(CraftChangeEntry entry)
        {
            craftAddQueue.Enqueue(entry);
        }

        public void QueueCraftDelete(CraftChangeEntry entry)
        {
            craftDeleteQueue.Enqueue(entry);
        }

        public void QueueCraftResponse(CraftResponseEntry entry)
        {
            craftResponseQueue.Enqueue(entry);
        }

        public static void Reset()
        {
            lock (Client.eventLock)
            {
                if (singleton != null)
                {
                    singleton.workerEnabled = false;
                    singleton.RemoveWindowLock();
                    Client.updateEvent.Remove(singleton.Update);
                    Client.drawEvent.Remove(singleton.Draw);
                }
                singleton = new CraftLibraryWorker();
                singleton.BuildUploadList();
                Client.updateEvent.Add(singleton.Update);
                Client.drawEvent.Add(singleton.Draw);
            }
        }
    }

    public class CraftChangeEntry
    {
        public string playerName;
        public CraftType craftType;
        public string craftName;
    }

    public class CraftResponseEntry
    {
        public string playerName;
        public CraftType craftType;
        public string craftName;
        public byte[] craftData;
    }
}

