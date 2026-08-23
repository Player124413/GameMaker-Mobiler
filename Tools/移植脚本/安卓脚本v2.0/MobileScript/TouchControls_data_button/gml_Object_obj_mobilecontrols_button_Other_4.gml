if (instance_exists(obj_mobilecontrols_button))
{
    var _gui_right = display_get_gui_width() - 640;
    var _gui_bottom = display_get_gui_height() - 480;
    var _zx = zx + _gui_right;
    var _zy = zy + _gui_bottom;
    var _xx = xx + _gui_right;
    var _xy = xy + _gui_bottom;
    var _cx = cx + _gui_right;
    var _cy = cy + _gui_bottom;
    var _upx = upx;
    var _upy = upy + _gui_bottom;
    var _leftx = leftx;
    var _lefty = lefty + _gui_bottom;
    var _rightx = rightx;
    var _righty = righty + _gui_bottom;
    var _downx = downx;
    var _downy = downy + _gui_bottom;

    virtual_key_zp = virtual_key_add(_zx, _zy, 27 * button_scale, 29 * button_scale, 125);
    virtual_key_xp = virtual_key_add(_xx, _xy, 27 * button_scale, 29 * button_scale, 124);
    virtual_key_cp = virtual_key_add(_cx, _cy, 27 * button_scale, 29 * button_scale, 94);
    virtual_key_upp = virtual_key_add(_upx, _upy, 27 * analog_scale, 29 * analog_scale, 97);
    virtual_key_downp = virtual_key_add(_downx, _downy, 27 * analog_scale, 29 * analog_scale, 98);
    virtual_key_leftp = virtual_key_add(_leftx, _lefty, 27 * analog_scale, 29 * analog_scale, 99);
    virtual_key_rightp = virtual_key_add(_rightx, _righty, 27 * analog_scale, 29 * analog_scale, 100);
    
    if (global.mobile_heal == 1)
        virtual_key_hp = virtual_key_add(hx, hy, 27 * button_scale, 29 * button_scale, 96);
    
    if (global.mobile_f2 == 1)
        virtual_key_restartp = virtual_key_add(f2x, f2y, 27 * button_scale, 29 * button_scale, 101);
    
    if (global.Android_System_Keyboard == 1)
        virtual_key_keyboard = virtual_key_add(keyboardx + _gui_right, keyboardy, 38, 50, 105);
    
    virtual_key_settings = virtual_key_add(-50, 5, 38, 50, 92);
    
    if (edit != 0)
        exit;
    
    virtual_key_z = virtual_key_add(_zx, _zy, 27 * button_scale, 29 * button_scale, 90);
    virtual_key_x = virtual_key_add(_xx, _xy, 27 * button_scale, 29 * button_scale, 88);
    virtual_key_c = virtual_key_add(_cx, _cy, 27 * button_scale, 29 * button_scale, 67);
    
    if (global.mobile_f2 == 1)
        virtual_key_restart = virtual_key_add(f2x, f2y, 27 * button_scale, 29 * button_scale, 113);
    
    if (global.mobile_heal == 1)
        virtual_key_h = virtual_key_add(hx, hy, 27 * button_scale, 29 * button_scale, 72);
    
    virtual_key_up = virtual_key_add(_upx, _upy, 27 * analog_scale, 29 * analog_scale, 38);
    virtual_key_down = virtual_key_add(_downx, _downy, 27 * analog_scale, 29 * analog_scale, 40);
    virtual_key_left = virtual_key_add(_leftx, _lefty, 27 * analog_scale, 29 * analog_scale, 37);
    virtual_key_right = virtual_key_add(_rightx, _righty, 27 * analog_scale, 29 * analog_scale, 39);
}
