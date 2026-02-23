using UnityEngine;

/// <summary>
/// Collects player and enemy metrics per wave and stores the last 2 for UI + difficulty decisions.
/// Attach this to the same object as EnemyGameplaySpawner.
/// </summary>
public class WaveMetricsCollector : MonoBehaviour
{
    [Header("Refs")]
    public Health playerHealth;
    public GunRaycast playerGun;

    [Header("Runtime")]
    public PlayerWaveMetrics prevPlayer = new PlayerWaveMetrics { waveIndex = -1 };
    public PlayerWaveMetrics lastPlayer = new PlayerWaveMetrics { waveIndex = -1 };
    public EnemyWaveTelemetry prevEnemy = new EnemyWaveTelemetry { waveIndex = -1 };
    public EnemyWaveTelemetry lastEnemy = new EnemyWaveTelemetry { waveIndex = -1 };

    float waveStartTime;
    float damageTaken;
    int shotsFired;
    int shotsHit;

    EnemyAgent currentEnemy;
    int waveIndex;

    void OnEnable()
    {
        if (playerHealth != null) playerHealth.Damaged += OnPlayerDamaged;
        if (playerGun != null) playerGun.ShotResolved += OnPlayerShotResolved;
    }

    void OnDisable()
    {
        if (playerHealth != null) playerHealth.Damaged -= OnPlayerDamaged;
        if (playerGun != null) playerGun.ShotResolved -= OnPlayerShotResolved;
    }

    void OnPlayerDamaged(Health h, float dmg)
    {
        damageTaken += Mathf.Max(0f, dmg);
    }

    void OnPlayerShotResolved(bool hit)
    {
        shotsFired++;
        if (hit) shotsHit++;
    }

    public void BeginWave(int newWaveIndex, EnemyAgent enemy)
    {
        waveIndex = newWaveIndex;
        waveStartTime = Time.time;
        damageTaken = 0f;
        shotsFired = 0;
        shotsHit = 0;

        currentEnemy = enemy;
        if (currentEnemy != null)
            currentEnemy.TelemetryResetForWave(Time.time);
    }

    public void EndWaveAndStore()
    {
        float dur = Mathf.Max(0.01f, Time.time - waveStartTime);

        // shift history (player)
        prevPlayer = lastPlayer;
        lastPlayer = new PlayerWaveMetrics
        {
            waveIndex = waveIndex,
            waveDurationSec = dur,
            damageTaken = damageTaken,
            shotsFired = shotsFired,
            shotsHit = shotsHit,
            accuracy01 = shotsFired > 0 ? (float)shotsHit / shotsFired : 0f,
            perf01 = 0f // filled by DifficultyController
        };

        // shift history (enemy)
        prevEnemy = lastEnemy;
        lastEnemy = (currentEnemy != null) ? currentEnemy.TelemetrySnapshot(waveIndex) : new EnemyWaveTelemetry { waveIndex = waveIndex };

        currentEnemy = null;
    }

    public bool HasTwoPlayerWaves()
    {
        return prevPlayer.waveIndex >= 0 && lastPlayer.waveIndex >= 0;
    }

    public bool HasTwoEnemyWaves()
    {
        return prevEnemy.waveIndex >= 0 && lastEnemy.waveIndex >= 0;
    }
}
