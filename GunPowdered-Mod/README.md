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
- **Deep North Dynamite Pack**: four sticks of dynamite and two grenades strapped together; unlocked after Fader is dead. Set it down, light it with a Torch (4 s fuse, countdown shown to everyone within 30 m) and run: the 20 m blast breaks even the frozen gammel trolls of the Deep North and leaves a 5 m crater.
- **Mobile Cannon** (Ashlands): a field gun on wheels.
  - **Move it by hand**: E on the push handle. It is slow (about 3.5 m/s) and uses stamina.
  - **Aim**: look at the crank and turn the mouse wheel to set the elevation (-5 to 25 degrees). Turn the whole carriage to aim left or right.
  - **Load and fire**: E at the muzzle loads 3 Gunpowder and 1 Cannon Ball. Then E on the touch hole with a Torch in hand lights the fuse. A loaded cannon stays loaded after a relog.
- **Asksvin Harness**: put it on a tame, saddled Asksvin, the same way as the saddle. Then E on the cannon's tow ring hitches the cannon behind the Asksvin.
  - The Asksvin tows the cannon at its full speed, ridden or following you.
  - E on the tow ring again unhitches it.
  - The harness drops when the saddle comes off or the Asksvin dies.

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

Edits to the file are picked up while the game runs.

Damage, recipes, tiers and more: see the FAQ.

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
