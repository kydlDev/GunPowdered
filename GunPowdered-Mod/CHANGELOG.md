# Changelog

## 0.1.0 (in development)
- Gunpowder chain: Bat Guano, Water + Bucket, Rain Barrel, Leaching Station, Saltpeter, Sulfur, Powder Mill, Gunpowder.
- Leaching Station upgrades: five extension pieces (Second Leaching Barrel, Settling Tank, Black Metal-banded Boiler, Dvergr Drip Pipes, Flametal Heater); each makes the batch bigger and the run 5 minutes shorter. Use fills half a batch at any time (up to what the station holds) and takes all the Saltpeter; the station runs by itself while at least 10 + 10 + 4 is inside and stops below that; finished Saltpeter piles up; the hover text says what is missing.
- Powder Mill is automatic: fill it with Saltpeter, Charcoal and Sulfur (3 + 1 + 1 per batch of 5 Gunpowder) and it grinds by itself in 10 minutes; Gunpowder piles up until you take it. Gunpowder is no longer crafted.
- Powder Mill upgrades: Obsidian Grindstone (Mountains), Dvergr Powder Sieve (Mistlands) and Flametal Drying Oven (Ashlands); each makes the batch bigger and the run 2.5 minutes shorter.
- The Flametal Heater and the Flametal Drying Oven show a bright fire behind their panes while their station runs.
- Station extensions count within 5 m of their station in every direction (above and below too). They snap side by side to their own station, never on top of it (the build ghost turns red there), and stations and extensions cannot overlap.
- Deep North Dynamite Pack (after Fader): 20 m blast that breaks the frozen gammel trolls.
- Weapons: Spiked Grenade, Cannon (stock + ball), Iron Musket (5 cartridges, R to reload, recoil), Explosive Arrow, Dynamite.
- Mobile Cannon: move it by hand (slow, uses stamina), aim it with the elevation crank (mouse wheel), load it at the muzzle and fire it with a Torch; the load stays after a relog.
- Ammunition: a shot deals the weapon's damage plus its ammunition's. Iron Cartridge (was Cartridge) and Iron Cannon Ball (was Cannon Ball) keep your existing items. New Flametal Cannon Ball (4 Flametal at the Black Forge): the Mobile Cannon fires Flametal balls or better, the Hand Cannon Iron balls or better.
- Asksvin Harness: put it on a saddled tame Asksvin and hitch the Mobile Cannon. The Asksvin tows it at full speed, ridden or following, and the hitch stays after a relog.
- Weapons API (musket load, stats, ShotFired event) and one Enable switch per weapon type.
- All items listed in KY-Items, with a grid view.