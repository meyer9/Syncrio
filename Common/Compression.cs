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
using System.IO.Compression;
using System.Threading;
using ICSharpCode.SharpZipLib.GZip;

namespace SyncrioCommon
{
    public class Compression
    {
        public const int COMPRESSION_THRESHOLD = 4096;
        public static bool compressionEnabled = false;
        public static bool sysIOCompressionWorks
        {
            get;
            private set;
        }

        public static long TestSysIOCompression()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            ManualResetEvent mre = new ManualResetEvent(false);
            Thread compressionThreadTester = new Thread(new ParameterizedThreadStart(CompressionTestWorker));
            compressionThreadTester.IsBackground = true;
            compressionThreadTester.Start(mre);
            bool result = mre.WaitOne(1000);
            if (!result)
            {
                compressionThreadTester.Abort();
            }
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        private static void CompressionTestWorker(object mreObject)
        {
            ManualResetEvent mre = (ManualResetEvent)mreObject;
            bool compressionWorks = true;
            try
            {
                byte[] smallEmptyTest = new byte[COMPRESSION_THRESHOLD / 2];
                byte[] bigEmptyTest = new byte[COMPRESSION_THRESHOLD * 2];
                byte[] smallRandomTest = new byte[COMPRESSION_THRESHOLD / 2];
                byte[] bigRandomTest = new byte[COMPRESSION_THRESHOLD * 2];
                Random rand = new Random();
                rand.NextBytes(smallRandomTest);
                rand.NextBytes(bigRandomTest);
                byte[] t1 = SysIOCompress(smallEmptyTest);
                byte[] t2 = SysIOCompress(bigEmptyTest);
                byte[] t3 = SysIOCompress(smallRandomTest);
                byte[] t4 = SysIOCompress(bigRandomTest);
                byte[] t5 = SysIODecompress(t1);
                byte[] t6 = SysIODecompress(t2);
                byte[] t7 = SysIODecompress(t3);
                byte[] t8 = SysIODecompress(t4);
                //Fail the test if the byte array doesn't match
                if (!ByteCompare(smallEmptyTest, t5))
                {
                    compressionWorks = false;
                }
                if (!ByteCompare(bigEmptyTest, t6))
                {
                    compressionWorks = false;
                }
                if (!ByteCompare(smallRandomTest, t7))
                {
                    compressionWorks = false;
                }
                if (!ByteCompare(bigRandomTest, t8))
                {
                    compressionWorks = false;
                }
                sysIOCompressionWorks = compressionWorks;
            }
            catch
            {
                sysIOCompressionWorks = false;
            }
            mre.Set();
        }

