using UnityEngine;

/// <summary>
/// Oyuncu Beyblade'inin arkasından aksiyon kamerası.
/// Sağ/sol tarafı zamanla ve çarpışmada değiştirir.
/// </summary>
public class TopDownCamera : MonoBehaviour
{
    [Header("Takip Hedefleri")]
    public Transform playerTarget;
    public Transform enemyTarget;

    [Header("Kilitli Takip")]
    public float backDistance = 5.8f;
    public float height = 2.85f;
    public float sideAmplitude = 2.15f;
    public float followSpeed = 6.2f;
    public float rotationSpeed = 9f;
    public float lookAhead = 0.42f;
    public float fieldOfView = 50f;

    [Header("Sarsılma Efekti")]
    public float shakeIntensity = 0.18f;
    public float shakeDuration = 0.16f;

    private float currentShake = 0f;
    private float shakeTimer = 0f;
    private Vector3 smoothedForward = Vector3.forward;
    private Vector3 velocityRef;
    private Camera cam;
    private bool snapped;

    private float targetSide = 1f;
    private float currentSide = 1f;
    private float sideTimer = 5f;
    private float heightBob;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = fieldOfView;
            cam.nearClipPlane = 0.12f;
        }
        targetSide = Random.value > 0.5f ? 1f : -1f;
        currentSide = targetSide;
        sideTimer = Random.Range(4.2f, 7.5f);
        FindTargets();
    }

    private void FindTargets()
    {
        if (playerTarget == null)
        {
            GameObject p = GameObject.Find("PlayerBeyblade");
            if (p != null) playerTarget = p.transform;
        }
        if (enemyTarget == null)
        {
            GameObject e = GameObject.Find("EnemyBeyblade");
            if (e != null) enemyTarget = e.transform;
        }
    }

    private void LateUpdate()
    {
        if (playerTarget == null)
        {
            FindTargets();
            if (playerTarget == null) return;
        }

        sideTimer -= Time.deltaTime;
        if (sideTimer <= 0f) FlipSide();

        currentSide = Mathf.Lerp(currentSide, targetSide, 1f - Mathf.Exp(-1.35f * Time.deltaTime));
        heightBob = Mathf.Lerp(heightBob, Mathf.Sin(Time.time * 0.35f) * 0.18f, Time.deltaTime * 2f);

        Vector3 playerPos = playerTarget.position;
        Vector3 enemyPos = enemyTarget != null ? enemyTarget.position : playerPos + Vector3.forward * 5f;

        Vector3 toEnemy = enemyPos - playerPos;
        toEnemy.y = 0f;

        if (toEnemy.sqrMagnitude > 0.12f)
        {
            Vector3 desiredForward = toEnemy.normalized;
            if (!snapped) smoothedForward = desiredForward;
            else smoothedForward = Vector3.Slerp(smoothedForward, desiredForward, 1f - Mathf.Exp(-4.2f * Time.deltaTime));
        }
        else if (!snapped)
        {
            Vector3 fromCenter = playerPos;
            fromCenter.y = 0f;
            smoothedForward = fromCenter.sqrMagnitude > 0.05f ? fromCenter.normalized : Vector3.forward;
        }

        Vector3 forward = smoothedForward.sqrMagnitude > 0.001f ? smoothedForward.normalized : Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        float fightDist = Vector3.Distance(playerPos, enemyPos);
        float back = Mathf.Clamp(backDistance + fightDist * 0.22f, 4.6f, 8.8f);
        float camHeight = Mathf.Clamp(height + fightDist * 0.1f, 2.3f, 4.4f) + heightBob;

        Vector3 desiredPos = playerPos
            - forward * back
            + right * (sideAmplitude * currentSide)
            + Vector3.up * camHeight;

        Vector3 lookPoint = Vector3.Lerp(playerPos, enemyPos, lookAhead) + Vector3.up * 0.32f;
        Quaternion targetRot = Quaternion.LookRotation(lookPoint - desiredPos, Vector3.up);

        if (!snapped)
        {
            transform.position = desiredPos;
            transform.rotation = targetRot;
            snapped = true;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocityRef, 1f / followSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookPoint - transform.position, Vector3.up), 1f - Mathf.Exp(-rotationSpeed * Time.deltaTime));
        }

        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float falloff = Mathf.Clamp01(shakeTimer / Mathf.Max(0.01f, shakeDuration));
            float t = Time.time;
            transform.position += right * (Mathf.Sin(t * 38f) * currentShake * falloff)
                                  + Vector3.up * (Mathf.Cos(t * 29f) * currentShake * 0.35f * falloff);
        }
    }

    public void FlipSide()
    {
        targetSide = currentSide >= 0f ? -1f : 1f;
        sideTimer = Random.Range(4.5f, 8.5f);
    }

    public void TriggerShake(float intensity = -1f)
    {
        if (intensity < 0f) intensity = shakeIntensity;
        currentShake = intensity;
        shakeTimer = shakeDuration;
        if (Random.value > 0.4f) FlipSide();
    }
}
