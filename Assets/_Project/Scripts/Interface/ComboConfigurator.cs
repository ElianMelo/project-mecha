using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

// Scene authoring UI uses private drafts. Only the explicit editor Save action edits assets.
public sealed class ComboConfigurator : MonoBehaviour
{
    [SerializeField] private ComboConfiguratorSettings settings;
    [SerializeField] private PlayerAttack player;
    [SerializeField] private ComboTrainingTarget target;
    [SerializeField] private TMP_Text title, pattern, detail, status, previewStatus;
    [SerializeField] private UnityEngine.UI.Button ground, air, previous, next, duplicate, add, save, preview, reset;
    [SerializeField] private GameObject[] rows;
    [SerializeField] private TMP_Text[] rowLabels;
    [SerializeField] private UnityEngine.UI.Button[] piecePrevious, pieceNext, moveUp, moveDown, remove;
    private ComboDefinition selected, draft;
    private ComboLoadout working;
    private ComboDomain domain;
    private int selectedRow;
    private Coroutine playing;
    public ComboDefinition Draft => draft;
    public ComboDefinition Selected => selected;

    private void Start()
    {
        working = Instantiate(settings.loadout);
        ground.onClick.AddListener(() => SelectDomain(ComboDomain.Ground));
        air.onClick.AddListener(() => SelectDomain(ComboDomain.Air));
        previous.onClick.AddListener(() => SelectPreset(-1)); next.onClick.AddListener(() => SelectPreset(1));
        duplicate.onClick.AddListener(Duplicate); add.onClick.AddListener(AddStep); save.onClick.AddListener(Save);
        preview.onClick.AddListener(Preview); reset.onClick.AddListener(ResetPreview);
        for (int i = 0; i < rows.Length; i++)
        {
            int slot = i;
            piecePrevious[i].onClick.AddListener(() => CyclePiece(slot, -1));
            pieceNext[i].onClick.AddListener(() => CyclePiece(slot, 1));
            moveUp[i].onClick.AddListener(() => MoveStep(slot, -1)); moveDown[i].onClick.AddListener(() => MoveStep(slot, 1));
            remove[i].onClick.AddListener(() => RemoveStep(slot));
        }
        SelectDomain(ComboDomain.Ground); ResetPreview();
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(ground.gameObject);
#if !UNITY_EDITOR
        save.interactable = duplicate.interactable = false;
        status.text = GameText.Get("Combat", "save.editorOnly");
#endif
    }
    private void Update()
    {
        if (player != null && target != null)
            previewStatus.text = GameText.Get("Combat", "preview.stats", (GameText.Term("Combat", player.CurrentAction)), (target.Hits), (target.TotalDamage), (settings.combat.damageMultiplier));
    }
    public void SelectDomain(ComboDomain value)
    {
        domain = value;
        Load(value == ComboDomain.Ground ? settings.loadout.groundCombo : settings.loadout.airCombo);
    }
    public void SelectPreset(int direction)
    {
        var choices = settings.presets.Where(p => p != null && p.domain == domain).ToArray();
        if (choices.Length == 0) return;
        int index = System.Array.IndexOf(choices, selected);
        Load(choices[(index + direction + choices.Length) % choices.Length]);
    }
    private void Load(ComboDefinition source)
    {
        if (draft != null) Destroy(draft);
        selected = source; draft = Instantiate(source);
        draft.attacks = (source.attacks ?? System.Array.Empty<AttackPiece>()).Where(p => p != null).ToArray(); selectedRow = 0;
        status.text = GameText.Get("Combat", "draft.loaded");
        Refresh();
    }
    public void CyclePiece(int slot, int direction)
    {
        if (slot < 0 || slot >= draft.attacks.Length) return;
        int index = settings.combat.IndexOf(draft.attacks[slot]);
        draft.attacks[slot] = settings.combat.pieces[(index + direction + settings.combat.pieces.Length) % settings.combat.pieces.Length];
        selectedRow = slot; Changed();
    }
    public void MoveStep(int slot, int direction)
    {
        int destination = slot + direction;
        if (slot < 0 || slot >= draft.attacks.Length || destination < 0 || destination >= draft.attacks.Length) return;
        var piece = draft.attacks[slot]; draft.attacks[slot] = draft.attacks[destination]; draft.attacks[destination] = piece;
        selectedRow = destination; Changed();
    }
    public void RemoveStep(int slot)
    {
        if (draft.attacks.Length <= 1 || slot < 0 || slot >= draft.attacks.Length) return;
        draft.attacks = draft.attacks.Where((_, i) => i != slot).ToArray();
        selectedRow = Mathf.Clamp(selectedRow, 0, draft.attacks.Length - 1); Changed();
    }
    public void AddStep()
    {
        if (draft.attacks.Length >= settings.maximumSteps) return;
        draft.attacks = draft.attacks.Concat(new[] {draft.attacks.LastOrDefault() ?? settings.combat.pieces[0]}).ToArray();
        selectedRow = draft.attacks.Length - 1; Changed();
    }
    private void Changed() { status.text = GameText.Get("Combat", "draft.unsaved"); Refresh(); }
    private void Refresh()
    {
        if (playing != null) { StopCoroutine(playing); playing = null; }
        title.text = selected.Label; pattern.text = draft.Inputs;
        ground.interactable = domain != ComboDomain.Ground; air.interactable = domain != ComboDomain.Air;
        for (int i = 0; i < rows.Length; i++)
        {
            bool active = i < draft.attacks.Length; rows[i].SetActive(active); if (!active) continue;
            rowLabels[i].text = GameText.Get("Combat", "step.summary", (i + 1), (draft.attacks[i].Label));
            moveUp[i].interactable = i > 0; moveDown[i].interactable = i < draft.attacks.Length - 1;
            remove[i].interactable = draft.attacks.Length > 1;
        }
        if (draft.attacks.Length > 0)
        {
            var data = draft.attacks[Mathf.Clamp(selectedRow, 0, draft.attacks.Length - 1)].action;
            detail.text = GameText.Get("Combat", "step.details", (selectedRow + 1), (GameText.Term("Combat", data.kind)), (data.impact), (data.cancel), (data.duration), (data.damage * settings.combat.damageMultiplier), (data.finalDamage * settings.combat.damageMultiplier), (data.pulses));
        }
        else detail.text = GameText.Get("Combat", "sequence.empty");
        preview.interactable = draft.attacks.Length > 0;
        add.interactable = draft.attacks.Length < settings.maximumSteps;
        working.groundCombo = domain == ComboDomain.Ground ? draft : settings.loadout.groundCombo;
        working.airCombo = domain == ComboDomain.Air ? draft : settings.loadout.airCombo;
        player.UseLoadout(working);
    }
    public void Save()
    {
#if UNITY_EDITOR
        if (draft.attacks.Length == 0) { status.text = GameText.Get("Combat", "save.empty"); return; }
        UnityEditor.Undo.RecordObject(selected, "Save combo sequence");
        selected.attacks = draft.attacks.ToArray(); selected.inputWindow = draft.inputWindow; selected.loop = draft.loop;
        UnityEditor.EditorUtility.SetDirty(selected);
        UnityEditor.Undo.RecordObject(settings.loadout, "Select gameplay combo");
        if (domain == ComboDomain.Ground) settings.loadout.groundCombo = selected; else settings.loadout.airCombo = selected;
        UnityEditor.EditorUtility.SetDirty(settings.loadout); UnityEditor.AssetDatabase.SaveAssets();
        status.text = GameText.Get("Combat", "save.success");
#endif
    }
    public void Duplicate()
    {
#if UNITY_EDITOR
        var copy = Instantiate(draft); copy.displayName = selected.Label + GameText.Get("Combat", "preset.copySuffix"); copy.name = copy.displayName;
        string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/Settings/Combos/" + copy.name + ".asset");
        UnityEditor.AssetDatabase.CreateAsset(copy, path);
        var collection = UnityEditor.Localization.LocalizationEditorSettings.GetStringTableCollection("Combat");
        string stem = string.Join(".", System.Text.RegularExpressions.Regex.Matches(copy.name.ToLowerInvariant(), "[a-z0-9]+")
            .Cast<System.Text.RegularExpressions.Match>().Take(3).Select(match => match.Value));
        if (string.IsNullOrEmpty(stem)) stem = "combo.copy";
        var table = collection.StringTables.First(t => t.LocaleIdentifier.Code == "en");
        string key = stem; int suffix = 2;
        while (collection.SharedData.GetEntry(key) != null) key = stem + suffix++;
        var entry = table.AddEntry(key, copy.displayName);
        copy.displayNameLocalized = new UnityEngine.Localization.LocalizedString("Combat", entry.KeyId);
        UnityEditor.EditorUtility.SetDirty(table); UnityEditor.EditorUtility.SetDirty(collection.SharedData); UnityEditor.EditorUtility.SetDirty(copy);
        UnityEditor.Localization.LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(this, collection);
        UnityEditor.Undo.RecordObject(settings, "Add combo preset");
        settings.presets = settings.presets.Concat(new[] {copy}).ToArray();
        UnityEditor.EditorUtility.SetDirty(settings); UnityEditor.AssetDatabase.SaveAssets(); Load(copy);
#endif
    }
    public void ResetPreview()
    {
        if (playing != null) { StopCoroutine(playing); playing = null; }
        player.Interrupt(); player.ResetPracticeResources(); player.GetComponent<PlayerMovement>().ResetTraversal();
        var body = player.GetComponent<Rigidbody>(); body.position = settings.previewSpawn; body.linearVelocity = Vector3.zero;
        player.GetComponent<PlayerMovement>().Face(1); target.ResetTarget(); Physics.SyncTransforms();
    }
    public void Preview()
    {
        if (draft.attacks.Length == 0) return;
        ResetPreview(); playing = StartCoroutine(PlaySequence());
    }
    private IEnumerator PlaySequence()
    {
        yield return new WaitForFixedUpdate();
        if (domain == ComboDomain.Air)
        {
            player.GetComponent<PlayerMovement>().HandleJump();
            yield return new WaitForSeconds(0.12f);
        }
        foreach (var piece in draft.attacks)
        {
            player.HandleAttack();
            yield return new WaitForSeconds(piece.action.cancel + 0.04f);
        }
        playing = null;
    }
    private void OnDestroy()
    {
        if (draft != null) Destroy(draft);
        if (working != null) Destroy(working);
    }
}
