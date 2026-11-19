namespace GunPowdered.Features.Musket.Domain
{
    /// <summary>
    /// The musket poses of the arms and fingers (D-092), as Unity humanoid muscle values for muscles <see cref="First"/> .. <see cref="First"/>+<see cref="Count"/>-1
    /// (both shoulders, arms, forearms, hands and all fingers), and where the musket sits in the right hand. Authored 2026-11-19 by Claude-Agent-6 on the game's own
    /// player rig in the Valheim AssetRipper project (read-only, in an unsaved scene): the musket put in place, the hands placed on it, the arms bent to them, then
    /// read back as muscles (numbers: _agent/lab/musket-poses.json via _agent/lab/gen_musket_poses.py, pictures: Photos/GunPowdered/20261119-0301_musket13-*). Only these numbers
    /// ship - no game animation data. The legs, hips and spine stay the game's own.
    /// </summary>
    internal static class MusketPoses
    {
        /// <summary>The first muscle index written (Unity HumanTrait: "Left Shoulder Down-Up").</summary>
        internal const int First = 37;

        /// <summary>How many muscles each pose holds (37 .. 94: shoulders, arms, hands, fingers).</summary>
        internal const int Count = 58;

        /// <summary>The musket under the right-hand attach point while aiming / carrying (grip in the hand, index finger at the trigger): x, y, z (m), pitch, yaw, roll (deg).</summary>
        internal static readonly float[] GripHold = { -0.11013f, -0.10431f, 0.22638f, 20.12f, 1.3f, 94.43f };

        /// <summary>The musket under the right-hand attach point while reloading (the hand holds the barrel, the musket stands upright away from the body).</summary>
        internal static readonly float[] GripReload = { -0.02218f, 0.20987f, -0.62473f, 20.45f, 355.23f, 92.32f };

        /// <summary>How far the ramrod's top end stands above the muzzle at the bottom and the top of one stroke (m); the left fist holds that end.</summary>
        internal const float RodLow = 0.05f, RodHigh = 0.4f;

        /// <summary>Where the ramrod lies in the musket model: its pipe line (y, m) and its top end (z, m); the bore line is y 0, the muzzle z <see cref="MuzzleZ"/>.</summary>
        internal const float RodPipeY = -0.06f, RodTopZ = 0.61f, MuzzleZ = 0.8f;

        /// <summary>The hold pose.</summary>
        internal static readonly float[] Hold =
        {
            0.1044f, -1.5870f, 0.4601f, -0.1339f, 0.8594f, 0.6283f, 1.5857f, -0.1409f, 1.3900f, 0.5716f,
            1.0256f, -0.2326f, -0.7288f, 0.2437f, -0.4720f, 1.0720f, 1.3231f, 2.8300f, -0.1000f, 0.0000f,
            -0.1000f, -0.1000f, -0.5500f, 0.0000f, -0.5500f, -0.5500f, -0.5500f, 0.0000f, -0.5500f, -0.5500f,
            -0.5500f, 0.0000f, -0.5500f, -0.5500f, -0.5500f, 0.0000f, -0.5500f, -0.5500f, -0.4500f, 0.0000f,
            -0.4500f, -0.4500f, 0.6000f, 0.0000f, 0.0000f, 0.0000f, -0.8000f, 0.0000f, -0.8000f, -0.8000f,
            -0.8000f, 0.0000f, -0.8000f, -0.8000f, -0.8000f, 0.0000f, -0.8000f, -0.8000f,
        };

        /// <summary>The recoil pose.</summary>
        internal static readonly float[] Recoil =
        {
            0.1044f, -1.5870f, 0.4872f, -0.0783f, 0.9250f, 0.3360f, 1.6377f, 0.0940f, 1.1212f, 0.5716f,
            1.0256f, -0.2337f, -0.7797f, 0.2102f, -0.6395f, 1.2965f, 1.5071f, 2.4241f, -0.1000f, 0.0000f,
            -0.1000f, -0.1000f, -0.5500f, 0.0000f, -0.5500f, -0.5500f, -0.5500f, 0.0000f, -0.5500f, -0.5500f,
            -0.5500f, 0.0000f, -0.5500f, -0.5500f, -0.5500f, 0.0000f, -0.5500f, -0.5500f, -0.4500f, 0.0000f,
            -0.4500f, -0.4500f, 0.6000f, 0.0000f, 0.0000f, 0.0000f, -0.8000f, 0.0000f, -0.8000f, -0.8000f,
            -0.8000f, 0.0000f, -0.8000f, -0.8000f, -0.8000f, 0.0000f, -0.8000f, -0.8000f,
        };

        /// <summary>The carry pose.</summary>
        internal static readonly float[] Carry =
        {
            0.1044f, -1.5870f, 0.2972f, 0.2068f, 0.1880f, 0.9808f, -1.6848f, -0.7929f, 2.4164f, 0.5716f,
            1.0256f, -1.1169f, -1.0305f, -0.3164f, 0.9171f, 0.3928f, -0.1484f, 1.0227f, -0.1000f, 0.0000f,
            -0.1000f, -0.1000f, -0.5500f, 0.0000f, -0.5500f, -0.5500f, -0.5500f, 0.0000f, -0.5500f, -0.5500f,
            -0.5500f, 0.0000f, -0.5500f, -0.5500f, -0.5500f, 0.0000f, -0.5500f, -0.5500f, -0.4500f, 0.0000f,
            -0.4500f, -0.4500f, 0.6000f, 0.0000f, 0.0000f, 0.0000f, -0.8000f, 0.0000f, -0.8000f, -0.8000f,
            -0.8000f, 0.0000f, -0.8000f, -0.8000f, -0.8000f, 0.0000f, -0.8000f, -0.8000f,
        };

        /// <summary>The reload-low pose.</summary>
        internal static readonly float[] ReloadLow =
        {
            0.1044f, -1.5870f, 0.5747f, -0.1265f, 0.5412f, -0.7803f, 0.4338f, 1.7811f, 3.3855f, 0.5716f,
            1.0256f, -0.5552f, -0.9504f, -0.1774f, 0.3028f, 0.5618f, 0.1129f, 0.0474f, -0.7000f, 0.0000f,
            -0.7000f, -0.7000f, -0.9000f, 0.0000f, -0.9000f, -0.9000f, -0.9000f, 0.0000f, -0.9000f, -0.9000f,
            -0.9000f, 0.0000f, -0.9000f, -0.9000f, -0.9000f, 0.0000f, -0.9000f, -0.9000f, -0.5000f, 0.0000f,
            -0.5000f, -0.5000f, -0.7500f, 0.0000f, -0.7500f, -0.7500f, -0.7500f, 0.0000f, -0.7500f, -0.7500f,
            -0.7500f, 0.0000f, -0.7500f, -0.7500f, -0.7500f, 0.0000f, -0.7500f, -0.7500f,
        };

        /// <summary>The reload-high pose.</summary>
        internal static readonly float[] ReloadHigh =
        {
            0.1044f, -1.5870f, 1.2367f, 0.9030f, 0.6359f, 0.1540f, 1.6313f, 1.0834f, 1.0267f, 0.5716f,
            1.0256f, -0.5552f, -0.9504f, -0.1774f, 0.3028f, 0.5618f, 0.1129f, 0.0474f, -0.7000f, 0.0000f,
            -0.7000f, -0.7000f, -0.9000f, 0.0000f, -0.9000f, -0.9000f, -0.9000f, 0.0000f, -0.9000f, -0.9000f,
            -0.9000f, 0.0000f, -0.9000f, -0.9000f, -0.9000f, 0.0000f, -0.9000f, -0.9000f, -0.5000f, 0.0000f,
            -0.5000f, -0.5000f, -0.7500f, 0.0000f, -0.7500f, -0.7500f, -0.7500f, 0.0000f, -0.7500f, -0.7500f,
            -0.7500f, 0.0000f, -0.7500f, -0.7500f, -0.7500f, 0.0000f, -0.7500f, -0.7500f,
        };
    }
}
