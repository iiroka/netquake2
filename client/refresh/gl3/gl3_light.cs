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
 * Lightmaps and dynamic lighting
 *
 * =======================================================================
 */

using Silk.NET.OpenGL;
using Silk.NET.Maths;
using System.Numerics;

namespace Quake2 {

    partial class QRefGl3
    {
        private const int DLIGHT_CUTOFF = 64;

        private int r_dlightframecount;
        private Vector3 pointcolor;
        private QShared.cplane_t? lightplane; /* used as shadow plane */
        private Vector3 lightspot;

        /*
        * Combine and scale multiple lightmaps into the floating format in blocklights
        */
        private void GL3_BuildLightMap(in msurface_t surf, int offsetInLMbuf, int stride)
        {
            // int smax, tmax;
            // int r, g, b, a, max;
            // int i, j, size, map, numMaps;
            // byte *lightmap;

            if ((surf.texinfo!.flags &
                (QCommon.SURF_SKY | QCommon.SURF_TRANS33 | QCommon.SURF_TRANS66 | QCommon.SURF_WARP)) != 0)
            {
                ri.Sys_Error(QShared.ERR_DROP, "GL3_BuildLightMap called for non-lit surface");
            }

            int smax = (surf.extents[0] >> 4) + 1;
            int tmax = (surf.extents[1] >> 4) + 1;
            int size = smax * tmax;

            stride -= (smax << 2);

            if (size > 34*34*3)
            {
                ri.Sys_Error(QShared.ERR_DROP, "Bad s_blocklights size");
            }

            // count number of lightmaps surf actually has
            int numMaps;
            for (numMaps = 0; numMaps < MAX_LIGHTMAPS_PER_SURFACE && surf.styles[numMaps] != 255; ++numMaps)
            {}

            int map;
            if (surf.samples_b == null)
            {
                // no lightmap samples? set at least one lightmap to fullbright, rest to 0 as normal

                if (numMaps == 0)  numMaps = 1; // make sure at least one lightmap is set to fullbright

                for (map = 0; map < MAX_LIGHTMAPS_PER_SURFACE; ++map)
                {
                    // we always create 4 (MAX_LIGHTMAPS_PER_SURFACE) lightmaps.
                    // if surf has less (numMaps < 4), the remaining ones are zeroed out.
                    // this makes sure that all 4 lightmap textures in gl3state.lightmap_textureIDs[i] have the same layout
                    // and the shader can use the same texture coordinates for all of them

                    byte c = (map < numMaps) ? (byte)255 : (byte)0;
                    int dest_i = map * gl3lightmapstate_t.LIGHTMAP_BUFFER_SIZE + offsetInLMbuf;

                    for (int i = 0; i < tmax; i++, dest_i += stride)
                    {
                        Array.Fill(gl3_lms.lightmap_buffers, c, dest_i, 4 * smax);
                        dest_i += 4*smax;
                    }
                }

                return;
            }

            /* add all the lightmaps */

            // Note: dynamic lights aren't handled here anymore, they're handled in the shader

            // as we don't apply scale here anymore, nor blend the numMaps lightmaps together,
            // the code has gotten a lot easier and we can copy directly from surf->samples to dest
            // without converting to float first etc

            var lightmap_b = surf.samples_b;
            var lightmap_i = surf.samples_i;

            for(map=0; map<numMaps; ++map)
            {
                int dest_i = map * gl3lightmapstate_t.LIGHTMAP_BUFFER_SIZE + offsetInLMbuf;
                int idxInLightmap = 0;
                for (int i = 0; i < tmax; i++, dest_i += stride)
                {
                    for (int j = 0; j < smax; j++)
                    {
                        var r = lightmap_b[lightmap_i + idxInLightmap * 3 + 0];
                        var g = lightmap_b[lightmap_i + idxInLightmap * 3 + 1];
                        var b = lightmap_b[lightmap_i + idxInLightmap * 3 + 2];

                        /* determine the brightest of the three color components */
                        byte max;
                        if (r > g)  max = r;
                        else  max = g;

                        if (b > max)  max = b;

                        /* alpha is ONLY used for the mono lightmap case. For this
                        reason we set it to the brightest of the color components
                        so that things don't get too dim. */
                        var a = max;

                        gl3_lms.lightmap_buffers[dest_i+0] = r;
                        gl3_lms.lightmap_buffers[dest_i+1] = g;
                        gl3_lms.lightmap_buffers[dest_i+2] = b;
                        gl3_lms.lightmap_buffers[dest_i+3] = a;

                        dest_i += 4;
                        ++idxInLightmap;
                    }
                }

                lightmap_i += size * 3; /* skip to next lightmap */
            }

            for ( ; map < MAX_LIGHTMAPS_PER_SURFACE; ++map)
            {
                // like above, fill up remaining lightmaps with 0

                int dest_i = map * gl3lightmapstate_t.LIGHTMAP_BUFFER_SIZE + offsetInLMbuf;

                for (int i = 0; i < tmax; i++, dest_i += stride)
                {
                    Array.Fill(gl3_lms.lightmap_buffers, (byte)0, dest_i, 4 * smax);
                    dest_i += 4*smax;
                }
            }
        }

    }
}