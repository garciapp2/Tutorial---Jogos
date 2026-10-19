using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>Um botao do PixelMenu. Com `value`/`adjust` vira uma linha de opcao (esquerda/direita).</summary>
public class PixelMenuItem : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public RectTransform visual;
    public PixelPanel panel;
    public PixelText label;
    public PixelText valueText;
    public Color color = new Color(0.294f, 0.561f, 0.961f);
    public UnityEvent onConfirm = new UnityEvent();

    /// <summary>Texto do valor mostrado a direita (opcoes).</summary>
    public System.Func<string> value;
    /// <summary>Esquerda/direita mudam o valor.</summary>
    public System.Action<int> adjust;

    [System.NonSerialized] public PixelMenu menu;

    public bool HasNoAction => onConfirm.GetPersistentEventCount() == 0 && runtimeConfirm == false;
    [System.NonSerialized] public bool runtimeConfirm;

    public void OnPointerEnter(PointerEventData e)
    {
        if (menu != null) menu.Hover(this);
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (menu == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out var local);
        menu.Click(this, local.x < ((RectTransform)transform).rect.center.x);
    }

    public void Draw(float focus, float sink)
    {
        if (visual == null) return;
        float grow = focus * 3f;
        visual.offsetMin = new Vector2(Mathf.Round(-grow), Mathf.Round(-grow * 0.3f - sink));
        visual.offsetMax = new Vector2(Mathf.Round(grow), Mathf.Round(grow * 0.3f - sink));

        bool on = focus > 0.5f;
        if (panel != null)
        {
            panel.SetColors(on ? color : Pal.Dusk, on ? Pal.White : Pal.Slate);
            panel.SetShadow(sink < 1.5f);
        }
        if (label != null)
        {
            label.color = on ? Pal.White : Pal.Bone;
            label.shadow = on;
            label.SetVerticesDirty();
        }
        if (valueText != null && value != null)
        {
            string v = value();
            valueText.Text = on && adjust != null ? $"<  {v}  >" : v;
            valueText.color = on ? Pal.Yellow : Pal.Fog;
            valueText.shadow = on;
            valueText.SetVerticesDirty();
        }
    }
}
