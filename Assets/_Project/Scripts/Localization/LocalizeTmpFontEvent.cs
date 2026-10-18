using System;
using TMPro;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

[Serializable] public sealed class TmpFontEvent : UnityEvent<TMP_FontAsset> { }

// The package owns locale selection, Addressables loading and asset-change subscriptions.
public sealed class LocalizeTmpFontEvent : LocalizedAssetEvent<TMP_FontAsset, LocalizedTmpFont, TmpFontEvent> { }
