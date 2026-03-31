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

- [x] **Intercepter le clic sur le bouton Ping** — `FlavorSynchronizer_Patch.cs` (Harmony Prefix sur `SendEndTurnPing`)
- [ ] Interface UI pour choisir le preset (wheel overlay) — voir section ILSpy ci-dessous
- [ ] Presets configurables (fichier JSON ou config BaseLib)
- [ ] Debounce custom 1 s dans le patch — **known issue** : le debounce vanilla est court-circuité par le Prefix, permettant du spam. À implémenter avant la wheel UI via `Traverse.Create(__instance).Field("_nextAllowedPingTime")`

## Architecture

```
CustomPingWheelInit.cs                    ← [ModInitializerAttribute("Initialize")] — point d'entrée
CustomPingWheelState.cs                   ← état global, SendPreset(), OnReceive(), helpers
Network/PingPresetMessage.cs              ← struct INetMessage — auto-découvert par MessageTypes
Patches/RunManager_Patch.cs              ← Harmony Postfix sur RunManager.InitializeShared
Patches/FlavorSynchronizer_Patch.cs      ← Harmony Prefix sur FlavorSynchronizer.SendEndTurnPing
```

---

## Cartographie du ping vanilla (sts2 v0.1.0.0)

### Flux complet depuis le bouton UI jusqu'à l'envoi réseau

```
[Clic bouton Ping dans l'UI combat]
  ↓
NCombatUi.PingButton  (propriété de type NPingButton)
  ↓
NPingButton  (MegaCrit.Sts2.Core.Nodes.Combat)
  ↓ [signal ou _Pressed — voir section ILSpy §1]
FlavorSynchronizer.SendEndTurnPing()
  ↓ [garde debounce interne : Time.GetTicksMsec() >= _nextAllowedPingTime]
  ├── _gameService.SendMessage(default(EndTurnPingMessage))        ← envoi réseau vanilla
  ├── _nextAllowedPingTime = Time.GetTicksMsec() + 1000            ← cooldown 1 s
  └── CreateEndTurnPingDialogueIfNecessary(LocalPlayer)            ← bulle locale vanilla
       ↓ [réseau, côté remote]
NMultiplayerPlayerState.OnPlayerEndTurnPing(...)
  ↓
FlavorSynchronizer.OnEndTurnPingReceived                           ← affichage remote
```

### Dump ILSpy confirmé — `FlavorSynchronizer.SendEndTurnPing` (sts2 v0.1.0.0)

```csharp
// MegaCrit.Sts2.Core.Multiplayer.Game.FlavorSynchronizer
// using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Flavor;

public void SendEndTurnPing()
{
    if (Time.GetTicksMsec() >= _nextAllowedPingTime)
    {
        _gameService.SendMessage(default(EndTurnPingMessage));
        _nextAllowedPingTime = Time.GetTicksMsec() + 1000;
        CreateEndTurnPingDialogueIfNecessary(LocalPlayer);
    }
}
```

**Points clés** :
- Méthode d'instance, aucun paramètre → Prefix Harmony fonctionne sans injection `__instance`
- Le debounce est **dans** la méthode — avec `return false` on le court-circuite. À gérer dans le patch.
- Namespace message vanilla : `MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Flavor`

### Classes et membres identifiés (ILSpy search `Ping`)

| Symbole | Namespace | Rôle |
|---|---|---|
| `NPingButton` | `MegaCrit.Sts2.Core.Nodes.Combat` | Classe du bouton Ping UI |
| `NCombatUi.PingButton : NPingButton` | `MegaCrit.Sts2.Core.Nodes.Combat.NCombatUi` | Propriété du bouton dans le HUD combat |
| `FlavorSynchronizer.SendEndTurnPing()` | `MegaCrit.Sts2.Core.Multiplayer.Game` | **Point d'injection principal** ✅ |
| `FlavorSynchronizer.SendMapPing(MapCoord, ...)` | idem | Ping sur la carte |
| `FlavorSynchronizer.CreateMapPing(MapCoord, ...)` | idem | Création ping carte |
| `FlavorSynchronizer.HandleMapPingMessage(...)` | idem | Handler réception ping carte |
| `FlavorSynchronizer.OnEndTurnPingReceived` | idem | Callback réception ping fin de tour |
| `FlavorSynchronizer._pingDebounceMsec : ulong` | idem | Debounce anti-spam |
| `FlavorSynchronizer._nextAllowedPingTime : ulong` | idem | Timestamp prochain ping autorisé |
| `FlavorSynchronizer._endTurnDialogues : Dictionary<...>` | idem | Dictionnaire dialogues fin de tour |
| `EndTurnPingMessage` | `MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Flavor` | INetMessage vanilla fin de tour |
| `MapPingMessage` | `MegaCrit.Sts2.Core.Multiplayer.Messages.Game` | INetMessage vanilla carte |
| `NMultiplayerPlayerState.OnPlayerEndTurnPing(...)` | `MegaCrit.Sts2.Core.Multiplayer` | Handler réseau côté réception |
| `NMapPingVfx` | `MegaCrit.Sts2.Core.Nodes.Vfx` | VFX ping sur la carte |

