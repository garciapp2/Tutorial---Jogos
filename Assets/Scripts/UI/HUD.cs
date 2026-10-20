using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interface da partida, em pixels logicos (480x270): coracoes, pontos, barra de calor com o
/// rastro branco atrasado, tempo, fragmentos, faixas, numeros que saltam, pausa e as telas
/// de derrota e de resultado.
/// </summary>
public class HUD : MonoBehaviour
{
    public static HUD Instance { get; private set; }

    [Header("Vida e pontos")]
    public Image[] hearts;
    public Sprite heartFull, heartEmpty;
    public PixelText scoreText;

    [Header("Calor")]
    public RectTransform heatFill, heatLag, heatShine;
    public Image heatFillImage;
    public PixelText heatText;
    public RectTransform heatIcon;
    public float heatBarWidth = 120f;
    public PixelText frostLabel;

    [Header("Tempo e fragmentos")]
    public PixelText timeText;
    public PixelText fragmentText;
    public RectTransform fragmentIcon;

    [Header("Mensagens")]
    public PixelText banner;
    public PixelText hint;
    public RectTransform popupLayer;
    public PixelText popupTemplate;

    [Header("Telas")]
    public Image dim;
    public GameObject pausePanel;
    public PixelMenu pauseMenu;
    public OptionsPanel options;
    public GameObject resultsPanel;
    public RectTransform resultsCard;
    public PixelText resultsTitle;
    public PixelText resultsGrade;
    public GameObject resultsGradeLabel;
    public Image resultsIcon;
    public PixelText resultsReason;
    public PixelText resultsLabels;
    public PixelText resultsValues;
    public PixelMenu resultsMenu;

    readonly Spring[] heartPop = { new Spring(1f, 380f, 14f), new Spring(1f, 380f, 14f), new Spring(1f, 380f, 14f) };
    readonly Spring heatPop = new Spring(1f, 380f, 14f);
    readonly Spring fragPop = new Spring(1f, 380f, 14f);
    readonly Spring cardPop = new Spring(1f, 260f, 16f);
    readonly Spring gradePop = new Spring(0f, 300f, 12f);
    float shownScore, lag = 1f, lagHold, gainFlash, bannerTimer, dimTarget;
    Vector2[] heartBase;
    Camera cam;

    class FloatingText { public PixelText text; public Vector3 world; public float t, life, vy, rise; }
    readonly List<FloatingText> popups = new List<FloatingText>();

    public bool OptionsOpen => options != null && options.gameObject.activeSelf;

