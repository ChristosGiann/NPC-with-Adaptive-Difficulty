using System.Reflection;
using UnityEngine;

public class ArenaSpawner : MonoBehaviour
{
    public GameObject dummyPlayerPrefab;
    public GameObject enemyPrefab;
    public GameObject bulletPrefab;
    public Transform[] arenas;

    [Header("Difficulty (5 levels) - round robin across ALL arenas")]
    public float[] difficultyLevels = new float[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
    private int rrIndex = 0;

    private void Start() => SpawnAll();

    [ContextMenu("Spawn All Now")]
    public void SpawnAll()
    {
        foreach (var arena in arenas)
        {
            if (arena == null) continue;

            // find / create spawns
            Transform playerSpawn = arena.Find("PlayerSpawn");
            Transform enemySpawn  = arena.Find("EnemySpawn");

            if (playerSpawn == null)
            {
                var go = new GameObject("PlayerSpawn");
                go.transform.SetParent(arena, false);
                go.transform.localPosition = new Vector3(-3f, 0f, 0f);
                go.transform.localRotation = Quaternion.identity;
                playerSpawn = go.transform;
            }

            if (enemySpawn == null)
            {
                var go = new GameObject("EnemySpawn");
                go.transform.SetParent(arena, false);
                go.transform.localPosition = new Vector3(3f, 0f, 0f);
                go.transform.localRotation = Quaternion.identity;
                enemySpawn = go.transform;
            }

            // PLAYER (spawn first)
            Transform playerT = arena.Find("Player");
            if (playerT == null)
            {
                var pObj = Instantiate(dummyPlayerPrefab, playerSpawn.position, playerSpawn.rotation, arena);
                pObj.name = "Player";

                if (!pObj.CompareTag("Player")) pObj.tag = "Player";

                playerT = pObj.transform;
            }

            // ENEMY
            Transform enemyT = arena.Find("Enemy");
            if (enemyT == null)
            {
                var eObj = Instantiate(enemyPrefab, enemySpawn.position, enemySpawn.rotation, arena);
                eObj.name = "Enemy";
                enemyT = eObj.transform;
            }

            // face each other
            FaceFlat(playerT, enemyT.position);
            FaceFlat(enemyT, playerT.position);

            // -------- WIRING (safe / version tolerant) --------
            var agent = enemyT.GetComponent<EnemyAgent>();
            if (agent != null)
            {
                // Try set core refs (works whether fields are public or private serialised)
                TrySetFieldOrProperty(agent, "player", playerT);

                // optional training refs (only if your EnemyAgent still has them)
                TrySetFieldOrProperty(agent, "playerSpawn", playerSpawn);
                TrySetFieldOrProperty(agent, "enemySpawn",  enemySpawn);
                TrySetFieldOrProperty(agent, "playerRb",    playerT.GetComponent<Rigidbody>());
                TrySetFieldOrProperty(agent, "playerHealth",playerT.GetComponent<Health>());
                TrySetFieldOrProperty(agent, "enemyHealth", enemyT.GetComponent<Health>());

                // muzzle
                Transform muzzleT = enemyT.Find("Muzzle");
                if (muzzleT == null)
                {
                    foreach (var t in enemyT.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == "Muzzle") { muzzleT = t; break; }
                    }
                }

                if (muzzleT == null)
                    Debug.LogWarning($"Enemy in '{arena.name}' has no child 'Muzzle'.");

                TrySetFieldOrProperty(agent, "muzzle", muzzleT);
                TrySetFieldOrProperty(agent, "bulletPrefab", bulletPrefab);

                // Mark trainingMode if your agent supports it
                TrySetFieldOrProperty(agent, "trainingMode", true);

                // Provide spawner reference if your agent supports it (common names)
                TrySetFieldOrProperty(agent, "arenaSpawner", this);
                TryCallMethod(agent, "ConfigureDifficultyProvider", this);

                // Set initial difficulty (and store in prefs key that your gameplay uses too)
                float d01 = GetNextDifficulty();
                PlayerPrefs.SetFloat("difficulty", d01);

                TryCallMethod(agent, "SetDifficulty", d01);
            }

            // player shooter (dummy training shooter)
            var shooter = playerT.GetComponent<DummyPlayerHitscanShooter>();
            if (shooter != null)
            {
                TrySetFieldOrProperty(shooter, "target", enemyT);
                TryCallMethod(shooter, "ResetWarmup");
            }
        }
    }

    // Called by EnemyAgent (if you call it) or used here
    public float GetNextDifficulty()
    {
        if (difficultyLevels == null || difficultyLevels.Length == 0) return 0.5f;
        float d = difficultyLevels[rrIndex % difficultyLevels.Length];
        rrIndex++;
        return d;
    }

    private static void FaceFlat(Transform t, Vector3 worldTarget)
    {
        if (t == null) return;
        Vector3 look = worldTarget - t.position;
        look.y = 0f;
        if (look.sqrMagnitude < 0.0001f) return;
        t.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
    }

    // ---------- Reflection helpers (no compile-time dependency on exact agent API) ----------

    static void TrySetFieldOrProperty(object obj, string name, object value)
    {
        if (obj == null) return;

        var type = obj.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // field
        var f = type.GetField(name, flags);
        if (f != null)
        {
            if (value == null || f.FieldType.IsInstanceOfType(value))
                f.SetValue(obj, value);
            return;
        }

        // property
        var p = type.GetProperty(name, flags);
        if (p != null && p.CanWrite)
        {
            if (value == null || p.PropertyType.IsInstanceOfType(value))
                p.SetValue(obj, value);
        }
    }

    static void TryCallMethod(object obj, string methodName, params object[] args)
    {
        if (obj == null) return;

        var type = obj.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (var m in type.GetMethods(flags))
        {
            if (m.Name != methodName) continue;

            var ps = m.GetParameters();
            if (ps.Length != (args?.Length ?? 0)) continue;

            bool ok = true;
            for (int i = 0; i < ps.Length; i++)
            {
                if (args[i] == null) continue;
                if (!ps[i].ParameterType.IsInstanceOfType(args[i])) { ok = false; break; }
            }

            if (!ok) continue;

            m.Invoke(obj, args);
            return;
        }
    }
}
