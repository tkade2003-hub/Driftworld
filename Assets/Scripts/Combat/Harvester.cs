using UnityEngine;

namespace Driftworld
{
    // MECH-005: the harvest. A defeated creature shows a prompt; holding it
    // for the harvest duration completes one harvest. Damage taken during
    // the hold cancels it with nothing lost and nothing granted. A
    // completed harvest grants through the LootBank transaction, then the
    // parts fly to the hero while the loot counter climbs.
    public class Harvester : MonoBehaviour
    {
        public HoldButton harvestButton;
        public PlayerCombat combat;

        public MossHorn Nearby { get; private set; }
        public float Progress { get; private set; }
        public string LastGrantSummary { get; private set; }
        public float LastGrantAt { get; private set; }

        private float holdTime;
        private float holdStartedAt;
        private bool holding;

        private void Update()
        {
            Nearby = FindNearby();
            bool wantHold = Nearby != null && ((harvestButton != null && harvestButton.IsHeld) || Input.GetKey(KeyCode.H));

            if (wantHold)
            {
                if (!holding)
                {
                    holding = true;
                    holdTime = 0f;
                    holdStartedAt = combat != null ? combat.LastDamageAt : -100f;
                }
                // Damage during the hold cancels it, without penalty.
                if (combat != null && combat.LastDamageAt > holdStartedAt)
                {
                    holding = false;
                    holdTime = 0f;
                    Progress = 0f;
                    return;
                }
                holdTime += Time.deltaTime;
                Progress = Mathf.Clamp01(holdTime / Tuning.Harvest.harvest_time_s);
                if (holdTime >= Tuning.Harvest.harvest_time_s)
                {
                    CompleteHarvest();
                    holding = false;
                    holdTime = 0f;
                    Progress = 0f;
                }
            }
            else
            {
                holding = false;
                holdTime = 0f;
                Progress = 0f;
            }
        }

        private MossHorn FindNearby()
        {
            MossHorn best = null;
            float bestDist = Tuning.Harvest.harvest_range_m;
            for (int i = 0; i < MossHorn.All.Count; i++)
            {
                MossHorn c = MossHorn.All[i];
                if (c == null || !c.IsDefeated || c.IsHarvested) continue;
                Vector3 to = c.Center - transform.position;
                to.y = 0f;
                float d = to.magnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = c;
                }
            }
            return best;
        }

        private void CompleteHarvest()
        {
            MossHorn target = Nearby;
            if (target == null || LootBank.Instance == null) return;
            if (LootBank.Instance.GrantHarvest(target, out string summary))
            {
                LastGrantSummary = summary;
                LastGrantAt = Time.time;
                target.MarkHarvested();
                SpawnLootFlight(target);
            }
        }

        private void SpawnLootFlight(MossHorn from)
        {
            for (int i = 0; i < 5; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "LootBit";
                go.transform.position = from.Center + new Vector3((i % 3) - 1f, 0.4f + 0.2f * i, (i % 2) * 0.6f - 0.3f);
                go.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
                var mat = Diag.SafeMaterial(new Color(1f, 0.85f, 0.35f));
                go.GetComponent<Renderer>().material = mat;
                Object.Destroy(go.GetComponent<Collider>());
                var flight = go.AddComponent<LootFlight>();
                flight.Begin(transform, i * 0.06f);
            }
        }
    }

    // One piece of loot flying to the hero over loot_flight_s.
    public class LootFlight : MonoBehaviour
    {
        private Transform target;
        private float delay;
        private float t;
        private Vector3 start;

        public void Begin(Transform hero, float delaySeconds)
        {
            target = hero;
            delay = delaySeconds;
            start = transform.position;
        }

        private void Update()
        {
            if (delay > 0f)
            {
                delay -= Time.deltaTime;
                return;
            }
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }
            t += Time.deltaTime / Tuning.Harvest.loot_flight_s;
            Vector3 goal = target.position + Vector3.up * 1.2f;
            Vector3 pos = Vector3.Lerp(start, goal, Mathf.Clamp01(t));
            pos.y += Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 0.8f;
            transform.position = pos;
            transform.Rotate(120f * Time.deltaTime, 160f * Time.deltaTime, 0f);
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
