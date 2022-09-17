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
 * OpenGL3 refresher: Handling shaders
 *
 * =======================================================================
 */

using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Quake2 {

    partial class QRefGl3
    {

        private unsafe uint CompileShader(GL gl, ShaderType shaderType, in string shaderSrc, in string? shaderSrc2)
        {
            uint shader = gl.CreateShader(shaderType);

        // #ifdef YQ2_GL3_GLES3
        //     const char* version = "#version 300 es\nprecision mediump float;\n";
        // #else // Desktop GL
            string version = "#version 150\n";
        // #endif
            string[] sources = new string[]{ version, shaderSrc, shaderSrc2 ?? "" };
            uint numSources = shaderSrc2 != null ? 3u : 2u;

            gl.ShaderSource(shader, numSources, sources, null);
            gl.CompileShader(shader);
            gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
            if (status != (int)GLEnum.True)
            {
                var shaderTypeStr = "";
                switch(shaderType)
                {
                    case ShaderType.VertexShader:   shaderTypeStr = "Vertex"; break;
                    case ShaderType.FragmentShader: shaderTypeStr = "Fragment"; break;
        //             // we don't use geometry shaders and GLES3.0 doesn't support them
        //             // case GL_GEOMETRY_SHADER: shaderTypeStr = "Geometry"; break;
        //             /* not supported in OpenGL3.2 and we're unlikely to need/use them anyway
        //             case GL_COMPUTE_SHADER:  shaderTypeStr = "Compute"; break;
        //             case GL_TESS_CONTROL_SHADER:    shaderTypeStr = "TessControl"; break;
        //             case GL_TESS_EVALUATION_SHADER: shaderTypeStr = "TessEvaluation"; break;
        //             */
                }
                R_Printf(QShared.PRINT_ALL, $"ERROR: Compiling {shaderTypeStr} Shader failed: {gl.GetShaderInfoLog(shader)}\n");
                gl.DeleteShader(shader);
                return 0;
            }

            return shader;
        }

        private uint CreateShaderProgram(GL gl, in uint[] shaders)
        {
            uint shaderProgram = gl.CreateProgram();
            if (shaderProgram == 0)
            {
                R_Printf(QShared.PRINT_ALL, "ERROR: Couldn't create a new Shader Program!\n");
                return 0;
            }

            foreach (var sh in shaders)
            {
                gl.AttachShader(shaderProgram, sh);
            }

            // make sure all shaders use the same attribute locations for common attributes
            // (so the same VAO can easily be used with different shaders)
            gl.BindAttribLocation(shaderProgram, GL3_ATTRIB_POSITION, "position");
            gl.BindAttribLocation(shaderProgram, GL3_ATTRIB_TEXCOORD, "texCoord");
            gl.BindAttribLocation(shaderProgram, GL3_ATTRIB_LMTEXCOORD, "lmTexCoord");
            gl.BindAttribLocation(shaderProgram, GL3_ATTRIB_COLOR, "vertColor");
            gl.BindAttribLocation(shaderProgram, GL3_ATTRIB_NORMAL, "normal");
            gl.BindAttribLocation(shaderProgram, GL3_ATTRIB_LIGHTFLAGS, "lightFlags");

            // the following line is not necessary/implicit (as there's only one output)
            // glBindFragDataLocation(shaderProgram, 0, "outColor"); XXX would this even be here?

            gl.LinkProgram(shaderProgram);

            gl.GetProgram(shaderProgram, ProgramPropertyARB.LinkStatus, out int status);
            if (status != (int)GLEnum.True)
            {
                R_Printf(QShared.PRINT_ALL, $"ERROR: Linking shader program failed: {gl.GetProgramInfoLog(shaderProgram)}\n");
                gl.DeleteProgram(shaderProgram);
                return 0;
            }

            foreach (var sh in shaders)
            {
                // after linking, they don't need to be attached anymore.
                // no idea  why they even are, if they don't have to..
                gl.DetachShader(shaderProgram, sh);
            }

            return shaderProgram;
        }

	    private const int GL3_BINDINGPOINT_UNICOMMON = 0;
	    private const int GL3_BINDINGPOINT_UNI2D = 1;
	    private const int GL3_BINDINGPOINT_UNI3D = 2;
	    private const int GL3_BINDINGPOINT_UNILIGHTS = 3;

        private bool initShader2D(GL gl, ref gl3ShaderInfo_t shaderInfo, in string vertSrc, in string fragSrc)
        {
            // GLuint shaders2D[2] = {0};
            // GLuint prog = 0;

            if (shaderInfo.shaderProgram != 0)
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: calling initShader2D for gl3ShaderInfo_t that already has a shaderProgram!\n");
                gl.DeleteProgram(shaderInfo.shaderProgram);
            }

            //shaderInfo->uniColor = shaderInfo->uniProjMatrix = shaderInfo->uniModelViewMatrix = -1;
            shaderInfo.shaderProgram = 0;
            shaderInfo.uniLmScalesOrTime = -1;
            shaderInfo.uniVblend = -1;

            uint[] shaders2D = new uint[2];
            shaders2D[0] = CompileShader(gl, ShaderType.VertexShader, vertSrc, null);
            if(shaders2D[0] == 0)  return false;

            shaders2D[1] = CompileShader(gl, ShaderType.FragmentShader, fragSrc, null);
            if(shaders2D[1] == 0)
            {
                gl.DeleteShader(shaders2D[0]);
                return false;
            }

            var prog = CreateShaderProgram(gl, shaders2D);

            // I think the shaders aren't needed anymore once they're linked into the program
            gl.DeleteShader(shaders2D[0]);
            gl.DeleteShader(shaders2D[1]);

            if(prog == 0)
            {
                return false;
            }

            shaderInfo.shaderProgram = prog;
            GL3_UseProgram(gl, prog);

            // // Bind the buffer object to the uniform blocks
            uint blockIndex = gl.GetUniformBlockIndex(prog, "uniCommon");
            if ((GLEnum)blockIndex != GLEnum.InvalidIndex)
            {
                gl.GetActiveUniformBlock(prog, blockIndex, UniformBlockPName.DataSize, out int blockSize);
                if (blockSize != gl3UniCommon_size)
                {
                    R_Printf(QShared.PRINT_ALL, $"WARNING: OpenGL driver disagrees with us about UBO size of 'uniCommon': {blockSize} vs {gl3UniCommon_size}\n");
                    gl.DeleteProgram(prog);
                    return false;
                }

                gl.UniformBlockBinding(prog, blockIndex, GL3_BINDINGPOINT_UNICOMMON);
            }
            else
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: Couldn't find uniform block index 'uniCommon'\n");
                gl.DeleteProgram(prog);
                return false;
            }
            blockIndex = gl.GetUniformBlockIndex(prog, "uni2D");
            if ((GLEnum)blockIndex != GLEnum.InvalidIndex)
            {
                gl.GetActiveUniformBlock(prog, blockIndex, UniformBlockPName.DataSize, out int blockSize);
                if (blockSize != gl3Uni2D_size)
                {
                    R_Printf(QShared.PRINT_ALL, $"WARNING: OpenGL driver disagrees with us about UBO size of 'uni2D': {blockSize} vs {gl3Uni2D_size}\n");
                    gl.DeleteProgram(prog);
                    return false;
                }

                gl.UniformBlockBinding(prog, blockIndex, GL3_BINDINGPOINT_UNI2D);
            }
            else
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: Couldn't find uniform block index 'uni2D'\n");
                gl.DeleteProgram(prog);
                return false;
            }

            shaderInfo.uniLmScalesOrTime = gl.GetUniformLocation(prog, "time");
            if(shaderInfo.uniLmScalesOrTime != -1)
            {
                gl.Uniform1(shaderInfo.uniLmScalesOrTime, 0.0f);
            }

            shaderInfo.uniVblend = gl.GetUniformLocation(prog, "v_blend");
            if(shaderInfo.uniVblend != -1)
            {
                gl.Uniform4(shaderInfo.uniVblend, 0f, 0f, 0f, 0f);
            }

            return true;
        }        

        private unsafe bool initShader3D(GL gl, ref gl3ShaderInfo_t shaderInfo, string vertSrc, string fragSrc)
        {
            // GLuint shaders3D[2] = {0};
            // GLuint prog = 0;
            // int i=0;

            if(shaderInfo.shaderProgram != 0)
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: calling initShader3D for gl3ShaderInfo_t that already has a shaderProgram!\n");
                gl.DeleteProgram(shaderInfo.shaderProgram);
            }

            shaderInfo.shaderProgram = 0;
            shaderInfo.uniLmScalesOrTime = -1;
            shaderInfo.uniVblend = -1;

            uint[] shaders3D = new uint[2];
            shaders3D[0] = CompileShader(gl, ShaderType.VertexShader, vertexCommon3D, vertSrc);
            if(shaders3D[0] == 0)  return false;

            shaders3D[1] = CompileShader(gl, ShaderType.FragmentShader, fragmentCommon3D, fragSrc);
            if(shaders3D[1] == 0)
            {
                gl.DeleteShader(shaders3D[0]);
                return false;
            }

            var prog = CreateShaderProgram(gl, shaders3D);

            if(prog == 0)
            {
                goto err_cleanup;
            }

            GL3_UseProgram(gl, prog);

            // Bind the buffer object to the uniform blocks
            uint blockIndex = gl.GetUniformBlockIndex(prog, "uniCommon");
            if ((GLEnum)blockIndex != GLEnum.InvalidIndex)
            {
                gl.GetActiveUniformBlock(prog, blockIndex, UniformBlockPName.DataSize, out int blockSize);
                if(blockSize != gl3UniCommon_size)
                {
                    R_Printf(QShared.PRINT_ALL, "WARNING: OpenGL driver disagrees with us about UBO size of 'uniCommon'\n");

                    goto err_cleanup;
                }

                gl.UniformBlockBinding(prog, blockIndex, GL3_BINDINGPOINT_UNICOMMON);
            }
            else
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: Couldn't find uniform block index 'uniCommon'\n");

                goto err_cleanup;
            }
            blockIndex = gl.GetUniformBlockIndex(prog, "uni3D");
            if ((GLEnum)blockIndex != GLEnum.InvalidIndex)
            {
                gl.GetActiveUniformBlock(prog, blockIndex, UniformBlockPName.DataSize, out int blockSize);
                if(blockSize != gl3Uni3D_size)
                {
                    R_Printf(QShared.PRINT_ALL, "WARNING: OpenGL driver disagrees with us about UBO size of 'uni3D'\n");
                    R_Printf(QShared.PRINT_ALL, $"         driver says {blockSize}, we expect {gl3Uni3D_size}\n");

                    goto err_cleanup;
                }

                gl.UniformBlockBinding(prog, blockIndex, GL3_BINDINGPOINT_UNI3D);
            }
            else
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: Couldn't find uniform block index 'uni3D'\n");

                goto err_cleanup;
            }
            blockIndex = gl.GetUniformBlockIndex(prog, "uniLights");
            if ((GLEnum)blockIndex != GLEnum.InvalidIndex)
            {
                gl.GetActiveUniformBlock(prog, blockIndex, UniformBlockPName.DataSize, out int blockSize);
                if(blockSize != gl3UniLights_size)
                {
                    R_Printf(QShared.PRINT_ALL, "WARNING: OpenGL driver disagrees with us about UBO size of 'uniLights'\n");
                    R_Printf(QShared.PRINT_ALL, $"         OpenGL says {blockSize}, we say {gl3UniLights_size}\n");

                    goto err_cleanup;
                }

                gl.UniformBlockBinding(prog, blockIndex, GL3_BINDINGPOINT_UNILIGHTS);
            }
            // else: as uniLights is only used in the LM shaders, it's ok if it's missing

            // make sure texture is GL_TEXTURE0
            int texLoc = gl.GetUniformLocation(prog, "tex");
            if(texLoc != -1)
            {
                gl.Uniform1(texLoc, 0);
            }

            // ..  and the 4 lightmap texture use GL_TEXTURE1..4
            for (int i=0; i<4; ++i)
            {
                var lmName = $"lightmap{i}";
                int lmLoc = gl.GetUniformLocation(prog, lmName);
                if (lmLoc != -1)
                {
                    gl.Uniform1(lmLoc, i+1); // lightmap0 belongs to GL_TEXTURE1, lightmap1 to GL_TEXTURE2 etc
                }
            }

            int lmScalesLoc = gl.GetUniformLocation(prog, "lmScales");
            shaderInfo.lmScales = new float[16];
            shaderInfo.uniLmScalesOrTime = lmScalesLoc;
            if (lmScalesLoc != -1)
            {
                Array.Fill(shaderInfo.lmScales, 1.0f, 0, 4);
                Array.Fill(shaderInfo.lmScales, 0.0f, 4, 16-4);

                fixed (float *f = shaderInfo.lmScales)
                {
                    gl.Uniform4(lmScalesLoc, 4, f);
                }
            }

            shaderInfo.shaderProgram = prog;

            // I think the shaders aren't needed anymore once they're linked into the program
            gl.DeleteShader(shaders3D[0]);
            gl.DeleteShader(shaders3D[1]);

            return true;

        err_cleanup:

            gl.DeleteShader(shaders3D[0]);
            gl.DeleteShader(shaders3D[1]);

            if(prog != 0)  gl.DeleteProgram(prog);

            return false;
        }

        private void initUBOs(GL gl)
        {
            gl3state.uniCommonData.gamma = 1.0f/vid_gamma!.Float;
            gl3state.uniCommonData.intensity = gl3_intensity!.Float;
            gl3state.uniCommonData.intensity2D = gl3_intensity_2D!.Float;
            gl3state.uniCommonData.color = new Vector4D<float>(1, 1, 1, 1);

            gl3state.uniCommonUBO = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.UniformBuffer, gl3state.uniCommonUBO);
            gl.BindBufferBase(BufferTargetARB.UniformBuffer, GL3_BINDINGPOINT_UNICOMMON, gl3state.uniCommonUBO);
            gl.BufferData(BufferTargetARB.UniformBuffer, gl3UniCommon_size, gl3state.uniCommonData, BufferUsageARB.DynamicDraw);

            // the matrix will be set to something more useful later, before being used
            gl3state.uni2DData.transMat4 = new Matrix4X4<float>();

            gl3state.uni2DUBO = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.UniformBuffer, gl3state.uni2DUBO);
            gl.BindBufferBase(BufferTargetARB.UniformBuffer, GL3_BINDINGPOINT_UNI2D, gl3state.uni2DUBO);
            gl.BufferData(BufferTargetARB.UniformBuffer, gl3Uni2D_size, gl3state.uni2DData, BufferUsageARB.DynamicDraw);

            // the matrices will be set to something more useful later, before being used
            gl3state.uni3DData.transProjViewMat4 = new Matrix4X4<float>();
            gl3state.uni3DData.transModelMat4 = gl3_identityMat4;
            gl3state.uni3DData.scroll = 0.0f;
            gl3state.uni3DData.time = 0.0f;
            gl3state.uni3DData.alpha = 1.0f;
            // gl3_overbrightbits 0 means "no scaling" which is equivalent to multiplying with 1
            gl3state.uni3DData.overbrightbits = (gl3_overbrightbits?.Float <= 0.0f) ? 1.0f : gl3_overbrightbits!.Float;
            gl3state.uni3DData.particleFadeFactor = gl3_particle_fade_factor!.Float;

            gl3state.uni3DUBO = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.UniformBuffer, gl3state.uni3DUBO);
            gl.BindBufferBase(BufferTargetARB.UniformBuffer, GL3_BINDINGPOINT_UNI3D, gl3state.uni3DUBO);
            gl.BufferData(BufferTargetARB.UniformBuffer, gl3Uni3D_size, gl3state.uni3DData, BufferUsageARB.DynamicDraw);

            gl3state.uniLightsUBO = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.UniformBuffer, gl3state.uniLightsUBO);
            gl.BindBufferBase(BufferTargetARB.UniformBuffer, GL3_BINDINGPOINT_UNILIGHTS, gl3state.uniLightsUBO);
            gl.BufferData(BufferTargetARB.UniformBuffer, gl3UniLights_size, gl3state.uniLightsData, BufferUsageARB.DynamicDraw);

            gl3state.currentUBO = gl3state.uniLightsUBO;
        }


        private bool createShaders(GL gl)
        {
            if(!initShader2D(gl, ref gl3state.si2D, vertexSrc2D, fragmentSrc2D))
            {                
                R_Printf(QShared.PRINT_ALL, "WARNING: Failed to create shader program for textured 2D rendering!\n");
                return false;
            }
            if(!initShader2D(gl, ref gl3state.si2Dcolor, vertexSrc2Dcolor, fragmentSrc2Dcolor))
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: Failed to create shader program for color-only 2D rendering!\n");
                return false;
            }

            if(!initShader2D(gl, ref gl3state.si2DpostProcess, vertexSrc2D, fragmentSrc2Dpostprocess))
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: Failed to create shader program to render framebuffer object!\n");
                return false;
            }
            if(!initShader2D(gl, ref gl3state.si2DpostProcessWater, vertexSrc2D, fragmentSrc2DpostprocessWater))
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: Failed to create shader program to render framebuffer object under water!\n");
                return false;
            }

            string lightmappedFrag = (gl3_colorlight!.Float == 0.0f) ? fragmentSrc3DlmNoColor : fragmentSrc3Dlm;

            if(!initShader3D(gl, ref gl3state.si3Dlm, vertexSrc3Dlm, lightmappedFrag))
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: Failed to create shader program for textured 3D rendering with lightmap!\n");
                return false;
            }
            if(!initShader3D(gl, ref gl3state.si3Dtrans, vertexSrc3D, fragmentSrc3D))
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: Failed to create shader program for rendering translucent 3D things!\n");
                return false;
            }
            if(!initShader3D(gl, ref gl3state.si3DcolorOnly, vertexSrc3D, fragmentSrc3Dcolor))
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: Failed to create shader program for flat-colored 3D rendering!\n");
                return false;
            }
            /*
            if(!initShader3D(&gl3state.si3Dlm, vertexSrc3Dlm, fragmentSrc3D))
            {
                R_Printf(PRINT_ALL, "WARNING: Failed to create shader program for blending 3D lightmaps rendering!\n");
                return false;
            }
            */
            if(!initShader3D(gl, ref gl3state.si3Dturb, vertexSrc3Dwater, fragmentSrc3Dwater))
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: Failed to create shader program for water rendering!\n");
                return false;
            }
            if(!initShader3D(gl, ref gl3state.si3DlmFlow, vertexSrc3DlmFlow, lightmappedFrag))
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: Failed to create shader program for scrolling textured 3D rendering with lightmap!\n");
                return false;
            }
            if(!initShader3D(gl, ref gl3state.si3DtransFlow, vertexSrc3Dflow, fragmentSrc3D))
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: Failed to create shader program for scrolling textured translucent 3D rendering!\n");
                return false;
            }
            if(!initShader3D(gl, ref gl3state.si3Dsky, vertexSrc3D, fragmentSrc3Dsky))
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: Failed to create shader program for sky rendering!\n");
                return false;
            }
            if(!initShader3D(gl, ref gl3state.si3Dsprite, vertexSrc3D, fragmentSrc3Dsprite))
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: Failed to create shader program for sprite rendering!\n");
                return false;
            }
            if(!initShader3D(gl, ref gl3state.si3DspriteAlpha, vertexSrc3D, fragmentSrc3DspriteAlpha))
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: Failed to create shader program for alpha-tested sprite rendering!\n");
                return false;
            }
            if(!initShader3D(gl, ref gl3state.si3Dalias, vertexSrcAlias, fragmentSrcAlias))
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: Failed to create shader program for rendering textured models!\n");
                return false;
            }
            if(!initShader3D(gl, ref gl3state.si3DaliasColor, vertexSrcAlias, fragmentSrcAliasColor))
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: Failed to create shader program for rendering flat-colored models!\n");
                return false;
            }

            string particleFrag = fragmentSrcParticles;
            if(gl3_particle_square!.Float != 0.0f)
            {
                particleFrag = fragmentSrcParticlesSquare;
            }

            if(!initShader3D(gl, ref gl3state.siParticle, vertexSrcParticles, particleFrag))
            {
                R_Printf(QShared.PRINT_ALL, "WARNING: Failed to create shader program for rendering particles!\n");
                return false;
            }

            gl3state.currentShaderProgram = 0;

            return true;
        }

        private bool GL3_InitShaders(GL gl)
        {
            initUBOs(gl);

            return createShaders(gl);
        }

        private void GL3_UpdateUBOCommon(GL gl)
        {
            if (gl3state.currentUBO != gl3state.uniCommonUBO)
            {
                gl3state.currentUBO = gl3state.uniCommonUBO;
                gl.BindBuffer(BufferTargetARB.UniformBuffer, gl3state.uniCommonUBO);
            }
            gl.BufferData(BufferTargetARB.UniformBuffer, gl3UniCommon_size, gl3state.uniCommonData, BufferUsageARB.DynamicDraw);
        }

        private void GL3_UpdateUBO2D(GL gl)
        {
            if(gl3state.currentUBO != gl3state.uni2DUBO)
            {
                gl3state.currentUBO = gl3state.uni2DUBO;
                gl.BindBuffer(BufferTargetARB.UniformBuffer, gl3state.uni2DUBO);
            }
            gl.BufferData(BufferTargetARB.UniformBuffer, gl3Uni2D_size, gl3state.uni2DData, BufferUsageARB.DynamicDraw);
        }

        private unsafe void GL3_UpdateUBO3D(GL gl)
        {
            if(gl3state.currentUBO != gl3state.uni3DUBO)
            {
                gl3state.currentUBO = gl3state.uni3DUBO;
                gl.BindBuffer(BufferTargetARB.UniformBuffer, gl3state.uni3DUBO);
            }
            gl.BufferData(BufferTargetARB.UniformBuffer, gl3Uni3D_size, gl3state.uni3DData, BufferUsageARB.DynamicDraw);
        }

        private unsafe void GL3_UpdateUBOLights(GL gl)
        {
            if(gl3state.currentUBO != gl3state.uniLightsUBO)
            {
                gl3state.currentUBO = gl3state.uniLightsUBO;
                gl.BindBuffer(BufferTargetARB.UniformBuffer, gl3state.uniLightsUBO);
            }
            gl.BufferData(BufferTargetARB.UniformBuffer, gl3UniLights_size, gl3state.uniLightsData, BufferUsageARB.DynamicDraw);
        }

        private readonly string vertexSrc2D = @"

