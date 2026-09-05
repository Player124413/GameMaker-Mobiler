using System;
using System.Collections.Generic;
using System.Linq;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace GameMaker_Mobiler.Services;

/// <summary>
/// A keyboard key the game actually uses.
/// </summary>
/// <param name="Code">GameMaker key code (same values as ord() / vk_*).</param>
/// <param name="Label">Text drawn on the touch button.</param>
/// <param name="Usage">How many times it is referenced in the bytecode (used for ranking).</param>
public sealed record DetectedKey(int Code, string Label, int Usage);

/// <summary>
/// Result of the keyboard usage analysis.
/// </summary>
public sealed class KeyUsageReport
{
    /// <summary>Whether the arrow keys are used.</summary>
    public bool UsesArrowKeys { get; init; }

    /// <summary>Whether WASD is used.</summary>
    public bool UsesWasd { get; init; }

    /// <summary>Regular on-screen buttons, ranked by usage and excluding movement keys.</summary>
    public IReadOnlyList<DetectedKey> Buttons { get; init; } = Array.Empty<DetectedKey>();

    /// <summary>Key codes emitted by the joystick: up, down, left, right.</summary>
    public int JoystickUp { get; init; } = 38;
    public int JoystickDown { get; init; } = 40;
    public int JoystickLeft { get; init; } = 37;
    public int JoystickRight { get; init; } = 39;

    /// <summary>Whether a joystick is needed (the game uses arrow keys or WASD).</summary>
    public bool NeedsJoystick => UsesArrowKeys || UsesWasd;

    /// <summary>True when nothing was detected and the fallback layout should be used.</summary>
    public bool IsEmpty => Buttons.Count == 0 && !NeedsJoystick;
}

/// <summary>
/// Scans the data.win bytecode to find the keyboard keys the game really uses,
/// so touch buttons are created only for them (every game uses a different set).
/// </summary>
public static class KeyUsageAnalyzer
{
    /// <summary>Maximum number of regular touch buttons to generate.</summary>
    public const int MaxButtons = 8;

    private static readonly HashSet<string> KeyboardFunctions = new(StringComparer.Ordinal)
    {
        "keyboard_check",
        "keyboard_check_pressed",
        "keyboard_check_released",
        "keyboard_check_direct",
        "keyboard_key_press",
        "keyboard_key_release"
    };

    // Common keys worth putting on screen; anything else is ignored to avoid button clutter.
    private static readonly Dictionary<int, string> KeyLabels = new()
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

    private const int VkLeft = 37;
    private const int VkUp = 38;
    private const int VkRight = 39;
    private const int VkDown = 40;

    /// <summary>
    /// Analyzes every code entry in data.win and counts keyboard key usage.
    /// </summary>
    public static KeyUsageReport Analyze(UndertaleData data, Action<string, bool>? log = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        var usage = new Dictionary<int, int>();

        foreach (var code in data.Code)
        {
            if (code is null || code.ParentEntry is not null)
                continue;

            CollectFromCode(code, usage);
        }

        var usesArrows = usage.ContainsKey(VkLeft) || usage.ContainsKey(VkRight)
                         || usage.ContainsKey(VkUp) || usage.ContainsKey(VkDown);
        var usesWasd = usage.ContainsKey('W') && usage.ContainsKey('A')
                       && usage.ContainsKey('S') && usage.ContainsKey('D');

        var directional = new HashSet<int> { VkLeft, VkRight, VkUp, VkDown };
        if (usesWasd && !usesArrows)
        {
            directional.Add('W');
            directional.Add('A');
            directional.Add('S');
            directional.Add('D');
        }

        var buttons = usage
            .Where(pair => !directional.Contains(pair.Key))
            .Where(pair => TryGetLabel(pair.Key, out _))
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key)
            .Take(MaxButtons)
            .Select(pair =>
            {
                TryGetLabel(pair.Key, out var label);
                return new DetectedKey(pair.Key, label!, pair.Value);
            })
            .ToList();

        var report = new KeyUsageReport
        {
            UsesArrowKeys = usesArrows,
            UsesWasd = usesWasd,
            Buttons = buttons,
            JoystickUp = (usesWasd && !usesArrows) ? 'W' : VkUp,
            JoystickDown = (usesWasd && !usesArrows) ? 'S' : VkDown,
            JoystickLeft = (usesWasd && !usesArrows) ? 'A' : VkLeft,
            JoystickRight = (usesWasd && !usesArrows) ? 'D' : VkRight
        };

