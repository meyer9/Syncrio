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
using UnityEngine;
using SyncrioCommon;

namespace SyncrioClientSide
{
    public class GroupWindow
    {
        private static GroupWindow singleton;
        public bool workerEnabled = false;
        public bool display = false;
        private bool initialized = false;
        private Vector2 scrollPosition;
        private Vector2 scrollPositionTwo;
        private Vector2 scrollPositionInvite;
        private Vector2 scrollPositionProgress;
        private bool isWindowLocked = false;
        private object groupWindowLock = new object();
        private Dictionary<string, PickPlayerOrGroupButtons> joinButton = new Dictionary<string, PickPlayerOrGroupButtons>();
        private Dictionary<string, PickPlayerOrGroupButtons> invitePlayerButton = new Dictionary<string, PickPlayerOrGroupButtons>();
        public Dictionary<string, PickPlayerOrGroupButtons> kickOrSelectPlayerButton = new Dictionary<string, PickPlayerOrGroupButtons>();
        public Dictionary<string, KickingPlayerStatus> KickingPlayer = new Dictionary<string, KickingPlayerStatus>();
        //GUI
        private GUIStyle windowStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle textAreaStyle;
        private GUIStyle scrollStyle;
        private GUILayoutOption[] labelOptions;
        private GUILayoutOption[] labelOptionsTwo;
        private GUILayoutOption[] layoutOptions;
        private GUILayoutOption[] layoutOptionsTwo;
        private GUILayoutOption[] layoutOptionsThree;
        private Rect windowRect;
        private Rect windowRectChangeLeader;
        private Rect windowRectInvitePlayer;
        private Rect windowRectGroupProgress;
        private Rect moveRect;
        private const float WINDOW_HEIGHT = 400;
        private const float WINDOW_WIDTH = 300;
        private const float WINDOW_HEIGHT_CHANGE_LEADER = 200;
        private const float WINDOW_WIDTH_CHANGE_LEADER = 300;
        private const float WINDOW_HEIGHT_INVITE_PLAYER = 300;
        private const float WINDOW_WIDTH_INVITE_PLAYER = 250;
        public static string groupNameCreate = "Syncrio's #1 Fans!";
        public static string groupPasswordCreate = string.Empty;
        public static string groupNameJoin = string.Empty;
        public static string groupPasswordJoin = string.Empty;
        public static string chosenPlayer = string.Empty;
        public static string chosenPlayerToKick = string.Empty;
        public static string chosenPlayerToInvite = string.Empty;
        public static bool isLeaderKickingPlayer = false;
        public static bool voteKickPlayer = false;
        public static bool voteKeepPlayer = false;
        public static string groupNameRename = string.Empty;
        public static string groupPasswordSet = string.Empty;
        private bool progressButtonPressed = false;
        private string progressButtonSelectedGroup = string.Empty;
        private int progressButtonSelectedSubspace = -1;
        private bool currencyViewButtonPressed = false;
        private bool techViewButtonPressed = false;
        private bool progressViewButtonPressed = false;
        private bool progressBasicViewButtonPressed = false;
        private bool celestialProgressViewButtonPressed = false;
        private bool secretsViewButtonPressed = false;

        public static GroupWindow fetch
        {
            get
            {
                return singleton;
            }
        }

        private void Update()
        {
            if (workerEnabled)
            {
                CheckWindowLock();
            }
        }

        private void Draw()
        {
            if (display)
            {
                if (!initialized)
                {
                    initialized = true;
                    InitGUI();
                    AssignPlayerGroup();
                    SetJoinGroupButton();
                    SetInvitePlayerButton();
                    SetKickOrSelectPlayerButton();
                    SetKickPlayerStatus();
                    CheckInvitePlayerButton();
                }
                windowRect = SyncrioGuiUtil.PreventOffscreenWindow(GUILayout.Window(7714 + Client.WINDOW_OFFSET, windowRect, DrawContent, "Syncrio - Group", windowStyle, layoutOptions));
                if (GroupSystem.groupChangeLeaderWindowDisplay)
                {
                    windowRectChangeLeader = SyncrioGuiUtil.PreventOffscreenWindow(GUILayout.Window(7715 + Client.WINDOW_OFFSET, windowRectChangeLeader, DrawContentChangeLeader, "Syncrio - Group - Become Leader", windowStyle, layoutOptionsTwo));
                }
                if (GroupSystem.invitePlayerButtons || GroupSystem.displayInvite)
                {
                    windowRectInvitePlayer = SyncrioGuiUtil.PreventOffscreenWindow(GUILayout.Window(7716 + Client.WINDOW_OFFSET, windowRectInvitePlayer, DrawContentInvitePlayer, "Syncrio - Group - Invite Player", windowStyle, layoutOptionsThree));
                }
                if (GroupSystem.groupProgressButtons)
                {
                    windowRectGroupProgress = SyncrioGuiUtil.PreventOffscreenWindow(GUILayout.Window(7718 + Client.WINDOW_OFFSET, windowRectGroupProgress, DrawContentGroupProgress, "Syncrio - Group - Progress", windowStyle, layoutOptions));
                }
            }
        }

        public void AssignPlayerGroup()
        {
            GroupSystem.playerGroupAssigned = GroupSystem.fetch.PlayerIsInGroup(Settings.fetch.playerName);
            if (GroupSystem.playerGroupAssigned)
            {
                GroupSystem.playerGroupName = GroupSystem.fetch.GetPlayerGroup(Settings.fetch.playerName);
                PlayerStatusWorker.fetch.myPlayerStatus.groupName = GroupSystem.playerGroupName;
                string groupLeader = GroupSystem.fetch.groups[GroupSystem.playerGroupName].members[0];
                if (groupLeader == Settings.fetch.playerName)
                {
                    GroupSystem.playerIsGroupLeader = true;
                }
                else
                {
                    GroupSystem.playerIsGroupLeader = false;
                }
            }
            else
            {
                GroupSystem.playerGroupName = string.Empty;
            }
        }

