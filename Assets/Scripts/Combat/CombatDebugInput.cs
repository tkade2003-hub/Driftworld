using UnityEngine;

namespace Driftworld
{
    // MECH-009 developer controls, keyboard side. The same actions are on
    // the HUD debug panel for touch. Keys: 1 to 5 switch weapons, Z hitbox
    // volume, X hurtbox volumes, N damage numbers, F crit override (off),
    // R reset the engaged monster, T force stagger, Y force knockdown,
    // U force stun, I force enrage.
    public class CombatDebugInput : MonoBehaviour
    {
        private PlayerCombat combat;

        private void Awake()
        {
            combat = GetComponent<PlayerCombat>();
        }

        private MossHorn Engaged()
        {
            if (Targeting.Current != null && !Targeting.Current.IsDefeated) return Targeting.Current;
            MossHorn best = null;
            float bestDist = 30f;
            for (int i = 0; i < MossHorn.All.Count; i++)
            {
                MossHorn m = MossHorn.All[i];
                if (m == null || m.IsDefeated) continue;
                float d = Vector3.Distance(transform.position, m.transform.position);
                if (d < bestDist) { bestDist = d; best = m; }
            }
            return best;
        }

        // Debug staging for tests and drills: G resets the fight and puts
        // the hero in front of the monster, H repositions in front without
        // a reset, B behind without a reset, V resets at charge distance.
        private void StageFight(float dist, bool behind, bool reset)
        {
            // Never yank the hero away from a harvestable corpse: the H
            // key is shared with the harvest hold.
            for (int i = 0; i < MossHorn.All.Count; i++)
            {
                MossHorn c = MossHorn.All[i];
                if (c != null && c.IsDefeated && !c.IsHarvested
                    && Vector3.Distance(transform.position, c.transform.position) < 3.5f) return;
            }
            MossHorn m = Engaged();
            if (m == null) return;
            if (reset) m.ResetFight();
            Vector3 fwd = m.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 pos = m.transform.position + (behind ? -fwd : fwd) * dist;
            pos.y = m.transform.position.y + 0.3f;
            var body = GetComponent<CharacterController>();
            body.enabled = false;
            transform.position = pos;
            body.enabled = true;
            Vector3 toM = m.transform.position - pos;
            toM.y = 0f;
            GetComponent<PlayerController>().SetHeading(Mathf.Atan2(toM.x, toM.z) * Mathf.Rad2Deg);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.G)) StageFight(2.3f, false, true);
            if (Input.GetKeyDown(KeyCode.H)) StageFight(2.3f, false, false);
            if (Input.GetKeyDown(KeyCode.B)) StageFight(2.3f, true, false);
            if (Input.GetKeyDown(KeyCode.V)) StageFight(5.5f, false, true);
            if (Input.GetKeyDown(KeyCode.Alpha1)) combat.EquipWeapon(WeaponCatalog.All[0].WeaponId);
            if (Input.GetKeyDown(KeyCode.Alpha2) && WeaponCatalog.All.Count > 1) combat.EquipWeapon(WeaponCatalog.All[1].WeaponId);
            if (Input.GetKeyDown(KeyCode.Alpha3) && WeaponCatalog.All.Count > 2) combat.EquipWeapon(WeaponCatalog.All[2].WeaponId);
            if (Input.GetKeyDown(KeyCode.Alpha4) && WeaponCatalog.All.Count > 3) combat.EquipWeapon(WeaponCatalog.All[3].WeaponId);
            if (Input.GetKeyDown(KeyCode.Alpha5) && WeaponCatalog.All.Count > 4) combat.EquipWeapon(WeaponCatalog.All[4].WeaponId);
            if (Input.GetKeyDown(KeyCode.Z)) CombatDebug.ShowHitboxes = !CombatDebug.ShowHitboxes;
            if (Input.GetKeyDown(KeyCode.X)) CombatDebug.ShowHurtboxes = !CombatDebug.ShowHurtboxes;
            if (Input.GetKeyDown(KeyCode.N)) CombatDebug.ShowDamageNumbers = !CombatDebug.ShowDamageNumbers;
            if (Input.GetKeyDown(KeyCode.F)) CombatDebug.ForceNoCrit = !CombatDebug.ForceNoCrit;

            MossHorn m = null;
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.T) || Input.GetKeyDown(KeyCode.Y)
                || Input.GetKeyDown(KeyCode.U) || Input.GetKeyDown(KeyCode.I)) m = Engaged();
            if (m == null) return;
            if (Input.GetKeyDown(KeyCode.R)) m.ResetFight();
            if (Input.GetKeyDown(KeyCode.T)) m.ForceStagger();
            if (Input.GetKeyDown(KeyCode.Y)) m.ForceKnockdown();
            if (Input.GetKeyDown(KeyCode.U)) m.ForceStun();
            if (Input.GetKeyDown(KeyCode.I)) m.ForceEnrage();
        }
    }
}
