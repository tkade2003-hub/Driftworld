using UnityEngine;
using UnityEngine.EventSystems;

namespace Driftworld
{
    // A UI button that reports press, hold, and release, for the combat
    // controls. Combat reads IsHeld for guard and charge, and the press and
    // release edges for strikes and skills. Mouse clicks work through the
    // same handlers, so the WebGL lane is fully playable on a desktop.
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool IsHeld { get; private set; }
        public bool PressedThisFrame { get; private set; }
        public bool ReleasedThisFrame { get; private set; }
        public float HeldSeconds { get; private set; }

        private bool releasePending;
        private float heldAtRelease;

        public float HeldSecondsAtRelease => heldAtRelease;

        public void OnPointerDown(PointerEventData eventData)
        {
            IsHeld = true;
            HeldSeconds = 0f;
            PressedThisFrame = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (IsHeld) Release();
        }

        private void Release()
        {
            if (!IsHeld) return;
            IsHeld = false;
            heldAtRelease = HeldSeconds;
            ReleasedThisFrame = true;
            releasePending = true;
        }

        private void Update()
        {
            if (IsHeld) HeldSeconds += Time.unscaledDeltaTime;
        }

        private void LateUpdate()
        {
            PressedThisFrame = false;
            if (releasePending)
            {
                ReleasedThisFrame = false;
                releasePending = false;
            }
        }

        private void OnDisable()
        {
            IsHeld = false;
        }
    }
}
