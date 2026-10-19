using System.Collections;
using UnityEngine;

/// <summary>
/// Regras da partida. O calor da brasa e um cronometro regressivo: quando zera, a brasa
/// apaga. Carvoes e fragmentos devolvem calor, o dash gasta e o gelo do chao drena mais rapido.
/// Um segundo cronometro conta o tempo total, que aparece no fim com a nota da partida.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum State { Ready, Playing, Paused, Won, Lost }

    public static GameManager Instance { get; private set; }
    public static bool IsPaused => Instance != null && Instance.CurrentState == State.Paused;

    public const string MenuScene = "MainMenu";
    public const string GameScene = "Game";

    [Header("Tempo (calor)")]
    public float startHeat = 30f;
    public float maxHeat = 45f;
    public float coalHeat = 3f;
    public float fragmentHeat = 5f;
    public float lowHeat = 10f;
    public float frostDrain = 2.5f;

    [Header("Pontuacao")]
    public int coalScore = 100;
    public int comboBonus = 25;
    public int fragmentScore = 500;
    public int heatBonusPerSecond = 20;
    public int healthBonus = 300;
    [Tooltip("Tempo de referencia da nota (segundos).")]
    public float parTime = 120f;

    [Header("Referencias")]
    public PlayerHealth player;
    public ExitBonfire exit;
    public HUD hud;
    public Backdrop backdrop;

    public State CurrentState { get; private set; } = State.Ready;
    public bool IsPlaying => CurrentState == State.Playing;
    public float Heat { get; private set; }
    public float Heat01 => Mathf.Clamp01(Heat / maxHeat);
    public float Elapsed { get; private set; }
    public int Score { get; private set; }
    public int Fragments { get; private set; }
    public int TotalFragments { get; private set; }
    public bool ExitUnlocked => TotalFragments > 0 && Fragments >= TotalFragments;
    public bool InFrostZone => frostZones > 0;
    /// <summary>Calor que o dash gastou por ultimo (a barra mostra o atraso em branco).</summary>
    public float LastSpent { get; private set; }

    int frostZones;
    int combo;
    float comboTimer;
    float heartbeat;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        Heat = startHeat;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    IEnumerator Start()
    {
        foreach (var p in FindObjectsByType<Pickup>())
            if (p.type == Pickup.Kind.Fragment) TotalFragments++;
        hud.Refresh(true);
        hud.Banner($"ACHE OS {TotalFragments} FRAGMENTOS", Pal.Yellow, 1.6f);
        AudioManager.SetLayers(0f, 0f);
        yield return new WaitForSeconds(1.6f);
        hud.Banner("VAI!", Pal.White, 0.6f, 4);
        if (Fx.Instance != null) Fx.Instance.AddPulse(0.6f);
        AudioManager.Play(Sfx.UiConfirm);
        CurrentState = State.Playing;
    }

    void Update()
    {
        if (GameInput.PausePressed && !hud.OptionsOpen)
        {
            if (CurrentState == State.Playing) Pause();
            else if (CurrentState == State.Paused) Resume();
        }

        UpdateMood();
        if (CurrentState != State.Playing) return;

        float dt = Time.deltaTime;
        Elapsed += dt;
        Heat -= dt * (InFrostZone ? frostDrain : 1f);

        comboTimer -= dt;
        if (comboTimer <= 0f) combo = 0;

        if (Heat <= lowHeat)
        {
            heartbeat -= dt;
            if (heartbeat <= 0f)
            {
                heartbeat = 0.45f + Heat / lowHeat * 0.5f;
                AudioManager.Play(Sfx.Warning, 0.9f);
                if (Fx.Instance != null) Fx.Instance.AddPulse(0.15f);
            }
        }

        if (Heat <= 0f)
        {
            Heat = 0f;
            StartCoroutine(Lose("A BRASA APAGOU", "O calor acabou. Carvões mantêm a chama acesa."));
        }
    }

    /// <summary>Clima de tela e de musica conforme o calor e o perigo.</summary>
    void UpdateMood()
    {
        float cold = Mathf.Clamp01(1f - Heat / 20f);
        if (Fx.Instance != null) Fx.Instance.Frost = Mathf.Clamp01(cold + (InFrostZone ? 0.35f : 0f));
        if (backdrop != null)
        {
            backdrop.coldness = Ease.Approach(backdrop.coldness, 1f - Heat01, 2f, Time.unscaledDeltaTime);
            backdrop.dim = Ease.Approach(backdrop.dim, CurrentState == State.Paused ? 0.5f : 0f, 8f, Time.unscaledDeltaTime);
        }
        if (CurrentState == State.Playing)
        {
            bool tense = Heat <= lowHeat + 4f || HunterEnemy.Chasing > 0 || ExitUnlocked;
            AudioManager.SetLayers(1f, tense ? 1f : 0f);
        }
        hud.Refresh(false);
    }

    // ------------------------------------------------------------- calor
    public void AddHeat(float amount) => Heat = Mathf.Min(maxHeat, Heat + amount);

    public bool TrySpendHeat(float amount)
    {
        if (!IsPlaying || Heat <= amount + 0.5f) return false;
        Heat -= amount;
        LastSpent = amount;
        hud.HeatSpent();
        return true;
    }

    public void EnterFrostZone() => frostZones++;
    public void ExitFrostZone() => frostZones = Mathf.Max(0, frostZones - 1);

    // ------------------------------------------------------------- coleta
    public void Collect(Pickup pickup)
    {
        if (!IsPlaying) return;
        Vector2 at = pickup.transform.position;
        var fx = Fx.Instance;

        if (pickup.type == Pickup.Kind.Coal)
        {
            int bonus = combo * comboBonus;
            Score += coalScore + bonus;
            AddHeat(coalHeat);
            AudioManager.PlayLadder(Sfx.Pickup, combo);
            hud.Popup($"+{coalHeat:0}s", at, Pal.Orange, 1);
            if (combo > 0) hud.Popup($"x{combo + 1}", at + Vector2.up * 0.7f, Pal.Yellow, 1);
            combo = Mathf.Min(combo + 1, 11);
            comboTimer = 1.8f;
            if (fx != null)
            {
                fx.Light(at, new[] { Pal.Orange, Pal.Yellow, Pal.White });
                fx.Sparks(at, Vector2.up, 6, new[] { Pal.Yellow, Pal.Orange }, 140f, 0.6f);
            }
            hud.HeatGained();
        }
        else
        {
            Fragments++;
            Score += fragmentScore;
            AddHeat(fragmentHeat);
            AudioManager.Play(Sfx.Fragment);
            hud.Popup($"FRAGMENTO {Fragments}/{TotalFragments}", at, Pal.Yellow, 2);
            if (fx != null)
            {
                fx.Stop(0.06f);
                fx.AddTrauma(0.3f);
                fx.AddPulse(0.7f);
                fx.zoom.Kick(0.9f);
                fx.Ring(at, Pal.Yellow, 46f);
                fx.Burst(at, 18, new[] { Pal.Red, Pal.Orange, Pal.Yellow, Pal.White }, 160f, 0.6f, 2f);
            }
            hud.FragmentGained();
            var pv = player.GetComponent<PlayerVisual>();
            if (pv != null) pv.FragmentGained();
            hud.HeatGained();

            if (ExitUnlocked)
            {
                StartCoroutine(Unlocked());
            }
        }
        hud.Refresh(true);
    }

    IEnumerator Unlocked()
    {
        yield return new WaitForSeconds(0.35f);
        AudioManager.Play(Sfx.Unlock);
        if (exit != null) exit.SetReady();
        hud.Banner("VOLTE À FOGUEIRA!", Pal.Orange, 2.2f);
        if (Fx.Instance != null)
        {
            Fx.Instance.AddPulse(1f);
            Fx.Instance.AddFlash(0.15f);
        }
    }

    public void OnPlayerDamaged(int healthLeft, Vector2 at)
    {
        hud.HeartLost(healthLeft);
        hud.Popup("-1", at, Pal.Red, 2);
        if (healthLeft <= 0) StartCoroutine(Lose("VOCÊ CONGELOU", "Os espíritos do gelo apagaram sua chama."));
    }

    public void TouchedExit()
    {
        if (!IsPlaying) return;
        if (!ExitUnlocked)
        {
            int left = TotalFragments - Fragments;
            hud.Banner(left == 1 ? "FALTA 1 FRAGMENTO" : $"FALTAM {left} FRAGMENTOS", Pal.BlueLight, 1.4f, 2);
            AudioManager.Play(Sfx.UiDeny);
            return;
        }
        StartCoroutine(Win());
    }

    // ------------------------------------------------------------- fim de jogo
    IEnumerator Win()
    {
        CurrentState = State.Won;
        AudioManager.StopMusic();
        AudioManager.Play(Sfx.Ignite);
        var fx = Fx.Instance;
        Vector2 at = exit.transform.position;
        exit.Ignite();
        if (fx != null)
        {
            fx.Stop(0.2f);
            fx.AddFlash(0.5f);
            fx.AddTrauma(0.7f);
            fx.AddPulse(1.4f);
            fx.focus = at;
            fx.zoom.target = 1.25f;
            fx.Ring(at, Pal.Yellow, 70f, 0.5f, 36);
            fx.Burst(at, 40, new[] { Pal.Red, Pal.Orange, Pal.Yellow, Pal.White }, 220f, 0.9f, 2f, -60f);
        }
        yield return new WaitForSecondsRealtime(0.3f);
        AudioManager.Play(Sfx.Win);
        if (fx != null) fx.Slow(0.35f, 0.9f);
        hud.Banner("FOGUEIRA ACESA!", Pal.Yellow, 99f, 3);
        yield return new WaitForSecondsRealtime(1.5f);

        int heatBonus = Mathf.CeilToInt(Heat) * heatBonusPerSecond;
        int lifeBonus = player.Current * healthBonus;
        int baseScore = Score;
        Score += heatBonus + lifeBonus;
        string grade = Grade();
        bool record = Settings.SubmitWin(Elapsed, Score, grade);
        hud.ShowVictory(Elapsed, baseScore, heatBonus, lifeBonus, Score, grade, record);
    }

    /// <summary>S, A, B ou C: 60% pela vida guardada, 40% pela pressa.</summary>
    string Grade()
    {
        float life = player.Current / (float)player.maxHealth;
        float speed = Mathf.Clamp01((parTime * 1.6f - Elapsed) / parTime);
        float k = life * 0.6f + speed * 0.4f;
        return k >= 0.85f ? "S" : k >= 0.65f ? "A" : k >= 0.4f ? "B" : "C";
    }

    IEnumerator Lose(string title, string reason)
    {
        if (CurrentState == State.Lost || CurrentState == State.Won) yield break;
        CurrentState = State.Lost;
        var fx = Fx.Instance;
        Vector2 at = player.transform.position;
        AudioManager.Duck(0.25f);
        AudioManager.Play(Sfx.Ko);

        // nocaute do Rebound: o quadro congela com a brasa em branco, depois ela estilhaca em camera lenta
        player.GetComponent<PlayerVisual>()?.Freeze(true);
        if (fx != null)
        {
            fx.Stop(Fx.Tune.StopKo);
            fx.AddTrauma(Fx.Tune.ShakeKo);
            fx.AddFlash(0.5f);
            fx.AddChroma(0.016f);
            fx.focus = at;
            fx.zoom.target = 1.32f;
        }
        yield return new WaitForSecondsRealtime(Fx.Tune.StopKo);
        player.GetComponent<PlayerVisual>()?.Shatter();
        if (fx != null)
        {
            fx.Slow(0.1f, 1.05f);
            fx.Burst(at, 46, new[] { Pal.Red, Pal.RedDark, Pal.Orange, Pal.Yellow, Pal.Ink }, 260f, 1.4f, 2f, 140f, true);
            fx.Ring(at, Pal.BlueLight, 60f, 0.5f, 32);
        }
        yield return new WaitForSecondsRealtime(0.7f);
        hud.Banner(title, Pal.BlueLight, 99f, 3);
        yield return new WaitForSecondsRealtime(1.1f);
        if (fx != null) fx.zoom.target = 1f;
        AudioManager.StopMusic();
        hud.ShowGameOver(title, reason, Elapsed, Score, Fragments, TotalFragments);
    }

    public void Pause()
    {
        if (CurrentState != State.Playing) return;
        CurrentState = State.Paused;
        Time.timeScale = 0f;
        AudioManager.Duck(0.35f);
        hud.ShowPause(true);
    }

    public void Resume()
    {
        if (CurrentState != State.Paused) return;
        CurrentState = State.Playing;
        Time.timeScale = 1f;
        AudioManager.Duck(1f);
        hud.ShowPause(false);
    }

    public void Restart() => Transition.Load(GameScene);
    public void GoToMenu() => Transition.Load(MenuScene);
}
