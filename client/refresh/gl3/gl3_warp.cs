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
 * Warps. Used on water surfaces und for skybox rotation.
 *
 * =======================================================================
 */

using Silk.NET.OpenGL;
using Silk.NET.Maths;
using System.Numerics;

namespace Quake2 {

    partial class QRefGl3
    {

        private void R_BoundPoly(int numverts, Vector3[] verts, out Vector3 mins, out Vector3 maxs)
        {
            mins = new Vector3(9999, 9999, 9999);
            maxs = new Vector3(-9999, -9999, -9999);

            for (int i = 0; i < numverts; i++)
            {
                mins = Vector3.Min(mins, verts[i]);
                maxs = Vector3.Max(maxs, verts[i]);
            }
        }

        private const float SUBDIVIDE_SIZE = 64.0f;

        private void R_SubdividePolygon(int numverts, Vector3[] verts, ref msurface_t warpface)
        {
            // int i, j, k;
            // vec3_t mins, maxs;
            // float m;
            // float *v;
            // vec3_t front[64], back[64];
            // int f, b;
            // float dist[64];
            // float frac;
            // glpoly_t *poly;
            // float s, t;
            // vec3_t total;
            // float total_s, total_t;
            // vec3_t normal;

            var normal = new Vector3D<float>(warpface.plane!.normal.X, warpface.plane!.normal.Y, warpface.plane!.normal.Z);

            if (numverts > 60)
            {
                ri.Sys_Error(QShared.ERR_DROP, $"numverts = {numverts}");
            }

            R_BoundPoly(numverts, verts, out var mins_, out var maxs_);
            var mins = new float[3]{ mins_.X, mins_.Y, mins_.Z };
            var maxs = new float[3]{ maxs_.X, maxs_.Y, maxs_.Z };

            var dist = new float[64];
            var front = new Vector3[64];
            var back = new Vector3[64];

            for (int i = 0; i < 3; i++)
            {
                float m = (mins[i] + maxs[i]) * 0.5f;
                m = SUBDIVIDE_SIZE * MathF.Floor(m / SUBDIVIDE_SIZE + 0.5f);

                if (maxs[i] - m < 8)
                {
                    continue;
                }

                if (m - mins[i] < 8)
                {
                    continue;
                }

                /* cut it */
                int j;
                for (j = 0; j < numverts; j++)
                {
                    if (i == 0)
                        dist[j] = verts[j].X - m;
                    else if (i == 1)
                        dist[j] = verts[j].Y - m;
                    else
                        dist[j] = verts[j].Z - m;
                }

                /* wrap cases */
                dist[j] = dist[0];
                verts[j] = verts[0];

                int f = 0;
                int b = 0;
                // v = verts;

                for (j = 0; j < numverts; j++)
                {
                    if (dist[j] >= 0)
                    {
                        front[f] = verts[j];
                        f++;
                    }

                    if (dist[j] <= 0)
                    {
                        back[b] = verts[j];
                        b++;
                    }

                    if ((dist[j] == 0) || (dist[j + 1] == 0))
                    {
                        continue;
                    }

                    if ((dist[j] > 0) != (dist[j + 1] > 0))
                    {
                        /* clip point */
                        var frac = dist[j] / (dist[j] - dist[j + 1]);

                        front[f] = verts[j] + frac * (verts[j + 1] - verts[j]);
                        back[b] = front[f];

                        f++;
                        b++;
                    }
                }

                R_SubdividePolygon(f, front, ref warpface);
                R_SubdividePolygon(b, back, ref warpface);
                return;
            }

            /* add a point in the center to help keep warp valid */
            var poly = new glpoly_t();
            poly.vertices = new gl3_3D_vtx_t[numverts + 2];
            poly.next = warpface.polys;
            warpface.polys = poly;
            Vector3D<float> total = new Vector3D<float>();
            float total_s = 0;
            float total_t = 0;

            for (int i = 0; i < numverts; i++)
            {
                var vert3d = new Vector3D<float>(verts[i].X, verts[i].Y, verts[i].Z);
                poly.vertices[i + 1].pos = vert3d;
                float s = Vector3.Dot(verts[i], new Vector3(warpface.texinfo!.vecs[0].X, warpface.texinfo.vecs[0].Y, warpface.texinfo.vecs[0].Z));
                float t = Vector3.Dot(verts[i], new Vector3(warpface.texinfo.vecs[1].X, warpface.texinfo.vecs[1].Y, warpface.texinfo.vecs[1].Z));

                total_s += s;
                total_t += t;
                total += vert3d;

                poly.vertices[i + 1].texCoord.X = s;
                poly.vertices[i + 1].texCoord.Y = t;
                poly.vertices[i + 1].normal = normal;
                poly.vertices[i + 1].lightFlags = 0;
            }

            poly.vertices[0].pos = total * (1.0f / numverts);
            poly.vertices[0].texCoord.X = total_s / numverts;
            poly.vertices[0].texCoord.Y = total_t / numverts;
            poly.vertices[0].normal = normal;

            /* copy first vertex to last */
            //memcpy(poly->vertices[i + 1], poly->vertices[1], sizeof(poly->vertices[0]));
            poly.vertices[numverts + 1] = poly.vertices[1];
        }

