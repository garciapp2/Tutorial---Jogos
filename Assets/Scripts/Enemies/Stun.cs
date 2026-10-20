using UnityEngine;

/// <summary>Estado de atordoado compartilhado pelos inimigos: tempo, recompensa e efeito.</summary>
public static class Stun
{
    public const float Duration = 2.2f;
    public const int Score = 150;

    public static void Hit(Vector2 at, Vector2 from)
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.AddScore(Score);
            gm.hud.Popup("ATORDOADO!", at, Pal.Yellow, 1);
        }
        AudioManager.Play(Sfx.Hurt, 0.7f, 1.5f);
        var fx = Fx.Instance;
        if (fx != null) fx.Medium(at, (at - from).normalized, new[] { Pal.White, Pal.Yellow, Pal.TealLight });
    }

    /// <summary>Cor e tremida de quem esta atordoado.</summary>
    public static void Show(SpriteRenderer sr, Transform t, float left, Color baseColor)
    {
        if (left > 0f)
        {
            bool blink = left < 0.6f && Mathf.Repeat(left, 0.12f) > 0.06f;
            sr.color = blink ? baseColor : Color.Lerp(baseColor, Pal.White, 0.55f);
            t.localPosition = new Vector3(Mathf.Sin(Time.time * 60f) * 0.04f, t.localPosition.y, 0f);
        }
        else sr.color = baseColor;
    }
}
