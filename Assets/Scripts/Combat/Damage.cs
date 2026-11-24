using System;
using System.Collections.Generic;
using UnityEngine;

namespace Driftworld
{
    // MECH-009: one resolved hit. Produced by DamagePipeline, consumed by
    // the monster (ApplyDamage), the damage numbers, the hit sparks, and
    // the test bridge event log. Presentation never writes back into it.
    public struct DamageResult
    {
        public MossHorn Target;
        public PartId Part;
        public string WeaponId;
        public string StepName;
        public int SwingId;
        public int HitIndex;
        public float Physical;
        public float Elemental;
        public float Total;
        public bool Crit;
        public float StaggerDealt;
        public float StunDealt;
        public Vector3 HitPoint;
        public bool BrokePart;
        public bool SeveredPart;
        public string Phase;   // combat phase the hit registered in, for tests
    }

    // The single event channel for combat outcomes (the juice skill rule:
    // effects bind to authoritative events, they never create hits).
    public static class CombatEvents
    {
        public static event Action<DamageResult> OnMonsterHit;
        public static event Action<float, bool> OnHeroHit; // amount, evaded

        // Ring of recent monster hits, read by the test bridge.
        public static readonly List<DamageResult> Recent = new List<DamageResult>();
        private const int RecentCap = 64;

        public static void PublishMonsterHit(DamageResult r)
        {
            Recent.Add(r);
            if (Recent.Count > RecentCap) Recent.RemoveAt(0);
            if (OnMonsterHit != null) OnMonsterHit(r);
        }

        public static void PublishHeroHit(float amount, bool evaded)
        {
            if (OnHeroHit != null) OnHeroHit(amount, evaded);
        }
    }

    // Developer toggles shared by the debug panel, the debug keys, and the
    // CDP tests. ForceNoCrit exists so part multiplier checks can read exact
    // ratios off the event log.
    public static class CombatDebug
    {
        public static bool ShowHitboxes;
        public static bool ShowHurtboxes;
        public static bool ShowDamageNumbers = true;
        public static bool ForceNoCrit;
    }

    // The ordered damage pipeline, modelled on the CG Combat Framework
    // damage step order (design reference only, see the Stage 1 audit):
    // base times step times part, then element, then crit on the physical
    // portion, then the vulnerable state multiplier, then reduction.
    public static class DamagePipeline
    {
        public static DamageResult Resolve(WeaponDefinition weapon, AttackStep step,
            MossHorn target, HurtboxPart part, int swingId, int hitIndex, string phase)
        {
            float phys = weapon.Power * step.DamageMult * part.EffectivePhysMult;
            bool crit = !CombatDebug.ForceNoCrit && UnityEngine.Random.value < Tuning.Combat.crit_chance;
            if (crit) phys *= Tuning.Combat.crit_mult;
            float elem = weapon.ElementPower * step.ElementMult * part.ElemMult;

            float total = phys + elem;
            if (target.IsDamageVulnerable) total *= Tuning.MossHorn.stagger_damage_mult;
            if (target.IsElder) total *= 1f - Tuning.MossHorn.damage_reduction_elder;

            float stagger = weapon.StaggerPower * step.StaggerMult;
            float stun = part.Id == PartId.Head
                ? stagger * step.StunMult * Tuning.MossHorn.stun_head_factor
                : 0f;

            return new DamageResult
            {
                Target = target,
                Part = part.Id,
                WeaponId = weapon.WeaponId,
                StepName = step.Name,
                SwingId = swingId,
                HitIndex = hitIndex,
                Physical = phys,
                Elemental = elem,
                Total = total,
                Crit = crit,
                StaggerDealt = stagger,
                StunDealt = stun,
                HitPoint = part.WorldCenter,
                Phase = phase,
            };
        }

        // The Wind Spear projectile resolves as a Long Sword move whose
        // numbers reproduce MECH-003: skill_damage total at multiplier 1
        // against the torso, stagger_skill stagger.
        public static DamageResult ResolveWindSpear(MossHorn target, HurtboxPart part)
        {
            var step = new AttackStep
            {
                Name = "Wind Spear",
                DamageMult = Tuning.Combat.skill_damage / Tuning.Weapons.longsword_power,
                StaggerMult = Tuning.MossHorn.stagger_skill / Tuning.Weapons.longsword_stagger,
                ElementMult = 1f,
                Kind = DamageKind.Cut,
            };
            var weapon = WeaponCatalog.ById("longsword");
            return Resolve(weapon, step, target, part, -1, 0, "Projectile");
        }
    }
}
