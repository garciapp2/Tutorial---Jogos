using UnityEngine;

/// <summary>Opcoes do jogador, guardadas entre sessoes.</summary>
public static class Settings
{
    const string Prefix = "ColdEmber.";

    public static float Music { get => Get("Music", 0.7f); set => Set("Music", value); }
    public static float Sfx { get => Get("Sfx", 0.9f); set => Set("Sfx", value); }
    /// <summary>Intensidade do tremor de tela (0 desliga).</summary>
    public static float Shake { get => Get("Shake", 1f); set => Set("Shake", value); }
    /// <summary>Claroes e aberracao cromatica nos golpes.</summary>
    public static bool Flashes { get => Get("Flashes", 1f) > 0.5f; set => Set("Flashes", value ? 1f : 0f); }
    /// <summary>Tela de fliperama: curvatura, vinheta e franja de cor.</summary>
    public static bool Crt { get => Get("Crt", 1f) > 0.5f; set => Set("Crt", value ? 1f : 0f); }

    public static float BestTime => PlayerPrefs.HasKey(Prefix + "BestTime") ? PlayerPrefs.GetFloat(Prefix + "BestTime") : -1f;
    public static int BestScore => PlayerPrefs.GetInt(Prefix + "BestScore", 0);
    public static string BestGrade => PlayerPrefs.GetString(Prefix + "BestGrade", "");

    /// <summary>Guarda o resultado de uma vitoria; devolve true se o tempo foi recorde.</summary>
    public static bool SubmitWin(float time, int score, string grade)
    {
        bool record = BestTime < 0f || time < BestTime;
        if (record) PlayerPrefs.SetFloat(Prefix + "BestTime", time);
        if (score > BestScore) PlayerPrefs.SetInt(Prefix + "BestScore", score);
        if (GradeRank(grade) > GradeRank(BestGrade)) PlayerPrefs.SetString(Prefix + "BestGrade", grade);
        PlayerPrefs.Save();
        return record;
    }

    static int GradeRank(string g) => g == "S" ? 4 : g == "A" ? 3 : g == "B" ? 2 : g == "C" ? 1 : 0;

    static float Get(string key, float fallback) => PlayerPrefs.GetFloat(Prefix + key, fallback);

    static void Set(string key, float value)
    {
        PlayerPrefs.SetFloat(Prefix + key, value);
        PlayerPrefs.Save();
    }

    public static string FormatTime(float seconds)
    {
        if (seconds < 0f) return "--:--.-";
        int m = (int)(seconds / 60f);
        float s = seconds - m * 60;
        return $"{m:00}:{s:00.0}";
    }
}
