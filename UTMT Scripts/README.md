# UTMT Scripts

Standalone scripts for [UndertaleModTool](https://github.com/UnderminersTeam/UndertaleModTool).
They work on their own — the GameMaker-Mobiler application is not required.

## MobileTouchControls.csx

Adds mobile touch controls to any GameMaker game, reading the keys straight out of `data.win`.

### How to use

1. Open your `data.win` in UndertaleModTool.
2. `Scripts` → `Run other script...` → select `MobileTouchControls.csx`.
3. Confirm the detected key layout in the dialog that appears.
4. `File` → `Save` (or `Save as...`).

### What it does

**Detects the keys the game really uses.** It walks the bytecode of every code entry and
looks for `keyboard_check`, `keyboard_check_pressed`, `keyboard_check_released`,
`keyboard_check_direct`, `keyboard_key_press` and `keyboard_key_release`, resolving both plain
numeric key codes and `ord("X")` forms. Keys are ranked by how often they are referenced, so the
most important ones end up on screen. This matters because every game uses a different key set.

**Injects a touch layer.** Movement keys (arrow keys, or WASD when the game uses those instead)
become an analog joystick with 8-way output and a dead zone. Every other detected key becomes a
round button. Up to 8 buttons are placed, arranged three per row in the bottom-right corner.
Multi-touch is supported (5 fingers).

**EDIT mode.** Hold the gear button in the top-right corner for 0.7 seconds:

| Row | Action |
| --- | --- |
| Button size | `[-]` / `[+]` — scale of all buttons |
| Joystick size | `[-]` / `[+]` — scale of the joystick |
| Opacity | `[-]` / `[+]` — transparency of all controls |
| Show / hide | toggles the currently selected control |
| Selected size | `[-]` / `[+]` — scale of the selected button only |
| Touch ON/OFF · Save & exit | disable touch controls entirely, or save and leave |

Drag any button or the joystick to move it. The selected control is highlighted in yellow,
hidden ones in red. The layout is written to `gmm_touch.ini` and restored on the next launch.

**Mobile optimization** (optional, on by default). Disables texture interpolation and repeat,
lowers circle drawing precision, turns off anti-aliasing and caps `application_surface` at
1080p. This is the cheapest way to gain FPS on weak phones and needs **no YYC compiler**.

### Configuration

Edit the constants near the top of the script before running it:

```csharp
const int MaxButtons = 8;          // how many buttons to place at most
bool enableOptimization = true;    // mobile performance patch
bool enableJoystick = true;        // set false for buttons only
const int ResolutionCap = 1080;    // lower to 720 for very weak phones
```

### Compatibility

The generated GML avoids every 2.3+ language feature (no structs, function literals, accessors
or `??`) and creates no script assets, because the script mechanism changed drastically in
GMS 2.3. It therefore compiles on **GameMaker 1.4 through 2024.x**.

Nothing but built-in drawing functions is used, so **no sprite or font assets are needed** —
the controls cannot break because an asset is missing.

### Notes

- The object is called `obj_gmm_touch`, is persistent and sits at depth `-100000`.
- Running the script twice is safe: the code is replaced and the room instance is not duplicated.
- If no keyboard calls are found (for example the game reads input through an extension), the
  script falls back to arrow keys + `Z` / `X` / `C` / `Enter` / `Esc`.
- Held keys are always released on room change and on cleanup, so nothing gets stuck.