        /*
        * Breaks a polygon up along axial 64 unit
        * boundaries so that turbulent and sky warps
        * can be done reasonably.
        */
        private void GL3_SubdivideSurface(ref msurface_t fa, in gl3brushmodel_t loadmodel)
        {
            /* convert edges back to a normal polygon */
            int numverts = 0;
            var verts = new Vector3[64];

            for (int i = 0; i < fa.numedges; i++)
            {
                int lindex = loadmodel.surfedges[fa.firstedge + i];

                if (lindex > 0)
                {
                    verts[numverts++] = loadmodel.vertexes[loadmodel.edges[lindex].v[0]].position;
                }
                else
                {
                    verts[numverts++] = loadmodel.vertexes[loadmodel.edges[-lindex].v[1]].position;
                }
            }

            R_SubdividePolygon(numverts, verts, ref fa);
        }

        // ########### below: Sky-specific stuff ##########

        private const float ON_EPSILON = 0.1f; /* point on plane side epsilon */
        private const int MAX_CLIP_VERTS = 64;


        private readonly int[] skytexorder = new int[]{0, 2, 1, 3, 4, 5};

        private float[][] skymins, skymaxs;
        private float sky_min, sky_max;

        private float skyrotate;
        private Vector3 skyaxis;
        private gl3image_t?[] sky_images;

        /* 3dstudio environment map names */
        private string[] suf = {"rt", "bk", "lf", "ft", "up", "dn"};

        private Vector3[] skyclip = {
            new Vector3(1, 1, 0),
            new Vector3(1, -1, 0),
            new Vector3(0, -1, 1),
            new Vector3(0, 1, 1),
            new Vector3(1, 0, 1),
            new Vector3(-1, 0, 1)
        };
        private int c_sky;

        private int[][] st_to_vec = {
            new int[]{3, -1, 2},
            new int[]{-3, 1, 2},

            new int[]{1, 3, 2},
            new int[]{-1, -3, 2},

            new int[]{-2, -1, 3}, /* 0 degrees yaw, look straight up */
            new int[]{2, -1, -3} /* look straight down */
        };

        private int[][] vec_to_st = {
            new int[]{-2, 3, 1},
            new int[]{2, 3, -1},

            new int[]{1, 3, 2},
            new int[]{-1, 3, -2},

            new int[]{-2, -1, 3},
            new int[]{-2, 1, -3}
        };

