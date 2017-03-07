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


using System;
using System.IO;
using System.Collections.Generic;

namespace SyncrioUtil
{
    public class FileHandler
    {
        private static Dictionary<string, object> fileLockList = new Dictionary<string, object>();

        private static object GetFileLock(string path)
        {
            if (Path.HasExtension(path))
            {
                path = Path.GetDirectoryName(path); 
            }

            if (!string.IsNullOrEmpty(path))
            {
                object fileLock;

                if (!fileLockList.TryGetValue(path, out fileLock))
                {
                    fileLock = new object();
                    fileLockList.Add(path, fileLock);
                }

                return fileLock;
            }
            else
            {
                return new object();
            }
        }

        public static void WriteToFile(byte[] bytes, string path)
        {
            lock (GetFileLock(path))
            {
                File.WriteAllBytes(path, bytes);
            }
        }

        public static byte[] ReadFromFile(string path)
        {
            lock (GetFileLock(path))
            {
                if (File.Exists(path))
                {
                    return File.ReadAllBytes(path);
                }
                else
                {
                    return new byte[0];
                }
            }
        }

        public static void DeleteDirectory(string path)
        {
            foreach (string file in Directory.GetFiles(path))
            {
                lock (GetFileLock(file))
                {
                    File.Delete(file);
                }
            }
            foreach (string folder in Directory.GetDirectories(path))
            {
                DeleteDirectory(folder);
            }
            Directory.Delete(path);
        }
    }
}
