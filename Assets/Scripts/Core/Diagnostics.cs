using System;
using UnityEngine;

namespace Driftworld
{
    // Build diagnostics for the greybox. The overlay is created before the
    // first scene loads and draws with IMGUI only, so it survives failures
    // that would take down uGUI, fonts, or materials. Bootstrap reports
    // each step here as it completes; if a step throws, the step it stopped
    // on and the error stay on screen where any screenshot can read them.
    // This file comes out once the loop is proven on a real device.
    public static class Diag
    {
        public static string Step = "boot";
        public static string Error = "";
        public static string Note = "";
        public static string Status = "";
        public static string CamStatus = "";

        public static void SetStep(string step)
        {
            Step = step;
        }

        public static void SetNote(string note)
        {
            Note = note;
        }

        public static void Fail(Exception ex)
        {
            Error = ex.GetType().Name + ": " + ex.Message;
        }

        // A material that cannot throw and cannot render in the magenta
        // error color: a clone of the shipped Resources material, whose
        // presence in the project forces the Standard shader to compile
        // into the build. The old paths (a bare Shader.Find, or cloning
        // the primitive default material) both produced magenta in the
        // WebGL player, because nothing in any scene used the shader.
        public static Material SafeMaterial(Color color)
        {
            if (template == null)
            {
                template = Resources.Load<Material>("Greybox/White");
            }
            if (template != null)
            {
                var clone = new Material(template);
                clone.color = color;
                return clone;
            }
            SetNote("Greybox template material missing, last resort fallback in use");
            Shader shader = Shader.Find("Standard");
            if (shader != null)
            {
                var m = new Material(shader);
                m.color = color;
                return m;
            }
            var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var last = new Material(probe.GetComponent<Renderer>().sharedMaterial);
            UnityEngine.Object.Destroy(probe);
            last.color = color;
            return last;
        }

        private static Material template;
    }

    public class DiagOverlay : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            var go = new GameObject("DiagOverlay");
            go.AddComponent<DiagOverlay>();
            DontDestroyOnLoad(go);
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(12f, 10f, 1000f, 26f), "DRIFTWORLD DIAG  step: " + Diag.Step);
            if (Diag.Note.Length > 0)
            {
                GUI.Label(new Rect(12f, 38f, 1000f, 26f), Diag.Note);
            }
            if (Diag.Error.Length > 0)
            {
                GUI.Label(new Rect(12f, 66f, 1000f, 52f), "ERROR: " + Diag.Error);
            }
        }
    }
}
