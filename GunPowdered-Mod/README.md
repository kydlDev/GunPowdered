# GunPowdered

Gunpowder for Valheim: gather guano, water and sulfur, mill gunpowder, then build grenades, cannons, a musket, explosive arrows and dynamite.

## Features
Make gunpowder, then build weapons with it.

**Making gunpowder**
- **Bat Guano**: rare deposits in Troll Caves, Burial Chambers, mountain caves and Swamp bat caves; mine with any pickaxe.
- **Water**: fill a Bucket in any water, or collect it in a **Rain Barrel** (fills only while it rains and nothing roofs it).
- **Leaching Station**: guano + water make Saltpeter after one in-game day.
- **Sulfur**: Sulfurous Mud in the swamp, plus Blob and Oozer drops.
- **Powder Mill**: saltpeter + sulfur + charcoal make Gunpowder.

**Weapons**
- **Spiked Grenade**: hold the attack button to aim the landing spot (arc and ring shown), release to throw, block to cancel. Damages creatures, buildings, trees and rocks.
- **Cannon** (with **Cannon Stock** and **Cannon Ball**): build the stock, mount the cannon, load the ball at the muzzle, light the fuse with a Torch and stand clear.
- **Iron Musket**: holds 5 cartridges, fires at least 0.8 s apart, **R** reloads one cartridge per 1.2 s (key can be changed). Recoil pushes you back. Counter next to the crosshair; key hints in the bottom right.
- **Explosive Arrow**: ammo for any bow; bursts on impact (3 m) and damages buildings too.
- **Dynamite**: set it down, light it with a Torch (4 s fuse), 4 m blast for buildings, rocks, trees and ore; creatures take a fifth of that; leaves a round crater.
- **Mobile Cannon** and **Asksvin Harness**: items and recipes only so far, no behaviour yet.

All pieces and items are listed in the KY-Items viewer (grid or list view).

## How to use
- Requires **KydlCore** (installed automatically by the mod manager).
- Build pieces of this mod are in the hammer's **KY-Items** category.

## Configuration
`BepInEx/config/com.kydldev.GunPowdered.cfg` (or the in-game config manager):

| Section | Setting | Default | What it does |
|---------|---------|---------|--------------|
| General | Enable Mod | true | Switches the whole mod on / off while playing |
| DebugLog | Enable | false | Detailed lines in the BepInEx log for bug reports |
| General | Enable Grenades / Cannons / Musket / Explosive Arrows / Dynamite | true | Switches one weapon type on / off (both cannons share one) |
| Musket | Reload Key | R | Key that reloads the musket |
| General | KY-Items View | Grid | KY-Items viewer: grid or list |
| Grenades, Cannon, Musket, Explosive Arrows, Dynamite | damage, radius, speed ... | see in game | Every number of the weapons is a setting |

Edits to the file are picked up while the game runs.

## For mod developers
`GunPowderedApi` exposes the prefab names and a small weapons part: musket load (read / set), capacity and stats, and a `ShotFired` event.
## Multiplayer
Every player needs the mod (same minor version).

## Bug reports
Turn on `[DebugLog] Enable`, reproduce the problem, and attach `BepInEx/LogOutput.log`.

---

![KydlDev](https://raw.githubusercontent.com/kydlDev/.github/main/branding/KydlDev-banner-960x240.png)

## Get help and stay in touch
Questions, bug reports, ideas or mod requests? Come say hi in the **KydlDev Discord**: **https://discord.gg/tX2CcTNC4W**
Everyone is welcome, and it is the quickest way to reach me. Source code and issues: https://github.com/kydlDev
