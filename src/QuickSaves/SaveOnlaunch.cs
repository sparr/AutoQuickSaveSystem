using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using static AutoQuickSaveSystem.AutoQuickSaveSystem;

namespace AutoQuickSaveSystem
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class SaveOnlaunch : MonoBehaviour
    {
        static Guid lastVesselLaunched;

        void Awake() { GameEvents.onFlightReady.Add(onFlightReady); }

        protected void OnDestroy() { GameEvents.onFlightReady.Remove(onFlightReady); }

        void onFlightReady()
        {
            if (Configuration.QuicksaveOnLaunch)
            {
                if (lastVesselLaunched != FlightGlobals.ActiveVessel.id)
                {
                    lastVesselLaunched = FlightGlobals.ActiveVessel.id;
                    if (!Quicksave.DoQuicksave(Quicksave.LAUNCH_QS_PREFIX + Configuration.LaunchNameTemplate, "Launch Save to"))
                        Quicksave.instance.QuicksaveWhenLoaded(Quicksave.LAUNCH_QS_PREFIX + Configuration.LaunchNameTemplate, "Launch Save to");
                }
            }
        }
    }
}