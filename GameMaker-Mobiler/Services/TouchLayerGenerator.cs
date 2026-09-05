using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace GameMaker_Mobiler.Services;

/// <summary>
/// 触控层生成选项。
/// </summary>
public sealed class TouchLayerOptions
{
    /// <summary>是否根据分析结果绘制摇杆。</summary>
    public bool EnableJoystick { get; init; } = true;

    /// <summary>是否启用移动端性能优化补丁。</summary>
    public bool EnableOptimization { get; init; } = true;

    /// <summary>目标是否为 GameMaker Studio 2（决定使用 gpu_* 还是 texture_*）。</summary>
    public bool IsGameMaker2 { get; init; } = true;
}

/// <summary>
/// 生成一套完全自绘（不依赖任何精灵资源）的触控控制层 GML 代码：
/// 摇杆 + 按键按钮 + EDIT 编辑模式（移动 / 缩放 / 显隐 / 关闭触控）。
/// <para>
/// 设计约束（为了在 GMS 1.4 ~ GMS 2024.x 全系列都能编译通过）：
/// <list type="bullet">
/// <item>不创建任何脚本资源（2.3 前后脚本机制不同），全部代码内联在对象事件里；</item>
/// <item>不使用结构体、函数字面量、数组访问器、?? 等 2.3+ 语法；</item>
/// <item>不引用任何精灵/字体资源，全部使用 draw_circle / draw_text 等内置绘制函数。</item>
/// </list>
/// </para>
/// </summary>
public static class TouchLayerGenerator
{
    public const string ObjectName = "obj_gmm_touch";

    /// <summary>Create 事件。</summary>
    public static string BuildCreate(KeyUsageReport report, TouchLayerOptions options)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(options);

        var buttons = report.Buttons.Take(KeyUsageAnalyzer.MaxButtons).ToList();
        var useJoystick = options.EnableJoystick && report.NeedsJoystick;

        var sb = new StringBuilder();
        sb.AppendLine("/// GameMaker-Mobiler 触控层 (自动生成，请勿手改)");
        sb.AppendLine("gmm_ready = 0;");
        sb.AppendLine("depth = -100000;");
        sb.AppendLine();
        sb.AppendLine("global.gmm_touch_on = 1;");
        sb.AppendLine("global.gmm_alpha = 0.55;");
        sb.AppendLine("global.gmm_btn_scale = 1;");
        sb.AppendLine("global.gmm_joy_scale = 1;");
        sb.AppendLine();
        sb.AppendLine("gmm_edit = 0;");
        sb.AppendLine("gmm_sel = -1;");
        sb.AppendLine("gmm_drag = -1;");
        sb.AppendLine("gmm_gear_hold = 0;");
        sb.AppendLine("gmm_repeat = 0;");
        sb.AppendLine("gmm_do_save = 0;");
        sb.AppendLine();
        sb.AppendLine("// 计时用的帧数（按当前房间帧率换算，room_speed 在新旧版本里都可读）");
        sb.AppendLine("gmm_hold_frames = 42;");
        sb.AppendLine("gmm_repeat_frames = 8;");
        sb.AppendLine();
        sb.AppendLine($"gmm_joy_on = {(useJoystick ? 1 : 0)};");
        sb.AppendLine("gmm_joy_show = 1;");
        sb.AppendLine("gmm_joy_fx = 0.16;");
        sb.AppendLine("gmm_joy_fy = 0.72;");
        sb.AppendLine("gmm_joy_dx = 0;");
        sb.AppendLine("gmm_joy_dy = 0;");
        sb.AppendLine($"gmm_joy_key[0] = {report.JoystickUp};");
        sb.AppendLine($"gmm_joy_key[1] = {report.JoystickDown};");
        sb.AppendLine($"gmm_joy_key[2] = {report.JoystickLeft};");
        sb.AppendLine($"gmm_joy_key[3] = {report.JoystickRight};");
        sb.AppendLine("gmm_joy_state[0] = 0;");
        sb.AppendLine("gmm_joy_state[1] = 0;");
        sb.AppendLine("gmm_joy_state[2] = 0;");
        sb.AppendLine("gmm_joy_state[3] = 0;");
        sb.AppendLine();
        sb.AppendLine($"gmm_count = {buttons.Count};");

        // 默认布局：右下角每行 3 个，从右往左、从下往上排布。
        for (var i = 0; i < buttons.Count; i++)
        {
            var button = buttons[i];
            var column = i % 3;
            var row = i / 3;
            var fx = 0.88 - (column * 0.11);
            var fy = 0.82 - (row * 0.17);

            sb.AppendLine($"gmm_key[{i}] = {button.Code};");
            sb.AppendLine($"gmm_label[{i}] = \"{Escape(button.Label)}\";");
            sb.AppendLine($"gmm_fx[{i}] = {Num(fx)};");
            sb.AppendLine($"gmm_fy[{i}] = {Num(fy)};");
            sb.AppendLine($"gmm_show[{i}] = 1;");
            sb.AppendLine($"gmm_size[{i}] = 1;");
            sb.AppendLine($"gmm_state[{i}] = 0;");
        }

