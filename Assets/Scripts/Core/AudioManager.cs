using UnityEngine;

public enum Sfx { Pickup, Fragment, Hurt, Dash, Bump, Unlock, Ignite, Win, Ko, UiMove, UiConfirm, UiBack, UiDeny, Warning, Alert, Frost }

/// <summary>
/// Efeitos com pitch por toque (a escada pentatonica dos carvoes) e trilha em camadas:
/// a base toca sempre, a bateria entra com a partida e a melodia quando a tensao sobe.
/// Pausa e nocaute abafam a musica.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Trilha (todas as camadas do mesmo tamanho)")]
    public AudioClip musicBase;
    public AudioClip musicDrums;
    public AudioClip musicLead;
    [Range(0, 1)] public float musicVolume = 0.55f;

    [Header("Efeitos, na ordem do enum Sfx")]
    public AudioClip[] clips = new AudioClip[16];

    /// <summary>Re menor pentatonica: cada carvao seguido sobe um degrau.</summary>
    static readonly int[] Ladder = { 0, 3, 5, 7, 10, 12, 15, 17, 19, 22, 24, 27 };

    AudioSource[] layers;
    AudioSource[] pool;
    int next;
    readonly float[] lastPlayed = new float[16];
    readonly float[] levels = { 1f, 0f, 0f };
    float drumsTarget, leadTarget, duck = 1f, duckTarget = 1f;

    void Awake()
    {
        Instance = this;
        pool = new AudioSource[12];
        for (int i = 0; i < pool.Length; i++)
        {
            pool[i] = gameObject.AddComponent<AudioSource>();
            pool[i].playOnAwake = false;
        }

        var stems = new[] { musicBase, musicDrums, musicLead };
        layers = new AudioSource[stems.Length];
        double start = AudioSettings.dspTime + 0.1;
        for (int i = 0; i < stems.Length; i++)
        {
            if (stems[i] == null) continue;
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = stems[i];
            s.loop = true;
            s.playOnAwake = false;
            s.volume = i == 0 ? 1f : 0f;
            s.PlayScheduled(start);
            layers[i] = s;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        duck = Ease.Approach(duck, duckTarget, 6f, dt);
        float vol = musicVolume * Settings.Music * duck;
        // camadas entram e saem devagar; o abafar e mais rapido
        levels[1] = Ease.Approach(levels[1], drumsTarget, 1.5f, dt);
        levels[2] = Ease.Approach(levels[2], leadTarget, 1.5f, dt);
        for (int i = 0; i < layers.Length; i++)
            if (layers[i] != null) layers[i].volume = vol * levels[i];
    }

    /// <summary>Liga/desliga as camadas da trilha do jogo (0..1).</summary>
    public static void SetLayers(float drums, float lead)
    {
        if (Instance == null) return;
        Instance.drumsTarget = drums;
        Instance.leadTarget = lead;
    }

    /// <summary>Abafa a trilha (pausa, nocaute). 1 = normal.</summary>
    public static void Duck(float amount)
    {
        if (Instance != null) Instance.duckTarget = amount;
    }

    public static void StopMusic()
    {
        if (Instance == null) return;
        foreach (var l in Instance.layers) if (l != null) l.Stop();
    }

    public static void Play(Sfx sfx, float volume = 1f, float pitch = 1f)
    {
        var a = Instance;
        if (a == null) return;
        int id = (int)sfx;
        if (id >= a.clips.Length || a.clips[id] == null) return;
        // o mesmo som duas vezes no mesmo instante so soma volume
        if (Time.unscaledTime - a.lastPlayed[id] < 0.03f) return;
        a.lastPlayed[id] = Time.unscaledTime;

        var src = a.pool[a.next];
        a.next = (a.next + 1) % a.pool.Length;
        src.pitch = pitch;
        src.PlayOneShot(a.clips[id], volume * Settings.Sfx);
    }

    /// <summary>Toca um som subindo a escada: step 0 e a nota base.</summary>
    public static void PlayLadder(Sfx sfx, int step, float volume = 1f)
    {
        int semis = Ladder[Mathf.Clamp(step, 0, Ladder.Length - 1)];
        Play(sfx, volume, Mathf.Min(3f, Mathf.Pow(2f, semis / 12f) * 0.6f));
    }
}
