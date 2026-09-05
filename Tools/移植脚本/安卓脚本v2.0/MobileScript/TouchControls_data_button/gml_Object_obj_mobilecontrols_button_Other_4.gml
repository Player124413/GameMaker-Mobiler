if (instance_exists(obj_mobilecontrols_button))
{
    virtual_key_zp = virtual_key_add((global.dual_controls == 1) ? zx2 : zx, (global.dual_controls == 1) ? zy2 : zy, 27 * button_scale, 29 * button_scale, 125);
    virtual_key_xp = virtual_key_add((global.dual_controls == 1) ? xx2 : xx, (global.dual_controls == 1) ? xy2 : xy, 27 * button_scale, 29 * button_scale, 124);
    virtual_key_cp = virtual_key_add((global.dual_controls == 1) ? cx2 : cx, (global.dual_controls == 1) ? cy2 : cy, 27 * button_scale, 29 * button_scale, 94);
    virtual_key_upp = virtual_key_add(upx, upy, 27 * analog_scale, 29 * analog_scale, 97);
    virtual_key_downp = virtual_key_add(downx, downy, 27 * analog_scale, 29 * analog_scale, 98);
    virtual_key_leftp = virtual_key_add(leftx, lefty, 27 * analog_scale, 29 * analog_scale, 99);
    virtual_key_rightp = virtual_key_add(rightx, righty, 27 * analog_scale, 29 * analog_scale, 100);
    
    if (global.dual_controls == 1)
    {
        virtual_key_up2p = virtual_key_add(up2x, up2y, 27 * analog_scale, 29 * analog_scale, 50); // Num 2
        virtual_key_down2p = virtual_key_add(down2x, down2y, 27 * analog_scale, 29 * analog_scale, 51); // Num 3
        virtual_key_left2p = virtual_key_add(left2x, left2y, 27 * analog_scale, 29 * analog_scale, 52); // Num 4
        virtual_key_right2p = virtual_key_add(right2x, right2y, 27 * analog_scale, 29 * analog_scale, 53); // Num 5
    }

    if (global.mobile_heal == 1)
        virtual_key_hp = virtual_key_add(hx, hy, 27 * button_scale, 29 * button_scale, 96);
    
    if (global.mobile_f2 == 1)
        virtual_key_restartp = virtual_key_add(f2x, f2y, 27 * button_scale, 29 * button_scale, 101);
    
    if (global.Android_System_Keyboard == 1)
        virtual_key_keyboard = virtual_key_add(keyboardx, keyboardy, 38, 50, 105);
    
    virtual_key_settings = virtual_key_add(-50, 5, 38, 50, 92);
    
    if (edit != 0)
        exit;
    
    virtual_key_z = virtual_key_add((global.dual_controls == 1) ? zx2 : zx, (global.dual_controls == 1) ? zy2 : zy, 27 * button_scale, 29 * button_scale, 90);
    virtual_key_x = virtual_key_add((global.dual_controls == 1) ? xx2 : xx, (global.dual_controls == 1) ? xy2 : xy, 27 * button_scale, 29 * button_scale, 88);
    virtual_key_c = virtual_key_add((global.dual_controls == 1) ? cx2 : cx, (global.dual_controls == 1) ? cy2 : cy, 27 * button_scale, 29 * button_scale, 67);
    
    if (global.mobile_f2 == 1)
        virtual_key_restart = virtual_key_add(f2x, f2y, 27 * button_scale, 29 * button_scale, 113);
    
    if (global.mobile_heal == 1)
        virtual_key_h = virtual_key_add(hx, hy, 27 * button_scale, 29 * button_scale, 72);
    
    virtual_key_up = virtual_key_add(upx, upy, 27 * analog_scale, 29 * analog_scale, 38);
    virtual_key_down = virtual_key_add(downx, downy, 27 * analog_scale, 29 * analog_scale, 40);
    virtual_key_left = virtual_key_add(leftx, lefty, 27 * analog_scale, 29 * analog_scale, 37);
    virtual_key_right = virtual_key_add(rightx, righty, 27 * analog_scale, 29 * analog_scale, 39);

    if (global.dual_controls == 1)
    {
        virtual_key_up2 = virtual_key_add(up2x, up2y, 27 * analog_scale, 29 * analog_scale, 87); // W
        virtual_key_down2 = virtual_key_add(down2x, down2y, 27 * analog_scale, 29 * analog_scale, 83); // S
        virtual_key_left2 = virtual_key_add(left2x, left2y, 27 * analog_scale, 29 * analog_scale, 65); // A
        virtual_key_right2 = virtual_key_add(right2x, right2y, 27 * analog_scale, 29 * analog_scale, 68); // D
    }
}
