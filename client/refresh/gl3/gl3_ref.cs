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
 * Local header for the OpenGL3 refresher.
 *
 * =======================================================================
 */
using Silk.NET.Maths; 
using Silk.NET.OpenGL;
using System.Runtime.Serialization;

namespace Quake2 {

    partial class QRefGl3
    {

        // width and height used to be 128, so now we should be able to get the same lightmap data
        // that used 32 lightmaps before into one, so 4 lightmaps should be enough
        private const int BLOCK_WIDTH = 1024;
        private const int BLOCK_HEIGHT = 512;
        private const int LIGHTMAP_BYTES = 4;
        private const int MAX_LIGHTMAPS = 4;
        private const int MAX_LIGHTMAPS_PER_SURFACE = QCommon.MAXLIGHTMAPS; // 4

        private struct gl3config_t
        {
            public string renderer_string;
            public string vendor_string;
            public string version_string;
            public string glsl_version_string;

            public int major_version;
            public int minor_version;

            // ----

            public bool anisotropic; // is GL_EXT_texture_filter_anisotropic supported?
            public bool debug_output; // is GL_ARB_debug_output supported?
            public bool stencil; // Do we have a stencil buffer?

            public bool useBigVBO; // workaround for AMDs windows driver for fewer calls to glBufferData()

            // ----

            public float max_anisotropy;
        }

        private struct gl3ShaderInfo_t
        {
            public uint shaderProgram;
            public int  uniVblend;
            public int uniLmScalesOrTime; // for 3D it's lmScales, for 2D underwater PP it's time
            public float[] lmScales;
            // hmm_vec4 lmScales[4];
        }

        [Serializable]
        [DataContract]
        private struct gl3UniCommon_t
        {
            public float gamma;
            public float intensity;
            public float intensity2D; // for HUD, menus etc

                // entries of std140 UBOs are aligned to multiples of their own size
                // so we'll need to pad accordingly for following vec4
                private float _padding;

            public Vector4D<float> color;
        }
        private const int gl3UniCommon_size = 8 * sizeof(float);

        [Serializable]
        [DataContract]
        private struct gl3Uni2D_t
        {
            public Matrix4X4<float> transMat4;
        }
        private const int gl3Uni2D_size = 16 * sizeof(float);

        [Serializable]
        [DataContract]
        private struct gl3Uni3D_t
        {
            public Matrix4X4<float> transProjViewMat4; // gl3state.projMat3D * gl3state.viewMat3D - so we don't have to do this in the shader
            public Matrix4X4<float> transModelMat4;

            public float scroll; // for SURF_FLOWING
            public float time; // for warping surfaces like water & possibly other things
            public float alpha; // for translucent surfaces (water, glass, ..)
            public float overbrightbits; // gl3_overbrightbits, applied to lightmaps (and elsewhere to models)
            public float particleFadeFactor; // gl3_particle_fade_factor, higher => less fading out towards edges

                private float _padding0; // again, some padding to ensure this has right size
                private float _padding1;
                private float _padding2;
        }

        private const int gl3Uni3D_size = ((16 * 2) + 8) * sizeof(float);

        [Serializable]
        [DataContract]
        private struct gl3UniDynLight
        {
            public Vector3D<float> origin;
            private float _padding;
            public Vector3D<float> color;
            private float intensity;
        } ;

        private const int gl3UniDynLight_size = 8 * sizeof(float);

        [Serializable]
        [DataContract]
        private struct gl3UniLights_t
        {
            public gl3UniDynLight dynLights0;
            public gl3UniDynLight dynLights1;
            public gl3UniDynLight dynLights2;
            public gl3UniDynLight dynLights3;
            public gl3UniDynLight dynLights4;
            public gl3UniDynLight dynLights5;
            public gl3UniDynLight dynLights6;
            public gl3UniDynLight dynLights7;
            public gl3UniDynLight dynLights8;
            public gl3UniDynLight dynLights9;
            public gl3UniDynLight dynLights10;
            public gl3UniDynLight dynLights11;
            public gl3UniDynLight dynLights12;
            public gl3UniDynLight dynLights13;
            public gl3UniDynLight dynLights14;
            public gl3UniDynLight dynLights15;
            public gl3UniDynLight dynLights16;
            public gl3UniDynLight dynLights17;
            public gl3UniDynLight dynLights18;
            public gl3UniDynLight dynLights19;
            public gl3UniDynLight dynLights20;
            public gl3UniDynLight dynLights21;
            public gl3UniDynLight dynLights22;
            public gl3UniDynLight dynLights23;
            public gl3UniDynLight dynLights24;
            public gl3UniDynLight dynLights25;
            public gl3UniDynLight dynLights26;
            public gl3UniDynLight dynLights27;
            public gl3UniDynLight dynLights28;
            public gl3UniDynLight dynLights29;
            public gl3UniDynLight dynLights30;
            public gl3UniDynLight dynLights31;
            public uint numDynLights;
            private uint _padding0;
            private uint _padding1;
            private uint _padding2;
        }