in vec2 position; // GL3_ATTRIB_POSITION
in vec2 texCoord; // GL3_ATTRIB_TEXCOORD

// for UBO shared between 2D shaders
layout (std140) uniform uni2D
{
    mat4 trans;
};

out vec2 passTexCoord;

void main()
{
    gl_Position = trans * vec4(position, 0.0, 1.0);
    passTexCoord = texCoord;
}
";

        private readonly string fragmentSrc2D = @"

in vec2 passTexCoord;

// for UBO shared between all shaders (incl. 2D)
layout (std140) uniform uniCommon
{
    float gamma;
    float intensity;
    float intensity2D; // for HUD, menu etc

    vec4 color;
};

uniform sampler2D tex;

out vec4 outColor;

void main()
{
    vec4 texel = texture(tex, passTexCoord);
    // the gl1 renderer used glAlphaFunc(GL_GREATER, 0.666);
    // and glEnable(GL_ALPHA_TEST); for 2D rendering
    // this should do the same
    if(texel.a <= 0.666)
        discard;

    // apply gamma correction and intensity
    texel.rgb *= intensity2D;
    outColor.rgb = pow(texel.rgb, vec3(gamma));
    outColor.a = texel.a; // I think alpha shouldn't be modified by gamma and intensity
}
";

        private readonly string fragmentSrc2Dpostprocess = @"
