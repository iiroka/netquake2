/*
 * Copyright (C) 1997-2001 Id Software, Inc.
 * Copyright (C) 2016-2017 Daniel Gibson
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
 * Refresher setup and main part of the frame generation, for OpenGL3
 *
 * =======================================================================
 */

using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;

namespace Quake2 {

    partial class QRefGl3 : refexport_t 
    {

        private refimport_t ri;

        public QRefGl3(refimport_t ri)
        {
            this.ri = ri;
        }


        private gl3config_t gl3config;
        private gl3state_t gl3state;

        /* screen size info */
        private refdef_t gl3_newrefdef;

        private gl3brushmodel_t? gl3_worldmodel;

        private float gl3depthmin=0.0f, gl3depthmax=1.0f;

        private viddef_t vid;

        private int gl3_viewcluster, gl3_viewcluster2, gl3_oldviewcluster, gl3_oldviewcluster2;

        /* view origin */
        private Vector3 vup;
        private Vector3 vpn;
        private Vector3 vright;
        private Vector3 gl3_origin;

        private Vector4 v_blend;

        private int c_brush_polys, c_alias_polys;
        private int gl3_visframecount; /* bumped when going to a new PVS */
        private int gl3_framecount; /* used for dlight push checking */

        private readonly Matrix4X4<float> gl3_identityMat4 = Matrix4X4<float>.Identity;

        private QShared.cplane_t[] frustum = new QShared.cplane_t[4];

        private cvar_t? gl_msaa_samples;
        private cvar_t? r_vsync;
        private cvar_t? r_retexturing;
        private cvar_t? r_scale8bittextures;
        private cvar_t? vid_fullscreen;
        private cvar_t? r_mode;
        private cvar_t? r_customwidth;
        private cvar_t? r_customheight;
        private cvar_t? vid_gamma;
        private cvar_t? gl_anisotropic;
        private cvar_t? gl_texturemode;
        private cvar_t? gl_drawbuffer;
        private cvar_t? r_clear;
        private cvar_t? gl3_particle_size;
        private cvar_t? gl3_particle_fade_factor;
        private cvar_t? gl3_particle_square;
        private cvar_t? gl3_colorlight;

        private cvar_t? gl_lefthand;
        private cvar_t? r_gunfov;
        private cvar_t? r_farsee;

        private cvar_t? gl3_intensity;
        private cvar_t? gl3_intensity_2D;
        private cvar_t? r_lightlevel;
        private cvar_t? gl3_overbrightbits;

        private cvar_t? r_norefresh;
        private cvar_t? r_drawentities;
        private cvar_t? r_drawworld;
        private cvar_t? gl_nolerp_list;
        private cvar_t? r_lerp_list;
        private cvar_t? r_2D_unfiltered;
        private cvar_t? r_videos_unfiltered;
        private cvar_t? gl_nobind;
        private cvar_t? r_lockpvs;
        private cvar_t? r_novis;
        private cvar_t? r_speeds;
        private cvar_t? gl_finish;

        private cvar_t? gl_cull;
        private cvar_t? gl_zfix;
        private cvar_t? r_fullbright;
        private cvar_t? r_modulate;
        private cvar_t? gl_lightmap;
        private cvar_t? gl_shadows;
        private cvar_t? gl3_debugcontext;
        private cvar_t? gl3_usebigvbo;
        private cvar_t? r_fixsurfsky;
        private cvar_t? gl3_usefbo;

        // Yaw-Pitch-Roll
        // equivalent to R_z * R_y * R_x where R_x is the trans matrix for rotating around X axis for aroundXdeg
        private Matrix4X4<float> rotAroundAxisZYX(float aroundZdeg, float aroundYdeg, float aroundXdeg)
        {
            // Naming of variables is consistent with http://planning.cs.uiuc.edu/node102.html
            // and https://de.wikipedia.org/wiki/Roll-Nick-Gier-Winkel#.E2.80.9EZY.E2.80.B2X.E2.80.B3-Konvention.E2.80.9C
            float alpha = QShared.ToRadians(aroundZdeg);
            float beta = QShared.ToRadians(aroundYdeg);
            float gamma = QShared.ToRadians(aroundXdeg);

            float sinA = MathF.Sin(alpha);
            float cosA = MathF.Cos(alpha);
            // TODO: or sincosf(alpha, &sinA, &cosA); ?? (not a standard function)
            float sinB = MathF.Sin(beta);
            float cosB = MathF.Cos(beta);
            float sinG = MathF.Sin(gamma);
            float cosG = MathF.Cos(gamma);

            return new Matrix4X4<float>(
                cosA*cosB,                  sinA*cosB,                   -sinB,    0, // first *column*
                cosA*sinB*sinG - sinA*cosG, sinA*sinB*sinG + cosA*cosG, cosB*sinG, 0,
                cosA*sinB*cosG + sinA*sinG, sinA*sinB*cosG - cosA*sinG, cosB*cosG, 0,
                0,                          0,                          0,        1
            );
        }

        private void GL3_RotateForEntity(GL gl, in entity_t e)
        {
            // angles: pitch (around y), yaw (around z), roll (around x)
            // rot matrices to be multiplied in order Z, Y, X (yaw, pitch, roll)
            var transMat = rotAroundAxisZYX(e.angles.Y, -e.angles.X, -e.angles.Z);

            transMat.M41 = e.origin.X;
            transMat.M42 = e.origin.Y;
            transMat.M43 = e.origin.Z;

            gl3state.uni3DData.transModelMat4 = HMM_MultiplyMat4(gl3state.uni3DData.transModelMat4, transMat);

            GL3_UpdateUBO3D(gl);
        }

        private void GL3_Strings(GL gl)
        {
            R_Printf(QShared.PRINT_ALL, $"GL_VENDOR: {gl3config.vendor_string}\n");
            R_Printf(QShared.PRINT_ALL, $"GL_RENDERER: {gl3config.renderer_string}\n");
            R_Printf(QShared.PRINT_ALL, $"GL_VERSION: {gl3config.version_string}\n");
            R_Printf(QShared.PRINT_ALL, $"GL_SHADING_LANGUAGE_VERSION: {gl3config.glsl_version_string}\n");

            gl.GetInteger(GetPName.NumExtensions, out int numExtensions);

            R_Printf(QShared.PRINT_ALL, "GL_EXTENSIONS:");
            for(int i = 0; i < numExtensions; i++)
            {
                R_Printf(QShared.PRINT_ALL, $" {gl.GetStringS(StringName.Extensions, (uint)i)}");
            }
            R_Printf(QShared.PRINT_ALL, "\n");
        }

