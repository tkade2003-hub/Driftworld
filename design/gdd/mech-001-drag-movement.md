# MECH-001 Drag Movement   (status: Draft)
Fantasy: Your thumb is the trail. The hero goes where you pull, and the screen stays clean.

## Rules
1. A touch beginning anywhere on the left half of the screen starts movement input. The touch point becomes the anchor; no control is drawn at a fixed position, ever.
2. Dragging beyond the deadzone moves the hero in the drag direction, relative to the camera facing. Speed scales with drag distance up to full speed at max drag.
3. A soft ripple renders at the anchor while the touch is held. When the touch ends, the ripple fades and the hero stops inside the stop time.
4. Movement input never triggers combat actions. The right half of the screen owns combat input (MECH-003).
5. During glide (MECH-002) the same drag steers instead of walking. One input scheme covers both.

## Tuning Constants  (file: Tuning ScriptableObject set, group Movement)
| Name | Unit | Default | Range | Notes |
|---|---|---|---|---|
| deadzone_px | px at 1080p width | 12 | 8 to 20 | Below this, the hero stands still |
| max_drag_px | px at 1080p width | 90 | 60 to 130 | Drag distance that reaches full speed |
| move_speed_mps | m per s | 4.2 | 3.4 to 5.2 | Island crossing pillar assumes this band |
| accel_time_s | s | 0.08 | 0.04 to 0.15 | Time to full speed |
| stop_time_s | s | 0.06 | 0.03 to 0.12 | Time to a full stop after release |
| ripple_fade_s | s | 0.25 | 0.15 to 0.4 | Presentation only |

## Edge Cases
- Touch begins on the left half and slides to the right half: movement continues until release; combat buttons still require a separate touch.
- Second touch during movement: ignored for movement, valid for combat.
- App loses focus mid drag: input clears, hero stops, no phantom movement on return.

## Acceptance Checks
| ID | Check | Method | Verified by |
|---|---|---|---|
| AC-1 | Hero displacement matches drag direction within 5 degrees at four camera yaws | scripted test | agent |
| AC-2 | Speed reaches full at max_drag_px and half at half drag, within 5 percent | headless test | agent |
| AC-3 | No fixed joystick object exists in any shipped scene | scene audit | agent |
| AC-4 | Hero stops within stop_time_s of release, every time in 100 trials | scripted test | agent |
| AC-5 | Four of five new testers move the hero within ten seconds with no instruction | playtest | human |

## Kill Criteria
If AC-5 fails twice after tuning, the onboarding cue is redesigned (a guided first drag in the opening seconds). The drag scheme itself stands, by founder direction; what changes is how the game teaches it.
