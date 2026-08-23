if (instance_exists(obj_mobilecontrols))
{
    var _gui_right = display_get_gui_width() - 640;
    var _gui_bottom = display_get_gui_height() - 480;
    var _zx = zx + _gui_right;
    var _zy = zy + _gui_bottom;
    var _xx = xx + _gui_right;
    var _xy = xy + _gui_bottom;
    var _cx = cx + _gui_right;
    var _cy = cy + _gui_bottom;
    var _analog_x = analog_posx;
    var _analog_y = analog_posy + _gui_bottom;

    virtual_key_zp = virtual_key_add(_zx, _zy, 27 * button_scale, 29 * button_scale, 125);
    virtual_key_xp = virtual_key_add(_xx, _xy, 27 * button_scale, 29 * button_scale, 124);
    virtual_key_cp = virtual_key_add(_cx, _cy, 27 * button_scale, 29 * button_scale, 94);
    
    if (global.mobile_heal == 1)
        virtual_key_hp = virtual_key_add(hx, hy, 27 * button_scale, 29 * button_scale, 96);
    
    if (global.mobile_f2 == 1)
        virtual_key_restartp = virtual_key_add(f2x, f2y, 27 * button_scale, 29 * button_scale, 101);
    
    virtual_key_analogp = virtual_key_add(_analog_x, _analog_y, 59 * analog_scale, 59 * analog_scale, 93);
    virtual_key_settings = virtual_key_add(settx, setty, 38, 50, 92);
    
    if (global.Android_System_Keyboard == 1)
        virtual_key_keyboard = virtual_key_add(keyboardx + _gui_right, keyboardy, 38, 50, 105);
    
    if (edit != 0)
        exit;
    
    virtual_key_z = virtual_key_add(_zx, _zy, 27 * button_scale, 29 * button_scale, 90);
    virtual_key_x = virtual_key_add(_xx, _xy, 27 * button_scale, 29 * button_scale, 88);
    virtual_key_c = virtual_key_add(_cx, _cy, 27 * button_scale, 29 * button_scale, 67);
    
    if (global.mobile_heal == 1)
        virtual_key_h = virtual_key_add(hx, hy, 27 * button_scale, 29 * button_scale, 72);
    
    if (global.mobile_f2 == 1)
        virtual_key_restart = virtual_key_add(f2x, f2y, 27 * button_scale, 29 * button_scale, 113);
    
    virtual_key_up = virtual_key_add(_analog_x - (arrowkeys_back_area_size * analog_scale), _analog_y - (arrowkeys_back_area_size * analog_scale), (arrowkeys_back_area_size * analog_scale) + ((59 * analog_scale) + (arrowkeys_back_area_size * analog_scale)), (arrowkeys_area_size * analog_scale) + (arrowkeys_back_area_size * analog_scale), 38);
    virtual_key_right = virtual_key_add((_analog_x + (59 * analog_scale)) - (arrowkeys_area_size * analog_scale), _analog_y - (arrowkeys_back_area_size * analog_scale), (arrowkeys_area_size * analog_scale) + (arrowkeys_back_area_size * analog_scale), (arrowkeys_back_area_size * analog_scale) + (59 * analog_scale) + (arrowkeys_back_area_size * analog_scale), 39);
    virtual_key_left = virtual_key_add(_analog_x - (arrowkeys_back_area_size * analog_scale), _analog_y - (arrowkeys_back_area_size * analog_scale), (arrowkeys_area_size * analog_scale) + (arrowkeys_back_area_size * analog_scale), (arrowkeys_back_area_size * analog_scale) + ((59 * analog_scale) + (arrowkeys_back_area_size * analog_scale)), 37);
    virtual_key_down = virtual_key_add(_analog_x - (arrowkeys_back_area_size * analog_scale), (_analog_y + (59 * analog_scale)) - (arrowkeys_area_size * analog_scale), (arrowkeys_back_area_size * analog_scale) + (59 * analog_scale) + (arrowkeys_back_area_size * analog_scale), (arrowkeys_area_size * analog_scale) + (arrowkeys_back_area_size * analog_scale), 40);
    virtual_key_analog = virtual_key_add(_analog_x - (arrowkeys_back_area_size * analog_scale), _analog_y - (arrowkeys_back_area_size * analog_scale), ((59 + arrowkeys_back_area_size) * analog_scale) + (arrowkeys_back_area_size * analog_scale), ((59 + arrowkeys_back_area_size) * analog_scale) + (arrowkeys_back_area_size * analog_scale), 126);
}
