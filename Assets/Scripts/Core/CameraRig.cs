using UnityEngine;

/// <summary>
/// Camera do jogo: segue o alvo com suavidade, respeita os limites da arena (com uma
/// folga que deixa o redemoinho aparecer nas bordas) e aplica o tremor, o empurrao e o
/// zoom do Fx por cima.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraRig : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.12f;
    public Rect bounds = new Rect(0, 0, 46, 30);
    public float margin = 1.5f;
    [Tooltip("Meia altura da visao em unidades (300x240 com 16 px por unidade).")]
    public float baseSize = 7.5f;

    Camera cam;
    Vector3 basePos;
    Vector3 velocity;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographicSize = baseSize;
    }

    void Start()
    {
        basePos = Desired();
        transform.position = basePos;
    }

    void LateUpdate()
    {
        if (target != null)
            basePos = Vector3.SmoothDamp(basePos, Desired(), ref velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

        var fx = Fx.Instance;
        Vector2 offset = Vector2.zero;
        float angle = 0f, zoom = 1f;
        if (fx != null) (offset, angle, zoom) = fx.Camera();

        Vector3 pos = basePos;
        if (fx != null && zoom > 1.001f)
        {
            // o zoom puxa a camera para o ponto de interesse (nocaute, fogueira)
            float k = 1f - 1f / zoom;
            pos = Vector3.Lerp(pos, new Vector3(fx.focus.x, fx.focus.y, pos.z), k);
        }
        transform.SetPositionAndRotation(pos + (Vector3)offset, Quaternion.Euler(0, 0, angle));
        cam.orthographicSize = baseSize / zoom;
    }

    Vector3 Desired()
    {
        Vector3 p = target != null ? target.position : transform.position;
        float halfH = baseSize, halfW = halfH * cam.aspect;
        return new Vector3(
            Clamp(p.x, bounds.xMin - margin + halfW, bounds.xMax + margin - halfW),
            Clamp(p.y, bounds.yMin - margin + halfH, bounds.yMax + margin - halfH),
            -10f);
    }

    static float Clamp(float v, float min, float max) => min > max ? (min + max) * 0.5f : Mathf.Clamp(v, min, max);
}
