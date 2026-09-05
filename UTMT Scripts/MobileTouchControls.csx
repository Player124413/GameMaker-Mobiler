// =============================================================================
//  Mobile Touch Controls  --  standalone UndertaleModTool script
//  Part of GameMaker-Mobiler (https://github.com/Player124413/GameMaker-Mobiler)
// =============================================================================
//
//  WHAT IT DOES
//  ------------
//  1. Scans the bytecode of the loaded data file and detects which keyboard
//     keys the game actually uses (every game uses a different set).
//  2. Injects a fully self-drawn touch layer: an analog joystick for the
//     movement keys plus one round button per detected key.
//  3. Adds an in-game EDIT mode (hold the gear button in the top-right corner
//     for 0.7 s): drag controls around, resize them, change opacity, show or
//     hide individual buttons, or switch touch controls off completely.
//     The layout is saved to "gmm_touch.ini" next to the save files.
//  4. Optionally applies mobile performance tweaks (no YYC required).
//
//  COMPATIBILITY
//  -------------
//  The generated GML avoids every 2.3+ language feature (no structs, no
//  function literals, no accessors, no ??) and creates no script assets,
//  so it compiles on GameMaker 1.4 up to 2024.x.
//  Nothing but built-in drawing functions is used - no sprites are needed.
//
//  USAGE
//  -----
//  UndertaleModTool -> Scripts -> Run other script... -> pick this file.
//  Then save the data file (File -> Save).
//
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Models;

EnsureDataLoaded();

// -----------------------------------------------------------------------------
// Configuration
// -----------------------------------------------------------------------------

const string ObjectName = "obj_gmm_touch";

// Maximum number of round buttons placed on screen.
const int MaxButtons = 8;

// Set to false to skip the mobile performance patch.
bool enableOptimization = true;

// Set to false to never draw a joystick (buttons only).
bool enableJoystick = true;

// Highest resolution the game is allowed to render at, in pixels of height.
// Lower it (e.g. 720) for very weak phones. Only used when enableOptimization is true.
const int ResolutionCap = 1080;

// -----------------------------------------------------------------------------
// Step 1: find the keys the game really uses
// -----------------------------------------------------------------------------

const int VkLeft = 37;
const int VkUp = 38;
const int VkRight = 39;
const int VkDown = 40;

HashSet<string> keyboardFunctions = new HashSet<string>(StringComparer.Ordinal)
{
    "keyboard_check",
    "keyboard_check_pressed",
    "keyboard_check_released",
    "keyboard_check_direct",
    "keyboard_key_press",
    "keyboard_key_release"
};

// Keys that are worth putting on screen. Anything else is ignored so the
// screen does not fill up with useless buttons.
Dictionary<int, string> keyLabels = new Dictionary<int, string>
{
    [8] = "BSP",
    [9] = "TAB",
    [13] = "ENT",
    [16] = "SHF",
    [17] = "CTR",
    [18] = "ALT",
    [27] = "ESC",
    [32] = "SPC",
    [33] = "PGU",
    [34] = "PGD",
    [35] = "END",
    [36] = "HOM",
    [45] = "INS",
    [46] = "DEL",
    [112] = "F1",
    [113] = "F2",
    [114] = "F3",
    [115] = "F4",
    [116] = "F5",
    [117] = "F6",
    [118] = "F7",
    [119] = "F8",
    [120] = "F9",
    [121] = "F10",
    [122] = "F11",
    [123] = "F12"
};

// ASCII only: the built-in GameMaker font has no arrow glyphs, they would
// be drawn as empty boxes on the buttons.
bool TryGetLabel(int code, out string label)
{
    label = null;

    if (code >= 'A' && code <= 'Z') { label = ((char)code).ToString(); return true; }
    if (code >= '0' && code <= '9') { label = ((char)code).ToString(); return true; }

    if (code == VkLeft) { label = "L"; return true; }
    if (code == VkUp) { label = "U"; return true; }
    if (code == VkRight) { label = "R"; return true; }
    if (code == VkDown) { label = "D"; return true; }

    return keyLabels.TryGetValue(code, out label);
}

bool TryGetLiteral(UndertaleInstruction instruction, out int value)
{
    value = 0;
    switch (instruction.Type1)
    {
        case UndertaleInstruction.DataType.Int16:
            value = instruction.ValueShort;
            return true;
        case UndertaleInstruction.DataType.Int32:
            value = instruction.ValueInt;
            return true;
        case UndertaleInstruction.DataType.Int64:
            long l = instruction.ValueLong;
            if (l < int.MinValue || l > int.MaxValue) return false;
            value = (int)l;
            return true;
        case UndertaleInstruction.DataType.Double:
            double d = instruction.ValueDouble;
            if (double.IsNaN(d) || d < 0 || d > 1000) return false;
            value = (int)Math.Round(d);
            return true;
        default:
            return false;
    }
}

bool TryGetSingleChar(UndertaleInstruction instruction, out char value)
{
    value = '\0';
    if (instruction.Type1 != UndertaleInstruction.DataType.String) return false;

    string content = instruction.ValueString?.Resource?.Content;
    if (string.IsNullOrEmpty(content) || content.Length != 1) return false;

    value = content[0];
    return true;
}

