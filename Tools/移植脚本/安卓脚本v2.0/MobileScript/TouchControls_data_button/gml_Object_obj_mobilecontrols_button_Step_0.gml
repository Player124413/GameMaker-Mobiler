var _gui_width = display_get_gui_width();
var _gui_height = display_get_gui_height();
var _gui_right = _gui_width - 640;
var _gui_bottom = _gui_height - 480;
var _touch_x = device_mouse_x_to_gui(0);
var _touch_y = device_mouse_y_to_gui(0);
var _layout_x = (_touch_x >= _gui_width * 0.5) ? _touch_x - _gui_right : _touch_x;
var _layout_y = (_touch_y >= _gui_height * 0.5) ? _touch_y - _gui_bottom : _touch_y;

if (global.Android_System_Keyboard == 1)
{
    if (keyboard_check_pressed(vk_numpad9))
        keyboard_virtual_show(1, 3, 0, 0);
}

if (keyboard_check_pressed(92))
{
    edit += 1;
    
    if (edit == 1)
    {
        show = 1;
        audio_play_sound(snd_spearappear_mobile, 0, false);
    }
    else if (edit == 3)
    {
        audio_play_sound(snd_egg_mobile, 0, false);
        virtual_key_delete(virtual_key_up);
        virtual_key_delete(virtual_key_down);
        virtual_key_delete(virtual_key_left);
        virtual_key_delete(virtual_key_right);
        virtual_key_delete(virtual_key_upp);
        virtual_key_delete(virtual_key_downp);
        virtual_key_delete(virtual_key_leftp);
        virtual_key_delete(virtual_key_rightp);
        virtual_key_delete(virtual_key_z);
        virtual_key_delete(virtual_key_x);
        virtual_key_delete(virtual_key_c);
        
        if (global.mobile_f2 == 1)
        {
            virtual_key_delete(virtual_key_restart);
            virtual_key_delete(virtual_key_restartp);
        }
        
        if (global.mobile_heal == 1)
        {
            virtual_key_delete(virtual_key_h);
            virtual_key_delete(virtual_key_hp);
        }
        
        virtual_key_delete(virtual_key_zp);
        virtual_key_delete(virtual_key_xp);
        virtual_key_delete(virtual_key_cp);
        ini_open("touchconfig_button.ini");
        ini_write_real("CONFIG", "zx", zx);
        ini_write_real("CONFIG", "zy", zy);
        ini_write_real("CONFIG", "xx", xx);
        ini_write_real("CONFIG", "xy", xy);
        ini_write_real("CONFIG", "cx", cx);
        ini_write_real("CONFIG", "cy", cy);
        ini_write_real("CONFIG", "f2x", f2x);
        ini_write_real("CONFIG", "f2y", f2y);
        ini_write_real("CONFIG", "hx", hx);
        ini_write_real("CONFIG", "hy", hy);
        ini_write_real("CONFIG", "upx", upx);
        ini_write_real("CONFIG", "upy", upy);
        ini_write_real("CONFIG", "downx", downx);
        ini_write_real("CONFIG", "downy", downy);
        ini_write_real("CONFIG", "leftx", leftx);
        ini_write_real("CONFIG", "lefty", lefty);
        ini_write_real("CONFIG", "rightx", rightx);
        ini_write_real("CONFIG", "righty", righty);
        ini_write_real("CONFIG", "button_scale", button_scale);
        ini_write_real("CONFIG", "analog_scale", analog_scale);
        ini_write_real("CONFIG", "joystick_type", joystick_type);
        ini_write_real("CONFIG", "controls_opacity", controls_opacity);
        ini_close();
        edit = 0;
        scr_add_keys_button();
    }
}

image_alpha = (show == 1) ? min(image_alpha + 0.1, 1) : max(image_alpha - 0.1, 0);
black_fade = (edit == 0) ? max(black_fade - 0.04, 0) : min(black_fade + 0.04, 0.4);
text_black_fade = (edit == 0) ? max(text_black_fade - 0.09, 0) : min(text_black_fade + 0.09, 0.9);

if (edit == 0)
    exit;

virtual_key_delete(virtual_key_up);
virtual_key_delete(virtual_key_down);
virtual_key_delete(virtual_key_left);
virtual_key_delete(virtual_key_right);
virtual_key_delete(virtual_key_z);
virtual_key_delete(virtual_key_x);
virtual_key_delete(virtual_key_c);

if (global.mobile_f2 == 1)
{
    virtual_key_delete(virtual_key_restart);
    virtual_key_delete(virtual_key_restartp);
}

if (global.mobile_heal == 1)
{
    virtual_key_delete(virtual_key_h);
    virtual_key_delete(virtual_key_hp);
}

virtual_key_delete(virtual_key_zp);
virtual_key_delete(virtual_key_xp);
virtual_key_delete(virtual_key_cp);
virtual_key_delete(virtual_key_upp);
virtual_key_delete(virtual_key_downp);
virtual_key_delete(virtual_key_leftp);
virtual_key_delete(virtual_key_rightp);
scr_add_keys_button();

