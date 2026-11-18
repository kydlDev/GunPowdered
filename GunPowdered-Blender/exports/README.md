# Exports

Made by `scripts/make_bucket.py` (run inside Blender; `export()` writes the FBX files).

- Units: 1 Blender unit = 1 m. Export: FBX, `apply_unit_scale`, `FBX_SCALE_ALL`, forward -Z, up Y, bake space transform, face smoothing.
- Blender objects are `snake_case` (`bucket_wood`, `bucket_bronze`, `bucket_leather`, `bucket_water`); exported files are `PascalCase`.
- `Bucket.fbx`: wood staves (two opposite staves rise above the rim as rounded ears with a hole, part of the stave), two bronze hoops, leather rope (arch + a loop through each ear hole). About 500 triangles.
- `BucketFull.fbx`: the same plus `bucket_water` (a blue water surface inside the rim).
- Materials: `bucket_wood_mat`, `bucket_bronze_mat`, `bucket_leather_mat`, `bucket_water_mat` (colours are placeholders; Unity gets its own Valheim-style materials).
- Not imported into Unity yet (the GunPowdered-Unity MCP link was not connected when these were made).
- `LeachingStation.fbx` (script `make_leaching_station.py`): parts leach_barrel (with a small outlet hole), leach_hoops, leach_stones, leach_stand, leach_trough, leach_flow (state: thin trickle), hearth_stones, cauldron, cauldron_legs, cauldron_liquid / cauldron_fire / cauldron_salt (states: burning = liquid + fire, done = salt only). About 4,000 triangles. No separate wood pile: the hearth takes the wood.
- `PowderMill.fbx` (script `make_powder_mill.py`): mill_base, mill_pan, mill_frame (fixed), mill_arm + mill_runners (rotate together; the object origin is the pivot on the shaft; automatic, no handle: the mod turns them while the mill works), mill_barrels, mill_barrel_hoops, mill_powder (state: black powder in the pan). A drive cog sits on top of the shaft. About 1,600 triangles.
- Copied into `GunPowdered-Unity/Assets/GunPowdered_Art/Models/<Name>/` (Bucket, LeachingStation, PowderMill).
- `RainBarrel.fbx` (script `make_rain_barrel.py`): rain_barrel, rain_barrel_hoops, rain_barrel_water (no tap: the player interacts with the barrel and takes all the water at once). The water disc is drawn at the full level (z=0.86); the mod moves it down toward z=0.13 as the barrel empties and hides it at 0. About 1,000 triangles. Pan of the Powder Mill is grey stone, its grinding stones are polished black stone, the powder is black.

- Items and deposits (script `make_items.py`; `build()` makes them, `export()` writes the FBX files, `icon(names, path)` renders a transparent PNG icon): `GuanoDeposit.fbx` (guano_deposit, dark brown heap, ~720 tris), `SulfurousMud.fbx` (sulfur_mud olive-yellow mound + sulfur_crystals bright yellow spikes, ~680), `BatGuano.fbx` (guano_item, small soft dark heap), `Saltpeter.fbx` (saltpeter_item, white heap with a few crystals, ~480), `Gunpowder.fbx` (gunpowder_item, black heap, ~400), `Water.fbx` (water_item, blue teardrop, ~300). Object origin = bottom centre. The items are what a dropped stack looks like. First pass: low-poly lumps, colours are placeholders.
- Icons (256x256 transparent PNG, rendered from the item models): `GunPowdered-Unity/Assets/GunPowdered_Art/Icons/{BatGuano,Saltpeter,Gunpowder,Water,GuanoDeposit,SulfurousMud}.png`. Icons for Bucket, BucketFull, LeachingStation, PowderMill and RainBarrel are made the same way. Contact sheet: Photos/GunPowdered/icons-sheet-v1.png.
- All FBX files above are copied into `GunPowdered-Unity/Assets/GunPowdered_Art/Models/<Name>/`.
