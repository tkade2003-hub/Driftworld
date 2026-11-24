using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Driftworld
{
    // MECH-009: floating damage numbers. Pure presentation bound to the
    // authoritative CombatEvents channel (juice skill rule: effects never
    // create hits). A fixed pool of uGUI texts on their own canvas; each
    // number rises and fades over its authored life and returns to the
    // pool. Crits read larger and yellow, breaks orange, severs red, wind
    // heavy hits pale cyan.
    public class DamageNumbers : MonoBehaviour
    {
        private class Item
        {
            public Text Text;
            public Vector3 WorldPos;
            public float Life;
            public bool Active;
        }

        private readonly List<Item> pool = new List<Item>();
        private Font font;
        private const int PoolSize = 28;
        private const float Lifetime = 0.85f;

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);

            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("DmgNum");
                go.transform.SetParent(transform, false);
                var t = go.AddComponent<Text>();
                t.font = font;
                t.alignment = TextAnchor.MiddleCenter;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.verticalOverflow = VerticalWrapMode.Overflow;
                t.raycastTarget = false;
                var r = go.GetComponent<RectTransform>();
                r.sizeDelta = new Vector2(300f, 70f);
                go.SetActive(false);
                pool.Add(new Item { Text = t });
            }
        }

        private void OnEnable() { CombatEvents.OnMonsterHit += HandleHit; }
        private void OnDisable() { CombatEvents.OnMonsterHit -= HandleHit; }

        private void HandleHit(DamageResult r)
        {
            if (!CombatDebug.ShowDamageNumbers) return;
            Item item = null;
            for (int i = 0; i < pool.Count; i++) if (!pool[i].Active) { item = pool[i]; break; }
            if (item == null) item = pool[0]; // steal the oldest slot under spam

            string label = Mathf.RoundToInt(r.Total).ToString();
            if (r.SeveredPart) { label = "SEVERED " + label; item.Text.color = new Color(1f, 0.35f, 0.3f); item.Text.fontSize = 54; }
            else if (r.BrokePart) { label = "BREAK " + label; item.Text.color = new Color(1f, 0.6f, 0.2f); item.Text.fontSize = 52; }
            else if (r.Crit) { label = label + "!"; item.Text.color = new Color(1f, 0.85f, 0.3f); item.Text.fontSize = 56; }
            else if (r.Elemental > r.Physical) { item.Text.color = new Color(0.65f, 0.95f, 1f); item.Text.fontSize = 44; }
            else { item.Text.color = Color.white; item.Text.fontSize = 44; }
            item.Text.text = label;
            item.WorldPos = r.HitPoint + Vector3.up * 0.4f;
            item.Life = Lifetime;
            item.Active = true;
            item.Text.gameObject.SetActive(true);
        }

        private void Update()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            for (int i = 0; i < pool.Count; i++)
            {
                var item = pool[i];
                if (!item.Active) continue;
                item.Life -= Time.deltaTime;
                if (item.Life <= 0f)
                {
                    item.Active = false;
                    item.Text.gameObject.SetActive(false);
                    continue;
                }
                float u = 1f - item.Life / Lifetime;
                Vector3 world = item.WorldPos + Vector3.up * (0.8f * u);
                Vector3 screen = cam.WorldToScreenPoint(world);
                var rt = item.Text.GetComponent<RectTransform>();
                if (screen.z < 0f)
                {
                    item.Text.gameObject.SetActive(false);
                    continue;
                }
                item.Text.gameObject.SetActive(true);
                rt.position = screen;
                var c = item.Text.color;
                c.a = 1f - u * u;
                item.Text.color = c;
            }
        }
    }
}
