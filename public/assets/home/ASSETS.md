# Homepage artwork

Update 2026-11-10: the homepage now uses `characters/billy-idle-v7.gif` from the newly generated `billy-v7` six-cell sheet, and `characters/mandy-idle.gif` from the original Mandy six-cell sheet. Billy plays source frames 1 through 6 once each in normal order; no reverse playback, copied endpoint or interpolation is used. Their poster WebPs are shown when animation is inactive. See `characters/manifest.json` for source sheets, filenames, playback order, timings and output dimensions. No non-uniform body scaling is performed during homepage integration.

Created for Issues #10 and #14 on 2026-11-09 using the built-in imagegen tool, then encoded as WebP with sharp. The original Unity files were read as references and were not changed.

| File | Use | Reference and generation brief |
| --- | --- | --- |
| `room.webp` | Background | Approved homepage concept, based on the current game screenshot: wooden bedroom, furniture, wall pictures, rug, violet night opening. Generate an empty scene without people, logo, UFO or UI. |
| `logo.webp` | Page title | Existing `Unity/Assets/Sprites/Gemini_Generated_Image_uhkbhkuhkbhkuhkb.jpg`: remove background and empty margins; retain the pink/white lettering and exact title. |
| `billy.webp` | Independent character layer | Approved concept and game screenshot: full-body Billy, red cap, large pink nose, white/red shirt, blue shorts, blue toy blaster pointing inward. Transparent background. |
| `mandy.webp` | Independent character layer | Approved concept and game screenshot: full-body Mandy, blonde hair, black headband, pink star dress, orange toy blaster pointing inward. Transparent background. |
| `ufo.webp` | Floating decoration | Approved concept: ivory saucer, blue dome, coral rim, soft underside glow. Transparent background. |

These are AI-assisted homepage illustrations, not screenshots or new game models. The design reinterprets the project's existing characters and scene. The sky overlay, lights, particles, text and controls are rendered by the website rather than baked into the image. Existing game/source asset credits remain the team's responsibility; record them in the project README before final submission.

All five images are served locally. No external image/CDN or font service is required. Transparent WebP is used for the logo, characters and UFO. Full-resolution generation originals are retained in the local image generation library; the repository contains the optimized website assets.
