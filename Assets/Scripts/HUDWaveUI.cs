using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class HUDWaveUI : MonoBehaviour
{
    [Header("UI Text Refs")]
    public Text countdownText;
    public Text adjustingText;
    public Text leftPanelText;
    public Text rightPanelText;

    private readonly StringBuilder _sb = new StringBuilder(1024);

    public void SetCountdown(string text)
    {
        if (countdownText != null) countdownText.text = text;
    }

    public void SetAdjusting(string text, bool visible)
    {
        if (adjustingText == null) return;
        adjustingText.text = visible ? text : "";
    }

    // Left panel: Player metrics for last 2 waves
    public void RenderLeft(PlayerWaveMetrics prev, PlayerWaveMetrics last)
    {
        if (leftPanelText == null) return;

        _sb.Length = 0;
        _sb.AppendLine("PLAYER (last 2 waves)");
        _sb.AppendLine("--------------------");

        _sb.Append("Damage Taken: ");
        _sb.Append(Format2(last.damageTaken, prev.damageTaken));
        _sb.AppendLine();

        _sb.Append("Clear Time:   ");
        _sb.Append(Format2(last.waveDurationSec, prev.waveDurationSec, "s"));
        _sb.AppendLine();

        _sb.Append("Accuracy:     ");
        _sb.Append(Format2Percent(last.accuracy01, prev.accuracy01));
        _sb.AppendLine();

        _sb.Append("Perf score:   ");
        _sb.Append(Format2(last.perf01, prev.perf01));
        _sb.AppendLine();

        _sb.Append("Perf2 avg:    ");
        _sb.Append(((last.perf01 + prev.perf01) * 0.5f).ToString("0.00"));
        _sb.AppendLine();

        leftPanelText.text = _sb.ToString();
    }

    private static string Tier5(float d01)
    {
        if (d01 < 0.20f) return "Very Easy";
        if (d01 < 0.40f) return "Easy";
        if (d01 < 0.60f) return "Medium";
        if (d01 < 0.80f) return "Hard";
        return "Very Hard";
    }

    // Right panel: Difficulty + Enemy telemetry + Knobs
    public void RenderRight(
        float currentDiff01,
        float nextTargetDiff01,
        EnemyWaveTelemetry prevEnemy,
        EnemyWaveTelemetry lastEnemy,
        float aimErrDeg,
        float cooldownSec,
        float moveSpeed)
    {
        if (rightPanelText == null) return;

        _sb.Length = 0;

        _sb.AppendLine("DIFFICULTY");
        _sb.AppendLine("----------");
        _sb.Append("Current:     ");
        _sb.Append(currentDiff01.ToString("0.00"));
        _sb.Append(" (");
        _sb.Append(Tier5(currentDiff01));
        _sb.AppendLine(")");

        _sb.Append("Next target: ");
        _sb.Append(nextTargetDiff01.ToString("0.00"));
        _sb.Append(" (");
        _sb.Append(Tier5(nextTargetDiff01));
        _sb.AppendLine(")");


        float delta = nextTargetDiff01 - currentDiff01;
        _sb.Append("Δ change:     ");
        _sb.Append((delta >= 0 ? "+" : "") + delta.ToString("0.00"));
        _sb.AppendLine();


        _sb.AppendLine();
        _sb.AppendLine("ENEMY TELEMETRY (last 2 waves)");
        _sb.AppendLine("-----------------------------");

        // δείχνουμε last & prev (ώστε να φαίνεται αλλαγή)
        _sb.Append("Strafe%:      ");
        _sb.Append(Format2Percent(lastEnemy.strafePercent01, prevEnemy.strafePercent01));
        _sb.AppendLine();

        _sb.Append("Avg distance: ");
        _sb.Append(Format2(lastEnemy.avgDistance, prevEnemy.avgDistance, "m"));
        _sb.AppendLine();

        _sb.Append("1st hit time: ");
        _sb.Append(Format2TimeOrDash(lastEnemy.timeToFirstHitSec, prevEnemy.timeToFirstHitSec));
        _sb.AppendLine();

        _sb.Append("Shots F/R:    ");
        _sb.Append("W");
        _sb.Append(lastEnemy.shotsFired.ToString());
        _sb.Append("/");
        _sb.Append(lastEnemy.shotsRequested.ToString());
        _sb.Append(" | Prev ");
        _sb.Append(prevEnemy.shotsFired.ToString());
        _sb.Append("/");
        _sb.Append(prevEnemy.shotsRequested.ToString());
        _sb.AppendLine();

        _sb.AppendLine();
        _sb.AppendLine("KNOBS (next wave)");
        _sb.AppendLine("-----------------");
        _sb.Append("Aim error:  ");
        _sb.Append(aimErrDeg.ToString("0.00"));
        _sb.AppendLine(" deg");

        _sb.Append("Cooldown:   ");
        _sb.Append(cooldownSec.ToString("0.00"));
        _sb.AppendLine(" s");

        _sb.Append("Move speed: ");
        _sb.Append(moveSpeed.ToString("0.00"));
        _sb.AppendLine();

        rightPanelText.text = _sb.ToString();
    }

    private static string Format2(float a, float b, string suffix = "")
    {
        if (string.IsNullOrEmpty(suffix))
            return "W" + a.ToString("0.00") + " | Prev " + b.ToString("0.00");
        return "W" + a.ToString("0.00") + suffix + " | Prev " + b.ToString("0.00") + suffix;
    }

    private static string Format2Percent(float a01, float b01)
    {
        float a = a01 * 100f;
        float b = b01 * 100f;
        return "W" + a.ToString("0") + "% | Prev " + b.ToString("0") + "%";
    }

    private static string Format2TimeOrDash(float a, float b)
    {
        string A = (a < 0f) ? "-" : (a.ToString("0.00") + "s");
        string B = (b < 0f) ? "-" : (b.ToString("0.00") + "s");
        return "W" + A + " | Prev " + B;
    }
}
