using UnityEngine;

/// <summary>
/// Movimento do jogador (base do tutorial): le a entrada no Update e aplica no Rigidbody2D
/// dentro do FixedUpdate, com velocidade publica ajustavel no Inspector.
/// Extra: dash que gasta calor e deixa a brasa intocavel por um instante.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Tooltip("Velocidade de movimento em unidades por segundo.")]
    public float speed = 6f;
    public float acceleration = 70f;

    [Header("Dash")]
    public float dashSpeed = 18f;
    public float dashDuration = 0.14f;
    public float dashCooldown = 0.45f;
    [Tooltip("Segundos de calor gastos a cada dash.")]
    public float dashHeatCost = 1.5f;

    Rigidbody2D rb;
    Vector2 input;
    Vector2 lastDirection = Vector2.up;
    bool dashQueued;
    float dashTimer, cooldownTimer;
    Vector2 dashDirection;
    Vector2 knockback;
    float knockbackTimer;

    public bool IsDashing => dashTimer > 0f;
    public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;
    public Vector2 Facing => lastDirection;
    public event System.Action<Vector2> Dashed;
    public event System.Action<Vector2, float> Bumped;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && !gm.IsPlaying)
        {
            input = Vector2.zero;
            return;
        }

        input = GameInput.Move;
        if (input.sqrMagnitude > 0.04f) lastDirection = input.normalized;

        if (GameInput.DashPressed && cooldownTimer <= 0f && dashTimer <= 0f)
        {
            if (gm == null || gm.TrySpendHeat(dashHeatCost)) dashQueued = true;
            else AudioManager.Play(Sfx.UiDeny, 0.6f);
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        cooldownTimer -= dt;

        var gm = GameManager.Instance;
        if (gm != null && !gm.IsPlaying)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, acceleration * dt);
            return;
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
            dashTimer = dashDuration;
            cooldownTimer = dashCooldown;
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

        Vector2 target = input * speed;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, acceleration * dt);
    }

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
