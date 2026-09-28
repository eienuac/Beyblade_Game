using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// Maç sonucu ve modern bitiş ekranı.
/// </summary>
public class BattleManager : MonoBehaviour
{
    private BeybladeController player;
    private BeybladeController enemy;
    private bool gameEnded;
    private float playerOutTime;
    private float enemyOutTime;

    private Canvas uiCanvas;
    private CanvasGroup group;
    private TextMeshProUGUI kickerText;
    private TextMeshProUGUI resultText;
    private TextMeshProUGUI subText;
    private Image accentBar;
    private RectTransform panelRt;

    private void Start()
    {
        CreateEndGameUI();
        StartCoroutine(FindBeybladesRoutine());
    }

    private IEnumerator FindBeybladesRoutine()
    {
        while (player == null || enemy == null)
        {
            GameObject pObj = GameObject.Find("PlayerBeyblade");
            if (pObj != null) player = pObj.GetComponent<BeybladeController>();

            GameObject eObj = GameObject.Find("EnemyBeyblade");
            if (eObj != null) enemy = eObj.GetComponent<BeybladeController>();

            yield return new WaitForSeconds(0.4f);
        }
    }

    private void Update()
    {
        if (gameEnded || player == null || enemy == null) return;

        if (player.isLaunched && enemy.isLaunched)
        {
            CheckRingOut(player, ref playerOutTime);
            CheckRingOut(enemy, ref enemyOutTime);

            if (player.hasToppled && !enemy.hasToppled) EndGame(false);
            else if (enemy.hasToppled && !player.hasToppled) EndGame(true);
            else if (player.hasToppled && enemy.hasToppled) EndGame(true, true);
        }
    }

    private void CheckRingOut(BeybladeController bey, ref float outTime)
    {
        if (bey.hasToppled || !bey.isSpinning)
        {
            outTime = 0f;
            return;
        }

        Vector3 p = bey.transform.position;
        bool outside;
        if (ArenaInfo.Known)
        {
            Vector3 flat = p - ArenaInfo.Center;
            flat.y = 0f;
            outside = flat.magnitude > ArenaInfo.Radius * 1.03f || p.y < ArenaInfo.MinY - 0.25f;
        }
        else
        {
            outside = p.y < -3f;
        }

        if (!outside)
        {
            outTime = 0f;
            return;
        }

        outTime += Time.deltaTime;
        if (outTime >= 0.35f) bey.ForceRingOut();
    }

    private void EndGame(bool playerWon, bool draw = false)
    {
        if (gameEnded) return;
        gameEnded = true;
        if (playerWon && !draw) PathingManager.OnMatchWon();
        StartCoroutine(ShowResultRoutine(playerWon, draw));
    }

    private IEnumerator ShowResultRoutine(bool playerWon, bool draw)
    {
        yield return new WaitForSeconds(1.6f);

        Color accent;
        if (draw)
        {
            kickerText.text = "MAÇ SONUCU";
            resultText.text = "BERABERE";
            subText.text = "İki beyblade de durdu.";
            accent = ModernUIKit.Gold;
        }
        else if (playerWon)
        {
            bool ringOut = enemy != null && enemy.WasRingedOut;
            kickerText.text = ringOut ? "RING OUT" : "MAÇ SONUCU";
            resultText.text = "ZAFER";
            subText.text = ringOut ? "Rakibi arenadan attın." : "Rakibin spinini kestin.";
            accent = ModernUIKit.Cyan;
        }
        else
        {
            bool ringOut = player != null && player.WasRingedOut;
            kickerText.text = ringOut ? "RING OUT" : "MAÇ SONUCU";
            resultText.text = "YENİLDİN";
            subText.text = ringOut ? "Arenadan düştün." : "Beyblade'in durdu.";
            accent = ModernUIKit.Magenta;
        }

        resultText.color = Color.white;
        kickerText.color = accent;
        accentBar.color = accent;
        subText.color = ModernUIKit.Dim;

        uiCanvas.gameObject.SetActive(true);
        group.alpha = 0f;
        panelRt.localScale = Vector3.one * 0.94f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 1.6f;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
            group.alpha = e;
            panelRt.localScale = Vector3.Lerp(Vector3.one * 0.94f, Vector3.one, e);
            yield return null;
        }
        group.alpha = 1f;
        panelRt.localScale = Vector3.one;
    }

    private void RestartGame()
    {
        SceneManager.LoadScene("SampleScene");
    }

    private void GoMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void CreateEndGameUI()
    {
        ModernUIKit.EnsureEventSystem();
        uiCanvas = ModernUIKit.CreateCanvas("BattleEndUI", 999);
        group = uiCanvas.gameObject.AddComponent<CanvasGroup>();

        Image veil = ModernUIKit.MakeImage(uiCanvas.transform, "Veil", new Color(0.02f, 0.03f, 0.05f, 0.78f), null, false);
        ModernUIKit.Stretch(veil.rectTransform);
        veil.sprite = null;
        veil.color = new Color(0.02f, 0.03f, 0.05f, 0.78f);

        Image glow = ModernUIKit.MakeImage(uiCanvas.transform, "Glow", new Color(0.2f, 0.7f, 1f, 0.12f), ModernUIKit.CircleSprite, false);
        ModernUIKit.Anchor(glow.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 900));
        glow.raycastTarget = false;

        GameObject panel = new GameObject("ResultCard");
        panel.transform.SetParent(uiCanvas.transform, false);
        panelRt = panel.AddComponent<RectTransform>();
        ModernUIKit.Anchor(panelRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 520));

        Image card = ModernUIKit.MakeImage(panel.transform, "Card", ModernUIKit.Panel);
        ModernUIKit.Stretch(card.rectTransform);

        accentBar = ModernUIKit.MakeImage(panel.transform, "Accent", ModernUIKit.Cyan, null, false);
        ModernUIKit.Anchor(accentBar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -28), new Vector2(120, 4));
        accentBar.sprite = null;

        kickerText = ModernUIKit.Label(panel.transform, "Kicker", "MAÇ SONUCU", 18, ModernUIKit.Cyan);
        kickerText.characterSpacing = 14f;
        kickerText.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(kickerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(700, 36));

        resultText = ModernUIKit.Label(panel.transform, "Title", "ZAFER", 92, Color.white);
        resultText.fontStyle = FontStyles.Bold;
        resultText.characterSpacing = 10f;
        ModernUIKit.Anchor(resultText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(720, 130));

        subText = ModernUIKit.Label(panel.transform, "Sub", "", 24, ModernUIKit.Dim);
        ModernUIKit.Anchor(subText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -230), new Vector2(640, 50));

        Button rematch = ModernUIKit.MakeButton(panel.transform, "Rematch", "TEKRAR", ModernUIKit.Cyan, new Vector2(280, 72), RestartGame);
        ModernUIKit.Anchor(rematch.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-16, 48), new Vector2(280, 72));

        Button menu = ModernUIKit.MakeButton(panel.transform, "Menu", "MENÜ", ModernUIKit.Dim, new Vector2(280, 72), GoMenu);
        ModernUIKit.Anchor(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(16, 48), new Vector2(280, 72));

        uiCanvas.gameObject.SetActive(false);
    }
}