Dictionary<int, int> usage = new Dictionary<int, int>();

foreach (UndertaleCode code in Data.Code)
{
    if (code == null || code.ParentEntry != null) continue;

    // Tracks the most recently pushed literal: a number, or a single character
    // string as produced by ord("X") before constant folding.
    bool hasLiteral = false;
    int literal = 0;

    List<UndertaleInstruction> instructions = code.Instructions;
    for (int i = 0; i < instructions.Count; i++)
    {
        UndertaleInstruction instruction = instructions[i];
        if (instruction == null) continue;

        switch (instruction.Kind)
        {
            case UndertaleInstruction.Opcode.Push:
            case UndertaleInstruction.Opcode.PushI:
            case UndertaleInstruction.Opcode.PushGlb:
            case UndertaleInstruction.Opcode.PushLoc:
            case UndertaleInstruction.Opcode.PushBltn:
            {
                int number;
                char character;
                if (TryGetLiteral(instruction, out number))
                {
                    hasLiteral = true;
                    literal = number;
                }
                else if (TryGetSingleChar(instruction, out character))
                {
                    hasLiteral = true;
                    literal = char.ToUpperInvariant(character);
                }
                else
                {
                    hasLiteral = false;
                }
                break;
            }

            case UndertaleInstruction.Opcode.Conv:
                // A conversion does not invalidate the literal.
                break;

            case UndertaleInstruction.Opcode.Call:
            {
                string name = instruction.ValueFunction?.Name?.Content;
                if (name == null)
                {
                    hasLiteral = false;
                    break;
                }

                // ord() returns the character that was just pushed, keep it.
                if (string.Equals(name, "ord", StringComparison.Ordinal)) break;

                string unusedLabel;
                if (keyboardFunctions.Contains(name) && hasLiteral && TryGetLabel(literal, out unusedLabel))
                {
                    int count;
                    usage.TryGetValue(literal, out count);
                    usage[literal] = count + 1;
                }

                hasLiteral = false;
                break;
            }

            default:
                hasLiteral = false;
                break;
        }
    }
}

bool usesArrows = usage.ContainsKey(VkLeft) || usage.ContainsKey(VkRight)
                  || usage.ContainsKey(VkUp) || usage.ContainsKey(VkDown);
bool usesWasd = usage.ContainsKey('W') && usage.ContainsKey('A')
                && usage.ContainsKey('S') && usage.ContainsKey('D');

HashSet<int> movementKeys = new HashSet<int> { VkLeft, VkRight, VkUp, VkDown };
if (usesWasd && !usesArrows)
{
    movementKeys.Add('W');
    movementKeys.Add('A');
    movementKeys.Add('S');
    movementKeys.Add('D');
}

List<int> buttonCodes = new List<int>();
List<string> buttonLabels = new List<string>();

foreach (KeyValuePair<int, int> pair in usage
    .Where(p => !movementKeys.Contains(p.Key))
    .OrderByDescending(p => p.Value)
    .ThenBy(p => p.Key)
    .Take(MaxButtons))
{
    string label;
    TryGetLabel(pair.Key, out label);
    buttonCodes.Add(pair.Key);
    buttonLabels.Add(label);
}

bool needsJoystick = usesArrows || usesWasd;

// Nothing detected at all: fall back to a layout that always works.
if (buttonCodes.Count == 0 && !needsJoystick)
{
    ScriptWarning("No keyboard calls were detected.\n"
        + "Falling back to the default layout: arrow keys + Z / X / C / Enter / Esc.");

    needsJoystick = true;
    usesArrows = true;
    usesWasd = false;

    buttonCodes.AddRange(new int[] { 'Z', 'X', 'C', 13, 27 });
    buttonLabels.AddRange(new string[] { "Z", "X", "C", "ENT", "ESC" });
}

int joyUp = (usesWasd && !usesArrows) ? 'W' : VkUp;
int joyDown = (usesWasd && !usesArrows) ? 'S' : VkDown;
int joyLeft = (usesWasd && !usesArrows) ? 'A' : VkLeft;
int joyRight = (usesWasd && !usesArrows) ? 'D' : VkRight;

bool drawJoystick = enableJoystick && needsJoystick;

string detectedSummary =
    "Joystick: " + (drawJoystick ? (usesArrows ? "arrow keys" : "WASD") : "none") + "\n" +
    "Buttons: " + (buttonCodes.Count == 0
        ? "none"
        : string.Join(", ", buttonLabels));

if (!ScriptQuestion("Mobile Touch Controls\n\n"
    + "Detected key usage:\n" + detectedSummary + "\n\n"
    + "Inject the touch layer now?"))
{
    ScriptMessage("Cancelled, nothing was changed.");
    return;
}

// -----------------------------------------------------------------------------
// Step 2: build the GML
// -----------------------------------------------------------------------------

string Num(double value)
{
    return value.ToString("0.####", CultureInfo.InvariantCulture);
}

