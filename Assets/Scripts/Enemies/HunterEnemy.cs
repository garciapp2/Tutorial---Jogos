using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cacador glacial: patrulha um corredor e, se enxergar a brasa (distancia + linha de visao),
/// passa a persegui-la. Ao perder o jogador de vista vai ate a ultima posicao vista, espera e
/// volta para a patrulha refazendo o caminho (migalhas), entao nunca fica preso longe de casa.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class HunterEnemy : MonoBehaviour, IStunnable
{
    enum State { Patrol, Chase, Search, Return }

    public Vector2 patrolDirection = Vector2.right;
    public float patrolSpeed = 2.2f;
    public float chaseSpeed = 4.4f;
    public float detectRadius = 6.5f;
    public float loseRadius = 9f;
    public float leashDistance = 16f;
    public float searchWait = 3f;
    public float radius = 0.42f;
    public LayerMask wallMask;
    public SpriteRenderer body;
    public GameObject alertIcon;

    /// <summary>Quantos cacadores estao perseguindo agora (a musica sobe com isso).</summary>
    public static int Chasing { get; private set; }

    Rigidbody2D rb;
    Transform player;
    State state = State.Patrol;
    readonly List<Vector2> breadcrumbs = new List<Vector2>();
    Vector2 home;
    Vector2 lastSeen;
    float searchTimer;
    Color baseColor;
    readonly Spring alertPop = new Spring(0f, 300f, 12f);
    bool wasChasing;
    float stunTimer;

    public bool Stunned => stunTimer > 0f;

    public void Stun(Vector2 from)
    {
        stunTimer = global::Stun.Duration;
        global::Stun.Hit(transform.position, from);
        if (rb != null) rb.linearVelocity = ((Vector2)transform.position - from).normalized * 6f;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        home = rb.position;
        patrolDirection = patrolDirection.normalized;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            // o cacador atravessa a brasa (o dano vem do trigger), sem empurroes fisicos
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), p.GetComponent<Collider2D>());
        }
        if (body != null) baseColor = body.color;
        if (alertIcon != null) alertIcon.SetActive(false);
    }

    bool CanSeePlayer(float range)
    {
        if (player == null) return false;
        Vector2 to = (Vector2)player.position - rb.position;
        if (to.magnitude > range) return false;
        // linha de visao: nenhuma parede entre o cacador e a brasa
        return Physics2D.CircleCast(rb.position, 0.15f, to.normalized, to.magnitude, wallMask).collider == null;
    }

    void FixedUpdate()
    {
        var gm = GameManager.Instance;
        if (gm != null && !gm.IsPlaying)
        {
            rb.linearVelocity = Vector2.zero;
            if (gm.CurrentState != GameManager.State.Ready) return;
            Patrol();
            return;
        }

        if (stunTimer > 0f)
        {
            stunTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, 30f * Time.fixedDeltaTime);
            return;
        }

        bool tooFar = Vector2.Distance(rb.position, home) > leashDistance;

        switch (state)
        {
            case State.Patrol:
                Patrol();
                if (CanSeePlayer(detectRadius)) StartChase();
                break;

            case State.Chase:
                if (!tooFar && CanSeePlayer(loseRadius))
                {
                    lastSeen = player.position;
                    MoveTowards(lastSeen, chaseSpeed);
                    DropCrumb();
                }
                else
                {
                    state = tooFar ? State.Return : State.Search;
                    searchTimer = searchWait;
                }
                break;

            case State.Search:
                if (CanSeePlayer(detectRadius) && !tooFar) { StartChase(); break; }
                // o timer corre sempre, assim ele desiste mesmo se nao alcancar o ponto
                searchTimer -= Time.fixedDeltaTime;
                if (Vector2.Distance(rb.position, lastSeen) > 0.2f)
                {
                    MoveTowards(lastSeen, chaseSpeed * 0.8f);
                    DropCrumb();
                }
                else
                {
                    rb.linearVelocity = Vector2.zero;
                }
                if (searchTimer <= 0f) state = State.Return;
                break;

            case State.Return:
                if (!tooFar && CanSeePlayer(detectRadius * 0.8f)) { StartChase(); break; }
                if (breadcrumbs.Count == 0)
                {
                    state = State.Patrol;
                    break;
                }
                Vector2 target = breadcrumbs[breadcrumbs.Count - 1];
                if (Vector2.Distance(rb.position, target) < 0.15f)
                    breadcrumbs.RemoveAt(breadcrumbs.Count - 1);
                else
                    MoveTowards(target, patrolSpeed * 1.4f);
                break;
        }

        bool chasing = state == State.Chase;
        if (chasing != wasChasing)
        {
            Chasing += chasing ? 1 : -1;
            wasChasing = chasing;
            alertPop.target = chasing ? 1f : 0f;
            if (chasing) alertPop.Kick(14f);
        }
    }

    void Update()
    {
        alertPop.Update(Time.deltaTime);
        if (alertIcon != null)
        {
            float s = Mathf.Max(0f, alertPop.value);
            alertIcon.SetActive(s > 0.05f);
            alertIcon.transform.localScale = Vector3.one * s;
        }
        if (body != null)
        {
            // olhos acesos e tremida na perseguicao
            body.color = wasChasing ? Color.Lerp(baseColor, Pal.PurpleLight, 0.25f + Mathf.Sin(Time.time * 18f) * 0.1f) : baseColor;
            if (stunTimer > 0f) global::Stun.Show(body, body.transform, stunTimer, baseColor);
            float flip = rb != null && rb.linearVelocity.x < -0.1f ? -1f : 1f;
            body.transform.localScale = new Vector3(flip, 1f, 1f);
            if (stunTimer <= 0f) body.transform.localPosition = wasChasing ? (Vector3)(Random.insideUnitCircle * 0.03f) : Vector3.zero;
        }
    }

    void OnDisable()
    {
        if (wasChasing) Chasing--;
        wasChasing = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCount() => Chasing = 0;

    void StartChase()
    {
        if (state == State.Patrol)
        {
            breadcrumbs.Clear();
            breadcrumbs.Add(rb.position);
        }
        if (state != State.Chase)
        {
            AudioManager.Play(Sfx.Alert, 0.8f);
            if (Fx.Instance != null) Fx.Instance.AddPulse(0.3f);
        }
        state = State.Chase;
    }

    void Patrol()
    {
        float step = patrolSpeed * Time.fixedDeltaTime;
        if (Physics2D.CircleCast(rb.position, radius, patrolDirection, step + 0.08f, wallMask).collider != null)
            patrolDirection = -patrolDirection;
        rb.linearVelocity = patrolDirection * patrolSpeed * GameManager.EnemySpeed;
    }

    void MoveTowards(Vector2 target, float speed)
    {
        Vector2 dir = target - rb.position;
        rb.linearVelocity = dir.magnitude > 0.05f ? dir.normalized * speed * GameManager.EnemySpeed : Vector2.zero;
    }

    void DropCrumb()
    {
        if (breadcrumbs.Count == 0 || Vector2.Distance(breadcrumbs[breadcrumbs.Count - 1], rb.position) > 0.6f)
            breadcrumbs.Add(rb.position);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
