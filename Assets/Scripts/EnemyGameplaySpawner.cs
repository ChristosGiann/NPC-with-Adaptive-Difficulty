using System.Collections;
using UnityEngine;

public class EnemyGameplaySpawner : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;

    [Header("Wave")]
    public int aliveEnemiesTarget = 1;            // keep 1 enemy at a time
    public int waveIndex = 0;
    public float nextWaveCountdownSec = 3f;

    [Header("Countdown UI")]
    public HUDWaveUI hud;                         // assign in scene
    public string countdownPrefix = "Next wave in:";
    public string adjustingText = "Adjusting difficulty...";

    [Header("Difficulty")]
    public string difficultyKey = "difficulty";
    [Range(0f, 1f)] public float currentDifficulty01 = 0.5f;
    [Range(0f, 1f)] public float nextTargetDifficulty01 = 0.5f;
    public bool adjustDifficultyEvery2Waves = true;

    [Header("Knobs (scaled by difficulty)")]
    public bool scaleKnobsByDifficulty = true;
    public float aimErrorEasy = 7f;
    public float aimErrorHard = 5f;
    public float cooldownEasy = 0.45f;
    public float cooldownHard = 0.30f;
    public float moveSpeedEasy = 11f;
    public float moveSpeedHard = 13f;

    [Header("Debug")]
    public bool logMetrics = true;
    public bool logSpawning = true;
    public float killY = -15f;
    public float randomSpawnRadius = 1.5f;

    [Header("Controllers")]
    public DifficultyController difficultyController;
    public WaveMetricsCollector metrics;

    GameObject currentEnemy;
    Health currentEnemyHealth;
    EnemyAgent currentEnemyAgent;
    Coroutine waveRoutine;

    // UI state
    public bool IsInCountdown { get; private set; }
    public float CountdownRemaining { get; private set; }
    public bool IsAdjustingWave { get; private set; }

    void Start()
    {
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }

        if (difficultyController != null)
            difficultyController.SetCurrent(currentDifficulty01);

        // first wave spawn
        SpawnWaveEnemy();
    }

    void Update()
    {
        if (currentEnemy != null && currentEnemy.transform.position.y < killY)
        {
            Debug.Log($"[KILL Y] Enemy y={currentEnemy.transform.position.y:F2} < {killY}. Cleanup.");

            if (logSpawning) Debug.Log($"[EnemyGameplaySpawner] Enemy fell out of bounds (y={currentEnemy.transform.position.y:F2}). Ending wave.");
            EndWaveAndScheduleNext();
        }
    }

    void SpawnWaveEnemy()
    {
        if (enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0) return;
        if (currentEnemy != null) return;
        if (aliveEnemiesTarget <= 0) aliveEnemiesTarget = 1;

        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector3 pos = sp.position + RandomXZ(randomSpawnRadius);

        currentEnemy = Instantiate(enemyPrefab, pos, sp.rotation);

        currentEnemyHealth = currentEnemy.GetComponentInChildren<Health>();
        if (currentEnemyHealth != null)
            currentEnemyHealth.Died += OnEnemyDied;

        currentEnemyAgent = currentEnemy.GetComponent<EnemyAgent>();
        if (currentEnemyAgent != null)
        {
            currentEnemyAgent.trainingMode = false;
            if (player != null) currentEnemyAgent.player = player;

            // Apply difficulty + knobs
            ApplyDifficultyToEnemy(currentEnemyAgent, currentDifficulty01);
            if (logSpawning && currentEnemyAgent != null)
            {
                Debug.Log(
                    $"[SPAWN APPLY] wave={waveIndex} diff={currentDifficulty01:0.00} " +
                    $"aimErr={currentEnemyAgent.aimErrorDeg:0.00} cd={currentEnemyAgent.shootCooldown:0.00} " +
                    $"move={currentEnemyAgent.moveSpeed:0.00}"
                );
            }

        }

        // Start collecting wave metrics
        if (metrics != null)
            metrics.BeginWave(waveIndex, currentEnemyAgent);

        if (logSpawning)
            Debug.Log($"[EnemyGameplaySpawner] Wave {waveIndex}: Spawned '{currentEnemy.name}' diff={currentDifficulty01:F2} @ {pos}");
    }

    void OnEnemyDied(Health h)
    {
        EndWaveAndScheduleNext();
    }

    void EndWaveAndScheduleNext()
    {
        CleanupCurrentEnemy();

        // store metrics for the wave that just ended
        if (metrics != null)
            metrics.EndWaveAndStore();

        if (difficultyController != null && metrics != null)
        {
            metrics.lastPlayer.perf01 = difficultyController.GetPerf01(metrics.lastPlayer);
            if (metrics.HasTwoPlayerWaves())
                metrics.prevPlayer.perf01 = difficultyController.GetPerf01(metrics.prevPlayer);
        }

        // LOG: wave summary
        if (logMetrics && metrics != null)
        {
            var p = metrics.lastPlayer;
            var e = metrics.lastEnemy;

            Debug.Log(
                $"[WAVE END] wave={p.waveIndex} " +
                $"damage={p.damageTaken:0.0} " +
                $"time={p.waveDurationSec:0.00}s " +
                $"shots={p.shotsFired} hits={p.shotsHit} acc={(p.accuracy01 * 100f):0}% " +
                $"perf={p.perf01:0.00} | " +
                $"enemy: strafe={(e.strafePercent01 * 100f):0}% avgDist={e.avgDistance:0.00}m " +
                $"firstHit={(e.timeToFirstHitSec < 0 ? "-" : (e.timeToFirstHitSec.ToString("0.00") + "s"))} " +
                $"shotsF/R={e.shotsFired}/{e.shotsRequested}"
            );
        }

        // schedule next wave countdown
        if (waveRoutine != null) StopCoroutine(waveRoutine);
        waveRoutine = StartCoroutine(NextWaveRoutine());
    }

    IEnumerator NextWaveRoutine()
    {
        // Decide if this transition will adjust difficulty (every 2 waves)
        bool willAdjust =
            adjustDifficultyEvery2Waves &&
            ((waveIndex + 1) % 2 == 0) &&
            difficultyController != null &&
            metrics != null &&
            metrics.HasTwoPlayerWaves();

        IsAdjustingWave = willAdjust;

        // Compute next target diff early and show it live during countdown
        if (willAdjust)
        {
            float perfPrev, perfLast, perf2;
            difficultyController.ComputeTargetFromLast2(
                metrics.prevPlayer, metrics.lastPlayer,
                out perfPrev, out perfLast, out perf2
            );

            // stamp perf back into stored metrics so UI can show it
            metrics.prevPlayer.perf01 = perfPrev;
            metrics.lastPlayer.perf01 = perfLast;

            nextTargetDifficulty01 = difficultyController.TargetDifficulty01;

            if (logMetrics)
            {
                Debug.Log(
                    $"[DIFF ADJUST] transitionToWave={waveIndex + 1} " +
                    $"prev(w={metrics.prevPlayer.waveIndex}, dmg={metrics.prevPlayer.damageTaken:0.0}, t={metrics.prevPlayer.waveDurationSec:0.00}, acc={(metrics.prevPlayer.accuracy01 * 100f):0}%, perf={perfPrev:0.00}) " +
                    $"last(w={metrics.lastPlayer.waveIndex}, dmg={metrics.lastPlayer.damageTaken:0.0}, t={metrics.lastPlayer.waveDurationSec:0.00}, acc={(metrics.lastPlayer.accuracy01 * 100f):0}%, perf={perfLast:0.00}) " +
                    $"perf2={perf2:0.00} -> nextTargetDiff={nextTargetDifficulty01:0.00}"
                );
            }
        }
        else
        {
            nextTargetDifficulty01 = currentDifficulty01;
            if (difficultyController != null) difficultyController.SetCurrent(currentDifficulty01);
        }

        if (logMetrics)
        {
            Debug.Log(
                $"[DIFF TARGET] willAdjust={willAdjust} waveNext={waveIndex+1} " +
                $"current={currentDifficulty01:0.00} target={nextTargetDifficulty01:0.00}"
            );
        }



        // Refresh HUD panels (show last 2 waves + next target)
        RefreshHUDPanels();

        // Countdown
        IsInCountdown = true;
        CountdownRemaining = nextWaveCountdownSec;

        if (hud != null)
            hud.SetAdjusting(adjustingText, willAdjust);

        while (CountdownRemaining > 0f)
        {
            if (hud != null)
                hud.SetCountdown($"{countdownPrefix} {Mathf.CeilToInt(CountdownRemaining)}");

            yield return null;
            CountdownRemaining -= Time.deltaTime;
        }

        if (hud != null)
        {
            hud.SetCountdown("");
            hud.SetAdjusting("", false);
        }

        IsInCountdown = false;
        CountdownRemaining = 0f;

        // Apply difficulty change (only on adjust transitions)
        if (willAdjust && difficultyController != null)
        {
            bool changed = difficultyController.ApplyTargetWithSmoothing();
            currentDifficulty01 = difficultyController.CurrentDifficulty01;

            if (logMetrics)
                Debug.Log($"[DIFF APPLY] changed={changed} current={currentDifficulty01:0.00} target={difficultyController.TargetDifficulty01:0.00}");
        }

        // Next wave
        waveIndex++;
        SpawnWaveEnemy();
    }


    void RefreshHUDPanels()
    {
        if (hud == null || metrics == null) return;

        // left: player last2
        hud.RenderLeft(metrics.prevPlayer, metrics.lastPlayer);

        // right: difficulty + enemy last2 + knobs for next
        float aimErr = Mathf.Lerp(aimErrorEasy, aimErrorHard, nextTargetDifficulty01);
        float cd = Mathf.Lerp(cooldownEasy, cooldownHard, nextTargetDifficulty01);
        float ms = Mathf.Lerp(moveSpeedEasy, moveSpeedHard, nextTargetDifficulty01);

        EnemyWaveTelemetry prevE = metrics.prevEnemy;
        EnemyWaveTelemetry lastE = metrics.lastEnemy;

        hud.RenderRight(
            currentDifficulty01,
            nextTargetDifficulty01,
            prevE,
            lastE,
            aimErr,
            cd,
            ms
        );
    }

    void ApplyDifficultyToEnemy(EnemyAgent agent, float d01)
    {
        if (agent == null) return;

        agent.SetDifficulty(d01);

        if (!scaleKnobsByDifficulty) return;

        agent.aimErrorDeg = Mathf.Lerp(aimErrorEasy, aimErrorHard, d01);
        agent.shootCooldown = Mathf.Lerp(cooldownEasy, cooldownHard, d01);
        agent.moveSpeed = Mathf.Lerp(moveSpeedEasy, moveSpeedHard, d01);
    }

    void CleanupCurrentEnemy()
    {
        if (currentEnemyHealth != null)
            currentEnemyHealth.Died -= OnEnemyDied;

        currentEnemyHealth = null;
        currentEnemyAgent = null;

        if (currentEnemy != null)
            Destroy(currentEnemy);

        currentEnemy = null;
    }

    static Vector3 RandomXZ(float r)
    {
        if (r <= 0f) return Vector3.zero;
        Vector2 v = Random.insideUnitCircle * r;
        return new Vector3(v.x, 0f, v.y);
    }
}