        private void GL3_Register()
        {
            gl_lefthand = ri.Cvar_Get("hand", "0", cvar_t.CVAR_USERINFO | cvar_t.CVAR_ARCHIVE);
            r_gunfov = ri.Cvar_Get("r_gunfov", "80", cvar_t.CVAR_ARCHIVE);
            r_farsee = ri.Cvar_Get("r_farsee", "0", cvar_t.CVAR_LATCH | cvar_t.CVAR_ARCHIVE);

            gl_drawbuffer = ri.Cvar_Get("gl_drawbuffer", "GL_BACK", 0);
            r_vsync = ri.Cvar_Get("r_vsync", "1", cvar_t.CVAR_ARCHIVE);
            gl_msaa_samples = ri.Cvar_Get ( "r_msaa_samples", "0", cvar_t.CVAR_ARCHIVE );
            r_retexturing = ri.Cvar_Get("r_retexturing", "1", cvar_t.CVAR_ARCHIVE);
            r_scale8bittextures = ri.Cvar_Get("r_scale8bittextures", "0", cvar_t.CVAR_ARCHIVE);
            gl3_debugcontext = ri.Cvar_Get("gl3_debugcontext", "0", 0);
            r_mode = ri.Cvar_Get("r_mode", "4", cvar_t.CVAR_ARCHIVE);
            r_customwidth = ri.Cvar_Get("r_customwidth", "1024", cvar_t.CVAR_ARCHIVE);
            r_customheight = ri.Cvar_Get("r_customheight", "768", cvar_t.CVAR_ARCHIVE);
            gl3_particle_size = ri.Cvar_Get("gl3_particle_size", "40", cvar_t.CVAR_ARCHIVE);
            gl3_particle_fade_factor = ri.Cvar_Get("gl3_particle_fade_factor", "1.2", cvar_t.CVAR_ARCHIVE);
            gl3_particle_square = ri.Cvar_Get("gl3_particle_square", "0", cvar_t.CVAR_ARCHIVE);
            // if set to 0, lights (from lightmaps, dynamic lights and on models) are white instead of colored
            gl3_colorlight = ri.Cvar_Get("gl3_colorlight", "1", cvar_t.CVAR_ARCHIVE);

            //  0: use lots of calls to glBufferData()
            //  1: reduce calls to glBufferData() with one big VBO (see GL3_BufferAndDraw3D())
            // -1: auto (let yq2 choose to enable/disable this based on detected driver)
            gl3_usebigvbo = ri.Cvar_Get("gl3_usebigvbo", "-1", cvar_t.CVAR_ARCHIVE);

            r_norefresh = ri.Cvar_Get("r_norefresh", "0", 0);
            r_drawentities = ri.Cvar_Get("r_drawentities", "1", 0);
            r_drawworld = ri.Cvar_Get("r_drawworld", "1", 0);
            r_fullbright = ri.Cvar_Get("r_fullbright", "0", 0);
            r_fixsurfsky = ri.Cvar_Get("r_fixsurfsky", "0", cvar_t.CVAR_ARCHIVE);

            /* don't bilerp characters and crosshairs */
            gl_nolerp_list = ri.Cvar_Get("r_nolerp_list", "pics/conchars.pcx pics/ch1.pcx pics/ch2.pcx pics/ch3.pcx", cvar_t.CVAR_ARCHIVE);
            /* textures that should always be filtered, even if r_2D_unfiltered or an unfiltered gl mode is used */
            r_lerp_list = ri.Cvar_Get("r_lerp_list", "", cvar_t.CVAR_ARCHIVE);
            /* don't bilerp any 2D elements */
            r_2D_unfiltered = ri.Cvar_Get("r_2D_unfiltered", "0", cvar_t.CVAR_ARCHIVE);
            /* don't bilerp videos */
            r_videos_unfiltered = ri.Cvar_Get("r_videos_unfiltered", "0", cvar_t.CVAR_ARCHIVE);
            gl_nobind = ri.Cvar_Get("gl_nobind", "0", 0);

            gl_texturemode = ri.Cvar_Get("gl_texturemode", "GL_LINEAR_MIPMAP_NEAREST", cvar_t.CVAR_ARCHIVE);
            gl_anisotropic = ri.Cvar_Get("r_anisotropic", "0", cvar_t.CVAR_ARCHIVE);

            vid_fullscreen = ri.Cvar_Get("vid_fullscreen", "0", cvar_t.CVAR_ARCHIVE);
            vid_gamma = ri.Cvar_Get("vid_gamma", "1.2", cvar_t.CVAR_ARCHIVE);
            gl3_intensity = ri.Cvar_Get("gl3_intensity", "1.5", cvar_t.CVAR_ARCHIVE);
            gl3_intensity_2D = ri.Cvar_Get("gl3_intensity_2D", "1.5", cvar_t.CVAR_ARCHIVE);

            r_lightlevel = ri.Cvar_Get("r_lightlevel", "0", 0);
            gl3_overbrightbits = ri.Cvar_Get("gl3_overbrightbits", "1.3", cvar_t.CVAR_ARCHIVE);

            gl_lightmap = ri.Cvar_Get("r_lightmap", "0", 0);
            gl_shadows = ri.Cvar_Get("r_shadows", "0", cvar_t.CVAR_ARCHIVE);

            r_modulate = ri.Cvar_Get("r_modulate", "1", cvar_t.CVAR_ARCHIVE);
            gl_zfix = ri.Cvar_Get("gl_zfix", "0", 0);
            r_clear = ri.Cvar_Get("r_clear", "1", 0);
            gl_cull = ri.Cvar_Get("gl_cull", "0", 0); // IKn
            r_lockpvs = ri.Cvar_Get("r_lockpvs", "0", 0);
            r_novis = ri.Cvar_Get("r_novis", "0", 0);
            r_speeds = ri.Cvar_Get("r_speeds", "0", 0);
            gl_finish = ri.Cvar_Get("gl_finish", "0", cvar_t.CVAR_ARCHIVE);

            gl3_usefbo = ri.Cvar_Get("gl3_usefbo", "1", cvar_t.CVAR_ARCHIVE); // use framebuffer object for postprocess effects (water)

        // #if 0 // TODO!
        //     //gl_lefthand = ri.Cvar_Get("hand", "0", CVAR_USERINFO | CVAR_ARCHIVE);
        //     //gl_farsee = ri.Cvar_Get("gl_farsee", "0", CVAR_LATCH | CVAR_ARCHIVE);
        //     //r_norefresh = ri.Cvar_Get("r_norefresh", "0", 0);
        //     //r_fullbright = ri.Cvar_Get("r_fullbright", "0", 0);
        //     //r_drawentities = ri.Cvar_Get("r_drawentities", "1", 0);
        //     //r_drawworld = ri.Cvar_Get("r_drawworld", "1", 0);
        //     //r_novis = ri.Cvar_Get("r_novis", "0", 0);
        //     //r_lerpmodels = ri.Cvar_Get("r_lerpmodels", "1", 0); NOTE: screw this, it looks horrible without
        //     //r_speeds = ri.Cvar_Get("r_speeds", "0", 0);

        //     //r_lightlevel = ri.Cvar_Get("r_lightlevel", "0", 0);
        //     //gl_overbrightbits = ri.Cvar_Get("gl_overbrightbits", "0", CVAR_ARCHIVE);

        //     gl1_particle_min_size = ri.Cvar_Get("gl1_particle_min_size", "2", CVAR_ARCHIVE);
        //     gl1_particle_max_size = ri.Cvar_Get("gl1_particle_max_size", "40", CVAR_ARCHIVE);
        //     //gl1_particle_size = ri.Cvar_Get("gl1_particle_size", "40", CVAR_ARCHIVE);
        //     gl1_particle_att_a = ri.Cvar_Get("gl1_particle_att_a", "0.01", CVAR_ARCHIVE);
        //     gl1_particle_att_b = ri.Cvar_Get("gl1_particle_att_b", "0.0", CVAR_ARCHIVE);
        //     gl1_particle_att_c = ri.Cvar_Get("gl1_particle_att_c", "0.01", CVAR_ARCHIVE);

        //     //gl_modulate = ri.Cvar_Get("gl_modulate", "1", CVAR_ARCHIVE);
        //     //r_mode = ri.Cvar_Get("r_mode", "4", CVAR_ARCHIVE);
        //     //gl_lightmap = ri.Cvar_Get("r_lightmap", "0", 0);
        //     //gl_shadows = ri.Cvar_Get("r_shadows", "0", CVAR_ARCHIVE);
        //     //gl_nobind = ri.Cvar_Get("gl_nobind", "0", 0);
        //     gl_showtris = ri.Cvar_Get("gl_showtris", "0", 0);
        //     gl_showbbox = Cvar_Get("gl_showbbox", "0", 0);
        //     //gl1_ztrick = ri.Cvar_Get("gl1_ztrick", "0", 0); NOTE: dump this.
        //     //gl_zfix = ri.Cvar_Get("gl_zfix", "0", 0);
        //     //gl_finish = ri.Cvar_Get("gl_finish", "0", CVAR_ARCHIVE);
        //     r_clear = ri.Cvar_Get("r_clear", "0", 0);
        // //	gl_cull = ri.Cvar_Get("gl_cull", "1", 0);
        //     //gl1_flashblend = ri.Cvar_Get("gl1_flashblend", "0", 0);

        //     //gl_texturemode = ri.Cvar_Get("gl_texturemode", "GL_LINEAR_MIPMAP_NEAREST", CVAR_ARCHIVE);
        //     gl1_texturealphamode = ri.Cvar_Get("gl1_texturealphamode", "default", CVAR_ARCHIVE);
        //     gl1_texturesolidmode = ri.Cvar_Get("gl1_texturesolidmode", "default", CVAR_ARCHIVE);
        //     //gl_anisotropic = ri.Cvar_Get("r_anisotropic", "0", CVAR_ARCHIVE);
        //     //r_lockpvs = ri.Cvar_Get("r_lockpvs", "0", 0);

        //     //gl1_palettedtexture = ri.Cvar_Get("gl1_palettedtexture", "0", CVAR_ARCHIVE); NOPE.
        //     gl1_pointparameters = ri.Cvar_Get("gl1_pointparameters", "1", CVAR_ARCHIVE);

        //     //gl_drawbuffer = ri.Cvar_Get("gl_drawbuffer", "GL_BACK", 0);
        //     //r_vsync = ri.Cvar_Get("r_vsync", "1", CVAR_ARCHIVE);


        //     //vid_fullscreen = ri.Cvar_Get("vid_fullscreen", "0", CVAR_ARCHIVE);
        //     //vid_gamma = ri.Cvar_Get("vid_gamma", "1.0", CVAR_ARCHIVE);

        //     //r_customwidth = ri.Cvar_Get("r_customwidth", "1024", CVAR_ARCHIVE);
        //     //r_customheight = ri.Cvar_Get("r_customheight", "768", CVAR_ARCHIVE);
        //     //gl_msaa_samples = ri.Cvar_Get ( "r_msaa_samples", "0", CVAR_ARCHIVE );

        //     //r_retexturing = ri.Cvar_Get("r_retexturing", "1", CVAR_ARCHIVE);


        //     gl1_stereo = ri.Cvar_Get( "gl1_stereo", "0", CVAR_ARCHIVE );
        //     gl1_stereo_separation = ri.Cvar_Get( "gl1_stereo_separation", "-0.4", CVAR_ARCHIVE );
        //     gl1_stereo_anaglyph_colors = ri.Cvar_Get( "gl1_stereo_anaglyph_colors", "rc", CVAR_ARCHIVE );
        //     gl1_stereo_convergence = ri.Cvar_Get( "gl1_stereo_convergence", "1", CVAR_ARCHIVE );
        // #endif // 0

            // ri.Cmd_AddCommand("imagelist", GL3_ImageList_f);
            // ri.Cmd_AddCommand("screenshot", GL3_ScreenShot);
            // ri.Cmd_AddCommand("modellist", GL3_Mod_Modellist_f);
            // ri.Cmd_AddCommand("gl_strings", GL3_Strings);
        }

        /*
        * Changes the video mode
        */

        // the following is only used in the next to functions,
        // no need to put it in a header
        private enum setmoderet_t
        {
            rserr_ok,

            rserr_invalid_mode,

            rserr_unknown
        };

        private setmoderet_t SetMode_impl(ref int width, ref int height, int mode, int fullscreen)
        {
            R_Printf(QShared.PRINT_ALL, $"Setting mode {mode}:");

            /* mode -1 is not in the vid mode table - so we keep the values in pwidth
            and pheight and don't even try to look up the mode info */
            if ((mode >= 0) && !ri.Vid_GetModeInfo(ref width, ref height, mode))
            {
                R_Printf(QShared.PRINT_ALL, " invalid mode\n");
                return setmoderet_t.rserr_invalid_mode;
            }

            /* We trying to get resolution from desktop */
            if (mode == -2)
            {
            //     if(!ri.GLimp_GetDesktopMode(pwidth, pheight))
            //     {
            //         R_Printf( PRINT_ALL, " can't detect mode\n" );
            //         return rserr_invalid_mode;
            //     }
            }

            R_Printf(QShared.PRINT_ALL, $" {width}x{height} (vid_fullscreen {fullscreen})\n");


            if (!ri.Vid_SetModeInfo(fullscreen, width, height))
            {
                return setmoderet_t.rserr_invalid_mode;
            }

            return setmoderet_t.rserr_ok;
        }