in vec2 passTexCoord;

// for UBO shared between all shaders (incl. 2D)
// TODO: not needed here, remove?
layout (std140) uniform uniCommon
{
    float gamma;
    float intensity;
    float intensity2D; // for HUD, menu etc

    vec4 color;
};

uniform sampler2D tex;
uniform vec4 v_blend;

out vec4 outColor;

void main()
{
    // no gamma or intensity here, it has been applied before
    // (this is just for postprocessing)
    vec4 res = texture(tex, passTexCoord);
    // apply the v_blend, usually blended as a colored quad with:
    // glBlendEquation(GL_FUNC_ADD); glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
    res.rgb = v_blend.a * v_blend.rgb + (1.0 - v_blend.a)*res.rgb;
    outColor =  res;
}
";

        private readonly string fragmentSrc2DpostprocessWater = @"
in vec2 passTexCoord;

// for UBO shared between all shaders (incl. 2D)
// TODO: not needed here, remove?
layout (std140) uniform uniCommon
{
    float gamma;
    float intensity;
    float intensity2D; // for HUD, menu etc

    vec4 color;
};

const float PI = 3.14159265358979323846;

uniform sampler2D tex;

uniform float time;
uniform vec4 v_blend;

out vec4 outColor;

void main()
{
    vec2 uv = passTexCoord;

    // warping based on vkquake2
    // here uv is always between 0 and 1 so ignore all that scrWidth and gl_FragCoord stuff
    //float sx = pc.scale - abs(pc.scrWidth  / 2.0 - gl_FragCoord.x) * 2.0 / pc.scrWidth;
    //float sy = pc.scale - abs(pc.scrHeight / 2.0 - gl_FragCoord.y) * 2.0 / pc.scrHeight;
    float sx = 1.0 - abs(0.5-uv.x)*2.0;
    float sy = 1.0 - abs(0.5-uv.y)*2.0;
    float xShift = 2.0 * time + uv.y * PI * 10.0;
    float yShift = 2.0 * time + uv.x * PI * 10.0;
    vec2 distortion = vec2(sin(xShift) * sx, sin(yShift) * sy) * 0.00666;

    uv += distortion;
    uv = clamp(uv, vec2(0.0, 0.0), vec2(1.0, 1.0));

    // no gamma or intensity here, it has been applied before
    // (this is just for postprocessing)
    vec4 res = texture(tex, uv);
    // apply the v_blend, usually blended as a colored quad with:
    // glBlendEquation(GL_FUNC_ADD); glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
    res.rgb = v_blend.a * v_blend.rgb + (1.0 - v_blend.a)*res.rgb;
    outColor =  res;
}
";

        // 2D color only rendering, GL3_Draw_Fill(), GL3_Draw_FadeScreen()
        private readonly string  vertexSrc2Dcolor = @"

