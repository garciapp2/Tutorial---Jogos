using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Troca de cena com duas cortinas de tinta que fecham e abrem (sobrevive a troca).</summary>
public class Transition : MonoBehaviour
{
    static Transition instance;
    RectTransform top, bottom;
    bool busy;

    static Transition Get()
    {
        if (instance != null) return instance;
        var go = new GameObject("Transicao");
        DontDestroyOnLoad(go);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        instance = go.AddComponent<Transition>();
        instance.top = Curtain(go.transform, 1f);
        instance.bottom = Curtain(go.transform, 0f);
        instance.Set(0f);
        return instance;
    }

    static RectTransform Curtain(Transform parent, float anchorY)
    {
        var rt = new GameObject("Cortina", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0f, anchorY);
        rt.anchorMax = new Vector2(1f, anchorY);
        rt.pivot = new Vector2(0.5f, anchorY);
        rt.GetComponent<Image>().color = Pal.Ink;
        return rt;
    }

    void Set(float closed)
    {
        float h = ((RectTransform)transform).rect.height * 0.5f + 4f;
        // degraus de 4 px: a cortina anda "em pixel"
        float size = Mathf.Round(Mathf.Clamp01(closed) * h / 4f) * 4f;
        top.sizeDelta = new Vector2(0f, size);
        bottom.sizeDelta = new Vector2(0f, size);
    }

    public static void Load(string scene)
    {
        var t = Get();
        if (t.busy) return;
        t.StartCoroutine(t.Run(scene));
    }

    IEnumerator Run(string scene)
    {
        busy = true;
        for (float e = 0f; e < 0.22f; e += Time.unscaledDeltaTime)
        {
            Set(Ease.OutCubic(e / 0.22f));
            yield return null;
        }
        Set(1f);
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync(scene);
        yield return null;
        for (float e = 0f; e < 0.3f; e += Time.unscaledDeltaTime)
        {
            Set(1f - Ease.OutCubic(e / 0.3f));
            yield return null;
        }
        Set(0f);
        busy = false;
    }
}