        private bool GL3_SetMode()
        {
            int fullscreen = vid_fullscreen?.Int ?? 0;

            /* a bit hackish approach to enable custom resolutions:
            Glimp_SetMode needs these values set for mode -1 */
            vid.width = r_customwidth?.Int ?? -1;
            vid.height = r_customheight?.Int ?? -1;

            var err = SetMode_impl(ref vid.width, ref vid.height, r_mode!.Int, fullscreen);
            if (err == setmoderet_t.rserr_ok)
            {
                if (r_mode!.Int == -1)
                {
                    gl3state.prev_mode = 4; /* safe default for custom mode */
                }
                else
                {
                    gl3state.prev_mode = r_mode!.Int;
                }
            }
            else
            {
            //     if (err == rserr_invalid_mode)
            //     {
            //         R_Printf(PRINT_ALL, "ref_gl3::GL3_SetMode() - invalid mode\n");

            //         if (gl_msaa_samples->value != 0.0f)
            //         {
            //             R_Printf(PRINT_ALL, "gl_msaa_samples was %d - will try again with gl_msaa_samples = 0\n", (int)gl_msaa_samples->value);
            //             ri.Cvar_SetValue("r_msaa_samples", 0.0f);
            //             gl_msaa_samples->modified = false;

            //             if ((err = SetMode_impl(&vid.width, &vid.height, r_mode->value, 0)) == rserr_ok)
            //             {
            //                 return true;
            //             }
            //         }
            //         if(r_mode->value == gl3state.prev_mode)
            //         {
            //             // trying again would result in a crash anyway, give up already
            //             // (this would happen if your initing fails at all and your resolution already was 640x480)
            //             return false;
            //         }

            //         ri.Cvar_SetValue("r_mode", gl3state.prev_mode);
            //         r_mode->modified = false;
            //     }

            //     /* try setting it back to something safe */
            //     if ((err = SetMode_impl(&vid.width, &vid.height, gl3state.prev_mode, 0)) != rserr_ok)
            //     {
            //         R_Printf(PRINT_ALL, "ref_gl3::GL3_SetMode() - could not revert to safe mode\n");
            //         return false;
            //     }
            }

            return true;
        }

        public bool Init ()
        {
        //     Swap_Init(); // FIXME: for fucks sake, this doesn't have to be done at runtime!

        //     R_Printf(PRINT_ALL, "Refresh: " REF_VERSION "\n");
        //     R_Printf(PRINT_ALL, "Client: " YQ2VERSION "\n\n");

        //     if(sizeof(float) != sizeof(GLfloat))
        //     {
        //         // if this ever happens, things would explode because we feed vertex arrays and UBO data
        //         // using floats to OpenGL, which expects GLfloat (can't easily change, those floats are from HMM etc)
        //         // (but to be honest I very much doubt this will ever happen.)
        //         R_Printf(PRINT_ALL, "ref_gl3: sizeof(float) != sizeof(GLfloat) - we're in real trouble here.\n");
        //         return false;
        //     }

            GL3_Draw_GetPalette();

            GL3_Register();

            /* set our "safe" mode */
            gl3state.prev_mode = 4;
            //gl_state.stereo_mode = gl1_stereo->value;

            /* create the window and set up the context */
            if (!GL3_SetMode())
            {
                R_Printf(QShared.PRINT_ALL, "ref_gl3::R_Init() - could not R_SetMode()\n");
                return false;
            }

        //     ri.Vid_MenuInit();

            return true;
        }

        public bool PostInit (IWindow window)
        {
            /* get our various GL strings */
            var gl = GL.GetApi(window);
            gl3config.vendor_string =gl.GetStringS(StringName.Vendor);
            gl3config.renderer_string =gl.GetStringS(StringName.Renderer);
            gl3config.version_string =gl.GetStringS(StringName.Version);
            gl3config.glsl_version_string =gl.GetStringS(StringName.ShadingLanguageVersion);

        	gl3config.debug_output = gl.IsExtensionPresent("GL_ARB_debug_output");
            gl3config.anisotropic = gl.IsExtensionPresent("GL_EXT_texture_filter_anisotropic");

            R_Printf(QShared.PRINT_ALL, "\nOpenGL setting:\n");
            GL3_Strings(gl);

            /*
            if (gl_config.major_version < 3)
            {
                // if (gl_config.major_version == 3 && gl_config.minor_version < 2)
                {
                    QGL_Shutdown();
                    R_Printf(PRINT_ALL, "Support for OpenGL 3.2 is not available\n");

                    return false;
                }
            }
            */

            R_Printf(QShared.PRINT_ALL, "\n\nProbing for OpenGL extensions:\n");


            /* Anisotropic */
            R_Printf(QShared.PRINT_ALL, " - Anisotropic Filtering: ");

            if(gl3config.anisotropic)
            {
                gl.GetFloat(GLEnum.MaxTextureMaxAnisotropy, out gl3config.max_anisotropy);

                R_Printf(QShared.PRINT_ALL, $"Max level: {(int)gl3config.max_anisotropy}x\n");
            }
            else
            {
                gl3config.max_anisotropy = 0;

                R_Printf(QShared.PRINT_ALL, "Not supported\n");
            }

            if(gl3config.debug_output)
            {
                R_Printf(QShared.PRINT_ALL, " - OpenGL Debug Output: Supported ");
                if (gl3_debugcontext?.Bool ?? false)
                {
                    R_Printf(QShared.PRINT_ALL, $"and enabled with gl3_debugcontext = {gl3_debugcontext!.Int}\n");
                    unsafe {
                        gl.DebugMessageCallback(DebugCallback, null);
                    }
		            gl.Enable(EnableCap.DebugOutput);

                }
                else
                {
                    R_Printf(QShared.PRINT_ALL, "(but disabled with gl3_debugcontext = 0)\n");
                }
            }
            else
            {
                R_Printf(QShared.PRINT_ALL, " - OpenGL Debug Output: Not Supported\n");
            }

            gl3config.useBigVBO = false;
            if(gl3_usebigvbo?.Int == 1)
            {
                R_Printf(QShared.PRINT_ALL, "Enabling useBigVBO workaround because gl3_usebigvbo = 1\n");
                gl3config.useBigVBO = true;
            }
            else if(gl3_usebigvbo?.Int == -1)
            {
        //         // enable for AMDs proprietary Windows and Linux drivers
        // #ifdef _WIN32
        //         if(gl3config.version_string != NULL && gl3config.vendor_string != NULL
        //         && strstr(gl3config.vendor_string, "ATI Technologies Inc") != NULL)
        //         {
        //             int a, b, ver;
        //             if(sscanf(gl3config.version_string, " %d.%d.%d ", &a, &b, &ver) >= 3 && ver >= 13431)
        //             {
        //                 // turns out the legacy driver is a lot faster *without* the workaround :-/
        //                 // GL_VERSION for legacy 16.2.1 Beta driver: 3.2.13399 Core Profile Forward-Compatible Context 15.200.1062.1004
        //                 //            (this is the last version that supports the Radeon HD 6950)
        //                 // GL_VERSION for (non-legacy) 16.3.1 driver on Radeon R9 200: 4.5.13431 Compatibility Profile Context 16.150.2111.0
        //                 // GL_VERSION for non-legacy 17.7.2 WHQL driver: 4.5.13491 Compatibility Profile/Debug Context 22.19.662.4
        //                 // GL_VERSION for 18.10.1 driver: 4.6.13541 Compatibility Profile/Debug Context 25.20.14003.1010
        //                 // GL_VERSION for (current) 19.3.2 driver: 4.6.13547 Compatibility Profile/Debug Context 25.20.15027.5007
        //                 // (the 3.2/4.5/4.6 can probably be ignored, might depend on the card and what kind of context was requested
        //                 //  but AFAIK the number behind that can be used to roughly match the driver version)
        //                 // => let's try matching for x.y.z with z >= 13431
        //                 // (no, I don't feel like testing which release since 16.2.1 has introduced the slowdown.)
        //                 R_Printf(PRINT_ALL, "Detected AMD Windows GPU driver, enabling useBigVBO workaround\n");
        //                 gl3config.useBigVBO = true;
        //             }
        //         }
        // #elif defined(__linux__)
        //         if(gl3config.vendor_string != NULL && strstr(gl3config.vendor_string, "Advanced Micro Devices, Inc.") != NULL)
        //         {
        //             R_Printf(PRINT_ALL, "Detected proprietary AMD GPU driver, enabling useBigVBO workaround\n");
        //             R_Printf(PRINT_ALL, "(consider using the open source RadeonSI drivers, they tend to work better overall)\n");
        //             gl3config.useBigVBO = true;
        //         }
        // #endif
            }

            // generate texture handles for all possible lightmaps
            gl3state.lightmap_textureIDs = new uint[MAX_LIGHTMAPS * MAX_LIGHTMAPS_PER_SURFACE];
            gl.GenTextures(MAX_LIGHTMAPS*MAX_LIGHTMAPS_PER_SURFACE, gl3state.lightmap_textureIDs);

            GL3_SetDefaultState(gl);

            if (GL3_InitShaders(gl))
            {
                R_Printf(QShared.PRINT_ALL, "Loading shaders succeeded.\n");
            }
            else
            {
                R_Printf(QShared.PRINT_ALL, "Loading shaders failed.\n");
                return false;
            }

            registration_sequence = 1; // from R_InitImages() (everything else from there shouldn't be needed anymore)

            GL3_Mod_Init();

            GL3_InitParticleTexture(gl);

            GL3_Draw_InitLocal(gl);

            GL3_SurfInit(gl);

            gl3state.ppFBO = gl.GenFramebuffer();
            // the rest for the FBO is done dynamically in GL3_RenderView() so it can
            // take the viewsize into account (enforce that by setting invalid size)
            gl3state.ppFBtexWidth = gl3state.ppFBtexHeight = -1;

            R_Printf(QShared.PRINT_ALL, "\n");            
            return true;
        }

