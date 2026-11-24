using UnityEngine;

namespace Driftworld
{
    // MECH-009: the one shared target. Before this class the engaged
    // creature was computed three times in three places with three rules
    // (PlayerCombat.BestTarget, the Harvester scan, the CombatHud scan) and
    // they could disagree in the same frame. Now PlayerCombat aims by it,
    // FollowCamera frames by it, and CombatHud draws its creature bar from
    // it. Harvester keeps its own defeated creature scan, which is a
    // different question (loot, not combat).
    public static class Targeting
    {
        public static MossHorn Current { get; private set; }
        public static bool Locked { get; private set; }

        // Fight mode (MECH-009 as amended 2026-11-24, Vashon direction:
        // "When in fight mode, there should be only swipe gestures like
        // in Monster Hunter Now, no walking or running"). Fight mode is
        // the Moss Horn AI engagement state: it is on while any living
        // monster is out of Graze (it has noticed the hero, or has been
        // damaged and not yet settled). The 22 m soft lock is NOT the
        // signal: it is an aim and camera aid that is held across most
        // of an arena, and gating locomotion on it would forbid walking
        // almost everywhere a monster exists. While FightMode is on,
        // DragInput returns no movement vector and classifies swipes
        // as dodges; exploration keeps the full gesture scheme.
        public static bool FightMode { get; private set; }

        // The aim target for an attack starting now: the locked target when
        // one is held, otherwise the nearest living monster inside the soft
        // acquire cone. Returns null when nothing is close enough to aim at.
        public static MossHorn AimTarget(Vector3 heroPos, float range)
        {
            if (Locked && Current != null && !Current.IsDefeated)
            {
                Vector3 to = Current.Center - heroPos; to.y = 0f;
                if (to.magnitude <= Mathf.Max(range, Tuning.LockOn.max_range_m)) return Current;
            }
            return Nearest(heroPos, range + Tuning.LockOn.aim_assist_bonus_m, null);
        }

        // The HUD creature: the locked target, else the nearest engaged one
        // (damaged or alerted), matching the old HUD rule but reading the
        // same registry as everything else.
        public static MossHorn DisplayTarget(Vector3 heroPos)
        {
            if (Locked && Current != null && !Current.IsDefeated) return Current;
            MossHorn best = null;
            float bestDist = 30f;
            for (int i = 0; i < MossHorn.All.Count; i++)
            {
                MossHorn c = MossHorn.All[i];
                if (c == null || c.IsDefeated) continue;
                if (c.Hp >= c.MaxHp && c.CurrentState == MossHorn.State.Graze) continue;
                Vector3 to = c.Center - heroPos; to.y = 0f;
                float d = to.magnitude;
                if (d < bestDist) { bestDist = d; best = c; }
            }
            return best;
        }

        // Controls overhaul 2026-11-24: lock on is automatic and
        // invisible. There is no lock button and no mode. Every frame
        // the shared target is the nearest living monster inside
        // acquire range, so AimTarget aims the hero's attacks at the
        // creature he faces, FollowCamera frames it, and the HUD
        // creature bar follows it, with nothing for the player to
        // toggle. Locked now simply means "a soft target is held".
        public static void SoftUpdate(Vector3 heroPos)
        {
            Current = Nearest(heroPos, Tuning.LockOn.acquire_range_m, null);
            Locked = Current != null;
            FightMode = false;
            for (int i = 0; i < MossHorn.All.Count; i++)
            {
                MossHorn c = MossHorn.All[i];
                if (c != null && !c.IsDefeated && c.CurrentState != MossHorn.State.Graze)
                {
                    FightMode = true;
                    break;
                }
            }
        }

        // Lock button behavior: unlock when locked with nothing to cycle
        // to, take the nearest when unlocked, cycle to the next nearest
        // other monster when locked and one exists. (Pre overhaul
        // behavior, kept for reference; nothing calls it now.)
        public static void ToggleOrCycle(Vector3 heroPos)
        {
            if (!Locked || Current == null)
            {
                MossHorn t = Nearest(heroPos, Tuning.LockOn.acquire_range_m, null);
                if (t != null) { Current = t; Locked = true; }
                return;
            }
            MossHorn next = Nearest(heroPos, Tuning.LockOn.acquire_range_m, Current);
            if (next != null) Current = next;
            else { Current = null; Locked = false; }
        }

        public static void Unlock()
        {
            Current = null;
            Locked = false;
        }

        // Per frame housekeeping from PlayerCombat: a lock on a dead or
        // distant monster drops; a held lock with no target re acquires the
        // nearest living monster in range so the camera never frames air.
        public static void Validate(Vector3 heroPos)
        {
            if (!Locked) { Current = null; return; }
            if (Current != null)
            {
                Vector3 to = Current.Center - heroPos; to.y = 0f;
                if (Current.IsDefeated || to.magnitude > Tuning.LockOn.max_range_m) Current = null;
            }
            if (Current == null)
            {
                Current = Nearest(heroPos, Tuning.LockOn.acquire_range_m, null);
                if (Current == null) Locked = false;
            }
        }

        private static MossHorn Nearest(Vector3 heroPos, float range, MossHorn exclude)
        {
            MossHorn best = null;
            float bestDist = range;
            for (int i = 0; i < MossHorn.All.Count; i++)
            {
                MossHorn c = MossHorn.All[i];
                if (c == null || c.IsDefeated || c == exclude) continue;
                Vector3 to = c.Center - heroPos; to.y = 0f;
                float d = to.magnitude;
                if (d < bestDist) { bestDist = d; best = c; }
            }
            return best;
        }
    }
}
