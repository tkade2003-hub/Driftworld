using System;
using UnityEngine;

namespace Driftworld
{
    // MECH-001 as amended 2026-11-24 (controls overhaul). ONE gesture
    // stream covers the whole screen; there is no left half or right
    // half split anymore. The old split silently ate every drag that
    // began on the right half, which is why the shipped combat build
    // could not be moved by a right thumb on a real phone while the
    // acceptance harness (synthetic drags, all left half) passed.
    //
    // The primary touch (or the mouse, for desktop testing) is tracked
    // from press to release and classified:
    //   Drag  sustained press and move: steers the hero through
    //         MoveVector for as long as it is held (MECH-001).
    //   Tap   short press, little movement: the strike press and
    //         release pair, plus the OnTap event (glide boost).
    //   Hold  still press held past the heavy charge time: the strike
    //         pair with a long held duration (Great Sword heavies and
    //         charges), and the harvest hold beside a corpse.
    //   Flick fast swipe released quickly: the dodge, direction
    //         included. All three flick gates (duration, distance,
    //         average velocity) must pass, so a sustained movement
    //         drag can never misfire as a dodge no matter how hard
    //         its final yank is: its average velocity stays low.
    // Durations come from the touch event timestamps when the platform
    // supplies them, falling back to the unscaled clock. Event stamps
    // keep tap, flick, and heavy judgement true to the gesture even
    // when frames run slowly.
    //
    // Fight mode (MECH-009 as amended 2026-11-24): while a monster is
    // engaged (Targeting.FightMode, the AI engagement state), there is
    // no locomotion at all, Monster Hunter Now style. MoveVector
    // returns zero, a tap still attacks, a still hold still charges,
    // and any decisive swipe released inside the fight swipe window
    // dodges in its direction at any speed, because there is no
    // movement drag for it to be confused with. Outside a fight the
    // scheme above is unchanged. A fight starting or ending mid
    // gesture re anchors the held touch so neither state sticks.
    [DefaultExecutionOrder(-50)]
    public class DragInput : MonoBehaviour
    {
        // Movement surface (MECH-001), unchanged contract: PlayerController
        // steers from MoveVector and TouchRipple draws from Anchor.
        public bool Active { get; private set; }
        public Vector2 Anchor { get; private set; }
        public Vector2 Current { get; private set; }
        public Vector2 DragPx => Current - Anchor;
        public bool BeganThisFrame { get; private set; }

        // Strike pair, mirroring the old HoldButton edges so combat reads
        // gestures exactly where it read the button.
        public bool StrikeHeld { get; private set; }
        public bool StrikePressedThisFrame { get; private set; }
        public bool StrikeReleasedThisFrame { get; private set; }
        public bool StrikeCanceledThisFrame { get; private set; }
        public float StrikeHeldSecondsAtRelease { get; private set; }

        // Flick output, valid on the frame FlickedThisFrame is set.
        // Direction is screen space (y up), same convention as MoveVector.
        public bool FlickedThisFrame { get; private set; }
        public Vector2 FlickDirection { get; private set; }

        // Raw touch state for context holds (harvest).
        public bool TouchHeld { get; private set; }
        public float PeakDisplacement { get; private set; }
        public bool HeldStill => TouchHeld && PeakDisplacement <= Tuning.Gestures.hold_still_px * Scale;

        // Observability for the test bridge.
        public string LastGesture { get; private set; } = "none";
        public float LastGestureAt { get; private set; } = -100f;

        public event Action OnTap;

        private int trackedTouchId = -1;
        private bool tracking;
        private bool strikeLive;
        private double startStamp;
        private float startUnscaled;
        private bool fightMode;

        private float Scale => Screen.width / 1080f;

