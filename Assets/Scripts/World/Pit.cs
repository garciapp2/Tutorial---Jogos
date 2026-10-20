using UnityEngine;

/// <summary>Abismo: so da para passar por cima com o dash. Quem para em cima cai e volta ao chao firme.</summary>
public class Pit : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        var m = other.GetComponent<PlayerMovement>();
        if (m != null) m.EnterPit();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var m = other.GetComponent<PlayerMovement>();
        if (m != null) m.ExitPit();
    }
}
