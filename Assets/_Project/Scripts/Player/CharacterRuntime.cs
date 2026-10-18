using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[DefaultExecutionOrder(-40)]
public sealed class CharacterRuntime : NetworkBehaviour
{
    [SerializeField] private CharacterCatalog catalog;
    [SerializeField] private CharacterRigBinding[] rigs;
    private readonly SyncVar<int> characterIndex = new SyncVar<int>(-1);
    private int appliedIndex = -1;
    private bool selectionLocked;
    public CharacterDefinition Definition { get; private set; }
    public bool Ready => !IsSpawned || characterIndex.Value >= 0 && (!IsOwner || selectionLocked);
    public CharacterSkillDefinition Skill(int slot) => Definition != null && slot >= 0 && slot < Definition.skills.Length ? Definition.skills[slot] : null;
    public bool Allows(CombatCapabilities capability) => Definition == null || Definition.Allows(capability);
    private void Awake() { characterIndex.OnChange += OnCharacterChanged; }
    private void Start() { if (!IsSpawned) Apply(catalog.IndexOf(CharacterSelectionMenu.SelectedId)); }
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (characterIndex.Value >= 0) Apply(characterIndex.Value);
        if (IsOwner) RequestCharacter(catalog.IndexOf(CharacterSelectionMenu.SelectedId));
    }
    [ServerRpc]
    private void RequestCharacter(int index)
    {
        if (selectionLocked || catalog.At(index) == null) return;
        selectionLocked = true; characterIndex.Value = index; Apply(index);
        ConfirmCharacter(Owner);
    }
    [TargetRpc]
    private void ConfirmCharacter(FishNet.Connection.NetworkConnection connection) => selectionLocked = true;
    private void OnCharacterChanged(int previous, int next, bool asServer) { if (next >= 0) Apply(next); }
    public void Apply(int index)
    {
        var definition = catalog.At(index);
        if (definition == null || appliedIndex == index) return;
        appliedIndex = index; Definition = definition;
        for (int i = 0; i < rigs.Length; i++) rigs[i].gameObject.SetActive(i == index);
        var rig = rigs[index];
        rig.transform.localScale = Vector3.one * definition.visualScale;
        GetComponent<PlayerMovement>().Configure(definition.movement);
        GetComponent<PlayerAttack>().Configure(definition.combat);
        GetComponent<CharacterAnimator>().Configure(definition.animation, rig.animator);
        GetComponent<CombatEmissionOrigin>().Bind(rig.skillHand);
        GetComponent<MechaAutoAim>().Bind(rig.leftArm);
        GetComponent<SecondaryWeaponPresentation>().Bind(rig.leftArm, rig.gun);
        GetComponent<PlayerCombatVfx>().Configure(definition.combat);
    }
    private void OnDestroy() => characterIndex.OnChange -= OnCharacterChanged;
}
