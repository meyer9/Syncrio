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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Net;
using SyncrioServer;
using SyncrioCommon;

namespace SyncrioServer
{
    [DataContract]
    class ServerInfo
    {
        [DataMember]
        public string server_name;

        [DataMember]
        public string version;

        [DataMember]
        public int protocol_version;

        [DataMember]
        public string players;

        [DataMember]
        public int player_count;

        [DataMember]
        public int max_players;

        [DataMember]
        public int port;

        [DataMember]
        public string game_mode;

        [DataMember]
        public bool cheats;

        [DataMember]
        public long ScenarioSize;

        [DataMember]
        public long lastPlayerActivity;

        public ServerInfo(SettingsStore settings)
        {
            server_name = settings.serverName;
            version = Common.PROGRAM_VERSION;
            protocol_version = Common.PROTOCOL_VERSION;
            player_count = Server.playerCount;
            players = Server.players;
            max_players = settings.maxPlayers;
            game_mode = settings.gameMode.ToString();
            port = settings.port;
            cheats = settings.cheats;
            ScenarioSize = Server.GetScenarioSize();
            lastPlayerActivity = Server.GetLastPlayerActivity();
        }

        public string GetJSON()
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ServerInfo));

            MemoryStream outStream = new MemoryStream();
            serializer.WriteObject(outStream, this);

            outStream.Position = 0;

            StreamReader sr = new StreamReader(outStream);

            return sr.ReadToEnd();
        }
    }
}
