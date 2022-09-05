/*
 * Copyright (C) 1997-2001 Id Software, Inc.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or (at
 * your option) any later version.
 *
 * This program is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 *
 * See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA
 * 02111-1307, USA.
 *
 * =======================================================================
 *
 * API between the client and renderers.
 *
 * =======================================================================
 */
using Silk.NET.Input;
using Silk.NET.Maths; 
using Silk.NET.Windowing;
using System.Numerics;

namespace Quake2 {

    internal struct viddef_t {
        public int height;
        public int	width;
    }


    internal class QVid : refimport_t {

        private QCommon common;
        private QInput input;

        public QVid(QCommon common, QInput input)
        {
            this.common = common;
            this.input = input;
        }

        // Renderer restart type.
        public enum ref_restart_t {
            RESTART_UNDEF,
            RESTART_NO,
            RESTART_FULL,
            RESTART_PARTIAL
        }


        // --------

        // Renderer load, restart and shutdown
        // -----------------------------------

        // Global console variables.
        private cvar_t? vid_gamma;
        private cvar_t? vid_fullscreen;
        private cvar_t? vid_renderer;

        public viddef_t viddef;

        private IWindow? window;

        /*
        * Initializes the video stuff.
        */
        public void Init()
        {
            // Console variables
            vid_gamma = common.Cvar_Get("vid_gamma", "1.0", cvar_t.CVAR_ARCHIVE);
            vid_fullscreen = common.Cvar_Get("vid_fullscreen", "0", cvar_t.CVAR_ARCHIVE);
            vid_renderer = common.Cvar_Get("vid_renderer", "gl3", cvar_t.CVAR_ARCHIVE);

            // Commands
            // Cmd_AddCommand("vid_restart", VID_Restart_f);
            // Cmd_AddCommand("vid_listmodes", VID_ListModes_f);
            // Cmd_AddCommand("r_listmodes", VID_ListModes_f); // more consistent with r_mode

            // Load the renderer and get things going.
            VID_CheckChanges();
        }

        // Renderer restart type requested.
        private ref_restart_t restart_state = ref_restart_t.RESTART_UNDEF;

        private refexport_t? re;

        /*
        * Checks if a renderer changes was requested and executes it.
        * Inclusive fallback through all renderers. :)
        */
        public void VID_CheckChanges()
        {
            // Hack around renderers that still abuse vid_fullscreen
            // to communicate restart requests to the client.
            ref_restart_t rs = restart_state;

            if (restart_state == ref_restart_t.RESTART_UNDEF)
            {
                if (vid_fullscreen!.modified)
                {
                    rs = ref_restart_t.RESTART_FULL;
                    vid_fullscreen!.modified = false;
                } else {
                    rs = ref_restart_t.RESTART_NO;
                }
            } else {
                restart_state = ref_restart_t.RESTART_NO;
            }

            if (rs == ref_restart_t.RESTART_FULL)
            {
                // Stop sound, because the clients blocks while
                // we're reloading the renderer. The sound system
                // would screw up it's internal timings.
            //     S_StopAllSounds();

                // Reset the client side of the renderer state.
                // cl.refresh_prepped = false;
                // cl.cinematicpalette_active = false;

                // More or less blocks the client.
                // cls.disable_screen = 1.0f;

                // Mkay, let's try our luck.
                while (!VID_LoadRenderer())
                {
            //         // We try: custom -> gl3 -> gl1 -> soft.
            //         if ((strcmp(vid_renderer->string, "gl3") != 0) &&
            //             (strcmp(vid_renderer->string, "gl1") != 0) &&
            //             (strcmp(vid_renderer->string, "soft") != 0))
            //         {
            //             Com_Printf("Retrying with gl3...\n");
            //             Cvar_Set("vid_renderer", "gl3");
            //         }
            //         else if (strcmp(vid_renderer->string, "gl3") == 0)
            //         {
            //             Com_Printf("Retrying with gl1...\n");
            //             Cvar_Set("vid_renderer", "gl1");
            //         }
            //         else if (strcmp(vid_renderer->string, "gl1") == 0)
            //         {
            //             Com_Printf("Retrying with soft...\n");
            //             Cvar_Set("vid_renderer", "soft");
            //         }
            //         else if (strcmp(vid_renderer->string, "soft") == 0)
            //         {
            //             // Sorry, no usable renderer found.
                        common.Com_Error(QShared.ERR_FATAL, "No usable renderer found!\n");
            //         }
                }

            //     // Unblock the client.
            //     cls.disable_screen = false;
            }

            // if (rs == RESTART_PARTIAL)
            // {
            //     cl.refresh_prepped = false;
            // }
        }

