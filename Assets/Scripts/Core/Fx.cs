using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Tudo que e resposta de impacto e nao mexe nas regras: particulas em pixel, tremor e empurrao
/// de camera, zoom, clarao, franja de cor, pulso do fundo e o controle de tempo (pausa de
/// impacto e camera lenta). Os numeros seguem o tuning do Rebound.
/// </summary>
public class Fx : MonoBehaviour
{
    public static Fx Instance { get; private set; }

    // ------------------------------------------------------------ ajuste
    public static class Tune
    {
        public const float StopLight = 0.035f, StopMedium = 0.075f, StopHeavy = 0.13f, StopKo = 0.4f;
        public const float ShakeLight = 0.2f, ShakeMedium = 0.38f, ShakeHeavy = 0.62f, ShakeKo = 1f;
        public const float MaxOffset = 9f / 16f, MaxAngle = 0.022f, ShakeDecay = 2.2f, ShakeFreq = 30f;
        public const float KickLight = 2.5f, KickMedium = 5f, KickHeavy = 9f;
        public const float FlashDecay = 7f, ChromaDecay = 7f, PulseDecay = 3f;
        /// <summary>Pixels por unidade do mundo: velocidades do Rebound sao em pixels.</summary>
        public const float Px = 1f / 16f;
    }

    [Header("Referencias")]
    public Volume volume;
    public Image flashOverlay;
    public Material particleMaterial;
    [Tooltip("Desliga tremor e clarao (tela de titulo).")]
    public bool calm;

    public float Trauma { get; private set; }
    public float Flash { get; private set; }
    public float Chroma { get; private set; }
    public float Pulse { get; private set; }
    /// <summary>0 = calor cheio, 1 = congelando: puxa a vinheta para o azul.</summary>
    public float Frost { get; set; }

    public readonly Spring zoom = new Spring(1f, 90f, 14f);
    public Vector2 focus;

    readonly Spring kickX = new Spring(0f, 260f, 18f);
    readonly Spring kickY = new Spring(0f, 260f, 18f);
    float hitstop, slowT, slowTarget = 1f, scale = 1f, time;

    ChromaticAberration chroma;
    Vignette vignette;
    LensDistortion lens;

    // ------------------------------------------------------------ particulas
    enum Kind { Square, Streak, Chip }

    struct Particle
    {
        public Kind kind;
        public Vector2 pos, vel;
        public float life, max, size, drag, gravity, rot, spin;
        public Color color;
        public bool fade;
    }

    readonly List<Particle> particles = new List<Particle>(512);
    readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>(512);
    Sprite pixel;
    Transform particleRoot;

    void Awake()
    {
        Instance = this;
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 16f);
        particleRoot = new GameObject("Particulas").transform;

        if (volume != null)
        {
            var profile = volume.profile; // copia propria: nao suja o asset
            profile.TryGet(out chroma);
            profile.TryGet(out vignette);
            profile.TryGet(out lens);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }

    float ShakeScale => calm ? 0f : Settings.Shake;
    float FlashScale => calm || !Settings.Flashes ? 0f : 1f;

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        time += dt;
        bool frozen = hitstop > 0f;
        hitstop = Mathf.Max(0f, hitstop - dt);
        slowT -= dt;
        scale = slowT > 0f ? slowTarget : Ease.Approach(scale, 1f, 9f, dt);

        if (!GameManager.IsPaused) Time.timeScale = frozen ? 0f : scale;

        Trauma = Mathf.Max(0f, Trauma - Tune.ShakeDecay * dt);
        Flash = Mathf.Max(0f, Flash - Tune.FlashDecay * dt * (0.3f + Flash));
        Chroma = Ease.Approach(Chroma, 0f, Tune.ChromaDecay, dt);
        Pulse = Ease.Approach(Pulse, 0f, Tune.PulseDecay, dt);
        kickX.Update(dt);
        kickY.Update(dt);
        zoom.Update(dt);