in vec2 position; // GL3_ATTRIB_POSITION

// for UBO shared between 2D shaders
layout (std140) uniform uni2D
{
    mat4 trans;
};

void main()
{
    gl_Position = trans * vec4(position, 0.0, 1.0);
}
";

        private readonly string fragmentSrc2Dcolor = @"

// for UBO shared between all shaders (incl. 2D)
layout (std140) uniform uniCommon
{
    float gamma;
    float intensity;
    float intensity2D; // for HUD, menus etc

    vec4 color;
};

out vec4 outColor;

void main()
{
    vec3 col = color.rgb * intensity2D;
    outColor.rgb = pow(col, vec3(gamma));
    outColor.a = color.a;
}
";

        // ############## shaders for 3D rendering #####################

        private readonly string vertexCommon3D = @"

in vec3 position;   // GL3_ATTRIB_POSITION
in vec2 texCoord;   // GL3_ATTRIB_TEXCOORD
in vec2 lmTexCoord; // GL3_ATTRIB_LMTEXCOORD
in vec4 vertColor;  // GL3_ATTRIB_COLOR
in vec3 normal;     // GL3_ATTRIB_NORMAL
in uint lightFlags; // GL3_ATTRIB_LIGHTFLAGS

out vec2 passTexCoord;

// for UBO shared between all 3D shaders
layout (std140) uniform uni3D
{
    mat4 transProjView;
    mat4 transModel;

    float scroll; // for SURF_FLOWING
    float time;
    float alpha;
    float overbrightbits;
    float particleFadeFactor;
    float _pad_1; // AMDs legacy windows driver needs this, otherwise uni3D has wrong size
    float _pad_2;
    float _pad_3;
};
";

        private readonly string fragmentCommon3D = @"