        private const int gl3UniLights_size = 4 * sizeof(uint) + gl3UniDynLight_size * QRef.MAX_DLIGHTS;

        private struct gl3state_t
        {
            // TODO: what of this do we need?
            public bool fullscreen;

            public int prev_mode;

            // each lightmap consists of 4 sub-lightmaps allowing changing shadows on the same surface
            // used for switching on/off light and stuff like that.
            // most surfaces only have one really and the remaining for are filled with dummy data
            public uint[] lightmap_textureIDs;
            // GLuint lightmap_textureIDs[MAX_LIGHTMAPS][MAX_LIGHTMAPS_PER_SURFACE]; // instead of lightmap_textures+i use lightmap_textureIDs[i]

            public uint currenttexture; // bound to GL_TEXTURE0
            public int currentlightmap; // lightmap_textureIDs[currentlightmap] bound to GL_TEXTURE1
            public TextureUnit currenttmu; // GL_TEXTURE0 or GL_TEXTURE1

            // FBO for postprocess effects (like under-water-warping)
            public uint ppFBO;
            public uint ppFBtex; // ppFBO's texture for color buffer
            public int ppFBtexWidth, ppFBtexHeight;
            public uint ppFBrbo; // ppFBO's renderbuffer object for depth and stencil buffer
            public  bool ppFBObound; // is it currently bound (rendered to)?

            // //float camera_separation;
            // //enum stereo_modes stereo_mode;

            public uint currentVAO;
            public uint currentVBO;
            public uint currentEBO;
            public uint currentShaderProgram;
            public uint currentUBO;

            // NOTE: make sure si2D is always the first shaderInfo (or adapt GL3_ShutdownShaders())
            public gl3ShaderInfo_t si2D;      // shader for rendering 2D with textures
            public gl3ShaderInfo_t si2Dcolor; // shader for rendering 2D with flat colors
            public gl3ShaderInfo_t si2DpostProcess; // shader to render postprocess FBO, when *not* underwater
            public gl3ShaderInfo_t si2DpostProcessWater; // shader to apply water-warp postprocess effect

            public gl3ShaderInfo_t si3Dlm;        // a regular opaque face (e.g. from brush) with lightmap
            // TODO: lm-only variants for gl_lightmap 1
            public gl3ShaderInfo_t si3Dtrans;     // transparent is always w/o lightmap
            public gl3ShaderInfo_t si3DcolorOnly; // used for beams - no lightmaps
            public gl3ShaderInfo_t si3Dturb;      // for water etc - always without lightmap
            public gl3ShaderInfo_t si3DlmFlow;    // for flowing/scrolling things with lightmap (conveyor, ..?)
            public gl3ShaderInfo_t si3DtransFlow; // for transparent flowing/scrolling things (=> no lightmap)
            public gl3ShaderInfo_t si3Dsky;       // guess what..
            public gl3ShaderInfo_t si3Dsprite;    // for sprites
            public gl3ShaderInfo_t si3DspriteAlpha; // for sprites with alpha-testing

            public gl3ShaderInfo_t si3Dalias;      // for models
            public gl3ShaderInfo_t si3DaliasColor; // for models w/ flat colors

            // NOTE: make sure siParticle is always the last shaderInfo (or adapt GL3_ShutdownShaders())
            public gl3ShaderInfo_t siParticle; // for particles. surprising, right?

            public uint vao3D, vbo3D; // for brushes etc, using 10 floats and one uint as vertex input (x,y,z, s,t, lms,lmt, normX,normY,normZ ; lightFlags)

            // the next two are for gl3config.useBigVBO == true
            public int vbo3Dsize;
            public int vbo3DcurOffset;

            public uint vaoAlias, vboAlias, eboAlias; // for models, using 9 floats as (x,y,z, s,t, r,g,b,a)
            public uint vaoParticle, vboParticle; // for particles, using 9 floats (x,y,z, size,distance, r,g,b,a)

            // UBOs and their data
            public gl3UniCommon_t uniCommonData;
            public gl3Uni2D_t uni2DData;
            public gl3Uni3D_t uni3DData;
            public gl3UniLights_t uniLightsData;
            public uint uniCommonUBO;
            public uint uni2DUBO;
            public uint uni3DUBO;
            public uint uniLightsUBO;

