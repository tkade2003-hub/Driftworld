using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Driftworld
{
    // MECH-009 observability: a JSON snapshot of the live fight, pushed to
    // window.__dwState in WebGL builds (and logged periodically as a
    // fallback) so the CDP harness can verify combat behavior on the
    // shipped artifact itself: movement input, damage events with swing
    // ids, part pools, monster states, hero vitals. Read only; the game
    // never consumes it. It ships with the diagnostics and comes out when
    // the loop is proven on a real device.
    public class TestBridge : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void DwPushState(string json);
#endif

        private PlayerCombat combat;
        private PlayerController controller;
        private float pushTimer;
        private float logTimer;
        private readonly List<DamageResult> hits = new List<DamageResult>();
        private readonly List<string> heroHits = new List<string>();

        private void OnEnable()
        {
            CombatEvents.OnMonsterHit += RecordHit;
            CombatEvents.OnHeroHit += RecordHeroHit;
        }

        private void OnDisable()
        {
            CombatEvents.OnMonsterHit -= RecordHit;
            CombatEvents.OnHeroHit -= RecordHeroHit;
        }

        private void RecordHit(DamageResult r)
        {
            hits.Add(r);
            if (hits.Count > 64) hits.RemoveAt(0);
        }

        private void RecordHeroHit(float amount, bool evaded)
        {
            heroHits.Add("{\"amt\":" + F(amount) + ",\"evaded\":" + (evaded ? "true" : "false")
                + ",\"t\":" + F(Time.time) + "}");
            if (heroHits.Count > 32) heroHits.RemoveAt(0);
        }

        private static string F(float v)
        {
            return v.ToString("F2", CultureInfo.InvariantCulture);
        }

        private void Update()
        {
            if (combat == null)
            {
                combat = Object.FindAnyObjectByType<PlayerCombat>();
                if (combat != null) controller = combat.GetComponent<PlayerController>();
                if (combat == null) return;
            }
            pushTimer -= Time.deltaTime;
            logTimer -= Time.deltaTime;
            if (pushTimer > 0f) return;
            pushTimer = 0.25f;

            string json = BuildJson();
#if UNITY_WEBGL && !UNITY_EDITOR
            DwPushState(json);
#endif
            if (logTimer <= 0f)
            {
                logTimer = 2f;
                Debug.Log("DWSTATE " + json);
            }

            Diag.Combat = "wpn " + (combat.Weapon != null ? combat.Weapon.DisplayName : "none")
                + " ph " + combat.CurrentPhase
                + (combat.CurrentStepName.Length > 0 ? " " + combat.CurrentStepName : "")
                + (Targeting.Locked && Targeting.Current != null ? " lock " + Targeting.Current.DisplayName : "")
                + (combat.IsInvulnerable ? " IFRAME" : "");
        }

        private string BuildJson()
        {
            var sb = new StringBuilder(2048);
            sb.Append("{\"t\":").Append(F(Time.time));
            Vector3 hp = combat.transform.position;
            sb.Append(",\"hero\":{\"x\":").Append(F(hp.x)).Append(",\"y\":").Append(F(hp.y)).Append(",\"z\":").Append(F(hp.z));
            sb.Append(",\"hp\":").Append(F(combat.Health)).Append(",\"stam\":").Append(F(combat.Stamina));
            sb.Append(",\"phase\":\"").Append(combat.CurrentPhase).Append("\"");
            sb.Append(",\"weapon\":\"").Append(combat.Weapon != null ? combat.Weapon.WeaponId : "none").Append("\"");
            sb.Append(",\"step\":\"").Append(combat.CurrentStepName).Append("\"");
            sb.Append(",\"locked\":").Append(Targeting.Locked ? "true" : "false");
            sb.Append(",\"lockName\":\"").Append(Targeting.Current != null ? Targeting.Current.DisplayName : "").Append("\"");
            sb.Append(",\"invuln\":").Append(combat.IsInvulnerable ? "true" : "false");
            sb.Append(",\"inp\":").Append(controller != null ? F(controller.LastInputMagnitude) : "0");
            sb.Append(",\"hdg\":").Append(controller != null ? F(Mathf.Atan2(controller.HeadingForward.x, controller.HeadingForward.z) * Mathf.Rad2Deg) : "0");
            sb.Append("},\"monsters\":[");
            for (int i = 0; i < MossHorn.All.Count; i++)
            {
                MossHorn m = MossHorn.All[i];
                if (m == null) continue;
                if (i > 0) sb.Append(",");
                sb.Append("{\"id\":\"").Append(m.TransactionId).Append("\"");
                sb.Append(",\"name\":\"").Append(m.DisplayName).Append("\"");
                sb.Append(",\"hp\":").Append(F(m.Hp)).Append(",\"max\":").Append(F(m.MaxHp));
                sb.Append(",\"state\":\"").Append(m.CurrentState).Append("\"");
                sb.Append(",\"stag\":").Append(F(m.StaggerPool)).Append(",\"stun\":").Append(F(m.StunPool)).Append(",\"kd\":").Append(F(m.KnockdownPool));
                sb.Append(",\"enr\":").Append(m.IsEnraged ? "true" : "false");
                sb.Append(",\"mhdg\":").Append(F(Mathf.Atan2(m.transform.forward.x, m.transform.forward.z) * Mathf.Rad2Deg));
                sb.Append(",\"x\":").Append(F(m.transform.position.x)).Append(",\"z\":").Append(F(m.transform.position.z));
                sb.Append(",\"parts\":{");
                if (m.Hurtboxes != null)
                {
                    var parts = m.Hurtboxes.Parts;
                    for (int p = 0; p < parts.Count; p++)
                    {
                        if (p > 0) sb.Append(",");
                        sb.Append("\"").Append(parts[p].Id).Append("\":{\"pool\":").Append(F(parts[p].DamagePool));
                        sb.Append(",\"sev\":").Append(F(parts[p].SeverPool));
                        sb.Append(",\"broken\":").Append(parts[p].Broken ? "true" : "false");
                        sb.Append(",\"severed\":").Append(parts[p].Severed ? "true" : "false").Append("}");
                    }
                }
                sb.Append("}}");
            }
            sb.Append("],\"loot\":{");
            var bank = LootBank.Instance;
            sb.Append("\"shards\":").Append(bank != null ? bank.Shards : 0);
            sb.Append(",\"kills\":").Append(bank != null ? bank.Kills : 0);
            sb.Append(",\"harvests\":").Append(bank != null ? bank.Harvests : 0).Append("}");
            sb.Append(",\"events\":[");
            int start = Mathf.Max(0, hits.Count - 24);
            for (int i = start; i < hits.Count; i++)
            {
                DamageResult r = hits[i];
                if (i > start) sb.Append(",");
                sb.Append("{\"swing\":").Append(r.SwingId).Append(",\"hit\":").Append(r.HitIndex);
                sb.Append(",\"target\":\"").Append(r.Target != null ? r.Target.TransactionId : "").Append("\"");
                sb.Append(",\"part\":\"").Append(r.Part).Append("\"");
                sb.Append(",\"total\":").Append(F(r.Total)).Append(",\"phys\":").Append(F(r.Physical)).Append(",\"elem\":").Append(F(r.Elemental));
                sb.Append(",\"crit\":").Append(r.Crit ? "true" : "false");
                sb.Append(",\"stag\":").Append(F(r.StaggerDealt)).Append(",\"stun\":").Append(F(r.StunDealt));
                sb.Append(",\"weapon\":\"").Append(r.WeaponId).Append("\",\"step\":\"").Append(r.StepName).Append("\"");
                sb.Append(",\"phase\":\"").Append(r.Phase).Append("\"");
                sb.Append(",\"broke\":").Append(r.BrokePart ? "true" : "false");
                sb.Append(",\"severed\":").Append(r.SeveredPart ? "true" : "false").Append("}");
            }
            sb.Append("],\"heroHits\":[").Append(string.Join(",", heroHits)).Append("]}");
            return sb.ToString();
        }
    }
}
