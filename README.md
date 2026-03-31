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

- [ ] **Intercepter le clic sur le bouton Ping** (en attente du dump ILSpy du handler ping)
  - Chercher dans ILSpy : `Ctrl+Shift+F` → `Ping` dans `MegaCrit.Sts2.Core.UI` / `MegaCrit.Sts2.Core.Combat`
  - On cherche la classe qui affiche le bouton + la méthode appelée au clic
- [ ] Interface UI pour choisir le preset (wheel overlay)
- [ ] Presets configurables (fichier JSON ou config BaseLib)
- [ ] Vérifier l'ID alphabétique de `PingPresetMessage` via dump `INetMessageSubtypes.All`

## Architecture

```
CustomPingWheelInit.cs       ← [ModInitializerAttribute("Initialize")] — point d'entrée
CustomPingWheelState.cs      ← état global, SendPreset(), OnReceive(), helpers
Network/PingPresetMessage.cs ← struct INetMessage — auto-découvert par MessageTypes
Patches/RunManager_Patch.cs  ← Harmony postfix sur RunManager.InitializeShared
```

## Flux réseau

```
Clic ping (local)
  → SendPreset(index)
      → ShowBubble(localCreature, text)       [local immédiat]
      → _net.SendMessage(PingPresetMessage)   [réseau]
           → OnReceive(msg, senderId)          [côté remote]
               → FindCreature(senderId)
               → ShowBubble(remoteCreature, text)
```