string EscapeGml(string text)
{
    return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

// ---- Create -----------------------------------------------------------------

StringBuilder create = new StringBuilder();
create.AppendLine("/// GameMaker-Mobiler touch layer (auto-generated, do not edit by hand)");
create.AppendLine("gmm_ready = 0;");
create.AppendLine("depth = -100000;");
create.AppendLine();
create.AppendLine("global.gmm_touch_on = 1;");
create.AppendLine("global.gmm_alpha = 0.55;");
create.AppendLine("global.gmm_btn_scale = 1;");
create.AppendLine("global.gmm_joy_scale = 1;");
create.AppendLine();
create.AppendLine("gmm_edit = 0;");
create.AppendLine("gmm_sel = -1;");
create.AppendLine("gmm_drag = -1;");
create.AppendLine("gmm_gear_hold = 0;");
create.AppendLine("gmm_repeat = 0;");
create.AppendLine("gmm_do_save = 0;");
create.AppendLine("gmm_hold_frames = 42;");
create.AppendLine("gmm_repeat_frames = 8;");
create.AppendLine();
create.AppendLine("gmm_joy_on = " + (drawJoystick ? "1" : "0") + ";");
create.AppendLine("gmm_joy_show = 1;");
create.AppendLine("gmm_joy_fx = 0.16;");
create.AppendLine("gmm_joy_fy = 0.72;");
create.AppendLine("gmm_joy_dx = 0;");
create.AppendLine("gmm_joy_dy = 0;");
create.AppendLine("gmm_joy_key[0] = " + joyUp + ";");
create.AppendLine("gmm_joy_key[1] = " + joyDown + ";");
create.AppendLine("gmm_joy_key[2] = " + joyLeft + ";");
create.AppendLine("gmm_joy_key[3] = " + joyRight + ";");
create.AppendLine("gmm_joy_state[0] = 0;");
create.AppendLine("gmm_joy_state[1] = 0;");
create.AppendLine("gmm_joy_state[2] = 0;");
create.AppendLine("gmm_joy_state[3] = 0;");
create.AppendLine();
create.AppendLine("gmm_count = " + buttonCodes.Count + ";");

// Default layout: bottom-right corner, three per row, right to left, bottom to top.
for (int i = 0; i < buttonCodes.Count; i++)
{
    int column = i % 3;
    int row = i / 3;
    double fx = 0.88 - (column * 0.11);
    double fy = 0.82 - (row * 0.17);

    create.AppendLine("gmm_key[" + i + "] = " + buttonCodes[i] + ";");
    create.AppendLine("gmm_label[" + i + "] = \"" + EscapeGml(buttonLabels[i]) + "\";");
    create.AppendLine("gmm_fx[" + i + "] = " + Num(fx) + ";");
    create.AppendLine("gmm_fy[" + i + "] = " + Num(fy) + ";");
    create.AppendLine("gmm_show[" + i + "] = 1;");
    create.AppendLine("gmm_size[" + i + "] = 1;");
    create.AppendLine("gmm_state[" + i + "] = 0;");
}

if (buttonCodes.Count == 0)
{
    // The arrays must exist: reading an uninitialized array throws on GMS 1.4.
    create.AppendLine("gmm_key[0] = 0;");
    create.AppendLine("gmm_label[0] = \"\";");
    create.AppendLine("gmm_fx[0] = 0;");
    create.AppendLine("gmm_fy[0] = 0;");
    create.AppendLine("gmm_show[0] = 0;");
    create.AppendLine("gmm_size[0] = 1;");
    create.AppendLine("gmm_state[0] = 0;");
}

create.AppendLine();
create.Append(@"// Load the layout saved by the player
ini_open(""gmm_touch.ini"");
global.gmm_touch_on = ini_read_real(""GMM"", ""on"", global.gmm_touch_on);
global.gmm_alpha = ini_read_real(""GMM"", ""alpha"", global.gmm_alpha);
global.gmm_btn_scale = ini_read_real(""GMM"", ""btn_scale"", global.gmm_btn_scale);
global.gmm_joy_scale = ini_read_real(""GMM"", ""joy_scale"", global.gmm_joy_scale);
gmm_joy_show = ini_read_real(""GMM"", ""joy_show"", gmm_joy_show);
gmm_joy_fx = ini_read_real(""GMM"", ""joy_fx"", gmm_joy_fx);
gmm_joy_fy = ini_read_real(""GMM"", ""joy_fy"", gmm_joy_fy);
for (gmm_i = 0; gmm_i < gmm_count; gmm_i += 1)
{
    gmm_fx[gmm_i] = ini_read_real(""GMM"", ""x"" + string(gmm_key[gmm_i]), gmm_fx[gmm_i]);
    gmm_fy[gmm_i] = ini_read_real(""GMM"", ""y"" + string(gmm_key[gmm_i]), gmm_fy[gmm_i]);
    gmm_show[gmm_i] = ini_read_real(""GMM"", ""v"" + string(gmm_key[gmm_i]), gmm_show[gmm_i]);
    gmm_size[gmm_i] = ini_read_real(""GMM"", ""s"" + string(gmm_key[gmm_i]), gmm_size[gmm_i]);
}
ini_close();

// Clamp everything, so a hand-edited ini cannot push controls off screen
global.gmm_alpha = clamp(global.gmm_alpha, 0.1, 1);
global.gmm_btn_scale = clamp(global.gmm_btn_scale, 0.5, 2.5);
global.gmm_joy_scale = clamp(global.gmm_joy_scale, 0.5, 2.5);
gmm_joy_fx = clamp(gmm_joy_fx, 0.05, 0.95);
gmm_joy_fy = clamp(gmm_joy_fy, 0.05, 0.95);
for (gmm_i = 0; gmm_i < gmm_count; gmm_i += 1)
{
    gmm_fx[gmm_i] = clamp(gmm_fx[gmm_i], 0.02, 0.98);
    gmm_fy[gmm_i] = clamp(gmm_fy[gmm_i], 0.02, 0.98);
    gmm_size[gmm_i] = clamp(gmm_size[gmm_i], 0.5, 2.5);
}

gmm_ready = 1;
");

if (enableOptimization)
{
    create.AppendLine();
    create.AppendLine("// ---- Mobile performance optimization (no YYC needed) ----");
    if (Data.IsGameMaker2())
    {
        create.AppendLine("gpu_set_tex_filter(false);");
        create.AppendLine("gpu_set_tex_repeat(false);");
    }
    else
    {
        create.AppendLine("texture_set_interpolation(false);");
        create.AppendLine("texture_set_repeat(false);");
    }

    create.AppendLine("draw_set_circle_precision(16);");
    create.AppendLine("display_reset(0, false);");
    create.AppendLine("if (surface_exists(application_surface))");
    create.AppendLine("{");
    create.AppendLine("    var _sw, _sh, _cap;");
    create.AppendLine("    _sw = surface_get_width(application_surface);");
    create.AppendLine("    _sh = surface_get_height(application_surface);");
    create.AppendLine("    _cap = " + ResolutionCap + ";");
    create.AppendLine("    if (_sh > _cap && _sw > 0 && _sh > 0)");
    create.AppendLine("    {");
    create.AppendLine("        surface_resize(application_surface, max(1, round(_sw * (_cap / _sh))), _cap);");
    create.AppendLine("    }");
    create.AppendLine("}");
}

// ---- shared snippets --------------------------------------------------------

string releaseAll = @"for (_i = 0; _i < gmm_count; _i += 1)
{
    if (gmm_state[_i] == 1)
    {
        keyboard_key_release(gmm_key[_i]);
        gmm_state[_i] = 0;
    }
}
for (_i = 0; _i < 4; _i += 1)
{
    if (gmm_joy_state[_i] == 1)
    {
        keyboard_key_release(gmm_joy_key[_i]);
        gmm_joy_state[_i] = 0;
    }
}
gmm_joy_dx = 0;
gmm_joy_dy = 0;";

string saveInline = @"ini_open(""gmm_touch.ini"");
ini_write_real(""GMM"", ""on"", global.gmm_touch_on);
ini_write_real(""GMM"", ""alpha"", global.gmm_alpha);
ini_write_real(""GMM"", ""btn_scale"", global.gmm_btn_scale);
ini_write_real(""GMM"", ""joy_scale"", global.gmm_joy_scale);
ini_write_real(""GMM"", ""joy_show"", gmm_joy_show);
ini_write_real(""GMM"", ""joy_fx"", gmm_joy_fx);
ini_write_real(""GMM"", ""joy_fy"", gmm_joy_fy);
for (_i = 0; _i < gmm_count; _i += 1)
{
    ini_write_real(""GMM"", ""x"" + string(gmm_key[_i]), gmm_fx[_i]);
    ini_write_real(""GMM"", ""y"" + string(gmm_key[_i]), gmm_fy[_i]);
    ini_write_real(""GMM"", ""v"" + string(gmm_key[_i]), gmm_show[_i]);
    ini_write_real(""GMM"", ""s"" + string(gmm_key[_i]), gmm_size[_i]);
}
ini_close();";

string Indent(string text, int levels)
{
    string pad = new string(' ', levels * 4);
    string[] lines = text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
    return string.Join("\n", lines.Select(l => l.Length == 0 ? l : pad + l));
}

// ---- Step -------------------------------------------------------------------

StringBuilder step = new StringBuilder();
step.Append(@"if (gmm_ready != 1) exit;

var _gw, _gh, _unit, _br, _jr, _jx, _jy, _i, _t, _tx, _ty, _hit, _px, _py, _pr;
_gw = display_get_gui_width();
_gh = display_get_gui_height();
if (_gw <= 0 || _gh <= 0) exit;
_unit = min(_gw, _gh);
_br = _unit * 0.085 * global.gmm_btn_scale;
_jr = _unit * 0.17 * global.gmm_joy_scale;
_jx = gmm_joy_fx * _gw;
_jy = gmm_joy_fy * _gh;

// Collect this frame's touch points (up to 5 fingers)
var _n, _px_arr, _py_arr, _new_arr;
_n = 0;
_px_arr[0] = 0;
_py_arr[0] = 0;
_new_arr[0] = 0;
for (_t = 0; _t < 5; _t += 1)
{
    if (device_mouse_check_button(_t, mb_left))
    {
        _px_arr[_n] = device_mouse_x_to_gui(_t);
        _py_arr[_n] = device_mouse_y_to_gui(_t);
        _new_arr[_n] = device_mouse_check_button_pressed(_t, mb_left);
        _n += 1;
    }
}

// ---- Gear button: hold 0.7s to enter / leave EDIT mode ----
var _gear_x, _gear_y, _gear_r, _gear_down;
_gear_r = _unit * 0.045;
_gear_x = _gw - (_gear_r * 1.4);
_gear_y = _gear_r * 1.4;
_gear_down = 0;
for (_i = 0; _i < _n; _i += 1)
{
    if (point_distance(_px_arr[_i], _py_arr[_i], _gear_x, _gear_y) <= _gear_r * 1.3) _gear_down = 1;
}
if (_gear_down == 1)
{
    gmm_gear_hold += 1;
    if (gmm_gear_hold == gmm_hold_frames)
    {
        gmm_edit = 1 - gmm_edit;
        gmm_sel = -1;
        gmm_drag = -1;
        gmm_repeat = gmm_repeat_frames * 3;
        if (gmm_edit == 0) gmm_do_save = 1;
    }
}
else
{
    gmm_gear_hold = 0;
}

// ---- Single save point (kept first: the edit logic can exit early) ----
if (gmm_do_save == 1)
{
    gmm_do_save = 0;
");
step.AppendLine(Indent(saveInline, 1));
step.AppendLine("}");
step.AppendLine();
step.AppendLine("if (gmm_edit == 1 || global.gmm_touch_on != 1)");
step.AppendLine("{");
step.AppendLine(Indent(releaseAll, 1));
step.AppendLine("}");
step.AppendLine();
step.AppendLine("if (gmm_edit == 1)");
step.AppendLine("{");
step.AppendLine(Indent(@"var _pw, _ph, _pnx, _pny, _rowh, _row, _on_panel, _left, _tdown, _tnew;
_pw = _unit * 0.66;
_ph = _unit * 0.62;
_pnx = (_gw - _pw) * 0.5;
_pny = (_gh - _ph) * 0.5;
_rowh = _ph / 7;

_tdown = 0;
_tnew = 0;
_tx = 0;
_ty = 0;
if (_n > 0)
{
    _tdown = 1;
    _tx = _px_arr[0];
    _ty = _py_arr[0];
    _tnew = _new_arr[0];
}

if (gmm_repeat > 0) gmm_repeat -= 1;
if (_tdown == 0)
{
    gmm_repeat = 0;
    gmm_drag = -1;
    exit;
}

_on_panel = 0;
if (_tx >= _pnx && _tx <= _pnx + _pw && _ty >= _pny && _ty <= _pny + _ph) _on_panel = 1;

if (_on_panel == 1)
{
    gmm_drag = -1;
    if (gmm_repeat <= 0)
    {
        gmm_repeat = gmm_repeat_frames;
        _row = floor((_ty - _pny) / _rowh);
        _left = 0;
        if (_tx < _pnx + _pw * 0.5) _left = 1;
        if (_row == 1)
        {
            if (_left == 1) global.gmm_btn_scale = max(0.5, global.gmm_btn_scale - 0.05);
            else global.gmm_btn_scale = min(2.5, global.gmm_btn_scale + 0.05);
        }
        else if (_row == 2)
        {
            if (_left == 1) global.gmm_joy_scale = max(0.5, global.gmm_joy_scale - 0.05);
            else global.gmm_joy_scale = min(2.5, global.gmm_joy_scale + 0.05);
        }
        else if (_row == 3)
        {
            if (_left == 1) global.gmm_alpha = max(0.1, global.gmm_alpha - 0.05);
            else global.gmm_alpha = min(1, global.gmm_alpha + 0.05);
        }
        else if (_row == 4)
        {
            if (gmm_sel == -2) gmm_joy_show = 1 - gmm_joy_show;
            else if (gmm_sel >= 0 && gmm_sel < gmm_count) gmm_show[gmm_sel] = 1 - gmm_show[gmm_sel];
        }
        else if (_row == 5)
        {
            if (gmm_sel >= 0 && gmm_sel < gmm_count)
            {
                if (_left == 1) gmm_size[gmm_sel] = max(0.5, gmm_size[gmm_sel] - 0.05);
                else gmm_size[gmm_sel] = min(2.5, gmm_size[gmm_sel] + 0.05);
            }
        }
        else if (_row == 6)
        {
            if (_left == 1)
            {
                global.gmm_touch_on = 1 - global.gmm_touch_on;
            }
            else
            {
                gmm_edit = 0;
                gmm_drag = -1;
                gmm_do_save = 1;
            }
        }
    }
    exit;
}

// Select / drag a control
if (gmm_drag == -1 && _tnew == 1)
{
    for (_i = 0; _i < gmm_count; _i += 1)
    {
        _px = gmm_fx[_i] * _gw;
        _py = gmm_fy[_i] * _gh;
        _pr = _br * gmm_size[_i];
        if (gmm_drag == -1 && point_distance(_tx, _ty, _px, _py) <= _pr)
        {
            gmm_drag = _i;
            gmm_sel = _i;
        }
    }
    if (gmm_drag == -1 && gmm_joy_on == 1 && point_distance(_tx, _ty, _jx, _jy) <= _jr)
    {
        gmm_drag = -2;
        gmm_sel = -2;
    }
}

if (gmm_drag >= 0)
{
    gmm_fx[gmm_drag] = clamp(_tx / _gw, 0.02, 0.98);
    gmm_fy[gmm_drag] = clamp(_ty / _gh, 0.02, 0.98);
}
else if (gmm_drag == -2)
{
    gmm_joy_fx = clamp(_tx / _gw, 0.05, 0.95);
    gmm_joy_fy = clamp(_ty / _gh, 0.05, 0.95);
}", 1));
step.AppendLine("}");
step.AppendLine("else if (global.gmm_touch_on == 1)");
step.AppendLine("{");
step.AppendLine(Indent(@"// ---- Regular buttons ----
for (_i = 0; _i < gmm_count; _i += 1)
{
    _hit = 0;
    if (gmm_show[_i] == 1)
    {
        _px = gmm_fx[_i] * _gw;
        _py = gmm_fy[_i] * _gh;
        _pr = _br * gmm_size[_i];
        for (_t = 0; _t < _n; _t += 1)
        {
            if (point_distance(_px_arr[_t], _py_arr[_t], _px, _py) <= _pr) _hit = 1;
        }
    }
    if (_hit == 1 && gmm_state[_i] == 0) keyboard_key_press(gmm_key[_i]);
    if (_hit == 0 && gmm_state[_i] == 1) keyboard_key_release(gmm_key[_i]);
    gmm_state[_i] = _hit;
}

// ---- Joystick ----
if (gmm_joy_on == 1 && gmm_joy_show == 1)
{
    var _found, _ddx, _ddy, _len, _dir, _want;
    _found = 0;
    _ddx = 0;
    _ddy = 0;
    for (_t = 0; _t < _n; _t += 1)
    {
        if (_found == 0 && point_distance(_px_arr[_t], _py_arr[_t], _jx, _jy) <= _jr * 1.6)
        {
            _found = 1;
            _ddx = _px_arr[_t] - _jx;
            _ddy = _py_arr[_t] - _jy;
        }
    }
    _want[0] = 0;
    _want[1] = 0;
    _want[2] = 0;
    _want[3] = 0;
    _len = point_distance(0, 0, _ddx, _ddy);
    if (_found == 1 && _len > _jr * 0.28)
    {
        _dir = point_direction(0, 0, _ddx, _ddy);
        if (_dir >= 292.5 || _dir <= 67.5) _want[3] = 1;
        if (_dir >= 22.5 && _dir <= 157.5) _want[0] = 1;
        if (_dir >= 112.5 && _dir <= 247.5) _want[2] = 1;
        if (_dir >= 202.5 && _dir <= 337.5) _want[1] = 1;
        if (_len > _jr)
        {
            _ddx = _ddx * (_jr / _len);
            _ddy = _ddy * (_jr / _len);
        }
        gmm_joy_dx = _ddx;
        gmm_joy_dy = _ddy;
    }
    else
    {
        gmm_joy_dx = 0;
        gmm_joy_dy = 0;
    }
    for (_i = 0; _i < 4; _i += 1)
    {
        if (_want[_i] == 1 && gmm_joy_state[_i] == 0) keyboard_key_press(gmm_joy_key[_i]);
        if (_want[_i] == 0 && gmm_joy_state[_i] == 1) keyboard_key_release(gmm_joy_key[_i]);
        gmm_joy_state[_i] = _want[_i];
    }
}", 1));
step.AppendLine("}");

// ---- Draw GUI ---------------------------------------------------------------

StringBuilder draw = new StringBuilder();
draw.Append(@"if (gmm_ready != 1) exit;

var _gw, _gh, _unit, _br, _jr, _jx, _jy, _i, _px, _py, _pr, _a;
var _gear_r, _gear_x, _gear_y;
_gw = display_get_gui_width();
_gh = display_get_gui_height();
if (_gw <= 0 || _gh <= 0) exit;
_unit = min(_gw, _gh);
_br = _unit * 0.085 * global.gmm_btn_scale;
_jr = _unit * 0.17 * global.gmm_joy_scale;
_jx = gmm_joy_fx * _gw;
_jy = gmm_joy_fy * _gh;
_gear_r = _unit * 0.045;
_gear_x = _gw - (_gear_r * 1.4);
_gear_y = _gear_r * 1.4;

var _old_a, _old_c, _old_ha, _old_va;
_old_a = draw_get_alpha();
_old_c = draw_get_colour();
_old_ha = draw_get_halign();
_old_va = draw_get_valign();
draw_set_halign(fa_center);
draw_set_valign(fa_middle);

// Gear button: hold 0.7s to enter EDIT mode
draw_set_alpha(0.35);
draw_set_colour(c_black);
draw_circle(_gear_x, _gear_y, _gear_r, false);
draw_set_alpha(0.9);
draw_set_colour(c_white);
draw_circle(_gear_x, _gear_y, _gear_r, true);
draw_circle(_gear_x, _gear_y, _gear_r * 0.42, true);
draw_text(_gear_x, _gear_y, ""E"");

if (gmm_edit == 0 && global.gmm_touch_on != 1)
{
    draw_set_alpha(_old_a);
    draw_set_colour(_old_c);
    draw_set_halign(_old_ha);
    draw_set_valign(_old_va);
    exit;
}

_a = global.gmm_alpha;
if (gmm_edit == 1) _a = max(_a, 0.75);

// Joystick
if (gmm_joy_on == 1 && (gmm_joy_show == 1 || gmm_edit == 1))
{
    draw_set_alpha(_a * 0.45);
    draw_set_colour(c_black);
    draw_circle(_jx, _jy, _jr, false);
    draw_set_alpha(_a);
    if (gmm_edit == 1 && gmm_sel == -2) draw_set_colour(c_yellow);
    else if (gmm_joy_show == 0) draw_set_colour(c_red);
    else draw_set_colour(c_white);
    draw_circle(_jx, _jy, _jr, true);
    draw_set_alpha(_a * 0.8);
    draw_circle(_jx + gmm_joy_dx, _jy + gmm_joy_dy, _jr * 0.42, false);
    if (gmm_joy_show == 0)
    {
        draw_set_colour(c_red);
        draw_text(_jx, _jy - _jr * 0.7, ""OFF"");
    }
}

// Buttons
for (_i = 0; _i < gmm_count; _i += 1)
{
    if (gmm_show[_i] == 1 || gmm_edit == 1)
    {
        _px = gmm_fx[_i] * _gw;
        _py = gmm_fy[_i] * _gh;
        _pr = _br * gmm_size[_i];
        if (gmm_state[_i] == 1) draw_set_alpha(_a * 0.75);
        else draw_set_alpha(_a * 0.4);
        draw_set_colour(c_black);
        draw_circle(_px, _py, _pr, false);
        draw_set_alpha(_a);
        if (gmm_edit == 1 && gmm_sel == _i) draw_set_colour(c_yellow);
        else if (gmm_show[_i] == 0) draw_set_colour(c_red);
        else draw_set_colour(c_white);
        draw_circle(_px, _py, _pr, true);
        draw_text(_px, _py, gmm_label[_i]);
    }
}

// Edit panel
if (gmm_edit == 1)
{
");
draw.AppendLine(Indent(@"var _pw, _ph, _pnx, _pny, _rowh, _cx, _k, _sel_name;
_pw = _unit * 0.66;
_ph = _unit * 0.62;
_pnx = (_gw - _pw) * 0.5;
_pny = (_gh - _ph) * 0.5;
_rowh = _ph / 7;
_cx = _pnx + _pw * 0.5;

draw_set_alpha(0.85);
draw_set_colour(c_black);
draw_rectangle(_pnx, _pny, _pnx + _pw, _pny + _ph, false);
draw_set_alpha(1);
draw_set_colour(c_white);
draw_rectangle(_pnx, _pny, _pnx + _pw, _pny + _ph, true);
for (_k = 1; _k < 7; _k += 1) draw_line(_pnx, _pny + _rowh * _k, _pnx + _pw, _pny + _rowh * _k);

if (gmm_sel == -2) _sel_name = ""JOYSTICK"";
else if (gmm_sel >= 0 && gmm_sel < gmm_count) _sel_name = gmm_label[gmm_sel];
else _sel_name = ""NONE"";

draw_text(_cx, _pny + _rowh * 0.5, ""EDIT  -  SELECTED: "" + _sel_name);
draw_text(_cx, _pny + _rowh * 1.5, ""[-]  BUTTON SIZE "" + string(round(global.gmm_btn_scale * 100) / 100) + ""  [+]"");
draw_text(_cx, _pny + _rowh * 2.5, ""[-]  JOYSTICK SIZE "" + string(round(global.gmm_joy_scale * 100) / 100) + ""  [+]"");
draw_text(_cx, _pny + _rowh * 3.5, ""[-]  OPACITY "" + string(round(global.gmm_alpha * 100) / 100) + ""  [+]"");
draw_text(_cx, _pny + _rowh * 4.5, ""TAP: SHOW / HIDE SELECTED"");
draw_text(_cx, _pny + _rowh * 5.5, ""[-]  SELECTED SIZE  [+]"");
if (global.gmm_touch_on == 1) draw_text(_cx, _pny + _rowh * 6.5, ""TOUCH: ON   |   SAVE & EXIT"");
else draw_text(_cx, _pny + _rowh * 6.5, ""TOUCH: OFF   |   SAVE & EXIT"");

draw_set_alpha(0.6);
draw_text(_cx, _pny + _ph + _rowh * 0.6, ""DRAG BUTTONS / JOYSTICK TO MOVE THEM"");", 1));
draw.AppendLine("}");
draw.AppendLine();
draw.AppendLine("draw_set_alpha(_old_a);");
draw.AppendLine("draw_set_colour(_old_c);");
draw.AppendLine("draw_set_halign(_old_ha);");
draw.AppendLine("draw_set_valign(_old_va);");

// ---- CleanUp / Room Start ---------------------------------------------------

StringBuilder cleanUp = new StringBuilder();
cleanUp.AppendLine("if (gmm_ready == 1)");
cleanUp.AppendLine("{");
cleanUp.AppendLine("    var _i;");
cleanUp.AppendLine(Indent(releaseAll, 1));
cleanUp.AppendLine("}");

StringBuilder roomStart = new StringBuilder();
roomStart.AppendLine("if (gmm_ready != 1) exit;");
roomStart.AppendLine("depth = -100000;");
roomStart.AppendLine("var _i;");
roomStart.AppendLine(releaseAll);

// -----------------------------------------------------------------------------
// Step 3: compile and attach everything
// -----------------------------------------------------------------------------

CodeImportGroup importGroup = new CodeImportGroup(Data);
importGroup.AutoCreateAssets = true;

// Object events only: no script assets are created, because the script
// mechanism changed drastically in GMS 2.3.
importGroup.QueueReplace("gml_Object_" + ObjectName + "_Create_0", create.ToString());
importGroup.QueueReplace("gml_Object_" + ObjectName + "_Step_0", step.ToString());
importGroup.QueueReplace("gml_Object_" + ObjectName + "_Draw_64", draw.ToString());
importGroup.QueueReplace("gml_Object_" + ObjectName + "_CleanUp_0", cleanUp.ToString());
// Other_4 = Room Start
importGroup.QueueReplace("gml_Object_" + ObjectName + "_Other_4", roomStart.ToString());

importGroup.Import();

UndertaleGameObject touchObject = Data.GameObjects.ByName(ObjectName);
if (touchObject == null)
{
    ScriptError("Failed to create the touch object " + ObjectName + ".");
    return;
}

touchObject.Persistent = true;
touchObject.Visible = true;
touchObject.Depth = -100000;

// -----------------------------------------------------------------------------
// Step 4: place an instance in the first room
// -----------------------------------------------------------------------------

if (Data.Rooms.Count == 0)
{
    ScriptWarning("The data file contains no rooms, the touch layer could not be placed.\n"
        + "Create the instance manually or the controls will never appear.");
}
else
{
    UndertaleRoom room = Data.Rooms[0];

    bool alreadyPlaced = room.GameObjects.Any(o => o != null && o.ObjectDefinition == touchObject);
    if (alreadyPlaced)
    {
        ScriptMessage("The first room already contains a touch layer instance, skipping placement.");
    }
    else
    {
        uint instanceId = Data.GeneralInfo != null ? Data.GeneralInfo.LastObj++ : 100000u;

        UndertaleRoom.GameObject instance = new UndertaleRoom.GameObject()
        {
            InstanceID = instanceId,
            ObjectDefinition = touchObject,
            X = 0,
            Y = 0,
            ScaleX = 1,
            ScaleY = 1,
            Color = 0xFFFFFFFF
        };

        room.GameObjects.Add(instance);

        // GMS2 rooms use layers: the instance must also join an instance layer
        // or it is never created at runtime.
        if (Data.IsGameMaker2())
        {
            UndertaleRoom.Layer layer = room.Layers.FirstOrDefault(l =>
                l.LayerType == UndertaleRoom.LayerType.Instances && l.InstancesData != null);

            if (layer == null)
            {
                layer = new UndertaleRoom.Layer()
                {
                    LayerName = Data.Strings.MakeString("GMM_Touch"),
                    LayerId = room.Layers.Count == 0 ? 1 : room.Layers.Max(l => l.LayerId) + 1,
                    LayerType = UndertaleRoom.LayerType.Instances,
                    LayerDepth = -100000,
                    IsVisible = true,
                    Data = new UndertaleRoom.Layer.LayerInstancesData()
                };
                layer.ParentRoom = room;
                room.Layers.Add(layer);
            }

            layer.InstancesData.Instances.Add(instance);
        }

        // GameMaker 2024.13+ keeps an instance creation order list for the first room.
        if (room.InstanceCreationOrderIDs != null)
        {
            room.InstanceCreationOrderIDs.InstanceIDs.Add(instanceId);
        }
    }
}

// -----------------------------------------------------------------------------
// Done
// -----------------------------------------------------------------------------

ScriptMessage("Mobile Touch Controls installed.\n\n"
    + detectedSummary + "\n\n"
    + "Object: " + ObjectName + "\n"
    + "Optimization: " + (enableOptimization ? "on" : "off") + "\n\n"
    + "In game: hold the gear button in the top-right corner for 0.7 s to open EDIT mode.\n"
    + "Remember to save the data file (File -> Save).");
