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

## Addendum 2026-11-24: controls overhaul (supersedes the screen split)

Directed by Vashon after playing the published combat build on his
phone: he could not move the character, and he ordered all action
buttons removed. Diagnosis on the shipped artifact (CDP touch probe,
2026-11-24): an identical upward drag starting on the RIGHT half of
the screen produced movement input 0.00 for its whole duration, while
the same drag on the left half read 1.00. The split below is the bug:
rule 4 and the edge cases that reference it are superseded.

New scheme, one gesture stream covering the whole screen:

- Drag (sustained press and move) steers the hero from anywhere on
  screen, with the MECH-001 numbers unchanged (deadzone 12 px, max
  drag 90 px, speed 4.2 m/s).
- Tap (short press, peak drift at or under 26 px at 1080p width)
  attacks; repeated taps chain the MECH-009 combo.
- Still hold charges heavies (MECH-009) and, beside a defeated
  creature, is the harvest hold (MECH-005).
- Flick (fast swipe: at most 0.32 s, at least 55 px, average velocity
  at least 700 px per s, all at 1080p width scale) dodges in the
  flick direction through the MECH-009 dodge and iframe system.
  A sustained drag can never misfire as a flick: the duration and
  average velocity gates reject it no matter how fast it ends.
- Glide boost (MECH-002) is a tap while gliding, anywhere now.
- There are no on screen action buttons. The HUD is display only and
  its canvas has no GraphicRaycaster, so no HUD element can intercept
  a touch. Movement input values live in Tuning group Gestures.

Proof standard raised by the same playtest: the acceptance harness
must drive realistic touch sequences (touchstart, touchmove,
touchend with real coordinates and timing) including drags that
start on the right half and over the HUD area, in
~/workspace/browser-test-scratch/touch_test.py.

## Addendum 2026-11-24: locomotion is exploration only

Under the MECH-009 fight mode addendum of the same date, drag
movement applies only outside a fight. While a monster is engaged
(Targeting.FightMode), MoveVector returns zero and the same drag is
classified purely as a combat gesture (tap, charge hold, swipe).
A fight starting or ending mid drag re anchors the gesture at the
finger's current position, so neither state sticks.
