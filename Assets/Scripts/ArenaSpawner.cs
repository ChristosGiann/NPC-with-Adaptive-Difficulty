using UnityEngine;

public class ArenaSpawner : MonoBehaviour
{
    public GameObject dummyPlayerPrefab;
    public GameObject enemyPrefab;
    public GameObject bulletPrefab;
    public Transform[] arenas;

    private void Start() => SpawnAll();

    [ContextMenu("Spawn All Now")]
    public void SpawnAll()
    {
        foreach (var arena in arenas)
        {
            if (arena == null) continue;

            // βρίσκουμε spawns (αν δεν υπάρχουν, δημιουργούμε offsets)
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

                // σημαντικό: για reward check με tag
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

            // Στρέψε τους προς τα μέσα (να μη φαίνεται ότι "πάνε δεξιά" λόγω rotation spawns)
            FaceFlat(playerT, enemyT.position);
            FaceFlat(enemyT, playerT.position);

            // WIRING (τρέχει πάντα, είτε ήταν ήδη spawned είτε μόλις έγινε)
            var agent = enemyT.GetComponent<EnemyAgent>();
            if (agent != null)
            {
                agent.player = playerT;

                // refs για episode reset
                agent.playerSpawn = playerSpawn;
                agent.enemySpawn  = enemySpawn;
                agent.playerRb = playerT.GetComponent<Rigidbody>();
                agent.playerHealth = playerT.GetComponent<Health>();
                agent.enemyHealth  = enemyT.GetComponent<Health>();

                // muzzle
                Transform muzzleT = enemyT.Find("Muzzle");
                if (muzzleT == null)
                {
                    // προσπάθησε να το βρεις βαθύτερα
                    foreach (var t in enemyT.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == "Muzzle") { muzzleT = t; break; }
                    }
                }

                if (muzzleT == null)
                    Debug.LogWarning($"Enemy in '{arena.name}' has no child 'Muzzle'.");

                agent.muzzle = muzzleT;
                agent.bulletPrefab = bulletPrefab;
            }

            var shooter = playerT.GetComponent<DummyPlayerHitscanShooter>();
            if (shooter != null)
            {
                shooter.target = enemyT;
                shooter.ResetWarmup();
            }
        }
    }

    private static void FaceFlat(Transform t, Vector3 worldTarget)
    {
        if (t == null) return;
        Vector3 look = worldTarget - t.position;
        look.y = 0f;
        if (look.sqrMagnitude < 0.0001f) return;
        t.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
    }
}
