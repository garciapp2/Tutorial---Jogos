using UnityEngine;

/// <summary>Causa dano ao jogador enquanto ele encosta neste trigger (espinhos e inimigos).</summary>
public class DamageOnTouch : MonoBehaviour
{
    public int damage = 1;

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var health = other.GetComponent<PlayerHealth>();
        if (health != null) health.TakeDamage(damage, transform.position);
    }
}
