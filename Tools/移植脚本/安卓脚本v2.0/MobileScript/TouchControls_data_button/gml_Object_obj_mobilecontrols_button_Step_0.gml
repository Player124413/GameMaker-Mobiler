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
        virtual_key_delete(virtual_key_up2);
        virtual_key_delete(virtual_key_down2);
        virtual_key_delete(virtual_key_left2);
        virtual_key_delete(virtual_key_right2);
        virtual_key_delete(virtual_key_upp);
        virtual_key_delete(virtual_key_downp);
        virtual_key_delete(virtual_key_leftp);
        virtual_key_delete(virtual_key_rightp);
        virtual_key_delete(virtual_key_up2p);
        virtual_key_delete(virtual_key_down2p);
        virtual_key_delete(virtual_key_left2p);
        virtual_key_delete(virtual_key_right2p);
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

        if (global.dual_controls == 0)
        {
            ini_open("touchconfig_button.ini");
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

        ini_open("touchconfig_button.ini");
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
        ini_write_real("CONFIG", "up2x", up2x);
        ini_write_real("CONFIG", "up2y", up2y);
        ini_write_real("CONFIG", "down2x", down2x);
        ini_write_real("CONFIG", "down2y", down2y);
        ini_write_real("CONFIG", "left2x", left2x);
        ini_write_real("CONFIG", "left2y", left2y);
        ini_write_real("CONFIG", "right2x", right2x);
        ini_write_real("CONFIG", "right2y", right2y);
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
virtual_key_delete(virtual_key_upp);
virtual_key_delete(virtual_key_downp);
virtual_key_delete(virtual_key_leftp);
virtual_key_delete(virtual_key_rightp);
virtual_key_delete(virtual_key_up2p);
virtual_key_delete(virtual_key_down2p);
virtual_key_delete(virtual_key_left2p);
virtual_key_delete(virtual_key_right2p);
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
    else if (keyboard_check_pressed(50)) // Num 2 for up2
    {
        active_key = 50;
        audio_play_sound(snd_noise_mobile, 0, false);
    }
    else if (keyboard_check_pressed(51)) // Num 3 for down2
    {
        active_key = 51;
        audio_play_sound(snd_noise_mobile, 0, false);
    }
    else if (keyboard_check_pressed(52)) // Num 4 for left2
    {
        active_key = 52;
        audio_play_sound(snd_noise_mobile, 0, false);
    }
    else if (keyboard_check_pressed(53)) // Num 5 for right2
    {
        active_key = 53;
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
else if (active_key == 97)
{
    upx = device_mouse_x_to_gui(0) - (13.5 * analog_scale);
    upy = device_mouse_y_to_gui(0) - (12.5 * analog_scale);
}
else if (active_key == 98)
{
    downx = device_mouse_x_to_gui(0) - (13.5 * analog_scale);
    downy = device_mouse_y_to_gui(0) - (12.5 * analog_scale);
}
else if (active_key == 99)
{
    leftx = device_mouse_x_to_gui(0) - (13.5 * analog_scale);
    lefty = device_mouse_y_to_gui(0) - (12.5 * analog_scale);
}
else if (active_key == 100)
{
    rightx = device_mouse_x_to_gui(0) - (13.5 * analog_scale);
    righty = device_mouse_y_to_gui(0) - (12.5 * analog_scale);
}
else if (active_key == 50)
{
    up2x = device_mouse_x_to_gui(0) - (13.5 * analog_scale);
    up2y = device_mouse_y_to_gui(0) - (12.5 * analog_scale);
}
else if (active_key == 51)
{
    down2x = device_mouse_x_to_gui(0) - (13.5 * analog_scale);
    down2y = device_mouse_y_to_gui(0) - (12.5 * analog_scale);
}
else if (active_key == 52)
{
    left2x = device_mouse_x_to_gui(0) - (13.5 * analog_scale);
    left2y = device_mouse_y_to_gui(0) - (12.5 * analog_scale);
}
else if (active_key == 53)
{
    right2x = device_mouse_x_to_gui(0) - (13.5 * analog_scale);
    right2y = device_mouse_y_to_gui(0) - (12.5 * analog_scale);
}
else if (active_key == 101)
{
    f2x = device_mouse_x_to_gui(0) - (13.5 * analog_scale);
    f2y = device_mouse_y_to_gui(0) - (12.5 * analog_scale);
}
else if (active_key == 96)
{
    hx = device_mouse_x_to_gui(0) - (13.5 * button_scale);
    hy = device_mouse_y_to_gui(0) - (12.5 * button_scale);
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
    upx = 59;
    upy = 194;
    leftx = -22;
    lefty = 275;
    rightx = 140;
    righty = 275;
    downx = 59;
    downy = 356;
    up2x = 521;
    up2y = 194;
    left2x = 440;
    left2y = 275;
    right2x = 602;
    right2y = 275;
    down2x = 521;
    down2y = 356;
    button_scale = 3;
    analog_scale = 3.5;
    joystick_type = 0;
    controls_opacity = 0.5;
}
