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
using SyncrioCommon;


namespace SyncrioServer
{
    public interface ISyncrioPlugin
    {
        /// <summary>
        /// Fires every main thread tick (10ms).
        /// </summary>
        void OnUpdate();
        /// <summary>
        /// Fires just after the server is started or restarted.
        /// </summary>
        void OnServerStart();
        /// <summary>
        /// Fires just before the server stops or restarts.
        /// </summary>
        void OnServerStop();
        /// <summary>
        /// Fires when the client's connection is accepted.
        /// </summary>
        void OnClientConnect(ClientObject client);
        /// <summary>
        /// Fires just after the client has authenticated
        /// </summary>
        void OnClientAuthenticated(ClientObject client);
        /// <summary>
        /// Fires when a client disconnects
        /// </summary>
        void OnClientDisconnect(ClientObject client);
        /// <summary>
        /// Fires every time a message is received from a client
        /// </summary>
        /// <param name="client">The client that has sent the message</param>
        /// <param name="messageData">The message payload (Null for certain types)</param>
        void OnMessageReceived(ClientObject client, ClientMessage messageData);
        /// <summary>
        /// Fires every time a message is received from a client
        /// </summary>
        /// <param name="client">The client that has sent the message</param>
        /// <param name="messageData">The message payload (Null for certain types)</param>
        void OnMessageSent(ClientObject client, ServerMessage messageData);
    }

    public abstract class SyncrioPlugin : ISyncrioPlugin
    {
        public virtual void OnUpdate() { }
        public virtual void OnServerStart() { }
        public virtual void OnServerStop() { }
        public virtual void OnClientConnect(ClientObject client) { }
        public virtual void OnClientAuthenticated(ClientObject client) { }
        public virtual void OnClientDisconnect(ClientObject client) { }
        public virtual void OnMessageReceived(ClientObject client, ClientMessage messageData) { }
        public virtual void OnMessageSent(ClientObject client, ServerMessage messageData) { }
    }
}
