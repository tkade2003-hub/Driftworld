using UnityEngine;

namespace Driftworld
{
    // MECH-003: the wind spear projectile, now the Long Sword special.
    // Travels straight, expires at max range, and resolves against the
    // first monster part it reaches through the MECH-009 damage pipeline.
    // Distance checks only; the greybox uses no physics collisions for
    // combat.
    public class WindSpear : MonoBehaviour
    {
        private Vector3 velocity;
        private float traveled;
        private bool spent;

        public void Launch(Vector3 origin, Vector3 direction)
        {
            transform.position = origin;
            velocity = direction.normalized * Tuning.Combat.skill_speed_mps;
            traveled = 0f;
            spent = false;
        }

        private void Update()
        {
            if (spent) return;
            float step = Tuning.Combat.skill_speed_mps * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            traveled += step;

            for (int i = 0; i < MossHorn.All.Count; i++)
            {
                MossHorn c = MossHorn.All[i];
                if (c == null || c.IsDefeated || c.Hurtboxes == null) continue;

                HurtboxPart bestPart = null;
                float bestDist = float.MaxValue;
                var parts = c.Hurtboxes.Parts;
                for (int p = 0; p < parts.Count; p++)
                {
                    if (parts[p].Inert) continue;
                    float d = Vector3.Distance(transform.position, parts[p].WorldCenter);
                    if (d < parts[p].WorldRadius + 0.35f && d < bestDist)
                    {
                        bestDist = d;
                        bestPart = parts[p];
                    }
                }
                if (bestPart == null) continue;

                DamageResult r = DamagePipeline.ResolveWindSpear(c, bestPart);
                int outcome = c.ApplyDamage(r, DamageKind.Cut);
                r.BrokePart = outcome == 1;
                r.SeveredPart = outcome == 2;
                CombatEvents.PublishMonsterHit(r);
                c.ApplyHitstop(Tuning.Combat.hitstop_ms / 1000f);
                spent = true;
                Destroy(gameObject);
                return;
            }

            if (traveled >= Tuning.Combat.skill_range_m)
            {
                spent = true;
                Destroy(gameObject);
            }
        }
    }
}
