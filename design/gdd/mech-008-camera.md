# MECH-008 Camera   (status: Draft)
Fantasy: The camera is a good hunting partner. It shows you the creature, the tell, and the way home, and it never argues.

## Rules
1. The camera follows behind and above the hero at the follow distance and height, looking where the hero moves. Movement stays camera relative (MECH-001).
2. In combat, the camera frames both the hero and the engaged creature. During the creature tell (MECH-004), both actors must be fully in frame; the tell is information, and the camera exists to deliver it.
3. A drag on the upper part of the screen nudges the camera yaw. After the return delay with no input, the camera eases back to its follow framing. The nudge never fights the player during combat framing.
4. The camera never clips through terrain or island edges. When geometry closes in, the camera rises and shortens its distance rather than passing through a surface.
5. During glide (MECH-002), the camera pulls back and tilts to show the lane ahead and the destination island. Speed reads through framing and field of view, not through shake. Camera shake is absent from the slice by design; feel comes from hitstop, sound, and motion.

## Tuning Constants  (file: Tuning ScriptableObject set, group Camera)
| Name | Unit | Default | Range | Notes |
|---|---|---|---|---|
| follow_distance_m | m | 6.5 | 5 to 8 | On foot |
| follow_height_m | m | 3.2 | 2.4 to 4 | On foot |
| glide_distance_m | m | 9 | 7.5 to 11 | Pulled back in glide |
| fov_deg | deg | 60 | 55 to 65 | Base |
| fov_boost_deg | deg | 6 | 3 to 9 | Added at boost, glide only |
| return_delay_s | s | 2.5 | 1.8 to 3.5 | After a manual nudge |
| nudge_return_s | s | 0.8 | 0.5 to 1.2 | Ease back duration |

## Edge Cases
- Creature charges past the hero: the camera holds framing on both actors through the pass and the recover, without a whip pan.
- Hero backs against a cliff: the camera rises over the shoulder instead of entering the rock.
- Target lost (creature returns to graze): framing eases back to follow mode with no snap.

## Acceptance Checks
| ID | Check | Method | Verified by |
|---|---|---|---|
| AC-1 | Frame captures at fixed offsets through a full charge show hero and creature fully in frame at tell start, contact, and recover | frame capture | agent |
| AC-2 | A collision sweep through every slice island records zero camera positions inside geometry | scripted sweep | agent |
| AC-3 | Glide captures show the destination island inside the frame for the full lane crossing | frame capture | agent |
| AC-4 | No literal camera values exist in gameplay code; all read from the tuning set | code audit | agent |
| AC-5 | Testers never mention the camera when asked what annoyed them, in four of five interviews | playtest | human |

## Kill Criteria
The camera passes by being unnoticed. If AC-5 fails twice, camera work takes priority over the next content item, because a camera players notice is a tax on every other system in this document set.
