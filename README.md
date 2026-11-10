# AI Animated Chess — Unity starter project

A mobile-first 3D battle chess game where every capture is a cinematic fight. Built to scale from phones (portrait) to TVs (landscape / screen mirroring).

**First theme:** Pirates vs British. Every piece has a name, attack style and placeholder model:

| Piece | Pirates (move first) | British Army |
|---|---|---|
| King | Pirate Captain — cutlass | Governor — sword |
| Queen | Pirate Queen — pistol | Admiral — sword |
| Rook | Pirate Ship — bow cannon | Cannon — fires cannonballs |
| Bishop | Gunner — blunderbuss | Officer — sword |
| Knight | Swashbuckler — leaps | Dragoon — gallops on horseback |
| Pawn | Deckhand — cutlass | Redcoat — musket |

The game is **fully playable right now with placeholder figures**. Real characters are dropped in later without touching code.

---

## What you need

- A Windows, Mac or Linux computer (the Unity editor does not run on phones)
- [Unity Hub](https://unity.com/download) with **Unity 6 LTS** installed
  - When installing, add **Android Build Support** and/or **iOS Build Support**

## Setup (about 10 minutes)

1. **Create the Unity project.** In Unity Hub, click **New project** → choose the **Universal 3D** template → name it `AI-Animated-Chess` → **Create**.
2. **Add this code.** Download this repository (green **Code** button → **Download ZIP**), unzip it, and copy the `Assets/BattleChess` folder into your new project's `Assets` folder. Unity imports it automatically.
3. **Build the scene.** In Unity's top menu bar click **Battle Chess → 1. Create Game Scene**.
4. **Press Play** ▶. You're playing.
5. Optional: **Battle Chess → 3. Apply Mobile + TV Player Settings** (auto-rotate between portrait and landscape).
6. Optional sanity check: **Battle Chess → Run Engine Self-Test** — prints PASSED in the Console if the chess rules engine is working.

> Tip: once the project opens fine, you can make the whole Unity project folder the Git repository (this repo's `.gitignore` is already set up for Unity).

## Controls

- **Tap** a piece to select it, tap a highlighted square to move.
- **Drag** to rotate the view, **pinch** (or mouse wheel) to zoom.
- **Action cam** button toggles the cinematic close-ups on captures.
- **Rotate** turns the board around; **Undo** takes back a move (two in vs-computer mode).

## Try it on your phone

- **Android:** File → Build Profiles → Android → Switch Platform → connect your phone with USB debugging on → **Build And Run**.
- **iPhone:** Switch Platform to iOS → Build → open the generated Xcode project on a Mac → run on your device.
- **TV:** mirror your phone (AirPlay / Chromecast / Smart View), or build for **Android TV** (Android platform) or **Apple TV** (tvOS platform). The camera automatically re-frames for landscape screens.

---

## Replacing placeholders with real characters

1. Import a **rigged humanoid** character (FBX) plus its animation clips into one folder, e.g. `Assets/Characters/Redcoat/`.
   Free route for prototyping: [Mixamo](https://www.mixamo.com) — upload or pick a character, download it, then download animations "Without Skin".
2. Name clips so the wizard can find them: `idle`, `walk`, `run`, `attack`, `hit`, `death`, `victory`, `selected` (names just need to *contain* these words).
3. Select the character model in the Project window → **Battle Chess → Make Character From Selected Model**.
   This creates an Animator Controller and a `_Piece` prefab.
4. Select `Assets/BattleChess/Resources/Themes/PiratesVsBritish` → find the piece (e.g. *Black → Pawn*) → drag the prefab into **Prefab**.
5. For shooters, add an empty child object named **Muzzle** at the gun tip so shots come from the right place.
6. For a perfectly timed hit, add an **animation event** called `Impact` on the attack clip at the moment of contact (otherwise the game uses the *Impact At* slider).

See `Assets/BattleChess/Docs/ART_GUIDE.md` for the full list of characters, animations and effects needed for this theme.

## Adding more themes

Right-click in the Project window → **Create → Battle Chess → Theme**, fill in both armies (names, attack styles, prefabs, colours), and drag it into the **Theme** field of the *Battle Chess* object in the scene. Planned themes from the web prototype: Olympians vs Titans, Survivors vs Zombies, Cats vs Dogs.

---

## Project layout

```
Assets/BattleChess/
  Scripts/Core/ChessEngine.cs      Chess rules + AI (pure C#, verified with standard perft tests)
  Scripts/Game/GameController.cs   Turns, selection, AI thread, undo, promotion, save/load
  Scripts/Game/BattleDirector.cs   Cinematic capture sequences (close-up, strike, slow-mo, death)
  Scripts/Game/PieceActor.cs       One piece: Animator-driven or placeholder animation
  Scripts/Game/CameraRig.cs        Auto-fit orbit camera, drag/pinch, close-ups, shake
  Scripts/Game/BoardView.cs        Board, coordinates, highlights
  Scripts/Game/Hud.cs              Code-built UI (works portrait + landscape, safe areas)
  Scripts/Game/ThemeDefinition.cs  Theme asset format
  Scripts/Game/ThemeLibrary.cs     Pirates vs British defaults
  Scripts/Game/PlaceholderFactory  Primitive stand-in characters
  Scripts/Game/Fx.cs, SoundBank.cs Procedural particles and synthesised sounds
  Scripts/Editor/                  "Battle Chess" menu + character setup wizard
  Docs/                            Art guide and architecture notes
```

## Status

- ✅ Rules engine ported from the web prototype and verified (perft: start pos, Kiwipete, endgame and promotion positions; AI finds mate-in-one)
- ✅ All C# compiled against the Unity API surface it uses, with both the new Input System and the legacy Input Manager
- ⏳ Not yet opened in the Unity editor — expect a few small fixes on first import; share the Console errors and they'll be fixed quickly