        public void SetSky(Silk.NET.Windowing.IWindow window, string name, float rotate, in Vector3 axis)
        {
            // int i;
            // char pathname[MAX_QPATH];
            // char skyname[MAX_QPATH];
            var gl = GL.GetApi(window);

            // Q_strlcpy(skyname, name, sizeof(skyname));
            skyrotate = rotate;
            skyaxis = axis;

            sky_images = new gl3image_t?[6];
            for (int i = 0; i < 6; i++)
            {
                // NOTE: there might be a paletted .pcx version, which was only used
                //       if gl_config.palettedtexture so it *shouldn't* be relevant for he GL3 renderer
                var pathname = $"env/{name}{suf[i]}.tga";

                sky_images[i] = GL3_FindImage(gl, pathname, imagetype_t.it_sky);

                if (sky_images[i] == null || sky_images[i] == gl3_notexture)
                {
                    pathname = $"pics/Skies/{name}{suf[i]}.m8";

                    sky_images[i] = GL3_FindImage(gl, pathname, imagetype_t.it_sky);
                }

                if (sky_images[i] == null)
                {
                    sky_images[i] = gl3_notexture;
                }

                sky_min = 1.0f / 512;
                sky_max = 511.0f / 512;
            }            
        }

        private void DrawSkyPolygon(int nump, in Vector3[] vecs)
        {
            // int i, j;
            // vec3_t v, av;
            // float s, t, dv;
            // int axis;
            // float *vp;

            c_sky++;

            /* decide which face it maps to */
            var v = QShared.vec3_origin;

            for (int i = 0; i < nump; i++)
            {
                v += vecs[i];
            }

            var av = new Vector3(MathF.Abs(v.X), MathF.Abs(v.Y), MathF.Abs(v.Z));

            int axis;
            if ((av.X > av.Y) && (av.X > av.Z))
            {
                if (v.X < 0)
                {
                    axis = 1;
                }
                else
                {
                    axis = 0;
                }
            }
            else if ((av.Y > av.Z) && (av.Y > av.X))
            {
                if (v.Y < 0)
                {
                    axis = 3;
                }
                else
                {
                    axis = 2;
                }
            }
            else
            {
                if (v.Z < 0)
                {
                    axis = 5;
                }
                else
                {
                    axis = 4;
                }
            }

            /* project new texture coords */
            for (int i = 0; i < nump; i++)
            {
                int j = vec_to_st[axis][2];

                float dv;
                switch (j)
                {
                    case 1: dv = vecs[i].X; break;
                    case 2: dv = vecs[i].Y; break;
                    case 3: dv = vecs[i].Z; break;
                    case -1: dv = -vecs[i].X; break;
                    case -2: dv = -vecs[i].Y; break;
                    case -3: dv = -vecs[i].Z; break;
                    default: throw new Exception("Internal error");
                }

                if (dv < 0.001)
                {
                    continue; /* don't divide by zero */
                }

                j = vec_to_st[axis][0];
                float s;
                switch (j)
                {
                    case 1: s = vecs[i].X / dv; break;
                    case 2: s = vecs[i].Y / dv; break;
                    case 3: s = vecs[i].Z / dv; break;
                    case -1: s = -vecs[i].X / dv; break;
                    case -2: s = -vecs[i].Y / dv; break;
                    case -3: s = -vecs[i].Z / dv; break;
                    default: throw new Exception("Internal error");
                }

                j = vec_to_st[axis][1];
                float t;
                switch (j)
                {
                    case 1: t = vecs[i].X / dv; break;
                    case 2: t = vecs[i].Y / dv; break;
                    case 3: t = vecs[i].Z / dv; break;
                    case -1: t = -vecs[i].X / dv; break;
                    case -2: t = -vecs[i].Y / dv; break;
                    case -3: t = -vecs[i].Z / dv; break;
                    default: throw new Exception("Internal error");
                }

                if (s < skymins[0][axis])
                {
                    skymins[0][axis] = s;
                }

                if (t < skymins[1][axis])
                {
                    skymins[1][axis] = t;
                }

                if (s > skymaxs[0][axis])
                {
                    skymaxs[0][axis] = s;
                }

                if (t > skymaxs[1][axis])
                {
                    skymaxs[1][axis] = t;
                }
            }
        }

