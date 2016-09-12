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
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Collections.Generic;

namespace SyncrioCommon
{
    public class Common
    {
        //Timeouts in milliseconds
        public const long HEART_BEAT_INTERVAL = 5000;
        public const long INITIAL_CONNECTION_TIMEOUT = 5000;
        public const long CONNECTION_TIMEOUT = 20000;
        //Any message bigger than 5MB will be invalid
        public const int MAX_MESSAGE_SIZE = 5242880;
        //Split messages into 8kb chunks to higher priority messages have more injection points into the TCP stream.
        public const int SPLIT_MESSAGE_LENGTH = 8192;
        //Bump this every time there is a network change (Basically, if MessageWriter or MessageReader is touched).
        public const int PROTOCOL_VERSION = 41;
        //Program version. This is written in the build scripts.
        public const string PROGRAM_VERSION = "v0.2.5.1";
        //Compression threshold
        public const int COMPRESSION_THRESHOLD = 4096;

        public static string CalculateSHA256HashFromString(string text)
        {
            return CalculateSHA256Hash(Encoding.UTF8.GetBytes(text));
        }

        public static string CalculateSHA256Hash(string fileName)
        {
            return CalculateSHA256Hash(File.ReadAllBytes(fileName));
        }

        public static string CalculateSHA256Hash(byte[] fileData)
        {
            StringBuilder sb = new StringBuilder();
            using (SHA256Managed sha = new SHA256Managed())
            {
                byte[] fileHashData = sha.ComputeHash(fileData);
                //Byte[] to string conversion adapted from MSDN...
                for (int i = 0; i < fileHashData.Length; i++)
                {
                    sb.Append(fileHashData[i].ToString("x2"));
                }
            }
            return sb.ToString();
        }