            public Matrix4X4<float> projMat3D;
            public Matrix4X4<float> viewMat3D;
        }

        // attribute locations for vertex shaders
        private const int GL3_ATTRIB_POSITION   = 0;
        private const int GL3_ATTRIB_TEXCOORD   = 1; // for normal texture
        private const int GL3_ATTRIB_LMTEXCOORD = 2; // for lightmap
        private const int GL3_ATTRIB_COLOR      = 3; // per-vertex color
        private const int GL3_ATTRIB_NORMAL     = 4; // vertex normal
        private const int GL3_ATTRIB_LIGHTFLAGS = 5;  // uint, each set bit means "dyn light i affects this surface"

        // always using RGBA now, GLES3 on RPi4 doesn't work otherwise
        // and I think all modern GPUs prefer 4byte pixels over 3bytes
        private const PixelFormat gl3_solid_format = PixelFormat.Rgba;
        private const PixelFormat gl3_alpha_format = PixelFormat.Rgba;
        private const InternalFormat gl3_tex_solid_format = InternalFormat.Rgba;
        private const InternalFormat gl3_tex_alpha_format = InternalFormat.Rgba;

        /*
        * skins will be outline flood filled and mip mapped
        * pics and sprites with alpha will be outline flood filled
        * pic won't be mip mapped
        *
        * model skin
        * sprite frame
        * wall texture
        * pic
        */
        private enum imagetype_t
        {
            it_skin,
            it_sprite,
            it_wall,
            it_pic,
            it_sky
        }

        /* NOTE: struct image_s* is what re.RegisterSkin() etc return so no gl3image_s!
        *       (I think the client only passes the pointer around and doesn't know the
        *        definition of this struct, so this being different from struct image_s
        *        in ref_gl should be ok)
        */
        private class gl3image_t : image_s
        {
            public string name = "";               /* game path, including extension */
            public imagetype_t type;
            public int width, height;                  /* source image */
            //int upload_width, upload_height;    /* after power of two and picmip */
            public int registration_sequence;          /* 0 = free */
            public msurface_t? texturechain;    /* for sort-by-texture world drawing */
            public uint texnum;                      /* gl texture binding */
            public float sl, tl, sh, th;               /* 0,0 - 1,1 unless part of the scrap */
            // qboolean scrap; // currently unused
            public bool has_alpha;

        }

        private void GL3_UseProgram(GL gl, uint shaderProgram)
        {
            if(shaderProgram != gl3state.currentShaderProgram)
            {
                gl3state.currentShaderProgram = shaderProgram;
                gl.UseProgram(shaderProgram);
            }
        }

        private void GL3_BindVAO(GL gl, uint vao)
        {
            if (vao != gl3state.currentVAO)
            {
                gl3state.currentVAO = vao;
                gl.BindVertexArray(vao);
            }
        }

        private void GL3_BindVBO(GL gl, uint vbo)
        {
            if (vbo != gl3state.currentVBO)
            {
                gl3state.currentVBO = vbo;
                gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            }
        }

        private void GL3_BindEBO(GL gl, uint ebo)
        {
            if (ebo != gl3state.currentEBO)
            {
                gl3state.currentEBO = ebo;
                gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
            }
        }

        private void GL3_SelectTMU(GL gl, TextureUnit tmu)
        {
            if (gl3state.currenttmu != tmu)
            {
                gl.ActiveTexture(tmu);
                gl3state.currenttmu = tmu;
            }
        }

        private struct gl3lightmapstate_t
        {
            public int internal_format;
            public int current_lightmap_texture; // index into gl3state.lightmap_textureIDs[]

            //msurface_t *lightmap_surfaces[MAX_LIGHTMAPS]; - no more lightmap chains, lightmaps are rendered multitextured

            public int[] allocated;

            /* the lightmap texture data needs to be kept in
            main memory so texsubimage can update properly */
            public byte[] lightmap_buffers;
            // byte lightmap_buffers[MAX_LIGHTMAPS_PER_SURFACE][4 * BLOCK_WIDTH * BLOCK_HEIGHT];

            public const int LIGHTMAP_BUFFER_SIZE = 4 * BLOCK_WIDTH * BLOCK_HEIGHT;

            public gl3lightmapstate_t() {
                internal_format = 0;
                current_lightmap_texture = 0;
                allocated = new int[BLOCK_WIDTH];
                lightmap_buffers = new byte[MAX_LIGHTMAPS_PER_SURFACE * LIGHTMAP_BUFFER_SIZE];
            }
        }


    }
}
