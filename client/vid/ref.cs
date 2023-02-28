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
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307,
 * USA.
 *
 * =======================================================================
 *
 * ABI between client and refresher
 *
 * =======================================================================
 */
using System.Numerics;

namespace Quake2 {


    internal class QRef {
        public const int MAX_DLIGHTS        = 32;
        public const int MAX_ENTITIES       = 128;
        public const int MAX_PARTICLES      = 4096;
        public const int MAX_LIGHTSTYLES    = 256;

    }

    internal interface model_s {}
    internal interface image_s {}

    internal struct entity_t {
        public model_s?		model; /* opaque type outside refresh */
        public Vector3		angles;

        /* most recent data */
        public Vector3		origin; /* also used as RF_BEAM's "from" */
        public int			frame; /* also used as RF_BEAM's diameter */

        /* previous data for lerping */
        public Vector3		oldorigin; /* also used as RF_BEAM's "to" */
        public int			oldframe;

        /* misc */
        public float	backlerp; /* 0.0 = current, 1.0 = old */
        public int		skinnum; /* also used as RF_BEAM's palette index */

        public int		lightstyle; /* for flashing entities */
        public float	alpha; /* ignore if RF_TRANSLUCENT isn't set */

        public image_s?	skin; /* NULL for inline skin */
        public int		flags;


        public void Clear() {
            model = null;
            angles.X = 0;
            angles.Y = 0;
            angles.Z = 0;
            origin.X = 0;
            origin.Y = 0;
            origin.Z = 0;
            frame = 0;
            oldorigin.X = 0;
            oldorigin.Y = 0;
            oldorigin.Z = 0;
            oldframe = 0;
            backlerp = 0;
            skinnum = 0;
            lightstyle = 0;
            alpha = 0;
            skin = null;
            flags = 0;
        }
    }

    internal struct dlight_t {
        public Vector3	origin;
        public Vector3	color;
        public float	intensity;
    }

    internal struct particle_t {
        public Vector3	origin;
        public int		color;
        public float	alpha;
    }

    internal struct lightstyle_t {
        public float[]		rgb; /* 0.0 - 2.0 */
        public float		white; /* r+g+b */
    }

    internal struct refdef_t {
        public int			x, y, width, height; /* in virtual screen coordinates */
        public float		fov_x, fov_y;
        public Vector3 vieworg;
        public Vector3 viewangles;
        public Vector4 blend; /* rgba 0-1 full screen blend */
        public float		time; /* time is used to auto animate */
        public int			rdflags; /* RDF_UNDERWATER, etc */

        public byte[]		areabits; /* if not NULL, only areas with set bits will be drawn */

        public lightstyle_t[]	lightstyles; /* [MAX_LIGHTSTYLES] */

        public int			num_entities;
        public entity_t[]	entities;

        public int			num_dlights; // <= 32 (MAX_DLIGHTS)
        public dlight_t[]	dlights;

        public int			num_particles;
        public particle_t[]	particles;
    }

    //
    // these are the functions exported by the refresh module
    //
    internal interface refexport_t
    {
        // if api_version is different, the dll cannot be used
        // int		api_version;

        // called when the library is loaded
        bool Init ();
        bool PostInit (Silk.NET.Windowing.IWindow window);

        // called before the library is unloaded
        // void	(EXPORT *Shutdown) (void);

        // called by GLimp_InitGraphics() before creating window,
        // returns flags for SDL window creation, returns -1 on error
        // int PrepareForWindow(Silk.NET.SDL.Sdl sdl);

        // called by GLimp_InitGraphics() *after* creating window,
        // passing the SDL_Window* (void* so we don't spill SDL.h here)
        // (or SDL_Surface* for SDL1.2, another reason to use void*)
        // returns true (1) on success
        // unsafe bool InitContext(Silk.NET.SDL.Sdl sdl, Silk.NET.SDL.Window* sdl_window);

        // // shuts down rendering (OpenGL) context.
        // void	(EXPORT *ShutdownContext)(void);

        // // returns true if vsync is active, else false
        // qboolean (EXPORT *IsVSyncActive)(void);

