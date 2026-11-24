# Changelog

## 1.0.0 (2026-11-24)
First release.

Making gunpowder
- Gunpowder chain: Bat Guano, Water + Bucket, Rain Barrel, Leaching Station, Saltpeter, Sulfur, Powder Mill, Gunpowder.
- Bat Guano deposits sit in Troll Caves, Burial Chambers, mountain caves, Swamp bat caves, Hildir's crypt and Hildir's cave. Emptied deposits refill after 2 in-game days.
- Sulfur comes from Sulfurous Mud (swamp geysers, swamp graves and mud next to bone piles with water within 15 m), from Blob and Oozer drops, and from Writhans in the Ashlands: one in five drops 3-5.
- Leaching Station: Use fills half a batch at any time (up to what the station holds) and takes all the Saltpeter. The station runs by itself while at least 10 + 10 + 4 is inside and stops below that. Finished Saltpeter piles up, and the hover text says what is missing.
- Leaching Station upgrades: five extension pieces (Second Leaching Barrel, Settling Tank, Black Metal-banded Boiler, Dvergr Drip Pipes, Flametal Heater); each makes the batch bigger and the run 5 minutes shorter.
- Powder Mill is automatic: fill it with Saltpeter, Charcoal and Sulfur (3 + 1 + 1 per batch of 5 Gunpowder) and it grinds by itself in 10 minutes. Gunpowder piles up until you take it; it is made only in the Powder Mill.
- Powder Mill upgrades: Obsidian Grindstone (Mountains), Dvergr Powder Sieve (Mistlands) and Flametal Drying Oven (Ashlands); each makes the batch bigger and the run 2.5 minutes shorter.
- The Flametal Heater and the Flametal Drying Oven show a bright fire behind their panes while their station runs.
- Station extensions count within 5 m of their station in every direction (above and below too). They snap side by side to their own station, never on top of it (the build ghost turns red there), and stations and extensions cannot overlap.

Weapons
- Spiked Grenade, Cannon (stock + ball), Iron Musket, Explosive Arrow, Dynamite, Deep North Dynamite Pack and Mobile Cannon.
- Ammunition: a shot deals the weapon's damage plus its ammunition's. Iron Cartridge, Iron Cannon Ball and Flametal Cannon Ball (4 Flametal at the Black Forge): the Mobile Cannon fires Flametal balls or better, the Hand Cannon Iron balls or better. Iron and Flametal Cannon Balls stack to 50.
- Spiked Grenade: hold the attack button to aim, release to throw. It lands and its fuse burns first, then it bursts. It digs a hole up to 1.5 m deep and chips rock and ore with light pickaxe damage.
- Iron Musket: 5 cartridges, R reloads (one cartridge at a time, also while running, not while jumping or swimming), recoil pushes you back. When the last cartridge is fired, the reload starts by itself if you carry a cartridge. Block 10 (15 at Blocking 100), parry bonus 1.5, no movement slowdown.
- Iron Musket handling: running lowers it to your right side, the mouse wheel click ([Musket] Aim Key) brings it back into both hands. A Torch or Shield can be held in the left hand next to the lowered musket; raising it puts them away. In both hands your character turns with the camera and strafes with the direction keys. A dodge roll keeps it in the right hand and an aimed musket stays aimed; a musket taken in hand always starts raised. Other players see every shot, including your very first.
- Dynamite: set it down (the character kneels and stands up afterwards), light it with a Torch. A placed, unlit stick sparkles light red so it is easy to find. Setting the last stick down does not start a punch.
- Deep North Dynamite Pack (after Fader): 20 m blast that breaks the frozen gammel trolls.
- Craters are measured from the ground under the charge, also when it lies on a rock: Dynamite up to 3 m deep, the Deep North Dynamite Pack 5 m. Blasts hit every piece of a big rock, also in a dense forest.
- Mobile Cannon: 2500 health; immune to fire, burning, lava and Ashlands ash (falling lava rocks still hurt it). E on the push handle moves it by hand (slow, uses stamina), E anywhere else on the cannon loads it at the muzzle, the elevation crank (mouse wheel) aims it, and a Torch fires it. The load stays after a relog.
- Asksvin Harness: put it on a saddled tame Asksvin and hitch the Mobile Cannon. The Asksvin tows it at full speed, ridden or following, and it steers around trees and rocks in its way. The hitch stays after a relog.

Skills
- Explosives: dynamite fuse 5 s at level 0 to 3 s at level 100, grenade fuse 3 s to 1 s.
- Muskets: reload 1.8 s per cartridge at level 0 to 1.2 s at level 100. The musket trains Muskets.
- Cannons: load 4 s at level 0 to 2.5 s at level 100, blunt damage up to x1.5.

Other
- The mod's Flametal items and pieces have a Flametal look with matching icons in the inventory, the build menu and KY-Items.
- Weapons API (musket load, stats, ShotFired event) and one Enable switch per weapon type.
- All items listed in KY-Items, with a grid view.