in vec2 passTexCoord;

out vec4 outColor;

// for UBO shared between all shaders (incl. 2D)
layout (std140) uniform uniCommon
{
    float gamma; // this is 1.0/vid_gamma
    float intensity;
    float intensity2D; // for HUD, menus etc

    vec4 color; // really?
};
// for UBO shared between all 3D shaders
layout (std140) uniform uni3D
{
    mat4 transProjView;
    mat4 transModel;

    float scroll; // for SURF_FLOWING
    float time;
    float alpha;
    float overbrightbits;
    float particleFadeFactor;
    float _pad_1; // AMDs legacy windows driver needs this, otherwise uni3D has wrong size
    float _pad_2;
    float _pad_3;
};
";

        private readonly string vertexSrc3D = @"

// it gets attributes and uniforms from vertexCommon3D

void main()
{
    passTexCoord = texCoord;
    gl_Position = transProjView * transModel * vec4(position, 1.0);
}
";

        private readonly string vertexSrc3Dflow = @"

// it gets attributes and uniforms from vertexCommon3D

void main()
{
    passTexCoord = texCoord + vec2(scroll, 0.0);
    gl_Position = transProjView * transModel * vec4(position, 1.0);
}
";

        private readonly string vertexSrc3Dlm = @"

// it gets attributes and uniforms from vertexCommon3D

