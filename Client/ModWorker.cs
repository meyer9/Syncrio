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
        public bool dllListBuilt = false;
        //Dll files, built at startup
        private Dictionary<string, string> dllList;

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
                    //We want the relative path to check against, example: Syncrio/Plugins/Syncrio.dll
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
    }
}

