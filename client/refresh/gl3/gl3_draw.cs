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
 * Drawing of all images that are not textures
 *
 * =======================================================================
 */
using Silk.NET.OpenGL;

namespace Quake2 {

    partial class QRefGl3
    {
        private uint[] d_8to24table = {};

        private gl3image_t? draw_chars;

        private uint vbo2D = 0, vao2D = 0, vao2Dcolor = 0; // vao2D is for textured rendering, vao2Dcolor for color-only

        private unsafe void GL3_Draw_InitLocal(GL gl)
        {
            /* load console characters */
            draw_chars = GL3_FindImage(gl, "pics/conchars.pcx", imagetype_t.it_pic);
            if (draw_chars == null)
            {
                ri.Sys_Error(QShared.ERR_FATAL, "Couldn't load pics/conchars.pcx");
            }

            // set up attribute layout for 2D textured rendering
            vao2D = gl.GenVertexArray();
            gl.BindVertexArray(vao2D);

            vbo2D = gl.GenBuffer();
            GL3_BindVBO(gl, vbo2D);

            GL3_UseProgram(gl, gl3state.si2D.shaderProgram);

            gl.EnableVertexAttribArray(GL3_ATTRIB_POSITION);
            // Note: the glVertexAttribPointer() configuration is stored in the VAO, not the shader or sth
            //       (that's why I use one VAO per 2D shader)
            gl.VertexAttribPointer(GL3_ATTRIB_POSITION, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);

            gl.EnableVertexAttribArray(GL3_ATTRIB_TEXCOORD);
            gl.VertexAttribPointer(GL3_ATTRIB_TEXCOORD, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)(2*sizeof(float)));

            // set up attribute layout for 2D flat color rendering

            vao2Dcolor = gl.GenVertexArray();
            gl.BindVertexArray(vao2Dcolor);

            GL3_BindVBO(gl, vbo2D); // yes, both VAOs share the same VBO

            GL3_UseProgram(gl, gl3state.si2Dcolor.shaderProgram);

            gl.EnableVertexAttribArray(GL3_ATTRIB_POSITION);
            gl.VertexAttribPointer(GL3_ATTRIB_POSITION, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), (void*)0);