        public void SetJoinGroupButton()
        {
            lock (groupWindowLock)
            {
                foreach (KeyValuePair<string, GroupObject> kvp in GroupSystem.fetch.groups)
                {
                    string groupName = kvp.Key;
                    if (!joinButton.ContainsKey(groupName))
                    {
                        joinButton.Add(groupName, new PickPlayerOrGroupButtons());
                    }
                    else
                    {
                        joinButton.Remove(groupName);
                        joinButton.Add(groupName, new PickPlayerOrGroupButtons());
                    }
                }
            }
        }

        public void SetInvitePlayerButton()
        {
            lock (groupWindowLock)
            {
                foreach (PlayerStatus ps in PlayerStatusWorker.fetch.playerStatusList)
                {
                    string playerName = ps.playerName;
                    if (!GroupSystem.fetch.PlayerIsInGroup(playerName))
                    {
                        if (!invitePlayerButton.ContainsKey(playerName))
                        {
                            invitePlayerButton.Add(playerName, new PickPlayerOrGroupButtons());
                        }
                        else
                        {
                            invitePlayerButton.Remove(playerName);
                            invitePlayerButton.Add(playerName, new PickPlayerOrGroupButtons());
                        }
                    }
                }
            }
        }

        public void CheckInvitePlayerButton()
        {
            lock (groupWindowLock)
            {
                foreach (PlayerStatus ps in PlayerStatusWorker.fetch.playerStatusList)
                {
                    string playerName = ps.playerName;
                    if (GroupSystem.fetch.PlayerIsInGroup(playerName))
                    {
                        invitePlayerButton.Remove(playerName);
                    }
                }
            }
        }

        public void SetKickOrSelectPlayerButton()
        {
            lock (groupWindowLock)
            {
                if (GroupSystem.playerGroupAssigned)
                {
                    if (!string.IsNullOrEmpty(GroupSystem.playerGroupName))
                    {
                        foreach (string member in GroupSystem.fetch.groups[GroupSystem.playerGroupName].members)
                        {
                            if (!kickOrSelectPlayerButton.ContainsKey(member))
                            {
                                kickOrSelectPlayerButton.Add(member, new PickPlayerOrGroupButtons());
                            }
                            else
                            {
                                //Keep me empty
                            }
                        }
                    }
                }
            }
        }

        public void SetKickPlayerStatus()
        {
            lock (groupWindowLock)
            {
                if (GroupSystem.playerGroupAssigned)
                {
                    if (!string.IsNullOrEmpty(GroupSystem.playerGroupName))
                    {
                        foreach (string member in GroupSystem.fetch.groups[GroupSystem.playerGroupName].members)
                        {
                            if (!KickingPlayer.ContainsKey(member))
                            {
                                KickingPlayer.Add(member, new KickingPlayerStatus());
                            }
                            else
                            {
                                KickingPlayer.Remove(member);
                                KickingPlayer.Add(member, new KickingPlayerStatus());
                            }
                        }
                    }
                }
            }
        }

        private void InitGUI()
        {
            //Setup GUI stuff
            windowRect = new Rect(Screen.width - WINDOW_WIDTH, Screen.height / 2f - WINDOW_HEIGHT / 2f, WINDOW_WIDTH, WINDOW_HEIGHT);
            windowRectChangeLeader = new Rect(Screen.width * 0.5f - WINDOW_WIDTH_CHANGE_LEADER, Screen.height / 2f - WINDOW_HEIGHT_CHANGE_LEADER / 2f, WINDOW_WIDTH_CHANGE_LEADER, WINDOW_HEIGHT_CHANGE_LEADER);
            windowRectInvitePlayer = new Rect(Screen.width * 0.5f - WINDOW_WIDTH_INVITE_PLAYER, Screen.height / 2f - WINDOW_HEIGHT_INVITE_PLAYER / 2f, WINDOW_WIDTH_INVITE_PLAYER, WINDOW_HEIGHT_INVITE_PLAYER);
            windowRectGroupProgress = new Rect(WINDOW_WIDTH * 2, Screen.height / 2f - WINDOW_HEIGHT / 2f, WINDOW_WIDTH, WINDOW_HEIGHT);
            moveRect = new Rect(0, 0, 10000, 20);
            windowStyle = new GUIStyle(GUI.skin.window);
            labelStyle = new GUIStyle(GUI.skin.label);
            buttonStyle = new GUIStyle(GUI.skin.button);
            textAreaStyle = new GUIStyle(GUI.skin.textArea);
            scrollStyle = new GUIStyle(GUI.skin.scrollView);
            layoutOptions = new GUILayoutOption[4];
            layoutOptions[0] = GUILayout.MinWidth(WINDOW_WIDTH);
            layoutOptions[1] = GUILayout.MaxWidth(WINDOW_WIDTH);
            layoutOptions[2] = GUILayout.MinHeight(WINDOW_HEIGHT);
            layoutOptions[3] = GUILayout.MaxHeight(WINDOW_HEIGHT);

            layoutOptionsTwo = new GUILayoutOption[4];
            layoutOptionsTwo[0] = GUILayout.MinWidth(WINDOW_WIDTH_CHANGE_LEADER);
            layoutOptionsTwo[1] = GUILayout.MaxWidth(WINDOW_WIDTH_CHANGE_LEADER);
            layoutOptionsTwo[2] = GUILayout.MinHeight(WINDOW_HEIGHT_CHANGE_LEADER);
            layoutOptionsTwo[3] = GUILayout.MaxHeight(WINDOW_HEIGHT_CHANGE_LEADER);

            layoutOptionsThree = new GUILayoutOption[4];
            layoutOptionsThree[0] = GUILayout.MinWidth(WINDOW_WIDTH_INVITE_PLAYER);
            layoutOptionsThree[1] = GUILayout.MaxWidth(WINDOW_WIDTH_INVITE_PLAYER);
            layoutOptionsThree[2] = GUILayout.MinHeight(WINDOW_HEIGHT_INVITE_PLAYER);
            layoutOptionsThree[3] = GUILayout.MaxHeight(WINDOW_HEIGHT_INVITE_PLAYER);

            labelOptions = new GUILayoutOption[1];
            labelOptions[0] = GUILayout.Width(100);

            labelOptionsTwo = new GUILayoutOption[2];
            labelOptionsTwo[0] = GUILayout.MinWidth(100);
            labelOptionsTwo[1] = GUILayout.MaxWidth(250);
        }

