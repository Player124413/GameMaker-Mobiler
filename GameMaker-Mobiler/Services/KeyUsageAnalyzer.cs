using System;
using System.Collections.Generic;
using System.Linq;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace GameMaker_Mobiler.Services;

/// <summary>
/// 一个被游戏实际使用的键位。
/// </summary>
/// <param name="Code">GameMaker 键值（与 ord()/vk_* 一致）。</param>
/// <param name="Label">在触控按钮上显示的文字。</param>
/// <param name="Usage">在字节码中被引用的次数（用于排序，越大越常用）。</param>
public sealed record DetectedKey(int Code, string Label, int Usage);

/// <summary>
/// 游戏键位使用情况分析结果。
/// </summary>
public sealed class KeyUsageReport
{
    /// <summary>方向键（上下左右）是否被使用。</summary>
    public bool UsesArrowKeys { get; init; }

    /// <summary>WASD 是否被使用。</summary>
    public bool UsesWasd { get; init; }

    /// <summary>需要放在屏幕上的普通按钮（已按使用频率排序，且不含方向键）。</summary>
    public IReadOnlyList<DetectedKey> Buttons { get; init; } = Array.Empty<DetectedKey>();

    /// <summary>摇杆输出的键值：上、下、左、右。</summary>
    public int JoystickUp { get; init; } = 38;
    public int JoystickDown { get; init; } = 40;
    public int JoystickLeft { get; init; } = 37;
    public int JoystickRight { get; init; } = 39;

    /// <summary>是否需要摇杆（游戏用了方向键或 WASD）。</summary>
    public bool NeedsJoystick => UsesArrowKeys || UsesWasd;

    /// <summary>分析是否成功命中了任何键位（否则使用兜底方案）。</summary>
    public bool IsEmpty => Buttons.Count == 0 && !NeedsJoystick;
}

/// <summary>
/// 扫描 data.win 的字节码，找出游戏真正使用的键盘按键，
/// 以便只为这些按键绘制触控按钮（不同游戏用的键不一样）。
/// </summary>
public static class KeyUsageAnalyzer
{
    /// <summary>最多生成多少个普通触控按钮。</summary>
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

    // 常见的、值得放到屏幕上的按键（其余的一律忽略，避免生成一屏幕垃圾按钮）。
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
    /// 分析 data.win 中所有代码，统计键盘按键的使用情况。
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
            log($"键位分析：摇杆 = {(report.NeedsJoystick ? (report.UsesArrowKeys ? "方向键" : "WASD") : "未检测到")}，" +
                $"按钮 = {(buttons.Count == 0 ? "无" : string.Join(", ", buttons.Select(b => $"{b.Label}({b.Usage})")))}", false);
        }

        return report;
    }

    /// <summary>
    /// 当分析不到任何键位时（例如游戏使用了扩展或 YYC 之外的特殊输入方式），
    /// 返回一套安全的默认布局，保证玩家至少能操作。
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
        // 记录最近一次压栈的字面量：数字或单字符字符串（对应 ord("X")）。
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
                        // ord("z") 未被常量折叠时，字符串会先入栈。
                        hasLiteral = true;
                        literal = char.ToUpperInvariant(ch);
                    }
                    else
                    {
                        hasLiteral = false;
                    }
                    break;

                case UndertaleInstruction.Opcode.Conv:
                    // 类型转换不会破坏字面量。
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
                        // ord 的结果就是刚才那个字符，保持 literal 不变。
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
    /// 取得按键在触控按钮上显示的名字；不支持/不适合上屏的键返回 false。
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
            label = code switch
            {
                VkLeft => "←",
                VkUp => "↑",
                VkRight => "→",
                _ => "↓"
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
