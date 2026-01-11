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

            var playerSpawn = arena.Find("PlayerSpawn");
            var enemySpawn  = arena.Find("EnemySpawn");

            if (playerSpawn == null || enemySpawn == null)
            {
                Debug.LogWarning($"Arena '{arena.name}' missing PlayerSpawn or EnemySpawn");
                continue;
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

            // WIRING (τρέχει πάντα, είτε spawned τώρα είτε υπήρχε ήδη)
            var agent = enemyT.GetComponent<EnemyAgent>();
            if (agent != null)
            {
                agent.player = playerT;

                // Muzzle: αν δεν υπάρχει, προειδοποίηση (για να το φτιάξεις στο prefab)
                var muzzleT = enemyT.Find("Muzzle");
                if (muzzleT == null)
                    Debug.LogWarning($"Enemy in '{arena.name}' has no child 'Muzzle'.");

                agent.muzzle = muzzleT;
                agent.bulletPrefab = bulletPrefab;
            }

            var shooter = playerT.GetComponent<DummyPlayerHitscanShooter>();
            if (shooter != null)
            {
                shooter.target = enemyT;
            }
        }
    }
}
