var _gui_width = display_get_gui_width();
var _gui_height = display_get_gui_height();
var _gui_right = _gui_width - 640;
var _gui_bottom = _gui_height - 480;

cu = 1;
cd = 1;
cl = 1;
cr = 1;
cz = 1;
cx = 1;
cg = 1;
_m = 0;
mubai = 1;

for (i = 0; i < 4; i++)
{
    _ak = 0;
    _ak2 = 0;
    var _touch_x = device_mouse_x_to_gui(i);
    var _touch_y = device_mouse_y_to_gui(i);
    var _layout_x = (_touch_x >= _gui_width * 0.5) ? _touch_x - _gui_right : _touch_x;
    var _layout_y = (_touch_y >= _gui_height * 0.5) ? _touch_y - _gui_bottom : _touch_y;
    
    if (device_mouse_check_button(i, mb_left))
    {
        if (_layout_x >= 560 && _layout_x <= 640 && _layout_y >= 51)
        {
            cz = 0.5;
            _ak = mk_z_button;
        }
        else if (_layout_x > 640)
        {
            cz = 0.5;
            _ak = mk_z_button;
        }
        else if (_layout_x >= 480 && _layout_x < 560)
        {
            cx = 0.5;
            _ak = mk_x_button;
        }
        else if (_layout_x >= 400 && _layout_x < 480)
        {
            cg = 0.5;
            _ak = mk_c_button;
        }
        else if (_layout_x >= 560 && _layout_x <= 640 && _layout_y <= 50)
        {
            mubai = 0.5;
            _ak = mk_up_right_button;
        }
        else if (!_m)
        {
            if (_layout_x < 400)
            {
                _m = 1;
                _dx = _layout_x - 140;
                _dy = _layout_y - 360;
                _da = point_direction(0, 0, _dx, _dy);
                
                if (_da >= 292.5 || _da <= 67.5)
                {
                    cr = 0.5;
                    
                    if (_ak == 0)
                        _ak = mk_right;
                    else
                        _ak2 = mk_right;
                }
                
                if (_da >= 22.5 && _da <= 157.5)
                {
                    cu = 0.5;
                    
                    if (_ak == 0)
                        _ak = mk_up;
                    else
                        _ak2 = mk_up;
                }
                
                if (_da >= 112.5 && _da <= 247.5)
                {
                    cl = 0.5;
                    
                    if (_ak == 0)
                        _ak = mk_left;
                    else
                        _ak2 = mk_left;
                }
                
                if (_da >= 202.5 && _da <= 337.5)
                {
                    cd = 0.5;
                    
                    if (_ak == 0)
                        _ak = mk_down;
                    else
                        _ak2 = mk_down;
                }
            }
        }
    }
    
    if (device_mouse_check_button_pressed(i, mb_left))
    {
        if (_ak != 0)
        {
            if (keyboard_check(_ak))
            {
                keyboard_key_release(_ak);
                keyboard_key_press(_ak);
            }
        }
        
        if (_ak2 != 0)
        {
            if (keyboard_check(_ak2))
            {
                keyboard_key_release(_ak2);
                keyboard_key_press(_ak2);
            }
        }
    }
}

if (mubai == 0.5 && !keyboard_check(mk_up_right_button))
    keyboard_key_press(mk_up_right_button);

if (cz == 0.5 && !keyboard_check(mk_z_button))
    keyboard_key_press(mk_z_button);

if (cx == 0.5 && !keyboard_check(mk_x_button))
    keyboard_key_press(mk_x_button);

if (cg == 0.5 && !keyboard_check(mk_c_button))
    keyboard_key_press(mk_c_button);

if (cr == 0.5 && !keyboard_check(mk_right))
    keyboard_key_press(mk_right);

if (cu == 0.5 && !keyboard_check(mk_up))
    keyboard_key_press(mk_up);

if (cl == 0.5 && !keyboard_check(mk_left))
    keyboard_key_press(mk_left);

if (cd == 0.5 && !keyboard_check(mk_down))
    keyboard_key_press(mk_down);

if (mubai == 1 && keyboard_check(mk_up_right_button))
    keyboard_key_release(mk_up_right_button);

if (cz == 1 && keyboard_check(mk_z_button))
    keyboard_key_release(mk_z_button);

if (cx == 1 && keyboard_check(mk_x_button))
    keyboard_key_release(mk_x_button);

if (cg == 1 && keyboard_check(mk_c_button))
    keyboard_key_release(mk_c_button);

if (cr == 1 && keyboard_check(mk_right))
    keyboard_key_release(mk_right);

if (cu == 1 && keyboard_check(mk_up))
    keyboard_key_release(mk_up);

if (cl == 1 && keyboard_check(mk_left))
    keyboard_key_release(mk_left);

if (cd == 1 && keyboard_check(mk_down))
    keyboard_key_release(mk_down);