        private void DrawContent(int windowID)
        {
            GroupSystem.playerGroupAssigned = !string.IsNullOrEmpty(GroupSystem.playerGroupName);
            if (GroupSystem.playerGroupAssigned)
            {
                string tempGroupName = GroupSystem.fetch.GetPlayerGroup(Settings.fetch.playerName);
                GroupSystem.groupFreeToInvite = GroupSystem.fetch.groups[tempGroupName].settings.inviteAvailable;
            }
            GUILayout.BeginVertical();
            GUI.DragWindow(moveRect);
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();

            if (!GroupSystem.playerCreatingGroup)
            {
                if (!GroupSystem.playerGroupAssigned)
                {
                    if (GUILayout.Button("Create a Group", buttonStyle))
                    {
                        GroupSystem.playerCreatingGroup = true;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(20);

                    scrollPosition = GUILayout.BeginScrollView(scrollPosition, scrollStyle);

                    if (GroupSystem.fetch.groups.Count > 0)
                    {
                        foreach (KeyValuePair<string, GroupObject> kvp in GroupSystem.fetch.groups)
                        {
                            GUILayout.BeginHorizontal();
                            string groupName = kvp.Key;
                            GroupPrivacy groupPrivacy = kvp.Value.privacy;
                            GUILayout.Label(groupName, labelOptions);
                            if (joinButton[groupName].pressed)
                            {
                                if (!joinButton[groupName].executed)
                                {
                                    if (GUILayout.Button("Cancel", buttonStyle))
                                    {
                                        joinButton[groupName].pressed = false;
                                    }
                                    if (groupPrivacy == GroupPrivacy.PRIVATE_PASSWORD)
                                    {
                                        GUILayout.EndHorizontal();

                                        GUILayout.BeginHorizontal();
                                        groupPasswordJoin = GUILayout.TextArea(groupPasswordJoin, textAreaStyle);
                                        bool isPassFieldEmpty = string.IsNullOrEmpty(groupPasswordJoin);
                                        if (!isPassFieldEmpty)
                                        {
                                            if (GUILayout.Button("Enter", buttonStyle))
                                            {
                                                joinButton[groupName].executed = true;
                                                groupNameJoin = groupName;
                                                GroupSystem.fetch.JoinGroup();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        joinButton[groupName].executed = true;
                                        groupNameJoin = groupName;
                                        GroupSystem.fetch.JoinGroup();
                                    }
                                }
                            }
                            else
                            {
                                if (groupPrivacy != GroupPrivacy.PRIVATE_INVITE_ONLY)
                                {
                                    if (GUILayout.Button("Join Group", buttonStyle))
                                    {
                                        joinButton[groupName].pressed = true;
                                    }
                                }
                                else
                                {
                                    GUILayout.Label("Invitation only", labelOptions);
                                }
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndScrollView();
                    GUILayout.BeginHorizontal();
                }
                else
                {
                    if (GroupSystem.playerIsGroupLeader)
                    {
                        GUILayout.Label("Welcome " + Settings.fetch.playerName + ", Leader of " + GroupSystem.playerGroupName + "!", labelOptionsTwo);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Your Orders, my Liege.", labelOptions);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GroupSystem.invitePlayerButtons = GUILayout.Toggle(GroupSystem.invitePlayerButtons, "Invite Player", buttonStyle);
                        if (GroupSystem.invitePlayerButtons)
                        {
                            //Do nothing, it is handled elsewhere
                        }
                        GroupSystem.kickPlayerButtons = GUILayout.Toggle(GroupSystem.kickPlayerButtons, "Kick Player", buttonStyle);
                        if (GroupSystem.kickPlayerButtons)
                        {
                            if (GroupSystem.fetch.groups[GroupSystem.playerGroupName].members.Count > 1)
                            {
                                GUILayout.EndHorizontal();
                                scrollPosition = GUILayout.BeginScrollView(scrollPosition, scrollStyle);
                                foreach (string member in GroupSystem.fetch.groups[GroupSystem.playerGroupName].members)
                                {
                                    if (member != Settings.fetch.playerName && member != GroupSystem.fetch.groups[GroupSystem.playerGroupName].members[0])
                                    {
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Label(member, labelOptions);
                                        if (kickOrSelectPlayerButton[member].pressed)
                                        {
                                            if (!kickOrSelectPlayerButton[member].executed)
                                            {
                                                kickOrSelectPlayerButton[member].executed = true;
                                                chosenPlayerToKick = member;
                                                isLeaderKickingPlayer = true;
                                                GroupSystem.fetch.KickPlayer();
                                            }
                                        }
                                        else
                                        {
                                            if (GUILayout.Button("Kick This Player", buttonStyle))
                                            {
                                                kickOrSelectPlayerButton[member].pressed = true;
                                            }
                                        }
                                        GUILayout.EndHorizontal();
                                    }
                                }
                                GUILayout.EndScrollView();
                                GUILayout.BeginHorizontal();
                            }
                        }

                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();

                        GroupSystem.renameGroupButtonPressed = GUILayout.Toggle(GroupSystem.renameGroupButtonPressed, "Rename Group", buttonStyle);
                        if (GroupSystem.renameGroupButtonPressed)
                        {
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            GUILayout.Label("New Group Name:", labelOptions);
                            groupNameRename = GUILayout.TextArea(groupNameRename, 32, textAreaStyle); // Max 32 characters
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            if (!string.IsNullOrEmpty(groupNameRename))
                            {
                                if (GUILayout.Button("Set new name", buttonStyle))
                                {
                                    GroupSystem.fetch.RenameGroup();
                                }
                            }
                            else
                            {
                                GUILayout.Label("Enter a new group name", labelOptions);
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                        }
                        GroupSystem.changeGroupPrivacyButtonPressed = GUILayout.Toggle(GroupSystem.changeGroupPrivacyButtonPressed, "Change Group Privacy", buttonStyle);
                        if (GroupSystem.changeGroupPrivacyButtonPressed)
                        {
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            GroupSystem.newGroupPrivacy_PUBLIC = GUILayout.Toggle(GroupSystem.newGroupPrivacy_PUBLIC, "Public", buttonStyle);
                            if (GroupSystem.newGroupPrivacy_PUBLIC)
                            {
                                GroupSystem.newGroupPrivacy_PASSWORD = false;
                                GroupSystem.newGroupPrivacy_INVITE_ONLY = false;
                            }
                            GroupSystem.newGroupPrivacy_PASSWORD = GUILayout.Toggle(GroupSystem.newGroupPrivacy_PASSWORD, "Password", buttonStyle);
                            if (GroupSystem.newGroupPrivacy_PASSWORD)
                            {
                                GroupSystem.newGroupPrivacy_PUBLIC = false;
                                GroupSystem.newGroupPrivacy_INVITE_ONLY = false;
                            }
                            GroupSystem.newGroupPrivacy_INVITE_ONLY = GUILayout.Toggle(GroupSystem.newGroupPrivacy_INVITE_ONLY, "Invite Only", buttonStyle);
                            if (GroupSystem.newGroupPrivacy_INVITE_ONLY)
                            {
                                GroupSystem.newGroupPrivacy_PUBLIC = false;
                                GroupSystem.newGroupPrivacy_PASSWORD = false;
                            }
                            GUILayout.EndHorizontal();
                            if (GroupSystem.newGroupPrivacy_PASSWORD)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Password:", labelOptions);
                                groupPasswordSet = GUILayout.TextArea(groupPasswordSet, textAreaStyle);
                                GUILayout.EndHorizontal();
                            }
                            else
                            {
                                GUILayout.BeginHorizontal();

                                GUILayout.EndHorizontal();
                            }
                            GUILayout.BeginHorizontal();
                            if (!GroupSystem.newGroupPrivacy_PUBLIC && !GroupSystem.newGroupPrivacy_PASSWORD && !GroupSystem.newGroupPrivacy_INVITE_ONLY)
                            {
                                GUILayout.Label("Please set a Privacy Level.", labelOptionsTwo);
                            }
                            else
                            {
                                if (GroupSystem.newGroupPrivacy_PASSWORD)
                                {
                                    if (!string.IsNullOrEmpty(groupPasswordSet))
                                    {
                                        if (GUILayout.Button("Set new privacy", buttonStyle))
                                        {
                                            GroupSystem.fetch.ChangeGroupPrivacy();
                                        }
                                    }
                                    else
                                    {
                                        GUILayout.Label("Please set a Password.", labelOptions);
                                    }
                                }
                                else
                                {
                                    if (GUILayout.Button("Set new privacy", buttonStyle))
                                    {
                                        GroupSystem.fetch.ChangeGroupPrivacy();
                                    }
                                }
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                        }

                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();

                        GroupSystem.stepDownButton = GUILayout.Toggle(GroupSystem.stepDownButton, "Step Down", buttonStyle);
                        if (GroupSystem.stepDownButton)
                        {
                            if (GroupSystem.fetch.groups[GroupSystem.playerGroupName].members.Count > 1)
                            {
                                GUILayout.EndHorizontal();
                                scrollPositionTwo = GUILayout.BeginScrollView(scrollPositionTwo, scrollStyle);
                                foreach (string member in GroupSystem.fetch.groups[GroupSystem.playerGroupName].members)
                                {
                                    if (member != Settings.fetch.playerName)
                                    {
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Label(member, labelOptions);
                                        if (kickOrSelectPlayerButton[member].pressed)
                                        {
                                            if (!kickOrSelectPlayerButton[member].executed)
                                            {
                                                kickOrSelectPlayerButton[member].executed = true;
                                                chosenPlayer = member;
                                                GroupSystem.fetch.StepDown();
                                            }
                                        }
                                        else
                                        {
                                            if (GUILayout.Button("Pick This Player", buttonStyle))
                                            {
                                                kickOrSelectPlayerButton[member].pressed = true;
                                            }
                                        }
                                        GUILayout.EndHorizontal();
                                    }
                                }
                                GUILayout.EndScrollView();
                                GUILayout.BeginHorizontal();
                            }
                            GroupSystem.kickPlayerButtons = false;
                        }

                        if (!GroupSystem.disbandGroupButtonPressed)
                        {
                            if (GUILayout.Button("Disband Group", buttonStyle))
                            {
                                GroupSystem.disbandGroupButtonPressed = true;
                            }
                        }
                        else
                        {
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            GroupSystem.kickPlayerButtons = false;
                            GUILayout.Label("Disband " + GroupSystem.playerGroupName, labelOptionsTwo);
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Are You Sure?", labelOptions);
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            if (GUILayout.Button("Yes, I'm Sure", buttonStyle))
                            {
                                GroupSystem.fetch.RemoveGroup();
                                GroupSystem.disbandGroupButtonPressed = false;
                            }
                            if (GUILayout.Button("No, I'm Not", buttonStyle))
                            {
                                GroupSystem.disbandGroupButtonPressed = false;
                            }
                        }

                        if (!GroupSystem.stepDownButton && !GroupSystem.kickPlayerButtons)
                        {
                            if (GroupSystem.fetch.groups[GroupSystem.playerGroupName].members.Count > 1)
                            {
                                GUILayout.EndHorizontal();
                                scrollPosition = GUILayout.BeginScrollView(scrollPosition, scrollStyle);
                                for (int i = 0; i < GroupSystem.fetch.groups[GroupSystem.playerGroupName].members.Count; i++)
                                {
                                    if (GroupSystem.fetch.groups[GroupSystem.playerGroupName].members[i] != Settings.fetch.playerName)
                                    {
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Label(GroupSystem.fetch.groups[GroupSystem.playerGroupName].members[i], labelOptions);
                                        if (i == 0)
                                        {
                                            GUILayout.Label("Leader", labelOptions);
                                        }
                                        else
                                        {
                                            GUILayout.Label("Member", labelOptions);
                                        }
                                        GUILayout.EndHorizontal();
                                    }
                                }
                                GUILayout.EndScrollView();
                                GUILayout.BeginHorizontal();
                            }
                        }

                        GUILayout.EndHorizontal();

                        GUILayout.FlexibleSpace();

                        GUILayout.BeginHorizontal();
                        
                        GroupSystem.groupProgressButtons = GUILayout.Toggle(GroupSystem.groupProgressButtons, "Progress View", buttonStyle);
                        if (GroupSystem.groupProgressButtons)
                        {
                            //Do nothing, it is handled elsewhere
                        }
                    }
                    else
                    {
                        GUILayout.Label("Welcome " + Settings.fetch.playerName + ", Member of " + GroupSystem.playerGroupName + ".", labelOptionsTwo);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("What can I do for you today?.", labelOptions);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (GroupSystem.groupFreeToInvite)
                        {
                            GroupSystem.invitePlayerButtons = GUILayout.Toggle(GroupSystem.invitePlayerButtons, "Invite Player", buttonStyle);
                            if (GroupSystem.invitePlayerButtons)
                            {
                                //Do nothing, it is handled elsewhere
                            }
                        }
                        if (GUILayout.Button("Leave Group", buttonStyle))
                        {
                            GroupSystem.fetch.LeaveGroup();
                            GroupSystem.kickPlayerButtons = false;
                        }

                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();

                        GroupSystem.kickPlayerButtons = GUILayout.Toggle(GroupSystem.kickPlayerButtons, "Vote to Kick Player", buttonStyle);

                        if (GroupSystem.kickPlayerButtons)
                        {
                            if (GroupSystem.fetch.groups[GroupSystem.playerGroupName].members.Count > 2)
                            {
                                GUILayout.EndHorizontal();
                                scrollPosition = GUILayout.BeginScrollView(scrollPosition, scrollStyle);
                                foreach (string member in GroupSystem.fetch.groups[GroupSystem.playerGroupName].members)
                                {
                                    if (member != Settings.fetch.playerName && member != GroupSystem.fetch.groups[GroupSystem.playerGroupName].members[0])
                                    {
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Label(member, labelOptions);
                                        if (kickOrSelectPlayerButton[member].pressed)
                                        {
                                            if (!kickOrSelectPlayerButton[member].executed)
                                            {
                                                kickOrSelectPlayerButton[member].executed = true;
                                                chosenPlayerToKick = member;
                                                isLeaderKickingPlayer = false;
                                                GroupSystem.fetch.KickPlayer();
                                            }
                                        }
                                        else
                                        {
                                            if (GUILayout.Button("Vote: Kick", buttonStyle))
                                            {
                                                kickOrSelectPlayerButton[member].pressed = true;
                                                voteKickPlayer = true;
                                                voteKeepPlayer = false;
                                            }
                                            if (KickingPlayer[member].BeingKicked)
                                            {
                                                if (GUILayout.Button("Vote: Keep", buttonStyle))
                                                {
                                                    kickOrSelectPlayerButton[member].pressed = true;
                                                    voteKickPlayer = false;
                                                    voteKeepPlayer = true;
                                                }
                                                GUILayout.EndHorizontal();

                                                GUILayout.BeginHorizontal();
                                                GUILayout.Label(KickingPlayer[member].numberOfVotes + " Total Votes for: " + member, labelOptions);
                                            }
                                        }
                                        GUILayout.EndHorizontal();
                                    }
                                }
                                GUILayout.EndScrollView();
                                GUILayout.BeginHorizontal();
                            }
                        }
                        else
                        {
                            if (GroupSystem.fetch.groups[GroupSystem.playerGroupName].members.Count > 1)
                            {
                                GUILayout.EndHorizontal();
                                scrollPosition = GUILayout.BeginScrollView(scrollPosition, scrollStyle);
                                for (int i = 0; i < GroupSystem.fetch.groups[GroupSystem.playerGroupName].members.Count; i++)
                                {
                                    if (GroupSystem.fetch.groups[GroupSystem.playerGroupName].members[i] != Settings.fetch.playerName)
                                    {
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Label(GroupSystem.fetch.groups[GroupSystem.playerGroupName].members[i], labelOptions);
                                        if (i == 0)
                                        {
                                            GUILayout.Label("Leader", labelOptions);
                                        }
                                        else
                                        {
                                            GUILayout.Label("Member", labelOptions);
                                        }
                                        GUILayout.EndHorizontal();
                                    }
                                }
                                GUILayout.EndScrollView();
                                GUILayout.BeginHorizontal();
                            }
                        }

                        GUILayout.EndHorizontal();

                        GUILayout.FlexibleSpace();

                        GUILayout.BeginHorizontal();

                        GroupSystem.groupProgressButtons = GUILayout.Toggle(GroupSystem.groupProgressButtons, "Progress View", buttonStyle);
                        if (GroupSystem.groupProgressButtons)
                        {
                            //Do nothing, it is handled elsewhere
                        }
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Cancel", buttonStyle))
                {
                    GroupSystem.playerCreatingGroup = false;
                    GroupSystem.setIfGroupIsFreeToInvite = true;
                    GroupSystem.isGroupCreationResponseError = false;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Group Name:", labelOptions);
                groupNameCreate = GUILayout.TextArea(groupNameCreate, 32, textAreaStyle); // Max 32 characters
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GroupSystem.newGroupPrivacy_PUBLIC = GUILayout.Toggle(GroupSystem.newGroupPrivacy_PUBLIC, "Public", buttonStyle);
                if (GroupSystem.newGroupPrivacy_PUBLIC)
                {
                    GroupSystem.newGroupPrivacy_PASSWORD = false;
                    GroupSystem.newGroupPrivacy_INVITE_ONLY = false;
                }
                GroupSystem.newGroupPrivacy_PASSWORD = GUILayout.Toggle(GroupSystem.newGroupPrivacy_PASSWORD, "Password", buttonStyle);
                if (GroupSystem.newGroupPrivacy_PASSWORD)
                {
                    GroupSystem.newGroupPrivacy_PUBLIC = false;
                    GroupSystem.newGroupPrivacy_INVITE_ONLY = false;
                }
                GroupSystem.newGroupPrivacy_INVITE_ONLY = GUILayout.Toggle(GroupSystem.newGroupPrivacy_INVITE_ONLY, "Invite Only", buttonStyle);
                if (GroupSystem.newGroupPrivacy_INVITE_ONLY)
                {
                    GroupSystem.newGroupPrivacy_PUBLIC = false;
                    GroupSystem.newGroupPrivacy_PASSWORD = false;
                }
                GUILayout.EndHorizontal();
                if (GroupSystem.newGroupPrivacy_PASSWORD)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Password:", labelOptions);
                    groupPasswordCreate = GUILayout.TextArea(groupPasswordCreate, textAreaStyle);
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();

                    GUILayout.EndHorizontal();
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label("Set Player alowed to invite: ", labelOptionsTwo);
                if (!GroupSystem.setIfGroupIsFreeToInvite)
                {
                    GroupSystem.setIfGroupIsFreeToInvite = GUILayout.Toggle(GroupSystem.setIfGroupIsFreeToInvite, " ", buttonStyle);
                }
                else
                {
                    GroupSystem.setIfGroupIsFreeToInvite = GUILayout.Toggle(GroupSystem.setIfGroupIsFreeToInvite, "X", buttonStyle);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                bool isNameFieldEmpty = string.IsNullOrEmpty(groupNameCreate);
                bool isPassFieldEmpty = string.IsNullOrEmpty(groupPasswordCreate);
                if (isNameFieldEmpty)
                {
                    GUILayout.Label("Please set a Group Name.", labelOptionsTwo);
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                else
                {
                    if (!GroupSystem.newGroupPrivacy_PUBLIC && !GroupSystem.newGroupPrivacy_PASSWORD && !GroupSystem.newGroupPrivacy_INVITE_ONLY)
                    {
                        GUILayout.Label("Please set a Privacy Level.", labelOptionsTwo);
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                    }
                    else
                    {
                        if (GroupSystem.newGroupPrivacy_PASSWORD)
                        {
                            if (isPassFieldEmpty)
                            {
                                GUILayout.Label("Please set a Password.", labelOptionsTwo);
                                GUILayout.EndHorizontal();
                                GUILayout.BeginHorizontal();
                            }
                            else
                            {
                                if (!GroupSystem.isGroupCreationResponseError)
                                {
                                    if (GUILayout.Button("Finish", buttonStyle))
                                    {
                                        GroupSystem.fetch.CreateGroup();
                                    }
                                }
                                else
                                {
                                    GUILayout.Label(GroupSystem.groupCreationError, labelOptions);
                                }
                            }
                        }
                        else
                        {
                            if (!GroupSystem.isGroupCreationResponseError)
                            {
                                if (GUILayout.Button("Finish", buttonStyle))
                                {
                                    GroupSystem.fetch.CreateGroup();
                                }
                            }
                            else
                            {
                                GUILayout.Label(GroupSystem.groupCreationError, labelOptions);
                            }
                        }
                    }
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawContentChangeLeader(int windowID)
        {
            GUILayout.BeginVertical();
            GUI.DragWindow(moveRect);
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            GUILayout.Label("The group commander: " + GroupSystem.groupLeaderChange + ",", labelOptionsTwo);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("has asked You to become commander of the group: " + GroupSystem.groupnameChange + "!", labelOptionsTwo);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("What will you choose?", labelOptionsTwo);
            GUILayout.EndHorizontal();

            GUILayout.Space(20);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Yes!", buttonStyle))
            {
                GroupSystem.groupChangeLeaderWindowDisplay = false;
                GroupSystem.fetch.ChangeGroupLeaderResponse(true);
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label("Or", labelOptions);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("No.", buttonStyle))
            {
                GroupSystem.groupChangeLeaderWindowDisplay = false;
                GroupSystem.fetch.ChangeGroupLeaderResponse(false);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawContentInvitePlayer(int windowID)
        {
            GUILayout.BeginVertical();
            GUI.DragWindow(moveRect);
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            if (!GroupSystem.displayInvite)
            {
                GUILayout.Label("Invite a Player", labelOptions);
                GUILayout.EndHorizontal();

                GUILayout.Space(20);

                scrollPositionInvite = GUILayout.BeginScrollView(scrollPositionInvite, scrollStyle);

                foreach (KeyValuePair<string, PickPlayerOrGroupButtons> kvp in invitePlayerButton)
                {
                    string playerName = kvp.Key;
                    if (playerName != Settings.fetch.playerName)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(playerName, labelOptions);
                        if (invitePlayerButton[playerName].pressed)
                        {
                            if (!invitePlayerButton[playerName].executed)
                            {
                                invitePlayerButton[playerName].executed = true;
                                chosenPlayerToInvite = playerName;
                                GroupSystem.fetch.InvitePlayer();
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("Invite", buttonStyle))
                            {
                                invitePlayerButton[playerName].pressed = true;
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("You are invited to join group: " + GroupSystem.groupnameInvite, labelOptionsTwo);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("What is your reply?", labelOptions);
                GUILayout.EndHorizontal();

                GUILayout.Space(20);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Yes!", buttonStyle))
                {
                    GroupSystem.displayInvite = false;
                    GroupSystem.fetch.InviteResponse(true);
                }
                GUILayout.FlexibleSpace();
                GUILayout.Label("Or", labelOptions);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("No.", buttonStyle))
                {
                    GroupSystem.displayInvite = false;
                    GroupSystem.fetch.InviteResponse(false);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void DrawContentGroupProgress(int windowID)
        {
            GUILayout.BeginVertical();
            GUI.DragWindow(moveRect);
            GUILayout.Space(20);

            scrollPositionProgress = GUILayout.BeginScrollView(scrollPositionProgress, scrollStyle);

            if (!progressButtonPressed)
            {
                currencyViewButtonPressed = false;
                techViewButtonPressed = false;
                progressViewButtonPressed = false;
                progressBasicViewButtonPressed = false;
                celestialProgressViewButtonPressed = false;
                secretsViewButtonPressed = false;

                GUILayout.BeginHorizontal();
                GUILayout.Label("Select a group to view", labelOptionsTwo);
                GUILayout.EndHorizontal();

                foreach (string key in GroupSystem.fetch.allGroupProgressList.Keys)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(key + ":", labelOptionsTwo);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("View", buttonStyle))
                    {
                        progressButtonPressed = true;
                        progressButtonSelectedGroup = key;
                        scrollPositionProgress = new Vector2();
                    }
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(progressButtonSelectedGroup))
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Back", buttonStyle))
                    {
                        if (progressButtonSelectedSubspace == -1)
                        {
                            progressButtonPressed = false;
                            progressButtonSelectedGroup = string.Empty;
                            progressButtonSelectedSubspace = -1;
                            scrollPositionProgress = new Vector2();
                        }
                        else
                        {
                            progressButtonSelectedSubspace = -1;
                            scrollPositionProgress = new Vector2();

                            currencyViewButtonPressed = false;
                            techViewButtonPressed = false;
                            progressViewButtonPressed = false;
                            progressBasicViewButtonPressed = false;
                            celestialProgressViewButtonPressed = false;
                            secretsViewButtonPressed = false;
                        }
                    }
                    GUILayout.EndHorizontal();

                    if (progressButtonSelectedSubspace == -1)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Select a subspace to view", labelOptionsTwo);
                        GUILayout.EndHorizontal();

                        foreach (int subspace in GroupSystem.fetch.allGroupProgressList[progressButtonSelectedGroup].Keys)
                        {
                            if (subspace != -1)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Subspace " + subspace, labelOptions);
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("Select", buttonStyle))
                                {
                                    progressButtonSelectedSubspace = subspace;
                                    scrollPositionProgress = new Vector2();
                                }
                                GUILayout.EndHorizontal();
                            }
                        }
                    }
                    else
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Select a something to view", labelOptionsTwo);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        currencyViewButtonPressed = GUILayout.Toggle(currencyViewButtonPressed, "Currencies", buttonStyle);
                        if (currencyViewButtonPressed)
                        {
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Funds = " + GroupSystem.fetch.allGroupProgressList[progressButtonSelectedGroup][progressButtonSelectedSubspace].Funds, labelOptionsTwo);
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Rep = " + GroupSystem.fetch.allGroupProgressList[progressButtonSelectedGroup][progressButtonSelectedSubspace].Rep, labelOptionsTwo);
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Sci = " + GroupSystem.fetch.allGroupProgressList[progressButtonSelectedGroup][progressButtonSelectedSubspace].Sci, labelOptionsTwo);
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        techViewButtonPressed = GUILayout.Toggle(techViewButtonPressed, "Tech", buttonStyle);
                        if (techViewButtonPressed)
                        {
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Number of Techs: " + GroupSystem.fetch.allGroupProgressList[progressButtonSelectedGroup][progressButtonSelectedSubspace].Techs.Count, labelOptionsTwo);
                            GUILayout.EndHorizontal();

                            foreach (string tech in GroupSystem.fetch.allGroupProgressList[progressButtonSelectedGroup][progressButtonSelectedSubspace].Techs)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Tech: " + tech, labelOptionsTwo);
                                GUILayout.EndHorizontal();
                            }
                            GUILayout.BeginHorizontal();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        progressViewButtonPressed = GUILayout.Toggle(progressViewButtonPressed, "Progress", buttonStyle);
                        if (progressViewButtonPressed)
                        {
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            if (GUILayout.Button("Basic", buttonStyle))
                            {
                                if (!progressBasicViewButtonPressed)
                                {
                                    progressBasicViewButtonPressed = true;
                                }
                                else
                                {
                                    progressBasicViewButtonPressed = false;
                                }
                                celestialProgressViewButtonPressed = false;
                                secretsViewButtonPressed = false;
                            }

                            if (GUILayout.Button("Celestial", buttonStyle))
                            {
                                progressBasicViewButtonPressed = false;
                                if (!celestialProgressViewButtonPressed)
                                {
                                    celestialProgressViewButtonPressed = true;
                                }
                                else
                                {
                                    celestialProgressViewButtonPressed = false;
                                }
                                secretsViewButtonPressed = false;
                            }

                            if (GUILayout.Button("Secrets", buttonStyle))
                            {
                                progressBasicViewButtonPressed = false;
                                celestialProgressViewButtonPressed = false;
                                if (!secretsViewButtonPressed)
                                {
                                    secretsViewButtonPressed = true;
                                }
                                else
                                {
                                    secretsViewButtonPressed = false;
                                }
                            }

                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();

                            if (progressBasicViewButtonPressed)
                            {
                                GUILayout.Label("Basic Progress", labelOptionsTwo);
                                GUILayout.EndHorizontal();

                                foreach (string basic in GroupSystem.fetch.allGroupProgressList[progressButtonSelectedGroup][progressButtonSelectedSubspace].Progress)
                                {
                                    GUILayout.BeginHorizontal();
                                    GUILayout.Label(basic, labelOptionsTwo);
                                    GUILayout.EndHorizontal();
                                }
                                GUILayout.BeginHorizontal();
                            }
                            if (celestialProgressViewButtonPressed)
                            {
                                GUILayout.Label("Celestial Progress", labelOptionsTwo);
                                GUILayout.EndHorizontal();

                                foreach (List<string> celestial in GroupSystem.fetch.allGroupProgressList[progressButtonSelectedGroup][progressButtonSelectedSubspace].CelestialProgress)
                                {
                                    GUILayout.BeginHorizontal();
                                    GUILayout.Label("Name:" + celestial[0], labelOptionsTwo);
                                    GUILayout.EndHorizontal();
                                    GUILayout.BeginHorizontal();
                                    GUILayout.Label("Reached:" + celestial[1], labelOptionsTwo);
                                    GUILayout.EndHorizontal();

                                    GUILayout.BeginHorizontal();
                                    GUILayout.Label("Achieved:", labelOptionsTwo);
                                    GUILayout.EndHorizontal();

                                    for (int v = 2; v < celestial.Count; v++)
                                    {
                                        GUILayout.BeginHorizontal();
                                        GUILayout.Space(50);
                                        GUILayout.Label(celestial[v], labelOptionsTwo);
                                        GUILayout.EndHorizontal();
                                    }
                                }
                                GUILayout.BeginHorizontal();
                            }
                            if (secretsViewButtonPressed)
                            {
                                GUILayout.Label("Secret Progress", labelOptionsTwo);
                                GUILayout.EndHorizontal();

                                foreach (string secret in GroupSystem.fetch.allGroupProgressList[progressButtonSelectedGroup][progressButtonSelectedSubspace].Secrets)
                                {
                                    GUILayout.BeginHorizontal();
                                    GUILayout.Label(secret, labelOptionsTwo);
                                    GUILayout.EndHorizontal();
                                }
                                GUILayout.BeginHorizontal();
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                else
                {
                    progressButtonPressed = false;
                    progressButtonSelectedGroup = string.Empty;
                    progressButtonSelectedSubspace = -1;
                    scrollPositionProgress = new Vector2();
                }
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        private void CheckWindowLock()
        {
            if (!Client.fetch.gameRunning)
            {
                RemoveWindowLock();
                return;
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                RemoveWindowLock();
                return;
            }

            if (display)
            {
                Vector2 mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;

                bool shouldLockWindow1 = windowRect.Contains(mousePos);
                bool shouldLockWindow2 = windowRectInvitePlayer.Contains(mousePos);
                bool shouldLockWindow3 = windowRectChangeLeader.Contains(mousePos);
                bool shouldLockWindow4 = windowRectGroupProgress.Contains(mousePos);

                if ((shouldLockWindow1 || shouldLockWindow2 || shouldLockWindow3 || shouldLockWindow4) && !isWindowLocked)
                {
                    InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "Syncrio_GroupWindowLock");
                    isWindowLocked = true;
                }
                if (!(shouldLockWindow1 && shouldLockWindow2 && shouldLockWindow3 && shouldLockWindow4) && isWindowLocked)
                {
                    RemoveWindowLock();
                }
            }

            if (!display && isWindowLocked)
            {
                RemoveWindowLock();
            }
        }

        private void RemoveWindowLock()
        {
            if (isWindowLocked)
            {
                isWindowLocked = false;
                InputLockManager.RemoveControlLock("Syncrio_GroupWindowLock");
            }
        }

        public static void Reset()
        {
            lock (Client.eventLock)
            {
                if (singleton != null)
                {
                    singleton.workerEnabled = false;
                    Client.drawEvent.Remove(singleton.Draw);
                    Client.updateEvent.Remove(singleton.Update);
                }
                singleton = new GroupWindow();
                Client.drawEvent.Add(singleton.Draw);
                Client.updateEvent.Add(singleton.Update);
            }
        }

        public class PickPlayerOrGroupButtons
        {
            public bool pressed = false;
            public bool executed = false;
        }

        public class KickingPlayerStatus
        {
            public bool BeingKicked = false;
            public int numberOfVotes = 0;
        }
    }
}
