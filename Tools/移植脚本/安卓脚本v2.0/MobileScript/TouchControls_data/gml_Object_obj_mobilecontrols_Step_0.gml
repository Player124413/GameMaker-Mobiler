if (global.Android_System_Keyboard == 1)
{
    if (keyboard_check_pressed(vk_numpad9))
        keyboard_virtual_show(1, 3, 0, 0);
}

var analog_touch_id = -1;
var analog2_touch_id = -1;
var deadzone = 41 * analog_scale;

for (var i = 0; i < 10; i++)
{
    if (device_mouse_check_button(i, mb_left))
    {
        var touch_x = device_mouse_x_to_gui(i);
        var touch_y = device_mouse_y_to_gui(i);

        if (touch_x >= (analog_posx - deadzone) && touch_x <= (analog_posx + (59 * analog_scale) + deadzone) && touch_y >= (analog_posy - deadzone) && touch_y <= (analog_posy + (59 * analog_scale) + deadzone))
            analog_touch_id = i;

        if (touch_x >= (analog2_posx - deadzone) && touch_x <= (analog2_posx + (59 * analog_scale) + deadzone) && touch_y >= (analog2_posy - deadzone) && touch_y <= (analog2_posy + (59 * analog_scale) + deadzone))
            analog2_touch_id = i;
    }
}

if (analog_touch_id != -1 && device_mouse_check_button(analog_touch_id, mb_left))
{
    var touch_x = device_mouse_x_to_gui(analog_touch_id);
    var touch_y = device_mouse_y_to_gui(analog_touch_id);
    var joy_center_x = analog_posx + ((59 * analog_scale) / 2);
    var joy_center_y = analog_posy + ((59 * analog_scale) / 2);
    var max_radius = (59 * analog_scale) / 2.75;
    var dist = point_distance(joy_center_x, joy_center_y, touch_x, touch_y);
    
    if (dist <= max_radius)
    {
        analog_center_x = touch_x - (21 * analog_scale);
        analog_center_y = touch_y - (21 * analog_scale);
    }
    else
    {
        var dir = point_direction(joy_center_x, joy_center_y, touch_x, touch_y);
        analog_center_x = (joy_center_x + lengthdir_x(max_radius, dir)) - (21 * analog_scale);
        analog_center_y = (joy_center_y + lengthdir_y(max_radius, dir)) - (21 * analog_scale);
    }
}
else
{
    analog_center_x = (analog_posx + ((59 * analog_scale) / 2)) - ((41 * analog_scale) / 2);
    analog_center_y = (analog_posy + ((59 * analog_scale) / 2)) - ((41 * analog_scale) / 2);
    analog_touch_id = -1;
}

