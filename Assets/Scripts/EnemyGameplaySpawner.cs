using UnityEngine;

public class EnemyGameplaySpawner : MonoBehaviour
{
    public enum DifficultyMode { Random, RoundRobin }

    [Header("Refs")]
    public Transform player;           
    public GameObject enemyPrefab;     
    public Transform[] spawnPoints;    
    public int aliveEnemiesTarget = 1; // ✅ 1 enemy at a time

    [Header("Debug")]
    public bool logSpawning = true;
    public float killY = -15f; // αν πέσει κάτω από αυτό, θεωρείται out-of-bounds

    [Header("Difficulty")]
    public string difficultyKey = "difficulty";
    [Range(0f, 1f)] public float currentDifficulty01 = 0.5f;
    public DifficultyMode difficultyMode = DifficultyMode.RoundRobin;
    public float[] difficultyLevels = new float[] { 0f, 0.25f, 0.5f, 0.75f, 1f };

    [Header("Spawn")]
    public float randomSpawnRadius = 1.5f;

    int rrIndex = 0;
    GameObject currentEnemy;
    Health currentEnemyHealth;

    void Start()
    {
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }

        if (PlayerPrefs.HasKey(difficultyKey))
            currentDifficulty01 = Mathf.Clamp01(PlayerPrefs.GetFloat(difficultyKey, currentDifficulty01));

        TrySpawnIfNeeded();
    }

    void Update()
    {
        // Αν ο enemy πέθανε/καταστράφηκε, φέρε επόμενο
        if (currentEnemy == null)
        {
            TrySpawnIfNeeded();
            return;
        }

        // Out of bounds safety (αν κάτι πάει στραβά στη σκηνή/physics)
        if (currentEnemy.transform.position.y < killY)
        {
            if (logSpawning) Debug.Log($"[EnemyGameplaySpawner] Enemy fell out of bounds (y={currentEnemy.transform.position.y:F2}). Respawn.");
            CleanupCurrentEnemy();
            TrySpawnIfNeeded();
        }
    }

    void TrySpawnIfNeeded()
    {
        if (enemyPrefab == null) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;
        if (currentEnemy != null) return;

        SpawnOne();
    }

    void SpawnOne()
    {
        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector3 pos = sp.position + RandomXZ(randomSpawnRadius);

        currentEnemy = Instantiate(enemyPrefab, pos, sp.rotation);

        if (logSpawning) Debug.Log($"[EnemyGameplaySpawner] Spawned '{currentEnemy.name}' at {pos} (sp={sp.name})");

        // Προσοχή: να έχει Tag = Enemy στο prefab
        // (αλλιώς μετά θα μπερδευτείς και σε άλλα checks)

        var agent = currentEnemy.GetComponent<EnemyAgent>();
        if (agent != null)
        {
            agent.trainingMode = false;

            if (player != null) agent.player = player;

            float d01 = SampleDifficulty01();
            currentDifficulty01 = d01;

            PlayerPrefs.SetFloat(difficultyKey, d01);
            agent.SetDifficulty(d01);
        }

        currentEnemyHealth = currentEnemy.GetComponent<Health>();
        if (currentEnemyHealth != null)
        {
            currentEnemyHealth.ResetHP();
            currentEnemyHealth.Died -= OnEnemyDied;
            currentEnemyHealth.Died += OnEnemyDied;
        }
    }

    void OnEnemyDied(Health h)
    {
        if (h == null) return;
        if (logSpawning) Debug.Log($"[EnemyGameplaySpawner] Enemy died ('{h.gameObject.name}'). Next spawn.");
        CleanupCurrentEnemy();
        TrySpawnIfNeeded();
    }

    void CleanupCurrentEnemy()
    {
        if (currentEnemyHealth != null)
            currentEnemyHealth.Died -= OnEnemyDied;

        if (currentEnemy != null)
            Destroy(currentEnemy);

        currentEnemy = null;
        currentEnemyHealth = null;
    }

    float SampleDifficulty01()
    {
        if (difficultyLevels == null || difficultyLevels.Length == 0)
            return currentDifficulty01;

        if (difficultyMode == DifficultyMode.Random)
            return Mathf.Clamp01(difficultyLevels[Random.Range(0, difficultyLevels.Length)]);

        float v = Mathf.Clamp01(difficultyLevels[rrIndex % difficultyLevels.Length]);
        rrIndex++;
        return v;
    }

    static Vector3 RandomXZ(float r)
    {
        if (r <= 0f) return Vector3.zero;
        Vector2 v = Random.insideUnitCircle * r;
        return new Vector3(v.x, 0f, v.y);
    }
}