---

## Flux réseau CustomPingWheel

```
[Clic bouton Ping]
  ↓
FlavorSynchronizer.SendEndTurnPing()
  ↓ [Harmony Prefix — return false → ping vanilla supprimé]
CustomPingWheelState.SendPreset(index)
  ├── ShowBubble(localCreature, text)              [local immédiat]
  └── _net.SendMessage(PingPresetMessage{index})   [réseau]
       ↓ [côté remote]
  OnReceive(msg, senderId)
       ├── FindCreature(senderId)
       └── ShowBubble(remoteCreature, text)
```

---

## INetMessageSubtypes — position de `PingPresetMessage`

Les types `INetMessage` sont auto-découverts **alphabétiquement** par `ReflectionHelper.GetSubtypesInMods`.
Il y a **49 types vanilla**. `PingPresetMessage` s'intercale entre :

```
... [29] PeerInputMessage
    [30] PingPresetMessage   ← notre message custom (aucun type vanilla "PingP*")
    [31] PlayerChoiceMessage
...
```

> ⚠️ Les deux joueurs doivent avoir le mod chargé. Si un seul côté a le mod,
> les IDs de bytes divergent et le join crash avec `IndexOutOfRangeException`.

---

## Structure des mods de référence (patterns Harmony)

### Pattern général d'un mod sts2 avec Harmony

Tout mod qui patche du code vanilla suit ce schéma :

```
MonMod/
├── MonMod.json          ← manifest (has_dll: true)
├── MonMod.dll           ← compilé depuis .csproj
└── MonModCode/
    ├── MonModInit.cs    ← [ModInitializerAttribute("Initialize")] + harmony.PatchAll()
    └── Patches/
        └── SomeClass_Patch.cs   ← [HarmonyPatch] + [HarmonyPrefix/Postfix]
```

### `[ModInitializerAttribute]` — point d'entrée

```csharp
// Appelé par le ModLoader au démarrage, avant InitializeShared
[ModInitializerAttribute("Initialize")]
public static class MyModInit
{
    public static void Initialize()
    {
        var harmony = new Harmony("Author.MyMod");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}
```

### `RunManager.InitializeShared` — hook réseau

```csharp
// Appelé après la mise en place du service réseau. Utiliser pour RegisterMessageHandler.
[HarmonyPatch(typeof(RunManager), "InitializeShared")]
public static class RunManager_InitializeShared_Patch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        var net = RunManager.Instance?.NetService;
        net?.RegisterMessageHandler<MyMessage>(OnReceive);
    }
}
```

### `FlavorSynchronizer.SendEndTurnPing` — hook bouton Ping combat

```csharp
// Intercepte le clic sur le bouton Ping avant l'envoi réseau vanilla.
// return false → supprime le ping vanilla ; return true → fallback vanilla.
[HarmonyPatch(typeof(FlavorSynchronizer), nameof(FlavorSynchronizer.SendEndTurnPing))]
public static class FlavorSynchronizer_SendEndTurnPing_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(FlavorSynchronizer __instance)
    {
        // logique custom ici
        return false; // ou true pour fallback
    }
}
```

### Accès à `FlavorSynchronizer` depuis un patch

`SendEndTurnPing` est une méthode d'instance. Le paramètre `__instance` donne accès à l'objet.
Pour accéder aux champs privés (`_nextAllowedPingTime`, `_gameService`) depuis un patch externe,
utiliser `Traverse` (HarmonyLib) :

```csharp
// Lire le debounce vanilla depuis le patch
ulong nextAllowed = Traverse.Create(__instance).Field("_nextAllowedPingTime").GetValue<ulong>();
```

