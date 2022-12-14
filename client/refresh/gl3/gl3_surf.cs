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
 * Surface generation and drawing
 *
 * =======================================================================
 */

using Silk.NET.OpenGL;
using System.Numerics;

namespace Quake2 {

    partial class QRefGl3
    {
        private int c_visible_lightmaps;
        private int c_visible_textures;
        private  Vector3 modelorg; /* relative to viewpoint */
        private  msurface_t? gl3_alpha_surfaces;

        private const double BACKFACE_EPSILON = 0.01f;

        private unsafe void GL3_SurfInit(GL gl)
        {
            // init the VAO and VBO for the standard vertexdata: 10 floats and 1 uint
            // (X, Y, Z), (S, T), (LMS, LMT), (normX, normY, normZ) ; lightFlags - last two groups for lightmap/dynlights

            gl3state.vao3D = gl.GenVertexArray();
            GL3_BindVAO(gl, gl3state.vao3D);

            gl3state.vbo3D = gl.GenBuffer();
            GL3_BindVBO(gl, gl3state.vbo3D);

            if(gl3config.useBigVBO)
            {
                gl3state.vbo3Dsize = 5*1024*1024; // a 5MB buffer seems to work well?
                gl3state.vbo3DcurOffset = 0;
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)gl3state.vbo3Dsize, null, BufferUsageARB.StreamDraw); // allocate/reserve that data
            }

            gl.EnableVertexAttribArray(GL3_ATTRIB_POSITION);
            gl.VertexAttribPointer(GL3_ATTRIB_POSITION, 3, VertexAttribPointerType.Float, false, 11 * sizeof(float), (void *)0);

            gl.EnableVertexAttribArray(GL3_ATTRIB_TEXCOORD);
            gl.VertexAttribPointer(GL3_ATTRIB_TEXCOORD, 2, VertexAttribPointerType.Float, false, 11 * sizeof(float), (void *)(3*sizeof(float)));

            gl.EnableVertexAttribArray(GL3_ATTRIB_LMTEXCOORD);
            gl.VertexAttribPointer(GL3_ATTRIB_LMTEXCOORD, 2, VertexAttribPointerType.Float, false, 11 * sizeof(float), (void *)(5*sizeof(float)));

            gl.EnableVertexAttribArray(GL3_ATTRIB_NORMAL);
            gl.VertexAttribPointer(GL3_ATTRIB_NORMAL, 3, VertexAttribPointerType.Float, false, 11 * sizeof(float), (void *)(7*sizeof(float)));

            gl.EnableVertexAttribArray(GL3_ATTRIB_LIGHTFLAGS);
            gl.VertexAttribIPointer(GL3_ATTRIB_LIGHTFLAGS, 1, VertexAttribIType.UnsignedInt, 11 * sizeof(float), (void *)(10*sizeof(float)));



            // init VAO and VBO for model vertexdata: 9 floats
            // (X,Y,Z), (S,T), (R,G,B,A)

            gl3state.vaoAlias = gl.GenVertexArray();
            GL3_BindVAO(gl, gl3state.vaoAlias);

            gl3state.vboAlias = gl.GenBuffer();
            GL3_BindVBO(gl, gl3state.vboAlias);

            gl.EnableVertexAttribArray(GL3_ATTRIB_POSITION);
            gl.VertexAttribPointer(GL3_ATTRIB_POSITION, 3, VertexAttribPointerType.Float, false, 9*sizeof(float), (void *)0);

            gl.EnableVertexAttribArray(GL3_ATTRIB_TEXCOORD);
            gl.VertexAttribPointer(GL3_ATTRIB_TEXCOORD, 2, VertexAttribPointerType.Float, false, 9*sizeof(float), (void *)(3*sizeof(float)));

            gl.EnableVertexAttribArray(GL3_ATTRIB_COLOR);
            gl.VertexAttribPointer(GL3_ATTRIB_COLOR, 4, VertexAttribPointerType.Float, false, 9*sizeof(float), (void *)(5*sizeof(float)));

