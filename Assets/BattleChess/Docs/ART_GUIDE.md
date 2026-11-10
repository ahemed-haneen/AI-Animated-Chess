# Art guide — Pirates vs British

What an artist (or Asset Store shopping list) needs to deliver for the first theme. The code is ready for all of it; each item plugs into the Theme asset.

## Characters (12 total)

All humanoids should be **rigged as Unity Humanoid** so they can share one animation library. Target **8–15k triangles** per character for mobile, with a second lower-detail version (LOD1, ~4k) for the wide board view. One 2K texture set (PBR: albedo, normal, mask) per character.

| Piece | Pirates | British Army |
|---|---|---|
| King | **Pirate Captain**: long dark coat, tricorne with skull, black beard, cutlass, hook hand | **Governor**: red coat with gold trim, powdered wig, bicorne with plume, sword |
| Queen | **Pirate Queen**: red bandana, white blouse, dark corset, long red hair, flintlock pistol | **Admiral**: navy coat, gold epaulettes, wig, bicorne, sword |
| Rook | **Pirate Ship**: small two-mast ship with black sails and a skull flag, bow cannon *(non-humanoid prop + animation)* | **Field Cannon**: bronze barrel on a wooden carriage + gunner with rammer |
| Bishop | **Gunner**: bulky, bald, beard, bandana, blunderbuss | **Officer**: red coat, sash, bicorne, sword |
| Knight | **Swashbuckler**: feathered hat, red vest, rapier, very athletic (flips) | **Dragoon**: brass helmet, red coat, sabre, on a horse *(rider + horse, or a combined rig)* |
| Pawn | **Deckhand**: striped shirt, bandana, cutlass (3–4 variations of colour or face) | **Redcoat**: tricorne, cross belts, musket with bayonet |

## Animations per humanoid

| Clip | Notes |
|---|---|
| `idle` | Loop with breathing and small weight shifts, plus a few variations if possible |
| `walk` | Loop, in place (root motion off) |
| `run` | Loop for longer moves (optional) |
| `attack` | The signature move. Add an **`Impact`** animation event at the moment of contact or firing |
| `hit` | Short flinch |
| `death` | Fall and lie still (non-looping) |
| `victory` | Cheer or celebrate (loop) |
| `selected` | "Ready" pose or salute when tapped (optional) |

**Signature attacks to make the game memorable:** the Swashbuckler's flip-slash, the Gunner's blunderbuss blast with knock-back, the Dragoon's charge with a rearing horse, the Pirate Queen's quick-draw, and the Captain's hook-and-cutlass combo.

## Effects (VFX prefabs)

Musket and pistol muzzle flashes with smoke, cannon blast with heavy smoke and screen shake, cannonball trail, sword clash sparks, dust puffs on footsteps and falls, water splash and wake for the ship, a gold "promotion" pillar, and confetti or cannon salute for victory.

Assign per piece in the Theme asset (**Impact Vfx**, **Muzzle Vfx**).

## Audio

Footsteps (boots on sand or wood), hooves, cannon roll, sword clashes, musket and pistol shots, cannon boom, grunts and death cries, ship creak, crowd cheer, and a music loop for each side (sea shanty versus fife and drum).

Assign per piece in the Theme asset (**Attack / Impact / Death Sound**). Missing ones fall back to the built-in synthesised sounds.

## Environment (for TV-worthy shots)

The board is a beach of sand and shallow-water squares. Around it: palm trees, a fort wall on the British side, a docked ship on the pirate side, a sunset sky, and baked lighting.

## Getting started cheaply

1. **Mixamo**: free rigged characters and hundreds of animations, enough to replace every humanoid with something that moves properly within a day.
2. **Unity Asset Store**: search "pirate", "redcoat", "18th century soldier", "cannon" and "pirate ship". Check that each licence allows commercial use.
3. **Commission** the signature characters and attacks once the sample build proves the fun.
