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

## État d'avancement

| Fonctionnalité | Status |
|---|---|
| Auto-découverte `PingPresetMessage` dans le bus réseau | ✅ |
| `RegisterMessageHandler` dans `InitializeShared` | ✅ |
| Interception clic bouton Ping (`FlavorSynchronizer.SendEndTurnPing`) | ✅ |
| Debounce custom 1 s (même durée que vanilla) | ✅ |
| Bulle locale immédiate | ✅ |
| Synchronisation réseau remote | ✅ |
| Fallback vanilla en cas d'exception | ✅ |
| Preset hardcodé à 0 | ⚠️ temporaire |
| UI wheel de sélection | ❌ TODO |
| Presets configurables (JSON/config) | ❌ TODO |

## TODO

- [x] **Intercepter le clic sur le bouton Ping**
  - `FlavorSynchronizer.SendEndTurnPing()` patché via Harmony Prefix
  - `Patches/FlavorSynchronizer_Patch.cs`
- [ ] **Interface UI wheel de sélection de preset**
  - Afficher un overlay de sélection (radial menu ou liste) quand le joueur clique Ping
  - Remplacer le `const int presetIndex = 0` dans `FlavorSynchronizer_Patch.cs`
  - Doit s'intégrer dans l'arbre de scène Godot de `NCombatRoom`
- [ ] **Presets configurables**
  - Lire les presets depuis un fichier JSON ou via `BaseLib` config
  - Remplacer `CustomPingWheelState.Presets` (actuellement hardcodé)
- [ ] **Vérifier l'ID alphabétique de `PingPresetMessage`**
  - Dump ILSpy : `INetMessageSubtypes.All` pour confirmer position 30 (entre `PeerInputMessage` 29 et `PlayerChoiceMessage` 31)

## Architecture

```
CustomPingWheelInit.cs                  ← [ModInitializerAttribute("Initialize")] — point d'entrée
CustomPingWheelState.cs                 ← état global, SendPreset(), OnReceive(), helpers
Network/PingPresetMessage.cs            ← struct INetMessage — auto-découvert par MessageTypes
Patches/RunManager_Patch.cs             ← Harmony postfix sur RunManager.InitializeShared
Patches/FlavorSynchronizer_Patch.cs     ← Harmony prefix sur FlavorSynchronizer.SendEndTurnPing
```

## Flux réseau

```
NPingButton clic
  ↓
FlavorSynchronizer.SendEndTurnPing()
  ↓ [Harmony Prefix — return false, debounce 1 s]
CustomPingWheelState.SendPreset(index)
  ├── ShowBubble(localCreature, text)       [local immédiat]
  └── _net.SendMessage(PingPresetMessage)   [réseau]
           → OnReceive(msg, senderId)        [côté remote]
               → FindCreature(senderId)
               → ShowBubble(remoteCreature, text)
```