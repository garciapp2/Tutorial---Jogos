using UnityEngine;

/// <summary>Carvao (+calor, +pontos, combo) ou fragmento de chama (objetivo da fase).</summary>
[RequireComponent(typeof(Collider2D))]
public class Pickup : MonoBehaviour
{
    public enum Kind { Coal, Fragment }

    public Kind type = Kind.Coal;
    public Transform visual;

    float phase;
    float emberTimer;

    void Start()
    {
        phase = Random.value * 10f;
    }

    void Update()
    {
        if (visual == null) return;
        float t = Time.time * 3f + phase;
        // flutua em degraus de pixel, para nao borrar a pixel art
        float bob = Mathf.Round(Mathf.Sin(t) * 1.5f) / 16f;
        visual.localPosition = new Vector3(0f, bob, 0f);

        if (Fx.Instance != null)
        {
            emberTimer -= Time.deltaTime;
            if (emberTimer <= 0f)
            {
                bool frag = type == Kind.Fragment;
                emberTimer = frag ? 0.18f : 0.45f + Random.value * 0.4f;
                Fx.Instance.Ember((Vector2)transform.position + Vector2.up * (frag ? 0.3f : 0.15f),
                    Random.value < 0.5f ? Pal.Yellow : Pal.Orange, frag ? 16f : 12f, frag ? 0.6f : 0.45f);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying) return;
        gm.Collect(this);
        Destroy(gameObject);
    }
}
