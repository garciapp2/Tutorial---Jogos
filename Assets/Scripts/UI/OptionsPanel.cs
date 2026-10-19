using UnityEngine;

/// <summary>Opcoes: volumes, tremor, claroes e a tela de fliperama. Esquerda/direita mudam o valor.</summary>
public class OptionsPanel : MonoBehaviour
{
    public PixelMenu menu;
    public PixelMenuItem music, sfx, shake, flashes, crt, back;

    System.Action onClose;

    void Awake()
    {
        Bind(music, () => Percent(Settings.Music), d => Settings.Music = Step(Settings.Music, d, 0.1f));
        Bind(sfx, () => Percent(Settings.Sfx), d =>
        {
            Settings.Sfx = Step(Settings.Sfx, d, 0.1f);
            AudioManager.Play(Sfx.Pickup);
        });
        Bind(shake, () => Percent(Settings.Shake), d =>
        {
            Settings.Shake = Step(Settings.Shake, d, 0.25f);
            if (Fx.Instance != null) Fx.Instance.AddTrauma(0.4f);
        });
        Bind(flashes, () => Settings.Flashes ? "LIGADOS" : "DESLIGADOS", d => Settings.Flashes = !Settings.Flashes);
        Bind(crt, () => Settings.Crt ? "LIGADA" : "DESLIGADA", d => Settings.Crt = !Settings.Crt);
        back.runtimeConfirm = true;
        back.onConfirm.AddListener(Close);
        menu.onBackRuntime = Close;
    }

    static void Bind(PixelMenuItem item, System.Func<string> value, System.Action<int> adjust)
    {
        item.value = value;
        item.adjust = adjust;
    }

    static string Percent(float v) => $"{Mathf.RoundToInt(v * 100f)}%";
    static float Step(float v, int dir, float step) => Mathf.Clamp01(Mathf.Round((v + dir * step) / step) * step);

    public void Open(System.Action closed)
    {
        onClose = closed;
        menu.index = 0;
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        onClose?.Invoke();
    }
}