        if (buttons.Count == 0)
        {
            // 数组必须存在：GMS1.4 下读取未初始化的数组会报错。
            sb.AppendLine("gmm_key[0] = 0;");
            sb.AppendLine("gmm_label[0] = \"\";");
            sb.AppendLine("gmm_fx[0] = 0;");
            sb.AppendLine("gmm_fy[0] = 0;");
            sb.AppendLine("gmm_show[0] = 0;");
            sb.AppendLine("gmm_size[0] = 1;");
            sb.AppendLine("gmm_state[0] = 0;");
        }

        sb.AppendLine();
        sb.AppendLine("// 读取玩家保存的布局");
        sb.AppendLine("ini_open(\"gmm_touch.ini\");");
        sb.AppendLine("global.gmm_touch_on = ini_read_real(\"GMM\", \"on\", global.gmm_touch_on);");
        sb.AppendLine("global.gmm_alpha = ini_read_real(\"GMM\", \"alpha\", global.gmm_alpha);");
        sb.AppendLine("global.gmm_btn_scale = ini_read_real(\"GMM\", \"btn_scale\", global.gmm_btn_scale);");
        sb.AppendLine("global.gmm_joy_scale = ini_read_real(\"GMM\", \"joy_scale\", global.gmm_joy_scale);");
        sb.AppendLine("gmm_joy_show = ini_read_real(\"GMM\", \"joy_show\", gmm_joy_show);");
        sb.AppendLine("gmm_joy_fx = ini_read_real(\"GMM\", \"joy_fx\", gmm_joy_fx);");
        sb.AppendLine("gmm_joy_fy = ini_read_real(\"GMM\", \"joy_fy\", gmm_joy_fy);");
        sb.AppendLine("for (gmm_i = 0; gmm_i < gmm_count; gmm_i += 1)");
        sb.AppendLine("{");
        sb.AppendLine("    gmm_fx[gmm_i] = ini_read_real(\"GMM\", \"x\" + string(gmm_key[gmm_i]), gmm_fx[gmm_i]);");
        sb.AppendLine("    gmm_fy[gmm_i] = ini_read_real(\"GMM\", \"y\" + string(gmm_key[gmm_i]), gmm_fy[gmm_i]);");
        sb.AppendLine("    gmm_show[gmm_i] = ini_read_real(\"GMM\", \"v\" + string(gmm_key[gmm_i]), gmm_show[gmm_i]);");
        sb.AppendLine("    gmm_size[gmm_i] = ini_read_real(\"GMM\", \"s\" + string(gmm_key[gmm_i]), gmm_size[gmm_i]);");
        sb.AppendLine("}");
        sb.AppendLine("ini_close();");
        sb.AppendLine();
        sb.AppendLine("// 数值兜底，防止 ini 被玩家改坏导致控件飞出屏幕");
        sb.AppendLine("global.gmm_alpha = clamp(global.gmm_alpha, 0.1, 1);");
        sb.AppendLine("global.gmm_btn_scale = clamp(global.gmm_btn_scale, 0.5, 2.5);");
        sb.AppendLine("global.gmm_joy_scale = clamp(global.gmm_joy_scale, 0.5, 2.5);");
        sb.AppendLine("gmm_joy_fx = clamp(gmm_joy_fx, 0.05, 0.95);");
        sb.AppendLine("gmm_joy_fy = clamp(gmm_joy_fy, 0.05, 0.95);");
        sb.AppendLine("for (gmm_i = 0; gmm_i < gmm_count; gmm_i += 1)");
        sb.AppendLine("{");
        sb.AppendLine("    gmm_fx[gmm_i] = clamp(gmm_fx[gmm_i], 0.02, 0.98);");
        sb.AppendLine("    gmm_fy[gmm_i] = clamp(gmm_fy[gmm_i], 0.02, 0.98);");
        sb.AppendLine("    gmm_size[gmm_i] = clamp(gmm_size[gmm_i], 0.5, 2.5);");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("gmm_ready = 1;");

        if (options.EnableOptimization)
        {
            sb.AppendLine();
            sb.Append(BuildOptimizationSnippet(options.IsGameMaker2));
        }

