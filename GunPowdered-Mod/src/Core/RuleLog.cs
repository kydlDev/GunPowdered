using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GunPowdered.Core
{
    /// <summary>
    /// Debug logging for the pure code (Core and the features' Domain): PROJECT_RULES 8 "every method logs". The pure code is also compiled into the unit
    /// tests, which run without the game, BepInEx or KydlCore, so it cannot call ModLog itself. Plugin.Awake connects this to ModLog (Sink / DebugOn);
    /// in the unit tests nothing is connected and every call does nothing.
    /// </summary>
    public static class RuleLog
    {
        static readonly Dictionary<string, long> LastWritten = new Dictionary<string, long>();
        static readonly Stopwatch Clock = Stopwatch.StartNew();

        /// <summary>Where the lines go (ModLog.Verbose in the game; null in the unit tests).</summary>
        public static Action<string> Sink { get; set; }

        /// <summary>Whether debug logging is on ([DebugLog] Enable in the game; null = off).</summary>
        public static Func<bool> DebugOn { get; set; }

        /// <summary>True when a line would be written: callers check it before building the text, so a game with debug logging off pays nothing.</summary>
        public static bool On => Sink != null && DebugOn != null && DebugOn();

        /// <summary>Writes the line when debug logging is on.</summary>
        public static void Verbose(string message)
        {
            if (On) Sink(message);
        }

        /// <summary>Writes the line at most once per <paramref name="seconds"/> for this key (code that runs every frame). Expected outcome: true when written.</summary>
        public static bool Every(string key, double seconds, string message)
        {
            if (!On) return false;
            long now = Clock.ElapsedMilliseconds;
            lock (LastWritten)
            {
                if (LastWritten.TryGetValue(key, out long last) && now - last < seconds * 1000d) return false;
                LastWritten[key] = now;
            }
            Sink(message);
            return true;
        }
    }
}