        // assumes gl3state.v[ab]o3D are bound
        // buffers and draws gl3_3D_vtx_t vertices
        // drawMode is something like GL_TRIANGLE_STRIP or GL_TRIANGLE_FAN or whatever
        private unsafe void GL3_BufferAndDraw3D(GL gl, gl3_3D_vtx_t[] verts, PrimitiveType drawMode)
        {
            if(!gl3config.useBigVBO)
            {
                fixed (void *b = verts) {
                    gl.BufferData( BufferTargetARB.ArrayBuffer, (nuint)(sizeof(float)*11*verts.Length), b, BufferUsageARB.StaticDraw );
                }
                gl.DrawArrays( drawMode, 0, (uint)verts.Length );
            }
            else // gl3config.useBigVBO == true
            {
                /*
                * For some reason, AMD's Windows driver doesn't seem to like lots of
                * calls to glBufferData() (some of them seem to take very long then).
                * GL3_BufferAndDraw3D() is called a lot when drawing world geometry
                * (once for each visible face I think?).
                * The simple code above caused noticeable slowdowns - even a fast
                * quadcore CPU and a Radeon RX580 weren't able to maintain 60fps..
                * The workaround is to not call glBufferData() with small data all the time,
                * but to allocate a big buffer and on each call to GL3_BufferAndDraw3D()
                * to use a different region of that buffer, resulting in a lot less calls
                * to glBufferData() (=> a lot less buffer allocations in the driver).
                * Only when the buffer is full and at the end of a frame (=> GL3_EndFrame())
                * we get a fresh buffer.
                *
                * BTW, we couldn't observe this kind of problem with any other driver:
                * Neither nvidias driver, nor AMDs or Intels Open Source Linux drivers,
                * not even Intels Windows driver seem to care that much about the
                * glBufferData() calls.. However, at least nvidias driver doesn't like
                * this workaround (with glMapBufferRange()), the framerate dropped
                * significantly - that's why both methods are available and
                * selectable at runtime.
                */
                Console.WriteLine("***** UNIMPLEMENTED USE BIG FBO ******");
        // #if 0
        //         // I /think/ doing it with glBufferSubData() didn't really help
        //         const int bufSize = gl3state.vbo3Dsize;
        //         int neededSize = numVerts*sizeof(gl3_3D_vtx_t);
        //         int curOffset = gl3state.vbo3DcurOffset;
        //         if(curOffset + neededSize > gl3state.vbo3Dsize)
        //             curOffset = 0;
        //         int curIdx = curOffset / sizeof(gl3_3D_vtx_t);

        //         gl3state.vbo3DcurOffset = curOffset + neededSize;

        //         glBufferSubData( GL_ARRAY_BUFFER, curOffset, neededSize, verts );
        //         glDrawArrays( drawMode, curIdx, numVerts );
        // #else
                // int curOffset = gl3state.vbo3DcurOffset;
                // int neededSize = numVerts*sizeof(gl3_3D_vtx_t);
                // if(curOffset+neededSize > gl3state.vbo3Dsize)
                // {
                //     // buffer is full, need to start again from the beginning
                //     // => need to sync or get fresh buffer
                //     // (getting fresh buffer seems easier)
                //     gl.BufferData(GL_ARRAY_BUFFER, gl3state.vbo3Dsize, NULL, GL_STREAM_DRAW);
                //     curOffset = 0;
                // }

                // // as we make sure to use a previously unused part of the buffer,
                // // doing it unsynchronized should be safe..
                // GLbitfield accessBits = GL_MAP_WRITE_BIT | GL_MAP_INVALIDATE_RANGE_BIT | GL_MAP_UNSYNCHRONIZED_BIT;
                // void* data = glMapBufferRange(GL_ARRAY_BUFFER, curOffset, neededSize, accessBits);
                // memcpy(data, verts, neededSize);
                // glUnmapBuffer(GL_ARRAY_BUFFER);

                // glDrawArrays(drawMode, curOffset/sizeof(gl3_3D_vtx_t), numVerts);

                // gl3state.vbo3DcurOffset = curOffset + neededSize; // TODO: padding or sth needed?
        // #endif
            }
        }

        private void GL3_DrawNullModel(GL gl, in entity_t currententity)
        {
            Vector3 shadelight;

            if ((currententity.flags & QShared.RF_FULLBRIGHT) != 0)
            {
                shadelight = new Vector3(1.0f);
            }
            else
            {
                GL3_LightPoint(currententity, currententity.origin, out shadelight);
            }

            var origModelMat = gl3state.uni3DData.transModelMat4;
            GL3_RotateForEntity(gl, currententity);

            gl3state.uniCommonData.color = new Vector4D<float>( shadelight.X, shadelight.Y, shadelight.Z, 1 );
            GL3_UpdateUBOCommon(gl);

            GL3_UseProgram(gl, gl3state.si3DcolorOnly.shaderProgram);

            GL3_BindVAO(gl, gl3state.vao3D);
            GL3_BindVBO(gl, gl3state.vbo3D);

            gl3_3D_vtx_t[] vtxA = new gl3_3D_vtx_t[6]{
                new gl3_3D_vtx_t(){pos={X=0, Y=0, Z=-16}, texCoord={X=0,Y=0}, lmTexCoord={X=0,Y=0} },
                new gl3_3D_vtx_t(){pos={X=16 * MathF.Cos( 0 * MathF.PI / 2 ), Y=16 * MathF.Sin( 0 * MathF.PI / 2 ), Z=0}, texCoord={X=0,Y=0}, lmTexCoord={X=0,Y=0}},
                new gl3_3D_vtx_t(){pos={X=16 * MathF.Cos( 1 * MathF.PI / 2 ), Y=16 * MathF.Sin( 1 * MathF.PI / 2 ), Z=0}, texCoord={X=0,Y=0}, lmTexCoord={X=0,Y=0}},
                new gl3_3D_vtx_t(){pos={X=16 * MathF.Cos( 2 * MathF.PI / 2 ), Y=16 * MathF.Sin( 2 * MathF.PI / 2 ), Z=0}, texCoord={X=0,Y=0}, lmTexCoord={X=0,Y=0}},
                new gl3_3D_vtx_t(){pos={X=16 * MathF.Cos( 3 * MathF.PI / 2 ), Y=16 * MathF.Sin( 3 * MathF.PI / 2 ), Z=0}, texCoord={X=0,Y=0}, lmTexCoord={X=0,Y=0}},
                new gl3_3D_vtx_t(){pos={X=16 * MathF.Cos( 4 * MathF.PI / 2 ), Y=16 * MathF.Sin( 4 * MathF.PI / 2 ), Z=0}, texCoord={X=0,Y=0}, lmTexCoord={X=0,Y=0}}
            };

            GL3_BufferAndDraw3D(gl, vtxA, PrimitiveType.TriangleFan);

            gl3_3D_vtx_t[] vtxB = new gl3_3D_vtx_t[6]{
                new gl3_3D_vtx_t(){pos={X=0, Y=0, Z=16}, texCoord={X=0,Y=0}, lmTexCoord={X=0,Y=0}},
                vtxA[5], vtxA[4], vtxA[3], vtxA[2], vtxA[1]
            };

            GL3_BufferAndDraw3D(gl, vtxB, PrimitiveType.TriangleFan);

            gl3state.uni3DData.transModelMat4 = origModelMat;
            GL3_UpdateUBO3D(gl);
        }

        private struct part_vtx {
            public Vector3D<float> pos;
            public float size;
            public float dist;
            public Vector4D<float> color;
        }

        private unsafe void GL3_DrawParticles(GL gl)
        {
            // TODO: stereo
            //qboolean stereo_split_tb = ((gl_state.stereo_mode == STEREO_SPLIT_VERTICAL) && gl_state.camera_separation);
            //qboolean stereo_split_lr = ((gl_state.stereo_mode == STEREO_SPLIT_HORIZONTAL) && gl_state.camera_separation);

            //if (!(stereo_split_tb || stereo_split_lr))
            // {
                int numParticles = gl3_newrefdef.num_particles;
                float pointSize = gl3_particle_size!.Float * (float)gl3_newrefdef.height/480.0f;


                // Don't try to draw particles if there aren't any.
                if (numParticles == 0)
                {
                    return;
                }

                var buf = new part_vtx[numParticles];

                // TODO: viewOrg could be in UBO
                var viewOrg = gl3_newrefdef.vieworg;

                gl.DepthMask(false);
                gl.Enable(EnableCap.Blend);

        // #ifdef YQ2_GL3_GLES
        //         // the RPi4 GLES3 implementation doesn't draw particles if culling is
        //         // enabled (at least with GL_FRONT which seems to be default in q2?)
        //         glDisable(GL_CULL_FACE);
        // #else
                // GLES doesn't have this, maybe it's always enabled? (https://gamedev.stackexchange.com/a/15528 says it works)
                // luckily we don't use glPointSize() but set gl_PointSize in shader anyway
                gl.Enable(EnableCap.ProgramPointSize);
        // #endif

                GL3_UseProgram(gl, gl3state.siParticle.shaderProgram);

                for (int i = 0; i < numParticles; i++)
                {
                    ref var p = ref gl3_newrefdef.particles[i];
                    var color = d_8to24table [ p.color & 0xFF ];
                    ref var cur = ref buf[i];
                    var offset = viewOrg - p.origin;

                    cur.pos.X = p.origin.X;
                    cur.pos.Y = p.origin.Y;
                    cur.pos.Z = p.origin.Z;
                    cur.size = pointSize;
                    cur.dist = offset.Length();

                    cur.color.X = (float)((color >> 0) & 0xFF) / 255.0f;
                    cur.color.Y = (float)((color >> 8) & 0xFF) / 255.0f;
                    cur.color.Z = (float)((color >> 16) & 0xFF) / 255.0f;
                    cur.color.W = p.alpha;
                }

                GL3_BindVAO(gl, gl3state.vaoParticle);
                GL3_BindVBO(gl, gl3state.vboParticle);
                fixed (void *p = buf) {
                    gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(sizeof(part_vtx)*numParticles), p, BufferUsageARB.StreamDraw);
                }
                gl.DrawArrays(GLEnum.Points, 0, (uint)numParticles);

