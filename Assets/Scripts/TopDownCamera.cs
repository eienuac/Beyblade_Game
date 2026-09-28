using UnityEngine;

/// <summary>
/// Oyuncu Beyblade'inin arkasından aksiyon kamerası.
/// Normalde biraz uzak; çarpışmada kısa süre yakınlaşır.
/// </summary>
public class TopDownCamera : MonoBehaviour
{
    [Header("Takip Hedefleri")]
    public Transform playerTarget;
    public Transform enemyTarget;

    [Header("Kilitli Takip")]
    public float backDistance = 7.4f;
    public float height = 3.6f;
    public float sideAmplitude = 2.35f;
    public float followSpeed = 5.4f;
    public float rotationSpeed = 8f;
    public float lookAhead = 0.38f;
    public float fieldOfView = 48f;

    [Header("Çarpışma Zoom")]
    public float impactZoomDistance = 5.2f;
    public float impactZoomHeight = 2.7f;
    public float impactZoomDuration = 0.55f;

    [Header("Sarsılma Efekti")]
    public float shakeIntensity = 0.14f;
    public float shakeDuration = 0.14f;

    private float currentShake = 0f;
    private float shakeTimer = 0f;
    private float impactZoomTimer = 0f;
    private Vector3 smoothedForward = Vector3.forward;
    private Vector3 velocityRef;
    private Camera cam;
    private bool snapped;

    private float targetSide = 1f;
    private float currentSide = 1f;
    private float sideTimer = 5f;
    private float heightBob;
    private float zoomBlend;

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

        if (impactZoomTimer > 0f)
            impactZoomTimer -= Time.deltaTime;

        float wantZoom = impactZoomTimer > 0f ? 1f : 0f;
        zoomBlend = Mathf.Lerp(zoomBlend, wantZoom, 1f - Mathf.Exp(-6f * Time.deltaTime));

        currentSide = Mathf.Lerp(currentSide, targetSide, 1f - Mathf.Exp(-1.35f * Time.deltaTime));
        heightBob = Mathf.Lerp(heightBob, Mathf.Sin(Time.time * 0.35f) * 0.14f, Time.deltaTime * 2f);

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
        float backFar = Mathf.Clamp(backDistance + fightDist * 0.18f, 6.2f, 10.5f);
        float heightFar = Mathf.Clamp(height + fightDist * 0.08f, 3.1f, 5.2f);
        float backNear = Mathf.Clamp(impactZoomDistance + fightDist * 0.08f, 4.4f, 6.8f);
        float heightNear = Mathf.Clamp(impactZoomHeight + fightDist * 0.05f, 2.3f, 3.6f);

        float back = Mathf.Lerp(backFar, backNear, zoomBlend);
        float camHeight = Mathf.Lerp(heightFar, heightNear, zoomBlend) + heightBob;

        Vector3 desiredPos = playerPos
            - forward * back
            + right * (sideAmplitude * currentSide)
            + Vector3.up * camHeight;

        Vector3 lookPoint = Vector3.Lerp(playerPos, enemyPos, lookAhead) + Vector3.up * 0.32f;

        if (!snapped)
        {
            transform.position = desiredPos;
            transform.rotation = Quaternion.LookRotation(lookPoint - desiredPos, Vector3.up);
            snapped = true;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocityRef, 1f / followSpeed);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookPoint - transform.position, Vector3.up),
                1f - Mathf.Exp(-rotationSpeed * Time.deltaTime));
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
        impactZoomTimer = impactZoomDuration;
        if (Random.value > 0.55f) FlipSide();
    }
}
