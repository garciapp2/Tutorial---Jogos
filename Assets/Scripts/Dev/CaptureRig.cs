#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Ferramenta de editor para a pagina do jogo: grava quadros da tela (para GIFs) e pilota a
/// brasa por um roteiro de movimentos e dashes. Nao entra na build.
/// </summary>
public class CaptureRig : MonoBehaviour
{
    public string folder = "Temp/gif";
    public float seconds = 4f;
    public int fps = 15;
    public readonly List<System.Func<IEnumerator>> route = new List<System.Func<IEnumerator>>();
    public bool Done { get; private set; }

    PlayerMovement player;

    IEnumerator Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.GetComponent<PlayerMovement>();
        Directory.CreateDirectory(folder);
        foreach (var f in Directory.GetFiles(folder, "*.png")) File.Delete(f);
        StartCoroutine(Pilot());
        int frame = 0;
        for (float t = 0f; t < seconds; t += 1f / fps)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(folder, $"f{frame++:000}.png"), tex.EncodeToPNG());
            Destroy(tex);
            yield return new WaitForSecondsRealtime(1f / fps);
        }
        if (player != null) player.demoInput = null;
        Done = true;
    }

    IEnumerator Pilot()
    {
        foreach (var step in route) yield return step();
        if (player != null) player.demoInput = Vector2.zero;
    }

    // --------------------------------------------------------------- passos do roteiro
    public System.Func<IEnumerator> MoveTo(Vector2 target) => () => MoveRoutine(target);
    public System.Func<IEnumerator> Dash(Vector2 dir, float after = 0.3f) => () => DashRoutine(() => dir, after);
    public System.Func<IEnumerator> DashAt(Transform target, float after = 0.35f) =>
        () => DashRoutine(() => target != null ? ((Vector2)(target.position - player.transform.position)).normalized : Vector2.right, after);
    public System.Func<IEnumerator> Wait(float s) => () => WaitRoutine(s);

    IEnumerator MoveRoutine(Vector2 target)
    {
        for (float t = 0; t < 3f; t += Time.deltaTime)
        {
            Vector2 to = target - (Vector2)player.transform.position;
            if (to.magnitude < 0.25f) break;
            player.demoInput = to.normalized;
            yield return null;
        }
        player.demoInput = Vector2.zero;
    }

    IEnumerator DashRoutine(System.Func<Vector2> dir, float after)
    {
        player.demoInput = dir();
        yield return null;
        player.DemoDash();
        yield return new WaitForSeconds(after);
        player.demoInput = Vector2.zero;
    }

    IEnumerator WaitRoutine(float s)
    {
        player.demoInput = Vector2.zero;
        yield return new WaitForSeconds(s);
    }
}
#endif