        if (log is not null)
        {
            log($"Key analysis: joystick = {(report.NeedsJoystick ? (report.UsesArrowKeys ? "arrow keys" : "WASD") : "not detected")}, " +
                $"buttons = {(buttons.Count == 0 ? "none" : string.Join(", ", buttons.Select(b => $"{b.Label}({b.Usage})")))}", false);
        }

        return report;
    }

    /// <summary>
    /// Used when nothing could be detected (for example the game reads input through an extension).
    /// Returns a safe default layout so the player can always control the game.
    /// </summary>
    public static KeyUsageReport CreateFallback()
    {
        return new KeyUsageReport
        {
            UsesArrowKeys = true,
            UsesWasd = false,
            Buttons = new List<DetectedKey>
            {
                new('Z', "Z", 0),
                new('X', "X", 0),
                new('C', "C", 0),
                new(13, "ENT", 0),
                new(27, "ESC", 0)
            }
        };
    }

    private static void CollectFromCode(UndertaleCode code, Dictionary<int, int> usage)
    {
        // Tracks the most recently pushed literal: a number or a single-character string (ord("X")).
        var hasLiteral = false;
        var literal = 0;

        var instructions = code.Instructions;
        for (var i = 0; i < instructions.Count; i++)
        {
            var instruction = instructions[i];
            if (instruction is null)
                continue;

            switch (instruction.Kind)
            {
                case UndertaleInstruction.Opcode.Push:
                case UndertaleInstruction.Opcode.PushI:
                case UndertaleInstruction.Opcode.PushGlb:
                case UndertaleInstruction.Opcode.PushLoc:
                case UndertaleInstruction.Opcode.PushBltn:
                    if (TryGetLiteral(instruction, out var value))
                    {
                        hasLiteral = true;
                        literal = value;
                    }
                    else if (TryGetSingleChar(instruction, out var ch))
                    {
                        // When ord("z") is not constant-folded the string is pushed first.
                        hasLiteral = true;
                        literal = char.ToUpperInvariant(ch);
                    }
                    else
                    {
                        hasLiteral = false;
                    }
                    break;

                case UndertaleInstruction.Opcode.Conv:
                    // A conversion does not invalidate the literal.
                    break;

                case UndertaleInstruction.Opcode.Call:
                {
                    var name = instruction.ValueFunction?.Name?.Content;
                    if (name is null)
                    {
                        hasLiteral = false;
                        break;
                    }

                    if (string.Equals(name, "ord", StringComparison.Ordinal))
                    {
                        // ord returns the character just pushed, so keep the literal as is.
                        break;
                    }

                    if (KeyboardFunctions.Contains(name) && hasLiteral && IsUsableKey(literal))
                    {
                        usage.TryGetValue(literal, out var count);
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

    private static bool TryGetLiteral(UndertaleInstruction instruction, out int value)
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
                var l = instruction.ValueLong;
                if (l is < int.MinValue or > int.MaxValue)
                    return false;
                value = (int)l;
                return true;
            case UndertaleInstruction.DataType.Double:
                var d = instruction.ValueDouble;
                if (double.IsNaN(d) || d is < 0 or > 1000)
                    return false;
                value = (int)Math.Round(d);
                return true;
            default:
                return false;
        }
    }

    private static bool TryGetSingleChar(UndertaleInstruction instruction, out char value)
    {
        value = '\0';
        if (instruction.Type1 != UndertaleInstruction.DataType.String)
            return false;

        var content = instruction.ValueString?.Resource?.Content;
        if (string.IsNullOrEmpty(content) || content!.Length != 1)
            return false;

        value = content[0];
        return true;
    }

    private static bool IsUsableKey(int code)
    {
        return TryGetLabel(code, out _);
    }

    /// <summary>
    /// Gets the label shown on the touch button; returns false for keys that should not be drawn.
    /// </summary>
    public static bool TryGetLabel(int code, out string? label)
    {
        label = null;

        if (code is >= 'A' and <= 'Z')
        {
            label = ((char)code).ToString();
            return true;
        }

        if (code is >= '0' and <= '9')
        {
            label = ((char)code).ToString();
            return true;
        }

        if (code is VkLeft or VkUp or VkRight or VkDown)
        {
            // ASCII only: the built-in GameMaker font cannot render arrow glyphs,
            // they would show up as empty boxes on the touch buttons.
            label = code switch
            {
                VkLeft => "L",
                VkUp => "U",
                VkRight => "R",
                _ => "D"
            };
            return true;
        }

        if (KeyLabels.TryGetValue(code, out var known))
        {
            label = known;
            return true;
        }

        return false;
    }
}
