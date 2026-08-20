# Cosmic Slice Demo - Asset Attribution

All ANGE library assets listed below are used under the authorization supplied by the project owner: the assets in the previously supplied ANGE library may be used directly in this demo. The collection, point ID, retrieval URL, and role are recorded here for auditability.

## 3D Models

| Role | Collection | Point ID | Source |
| --- | --- | --- | --- |
| Ceiling portal ring | `ange-asset-v1` (legacy collection) | `5e19dd83-6841-43d0-ac42-6ec2b2e2aabf` | https://cdn-hm.holymolly.ai/3/9/39b0964b-1677-43c2-b0f4-eda0324fdc5f.glb |
| Stylized meteor | `ange-asset-v1` (legacy collection) | `e3a10a13-805b-4945-873d-0cc19cc93b64` | https://cdn-hm.holymolly.ai/f/4/f4e3d3fa-a0de-4b02-91e2-bd90cb5ff15d.glb |
| Plasma core | `ange-asset-v1` (legacy collection) | `17bf3967-1b7c-4785-ad1f-11ce01f43400` | https://cdn-hm.holymolly.ai/c/f/cf5bafe3-9a81-495f-bd34-89086af3ffc4.glb |
| Unstable bomb | `ange-asset-v1` (legacy collection) | `29bf0195-5670-421d-9d4c-f44b4ab9ceb2` | https://cdn-hm.holymolly.ai/f/7/f7a0dfe4-167b-4a3e-a948-8fa736cfea4f.glb |
| Quantum pyramid | `h5_respack_v1` | `af18935f-9fb1-42c7-a306-19705db4ff42` | https://cdn-ba.holymolly.ai/7/7/774c430b-5e88-446d-b7d1-10081d974ea3.glb |

## Audio

| Role | Collection | Point ID | Source |
| --- | --- | --- | --- |
| Positive score feedback | `ui_audio` | `85e64f94-5509-4cad-9802-7739ff4ac29d` | https://cdn-ange.holymolly.ai/c/a/ca71ae3f-1d68-433e-867f-2e185eb8f32d.mp3 |
| Negative/wrong-hand feedback | `ui_audio` | `218fd599-cb20-4c57-9658-71247e84e26c` | https://cdn-ange.holymolly.ai/a/7/a71ea856-975a-4e66-8d11-572aea136e47.mp3 |
| Space suspense music | `v2_music_realistic` | `442a02fe-5da9-4e96-aae4-b6b39cd1e47d` | https://cdn-ange.holymolly.ai/f/c/fc74cbb4-0add-4735-966a-a87e0a7d4cb7.mp3 |
| Gameplay BGM, `Melodic Techno - Midnight Pulse` | user-supplied `EDM-Test.zip` | n/a | `/Users/ld/Downloads/EDM-Test.zip` |
| Portal loop | supplied ANGE library | see retrieval log | https://cdn-ange.holymolly.ai/2/3/23f9730f-5b92-4092-9f28-944663db01f5.mp3 |
| Blade whoosh | supplied ANGE library | see retrieval log | https://cdn-ange.holymolly.ai/3/8/3875e69c-c462-48f5-b9f9-6b51e9cf661f.mp3 |
| Explosion | supplied ANGE library | see retrieval log | https://cdn-ange.holymolly.ai/f/3/f3d14d7a-9377-45d2-a55e-955094667b99.mp3 |

## Generated UI

The original HUD backdrop was generated specifically for this project through the Tripo image API using the `banana2` model (Nano Banana route).

- Task ID: `c205d9b0-13ec-49bb-997a-000b5cb54dcd`
- Status: `success`
- Credits consumed: `10`
- Local asset: `Assets/CosmicSlice/Art/UI/AnomalyHUD.png`

The spatial redesign panel was generated with Tripo `banana_pro` and keeps the center empty for native Unity text.

- Task ID: `32983aec-4260-4665-817b-d8732107b719`
- Status: `success`
- Credits consumed: `15`
- Local asset: `Assets/CosmicSlice/Art/UI/AnomalyHUD_Spatial.png`

No third-party text or logos from the generated image are used as functional UI. All live labels, scores, timers, and instructions are native Unity text.

## Generated Environment

The deep-space panorama was generated specifically for this project with Tripo `banana_pro`. The desktop player uses it as a full backdrop. PICO/Android and Unity's in-editor MR preview use content-adaptive transparency: dark space becomes nearly clear while bright stars and blue-violet nebulae peak at 38% opacity. The primary nebula band is rotated to face the player's initial view, keeping the real room readable behind the models, portal, particles, and transparent containment grid without losing the space atmosphere.

- Task ID: `182a9539-e651-4a35-8a6f-0fb95656317f`
- Status: `success`
- Credits consumed: `15`
- Local asset: `Assets/CosmicSlice/Art/Environment/01_generated_image_url.png`
