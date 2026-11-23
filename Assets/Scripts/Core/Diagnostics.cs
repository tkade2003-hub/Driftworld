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

        // A material that cannot throw: the Standard shader when the
        // player has it, the built in default material when it does not.
        public static Material SafeMaterial(Color color)
        {
            Shader shader = Shader.Find("Standard");
            Material m;
            if (shader != null)
            {
                m = new Material(shader);
            }
            else
            {
                SetNote("Standard shader missing in player, fallback material in use");
                var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                m = new Material(probe.GetComponent<Renderer>().sharedMaterial);
                UnityEngine.Object.Destroy(probe);
            }
            m.color = color;
            return m;
        }
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
