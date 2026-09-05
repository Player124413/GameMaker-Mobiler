hello = "你好啊，喜欢的话就用吧";
settx = -50;
setty = 5;
keyboardx = 652;
keyboardy = 5;
settings_font = global.joystick_font;
settings_num_x = 503;
settings_num_y = 6.5;
button_colour = 16777215;
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
analog_edit_selected = 0;
analog_center_x = analog_posx + (((59 * analog_scale) / 2) - ((41 * analog_scale) / 2));
analog_center_y = analog_posy + (((59 * analog_scale) / 2) - ((41 * analog_scale) / 2));
analog2_center_x = (analog2_posx + ((59 * analog_scale) / 2)) - ((41 * analog_scale) / 2);
analog2_center_y = (analog2_posy + ((59 * analog_scale) / 2)) - ((41 * analog_scale) / 2);
arrowkeys_area_size = 19.675;
arrowkeys_back_area_size = 45;
joystick_type = 0;
show = 1;
edit = 0;
black_fade = 0;
text_black_fade = 0;
controls_opacity = 0.5;
image_alpha = 1;
active_key = -1;

if (global.mobile_prioritize_display == 1)
    depth = -16000;

if (global.mobile_prioritize_display == 2)
    depth = -infinity;

if (file_exists("touchconfig.ini"))
{
    ini_open("touchconfig.ini");
    zx = ini_read_real("CONFIG", "zx", zx);
    zy = ini_read_real("CONFIG", "zy", zy);
    xx = ini_read_real("CONFIG", "xx", xx);
    xy = ini_read_real("CONFIG", "xy", xy);
    cx = ini_read_real("CONFIG", "cx", cx);
    cy = ini_read_real("CONFIG", "cy", cy);
    f2x = ini_read_real("CONFIG", "f2x", f2x);
    f2y = ini_read_real("CONFIG", "f2y", f2y);
    hx = ini_read_real("CONFIG", "hx", hx);
    hy = ini_read_real("CONFIG", "hy", hy);
    analog_posx = ini_read_real("CONFIG", "analog_posx", analog_posx);
    analog_posy = ini_read_real("CONFIG", "analog_posy", analog_posy);
    analog2_posx = ini_read_real("CONFIG", "analog2_posx", analog2_posx);
    analog2_posy = ini_read_real("CONFIG", "analog2_posy", analog2_posy);
    button_scale = ini_read_real("CONFIG", "button_scale", button_scale);
    analog_scale = ini_read_real("CONFIG", "analog_scale", analog_scale);
    joystick_type = ini_read_real("CONFIG", "joystick_type", joystick_type);
    controls_opacity = ini_read_real("CONFIG", "controls_opacity", controls_opacity);
    ini_close();
}

if (file_exists("touchconfig2.ini"))
{
    ini_open("touchconfig2.ini");
    zx2 = ini_read_real("CONFIG", "zx", zx2);
    zy2 = ini_read_real("CONFIG", "zy", zy2);
    xx2 = ini_read_real("CONFIG", "xx", xx2);
    xy2 = ini_read_real("CONFIG", "xy", xy2);
    cx2 = ini_read_real("CONFIG", "cx", cx2);
    cy2 = ini_read_real("CONFIG", "cy", cy2);
    ini_close();
}