        private void ClipSkyPolygon(int nump, in Vector3[] vecs, int stage)
        {
            if (nump > MAX_CLIP_VERTS - 2)
            {
                ri.Sys_Error(QShared.ERR_DROP, "R_ClipSkyPolygon: MAX_CLIP_VERTS");
            }

            if (stage == 6)
            {
                /* fully clipped, so draw it */
                DrawSkyPolygon(nump, vecs);
                return;
            }

            var front = false;
            var back = false;
            ref var norm = ref skyclip[stage];
            var dists = new float[nump + 2];
            var sides = new int[nump + 2];
            

            for (int i = 0; i < nump; i++)
            {
                var d = Vector3.Dot(vecs[i], norm);

                if (d > ON_EPSILON)
                {
                    front = true;
                    sides[i] = SIDE_FRONT;
                }
                else if (d < -ON_EPSILON)
                {
                    back = true;
                    sides[i] = SIDE_BACK;
                }
                else
                {
                    sides[i] = SIDE_ON;
                }

                dists[i] = d;
            }

            if (!front || !back)
            {
                /* not clipped */
                ClipSkyPolygon(nump, vecs, stage + 1);
                return;
            }

            /* clip it */
            sides[nump] = sides[0];
            dists[nump] = dists[0];
            vecs[nump] = vecs[0];
            var newc = new int[]{ 0, 0 };
            var newv = new Vector3[2][]{
                new Vector3[MAX_CLIP_VERTS],
                new Vector3[MAX_CLIP_VERTS]
            };

            for (int i = 0; i < nump; i++)
            {
                switch (sides[i])
                {
                    case SIDE_FRONT:
                        newv[0][newc[0]] = vecs[i];
                        newc[0]++;
                        break;
                    case SIDE_BACK:
                        newv[1][newc[1]] = vecs[i];
                        newc[1]++;
                        break;
                    case SIDE_ON:
                        newv[0][newc[0]] = vecs[i];
                        newc[0]++;
                        newv[1][newc[1]] = vecs[i];
                        newc[1]++;
                        break;
                }

                if ((sides[i] == SIDE_ON) ||
                    (sides[i + 1] == SIDE_ON) ||
                    (sides[i + 1] == sides[i]))
                {
                    continue;
                }

                var d = dists[i] / (dists[i] - dists[i + 1]);

                var e = vecs[i] + d * (vecs[i + 1] - vecs[i]);
                newv[0][newc[0]] = e;
                newv[1][newc[1]] = e;

                newc[0]++;
                newc[1]++;
            }

            /* continue */
            ClipSkyPolygon(newc[0], newv[0], stage + 1);
            ClipSkyPolygon(newc[1], newv[1], stage + 1);
        }

        private void GL3_AddSkySurface(in msurface_t fa)
        {
            /* calculate vertex values for sky box */
            var verts = new Vector3[MAX_CLIP_VERTS];
            for (var p = fa.polys; p != null; p = p.next)
            {
                for (int i = 0; i < p.vertices.Length; i++)
                {
                    verts[i] = (Vector3)p.vertices[i].pos - gl3_origin;
                }

                ClipSkyPolygon(p.vertices.Length, verts, 0);
            }
        }

        private void GL3_ClearSkyBox()
        {
            int i;

            skymins = new float[2][]{ new float[6], new float[6] };
            skymaxs = new float[2][]{ new float[6], new float[6] };
            for (i = 0; i < 6; i++)
            {
                skymins[0][i] = skymins[1][i] = 9999;
                skymaxs[0][i] = skymaxs[1][i] = -9999;
            }
        }


