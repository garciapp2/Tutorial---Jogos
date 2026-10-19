using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Entradas do jogo pelo Input System: teclado (WASD / setas) e controle de Xbox
/// (analogico ou D-pad, A confirma/dash, B volta, Start pausa). Nos menus as direcoes
/// repetem quando seguradas, como no Rebound.
/// </summary>
public static class GameInput
{
    static InputAction move, dash, pause, confirm, back;
    static int lastFrame = -1;
    static Vector2Int menuDir;
    static Vector2Int heldDir;
    static float repeatAt;

    /// <summary>Verdadeiro quando a ultima entrada veio de um controle (troca as dicas na tela).</summary>
    public static bool UsingGamepad { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        foreach (var a in new[] { move, dash, pause, confirm, back }) a?.Dispose();
        move = dash = pause = confirm = back = null;
        lastFrame = -1;
    }

    static void Init()
    {
        if (move != null) return;

        move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        move.AddBinding("<Gamepad>/leftStick");
        move.AddBinding("<Gamepad>/dpad");

        dash = Button("<Keyboard>/space", "<Keyboard>/leftShift", "<Keyboard>/k", "<Gamepad>/buttonSouth", "<Gamepad>/rightTrigger", "<Gamepad>/rightShoulder");
        pause = Button("<Keyboard>/escape", "<Keyboard>/p", "<Gamepad>/start");
        confirm = Button("<Keyboard>/space", "<Keyboard>/enter", "<Keyboard>/numpadEnter", "<Gamepad>/buttonSouth");
        back = Button("<Keyboard>/escape", "<Keyboard>/backspace", "<Gamepad>/buttonEast");

        foreach (var a in new[] { move, dash, pause, confirm, back }) a.Enable();
    }

    static InputAction Button(params string[] bindings)
    {
        var a = new InputAction(type: InputActionType.Button);
        foreach (var b in bindings) a.AddBinding(b);
        return a;
    }

    /// <summary>Atualiza a repeticao de direcao dos menus uma vez por quadro.</summary>
    static void Tick()
    {
        Init();
        if (lastFrame == Time.frameCount) return;
        lastFrame = Time.frameCount;

        var dev = move.activeControl?.device;
        if (dev != null) UsingGamepad = dev is Gamepad;
        foreach (var a in new[] { dash, confirm, back, pause })
            if (a.WasPressedThisFrame() && a.activeControl != null) UsingGamepad = a.activeControl.device is Gamepad;

        Vector2 v = move.ReadValue<Vector2>();
        var dir = Vector2Int.zero;
        if (Mathf.Abs(v.y) > 0.5f && Mathf.Abs(v.y) >= Mathf.Abs(v.x)) dir.y = v.y > 0 ? 1 : -1;
        else if (Mathf.Abs(v.x) > 0.5f) dir.x = v.x > 0 ? 1 : -1;

        menuDir = Vector2Int.zero;
        float now = Time.unscaledTime;
        if (dir != heldDir)
        {
            heldDir = dir;
            menuDir = dir;
            repeatAt = now + 0.35f;
        }
        else if (dir != Vector2Int.zero && now >= repeatAt)
        {
            menuDir = dir;
            repeatAt = now + 0.09f;
        }
    }

    public static Vector2 Move
    {
        get { Tick(); return Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f); }
    }

    public static bool DashPressed { get { Tick(); return dash.WasPressedThisFrame(); } }
    public static bool PausePressed { get { Tick(); return pause.WasPressedThisFrame(); } }
    public static bool ConfirmPressed { get { Tick(); return confirm.WasPressedThisFrame(); } }
    public static bool BackPressed { get { Tick(); return back.WasPressedThisFrame(); } }

    /// <summary>Direcao de menu deste quadro (com repeticao ao segurar).</summary>
    public static Vector2Int MenuDirection { get { Tick(); return menuDir; } }
}
