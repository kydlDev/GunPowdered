namespace GunPowdered.Features.Musket.Domain
{
    /// <summary>
    /// The musket poses of the arms and fingers (D-092), as Unity humanoid muscle values for muscles <see cref="First"/> .. <see cref="First"/>+<see cref="Count"/>-1
    /// (both shoulders, arms, forearms, hands and all fingers), and where the musket sits in the right hand. Authored 2026-11-19 by Claude-Agent-6 (left-hand grips found by a measured search, D-093, Claude-Agent-7) on the game's own
    /// player rig in the Valheim AssetRipper project (read-only, in an unsaved scene): the musket put in place, the hands placed on it, the arms bent to them, then
    /// read back as muscles (numbers: _agent/lab/musket-poses.json via _agent/lab/gen_musket_poses.py, pictures: Photos/GunPowdered/20261119-1133-*). Only these numbers
    /// ship - no game animation data. The legs, hips and spine stay the game's own.
    /// </summary>
    internal static class MusketPoses
    {
        /// <summary>The first muscle index written (Unity HumanTrait: "Left Shoulder Down-Up").</summary>
        internal const int First = 37;

        /// <summary>How many muscles each pose holds (37 .. 94: shoulders, arms, hands, fingers).</summary>
        internal const int Count = 58;

        /// <summary>The musket under the right-hand attach point while aiming / carrying (grip in the hand, index finger at the trigger): x, y, z (m), pitch, yaw, roll (deg).</summary>
        internal static readonly float[] GripHold = { -0.05062f, -0.04602f, 0.08002f, 14.08f, 36.69f, 105.16f };

        /// <summary>The musket under the right-hand attach point while reloading (the hand holds the barrel, the musket stands upright away from the body).</summary>
        internal static readonly float[] GripReload = { 0.00558f, 0.20128f, -0.58054f, 20.45f, 355.23f, 92.32f };

        /// <summary>
        /// D-155: where the right palm goes for the side carry, from the right shoulder (upper-arm bone) in the CHARACTER's directions: right, up, forward (m).
        /// The placement the side pose was authored with (musket-side-author.cs.txt v2 -Down 0.52 -Out 0.10 -Fwd 0.04); MusketRig reaches it with an IK.
        /// </summary>
        internal static readonly float[] SidePalm = { 0.10f, -0.52f, 0.04f };

        /// <summary>D-155: the musket under the right-hand attach point while it hangs at the side (palm under the stock, about its middle).</summary>
        internal static readonly float[] GripSide = { -0.07756f, 0.0574f, -0.17583f, 20.13f, 1.11f, 94.36f };

        /// <summary>How far the ramrod's top end stands above the muzzle at the bottom and the top of one stroke (m); the left fist holds that end.</summary>
        internal const float RodLow = 0.05f, RodHigh = 0.4f;

        /// <summary>Where the ramrod lies in the musket model: its pipe line (y, m) and its top end (z, m); the bore line is y 0, the muzzle z <see cref="MuzzleZ"/>.</summary>
        internal const float RodPipeY = -0.06f, RodTopZ = 0.61f, MuzzleZ = 0.8f;

        /// <summary>The hold pose.</summary>
        internal static readonly float[] Hold =
        {
            0.1044f, -1.5870f, 0.5001f, -0.0979f, 0.9031f, 0.5153f, 1.4172f, 0.4378f, 1.2174f, 0.5716f,
            1.0256f, -0.1002f, -0.9175f, 0.2460f, -0.0460f, 0.7149f, 0.4740f, 1.9127f, 0.6000f, -1.0000f,
            0.6000f, 0.6000f, -0.2000f, -0.6000f, -0.2000f, -0.2000f, -0.2000f, -0.6000f, -0.2000f, -0.2000f,
            -0.2000f, -0.6000f, -0.2000f, -0.2000f, -0.2000f, -0.6000f, -0.2000f, -0.2000f, 0.0000f, 0.0000f,
            0.3000f, 0.3000f, 0.1000f, 0.0000f, -0.8000f, -0.8000f, -0.4000f, 0.0000f, -0.4000f, -0.4000f,
            -0.4000f, 0.0000f, -0.4000f, -0.4000f, -0.4000f, 0.0000f, -0.4000f, -0.4000f,
        };

        /// <summary>The recoil pose.</summary>
        internal static readonly float[] Recoil =
        {
            0.1044f, -1.5870f, 0.5656f, -0.0398f, 0.9572f, 0.3153f, 1.5078f, 0.6485f, 1.0759f, 0.5716f,
            1.0256f, -0.1048f, -0.9493f, 0.2348f, -0.2375f, 0.7902f, 0.7099f, 1.9118f, 0.6000f, -1.0000f,
            0.6000f, 0.6000f, -0.2000f, -0.6000f, -0.2000f, -0.2000f, -0.2000f, -0.6000f, -0.2000f, -0.2000f,
            -0.2000f, -0.6000f, -0.2000f, -0.2000f, -0.2000f, -0.6000f, -0.2000f, -0.2000f, 0.0000f, 0.0000f,
            0.3000f, 0.3000f, 0.1000f, 0.0000f, -0.8000f, -0.8000f, -0.4000f, 0.0000f, -0.4000f, -0.4000f,
            -0.4000f, 0.0000f, -0.4000f, -0.4000f, -0.4000f, 0.0000f, -0.4000f, -0.4000f,
        };

        /// <summary>
        /// D-155 (user 2026-11-22): the side carry while running - trail arms: the arm hangs almost straight, the musket lies in the palm front to back and
        /// rests on the fingers curled under it at about its middle, thumb over the top, wrist straight (pictures Photos/GunPowdered/20261122-0317_musket19-side-*,
        /// script _agent/lab/musket-side-author.cs.txt). Only the RIGHT fingers count in game: MusketRig places the arm with an IK (MusketPoses.SidePalm)
        /// and the left arm is the game's own while the musket hangs at the side.
        /// </summary>
        internal static readonly float[] Side =
        {
            0.1044f, -1.5870f, -0.0228f, -0.0401f, -0.0155f, 0.4255f, 1.3548f, -0.5014f, 0.0336f, 0.5716f,
            1.0256f, -0.2970f, -0.3409f, -0.4675f, 0.1336f, 0.6961f, 0.1340f, 0.5919f, 1.5790f, -1.6588f,
            0.1112f, 0.1172f, 0.1745f, -0.4495f, 0.1031f, -0.3138f, 0.1797f, -0.5199f, -0.0573f, -0.3058f,
            0.1829f, -0.7100f, -0.0297f, -0.2992f, 0.2324f, -0.2963f, 0.1135f, -0.1405f, 0.2000f, 0.6000f,
            -0.5000f, -0.5000f, -0.8500f, 0.0000f, -0.8500f, -0.8500f, -0.8500f, 0.0000f, -0.8500f, -0.8500f,
            -0.8500f, 0.0000f, -0.8500f, -0.8500f, -0.8500f, 0.0000f, -0.8500f, -0.8500f,
        };

        /// <summary>The reload-low pose.</summary>
        internal static readonly float[] ReloadLow =
        {
            0.1044f, -1.5870f, 0.4798f, 0.2227f, 0.7207f, -0.8422f, 1.4033f, 0.8600f, 0.4122f, 0.5716f,
            1.0256f, -0.5732f, -0.8832f, -0.2392f, 0.2523f, 0.5590f, 0.1295f, -0.0609f, -0.8000f, -0.8000f,
            -1.0000f, -1.0000f, -1.0000f, -0.6000f, -1.0000f, -1.0000f, -1.0000f, -0.6000f, -1.0000f, -1.0000f,
            -1.0000f, -0.6000f, -1.0000f, -1.0000f, -1.0000f, -0.6000f, -1.0000f, -1.0000f, -0.4000f, -0.8000f,
            -0.7000f, -0.7000f, -0.4000f, 0.0000f, -0.4000f, -0.4000f, -0.4000f, 0.0000f, -0.4000f, -0.4000f,
            -0.4000f, 0.0000f, -0.4000f, -0.4000f, -0.4000f, 0.0000f, -0.4000f, -0.4000f,
        };

        /// <summary>The reload-high pose.</summary>
        internal static readonly float[] ReloadHigh =
        {
            0.1044f, -1.5870f, 1.0782f, 0.8959f, 0.7957f, 0.1046f, 1.8542f, 0.3432f, 0.9691f, 0.5716f,
            1.0256f, -0.5732f, -0.8832f, -0.2392f, 0.2523f, 0.5590f, 0.1295f, -0.0609f, -0.8000f, -0.8000f,
            -1.0000f, -1.0000f, -1.0000f, -0.6000f, -1.0000f, -1.0000f, -1.0000f, -0.6000f, -1.0000f, -1.0000f,
            -1.0000f, -0.6000f, -1.0000f, -1.0000f, -1.0000f, -0.6000f, -1.0000f, -1.0000f, -0.4000f, -0.8000f,
            -0.7000f, -0.7000f, -0.4000f, 0.0000f, -0.4000f, -0.4000f, -0.4000f, 0.0000f, -0.4000f, -0.4000f,
            -0.4000f, 0.0000f, -0.4000f, -0.4000f, -0.4000f, 0.0000f, -0.4000f, -0.4000f,
        };
    }
}
