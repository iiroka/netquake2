using Silk.NET.Input;
using System.Collections.Concurrent;

namespace Quake2 {

    class QInput {

        private QClient client;
        private QCommon common;

        public QInput(QClient client, QCommon common)
        {
            this.client = client;
            this.common = common;
        }

        // Console Variables
        private cvar_t? freelook;
        private cvar_t? lookstrafe;
        private cvar_t? m_forward;
        private cvar_t? m_pitch;
        private cvar_t? m_side;
        private cvar_t? m_up;
        private cvar_t? m_yaw;
        private cvar_t? sensitivity;

        private cvar_t? exponential_speedup;
        private cvar_t? in_grab;
        private cvar_t? m_filter;
        private cvar_t? windowed_mouse;

        private record struct KeyEvent {
            public Key key { get; init; }
            public bool down { get; init; }
            public int scancode { get; init; }
        };

        public int sys_frame_time;

        private ConcurrentQueue<KeyEvent> keyQueue = new ConcurrentQueue<KeyEvent>();


        /*
        * Initializes the backend
        */
        public void Init(IInputContext context)
        {
            common.Com_Printf("------- input initialization -------\n");

            exponential_speedup = common.Cvar_Get("exponential_speedup", "0", cvar_t.CVAR_ARCHIVE);
            freelook = common.Cvar_Get("freelook", "1", cvar_t.CVAR_ARCHIVE);
            in_grab = common.Cvar_Get("in_grab", "2", cvar_t.CVAR_ARCHIVE);
            lookstrafe = common.Cvar_Get("lookstrafe", "0", cvar_t.CVAR_ARCHIVE);
            m_filter = common.Cvar_Get("m_filter", "0", cvar_t.CVAR_ARCHIVE);
            m_up = common.Cvar_Get("m_up", "1", cvar_t.CVAR_ARCHIVE);
            m_forward = common.Cvar_Get("m_forward", "1", cvar_t.CVAR_ARCHIVE);
            m_pitch = common.Cvar_Get("m_pitch", "0.022", cvar_t.CVAR_ARCHIVE);
            m_side = common.Cvar_Get("m_side", "0.8", cvar_t.CVAR_ARCHIVE);
            m_yaw = common.Cvar_Get("m_yaw", "0.022", cvar_t.CVAR_ARCHIVE);
            sensitivity = common.Cvar_Get("sensitivity", "3", cvar_t.CVAR_ARCHIVE);

            // joy_haptic_magnitude = common.Cvar_Get("joy_haptic_magnitude", "0.0", cvar_t.CVAR_ARCHIVE);

            // joy_yawsensitivity = common.Cvar_Get("joy_yawsensitivity", "1.0", cvar_t.CVAR_ARCHIVE);
            // joy_pitchsensitivity = common.Cvar_Get("joy_pitchsensitivity", "1.0", cvar_t.CVAR_ARCHIVE);
            // joy_forwardsensitivity = common.Cvar_Get("joy_forwardsensitivity", "1.0", cvar_t.CVAR_ARCHIVE);
            // joy_sidesensitivity = common.Cvar_Get("joy_sidesensitivity", "1.0", cvar_t.CVAR_ARCHIVE);
            // joy_upsensitivity = common.Cvar_Get("joy_upsensitivity", "1.0", cvar_t.CVAR_ARCHIVE);
            // joy_expo = common.Cvar_Get("joy_expo", "2.0", cvar_t.CVAR_ARCHIVE);

            // joy_axis_leftx = common.Cvar_Get("joy_axis_leftx", "sidemove", cvar_t.CVAR_ARCHIVE);
            // joy_axis_lefty = common.Cvar_Get("joy_axis_lefty", "forwardmove", cvar_t.CVAR_ARCHIVE);
            // joy_axis_rightx = common.Cvar_Get("joy_axis_rightx", "yaw", cvar_t.CVAR_ARCHIVE);
            // joy_axis_righty = common.Cvar_Get("joy_axis_righty", "pitch", cvar_t.CVAR_ARCHIVE);
            // joy_axis_triggerleft = common.Cvar_Get("joy_axis_triggerleft", "triggerleft", cvar_t.CVAR_ARCHIVE);
            // joy_axis_triggerright = common.Cvar_Get("joy_axis_triggerright", "triggerright", cvar_t.CVAR_ARCHIVE);

            // joy_axis_leftx_threshold = common.Cvar_Get("joy_axis_leftx_threshold", "0.15", cvar_t.CVAR_ARCHIVE);
            // joy_axis_lefty_threshold = common.Cvar_Get("joy_axis_lefty_threshold", "0.15", cvar_t.CVAR_ARCHIVE);
            // joy_axis_rightx_threshold = common.Cvar_Get("joy_axis_rightx_threshold", "0.15", cvar_t.CVAR_ARCHIVE);
            // joy_axis_righty_threshold = common.Cvar_Get("joy_axis_righty_threshold", "0.15", cvar_t.CVAR_ARCHIVE);
            // joy_axis_triggerleft_threshold = common.Cvar_Get("joy_axis_triggerleft_threshold", "0.15", cvar_t.CVAR_ARCHIVE);
            // joy_axis_triggerright_threshold = common.Cvar_Get("joy_axis_triggerright_threshold", "0.15", cvar_t.CVAR_ARCHIVE);

            // gyro_calibration_x = common.Cvar_Get("gyro_calibration_x", "0.0", cvar_t.CVAR_ARCHIVE);
            // gyro_calibration_y = common.Cvar_Get("gyro_calibration_y", "0.0", cvar_t.CVAR_ARCHIVE);
            // gyro_calibration_z = common.Cvar_Get("gyro_calibration_z", "0.0", cvar_t.CVAR_ARCHIVE);

            // gyro_yawsensitivity = common.Cvar_Get("gyro_yawsensitivity", "1.0", cvar_t.CVAR_ARCHIVE);
            // gyro_pitchsensitivity = common.Cvar_Get("gyro_pitchsensitivity", "1.0", cvar_t.CVAR_ARCHIVE);
            // gyro_turning_axis = common.Cvar_Get("gyro_turning_axis", "0", cvar_t.CVAR_ARCHIVE);

            // gyro_mode = common.Cvar_Get("gyro_mode", "2", cvar_t.CVAR_ARCHIVE);
            // if ((int)gyro_mode->value == 2)
            // {
            //     gyro_active = true;
            // }

	        windowed_mouse = common.Cvar_Get("windowed_mouse", "1", cvar_t.CVAR_USERINFO | cvar_t.CVAR_ARCHIVE);

            common.Com_Printf($"{context.Keyboards.Count} keyboards were found.\n");
            for (int i = 0; i < context.Keyboards.Count; i++)
            {
                context.Keyboards[i].KeyDown += KeyDown;
                context.Keyboards[i].KeyUp += KeyUp;
            }

            int miceCount = 0;
            for (int i = 0; i < context.Mice.Count; i++)
            {
                if (context.Mice[i].IsConnected) 
                {
                    miceCount++;
                }
            }
            common.Com_Printf($"{miceCount} mice were found.\n");

            int joystickCount = 0;
            for (int i = 0; i < context.Joysticks.Count; i++)
            {
                if (context.Joysticks[i].IsConnected) 
                {
                    joystickCount++;
                }
            }
            common.Com_Printf($"{joystickCount} joysticks were found.\n");

	        common.Com_Printf("------------------------------------\n\n");
        }

