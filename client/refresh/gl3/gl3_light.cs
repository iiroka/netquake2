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

        // bit: 1 << i for light number i, will be or'ed into msurface_t::dlightbits if surface is affected by this light
        private void GL3_MarkLights(in dlight_t light, int bit, mnode_or_leaf_t anode)
        {
            // cplane_t *splitplane;
            // float dist;
            // msurface_t *surf;
            // int i;
            // int sidebit;

            if (anode.contents != -1)
            {
                return;
            }

            var node = (mnode_t)anode;
            var splitplane = node.plane!;
            var dist = Vector3.Dot(light.origin, splitplane.normal) - splitplane.dist;

            if (dist > light.intensity - DLIGHT_CUTOFF)
            {
                GL3_MarkLights(light, bit, node.children[0]!);
                return;
            }

            if (dist < -light.intensity + DLIGHT_CUTOFF)
            {
                GL3_MarkLights(light, bit, node.children[1]!);
                return;
            }

            /* mark the polygons */

            for (int i = 0; i < node.numsurfaces; i++)
            {
                ref var surf = ref gl3_worldmodel!.surfaces[node.firstsurface + i];
                if (surf.dlightframe != r_dlightframecount)
                {
                    surf.dlightbits = 0;
                    surf.dlightframe = r_dlightframecount;
                }

                dist = Vector3.Dot(light.origin, surf.plane!.normal) - surf.plane.dist;

                int sidebit;
                if (dist >= 0)
                {
                    sidebit = 0;
                }
                else
                {
                    sidebit = SURF_PLANEBACK;
                }

                if ((surf.flags & SURF_PLANEBACK) != sidebit)
                {
                    continue;
                }

                surf.dlightbits |= bit;
            }

            GL3_MarkLights(light, bit, node.children[0]!);
            GL3_MarkLights(light, bit, node.children[1]!);
        }

        private void GL3_PushDlights(GL gl)
        {
            /* because the count hasn't advanced yet for this frame */
            r_dlightframecount = gl3_framecount + 1;


            gl3state.uniLightsData.numDynLights = (uint)gl3_newrefdef.num_dlights;

            if (gl3_newrefdef.num_dlights > 0) {
                ref var l = ref gl3_newrefdef.dlights[0];
                GL3_MarkLights(l, 1, gl3_worldmodel!.nodes[0]);
                gl3state.uniLightsData.dynLights0.origin = new Vector3D<float>(l.origin.X, l.origin.Y, l.origin.Z);
                gl3state.uniLightsData.dynLights0.color = new Vector3D<float>(l.color.X, l.color.Y, l.color.Z);
                gl3state.uniLightsData.dynLights0.intensity = l.intensity;
            } else {
                gl3state.uniLightsData.dynLights0.origin = new Vector3D<float>();
                gl3state.uniLightsData.dynLights0.color = new Vector3D<float>();
                gl3state.uniLightsData.dynLights0.intensity = 0;
            }

            if (gl3_newrefdef.num_dlights > 1) {
                ref var l = ref gl3_newrefdef.dlights[1];
                GL3_MarkLights(l, 1 << 1, gl3_worldmodel!.nodes[0]);
                gl3state.uniLightsData.dynLights1.origin = new Vector3D<float>(l.origin.X, l.origin.Y, l.origin.Z);
                gl3state.uniLightsData.dynLights1.color = new Vector3D<float>(l.color.X, l.color.Y, l.color.Z);
                gl3state.uniLightsData.dynLights1.intensity = l.intensity;
            } else {
                gl3state.uniLightsData.dynLights1.origin = new Vector3D<float>();
                gl3state.uniLightsData.dynLights1.color = new Vector3D<float>();
                gl3state.uniLightsData.dynLights1.intensity = 0;
            }

            if (gl3_newrefdef.num_dlights > 2) {
                ref var l = ref gl3_newrefdef.dlights[2];
                GL3_MarkLights(l, 1 << 2, gl3_worldmodel!.nodes[0]);
                gl3state.uniLightsData.dynLights2.origin = new Vector3D<float>(l.origin.X, l.origin.Y, l.origin.Z);
                gl3state.uniLightsData.dynLights2.color = new Vector3D<float>(l.color.X, l.color.Y, l.color.Z);
                gl3state.uniLightsData.dynLights2.intensity = l.intensity;
            } else {
                gl3state.uniLightsData.dynLights2.origin = new Vector3D<float>();
                gl3state.uniLightsData.dynLights2.color = new Vector3D<float>();
                gl3state.uniLightsData.dynLights2.intensity = 0;
            }

            if (gl3_newrefdef.num_dlights > 3) {
                ref var l = ref gl3_newrefdef.dlights[3];
                GL3_MarkLights(l, 1 << 3, gl3_worldmodel!.nodes[0]);
                gl3state.uniLightsData.dynLights3.origin = new Vector3D<float>(l.origin.X, l.origin.Y, l.origin.Z);
                gl3state.uniLightsData.dynLights3.color = new Vector3D<float>(l.color.X, l.color.Y, l.color.Z);
                gl3state.uniLightsData.dynLights3.intensity = l.intensity;
            } else {
                gl3state.uniLightsData.dynLights3.origin = new Vector3D<float>();
                gl3state.uniLightsData.dynLights3.color = new Vector3D<float>();
                gl3state.uniLightsData.dynLights3.intensity = 0;
            }

            // TODO: Lots of stuff

            GL3_UpdateUBOLights(gl);
        }

        private int RecursiveLightPoint(mnode_or_leaf_t anode, in Vector3 start, in Vector3 end)
        {
            // float front, back, frac;
            // int side;
            // cplane_t *plane;
            // vec3_t mid;
            // msurface_t *surf;
            // int s, t, ds, dt;
            // int i;
            // mtexinfo_t *tex;
            // byte *lightmap;
            // int maps;
            // int r;

            if (anode.contents != -1)
            {
                return -1;     /* didn't hit anything */
            }

            var node = (mnode_t)anode;
            /* calculate mid point */
            var plane = node.plane!;
            var front = Vector3.Dot(start, plane.normal) - plane.dist;
            var back = Vector3.Dot(end, plane.normal) - plane.dist;
            var side = front < 0;

            if ((back < 0) == side)
            {
                return RecursiveLightPoint(node.children[side ? 1 : 0]!, start, end);
            }

            var frac = front / (front - back);
            var mid = start + (end - start) * frac;

            /* go down front side */
            var r = RecursiveLightPoint(node.children[side ? 1 : 0]!, start, mid);
            if (r >= 0)
            {
                return r;     /* hit something */
            }

            if ((back < 0) == side)
            {
                return -1;     /* didn't hit anuthing */
            }

            /* check for impact on this node */
            var lightspot = mid;
            lightplane = plane;


            for (int i = 0; i < node.numsurfaces; i++)
            {
                ref var surf = ref gl3_worldmodel!.surfaces[node.firstsurface + i];
                if ((surf.flags & (SURF_DRAWTURB | SURF_DRAWSKY)) != 0)
                {
                    continue; /* no lightmaps */
                }

                var tex = surf.texinfo!;

                int s = (int)(Vector3.Dot(mid, new Vector3(tex.vecs[0].X, tex.vecs[0].Y, tex.vecs[0].Z)) + tex.vecs[0].W);
                int t = (int)(Vector3.Dot(mid, new Vector3(tex.vecs[1].X, tex.vecs[1].Y, tex.vecs[1].Z)) + tex.vecs[1].W);

                if ((s < surf.texturemins[0]) ||
                    (t < surf.texturemins[1]))
                {
                    continue;
                }

                int ds = s - surf.texturemins[0];
                int dt = t - surf.texturemins[1];

                if ((ds > surf.extents[0]) || (dt > surf.extents[1]))
                {
                    continue;
                }

                if (surf.samples_b == null)
                {
                    return 0;
                }

                ds >>= 4;
                dt >>= 4;

                // lightmap = surf->samples;
                pointcolor = Vector3.Zero;

                var lightmap_i = 3 * (dt * ((surf.extents[0] >> 4) + 1) + ds);

                for (int maps = 0; maps < MAX_LIGHTMAPS_PER_SURFACE && surf.styles[maps] != 255; maps++)
                {
                    var rgb = gl3_newrefdef.lightstyles[surf.styles[maps]].rgb;

                    /* Apply light level to models */
                    pointcolor.X += surf.samples_b[lightmap_i + 0] * rgb[0] * r_modulate!.Float * (1.0f / 255);
                    pointcolor.Y += surf.samples_b[lightmap_i + 1] * rgb[1] * r_modulate!.Float * (1.0f / 255);
                    pointcolor.Z += surf.samples_b[lightmap_i + 2] * rgb[2] * r_modulate!.Float * (1.0f / 255);

                    lightmap_i += 3 * ((surf.extents[0] >> 4) + 1) * ((surf.extents[1] >> 4) + 1);
                }

                return 1;
            }

            /* go down back side */
            return RecursiveLightPoint(node.children[side ? 0 : 1]!, mid, end);
        }

        private void GL3_LightPoint(in entity_t? currententity, in Vector3 p, out Vector3 color)
        {
            // vec3_t end;
            // float r;
            // int lnum;
            // dlight_t *dl;
            // vec3_t dist;
            // float add;

            if (gl3_worldmodel?.lightdata == null || currententity == null)
            {
                color = new Vector3(1.0f);
                return;
            }

            var end = p;
            end.Z -= 2048;

            // TODO: don't just aggregate the color, but also save position of brightest+nearest light
            //       for shadow position and maybe lighting on model?

            var r = RecursiveLightPoint(gl3_worldmodel!.nodes[0], p, end);

            if (r == -1)
            {
                color = Vector3.Zero;
            }
            else
            {
                color = pointcolor;
            }

            /* add dynamic lights */

            // for (lnum = 0; lnum < gl3_newrefdef.num_dlights; lnum++, dl++)
            // {
            //     dl = gl3_newrefdef.dlights;
            //     VectorSubtract(currententity->origin,
            //             dl->origin, dist);
            //     add = dl->intensity - VectorLength(dist);
            //     add *= (1.0f / 256.0f);

            //     if (add > 0)
            //     {
            //         VectorMA(color, add, dl->color, color);
            //     }
            // }

            // VectorScale(color, r_modulate->value, color);
        }


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