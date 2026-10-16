using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Manual local Editor controls. One invocation is one operator-requested gesture; no test runner or assertions.
public static class CadenceLab
{
    private static PlayerMovement Player => UnityEngine.Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None).First(p => p.HasControl);
    public static object Snapshot() => new { frame = Time.frameCount, time = Time.time,
        position = Player.transform.position.ToString("F3"), velocity = Player.Velocity.ToString("F3"),
        state = Player.MovementState, ground = Player.IsGrounded,
        action = Player.GetComponent<PlayerAttack>().CurrentAction, phase = Player.GetComponent<PlayerAttack>().Phase,
        cancel = Player.GetComponent<PlayerAttack>().CancelRemaining,
        damage = Player.GetComponent<PlayerAttack>().TotalDamage,
        inputs = Player.GetComponent<PlayerInputs>().MoveInput.ToString() };
    public static async Task<object> Gesture(string keys, int milliseconds = 100, bool pause = false)
    {
        EditorApplication.isPaused = false;
        var view = EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")); view.Focus();
        var codes = keys.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(k => (Key)Enum.Parse(typeof(Key), k, true)).ToArray();
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(codes));
        await Task.Delay(milliseconds);
        var result = Snapshot();
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        if (pause) { await Task.Delay(15); EditorApplication.isPaused = true; }
        return result;
    }
    public static object Position(float x, float y)
    {
        EditorApplication.isPaused = false;
        Player.GetComponent<PlayerAttack>().Interrupt();
        Player.ResetTraversal();
        var body = Player.GetComponent<Rigidbody>(); body.position = new Vector3(x, y, 0f); body.linearVelocity = Vector3.zero;
        Player.transform.position = body.position; Physics.SyncTransforms();
        return Snapshot();
    }
    public static async Task<object> Sequence(string steps, bool pause = false, string capturePath = "")
    {
        EditorApplication.isPaused = false;
        EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
        var snapshots = new System.Collections.Generic.List<object>();
        foreach (string step in steps.Split(';'))
        {
            var parts = step.Split(':');
            var codes = parts[0].Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(k => (Key)Enum.Parse(typeof(Key), k, true)).ToArray();
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(codes));
            await Task.Delay(int.Parse(parts[1])); snapshots.Add(Snapshot());
        }
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        if (!string.IsNullOrEmpty(capturePath))
        {
            ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath(capturePath));
            await Task.Delay(100);
        }
        if (pause) { await Task.Delay(15); EditorApplication.isPaused = true; }
        return snapshots;
    }
    public static object Prepare()
    {
        foreach (var enemy in EnemyController.ActiveEnemies) enemy.PracticePassive = true;
        Player.GetComponent<PlayerHealth>().EnterLevel(new Vector3(-5f, -3.4f, 0f));
        Player.Face(1);
        var target = EnemyController.ActiveEnemies.First(e => !e.IsDead);
        target.GetComponent<Rigidbody>().position = new Vector3(-4f, -3.4f, 0f);
        target.transform.position = new Vector3(-4f, -3.4f, 0f);
        target.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        Physics.SyncTransforms();
        return Snapshot();
    }
    public static object SoloTarget(float x = -8f)
    {
        EditorApplication.isPaused = false;
        var targets = Resources.FindObjectsOfTypeAll<EnemyController>().Where(e => e.gameObject.scene.IsValid() && e.NetworkObject.IsSpawned && !e.IsDead).OrderByDescending(e => e.Health).ToArray();
        var target = targets[0]; target.gameObject.SetActive(true);
        foreach (var enemy in targets) { enemy.PracticePassive = true; if (enemy != target) enemy.gameObject.SetActive(false); }
        foreach (var prop in UnityEngine.Object.FindObjectsByType<DestructibleProp>(FindObjectsSortMode.None)) prop.gameObject.SetActive(false);
        Player.GetComponent<PlayerHealth>().EnterLevel(new Vector3(x - 1f, -3.4f, 0f)); Player.Face(1);
        target.GetComponent<Rigidbody>().position = new Vector3(x, -3.4f, 0f);
        target.transform.position = new Vector3(x, -3.4f, 0f); target.GetComponent<EnemyMotor>().SetImpact(Vector3.zero);
        Physics.SyncTransforms(); return new { target = target.name, target.Health, player = Snapshot() };
    }
    public static object Diagnostics() { UnityEngine.Object.FindFirstObjectByType<GameplayDiagnostics>().Toggle(); return Snapshot(); }
}