        // All data that will be used in a level should be
        // registered before rendering any frames to prevent disk hits,
        // but they can still be registered at a later time
        // if necessary.
        //
        // EndRegistration will free any remaining data that wasn't registered.
        // Any model_s or skin_s pointers from before the BeginRegistration
        // are no longer valid after EndRegistration.
        //
        // Skins and images need to be differentiated, because skins
        // are flood filled to eliminate mip map edge errors, and pics have
        // an implicit "pics/" prepended to the name. (a pic name that starts with a
        // slash will not use the "pics/" prefix or the ".pcx" postfix)
        void BeginRegistration (Silk.NET.Windowing.IWindow window, string map);
        model_s? RegisterModel (Silk.NET.Windowing.IWindow window, string name);
        image_s? RegisterSkin (Silk.NET.Windowing.IWindow window, string name);

        void SetSky(Silk.NET.Windowing.IWindow window, string name, float rotate, in Vector3 axis);
        // void	(EXPORT *EndRegistration) (void);

        void RenderFrame (Silk.NET.Windowing.IWindow window, in refdef_t fd);

        // struct image_s * (EXPORT *DrawFindPic)(char *name);

        void DrawGetPicSize (Silk.NET.Windowing.IWindow window, out int w, out int h, string name);	// will return 0 0 if not found
        void DrawPicScaled (Silk.NET.Windowing.IWindow window, int x, int y, string pic, float factor);
        void DrawStretchPic (Silk.NET.Windowing.IWindow window, int x, int y, int w, int h, string name);
        void DrawCharScaled(Silk.NET.Windowing.IWindow window, int x, int y, int num, float scale);
        // void	(EXPORT *DrawTileClear) (int x, int y, int w, int h, char *name);
        // void	(EXPORT *DrawFill) (int x, int y, int w, int h, int c);
        // void	(EXPORT *DrawFadeScreen) (void);

        // // Draw images for cinematic rendering (which can have a different palette). Note that calls
        // void	(EXPORT *DrawStretchRaw) (int x, int y, int w, int h, int cols, int rows, byte *data);

        /*
        ** video mode and refresh state management entry points
        */
        // void	(EXPORT *SetPalette)( const unsigned char *palette);	// NULL = game palette
        void BeginFrame(Silk.NET.Windowing.IWindow window, float camera_separation);
        void EndFrame (Silk.NET.Windowing.IWindow window);
        // qboolean	(EXPORT *EndWorldRenderpass) (void); // finish world rendering, apply postprocess and switch to UI render pass

        //void	(EXPORT *AppActivate)( qboolean activate );
    }

    internal interface refimport_t
    {
        void Sys_Error (int err_level, string msg);

        // void	(IMPORT *Cmd_AddCommand) (char *name, void(*cmd)(void));
        // void	(IMPORT *Cmd_RemoveCommand) (char *name);
        // int		(IMPORT *Cmd_Argc) (void);
        // char	*(IMPORT *Cmd_Argv) (int i);
        // void	(IMPORT *Cmd_ExecuteText) (int exec_when, char *text);

        void Com_VPrintf(int print_level, string msg);

        // // files will be memory mapped read only
        // // the returned buffer may be part of a larger pak file,
        // // or a discrete file from anywhere in the quake search path
        // // a -1 return means the file does not exist
        // // NULL can be passed for buf to just determine existance
        byte[]? FS_LoadFile(string name);
        // void	(IMPORT *FS_FreeFile) (void *buf);

        // // gamedir will be the current directory that generated
        // // files should be stored to, ie: "f:\quake\id1"
        // char	*(IMPORT *FS_Gamedir) (void);

        cvar_t? Cvar_Get (string name, string? value, int flags);
        // cvar_t	*(IMPORT *Cvar_Set) (char *name, char *value);
        // void	 (IMPORT *Cvar_SetValue) (char *name, float value);

        bool Vid_GetModeInfo(ref int width, ref int height, int mode);
        // void		(IMPORT *Vid_MenuInit)( void );
        // // called with image data of width*height pixel which comp bytes per pixel (must be 3 or 4 for RGB or RGBA)
        // // expects the pixels data to be row-wise, starting at top left
        // void		(IMPORT *Vid_WriteScreenshot)( int width, int height, int comp, const void* data );

        bool Vid_SetModeInfo(int fullscreen, int width, int height);

        // void		(IMPORT *Vid_RequestRestart)(ref_restart_t rs);
    }

}