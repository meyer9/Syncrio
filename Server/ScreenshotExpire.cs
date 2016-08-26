/*
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
    public class ScreenshotExpire
    {
        private static string screenshotDirectory
        {
            get
            {
                if (Settings.settingsStore.screenshotDirectory != "")
                {
                    return Settings.settingsStore.screenshotDirectory;
                }
                return Path.Combine(Server.ScenarioDirectory, "Screenshots");
            }
        }

        public static void ExpireScreenshots()
        {
            if (!Directory.Exists(screenshotDirectory))
            {
                //Screenshot directory is missing so there will be no screenshots to delete.
                return;
            }
            string[] screenshotFiles = Directory.GetFiles(screenshotDirectory);
            foreach (string screenshotFile in screenshotFiles)
            {
                string cacheFile = Path.Combine(screenshotDirectory, screenshotFile + ".png");
                //Check if the expireScreenshots setting is enabled
                if (Settings.settingsStore.expireScreenshots > 0)
                {
                    //If the file is older than a day, delete it
                    if (File.GetCreationTime(cacheFile).AddDays(Settings.settingsStore.expireScreenshots) < DateTime.Now)
                    {
                        SyncrioLog.Debug("Deleting saved screenshot '" + screenshotFile + "', reason: Expired!");
                        try
                        {
                            File.Delete(cacheFile);
                        }
                        catch (Exception e)
                        {
                            SyncrioLog.Error("Exception while trying to delete " + cacheFile + "!, Exception: " + e.Message);
                        }
                    }
                }
            }
        }
    }
}