using UnityEngine;

/// <summary>Algo que o dash consegue atordoar (os inimigos).</summary>
public interface IStunnable
{
    bool Stunned { get; }
    void Stun(Vector2 from);
}

/// <summary>
/// Causa dano ao jogador enquanto ele encosta neste trigger. Se a brasa vier de dash, quem
/// apanha e o inimigo: fica atordoado e inofensivo por um tempo.
/// </summary>
public class DamageOnTouch : MonoBehaviour
{
    public int damage = 1;

    IStunnable stunnable;

    void Awake() => stunnable = GetComponentInParent<IStunnable>();

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var movement = other.GetComponent<PlayerMovement>();
        if (stunnable != null)
        {
            if (stunnable.Stunned) return;
            if (movement != null && movement.IsDashing)
            {
                stunnable.Stun(other.transform.position);
                return;
            }
        }
        var health = other.GetComponent<PlayerHealth>();
        if (health != null) health.TakeDamage(damage, transform.position);
    }
}
