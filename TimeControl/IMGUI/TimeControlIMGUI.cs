﻿/*
All code in this file Copyright(c) 2016 Nate West
Rewritten from scratch, but based on code Copyright(c) 2014 Xaiier using the same license as below

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
using TimeControl.Framework;

namespace TimeControl
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    internal class TimeControlIMGUI : MonoBehaviour
    {

        #region Singleton
        private static TimeControlIMGUI instance;
        internal static TimeControlIMGUI Instance { get { return instance; } }
        public static bool IsReady { get; private set; } = false;
        #endregion Singleton

        #region Public Properties
        public bool WindowVisible { get; set; } = false;
        public bool SettingsWindowOpen { get; set; } = false;
        public bool GUITempHidden { get => tempGUIHidden.Count != 0; }
        #endregion Public Properties

        #region Public Methods
        public void ToggleGUIVisibility()
        {
            WindowVisible = !WindowVisible;
        }

        public void SetGUIVisibility(bool v)
        {
            WindowVisible = v;
        }

        public void TempHideGUI(string lockedBy)
        {
            tempGUIHidden.Add(lockedBy);
        }

        public void TempUnHideGUI(string lockedBy)
        {
            tempGUIHidden.RemoveAll(x => x == lockedBy);
        }

        public void TempUnHideGUI()
        {
            tempGUIHidden.Clear();
        }
        #endregion

        private enum GUIMode
        {
            RailsEditor = 1,            
            HyperWarp = 2,
            SlowMotion = 3,
            RailsWarpTo = 4,
            Details = 5,
            KeyBindingsEditor = 6,
            QuickWarp = 7
        }
        
        #region Fields
        // Temp Hide/Show GUI Windows
        private List<string> tempGUIHidden = new List<string>();

        //private bool windowsVisible = false;
        //private int windowSelectedFlightMode = 0;

        //private bool useCustomDateTimeFormatter = false;

        // Date Time Formatter
        //private TCDateTimeFormatter customDTFormatter = new TCDateTimeFormatter();
        //private IDateTimeFormatter defaultDTFormatter;

        //GUI Layout        
        private bool priorWindowVisible = false;

        private static Rect mode0Button = new Rect(10, -1, 25, 20);
        private static Rect mode1Button = new Rect(25, -1, 25, 20);
        private static Rect mode2Button = new Rect(40, -1, 25, 20);
        private static Rect mode3Button = new Rect( 55, -1, 25, 20 );
        private static Rect mode4Button = new Rect( 70, -1, 25, 20 );
        private static Rect mode5Button = new Rect( 85, -1, 25, 20 );
        private static Rect mode6Button = new Rect( 100, -1, 25, 20 );

        private Rect windowRect = new Rect(100, 100, 375, 0);

        private int tcsWindowHashCode = "Time Control IMGUI".GetHashCode();
        
        private RailsEditorIMGUI railsEditorGUI;
        private HyperIMGUI hyperGUI;
        private DetailsIMGUI detailsGUI;
        private SlowMoIMGUI slomoGUI;
        private RailsWarpToIMGUI railsWarpToGUI;
        private KeyBindingsEditorIMGUI keyBindingsGUI;
        private QuickWarpToIMGUI quickWarpToGUI;

        private float flightModeWindow_x = 100;
        private float flightModeWindow_y = 100;
        private float spaceCenterWindow_x = 100;
        private float spaceCenterWindow_y = 100;
        private float trackingStationWindow_x = 100;
        private float trackingStationWindow_y = 100;
        private bool spaceCenterWindowIsDisplayed = false;
        private bool trackingStationWindowIsDisplayed = false;
        private bool flightModeWindowIsDisplayed = false;

        private bool windowLocationSettingsNeedUpdate = false;

        private EventData<bool> OnTimeControlGlobalSettingsLoadedEvent;
        #endregion

        //private GUIMode priorWindowSelectedMode = GUIMode.RailsWarpTo;
        private GUIMode windowSelectedMode = GUIMode.RailsWarpTo;
        private GUIMode WindowSelectedMode
        {
            get => windowSelectedMode;
            set
            {
                if (windowSelectedMode != value)
                {
                    windowSelectedMode = value;
                }
            }
        }

        #region Private Methods

        private void SetDateTimeFormatter()
        {
            /*
            if (UseCustomDateTimeFormatter)
            {
                Log.Info( "Changing Date Time Formatter to customDTFormatter" );
                KSPUtil.dateTimeFormatter = customDTFormatter;
                // Only run this test in trace mode
                if (Log.LoggingLevel == LogSeverity.Trace)
                    TestDateTimeDisplay.RunDateTimeDisplayTest(HighLogic.CurrentGame.UniversalTime);
            }
            else
            {
                Log.Info( "Changing Date Time Formatter to defaultDTFormatter" );
                KSPUtil.dateTimeFormatter = defaultDTFormatter;
                // Only run this test in trace mode
                if (Log.LoggingLevel == LogSeverity.Trace)
                    TestDateTimeDisplay.RunDateTimeDisplayTest(HighLogic.CurrentGame.UniversalTime);
            }
            */
        }

        private bool SupressFlightResultsDialog
        {
            get => HighLogic.CurrentGame?.Parameters?.CustomParams<TimeControlParameterNode>()?.SupressFlightResultsDialog ?? true;
        }
        
        private double PhysicsTimeRatio
        {
            get => (PerformanceManager.IsReady ? PerformanceManager.Instance?.PhysicsTimeRatio ?? 0.0 : 0.0);
        }

        private double FramesPerSecond
        {
            get => (PerformanceManager.IsReady ? PerformanceManager.Instance?.FramesPerSecond ?? 0.0 : 0.0);
        }

        public bool KACAPIIntegrated { get; set; } = false;
        public bool TriedToLoadKAC { get; set; } = false;

        #endregion
        #region MonoBehavior and related private methods
        #region One-Time
        private void Awake()
        {
            const string logBlockName = nameof( TimeControlIMGUI ) + "." + nameof( Awake );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                UnityEngine.Object.DontDestroyOnLoad( this ); //Don't go away on scene changes
                instance = this;
            }
        }

        private void Start()
        {
            const string logBlockName = nameof( TimeControlIMGUI ) + "." + nameof( Start );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                // Hide / Show UI on these events
                global::GameEvents.onGameSceneLoadRequested.Add( this.onGameSceneLoadRequested );
                global::GameEvents.onLevelWasLoaded.Add( this.onLevelWasLoaded );
                global::GameEvents.onHideUI.Add( this.onHideUI );
                global::GameEvents.onShowUI.Add( this.onShowUI );
                //global::GameEvents.OnGameSettingsApplied.Add( this.OnGameSettingsApplied );

                //defaultDTFormatter = KSPUtil.dateTimeFormatter;

                StartCoroutine( StartAfterSettingsAndControllerAreReady() );
            }
        }
        private void OnDestroy()
        {
            OnTimeControlGlobalSettingsLoadedEvent?.Remove( OnTimeControlGlobalSettingsLoaded );
        }

        private void OnTimeControlGlobalSettingsLoaded(bool b)
        {
            const string logBlockName = nameof( TimeControlIMGUI ) + "." + nameof( OnTimeControlGlobalSettingsLoaded );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                LoadSettings();
            }
        }

        private void LoadSettings()
        {
            const string logBlockName = nameof( TimeControlIMGUI ) + "." + nameof( LoadSettings );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                spaceCenterWindow_x = GlobalSettings.Instance.SpaceCenterWindow_x;
                spaceCenterWindow_y = GlobalSettings.Instance.SpaceCenterWindow_y;
                trackingStationWindow_x = GlobalSettings.Instance.TrackingStationWindow_x;
                trackingStationWindow_y = GlobalSettings.Instance.TrackingStationWindow_y;
                flightModeWindow_x = GlobalSettings.Instance.FlightModeWindow_x;
                flightModeWindow_y = GlobalSettings.Instance.FlightModeWindow_y;
                spaceCenterWindowIsDisplayed = GlobalSettings.Instance.SpaceCenterWindowIsDisplayed;
                trackingStationWindowIsDisplayed = GlobalSettings.Instance.TrackingStationWindowIsDisplayed;
                flightModeWindowIsDisplayed = GlobalSettings.Instance.FlightModeWindowIsDisplayed;                
            }
        }

        private void SaveSettings()
        {
            const string logBlockName = nameof( TimeControlIMGUI ) + "." + nameof( SaveSettings );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {   
                GlobalSettings.Instance.SpaceCenterWindow_x = spaceCenterWindow_x;
                GlobalSettings.Instance.SpaceCenterWindow_y = spaceCenterWindow_y;
                GlobalSettings.Instance.TrackingStationWindow_x = trackingStationWindow_x;
                GlobalSettings.Instance.TrackingStationWindow_y = trackingStationWindow_y;
                GlobalSettings.Instance.FlightModeWindow_x = flightModeWindow_x;
                GlobalSettings.Instance.FlightModeWindow_y = flightModeWindow_y;
                GlobalSettings.Instance.SpaceCenterWindowIsDisplayed = spaceCenterWindowIsDisplayed;
                GlobalSettings.Instance.TrackingStationWindowIsDisplayed = trackingStationWindowIsDisplayed;
                GlobalSettings.Instance.FlightModeWindowIsDisplayed = flightModeWindowIsDisplayed;

                GlobalSettings.Instance.Save();
            }
        }

        /// <summary>
        /// Configures the GUI once the Settings are loaded and the TimeController is ready to operate
        /// </summary>
        private IEnumerator StartAfterSettingsAndControllerAreReady()
        {
            const string logBlockName = nameof( TimeControlIMGUI ) + "." + nameof( StartAfterSettingsAndControllerAreReady );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                // Wait for TimeController object to be ready
                while (!TimeController.IsReady || !RailsWarpController.IsReady || !SlowMoController.IsReady || !HyperWarpController.IsReady || !GlobalSettings.IsReady)
                {
                    yield return null;
                }
                
                OnTimeControlGlobalSettingsLoadedEvent = GameEvents.FindEvent<EventData<bool>>( nameof( TimeControlEvents.OnTimeControlGlobalSettingsLoaded ) );
                OnTimeControlGlobalSettingsLoadedEvent?.Add( OnTimeControlGlobalSettingsLoaded );

                railsWarpToGUI = new RailsWarpToIMGUI();
                railsEditorGUI = new RailsEditorIMGUI();
                slomoGUI = new SlowMoIMGUI();
                hyperGUI = new HyperIMGUI();
                detailsGUI = new DetailsIMGUI();
                keyBindingsGUI = new KeyBindingsEditorIMGUI();
                quickWarpToGUI = new QuickWarpToIMGUI();

                Log.Info( "TCGUI.Instance is Ready!", logBlockName );
                IsReady = true;

            }
            yield break;
        }
        #endregion

        #region Event Handlers
        private void onGameSceneLoadRequested(GameScenes gs)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( onGameSceneLoadRequested );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                onHideUI();
            }
        }

        private void onLevelWasLoaded(GameScenes gs)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( onLevelWasLoaded );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {                
                if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
                {
                    windowRect.x = spaceCenterWindow_x;
                    windowRect.y = spaceCenterWindow_y;
                    WindowVisible = spaceCenterWindowIsDisplayed;
                }
                if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
                {
                    windowRect.x = trackingStationWindow_x;
                    windowRect.y = trackingStationWindow_y;
                    WindowVisible = trackingStationWindowIsDisplayed;
                }
                if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                {
                    windowRect.x = flightModeWindow_x;
                    windowRect.y = flightModeWindow_y;
                    WindowVisible = flightModeWindowIsDisplayed;
                }
                windowRect.ClampToScreen();
                onShowUI();
            }
        }

        private void onHideUI()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( onHideUI );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Hiding GUI for Settings Lock", logBlockName );
                TempHideGUI( "GameEventsUI" );
            }
        }

        private void onShowUI()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( onShowUI );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Unhiding GUI for Settings Lock", logBlockName );
                TempUnHideGUI( "GameEventsUI" );
            }
        }
        #endregion

        #region Update Methods
        private void Update()
        {
            if (!IsReady || GUITempHidden || !TimeController.IsReady || !WindowVisible)
            {
                return;
            }

            if (!TriedToLoadKAC)
            {
                SetupKACAlarms();
            }
        }

        internal KACWrapper.KACAPI.KACAlarm ClosestKACAlarm { get; private set; }

        private void SetupKACAlarms()
        {
            const string logBlockName = nameof( TimeControlIMGUI ) + "." + nameof( SetupKACAlarms );

            TriedToLoadKAC = true;
            KACAPIIntegrated = KACWrapper.InitKACWrapper();
            if (KACAPIIntegrated)
            {
                StartCoroutine( CheckKACAlarms() );
                Log.Info( "KAC Integrated With TimeControl", logBlockName );
            }
            else
            {
                Log.Info( "KAC Not Integrated With TimeControl", logBlockName );
            }
        }

        private IEnumerator CheckKACAlarms()
        {
            const string logBlockName = nameof( TimeControlIMGUI ) + "." + nameof( CheckKACAlarms );

            while (true)
            {
                if (KACAPIIntegrated && (WindowSelectedMode == GUIMode.RailsWarpTo || WindowSelectedMode == GUIMode.QuickWarp))
                {
                    var list = KACWrapper.KAC.Alarms.Where( f => f.AlarmTime > Planetarium.GetUniversalTime() && f.AlarmType != KACWrapper.KACAPI.AlarmTypeEnum.EarthTime ).OrderBy( f => f.AlarmTime );
                    if (list != null && list.Count() != 0)
                    {
                        var upNextAlarm = list.First();
                        if (ClosestKACAlarm == null || ClosestKACAlarm.ID != upNextAlarm.ID)
                        {
                            Log.Info( "Updating Next KAC Alarm", logBlockName );
                            ClosestKACAlarm = upNextAlarm;
                        }
                    }
                    else if (ClosestKACAlarm != null)
                    {
                        Log.Info( "Clearing Next KAC Alarm", logBlockName );
                        ClosestKACAlarm = null;
                    }
                }

                yield return new WaitForSeconds( 1f );
            }
        }
        #endregion
        
        #region GUI Methods
        private void OnGUI()
        {
            // Don't do anything until the settings are loaded or we can actally warp
            if (!IsReady || GUITempHidden || !TimeController.IsReady)
            {
                return;
            }

            // Don't show GUI unless we are in the appropriate scene
            if (HighLogic.LoadedScene != GameScenes.FLIGHT && HighLogic.LoadedScene != GameScenes.SPACECENTER && HighLogic.LoadedScene != GameScenes.TRACKSTATION)
            {
                return;
            }

            UnityEngine.GUI.skin = null;
            if (WindowVisible)
            {
                if (PerformanceManager.IsReady)
                {
                    PerformanceManager.Instance.PerformanceCountersOn = true;
                }
                OnGUIWindow();
            }
            else
            {
                if (PerformanceManager.IsReady)
                {
                    PerformanceManager.Instance.PerformanceCountersOn = false;
                }
            }
            UnityEngine.GUI.skin = HighLogic.Skin;

            if (WindowVisible != priorWindowVisible)
            {
                priorWindowVisible = WindowVisible;

                if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
                {
                    spaceCenterWindowIsDisplayed = WindowVisible;
                }
                if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)                    
                {
                    trackingStationWindowIsDisplayed = WindowVisible;
                }
                if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                {
                    flightModeWindowIsDisplayed = WindowVisible;
                }
                SaveSettings();
            }

            OnGUIMouseEvents();
        }

        private void OnGUIWindow()
        {
            windowRect = GUILayout.Window(tcsWindowHashCode, windowRect, MainGUI, "Time Control");
            windowRect.ClampToScreen();
            
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                if (windowRect.x != spaceCenterWindow_x)
                {
                    spaceCenterWindow_x = windowRect.x;
                    windowLocationSettingsNeedUpdate = true;
                }
                if (windowRect.y != spaceCenterWindow_y)
                {
                    spaceCenterWindow_y = windowRect.y;
                    windowLocationSettingsNeedUpdate = true;
                }
            }
            if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                if (windowRect.x != trackingStationWindow_x)
                {
                    trackingStationWindow_x = windowRect.x;
                    windowLocationSettingsNeedUpdate = true;
                }
                if (windowRect.y != trackingStationWindow_y)
                {
                    trackingStationWindow_y = windowRect.y;
                    windowLocationSettingsNeedUpdate = true;
                }
            }
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                if (windowRect.x != flightModeWindow_x)
                {
                    flightModeWindow_x = windowRect.x;
                    windowLocationSettingsNeedUpdate = true;
                }
                if (windowRect.y != flightModeWindow_y)
                {
                    flightModeWindow_y = windowRect.y;
                    windowLocationSettingsNeedUpdate = true;
                }
            }
        }

        private void OnGUIMouseEvents()
        {
            if (Event.current.type == EventType.mouseUp && Event.current.button == 0)
            {
                if (windowLocationSettingsNeedUpdate)
                {
                    SaveSettings();
                }
            }
        }

        #region Main GUI
        private void MainGUI(int windowId)
        {
            UnityEngine.GUI.enabled = true;

            GUIHeaderButtons();
            GUIHeaderCurrentWarpState();


            if ((HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER) && (WindowSelectedMode == GUIMode.SlowMotion || WindowSelectedMode == GUIMode.HyperWarp))
            {
                WindowSelectedMode = GUIMode.RailsWarpTo;
            }

            switch (WindowSelectedMode)
            {
                case GUIMode.SlowMotion:
                    slomoGUI.SlowMoGUI();
                    break;
                case GUIMode.HyperWarp:
                    hyperGUI.HyperGUI();
                    break;
                case GUIMode.RailsEditor:
                    railsEditorGUI.RailsEditorGUI();
                    break;
                case GUIMode.RailsWarpTo:
                    railsWarpToGUI.WarpToGUI();
                    break;
                case GUIMode.Details:
                    detailsGUI.DetailsGUI();
                    break;
                case GUIMode.KeyBindingsEditor:
                    keyBindingsGUI.KeyBindingsEditorGUI();
                    break;
                case GUIMode.QuickWarp:
                    quickWarpToGUI.WarpToGUI();
                    break;
            }
            
            UnityEngine.GUI.enabled = true;

            if (Event.current.button > 0 && Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout) //Ignore right & middle clicks
                Event.current.Use();

            UnityEngine.GUI.DragWindow();
        }

        private void GUIHeaderCurrentWarpState()
        {
            GUILayout.BeginHorizontal();
            {
                if (RailsWarpController.Instance.IsRailsWarpingNoPhys)
                {
                    string rate = RailsWarpController.Instance.CurrentWarpRate.MemoizedToString().MemoizedConcat( "x" );
                    GUILayout.Label( "Rails: ".MemoizedConcat( rate ) );
                }
                else if (RailsWarpController.Instance.IsRailsWarpingPhys)
                {
                    string rate = RailsWarpController.Instance.CurrentWarpRate.MemoizedToString().MemoizedConcat( "x" );
                    GUILayout.Label( "KSP-Phys: ".MemoizedConcat( rate ) );
                }
                else
                {
                    if (PerformanceManager.Instance?.PerformanceCountersOn ?? false)
                    {
                        string rate = ((PhysicsTimeRatio / 1 * 100).MemoizedToString( "0" )).MemoizedConcat( "%" );
                        GUILayout.Label( "PTR: ".MemoizedConcat( rate ) );
                    }
                    else
                    {
                        GUILayout.Label( "PTR: N/A" );
                    }
                }

                if (PerformanceManager.Instance?.PerformanceCountersOn ?? false)
                {
                    GUILayout.Label( "FPS: ".MemoizedConcat( (Mathf.Floor( Convert.ToSingle( FramesPerSecond ) )).MemoizedToString() ) );
                }
                else
                {
                    GUILayout.Label( "FPS: N/A" );
                }

                GUILayout.FlexibleSpace();

                GUIPauseOrResumeButton();
                GUITimeStepButton();
                GUIReturnToRealtimeButton();
            }
            GUILayout.EndHorizontal();
        }

        private void GUIHeaderButtons()
        {
            Color bc = UnityEngine.GUI.backgroundColor;
            Color cc = UnityEngine.GUI.contentColor;

            UnityEngine.GUI.backgroundColor = Color.clear;
            
            //Details mode
            {
                if (WindowSelectedMode != GUIMode.Details)
                {
                    UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );
                }
                if (UnityEngine.GUI.Button( mode0Button, "?" ))
                {
                    WindowSelectedMode = GUIMode.Details;
                    windowRect.height = 0;
                }
                UnityEngine.GUI.contentColor = cc;
            }
            //Rails QuickWarp mode
            {
                if (WindowSelectedMode != GUIMode.QuickWarp)
                {
                    UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );
                }
                if (UnityEngine.GUI.Button( mode1Button, "Q" ))
                {
                    WindowSelectedMode = GUIMode.QuickWarp;
                    windowRect.height = 0;
                }
                UnityEngine.GUI.contentColor = cc;
            }
            //Rails Warp-To mode
            {
                if (WindowSelectedMode != GUIMode.RailsWarpTo)
                {
                    UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );
                }
                if (UnityEngine.GUI.Button( mode2Button, "W" ))
                {
                    WindowSelectedMode = GUIMode.RailsWarpTo;
                    windowRect.height = 0;
                }
                UnityEngine.GUI.contentColor = cc;
            }
            
            //Rails Editor mode
            {
                if (WindowSelectedMode != GUIMode.RailsEditor)
                {
                    UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );
                }
                if (UnityEngine.GUI.Button( mode3Button, "R" ))
                {
                    WindowSelectedMode = GUIMode.RailsEditor;
                    windowRect.height = 0;
                }
                UnityEngine.GUI.contentColor = cc;
            }

            //Key Bindings Editor mode
            {
                if (WindowSelectedMode != GUIMode.KeyBindingsEditor)
                {
                    UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );
                }
                if (UnityEngine.GUI.Button( mode4Button, "K" ))
                {
                    WindowSelectedMode = GUIMode.KeyBindingsEditor;
                    windowRect.height = 0;
                }
                UnityEngine.GUI.contentColor = cc;
            }

            // Only allow hyper warp and slow motion when in flight
            if (HighLogic.LoadedSceneIsFlight)
            {
                //Slow-mo mode
                {
                    if (WindowSelectedMode != GUIMode.SlowMotion)
                    {
                        UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );
                    }
                    if (UnityEngine.GUI.Button( mode5Button, "S" ))
                    {
                        WindowSelectedMode = GUIMode.SlowMotion;
                        windowRect.height = 0;
                    }
                    UnityEngine.GUI.contentColor = cc;
                }

                //Hyper mode
                {
                    if (WindowSelectedMode != GUIMode.HyperWarp)
                    {
                        UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );
                    }
                    if (UnityEngine.GUI.Button( mode6Button, "H" ))
                    {
                        WindowSelectedMode = GUIMode.HyperWarp;
                        windowRect.height = 0;
                    }
                    UnityEngine.GUI.contentColor = cc;
                }
            }            
            UnityEngine.GUI.backgroundColor = bc;
            
            GUI.enabled = true;
        }

        private void GUIReturnToRealtimeButton()
        {
            bool returnButton = false;
            if (FlightDriver.Pause)
            {
                GUILayout.Label( "PAUSED-KSP" );
            }
            else if (TimeController.Instance.TimePaused)
            {
                GUILayout.Label( "PAUSED-TC" );
            }
            else if (HyperWarpController.Instance.IsHyperWarping)
            {
                returnButton = GUILayout.Button( "HYPER" );
            }
            else if (RailsWarpController.Instance.IsRailsWarpingNoPhys)
            {
                returnButton = GUILayout.Button( "RAILS" );
            }
            else if (RailsWarpController.Instance.IsRailsWarpingPhys)
            {
                returnButton = GUILayout.Button( "PHYS" );
            }
            else if (SlowMoController.Instance.IsSlowMo)
            {
                returnButton = GUILayout.Button( "SLOWMO" );
            }
            else
            {
                GUILayout.Label( "NORMAL" );
            }
            if (returnButton)
            {
                TimeController.Instance.GoRealTime();
            }
        }

        private void GUIPauseOrResumeButton()
        {
            if (GUILayout.Button( (TimeController.Instance.TimePaused ? "Resume" : "Pause"), GUILayout.Width( 60 ) ))
            {
                TimeController.Instance?.TogglePause();
            }
        }

        private void GUITimeStepButton()
        {
            if (GUILayout.Button( ">", GUILayout.Width( 20 ) ))
            {
                TimeController.Instance?.IncrementTimeStep();
            }
        }


        #endregion

        #endregion

        #endregion
    }
}