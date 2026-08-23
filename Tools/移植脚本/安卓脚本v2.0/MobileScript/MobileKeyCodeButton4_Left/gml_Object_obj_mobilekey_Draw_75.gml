var _gui_right = display_get_gui_width() - 640;
var _gui_bottom = display_get_gui_height() - 480;

draw_set_alpha(mk_button_alpha);
draw_roundrect_color(400 + _gui_right, 460 + _gui_bottom, 480 + _gui_right, 480 + _gui_bottom, mk_c_color, mk_c_color, 0);
draw_roundrect_color(480 + _gui_right, 420 + _gui_bottom, 560 + _gui_right, 460 + _gui_bottom, mk_x_color, mk_x_color, 0);
draw_roundrect_color(560 + _gui_right, 420 + _gui_bottom, 640 + _gui_right, 460 + _gui_bottom, mk_z_color, mk_z_color, 0);
draw_roundrect_color(80, 0, 0, 30, mk_up_left_color, mk_up_left_color, 0);
draw_set_alpha(1);
draw_sprite_ext(spr_mobilekey, 0, 76, 296 + _gui_bottom, 2, 2, 0, c_white, 0.41);
