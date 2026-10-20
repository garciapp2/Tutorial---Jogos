using UnityEngine;

/// <summary>
/// Tudo que a brasa mostra e nao mexe na fisica: amassa e estica com mola, a labareda cresce
/// com o calor e se inclina contra o movimento, solta brasas, deixa copias fantasmas no dash,
/// pisca em branco ao apanhar e aponta a fogueira quando ela abre.
/// </summary>
public class PlayerVisual : MonoBehaviour
{
    public Transform body;
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer whiteFlash;
    [Tooltip("O fogo dentro da bola: cresce a cada fragmento.")]
    public SpriteRenderer flame;
    public SpriteRenderer glass;
    public Transform arrow;
    public Material ghostMaterial;

    Rigidbody2D rb;
    PlayerHealth health;
    PlayerMovement movement;
    readonly Spring sx = new Spring(1f, 520f, 15f);
    readonly Spring sy = new Spring(1f, 520f, 15f);
    readonly Spring core = new Spring(0.6f, 200f, 10f);
    float flashTimer, emberTimer, ghostTimer, dashTrail;
    bool frozen, shattered;
    Vector2 lastVel;

    SpriteRenderer[] ghosts;
    float[] ghostLife;
    int nextGhost;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<PlayerHealth>();
        movement = GetComponent<PlayerMovement>();
        if (whiteFlash != null) whiteFlash.enabled = false;