        public static bool ByteCompare(byte[] lhs, byte[] rhs)
        {
            if (lhs.Length != rhs.Length)
            {
                return false;
            }
            for (int i = 0; i < lhs.Length; i++)
            {
                if (lhs[i] != rhs[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static byte[] CompressIfNeeded(byte[] inputBytes)
        {
            if (inputBytes == null)
            {
                throw new Exception("Input bytes are null");
            }
            if (inputBytes.Length < COMPRESSION_THRESHOLD || !compressionEnabled)
            {
                return AddCompressionHeader(inputBytes, false);
            }
            return AddCompressionHeader(Compress(inputBytes), true);
        }



        public static byte[] DecompressIfNeeded(byte[] inputBytes)
        {
            if (inputBytes == null)
            {
                throw new Exception("Input bytes are null");
            }
            if (!BytesAreCompressed(inputBytes))
            {
                return RemoveDecompressedHeader(inputBytes);
            }
            if (!compressionEnabled)
            {
                throw new Exception("Cannot decompress if compression is disabled!");
            }
            return Decompress(RemoveDecompressedHeader(inputBytes));
        }

        /// <summary>
        /// Tests if the byte[] is compressed.
        /// </summary>
        /// <returns><c>true</c>, if the byte[] was compressed, <c>false</c> otherwise.</returns>
        /// <param name="inputBytes">Input bytes.</param>
        public static bool BytesAreCompressed(byte[] inputBytes)
        {
            if (inputBytes == null)
            {
                throw new Exception("Input bytes are null");
            }
            return BitConverter.ToBoolean(inputBytes, 0);
        }

        /// <summary>
        /// Appends the decompressed header.
        /// </summary>
        /// <returns>The message with the prepended header</returns>
        /// <param name="inputBytes">Input bytes.</param>
        public static byte[] AddCompressionHeader(byte[] inputBytes, bool value)
        {
            if (inputBytes == null)
            {
                throw new Exception("Input bytes are null");
            }
            byte[] returnBytes = new byte[inputBytes.Length + 1];
            BitConverter.GetBytes(value).CopyTo(returnBytes, 0);
            Array.Copy(inputBytes, 0, returnBytes, 1, inputBytes.Length);
            return returnBytes;
        }

        /// <summary>
        /// Removes the decompressed header.
        /// </summary>
        /// <returns>The message without the prepended header</returns>
        /// <param name="inputBytes">Input bytes.</param>
        public static byte[] RemoveDecompressedHeader(byte[] inputBytes)
        {
            if (inputBytes == null)
            {
                throw new Exception("Input bytes are null");
            }
            byte[] returnBytes = new byte[inputBytes.Length - 1];
            Array.Copy(inputBytes, 1, returnBytes, 0, inputBytes.Length - 1);
            return returnBytes;
        }

        public static byte[] Compress(byte[] inputBytes)
        {
            if (sysIOCompressionWorks)
            {
                return SysIOCompress(inputBytes);
            }
            return ICSharpCompress(inputBytes);
        }

        public static byte[] Decompress(byte[] inputBytes)
        {
            if (sysIOCompressionWorks)
            {
                return SysIODecompress(inputBytes);
            }
            return ICSharpDecompress(inputBytes);
        }

        private static byte[] SysIOCompress(byte[] inputBytes)
        {
            byte[] returnBytes = null;
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream gs = new GZipStream(ms, CompressionMode.Compress))
                {
                    gs.Write(inputBytes, 0, inputBytes.Length);
                }
                returnBytes = ms.ToArray();
            }
            return returnBytes;
        }

        private static byte[] SysIODecompress(byte[] inputBytes)
        {
            byte[] returnBytes = null;
            using (MemoryStream outputStream = new MemoryStream())
            {
                using (MemoryStream ms = new MemoryStream(inputBytes))
                {
                    using (GZipStream gs = new GZipStream(ms, CompressionMode.Decompress))
                    {
                        //Stream.CopyTo is a .NET 4 feature?
                        byte[] buffer = new byte[4096];
                        int numRead;
                        while ((numRead = gs.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            outputStream.Write(buffer, 0, numRead);
                        }
                    }
                }
                returnBytes = outputStream.ToArray();
            }
            return returnBytes;
        }

        private static byte[] ICSharpCompress(byte[] inputBytes)
        {
            byte[] returnBytes = null;
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipOutputStream gs = new GZipOutputStream(ms))
                {
                    gs.Write(inputBytes, 0, inputBytes.Length);
                }
                returnBytes = ms.ToArray();
            }
            return returnBytes;
        }

        private static byte[] ICSharpDecompress(byte[] inputBytes)
        {
            byte[] returnBytes = null;
            using (MemoryStream outputStream = new MemoryStream())
            {
                using (MemoryStream ms = new MemoryStream(inputBytes))
                {
                    using (GZipInputStream gs = new GZipInputStream(ms))
                    {
                        //Stream.CopyTo is a .NET 4 feature?
                        byte[] buffer = new byte[4096];
                        int numRead;
                        while ((numRead = gs.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            outputStream.Write(buffer, 0, numRead);
                        }
                    }
                }
                returnBytes = outputStream.ToArray();
            }
            return returnBytes;
        }
    }
}