    void Awake()
    {
        Instance = this;
        cam = Camera.main;
        heartBase = new Vector2[hearts.Length];
        for (int i = 0; i < hearts.Length; i++) heartBase[i] = hearts[i].rectTransform.anchoredPosition;
        pausePanel.SetActive(false);
        resultsPanel.SetActive(false);
        options.gameObject.SetActive(false);
        popupTemplate.gameObject.SetActive(false);
        banner.Text = "";
        dim.color = Pal.Alpha(Pal.Ink, 0f);
        pauseMenu.onBackRuntime = () => GameManager.Instance.Resume();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ------------------------------------------------------------ status
    public void Refresh(bool immediate)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        float dt = Time.unscaledDeltaTime;

        int hp = gm.player != null ? gm.player.Current : 0;
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].sprite = i < hp ? heartFull : heartEmpty;
            heartPop[i].Update(dt);
            float s = heartPop[i].value;
            // o ultimo coracao treme quando e o unico
            Vector2 shake = hp == 1 && i == 0 && gm.IsPlaying ? new Vector2(Mathf.Round(Mathf.Sin(Time.unscaledTime * 40f)), 0f) : Vector2.zero;
            hearts[i].rectTransform.localScale = new Vector3(s, s, 1f);
            hearts[i].rectTransform.anchoredPosition = heartBase[i] + shake;
        }

        shownScore = immediate && shownScore == 0 ? gm.Score : Mathf.MoveTowards(shownScore, gm.Score, Mathf.Max(60f, Mathf.Abs(gm.Score - shownScore) * 8f) * dt);
        scoreText.Text = Mathf.RoundToInt(shownScore).ToString("000000");

        // calor: barra, rastro branco atrasado e clarao ao ganhar
        float frac = gm.Heat01;
        lagHold -= dt;
        if (lag < frac) lag = frac;
        else if (lagHold <= 0f) lag = Ease.Approach(lag, frac, 6f, dt);
        SetWidth(heatFill, frac);
        SetWidth(heatShine, frac);
        SetWidth(heatLag, lag);
        gainFlash -= dt;
        bool low = gm.Heat <= gm.lowHeat && gm.IsPlaying;
        Color fill = Color.Lerp(Pal.Red, Pal.Orange, Mathf.Clamp01((frac - 0.15f) * 3f));
        if (frac > 0.66f) fill = Color.Lerp(Pal.Orange, Pal.Yellow, (frac - 0.66f) * 3f);
        if (low && Mathf.Repeat(Time.unscaledTime, 0.4f) > 0.2f) fill = Pal.Yellow;
        if (gm.InFrostZone) fill = Color.Lerp(fill, Pal.BlueLight, 0.55f);
        if (gainFlash > 0f) fill = Pal.White;
        heatFillImage.color = fill;
        heatText.Text = $"{Mathf.Max(0f, gm.Heat):0.0}s";
        heatText.color = low ? (Mathf.Repeat(Time.unscaledTime, 0.4f) > 0.2f ? Pal.Red : Pal.White) : Pal.White;
        heatPop.Update(dt);
        heatIcon.localScale = Vector3.one * heatPop.value;
        frostLabel.enabled = gm.InFrostZone && gm.IsPlaying && Mathf.Repeat(Time.unscaledTime, 0.6f) > 0.15f;

        timeText.Text = Settings.FormatTime(gm.Elapsed);
        fragmentText.Text = GameManager.Endless ? $"O{gm.Wave}  {gm.Fragments}/{gm.TotalFragments}" : $"{gm.Fragments}/{gm.TotalFragments}";
        fragmentText.color = gm.ExitUnlocked ? Pal.Yellow : Pal.White;
        fragPop.Update(dt);
        fragmentIcon.localScale = Vector3.one * fragPop.value;

        hint.Text = GameInput.UsingGamepad
            ? "<#f8cf4e>A</> dash   <#f8cf4e>START</> pausa"
            : "<#f8cf4e>ESPAÇO</> dash   <#f8cf4e>ESC</> pausa";
        // a dica some depois do comeco da partida
        var hc = hint.color;
        hc.a = Mathf.Clamp01(1f - (gm.Elapsed - 6f));
        hint.color = hc;
    }

    void SetWidth(RectTransform rt, float frac)
    {
        rt.sizeDelta = new Vector2(Mathf.Round(heatBarWidth * Mathf.Clamp01(frac)), rt.sizeDelta.y);
    }

    public void HeatSpent() => lagHold = 0.35f;

    public void HeatGained()
    {
        gainFlash = 0.08f;
        heatPop.Kick(10f);
    }

    public void FragmentGained() => fragPop.Kick(16f);

    public void HeartLost(int healthLeft)
    {
        int i = Mathf.Clamp(healthLeft, 0, hearts.Length - 1);
        heartPop[i].Kick(-14f);
        for (int k = 0; k < hearts.Length; k++) heartPop[k].Kick(4f);
    }

    // ------------------------------------------------------------ mensagens
    public void Banner(string text, Color color, float duration, int scale = 3)
    {
        banner.Text = text;
        // diminui ate caber na largura da tela
        while (scale > 1 && PixelText.Width(text, scale) > 290) scale--;
        banner.scale = scale;
        banner.color = color;
        banner.Replay();
        bannerTimer = duration;
    }

    public void Popup(string text, Vector2 world, Color color, int scale = 1)
    {
        var t = Instantiate(popupTemplate, popupLayer);
        t.gameObject.SetActive(true);
        t.Text = text;
        t.color = color;
        t.scale = scale;
        popups.Add(new FloatingText { text = t, world = world, life = 0.8f, vy = 30f, rise = 0f });
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (bannerTimer > 0f)
        {
            bannerTimer -= dt;
            var c = banner.color;
            c.a = Mathf.Clamp01(bannerTimer / 0.3f);
            banner.color = c;
            if (bannerTimer <= 0f) banner.Text = "";
        }

        for (int i = popups.Count - 1; i >= 0; i--)
        {
            var p = popups[i];
            p.t += dt;
            p.rise += p.vy * dt;
            p.vy = Ease.Approach(p.vy, 6f, 4f, dt);
            if (p.t >= p.life)
            {
                Destroy(p.text.gameObject);
                popups.RemoveAt(i);
                continue;
            }
            Vector2 screen = cam.WorldToScreenPoint(p.world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(popupLayer, screen, cam, out var local);
            float jitter = p.t < 0.12f ? Random.Range(-1, 2) : 0f;
            p.text.rectTransform.anchoredPosition = new Vector2(Mathf.Round(local.x + jitter), Mathf.Round(local.y + 10f + p.rise));
            var c = p.text.color;
            c.a = Mathf.Clamp01((p.life - p.t) / 0.25f);
            p.text.color = c;
        }

        float a = Ease.Approach(dim.color.a, dimTarget, 10f, dt);
        dim.color = Pal.Alpha(Pal.Ink, a);
        dim.enabled = a > 0.01f;

        if (resultsPanel.activeSelf)
        {
            cardPop.Update(dt);
            resultsCard.localScale = Vector3.one * cardPop.value;
            gradePop.Update(dt);
            resultsGrade.rectTransform.localScale = Vector3.one * Mathf.Max(0f, gradePop.value);
        }
    }

    // ------------------------------------------------------------ telas
    public void ShowPause(bool show)
    {
        pausePanel.SetActive(show);
        if (!show) options.gameObject.SetActive(false);
        dimTarget = show ? 0.55f : 0f;
        if (show) pauseMenu.index = 0;
    }

    public void ShowGameOver(string title, string reason, float time, int score, int fragments, int total, int wave = 0)
    {
        resultsTitle.Text = title;
        resultsTitle.color = Pal.BlueLight;
        resultsGrade.Text = "";
        resultsGradeLabel.SetActive(false);
        resultsIcon.enabled = true;
        resultsReason.Text = reason;
        resultsLabels.Text = wave > 0 ? "TEMPO\nONDA\nFRAGMENTOS\nPONTOS" : "TEMPO\nFRAGMENTOS\nPONTOS";
        resultsValues.Text = wave > 0
            ? $"{Settings.FormatTime(time)}\n{wave}\n{fragments}/{total}\n{score}"
            : $"{Settings.FormatTime(time)}\n{fragments}/{total}\n{score}";
        resultsMenu.items[0].label.Text = "TENTAR DE NOVO";
        OpenResults();
    }

    public void ShowVictory(float time, int baseScore, int heatBonus, int lifeBonus, int total, string grade, bool record)
    {
        resultsTitle.Text = "FOGUEIRA ACESA!";
        resultsTitle.color = Pal.Yellow;
        resultsMenu.items[0].label.Text = "JOGAR DE NOVO";
        StartCoroutine(CountUp(time, baseScore, heatBonus, lifeBonus, total, grade, record));
    }

    IEnumerator CountUp(float time, int baseScore, int heatBonus, int lifeBonus, int total, string grade, bool record)
    {
        resultsGrade.Text = "";
        resultsGradeLabel.SetActive(true);
        resultsIcon.enabled = false;
        resultsReason.Text = record ? "<#f8cf4e>NOVO RECORDE DE TEMPO!</>" : $"Melhor tempo: {Settings.FormatTime(Settings.BestTime)}";
        resultsLabels.Text = "";
        resultsValues.Text = "";
        OpenResults();
        resultsMenu.enabled = false;
        string[] labels = { "TEMPO", "PONTOS", "BÔNUS DE CALOR", "BÔNUS DE VIDA", "<#f8cf4e>TOTAL</>" };
        string[] values = { Settings.FormatTime(time), baseScore.ToString(), $"+{heatBonus}", $"+{lifeBonus}", $"<#f8cf4e>{total}</>" };
        for (int i = 0; i < labels.Length; i++)
        {
            yield return new WaitForSecondsRealtime(0.28f);
            resultsLabels.Text += (i > 0 ? "\n" : "") + labels[i];
            resultsValues.Text += (i > 0 ? "\n" : "") + values[i];
            AudioManager.Play(Sfx.UiMove);
            cardPop.Kick(1.2f);
        }
        yield return new WaitForSecondsRealtime(0.4f);
        resultsGrade.Text = grade;
        resultsGrade.color = grade == "S" ? Pal.Yellow : grade == "A" ? Pal.Green : grade == "B" ? Pal.Blue : Pal.Fog;
        gradePop.Snap(0f);
        gradePop.target = 1f;
        gradePop.Kick(18f);
        AudioManager.Play(Sfx.Fragment);
        if (Fx.Instance != null)
        {
            Fx.Instance.AddPulse(1f);
            Fx.Instance.AddFlash(0.08f);
        }
        yield return new WaitForSecondsRealtime(0.3f);
        resultsMenu.enabled = true;
    }

    void OpenResults()
    {
        banner.Text = "";
        bannerTimer = 0f;
        dimTarget = 0.6f;
        resultsPanel.SetActive(true);
        resultsMenu.index = 0;
        cardPop.Snap(0.8f);
        cardPop.target = 1f;
        cardPop.Kick(4f);
    }

    // ------------------------------------------------------------ botoes
    public void OnResume() => GameManager.Instance.Resume();
    public void OnRestart() => GameManager.Instance.Restart();
    public void OnMenu() => GameManager.Instance.GoToMenu();

    public void OnOptions()
    {
        pausePanel.SetActive(false);
        options.Open(() => pausePanel.SetActive(true));
    }
}
