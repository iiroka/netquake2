namespace Quake2 {

    partial class QClient {

        private interface IQMenuLayer {
            void draw();
            string? key(int key);
        }

        private List<IQMenuLayer> m_layers = new List<IQMenuLayer>();

        /* Number of the frames of the spinning quake logo */
        private const int NUM_CURSOR_FRAMES = 15;
        private int m_cursor_width = 0;

        /*
        * This crappy function maintaines a stack of opened menus.
        * The steps in this horrible mess are:
        *
        * 1. But the game into pause if a menu is opened
        *
        * 2. If the requested menu is already open, close it.
        *
        * 3. If the requested menu is already open but not
        *    on top, close all menus above it and the menu
        *    itself. This is necessary since an instance of
        *    the reqeuested menu is in flight and will be
        *    displayed.
        *
        * 4. Save the previous menu on top (which was in flight)
        *    to the stack and make the requested menu the menu in
        *    flight.
        */
        private void M_PushMenu(IQMenuLayer layer)
        {
        //     int i;
        //     int alreadyPresent = 0;

            if ((common.Cvar_VariableInt("maxclients") == 1) &&
                    common.ServerState != 0)
            {
                common.Cvar_Set("paused", "1");
            }

        // #ifdef USE_OPENAL
        //     if (cl.cinematic_file && sound_started == SS_OAL)
        //     {
        //         AL_UnqueueRawSamples();
        //     }
        // #endif

        //     /* if this menu is already open (and on top),
        //     close it => toggling behaviour */
        //     if ((m_drawfunc == draw) && (m_keyfunc == key))
        //     {
        //         M_PopMenu();
        //         return;
        //     }

        //     /* if this menu is already present, drop back to
        //     that level to avoid stacking menus by hotkeys */
        //     for (i = 0; i < m_menudepth; i++)
        //     {
        //         if ((m_layers[i].draw == draw) &&
        //                 (m_layers[i].key == key))
        //         {
        //             alreadyPresent = 1;
        //             break;
        //         }
        //     }

        //     /* menu was already opened further down the stack */
        //     while (alreadyPresent && i <= m_menudepth)
        //     {
        //         M_PopMenu(); /* decrements m_menudepth */
        //     }

        //     if (m_menudepth >= MAX_MENU_DEPTH)
        //     {
        //         Com_Printf("Too many open menus!\n");
        //         return;
        //     }

            m_layers.Add(layer);

        //     m_entersound = true;

            cls.key_dest = keydest_t.key_menu;
        }

        private void M_ForceMenuOff()
        {
            m_layers.Clear();
            cls.key_dest = keydest_t.key_game;
            // Key_MarkAllUp();
            common.Cvar_Set("paused", "0");
        }


        /*
        * Draws an animating cursor with the point at
        * x,y. The pic will extend to the left of x,
        * and both above and below y.
        */
        protected void DrawCursor(int x, int y, int f)
        {
            // char cursorname[80];
            // static qboolean cached;
            // float scale = SCR_GetMenuScale();
            float scale = 1.0f;

            // if (!cached)
            // {
            //     int i;

            //     for (i = 0; i < NUM_CURSOR_FRAMES; i++)
            //     {
            //         Com_sprintf(cursorname, sizeof(cursorname), "m_cursor%d", i);

            //         Draw_FindPic(cursorname);
            //     }

            //     cached = true;
            // }

            var cursorname = $"m_cursor{f}";
            vid.Draw_PicScaled((int)(x * scale), (int)(y * scale), cursorname, scale);
        }


        private class QMainMenu : menuframework_s, IQMenuLayer
        {
            private menubitmap_s s_plaque;
            private menubitmap_s s_logo;
            private menubitmap_s s_game;
            private menubitmap_s s_multiplayer;
            private menubitmap_s s_options;

            private static string[] names = {
                "m_main_game",
                "m_main_multiplayer",
                "m_main_options",
                "m_main_video",
                "m_main_quit"
            };

            public QMainMenu(QClient client) : base(client)
            {
                float scale = 1.0f;
                int widest = -1;
                int w, h;
                for (int i = 0; i < names.Length; i++)
                {
                    client.vid.Draw_GetPicSize(out w, out h, names[i]);

                    if (w > widest)
                    {
                        widest = w;
                    }
                }

                int x = (int)((client.vid.viddef.width / scale - widest + 70) / 2);
                int y = (int)(client.vid.viddef.height / (2 * scale) - 110);

                client.vid.Draw_GetPicSize(out w, out h, "m_main_plaque");

                s_plaque = new menubitmap_s() 
                {
                    flags = MenuFlags.QMF_LEFT_JUSTIFY | MenuFlags.QMF_INACTIVE,
                    x = (x - (client.m_cursor_width + 5) - w),
                    y = y,
                    name = "m_main_plaque"
                };
                s_logo = new menubitmap_s() 
                {
                    flags = MenuFlags.QMF_LEFT_JUSTIFY | MenuFlags.QMF_INACTIVE,
                    x = (x - (client.m_cursor_width + 5) - w),
                    y = y + h + 5,
                    name = "m_main_logo"
                };

                y += 10;

                s_game = new menubitmap_s() 
                {
                    flags = MenuFlags.QMF_LEFT_JUSTIFY | MenuFlags.QMF_HIGHLIGHT_IF_FOCUS,
                    x = x,
                    y = y,
                    name = "m_main_game",
                    callback = GameFunc,
                    focuspic = "m_main_game_sel"
                };

                client.vid.Draw_GetPicSize(out w, out h, s_game.name);
                y += h + 8;

                s_multiplayer = new menubitmap_s() 
                {
                    flags = MenuFlags.QMF_LEFT_JUSTIFY | MenuFlags.QMF_HIGHLIGHT_IF_FOCUS,
                    x = x,
                    y = y,
                    name = "m_main_multiplayer",
                    // callback = MultiplayerFunc,
                    focuspic = "m_main_multiplayer_sel"
                };

                client.vid.Draw_GetPicSize(out w, out h, s_multiplayer.name);
                y += h + 8;

                s_options = new menubitmap_s() 
                {
                    flags = MenuFlags.QMF_LEFT_JUSTIFY | MenuFlags.QMF_HIGHLIGHT_IF_FOCUS,
                    x = x,
                    y = y,
                    name = "m_main_options",
                    // callback = OptionsFunc,
                    focuspic = "m_main_options_sel"
                };

                client.vid.Draw_GetPicSize(out w, out h, s_options.name);
                y += h + 8;

                AddItem(s_plaque);
                AddItem(s_logo);
                AddItem(s_game);
                AddItem(s_multiplayer);
                AddItem(s_options);

                Center();

                // force first available item to have focus 
                while (cursor >= 0 && cursor < items.Count)
                {
                    if ((items[cursor].flags & MenuFlags.QMF_INACTIVE) != 0)
                    {
                        cursor++;
                    }
                    else
                    {
                        break;
                    }
                }

            }

            public void draw()
            {
                int x = 0;
                int y = 0;

                var item = items[cursor];
                if (item != null)
                {
                    x = item.x;
                    y = item.y;
                }

                Draw();
                client.DrawCursor(x - client.m_cursor_width, y, ( int )(client.cls.realtime / 100) % NUM_CURSOR_FRAMES);
            }

            private void GameFunc(menucommon_s _unused)
            {
                client.M_Menu_Game();
            }


        }


        private void M_Menu_Main()
        {
            // menucommon_s * item = 0;

            // InitMainMenu();

            // // force first available item to have focus 
            // while (s_main.cursor >= 0 && s_main.cursor < s_main.nitems)
            // {
            //     item = ( menucommon_s * )s_main.items[s_main.cursor];
                
            //     if ((item->flags & (QMF_INACTIVE)))
            //     {
            //         s_main.cursor++;
            //     }
            //     else
            //     {
            //         break;
            //     }
            // }

            M_PushMenu(new QMainMenu(this));
        }

        private class QGameMenu : menuframework_s, IQMenuLayer
        {
            private menuaction_s s_easy_game_action;
            private menuaction_s s_medium_game_action;
            private menuaction_s s_hard_game_action;

            public QGameMenu(QClient client) : base(client)
            {
                this.x = (int)(client.vid.viddef.width * 0.50f);

                s_easy_game_action = new menuaction_s(){
                    flags = MenuFlags.QMF_LEFT_JUSTIFY,
                    x = 0,
                    y = 0,
                    name = "easy",
                    callback = EasyGameFunc
                };

                s_medium_game_action = new menuaction_s(){
                    flags = MenuFlags.QMF_LEFT_JUSTIFY,
                    x = 0,
                    y = 10,
                    name = "medium"
                    // callback = EasyGameFunc;
                };

                s_hard_game_action = new menuaction_s(){
                    flags = MenuFlags.QMF_LEFT_JUSTIFY,
                    x = 0,
                    y = 20,
                    name = "hard"
                    // callback = EasyGameFunc;
                };

                AddItem(s_easy_game_action);
                AddItem(s_medium_game_action);
                AddItem(s_hard_game_action);

                Center();

            }

            public void draw()
            {
                AdjustCursor(1);
                Draw();
            }

            private void StartGame()
            {
                if (client.cls.state != connstate_t.ca_disconnected && client.cls.state != connstate_t.ca_uninitialized)
                {
                    client.CL_Disconnect();
                }

                /* disable updates and start the cinematic going */
                client.cl.servercount = -1;
                client.M_ForceMenuOff();
                client.common.Cvar_Set("deathmatch", "0");
                client.common.Cvar_Set("coop", "0");

                client.common.Cbuf_AddText("loading ; killserver ; wait ; newgame\n");
                client.cls.key_dest = keydest_t.key_game;
            }

            private void EasyGameFunc(menucommon_s _data)
            {
                client.common.Cvar_ForceSet("skill", "0");
                StartGame();
            }

        }

        private void M_Menu_Game()
        {
            // Game_MenuInit();
            M_PushMenu(new QGameMenu(this));
            // m_game_cursor = 1;
        }


        private void M_Init()
        {
            // Cmd_AddCommand("menu_main", M_Menu_Main_f);
            // Cmd_AddCommand("menu_game", M_Menu_Game_f);
            // Cmd_AddCommand("menu_loadgame", M_Menu_LoadGame_f);
            // Cmd_AddCommand("menu_savegame", M_Menu_SaveGame_f);
            // Cmd_AddCommand("menu_joinserver", M_Menu_JoinServer_f);
            // Cmd_AddCommand("menu_addressbook", M_Menu_AddressBook_f);
            // Cmd_AddCommand("menu_startserver", M_Menu_StartServer_f);
            // Cmd_AddCommand("menu_dmoptions", M_Menu_DMOptions_f);
            // Cmd_AddCommand("menu_playerconfig", M_Menu_PlayerConfig_f);
            // Cmd_AddCommand("menu_downloadoptions", M_Menu_DownloadOptions_f);
            // Cmd_AddCommand("menu_credits", M_Menu_Credits_f);
            // Cmd_AddCommand("menu_mods", M_Menu_Mods_f);
            // Cmd_AddCommand("menu_multiplayer", M_Menu_Multiplayer_f);
            // Cmd_AddCommand("menu_multiplayer_keys", M_Menu_Multiplayer_Keys_f);
            // Cmd_AddCommand("menu_video", M_Menu_Video_f);
            // Cmd_AddCommand("menu_options", M_Menu_Options_f);
            // Cmd_AddCommand("menu_keys", M_Menu_Keys_f);
            // Cmd_AddCommand("menu_joy", M_Menu_Joy_f);
            // Cmd_AddCommand("menu_gyro", M_Menu_Gyro_f);
            // Cmd_AddCommand("menu_buttons", M_Menu_ControllerButtons_f);
            // Cmd_AddCommand("menu_altbuttons", M_Menu_ControllerAltButtons_f);
            // Cmd_AddCommand("menu_quit", M_Menu_Quit_f);

            // /* initialize the server address book cvars (adr0, adr1, ...)
            // * so the entries are not lost if you don't open the address book */
            // for (int index = 0; index < NUM_ADDRESSBOOK_ENTRIES; index++)
            // {
            //     char buffer[20];
            //     Com_sprintf(buffer, sizeof(buffer), "adr%d", index);
            //     Cvar_Get(buffer, "", CVAR_ARCHIVE);
            // }

            // cache the cursor frames
            for (int i = 0; i < NUM_CURSOR_FRAMES; i++)
            {
                var cursorname = $"m_cursor{i}";
                // vid.Draw_FindPic(cursorname);
                vid.Draw_GetPicSize(out var w, out var h, cursorname);

                if (w > m_cursor_width)
                {
                    m_cursor_width = w;
                }
            }
        }

        private void M_Draw()
        {
            if (cls.key_dest != keydest_t.key_menu)
            {
                return;
            }

            /* repaint everything next frame */
            SCR_DirtyScreen();

            /* dim everything behind it down */
            // if (cl.cinematictime > 0)
            // {
            //     Draw_Fill(0, 0, viddef.width, viddef.height, 0);
            // }

            // else
            // {
            //     Draw_FadeScreen();
            // }

            if (m_layers.Count > 0)
            {
                var layer = m_layers.Last();
                layer.draw();

                /* delay playing the enter sound until after the
                menu has been drawn, to avoid delay while
                caching images */
                // if (m_entersound)
                // {
                //     S_StartLocalSound(menu_in_sound);
                //     m_entersound = false;
                // }
            }
        }

        private void M_Keydown(int key)
        {
            if (m_layers.Count > 0)
            {
                var s = m_layers.Last().key(key);
                if (s != null)
                {
                    // S_StartLocalSound((char *)s);
                }
            }
        }


    }
}