        /*
        * Loads and initializes a renderer.
        */
        private bool VID_LoadRenderer()
        {
            // refimport_t	ri;
            // GetRefAPI_t	GetRefAPI;

            // char reflib_name[64] = {0};
            // char reflib_path[MAX_OSPATH] = {0};

            // // If the refresher is already active we need
            // // to shut it down before loading a new one
            // VID_ShutdownRenderer();

            // Log what we're doing.
            common.Com_Printf("----- refresher initialization -----\n");

            var ref_name = vid_renderer!.str;

            if (ref_name.Equals("gl3"))
            {
                re = new QRefGl3(this);
            }
            else
            {
                common.Com_Printf($"Refresher {ref_name} cannot be found!\n");
                return false;
            }

            // // Fill in the struct exported to the renderer.
            // // FIXME: Do we really need all these?
            // ri.Cmd_AddCommand = Cmd_AddCommand;
            // ri.Cmd_Argc = Cmd_Argc;
            // ri.Cmd_Argv = Cmd_Argv;
            // ri.Cmd_ExecuteText = Cbuf_ExecuteText;
            // ri.Cmd_RemoveCommand = Cmd_RemoveCommand;
            // ri.Com_VPrintf = Com_VPrintf;
            // ri.Cvar_Get = Cvar_Get;
            // ri.Cvar_Set = Cvar_Set;
            // ri.Cvar_SetValue = Cvar_SetValue;
            // ri.FS_FreeFile = FS_FreeFile;
            // ri.FS_Gamedir = FS_Gamedir;
            // ri.FS_LoadFile = FS_LoadFile;
            // ri.GLimp_InitGraphics = GLimp_InitGraphics;
            // ri.GLimp_GetDesktopMode = GLimp_GetDesktopMode;
            // ri.Sys_Error = Com_Error;
            // ri.Vid_GetModeInfo = VID_GetModeInfo;
            // ri.Vid_MenuInit = VID_MenuInit;
            // ri.Vid_WriteScreenshot = VID_WriteScreenshot;
            // ri.Vid_RequestRestart = VID_RequestRestart;

            // // Exchange our export struct with the renderers import struct.
            // re = GetRefAPI(ri);

            // // Declare the refresher as active.
            // ref_active = true;

            // // Let's check if we've got a compatible renderer.
            // if (re.api_version != API_VERSION)
            // {
            //     VID_ShutdownRenderer();

            //     Com_Printf("%s has incompatible api_version %d!\n", reflib_name, re.api_version);

            //     return false;
            // }

            // Everything seems okay, initialize it.
            if (!re.Init())
            {
                // VID_ShutdownRenderer();

                common.Com_Printf($"ERROR: Loading {ref_name} as rendering backend failed.\n");
                common.Com_Printf("------------------------------------\n\n");

                return false;
            }

            // /* Ensure that all key states are cleared */
            // Key_MarkAllUp();

            common.Com_Printf($"Successfully loaded {ref_name} as rendering backend.\n");
            common.Com_Printf("------------------------------------\n\n");

            return true;
        }

        // --------

        // Video mode array
        // ----------------

        private record struct vidmode_t
        {
            public string description { get; init; }
            public int width  { get; init; }
            public int height { get; init; }
            public int mode { get; init; }
        }

        // This must be the same as VID_MenuInit()->resolutions[] in videomenu.c!
        private readonly vidmode_t[] vid_modes = {
            new vidmode_t(){description = "Mode  0:  320x240", width = 320, height = 240, mode = 0},
            new vidmode_t(){description = "Mode  1:  400x300", width = 400, height = 300, mode = 1},
            new vidmode_t(){description = "Mode  2:  512x384", width = 512, height = 384, mode = 2},
            new vidmode_t(){description = "Mode  3:  640x400", width = 640, height = 400, mode = 3},
            new vidmode_t(){description = "Mode  4:  640x480", width = 640, height = 480, mode = 4},
            new vidmode_t(){description = "Mode  5:  800x500", width = 800, height = 500, mode = 5},
            new vidmode_t(){description = "Mode  6:  800x600", width = 800, height = 600, mode = 6},
            new vidmode_t(){description = "Mode  7:  960x720", width = 960, height = 720, mode = 7},
            new vidmode_t(){description = "Mode  8: 1024x480", width = 1024, height = 480, mode = 8},
            new vidmode_t(){description = "Mode  9: 1024x640", width = 1024, height = 640, mode = 9},
            new vidmode_t(){description = "Mode 10: 1024x768", width = 1024, height = 768, mode = 10},
            new vidmode_t(){description = "Mode 11: 1152x768", width = 1152, height = 768, mode = 11},
            new vidmode_t(){description = "Mode 12: 1152x864", width = 1152, height = 864, mode = 12},
            new vidmode_t(){description = "Mode 13: 1280x800", width = 1280, height = 800, mode = 13},
            new vidmode_t(){description = "Mode 14: 1280x720", width = 1280, height = 720, mode = 14},
            new vidmode_t(){description = "Mode 15: 1280x960", width = 1280, height = 960, mode = 15},
            new vidmode_t(){description = "Mode 16: 1280x1024", width = 1280, height = 1024, mode = 16},
            new vidmode_t(){description = "Mode 17: 1366x768", width = 1366, height = 768, mode = 17},
            new vidmode_t(){description = "Mode 18: 1440x900", width = 1440, height = 900, mode = 18},
            new vidmode_t(){description = "Mode 19: 1600x1200", width = 1600, height = 1200, mode = 19},
            new vidmode_t(){description = "Mode 20: 1680x1050", width = 1680, height = 1050, mode = 20},
            new vidmode_t(){description = "Mode 21: 1920x1080", width = 1920, height = 1080, mode = 21},
            new vidmode_t(){description = "Mode 22: 1920x1200", width = 1920, height = 1200, mode = 22},
            new vidmode_t(){description = "Mode 23: 2048x1536", width = 2048, height = 1536, mode = 23},
            new vidmode_t(){description = "Mode 24: 2560x1080", width = 2560, height = 1080, mode = 24},
            new vidmode_t(){description = "Mode 25: 2560x1440", width = 2560, height = 1440, mode = 25},
            new vidmode_t(){description = "Mode 26: 2560x1600", width = 2560, height = 1600, mode = 26},
            new vidmode_t(){description = "Mode 27: 3440x1440", width = 3440, height = 1440, mode = 27},
            new vidmode_t(){description = "Mode 28: 3840x1600", width = 3840, height = 1600, mode = 28},
            new vidmode_t(){description = "Mode 29: 3840x2160", width = 3840, height = 2160, mode = 29},
            new vidmode_t(){description = "Mode 30: 4096x2160", width = 4096, height = 2160, mode = 30},
            new vidmode_t(){description = "Mode 31: 5120x2880", width = 5120, height = 2880, mode = 31}
        };

