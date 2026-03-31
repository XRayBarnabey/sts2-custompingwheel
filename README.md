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
- [x] ~~Vérifier l'ID alphabétique de `PingPresetMessage` via dump `INetMessageSubtypes.All`~~ — voir section **Recherche réseau** ci-dessous

## Recherche réseau — `INetMessageSubtypes` / `_subtypes`

### Résultats de la recherche dans ce dépôt

| Terme | Trouvé dans le mod ? | Notes |
|---|---|---|
| `INetMessageSubtypes` | ❌ | Classe privée du DLL du jeu (`sts2.dll`), non exposée aux mods |
| `_subtypes` | ❌ | Champ privé backing de `INetMessageSubtypes.All`, inaccessible depuis le mod |
| `MessageTypes` | ✅ (commentaires) | Classe du jeu qui construit le cache vanilla + mods au démarrage |
| `PingPresetMessage` | ✅ | `CustomPingWheelCode/Network/PingPresetMessage.cs` |

### Chaîne de découverte automatique

```
ModManager.LoadedMods              ← DLLs des mods chargés
  ↓
ReflectionHelper.ModTypes          ← tous les Types de toutes les DLLs de mods (lazy, une fois)
  ↓
ReflectionHelper.GetSubtypesInMods<INetMessage>()
  ↓
MessageTypes static ctor           ← vanilla (INetMessageSubtypes._subtypes) + mods, triés alphabétiquement
  ↓
NetTypeCache                       ← lookup byte ID → Type (désérialisation réseau)
```

`INetMessageSubtypes._subtypes` est le tableau interne des types vanilla. Le mod n'a pas besoin
d'y accéder : `MessageTypes` l'inclut automatiquement et ajoute les types mods détectés par réflexion.

### Position alphabétique de `PingPresetMessage`

- Nom : **`PingPresetMessage`**
- Commence par **`P`** → s'intercale **après** tous les types vanilla commençant par `A`–`O`
  et **avant** tous ceux commençant par `Q`–`Z`.
- Dans le bucket `P` : vient après `Pa…`–`Ph…` et avant `Po…`–`Py…`.
- L'ID numérique exact dépend de la liste complète vanilla ; pour le connaître à l'exécution :

```csharp
GD.Print($"[CPW] PingPresetMessage ID = {MessageTypes.TypeToId<PingPresetMessage>()}");
```

### Conditions de fonctionnement

1. La DLL du mod doit être chargée par `ModManager` **avant** la première utilisation de `MessageTypes`.
2. **Les deux joueurs doivent avoir le mod chargé** — sinon les IDs divergent et `NetMessageBus`
   lève `IndexOutOfRangeException` chez le joueur sans le mod.

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