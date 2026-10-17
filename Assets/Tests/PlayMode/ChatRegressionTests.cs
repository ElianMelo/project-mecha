#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using Mecha.Tests;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Explicitly requested chat regression: exercise the saved UI and native Enter flow.
public sealed class ChatRegressionTests
{
    private Keyboard keyboard;
    private ScriptableObject profile;
    private FishNet.Managing.NetworkManager network;
    [UnityTest]
    public IEnumerator EnterSendsOnceExpiresMessagesAndRemainsUsableAfterHostStarts()
    {
        yield return EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/MainScene.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        var chat = Object.FindFirstObjectByType(GameCode.Type("MechaChat")) as MonoBehaviour;
        Assert.That(chat, Is.Not.Null);
        var original = chat.GetType().GetField("settings", BindingFlags.NonPublic | BindingFlags.Instance);
        profile = Object.Instantiate((ScriptableObject)original.GetValue(chat));
        profile.GetType().GetField("messageLifetime").SetValue(profile, 0.7f);
        profile.GetType().GetField("sendInterval").SetValue(profile, 0.1f);
        profile.GetType().GetField("visibleMessages").SetValue(profile, 2);
        original.SetValue(chat, profile);
        keyboard = InputSystem.AddDevice<Keyboard>();
        yield return Press(Key.Enter);
        Assert.That(Editing(chat), Is.True, "Enter must open the persistent composer.");
        var input = (TMP_InputField)chat.GetType().GetProperty("InputField").GetValue(chat);
        input.text = "first <color=red>literal</color>";
        yield return Press(Key.Enter);
        Assert.That(Editing(chat), Is.False);
        Assert.That(Messages(chat), Does.Contain("first <color=red>literal</color>"));
        Assert.That(Messages(chat).Split('\n').Length, Is.EqualTo(1), "Native TMP submit and Enter polling must not double-send.");
        yield return new WaitForSecondsRealtime(0.15f);
        yield return Press(Key.Enter); input.text = "second"; yield return Press(Key.Enter);
        yield return new WaitForSecondsRealtime(0.15f);
        yield return Press(Key.Enter); input.text = "third"; yield return Press(Key.Enter);
        Assert.That(Messages(chat), Does.Not.Contain("first"));
        Assert.That(Messages(chat), Does.Contain("second").And.Contain("third"));
        yield return new WaitForSecondsRealtime(0.8f);
        Assert.That(Messages(chat), Is.Empty, "Old messages should expire without disabling the chat.");
        Assert.That(chat.gameObject.activeInHierarchy, Is.True);
        yield return Press(Key.Enter); input.text = "cancel me"; yield return Press(Key.Escape);
        Assert.That(Editing(chat), Is.False); Assert.That(input.text, Is.Empty);
        Assert.That(Messages(chat), Is.Empty);

        // Local menu preview only: don't query an external lobby service in a chat regression.
        var lobby = Object.FindFirstObjectByType(GameCode.Type("MechaLobbyPanel"));
        var panel = (GameObject)lobby.GetType().GetField("panel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(lobby);
        panel.SetActive(true); yield return null;
        var textEntry = Object.FindFirstObjectByType(GameCode.Type("ControllerTextEntry"));
        var pilotField = (TMP_InputField)lobby.GetType().GetField("playerName", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(lobby);
        GameCode.Call(textEntry, "Open", pilotField); yield return null;
        yield return Press(Key.Enter); input.text = "chat over modal"; yield return Press(Key.Enter);
        Assert.That(Messages(chat), Does.Contain("chat over modal"));
        Assert.That(chat.GetComponent<Canvas>().sortingOrder, Is.GreaterThan(panel.GetComponentsInChildren<Canvas>()[0].sortingOrder),
            "The text-entry overlay must not hide persistent comms.");
        GameCode.Call(textEntry, "Cancel"); panel.SetActive(false);
        yield return new WaitForSecondsRealtime(0.8f);

        // Pointer/native TMP focus must also enter chat and release focus after submit.
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(input.gameObject);
        input.ActivateInputField(); yield return null; yield return null;
        Assert.That(Editing(chat), Is.True);
        input.text = "mouse composer"; yield return Press(Key.Enter);
        Assert.That(Editing(chat), Is.False);
        yield return null;
        Assert.That(Editing(chat), Is.False, "Submitting must not reopen the just-closed field.");
        yield return new WaitForSecondsRealtime(0.8f);

        network = Object.FindFirstObjectByType<FishNet.Managing.NetworkManager>();
        var manager = Object.FindFirstObjectByType(GameCode.Type("GameManager"));
        GameCode.Call(manager, "StartHost");
        float deadline = Time.realtimeSinceStartup + 5f;
        while (!network.ClientManager.Started && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(network.ClientManager.Started && network.ServerManager.Started, Is.True);
        yield return new WaitForSecondsRealtime(0.3f);
        yield return Press(Key.Enter);
        foreach (var player in Object.FindObjectsByType(GameCode.Type("PlayerInputs"), FindObjectsSortMode.None))
            Assert.That((bool)player.GetType().GetProperty("HasControl").GetValue(player), Is.False, "Typing must block gameplay.");
        input.text = "host round trip"; yield return Press(Key.Enter);
        deadline = Time.realtimeSinceStartup + 2f;
        while (!Messages(chat).Contains("host round trip") && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Messages(chat), Does.Contain("host round trip"));
        Assert.That(Messages(chat).Split('\n').Length, Is.EqualTo(1));
        network.ClientManager.StopConnection(); network.ServerManager.StopConnection(true);
        yield return new WaitForSecondsRealtime(0.2f);
        yield return Press(Key.Enter); input.text = "still usable"; yield return Press(Key.Enter);
        Assert.That(Messages(chat), Does.Contain("still usable"));
        Assert.That(chat.GetComponent<Canvas>().enabled && input.gameObject.activeInHierarchy, Is.True);
    }
    private static bool Editing(Object chat) => (bool)chat.GetType().GetProperty("Editing").GetValue(chat);
    private static string Messages(Object chat) => (string)chat.GetType().GetProperty("VisibleMessages").GetValue(chat);
    private IEnumerator Press(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
    }
    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (network != null) { network.ClientManager.StopConnection(); network.ServerManager.StopConnection(true); }
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (profile != null) Object.Destroy(profile);
        yield return null;
    }
}
#endif
