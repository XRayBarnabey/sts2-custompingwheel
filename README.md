# CustomPingWheel — Slay the Spire 2 Mod

Remplace le ping par défaut en combat multijoueur par une **roue de messages prédéfinis**.

## Fonctionnalités

- Affiche une bulle de dialogue au-dessus du personnage avec le message choisi
- Synchronise le message avec le(s) autre(s) joueur(s) via un message réseau custom
- 5 presets configurables (actuellement en dur dans le code)

## Presets par défaut

1. J'ai besoin d'aide ici !
2. Je prends cette route.
3. Attention à cet ennemi !
4. Je vais ouvrir ce coffre.
5. On se regroupe ici.

## Prérequis

- **Slay the Spire 2** installé via Steam
- **MegaDot Godot 4.5.1** (`MegaDot_v4.5.1-stable_mono_win64.exe`) pour la compilation
- .NET 9 SDK

## Build

```bash
dotnet build -c Release
```

Le `.dll` et le `.json` sont copiés automatiquement dans `mods/CustomPingWheel/`.

## Installation manuelle

Copier dans `<STS2>/mods/CustomPingWheel/` :
- `CustomPingWheel.dll`
- `CustomPingWheel.json`

## Limitation importante

> ⚠️ **Les deux joueurs doivent avoir le mod installé et activé** pour que la synchronisation réseau fonctionne.  
> Si un seul côté a le mod, le join peut crash avec `IndexOutOfRangeException` dans `NetMessageBus`.

## TODO

- [x] **Intercepter le clic sur le bouton Ping** — patch Harmony prefix sur `NPingButton.OnRelease`
- [ ] Interface UI pour choisir le preset (wheel overlay)
- [ ] Presets configurables (fichier JSON ou config BaseLib)
- [ ] Vérifier l'ID alphabétique de `PingPresetMessage` via dump `INetMessageSubtypes.All`

## Architecture

```
CustomPingWheelInit.cs           ← [ModInitializerAttribute("Initialize")] — point d'entrée
CustomPingWheelState.cs          ← état global, SendPreset(), OnReceive(), helpers
Network/PingPresetMessage.cs     ← struct INetMessage — auto-découvert par MessageTypes
Patches/RunManager_Patch.cs      ← Harmony postfix sur RunManager.InitializeShared (enregistre le handler réseau)
Patches/NPingButton_Patch.cs     ← Harmony prefix sur NPingButton.OnRelease (intercepte le clic ping)
```

## Flux réseau

```
Clic ping (local) → NPingButton.OnRelease [intercepté par patch Harmony prefix]
  → SendPreset(index)
      → ShowBubble(localCreature, text)       [local immédiat]
      → _net.SendMessage(PingPresetMessage)   [réseau]
           → OnReceive(msg, senderId)          [côté remote]
               → FindCreature(senderId)
               → ShowBubble(remoteCreature, text)
```

## Cartographie du Ping Vanilla — NCombatUi & NPingButton

Résultats de l'analyse ILSpy (confirmés, confiance 99 %) :

### NCombatUi
- `PingButton` (type `NPingButton`) est initialisé dans `_Ready()` via `GetNode<NPingButton>("%PingButton")`.
- Aucun signal Godot custom lié au PingButton n'est défini dans `NCombatUi`.
- Méthodes notables mentionnant `PingButton` :
  - `_Ready()` — initialisation
  - `Enable()` / `Disable()` — appelle `PingButton.RefreshEnabled()` (affiche/masque selon état combat)
  - `AnimOut()` — appelle `PingButton.OnCombatEnded()` (reset à la fin du combat)

### NPingButton (héritage NButton)
- Hérite de `NButton` ; la gestion du clic se fait via `OnRelease()`.
- **Point d'injection retenu** : `NPingButton.OnRelease` (Harmony Prefix, `return false` pour supprimer le ping vanilla).
- Le patch est dans `Patches/NPingButton_Patch.cs`.
- Si le patch ne s'applique pas (avertissement Harmony dans les logs), vérifier le namespace exact de `NPingButton` dans ILSpy — il pourrait être dans `MegaCrit.Sts2.Core.Combat.UI` plutôt que `MegaCrit.Sts2.Core.UI`.