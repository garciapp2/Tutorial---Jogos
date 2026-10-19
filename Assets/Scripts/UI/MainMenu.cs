using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tela de titulo: o nome do jogo letra por letra (o "O" de COLD e a brasa quicando, como a
/// bola do REBOUND), o menu a esquerda e uma demonstracao da cripta rodando a direita.
/// </summary>
public class MainMenu : MonoBehaviour
{
    public RectTransform logoRoot;
    public Sprite emberSprite;
    public PixelMenu menu;
    public PixelMenuItem quitItem;
    public GameObject mainPanel;
    public GameObject howToPanel;
    public PixelMenu howToMenu;
    public OptionsPanel options;
    public PixelText record;
    public PixelText hint;

    const int Scale = 4;

    class Letter { public RectTransform rt, under; public float x, y; public int index; }
    readonly List<Letter> letters = new List<Letter>();
    RectTransform ember, emberShadow;
    Vector2 emberHome;
    float time;

    void Start()
    {
        Time.timeScale = 1f;
#if UNITY_WEBGL
        if (quitItem != null) quitItem.gameObject.SetActive(false);
#endif
        BuildLogo();
        howToPanel.SetActive(false);
        options.gameObject.SetActive(false);
        howToMenu.onBackRuntime = ShowMain;
        float best = Settings.BestTime;
        record.Text = best >= 0f
            ? $"RECORDE  <#fff6e0>{Settings.FormatTime(best)}</>   NOTA  <#f8cf4e>{(Settings.BestGrade == "" ? "-" : Settings.BestGrade)}</>   <#fff6e0>{Settings.BestScore}</> PTS"
            : "NENHUMA FOGUEIRA ACESA AINDA";
    }

    void BuildLogo()
    {
        AddWord("C", 0, Pal.BlueLight, Pal.BlueDark, 0);
        // o "O" e a brasa
        int oW = 5 * Scale + Scale;
        float cx = PixelText.Width("C", Scale) + Scale;
        emberHome = new Vector2(cx + oW / 2f - Scale / 2f, -Scale * 3.5f);
        emberShadow = Img("SombraBrasa", emberSprite, Pal.Alpha(Pal.Ink, 0.4f));
        ember = Img("Brasa", emberSprite, Color.white);
        AddWord("LD", cx + oW, Pal.BlueLight, Pal.BlueDark, 2);
        float emberW = PixelText.Width("EMBER", Scale);
        AddWord("EMBER", -emberW / 2f + PixelText.Width("COLD", Scale) / 2f, Pal.Orange, Pal.RedDark, 4, -Scale * 10);
    }

    void AddWord(string word, float x, Color face, Color under, int startIndex, float y = 0f)
    {
        foreach (char c in word)
        {
            var l = new Letter { x = x, y = y, index = startIndex++ };
            l.under = Glyph(c, under, false);
            l.rt = Glyph(c, face, true);
            letters.Add(l);
            x += PixelText.Width(c.ToString(), Scale) + Scale;
        }
    }

    RectTransform Glyph(char c, Color color, bool shadow)
    {
        var rt = new GameObject(c.ToString(), typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(logoRoot, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(60, 60);
        var t = rt.gameObject.AddComponent<PixelText>();
        t.scale = Scale;
        t.color = color;
        t.shadow = shadow;
        t.Text = c.ToString();
        t.raycastTarget = false;
        return rt;
    }

    RectTransform Img(string name, Sprite sprite, Color color)
    {
        var rt = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rt.SetParent(logoRoot, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        var img = rt.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        rt.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height) * 2f;
        return rt;
    }

    void Update()
    {
        time += Time.unscaledDeltaTime;
        foreach (var l in letters)
        {
            float enter = Ease.OutBack(Mathf.Clamp01((time - l.index * 0.06f) / 0.35f), 2f);
            float bob = Mathf.Sin(time * 2.2f + l.index * 0.55f) * Scale * 0.35f;
            float y = Mathf.Round(l.y + bob + (1f - enter) * 40f);
            l.rt.anchoredPosition = new Vector2(l.x, y);
            l.under.anchoredPosition = new Vector2(l.x, y - Mathf.Round(Scale * 0.7f));
            SetAlpha(l.rt, enter);
            SetAlpha(l.under, enter);
        }

        // a brasa quica no lugar do "O" e amassa ao tocar o chao
        float bounce = Mathf.Abs(Mathf.Sin(time * 3.1f));
        float squash = bounce < 0.12f ? (0.12f - bounce) * 2.2f : 0f;
        float enterO = Ease.OutBack(Mathf.Clamp01((time - 0.06f) / 0.35f), 2f);
        var pos = emberHome + new Vector2(0, bounce * Scale * 2.5f + (1f - enterO) * 40f);
        ember.anchoredPosition = new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));
        ember.localScale = new Vector3(1f + squash, 1f - squash, 1f);
        emberShadow.anchoredPosition = emberHome + new Vector2(3, -4);
        emberShadow.localScale = Vector3.one * (1f - bounce * 0.3f);

        hint.Text = GameInput.UsingGamepad
            ? "<#f8cf4e>ANALÓGICO</> escolhe   <#f8cf4e>A</> confirma   <#f8cf4e>B</> volta"
            : "<#f8cf4e>SETAS</> escolhem   <#f8cf4e>ESPAÇO</> confirma   <#f8cf4e>ESC</> volta";
    }

    static void SetAlpha(RectTransform rt, float a)
    {
        var g = rt.GetComponent<Graphic>();
        var c = g.color;
        c.a = Mathf.Clamp01(a);
        g.color = c;
    }

    // ------------------------------------------------------------ botoes
    public void Play() => Transition.Load(GameManager.GameScene);

    public void ShowHowTo()
    {
        mainPanel.SetActive(false);
        howToPanel.SetActive(true);
    }

    public void ShowOptions()
    {
        mainPanel.SetActive(false);
        options.Open(ShowMain);
    }

    public void ShowMain()
    {
        howToPanel.SetActive(false);
        options.gameObject.SetActive(false);
        mainPanel.SetActive(true);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