                gl.Disable(EnableCap.Blend);
                gl.DepthMask(true);
        // #ifdef YQ2_GL3_GLES
        //         if(r_cull->value != 0.0f)
        //             glEnable(GL_CULL_FACE);
        // #else
                gl.Disable(EnableCap.ProgramPointSize);
        // #endif

        //         YQ2_VLAFREE(buf);
        //     }
        }

        private void GL3_DrawEntitiesOnList(GL gl)
        {
            int i;

            if (!r_drawentities!.Bool)
            {
                return;
            }

            GL3_ResetShadowAliasModels();

            /* draw non-transparent first */
            for (i = 0; i < gl3_newrefdef.num_entities; i++)
            {
                ref var currententity = ref gl3_newrefdef.entities[i];

                if ((currententity.flags & QShared.RF_TRANSLUCENT) != 0)
                {
                    continue; /* solid */
                }

                if ((currententity.flags & QShared.RF_BEAM) != 0)
                {
                    Console.WriteLine("GL3_DrawBeam");
                    // GL3_DrawBeam(currententity);
                }
                else
                {
                    if (currententity.model == null)
                    {
                        GL3_DrawNullModel(gl, currententity);
                        continue;
                    }
                    var currentmodel = (gl3model_t)currententity.model;

                    switch (currentmodel.type)
                    {
                        case modtype_t.mod_alias:
                            GL3_DrawAliasModel(gl, ref currententity);
                            break;
                        case modtype_t.mod_brush:
                            GL3_DrawBrushModel(gl, ref currententity, (gl3brushmodel_t)currentmodel);
                            break;
                        case modtype_t.mod_sprite:
                            Console.WriteLine("GL3_DrawSpriteModel");
                            // GL3_DrawSpriteModel(currententity, currentmodel);
                            break;
                        default:
                            ri.Sys_Error(QShared.ERR_DROP, "Bad modeltype");
                            break;
                    }
                }
            }

            /* draw transparent entities
            we could sort these if it ever
            becomes a problem... */
            gl.DepthMask(false);

            for (i = 0; i < gl3_newrefdef.num_entities; i++)
            {
                ref var currententity = ref gl3_newrefdef.entities[i];

                if ((currententity.flags & QShared.RF_TRANSLUCENT) == 0)
                {
                    continue; /* solid */
                }

                if ((currententity.flags & QShared.RF_BEAM) != 0)
                {
                    Console.WriteLine("GL3_DrawBeam");
                    // GL3_DrawBeam(currententity);
                }
                else
                {
                    if (currententity.model == null)
                    {
                        GL3_DrawNullModel(gl, currententity);
                        continue;
                    }
                    var currentmodel = (gl3model_t)currententity.model;

                    switch (currentmodel.type)
                    {
                        case modtype_t.mod_alias:
                            GL3_DrawAliasModel(gl, ref currententity);
                            break;
                        case modtype_t.mod_brush:
                            GL3_DrawBrushModel(gl, ref currententity, (gl3brushmodel_t)currentmodel);
                            break;
                        case modtype_t.mod_sprite:
                            Console.WriteLine("GL3_DrawSpriteModel");
                            // GL3_DrawSpriteModel(currententity, currentmodel);
                            break;
                        default:
                            ri.Sys_Error(QShared.ERR_DROP, "Bad modeltype");
                            break;
                    }
                }
            }

            // GL3_DrawAliasShadows();

            gl.DepthMask(true); /* back to writing */

        }

        private byte SignbitsForPlane(in QShared.cplane_t outd)
        {
            /* for fast box on planeside test */
            byte bits = 0;

            if (outd.normal.X < 0) {
                bits |= 1;
            }
            if (outd.normal.Y < 0) {
                bits |= 2;
            }
            if (outd.normal.Z < 0) {
                bits |= 4;
            }

            return bits;
        }

        private void SetFrustum()
        {
            int i;

            for (i = 0; i < 4; i++)
            {
                frustum[i] = new QShared.cplane_t();
            }


            /* rotate VPN right by FOV_X/2 degrees */
            frustum[0].normal = QShared.RotatePointAroundVector(vup, vpn, -(90 - gl3_newrefdef.fov_x / 2));
            /* rotate VPN left by FOV_X/2 degrees */
            frustum[1].normal = QShared.RotatePointAroundVector(vup, vpn, 90 - gl3_newrefdef.fov_x / 2);
            /* rotate VPN up by FOV_X/2 degrees */
            frustum[2].normal = QShared.RotatePointAroundVector(vright, vpn, 90 - gl3_newrefdef.fov_y / 2);
            /* rotate VPN down by FOV_X/2 degrees */
            frustum[3].normal = QShared.RotatePointAroundVector(vright, vpn, -(90 - gl3_newrefdef.fov_y / 2));

            for (i = 0; i < 4; i++)
            {
                frustum[i].type = (byte)QCommon.PLANE_ANYZ;
                frustum[i].dist = Vector3.Dot(gl3_origin, frustum[i].normal);
                frustum[i].signbits = SignbitsForPlane(frustum[i]);
            }
        }

        private void SetupFrame(GL gl)
        {
            // int i;
            // mleaf_t *leaf;

            gl3_framecount++;

            /* build the transformation matrix for the given view angles */
            gl3_origin = gl3_newrefdef.vieworg;

            QShared.AngleVectors(gl3_newrefdef.viewangles, out vpn, out vright, out vup);

            /* current viewcluster */
            if ((gl3_newrefdef.rdflags & QShared.RDF_NOWORLDMODEL) == 0)
            {
                gl3_oldviewcluster = gl3_viewcluster;
                gl3_oldviewcluster2 = gl3_viewcluster2;
                var leaf = GL3_Mod_PointInLeaf(gl, gl3_origin, gl3_worldmodel);
                gl3_viewcluster = gl3_viewcluster2 = leaf.cluster;

                /* check above and below so crossing solid water doesn't draw wrong */
                if (leaf.contents == 0)
                {
                    /* look down a bit */
                    var temp = gl3_origin;

                    temp.Z -= 16;
                    leaf = GL3_Mod_PointInLeaf(gl, temp, gl3_worldmodel);

                    if ((leaf.contents & QCommon.CONTENTS_SOLID) == 0 &&
                        (leaf.cluster != gl3_viewcluster2))
                    {
                        gl3_viewcluster2 = leaf.cluster;
                    }
                }
                else
                {
                    /* look up a bit */
                    var temp = gl3_origin;

                    temp.Z += 16;
                    leaf = GL3_Mod_PointInLeaf(gl, temp, gl3_worldmodel);

                    if ((leaf.contents & QCommon.CONTENTS_SOLID) == 0 &&
                        (leaf.cluster != gl3_viewcluster2))
                    {
                        gl3_viewcluster2 = leaf.cluster;
                    }
                }
            }

            v_blend = gl3_newrefdef.blend;

            c_brush_polys = 0;
            c_alias_polys = 0;

            /* clear out the portion of the screen that the NOWORLDMODEL defines */
            if ((gl3_newrefdef.rdflags & QShared.RDF_NOWORLDMODEL) != 0)
            {
            //     glEnable(GL_SCISSOR_TEST);
                gl.ClearColor(0.3f, 0.3f, 0.3f, 1);
            //     glScissor(gl3_newrefdef.x,
            //             vid.height - gl3_newrefdef.height - gl3_newrefdef.y,
            //             gl3_newrefdef.width, gl3_newrefdef.height);
            //     glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
                gl.ClearColor(1, 0, 0.5f, 0.5f);
            //     glDisable(GL_SCISSOR_TEST);
            }
        }

        private void GL3_SetGL2D(GL gl)
        {
            int x = 0;
            uint w = (uint)vid.width;
            int y = 0;
            uint h = (uint)vid.height;

        // #if 0 // TODO: stereo
        //     /* set 2D virtual screen size */
        //     qboolean drawing_left_eye = gl_state.camera_separation < 0;
        //     qboolean stereo_split_tb = ((gl_state.stereo_mode == STEREO_SPLIT_VERTICAL) && gl_state.camera_separation);
        //     qboolean stereo_split_lr = ((gl_state.stereo_mode == STEREO_SPLIT_HORIZONTAL) && gl_state.camera_separation);

        //     if(stereo_split_lr) {
        //         w =  w / 2;
        //         x = drawing_left_eye ? 0 : w;
        //     }

        //     if(stereo_split_tb) {
        //         h =  h / 2;
        //         y = drawing_left_eye ? h : 0;
        //     }
        // #endif // 0

            gl.Viewport(x, y, w, h);

            var transMatr = Matrix4X4.CreateOrthographicOffCenter<float>(0, vid.width, vid.height, 0, -99999f, 99999f);
            // hmm_mat4 transMatr = HMM_Orthographic(0, vid.width, vid.height, 0, -99999, 99999);
            // var transMatr = Matrix4X4.CreateOrthographic<float>(vid.width, vid.height, -99999f, 99999f);

            gl3state.uni2DData.transMat4 = transMatr;

            GL3_UpdateUBO2D(gl);

            gl.Disable(EnableCap.DepthTest);
            gl.Disable(EnableCap.CullFace);
            gl.Disable(EnableCap.Blend);
        }
        
