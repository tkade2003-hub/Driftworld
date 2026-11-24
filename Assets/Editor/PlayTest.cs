using UnityEditor;
using UnityEngine;

namespace Driftworld.EditorTools
{
    // Headless play test: enters play mode, lets the greybox run untouched
    // for a few seconds, and logs the hero state once per second so a batch
    // run shows whether the hero grounds, falls, or glides with no input.
    // Editor only; never ships in a player build.
    //
    // Harness note (learned 2026-11-23): entering play mode reloads the
    // domain, which wipes static fields and event subscriptions. The first
    // version subscribed inside Run() and stamped plain statics, so after
    // the reload nothing ever fired again: the editor sat in play mode
    // forever and logged zero PLAYTEST lines. All state below therefore
    // lives in SessionState (which survives reloads inside one editor
    // session), and the subscriptions are made from an [InitializeOnLoad]
    // static constructor so they are renewed after every reload.
    [InitializeOnLoad]
    public static class PlayTest
    {
        private const string KeyArmed = "Driftworld.PlayTest.Armed";
        private const string KeyRunAt = "Driftworld.PlayTest.RunAt";
        private const string KeyStart = "Driftworld.PlayTest.Start";
        private const string KeyLast = "Driftworld.PlayTest.Last";
        private const string KeyProbed = "Driftworld.PlayTest.Probed";
        private const string KeySawPlay = "Driftworld.PlayTest.SawPlay";

        static PlayTest()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnState;
        }

        public static void Run()
        {
            SessionState.SetBool(KeyArmed, true);
            SessionState.SetBool(KeySawPlay, false);
            SessionState.SetBool(KeyProbed, false);
            SessionState.SetFloat(KeyStart, 0f);
            SessionState.SetFloat(KeyRunAt, (float)EditorApplication.timeSinceStartup);
            EditorApplication.isPlaying = true;
        }

        private static void OnState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode
                && SessionState.GetBool(KeyArmed, false)
                && SessionState.GetBool(KeySawPlay, false))
            {
                SessionState.SetBool(KeyArmed, false);
                EditorApplication.Exit(0);
            }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(KeyArmed, false)) return;
            double now = EditorApplication.timeSinceStartup;

            // Failsafe: never sit in play mode forever again.
            double runAt = SessionState.GetFloat(KeyRunAt, 0f);
            if (runAt > 0 && now - runAt > 90.0)
            {
                Debug.Log("PLAYTEST FAILSAFE: 90 s without finishing, exiting");
                SessionState.SetBool(KeyArmed, false);
                EditorApplication.isPlaying = false;
                EditorApplication.Exit(1);
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                if (SessionState.GetBool(KeySawPlay, false))
                {
                    SessionState.SetBool(KeyArmed, false);
                    EditorApplication.Exit(0);
                }
                return;
            }

            SessionState.SetBool(KeySawPlay, true);
            double start = SessionState.GetFloat(KeyStart, 0f);
            if (start <= 0)
            {
                start = now;
                SessionState.SetFloat(KeyStart, (float)now);
                SessionState.SetFloat(KeyLast, (float)now);
            }

            double last = SessionState.GetFloat(KeyLast, 0f);
            if (now - last >= 1.0)
            {
                SessionState.SetFloat(KeyLast, (float)now);
                var hero = Object.FindAnyObjectByType<PlayerController>();
                if (hero != null)
                {
                    Debug.Log("PLAYTEST t=" + (now - start).ToString("F1")
                        + " pos=" + hero.transform.position.ToString("F2")
                        + " | " + Diag.Status);
                    if (!SessionState.GetBool(KeyProbed, false))
                    {
                        SessionState.SetBool(KeyProbed, true);
                        Probe(hero);
                    }
                }
                else
                {
                    Debug.Log("PLAYTEST t=" + (now - start).ToString("F1") + " no hero yet, step=" + Diag.Step);
                }
            }

            if (now - start >= 6.0)
            {
                EditorApplication.isPlaying = false;
            }
        }

        // One shot physics dump, taken on the first logged second: does the
        // island collider exist, and can plain physics queries see it in the
        // spawn column? This separates "collider missing" from "the
        // CharacterController sweep is not colliding" in a single run.
        private static void Probe(PlayerController hero)
        {
            var island = GameObject.Find("IslandA");
            string islandInfo = "IslandA MISSING";
            if (island != null)
            {
                var col = island.GetComponent<BoxCollider>();
                islandInfo = "IslandA pos=" + island.transform.position.ToString("F2")
                    + " layer=" + island.layer
                    + " active=" + island.activeInHierarchy
                    + " collider=" + (col == null
                        ? "NONE"
                        : "enabled=" + col.enabled + " trigger=" + col.isTrigger
                            + " bounds=" + col.bounds.ToString("F2"));
            }

            var body = hero.GetComponent<CharacterController>();
            string heroInfo = "hero layer=" + hero.gameObject.layer
                + " ccEnabled=" + body.enabled
                + " grounded=" + body.isGrounded
                + " height=" + body.height.ToString("F2")
                + " radius=" + body.radius.ToString("F2")
                + " center=" + body.center.ToString("F2")
                + " skin=" + body.skinWidth.ToString("F3");

            RaycastHit hit;
            string spawnRay = Physics.Raycast(new Vector3(8f, 2f, 0f), Vector3.down, out hit, 10f)
                ? "spawnRay hit " + hit.collider.gameObject.name + " dist " + hit.distance.ToString("F2")
                : "spawnRay hit NOTHING";
            string overlap = "spawnOverlap=" + Physics.CheckSphere(new Vector3(8f, 0.2f, 0f), 0.4f);
            string matrix = "ignoreCollision(0,0)=" + Physics.GetIgnoreLayerCollision(0, 0);

            Debug.Log("PLAYTEST PROBE " + islandInfo + " || " + heroInfo + " || "
                + spawnRay + " || " + overlap + " || " + matrix);
        }
    }
}