out vec2 passLMcoord;
out vec3 passWorldCoord;
out vec3 passNormal;
flat out uint passLightFlags;

void main()
{
    passTexCoord = texCoord;
    passLMcoord = lmTexCoord;
    // vec4 worldCoord = transModel * vec4(position, 1.0);
    vec4 worldCoord = vec4(position, 1.0);
    passWorldCoord = worldCoord.xyz;
    // vec4 worldNormal = transModel * vec4(normal, 0.0f);
    vec4 worldNormal = vec4(normal, 0.0f);
    passNormal = normalize(worldNormal.xyz);
    // passLightFlags = lightFlags;
    passLightFlags = 0xFFFFFFFFU;

    gl_Position = transProjView * worldCoord;
}
";

        private readonly string vertexSrc3DlmFlow = @"

// it gets attributes and uniforms from vertexCommon3D

out vec2 passLMcoord;
out vec3 passWorldCoord;
out vec3 passNormal;
flat out uint passLightFlags;

void main()
{
    passTexCoord = texCoord + vec2(scroll, 0.0);
    passLMcoord = lmTexCoord;
    vec4 worldCoord = transModel * vec4(position, 1.0);
    passWorldCoord = worldCoord.xyz;
    vec4 worldNormal = transModel * vec4(normal, 0.0f);
    passNormal = normalize(worldNormal.xyz);
    passLightFlags = lightFlags;

    gl_Position = transProjView * worldCoord;
}
";

        private readonly string fragmentSrc3D = @"

// it gets attributes and uniforms from fragmentCommon3D

uniform sampler2D tex;

void main()
{
    vec4 texel = texture(tex, passTexCoord);

    // apply intensity and gamma
    texel.rgb *= intensity;
    outColor.rgb = pow(texel.rgb, vec3(gamma));
    outColor.a = texel.a*alpha; // I think alpha shouldn't be modified by gamma and intensity
}
";

        private readonly string fragmentSrc3Dwater = @"

// it gets attributes and uniforms from fragmentCommon3D

uniform sampler2D tex;

void main()
{
    vec2 tc = passTexCoord;
    tc.s += sin( passTexCoord.t*0.125 + time ) * 4.0;
    tc.s += scroll;
    tc.t += sin( passTexCoord.s*0.125 + time ) * 4.0;
    tc *= 1.0/64.0; // do this last

    vec4 texel = texture(tex, tc);

    // apply intensity and gamma
    texel.rgb *= intensity*0.5;
    outColor.rgb = pow(texel.rgb, vec3(gamma));
    outColor.a = texel.a*alpha; // I think alpha shouldn't be modified by gamma and intensity
}
";

        private readonly string fragmentSrc3Dlm = @"

// it gets attributes and uniforms from fragmentCommon3D

struct DynLight { // gl3UniDynLight in C
    vec3 lightOrigin;
    float _pad;
    //vec3 lightColor;
    //float lightIntensity;
    vec4 lightColor; // .a is intensity; this way it also works on OSX...
    // (otherwise lightIntensity always contained 1 there)
};