        private int TranslateToQ2Key(Key key)
        {
            switch (key)
            {
                case Key.Enter:
                    return (int)QClient.QKEYS.K_ENTER;
                case Key.Escape:
                    return (int)QClient.QKEYS.K_ESCAPE;
                case Key.Space:
                    return (int)QClient.QKEYS.K_SPACE;
                case Key.Up:
                    return (int)QClient.QKEYS.K_UPARROW;
                case Key.Down:
                    return (int)QClient.QKEYS.K_DOWNARROW;
                case Key.Left:
                    return (int)QClient.QKEYS.K_LEFTARROW;
                case Key.Right:
                    return (int)QClient.QKEYS.K_RIGHTARROW;
                case Key.ControlLeft:
                    return (int)QClient.QKEYS.K_CTRL;
            }
            return 0;
        }

        /* ------------------------------------------------------------------ */

        /*
        * Updates the input queue state. Called every
        * frame by the client and does nearly all the
        * input magic.
        */
        public void Update()
        {
            while (keyQueue.TryDequeue(out KeyEvent kevent))
            {
				if (kevent.key >= Key.Number0 && kevent.key <= Key.Number9)
				{
					client.Key_Event('0' + (kevent.key - Key.Number0), kevent.down, false);
				}
				else if (kevent.key >= Key.F1 && kevent.key <= Key.F15)
				{
					client.Key_Event((int)QClient.QKEYS.K_F1 + (kevent.key - Key.F1), kevent.down, false);
				}
				else if (kevent.key >= Key.A && kevent.key <= Key.Z)
				{
					client.Key_Event((int)QClient.QKEYS.K_SC_A + (kevent.key - Key.A), kevent.down, false);
                }
                else
                {
                    int key = TranslateToQ2Key(kevent.key);
                    if (key != 0)
                    {
                        client.Key_Event(key, kevent.down, false);
                    } else {
                        Console.WriteLine($"Key {kevent.key} down:{kevent.down} sc:{kevent.scancode}");
                    }
                }

            }

            // We need to save the frame time so other subsystems
            // know the exact time of the last input events.
            sys_frame_time = common.Sys_Milliseconds();
        }


         private void KeyDown(IKeyboard keyboard, Key key, int scancode)
         {
            keyQueue.Enqueue(new KeyEvent() {
                key = key, down = true, scancode = scancode
            });
         }

         private void KeyUp(IKeyboard keyboard, Key key, int scancode)
         {
            keyQueue.Enqueue(new KeyEvent() {
                key = key, down = false, scancode = scancode
            });
         }

    }

}