        // Na pausa de impacto as faiscas continuam, devagar: o golpe "segura" o quadro.
        float fxDt = GameManager.IsPaused ? 0f : dt * (frozen ? 0.3f : scale);
        UpdateParticles(fxDt);
        ApplyPost();
    }

    void ApplyPost()
    {
        bool crt = Settings.Crt;
        if (chroma != null) chroma.intensity.value = Mathf.Clamp01((crt ? 0.04f : 0f) + Chroma * 60f);
        if (lens != null)
        {
            lens.active = crt;
            lens.intensity.value = -0.07f;
            lens.scale.value = 1f;
        }
        if (vignette != null)
        {
            float pulse = Frost > 0.75f ? (Mathf.Sin(time * 7f) * 0.5f + 0.5f) * 0.08f : 0f;
            vignette.intensity.value = (crt ? 0.32f : 0.18f) + Frost * 0.22f + pulse;
            vignette.color.value = Color.Lerp(Color.black, new Color(0.25f, 0.6f, 1f), Frost * 0.9f);
        }
        if (flashOverlay != null)
        {
            var c = Pal.White;
            c.a = Mathf.Clamp01(Flash);
            flashOverlay.color = c;
            flashOverlay.enabled = Flash > 0.001f;
        }
    }

    // ------------------------------------------------------------ camera
    public void AddTrauma(float amount) => Trauma = Mathf.Min(1f, Trauma + amount);

    public void Kick(Vector2 dir, float amount)
    {
        kickX.Kick(dir.x * amount * 22f * Tune.Px);
        kickY.Kick(dir.y * amount * 22f * Tune.Px);
    }

    public void Stop(float seconds) => hitstop = Mathf.Max(hitstop, seconds);

    public void Slow(float timeScale, float seconds)
    {
        slowTarget = timeScale;
        slowT = seconds;
        scale = timeScale;
    }

    public void AddFlash(float amount) => Flash = Mathf.Min(0.85f, Flash + amount * FlashScale);
    public void AddChroma(float amount) => Chroma = Mathf.Min(0.03f, Chroma + amount * FlashScale);
    public void AddPulse(float amount) => Pulse = Mathf.Min(1.4f, Pulse + amount);

    /// <summary>Deslocamento, giro e zoom da camera neste quadro.</summary>
    public (Vector2 offset, float angle, float zoom) Camera()
    {
        float power = Trauma * Trauma * ShakeScale;
        float t = time * Tune.ShakeFreq;
        var offset = new Vector2(kickX.value, kickY.value) * ShakeScale
                     + new Vector2(Ease.SmoothNoise(t, 11), Ease.SmoothNoise(t, 47)) * Tune.MaxOffset * power;
        return (offset, Ease.SmoothNoise(t, 83) * Tune.MaxAngle * power * Mathf.Rad2Deg, zoom.value);
    }

    // ------------------------------------------------------------ emissores (velocidades em px/s)
    /// <summary>Faiscas num leque em volta da direcao dir.</summary>
    public void Sparks(Vector2 pos, Vector2 dir, int count, Color[] colors, float speed = 190f, float spread = 1.1f)
    {
        float baseAngle = Mathf.Atan2(dir.y, dir.x);
        for (int i = 0; i < count; i++)
        {
            float a = baseAngle + (Random.value - 0.5f) * spread * 2f;
            float v = speed * (0.35f + Random.value * 0.9f) * Tune.Px;
            float life = 0.16f + Random.value * 0.26f;
            Add(new Particle
            {
                kind = Kind.Streak, pos = pos, vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * v,
                life = life, max = life, size = Random.value < 0.35f ? 2 : 1, color = colors[i % colors.Length], drag = 4.5f
            });
        }
    }

    /// <summary>Quadradinhos em todas as direcoes.</summary>
    public void Burst(Vector2 pos, int count, Color[] colors, float speed, float life = 0.45f, float size = 2f, float gravity = 0f, bool chips = false)
    {
        for (int i = 0; i < count; i++)
        {
            float a = Random.value * Mathf.PI * 2f;
            float v = speed * (0.25f + Random.value * 0.85f) * Tune.Px;
            float l = life * (0.55f + Random.value * 0.7f);
            Add(new Particle
            {
                kind = chips ? Kind.Chip : Kind.Square, pos = pos, vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * v,
                life = l, max = l, size = Mathf.Max(1f, Mathf.Round(size * (0.6f + Random.value * 0.6f))),
                color = colors[i % colors.Length], drag = 3f, gravity = gravity * Tune.Px,
                spin = chips ? (Random.value - 0.5f) * 20f : 0f, fade = true
            });
        }
    }

    /// <summary>Anel de pontos que se abre a partir de pos.</summary>
    public void Ring(Vector2 pos, Color color, float radiusPx = 40f, float duration = 0.35f, int count = 28)
    {
        for (int i = 0; i < count; i++)
        {
            float a = i / (float)count * Mathf.PI * 2f;
            var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Add(new Particle
            {
                kind = Kind.Square, pos = pos + dir * 4f * Tune.Px, vel = dir * (radiusPx / duration) * 1.6f * Tune.Px,
                life = duration, max = duration, size = 1f, color = color, drag = 3.2f, fade = true
            });
        }
    }

    /// <summary>Uma brasa solta que sobe e apaga (rastro do jogador, fogueira).</summary>
    public void Ember(Vector2 pos, Color color, float rise = 18f, float life = 0.6f)
    {
        Add(new Particle
        {
            kind = Kind.Square, pos = pos + Random.insideUnitCircle * 3f * Tune.Px,
            vel = new Vector2((Random.value - 0.5f) * 10f, rise * (0.6f + Random.value * 0.8f)) * Tune.Px,
            life = life, max = life, size = 1f, color = color, drag = 1.2f, fade = false
        });
    }

    void Add(Particle p)
    {
        if (particles.Count >= 600) return;
        particles.Add(p);
    }

    void UpdateParticles(float dt)
    {
        while (renderers.Count < particles.Count)
        {
            var go = new GameObject("p");
            go.transform.SetParent(particleRoot, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = pixel;
            if (particleMaterial != null) sr.sharedMaterial = particleMaterial;
            sr.sortingOrder = 30;
            renderers.Add(sr);
        }

        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i];
            p.life -= dt;
            if (p.life <= 0f)
            {
                particles[i] = particles[particles.Count - 1];
                particles.RemoveAt(particles.Count - 1);
                continue;
            }
            float drag = Mathf.Exp(-p.drag * dt);
            p.vel *= drag;
            p.vel.y = p.vel.y - p.gravity * dt; // gravidade em px/s^2, para baixo
            p.pos += p.vel * dt;
            p.rot += p.spin * dt;
            particles[i] = p;
        }

        for (int i = 0; i < renderers.Count; i++)
        {
            var sr = renderers[i];
            if (i >= particles.Count)
            {
                if (sr.enabled) sr.enabled = false;
                continue;
            }
            var p = particles[i];
            float k = p.life / p.max;
            sr.enabled = true;
            var t = sr.transform;
            t.position = new Vector3(p.pos.x, p.pos.y, 0f);
            var c = p.color;
            if (p.fade) c.a = Mathf.Clamp01(k * 1.6f);
            sr.color = c;
            if (p.kind == Kind.Streak)
            {
                float len = Mathf.Max(1f, p.vel.magnitude / Tune.Px * 0.035f) * k + 1f;
                t.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(p.vel.y, p.vel.x) * Mathf.Rad2Deg);
                t.localScale = new Vector3(len, p.size, 1f);
            }
            else
            {
                float s = p.kind == Kind.Square && !p.fade ? p.size * Mathf.Ceil(k * 2f) / 2f : p.size;
                t.rotation = Quaternion.Euler(0, 0, p.rot * Mathf.Rad2Deg);
                t.localScale = new Vector3(Mathf.Max(1f, s), Mathf.Max(1f, s), 1f);
            }
        }
    }

    // ------------------------------------------------------------ atalhos de golpe
    /// <summary>Golpe pesado: dano no jogador.</summary>
    public void Heavy(Vector2 at, Vector2 dir)
    {
        Stop(Tune.StopHeavy);
        AddTrauma(Tune.ShakeHeavy);
        Kick(dir, Tune.KickHeavy);
        AddFlash(0.1f);
        AddChroma(0.007f);
        AddPulse(0.9f);
        Sparks(at, dir, 26, new[] { Pal.White, Pal.BlueLight, Pal.TealLight });
        Burst(at, 8, new[] { Pal.Blue, Pal.BlueLight, Pal.White }, 120f, 0.5f, 2f, 160f, true);
    }

    public void Medium(Vector2 at, Vector2 dir, Color[] colors)
    {
        Stop(Tune.StopMedium);
        AddTrauma(Tune.ShakeMedium);
        Kick(dir, Tune.KickMedium);
        AddChroma(0.0025f);
        AddPulse(0.5f);
        Sparks(at, dir, 14, colors);
    }

    public void Light(Vector2 at, Color[] colors)
    {
        Stop(Tune.StopLight);
        AddTrauma(Tune.ShakeLight * 0.6f);
        AddPulse(0.25f);
        Burst(at, 7, colors, 90f, 0.4f, 2f);
    }
}
