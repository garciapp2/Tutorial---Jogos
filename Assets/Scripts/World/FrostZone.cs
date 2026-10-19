using UnityEngine;

/// <summary>Chao congelado: enquanto a brasa estiver em cima, o calor cai mais rapido e solta vapor frio.</summary>
public class FrostZone : MonoBehaviour
{
    Transform inside;
    float puff;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || GameManager.Instance == null) return;
        GameManager.Instance.EnterFrostZone();
        inside = other.transform;
        AudioManager.Play(Sfx.Frost, 0.8f);
        if (Fx.Instance != null)
            Fx.Instance.Burst(other.transform.position, 10, new[] { Pal.White, Pal.BlueLight, Pal.TealLight }, 90f, 0.5f, 1f);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || GameManager.Instance == null) return;
        GameManager.Instance.ExitFrostZone();
        inside = null;
    }

    void Update()
    {
        if (inside == null || Fx.Instance == null) return;
        puff -= Time.deltaTime;
        if (puff > 0f) return;
        puff = 0.08f;
        Fx.Instance.Ember((Vector2)inside.position + Random.insideUnitCircle * 0.5f, Random.value < 0.5f ? Pal.BlueLight : Pal.White, 10f, 0.5f);
    }
}
