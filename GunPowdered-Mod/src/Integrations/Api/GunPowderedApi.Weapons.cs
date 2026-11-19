using System;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.Musket.Infrastructure;

namespace GunPowdered
{
    // The game-side part of the public API (D-097): members that take or give game objects. The pure part (capacity, stats) is in
    // src/Core/GunPowderedApi.cs.
    public static partial class GunPowderedApi
    {
        public static partial class Musket
        {
            /// <summary>True when the item is an Iron Musket.</summary>
            public static bool IsMusket(ItemDrop.ItemData item) => MusketGun.IsMusket(item);

            /// <summary>How many cartridges this musket holds (0 .. <see cref="Capacity"/>), or -1 when the item is not a musket.</summary>
            public static int GetLoaded(ItemDrop.ItemData musket) => MusketGun.IsMusket(musket) ? MusketGun.Loaded(musket) : -1;

            /// <summary>
            /// Sets how many cartridges this musket holds; values outside 0 .. <see cref="Capacity"/> are brought inside. No cartridge is taken from or
            /// given to anyone. The load is stored on the item (custom data), so it is saved with it. Returns the load stored, or -1 when the item is
            /// not a musket.
            /// </summary>
            public static int SetLoaded(ItemDrop.ItemData musket, int loaded) => MusketGun.IsMusket(musket) ? MusketGun.SetLoaded(musket, loaded) : -1;

            /// <summary>The musket the player holds in hand, or null.</summary>
            public static ItemDrop.ItemData HeldBy(Player player) => MusketGun.Held(player);

            static event Action<MusketShot> _shotFired;
            static bool _hooked;

            /// <summary>
            /// Raised after every musket shot, on the client of the player who fired (other clients see the bullet, not the event). A listener that
            /// throws is logged and does not stop the shot or other listeners.
            /// </summary>
            public static event Action<MusketShot> ShotFired
            {
                add { Hook(); _shotFired += value; }
                remove { _shotFired -= value; }
            }

            static void Hook()
            {
                if (_hooked) return;
                _hooked = true;
                MusketGun.Fired += Pass;
            }

            static void Pass(Player shooter, ItemDrop.ItemData musket, Vector3 muzzle, Vector3 direction, int left, GameObject bullet)
            {
                Action<MusketShot> listeners = _shotFired;
                if (listeners == null) return;
                var shot = new MusketShot(shooter, musket, muzzle, direction, left, bullet);
                foreach (Delegate d in listeners.GetInvocationList())
                {
                    try { ((Action<MusketShot>)d)(shot); }
                    catch (Exception e) { ModLog.Warn("GunPowderedApi.Musket.ShotFired: a listener failed (" + d.Method.DeclaringType + "." + d.Method.Name + "): " + e.GetType().Name + ": " + e.Message); }
                }
            }
        }

        /// <summary>One musket shot, as <see cref="Musket.ShotFired"/> reports it.</summary>
        public sealed class MusketShot
        {
            internal MusketShot(Player shooter, ItemDrop.ItemData musket, Vector3 muzzle, Vector3 direction, int cartridgesLeft, GameObject bullet)
            {
                Shooter = shooter;
                Weapon = musket;
                Muzzle = muzzle;
                Direction = direction;
                CartridgesLeft = cartridgesLeft;
                Bullet = bullet;
            }

            /// <summary>The player who fired.</summary>
            public Player Shooter { get; }

            /// <summary>The musket item that fired (its load is already one less).</summary>
            public ItemDrop.ItemData Weapon { get; }

            /// <summary>Where the bullet left the muzzle (world).</summary>
            public Vector3 Muzzle { get; }

            /// <summary>The direction the bullet flies (normalised, spread included).</summary>
            public Vector3 Direction { get; }

            /// <summary>Cartridges still loaded after this shot.</summary>
            public int CartridgesLeft { get; }

            /// <summary>The flying bullet (a projectile; it is gone after a hit or <see cref="Musket.BulletSeconds"/>).</summary>
            public GameObject Bullet { get; }
        }
    }
}
