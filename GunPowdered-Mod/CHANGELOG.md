# Changelog

## 1.0.2

- Changed: GunPowdered no longer needs Jotunn. Items, pieces, deposits, skills, the musket's key hints and the version check on servers now come from KydlCore 1.2.0 (which this version requires). Saved skill levels, items and placed pieces carry over unchanged.
- Changed: the buildable pieces (Leaching Station, Powder Mill, extensions, Rain Barrel, cannon pieces) now appear in the Hammer's KY-Items category together with the other KydlDev pieces.
- Fixed: the musket's bullet is made even when the game's iron bolt cannot be found at game start (a start error reported "the musket cannot fire"); the bolt is looked up again when the musket is first fired.

- New: the musket works with a gamepad. Aim uses the game's secondary attack button and Reload the game's Use button (only while you are not pointing at something to interact with); both are settings (`[Musket] Aim Gamepad Button` / `Reload Gamepad Button`, a game button name). Fire and Block are as before. The musket's key hints show the gamepad glyphs.
- Controller support is offered, but it has not been tested with a real controller yet (only with simulated button presses in the test lab).

### Testing
- Built and unit / integration tested against the 1.0.31 game files.
- Not tested in game yet.

## 1.0.1

- Fixed: once a cannon (Hand Cannon or Mobile Cannon) is filled, your character stops the fill animation at once (also when another player filled it while you were loading), and no fill animation starts in the last moment before it is loaded.
- Fixed: the Mobile Cannon's muzzle no longer offers "Load" while the cannon is loaded, being loaded or its fuse burns; it offers it again once the cannon is empty.
- Fixed: builds against Valheim 1.0.31 again (a test hook used `SoftReference.Name`, which the game renamed to `name`); no change for players.

### Testing
- Built and unit / integration tested against the 1.0.31 game files.
- Not tested in game yet.

## 1.0.0 (2026-11-24)
First release.
- Gunpowder chain: gather Bat Guano, Water and Sulfur, then make Saltpeter in the Leaching Station and Gunpowder in the Powder Mill (both automatic, each with extension pieces).
- Weapons: Spiked Grenade, Cannon, Iron Musket, Explosive Arrow, Dynamite, Deep North Dynamite Pack and a Mobile Cannon you can tow with an Asksvin Harness.
- Three skills: Explosives, Muskets and Cannons.
- Weapons API for other mods, one Enable switch per weapon type, all items in KY-Items.
