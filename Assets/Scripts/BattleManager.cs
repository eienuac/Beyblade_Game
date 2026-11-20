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
    private TextMeshProUGUI rewardText;
    private int rewardGold;
    private bool playerWonMatch;
    private Image accentBar;
    private RectTransform panelRt;
    private TextMeshProUGUI rematchLabel;

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

        BeybladeCore.MatchOutcome outcome = draw ? BeybladeCore.MatchOutcome.Draw
            : playerWon ? BeybladeCore.MatchOutcome.Win : BeybladeCore.MatchOutcome.Loss;
        bool ringOut = outcome == BeybladeCore.MatchOutcome.Win ? (enemy != null && enemy.WasRingedOut)
                     : outcome == BeybladeCore.MatchOutcome.Loss && player != null && player.WasRingedOut;

        // Denge testi: sonucu bildir, ödül/ekran yok
        if (MatchContext.IsBalanceTest)
        {
            BalanceTestRunner.Report(outcome, ringOut, Time.timeSinceLevelLoad);
            return;
        }

        rewardGold = BeybladeCore.MatchRewards.Calculate(outcome, MatchContext.IsCareer, MatchContext.CareerLevel,
            outcome == BeybladeCore.MatchOutcome.Win && ringOut);
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.AddGold(rewardGold);
            PlayerDataManager.Instance.RecordMatch(outcome == BeybladeCore.MatchOutcome.Win, draw, ringOut);
        }

        playerWonMatch = outcome == BeybladeCore.MatchOutcome.Win;
        if (playerWonMatch) PathingManager.OnMatchWon();
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
            kickerText.text = ringOut ? "ARENA DIŞI" : "SPİN BİTİŞİ";
            resultText.text = "ZAFER";
            subText.text = ringOut ? "Rakibi arenadan attın." : "Rakibin dönüşünü bitirdin.";
            accent = ModernUIKit.Battle;
        }
        else
        {
            bool ringOut = player != null && player.WasRingedOut;
            kickerText.text = ringOut ? "ARENA DIŞI" : "SPİN BİTİŞİ";
            resultText.text = "YENİLDİN";
            subText.text = ringOut ? "Arenadan düştün." : "Beyin durdu. Tipini ve fırlatışını değiştirmeyi dene.";
            accent = ModernUIKit.Danger;
        }

        resultText.color = Color.white;
        kickerText.color = accent;
        accentBar.color = accent;
        subText.color = ModernUIKit.Dim;
        rewardText.text = "+" + rewardGold + " BEYCOIN";
        if (MatchContext.IsCareer)
            rewardText.text += "   ·   KARİYER LV " + MatchContext.CareerLevel;
        rewardText.color = ModernUIKit.Gold;
        if (rematchLabel != null)
            rematchLabel.text = MatchContext.IsCareer && playerWonMatch ? "SONRAKİ SEVİYE" : "TEKRAR";

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
        // Kariyerde kazandıysan sıradaki seviyeye geç, kaybettiysen aynı seviyeyi tekrar oyna
        if (MatchContext.IsCareer && PlayerDataManager.Instance != null)
            MatchContext.SetupCareerMatch(Mathf.Min(MatchContext.CareerLevel + (playerWonMatch ? 1 : 0),
                PlayerDataManager.Instance.data.currentPathingLevel));
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

        Image glow = ModernUIKit.MakeImage(uiCanvas.transform, "Glow", new Color(0.2f, 0.7f, 1f, 0.12f), ModernUIKit.GlowSprite, false);
        ModernUIKit.Anchor(glow.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 900));
        glow.raycastTarget = false;

        GameObject panel = new GameObject("ResultCard");
        panel.transform.SetParent(uiCanvas.transform, false);
        panelRt = panel.AddComponent<RectTransform>();
        ModernUIKit.Anchor(panelRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860, 540));

        Image card = ModernUIKit.MakeSkewPanel(panel.transform, "Card", new Color(0.03f, 0.05f, 0.1f, 0.94f));
        ModernUIKit.Stretch(card.rectTransform);

        accentBar = ModernUIKit.MakeSkewPanel(panel.transform, "Accent", ModernUIKit.Cyan);
        ModernUIKit.Anchor(accentBar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(160, 8));

        kickerText = ModernUIKit.Label(panel.transform, "Kicker", "MAÇ SONUCU", 18, ModernUIKit.Cyan, ModernUIKit.FontRole.Title);
        kickerText.characterSpacing = 14f;
        kickerText.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(kickerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -52), new Vector2(760, 30));

        resultText = ModernUIKit.Label(panel.transform, "Title", "ZAFER", 110, Color.white, ModernUIKit.FontRole.Display);
        resultText.fontStyle = FontStyles.Bold | FontStyles.Italic;
        resultText.characterSpacing = 12f;
        ModernUIKit.Anchor(resultText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -86), new Vector2(800, 130));

        subText = ModernUIKit.Label(panel.transform, "Sub", "", 22, ModernUIKit.Dim, ModernUIKit.FontRole.Title);
        ModernUIKit.Anchor(subText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -226), new Vector2(760, 50));

        rewardText = ModernUIKit.Label(panel.transform, "Reward", "", 28, ModernUIKit.Gold, ModernUIKit.FontRole.Display);
        rewardText.fontStyle = FontStyles.Bold;
        rewardText.characterSpacing = 4f;
        ModernUIKit.Anchor(rewardText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -290), new Vector2(760, 44));

        Button rematch = MakeResultButton(panel.transform, "Rematch", "TEKRAR", ModernUIKit.Cyan, true, new Vector2(-16, 52), new Vector2(1f, 0f), RestartGame);
        rematchLabel = rematch.GetComponentInChildren<TextMeshProUGUI>();
        MakeResultButton(panel.transform, "Menu", "MENÜ", ModernUIKit.Dim, false, new Vector2(16, 52), new Vector2(0f, 0f), GoMenu);

        uiCanvas.gameObject.SetActive(false);
    }

    private Button MakeResultButton(Transform parent, string name, string label, Color accent, bool primary, Vector2 pos, Vector2 pivot, UnityEngine.Events.UnityAction onClick)
    {
        Image img = ModernUIKit.MakeSkewPanel(parent, name, primary ? accent : new Color(1f, 1f, 1f, 0.08f));
        img.raycastTarget = true;
        ModernUIKit.Anchor(img.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), pivot, pos, new Vector2(320, 76));
        TextMeshProUGUI l = ModernUIKit.Label(img.transform, "L", label, 26, primary ? new Color(0.02f, 0.04f, 0.08f) : Color.white, ModernUIKit.FontRole.Display);
        l.fontStyle = FontStyles.Bold | FontStyles.Italic;
        l.characterSpacing = 5f;
        ModernUIKit.Stretch(l.rectTransform);
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(onClick);
        ModernUIKit.AddPressFeedback(b);
        return b;
    }
}