        // equivalent to R_x * R_y * R_z where R_x is the trans matrix for rotating around X axis for aroundXdeg
        private Matrix4X4<float> rotAroundAxisXYZ(float aroundXdeg, float aroundYdeg, float aroundZdeg)
        {
            float alpha = QShared.ToRadians(aroundXdeg);
            float beta = QShared.ToRadians(aroundYdeg);
            float gamma = QShared.ToRadians(aroundZdeg);

            float sinA = MathF.Sin(alpha);
            float cosA = MathF.Cos(alpha);
            float sinB = MathF.Sin(beta);
            float cosB = MathF.Cos(beta);
            float sinG = MathF.Sin(gamma);
            float cosG = MathF.Cos(gamma);

            return new Matrix4X4<float>(
                cosB*cosG,  sinA*sinB*cosG + cosA*sinG, -cosA*sinB*cosG + sinA*sinG, 0, // first *column*
                -cosB*sinG, -sinA*sinB*sinG + cosA*cosG,  cosA*sinB*sinG + sinA*cosG, 0,
                sinB,      -sinA*cosB,                   cosA*cosB,                  0,
                0,          0,                           0,                          1
            );
        }

        // equivalent to R_MYgluPerspective() but returning a matrix instead of setting internal OpenGL state
        private Matrix4X4<float> GL3_MYgluPerspective(double fovy, double aspect, double zNear, double zFar)
        {
            // calculation of left, right, bottom, top is from R_MYgluPerspective() of old gl backend
            // which seems to be slightly different from the real gluPerspective()
            // and thus also from HMM_Perspective()
            double top = zNear * Math.Tan(fovy * Math.PI / 360.0);
            double bottom = -top;

            double left = bottom * aspect;
            double right = top * aspect;

            // TODO:  stereo stuff
            // left += - gl1_stereo_convergence->value * (2 * gl_state.camera_separation) / zNear;
            // right += - gl1_stereo_convergence->value * (2 * gl_state.camera_separation) / zNear;

            // the following emulates glFrustum(left, right, bottom, top, zNear, zFar)
            // see https://www.khronos.org/registry/OpenGL-Refpages/gl2.1/xhtml/glFrustum.xml
            //  or http://docs.gl/gl2/glFrustum#description (looks better in non-Firefox browsers)
            float A = (float)((right+left)/(right-left));
            float B = (float)((top+bottom)/(top-bottom));
            float C = (float)(-(zFar+zNear)/(zFar-zNear));
            float D = (float)(-(2.0*zFar*zNear)/(zFar-zNear));

            return new Matrix4X4<float>(
                (float)((2.0*zNear)/(right-left)), 0, 0, 0, // first *column*
                0, (float)((2.0*zNear)/(top-bottom)), 0, 0,
                A, B, C, -1.0f,
                0, 0, D, 0
            );
        }

        private Matrix4X4<float> HMM_MultiplyMat4(in Matrix4X4<float> Left, in Matrix4X4<float> Right)
        {
            return new Matrix4X4<float>(
                // Column1
                Left.M11 * Right.M11 + Left.M21 * Right.M12 + Left.M31 * Right.M13 + Left.M41 * Right.M14,
                Left.M12 * Right.M11 + Left.M22 * Right.M12 + Left.M32 * Right.M13 + Left.M42 * Right.M14,
                Left.M13 * Right.M11 + Left.M23 * Right.M12 + Left.M33 * Right.M13 + Left.M43 * Right.M14,
                Left.M14 * Right.M11 + Left.M24 * Right.M12 + Left.M34 * Right.M13 + Left.M44 * Right.M14,
                // Column2
                Left.M11 * Right.M21 + Left.M21 * Right.M22 + Left.M31 * Right.M23 + Left.M41 * Right.M24,
                Left.M12 * Right.M21 + Left.M22 * Right.M22 + Left.M32 * Right.M23 + Left.M42 * Right.M24,
                Left.M13 * Right.M21 + Left.M23 * Right.M22 + Left.M33 * Right.M23 + Left.M43 * Right.M24,
                Left.M14 * Right.M21 + Left.M24 * Right.M22 + Left.M34 * Right.M23 + Left.M44 * Right.M24,
                // Column3
                Left.M11 * Right.M31 + Left.M21 * Right.M32 + Left.M31 * Right.M33 + Left.M41 * Right.M34,
                Left.M12 * Right.M31 + Left.M22 * Right.M32 + Left.M32 * Right.M33 + Left.M42 * Right.M34,
                Left.M13 * Right.M31 + Left.M23 * Right.M32 + Left.M33 * Right.M33 + Left.M43 * Right.M34,
                Left.M14 * Right.M31 + Left.M24 * Right.M32 + Left.M34 * Right.M33 + Left.M44 * Right.M34,
                // Column4
                Left.M11 * Right.M41 + Left.M21 * Right.M42 + Left.M31 * Right.M43 + Left.M41 * Right.M44,
                Left.M12 * Right.M41 + Left.M22 * Right.M42 + Left.M32 * Right.M43 + Left.M42 * Right.M44,
                Left.M13 * Right.M41 + Left.M23 * Right.M42 + Left.M33 * Right.M43 + Left.M43 * Right.M44,
                Left.M14 * Right.M41 + Left.M24 * Right.M42 + Left.M34 * Right.M43 + Left.M44 * Right.M44
            );
        }

        private Matrix4X4<float> HMM_Rotate(float Angle, in Vector3D<float> _Axis)
        {
            var Result = new Matrix4X4<float>();
            
            var Axis = Vector3D.Normalize(_Axis);
            
            float SinTheta = MathF.Sin(QShared.ToRadians(Angle));
            float CosTheta = MathF.Cos(QShared.ToRadians(Angle));
            float CosValue = 1.0f - CosTheta;
            
            Result.M11 = (Axis.X * Axis.X * CosValue) + CosTheta;
            Result.M12 = (Axis.X * Axis.Y * CosValue) + (Axis.Z * SinTheta);
            Result.M13 = (Axis.X * Axis.Z * CosValue) - (Axis.Y * SinTheta);
            
            Result.M21 = (Axis.Y * Axis.X * CosValue) - (Axis.Z * SinTheta);
            Result.M22 = (Axis.Y * Axis.Y * CosValue) + CosTheta;
            Result.M23 = (Axis.Y * Axis.Z * CosValue) + (Axis.X * SinTheta);
            
            Result.M31 = (Axis.Z * Axis.X * CosValue) + (Axis.Y * SinTheta);
            Result.M32 = (Axis.Z * Axis.Y * CosValue) - (Axis.X * SinTheta);
            Result.M33 = (Axis.Z * Axis.Z * CosValue) + CosTheta;

            Result.M44 = 1;
            
            return Result;
        }

        private unsafe void SetupGL(GL gl)
        {
            /* set up viewport */
            int x = (int)Math.Floor(gl3_newrefdef.x * (double)vid.width / (double)vid.width);
            int x2 = (int)Math.Ceiling((gl3_newrefdef.x + gl3_newrefdef.width) * (double)vid.width / (double)vid.width);
            int y = (int)Math.Floor(vid.height - gl3_newrefdef.y * (double)vid.height / (double)vid.height);
            int y2 = (int)Math.Ceiling(vid.height - (gl3_newrefdef.y + gl3_newrefdef.height) * (double)vid.height / (double)vid.height);

            uint w = (uint)(x2 - x);
            uint h = (uint)(y - y2);

        // #if 0 // TODO: stereo stuff
        //     qboolean drawing_left_eye = gl_state.camera_separation < 0;
        //     qboolean stereo_split_tb = ((gl_state.stereo_mode == STEREO_SPLIT_VERTICAL) && gl_state.camera_separation);
        //     qboolean stereo_split_lr = ((gl_state.stereo_mode == STEREO_SPLIT_HORIZONTAL) && gl_state.camera_separation);

        //     if(stereo_split_lr) {
        //         w = w / 2;
        //         x = drawing_left_eye ? (x / 2) : (x + vid.width) / 2;
        //     }

        //     if(stereo_split_tb) {
        //         h = h / 2;
        //         y2 = drawing_left_eye ? (y2 + vid.height) / 2 : (y2 / 2);
        //     }
        // #endif // 0

            // set up the FBO accordingly, but only if actually rendering the world
            // (=> don't use FBO when rendering the playermodel in the player menu)
            // also, only do this when under water, because this has a noticeable overhead on some systems
            if ((gl3_usefbo?.Bool ?? false) && gl3state.ppFBO != 0
                && (gl3_newrefdef.rdflags & (QShared.RDF_NOWORLDMODEL|QShared.RDF_UNDERWATER)) == QShared.RDF_UNDERWATER)
            {
                gl.BindFramebuffer(FramebufferTarget.Framebuffer, gl3state.ppFBO);
                gl3state.ppFBObound = true;
                if(gl3state.ppFBtex == 0)
                {
                    gl3state.ppFBtexWidth = -1; // make sure we generate the texture storage below
                    gl3state.ppFBtex = gl.GenTexture();
                }

                if(gl3state.ppFBrbo == 0)
                {
                    gl3state.ppFBtexWidth = -1; // make sure we generate the RBO storage below
                    gl3state.ppFBrbo = gl.GenRenderbuffer();
                }

                // even if the FBO already has a texture and RBO, the viewport size
                // might have changed so they need to be regenerated with the correct sizes
                if(gl3state.ppFBtexWidth != w || gl3state.ppFBtexHeight != h)
                {
                    gl3state.ppFBtexWidth = (int)w;
                    gl3state.ppFBtexHeight = (int)h;
                    GL3_Bind(gl, gl3state.ppFBtex);
                    // create texture for FBO with size of the viewport
                    gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, w, h, 0, PixelFormat.Rgb, PixelType.UnsignedByte, null);
                    gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear );
                    gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
                    GL3_Bind(gl, 0);
                    // attach it to currently bound FBO
                    gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, gl3state.ppFBtex, 0);

