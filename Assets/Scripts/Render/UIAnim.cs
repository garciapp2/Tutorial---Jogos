using UnityEngine;
using UnityEngine.UI;

/// <summary>Anima uma Image da interface por uma tira de quadros (icones da tela de como jogar).</summary>
[RequireComponent(typeof(Image))]
public class UIAnim : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 6f;

    Image image;
    float t;

    void Awake() => image = GetComponent<Image>();

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        t += Time.unscaledDeltaTime * fps;
        image.sprite = frames[(int)t % frames.Length];
    }
}
