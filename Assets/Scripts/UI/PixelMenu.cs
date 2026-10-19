using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Lista de botoes navegavel por teclado, controle e mouse (como o Menu do Rebound). Cada
/// botao tem mola propria: cresce quando ganha foco e afunda quando e apertado.
/// </summary>
public class PixelMenu : MonoBehaviour
{
    public List<PixelMenuItem> items = new List<PixelMenuItem>();
    public UnityEvent onBack = new UnityEvent();
    public int index;

    readonly List<Spring> focus = new List<Spring>();
    readonly List<Spring> press = new List<Spring>();
    float enabledAt;

    void OnEnable()
    {
        enabledAt = Time.unscaledTime;
        Sync();
        for (int i = 0; i < items.Count; i++)
        {
            focus[i].Snap(i == index ? 1f : 0f);
            press[i].Snap(0f);
        }
        Draw();
    }

    void Sync()
    {
        while (focus.Count < items.Count)
        {
            focus.Add(new Spring(0f, 380f, 22f));
            press.Add(new Spring(0f, 520f, 20f));
        }
        for (int i = 0; i < items.Count; i++) items[i].menu = this;
        index = Mathf.Clamp(index, 0, Mathf.Max(0, items.Count - 1));
        if (items.Count > 0 && !items[index].gameObject.activeSelf) Move(1, false);
    }

    /// <summary>Foca um item (usado pelo mouse).</summary>
    public void Hover(PixelMenuItem item)
    {
        int i = items.IndexOf(item);
        if (i < 0 || i == index || !Ready) return;
        index = i;
        AudioManager.Play(Sfx.UiMove);
    }

    public void Click(PixelMenuItem item, bool leftHalf)
    {
        int i = items.IndexOf(item);
        if (i < 0 || !Ready) return;
        index = i;
        if (item.HasNoAction && item.adjust != null)
        {
            item.adjust(leftHalf ? -1 : 1);
            AudioManager.Play(Sfx.UiMove);
            press[i].Kick(26f);
        }
        else Activate(i);
    }

    bool Ready => Time.unscaledTime - enabledAt > 0.15f;

    void Move(int dir, bool sound = true)
    {
        int n = items.Count;
        for (int k = 1; k <= n; k++)
        {
            int next = ((index + dir * k) % n + n) % n;
            if (items[next].gameObject.activeSelf)
            {
                if (next != index && sound) AudioManager.Play(Sfx.UiMove);
                index = next;
                return;
            }
        }
    }

    void Activate(int i)
    {
        var item = items[i];
        press[i].Kick(26f);
        if (!item.HasNoAction)
        {
            AudioManager.Play(Sfx.UiConfirm);
            item.onConfirm.Invoke();
        }
        else if (item.adjust != null)
        {
            AudioManager.Play(Sfx.UiMove);
            item.adjust(1);
        }
    }

    void Update()
    {
        Sync();
        if (Ready && items.Count > 0)
        {
            var dir = GameInput.MenuDirection;
            if (dir.y > 0) Move(-1);
            if (dir.y < 0) Move(1);
            var item = items[index];
            if (item.adjust != null && dir.x != 0)
            {
                item.adjust(dir.x);
                AudioManager.Play(Sfx.UiMove);
            }
            if (GameInput.ConfirmPressed) Activate(index);
            else if (GameInput.BackPressed && onBack.GetPersistentEventCount() + (onBackRuntime != null ? 1 : 0) > 0)
            {
                AudioManager.Play(Sfx.UiBack);
                onBack.Invoke();
                onBackRuntime?.Invoke();
            }
        }

        float dt = Time.unscaledDeltaTime;
        for (int i = 0; i < items.Count; i++)
        {
            focus[i].target = i == index ? 1f : 0f;
            focus[i].Update(dt);
            press[i].Update(dt);
        }
        Draw();
    }

    /// <summary>Acao de voltar definida por codigo (alem do onBack do Inspector).</summary>
    public System.Action onBackRuntime;

    void Draw()
    {
        for (int i = 0; i < items.Count; i++)
            items[i].Draw(focus[i].value, Mathf.Clamp(press[i].value, 0f, 3f));
    }
}
