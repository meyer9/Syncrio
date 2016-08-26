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
using SyncrioCommon;
using MessageStream2;

namespace SyncrioServer.Messages
{
    public class ServerSettings
    {
        public static void SendServerSettings(ClientObject client)
        {
            int numberOfKerbals = Directory.GetFiles(Path.Combine(Server.ScenarioDirectory, "Kerbals")).Length;
            int numberOfScenarioModules = Directory.GetFiles(Path.Combine(Server.ScenarioDirectory, "Players", client.playerName)).Length;
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.SERVER_SETTINGS;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<bool>(Settings.settingsStore.DarkMultiPlayerCoopMode);
                mw.Write<int>((int)Settings.settingsStore.gameMode);
                mw.Write<bool>(Settings.settingsStore.cheats);
                //Tack the amount of kerbals and scenario modules onto this message
                mw.Write<int>(numberOfKerbals);
                //mw.Write<int>(numberOfScenarioModules);
                //Send scenario settings
                mw.Write<bool>(Settings.settingsStore.autoSyncScenarios);
                mw.Write<bool>(Settings.settingsStore.nonGroupScenarios); 
                mw.Write<bool>(Settings.settingsStore.canResetScenario);
                mw.Write<int>(Settings.settingsStore.screenshotHeight);
                mw.Write<string>(Settings.settingsStore.consoleIdentifier);
                mw.Write<int>((int)Settings.settingsStore.gameDifficulty);

                if (Settings.settingsStore.gameDifficulty == GameDifficulty.CUSTOM)
                {
                    mw.Write<bool>(GameplaySettings.settingsStore.allowStockVessels);
                    mw.Write<bool>(GameplaySettings.settingsStore.autoHireCrews);
                    mw.Write<bool>(GameplaySettings.settingsStore.bypassEntryPurchaseAfterResearch);
                    mw.Write<bool>(GameplaySettings.settingsStore.indestructibleFacilities);
                    mw.Write<bool>(GameplaySettings.settingsStore.missingCrewsRespawn);
                    mw.Write<float>(GameplaySettings.settingsStore.reentryHeatScale);
                    mw.Write<float>(GameplaySettings.settingsStore.resourceAbundance);
                    mw.Write<bool>(GameplaySettings.settingsStore.canQuickLoad);
                    mw.Write<float>(GameplaySettings.settingsStore.fundsGainMultiplier);
                    mw.Write<float>(GameplaySettings.settingsStore.fundsLossMultiplier);
                    mw.Write<float>(GameplaySettings.settingsStore.repGainMultiplier);
                    mw.Write<float>(GameplaySettings.settingsStore.repLossMultiplier);
                    mw.Write<float>(GameplaySettings.settingsStore.repLossDeclined);
                    mw.Write<float>(GameplaySettings.settingsStore.scienceGainMultiplier);
                    mw.Write<float>(GameplaySettings.settingsStore.startingFunds);
                    mw.Write<float>(GameplaySettings.settingsStore.startingReputation);
                    mw.Write<float>(GameplaySettings.settingsStore.startingScience);
                }
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToClient(client, newMessage, true);
        }
    }
}