layout (std140) uniform uniLights
{
    DynLight dynLights[32];
    uint numDynLights;
    uint _pad1; uint _pad2; uint _pad3; // FFS, AMD!
};

uniform sampler2D tex;

uniform sampler2D lightmap0;
uniform sampler2D lightmap1;
uniform sampler2D lightmap2;
uniform sampler2D lightmap3;

uniform vec4 lmScales[4];

in vec2 passLMcoord;
in vec3 passWorldCoord;
in vec3 passNormal;
flat in uint passLightFlags;

void main()
{
    vec4 texel = texture(tex, passTexCoord);

    // apply intensity
    texel.rgb *= intensity;

    // apply lightmap
    vec4 lmTex = texture(lightmap0, passLMcoord) * lmScales[0];
    lmTex     += texture(lightmap1, passLMcoord) * lmScales[1];
    lmTex     += texture(lightmap2, passLMcoord) * lmScales[2];
    lmTex     += texture(lightmap3, passLMcoord) * lmScales[3];

    if(passLightFlags != 0u)
    {
        // TODO: or is hardcoding 32 better?
        for(uint i=0u; i<numDynLights; ++i)
        {
            // I made the following up, it's probably not too cool..
            // it basically checks if the light is on the right side of the surface
            // and, if it is, sets intensity according to distance between light and pixel on surface

            // dyn light number i does not affect this plane, just skip it
            if((passLightFlags & (1u << i)) == 0u)  continue;

            float intens = dynLights[i].lightColor.a;

            vec3 lightToPos = dynLights[i].lightOrigin - passWorldCoord;
            float distLightToPos = length(lightToPos);
            float fact = max(0.0, intens - distLightToPos - 52.0);

            // move the light source a bit further above the surface
            // => helps if the lightsource is so close to the surface (e.g. grenades, rockets)
            //    that the dot product below would return 0
            // (light sources that are below the surface are filtered out by lightFlags)
            lightToPos += passNormal*32.0;

            // also factor in angle between light and point on surface
            fact *= max(0.0, dot(passNormal, normalize(lightToPos)));


            lmTex.rgb += dynLights[i].lightColor.rgb * fact * (1.0/256.0);
        }
    }

    lmTex.rgb *= overbrightbits;
    outColor = lmTex*texel;
    outColor.rgb = pow(outColor.rgb, vec3(gamma)); // apply gamma correction to result

    outColor.a = 1.0; // lightmaps aren't used with translucent surfaces
}
";

        private readonly string fragmentSrc3DlmNoColor = @"

// it gets attributes and uniforms from fragmentCommon3D

struct DynLight { // gl3UniDynLight in C
    vec3 lightOrigin;
    float _pad;
    //vec3 lightColor;
    //float lightIntensity;
    vec4 lightColor; // .a is intensity; this way it also works on OSX...
    // (otherwise lightIntensity always contained 1 there)
};

layout (std140) uniform uniLights
{
    DynLight dynLights[32];
    uint numDynLights;
    uint _pad1; uint _pad2; uint _pad3; // FFS, AMD!
};

uniform sampler2D tex;

uniform sampler2D lightmap0;
uniform sampler2D lightmap1;
uniform sampler2D lightmap2;
uniform sampler2D lightmap3;

uniform vec4 lmScales[4];

in vec2 passLMcoord;
in vec3 passWorldCoord;
in vec3 passNormal;
flat in uint passLightFlags;

void main()
{
    vec4 texel = texture(tex, passTexCoord);

    // apply intensity
    texel.rgb *= intensity;

    // apply lightmap
    vec4 lmTex = texture(lightmap0, passLMcoord) * lmScales[0];
    lmTex     += texture(lightmap1, passLMcoord) * lmScales[1];
    lmTex     += texture(lightmap2, passLMcoord) * lmScales[2];
    lmTex     += texture(lightmap3, passLMcoord) * lmScales[3];

    if(passLightFlags != 0u)
    {
        // TODO: or is hardcoding 32 better?
        for(uint i=0u; i<numDynLights; ++i)
        {
            // I made the following up, it's probably not too cool..
            // it basically checks if the light is on the right side of the surface
            // and, if it is, sets intensity according to distance between light and pixel on surface

            // dyn light number i does not affect this plane, just skip it
            if((passLightFlags & (1u << i)) == 0u)  continue;

            float intens = dynLights[i].lightColor.a;

            vec3 lightToPos = dynLights[i].lightOrigin - passWorldCoord;
            float distLightToPos = length(lightToPos);
            float fact = max(0.0, intens - distLightToPos - 52.0);

            // move the light source a bit further above the surface
            // => helps if the lightsource is so close to the surface (e.g. grenades, rockets)
            //    that the dot product below would return 0
            // (light sources that are below the surface are filtered out by lightFlags)
            lightToPos += passNormal*32.0;

            // also factor in angle between light and point on surface
            fact *= max(0.0, dot(passNormal, normalize(lightToPos)));


            lmTex.rgb += dynLights[i].lightColor.rgb * fact * (1.0/256.0);
        }
    }

    // turn lightcolor into grey for gl3_colorlight 0
    lmTex.rgb = vec3(0.333 * (lmTex.r+lmTex.g+lmTex.b));

    lmTex.rgb *= overbrightbits;
    outColor = lmTex*texel;
    outColor.rgb = pow(outColor.rgb, vec3(gamma)); // apply gamma correction to result

    outColor.a = 1.0; // lightmaps aren't used with translucent surfaces
}
";

        private readonly string fragmentSrc3Dcolor = @"

