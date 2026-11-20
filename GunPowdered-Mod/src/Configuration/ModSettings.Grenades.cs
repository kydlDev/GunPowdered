using KydlCore.Common;

namespace GunPowdered.Configuration
{
    /// <summary>
    /// The Spiked Grenade numbers the user approved (D-047) as dev-only Tunables (KydlTools F7). They are applied to the grenade, its flight and its
    /// blast while a grenade is in hand (GrenadeSetup.Apply, every few seconds), so a change shows in game without a restart; the recipe is read at start.
    /// </summary>
    internal static partial class ModSettings
    {
        public static readonly Tunable<float> GrenadeSpeed = new Tunable<float>("Grenades", "GrenadeSpeed", 12f, 4f, 30f,
            "Throw speed in m/s (4-30). 12 m/s with gravity 10 lands about 15 m away on flat ground at the best angle (D-047).");
        public static readonly Tunable<float> GrenadeGravity = new Tunable<float>("Grenades", "GrenadeGravity", 10f, 1f, 30f,
            "Gravity of the grenade in flight, m/s per second (1-30), like the vanilla bombs.");
        public static readonly Tunable<float> GrenadeFuseSeconds = new Tunable<float>("Grenades", "GrenadeFuseSeconds", 8f, 1f, 20f,
            "Seconds after which a grenade that hit nothing bursts in the air (1-20); the drawn arc is followed for the same time.");
        public static readonly Tunable<float> GrenadeRadius = new Tunable<float>("Grenades", "GrenadeRadius", 4f, 1f, 10f,
            "Blast radius in metres (1-10); also the size of the landing ring.");
        public static readonly Tunable<float> GrenadeBlunt = new Tunable<float>("Grenades", "GrenadeBlunt", 50f, 0f, 500f, "Blunt damage of the blast (0-500).");
        public static readonly Tunable<float> GrenadePierce = new Tunable<float>("Grenades", "GrenadePierce", 30f, 0f, 500f, "Pierce damage of the spikes (0-500).");
        public static readonly Tunable<float> GrenadeFire = new Tunable<float>("Grenades", "GrenadeFire", 5f, 0f, 100f, "Fire damage per second of the burning the blast sets, for 5 s (0-100, D-107).");
        public static readonly Tunable<float> GrenadeRecoil = new Tunable<float>("Grenades", "GrenadeRecoil", 1f, 0f, 10f,
            "Push back on the thrower when the grenade leaves the hand (0-10, the game's own recoil pushback).");

        // aiming (D-050): hold the left button, move the mouse to move the landing spot, let go to throw; defaults are a first guess to settle in game
        public static readonly Tunable<float> GrenadeAimMetersPerUnit = new Tunable<float>("Grenades", "GrenadeAimMetersPerUnit", 0.5f, 0.05f, 5f,
            "Metres the landing spot moves farther (mouse up) or nearer (mouse down) per unit of mouse movement while aiming (0.05-5). One unit is one degree of camera turn at the game's mouse sensitivity.");
        public static readonly Tunable<float> GrenadeAimDegreesPerUnit = new Tunable<float>("Grenades", "GrenadeAimDegreesPerUnit", 1f, 0.1f, 5f,
            "Degrees the landing spot swings right (mouse right) or left (mouse left) per unit of mouse movement while aiming (0.1-5).");
        public static readonly Tunable<float> GrenadeAimMinDistance = new Tunable<float>("Grenades", "GrenadeAimMinDistance", 2f, 0.5f, 15f,
            "Nearest landing spot while aiming, in metres from the thrower (0.5-15).");
        public static readonly Tunable<float> GrenadeAimMaxDistance = new Tunable<float>("Grenades", "GrenadeAimMaxDistance", 15f, 5f, 30f,
            "Farthest landing spot while aiming, in metres from the thrower (5-30); the throw speed decides how far a grenade can really go (about 14 m at 12 m/s).");
        public static readonly Tunable<float> GrenadeAimMaxSide = new Tunable<float>("Grenades", "GrenadeAimMaxSide", 60f, 10f, 180f,
            "How far the landing spot may swing to either side of the way the thrower faces, in degrees (10-180).");
    }
}
