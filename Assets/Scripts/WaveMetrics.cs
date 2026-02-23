using System;

[Serializable]
public class PlayerWaveMetrics
{
    public int waveIndex;
    public float waveDurationSec;

    public float damageTaken;
    public int shotsFired;
    public int shotsHit;
    public float accuracy01;
    public float perf01; // 0..1
}

[Serializable]
public class EnemyWaveTelemetry
{
    public int waveIndex;
    public float strafePercent01;      // 0..1
    public float avgDistance;          // meters
    public float timeToFirstHitSec;    // -1 if never hit
    public int shotsRequested;
    public int shotsFired;
    public float timingQuality01;      // shotsFired/shotsRequested (0..1)
}
