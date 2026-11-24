# Changelog

## 1.0.0 (2026-11-24)
First release.
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
- Iron Musket: running lowers it to your right side; the mouse wheel click (new [Musket] Aim Key) brings it back into both hands. Reloading works while running (not while jumping or swimming). A Torch or Shield can be held in the left hand next to the lowered musket; raising it puts them away. Block 10 (15 at Blocking 100), parry bonus 1.5, no movement slowdown.
- Other players now see your very first musket shot (it was missing before).
- Iron Musket: a dodge roll keeps it in the right hand and an aimed musket stays aimed; a musket taken in hand always starts raised; other players no longer see a female character's arms shake while she holds it.
- Emptied deposits refill after 2 in-game days (was 3).
- New skills: Explosives (dynamite fuse 5 s -> 3 s, landed grenade fuse 3 s -> 1 s), Muskets (reload 1.8 s -> 1.2 s per cartridge), Cannons (load 4 s -> 2.5 s, blunt damage up to x1.5). The musket now trains Muskets instead of Crossbows.
- Spiked Grenade: it no longer bursts on impact; it lands and its fuse burns first.
- Dynamite: the character stands up again after setting a stick down; a placed, unlit stick sparkles light red so it is easy to find.
- Hildir's crypt and Hildir's cave now get Bat Guano like a Burial Chamber and a mountain cave.
- Sulfurous Mud now also appears at geysers and swamp graves whose area the game created while you were still far away (they had none before); a world that had them before the mod gets their mud on the first visit.
- Mud next to a bone pile is placed wherever there is water within 15 m of it (before, half the piles by water had none); bone piles with no water around keep none.
- Writhans (Ashlands) drop Sulfur: one in five drops 3-5.
- Iron Musket: in both hands your character turns with the camera and strafes with the direction keys; the mouse wheel no longer frees the camera. When the last cartridge is fired, the reload starts by itself if you carry a cartridge.
- Iron and Flametal Cannon Balls stack to 50.
- Craters are measured from the ground under the charge, also when it lies on a rock: Dynamite up to 3 m deep, the Deep North Dynamite Pack 5 m. The Spiked Grenade now digs too (up to 1.5 m) and chips rock and ore with light pickaxe damage.
- Blasts hit every piece of a big rock, also in a dense forest.
- Dynamite: setting the last stick down no longer makes your character punch right after.
- Mobile Cannon: 2500 health; immune to fire, burning, lava and Ashlands ash (falling lava rocks still hurt it). E on the push handle moves it, E anywhere else on the cannon loads it. Towed by an Asksvin it steers around trees and rocks in its way.
- Every Flametal part of the mod's items and pieces uses the game's own Flametal look.
