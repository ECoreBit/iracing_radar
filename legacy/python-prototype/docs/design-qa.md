# Design QA

- Source visual truth: `C:\Users\94681\Documents\iraing_Rader\reference-radar.png`
- Implementation preview: `C:\Users\94681\Documents\iraing_Rader\qa-radar-preview.png`
- Side-by-side comparison: `C:\Users\94681\Documents\iraing_Rader\qa-comparison.jpg`
- Viewport: 320 x 180
- State: simulated left-front and right-rear opponents; player centered

## Full-view comparison evidence

The reference and implementation use the same visual grammar: transparent game overlay, centered white player marker, red horizontal blocks extending from the corresponding side, and vertical displacement for fore/aft position. The game image in the reference is environmental content and is intentionally not embedded in the transparent SimHub overlay.

## Focused-region comparison evidence

No focused crop was needed. The radar contains no typography, icons, images, or small controls; all six visible elements are legible in the full-view 320 x 180 comparison.

## Required fidelity surfaces

- Fonts and typography: not applicable; the final overlay contains no text.
- Spacing and layout rhythm: centered 30 px player silhouette, 152 px side blocks, 48 px opponent height, and symmetric left/right reach match the reference proportions.
- Colors and visual tokens: white player marker and high-visibility translucent warning red match the source. Transparency is preserved for in-game compositing.
- Image quality and asset fidelity: no raster assets are required; the overlay is rendered natively by SimHub at its configured size.
- Copy and content: not applicable.

## Comparison history

1. Earlier implementation had a dark panel, blue idle zones, labels, and a top-down car drawing. These were P1 mismatches against the selected reference.
2. Rebuilt the overlay as a transparent six-shape proximity graphic and replaced the old nearest-distance fallback with SimHub spotter distance and angle fields.
3. Post-fix comparison shows no remaining P0/P1/P2 mismatch. Slight opacity differences against the reference's unknown game scene are acceptable P3 tuning.

## Verification

- C# plugin compilation: passed.
- Radar position math: passed for front (45 degrees), alongside (90 degrees), rear (135 degrees), and radians input.
- Overlay JSON parse: passed; 320 x 180 with six items.
- SimHub plugin load: passed; log contains `iRacing Radar plugin started`.
- SimHub overlay autostart: passed; log contains the installed `iRacing Radar.djson` path.
- Live opponent telemetry: requires an on-track iRacing session.

final result: passed
