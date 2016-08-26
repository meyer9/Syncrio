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
using System.Reflection;
using System.Collections.Generic;
using SyncrioCommon;

namespace SyncrioServer
{
    internal static class SyncrioPluginHandler
    {
        private static readonly List<ISyncrioPlugin> loadedPlugins = new List<ISyncrioPlugin>();

        static SyncrioPluginHandler()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            //This will find and return the assembly requested if it is already loaded
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName == args.Name)
                {
                    SyncrioLog.Debug("Resolved plugin assembly reference: " + args.Name + " (referenced by " + args.RequestingAssembly.FullName + ")");
                    return assembly;
                }
            }

            SyncrioLog.Error("Could not resolve assembly " + args.Name + " referenced by " + args.RequestingAssembly.FullName);
            return null;
        }

        public static void LoadPlugins()
        {
            string pluginDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            if (!Directory.Exists(pluginDirectory))
            {
                Directory.CreateDirectory(pluginDirectory);
            }
            SyncrioLog.Debug("Loading plugins!");
            //Load all the assemblies just in case they depend on each other during instantation
            List<Assembly> loadedAssemblies = new List<Assembly>();
            string[] pluginFiles = Directory.GetFiles(pluginDirectory, "*", SearchOption.AllDirectories);
            foreach (string pluginFile in pluginFiles)
            {
                if (Path.GetExtension(pluginFile).ToLower() == ".dll")
                {
                    try
                    {
                        //UnsafeLoadFrom will not throw an exception if the dll is marked as unsafe, such as downloaded from internet in Windows
                        //See http://stackoverflow.com/a/15238782
                        Assembly loadedAssembly = Assembly.UnsafeLoadFrom(pluginFile);
                        loadedAssemblies.Add(loadedAssembly);
                        SyncrioLog.Debug("Loaded " + pluginFile);
                    }
                    catch (NotSupportedException)
                    {
                        //This should only occur if using Assembly.LoadFrom() above instead of Assembly.UnsafeLoadFrom()
                        SyncrioLog.Debug("Can't load dll, perhaps it is blocked: " + pluginFile);
                    }
                    catch
                    {
                        SyncrioLog.Debug("Error loading " + pluginFile);
                    }
                }
            }
            
            //Iterate through the assemblies looking for classes that have the ISyncrioPlugin interface

            Type SyncrioInterfaceType = typeof(ISyncrioPlugin);

            foreach (Assembly loadedAssembly in loadedAssemblies)
            {
                Type[] loadedTypes = loadedAssembly.GetExportedTypes();
                foreach (Type loadedType in loadedTypes)
                {
                    Type[] typeInterfaces = loadedType.GetInterfaces();
                    bool containsSyncrioInterface = false;
                    foreach (Type typeInterface in typeInterfaces)
                    {
                        if (typeInterface == SyncrioInterfaceType)
                        {
                            containsSyncrioInterface = true;
                        }
                    }
                    if (containsSyncrioInterface)
                    {
                        SyncrioLog.Debug("Loading plugin: " + loadedType.FullName);

                        try
                        {
                            ISyncrioPlugin pluginInstance = ActivatePluginType(loadedType);

                            if (pluginInstance != null)
                            {
                                SyncrioLog.Debug("Loaded plugin: " + loadedType.FullName);

                                loadedPlugins.Add(pluginInstance);
                            }
                        }
                        catch (Exception ex)
                        {
                            SyncrioLog.Error("Error loading plugin " + loadedType.FullName + "(" + loadedType.Assembly.FullName + ") Exception: " + ex.ToString());
                        }
                    }
                }
            }
            SyncrioLog.Debug("Done!");
        }

        private static ISyncrioPlugin ActivatePluginType(Type loadedType)
        {
            try
            {
                //"as ISyncrioPlugin" will cast or return null if the type is not a ISyncrioPlugin
                ISyncrioPlugin pluginInstance = Activator.CreateInstance(loadedType) as ISyncrioPlugin;
                return pluginInstance;
            }
            catch (Exception e)
            {
                SyncrioLog.Error("Cannot activate plugin " + loadedType.Name + ", Exception: " + e.ToString());
                return null;
            }
        }

        //Fire OnUpdate
        public static void FireOnUpdate()
        {
            foreach (var plugin in loadedPlugins)
            {
                try
                {
                    plugin.OnUpdate();
                }
                catch (Exception e)
                {
                    Type type = plugin.GetType();
                    SyncrioLog.Debug("Error thrown in OnUpdate event for " + type.FullName + " (" + type.Assembly.FullName + "), Exception: " + e);
                }
            }
        }

        //Fire OnServerStart
        public static void FireOnServerStart()
        {
            foreach (var plugin in loadedPlugins)
            {
                try
                {
                    plugin.OnServerStart();
                }
                catch (Exception e)
                {
                    Type type = plugin.GetType();
                    SyncrioLog.Debug("Error thrown in OnServerStart event for " + type.FullName + " (" + type.Assembly.FullName + "), Exception: " + e);
                }
            }
        }

        //Fire OnServerStart
        public static void FireOnServerStop()
        {
            foreach (var plugin in loadedPlugins)
            {
                try
                {
                    plugin.OnServerStop();
                }
                catch (Exception e)
                {
                    Type type = plugin.GetType();
                    SyncrioLog.Debug("Error thrown in OnServerStop event for " + type.FullName + " (" + type.Assembly.FullName + "), Exception: " + e);
                }
            }
        }

        //Fire OnClientConnect
        public static void FireOnClientConnect(ClientObject client)
        {
            foreach (var plugin in loadedPlugins)
            {
                try
                {
                    plugin.OnClientConnect(client);
                }
                catch (Exception e)
                {
                    Type type = plugin.GetType();
                    SyncrioLog.Debug("Error thrown in OnClientConnect event for " + type.FullName + " (" + type.Assembly.FullName + "), Exception: " + e);
                }
            }
        }

        //Fire OnClientAuthenticated
        public static void FireOnClientAuthenticated(ClientObject client)
        {
            foreach (var plugin in loadedPlugins)
            {
                try
                {
                    plugin.OnClientAuthenticated(client);
                }
                catch (Exception e)
                {
                    Type type = plugin.GetType();
                    SyncrioLog.Debug("Error thrown in OnClientAuthenticated event for " + type.FullName + " (" + type.Assembly.FullName + "), Exception: " + e);
                }
            }
        }

        //Fire OnClientDisconnect
        public static void FireOnClientDisconnect(ClientObject client)
        {
            foreach (var plugin in loadedPlugins)
            {
                try
                {
                    plugin.OnClientDisconnect(client);
                }
                catch (Exception e)
                {
                    Type type = plugin.GetType();
                    SyncrioLog.Debug("Error thrown in OnClientDisconnect event for " + type.FullName + " (" + type.Assembly.FullName + "), Exception: " + e);
                }
            }
        }

        //Fire OnMessageReceived
        public static void FireOnMessageReceived(ClientObject client, ClientMessage message)
        {
            bool handledByAny = false;
            foreach (var plugin in loadedPlugins)
            {
                try
                {
                    plugin.OnMessageReceived(client, message);

                    //prevent plugins from unhandling other plugin's handled requests
                    if (message.handled)
                    {
                        handledByAny = true;
                    }
                }
                catch (Exception e)
                {
                    Type type = plugin.GetType();
                    SyncrioLog.Debug("Error thrown in OnMessageReceived event for " + type.FullName + " (" + type.Assembly.FullName + "), Exception: " + e);
                }
            }
            message.handled = handledByAny;
        }

        //Fire OnMessageReceived
        public static void FireOnMessageSent(ClientObject client, ServerMessage message)
        {
            foreach (var plugin in loadedPlugins)
            {
                try
                {
                    plugin.OnMessageSent(client, message);
                }
                catch (Exception e)
                {
                    Type type = plugin.GetType();
                    SyncrioLog.Debug("Error thrown in OnMessageSent event for " + type.FullName + " (" + type.Assembly.FullName + "), Exception: " + e);
                }
            }
        }
    }
}

