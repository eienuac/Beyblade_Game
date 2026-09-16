using UnityEngine;

/// <summary>
/// Basit Rakip AI: Otomatik fırlatılır ve arenada dolaşır.
/// </summary>
public class AIBeyblade : MonoBehaviour
{
    [Header("AI Ayarları")]
    [Tooltip("AI'ın fırlatma gücü (0-1 arası)")]
    public float aiPower = 0.75f;

    [Tooltip("Fırlatma gecikmesi (saniye - SADECE OYUNCU FIRLATTIKTAN SONRA SAYAR)")]
    public float launchDelay = 0.2f;

    [Tooltip("Fırlatma kuvveti")]
    public float launchForce = 12f;

    private BeybladeController beyblade;
    private Rigidbody rb;
    private bool launched = false;
    private bool playerHasLaunched = false;

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
    }

    private void Update()
    {
        if (launched) return;

        // Oyuncunun fırlatıp fırlatmadığını kontrol et
        if (!playerHasLaunched)
        {
            GameObject player = GameObject.Find("PlayerBeyblade");
            if (player != null)
            {
                BeybladeController pControl = player.GetComponent<BeybladeController>();
                if (pControl != null && pControl.isLaunched)
                {
                    playerHasLaunched = true;
                    // Oyuncu fırlattıktan kısa süre sonra biz de fırlatılıyoruz
                    Invoke("LaunchAI", launchDelay);
                }
            }
        }
    }

    private void LaunchAI()
    {
        if (launched) return;
        launched = true;

        if (rb != null) rb.isKinematic = false;

        // Arenanın merkezine doğru fırlat
        Vector3 launchDir = (Vector3.zero - transform.position).normalized;
        launchDir.y = -0.2f;
        launchDir = launchDir.normalized;

        float power = Random.Range(aiPower - 0.1f, aiPower + 0.1f);
        power = Mathf.Clamp(power, 0.4f, 1f); // Minimum 0.4f güç ile merkeze süzülsün

        if (rb != null) rb.AddForce(launchDir * launchForce * power, ForceMode.Impulse);

        if (beyblade != null)
        {
            beyblade.currentStamina = beyblade.maxStamina * power;
            beyblade.currentSpinRPM = beyblade.maxSpinRPM * power;
            beyblade.isLaunched = true;
            beyblade.isSpinning = true;
            
            CapsuleCollider col = beyblade.GetComponent<CapsuleCollider>();
            if (col != null) col.enabled = true;
        }
    }
}
