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
 * Misc OpenGL3 refresher functions
 *
 * =======================================================================
 */
using Silk.NET.OpenGL;
using Silk.NET.Maths;

namespace Quake2 {

    partial class QRefGl3
    {

        private gl3image_t? gl3_particletexture;
        private gl3image_t? gl3_notexture;

        private void GL3_SetDefaultState(GL gl)
        {
            gl.ClearColor(1, 0, 0.5f, 0.5f);
        // #ifndef YQ2_GL3_GLES
        //     // in GLES this is only supported with an extension:
        //     // https://www.khronos.org/registry/OpenGL/extensions/EXT/EXT_multisample_compatibility.txt
        //     // but apparently it's just enabled by default if set in the context?
            gl.Disable(EnableCap.Multisample);
        // #endif
            gl.CullFace(CullFaceMode.Front);

            gl.Disable(EnableCap.DepthTest);
            gl.Disable(EnableCap.CullFace);

        // #ifndef YQ2_GL3_GLES
        //     // in GLES GL_FILL is the only supported mode
            gl.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        // #endif

            // TODO: gl1_texturealphamode?
            GL3_TextureMode(gl, gl_texturemode!.str);
            //R_TextureAlphaMode(gl1_texturealphamode->string);
            //R_TextureSolidMode(gl1_texturesolidmode->string);

            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, gl_filter_min);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, gl_filter_max);

            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);

            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // #ifndef YQ2_GL3_GLES // see above
            if (gl_msaa_samples?.Bool ?? false)
            {
                gl.Enable(EnableCap.Multisample);
                // glHint(GL_MULTISAMPLE_FILTER_HINT_NV, GL_NICEST); TODO what is this for?
            }
        // #endif
        }        


        private static byte[][] dottexture = {
            new byte[]{0, 0, 0, 0, 0, 0, 0, 0},
            new byte[]{0, 0, 1, 1, 0, 0, 0, 0},
            new byte[]{0, 1, 1, 1, 1, 0, 0, 0},
            new byte[]{0, 1, 1, 1, 1, 0, 0, 0},
            new byte[]{0, 0, 1, 1, 0, 0, 0, 0},
            new byte[]{0, 0, 0, 0, 0, 0, 0, 0},
            new byte[]{0, 0, 0, 0, 0, 0, 0, 0},
            new byte[]{0, 0, 0, 0, 0, 0, 0, 0},
        };

        private void GL3_InitParticleTexture(GL gl)
        {
            byte[] data = new byte[8*8*4];

            /* particle texture */
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    data[((y*8)+x)*4 + 0] = 255;
                    data[((y*8)+x)*4 + 1] = 255;
                    data[((y*8)+x)*4 + 2] = 255;
                    data[((y*8)+x)*4 + 3] = (byte)(dottexture[x][y] * 255);
                }
            }

            gl3_particletexture = GL3_LoadPic(gl, "***particle***", data, 0, 8, 0, 8, 0, imagetype_t.it_sprite, 32);

            /* also use this for bad textures, but without alpha */
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    data[((y*8)+x)*4 + 0] = (byte)(dottexture[x & 3][y & 3] * 255);
                    data[((y*8)+x)*4 + 1] = 0;
                    data[((y*8)+x)*4 + 2] = 0;
                    data[((y*8)+x)*4 + 3] = 255;
                }
            }

            gl3_notexture = GL3_LoadPic(gl, "***r_notexture***", data, 0, 8, 0, 8, 0, imagetype_t.it_wall, 32);
        }

    }
}