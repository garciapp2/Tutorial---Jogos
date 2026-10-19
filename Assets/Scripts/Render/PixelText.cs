using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Texto com a fonte bitmap 5x7 (a do Rebound), desenhado como malha de UI: escala inteira,
/// sombra dura, contorno, quebra de linha, cor por trecho e letras que ondulam ou caem
/// na entrada. Uma unidade do canvas e um pixel logico.
///
/// Cor por trecho: "texto <#f8cf4e>amarelo</> normal".
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class PixelText : MaskableGraphic
{
    public enum Align { Left, Center, Right }

    [TextArea(2, 8)] [SerializeField] string text = "";
    public int scale = 1;
    public Align align = Align.Left;
    public bool middle;
    public bool wrap;
    public bool shadow = true;
    public Color shadowColor = new Color(0.043f, 0.035f, 0.07f, 1f);
    public bool outline;
    public Color outlineColor = new Color(0.043f, 0.035f, 0.07f, 1f);
    [Tooltip("Pixels de onda por letra (0 desliga).")]
    public float wave;
    public float waveSpeed = 2.2f;
    public float waveStep = 0.55f;
    [Tooltip("Letras caem uma a uma ao aparecer.")]
    public bool dropIn;
    public int lineHeight = 10;

    const int Rows = 11, BaseRow = 2;
    static Texture2D atlas;
    static Dictionary<char, Vector2Int> glyphs;
    float enterTime = -1f;

    public string Text
    {
        get => text;
        set
        {
            if (text == value) return;
            text = value;
            SetVerticesDirty();
        }
    }

    public override Texture mainTexture
    {
        get { Load(); return atlas; }
    }

    static void Load()
    {
        if (glyphs != null) return;
        atlas = Resources.Load<Texture2D>("PixelFont");
        glyphs = new Dictionary<char, Vector2Int>();
        var table = Resources.Load<TextAsset>("PixelFont");
        if (table == null) return;
        foreach (var line in table.text.Split('\n'))
        {
            var parts = line.Trim().Split(' ');
            if (parts.Length < 3) continue;
            glyphs[(char)int.Parse(parts[0])] = new Vector2Int(int.Parse(parts[1]), int.Parse(parts[2]));
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (dropIn) Replay();
    }

    /// <summary>Reinicia a animacao de entrada das letras.</summary>
    public void Replay()
    {
        enterTime = Time.unscaledTime;
        SetVerticesDirty();
    }

    void Update()
    {
        if (wave > 0f || (dropIn && Time.unscaledTime - enterTime < 2f)) SetVerticesDirty();
    }

    static Vector2Int Glyph(char c)
    {
        Load();
        if (glyphs.TryGetValue(c, out var g)) return g;
        if (glyphs.TryGetValue(char.ToUpperInvariant(c), out g)) return g;
        return glyphs.TryGetValue('?', out g) ? g : new Vector2Int(0, 3);
    }

    public static int Width(string s, int scale = 1)
    {
        int w = 0;
        foreach (char c in Strip(s)) w += Glyph(c).y + 1;
        return Mathf.Max(0, w - 1) * scale;
    }

    static string Strip(string s)
    {
        if (s.IndexOf('<') < 0) return s;
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '<' && TryTag(s, i, out int end, out _)) { i = end; continue; }
            sb.Append(s[i]);
        }
        return sb.ToString();
    }

    static bool TryTag(string s, int i, out int end, out Color? color)
    {
        color = null;
        end = s.IndexOf('>', i);
        if (end < 0) return false;
        string tag = s.Substring(i + 1, end - i - 1);
        if (tag == "/") return true;
        if (tag.Length == 7 && tag[0] == '#' && ColorUtility.TryParseHtmlString(tag, out var c))
        {
            color = c;
            return true;
        }
        return false;
    }

    struct Ch { public char c; public Color color; }

    List<List<Ch>> Layout(float maxWidth)
    {
        var lines = new List<List<Ch>>();
        var colorStack = color;
        var current = new List<Ch>();
        // primeiro: caracteres com cor
        var all = new List<Ch>();
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '<' && TryTag(text, i, out int end, out var tagColor))
            {
                colorStack = tagColor.HasValue ? new Color(tagColor.Value.r, tagColor.Value.g, tagColor.Value.b, color.a) : color;
                i = end;
                continue;
            }
            all.Add(new Ch { c = text[i], color = colorStack });
        }

        int limit = wrap ? Mathf.FloorToInt(maxWidth / scale) : int.MaxValue;
        int lineW = 0, wordStart = 0;
        foreach (var ch in all)
        {
            if (ch.c == '\n')
            {
                lines.Add(current);
                current = new List<Ch>();
                lineW = 0;
                wordStart = 0;
                continue;
            }
            int gw = Glyph(ch.c).y + 1;
            if (ch.c == ' ') wordStart = current.Count + 1;
            if (lineW + gw - 1 > limit && current.Count > 0 && ch.c != ' ')
            {
                // quebra na ultima palavra
                var carry = new List<Ch>();
                if (wordStart > 0 && wordStart < current.Count)
                {
                    carry.AddRange(current.GetRange(wordStart, current.Count - wordStart));
                    current.RemoveRange(wordStart - 1, current.Count - wordStart + 1);
                }
                lines.Add(current);
                current = carry;
                lineW = 0;
                foreach (var cc in current) lineW += Glyph(cc.c).y + 1;
                wordStart = 0;
            }
            current.Add(ch);
            lineW += gw;
        }
        lines.Add(current);
        return lines;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Load();
        if (atlas == null || string.IsNullOrEmpty(text)) return;

        var r = rectTransform.rect;
        var lines = Layout(r.width);
        int s = Mathf.Max(1, scale);
        float blockH = (lines.Count - 1) * lineHeight * s + 7 * s;
        float capsTop = middle ? Mathf.Round(r.center.y + blockH / 2f) : Mathf.Round(r.yMax);
        float now = Time.unscaledTime;

        // passes: sombra, contorno, face
        var passes = new List<(Vector2 off, Color? tint)>();
        if (shadow)
        {
            if (outline)
                foreach (var o in Ring()) passes.Add((o * s + new Vector2(s, -s), shadowColor));
            else passes.Add((new Vector2(s, -s), shadowColor));
        }
        if (outline) foreach (var o in Ring()) passes.Add((o * s, outlineColor));
        passes.Add((Vector2.zero, null));

        float invW = 1f / atlas.width;
        foreach (var (off, tint) in passes)
        {
            int index = 0;
            for (int li = 0; li < lines.Count; li++)
            {
                var line = lines[li];
                int w = 0;
                foreach (var ch in line) w += Glyph(ch.c).y + 1;
                w = Mathf.Max(0, w - 1) * s;
                float x = align == Align.Left ? r.xMin : align == Align.Center ? r.center.x - w / 2f : r.xMax - w;
                x = Mathf.Round(x);
                float top = capsTop - li * lineHeight * s + BaseRow * s;
                foreach (var ch in line)
                {
                    var g = Glyph(ch.c);
                    float dx = 0f, dy = 0f, alpha = 1f;
                    if (wave > 0f) dy += Mathf.Sin(now * waveSpeed + index * waveStep) * wave * s * 0.35f;
                    if (dropIn && enterTime >= 0f)
                    {
                        float k = Mathf.Clamp01((now - enterTime - index * 0.04f) / 0.35f);
                        dy += (1f - Ease.OutBack(k, 2f)) * 40f;
                        alpha = k;
                    }
                    var c = tint ?? ch.color;
                    c.a *= alpha * (tint.HasValue ? ch.color.a : 1f);
                    if (ch.c != ' ')
                    {
                        float x0 = x + off.x + dx, y1 = top + off.y + dy;
                        Quad(vh, x0, y1 - Rows * s, x0 + g.y * s, y1, g.x * invW, (g.x + g.y) * invW, c);
                    }
                    x += (g.y + 1) * s;
                    index++;
                }
            }
        }
    }

    static IEnumerable<Vector2> Ring()
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (dx != 0 || dy != 0) yield return new Vector2(dx, dy);
    }

    static void Quad(VertexHelper vh, float x0, float y0, float x1, float y1, float u0, float u1, Color c)
    {
        int i = vh.currentVertCount;
        Color32 c32 = c;
        vh.AddVert(new Vector3(x0, y0), c32, new Vector2(u0, 0f));
        vh.AddVert(new Vector3(x0, y1), c32, new Vector2(u0, 1f));
        vh.AddVert(new Vector3(x1, y1), c32, new Vector2(u1, 1f));
        vh.AddVert(new Vector3(x1, y0), c32, new Vector2(u1, 0f));
        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i + 2, i + 3, i);
    }
}
