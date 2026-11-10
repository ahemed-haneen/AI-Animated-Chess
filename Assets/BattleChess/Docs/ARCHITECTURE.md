# Architecture

```
Bootstrap (scene object)
 ├─ CameraRig        orbit camera, input (drag / pinch / tap), cinematic close-ups, shake
 ├─ BoardView        board surface, coordinates, highlights
 ├─ Fx / SoundBank   particles, flashes, projectiles / synthesised sounds
 ├─ Hud              uGUI built in code; panels for new game, promotion, game over
 ├─ BattleDirector   plays one move: walk, castle, capture set piece, promotion
 └─ GameController   rules (BattleChess.Core), AI on a worker thread, undo, save/load
        └─ PieceActor x32   one per piece; Animator-driven or PlaceholderFactory figure
```

**Data:** a `ThemeDefinition` asset holds two armies × six piece types. Each `PieceDefinition` has a display name, prefab, attack style (Slash, Thrust, Smash, Shoot, Cannon or Charge), locomotion (Walk, Gallop, Roll or Sail), impact timing, and optional sound and VFX overrides.

**Animator contract:** the parameters are `Speed` (float), `Selected` (bool), and the triggers `Attack`, `Hit`, `Die` and `Victory`. The Attack state is tagged `"Attack"`. An optional `Impact` animation event goes through `AnimationEventRelay`.

**Capture sequence (BattleDirector.Play):**
1. Camera close-up (`CameraRig.BeginCloseUp`).
2. Melee: the attacker approaches to within 0.7 squares. Ranged: the attacker turns to aim.
3. `PieceActor.Attack` plays the attack. At impact, sparks and sound fire, slow motion kicks in (0.35×) and the victim's `Die` starts.
4. Camera returns, and the attacker walks onto the captured square.

**Next steps to make it TV-worthy:**
- Swap the camera close-ups for Cinemachine virtual cameras plus Timeline sequences per attack style.
- Use ragdolls on death (add a `Ragdoll` component to the prefab, switch it on in `Die`).
- Add URP post-processing (bloom, depth of field during close-ups, colour grading) as a Volume on a quality tier for high-end devices and TV.
- Build environment scenes for each theme.
