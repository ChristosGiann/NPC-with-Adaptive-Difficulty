using UnityEngine;

/// <summary>
/// Converts the last 2 PlayerWaveMetrics into a target difficulty (0..1),
/// then smooths toward it with deadzone to avoid oscillation.
/// </summary>
public class DifficultyController : MonoBehaviour
{
    [Header("Targets / Normalization")]
    public float targetClearTimeSec = 12f;         // "good" clear time
    public float maxAcceptableDamage = 60f;        // damage per wave after which score -> 0
    [Range(0f, 1f)] public float targetAccuracy01 = 0.35f;

    [Header("Weights (sum doesn't need to be 1)")]
    public float wSurvival = 0.45f;
    public float wSpeed = 0.35f;
    public float wAccuracy = 0.20f;

    [Header("Difficulty Mapping")]
    [Range(0f, 1f)] public float baseDifficulty01 = 0.50f;
    public float gain = 0.65f;                    // how strongly perf shifts difficulty
    public float deadzone = 0.05f;
    [Range(0f, 1f)] public float smoothLerp = 0.20f;

    public float CurrentDifficulty01 { get; private set; } = 0.5f;
    public float TargetDifficulty01 { get; private set; } = 0.5f;

    public void SetCurrent(float d01)
    {
        CurrentDifficulty01 = Mathf.Clamp01(d01);
        TargetDifficulty01 = CurrentDifficulty01;
    }

    public void UpdateTargetFromLast2(PlayerWaveMetrics prev, PlayerWaveMetrics last)
    {
        float p1 = ComputePerf01(prev);
        float p2 = ComputePerf01(last);

        prev.perf01 = p1;
        last.perf01 = p2;

        float perf2 = Mathf.Clamp01((p1 + p2) * 0.5f);

        float target = baseDifficulty01 + (perf2 - 0.5f) * gain;
        TargetDifficulty01 = Mathf.Clamp01(target);
    }

    public float GetPerf01(PlayerWaveMetrics w)
    {
        return ComputePerf01(w);
    }


    // Backward-compatible alias (older spawner builds call this)
    public void ComputeTargetFromLast2(PlayerWaveMetrics prev, PlayerWaveMetrics last)
    {
        UpdateTargetFromLast2(prev, last);
    }

    public bool ApplyTargetWithSmoothing()
    {
        float diff = Mathf.Abs(TargetDifficulty01 - CurrentDifficulty01);
        if (diff < deadzone) return false;

        CurrentDifficulty01 = Mathf.Lerp(CurrentDifficulty01, TargetDifficulty01, smoothLerp);
        CurrentDifficulty01 = Mathf.Clamp01(CurrentDifficulty01);
        return true;
    }

    // Overload used by EnemyGameplaySpawner (returns perf breakdown for UI)
    public void ComputeTargetFromLast2(
        PlayerWaveMetrics prev,
        PlayerWaveMetrics last,
        out float perfPrev,
        out float perfLast,
        out float perf2)
    {
        // Χρησιμοποιούμε την ίδια λογική που ήδη έχεις για να υπολογίζεις perf
        perfPrev = ComputePerf01(prev);
        perfLast = ComputePerf01(last);
        perf2 = (perfPrev + perfLast) * 0.5f;

        // Μετατροπή perf2 -> target difficulty
        float target = ComputeTargetDifficultyFromPerf(perf2);

        // Αν θες, ενημέρωσε το TargetDifficulty01 εδώ (ώστε το HUD να δείχνει live το next target)
        TargetDifficulty01 = target;
    }

    // --- helpers ---
    // Αν τα έχεις ήδη με άλλο όνομα, κράτα τα δικά σου και απλά κάνε match τα ονόματα.
    private float ComputePerf01(PlayerWaveMetrics w)
    {
        // ΠΡΟΣΑΡΜΟΣΕ αυτά τα fields στα δικά σου ονόματα αν διαφέρουν:
        // damageTaken, waveDurationSec, accuracy01
        float survivalScore = 1f - Mathf.Clamp01(w.damageTaken / Mathf.Max(1f, maxAcceptableDamage));
        // 0.5 όταν είσαι στο target, >0.5 όταν καλύτερα, <0.5 όταν χειρότερα
        float clearScore = Mathf.Clamp01(0.5f + 0.5f * ((targetClearTimeSec - w.waveDurationSec) / targetClearTimeSec));

        // ίδια λογική για accuracy: 0.5 στο targetAccuracy, 1 στο 100%, 0 στο 0%
        float accuracyScore = Mathf.Clamp01(0.5f + 0.5f * ((w.accuracy01 - targetAccuracy01) / (1f - targetAccuracy01)));


        float perf = 0.45f * survivalScore + 0.35f * clearScore + 0.20f * accuracyScore;
        return Mathf.Clamp01(perf);
    }

    private float ComputeTargetDifficultyFromPerf(float perf2)
    {
        float raw = Mathf.Clamp01(baseDifficulty01 + (perf2 - 0.5f) * gain);

        return raw;
    }


}
