using UnityEngine;

namespace Driftworld
{
    // MECH-003: the wind spear projectile. Travels straight, expires at max
    // range, and applies skill damage and wind stagger to the first Moss
    // Horn it reaches. Distance checks only; the greybox uses no physics
    // collisions for combat.
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
                if (c == null || c.IsDefeated) continue;
                Vector3 to = c.Center - transform.position;
                to.y = 0f;
                if (to.magnitude < 1.5f)
                {
                    c.TakeHit(Tuning.Combat.skill_damage, Tuning.MossHorn.stagger_skill, true);
                    spent = true;
                    Destroy(gameObject);
                    return;
                }
            }

            if (traveled >= Tuning.Combat.skill_range_m)
            {
                spent = true;
                Destroy(gameObject);
            }
        }
    }
}
