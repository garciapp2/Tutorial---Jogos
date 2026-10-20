using UnityEngine;

/// <summary>
/// Quadro atras de tudo que desenha o redemoinho. Cobre a visao da camera e passa para o
/// shader o tempo, o pulso dos golpes e os tres tons (que esfriam junto com o calor).
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class Backdrop : MonoBehaviour
{
    public Camera cam;
    public Color[] warm = Pal.BgMenu;
    public Color[] cold = Pal.BgCold;
    [Range(0, 1)] public float coldness;
    [Range(0, 1)] public float dim;

    static readonly int TimeId = Shader.PropertyToID("_CE_Time");
    static readonly int PulseId = Shader.PropertyToID("_CE_Pulse");
    static readonly int DimId = Shader.PropertyToID("_CE_Dim");
    static readonly int LogicalId = Shader.PropertyToID("_CE_Logical");
    static readonly int[] BgIds = { Shader.PropertyToID("_CE_Bg0"), Shader.PropertyToID("_CE_Bg1"), Shader.PropertyToID("_CE_Bg2") };

    float time;

    void LateUpdate()
    {
        time += Time.unscaledDeltaTime;
        if (cam != null)
        {
            float h = cam.orthographicSize * 2f * 1.4f;
            transform.localScale = new Vector3(h * cam.aspect, h, 1f);
            transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 50f);
            transform.rotation = Quaternion.identity;
        }

        var fx = Fx.Instance;
        Shader.SetGlobalFloat(TimeId, time);
        Shader.SetGlobalFloat(PulseId, fx != null ? fx.Pulse : 0f);
        Shader.SetGlobalFloat(DimId, dim);
        float aspect = Screen.width / Mathf.Max(1f, Screen.height);
        Shader.SetGlobalVector(LogicalId, new Vector4(240f * aspect, 240f, 0, 0));
        for (int i = 0; i < 3; i++)
            Shader.SetGlobalColor(BgIds[i], Color.Lerp(warm[i], cold[i], coldness));
    }
}
