using UnityEngine;

/// <summary>Anima um SpriteRenderer por uma tira de quadros (as anims geradas pelo pxl).</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnim : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 8f;
    public bool randomStart = true;
    public bool unscaled;

    SpriteRenderer sr;
    float t;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (randomStart) t = Random.value * 10f;
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        t += (unscaled ? Time.unscaledDeltaTime : Time.deltaTime) * fps;
        sr.sprite = frames[(int)t % frames.Length];
    }
}