                    // also create a renderbuffer object so the FBO has a stencil- and depth-buffer
                    gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, gl3state.ppFBrbo);
                    gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, w, h);
                    gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
                    // attach it to the FBO
                    gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment,
                                            RenderbufferTarget.Renderbuffer, gl3state.ppFBrbo);

                    var fbState = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                    if(fbState != GLEnum.FramebufferComplete)
                    {
                        R_Printf(QShared.PRINT_ALL, $"GL3 SetupGL(): WARNING: FBO is not complete, status = 0x{fbState.ToString("X")}\n");
                        gl3state.ppFBtexWidth = -1; // to try again next frame; TODO: maybe give up?
                        gl3state.ppFBObound = false;
                        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                    }
                }

                GL3_Clear(gl); // clear the FBO that's bound now

                gl.Viewport(0, 0, w, h); // this will be moved to the center later, so no x/y offset
            }
            else // rendering directly (not to FBO for postprocessing)
            {
                gl.Viewport(x, y2, w, h);
            }

            /* set up projection matrix (eye coordinates -> clip coordinates) */
            {
                var screenaspect = (float)gl3_newrefdef.width / gl3_newrefdef.height;
                float dist = (r_farsee?.Bool ?? false) ? 4096.0f : 8192.0f;
                gl3state.projMat3D = GL3_MYgluPerspective(gl3_newrefdef.fov_y, screenaspect, 4, dist);
            }

            gl.CullFace(CullFaceMode.Front);

            /* set up view matrix (world coordinates -> eye coordinates) */
            {
                // first put Z axis going up
                Matrix4X4<float> viewMat = new Matrix4X4<float>(
                    0, 0, -1, 0, // first *column* (the matrix is column-major)
                    -1, 0,  0, 0,
                    0, 1,  0, 0,
                    0, 0,  0, 1
                );

                // now rotate by view angles
                var rotMat = rotAroundAxisXYZ(-gl3_newrefdef.viewangles.Z, -gl3_newrefdef.viewangles.X, -gl3_newrefdef.viewangles.Y);

                viewMat = HMM_MultiplyMat4(viewMat, rotMat);

                // .. and apply translation for current position
                var trans = new Vector3D<float>(-gl3_newrefdef.vieworg.X, -gl3_newrefdef.vieworg.Y, -gl3_newrefdef.vieworg.Z);
                viewMat = HMM_MultiplyMat4(viewMat, Matrix4X4.CreateTranslation(trans));

                gl3state.viewMat3D = viewMat;
            }

            // just use one projection-view-matrix (premultiplied here)
            // so we have one less mat4 multiplication in the 3D shaders
            gl3state.uni3DData.transProjViewMat4 = HMM_MultiplyMat4(gl3state.projMat3D, gl3state.viewMat3D);

            gl3state.uni3DData.transModelMat4 = gl3_identityMat4;

            gl3state.uni3DData.time = gl3_newrefdef.time;

            GL3_UpdateUBO3D(gl);

            /* set drawing parms */
            if (gl_cull?.Bool ?? false)
            {
                gl.Enable(EnableCap.CullFace);
            }
            else
            {
                gl.Disable(EnableCap.CullFace);
            }

            gl.Enable(EnableCap.DepthTest);
        }

        /*
        * gl3_newrefdef must be set before the first call
        */
        public void GL3_RenderView(GL gl, in refdef_t fd)
        {
        // #if 0 // TODO: keep stereo stuff?
        //     if ((gl_state.stereo_mode != STEREO_MODE_NONE) && gl_state.camera_separation) {

        //         qboolean drawing_left_eye = gl_state.camera_separation < 0;
        //         switch (gl_state.stereo_mode) {
        //             case STEREO_MODE_ANAGLYPH:
        //                 {

        //                     // Work out the colour for each eye.
        //                     int anaglyph_colours[] = { 0x4, 0x3 }; // Left = red, right = cyan.

        //                     if (strlen(gl1_stereo_anaglyph_colors->string) == 2) {
        //                         int eye, colour, missing_bits;
        //                         // Decode the colour name from its character.
        //                         for (eye = 0; eye < 2; ++eye) {
        //                             colour = 0;
        //                             switch (toupper(gl1_stereo_anaglyph_colors->string[eye])) {
        //                                 case 'B': ++colour; // 001 Blue
        //                                 case 'G': ++colour; // 010 Green
        //                                 case 'C': ++colour; // 011 Cyan
        //                                 case 'R': ++colour; // 100 Red
        //                                 case 'M': ++colour; // 101 Magenta
        //                                 case 'Y': ++colour; // 110 Yellow
        //                                     anaglyph_colours[eye] = colour;
        //                                     break;
        //                             }
        //                         }
        //                         // Fill in any missing bits.
        //                         missing_bits = ~(anaglyph_colours[0] | anaglyph_colours[1]) & 0x3;
        //                         for (eye = 0; eye < 2; ++eye) {
        //                             anaglyph_colours[eye] |= missing_bits;
        //                         }
        //                     }

        //                     // Set the current colour.
        //                     glColorMask(
        //                         !!(anaglyph_colours[drawing_left_eye] & 0x4),
        //                         !!(anaglyph_colours[drawing_left_eye] & 0x2),
        //                         !!(anaglyph_colours[drawing_left_eye] & 0x1),
        //                         GL_TRUE
        //                     );
        //                 }
        //                 break;
        //             case STEREO_MODE_ROW_INTERLEAVED:
        //             case STEREO_MODE_COLUMN_INTERLEAVED:
        //             case STEREO_MODE_PIXEL_INTERLEAVED:
        //                 {
        //                     qboolean flip_eyes = true;
        //                     int client_x, client_y;

        //                     //GLimp_GetClientAreaOffset(&client_x, &client_y);
        //                     client_x = 0;
        //                     client_y = 0;

        //                     GL3_SetGL2D();

        //                     glEnable(GL_STENCIL_TEST);
        //                     glStencilMask(GL_TRUE);
        //                     glColorMask(GL_FALSE, GL_FALSE, GL_FALSE, GL_FALSE);

        //                     glStencilOp(GL_REPLACE, GL_KEEP, GL_KEEP);
        //                     glStencilFunc(GL_NEVER, 0, 1);

        //                     glBegin(GL_QUADS);
        //                     {
        //                         glVertex2i(0, 0);
        //                         glVertex2i(vid.width, 0);
        //                         glVertex2i(vid.width, vid.height);
        //                         glVertex2i(0, vid.height);
        //                     }
        //                     glEnd();

        //                     glStencilOp(GL_INVERT, GL_KEEP, GL_KEEP);
        //                     glStencilFunc(GL_NEVER, 1, 1);

        //                     glBegin(GL_LINES);
        //                     {
        //                         if (gl_state.stereo_mode == STEREO_MODE_ROW_INTERLEAVED || gl_state.stereo_mode == STEREO_MODE_PIXEL_INTERLEAVED) {
        //                             int y;
        //                             for (y = 0; y <= vid.height; y += 2) {
        //                                 glVertex2f(0, y - 0.5f);
        //                                 glVertex2f(vid.width, y - 0.5f);
        //                             }
        //                             flip_eyes ^= (client_y & 1);
        //                         }

        //                         if (gl_state.stereo_mode == STEREO_MODE_COLUMN_INTERLEAVED || gl_state.stereo_mode == STEREO_MODE_PIXEL_INTERLEAVED) {
        //                             int x;
        //                             for (x = 0; x <= vid.width; x += 2) {
        //                                 glVertex2f(x - 0.5f, 0);
        //                                 glVertex2f(x - 0.5f, vid.height);
        //                             }
        //                             flip_eyes ^= (client_x & 1);
        //                         }
        //                     }
        //                     glEnd();

        //                     glStencilMask(GL_FALSE);
        //                     glColorMask(GL_TRUE, GL_TRUE, GL_TRUE, GL_TRUE);

        //                     glStencilFunc(GL_EQUAL, drawing_left_eye ^ flip_eyes, 1);
        //                     glStencilOp(GL_KEEP, GL_KEEP, GL_KEEP);
        //                 }
        //                 break;
        //             default:
        //                 break;
        //         }
        //     }
        // #endif // 0 (stereo stuff)

            if (r_norefresh?.Bool ?? false)
            {
                return;
            }

            gl3_newrefdef = fd;

            if (gl3_worldmodel == null && (gl3_newrefdef.rdflags & QShared.RDF_NOWORLDMODEL) == 0)
            {
                ri.Sys_Error(QShared.ERR_DROP, "R_RenderView: NULL worldmodel");
            }

            if (r_speeds?.Bool ?? false)
            {
                c_brush_polys = 0;
                c_alias_polys = 0;
            }

            GL3_PushDlights(gl);

            if (gl_finish?.Bool ?? false)
            {
                gl.Finish();
            }

            SetupFrame(gl);

            SetFrustum();

            SetupGL(gl);

            GL3_MarkLeaves(); /* done here so we know if we're in water */

            GL3_DrawWorld(gl);

            GL3_DrawEntitiesOnList(gl);

            // kick the silly gl1_flashblend poly lights
            // GL3_RenderDlights();

            GL3_DrawParticles(gl);

            GL3_DrawAlphaSurfaces(gl);

            // Note: R_Flash() is now GL3_Draw_Flash() and called from GL3_RenderFrame()

            if (r_speeds?.Bool ?? false)
            {
                R_Printf(QShared.PRINT_ALL, $"{c_brush_polys} wpoly {c_alias_polys} epoly {c_visible_textures} tex {c_visible_lightmaps} lmaps\n");
            }

        // #if 0 // TODO: stereo stuff
        //     switch (gl_state.stereo_mode) {
        //         case STEREO_MODE_NONE:
        //             break;
        //         case STEREO_MODE_ANAGLYPH:
        //             glColorMask(GL_TRUE, GL_TRUE, GL_TRUE, GL_TRUE);
        //             break;
        //         case STEREO_MODE_ROW_INTERLEAVED:
        //         case STEREO_MODE_COLUMN_INTERLEAVED:
        //         case STEREO_MODE_PIXEL_INTERLEAVED:
        //             glDisable(GL_STENCIL_TEST);
        //             break;
        //         default:
        //             break;
        //     }
        // #endif // 0
        }


        public void RenderFrame (Silk.NET.Windowing.IWindow window, in refdef_t fd)
        {
            var gl = GL.GetApi(window);
            GL3_RenderView(gl, fd);
            // GL3_SetLightLevel(NULL);
            var usedFBO = gl3state.ppFBObound; // if it was/is used this frame
            if(usedFBO)
            {
                gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0); // now render to default framebuffer
                gl3state.ppFBObound = false;
            }
            GL3_SetGL2D(gl);

            int x = (vid.width - gl3_newrefdef.width)/2;
            int y = (vid.height - gl3_newrefdef.height)/2;
            if (usedFBO)
            {
                // if we're actually drawing the world and using an FBO, render the FBO's texture
                Console.WriteLine("GL3_DrawFrameBufferObject");
                // GL3_DrawFrameBufferObject(gl, x, y, gl3_newrefdef.width, gl3_newrefdef.height, gl3state.ppFBtex, v_blend);
            }
            // else if(v_blend[3] != 0.0f)
            // {
            //     GL3_Draw_Flash(v_blend, x, y, gl3_newrefdef.width, gl3_newrefdef.height);
            // }
        }

        private void GL3_Clear(GL gl)
        {
            // Check whether the stencil buffer needs clearing, and do so if need be.
            ClearBufferMask stencilFlags = 0;
        // #if 0 // TODO: stereo stuff
        //     if (gl3state.stereo_mode >= STEREO_MODE_ROW_INTERLEAVED && gl_state.stereo_mode <= STEREO_MODE_PIXEL_INTERLEAVED) {
        //         glClearStencil(0);
        //         stencilFlags |= GL_STENCIL_BUFFER_BIT;
        //     }
        // #endif // 0


            if (r_clear?.Bool ?? false)
            {
                gl.Clear(ClearBufferMask.ColorBufferBit | stencilFlags | ClearBufferMask.DepthBufferBit);
            }
            else
            {
                gl.Clear(ClearBufferMask.DepthBufferBit | stencilFlags);
            }
            gl3depthmin = 0;
            gl3depthmax = 1;
            gl.DepthFunc(DepthFunction.Lequal);

            gl.DepthRange(gl3depthmin, gl3depthmax);

            if (gl_zfix?.Bool ?? false)
            {
                if (gl3depthmax > gl3depthmin)
                {
                    gl.PolygonOffset(0.05f, 1);
                }
                else
                {
                    gl.PolygonOffset(-0.05f, -1);
                }
            }

            /* stencilbuffer shadows */
            if (gl_shadows?.Bool ?? false && gl3config.stencil)
            {
                gl.ClearStencil(1);
                gl.Clear(ClearBufferMask.StencilBufferBit);
            }
        }

        public void BeginFrame(IWindow window, float camera_separation)
        {
            var gl = GL.GetApi(window);

        // #if 0 // TODO: stereo stuff
        //     gl_state.camera_separation = camera_separation;
        //     // force a vid_restart if gl1_stereo has been modified.
        //     if ( gl_state.stereo_mode != gl1_stereo->value ) {
        //         // If we've gone from one mode to another with the same special buffer requirements there's no need to restart.
        //         if ( GL_GetSpecialBufferModeForStereoMode( gl_state.stereo_mode ) == GL_GetSpecialBufferModeForStereoMode( gl1_stereo->value )  ) {
        //             gl_state.stereo_mode = gl1_stereo->value;
        //         }
        //         else
        //         {
        //             R_Printf(PRINT_ALL, "stereo supermode changed, restarting video!\n");
        //             vid_fullscreen->modified = true;
        //         }
        //     }
        // #endif // 0

            if (vid_gamma!.modified || gl3_intensity!.modified || gl3_intensity_2D!.modified)
            {
                vid_gamma!.modified = false;
                gl3_intensity!.modified = false;
                gl3_intensity_2D!.modified = false;

                gl3state.uniCommonData.gamma = 1.0f/vid_gamma!.Float;
                gl3state.uniCommonData.intensity = gl3_intensity!.Float;
                gl3state.uniCommonData.intensity2D = gl3_intensity_2D!.Float;
                GL3_UpdateUBOCommon(gl);
            }

            // in GL3, overbrightbits can have any positive value
            if (gl3_overbrightbits?.modified ?? false)
            {
                gl3_overbrightbits!.modified = false;

                if(gl3_overbrightbits!.Float < 0.0f)
                {
                    // ri.Cvar_Set("gl3_overbrightbits", "0");
                }

                gl3state.uni3DData.overbrightbits = (gl3_overbrightbits!.Float <= 0.0f) ? 1.0f : gl3_overbrightbits!.Float;
                GL3_UpdateUBO3D(gl);
            }

            if(gl3_particle_fade_factor?.modified ?? false)
            {
                gl3_particle_fade_factor!.modified = false;
                gl3state.uni3DData.particleFadeFactor = gl3_particle_fade_factor!.Float;
                GL3_UpdateUBO3D(gl);
            }

        //     if(gl3_particle_square->modified || gl3_colorlight->modified)
        //     {
        //         gl3_particle_square->modified = false;
        //         gl3_colorlight->modified = false;
        //         GL3_RecreateShaders();
        //     }


            /* go into 2D mode */

            GL3_SetGL2D(gl);

            /* draw buffer stuff */
            if (gl_drawbuffer?.modified ?? false)
            {
                gl_drawbuffer!.modified = false;


        // #ifdef YQ2_GL3_GLES
        //         // OpenGL ES3 only supports GL_NONE, GL_BACK and GL_COLOR_ATTACHMENT*
        //         // so this doesn't make sense here, see https://docs.gl/es3/glDrawBuffers
        //         R_Printf(PRINT_ALL, "NOTE: gl_drawbuffer not supported by OpenGL ES!\n");
        // #else // Desktop GL
        //         // TODO: stereo stuff
        //         //if ((gl3state.camera_separation == 0) || gl3state.stereo_mode != STEREO_MODE_OPENGL)
        //         {
                    DrawBufferMode drawBuffer = DrawBufferMode.Back;
                    if (gl_drawbuffer!.str.Equals("GL_FRONT"))
                    {
                        drawBuffer = DrawBufferMode.Front;
                    }
                    gl.DrawBuffer(drawBuffer);
        //         }
        // #endif
            }

            /* texturemode stuff */
            if ((gl_texturemode?.modified ?? false) || (gl_anisotropic?.modified ?? false) ||
                (gl_nolerp_list?.modified ?? false) || (r_lerp_list?.modified ?? false) ||
                (r_2D_unfiltered?.modified ?? false) || (r_videos_unfiltered?.modified ?? false))
            {
                GL3_TextureMode(gl, gl_texturemode!.str);
                gl_texturemode!.modified = false;
                gl_anisotropic!.modified = false;
                gl_nolerp_list!.modified = false;
                r_lerp_list!.modified = false;
                r_2D_unfiltered!.modified = false;
                r_videos_unfiltered!.modified = false;
            }

        //     if (r_vsync->modified)
        //     {
        //         r_vsync->modified = false;
        //         GL3_SetVsync();
        //     }

            /* clear screen if desired */
            GL3_Clear(gl);            
        }

        /*
        * Swaps the buffers and shows the next frame.
        */
        public unsafe void EndFrame (IWindow window)
        {
            var gl = GL.GetApi(window);
            if(gl3config.useBigVBO)
            {
                // I think this is a good point to orphan the VBO and get a fresh one
                GL3_BindVAO(gl, gl3state.vao3D);
                GL3_BindVBO(gl, gl3state.vbo3D);
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)gl3state.vbo3Dsize, null, BufferUsageARB.StreamDraw);
                gl3state.vbo3DcurOffset = 0;
            }

            // SDL_GL_SwapWindow(window);
            // window.GLContext?.SwapBuffers();
            // window.SwapBuffers();
        }


        private void R_Printf(int level, string msg)
        {
            ri.Com_VPrintf(level, msg);
        }

        /*
        * Callback function for debug output.
        */
        private static void DebugCallback(GLEnum source, GLEnum type, int id, GLEnum severity, int length,
                    nint message, nint userParam)
        {
            string severityStr = "Severity: Unknown";
            switch (severity)
            {
                case GLEnum.DebugSeverityHigh:
                    severityStr = "Severity: High";
                    break;
                case GLEnum.DebugSeverityLow:
                    severityStr = "Severity: Low";
                    break;
                case GLEnum.DebugSeverityMedium:
                    severityStr = "Severity: Medium";
                    break;
                case GLEnum.DebugSeverityNotification:
                    severityStr = "Severity: Notification";
                    break;
            }

	        string sourceStr = "Source: Unknown";
            switch (source)
            {
                case GLEnum.DebugSourceApi:
                    sourceStr = "Source: API";
                    break;
                case GLEnum.DebugSourceWindowSystem:
                    sourceStr = "Source: WINDOW_SYSTEM";
                    break;
            }
	        string typeStr = "Type: Unknown";
            switch (type)
            {
                case GLEnum.DebugTypeError:
                    typeStr = "Type: ERROR";
                    break;
                case GLEnum.DebugTypeDeprecatedBehavior:
                    typeStr = "Type: DEPRECATED_BEHAVIOR";
                    break;
                case GLEnum.DebugTypeUndefinedBehavior:
                    typeStr = "Type: UNDEFINED_BEHAVIOR";
                    break;
                case GLEnum.DebugTypePortability:
                    typeStr = "Type: PORTABILITY";
                    break;
                case GLEnum.DebugTypePerformance:
                    typeStr = "Type: PERFORMANCE";
                    break;
                case GLEnum.DebugTypeOther:
                    typeStr = "Type: OTHER";
                    break;
                default:
                    typeStr = $"Type: Unknown({type})";
                    break;
            }

            Console.WriteLine($"DebugCallback {severityStr} {sourceStr} {typeStr} {System.Runtime.InteropServices.Marshal.PtrToStringAnsi(message)}");
        }

    }
}