// it gets attributes and uniforms from fragmentCommon3D

void main()
{
    vec4 texel = color;

    // apply gamma correction and intensity
    // texel.rgb *= intensity; TODO: use intensity here? (this is used for beams)
    outColor.rgb = pow(texel.rgb, vec3(gamma));
    outColor.a = texel.a*alpha; // I think alpha shouldn't be modified by gamma and intensity
}
";

        private readonly string fragmentSrc3Dsky = @"

// it gets attributes and uniforms from fragmentCommon3D

uniform sampler2D tex;

void main()
{
    vec4 texel = texture(tex, passTexCoord);

    // TODO: something about GL_BLEND vs GL_ALPHATEST etc

    // apply gamma correction
    // texel.rgb *= intensity; // TODO: really no intensity for sky?
    outColor.rgb = pow(texel.rgb, vec3(gamma));
    outColor.a = texel.a*alpha; // I think alpha shouldn't be modified by gamma and intensity
}
";

        private readonly string fragmentSrc3Dsprite = @"

// it gets attributes and uniforms from fragmentCommon3D

uniform sampler2D tex;

void main()
{
    vec4 texel = texture(tex, passTexCoord);

    // apply gamma correction and intensity
    texel.rgb *= intensity;
    outColor.rgb = pow(texel.rgb, vec3(gamma));
    outColor.a = texel.a*alpha; // I think alpha shouldn't be modified by gamma and intensity
}
";

        private readonly string fragmentSrc3DspriteAlpha = @"

// it gets attributes and uniforms from fragmentCommon3D

uniform sampler2D tex;

void main()
{
    vec4 texel = texture(tex, passTexCoord);

    if(texel.a <= 0.666)
        discard;

    // apply gamma correction and intensity
    texel.rgb *= intensity;
    outColor.rgb = pow(texel.rgb, vec3(gamma));
    outColor.a = texel.a*alpha; // I think alpha shouldn't be modified by gamma and intensity
}
";

        private readonly string vertexSrc3Dwater = @"

// it gets attributes and uniforms from vertexCommon3D
void main()
{
    passTexCoord = texCoord;

    gl_Position = transProjView * transModel * vec4(position, 1.0);
}
";

        private readonly string vertexSrcAlias = @"

// it gets attributes and uniforms from vertexCommon3D

out vec4 passColor;

void main()
{
    passColor = vertColor*overbrightbits;
    passTexCoord = texCoord;
    gl_Position = transProjView * transModel * vec4(position, 1.0);
}
";

        private readonly string fragmentSrcAlias = @"

// it gets attributes and uniforms from fragmentCommon3D

uniform sampler2D tex;

in vec4 passColor;

void main()
{
    vec4 texel = texture(tex, passTexCoord);

    // apply gamma correction and intensity
    texel.rgb *= intensity;
    texel.a *= alpha; // is alpha even used here?
    texel *= min(vec4(1.5), passColor);

    outColor.rgb = pow(texel.rgb, vec3(gamma));
    outColor.a = texel.a; // I think alpha shouldn't be modified by gamma and intensity
}
";

        private readonly string fragmentSrcAliasColor = @"

// it gets attributes and uniforms from fragmentCommon3D

in vec4 passColor;

void main()
{
    vec4 texel = passColor;

    // apply gamma correction and intensity
    // texel.rgb *= intensity; // TODO: color-only rendering probably shouldn't use intensity?
    texel.a *= alpha; // is alpha even used here?
    outColor.rgb = pow(texel.rgb, vec3(gamma));
    outColor.a = texel.a; // I think alpha shouldn't be modified by gamma and intensity
}
";

        private readonly string vertexSrcParticles = @"

// it gets attributes and uniforms from vertexCommon3D

out vec4 passColor;

void main()
{
    passColor = vertColor;
    gl_Position = transProjView * transModel * vec4(position, 1.0);

    // abusing texCoord for pointSize, pointDist for particles
    float pointDist = texCoord.y*0.1; // with factor 0.1 it looks good.

    gl_PointSize = texCoord.x/pointDist;
}
";

        private readonly string fragmentSrcParticles = @"

// it gets attributes and uniforms from fragmentCommon3D

in vec4 passColor;

void main()
{
    vec2 offsetFromCenter = 2.0*(gl_PointCoord - vec2(0.5, 0.5)); // normalize so offset is between 0 and 1 instead 0 and 0.5
    float distSquared = dot(offsetFromCenter, offsetFromCenter);
    if(distSquared > 1.0) // this makes sure the particle is round
        discard;

    vec4 texel = passColor;

    // apply gamma correction and intensity
    //texel.rgb *= intensity; TODO: intensity? Probably not?
    outColor.rgb = pow(texel.rgb, vec3(gamma));

    // I want the particles to fade out towards the edge, the following seems to look nice
    texel.a *= min(1.0, particleFadeFactor*(1.0 - distSquared));

    outColor.a = texel.a; // I think alpha shouldn't be modified by gamma and intensity
}
";

        private readonly string fragmentSrcParticlesSquare = @"

// it gets attributes and uniforms from fragmentCommon3D

in vec4 passColor;

void main()
{
    // outColor = passColor;
    // so far we didn't use gamma correction for square particles, but this way
    // uniCommon is referenced so hopefully Intels Ivy Bridge HD4000 GPU driver
    // for Windows stops shitting itself (see https://github.com/yquake2/yquake2/issues/391)
    outColor.rgb = pow(passColor.rgb, vec3(gamma));
    outColor.a = passColor.a;
}
";

    }
}
