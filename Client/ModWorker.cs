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
using System.Text;
using SyncrioCommon;

namespace SyncrioClientSide
{
    public class ModWorker
    {
        private static ModWorker singleton = new ModWorker();
        public ModControlMode modControl = ModControlMode.ENABLED_STOP_INVALID_PART_JOIN;
        public bool dllListBuilt = false;
        //Dll files, built at startup
        private Dictionary<string, string> dllList;
        //Accessed from ModWindow
        private List<string> allowedParts;
        private string lastModFileData = "";

        public string failText
        {
            private set;
            get;
        }

        public static ModWorker fetch
        {
            get
            {
                return singleton;
            }
        }

        private bool CheckFile(string relativeFileName, string referencefileHash)
        {
            string fullFileName = Path.Combine(KSPUtil.ApplicationRootPath, Path.Combine("GameData", relativeFileName));
            string fileHash = Common.CalculateSHA256Hash(fullFileName);
            if (fileHash != referencefileHash)
            {
                SyncrioLog.Debug(relativeFileName + " hash mismatch");
                return false;
            }
            return true;
        }

        public void BuildDllFileList()
        {
            dllList = new Dictionary<string, string>();
            string[] checkList = Directory.GetFiles(Path.Combine(KSPUtil.ApplicationRootPath, "GameData"), "*", SearchOption.AllDirectories);

            foreach (string checkFile in checkList)
            {
                //Only check DLL's
                if (checkFile.ToLower().EndsWith(".dll"))
                {
                    //We want the relative path to check against, example: DarkMultiPlayer/Plugins/DarkMultiPlayer.dll
                    //Strip off everything from GameData
                    //Replace windows backslashes with mac/linux forward slashes.
                    //Make it lowercase so we don't worry about case sensitivity.
                    string relativeFilePath = checkFile.ToLowerInvariant().Substring(checkFile.ToLowerInvariant().IndexOf("gamedata") + 9).Replace('\\', '/');
                    string fileHash = Common.CalculateSHA256Hash(checkFile);
                    dllList.Add(relativeFilePath, fileHash);
                    SyncrioLog.Debug("Hashed file: " + relativeFilePath + ", hash: " + fileHash);
                }
            }
        }