        return sb.ToString();
    }

    /// <summary>Step 事件：触摸判定与按键注入。</summary>
    public static string BuildStep()
    {
        var sb = new StringBuilder();
        sb.AppendLine("if (gmm_ready != 1) exit;");
        sb.AppendLine();
        sb.AppendLine("var _gw, _gh, _unit, _br, _jr, _jx, _jy, _i, _t, _tx, _ty, _hit, _px, _py, _pr;");
        sb.AppendLine("_gw = display_get_gui_width();");
        sb.AppendLine("_gh = display_get_gui_height();");
        sb.AppendLine("if (_gw <= 0 || _gh <= 0) exit;");
        sb.AppendLine("_unit = min(_gw, _gh);");
        sb.AppendLine("_br = _unit * 0.085 * global.gmm_btn_scale;");
        sb.AppendLine("_jr = _unit * 0.17 * global.gmm_joy_scale;");
        sb.AppendLine("_jx = gmm_joy_fx * _gw;");
        sb.AppendLine("_jy = gmm_joy_fy * _gh;");
        sb.AppendLine();
        sb.AppendLine("// 采集本帧的触点（最多 5 指）");
        sb.AppendLine("var _n, _dn, _px_arr, _py_arr, _new_arr;");
        sb.AppendLine("_n = 0;");
        sb.AppendLine("_px_arr[0] = 0;");
        sb.AppendLine("_py_arr[0] = 0;");
        sb.AppendLine("_new_arr[0] = 0;");
        sb.AppendLine("for (_t = 0; _t < 5; _t += 1)");
        sb.AppendLine("{");
        sb.AppendLine("    if (device_mouse_check_button(_t, mb_left))");
        sb.AppendLine("    {");
        sb.AppendLine("        _px_arr[_n] = device_mouse_x_to_gui(_t);");
        sb.AppendLine("        _py_arr[_n] = device_mouse_y_to_gui(_t);");
        sb.AppendLine("        _new_arr[_n] = device_mouse_check_button_pressed(_t, mb_left);");
        sb.AppendLine("        _n += 1;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("// ---- 齿轮按钮：长按 0.7 秒进入 / 退出编辑模式 ----");
        sb.AppendLine("var _gear_x, _gear_y, _gear_r, _gear_down;");
        sb.AppendLine("_gear_r = _unit * 0.045;");
        sb.AppendLine("_gear_x = _gw - (_gear_r * 1.4);");
        sb.AppendLine("_gear_y = _gear_r * 1.4;");
        sb.AppendLine("_gear_down = 0;");
        sb.AppendLine("for (_i = 0; _i < _n; _i += 1)");
        sb.AppendLine("{");
        sb.AppendLine("    if (point_distance(_px_arr[_i], _py_arr[_i], _gear_x, _gear_y) <= _gear_r * 1.3) _gear_down = 1;");
        sb.AppendLine("}");
        sb.AppendLine("if (_gear_down == 1)");
        sb.AppendLine("{");
        sb.AppendLine("    gmm_gear_hold += 1;");
        sb.AppendLine("    if (gmm_gear_hold == gmm_hold_frames)");
        sb.AppendLine("    {");
        sb.AppendLine("        gmm_edit = 1 - gmm_edit;");
        sb.AppendLine("        gmm_sel = -1;");
        sb.AppendLine("        gmm_drag = -1;");
        sb.AppendLine("        gmm_repeat = gmm_repeat_frames * 3;");
        sb.AppendLine("        if (gmm_edit == 0) gmm_do_save = 1;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine("else");
        sb.AppendLine("{");
        sb.AppendLine("    gmm_gear_hold = 0;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("// ---- 统一的保存点（放在最前面：编辑逻辑里有 exit）----");
        sb.AppendLine("if (gmm_do_save == 1)");
        sb.AppendLine("{");
        sb.AppendLine("    gmm_do_save = 0;");
        sb.AppendLine(Indent(BuildSaveInline(), 1));
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("if (gmm_edit == 1 || global.gmm_touch_on != 1)");
        sb.AppendLine("{");
        sb.AppendLine(Indent(BuildReleaseAllInline(), 1));
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("if (gmm_edit == 1)");
        sb.AppendLine("{");
        sb.AppendLine(Indent(BuildEditLogic(), 1));
        sb.AppendLine("}");
        sb.AppendLine("else if (global.gmm_touch_on == 1)");
        sb.AppendLine("{");
        sb.AppendLine(Indent(BuildPlayLogic(), 1));
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string BuildPlayLogic()
    {
        var sb = new StringBuilder();
        sb.AppendLine("// ---- 普通按钮 ----");
        sb.AppendLine("for (_i = 0; _i < gmm_count; _i += 1)");
        sb.AppendLine("{");
        sb.AppendLine("    _hit = 0;");
        sb.AppendLine("    if (gmm_show[_i] == 1)");
        sb.AppendLine("    {");
        sb.AppendLine("        _px = gmm_fx[_i] * _gw;");
        sb.AppendLine("        _py = gmm_fy[_i] * _gh;");
        sb.AppendLine("        _pr = _br * gmm_size[_i];");
        sb.AppendLine("        for (_t = 0; _t < _n; _t += 1)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (point_distance(_px_arr[_t], _py_arr[_t], _px, _py) <= _pr) _hit = 1;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("    if (_hit == 1 && gmm_state[_i] == 0) keyboard_key_press(gmm_key[_i]);");
        sb.AppendLine("    if (_hit == 0 && gmm_state[_i] == 1) keyboard_key_release(gmm_key[_i]);");
        sb.AppendLine("    gmm_state[_i] = _hit;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("// ---- 摇杆 ----");
        sb.AppendLine("if (gmm_joy_on == 1 && gmm_joy_show == 1)");
        sb.AppendLine("{");
        sb.AppendLine("    var _found, _ddx, _ddy, _len, _dir, _want;");
        sb.AppendLine("    _found = 0;");
        sb.AppendLine("    _ddx = 0;");
        sb.AppendLine("    _ddy = 0;");
        sb.AppendLine("    for (_t = 0; _t < _n; _t += 1)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (_found == 0 && point_distance(_px_arr[_t], _py_arr[_t], _jx, _jy) <= _jr * 1.6)");
        sb.AppendLine("        {");
        sb.AppendLine("            _found = 1;");
        sb.AppendLine("            _ddx = _px_arr[_t] - _jx;");
        sb.AppendLine("            _ddy = _py_arr[_t] - _jy;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("    _want[0] = 0;");
        sb.AppendLine("    _want[1] = 0;");
        sb.AppendLine("    _want[2] = 0;");
        sb.AppendLine("    _want[3] = 0;");
        sb.AppendLine("    _len = point_distance(0, 0, _ddx, _ddy);");
        sb.AppendLine("    if (_found == 1 && _len > _jr * 0.28)");
        sb.AppendLine("    {");
        sb.AppendLine("        _dir = point_direction(0, 0, _ddx, _ddy);");
        sb.AppendLine("        if (_dir >= 292.5 || _dir <= 67.5) _want[3] = 1;");
        sb.AppendLine("        if (_dir >= 22.5 && _dir <= 157.5) _want[0] = 1;");
        sb.AppendLine("        if (_dir >= 112.5 && _dir <= 247.5) _want[2] = 1;");
        sb.AppendLine("        if (_dir >= 202.5 && _dir <= 337.5) _want[1] = 1;");
        sb.AppendLine("        if (_len > _jr)");
        sb.AppendLine("        {");
        sb.AppendLine("            _ddx = _ddx * (_jr / _len);");
        sb.AppendLine("            _ddy = _ddy * (_jr / _len);");
        sb.AppendLine("        }");
        sb.AppendLine("        gmm_joy_dx = _ddx;");
        sb.AppendLine("        gmm_joy_dy = _ddy;");
        sb.AppendLine("    }");
        sb.AppendLine("    else");
        sb.AppendLine("    {");
        sb.AppendLine("        gmm_joy_dx = 0;");
        sb.AppendLine("        gmm_joy_dy = 0;");
        sb.AppendLine("    }");
        sb.AppendLine("    for (_i = 0; _i < 4; _i += 1)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (_want[_i] == 1 && gmm_joy_state[_i] == 0) keyboard_key_press(gmm_joy_key[_i]);");
        sb.AppendLine("        if (_want[_i] == 0 && gmm_joy_state[_i] == 1) keyboard_key_release(gmm_joy_key[_i]);");
        sb.AppendLine("        gmm_joy_state[_i] = _want[_i];");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string BuildEditLogic()
    {
        var sb = new StringBuilder();
        sb.AppendLine("var _pw, _ph, _pnx, _pny, _rowh, _row, _on_panel, _left, _tdown, _tnew;");
        sb.AppendLine("_pw = _unit * 0.66;");
        sb.AppendLine("_ph = _unit * 0.62;");
        sb.AppendLine("_pnx = (_gw - _pw) * 0.5;");
        sb.AppendLine("_pny = (_gh - _ph) * 0.5;");
        sb.AppendLine("_rowh = _ph / 7;");
        sb.AppendLine();
        sb.AppendLine("_tdown = 0;");
        sb.AppendLine("_tnew = 0;");
        sb.AppendLine("_tx = 0;");
        sb.AppendLine("_ty = 0;");
        sb.AppendLine("if (_n > 0)");
        sb.AppendLine("{");
        sb.AppendLine("    _tdown = 1;");
        sb.AppendLine("    _tx = _px_arr[0];");
        sb.AppendLine("    _ty = _py_arr[0];");
        sb.AppendLine("    _tnew = _new_arr[0];");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("if (gmm_repeat > 0) gmm_repeat -= 1;");
        sb.AppendLine("if (_tdown == 0)");
        sb.AppendLine("{");
        sb.AppendLine("    gmm_repeat = 0;");
        sb.AppendLine("    gmm_drag = -1;");
        sb.AppendLine("    exit;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("_on_panel = 0;");
        sb.AppendLine("if (_tx >= _pnx && _tx <= _pnx + _pw && _ty >= _pny && _ty <= _pny + _ph) _on_panel = 1;");
        sb.AppendLine();
        sb.AppendLine("if (_on_panel == 1)");
        sb.AppendLine("{");
        sb.AppendLine("    gmm_drag = -1;");
        sb.AppendLine("    if (gmm_repeat <= 0)");
        sb.AppendLine("    {");
        sb.AppendLine("        gmm_repeat = gmm_repeat_frames;");
        sb.AppendLine("        _row = floor((_ty - _pny) / _rowh);");
        sb.AppendLine("        _left = 0;");
        sb.AppendLine("        if (_tx < _pnx + _pw * 0.5) _left = 1;");
        sb.AppendLine("        if (_row == 1)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (_left == 1) global.gmm_btn_scale = max(0.5, global.gmm_btn_scale - 0.05);");
        sb.AppendLine("            else global.gmm_btn_scale = min(2.5, global.gmm_btn_scale + 0.05);");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (_row == 2)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (_left == 1) global.gmm_joy_scale = max(0.5, global.gmm_joy_scale - 0.05);");
        sb.AppendLine("            else global.gmm_joy_scale = min(2.5, global.gmm_joy_scale + 0.05);");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (_row == 3)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (_left == 1) global.gmm_alpha = max(0.1, global.gmm_alpha - 0.05);");
        sb.AppendLine("            else global.gmm_alpha = min(1, global.gmm_alpha + 0.05);");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (_row == 4)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (gmm_sel == -2) gmm_joy_show = 1 - gmm_joy_show;");
        sb.AppendLine("            else if (gmm_sel >= 0 && gmm_sel < gmm_count) gmm_show[gmm_sel] = 1 - gmm_show[gmm_sel];");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (_row == 5)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (gmm_sel >= 0 && gmm_sel < gmm_count)");
        sb.AppendLine("            {");
        sb.AppendLine("                if (_left == 1) gmm_size[gmm_sel] = max(0.5, gmm_size[gmm_sel] - 0.05);");
        sb.AppendLine("                else gmm_size[gmm_sel] = min(2.5, gmm_size[gmm_sel] + 0.05);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (_row == 6)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (_left == 1)");
        sb.AppendLine("            {");
        sb.AppendLine("                global.gmm_touch_on = 1 - global.gmm_touch_on;");
        sb.AppendLine("            }");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine("                gmm_edit = 0;");
        sb.AppendLine("                gmm_drag = -1;");
        sb.AppendLine("                gmm_do_save = 1;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("    exit;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("// 选中 / 拖动控件");
        sb.AppendLine("if (gmm_drag == -1 && _tnew == 1)");
        sb.AppendLine("{");
        sb.AppendLine("    for (_i = 0; _i < gmm_count; _i += 1)");
        sb.AppendLine("    {");
        sb.AppendLine("        _px = gmm_fx[_i] * _gw;");
        sb.AppendLine("        _py = gmm_fy[_i] * _gh;");
        sb.AppendLine("        _pr = _br * gmm_size[_i];");
        sb.AppendLine("        if (gmm_drag == -1 && point_distance(_tx, _ty, _px, _py) <= _pr)");
        sb.AppendLine("        {");
        sb.AppendLine("            gmm_drag = _i;");
        sb.AppendLine("            gmm_sel = _i;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("    if (gmm_drag == -1 && gmm_joy_on == 1 && point_distance(_tx, _ty, _jx, _jy) <= _jr)");
        sb.AppendLine("    {");
        sb.AppendLine("        gmm_drag = -2;");
        sb.AppendLine("        gmm_sel = -2;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("if (gmm_drag >= 0)");
        sb.AppendLine("{");
        sb.AppendLine("    gmm_fx[gmm_drag] = clamp(_tx / _gw, 0.02, 0.98);");
        sb.AppendLine("    gmm_fy[gmm_drag] = clamp(_ty / _gh, 0.02, 0.98);");
        sb.AppendLine("}");
        sb.AppendLine("else if (gmm_drag == -2)");
        sb.AppendLine("{");
        sb.AppendLine("    gmm_joy_fx = clamp(_tx / _gw, 0.05, 0.95);");
        sb.AppendLine("    gmm_joy_fy = clamp(_ty / _gh, 0.05, 0.95);");
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>Draw GUI 事件。</summary>
    public static string BuildDrawGui()
    {
        var sb = new StringBuilder();
        sb.AppendLine("if (gmm_ready != 1) exit;");
        sb.AppendLine();
        sb.AppendLine("var _gw, _gh, _unit, _br, _jr, _jx, _jy, _i, _px, _py, _pr, _a;");
        sb.AppendLine("var _gear_r, _gear_x, _gear_y;");
        sb.AppendLine("_gw = display_get_gui_width();");
        sb.AppendLine("_gh = display_get_gui_height();");
        sb.AppendLine("if (_gw <= 0 || _gh <= 0) exit;");
        sb.AppendLine("_unit = min(_gw, _gh);");
        sb.AppendLine("_br = _unit * 0.085 * global.gmm_btn_scale;");
        sb.AppendLine("_jr = _unit * 0.17 * global.gmm_joy_scale;");
        sb.AppendLine("_jx = gmm_joy_fx * _gw;");
        sb.AppendLine("_jy = gmm_joy_fy * _gh;");
        sb.AppendLine("_gear_r = _unit * 0.045;");
        sb.AppendLine("_gear_x = _gw - (_gear_r * 1.4);");
        sb.AppendLine("_gear_y = _gear_r * 1.4;");
        sb.AppendLine();
        sb.AppendLine("var _old_a, _old_c, _old_ha, _old_va;");
        sb.AppendLine("_old_a = draw_get_alpha();");
        sb.AppendLine("_old_c = draw_get_colour();");
        sb.AppendLine("_old_ha = draw_get_halign();");
        sb.AppendLine("_old_va = draw_get_valign();");
        sb.AppendLine("draw_set_halign(fa_center);");
        sb.AppendLine("draw_set_valign(fa_middle);");
        sb.AppendLine();
        sb.AppendLine("// 齿轮按钮：长按 0.7 秒进入编辑模式");
        sb.AppendLine("draw_set_alpha(0.35);");
        sb.AppendLine("draw_set_colour(c_black);");
        sb.AppendLine("draw_circle(_gear_x, _gear_y, _gear_r, false);");
        sb.AppendLine("draw_set_alpha(0.9);");
        sb.AppendLine("draw_set_colour(c_white);");
        sb.AppendLine("draw_circle(_gear_x, _gear_y, _gear_r, true);");
        sb.AppendLine("draw_circle(_gear_x, _gear_y, _gear_r * 0.42, true);");
        sb.AppendLine("draw_text(_gear_x, _gear_y, \"E\");");
        sb.AppendLine();
        sb.AppendLine("if (gmm_edit == 0 && global.gmm_touch_on != 1)");
        sb.AppendLine("{");
        sb.AppendLine("    draw_set_alpha(_old_a);");
        sb.AppendLine("    draw_set_colour(_old_c);");
        sb.AppendLine("    draw_set_halign(_old_ha);");
        sb.AppendLine("    draw_set_valign(_old_va);");
        sb.AppendLine("    exit;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("_a = global.gmm_alpha;");
        sb.AppendLine("if (gmm_edit == 1) _a = max(_a, 0.75);");
        sb.AppendLine();
        sb.AppendLine("// 摇杆");
        sb.AppendLine("if (gmm_joy_on == 1 && (gmm_joy_show == 1 || gmm_edit == 1))");
        sb.AppendLine("{");
        sb.AppendLine("    draw_set_alpha(_a * 0.45);");
        sb.AppendLine("    draw_set_colour(c_black);");
        sb.AppendLine("    draw_circle(_jx, _jy, _jr, false);");
        sb.AppendLine("    draw_set_alpha(_a);");
        sb.AppendLine("    if (gmm_edit == 1 && gmm_sel == -2) draw_set_colour(c_yellow);");
        sb.AppendLine("    else if (gmm_joy_show == 0) draw_set_colour(c_red);");
        sb.AppendLine("    else draw_set_colour(c_white);");
        sb.AppendLine("    draw_circle(_jx, _jy, _jr, true);");
        sb.AppendLine("    draw_set_alpha(_a * 0.8);");
        sb.AppendLine("    draw_circle(_jx + gmm_joy_dx, _jy + gmm_joy_dy, _jr * 0.42, false);");
        sb.AppendLine("    if (gmm_joy_show == 0)");
        sb.AppendLine("    {");
        sb.AppendLine("        draw_set_colour(c_red);");
        sb.AppendLine("        draw_text(_jx, _jy - _jr * 0.7, \"OFF\");");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("// 按钮");
        sb.AppendLine("for (_i = 0; _i < gmm_count; _i += 1)");
        sb.AppendLine("{");
        sb.AppendLine("    if (gmm_show[_i] == 1 || gmm_edit == 1)");
        sb.AppendLine("    {");
        sb.AppendLine("        _px = gmm_fx[_i] * _gw;");
        sb.AppendLine("        _py = gmm_fy[_i] * _gh;");
        sb.AppendLine("        _pr = _br * gmm_size[_i];");
        sb.AppendLine("        if (gmm_state[_i] == 1) draw_set_alpha(_a * 0.75);");
        sb.AppendLine("        else draw_set_alpha(_a * 0.4);");
        sb.AppendLine("        draw_set_colour(c_black);");
        sb.AppendLine("        draw_circle(_px, _py, _pr, false);");
        sb.AppendLine("        draw_set_alpha(_a);");
        sb.AppendLine("        if (gmm_edit == 1 && gmm_sel == _i) draw_set_colour(c_yellow);");
        sb.AppendLine("        else if (gmm_show[_i] == 0) draw_set_colour(c_red);");
        sb.AppendLine("        else draw_set_colour(c_white);");
        sb.AppendLine("        draw_circle(_px, _py, _pr, true);");
        sb.AppendLine("        draw_text(_px, _py, gmm_label[_i]);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("// 编辑面板");
        sb.AppendLine("if (gmm_edit == 1)");
        sb.AppendLine("{");
        sb.AppendLine(Indent(BuildEditPanelDraw(), 1));
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("draw_set_alpha(_old_a);");
        sb.AppendLine("draw_set_colour(_old_c);");
        sb.AppendLine("draw_set_halign(_old_ha);");
        sb.AppendLine("draw_set_valign(_old_va);");
        return sb.ToString();
    }

    private static string BuildEditPanelDraw()
    {
        var sb = new StringBuilder();
        sb.AppendLine("var _pw, _ph, _pnx, _pny, _rowh, _cx, _k, _sel_name;");
        sb.AppendLine("_pw = _unit * 0.66;");
        sb.AppendLine("_ph = _unit * 0.62;");
        sb.AppendLine("_pnx = (_gw - _pw) * 0.5;");
        sb.AppendLine("_pny = (_gh - _ph) * 0.5;");
        sb.AppendLine("_rowh = _ph / 7;");
        sb.AppendLine("_cx = _pnx + _pw * 0.5;");
        sb.AppendLine();
        sb.AppendLine("draw_set_alpha(0.85);");
        sb.AppendLine("draw_set_colour(c_black);");
        sb.AppendLine("draw_rectangle(_pnx, _pny, _pnx + _pw, _pny + _ph, false);");
        sb.AppendLine("draw_set_alpha(1);");
        sb.AppendLine("draw_set_colour(c_white);");
        sb.AppendLine("draw_rectangle(_pnx, _pny, _pnx + _pw, _pny + _ph, true);");
        sb.AppendLine("for (_k = 1; _k < 7; _k += 1) draw_line(_pnx, _pny + _rowh * _k, _pnx + _pw, _pny + _rowh * _k);");
        sb.AppendLine();
        sb.AppendLine("if (gmm_sel == -2) _sel_name = \"JOYSTICK\";");
        sb.AppendLine("else if (gmm_sel >= 0 && gmm_sel < gmm_count) _sel_name = gmm_label[gmm_sel];");
        sb.AppendLine("else _sel_name = \"NONE\";");
        sb.AppendLine();
        sb.AppendLine("draw_text(_cx, _pny + _rowh * 0.5, \"EDIT  -  SELECTED: \" + _sel_name);");
        sb.AppendLine("draw_text(_cx, _pny + _rowh * 1.5, \"[-]  BUTTON SIZE \" + string(round(global.gmm_btn_scale * 100) / 100) + \"  [+]\");");
        sb.AppendLine("draw_text(_cx, _pny + _rowh * 2.5, \"[-]  JOYSTICK SIZE \" + string(round(global.gmm_joy_scale * 100) / 100) + \"  [+]\");");
        sb.AppendLine("draw_text(_cx, _pny + _rowh * 3.5, \"[-]  OPACITY \" + string(round(global.gmm_alpha * 100) / 100) + \"  [+]\");");
        sb.AppendLine("draw_text(_cx, _pny + _rowh * 4.5, \"TAP: SHOW / HIDE SELECTED\");");
        sb.AppendLine("draw_text(_cx, _pny + _rowh * 5.5, \"[-]  SELECTED SIZE  [+]\");");
        sb.AppendLine("if (global.gmm_touch_on == 1) draw_text(_cx, _pny + _rowh * 6.5, \"TOUCH: ON   |   SAVE & EXIT\");");
        sb.AppendLine("else draw_text(_cx, _pny + _rowh * 6.5, \"TOUCH: OFF   |   SAVE & EXIT\");");
        sb.AppendLine();
        sb.AppendLine("draw_set_alpha(0.6);");
        sb.AppendLine("draw_text(_cx, _pny + _ph + _rowh * 0.6, \"DRAG BUTTONS / JOYSTICK TO MOVE THEM\");");
        return sb.ToString();
    }

    /// <summary>CleanUp 事件：确保对象销毁时不会残留被按住的按键。</summary>
    public static string BuildCleanUp()
    {
        var sb = new StringBuilder();
        sb.AppendLine("if (gmm_ready == 1)");
        sb.AppendLine("{");
        sb.AppendLine("    var _i;");
        sb.AppendLine(Indent(BuildReleaseAllInline(), 1));
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>Room Start 事件：换房间后重新置顶，避免被游戏自己的 GUI 盖住。</summary>
    public static string BuildRoomStart()
    {
        var sb = new StringBuilder();
        sb.AppendLine("if (gmm_ready != 1) exit;");
        sb.AppendLine("depth = -100000;");
        sb.AppendLine("var _i;");
        sb.Append(BuildReleaseAllInline());
        return sb.ToString();
    }

    private static string BuildReleaseAllInline()
    {
        var sb = new StringBuilder();
        sb.AppendLine("for (_i = 0; _i < gmm_count; _i += 1)");
        sb.AppendLine("{");
        sb.AppendLine("    if (gmm_state[_i] == 1)");
        sb.AppendLine("    {");
        sb.AppendLine("        keyboard_key_release(gmm_key[_i]);");
        sb.AppendLine("        gmm_state[_i] = 0;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine("for (_i = 0; _i < 4; _i += 1)");
        sb.AppendLine("{");
        sb.AppendLine("    if (gmm_joy_state[_i] == 1)");
        sb.AppendLine("    {");
        sb.AppendLine("        keyboard_key_release(gmm_joy_key[_i]);");
        sb.AppendLine("        gmm_joy_state[_i] = 0;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine("gmm_joy_dx = 0;");
        sb.AppendLine("gmm_joy_dy = 0;");
        return sb.ToString();
    }

    private static string BuildSaveInline()
    {
        var sb = new StringBuilder();
        sb.AppendLine("ini_open(\"gmm_touch.ini\");");
        sb.AppendLine("ini_write_real(\"GMM\", \"on\", global.gmm_touch_on);");
        sb.AppendLine("ini_write_real(\"GMM\", \"alpha\", global.gmm_alpha);");
        sb.AppendLine("ini_write_real(\"GMM\", \"btn_scale\", global.gmm_btn_scale);");
        sb.AppendLine("ini_write_real(\"GMM\", \"joy_scale\", global.gmm_joy_scale);");
        sb.AppendLine("ini_write_real(\"GMM\", \"joy_show\", gmm_joy_show);");
        sb.AppendLine("ini_write_real(\"GMM\", \"joy_fx\", gmm_joy_fx);");
        sb.AppendLine("ini_write_real(\"GMM\", \"joy_fy\", gmm_joy_fy);");
        sb.AppendLine("for (_i = 0; _i < gmm_count; _i += 1)");
        sb.AppendLine("{");
        sb.AppendLine("    ini_write_real(\"GMM\", \"x\" + string(gmm_key[_i]), gmm_fx[_i]);");
        sb.AppendLine("    ini_write_real(\"GMM\", \"y\" + string(gmm_key[_i]), gmm_fy[_i]);");
        sb.AppendLine("    ini_write_real(\"GMM\", \"v\" + string(gmm_key[_i]), gmm_show[_i]);");
        sb.AppendLine("    ini_write_real(\"GMM\", \"s\" + string(gmm_key[_i]), gmm_size[_i]);");
        sb.AppendLine("}");
        sb.AppendLine("ini_close();");
        return sb.ToString();
    }

    private static string BuildOptimizationSnippet(bool isGameMaker2)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// ---- 移动端性能优化（无需 YYC）----");
        if (isGameMaker2)
        {
            sb.AppendLine("gpu_set_tex_filter(false);");
            sb.AppendLine("gpu_set_tex_repeat(false);");
        }
        else
        {
            sb.AppendLine("texture_set_interpolation(false);");
            sb.AppendLine("texture_set_repeat(false);");
        }

        sb.AppendLine("draw_set_circle_precision(16);");
        sb.AppendLine("display_reset(0, false);");
        sb.AppendLine("if (surface_exists(application_surface))");
        sb.AppendLine("{");
        sb.AppendLine("    var _sw, _sh, _cap;");
        sb.AppendLine("    _sw = surface_get_width(application_surface);");
        sb.AppendLine("    _sh = surface_get_height(application_surface);");
        sb.AppendLine("    _cap = 1080;");
        sb.AppendLine("    if (_sh > _cap && _sw > 0 && _sh > 0)");
        sb.AppendLine("    {");
        sb.AppendLine("        surface_resize(application_surface, max(1, round(_sw * (_cap / _sh))), _cap);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string Indent(string text, int levels)
    {
        var pad = new string(' ', levels * 4);
        var lines = text.TrimEnd('\r', '\n').Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        return string.Join("\n", lines.Select(l => l.Length == 0 ? l : pad + l));
    }

    private static string Num(double value) =>
        value.ToString("0.####", CultureInfo.InvariantCulture);

    private static string Escape(string text) =>
        text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}
