using System;
using UnityEngine;

namespace Driftworld
{
    // MECH-001: drag movement input. A touch that begins on the left half of
    // the screen becomes the movement anchor; nothing is ever drawn at a
    // fixed position. A tap on the right half is the context action (boost
    // while gliding in this build step; combat buttons arrive with MECH-003).
    // Mouse input mirrors touch so the WebGL lane can be driven on a desktop.
    public class DragInput : MonoBehaviour
    {
        public bool Active { get; private set; }
        public Vector2 Anchor { get; private set; }      // screen px
        public Vector2 Current { get; private set; }     // screen px
        public Vector2 DragPx => Current - Anchor;       // screen px
        public bool BeganThisFrame { get; private set; }

        public event Action OnRightTap;

        private int trackedTouchId = -1;
        private bool mouseHeld;

        private void Update()
        {
            BeganThisFrame = false;

            if (Input.touchCount > 0)
            {
                HandleTouches();
            }
            else
            {
                HandleMouse();
            }
        }

        private void HandleTouches()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);

                if (trackedTouchId == -1 && t.phase == TouchPhase.Began)
                {
                    if (t.position.x < Screen.width * 0.5f)
                    {
                        trackedTouchId = t.fingerId;
                        Anchor = t.position;
                        Current = t.position;
                        Active = true;
                        BeganThisFrame = true;
                    }
                    else
                    {
                        OnRightTap?.Invoke();
                    }
                }
                else if (t.fingerId == trackedTouchId)
                {
                    if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                    {
                        Current = t.position;
                    }
                    else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        trackedTouchId = -1;
                        Active = false;
                    }
                }
            }
        }

        private void HandleMouse()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 p = Input.mousePosition;
                if (p.x < Screen.width * 0.5f)
                {
                    mouseHeld = true;
                    Anchor = p;
                    Current = p;
                    Active = true;
                    BeganThisFrame = true;
                }
                else
                {
                    OnRightTap?.Invoke();
                }
            }
            else if (Input.GetMouseButton(0) && mouseHeld)
            {
                Current = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                mouseHeld = false;
                Active = false;
            }
        }

        // Movement input as a direction in screen space with magnitude 0 to 1,
        // shaped by the MECH-001 deadzone and max drag constants. The px
        // constants are authored at 1080p width and scaled to the real screen.
        public Vector2 MoveVector()
        {
            if (!Active) return Vector2.zero;

            float scale = Screen.width / 1080f;
            float dead = Tuning.Movement.deadzone_px * scale;
            float max = Tuning.Movement.max_drag_px * scale;

            Vector2 d = DragPx;
            float mag = d.magnitude;
            if (mag <= dead) return Vector2.zero;

            float shaped = Mathf.Clamp01((mag - dead) / (max - dead));
            return d.normalized * shaped;
        }

        private void OnApplicationFocus(bool focus)
        {
            // MECH-001 edge case: focus loss clears input, no phantom movement.
            if (!focus)
            {
                trackedTouchId = -1;
                mouseHeld = false;
                Active = false;
            }
        }
    }
}