        ghosts = new SpriteRenderer[8];
        ghostLife = new float[8];
        for (int i = 0; i < ghosts.Length; i++)
        {
            var g = new GameObject("Fantasma").AddComponent<SpriteRenderer>();
            g.sprite = bodyRenderer.sprite;
            g.sortingOrder = bodyRenderer.sortingOrder - 1;
            if (ghostMaterial != null) g.sharedMaterial = ghostMaterial;
            g.enabled = false;
            ghosts[i] = g;
        }
    }

    void OnEnable()
    {
        if (movement != null)
        {
            movement.Dashed += OnDash;
            movement.Bumped += OnBump;
            movement.Refilled += OnRefill;
            movement.Fell += OnFall;
        }
    }

    void OnDisable()
    {
        if (movement != null)
        {
            movement.Dashed -= OnDash;
            movement.Bumped -= OnBump;
            movement.Refilled -= OnRefill;
            movement.Fell -= OnFall;
        }
    }

    void OnDash(Vector2 dir)
    {
        dashTrail = 0.16f;
        // estica na direcao do dash
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) { sx.Kick(9f); sy.Kick(-6f); }
        else { sy.Kick(9f); sx.Kick(-6f); }
        var fx = Fx.Instance;
        if (fx == null) return;
        fx.Kick(-dir, Fx.Tune.KickLight * 0.6f);
        fx.AddTrauma(0.08f);
        fx.Sparks(transform.position, -dir, 10, new[] { Pal.Orange, Pal.Yellow, Pal.Red }, 160f, 0.5f);
    }

    float fallAnim, refillFlash;

    void OnFall() => fallAnim = 0.45f;

    void OnRefill()
    {
        refillFlash = 0.12f;
        sx.Kick(5f);
        sy.Kick(5f);
        if (Fx.Instance != null) Fx.Instance.Ring(transform.position, Pal.White, 18f, 0.25f, 16);
    }

    void OnBump(Vector2 normal, float impact)
    {
        float k = Mathf.Clamp01(impact / 10f);
        if (Mathf.Abs(normal.x) > Mathf.Abs(normal.y)) { sx.Kick(-10f * k); sy.Kick(6f * k); }
        else { sy.Kick(-10f * k); sx.Kick(6f * k); }
        AudioManager.Play(Sfx.Bump, 0.4f + k * 0.6f, 0.9f + Random.value * 0.2f);
        var fx = Fx.Instance;
        if (fx == null) return;
        fx.AddTrauma(0.05f + k * 0.08f);
        fx.Kick(-normal, k * 1.5f);
        fx.Burst((Vector2)transform.position - normal * 0.4f, 4 + (int)(k * 4), new[] { Pal.Bone, Pal.Fog }, 70f, 0.3f, 1f);
    }

    /// <summary>Golpe recebido: clarao branco e amassado.</summary>
    public void Hit(Vector2 dir)
    {
        flashTimer = 0.11f;
        sx.Kick(-12f);
        sy.Kick(12f);
    }

    /// <summary>Fragmento pego: o fogo de dentro da um salto.</summary>
    public void FragmentGained() => core.Kick(6f);

    public void Freeze(bool on)
    {
        frozen = on;
        if (whiteFlash != null) whiteFlash.enabled = on;
    }

    public void Shatter()
    {
        shattered = true;
        Freeze(false);
        bodyRenderer.enabled = false;
        if (glass != null) glass.enabled = false;
        if (flame != null) flame.enabled = false;
        if (arrow != null) arrow.gameObject.SetActive(false);
    }

    void Update()
    {
        if (shattered) return;
        float dt = Time.deltaTime;
        var gm = GameManager.Instance;
        float heat = gm != null ? gm.Heat01 : 0.8f;
        Vector2 vel = rb != null ? rb.linearVelocity : Vector2.zero;

        // mola de amassar: mudancas bruscas de velocidade empurram
        Vector2 dv = vel - lastVel;
        lastVel = vel;
        if (dt > 0f && dv.magnitude > 3f && !(movement != null && movement.IsDashing))
        {
            sx.Kick(-Mathf.Abs(dv.x) * 0.5f + Mathf.Abs(dv.y) * 0.3f);
            sy.Kick(-Mathf.Abs(dv.y) * 0.5f + Mathf.Abs(dv.x) * 0.3f);
        }
        sx.target = sy.target = 1f;
        sx.Update(Time.unscaledDeltaTime);
        sy.Update(Time.unscaledDeltaTime);
        float fall = 1f;
        if (fallAnim > 0f)
        {
            fallAnim -= dt;
            fall = Mathf.Clamp01(fallAnim / 0.45f);
            body.localRotation = Quaternion.Euler(0, 0, (1f - fall) * 270f);
        }
        else body.localRotation = Quaternion.identity;
        body.localScale = new Vector3(Mathf.Clamp(sx.value, 0.6f, 1.5f), Mathf.Clamp(sy.value, 0.6f, 1.5f), 1f) * fall;

        // o fogo de dentro cresce com os fragmentos; com o calor no fim ele encolhe e azula
        if (flame != null)
        {
            float frac = gm != null && gm.TotalFragments > 0 ? gm.Fragments / (float)gm.TotalFragments : 0.4f;
            core.target = Mathf.Lerp(0.6f, 1.05f, frac) * Mathf.Lerp(0.75f, 1f, Mathf.Clamp01(heat * 3f));
            core.Update(Time.unscaledDeltaTime);
            float size = Mathf.Max(0.2f, core.value);
            flame.transform.localScale = new Vector3(size, size * (1f + Mathf.Sin(Time.time * 17f) * 0.06f), 1f);
            flame.transform.localPosition = new Vector3(0f, -0.28f * (1f - size), 0f);
            flame.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Clamp(-vel.x * 2f, -15f, 15f));
            flame.color = Color.Lerp(new Color(0.6f, 0.75f, 1f), Color.white, Mathf.Clamp01(heat * 3f));
        }

        // brasas soltas
        var fx = Fx.Instance;
        emberTimer -= dt;
        if (fx != null && emberTimer <= 0f && dt > 0f)
        {
            emberTimer = Mathf.Lerp(0.35f, 0.06f, heat) * (vel.sqrMagnitude > 1f ? 0.6f : 1f);
            var c = Random.value < 0.5f ? Pal.Orange : Random.value < 0.5f ? Pal.Yellow : Pal.Red;
            fx.Ember((Vector2)transform.position + Vector2.up * 0.45f, heat < 0.2f ? Pal.BlueLight : c, 22f, 0.5f);
        }

        // clarao e piscar
        bool blink = health != null && health.InvulnerableLeft > 0f && Mathf.Repeat(health.InvulnerableLeft, 0.14f) > 0.07f;
        flashTimer -= Time.unscaledDeltaTime;
        if (whiteFlash != null) whiteFlash.enabled = frozen || flashTimer > 0f;
        var tint = bodyRenderer.color;
        tint.a = blink ? 0.35f : 1f;
        bodyRenderer.color = tint;
        if (glass != null)
        {
            // o vidro mostra a carga do dash: claro com carga, apagado sem
            refillFlash -= Time.unscaledDeltaTime;
            bool ready = movement == null || movement.HasDash;
            var g = refillFlash > 0f ? Color.white : ready ? Color.white : new Color(0.45f, 0.5f, 0.75f);
            g.a = tint.a;
            glass.color = g;
            if (whiteFlash != null && refillFlash > 0f) whiteFlash.enabled = true;
        }
        if (flame != null) flame.color = new Color(flame.color.r, flame.color.g, flame.color.b, tint.a);

        UpdateGhosts(dt);
        UpdateArrow(gm);
    }

    void UpdateGhosts(float dt)
    {
        dashTrail -= dt;
        ghostTimer -= dt;
        if (dashTrail > 0f && ghostTimer <= 0f)
        {
            ghostTimer = 0.025f;
            var g = ghosts[nextGhost];
            ghostLife[nextGhost] = 0.25f;
            nextGhost = (nextGhost + 1) % ghosts.Length;
            g.transform.SetPositionAndRotation(body.position, body.rotation);
            g.transform.localScale = body.lossyScale;
            g.enabled = true;
        }
        for (int i = 0; i < ghosts.Length; i++)
        {
            if (!ghosts[i].enabled) continue;
            ghostLife[i] -= dt;
            if (ghostLife[i] <= 0f) { ghosts[i].enabled = false; continue; }
            float k = ghostLife[i] / 0.25f;
            ghosts[i].color = new Color(Pal.Orange.r, Pal.Orange.g, Pal.Orange.b, 0.32f * k);
        }
    }

    void UpdateArrow(GameManager gm)
    {
        if (arrow == null) return;
        bool show = gm != null && gm.IsPlaying && gm.ExitUnlocked && gm.exit != null;
        Vector2 dir = show ? (Vector2)(gm.exit.transform.position - transform.position) : Vector2.zero;
        show &= dir.magnitude > 2.2f;
        arrow.gameObject.SetActive(show);
        if (!show) return;
        dir.Normalize();
        float bob = Mathf.Sin(Time.time * 8f) * 0.08f;
        arrow.position = transform.position + (Vector3)(dir * (1.15f + bob));
        arrow.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
    }
}
