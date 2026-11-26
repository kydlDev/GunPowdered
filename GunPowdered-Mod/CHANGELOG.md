# Changelog

## 1.0.1

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
