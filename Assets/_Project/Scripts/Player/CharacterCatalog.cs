using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Characters/Roster")]
public sealed class CharacterCatalog : ScriptableObject
{
    public CharacterDefinition[] characters;
    public int Count => characters == null ? 0 : characters.Length;
    public CharacterDefinition At(int index) => index >= 0 && index < Count ? characters[index] : null;
    public int IndexOf(string id)
    {
        for (int i = 0; i < Count; i++) if (characters[i] != null && characters[i].id == id) return i;
        return 0;
    }
}
