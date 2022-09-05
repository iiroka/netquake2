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
 * Lightmap handling
 *
 * =======================================================================
 */

using Silk.NET.OpenGL;
using Silk.NET.Maths;
using System.Numerics;

namespace Quake2 {

    partial class QRefGl3
    {
        private gl3lightmapstate_t gl3_lms = new gl3lightmapstate_t();

        private void GL3_LM_InitBlock()
        {
            Array.Fill(gl3_lms.allocated, 0);
        }

        private void GL3_LM_UploadBlock(GL gl)
        {
            int map;

            // NOTE: we don't use the dynamic lightmap anymore - all lightmaps are loaded at level load
            //       and not changed after that. they're blended dynamically depending on light styles
            //       though, and dynamic lights are (will be) applied in shader, hopefully per fragment.

            GL3_BindLightmap(gl, gl3_lms.current_lightmap_texture);

            // upload all 4 lightmaps
            for(map=0; map < MAX_LIGHTMAPS_PER_SURFACE; ++map)
            {
                GL3_SelectTMU(gl, TextureUnit.Texture1+map); // this relies on GL_TEXTURE2 being GL_TEXTURE1+1 etc
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

                gl3_lms.internal_format = (int)GLEnum.Rgba;
                gl.TexImage2D(TextureTarget.Texture2D, 0, gl3_lms.internal_format,
                            BLOCK_WIDTH, BLOCK_HEIGHT, 0, PixelFormat.Rgba,
                            PixelType.UnsignedByte, new ReadOnlySpan<byte>(gl3_lms.lightmap_buffers, map * gl3lightmapstate_t.LIGHTMAP_BUFFER_SIZE, gl3lightmapstate_t.LIGHTMAP_BUFFER_SIZE));
            }

            if (++gl3_lms.current_lightmap_texture == MAX_LIGHTMAPS)
            {
                ri.Sys_Error(QShared.ERR_DROP, "LM_UploadBlock() - MAX_LIGHTMAPS exceeded\n");
            }
        }

        /*
        * returns a texture number and the position inside it
        */
        private bool GL3_LM_AllocBlock(int w, int h, ref int x, ref int y)
        {
            int i, j;
            int best, best2;

            best = BLOCK_HEIGHT;

            for (i = 0; i < BLOCK_WIDTH - w; i++)
            {
                best2 = 0;

                for (j = 0; j < w; j++)
                {
                    if (gl3_lms.allocated[i + j] >= best)
                    {
                        break;
                    }

                    if (gl3_lms.allocated[i + j] > best2)
                    {
                        best2 = gl3_lms.allocated[i + j];
                    }
                }

                if (j == w)
                {
                    /* this is a valid spot */
                    x = i;
                    y = best = best2;
                }
            }

            if (best + h > BLOCK_HEIGHT)
            {
                return false;
            }

            for (i = 0; i < w; i++)
            {
                gl3_lms.allocated[x + i] = best + h;
            }

            return true;
        }