            GL3_BindVAO(gl, 0);
        }

        // bind the texture before calling this
        private unsafe void drawTexturedRectangle(GL gl, float x, float y, float w, float h,
                            float sl, float tl, float sh, float th)
        {
            /*
            *  x,y+h      x+w,y+h
            * sl,th--------sh,th
            *  |             |
            *  |             |
            *  |             |
            * sl,tl--------sh,tl
            *  x,y        x+w,y
            */

            float[] vBuf = {
            //  X,   Y,   S,  T
                x,   y+h, sl, th,
                x,   y,   sl, tl,
                x+w, y+h, sh, th,
                x+w, y,   sh, tl
            };

            GL3_BindVAO(gl, vao2D);

            // Note: while vao2D "remembers" its vbo for drawing, binding the vao does *not*
            //       implicitly bind the vbo, so I need to explicitly bind it before glBufferData()
            GL3_BindVBO(gl, vbo2D);
            fixed (void* d = vBuf)
            {            
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vBuf.Length * sizeof(float)), d, BufferUsageARB.StreamDraw);
            }

            gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            //glMultiDrawArrays(mode, first, count, drawcount) ??
        }

        /*
        * Draws one 8*8 graphics character with 0 being transparent.
        * It can be clipped to the top of the screen to allow the console to be
        * smoothly scrolled off.
        */
        public void DrawCharScaled(Silk.NET.Windowing.IWindow window, int x, int y, int num, float scale)
        {
            var gl = GL.GetApi(window);
            // int row, col;
            // float frow, fcol, size, scaledSize;
            num &= 255;

            if ((num & 127) == 32)
            {
                return; /* space */
            }

            if (y <= -8)
            {
                return; /* totally off screen */
            }

            int row = num >> 4;
            int col = num & 15;

            float frow = row * 0.0625f;
            float fcol = col * 0.0625f;
            float size = 0.0625f;

            float scaledSize = 8*scale;

            // TODO: batchen?

            GL3_UseProgram(gl, gl3state.si2D.shaderProgram);
            GL3_Bind(gl, draw_chars!.texnum);
            drawTexturedRectangle(gl, x, y, scaledSize, scaledSize, fcol, frow, fcol+size, frow+size);
        }


        private gl3image_t? GL3_Draw_FindPic(GL gl, string name)
        {
            if ((name[0] != '/') && (name[0] != '\\'))
            {
                var fullname = $"pics/{name}.pcx";
                return GL3_FindImage(gl, fullname, imagetype_t.it_pic);
            }
            else
            {
                return GL3_FindImage(gl, name.Substring(1), imagetype_t.it_pic);
            }
        }


        public void DrawGetPicSize (Silk.NET.Windowing.IWindow window, out int w, out int h, string name)
        {
            var gl = GL.GetApi(window);

            var img = GL3_Draw_FindPic(gl, name);

            if (img == null)
            {
                w = h = -1;
                return;
            }

            w = img.width;
            h = img.height;
        }

        public void DrawStretchPic (Silk.NET.Windowing.IWindow window, int x, int y, int w, int h, string name)
        {
            var gl = GL.GetApi(window);

            var img = GL3_Draw_FindPic(gl, name);

            if (img == null)
            {
                R_Printf(QShared.PRINT_ALL, $"Can't find pic: {name}\n");
                return;
            }

            GL3_UseProgram(gl, gl3state.si2D.shaderProgram);
            GL3_Bind(gl, img.texnum);

            drawTexturedRectangle(gl, x, y, w, h, img.sl, img.tl, img.sh, img.th);
        }

        public void DrawPicScaled (Silk.NET.Windowing.IWindow window, int x, int y, string name, float factor)
        {
            var gl = GL.GetApi(window);

            var img = GL3_Draw_FindPic(gl, name);

            if (img == null)
            {
                R_Printf(QShared.PRINT_ALL, $"Can't find pic: {name}\n");
                return;
            }

            GL3_UseProgram(gl, gl3state.si2D.shaderProgram);
            GL3_Bind(gl, img.texnum);

            drawTexturedRectangle(gl, x, y, img.width*factor, img.height*factor, img.sl, img.tl, img.sh, img.th);
        }

        // void
        // GL3_DrawFrameBufferObject(int x, int y, int w, int h, GLuint fboTexture, const float v_blend[4])
        // {
        //     qboolean underwater = (gl3_newrefdef.rdflags & RDF_UNDERWATER) != 0;
        //     gl3ShaderInfo_t* shader = underwater ? &gl3state.si2DpostProcessWater
        //                                         : &gl3state.si2DpostProcess;
        //     GL3_UseProgram(shader->shaderProgram);
        //     GL3_Bind(fboTexture);

        //     if(underwater && shader->uniLmScalesOrTime != -1)
        //     {
        //         glUniform1f(shader->uniLmScalesOrTime, gl3_newrefdef.time);
        //     }
        //     if(shader->uniVblend != -1)
        //     {
        //         glUniform4fv(shader->uniVblend, 1, v_blend);
        //     }

        //     drawTexturedRectangle(x, y, w, h, 0, 1, 1, 0);
        // }


        private void GL3_Draw_GetPalette()
        {
            /* get the palette */
            QPCX.Load(ri, "pics/colormap.pcx", out var pic, out var pal, out var width, out var height);

            if (pal == null)
            {
                ri.Sys_Error(QShared.ERR_FATAL, "Couldn't load pics/colormap.pcx");
            }

            d_8to24table = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                byte r = pal![i * 3 + 0];
                byte g = pal[i * 3 + 1];
                byte b = pal[i * 3 + 2];

                uint v = (uint)(255u << 24) + (uint)(r << 0) + (uint)(g << 8) + (uint)(b << 16);
                d_8to24table[i] = v;
            }

            d_8to24table[255] &= 0xffffff; /* 255 is transparent */
        }

    }
}
