using UnityEngine;

/// <summary>Flocos de um pixel caindo devagar na frente da camera.</summary>
public class AmbientSnow : MonoBehaviour
{
    public Camera cam;
    public float rate = 7f;
    float acc;

    void Update()
    {
        var fx = Fx.Instance;
        if (fx == null || cam == null) return;
        acc += Time.deltaTime * rate;
        float h = cam.orthographicSize, w = h * cam.aspect;
        Vector2 c = cam.transform.position;
        while (acc >= 1f)
        {
            acc -= 1f;
            var pos = c + new Vector2(Random.Range(-w, w), Random.Range(-h, h));
            fx.Ember(pos, Random.value < 0.6f ? Pal.Alpha(Pal.BlueLight, 0.6f) : Pal.Alpha(Pal.White, 0.5f), -10f, 2.2f);
        }
    }
}
