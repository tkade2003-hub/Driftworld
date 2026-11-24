using UnityEngine;

namespace Driftworld
{
    // MECH-009 debug: the weapon hitbox volume, drawn while the hitbox
    // toggle is on. A flat disc marks the active step's reach and an
    // elongated footprint marks the facing arc zone the sweep tests
    // against. Presentation of the authoritative step data only.
    public class WeaponRangeViz : MonoBehaviour
    {
        private PlayerCombat combat;
        private GameObject disc;
        private GameObject wedge;

        private void Awake()
        {
            combat = GetComponent<PlayerCombat>();
            disc = MakeFlat(new Color(1f, 0.2f, 0.6f, 0.18f));
            wedge = MakeFlat(new Color(1f, 0.2f, 0.6f, 0.3f));
        }

        private GameObject MakeFlat(Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(go.GetComponent<Collider>());
            var mat = Diag.SafeMaterial(color);
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 2999;
            go.GetComponent<Renderer>().material = mat;
            go.SetActive(false);
            return go;
        }

        private void Update()
        {
            bool show = CombatDebug.ShowHitboxes && combat != null && combat.Weapon != null;
            disc.SetActive(show);
            wedge.SetActive(show);
            if (!show) return;

            float range = Tuning.Combat.strike_range_m;
            float arc = 100f;
            // During a swing the viz follows the live step; at rest it
            // shows the combo opener so the reach can be judged while
            // approaching.
            var weapon = combat.Weapon;
            if (weapon.Combo.Count > 0)
            {
                range = weapon.Combo[0].Range;
                arc = weapon.Combo[0].ArcDeg;
            }

            Vector3 pos = transform.position + Vector3.up * 0.06f;
            disc.transform.position = pos;
            disc.transform.localScale = new Vector3(range * 2f, 0.04f, range * 2f);

            float wedgeWidth = 2f * range * Mathf.Tan(Mathf.Deg2Rad * arc * 0.5f);
            wedge.transform.position = pos + combat.GetComponent<PlayerController>().HeadingForward * (range * 0.5f);
            wedge.transform.localScale = new Vector3(wedgeWidth, 0.05f, range);
            wedge.transform.rotation = Quaternion.LookRotation(combat.GetComponent<PlayerController>().HeadingForward);
        }
    }
}