        private void Update()
        {
            // Fight mode transitions (MECH-009 fight mode): when the
            // fight starts or ends mid gesture, re anchor the held
            // touch where the finger currently rests and restart its
            // clock. A drag that began as exploration movement must not
            // keep walking the hero once the fight begins (MoveVector
            // is gated separately), and a drag held as the fight ends
            // must not lurch from a stale anchor offset: from the
            // transition on, the gesture measures only what the finger
            // does next, exactly like a fresh press at that point.
            bool fm = Targeting.FightMode;
            if (fm != fightMode)
            {
                fightMode = fm;
                if (tracking)
                {
                    Anchor = Current;
                    PeakDisplacement = 0f;
                    startStamp = Time.unscaledTime;
                    startUnscaled = Time.unscaledTime;
                }
            }

            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    if (!tracking && t.phase == TouchPhase.Began)
                    {
                        trackedTouchId = t.fingerId;
                        BeginTouch(t.position, Time.unscaledTime);
                    }
                    else if (tracking && t.fingerId == trackedTouchId)
                    {
                        if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                        {
                            UpdatePosition(t.position);
                        }
                        else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                        {
                            // The release point is part of the gesture:
                            // a flick is measured to where the finger
                            // lifted, not to the last Moved sighting
                            // before it.
                            UpdatePosition(t.position);
                            EndTouch(Time.unscaledTime);
                        }
                    }
                    else if (!tracking && t.phase == TouchPhase.Ended)
                    {
                        // A tap shorter than one frame: the platform
                        // folded the begin into this ended sighting.
                        // Replay it as an instant tap so sub frame taps
                        // still strike, the touch twin of the event
                        // based key edge in PlayerCombat.
                        BeginTouch(t.position, Time.unscaledTime);
                        EndTouch(Time.unscaledTime);
                    }
                }
            }
            else
            {
                // Mouse mirrors the same single stream for desktop play
                // and the keyboard driven acceptance lanes.
                if (!tracking && Input.GetMouseButtonDown(0))
                {
                    trackedTouchId = -2;
                    BeginTouch(Input.mousePosition, Time.unscaledTime);
                }
                else if (tracking && trackedTouchId == -2)
                {
                    if (Input.GetMouseButton(0))
                    {
                        UpdatePosition(Input.mousePosition);
                    }
                    // The release is the up edge OR the button simply
                    // no longer being held: a click shorter than one
                    // frame delivers its down and up edges in the same
                    // frame batch, the begin branch above consumes that
                    // frame, and the up edge is gone by the next one.
                    // Without the held check the tracker wedges shut
                    // and every later touch is ignored (the combat
                    // harness focus click did exactly this to AC-1).
                    if (Input.GetMouseButtonUp(0) || !Input.GetMouseButton(0))
                    {
                        UpdatePosition(Input.mousePosition);
                        EndTouch(Time.unscaledTime);
                    }
                }
            }
        }

        private void BeginTouch(Vector2 pos, double stamp)
        {
            tracking = true;
            TouchHeld = true;
            Active = true;
            BeganThisFrame = true;
            Anchor = pos;
            Current = pos;
            PeakDisplacement = 0f;
            startStamp = stamp;
            startUnscaled = Time.unscaledTime;
            strikeLive = true;
            StrikeHeld = true;
            StrikePressedThisFrame = true;
        }

        private void UpdatePosition(Vector2 pos)
        {
            Current = pos;
            float dist = Vector2.Distance(pos, Anchor);
            if (dist > PeakDisplacement) PeakDisplacement = dist;
            if (strikeLive && PeakDisplacement > Tuning.Gestures.hold_still_px * Scale)
            {
                // The touch has become a movement drag: it can no longer
                // end as a tap or a charge.
                CancelStrike();
            }
        }

        private void CancelStrike()
        {
            strikeLive = false;
            StrikeHeld = false;
            StrikeCanceledThisFrame = true;
        }

        private float Duration(double endStamp)
        {
            double d = endStamp - startStamp;
            if (d > 0.0 && d < 60.0) return (float)d;
            return Time.unscaledTime - startUnscaled;
        }

        private void EndTouch(double stamp)
        {
            float duration = Duration(stamp);
            Vector2 disp = Current - Anchor;
            float totalDist = disp.magnitude;
            float scale = Scale;
            float avgVel = duration > 0.01f ? totalDist / duration : 0f;
            bool flick = duration <= Tuning.Gestures.flick_max_duration_s
                && totalDist >= Tuning.Gestures.flick_min_distance_px * scale
                && avgVel >= Tuning.Gestures.flick_min_velocity_px_s * scale;

            if (flick)
            {
                FlickedThisFrame = true;
                FlickDirection = totalDist > 0.01f ? disp / totalDist : Vector2.up;
                LastGesture = "flick";
                if (strikeLive) CancelStrike();
            }
            else if (fightMode && totalDist >= Tuning.Gestures.flick_min_distance_px * scale
                && duration <= Tuning.Gestures.fight_swipe_max_duration_s)
            {
                // Fight mode swipe (MECH-009 fight mode): with no
                // locomotion for a swipe to be confused with, any
                // decisive swipe inside the window dodges in its
                // direction, at any speed. It feeds the same
                // FlickedThisFrame seam as a flick, so PlayerCombat
                // dodges through the one existing path. A longer hold
                // is a sustained drag, not a swipe, and does nothing.
                FlickedThisFrame = true;
                FlickDirection = disp / totalDist;
                LastGesture = "swipe";
                if (strikeLive) CancelStrike();
            }
            else if (strikeLive)
            {
                StrikeReleasedThisFrame = true;
                StrikeHeldSecondsAtRelease = duration;
                LastGesture = duration <= Tuning.Gestures.tap_max_duration_s ? "tap" : "hold";
                if (duration <= Tuning.Gestures.tap_max_duration_s)
                {
                    OnTap?.Invoke();
                }
            }
            else
            {
                LastGesture = "drag";
            }
            LastGestureAt = Time.unscaledTime;

            StrikeHeld = false;
            strikeLive = false;
            tracking = false;
            TouchHeld = false;
            Active = false;
            trackedTouchId = -1;
        }

        private void LateUpdate()
        {
            BeganThisFrame = false;
            StrikePressedThisFrame = false;
            StrikeReleasedThisFrame = false;
            StrikeCanceledThisFrame = false;
            FlickedThisFrame = false;
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus)
            {
                tracking = false;
                TouchHeld = false;
                Active = false;
                trackedTouchId = -1;
                strikeLive = false;
                StrikeHeld = false;
            }
        }

        // Unit direction with magnitude 0..1 (MECH-001 rule 2): deadzone
        // at or below Tuning.Movement.deadzone_px, full speed at
        // max_drag_px, clamped. Same math as before the overhaul; only
        // the source stream changed (anywhere on screen).
        public Vector2 MoveVector()
        {
            if (!Active) return Vector2.zero;
            // Fight mode (MECH-009 as amended): no walking or running
            // while a monster is engaged. The drag is still tracked and
            // classified (tap, charge hold, swipe), it simply never
            // becomes locomotion until the fight ends.
            if (Targeting.FightMode) return Vector2.zero;
            float scale = Scale;
            float deadzone = Tuning.Movement.deadzone_px * scale;
            float maxDrag = Tuning.Movement.max_drag_px * scale;
            Vector2 drag = DragPx;
            float mag = drag.magnitude;
            if (mag <= deadzone) return Vector2.zero;
            float t = Mathf.Clamp01((mag - deadzone) / Mathf.Max(maxDrag - deadzone, 1f));
            return (drag / mag) * t;
        }
    }
}
