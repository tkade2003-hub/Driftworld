using System.Collections.Generic;
using UnityEngine;

namespace Driftworld
{
    // MECH-002: a wind lane is an authored volume between islands. Inside it a
    // gliding hero gains lift and speed along the lane direction. Containment
    // is a manual box test so the behavior is deterministic and testable,
    // with no dependence on physics trigger quirks.
    public class WindLane : MonoBehaviour
    {
        // Local box size: x is width, y is height, z is length along forward.
        public Vector3 size = new Vector3(14f, 12f, 44f);

        private static readonly List<WindLane> All = new List<WindLane>();

        private void OnEnable() { All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        public bool Contains(Vector3 worldPos)
        {
            Vector3 local = transform.InverseTransformPoint(worldPos);
            Vector3 half = size * 0.5f;
            return Mathf.Abs(local.x) <= half.x
                && Mathf.Abs(local.y) <= half.y
                && Mathf.Abs(local.z) <= half.z;
        }

        public static WindLane LaneContaining(Vector3 worldPos)
        {
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i].Contains(worldPos)) return All[i];
            }
            return null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.25f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, size);
        }
    }
}