if (analog2_touch_id != -1 && device_mouse_check_button(analog2_touch_id, mb_left))
{
    var touch_x = device_mouse_x_to_gui(analog2_touch_id);
    var touch_y = device_mouse_y_to_gui(analog2_touch_id);
    var joy_center_x = analog2_posx + ((59 * analog_scale) / 2);
    var joy_center_y = analog2_posy + ((59 * analog_scale) / 2);
    var max_radius = (59 * analog_scale) / 2.75;
    var dist = point_distance(joy_center_x, joy_center_y, touch_x, touch_y);

    if (dist <= max_radius)
    {
        analog2_center_x = touch_x - (21 * analog_scale);
        analog2_center_y = touch_y - (21 * analog_scale);
    }
    else
    {
        var dir = point_direction(joy_center_x, joy_center_y, touch_x, touch_y);
        analog2_center_x = (joy_center_x + lengthdir_x(max_radius, dir)) - (21 * analog_scale);
        analog2_center_y = (joy_center_y + lengthdir_y(max_radius, dir)) - (21 * analog_scale);
    }
}
else
{
    analog2_center_x = (analog2_posx + ((59 * analog_scale) / 2)) - ((41 * analog_scale) / 2);
    analog2_center_y = (analog2_posy + ((59 * analog_scale) / 2)) - ((41 * analog_scale) / 2);
    analog2_touch_id = -1;
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
        virtual_key_delete(virtual_key_up2);
        virtual_key_delete(virtual_key_down2);
        virtual_key_delete(virtual_key_left2);
        virtual_key_delete(virtual_key_right2);
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
        virtual_key_delete(virtual_key_analog);
        virtual_key_delete(virtual_key_analog2);
        virtual_key_delete(virtual_key_analogp);
        virtual_key_delete(virtual_key_analog2p);

        if (global.dual_controls == 0)
        {
            ini_open("touchconfig.ini");
            ini_write_real("CONFIG", "zx", zx);
            ini_write_real("CONFIG", "zy", zy);
            ini_write_real("CONFIG", "xx", xx);
            ini_write_real("CONFIG", "xy", xy);
            ini_write_real("CONFIG", "cx", cx);
            ini_write_real("CONFIG", "cy", cy);
            ini_close();
        }
        else if (global.dual_controls == 1)
        {
            ini_open("touchconfig2.ini");
            ini_write_real("CONFIG", "zx", zx2);
            ini_write_real("CONFIG", "zy", zy2);
            ini_write_real("CONFIG", "xx", xx2);
            ini_write_real("CONFIG", "xy", xy2);
            ini_write_real("CONFIG", "cx", cx2);
            ini_write_real("CONFIG", "cy", cy2);
            ini_close();
        }

        ini_open("touchconfig.ini");
        ini_write_real("CONFIG", "f2x", f2x);
        ini_write_real("CONFIG", "f2y", f2y);
        ini_write_real("CONFIG", "hx", hx);
        ini_write_real("CONFIG", "hy", hy);
        ini_write_real("CONFIG", "analog_posx", analog_posx);
        ini_write_real("CONFIG", "analog_posy", analog_posy);
        ini_write_real("CONFIG", "analog2_posx", analog2_posx);
        ini_write_real("CONFIG", "analog2_posy", analog2_posy);
        ini_write_real("CONFIG", "button_scale", button_scale);
        ini_write_real("CONFIG", "analog_scale", analog_scale);
        ini_write_real("CONFIG", "joystick_type", joystick_type);
        ini_write_real("CONFIG", "controls_opacity", controls_opacity);
        ini_close();
        edit = 0;
        scr_add_keys();
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
virtual_key_delete(virtual_key_up2);
virtual_key_delete(virtual_key_down2);
virtual_key_delete(virtual_key_left2);
virtual_key_delete(virtual_key_right2);
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
virtual_key_delete(virtual_key_analog);
virtual_key_delete(virtual_key_analog2);
virtual_key_delete(virtual_key_analogp);
virtual_key_delete(virtual_key_analog2p);
scr_add_keys();

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
    else if (keyboard_check_pressed(vk_numpad5))
    {
        active_key = 101;
        audio_play_sound(snd_noise_mobile, 0, false);
    }
    else if (keyboard_check_pressed(vk_numpad0))
    {
        active_key = 96;
        audio_play_sound(snd_noise_mobile, 0, false);
    }
    else if (keyboard_check_pressed(59) && global.dual_controls == 1)
    {
        active_key = 59;
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
    if (global.dual_controls == 0)
    {
        zx = device_mouse_x_to_gui(0) - (13.5 * button_scale);
        zy = device_mouse_y_to_gui(0) - (12.5 * button_scale);
    }
    else
    {
        zx2 = device_mouse_x_to_gui(0) - (13.5 * button_scale);
        zy2 = device_mouse_y_to_gui(0) - (12.5 * button_scale);
    }
}
else if (active_key == 124)
{
    if (global.dual_controls == 0)
    {
        xx = device_mouse_x_to_gui(0) - (13.5 * button_scale);
        xy = device_mouse_y_to_gui(0) - (12.5 * button_scale);
    }
    else
    {
        xx2 = device_mouse_x_to_gui(0) - (13.5 * button_scale);
        xy2 = device_mouse_y_to_gui(0) - (12.5 * button_scale);
    }
}
else if (active_key == 94)
{
    if (global.dual_controls == 0)
    {
        cx = device_mouse_x_to_gui(0) - (13.5 * button_scale);
        cy = device_mouse_y_to_gui(0) - (12.5 * button_scale);
    }
    else
    {
        cx2 = device_mouse_x_to_gui(0) - (13.5 * button_scale);
        cy2 = device_mouse_y_to_gui(0) - (12.5 * button_scale);
    }
}
else if (active_key == 101)
{
    f2x = device_mouse_x_to_gui(0) - (13.5 * button_scale);
    f2y = device_mouse_y_to_gui(0) - (12.5 * button_scale);
}
else if (active_key == 96)
{
    hx = device_mouse_x_to_gui(0) - (13.5 * button_scale);
    hy = device_mouse_y_to_gui(0) - (12.5 * button_scale);
}
else if (active_key == 93)
{
    analog_posx = device_mouse_x_to_gui(0) - (29.5 * analog_scale);
    analog_posy = device_mouse_y_to_gui(0) - (29.5 * analog_scale);
}
else if (active_key == 59 && global.dual_controls == 1)
{
    analog2_posx = device_mouse_x_to_gui(0) - (29.5 * analog_scale);
    analog2_posy = device_mouse_y_to_gui(0) - (29.5 * analog_scale);
}

if (device_mouse_x_to_gui(0) >= 440.5 && device_mouse_y_to_gui(0) >= 75 && device_mouse_x_to_gui(0) <= 469.5 && device_mouse_y_to_gui(0) <= 93 && mouse_check_button_pressed(mb_left))
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

if (device_mouse_x_to_gui(0) >= 531.5 && device_mouse_y_to_gui(0) >= 75 && device_mouse_x_to_gui(0) <= 561.5 && device_mouse_y_to_gui(0) <= 93 && mouse_check_button_pressed(mb_left))
{
    button_scale += 0.1;
    audio_play_sound(snd_coin_mobile, 0, false);
}

if (device_mouse_x_to_gui(0) >= 440.5 && device_mouse_y_to_gui(0) >= 121 && device_mouse_x_to_gui(0) <= 469.5 && device_mouse_y_to_gui(0) <= 139 && mouse_check_button_pressed(mb_left))
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

if (device_mouse_x_to_gui(0) >= 531.5 && device_mouse_y_to_gui(0) >= 121 && device_mouse_x_to_gui(0) <= 561.5 && device_mouse_y_to_gui(0) <= 139 && mouse_check_button_pressed(mb_left))
{
    analog_scale += 0.1;
    audio_play_sound(snd_coin_mobile, 0, false);
}

if (device_mouse_x_to_gui(0) >= 440.5 && device_mouse_y_to_gui(0) >= 167 && device_mouse_x_to_gui(0) <= 469.5 && device_mouse_y_to_gui(0) <= 185 && mouse_check_button_pressed(mb_left))
{
    if (joystick_type == 1)
    {
        joystick_type -= 1;
        audio_play_sound(snd_equip_mobile, 0, false);
    }
    else
    {
        audio_play_sound(snd_hurt_mobile, 0, false);
    }
}

if (device_mouse_x_to_gui(0) >= 531.5 && device_mouse_y_to_gui(0) >= 167 && device_mouse_x_to_gui(0) <= 561.5 && device_mouse_y_to_gui(0) <= 185 && mouse_check_button_pressed(mb_left))
{
    if (joystick_type == 0)
    {
        joystick_type += 1;
        audio_play_sound(snd_coin_mobile, 0, false);
    }
    else
    {
        audio_play_sound(snd_hurt_mobile, 0, false);
    }
}

if (device_mouse_x_to_gui(0) >= 440.5 && device_mouse_y_to_gui(0) >= 213 && device_mouse_x_to_gui(0) <= 469.5 && device_mouse_y_to_gui(0) <= 231 && mouse_check_button_pressed(mb_left))
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

if (device_mouse_x_to_gui(0) >= 531.5 && device_mouse_y_to_gui(0) >= 213 && device_mouse_x_to_gui(0) <= 561.5 && device_mouse_y_to_gui(0) <= 231 && mouse_check_button_pressed(mb_left))
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

if (device_mouse_x_to_gui(0) >= 241 && device_mouse_y_to_gui(0) >= 412.25 && device_mouse_x_to_gui(0) <= 399 && device_mouse_y_to_gui(0) <= 436.25 && mouse_check_button_pressed(mb_left))
{
    audio_play_sound(snd_noise_mobile, 0, false);
    zx = 404;
    zy = 338;
    xx = 488;
    xy = 294;
    cx = 573;
    cy = 253;
    zx2 = 454;
    zy2 = 0;
    xx2 = 538;
    xy2 = 0;
    cx2 = 623;
    cy2 = 0;
    hx = 350;
    hy = 5;
    f2x = 5;
    f2y = 5;
    button_scale = 3;
    analog_scale = 3.5;
    analog_posx = -42;
    analog_posy = 232.5;
    analog2_posx = 475.5;
    analog2_posy = 232.5;
    joystick_type = 0;
    controls_opacity = 0.5;
}
