using UnityEngine;

/// <summary>
/// Movimento do jogador (base do tutorial): le a entrada no Update e aplica no Rigidbody2D
/// dentro do FixedUpdate, com velocidade publica ajustavel no Inspector.
/// Extra, no estilo do Celeste: dash em 8 direcoes com uma carga, que volta sozinha depois de
/// um instante ou na hora ao pegar um carvao. No dash a brasa e intocavel, quebra cristais,
/// atordoa inimigos e passa por cima dos abismos.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Tooltip("Velocidade de movimento em unidades por segundo.")]
    public float speed = 6f;
    public float acceleration = 70f;

    [Header("Dash")]
    public float dashSpeed = 19f;
    public float dashDuration = 0.14f;
    public int maxDashes = 1;
    [Tooltip("Tempo para a carga do dash voltar sozinha.")]
    public float refillTime = 0.6f;
    [Tooltip("Segundos de calor gastos a cada dash.")]
    public float dashHeatCost = 1f;

    Rigidbody2D rb;
    Vector2 input;
    Vector2 lastDirection = Vector2.up;
    bool dashQueued;
    float dashTimer, refillTimer;
    int dashes;
    Vector2 dashDirection;
    Vector2 knockback;
    float knockbackTimer;
    int pitContacts;
    float fallTimer, graceTimer, lockTimer;
    Vector2 lastSafe;
    readonly Collider2D[] near = new Collider2D[8];
    ContactFilter2D triggers;

    /// <summary>So para gravar as demonstracoes da pagina do jogo: entrada no lugar do jogador.</summary>
    [System.NonSerialized] public Vector2? demoInput;
    bool demoDash;
    public void DemoDash() => demoDash = true;

    public bool IsDashing => dashTimer > 0f;
    public bool IsFalling => fallTimer > 0f;
    public bool HasDash => dashes > 0;
    public bool OverPit => pitContacts > 0;
    public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;
    public event System.Action<Vector2> Dashed;
    public event System.Action Refilled;
    public event System.Action Fell;
    public event System.Action<Vector2, float> Bumped;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        dashes = maxDashes;
        lastSafe = rb.position;
        triggers = new ContactFilter2D { useTriggers = true };
    }

    /// <summary>Chao firme de verdade: nenhum abismo a menos de uma casa (para voltar longe da beira).</summary>
    bool FarFromPits()
    {
        int n = Physics2D.OverlapCircle(rb.position, 1.1f, triggers, near);
        for (int i = 0; i < n; i++) if (near[i].GetComponent<Pit>() != null) return false;
        return true;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if ((gm != null && !gm.IsPlaying) || IsFalling || lockTimer > 0f)
        {
            input = Vector2.zero;
            return;
        }

        input = demoInput ?? GameInput.Move;
        if (input.sqrMagnitude > 0.09f) lastDirection = Snap8(input);

        bool dashPressed = GameInput.DashPressed || demoDash;
        demoDash = false;
        if (dashPressed && dashes > 0 && dashTimer <= 0f)
        {
            if (gm == null || gm.TrySpendHeat(dashHeatCost)) dashQueued = true;
            else AudioManager.Play(Sfx.UiDeny, 0.6f);
        }
    }

    /// <summary>Direcao do dash presa a 8 direcoes, como no Celeste.</summary>
    static Vector2 Snap8(Vector2 v)
    {
        float a = Mathf.Round(Mathf.Atan2(v.y, v.x) / (Mathf.PI / 4f)) * (Mathf.PI / 4f);
        return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        var gm = GameManager.Instance;

        if (fallTimer > 0f)
        {
            rb.linearVelocity = Vector2.zero;
            fallTimer -= dt;
            if (fallTimer <= 0f)
            {
                // volta para o ultimo chao firme, longe da beira, com um respiro antes de valer de novo
                rb.position = lastSafe;
                graceTimer = 0.5f;
                lockTimer = 0.25f;
            }
            return;
        }

        if (gm != null && !gm.IsPlaying)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, acceleration * dt);
            return;
        }

        graceTimer -= dt;
        lockTimer -= dt;
        if (dashes < maxDashes && dashTimer <= 0f)
        {
            refillTimer -= dt;
            if (refillTimer <= 0f) RefillDash(false);
        }

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= dt;
            rb.linearVelocity = knockback;
            return;
        }

        if (dashQueued)
        {
            dashQueued = false;
            dashes--;
            dashTimer = dashDuration;
            refillTimer = refillTime;
            dashDirection = lastDirection;
            AudioManager.Play(Sfx.Dash);
            Dashed?.Invoke(dashDirection);
        }

        if (dashTimer > 0f)
        {
            dashTimer -= dt;
            rb.linearVelocity = dashDirection * dashSpeed;
            return;
        }

        // caiu: parou em cima do abismo sem dash
        if (OverPit && graceTimer <= 0f)
        {
            fallTimer = 0.45f;
            rb.linearVelocity = Vector2.zero;
            Fell?.Invoke();
            return;
        }
        if (!OverPit && FarFromPits()) lastSafe = rb.position;

        Vector2 target = input * speed;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, acceleration * dt);
    }

    /// <summary>Devolve a carga do dash (carvao, ou o tempo de recarga acabou).</summary>
    public void RefillDash(bool instant = true)
    {
        if (dashes >= maxDashes) return;
        dashes = maxDashes;
        Refilled?.Invoke();
        if (instant) AudioManager.Play(Sfx.Frost, 0.5f, 1.4f);
    }

    public void EnterPit() => pitContacts++;
    public void ExitPit() => pitContacts = Mathf.Max(0, pitContacts - 1);

    void OnCollisionEnter2D(Collision2D c)
    {
        float impact = Vector2.Dot(c.relativeVelocity, c.GetContact(0).normal);
        if (Mathf.Abs(impact) > 2.5f) Bumped?.Invoke(c.GetContact(0).normal, Mathf.Abs(impact));
    }

    public void ApplyKnockback(Vector2 velocity, float duration)
    {
        dashTimer = 0f;
        knockback = velocity;
        knockbackTimer = duration;
    }
}
