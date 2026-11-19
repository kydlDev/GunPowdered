namespace GunPowdered.Features.Musket.Domain
{
    /// <summary>
    /// The musket poses of the arms and fingers (D-092), as Unity humanoid muscle values for muscles <see cref="First"/> .. <see cref="First"/>+<see cref="Count"/>-1
    /// (both shoulders, arms, forearms, hands and all fingers), and where the musket sits in the right hand. Authored 2026-11-19 by Claude-Agent-6 (left-hand grips found by a measured search, D-093, Claude-Agent-7) on the game's own
    /// player rig in the Valheim AssetRipper project (read-only, in an unsaved scene): the musket put in place, the hands placed on it, the arms bent to them, then
    /// read back as muscles (numbers: _agent/lab/musket-poses.json via _agent/lab/gen_musket_poses.py, pictures: Photos/GunPowdered/20261119-0342-*). Only these numbers
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
            0.1044f, -1.5870f, 0.5001f, -0.0979f, 0.9031f, 0.5153f, 1.4172f, 0.4378f, 1.2174f, 0.5716f,
            1.0256f, -0.2317f, -0.7287f, 0.2436f, -0.4720f, 1.0727f, 1.3230f, 2.8283f, 0.6000f, -1.0000f,
            0.6000f, 0.6000f, -0.2000f, -0.6000f, -0.2000f, -0.2000f, -0.2000f, -0.6000f, -0.2000f, -0.2000f,
            -0.2000f, -0.6000f, -0.2000f, -0.2000f, -0.2000f, -0.6000f, -0.2000f, -0.2000f, -0.4500f, 0.0000f,
            -0.4500f, -0.4500f, 0.6000f, 0.0000f, 0.0000f, 0.0000f, -0.8000f, 0.0000f, -0.8000f, -0.8000f,
            -0.8000f, 0.0000f, -0.8000f, -0.8000f, -0.8000f, 0.0000f, -0.8000f, -0.8000f,
        };

        /// <summary>The recoil pose.</summary>
        internal static readonly float[] Recoil =
        {
            0.1044f, -1.5870f, 0.5656f, -0.0398f, 0.9572f, 0.3153f, 1.5078f, 0.6485f, 1.0759f, 0.5716f,
            1.0256f, -0.2327f, -0.7796f, 0.2102f, -0.6394f, 1.2972f, 1.5067f, 2.4217f, 0.6000f, -1.0000f,
            0.6000f, 0.6000f, -0.2000f, -0.6000f, -0.2000f, -0.2000f, -0.2000f, -0.6000f, -0.2000f, -0.2000f,
            -0.2000f, -0.6000f, -0.2000f, -0.2000f, -0.2000f, -0.6000f, -0.2000f, -0.2000f, -0.4500f, 0.0000f,
            -0.4500f, -0.4500f, 0.6000f, 0.0000f, 0.0000f, 0.0000f, -0.8000f, 0.0000f, -0.8000f, -0.8000f,
            -0.8000f, 0.0000f, -0.8000f, -0.8000f, -0.8000f, 0.0000f, -0.8000f, -0.8000f,
        };

        /// <summary>The carry pose.</summary>
        internal static readonly float[] Carry =
        {
            0.1044f, -1.5870f, 0.1924f, 0.1994f, 1.2025f, 0.5517f, 1.1582f, -0.0909f, 1.8588f, 0.5716f,
            1.0256f, -1.1249f, -1.0292f, -0.2700f, 0.9155f, 0.3415f, -0.1522f, 1.0268f, 0.6000f, -1.0000f,
            0.6000f, 0.6000f, -0.2000f, -0.6000f, -0.2000f, -0.2000f, -0.2000f, -0.6000f, -0.2000f, -0.2000f,
            -0.2000f, -0.6000f, -0.2000f, -0.2000f, -0.2000f, -0.6000f, -0.2000f, -0.2000f, -0.4500f, 0.0000f,
            -0.4500f, -0.4500f, 0.6000f, 0.0000f, 0.0000f, 0.0000f, -0.8000f, 0.0000f, -0.8000f, -0.8000f,
            -0.8000f, 0.0000f, -0.8000f, -0.8000f, -0.8000f, 0.0000f, -0.8000f, -0.8000f,
        };

        /// <summary>The reload-low pose.</summary>
        internal static readonly float[] ReloadLow =
        {
            0.1044f, -1.5870f, 0.4798f, 0.2227f, 0.7207f, -0.8422f, 1.4033f, 0.8600f, 0.4122f, 0.5716f,
            1.0256f, -0.5561f, -0.9503f, -0.1771f, 0.3028f, 0.5611f, 0.1126f, 0.0481f, -0.8000f, -0.8000f,
            -1.0000f, -1.0000f, -1.0000f, -0.6000f, -1.0000f, -1.0000f, -1.0000f, -0.6000f, -1.0000f, -1.0000f,
            -1.0000f, -0.6000f, -1.0000f, -1.0000f, -1.0000f, -0.6000f, -1.0000f, -1.0000f, -0.5000f, 0.0000f,
            -0.5000f, -0.5000f, -0.7500f, 0.0000f, -0.7500f, -0.7500f, -0.7500f, 0.0000f, -0.7500f, -0.7500f,
            -0.7500f, 0.0000f, -0.7500f, -0.7500f, -0.7500f, 0.0000f, -0.7500f, -0.7500f,
        };

        /// <summary>The reload-high pose.</summary>
        internal static readonly float[] ReloadHigh =
        {
            0.1044f, -1.5870f, 1.0782f, 0.8959f, 0.7957f, 0.1046f, 1.8542f, 0.3432f, 0.9691f, 0.5716f,
            1.0256f, -0.5561f, -0.9503f, -0.1771f, 0.3028f, 0.5611f, 0.1126f, 0.0481f, -0.8000f, -0.8000f,
            -1.0000f, -1.0000f, -1.0000f, -0.6000f, -1.0000f, -1.0000f, -1.0000f, -0.6000f, -1.0000f, -1.0000f,
            -1.0000f, -0.6000f, -1.0000f, -1.0000f, -1.0000f, -0.6000f, -1.0000f, -1.0000f, -0.5000f, 0.0000f,
            -0.5000f, -0.5000f, -0.7500f, 0.0000f, -0.7500f, -0.7500f, -0.7500f, 0.0000f, -0.7500f, -0.7500f,
            -0.7500f, 0.0000f, -0.7500f, -0.7500f, -0.7500f, 0.0000f, -0.7500f, -0.7500f,
        };
    }
}
