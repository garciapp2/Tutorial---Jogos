using System.Collections;
using UnityEngine;

/// <summary>
/// A fogueira congelada no centro da cripta. Com todos os fragmentos ela chama o jogador
/// (aneis e faiscas); tocar nela acende o fogo e vence a partida.
/// </summary>
public class ExitBonfire : MonoBehaviour
{
    public SpriteRenderer sprite;
    public Sprite frozenSprite;
    public Sprite[] litFrames;

    SpriteAnim anim;
    bool ready, lit;
    float ringTimer, emberTimer;
    readonly Spring pop = new Spring(1f, 300f, 12f);

    void Awake()
    {
        anim = sprite.GetComponent<SpriteAnim>();
        if (anim != null) anim.enabled = false;
        sprite.sprite = frozenSprite;
    }

    public void SetReady()
    {
        ready = true;
        pop.Kick(8f);
    }

    public void Ignite()
    {
        lit = true;
        pop.Kick(14f);
        if (anim != null)
        {
            anim.frames = litFrames;
            anim.enabled = true;
        }
    }

    /// <summary>Modo infinito: depois de arder um instante, a fogueira congela de novo.</summary>
    public void ResetFrozen() => StartCoroutine(Refreeze());

    IEnumerator Refreeze()
    {
        yield return new WaitForSeconds(1.4f);
        lit = false;
        ready = false;
        if (anim != null) anim.enabled = false;
        sprite.sprite = frozenSprite;
        pop.Kick(-8f);
        if (Fx.Instance != null) Fx.Instance.Burst(transform.position, 14, new[] { Pal.White, Pal.BlueLight }, 120f, 0.5f, 2f);
    }

    void Update()
    {
        pop.Update(Time.unscaledDeltaTime);
        float s = Mathf.Clamp(pop.value, 0.6f, 1.6f);
        sprite.transform.localScale = new Vector3(2f - s, s, 1f);

        var fx = Fx.Instance;
        if (fx == null || !(ready || lit)) return;
        float dt = Time.deltaTime;
        ringTimer -= dt;
        emberTimer -= dt;
        if (ready && !lit && ringTimer <= 0f)
        {
            ringTimer = 1.1f;
            fx.Ring(transform.position, Pal.Orange, 34f, 0.5f, 24);
            pop.Kick(4f);
        }
        if (emberTimer <= 0f)
        {
            emberTimer = lit ? 0.03f : 0.12f;
            fx.Ember((Vector2)transform.position + new Vector2((Random.value - 0.5f) * 0.6f, 0.2f),
                Random.value < 0.4f ? Pal.Yellow : Pal.Orange, lit ? 40f : 20f, lit ? 0.9f : 0.5f);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && GameManager.Instance != null)
            GameManager.Instance.TouchedExit();
    }
}