        private void MakeSkyVec(float s, float t, int axis, ref gl3_3D_vtx_t vert)
        {
            // vec3_t v, b;
            // int j, k;

            float dist = (r_farsee!.Float == 0) ? 2300.0f : 4096.0f;

            var b = new Vector3(s*dist, t*dist, dist);
            var v = new Vector3D<float>();

            for (int j = 0; j < 3; j++)
            {
                int k = st_to_vec[axis][j];
                float va;

                switch (k)
                {
                    case 1: va = b.X; break;
                    case 2: va = b.Y; break;
                    case 3: va = b.Z; break;
                    case -1: va = -b.X; break;
                    case -2: va = -b.Y; break;
                    case -3: va = -b.Z; break;
                    default: throw new Exception("Internal error");
                }

                if (j == 0) v.X = va;
                else if (j == 1) v.Y = va;
                else v.Z = va;
            }

            /* avoid bilerp seam */
            s = (s + 1) * 0.5f;
            t = (t + 1) * 0.5f;

            if (s < sky_min)
            {
                s = sky_min;
            }
            else if (s > sky_max)
            {
                s = sky_max;
            }

            if (t < sky_min)
            {
                t = sky_min;
            }
            else if (t > sky_max)
            {
                t = sky_max;
            }

            t = 1.0f - t;

            vert.pos = v;

            vert.texCoord.X = s;
            vert.texCoord.Y = t;

            vert.lmTexCoord.X = 0;
            vert.lmTexCoord.Y = 0;
        }

        private void GL3_DrawSkyBox(GL gl)
        {
            int i;

            if (skyrotate != 0)
            {   /* check for no sky at all */
                for (i = 0; i < 6; i++)
                {
                    if ((skymins[0][i] < skymaxs[0][i]) &&
                        (skymins[1][i] < skymaxs[1][i]))
                    {
                        break;
                    }
                }

                if (i == 6)
                {
                    return; /* nothing visible */
                }
            }

            // glPushMatrix();
            var origModelMat = gl3state.uni3DData.transModelMat4;

            // glTranslatef(gl3_origin[0], gl3_origin[1], gl3_origin[2]);
            var transl = new Vector3D<float>(gl3_origin.X, gl3_origin.Y, gl3_origin.Z);
            var modMVmat = origModelMat * Matrix4X4.CreateTranslation(transl);
            if(skyrotate != 0.0f)
            {
                // glRotatef(gl3_newrefdef.time * skyrotate, skyaxis[0], skyaxis[1], skyaxis[2]);
                var rotAxis = new Vector3D<float>(skyaxis.X, skyaxis.Y, skyaxis.Z);
                // modMVmat = HMM_MultiplyMat4(modMVmat, HMM_Rotate(gl3_newrefdef.time * skyrotate, rotAxis));
            }
            gl3state.uni3DData.transModelMat4 = modMVmat;
            GL3_UpdateUBO3D(gl);

            GL3_UseProgram(gl, gl3state.si3Dsky.shaderProgram);
            GL3_BindVAO(gl, gl3state.vao3D);
            GL3_BindVBO(gl, gl3state.vbo3D);

            // TODO: this could all be done in one drawcall.. but.. whatever, it's <= 6 drawcalls/frame

            var skyVertices = new gl3_3D_vtx_t[4];

            for (i = 0; i < 6; i++)
            {
                if (skyrotate != 0.0f)
                {
                    skymins[0][i] = -1;
                    skymins[1][i] = -1;
                    skymaxs[0][i] = 1;
                    skymaxs[1][i] = 1;
                }

                if ((skymins[0][i] >= skymaxs[0][i]) ||
                    (skymins[1][i] >= skymaxs[1][i]))
                {
                    continue;
                }

                GL3_Bind(gl, sky_images[skytexorder[i]]!.texnum);

                MakeSkyVec( skymins [ 0 ] [ i ], skymins [ 1 ] [ i ], i, ref skyVertices[0] );
                MakeSkyVec( skymins [ 0 ] [ i ], skymaxs [ 1 ] [ i ], i, ref skyVertices[1] );
                MakeSkyVec( skymaxs [ 0 ] [ i ], skymaxs [ 1 ] [ i ], i, ref skyVertices[2] );
                MakeSkyVec( skymaxs [ 0 ] [ i ], skymins [ 1 ] [ i ], i, ref skyVertices[3] );

                GL3_BufferAndDraw3D(gl, skyVertices, PrimitiveType.TriangleFan);
            }

            // glPopMatrix();
            gl3state.uni3DData.transModelMat4 = origModelMat;
            GL3_UpdateUBO3D(gl);
        }


    }
}