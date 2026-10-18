# Game localization

English (`en`) is the sole approved locale. Adding any other language requires the user's explicit request and direct approval. Do not add regional variants, pseudo-locales, translations, automatic system-language selection, or a language selector without that approval. The build guard enforces the currently approved English-only configuration.

Unity Localization 1.5.13 is installed through the Package Manager API, with its Addressables dependency. The package settings and all game localization assets live under `Assets/_Project/Localization`. English is both the project locale and the specific startup locale. String tables preload for the selected locale. Unity's package owns locale selection, table loading, formatting, and localized asset loading.

## Source of truth

Use **Window > Asset Management > Localization Tables** to edit game wording. Collections are `Settings`, `HUD`, `Lobby`, `Routing`, `Combat`, `Input`, `Chat`, and `Diagnostics`. Dynamic formats retain their numeric placeholders and rich-text tags in the table; code supplies values through `GameText.Get`. Combat state, animation, transport property and input-action identifiers remain internal identifiers; their displayed names use table entries. Player-written pilot/lobby names and chat messages remain user content. Numeric-only damage, percentages, input characters, punctuation and native display resolutions do not need translations.

[LocalizationTextCoverage.csv](LocalizationTextCoverage.csv) records the original presentation assignment sites and how each is handled, including user-content and numeric-only exceptions. Original line numbers describe the pre-migration source.

Authored combo/attack names, encounter names/descriptions/rewards, controller help, keyboard tutorial, skill prefix, and lobby defaults use serialized `LocalizedString` references. Hidden old string fields remain migration inputs for existing setup tools; gameplay reads the localized references exclusively. Changing those hidden fields does not override table wording. New authored assets receive references on import. New combo copies get their own name entry.

`Fonts` is an Asset Table collection. Its English `Default` entry references `Fonts/EnglishDefault.asset`, a copy of the existing Liberation Sans SDF font outside Resources, registered with Addressables. Every existing game text has a `LocalizeTmpFontEvent`; new runtime floating damage text binds the same font through `GameText.BindFont`.

## Adding text

Use concise keys with two or three meaningful words, such as `hitbox.shown`, `draft.unsaved`, or `controller.disconnected`. Avoid class names, full sentences, hierarchy paths, GUIDs and formatting tokens in keys. Automatic capture follows this rule and adds a short number only when needed for uniqueness. Serialized localized references use stable entry IDs, so renaming a key preserves its existing bindings and wording.

Create TMP text normally in game scenes (`Assets/Scenes`) or project prefabs (`Assets/_Project`). The editor watches object changes, scene saving and prefab imports. A new non-empty static label automatically gets a category entry and a native `LocalizeStringEvent`, with an Editor-and-Runtime persistent setter. It also gets the English font asset binding. Once captured, edit wording in its table, not on the TMP component. Existing references and translations are preserved when capture runs again. Save the scene/prefab and localization assets as usual.

For runtime-created labels, use `GameText.Bind(text, localizedStringReference)` to bind both native string events and the localized font. For changing telemetry, names and counters, use `GameText.Get(table, key, arguments)` or `GameText.Get(serializedReference)`. Create the entry in Unity's Localization Tables first; never use English literals as runtime fallbacks. New controls should keep the established controller navigation and persistent focus feedback.

`Tools > Mecha > Localization > Localize Game Content` reapplies capture to the three game scenes, project prefabs and authored data. It restores the previous scene setup and requires existing open scenes to be saved first. Supplied vendor demonstration scenes and reference libraries are outside the game-text scope.

## Validation and maintenance

Compile in the connected Editor, inspect table completeness and event wiring, build Addressables content after asset-table changes, and inspect the relevant screens in Play Mode. Do not create or run automated tests unless the user explicitly asks. `AgentScripts/InspectLocalization.cs` provides read-only table/reference/binding and live runtime inspection, plus the Addressables content build entry point. It does not launch test runners or extra network players.

Keep package-generated Addressables settings and table `.meta` files with their assets. Rebuildable bundles in Library/Temp remain local output. These tables and settings are provisional **Authored Data**; the copied font is imported artwork of unverified origin and remains **Placeholder**. Runtime source is **Code**, editor/migration tooling is **Others**.

The controller review found that the standalone combo configurator had no first focus. It now selects Ground and reuses the existing `UiNavigationScope` and `UiInteractionFeedback` border, including when optional animation is disabled. No new navigation or focus implementation was introduced.

Manual inspection on 2026-10-17 covered options, audio, video, lobby defaults/placeholders, controller text entry, the route board and confirmation, combat HUD/room objectives, diagnostics, and the combo configurator. Runtime frames advanced; English initialization completed, all inspected text had fonts, and no missing-translation messages or new game runtime errors were found. A static audio title was cleared and refreshed through its native event; the table restored its wording and `EnglishDefault` font. Play Mode was stopped afterward and MainScene restored. Tool timeout diagnostics from background/import pauses are separate from game runtime errors. No automated tests were created, modified or run.
