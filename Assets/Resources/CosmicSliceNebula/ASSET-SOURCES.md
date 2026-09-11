# Localized portal nebula

- Source: Tripo Nano Banana (`banana`), image-to-image generation.
- Task: `18e74f6c-604e-4e71-8a7e-be223bc05ebb`, status success, 2026-09-09, 5 credits.
- [Generated source image](https://tripo-data.rg1.data.tripo3d.com/tcli_c6fae067c9334bd9af3d07029c0e7b00/20260909/18e74f6c-604e-4e71-8a7e-be223bc05ebb/generated_image.png)
- Local original: `outputs/CosmicSlice-Nebula-20260909/art/01_generated_image_url.png` relative to the parent workspace. Prompt and task response are in the adjacent `tripo_task_18e74f6c-604e-4e71-8a7e-be223bc05ebb.json`.
- User supplied image #4 was used as a style reference only; none of the reference images were imported as game textures. New art is a square wispy blue/violet irregular annulus with an empty black centre, no text, no planets, no UI. Shader treats source black as transparent, provides local motion, physical shallow layers and sparse dust.
- Asset retrieval first: ange-embed `list_v2_collections`, facets for `h5_respack_texture_v1` (artistic_style/environment) and `h5_respack_v1` (asset_type/theme), then nebula searches. Results were ground materials or unrelated architecture/image collections, with no suitable licensed nebula; no retrieved asset was imported.
- Tripo-generated output follows the user's requested generation service and its applicable terms; it is not claimed to be CC0 or a retrieved stock license.
- Initial default-size request returned HTTP400 (banana rejects 2K); successful request explicitly used 1024x1024, 1:1.
