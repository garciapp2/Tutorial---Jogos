using UnityEngine;

/// <summary>Vida da brasa: coracoes, invulnerabilidade apos o dano e empurrao.</summary>
public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    public float invulnerableTime = 1.2f;
    public float knockbackForce = 10f;

    public int Current { get; private set; }
    public bool IsInvulnerable => invulnerableTimer > 0f || (movement != null && movement.IsDashing);
    public float InvulnerableLeft => invulnerableTimer;

    PlayerMovement movement;
    PlayerVisual visual;
    float invulnerableTimer;

    void Awake()
    {
        Current = maxHealth;
        movement = GetComponent<PlayerMovement>();
        visual = GetComponent<PlayerVisual>();
        if (movement != null) movement.Fell += OnFell;
    }

    /// <summary>Caiu no abismo: custa calor (nao coracao) e volta ao chao firme, como no Celeste.</summary>
    void OnFell()
    {
        var gm = GameManager.Instance;
        if (Current <= 0 || (gm != null && !gm.IsPlaying)) return;
        invulnerableTimer = 0.8f;
        AudioManager.Play(Sfx.Hurt, 0.6f, 0.7f);
        if (Fx.Instance != null)
        {
            Fx.Instance.AddTrauma(Fx.Tune.ShakeLight);
            Fx.Instance.Burst(transform.position, 10, new[] { Pal.Ink, Pal.Slate, Pal.Orange }, 60f, 0.5f, 2f);
        }
        if (gm != null) gm.FellInPit(transform.position);
    }

    void Update()
    {
        if (invulnerableTimer > 0f) invulnerableTimer -= Time.deltaTime;
    }

    public void TakeDamage(int amount, Vector2 source)
    {
        var gm = GameManager.Instance;
        if (Current <= 0 || IsInvulnerable || (gm != null && !gm.IsPlaying)) return;

        Current = Mathf.Max(0, Current - amount);
        invulnerableTimer = invulnerableTime;

        Vector2 dir = ((Vector2)transform.position - source).normalized;
        if (dir == Vector2.zero) dir = Vector2.up;
        if (movement != null) movement.ApplyKnockback(dir * knockbackForce, 0.16f);

        AudioManager.Play(Sfx.Hurt);
        if (Fx.Instance != null) Fx.Instance.Heavy(transform.position, dir);
        if (visual != null) visual.Hit(dir);
        if (gm != null) gm.OnPlayerDamaged(Current, transform.position);
    }
}
