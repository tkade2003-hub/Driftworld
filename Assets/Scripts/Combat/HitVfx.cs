using UnityEngine;

namespace Driftworld
{
    // MECH-009 juice: pooled hit sparks. Presentation only, spawned from
    // the authoritative CombatEvents channel after a hit has resolved,
    // capped at a fixed pool with steal oldest behavior under spam, and
    // every cube returns to rest when its burst ends (juice skill rules on
    // pooling, caps, and cleanup).
    public class HitVfx : MonoBehaviour
    {
        private class Burst
        {
            public GameObject Root;
            public Transform[] Cubes;
            public Vector3[] Velocities;
            public float Life;
            public bool Active;
        }

        private readonly System.Collections.Generic.List<Burst> bursts = new System.Collections.Generic.List<Burst>();
        private const int PoolSize = 14;
        private const int CubesPerBurst = 5;
        private const float Lifetime = 0.28f;

        private void Awake()
        {
            for (int i = 0; i < PoolSize; i++)
            {
                var root = new GameObject("SparkBurst");
                root.transform.SetParent(transform, false);
                var cubes = new Transform[CubesPerBurst];
                for (int c = 0; c < CubesPerBurst; c++)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.SetParent(root.transform, false);
                    go.transform.localScale = new Vector3(0.09f, 0.09f, 0.09f);
                    Object.Destroy(go.GetComponent<Collider>());
                    go.GetComponent<Renderer>().material = Diag.SafeMaterial(Color.white);
                    cubes[c] = go.transform;
                }
                root.SetActive(false);
                bursts.Add(new Burst { Root = root, Cubes = cubes, Velocities = new Vector3[CubesPerBurst] });
            }
        }

        private void OnEnable() { CombatEvents.OnMonsterHit += HandleHit; }
        private void OnDisable() { CombatEvents.OnMonsterHit -= HandleHit; }

        private void HandleHit(DamageResult r)
        {
            Burst burst = null;
            for (int i = 0; i < bursts.Count; i++) if (!bursts[i].Active) { burst = bursts[i]; break; }
            if (burst == null) burst = bursts[0];

            Color color = r.SeveredPart || r.BrokePart ? new Color(1f, 0.55f, 0.15f)
                : r.Crit ? new Color(1f, 0.85f, 0.3f)
                : r.Elemental > r.Physical ? new Color(0.6f, 0.95f, 1f)
                : new Color(1f, 0.95f, 0.8f);

            burst.Root.transform.position = r.HitPoint;
            burst.Root.SetActive(true);
            burst.Active = true;
            burst.Life = Lifetime;
            for (int c = 0; c < burst.Cubes.Length; c++)
            {
                burst.Cubes[c].localPosition = Vector3.zero;
                burst.Cubes[c].GetComponent<Renderer>().material.color = color;
                Vector3 dir = new Vector3(Random.Range(-1f, 1f), Random.Range(0.2f, 1.2f), Random.Range(-1f, 1f)).normalized;
                burst.Velocities[c] = dir * Random.Range(1.6f, 3.2f);
            }
        }

        private void Update()
        {
            for (int i = 0; i < bursts.Count; i++)
            {
                var burst = bursts[i];
                if (!burst.Active) continue;
                burst.Life -= Time.deltaTime;
                if (burst.Life <= 0f)
                {
                    burst.Active = false;
                    burst.Root.SetActive(false);
                    continue;
                }
                float shrink = Mathf.Clamp01(burst.Life / Lifetime);
                for (int c = 0; c < burst.Cubes.Length; c++)
                {
                    burst.Velocities[c] += Vector3.down * 6f * Time.deltaTime;
                    burst.Cubes[c].localPosition += burst.Velocities[c] * Time.deltaTime;
                    float s = 0.09f * shrink;
                    burst.Cubes[c].localScale = new Vector3(s, s, s);
                }
            }
        }
    }
}
