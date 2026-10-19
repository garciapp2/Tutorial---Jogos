using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Painel chapado do Rebound: contorno de tinta com cantos cortados, borda colorida, miolo e
/// sombra dura deslocada. Montado com tres imagens fatiadas e uma lisa, todas em pixel logico.
/// </summary>
public class PixelPanel : MonoBehaviour
{
    public Image shadowImage, ink, border, fill;

    static Sprite notch, pixel;

    static void Load()
    {
        if (notch == null) notch = Resources.Load<Sprite>("ui_notch");
        if (pixel == null) pixel = Resources.Load<Sprite>("ui_pixel");
    }

    /// <summary>Monta o painel dentro de target (que define tamanho e posicao).</summary>
    public static PixelPanel Create(RectTransform target, Color fillColor, Color borderColor, bool withShadow = true)
    {
        Load();
        var panel = target.gameObject.GetComponent<PixelPanel>();
        if (panel == null) panel = target.gameObject.AddComponent<PixelPanel>();
        if (withShadow) panel.shadowImage = Layer(target, "Sombra", notch, Pal.Alpha(Pal.Ink, 0.5f), new Vector2(2, -3), new Vector2(2, -3));
        panel.ink = Layer(target, "Contorno", notch, Pal.Ink, Vector2.zero, Vector2.zero);
        panel.border = Layer(target, "Borda", notch, borderColor, new Vector2(1, 1), new Vector2(-1, -1));
        panel.fill = Layer(target, "Miolo", pixel, fillColor, new Vector2(2, 2), new Vector2(-2, -2));
        // atras de qualquer conteudo do painel, na ordem sombra, contorno, borda, miolo
        int index = 0;
        foreach (var img in new[] { panel.shadowImage, panel.ink, panel.border, panel.fill })
            if (img != null) img.transform.SetSiblingIndex(index++);
        return panel;
    }

    static Image Layer(RectTransform parent, string name, Sprite sprite, Color color, Vector2 min, Vector2 max)
    {
        var rt = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = min;
        rt.offsetMax = max;
        var img = rt.GetComponent<Image>();
        img.sprite = sprite;
        img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        img.pixelsPerUnitMultiplier = 1f;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public void SetColors(Color fillColor, Color borderColor)
    {
        if (fill != null) fill.color = fillColor;
        if (border != null) border.color = borderColor;
    }

    public void SetShadow(bool on)
    {
        if (shadowImage != null) shadowImage.enabled = on;
    }
}
