using UnityEngine;

/// <summary>A paleta do jogo (a mesma de 24 cores do Rebound) e os tons do fundo em redemoinho.</summary>
public static class Pal
{
    public static readonly Color Ink = Hex(0x0b0912), Night = Hex(0x15112a), Dusk = Hex(0x231c40), Slate = Hex(0x3d3769);
    public static readonly Color Fog = Hex(0x7a75aa), Bone = Hex(0xd8d0bb), White = Hex(0xfff6e0);
    public static readonly Color Red = Hex(0xef4b55), RedDark = Hex(0x8d2140), RedLight = Hex(0xff9d8c);
    public static readonly Color Orange = Hex(0xf58a3a), Yellow = Hex(0xf8cf4e), Gold = Hex(0xb9772a);
    public static readonly Color Green = Hex(0x56c86f), GreenDark = Hex(0x237a5b);
    public static readonly Color Teal = Hex(0x38c7b6), TealDark = Hex(0x1b6b78), TealLight = Hex(0xaaf3e4);
    public static readonly Color Blue = Hex(0x4b8ff5), BlueDark = Hex(0x2a4ba8), BlueLight = Hex(0xa3cbff);
    public static readonly Color Purple = Hex(0xa35fe6), PurpleDark = Hex(0x5b2f96), PurpleLight = Hex(0xd9b5ff);

    /// <summary>Tres tons do redemoinho, do mais escuro ao mais claro.</summary>
    public static readonly Color[] BgMenu = { Hex(0x120e26), Hex(0x221a48), Hex(0x38286c) };
    public static readonly Color[] BgWarm = { Hex(0x1c0c10), Hex(0x3c141c), Hex(0x642226) };
    public static readonly Color[] BgCold = { Hex(0x0c141e), Hex(0x162a3c), Hex(0x234556) };

    public static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

    public static Color Alpha(Color c, float a) { c.a = a; return c; }
}

/// <summary>Mola amortecida: a base de quase todo movimento de interface e de impacto.</summary>
public class Spring
{
    public float value, velocity, target, stiffness, damping;

    public Spring(float value = 0f, float stiffness = 180f, float damping = 14f)
    {
        this.value = target = value;
        this.stiffness = stiffness;
        this.damping = damping;
    }

    public float Update(float dt)
    {
        // Semi-implicito, em subpassos, para nao explodir com molas duras em quadros longos.
        int steps = Mathf.Max(1, Mathf.CeilToInt(dt / (1f / 120f)));
        float h = dt / steps;
        for (int i = 0; i < steps; i++)
        {
            velocity += (-stiffness * (value - target) - damping * velocity) * h;
            value += velocity * h;
        }
        return value;
    }

    public void Kick(float amount) => velocity += amount;

    public void Snap(float v)
    {
        value = target = v;
        velocity = 0f;
    }
}

public static class Ease
{
    public static float OutBack(float t, float overshoot = 1.70158f)
    {
        float u = t - 1f;
        return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
    }

    public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    /// <summary>Aproxima current de target de forma independente do passo de tempo.</summary>
    public static float Approach(float current, float target, float rate, float dt) =>
        target + (current - target) * Mathf.Exp(-rate * dt);

    /// <summary>Ruido suave 1D em -1..1, para o tremor de camera.</summary>
    public static float SmoothNoise(float t, int seed)
    {
        int i = Mathf.FloorToInt(t);
        float f = t - i;
        float a = Hash(i, seed), b = Hash(i + 1, seed);
        float u = f * f * (3f - 2f * f);
        return (a + (b - a) * u) * 2f - 1f;
    }

    static float Hash(int n, int seed)
    {
        unchecked
        {
            uint h = (uint)(n * 374761393) ^ (uint)(seed * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h ^ (h >> 16)) / 4294967296f;
        }
    }
}
