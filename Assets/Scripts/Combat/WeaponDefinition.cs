using System.Collections.Generic;
using UnityEngine;

namespace Driftworld
{
    // MECH-009: weapon data. A weapon is a ScriptableObject definition, but
    // the greybox stays fully code built: WeaponCatalog constructs the
    // definitions at runtime from Tuning constants, so no hand authored
    // assets exist and no editor step is needed to run a fight. The same
    // classes could be saved as assets later without code changes.
    public enum ElementType { None, Wind }
    public enum DamageKind { Cut, Blunt }

    [System.Serializable]
    public class AttackStep
    {
        public string Name;
        public float Startup;        // s before the hit window opens
        public float Active;         // s the hit window stays open
        public float Recovery;       // s after the window before the hero is free
        public float DamageMult;     // times weapon power
        public float StaggerMult;    // times weapon stagger power
        public float ElementMult = 1f; // times weapon element power
        public float StaminaCost;    // points, spent when the move starts
        public float Range;          // m, weapon reach from the hero
        public float ArcDeg;         // deg, facing cone the hit tests against
        public float StepIn;         // m the attack carries the hero forward
        public int Hits = 1;         // hits this step can register per monster
        public float HitInterval;    // s between hits of a multi hit step
        public DamageKind Kind = DamageKind.Cut;
        public float StunMult = 1f;  // head stun gain multiplier (hammer lives here)
        public bool DodgeCancelStartup; // dodge may cancel the startup phase
        public bool HyperArmor;      // hits taken do not interrupt this move
        public float HitstopMs;      // ms of hitstop on a connect, 0 for none

        public float Total => Startup + Active + Recovery;
    }

