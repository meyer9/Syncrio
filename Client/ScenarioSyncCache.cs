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
using System.IO;
using System.Collections.Generic;
using System.Threading;
using SyncrioCommon;

namespace SyncrioClientSide
{
    public class ScenarioSyncCache
    {
        private static ScenarioSyncCache singleton = new ScenarioSyncCache();

        public string cacheDirectory
        {
            get
            {
                return Path.Combine(Path.Combine(Path.Combine(KSPUtil.ApplicationRootPath, "GameData"), "Syncrio"), "Cache");
            }
        }

        private AutoResetEvent incomingEvent = new AutoResetEvent(false);
        private Queue<byte[]> incomingQueue = new Queue<byte[]>();
        private Dictionary<string, long> fileLengths = new Dictionary<string, long>();
        private Dictionary<string, DateTime> fileCreationTimes = new Dictionary<string, DateTime>();

        public long currentCacheSize
        {
            get;
            private set;
        }

        public ScenarioSyncCache()
        {
            Thread processingThread = new Thread(new ThreadStart(ProcessingThreadMain));
            processingThread.IsBackground = true;
            processingThread.Start();
        }

        public static ScenarioSyncCache fetch
        {
            get
            {
                return singleton;
            }
        }

        private void ProcessingThreadMain()
        {
            while (true)
            {
                if (incomingQueue.Count == 0)
                {
                    incomingEvent.WaitOne(500);
                }
                else
                {
                    byte[] incomingBytes;
                    lock (incomingQueue)
                    {
                        incomingBytes = incomingQueue.Dequeue();
                    }
                    SaveToCache(incomingBytes);
                }
            }
        }

        private string[] GetCachedFiles()
        {
            return Directory.GetFiles(cacheDirectory);
        }

        public string[] GetCachedObjects()
        {
            string[] cacheFiles = GetCachedFiles();
            string[] cacheObjects = new string[cacheFiles.Length];
            for (int i = 0; i < cacheFiles.Length; i++)
            {
                cacheObjects[i] = Path.GetFileNameWithoutExtension(cacheFiles[i]);
            }
            return cacheObjects;
        }

        public void ExpireCache()
        {
            SyncrioLog.Debug("Expiring cache!");
            //No folder, no delete.
            if (!Directory.Exists(Path.Combine(cacheDirectory, "Incoming")))
            {
                SyncrioLog.Debug("No sync cache folder, skipping expire.");
                return;
            }
            //Delete partial incoming files
            string[] incomingFiles = Directory.GetFiles(Path.Combine(cacheDirectory, "Incoming"));
            foreach (string incomingFile in incomingFiles)
            {
                SyncrioLog.Debug("Deleting partially cached object " + incomingFile);
                File.Delete(incomingFile);
            }
            //Delete old files
            string[] cacheObjects = GetCachedObjects();
            currentCacheSize = 0;
            foreach (string cacheObject in cacheObjects)
            {
                string cacheFile = Path.Combine(cacheDirectory, cacheObject + ".txt");
                //If the file is older than a week, delete it.
                if (File.GetCreationTime(cacheFile).AddDays(7d) < DateTime.Now)
                {
                    SyncrioLog.Debug("Deleting cached object " + cacheObject + ", reason: Expired!");
                    File.Delete(cacheFile);
                }
                else
                {
                    FileInfo fi = new FileInfo(cacheFile);
                    fileCreationTimes[cacheObject] = fi.CreationTime;
                    fileLengths[cacheObject] = fi.Length;
                    currentCacheSize += fi.Length;
                }
            }
            //While the directory is over (cacheSize) MB
            while (currentCacheSize > (Settings.fetch.cacheSize * 1024 * 1024))
            {
                string deleteObject = null;
                //Find oldest file
                foreach (KeyValuePair<string, DateTime> testFile in fileCreationTimes)
                {
                    if (deleteObject == null)
                    {
                        deleteObject = testFile.Key;
                    }
                    if (testFile.Value < fileCreationTimes[deleteObject])
                    {
                        deleteObject = testFile.Key;
                    }
                }
                SyncrioLog.Debug("Deleting cached object " + deleteObject + ", reason: Cache full!");
                string deleteFile = Path.Combine(cacheDirectory, deleteObject + ".txt");
                File.Delete(deleteFile);
                currentCacheSize -= fileLengths[deleteObject];
                if (fileCreationTimes.ContainsKey(deleteObject))
                {
                    fileCreationTimes.Remove(deleteObject);
                }
                if (fileLengths.ContainsKey(deleteObject))
                {
                    fileLengths.Remove(deleteObject);
                }
            }
        }

        /// <summary>
        /// Queues to cache. This method is non-blocking, using SaveToCache for a blocking method.
        /// </summary>
        /// <param name="fileData">File data.</param>
        public void QueueToCache(byte[] fileData)
        {
            lock (incomingQueue)
            {
                incomingQueue.Enqueue(fileData);
            }
            incomingEvent.Set();
        }

        /// <summary>
        /// Saves to cache. This method is blocking, use QueueToCache for a non-blocking method.
        /// </summary>
        /// <param name="fileData">File data.</param>
        public void SaveToCache(byte[] fileData)
        {
            if (fileData == null || fileData.Length == 0)
            {
                //Don't save 0 byte data.
                return;
            }
            string objectName = Common.CalculateSHA256Hash(fileData);
            string objectFile = Path.Combine(cacheDirectory, objectName + ".txt");
            string incomingFile = Path.Combine(Path.Combine(cacheDirectory, "Incoming"), objectName + ".txt");
            if (!File.Exists(objectFile))
            {
                File.WriteAllBytes(incomingFile, fileData);
                File.Move(incomingFile, objectFile);
                currentCacheSize += fileData.Length;
                fileLengths[objectName] = fileData.Length;
                fileCreationTimes[objectName] = new FileInfo(objectFile).CreationTime;
            }
            else
            {
                File.SetCreationTime(objectFile, DateTime.Now);
                fileCreationTimes[objectName] = new FileInfo(objectFile).CreationTime;
            }
        }

        public byte[] GetFromCache(string objectName)
        {
            string objectFile = Path.Combine(cacheDirectory, objectName + ".txt");
            if (File.Exists(objectFile))
            {
                return File.ReadAllBytes(objectFile);
            }
            else
            {
                throw new IOException("Cached object " + objectName + " does not exist");
            }
        }

        public void DeleteCache()
        {
            SyncrioLog.Debug("Deleting cache!");
            foreach (string cacheFile in GetCachedFiles())
            {
                File.Delete(cacheFile);
            }
            fileLengths = new Dictionary<string, long>();
            fileCreationTimes = new Dictionary<string, DateTime>();
            currentCacheSize = 0;
        }
    }
}