        public bool ParseModFile(string modFileData)
        {
            if (modControl == ModControlMode.DISABLED)
            {
                return true;
            }
            bool modCheckOk = true;
            //Save mod file so we can recheck it.
            lastModFileData = modFileData;
            //Err...
            string tempModFilePath = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Path.Combine(KSPUtil.ApplicationRootPath, "GameData"), "Syncrio"), "Plugins"), "Data"), "SyncrioModControl.txt");
            using (StreamWriter sw = new StreamWriter(tempModFilePath))
            {
                sw.WriteLine("#This file is downloaded from the server during connection. It is saved here for convenience.");
                sw.WriteLine(lastModFileData);
            }

            //Parse
            Dictionary<string, string> parseRequired = new Dictionary<string, string>();
            Dictionary<string, string> parseOptional = new Dictionary<string, string>();
            List<string> parseWhiteBlackList = new List<string>();
            List<string> parsePartsList = new List<string>();
            bool isWhiteList = false;
            string readMode = "";
            using (StringReader sr = new StringReader(modFileData))
            {
                while (true)
                {
                    string currentLine = sr.ReadLine();
                    if (currentLine == null)
                    {
                        //Done reading
                        break;
                    }
                    //Remove tabs/spaces from the start & end.
                    string trimmedLine = currentLine.Trim();
                    if (trimmedLine.StartsWith("#") || String.IsNullOrEmpty(trimmedLine))
                    {
                        //Skip comments or empty lines.
                        continue;
                    }
                    if (trimmedLine.StartsWith("!"))
                    {
                        //New section
                        switch (trimmedLine.Substring(1))
                        {
                            case "required-files":
                            case "optional-files":
                            case "partslist":
                                readMode = trimmedLine.Substring(1);
                                break;
                            case "resource-blacklist":
                                readMode = trimmedLine.Substring(1);
                                isWhiteList = false;
                                break;
                            case "resource-whitelist":
                                readMode = trimmedLine.Substring(1);
                                isWhiteList = true;
                                break;
                        }
                    }
                    else
                    {
                        switch (readMode)
                        {
                            case "required-files":
                                {
                                    string lowerFixedLine = trimmedLine.ToLowerInvariant().Replace('\\', '/');
                                    if (lowerFixedLine.Contains("="))
                                    {
                                        string[] splitLine = lowerFixedLine.Split('=');
                                        if (splitLine.Length == 2)
                                        {
                                            if (!parseRequired.ContainsKey(splitLine[0]))
                                            {
                                                parseRequired.Add(splitLine[0], splitLine[1].ToLowerInvariant());
                                            }
                                        }
                                        else
                                        {
                                            if (splitLine.Length == 1)
                                            {
                                                if (!parseRequired.ContainsKey(splitLine[0]))
                                                {
                                                    parseRequired.Add(splitLine[0], "");
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (!parseRequired.ContainsKey(lowerFixedLine))
                                        {
                                            parseRequired.Add(lowerFixedLine, "");
                                        }
                                    }
                                }
                                break;
                            case "optional-files":
                                {
                                    string lowerFixedLine = trimmedLine.ToLowerInvariant().Replace('\\', '/');
                                    if (lowerFixedLine.Contains("="))
                                    {
                                        string[] splitLine = lowerFixedLine.Split('=');
                                        if (splitLine.Length == 2)
                                        {
                                            if (!parseOptional.ContainsKey(splitLine[0]))
                                            {
                                                parseOptional.Add(splitLine[0], splitLine[1]);
                                            }
                                        }
                                        else
                                        {
                                            if (splitLine.Length == 1)
                                            {
                                                if (!parseOptional.ContainsKey(splitLine[0]))
                                                {
                                                    parseOptional.Add(splitLine[0], "");
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (!parseOptional.ContainsKey(lowerFixedLine))
                                        {
                                            parseOptional.Add(lowerFixedLine, "");
                                        }
                                    }
                                }
                                break;
                            case "resource-whitelist":
                            case "resource-blacklist":
                                {
                                    string lowerFixedLine = trimmedLine.ToLowerInvariant().Replace('\\', '/');
                                    //Resource is dll's only.
                                    if (lowerFixedLine.ToLowerInvariant().EndsWith(".dll"))
                                    {
                                        if (parseWhiteBlackList.Contains(lowerFixedLine))
                                        {
                                            parseWhiteBlackList.Add(lowerFixedLine);
                                        }
                                    }
                                }
                                break;
                            case "partslist":
                                if (!parsePartsList.Contains(trimmedLine))
                                {
                                    parsePartsList.Add(trimmedLine);
                                }
                                break;
                        }
                    }
                }
            }

            string[] currentGameDataFiles = Directory.GetFiles(Path.Combine(KSPUtil.ApplicationRootPath, "GameData"), "*", SearchOption.AllDirectories);
            List<string> currentGameDataFilesNormal = new List<string>();
            List<string> currentGameDataFilesLower = new List<string>();
            foreach (string currentFile in currentGameDataFiles)
            {
                string relativeFilePath = currentFile.Substring(currentFile.ToLowerInvariant().IndexOf("gamedata") + 9).Replace('\\', '/');
                currentGameDataFilesNormal.Add(relativeFilePath);
                currentGameDataFilesLower.Add(relativeFilePath.ToLowerInvariant());
            }
            //Check
            StringBuilder sb = new StringBuilder();
            //Check Required
            foreach (KeyValuePair<string, string> requiredEntry in parseRequired)
            {
                if (!requiredEntry.Key.EndsWith("dll"))
                {
                    //Protect against windows-style entries in DMPModControl.txt. Also use case insensitive matching.
                    if (!currentGameDataFilesLower.Contains(requiredEntry.Key))
                    {
                        modCheckOk = false;
                        SyncrioLog.Debug("Required file " + requiredEntry.Key + " is missing!");
                        sb.AppendLine("Required file " + requiredEntry.Key + " is missing!");
                        continue;
                    }
                    //If the entry has a SHA sum, we need to check it.
                    if (requiredEntry.Value != "")
                    {

                        string normalCaseFileName = currentGameDataFilesNormal[currentGameDataFilesLower.IndexOf(requiredEntry.Key)];
                        string fullFileName = Path.Combine(Path.Combine(KSPUtil.ApplicationRootPath, "GameData"), normalCaseFileName);
                        if (!CheckFile(fullFileName, requiredEntry.Value))
                        {
                            modCheckOk = false;
                            SyncrioLog.Debug("Required file " + requiredEntry.Key + " does not match hash " + requiredEntry.Value + "!");
                            sb.AppendLine("Required file " + requiredEntry.Key + " does not match hash " + requiredEntry.Value + "!");
                            continue;
                        }
                    }
                }
                else
                {
                    //DLL entries are cached from startup.
                    if (!dllList.ContainsKey(requiredEntry.Key))
                    {
                        modCheckOk = false;
                        SyncrioLog.Debug("Required file " + requiredEntry.Key + " is missing!");
                        sb.AppendLine("Required file " + requiredEntry.Key + " is missing!");
                        continue;
                    }
                    if (requiredEntry.Value != "")
                    {
                        if (dllList[requiredEntry.Key] != requiredEntry.Value)
                        {
                            modCheckOk = false;
                            SyncrioLog.Debug("Required file " + requiredEntry.Key + " does not match hash " + requiredEntry.Value + "!");
                            sb.AppendLine("Required file " + requiredEntry.Key + " does not match hash " + requiredEntry.Value + "!");
                            continue;
                        }
                    }
                }
            }
            //Check Optional
            foreach (KeyValuePair<string, string> optionalEntry in parseOptional)
            {
                if (!optionalEntry.Key.EndsWith("dll"))
                {
                    //Protect against windows-style entries in DMPModControl.txt. Also use case insensitive matching.
                    if (!currentGameDataFilesLower.Contains(optionalEntry.Key))
                    {
                        //File is optional, nothing to check if it doesn't exist.
                        continue;
                    }
                    //If the entry has a SHA sum, we need to check it.
                    if (optionalEntry.Value != "")
                    {

                        string normalCaseFileName = currentGameDataFilesNormal[currentGameDataFilesLower.IndexOf(optionalEntry.Key)];
                        string fullFileName = Path.Combine(Path.Combine(KSPUtil.ApplicationRootPath, "GameData"), normalCaseFileName);
                        if (!CheckFile(fullFileName, optionalEntry.Value))
                        {
                            modCheckOk = false;
                            SyncrioLog.Debug("Optional file " + optionalEntry.Key + " does not match hash " + optionalEntry.Value + "!");
                            sb.AppendLine("Optional file " + optionalEntry.Key + " does not match hash " + optionalEntry.Value + "!");
                            continue;
                        }
                    }
                }
                else
                {
                    //DLL entries are cached from startup.
                    if (!dllList.ContainsKey(optionalEntry.Key))
                    {
                        //File is optional, nothing to check if it doesn't exist.
                        continue;
                    }
                    if (optionalEntry.Value != "")
                    {
                        if (dllList[optionalEntry.Key] != optionalEntry.Value)
                        {
                            modCheckOk = false;
                            SyncrioLog.Debug("Optional file " + optionalEntry.Key + " does not match hash " + optionalEntry.Value + "!");
                            sb.AppendLine("Optional file " + optionalEntry.Key + " does not match hash " + optionalEntry.Value + "!");
                            continue;
                        }
                    }
                }
            }
            if (isWhiteList)
            {
                //Check Resource whitelist
                List<string> autoAllowed = new List<string>();
                autoAllowed.Add("syncrio/plugins/syncrioclientside.dll");
                autoAllowed.Add("syncrio/plugins/syncriocommon.dll");
                autoAllowed.Add("syncrio/plugins/syncrioutil.dll");
                //Message System
                autoAllowed.Add("syncrio/plugins/messagewriter2.dll");
                //Compression
                autoAllowed.Add("syncrio/plugins/icsharpcode.sharpziplib.dll");
                foreach (KeyValuePair<string, string> dllResource in dllList)
                {
                    //Allow DMP files
                    if (autoAllowed.Contains(dllResource.Key))
                    {
                        continue;
                    }
                    //Ignore squad plugins
                    if (dllResource.Key.StartsWith("squad/plugins"))
                    {
                        continue;
                    }
                    //Check required (Required implies whitelist)
                    if (parseRequired.ContainsKey(dllResource.Key))
                    {
                        continue;
                    }
                    //Check optional (Optional implies whitelist)
                    if (parseOptional.ContainsKey(dllResource.Key))
                    {
                        continue;
                    }
                    //Check whitelist
                    if (parseWhiteBlackList.Contains(dllResource.Key))
                    {
                        continue;
                    }
                    modCheckOk = false;
                    SyncrioLog.Debug("Non-whitelisted resource " + dllResource.Key + " exists on client!");
                    sb.AppendLine("Non-whitelisted resource " + dllResource.Key + " exists on client!");
                }
            }
            else
            {
                //Check Resource blacklist
                foreach (string blacklistEntry in parseWhiteBlackList)
                {
                    if (dllList.ContainsKey(blacklistEntry.ToLowerInvariant()))
                    {
                        modCheckOk = false;
                        SyncrioLog.Debug("Banned resource " + blacklistEntry + " exists on client!");
                        sb.AppendLine("Banned resource " + blacklistEntry + " exists on client!");
                    }
                }
            }
            if (!modCheckOk)
            {
                failText = sb.ToString();
                ModWindow.fetch.display = true;
                return false;
            }
            allowedParts = parsePartsList;
            SyncrioLog.Debug("Mod check passed!");
            return true;
        }

        public List<string> GetAllowedPartsList()
        {
            //Return a copy
            if (modControl == ModControlMode.DISABLED)
            {
                return null;
            }
            return new List<string>(allowedParts);
        }

        public void GenerateModControlFile(bool whitelistMode)
        {
            string gameDataDir = Path.Combine(KSPUtil.ApplicationRootPath, "GameData");
            string[] topLevelFiles = Directory.GetFiles(gameDataDir);
            string[] modDirectories = Directory.GetDirectories(gameDataDir);

            List<string> requiredFiles = new List<string>();
            List<string> optionalFiles = new List<string>();
            List<string> partsList = Common.GetStockParts();
            //If whitelisting, add top level dll's to required (It's usually things like modulemanager)
            foreach (string dllFile in topLevelFiles)
            {
                if (Path.GetExtension(dllFile).ToLower() == ".dll")
                {
                    requiredFiles.Add(Path.GetFileName(dllFile));
                }
            }

            foreach (string modDirectory in modDirectories)
            {
                string lowerDirectoryName = modDirectory.Substring(modDirectory.ToLower().IndexOf("gamedata") + 9).ToLower();
                if (lowerDirectoryName.StartsWith("squad"))
                {
                    continue;
                }
                if (lowerDirectoryName.StartsWith("nasamission"))
                {
                    continue;
                }
                if (lowerDirectoryName.StartsWith("syncrio"))
                {
                    continue;
                }
                bool modIsRequired = false;
                string[] partFiles = Directory.GetFiles(Path.Combine(gameDataDir, modDirectory), "*", SearchOption.AllDirectories);
                List<string> modDllFiles = new List<string>();
                List<string> modPartCfgFiles = new List<string>();
                foreach (string partFile in partFiles)
                {
                    bool fileIsPartFile = false;
                    string relativeFileName = partFile.Substring(partFile.ToLower().IndexOf("gamedata") + 9).Replace(@"\", "/");
                    if (Path.GetExtension(partFile).ToLower() == ".cfg")
                    {
                        ConfigNode cn = ConfigNode.Load(partFile);
                        if (cn == null)
                        {
                            continue;
                        }
                        foreach (ConfigNode partNode in cn.GetNodes("PART"))
                        {
                            string partName = partNode.GetValue("name");
                            if (partName != null)
                            {
                                SyncrioLog.Debug("Part detected in " + relativeFileName + " , name: " + partName);
                                partName = partName.Replace('_', '.');
                                modIsRequired = true;
                                fileIsPartFile = true;
                                partsList.Add(partName);
                            }
                        }

                    }
                    if (fileIsPartFile)
                    {
                        modPartCfgFiles.Add(relativeFileName);
                    }
                    if (Path.GetExtension(partFile).ToLower() == ".dll")
                    {
                        modDllFiles.Add(relativeFileName);
                    }
                }

                if (modIsRequired)
                {
                    if (modDllFiles.Count > 0)
                    {
                        //If the mod as a plugin, just require that. It's clear enough.
                        requiredFiles.AddRange(modDllFiles);
                    }
                    else
                    {
                        //If the mod does *not* have a plugin (Scoop-o-matic is an example), add the part files to required instead.
                        requiredFiles.AddRange(modPartCfgFiles);
                    }
                }
                else
                {
                    if (whitelistMode)
                    {
                        optionalFiles.AddRange(modDllFiles);
                    }
                }
            }
            string modFileData = Common.GenerateModFileStringData(requiredFiles.ToArray(), optionalFiles.ToArray(), whitelistMode, new string[0], partsList.ToArray());
            string saveModFile = Path.Combine(KSPUtil.ApplicationRootPath, "SyncrioModControl.txt");
            using (StreamWriter sw = new StreamWriter(saveModFile, false))
            {
                sw.Write(modFileData);
            }
            ScreenMessages.PostScreenMessage("SyncrioModFile.txt file generated in your KSP folder", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        public void CheckCommonStockParts()//This is callable (Through the options window) in debug mode only
        {
            int totalParts = 0;
            int missingParts = 0;
            List<string> stockParts = Common.GetStockParts();
            SyncrioLog.Debug("Missing parts start");
            foreach (AvailablePart part in PartLoader.LoadedPartsList)
            {
                totalParts++;
                if (!stockParts.Contains(part.name))
                {
                    missingParts++;
                    SyncrioLog.Debug("Missing '" + part.name + "'");
                }
            }
            SyncrioLog.Debug("Missing parts end");
            if (missingParts != 0)
            {
                ScreenMessages.PostScreenMessage(missingParts + " missing part(s) from Common.dll printed to debug log (" + totalParts + " total)", 5f, ScreenMessageStyle.UPPER_CENTER);
            }
            else
            {
                ScreenMessages.PostScreenMessage("No missing parts out of from Common.dll (" + totalParts + " total)", 5f, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        public bool CheckForMissingParts()
        {
            if (modControl == ModControlMode.DISABLED)
            {
                return true;
            }
            bool returnValue = true;
            List<string> allowedParts = GetAllowedPartsList();
            List<string> loadedParts = new List<string>();
            int extraParts = 0;
            int missingParts = 0;

            foreach (AvailablePart part in PartLoader.LoadedPartsList)
            {
                loadedParts.Add(part.name);
                if (!allowedParts.Contains(part.name))
                {
                    extraParts++;
                    SyncrioLog.Debug("You have the part '" + part.name + "' and server does not");
                }
            }

            foreach (string part in allowedParts)
            {
                if (!loadedParts.Contains(part))
                {
                    missingParts++;
                    SyncrioLog.Debug("The server has the part '" + part + "' and you do not");
                }
            }

            if (missingParts != 0)
            {
                returnValue = false;
                ScreenMessages.PostScreenMessage("There are " + missingParts + " parts are on the server side that you do not have!", 5f, ScreenMessageStyle.UPPER_CENTER);
            }

            if (extraParts != 0)
            {
                returnValue = false;
                ScreenMessages.PostScreenMessage("There are " + extraParts + " parts are that you have and the server does not!", 5f, ScreenMessageStyle.UPPER_CENTER);
            }

            return returnValue;
        }
    }
}

