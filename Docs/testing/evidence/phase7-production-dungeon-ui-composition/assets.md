# Original asset inventory and provenance

All artwork is original for this repository. No external downloaded asset, commercial purchase or extracted concept-image region was used. The approved concept supplied art direction only; no illustrative values or fictitious option IDs were copied.

`Assets/_Project/UI/ProductionDungeon/Art/content-atlas.png` is an original transparent raster generated through the available OpenAI image-generation workflow. Prompt direction: separate dark-fantasy dungeon identity sprites, stone/metal forms, restrained amber/cyan highlights, neutral camera, no text, on a transparent background. A follow-up transparency edit preserved the six identities and removed background haze. The committed PNG is the editable raster source; no layered source was produced. Unity's named 512-pixel slices preserve stable sprite IDs on reauthoring.

| Atlas slice | Presentation identity |
|---|---|
| skeleton | `placement.category.monster` + `placement.option.monster.skeleton` |
| spike | `placement.category.trap` + `placement.option.trap.spike` |
| hoard | `placement.category.loot_node` + `placement.option.loot_node.glittering_hoard` |
| entrance | Existing authored Entrance Hall kind |
| terminal | Existing Completion Terminal kind |
| monster-fallback | Unmapped monster options; deliberately generic hooded creature |

`Tools/Presentation/author_surfaces.py` is the editable original offline Pillow source for 128-pixel stone, corridor and surrounding-rock textures, sixteen exposed-edge masks, sixteen selected-outline masks, grid, hollow legal-anchor diamond, invalid X, selection frame, nine-slice panel/action frames, distinct trap/loot fallback emblems, and resource/floor/focus/collapse icons. Run with the bundled Python runtime; no runtime image generator or added graphics package exists. Resource icons are used by the current HUD. Floor/focus/collapse artwork is available for a later icon-placement refinement; critical current controls retain localized labels.

`DungeonVisualAssetAuthoring.Author` imports the committed PNGs and assigns the small presentation-only `Visuals.asset`. It uses the existing Sprite Editor data-provider API, named slices, bilinear sampling, alpha, no mipmaps and bounded texture size. Valid Unity metadata is committed. The runtime controller requires a complete catalog; isolated world-unit tests may use their existing inline fake presentation policy.

Unmapped goblin/other authored monsters, snare/chilling-sigil traps and basic/hidden-cache loot currently use category fallbacks. Those fallbacks express category, not an invented dedicated identity. The first atlas uses a three-quarter illustrative perspective over a top-down tile plan; a later coherent final-art pass can replace individual sprites and surfaces without changing IDs, gameplay data, selection or UI architecture. Static depth is deliberate; animation, dynamic lighting and full future catalogs are deferred.

Textures remain uncompressed/readable for bounded alpha/import qualification. This first pass does not claim final mobile memory optimization. Future import compression or readability changes require asset/build verification, not gameplay changes.
