using UnityEngine;
using BeybladeCore;

/// <summary>
/// Rakip botun fırlatılması. Hareket/taktik kararları BeyControl → BotBrain'dedir.
/// Oyuncu fırlattıktan kısa süre sonra fırlatır (denge testinde hemen).
/// </summary>
public class AIBeyblade : MonoBehaviour
{
    [Tooltip("Fırlatma gecikmesi (oyuncu fırlattıktan sonra)")]
    public float launchDelay = 0.2f;

    [Header("Hızlı savaş fırlatma gücü (oyuncuya yakın)")]
    public float quickMatchPowerMin = 0.72f;
    public float quickMatchPowerMax = 0.92f;

    private BeybladeController beyblade;
    private BeybladeController player;
    private Rigidbody rb;
    private bool launched = false;
    private bool queued = false;

    private void Start()
    {
        beyblade = GetComponent<BeybladeController>();
        rb = GetComponent<Rigidbody>();

        if (beyblade != null)
        {
            beyblade.isLaunched = false;
            beyblade.isSpinning = true;
            if (rb != null) rb.isKinematic = true;
        }

        if (MatchContext.IsBalanceTest)
        {
            queued = true;
            Invoke(nameof(LaunchAI), 0.05f);
        }
    }

    private void Update()
    {
        if (launched || queued) return;

        if (player == null)
        {
            GameObject p = GameObject.Find("PlayerBeyblade");
            if (p != null) player = p.GetComponent<BeybladeController>();
        }
        if (player != null && player.isLaunched)
        {
            queued = true;
            Invoke(nameof(LaunchAI), launchDelay);
        }
    }

    float PickPower()
    {
        if (MatchContext.IsBalanceTest) return Random.Range(0.7f, 0.95f);
        if (!MatchContext.IsCareer) return Random.Range(quickMatchPowerMin, quickMatchPowerMax);
        BotDifficulty diff = MatchContext.Difficulty;
        return Random.Range(diff.LaunchPowerMin, diff.LaunchPowerMax);
    }

    private void LaunchAI()
    {
        if (launched || beyblade == null) return;
        launched = true;
        Launch(beyblade, PickPower());
    }

    /// <summary>Oyuncu ve bot için ortak fırlatma (stamina, RPM, itki aynı formülle).</summary>
    public static void Launch(BeybladeController bey, float power)
    {
        if (bey == null) return;
        CombatTuning t = CombatTuning.Default;
        power = Mathf.Clamp(power, 0.08f, 1f);

        Rigidbody body = bey.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = false;
            Vector3 center = ArenaInfo.Known ? ArenaInfo.Center : Vector3.zero;
            Vector3 dir = center - bey.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            dir.Normalize();
            // Hafif açı sapması: her fırlatış aynı çizgide olmasın
            dir = Quaternion.AngleAxis(Random.Range(-14f, 14f), Vector3.up) * dir;
            body.AddForce(dir * (t.LaunchImpulse * power), ForceMode.Impulse);
        }

        float spin = Combat.LaunchSpinFraction(power, t);
        bey.currentStamina = bey.maxStamina * spin;
        bey.currentSpinRPM = bey.maxSpinRPM * spin;
        bey.isLaunched = true;
        bey.isSpinning = true;
        CapsuleCollider col = bey.GetComponent<CapsuleCollider>();
        if (col != null) col.enabled = true;
    }
}