        public static byte[] PrependNetworkFrame(int messageType, byte[] messageData)
        {
            byte[] returnBytes;
            //Get type bytes
            byte[] typeBytes = BitConverter.GetBytes(messageType);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(typeBytes);
            }
            if (messageData == null || messageData.Length == 0)
            {
                returnBytes = new byte[8];
                typeBytes.CopyTo(returnBytes, 0);
            }
            else
            {
                //Get length bytes if we have a payload
                byte[] lengthBytes = BitConverter.GetBytes(messageData.Length);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(lengthBytes);
                }
                returnBytes = new byte[8 + messageData.Length];
                typeBytes.CopyTo(returnBytes, 0);
                lengthBytes.CopyTo(returnBytes, 4);
                messageData.CopyTo(returnBytes, 8);
            }
            return returnBytes;
        }

        public static string ConvertConfigStringToGUIDString(string configNodeString)
        {
            if (configNodeString == null || configNodeString.Length != 32)
            {
                return null;
            }
            string[] returnString = new string[5];
            returnString[0] = configNodeString.Substring(0, 8);
            returnString[1] = configNodeString.Substring(8, 4);
            returnString[2] = configNodeString.Substring(12, 4);
            returnString[3] = configNodeString.Substring(16, 4);
            returnString[4] = configNodeString.Substring(20);
            return String.Join("-", returnString);
        }
    }

    public enum CraftType
    {
        VAB,
        SPH,
        SUBASSEMBLY
    }

    public enum ClientMessageType
    {
        HEARTBEAT,
        HANDSHAKE_RESPONSE,
        CHAT_MESSAGE,
        PLAYER_STATUS,
        PLAYER_COLOR,
        SYNC_SCENARIO_REQUEST,
        INITIAL_SCENARIO_DATA_REQUEST,
        RESET_SCENARIO,
        JOIN_GROUP_REQUEST,
        LEAVE_GROUP,
        CREATE_GROUP_REQUEST,
        REMOVE_GROUP_REQUEST,
        RENAME_GROUP_REQUEST,
        CHANGE_GROUP_PRIVACY_REQUEST,
        CHANGE_LEADER_REQUEST,
        SET_LEADER,
        INVITE_PLAYER,
        KICK_PLAYER_REQUEST,
        KERBALS_REQUEST,
        KERBAL_PROTO,
        CRAFT_LIBRARY,
        SCREENSHOT_LIBRARY,
        FLAG_SYNC,
        SYNC_TIME_REQUEST,
        PING_REQUEST,
        MOTD_REQUEST,
        LOCK_SYSTEM,
        GROUP_SYSTEM,
        MOD_DATA,
        SPLIT_MESSAGE,
        CONNECTION_END
    }

    public enum ServerMessageType
    {
        HEARTBEAT,
        HANDSHAKE_CHALLANGE,
        HANDSHAKE_REPLY,
        SERVER_SETTINGS,
        CHAT_MESSAGE,
        PLAYER_STATUS,
        PLAYER_COLOR,
        PLAYER_JOIN,
        PLAYER_DISCONNECT,
        SCENARIO_DATA,
        INITIAL_SCENARIO_DATA_REPLY,
        AUTO_SYNC_SCENARIO_REQUEST,
        CREATE_GROUP_REPLY,
        CREATE_GROUP_ERROR,
        CHANGE_LEADER_REQUEST_RELAY,
        INVITE_PLAYER_REQUEST_RELAY,
        KICK_PLAYER_REPLY,
        KERBAL_REPLY,
        KERBAL_COMPLETE,
        CRAFT_LIBRARY,
        SCREENSHOT_LIBRARY,
        FLAG_SYNC,
        SYNC_TIME_REPLY,
        PING_REPLY,
        MOTD_REPLY,
        ADMIN_SYSTEM,
        LOCK_SYSTEM,
        GROUP_SYSTEM,
        MOD_DATA,
        SPLIT_MESSAGE,
        CONNECTION_END
    }

    public enum ConnectionStatus
    {
        DISCONNECTED,
        CONNECTING,
        CONNECTED
    }

    public enum ClientState
    {
        DISCONNECTED,
        CONNECTING,
        CONNECTED,
        HANDSHAKING,
        AUTHENTICATED,
        TIME_SYNCING,
        TIME_SYNCED,
        SYNCING_KERBALS,
        KERBALS_SYNCED,
        TIME_LOCKING,
        TIME_LOCKED,
        STARTING,
        RUNNING,
        DISCONNECTING
    }

    public enum GameMode
    {
        SANDBOX,
        SCIENCE,
        CAREER
    }

    public enum CraftMessageType
    {
        LIST,
        REQUEST_FILE,
        RESPOND_FILE,
        UPLOAD_FILE,
        ADD_FILE,
        DELETE_FILE,
    }

    public enum ScreenshotMessageType
    {
        NOTIFY,
        SEND_START_NOTIFY,
        WATCH,
        SCREENSHOT,
    }

    public enum ChatMessageType
    {
        LIST,
        JOIN,
        LEAVE,
        CHANNEL_MESSAGE,
        PRIVATE_MESSAGE,
        CONSOLE_MESSAGE
    }

    public enum AdminMessageType
    {
        LIST,
        ADD,
        REMOVE,
    }

    public enum LockMessageType
    {
        LIST,
        ACQUIRE,
        RELEASE,
    }

    public enum FlagMessageType
    {
        LIST,
        FLAG_DATA,
        UPLOAD_FILE,
        DELETE_FILE,
    }

    public enum PlayerColorMessageType
    {
        LIST,
        SET,
    }

    public enum GroupMessageType
    {
        SET,
        REMOVE
    }

    public enum GroupPrivacy
    {
        PUBLIC,
        PRIVATE_PASSWORD,
        PRIVATE_INVITE_ONLY
    }

    public class ClientMessage
    {
        public bool handled;
        public ClientMessageType type;
        public byte[] data;
    }

    public class ServerMessage
    {
        public ServerMessageType type;
        public byte[] data;
    }
    
    public class PlayerStatus
    {
        public string playerName;
        public string vesselText;
        public string statusText;
    }

    public class GroupObject
    {
        public List<string> members = new List<string>();
        public GroupPrivacy privacy = GroupPrivacy.PUBLIC;
        public GroupSettings settings = new GroupSettings();
        public string passwordSalt;
        public string passwordHash;
    }

    public class GroupSettings
    {
        public bool inviteAvailable;
        //Put any additional settings here.
    }

    public enum HandshakeReply : int
    {
        HANDSHOOK_SUCCESSFULLY = 0,
        PROTOCOL_MISMATCH = 1,
        ALREADY_CONNECTED = 2,
        RESERVED_NAME = 3,
        INVALID_KEY = 4,
        PLAYER_BANNED = 5,
        SERVER_FULL = 6,
        NOT_WHITELISTED = 7,
        INVALID_PLAYERNAME = 98,
        MALFORMED_HANDSHAKE = 99
    }

    public enum GameDifficulty : int
    {
        EASY,
        NORMAL,
        MODERATE,
        HARD,
        CUSTOM
    }
}
