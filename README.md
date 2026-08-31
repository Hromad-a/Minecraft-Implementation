# Minecraft Implementation

A small Minecraft-style voxel sandbox built in Unity (6000.3), focused on a data-driven
world generation pipeline and a simple but efficient chunk renderer. Terrain is generated from
stackable noise layers, rendered as face-culled chunk meshes, and the player can walk around,
mine and place blocks.

I first built a quick prototype with Claude ([prototype repo](https://github.com/Hromad-a/Custom-Minecraft-Implementation)) to test out how it might work. Going in, I knew I wanted seed-based, deterministic generation and ScriptableObject-driven settings for block types and generation, so everything can be tweaked designer-style in the editor during play mode. This repository is the second, more deliberate pass, where I approached the implementation more thoroughly.

A chunk-based approach was the natural choice, so I started with data generation and simple rendering with instantiated cubes. Unity's built-in perlin noise doesn't support fractal Brownian motion (fBm), which the layered terrain needs, so I used a known [implementation](https://github.com/keijiro/PerlinNoise). Terrain is a stack of noise layers that combine into more interesting generation — occasional mountains, chasms, or just plain areas. I created four: two basic ones for variation, one for mountains and one for chasms and holes in the terrain. Each block type defines a height range, and where ranges overlap, an influence value plus a jitter noise makes the transitions between types more organic. Some decisions here deliberately diverge from the prototype. It stored chunks in a 2D dictionary of full-height columns, but I chose a 3D dictionary of cubic chunks instead — it leaves the door open for cave generation, and it keeps streaming chunk units small, so sky and fully buried chunks can be skipped without even generating their data. Blocks render as one generated mesh per chunk. For player collision I use a custom check against the block data instead of a collider, which makes movement in tight block areas more precise. The prototype had no colliders on the chunks at all and used a voxel raycaster for block targeting. Here I went with mesh colliders from the generated chunk meshes and a physics raycast, since it didn't look like it needed that optimization at this scale.

I used Claude in this repository too — at the beginning mostly for consultation, later, with less time on my hands, for code changes and implementations as well. I mainly used it to help me implement the world rendering and streaming, while I provided constraints of what I wanted to render or not to render to improve the performance. I also used it for general refactoring and code improvements, player movement physics, the mining/placing interactions or layer inspector previews. I usually plan out how I want the features to work, suggest an approach and let it make iteration. I enjoyed planning out the architecture, and I especially enjoyed tweaking the system, adding parameters to layers so that I can comfortably prepare interesting terrain and proper tools to make it easily adjustable.


## Features

- **Layered terrain generation** — the heightmap is a sum of `NoiseLayer` ScriptableObjects
  (fBm perlin noise), each with its own scale, octaves, amplitude, height offset, blur,
  0–1 influence slider and enable toggle.
- **Per-layer masks** — each layer can be limited to organic regions by a perlin-based mask
  with Photoshop-levels-style threshold + feather, octaves, blur and invert. Layer assets show
  live noise / mask / combined previews in the inspector.
- **Seeded worlds** — one seed string drives every layer and mask through per-index sub-seeds;
  an empty seed generates a random world each time.
- **Height-banded block types** — block definitions claim height ranges (as fractions of world
  height); the band boundaries are waved by a separate jitter noise so type transitions are
  smooth areas instead of flat cuts.
- **Chunk streaming** — the world is horizontally infinite and generated lazily:
  cubic chunks stream in around the player nearest-first within a per-frame
  time budget, and are destroyed when left behind. Chunks with no visible
  geometry (sky, fully buried) get no GameObject at all. View radius and
  budget are tunable on the WorldRenderer component.
- **Chunk mesh rendering** — one mesh per chunk with only air-facing faces emitted
  (cross-chunk culling included), one submesh per block type, per-face UVs into a
  3-tile texture atlas (top / sides / bottom). MeshColliders back the block
  targeting raycast.
- **Custom voxel player physics** — the player is an axis-aligned 0.6×1.8 box resolved
  axis-by-axis directly against the block data (no CharacterController), so 1-block holes
  and tunnels are walkable. Spawns at the world origin, 2 blocks above the surface.
- **Mining & building** — hold left mouse to mine (per-block `mineDuration`, unbreakable
  blocks supported), right mouse to place, scroll wheel to select the block type.
  The bottom world layer cannot be mined and nothing can be placed above the world ceiling.
  Edits rebuild only the affected chunk (and border neighbors).

## Controls

| Input        | Action                                    |
| ------------ | ----------------------------------------- |
| Mouse        | Look (click to lock cursor, Esc to free)  |
| W/A/S/D      | Move                                      |
| Space (hold) | Jump                                      |
| Left mouse   | Mine the targeted block (hold)            |
| Right mouse  | Place the selected block                  |
| Scroll wheel | Cycle the block type to place             |

A minimal HUD shows the crosshair, selected block and mining progress.

## Project structure

```
Assets/
  Data/                      ScriptableObject assets
    WorldSettings.asset      world height, chunk size, seed, ground level, layer & block lists
    Layers/                  NoiseLayer assets (BaseLayer, Mountains, ...)
    Blocks/                  block definitions (Rock, Grass, Snow)
  Graphics/
    Materials/               one material per block type
    Textures/                3-tile atlases per block (top | sides | bottom)
  Scripts/
    Data/                    WorldSettings, NoiseLayer (+inspector previews),
                             BlockDefinitionBase, BlockData, WorldData
    Generation/              WorldGenerator (heightmap, bands, jitter), World (block API,
                             regenerate, surface lookup)
    Perlin/                  perlin noise + fBm implementation
    Rendering/               ChunkMeshBuilder (face-culled chunk meshes), WorldRenderer
    Player/                  PlayerController (voxel physics), PlayerInteraction (mine/place)
```

## How generation works

1. `WorldData` resolves the seed once and precomputes the generation context (block-type
   height bands, per-layer noise offsets, type-jitter offset). Cubic chunks are generated
   lazily — reading any cell creates its chunk on demand, deterministically — with terrain
   heights cached per chunk column so a vertical stack computes its heightmap only once.
2. For every terrain column the enabled layers are summed:
   `groundLevel * ySize + Σ (fbm(x,z) * amplitude + heightOffset) * mask * influence`.
3. Blocks below the resulting height are solid; each solid block picks its type from the
   height bands (evaluated at `worldY + jitter` for wavy transitions), where the band whose
   midpoint is closest — by tent-function influence — wins. TypeId 0 means air.
4. `WorldRenderer` streams chunks around the viewer within a millisecond budget per frame,
   building one face-culled mesh (+ collider) per chunk, with materials per block type
   via submeshes.

## Tweaking the world

Everything is tunable from the assets in `Assets/Data` (changes apply on the next
play mode session):

- **WorldSettings** — world height, chunk size, seed, ground level (fraction of world
  height), and the block type boundary jitter (amplitude / scale / octave).
- **Layer assets** — create via `Create → Noise Layer`, add to the WorldSettings list.
  Amplitude is vertical strength in blocks; the mask limits where the layer applies.
  The inspector previews update as you tweak.
- **Block assets** — material, mine duration (negative = unbreakable) and the height range
  band that the type occupies.