        private void GL3_LM_BuildPolygonFromSurface(in gl3brushmodel_t currentmodel, ref msurface_t fa)
        {
            /* reconstruct the polygon */
            ref var pedges = ref currentmodel.edges;
            int lnumverts = fa.numedges;

            var total = new Vector3D<float>();

            /* draw texture */
            var poly = new glpoly_t();
            poly.vertices = new gl3_3D_vtx_t[lnumverts];
            poly.next = fa.polys;
            poly.flags = fa.flags;
            fa.polys = poly;

            var normal =  new Vector3D<float>(fa.plane!.normal.X, fa.plane.normal.Y, fa.plane.normal.Z);

            if ((fa.flags & SURF_PLANEBACK) != 0)
            {
                // if for some reason the normal sticks to the back of the plane, invert it
                // so it's usable for the shader
                normal = -normal;
            }

            for (int i = 0; i < lnumverts; i++)
            {
                ref var vert = ref poly.vertices[i];

                var lindex = currentmodel.surfedges[fa.firstedge + i];

                Vector3 vec_;
                if (lindex > 0)
                {
                    ref var r_pedge = ref pedges[lindex];
                    vec_ = currentmodel.vertexes[r_pedge.v[0]].position;
                }
                else
                {
                    ref var r_pedge = ref pedges[-lindex];
                    vec_ = currentmodel.vertexes[r_pedge.v[1]].position;
                }

                var vec = new Vector3D<float>(vec_.X, vec_.Y, vec_.Z);
                var s = Vector3D.Dot(vec, new Vector3D<float>(fa.texinfo!.vecs[0].X, fa.texinfo.vecs[0].Y, fa.texinfo.vecs[0].Z)) + fa.texinfo.vecs[0].W;
                s /= fa.texinfo.image!.width;

                var t = Vector3D.Dot(vec, new Vector3D<float>(fa.texinfo!.vecs[1].X, fa.texinfo.vecs[1].Y, fa.texinfo.vecs[1].Z)) + fa.texinfo.vecs[1].W;
                t /= fa.texinfo.image.height;

                total += vec;
                vert.pos = vec;
                vert.texCoord.X = s;
                vert.texCoord.Y = t;

                /* lightmap texture coordinates */
                s = Vector3D.Dot(vec, new Vector3D<float>(fa.texinfo!.vecs[0].X, fa.texinfo.vecs[0].Y, fa.texinfo.vecs[0].Z)) + fa.texinfo.vecs[0].W;
                s -= fa.texturemins[0];
                s += fa.light_s * 16;
                s += 8;
                s /= BLOCK_WIDTH * 16; /* fa->texinfo->texture->width; */

                t = Vector3D.Dot(vec, new Vector3D<float>(fa.texinfo!.vecs[1].X, fa.texinfo.vecs[1].Y, fa.texinfo.vecs[1].Z)) + fa.texinfo.vecs[1].W;
                t -= fa.texturemins[1];
                t += fa.light_t * 16;
                t += 8;
                t /= BLOCK_HEIGHT * 16; /* fa->texinfo->texture->height; */

                vert.lmTexCoord.X = s;
                vert.lmTexCoord.Y = t;

                vert.normal = normal;
                vert.lightFlags = 0;
            }
        }

        private void GL3_LM_CreateSurfaceLightmap(GL gl, ref msurface_t surf)
        {
            int smax, tmax;

            if ((surf.flags & (SURF_DRAWSKY | SURF_DRAWTURB)) != 0)
            {
                return;
            }

            smax = (surf.extents[0] >> 4) + 1;
            tmax = (surf.extents[1] >> 4) + 1;

            if (!GL3_LM_AllocBlock(smax, tmax, ref surf.light_s, ref surf.light_t))
            {
                GL3_LM_UploadBlock(gl);
                GL3_LM_InitBlock();

                if (!GL3_LM_AllocBlock(smax, tmax, ref surf.light_s, ref surf.light_t))
                {
                    ri.Sys_Error(QShared.ERR_FATAL, $"Consecutive calls to LM_AllocBlock({smax},{tmax}) failed\n");
                }
            }

            surf.lightmaptexturenum = gl3_lms.current_lightmap_texture;

            GL3_BuildLightMap(surf, (surf.light_t * BLOCK_WIDTH + surf.light_s) * LIGHTMAP_BYTES, BLOCK_WIDTH * LIGHTMAP_BYTES);
        }

        private void GL3_LM_BeginBuildingLightmaps(in gl3brushmodel_t m)
        {

            Array.Fill(gl3_lms.allocated, 0);

            gl3_framecount = 1; /* no dlightcache */

            /* setup the base lightstyles so the lightmaps
            won't have to be regenerated the first time
            they're seen */
            var lightstyles = new lightstyle_t[QRef.MAX_LIGHTSTYLES];
            for (int i = 0; i < QRef.MAX_LIGHTSTYLES; i++)
            {
                lightstyles[i].rgb = new float[3]{ 1, 1, 1 };
                lightstyles[i].white = 3;
            }

            gl3_newrefdef.lightstyles = lightstyles;

            gl3_lms.current_lightmap_texture = 0;
            gl3_lms.internal_format = (int)GLEnum.Rgba;

            // Note: the dynamic lightmap used to be initialized here, we don't use that anymore.
        }

        private void GL3_LM_EndBuildingLightmaps(GL gl)
        {
            GL3_LM_UploadBlock(gl);
        }

    }
}