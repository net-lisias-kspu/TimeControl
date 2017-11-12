﻿/*
All code in this file Copyright(c) 2016 Nate West

The MIT License (MIT)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

using System;
using System.Collections;
using System.Collections.Generic;
using SC = System.ComponentModel;
using System.Reflection;
using System.Linq;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;
using KSP.UI.Dialogs;
using KSPPluginFramework;

using TimeControl.KeyBindings;

namespace TimeControl
{
    internal class KeyBindingsEditorIMGUI
    {
        private bool currentlyAssigningKey = false;
        private bool refreshKBCache = true;
        private List<TimeControlKeyBinding> keyBindings = new List<TimeControlKeyBinding>();

        private Vector2 userDefinedScroll = new Vector2();
        private bool addingNewKeyBinding = false;

        private List<KeyBindingsAddIMGUI> userDefinedKBAdd;

        public KeyBindingsEditorIMGUI()
        {
            ResetUserDefinedKBAdd();
        }

        private void ResetUserDefinedKBAdd()
        {
            userDefinedKBAdd = userDefinedKBAdd ?? new List<KeyBindingsAddIMGUI>();
            userDefinedKBAdd.Clear();

            userDefinedKBAdd.Add( new KeyBindingsAddIMGUI( new HyperRateSetRate() { IsUserDefined = true } ) );
            userDefinedKBAdd.Add( new KeyBindingsAddIMGUI( new HyperRateSlowDown() { IsUserDefined = true } ) );
            userDefinedKBAdd.Add( new KeyBindingsAddIMGUI( new HyperRateSpeedUp() { IsUserDefined = true } ) );

            userDefinedKBAdd.Add( new KeyBindingsAddIMGUI( new HyperPhysicsAccuracySet() { IsUserDefined = true } ) );
            userDefinedKBAdd.Add( new KeyBindingsAddIMGUI( new HyperPhysicsAccuracyUp() { IsUserDefined = true } ) );
            userDefinedKBAdd.Add( new KeyBindingsAddIMGUI( new HyperPhysicsAccuracyDown() { IsUserDefined = true } ) );

            userDefinedKBAdd.Add( new KeyBindingsAddIMGUI( new SlowMoSetRate() { IsUserDefined = true } ) );
            userDefinedKBAdd.Add( new KeyBindingsAddIMGUI( new SlowMoSpeedUp() { IsUserDefined = true } ) );
            userDefinedKBAdd.Add( new KeyBindingsAddIMGUI( new SlowMoSlowDown() { IsUserDefined = true } ) );

            userDefinedKBAdd.Add( new KeyBindingsAddIMGUI( new WarpToVesselOrbitLocation( WarpToVesselOrbitLocation.VesselOrbitLocation.Ap ) { IsUserDefined = true } ) );
            userDefinedKBAdd.Add( new KeyBindingsAddIMGUI( new WarpToVesselOrbitLocation( WarpToVesselOrbitLocation.VesselOrbitLocation.Pe ) { IsUserDefined = true } ) );
            userDefinedKBAdd.Add( new KeyBindingsAddIMGUI( new WarpToVesselOrbitLocation( WarpToVesselOrbitLocation.VesselOrbitLocation.AN ) { IsUserDefined = true } ) );
            userDefinedKBAdd.Add( new KeyBindingsAddIMGUI( new WarpToVesselOrbitLocation( WarpToVesselOrbitLocation.VesselOrbitLocation.DN ) { IsUserDefined = true } ) );
            userDefinedKBAdd.Add( new KeyBindingsAddIMGUI( new WarpToVesselOrbitLocation( WarpToVesselOrbitLocation.VesselOrbitLocation.SOI ) { IsUserDefined = true } ) );
            userDefinedKBAdd.Add( new KeyBindingsAddIMGUI( new WarpToVesselOrbitLocation( WarpToVesselOrbitLocation.VesselOrbitLocation.ManuverNode ) { IsUserDefined = true } ) );

            userDefinedKBAdd.Add( new KeyBindingsAddIMGUI( new WarpForNOrbits() { IsUserDefined = true } ) );
            userDefinedKBAdd.Add( new KeyBindingsAddIMGUI( new WarpForNSeconds() { IsUserDefined = true } ) );
        }

        private void CacheKeyBinds()
        {
            if (keyBindings == null || refreshKBCache)
            {
                keyBindings.Clear();
                keyBindings.AddRange(KeyboardInputManager.Instance.GetActiveKeyBinds());
                refreshKBCache = false;
            }
        }

        public void KeyBindingsEditorGUI()
        {
            if (!KeyboardInputManager.IsReady)
            {
                return;
            }

            bool guiPriorEnabled = GUI.enabled;
            Color guiPriorColor = GUI.contentColor;

            GUI.enabled = guiPriorEnabled
                && HyperWarpController.IsReady
                && !HyperWarpController.Instance.IsHyperWarping
                && RailsWarpController.IsReady
                && !RailsWarpController.Instance.IsRailsWarping
                && SlowMoController.IsReady
                && !SlowMoController.Instance.IsSlowMo;

            CacheKeyBinds();
            
            GUI.enabled = guiPriorEnabled && !currentlyAssigningKey;

            if (!addingNewKeyBinding)
            {
                if (GUILayout.Button( "Reset Key Bindings" ))
                {
                    KeyboardInputManager.Instance.ResetKeyBindingsToDefault();
                    refreshKBCache = true;
                }

                if (GUILayout.Button( "Add New User-Defined Action"))
                {
                    addingNewKeyBinding = true;
                    ResetUserDefinedKBAdd();
                }
            }
            
            GUILayout.Label( "", GUILayout.Height( 5 ) );

            if (addingNewKeyBinding)
            {
                GUILayout.Label( "Add New User-Defined Key Binding Action" );
                if (GUILayout.Button( "Cancel" ))
                {
                    addingNewKeyBinding = false;
                }

                foreach (KeyBindingsAddIMGUI kbadd in userDefinedKBAdd)
                {
                    if (kbadd.KeyBindingsAddGUI())
                    {
                        addingNewKeyBinding = false;
                        refreshKBCache = true;
                    }
                }
            }
            else
            {
                GUILayout.Label( "Key Bindings: Left-Click Set, Right-Click Clear" );
                GUILayout.Label( "Built-In Actions:" );

                GUILayout.BeginVertical();
                {
                    foreach (TimeControlKeyBinding kb in keyBindings.Where( k => k.IsUserDefined == false ))
                    {
                        if (kb is WarpToNextKACAlarm)
                        {
                            GUI.enabled = guiPriorEnabled && KACWrapper.InstanceExists;
                        }

                        GUI.contentColor = (kb.IsKeyAssigned ? Color.yellow : guiPriorColor);
                        string buttonDesc = kb.Description.MemoizedConcat( ": " ).MemoizedConcat( kb.IsKeyAssigned ? kb.KeyCombinationDescription : "None" );
                        if (GUILayout.Button( buttonDesc ))
                        {
                            GUIAssignKey( buttonDesc, kb );
                        }

                        GUI.enabled = guiPriorEnabled;
                    }
                }
                GUILayout.EndVertical();

                GUILayout.Label( "", GUILayout.Height( 5 ) );
                GUILayout.Label( "User-Defined Actions:" );

                userDefinedScroll = GUILayout.BeginScrollView( userDefinedScroll, GUILayout.Height( 210 ) );
                {
                    GUILayout.BeginVertical();
                    {
                        foreach (TimeControlKeyBinding kb in keyBindings.Where( k => k.IsUserDefined == true ))
                        {
                            GUILayout.BeginHorizontal();
                            {
                                if (GUILayout.Button( "-", GUILayout.Width( 25 ) ))
                                {
                                    KeyboardInputManager.Instance.DeleteKeyBinding( kb );
                                    refreshKBCache = true;
                                }
                                GUI.contentColor = (kb.IsKeyAssigned ? Color.yellow : guiPriorColor);
                                string buttonDesc = kb.Description.MemoizedConcat( ": " ).MemoizedConcat( kb.IsKeyAssigned ? kb.KeyCombinationDescription : "None" );
                                if (GUILayout.Button( buttonDesc ))
                                {
                                    GUIAssignKey( buttonDesc, kb );
                                }
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndScrollView();
            }
            GUI.contentColor = guiPriorColor;
            GUI.enabled = guiPriorEnabled;
        }

        private void GUIAssignKey(string buttonDesc, TimeControlKeyBinding kb)
        {
            const string logBlockName = nameof ( KeyBindingsEditorIMGUI ) + "." + nameof( KeyBindingsEditorIMGUI.GUIAssignKey );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                // Left Mouse Button, Assign Key
                if (Event.current.button == 0 && !currentlyAssigningKey)
                {
                    currentlyAssigningKey = true;
                    KeyboardInputManager.Instance.GetPressedKeyCombination( (lkc) =>
                    {
                        const string logBlockName2 = nameof( KeyBindingsEditorIMGUI ) + "." + nameof( KeyBindingsEditorIMGUI.GUIAssignKey ) + " - " + nameof ( KeyboardInputManager.Instance.GetPressedKeyCombination ) + " Callback";
                        using (EntryExitLogger.EntryExitLog( logBlockName2, EntryExitLoggerOptions.All ))
                        {
                            currentlyAssigningKey = false;
                            kb.KeyCombination = new List<KeyCode>( lkc );
                            refreshKBCache = true;
                            Log.Info( "Key Combination " + kb.KeyCombinationDescription + " assigned to button " + buttonDesc, logBlockName2 );

                            TimeControlEvents.OnTimeControlKeyBindingsChanged?.Fire( kb );
                        }
                    } );
                }
                // Right Mouse Button, Clear Assigned Key
                else if (Event.current.button == 1 && !currentlyAssigningKey)
                {
                    kb.KeyCombination = new List<KeyCode>();
                    TimeControlEvents.OnTimeControlKeyBindingsChanged?.Fire( kb );
                }
            }
        }
    }
}
