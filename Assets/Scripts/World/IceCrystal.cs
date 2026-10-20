using UnityEngine;

/// <summary>Cristal de gelo: machuca quem encosta, mas o dash estilhaca (e devolve um pouco de calor).</summary>
public class IceCrystal : MonoBehaviour
{
    public int score = 50;
    public float heat = 1f;

    void OnTriggerStay2D(Collider2D other)
    {
        var m = other.GetComponent<PlayerMovement>();
        if (m == null || !m.IsDashing) return;

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.AddHeat(heat);
            gm.AddScore(score);
            gm.hud.Popup($"+{heat:0}s", transform.position, Pal.BlueLight, 1);
        }
        AudioManager.Play(Sfx.Bump, 1f, 1.6f);
        AudioManager.Play(Sfx.Frost, 0.8f);
        var fx = Fx.Instance;
        if (fx != null)
        {
            fx.Stop(0.04f);
            fx.AddTrauma(0.25f);
            fx.Burst(transform.position, 16, new[] { Pal.White, Pal.BlueLight, Pal.Blue }, 170f, 0.6f, 2f, 120f, true);
            fx.Sparks(transform.position, m.Velocity.normalized, 10, new[] { Pal.White, Pal.BlueLight });
        }
        Destroy(gameObject);
    }
}