            gl3state.eboAlias = gl.GenBuffer();

            // init VAO and VBO for particle vertexdata: 9 floats
            // (X,Y,Z), (point_size,distace_to_camera), (R,G,B,A)

            gl3state.vaoParticle = gl.GenVertexArray();
            GL3_BindVAO(gl, gl3state.vaoParticle);

            gl3state.vboParticle = gl.GenBuffer();
            GL3_BindVBO(gl, gl3state.vboParticle);

            gl.EnableVertexAttribArray(GL3_ATTRIB_POSITION);
            gl.VertexAttribPointer(GL3_ATTRIB_POSITION, 3, VertexAttribPointerType.Float, false, 9*sizeof(float), (void *)0);

            // TODO: maybe move point size and camera origin to UBO and calculate distance in vertex shader
            gl.EnableVertexAttribArray(GL3_ATTRIB_TEXCOORD); // it's abused for (point_size, distance) here..
            gl.VertexAttribPointer(GL3_ATTRIB_TEXCOORD, 2, VertexAttribPointerType.Float, false, 9*sizeof(float), (void *)(3*sizeof(float)));

            gl.EnableVertexAttribArray(GL3_ATTRIB_COLOR);
            gl.VertexAttribPointer(GL3_ATTRIB_COLOR, 4, VertexAttribPointerType.Float, false, 9*sizeof(float), (void *)(5*sizeof(float)));
        }

        /*
        * Returns true if the box is completely outside the frustom
        */
        private bool CullBox(in Vector3 mins, in Vector3 maxs)
        {
            int i;

            if (!gl_cull!.Bool)
            {
                return false;
            }

            for (i = 0; i < 4; i++)
            {
                if (QShared.BoxOnPlaneSide(mins, maxs, frustum[i]) == 2)
                {
                    return true;
                }
            }

            return false;
        }

        /*
        * Returns the proper texture for a given time and base texture
        */
        private gl3image_t TextureAnimation(in entity_t currententity, in mtexinfo_t tex)
        {
            if (tex.next == null)
            {
                return tex.image!;
            }

            int c = currententity.frame % tex.numframes;
            var t = tex;

            while (c > 0)
            {
                t = t!.next;
                c--;
            }

            return t!.image!;
        }

        private void SetLightFlags(in msurface_t surf)
        {
            uint lightFlags = 0xffffffff;
            // uint lightFlags = 0;
            // if (surf.dlightframe == gl3_framecount)
            // {
            //     lightFlags = surf.dlightbits;
            // }

            for(int i=0; i<surf.polys!.vertices.Length; ++i)
            {
                surf.polys!.vertices[i].lightFlags = lightFlags;
            }
        }

        private void SetAllLightFlags(in msurface_t surf)
        {
            uint lightFlags = 0xffffffff;

            // uint lightFlags = 0xffffffff;
            // if (surf.dlightframe == gl3_framecount)
            // {
            //     lightFlags = surf.dlightbits;
            // }

            for(int i=0; i<surf.polys!.vertices.Length; ++i)
            {
                surf.polys!.vertices[i].lightFlags = lightFlags;
            }
        }

        private void GL3_DrawGLPoly(GL gl, in msurface_t fa)
        {
            GL3_BindVAO(gl, gl3state.vao3D);
            GL3_BindVBO(gl, gl3state.vbo3D);

            GL3_BufferAndDraw3D(gl, fa.polys!.vertices, PrimitiveType.TriangleFan);
        }

        private unsafe void UpdateLMscales(GL gl, in Vector4[] lmScales, ref gl3ShaderInfo_t si)
        {
            bool hasChanged = false;

            for(int i=0; i<MAX_LIGHTMAPS_PER_SURFACE; ++i)
            {
                if(hasChanged)
                {
                    si.lmScales[i*4+0] = lmScales[i].X;
                    si.lmScales[i*4+1] = lmScales[i].Y;
                    si.lmScales[i*4+2] = lmScales[i].Z;
                    si.lmScales[i*4+3] = lmScales[i].W;
                }
                else if(   si.lmScales[i*4+0] != lmScales[i].X
                        || si.lmScales[i*4+1] != lmScales[i].Y
                        || si.lmScales[i*4+2] != lmScales[i].Z
                        || si.lmScales[i*4+3] != lmScales[i].W )
                {
                    si.lmScales[i*4+0] = lmScales[i].X;
                    si.lmScales[i*4+1] = lmScales[i].Y;
                    si.lmScales[i*4+2] = lmScales[i].Z;
                    si.lmScales[i*4+3] = lmScales[i].W;
                    hasChanged = true;
                }
            }

            if(hasChanged)
            {
                fixed (float *f = si.lmScales)
                {
                    gl.Uniform4(si.uniLmScalesOrTime, MAX_LIGHTMAPS_PER_SURFACE, f);
                }
            }
        }

        private void RenderBrushPoly(GL gl, in entity_t currententity, in msurface_t fa)
        {
            // int map;
            // gl3image_t *image;

            c_brush_polys++;

            var image = TextureAnimation(currententity, fa.texinfo!);

            // if (fa->flags & SURF_DRAWTURB)
            // {
            //     GL3_Bind(image->texnum);

            //     GL3_EmitWaterPolys(fa);

            //     return;
            // }
            // else
            // {
                GL3_Bind(gl, image.texnum);
            // }

            var lmScales = new Vector4[MAX_LIGHTMAPS_PER_SURFACE];
            lmScales[0] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

            GL3_BindLightmap(gl, fa.lightmaptexturenum);

            // Any dynamic lights on this surface?
            for (int map = 0; map < MAX_LIGHTMAPS_PER_SURFACE && fa.styles[map] != 255; map++)
            {
                lmScales[map].X = gl3_newrefdef.lightstyles[fa.styles[map]].rgb[0];
                lmScales[map].Y = gl3_newrefdef.lightstyles[fa.styles[map]].rgb[1];
                lmScales[map].Z = gl3_newrefdef.lightstyles[fa.styles[map]].rgb[2];
                lmScales[map].W = 1.0f;
            }

            // if (fa->texinfo->flags & SURF_FLOWING)
            // {
            //     GL3_UseProgram(gl3state.si3DlmFlow.shaderProgram);
            //     UpdateLMscales(lmScales, &gl3state.si3DlmFlow);
            //     GL3_DrawGLFlowingPoly(fa);
            // }
            // else
            // {
                GL3_UseProgram(gl, gl3state.si3Dlm.shaderProgram);
                UpdateLMscales(gl, lmScales, ref gl3state.si3Dlm);
                GL3_DrawGLPoly(gl, fa);
            // }

            // Note: lightmap chains are gone, lightmaps are rendered together with normal texture in one pass
        }


        private void DrawTextureChains(GL gl, in entity_t currententity)
        {
            c_visible_textures = 0;

            for (int i = 0; i < gl3textures.Length; i++)
            {
                if (gl3textures[i] == null)
                {
                    continue;
                }
                if (gl3textures[i].registration_sequence == 0)
                {
                    continue;
                }

                var s = gl3textures[i].texturechain;

                if (s == null)
                {
                    continue;
                }

                c_visible_textures++;

                for ( ; s != null; s = s.texturechain)
                {
                    SetLightFlags(s);
                    RenderBrushPoly(gl, currententity, s);
                }

                gl3textures[i].texturechain = null;
            }

            // TODO: maybe one loop for normal faces and one for SURF_DRAWTURB ???
        }

        private void DrawInlineBModel(GL gl, in entity_t currententity, in gl3brushmodel_t currentmodel)
        {
            // int i, k;
            // cplane_t *pplane;
            // float dot;
            // msurface_t *psurf;
            // dlight_t *lt;

            // /* calculate dynamic lighting for bmodel */
            // lt = gl3_newrefdef.dlights;

            // for (k = 0; k < gl3_newrefdef.num_dlights; k++, lt++)
            // {
            //     GL3_MarkLights(lt, 1 << k, currentmodel->nodes + currentmodel->firstnode);
            // }

            // psurf = &currentmodel->surfaces[currentmodel->firstmodelsurface];

            // if (currententity->flags & RF_TRANSLUCENT)
            // {
            //     glEnable(GL_BLEND);
            //     /* TODO: should I care about the 0.25 part? we'll just set alpha to 0.33 or 0.66 depending on surface flag..
            //     glColor4f(1, 1, 1, 0.25);
            //     R_TexEnv(GL_MODULATE);
            //     */
            // }

            /* draw texture */
            for (int i = 0; i < currentmodel.nummodelsurfaces; i++)
            {
                /* find which side of the node we are on */
                ref var psurf = ref currentmodel.surfaces[currentmodel.firstmodelsurface + i];
                var pplane = psurf.plane!;

                var dot = Vector3.Dot(modelorg, pplane.normal) - pplane.dist;

                /* draw the polygon */
                if (((psurf.flags & SURF_PLANEBACK) != 0 && (dot < -BACKFACE_EPSILON)) ||
                    ((psurf.flags & SURF_PLANEBACK) == 0 && (dot > BACKFACE_EPSILON)))
                {
            //         if (psurf->texinfo->flags & (SURF_TRANS33 | SURF_TRANS66))
            //         {
            //             /* add to the translucent chain */
            //             psurf->texturechain = gl3_alpha_surfaces;
            //             gl3_alpha_surfaces = psurf;
            //         }
            //         else if(!(psurf->flags & SURF_DRAWTURB))
            //         {
            //             SetAllLightFlags(psurf);
            //             RenderLightmappedPoly(currententity, psurf);
            //         }
            //         else
            //         {
                        RenderBrushPoly(gl, currententity, psurf);
            //         }
                }
            }

            // if (currententity->flags & RF_TRANSLUCENT)
            // {
            //     glDisable(GL_BLEND);
            // }
        }

        private void GL3_DrawBrushModel(GL gl, in entity_t e, in gl3brushmodel_t currentmodel)
        {
            // vec3_t mins, maxs;
            // int i;
            // qboolean rotated;

            if (currentmodel.nummodelsurfaces == 0)
            {
                return;
            }

            gl3state.currenttexture = 0xFFFFFFFFU;
            bool rotated;
            Vector3 mins, maxs;

            if (e.angles.X != 0 || e.angles.Y != 0 || e.angles.Z != 0)
            {
                rotated = true;

                mins = new Vector3(e.origin.X - currentmodel.radius, e.origin.Y - currentmodel.radius, e.origin.Z - currentmodel.radius);
                maxs = new Vector3(e.origin.X + currentmodel.radius, e.origin.Y + currentmodel.radius, e.origin.Z + currentmodel.radius);
            }
            else
            {
                rotated = false;
                mins = e.origin - currentmodel.mins;
                maxs = e.origin + currentmodel.maxs;
            }

            if (CullBox(mins, maxs))
            {
                return;
            }

            // if (gl_zfix->value)
            // {
            //     glEnable(GL_POLYGON_OFFSET_FILL);
            // }

            modelorg = gl3_newrefdef.vieworg - e.origin;

            if (rotated)
            {
            //     vec3_t temp;
            //     vec3_t forward, right, up;

            //     VectorCopy(modelorg, temp);
            //     AngleVectors(e->angles, forward, right, up);
            //     modelorg[0] = DotProduct(temp, forward);
            //     modelorg[1] = -DotProduct(temp, right);
            //     modelorg[2] = DotProduct(temp, up);
            }



            //glPushMatrix();
            var oldMat = gl3state.uni3DData.transModelMat4;

            // e->angles[0] = -e->angles[0];
            // e->angles[2] = -e->angles[2];
            // GL3_RotateForEntity(e);
            // e->angles[0] = -e->angles[0];
            // e->angles[2] = -e->angles[2];

            DrawInlineBModel(gl, e, currentmodel);

            // // glPopMatrix();
            gl3state.uni3DData.transModelMat4 = oldMat;
            GL3_UpdateUBO3D(gl);

            // if (gl_zfix->value)
            // {
            //     glDisable(GL_POLYGON_OFFSET_FILL);
            // }
        }

        private void RecursiveWorldNode(GL gl, in entity_t currententity, in mnode_or_leaf_t anode, in gl3brushmodel_t model)
        {
        //     int c, side, sidebit;
        //     cplane_t *plane;
        //     msurface_t *surf, **mark;
        //     mleaf_t *pleaf;
        //     float dot;
        //     gl3image_t *image;

            if (anode.contents == QCommon.CONTENTS_SOLID)
            {
                return; /* solid */
            }

            if (anode.visframe != gl3_visframecount)
            {
                return;
            }

            if (CullBox(anode.mins, anode.maxs))
            {
                return;
            }

            /* if a leaf node, draw stuff */
            if (anode.contents != -1)
            {
                var pleaf = (mleaf_t)anode;

        //         /* check for door connected areas */
        //         if (gl3_newrefdef.areabits)
        //         {
        //             if (!(gl3_newrefdef.areabits[pleaf->area >> 3] & (1 << (pleaf->area & 7))))
        //             {
        //                 return; /* not visible */
        //             }
        //         }

                // mark = pleaf->firstmarksurface;
                // var c = pleaf.nummarksurfaces;

                for (int c = 0; c < pleaf.nummarksurfaces; c++)
                {
                    model.surfaces[model.marksurfaces[pleaf.firstmarksurface_i + c]].visframe = gl3_framecount;
                }
                // if (c)
        //         {
        //             do
        //             {
        //                 (*mark)->visframe = gl3_framecount;
        //                 mark++;
        //             }
        //             while (--c);
        //         }

                return;
            }

            /* node is just a decision point, so go down the apropriate
            sides find which side of the node we are on */
            var node = (mnode_t)anode;
            var plane = node.plane!;

            float dot;
            switch ((int)plane.type)
            {
                case QCommon.PLANE_X:
                    dot = modelorg.X - plane.dist;
                    break;
                case QCommon.PLANE_Y:
                    dot = modelorg.Y - plane.dist;
                    break;
                case QCommon.PLANE_Z:
                    dot = modelorg.Z - plane.dist;
                    break;
                default:
                    dot = Vector3.Dot(modelorg, plane.normal) - plane.dist;
                    break;
            }

            int side;
            int sidebit;
            if (dot >= 0)
            {
                side = 0;
                sidebit = 0;
            }
            else
            {
                side = 1;
                sidebit = SURF_PLANEBACK;
            }

            /* recurse down the children, front side first */
            RecursiveWorldNode(gl, currententity, node.children[side]!, model);

            /* draw stuff */
            for (int c = 0; c < node.numsurfaces; c++)
            {
                ref var surf = ref gl3_worldmodel!.surfaces[node.firstsurface + c];
                // if (surf.visframe != gl3_framecount)
                // {
                //     continue;
                // }

                if ((surf.flags & SURF_PLANEBACK) != sidebit)
                {
                    continue; /* wrong side */
                }

                if ((surf.texinfo!.flags & QCommon.SURF_SKY) != 0)
                {
                    /* just adds to visible sky bounds */
                    GL3_AddSkySurface(surf);
                }
                else if ((surf.texinfo.flags & (QCommon.SURF_TRANS33 | QCommon.SURF_TRANS66)) != 0)
                {
                    /* add to the translucent chain */
                    surf.texturechain = gl3_alpha_surfaces;
                    gl3_alpha_surfaces = surf;
                    gl3_alpha_surfaces.texinfo.image = TextureAnimation(currententity, surf.texinfo);
                }
                else
                {
                    // calling RenderLightmappedPoly() here probably isn't optimal, rendering everything
                    // through texturechains should be faster, because far less glBindTexture() is needed
                    // (and it might allow batching the drawcalls of surfaces with the same texture)
        // #if 0
        //             if(!(surf->flags & SURF_DRAWTURB))
        //             {
        //                 RenderLightmappedPoly(surf);
        //             }
        //             else
        // #endif // 0
                    {
                        /* the polygon is visible, so add it to the texture sorted chain */
                        var image = TextureAnimation(currententity, surf.texinfo);
                        surf.texturechain = image.texturechain;
                        image.texturechain = surf;
                    }
                }
            }

            /* recurse down the back side */
            RecursiveWorldNode(gl, currententity, node.children[side ^ 1]!, model);
        }

        private void GL3_DrawWorld(GL gl)
        {
            if (!r_drawworld!.Bool)
            {
                return;
            }

            if ((gl3_newrefdef.rdflags & QShared.RDF_NOWORLDMODEL) != 0)
            {
                return;
            }

            modelorg = gl3_newrefdef.vieworg;

            /* auto cycle the world frame for texture animation */
            var ent = new entity_t();
            ent.frame = (int)(gl3_newrefdef.time * 2);

            gl3state.currenttexture = 0xFFFFFFFFU;

            GL3_ClearSkyBox();
            RecursiveWorldNode(gl, ent, gl3_worldmodel!.nodes[0], gl3_worldmodel);
            DrawTextureChains(gl, ent);
            GL3_DrawSkyBox(gl);
            // DrawTriangleOutlines();
        }

        /*
        * Mark the leaves and nodes that are
        * in the PVS for the current cluster
        */
        private void GL3_MarkLeaves()
        {
            // const byte *vis;
            // YQ2_ALIGNAS_TYPE(int) byte fatvis[MAX_MAP_LEAFS / 8];
            // mnode_t *node;
            // int i, c;
            // mleaf_t *leaf;
            // int cluster;

            if ((gl3_oldviewcluster == gl3_viewcluster) &&
                (gl3_oldviewcluster2 == gl3_viewcluster2) &&
                !r_novis!.Bool &&
                (gl3_viewcluster != -1))
            {
                return;
            }

            /* development aid to let you run around
            and see exactly where the pvs ends */
            if (r_lockpvs!.Bool)
            {
                return;
            }

            gl3_visframecount++;
            gl3_oldviewcluster = gl3_viewcluster;
            gl3_oldviewcluster2 = gl3_viewcluster2;

            // if (r_novis->value || (gl3_viewcluster == -1) || !gl3_worldmodel->vis)
            // {
                /* mark everything */
                for (int i = 0; i < gl3_worldmodel!.numleafs; i++)
                {
                    gl3_worldmodel.leafs[i].visframe = gl3_visframecount;
                }

                for (int i = 0; i < gl3_worldmodel.nodes.Length; i++)
                {
                    gl3_worldmodel.nodes[i].visframe = gl3_visframecount;
                }

                return;
            // }

            // vis = GL3_Mod_ClusterPVS(gl3_viewcluster, gl3_worldmodel);

            // /* may have to combine two clusters because of solid water boundaries */
            // if (gl3_viewcluster2 != gl3_viewcluster)
            // {
            //     memcpy(fatvis, vis, (gl3_worldmodel->numleafs + 7) / 8);
            //     vis = GL3_Mod_ClusterPVS(gl3_viewcluster2, gl3_worldmodel);
            //     c = (gl3_worldmodel->numleafs + 31) / 32;

            //     for (i = 0; i < c; i++)
            //     {
            //         ((int *)fatvis)[i] |= ((int *)vis)[i];
            //     }

            //     vis = fatvis;
            // }

            // for (i = 0, leaf = gl3_worldmodel->leafs;
            //     i < gl3_worldmodel->numleafs;
            //     i++, leaf++)
            // {
            //     cluster = leaf->cluster;

            //     if (cluster == -1)
            //     {
            //         continue;
            //     }

            //     if (vis[cluster >> 3] & (1 << (cluster & 7)))
            //     {
            //         node = (mnode_t *)leaf;

            //         do
            //         {
            //             if (node->visframe == gl3_visframecount)
            //             {
            //                 break;
            //             }

            //             node->visframe = gl3_visframecount;
            //             node = node->parent;
            //         }
            //         while (node);
            //     }
            // }
        }


    }
}