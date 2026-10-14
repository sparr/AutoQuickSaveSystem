using System;
using System.Collections;
using UnityEngine;

using static AutoQuickSaveSystem.AutoQuickSaveSystem;

namespace AutoQuickSaveSystem
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class Quicksave : MonoBehaviour
    {
        internal static Quicksave instance;

        static public string dateFormat = "yyyy-MM-dd--HH-mm-ss";
        static internal float lastBackup = 0;
        // Set when a timed quicksave was due but deferred until the scene has finished loading.
        static bool retrySoon;
        // Seconds a launch quicksave waits for the scene to finish loading.
        const float MaxLoadWait = 30f;
        public const string LAUNCH_QS_PREFIX = "LaunchQSave_";
        public const string AUTO_QS_PREFIX = "AutoQSave_";
        public const string SCENE_QS_PREFIX = "SceneQSave_";

        protected void Start()
        {
            instance = this;
            DontDestroyOnLoad(this);
            Configuration.Init();
            Configuration.Load();
            RestartLoop();
        }

        internal void RestartLoop()
        {
            StopCoroutine("QuickSaveLoop");
            StartCoroutine("QuickSaveLoop");
        }

#if true
        static DateTime setTime = DateTime.Now;
        IEnumerator QuickSaveLoop()
        {
            Log.Info("Starting QuickSaveLoop");
            float sleepTime = 1f;
            while (true)
            {
                TimeSpan elapsed = DateTime.Now - setTime;

                if (!HighLogic.LoadedSceneIsEditor)
                {
                    switch (Configuration.QuicksaveInterval)
                    {
                        case Configuration.QuickSave_Interval.ONCE_IN_10_MINUTES:
                            sleepTime = 10;
                            if (elapsed.TotalMinutes >= 10)
                            {
                                QuicksaveGame();
                            }
                            break;
                        case Configuration.QuickSave_Interval.ONCE_IN_30_MINUTES:
                            sleepTime = 30;
                            if (elapsed.TotalMinutes >= 30)
                            {
                                QuicksaveGame();
                            }
                            break;
                        case Configuration.QuickSave_Interval.ONCE_PER_HOUR:
                            sleepTime = 60;
                            if (elapsed.TotalHours >= 1)
                            {
                                QuicksaveGame();
                            }
                            break;
                        case Configuration.QuickSave_Interval.ONCE_IN_2_HOURS:
                            sleepTime = 120;
                            if (elapsed.TotalHours >= 2)
                            {
                                QuicksaveGame();
                            }
                            break;
                        case Configuration.QuickSave_Interval.ONCE_IN_4_HOURS:
                            sleepTime = 240;
                            if (elapsed.TotalHours >= 4)
                            {
                                QuicksaveGame();
                            }
                            break;
                        case Configuration.QuickSave_Interval.CUSTOM:
                            sleepTime = Configuration.CustomQuicksaveInterval;
                            if (elapsed.Minutes >= Configuration.CustomQuicksaveInterval)
                            {
                                QuicksaveGame();
                            }
                            break;
                        default:
                            Log.Error("invalid QuickSave_Interval ignored");

                            break;
                    }
                    if (retrySoon)
                    {
                        retrySoon = false;
                        yield return new WaitForSecondsRealtime(1f);
                    }
                    else
                        yield return new WaitForSecondsRealtime((float)(sleepTime * 60f - elapsed.TotalSeconds));
                }
                else
                    yield return new WaitForSecondsRealtime(60f);
            }
        }
#endif
        internal static void QuicksaveGame()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                setTime.AddSeconds(60);
                return;
            }
            //Log.Info("QuicksaveGame, stacktrace: " + Environment.StackTrace);
            if (!DoQuicksave(AUTO_QS_PREFIX + Configuration.QuickSaveNameTemplate, "AutoQuickSave to"))
            {
                retrySoon = true;
                return;
            }
            setTime = DateTime.Now;
        }

        // Saves once the scene's scenario modules have finished loading, or gives up if the scene
        // changes or they take longer than MaxLoadWait.
        internal void QuicksaveWhenLoaded(string template, string message)
        {
            StartCoroutine(QuicksaveWhenLoadedRoutine(template, message));
        }

        IEnumerator QuicksaveWhenLoadedRoutine(string template, string message)
        {
            GameScenes scene = HighLogic.LoadedScene;
            float start = Time.realtimeSinceStartup;
            while (ScenarioLoadState.Pending() != null)
            {
                if (HighLogic.LoadedScene != scene || Time.realtimeSinceStartup - start > MaxLoadWait)
                {
                    Log.Info("Quicksave to " + template + " skipped, the scene did not finish loading");
                    yield break;
                }
                yield return null;
            }
            DoQuicksave(template, message);
        }

        // Returns false without saving if a scenario module has not finished loading its state from
        // the save, because the save would lose that state.
        internal static bool DoQuicksave(string template, string message)
        {
            string pending = ScenarioLoadState.Pending();
            if (pending != null)
            {
                Log.Info("Quicksave to " + template + " deferred, " + pending + " has not finished loading");
                return false;
            }

            if (HighLogic.LoadedScene == GameScenes.SPACECENTER ||
                HighLogic.LoadedScene == GameScenes.EDITOR ||
                HighLogic.LoadedScene == GameScenes.FLIGHT ||
                HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                string newName = StringTranslation.AddFormatInfo(template, "", dateFormat);
                if (newName != null && newName.Length > 0)
                {
                    // First we need to acquire the current game status
                    Game currentGame = HighLogic.CurrentGame.Updated();

                    // If we are not at the space center, we have to reset the startScene to flight,
                    // because calling Updated() sets it to space center.
                    if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                        currentGame.startScene = GameScenes.FLIGHT;

                    string filename = GamePersistence.SaveGame(currentGame, newName, HighLogic.SaveFolder, SaveMode.OVERWRITE);

                    lastBackup = Time.realtimeSinceStartup;

                    ScreenMessages.PostScreenMessage(message + ": " + newName);

                }

                QuicksaveCleanup.Cleanup();
            }
            return true;
        }
    }

}