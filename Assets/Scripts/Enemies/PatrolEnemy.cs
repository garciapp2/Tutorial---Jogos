using UnityEngine;

/// <summary>
/// Espirito de gelo: anda numa direcao fixa (horizontal ou vertical) e inverte ao encostar
/// numa parede. Nao persegue, mas machuca ao tocar (DamageOnTouch no filho).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PatrolEnemy : MonoBehaviour
{
    public Vector2 direction = Vector2.right;
    public float speed = 3f;
    public float radius = 0.4f;
    public LayerMask wallMask;
    public Transform visual;

    Rigidbody2D rb;
    float phase;
    readonly Spring squash = new Spring(1f, 400f, 14f);

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        direction = direction.normalized;
        phase = Random.value * 10f;
    }

    void FixedUpdate()
    {
        var gm = GameManager.Instance;
        if (gm != null && !gm.IsPlaying && gm.CurrentState != GameManager.State.Ready) return;

        float step = speed * Time.fixedDeltaTime;
        if (Physics2D.CircleCast(rb.position, radius, direction, step + 0.05f, wallMask).collider != null)
        {
            direction = -direction;
            squash.Kick(-6f); // achata ao bater e virar
        }
        rb.MovePosition(rb.position + direction * step);
    }

    void Update()
    {
        if (visual == null) return;
        squash.Update(Time.deltaTime);
        float s = Mathf.Clamp(squash.value, 0.7f, 1.3f);
        // flutua e vira para o lado em que anda
        visual.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 4f + phase) * 0.1f, 0f);
        float flip = Mathf.Abs(direction.x) > 0.1f && direction.x < 0f ? -1f : 1f;
        visual.localScale = new Vector3(flip * (2f - s), s, 1f);
    }
}