        /*
        * Callback function for the 'vid_listmodes' cmd.
        */
        private void VID_ListModes_f(string[] args)
        {
            common.Com_Printf("Supported video modes (r_mode):\n");

            for (int i = 0; i < vid_modes.Length; ++i)
            {
                common.Com_Printf($"  {vid_modes[i].description}\n");
            }
            common.Com_Printf("  Mode -1: r_customwidth x r_customheight\n");
        }

        /*
        * Returns informations about the given mode.
        */
        public bool Vid_GetModeInfo(ref int width, ref int height, int mode)
        {
            if ((mode < 0) || (mode >= vid_modes.Length))
            {
                return false;
            }

            width = vid_modes[mode].width;
            height = vid_modes[mode].height;

            return true;
        }

        public bool Vid_SetModeInfo(int fullscreen, int width, int height)
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(width, height);
            options.PreferredBitDepth = new Vector4D<int>(8, 8, 8, 8);
            options.UpdatesPerSecond = 60;
            options.Title = "Quake2";

            window = Window.Create(options);

            window.Load += OnLoad;
            window.Render += OnRender;
            window.Closing += OnClose;

            /* We need the window size for the menu, the HUD, etc. */
            viddef.width = width;
            viddef.height = height;
            return true;
        }

        public void Start()
        {
            window?.Run();
        }

        private void OnLoad()
        {
            Console.WriteLine($"Window size {window!.Size[0]}x{window!.Size[1]}");

            input.Init(window!.CreateInput());

            //Set-up input context.
            if (!(re?.PostInit(window!) ?? false)) {
                window?.Close();
            }
        }

        private void OnRender(double sec)
        {
            //Here all rendering should be done.
            try {
                common.Qcommon_Frame(sec);
            } catch (QCommon.AbortFrame) {}
        }

        private void OnClose()
        {
            Console.WriteLine("Closing");
        }

        public void Com_VPrintf(int print_level, string msg)
        {
            common.Com_Printf(msg);
        }

        public cvar_t? Cvar_Get (string name, string? value, int flags)
        {
            return common.Cvar_Get(name, value, flags);
        }

        public void Sys_Error (int err_level, string msg)
        {
            common.Com_Error(err_level, msg);
        }

        public byte[]? FS_LoadFile(string name)
        {
            return common.FS_LoadFile(name);
        }

        // ----

        // Wrappers for the functions provided by the renderer libs.
        // =========================================================

        public void R_BeginRegistration(string map)
        {
            re?.BeginRegistration(window!, map);
        }

        public model_s? R_RegisterModel(string name)
        {
            return re?.RegisterModel(window!, name);
        }

        public image_s? R_RegisterSkin(string name)
        {
            return re?.RegisterSkin(window!, name);
        }

        public void R_SetSky(string name, float rotate, in Vector3 axis)
        {
            re?.SetSky(window!, name, rotate, axis);
        }

        public void R_BeginFrame(float camera_separation)
        {
            re?.BeginFrame(window!, camera_separation);
        }

        public void R_RenderFrame(in refdef_t fd)
        {
            re?.RenderFrame(window!, fd);
        }


        public void Draw_StretchPic(int x, int y, int w, int h, string name)
        {
            re?.DrawStretchPic(window!, x, y, w, h, name);
        }

        public void Draw_PicScaled(int x, int y, string pic, float factor)
        {
            re?.DrawPicScaled(window!, x, y, pic, factor);
        }

        public void Draw_GetPicSize(out int w, out int h, string name)
        {
            if (re != null) {
                re.DrawGetPicSize(window!, out w, out h, name);
            } else {
                w = h = -1;
            }
        }

        public void Draw_CharScaled(int x, int y, int num, float scale)
        {
            re?.DrawCharScaled(window!, x, y, num, scale);
        }

        public void R_EndFrame()
        {
            re?.EndFrame(window!);
        }


    }
}