if (active_key == -1)
{
    if (keyboard_check_pressed(125))
    {
        active_key = 125;
        audio_play_sound(snd_noise_mobile, 0, false);
    }
    else if (keyboard_check_pressed(124))
    {
        active_key = 124;
        audio_play_sound(snd_noise_mobile, 0, false);
    }
    else if (keyboard_check_pressed(94))
    {
        active_key = 94;
        audio_play_sound(snd_noise_mobile, 0, false);
    }
    else if (keyboard_check_pressed(93))
    {
        active_key = 93;
        audio_play_sound(snd_noise_mobile, 0, false);
    }
    else if (keyboard_check_pressed(vk_numpad0))
    {
        active_key = 96;
        audio_play_sound(snd_noise_mobile, 0, false);
    }
    else if (keyboard_check_pressed(vk_numpad5))
    {
        active_key = 101;
        audio_play_sound(snd_noise_mobile, 0, false);
    }
    else if (keyboard_check_pressed(vk_numpad1))
    {
        active_key = 97;
        audio_play_sound(snd_noise_mobile, 0, false);
    }
    else if (keyboard_check_pressed(vk_numpad2))
    {
        active_key = 98;
        audio_play_sound(snd_noise_mobile, 0, false);
    }
    else if (keyboard_check_pressed(vk_numpad3))
    {
        active_key = 99;
        audio_play_sound(snd_noise_mobile, 0, false);
    }
    else if (keyboard_check_pressed(vk_numpad4))
    {
        active_key = 100;
        audio_play_sound(snd_noise_mobile, 0, false);
    }
}

if (active_key != -1 && keyboard_check_released(active_key))
{
    audio_play_sound(snd_menu_confirm_mobile, 0, false);
    active_key = -1;
}

if (active_key == 125)
{
    zx = _layout_x - (13.5 * button_scale);
    zy = _layout_y - (12.5 * button_scale);
}
else if (active_key == 124)
{
    xx = _layout_x - (13.5 * button_scale);
    xy = _layout_y - (12.5 * button_scale);
}
else if (active_key == 94)
{
    cx = _layout_x - (13.5 * button_scale);
    cy = _layout_y - (12.5 * button_scale);
}
else if (active_key == 97)
{
    upx = _layout_x - (13.5 * analog_scale);
    upy = _layout_y - (12.5 * analog_scale);
}
else if (active_key == 98)
{
    downx = _layout_x - (13.5 * analog_scale);
    downy = _layout_y - (12.5 * analog_scale);
}
else if (active_key == 99)
{
    leftx = _layout_x - (13.5 * analog_scale);
    lefty = _layout_y - (12.5 * analog_scale);
}
else if (active_key == 100)
{
    rightx = _layout_x - (13.5 * analog_scale);
    righty = _layout_y - (12.5 * analog_scale);
}
else if (active_key == 101)
{
    f2x = _layout_x - (13.5 * analog_scale);
    f2y = _layout_y - (12.5 * analog_scale);
}
else if (active_key == 96)
{
    hx = _layout_x - (13.5 * button_scale);
    hy = _layout_y - (12.5 * button_scale);
}

if (_layout_x >= 440.5 && _layout_y >= 75 && _layout_x <= 469.5 && _layout_y <= 93 && mouse_check_button_pressed(mb_left))
{
    if (button_scale > 1)
    {
        button_scale -= 0.1;
        audio_play_sound(snd_equip_mobile, 0, false);
    }
    else
    {
        audio_play_sound(snd_hurt_mobile, 0, false);
    }
}

if (_layout_x >= 531.5 && _layout_y >= 75 && _layout_x <= 561.5 && _layout_y <= 93 && mouse_check_button_pressed(mb_left))
{
    button_scale += 0.1;
    audio_play_sound(snd_coin_mobile, 0, false);
}

if (_layout_x >= 440.5 && _layout_y >= 121 && _layout_x <= 469.5 && _layout_y <= 139 && mouse_check_button_pressed(mb_left))
{
    if (analog_scale > 1)
    {
        analog_scale -= 0.1;
        audio_play_sound(snd_equip_mobile, 0, false);
    }
    else
    {
        audio_play_sound(snd_hurt_mobile, 0, false);
    }
}

if (_layout_x >= 531.5 && _layout_y >= 121 && _layout_x <= 561.5 && _layout_y <= 139 && mouse_check_button_pressed(mb_left))
{
    analog_scale += 0.1;
    audio_play_sound(snd_coin_mobile, 0, false);
}

if (_layout_x >= 440.5 && _layout_y >= 167 && _layout_x <= 469.5 && _layout_y <= 185 && mouse_check_button_pressed(mb_left))
{
    if (controls_opacity > 0.1)
    {
        controls_opacity -= 0.05;
        audio_play_sound(snd_equip_mobile, 0, false);
    }
    else
    {
        audio_play_sound(snd_hurt_mobile, 0, false);
    }
}

if (_layout_x >= 531.5 && _layout_y >= 167 && _layout_x <= 561.5 && _layout_y <= 185 && mouse_check_button_pressed(mb_left))
{
    if (controls_opacity < 1)
    {
        controls_opacity += 0.05;
        audio_play_sound(snd_coin_mobile, 0, false);
    }
    else
    {
        audio_play_sound(snd_hurt_mobile, 0, false);
    }
}

if (_layout_x >= 241 && _layout_y >= 412.25 && _layout_x <= 399 && _layout_y <= 436.25 && mouse_check_button_pressed(mb_left))
{
    audio_play_sound(snd_noise_mobile, 0, false);
    zx = 404;
    zy = 338;
    xx = 488;
    xy = 294;
    cx = 573;
    cy = 253;
    hx = 556;
    hy = 5;
    f2x = 5;
    f2y = 5;
    upx = 59;
    upy = 194;
    leftx = -22;
    lefty = 275;
    rightx = 140;
    righty = 275;
    downx = 59;
    downy = 356;
    button_scale = 3;
    analog_scale = 3.5;
    joystick_type = 0;
    controls_opacity = 0.5;
}