    [CreateAssetMenu(menuName = "Driftworld/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        public string WeaponId;
        public string DisplayName;
        public float Power;          // base physical attack
        public float StaggerPower;   // base stagger per hit at multiplier 1
        public ElementType Element;
        public float ElementPower;
        public float GuardMult;      // damage ratio while guarding with this weapon
        public float SpecialCooldown;
        public List<AttackStep> Combo = new List<AttackStep>();
        public AttackStep Heavy;
        public AttackStep Special;   // null when the special is the Wind Spear projectile
        public bool SpecialIsProjectile;
    }

    // The weapon registry. Stage 2 ships the Long Sword (the MECH-003 spear,
    // reclassed) and the Hammer. Stage 3 adds the remaining classes here.
    public static class WeaponCatalog
    {
        private static List<WeaponDefinition> all;
        public static List<WeaponDefinition> All
        {
            get
            {
                if (all == null)
                {
                    all = new List<WeaponDefinition>
                    {
                        BuildLongSword(),
                        BuildHammer(),
                        BuildGreatSword(),
                        BuildSwordShield(),
                        BuildDualBlades(),
                    };
                }
                return all;
            }
        }

        public static WeaponDefinition ById(string id)
        {
            var list = All;
            for (int i = 0; i < list.Count; i++) if (list[i].WeaponId == id) return list[i];
            return list[0];
        }

        private static AttackStep Step(string name, float startup, float active, float recovery,
            float dmg, float stagger, float cost, float range, float arc, float stepIn)
        {
            return new AttackStep
            {
                Name = name,
                Startup = startup,
                Active = active,
                Recovery = recovery,
                DamageMult = dmg,
                StaggerMult = stagger,
                StaminaCost = cost,
                Range = range,
                ArcDeg = arc,
                StepIn = stepIn,
            };
        }

        // The MECH-003 spear, reclassed as the Long Sword. Its numbers are
        // anchored to the slice: power 12 and stagger 8 are the old strike
        // values, the heavy multiplier is the old 2.2, and the special is
        // the Wind Spear projectile on the old cooldown.
        private static WeaponDefinition BuildLongSword()
        {
            var w = ScriptableObject.CreateInstance<WeaponDefinition>();
            w.WeaponId = "longsword";
            w.DisplayName = "Long Sword";
            w.Power = Tuning.Weapons.longsword_power;
            w.StaggerPower = Tuning.Weapons.longsword_stagger;
            w.Element = ElementType.Wind;
            w.ElementPower = Tuning.Weapons.longsword_wind;
            w.GuardMult = Tuning.Combat.guard_damage_mult;
            w.SpecialCooldown = Tuning.Combat.skill_cooldown_s;
            w.SpecialIsProjectile = true;

            var s1 = Step("Rising Slash", 0.10f, 0.12f, 0.23f, 1.0f, 1.0f, 0f, 2.6f, 100f, 0.35f);
            var s2 = Step("Thrust", 0.09f, 0.12f, 0.22f, 1.1f, 1.0f, 0f, 2.7f, 80f, 0.4f);
            var s3 = Step("Spirit Arc", 0.12f, 0.14f, 0.30f, 1.4f, 1.25f, 0f, 2.6f, 120f, 0.3f);
            w.Combo.Add(s1); w.Combo.Add(s2); w.Combo.Add(s3);

            var heavy = Step("Spirit Thrust", 0.28f, 0.16f, 0.40f, Tuning.Combat.heavy_damage_mult,
                Tuning.MossHorn.stagger_heavy / Tuning.Weapons.longsword_stagger,
                Tuning.Combat.heavy_stamina_cost, 2.8f, 90f, 0.55f);
            heavy.HitstopMs = Tuning.Combat.hitstop_ms;
            w.Heavy = heavy;
            return w;
        }

        // The Hammer: slow, blunt, and built to stun through the head. Its
        // stun multipliers are what make head hits stun faster than any
        // other weapon (MECH-009 AC-10).
        private static WeaponDefinition BuildHammer()
        {
            var w = ScriptableObject.CreateInstance<WeaponDefinition>();
            w.WeaponId = "hammer";
            w.DisplayName = "Hammer";
            w.Power = Tuning.Weapons.hammer_power;
            w.StaggerPower = Tuning.Weapons.hammer_stagger;
            w.Element = ElementType.None;
            w.ElementPower = 0f;
            w.GuardMult = Tuning.Combat.guard_damage_mult;
            w.SpecialCooldown = Tuning.Combat.skill_cooldown_s;
            w.SpecialIsProjectile = false;

            var s1 = Step("Overhead Smash", 0.22f, 0.14f, 0.34f, 1.0f, 1.0f, 0f, 2.2f, 70f, 0.3f);
            s1.Kind = DamageKind.Blunt; s1.StunMult = 1.6f; s1.HitstopMs = 30f;
            var s2 = Step("Side Swing", 0.20f, 0.14f, 0.32f, 1.1f, 1.05f, 0f, 2.3f, 110f, 0.3f);
            s2.Kind = DamageKind.Blunt; s2.StunMult = 1.6f; s2.HitstopMs = 30f;
            var s3 = Step("Uppercut", 0.26f, 0.16f, 0.44f, 1.5f, 1.3f, 0f, 2.2f, 80f, 0.35f);
            s3.Kind = DamageKind.Blunt; s3.StunMult = 2.0f; s3.HitstopMs = 40f; s3.HyperArmor = true;
            w.Combo.Add(s1); w.Combo.Add(s2); w.Combo.Add(s3);

            var heavy = Step("Charged Slam", 0.45f, 0.18f, 0.55f, 2.4f, 1.6f,
                Tuning.Combat.heavy_stamina_cost, 2.4f, 90f, 0.4f);
            heavy.Kind = DamageKind.Blunt; heavy.StunMult = 2.2f;
            heavy.HitstopMs = Tuning.Combat.hitstop_ms; heavy.HyperArmor = true;
            w.Heavy = heavy;

            var special = Step("Big Bang", 0.5f, 0.18f, 0.6f, 3.0f, 1.8f, 0f, 2.4f, 90f, 0.3f);
            special.Kind = DamageKind.Blunt; special.StunMult = 2.5f;
            special.HitstopMs = Tuning.Combat.hitstop_ms; special.HyperArmor = true;
            w.Special = special;
            return w;
        }

        // Stage 3 classes follow. Great Sword: the slowest, heaviest hitter,
        // with a hold to charge heavy whose levels scale the multiplier.
        private static WeaponDefinition BuildGreatSword()
        {
            var w = ScriptableObject.CreateInstance<WeaponDefinition>();
            w.WeaponId = "greatsword";
            w.DisplayName = "Great Sword";
            w.Power = Tuning.Weapons.greatsword_power;
            w.StaggerPower = Tuning.Weapons.greatsword_stagger;
            w.Element = ElementType.None;
            w.ElementPower = 0f;
            w.GuardMult = Tuning.Combat.guard_damage_mult;
            w.SpecialCooldown = 10f;
            w.SpecialIsProjectile = false;

            var s1 = Step("Overhead Chop", 0.38f, 0.16f, 0.52f, 1.0f, 1.0f, 0f, 2.8f, 90f, 0.3f);
            s1.HitstopMs = 40f; s1.HyperArmor = true;
            var s2 = Step("Wide Sweep", 0.36f, 0.16f, 0.50f, 1.15f, 1.05f, 0f, 2.9f, 140f, 0.3f);
            s2.HitstopMs = 40f; s2.HyperArmor = true;
            var s3 = Step("Rising Crush", 0.42f, 0.18f, 0.60f, 1.6f, 1.3f, 0f, 2.8f, 100f, 0.35f);
            s3.HitstopMs = 50f; s3.HyperArmor = true;
            w.Combo.Add(s1); w.Combo.Add(s2); w.Combo.Add(s3);

            // DamageMult here is charge level 1. PlayerCombat scales it by
            // the charge level reached while the strike button is held:
            // level 2 at heavy_charge_s times 2.5, level 3 at times 5,
            // capped by the multipliers in ChargeLevelMult below.
            var heavy = Step("Charged Slash", 0.30f, 0.18f, 0.62f, 1.6f, 1.4f,
                Tuning.Combat.heavy_stamina_cost, 3.0f, 100f, 0.4f);
            heavy.HitstopMs = Tuning.Combat.hitstop_ms; heavy.HyperArmor = true;
            w.Heavy = heavy;

            var special = Step("True Charged Slash", 0.55f, 0.2f, 0.7f, 3.2f, 1.8f, 0f, 3.0f, 100f, 0.4f);
            special.HitstopMs = Tuning.Combat.hitstop_ms; special.HyperArmor = true;
            w.Special = special;
            return w;
        }

        // Great Sword charge level multipliers, indexed by level 1 to 3.
        public static readonly float[] GreatSwordChargeMult = { 0f, 1.0f, 1.375f, 1.875f };

        private static WeaponDefinition BuildSwordShield()
        {
            var w = ScriptableObject.CreateInstance<WeaponDefinition>();
            w.WeaponId = "swordshield";
            w.DisplayName = "Sword and Shield";
            w.Power = Tuning.Weapons.swordshield_power;
            w.StaggerPower = Tuning.Weapons.swordshield_stagger;
            w.Element = ElementType.None;
            w.ElementPower = 0f;
            w.GuardMult = Tuning.Weapons.swordshield_guard_mult;
            w.SpecialCooldown = Tuning.Combat.skill_cooldown_s;
            w.SpecialIsProjectile = false;

            var s1 = Step("Quick Slash", 0.07f, 0.10f, 0.16f, 0.9f, 1.0f, 0f, 2.0f, 110f, 0.25f);
            s1.DodgeCancelStartup = true;
            var s2 = Step("Return Slash", 0.07f, 0.10f, 0.16f, 0.95f, 1.0f, 0f, 2.0f, 110f, 0.25f);
            s2.DodgeCancelStartup = true;
            var s3 = Step("Spinning Slash", 0.08f, 0.11f, 0.20f, 1.05f, 1.1f, 0f, 2.1f, 150f, 0.25f);
            var s4 = Step("Shield Bash", 0.10f, 0.10f, 0.22f, 0.8f, 1.4f, 0f, 1.9f, 90f, 0.2f);
            s4.Kind = DamageKind.Blunt; s4.StunMult = 1.8f;
            w.Combo.Add(s1); w.Combo.Add(s2); w.Combo.Add(s3); w.Combo.Add(s4);

            var heavy = Step("Leaping Slash", 0.16f, 0.14f, 0.30f, 1.8f, 1.3f, 20f, 2.4f, 100f, 0.7f);
            w.Heavy = heavy;

            var special = Step("Perfect Rush", 0.2f, 0.3f, 0.35f, 2.0f, 1.4f, 0f, 2.2f, 110f, 0.4f);
            special.Hits = 2; special.HitInterval = 0.12f;
            w.Special = special;
            return w;
        }

        private static WeaponDefinition BuildDualBlades()
        {
            var w = ScriptableObject.CreateInstance<WeaponDefinition>();
            w.WeaponId = "dualblades";
            w.DisplayName = "Dual Blades";
            w.Power = Tuning.Weapons.dualblades_power;
            w.StaggerPower = Tuning.Weapons.dualblades_stagger;
            w.Element = ElementType.Wind;
            w.ElementPower = Tuning.Weapons.dualblades_wind;
            w.GuardMult = Tuning.Combat.guard_damage_mult;
            w.SpecialCooldown = Tuning.Combat.skill_cooldown_s;
            w.SpecialIsProjectile = false;

            var s1 = Step("Twin Fang", 0.06f, 0.14f, 0.14f, 0.8f, 1.0f, 0f, 1.9f, 110f, 0.25f);
            s1.Hits = 2; s1.HitInterval = 0.07f; s1.DodgeCancelStartup = true;
            var s2 = Step("Cross Fang", 0.06f, 0.14f, 0.14f, 0.85f, 1.0f, 0f, 1.9f, 110f, 0.25f);
            s2.Hits = 2; s2.HitInterval = 0.07f; s2.DodgeCancelStartup = true;
            var s3 = Step("Whirlwind", 0.08f, 0.18f, 0.20f, 1.0f, 1.1f, 0f, 2.0f, 160f, 0.25f);
            s3.Hits = 3; s3.HitInterval = 0.06f;
            w.Combo.Add(s1); w.Combo.Add(s2); w.Combo.Add(s3);

            var heavy = Step("Demon Dance", 0.20f, 0.40f, 0.35f, 0.7f, 0.9f, 25f, 2.0f, 130f, 0.3f);
            heavy.Hits = 4; heavy.HitInterval = 0.09f;
            w.Heavy = heavy;

            var special = Step("Spiral Slash", 0.18f, 0.36f, 0.3f, 0.8f, 1.0f, 0f, 2.1f, 140f, 0.5f);
            special.Hits = 5; special.HitInterval = 0.07f;
            w.Special = special;
            return w;
        }
    }
}
