using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Mecha/Interface/Controller")]
public sealed class ControllerInterfaceSettings : ScriptableObject
{
    public InputActionAsset actions;
    [Min(0f)] public float menuResumeDelay = 0.15f;
    [Min(0.05f)] public float navigationRepeatDelay = 0.35f, navigationRepeatRate = 0.1f;
    public string keyboardCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_'" + ".";
    public Vector2 keyboardPanelAnchor = new Vector2(0.65f, 0.58f);
    [TextArea] public string controllerHelp = "STICK / D-PAD Move   {jump} Jump   {attack} Light attack   {dash} Dash\n{tech} Heavy attack   {skill1} 25 MP / {skill2} 40 MP / {skill3} 60 MP   HOLD {attack} Portal\n{dash} + {jump} Rocket   then DOWN Mushidon   DOWN + {attack} Dive\n{menu} Menu   {diagnostics} Combat lab";
}
