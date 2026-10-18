using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;

[CreateAssetMenu(menuName = "Mecha/Interface/Controls")]
public sealed class ControlBindingSettings : ScriptableObject
{
    [Serializable]
    public sealed class ActionDescription
    {
        public string action;
        public LocalizedString title = new LocalizedString();
        public LocalizedString description = new LocalizedString();
        public bool tutorial;
    }
    public InputActionAsset actions;
    [Min(3f)] public float captureTimeout = 10f;
    public ActionDescription[] descriptions = Array.Empty<ActionDescription>();
}
