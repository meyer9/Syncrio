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

namespace SyncrioServer
{
    public class SyncrioLog
    {
        public static string LogFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        public static string LogFilename = Path.Combine(LogFolder, "Syncrioserver " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".log");
        private static object logLock = new object();

        public enum LogLevels
        {
            DEBUG,
            INFO,
            CHAT,
            ERROR,
            FATAL
        }

        private static void WriteLog(LogLevels level, string message, bool sendToConsole)
        {
            if (!Directory.Exists(LogFolder))
            {
                Directory.CreateDirectory(LogFolder);
            }

            if (level >= Settings.settingsStore.logLevel)
            {
                string output;
                if (Settings.settingsStore.useUTCTimeInLog)
                {
                    output = "[" + DateTime.UtcNow.ToString("HH:mm:ss") + "][" + level.ToString() + "] : " + message;
                }
                else
                {
                    output = "[" + DateTime.Now.ToString("HH:mm:ss") + "][" + level.ToString() + "] : " + message;
                }
                if (sendToConsole)
                {
                    Console.WriteLine(output);
                    Messages.Chat.SendConsoleMessageToAdmins(output);
                }
                try
                {
                    lock (logLock) {
                        File.AppendAllText(LogFilename, output + Environment.NewLine);
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("Error writing to log file!, Exception: " + e);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
        }

        public static void WriteToLog(string message)
        {
            WriteLog(LogLevels.INFO, message, false);
        }

        public static void Debug(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            WriteLog(LogLevels.DEBUG, message, true);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void Normal(string message)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            WriteLog(LogLevels.INFO, message, true);
        }

        public static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            WriteLog(LogLevels.ERROR, message, true);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void Fatal(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            WriteLog(LogLevels.FATAL, message, true);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void ChatMessage(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            WriteLog(LogLevels.CHAT, message, true);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}