---

## Recherches ILSpy restantes

### 🔴 Priorité 1 — `NPingButton` classe complète

```
ILSpy → MegaCrit.Sts2.Core.Nodes.Combat → NPingButton
```

**But** : savoir quel signal/méthode est appelé au clic. Si `NPingButton` hérite de `Button` Godot,
il y a probablement un `_Pressed()` ou un signal `pressed` connecté à `FlavorSynchronizer.SendEndTurnPing`.
Confirmer si le bouton est recréé à chaque combat (important pour la robustesse du patch Prefix).

**Quoi chercher** :
- Signature du constructeur (y a-t-il un `FlavorSynchronizer` injecté ?)
- Méthode `_Pressed()` ou handler `Pressed` connecté
- Champ `_flavorSynchronizer` ou équivalent

### 🟠 Priorité 2 — `FlavorSynchronizer` — accès à l'instance

```
ILSpy → MegaCrit.Sts2.Core.Multiplayer.Game.FlavorSynchronizer → champs + propriétés
```

**But** : confirmer comment obtenir l'instance de `FlavorSynchronizer` depuis l'extérieur.
Est-ce un singleton (`FlavorSynchronizer.Instance`) ? Accessible via `NCombatRoom` ?
Via `RunManager` ? Utile si on veut lire/écrire `_nextAllowedPingTime` pour implémenter
le debounce custom.

**Quoi chercher** :
- Propriété `static Instance` ou `static Current`
- Présence dans `NCombatRoom` (propriété `FlavorSynchronizer` ?)
- Présence dans `RunManager.Instance`

### 🟠 Priorité 3 — `EndTurnPingMessage` struct complète

```
ILSpy → MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Flavor → EndTurnPingMessage
```

**But** : confirmer la structure du message vanilla (champs, `ShouldBroadcast`, `Mode`).
Vérifier qu'il n'y a pas de payload supplémentaire qu'il faudrait reproduire dans `PingPresetMessage`.

### 🟡 Priorité 4 — `FlavorSynchronizer.CreateEndTurnPingDialogueIfNecessary`

```
ILSpy → FlavorSynchronizer → CreateEndTurnPingDialogueIfNecessary
```

**But** : comprendre ce que fait la bulle vanilla (quelle chaîne, quel VFX) pour pouvoir
la remplacer proprement ou la réutiliser avec le texte du preset.

### 🟡 Priorité 5 — `FlavorSynchronizer.OnEndTurnPingReceived`

```
ILSpy → FlavorSynchronizer → OnEndTurnPingReceived
```

**But** : comprendre l'affichage côté remote (bulle, VFX, son). À reproduire dans `OnReceive`.

### 🟡 Priorité 6 — UI wheel overlay (ouverture avant envoi)

Pour afficher une roue de presets **avant** l'envoi réseau, il faut soit :
- Créer un nœud Godot overlay dans `NCombatRoom` (instancié depuis le mod)
- Ou bloquer l'envoi (Prefix retourne `false`) et déclencher l'overlay Godot

```
ILSpy → MegaCrit.Sts2.Core.Nodes.Combat.NCombatRoom → champs + AddChild pattern
ILSpy → MegaCrit.Sts2.Core.Nodes.Combat → NActionWheelUi (si existe)
ILSpy → Ctrl+Shift+F → "wheel" ou "overlay" ou "radial" dans MegaCrit.Sts2.Core.Nodes.Combat
```

**Quoi chercher** :
- Y a-t-il déjà une classe `NWheelMenu` / `NRadialMenu` / `NActionWheel` réutilisable ?
- Comment d'autres UI modales de combat sont-elles ouvertes (ex. menu cartes) ?

---

## Flux d'exploration recommandé (avant tout nouveau code)

1. **Dumper `NPingButton`** (ILSpy) → confirmer le handler du clic → ajuster le patch si nécessaire
2. **Dumper `FlavorSynchronizer` champs** → confirmer l'accès à l'instance + champs privés
3. **Chercher une wheel UI existante** (`NActionWheelUi`, `NRadialMenu`) dans le jeu
4. Si wheel UI trouvée → l'instancier depuis le mod en Prefix (bloquer, afficher, envoyer au choix)
5. Si wheel UI absente → créer un overlay Godot minimal dans le mod (Control + 5 boutons)