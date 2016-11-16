using System;
using System.IO;
using System.Collections.Concurrent;

namespace SyncrioUtil
{
    public class FileHandler
    {
        private static ConcurrentDictionary<string, object> fileLockList = new ConcurrentDictionary<string, object>();

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
                    fileLockList.TryAdd(path, fileLock);
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
