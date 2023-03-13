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
 * Mesh handling
 *
 * =======================================================================
 */

using Silk.NET.OpenGL;
using Silk.NET.Maths;
using System.Numerics;

namespace Quake2 {

    partial class QRefGl3
    {
        const int SHADEDOT_QUANT = 16;


        private (int, int) CountVerticesAndIndices(in gl3aliasmodel_t model)
        {
            int vertices = 0;
            int indices = 0;
            int order = 0;
            while (true)
            {
            //     GLushort nextVtxIdx = da_count(vtxBuf);

                /* get the vertex count and primitive type */
                var count = model.glcmds[order++];
                if (count == 0)
                {
                    break; /* done */
                }

                if (count < 0) {
                    count = -count;
                }

                order += 3 * count;

                vertices += count;

                for(int i=1; i < count-1; ++i)
                    indices += 3;
            }
            return (vertices, indices);
        }

        private void LerpVerts(bool powerUpEffect, int nverts, QCommon.dtrivertx_t[] v, QCommon.dtrivertx_t[] ov,
                QCommon.dtrivertx_t[] verts, ref Vector3D<float>[] lerp, Vector3 move, Vector3 frontv, Vector3 backv)
        {
            // if (powerUpEffect)
            // {
            //     for (i = 0; i < nverts; i++, v++, ov++, lerp += 4)
            //     {
            //         float *normal = r_avertexnormals[verts[i].lightnormalindex];

            //         lerp[0] = move[0] + ov->v[0] * backv[0] + v->v[0] * frontv[0] +
            //                 normal[0] * POWERSUIT_SCALE;
            //         lerp[1] = move[1] + ov->v[1] * backv[1] + v->v[1] * frontv[1] +
            //                 normal[1] * POWERSUIT_SCALE;
            //         lerp[2] = move[2] + ov->v[2] * backv[2] + v->v[2] * frontv[2] +
            //                 normal[2] * POWERSUIT_SCALE;
            //     }
            // }
            // else
            // {
                // for (int i = 0; i < nverts; i++, v++, ov++, lerp += 4)
                // {
                //     lerp[0] = move[0] + ov->v[0] * backv[0] + v->v[0] * frontv[0];
                //     lerp[1] = move[1] + ov->v[1] * backv[1] + v->v[1] * frontv[1];
                //     lerp[2] = move[2] + ov->v[2] * backv[2] + v->v[2] * frontv[2];
                // }
                for (int i = 0; i < nverts; i++)
                {
                    lerp[i] = new Vector3D<float>();
                    lerp[i].X = move.X + ov[i].v[0] * backv.X + v[i].v[0] * frontv.X;
                    lerp[i].Y = move.Y + ov[i].v[1] * backv.Y + v[i].v[1] * frontv.Y;
                    lerp[i].Z = move.Z + ov[i].v[2] * backv.Z + v[i].v[2] * frontv.Z;
                }
            // }
        }

        /*
        * Interpolates between two frames and origins
        */
        private unsafe void DrawAliasFrameLerp(GL gl, in gl3aliasmodel_t model, in entity_t entity, in Vector3 shadelight)
        {
            // GLenum type;
            // float l;
            // daliasframe_t *frame, *oldframe;
            // dtrivertx_t *v, *ov, *verts;
            // int *order;
            // int count;
            // float alpha;
            // vec3_t move, delta, vectors[3];
            // vec3_t frontv, backv;
            // int i;
            // int index_xyz;
            float backlerp = entity.backlerp;
            float frontlerp = 1.0f - backlerp;
            // float *lerp;
            // draw without texture? used for quad damage effect etc, I think
            var colorOnly = (0 != (entity.flags &
                    (QShared.RF_SHELL_RED | QShared.RF_SHELL_GREEN | QShared.RF_SHELL_BLUE | QShared.RF_SHELL_DOUBLE |
                    QShared.RF_SHELL_HALF_DAM)));

            // TODO: maybe we could somehow store the non-rotated normal and do the dot in shader?
            ref var shadedots = ref r_avertexnormal_dots[((int)(entity.angles.Y *
                        (SHADEDOT_QUANT / 360.0))) & (SHADEDOT_QUANT - 1)];

            ref var frame = ref model.frames[entity.frame];
            // verts = v = frame->verts;

            ref var oldframe = ref model.frames[entity.oldframe];
            // ov = oldframe->verts;

            // order = (int *)((byte *)paliashdr + paliashdr->ofs_glcmds);

            float alpha;
            if ((entity.flags & QShared.RF_TRANSLUCENT) != 0)
            {
                alpha = entity.alpha * 0.666f;
            }
            else
            {
                alpha = 1.0f;
            }

            if (colorOnly)
            {
                GL3_UseProgram(gl, gl3state.si3DaliasColor.shaderProgram);
            }
            else
            {
                GL3_UseProgram(gl, gl3state.si3Dalias.shaderProgram);
            }

            // if(gl3_colorlight->value == 0.0f)
            // {
            //     float avg = 0.333333f * (shadelight[0]+shadelight[1]+shadelight[2]);
            //     shadelight[0] = shadelight[1] = shadelight[2] = avg;
            // }

            /* move should be the delta back to the previous frame * backlerp */
            var delta = entity.oldorigin - entity.origin;
            var vectors = new Vector3[3];
            QShared.AngleVectors(entity.angles, ref vectors[0], ref vectors[1], ref vectors[2]);

            var move = new Vector3();
            move.X = Vector3.Dot(delta, vectors[0]); /* forward */
            move.Y = -Vector3.Dot(delta, vectors[1]); /* left */
            move.Z = Vector3.Dot(delta, vectors[2]); /* up */

            move.X += oldframe.translate[0];
            move.Y += oldframe.translate[1];
            move.Z += oldframe.translate[2];

            move = backlerp * move + frontlerp * new Vector3(frame.translate[0], frame.translate[1], frame.translate[2]);
            var frontv = frontlerp * new Vector3(frame.scale[0], frame.scale[1], frame.scale[2]);
            var backv = backlerp * new Vector3(oldframe.scale[0], oldframe.scale[1], oldframe.scale[2]);

            // lerp = s_lerped[0];
            var s_lerped = new Vector3D<float>[model.header.num_xyz];

            LerpVerts(colorOnly, model.header.num_xyz, frame.verts, oldframe.verts, frame.verts, ref s_lerped, move, frontv, backv);

            // assert(sizeof(gl3_alias_vtx_t) == 9*sizeof(GLfloat));

            // all the triangle fans and triangle strips of this model will be converted to
            // just triangles: the vertices stay the same and are batched in vtxBuf,
            // but idxBuf will contain indices to draw them all as GL_TRIANGLE
            // this way there's only one draw call (and two glBufferData() calls)
            // instead of (at least) dozens. *greatly* improves performance.

            // so first clear out the data from last call to this function
            // (the buffers are static global so we don't have malloc()/free() for each rendered model)
            var (verts, indices) = CountVerticesAndIndices(model);
            var vertexBuffer = new gl3_alias_vtx_t[verts];
            var indexBuffer = new ushort[indices];
            int vertexCount = 0;
            int indexCount = 0;
            // da_clear(vtxBuf);
            // da_clear(idxBuf);

            int order = 0;
            while (true)
            {
                ushort nextVtxIdx = (ushort)vertexCount;

                /* get the vertex count and primitive type */
                var count = model.glcmds[order++];

                if (count == 0)
                {
                    break; /* done */
                }

                GLEnum type;
                if (count < 0)
                {
                    count = -count;

                    type = GLEnum.TriangleFan;
                }
                else
                {
                    type = GLEnum.TriangleStrip;
                }

            //     gl3_alias_vtx_t* buf = da_addn_uninit(vtxBuf, count);

                if (colorOnly)
                {
            //         int i;
                    for(int i = 0; i < count; ++i)
                    {
                        ref var cur = ref vertexBuffer[vertexCount + i];
                        var index_xyz = model.glcmds[order+2];
                        order += 3;

                        cur.pos = s_lerped[index_xyz];
                        cur.color = new Vector4D<float>(shadelight.X, shadelight.Y, shadelight.Z, alpha);
                    }
                }
                else
                {
                    for(int i=0; i<count; ++i)
                    {
                        ref var cur = ref vertexBuffer[vertexCount + i];
                        /* texture coordinates come from the draw list */
                        cur.texCoord.X = BitConverter.Int32BitsToSingle(model.glcmds[order+0]);
                        cur.texCoord.Y = BitConverter.Int32BitsToSingle(model.glcmds[order+1]);
                        var index_xyz = model.glcmds[order+2];

                        order += 3;

                        /* normals and vertexes come from the frame list */
                        // shadedots is set above according to rotation (around Z axis I think)
                        // to one of 16 (SHADEDOT_QUANT) presets in r_avertexnormal_dots
                        var l = shadedots[frame.verts[index_xyz].lightnormalindex];

                        cur.pos = s_lerped[index_xyz];
                        cur.color = new Vector4D<float>(l * shadelight.X, l * shadelight.Y, l * shadelight.Z, alpha);
                        cur.color.W = alpha;
                    }
                }
                vertexCount += count;

                // translate triangle fan/strip to just triangle indices
                if(type == GLEnum.TriangleFan)
                {
                    for(ushort i=1; i < count-1; ++i)
                    {
                        indexBuffer[indexCount++] = nextVtxIdx;
                        indexBuffer[indexCount++] = (ushort)(nextVtxIdx+i);
                        indexBuffer[indexCount++] = (ushort)(nextVtxIdx+i+1);
                    }
                }
                else // triangle strip
                {
                    ushort i;
                    for(i=1; i < count-2; i+=2)
                    {
                        // add two triangles at once, because the vertex order is different
                        // for odd vs even triangles
                        indexBuffer[indexCount++] = (ushort)(nextVtxIdx+i-1);
                        indexBuffer[indexCount++] = (ushort)(nextVtxIdx+i);
                        indexBuffer[indexCount++] = (ushort)(nextVtxIdx+i+1);

                        indexBuffer[indexCount++] = (ushort)(nextVtxIdx+i);
                        indexBuffer[indexCount++] = (ushort)(nextVtxIdx+i+2);
                        indexBuffer[indexCount++] = (ushort)(nextVtxIdx+i+1);
                    }
                    // add remaining triangle, if any
                    if(i < count-1)
                    {
                        indexBuffer[indexCount++] = (ushort)(nextVtxIdx+i-1);
                        indexBuffer[indexCount++] = (ushort)(nextVtxIdx+i);
                        indexBuffer[indexCount++] = (ushort)(nextVtxIdx+i+1);
                    }
                }
            }

            GL3_BindVAO(gl, gl3state.vaoAlias);
            GL3_BindVBO(gl, gl3state.vboAlias);

            fixed (void *p = vertexBuffer) {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertexCount*sizeof(gl3_alias_vtx_t)), p, BufferUsageARB.StreamDraw);
            }
            GL3_BindEBO(gl, gl3state.eboAlias);
            fixed (void *p = indexBuffer) {
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indexCount*sizeof(ushort)), p, BufferUsageARB.StreamDraw);
            }
            gl.DrawElements(PrimitiveType.Triangles, (uint)indexCount, DrawElementsType.UnsignedShort, null);
        }

        private bool CullAliasModel(out Vector3[] bbox, ref entity_t e)
        {
            // int i;
            // vec3_t mins, maxs;
            // dmdl_t *paliashdr;
            // vec3_t vectors[3];
            // vec3_t thismins, oldmins, thismaxs, oldmaxs;
            // daliasframe_t *pframe, *poldframe;
            // vec3_t angles;

            gl3aliasmodel_t model = (gl3aliasmodel_t)e.model!;

            var paliashdr = model.header;

            if ((e.frame >= paliashdr.num_frames) || (e.frame < 0))
            {
                R_Printf(QShared.PRINT_DEVELOPER, $"R_CullAliasModel {model.name}: no such frame {e.frame}\n");
                e.frame = 0;
            }

            if ((e.frame >= paliashdr.num_frames) || (e.oldframe < 0))
            {
                R_Printf(QShared.PRINT_DEVELOPER, $"R_CullAliasModel {model.name}: no such oldframe {e.oldframe}\n");
                e.oldframe = 0;
            }

            ref var pframe = ref model.frames[e.frame];
            ref var poldframe = ref model.frames[e.oldframe];

            /* compute axially aligned mins and maxs */
            Vector3 mins, maxs;
            if (pframe == poldframe)
            {
                mins = new Vector3(pframe.translate);
                maxs = mins + new Vector3(pframe.scale) * 255;
            }
            else
            {
                var thismins = new Vector3(pframe.translate);
                var thismaxs = thismins + new Vector3(pframe.scale) * 255;

                var oldmins = new Vector3(poldframe.translate);
                var oldmaxs = oldmins + new Vector3(poldframe.scale) * 255;

                mins = Vector3.Min(thismins, oldmins);
                maxs = Vector3.Max(thismaxs, oldmaxs);
            }

            /* compute a full bounding box */
            bbox = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                Vector3 tmp = new Vector3();

                if ((i & 1) != 0)
                {
                    tmp.X = mins.X;
                }
                else
                {
                    tmp.X = maxs.X;
                }

                if ((i & 2) != 0)
                {
                    tmp.Y = mins.Y;
                }
                else
                {
                    tmp.Y = maxs.Y;
                }

                if ((i & 4) != 0)
                {
                    tmp.Z = mins.Z;
                }
                else
                {
                    tmp.Z = maxs.Z;
                }

                bbox[i] = tmp;
            }

            /* rotate the bounding box */
            var angles = e.angles;
            angles.SetYaw(angles.Yaw());
            var vectors = new Vector3[3];
            QShared.AngleVectors(angles, ref vectors[0], ref vectors[1], ref vectors[2]);

            for (var i = 0; i < 8; i++)
            {
                var tmp = bbox[i];

                bbox[i].X = Vector3.Dot(vectors[0], tmp);
                bbox[i].Y = -Vector3.Dot(vectors[1], tmp);
                bbox[i].Z = Vector3.Dot(vectors[2], tmp);

                bbox[i] += e.origin;
            }

            // int p, f, aggregatemask = ~0;
            var aggregatemask = 0xFFFFFFFFU;

            for (int p = 0; p < 8; p++)
            {
                uint mask = 0;

                for (var f = 0; f < 4; f++)
                {
                    float dp = Vector3.Dot(frustum[f].normal, bbox[p]);

                    if ((dp - frustum[f].dist) < 0)
                    {
                        mask |= (1U << f);
                    }
                }

                aggregatemask &= mask;
            }

            if (aggregatemask != 0)
            {
                return true;
            }

            return false;
        }

        private void GL3_DrawAliasModel(GL gl, ref entity_t entity)
        {
            // int i;
            // dmdl_t *paliashdr;
            // float an;
            // vec3_t bbox[8];
            // vec3_t shadevector;
            // gl3image_t *skin;
            // hmm_mat4 origProjViewMat = {0}; // use for left-handed rendering
            // // used to restore ModelView matrix after changing it for this entities position/rotation
            // hmm_mat4 origModelMat = {0};

            if ((entity.flags & QShared.RF_WEAPONMODEL) == 0)
            {
                if (CullAliasModel(out var bbox, ref entity))
                {
                    return;
                }
            }

            // if (entity->flags & RF_WEAPONMODEL)
            // {
            //     if (gl_lefthand->value == 2)
            //     {
            //         return;
            //     }
            // }

            var model = (gl3aliasmodel_t)entity.model!;
            // paliashdr = (dmdl_t *)model->extradata;

            /* get lighting information */
            Vector3 shadelight = new Vector3();
            if ((entity.flags &
                (QShared.RF_SHELL_HALF_DAM | QShared.RF_SHELL_GREEN | QShared.RF_SHELL_RED |
                QShared.RF_SHELL_BLUE | QShared.RF_SHELL_DOUBLE)) != 0)
            {
            //     VectorClear(shadelight);

                if ((entity.flags & QShared.RF_SHELL_HALF_DAM) != 0)
                {
                    shadelight.X = 0.56f;
                    shadelight.Y = 0.59f;
                    shadelight.Z = 0.45f;
                }

                if ((entity.flags & QShared.RF_SHELL_DOUBLE) != 0)
                {
                    shadelight.X = 0.9f;
                    shadelight.Y = 0.7f;
                }

                if ((entity.flags & QShared.RF_SHELL_RED) != 0)
                {
                    shadelight.X = 1.0f;
                }

                if ((entity.flags & QShared.RF_SHELL_GREEN) != 0)
                {
                    shadelight.Y = 1.0f;
                }

                if ((entity.flags & QShared.RF_SHELL_BLUE) != 0)
                {
                    shadelight.Z = 1.0f;
                }
            }
            else if ((entity.flags & QShared.RF_FULLBRIGHT) != 0)
            {
                shadelight = new Vector3(1.0f);
            }
            else
            {
                GL3_LightPoint(entity, entity.origin, out shadelight);

            //     /* player lighting hack for communication back to server */
            //     if (entity->flags & RF_WEAPONMODEL)
            //     {
            //         /* pick the greatest component, which should be
            //         the same as the mono value returned by software */
            //         if (shadelight[0] > shadelight[1])
            //         {
            //             if (shadelight[0] > shadelight[2])
            //             {
            //                 r_lightlevel->value = 150 * shadelight[0];
            //             }
            //             else
            //             {
            //                 r_lightlevel->value = 150 * shadelight[2];
            //             }
            //         }
            //         else
            //         {
            //             if (shadelight[1] > shadelight[2])
            //             {
            //                 r_lightlevel->value = 150 * shadelight[1];
            //             }
            //             else
            //             {
            //                 r_lightlevel->value = 150 * shadelight[2];
            //             }
            //         }
            //     }
            }

            if ((entity.flags & QShared.RF_MINLIGHT) != 0)
            {
                if (shadelight.X <= 0.1 && shadelight.X <= 0.1 && shadelight.X <= 0.1)
                {
                    shadelight = new Vector3(0.1f);
                }
            }

            // if (entity.flags & RF_GLOW)
            // {
            //     /* bonus items will pulse with time */
            //     float scale;
            //     float min;

            //     scale = 0.1 * sin(gl3_newrefdef.time * 7);

            //     for (i = 0; i < 3; i++)
            //     {
            //         min = shadelight[i] * 0.8;
            //         shadelight[i] += scale;

            //         if (shadelight[i] < min)
            //         {
            //             shadelight[i] = min;
            //         }
            //     }
            // }

            // Note: gl_overbrightbits are now applied in shader.

            // /* ir goggles color override */
            // if ((gl3_newrefdef.rdflags & RDF_IRGOGGLES) && (entity->flags & RF_IR_VISIBLE))
            // {
            //     shadelight[0] = 1.0;
            //     shadelight[1] = 0.0;
            //     shadelight[2] = 0.0;
            // }

            // an = entity.angles[1] / 180 * M_PI;
            // shadevector[0] = cos(-an);
            // shadevector[1] = sin(-an);
            // shadevector[2] = 1;
            // VectorNormalize(shadevector);

            /* locate the proper data */
            c_alias_polys += model.header.num_tris;

            /* draw all the triangles */
            if ((entity.flags & QShared.RF_DEPTHHACK) != 0)
            {
                /* hack the depth range to prevent view model from poking into walls */
                gl.DepthRange(gl3depthmin, gl3depthmin + 0.3 * (gl3depthmax - gl3depthmin));
            }

            var origProjViewMat = gl3state.uni3DData.transProjViewMat4;
            if ((entity.flags &QShared.RF_WEAPONMODEL) != 0)
            {
            //     extern hmm_mat4 GL3_MYgluPerspective(GLdouble fovy, GLdouble aspect, GLdouble zNear, GLdouble zFar);

                // render weapon with a different FOV (r_gunfov) so it's not distorted at high view FOV
                double screenaspect = (double)gl3_newrefdef.width / (double)gl3_newrefdef.height;
                double dist = (r_farsee!.Int == 0) ? 4096.0 : 8192.0;

                Matrix4X4<float> projMat;
                if (r_gunfov!.Int < 0)
                {
                    projMat = GL3_MYgluPerspective(gl3_newrefdef.fov_y, screenaspect, 4, dist);
                }
                else
                {
                    projMat = GL3_MYgluPerspective(r_gunfov!.Double, screenaspect, 4, dist);
                }

            //     if(gl_lefthand->value == 1.0F)
            //     {
            //         // to mirror gun so it's rendered left-handed, just invert X-axis column
            //         // of projection matrix
            //         for(int i=0; i<4; ++i)
            //         {
            //             projMat.Elements[0][i] = - projMat.Elements[0][i];
            //         }
            //         //GL3_UpdateUBO3D(); Note: GL3_RotateForEntity() will call this,no need to do it twice before drawing

            //         glCullFace(GL_BACK);
            //     }
                gl3state.uni3DData.transProjViewMat4 = HMM_MultiplyMat4(projMat, gl3state.viewMat3D);
            }


            //glPushMatrix();
            var origModelMat = gl3state.uni3DData.transModelMat4;

            entity.angles.X = -entity.angles.X;
            GL3_RotateForEntity(gl, entity);
            entity.angles.X = -entity.angles.X;


            /* select skin */
            gl3image_t? skin;
            if (entity.skin != null)
            {
                skin = (gl3image_t)entity.skin; /* custom player skin */
            }
            else
            {
                if (entity.skinnum >= model.skins.Length)
                {
                    skin = model.skins[0];
                }
                else
                {
                    skin = model.skins[entity.skinnum];

                    if (skin == null)
                    {
                        skin = model.skins[0];
                    }
                }
            }

            if (skin == null)
            {
                skin = gl3_notexture; /* fallback... */
            }

            GL3_Bind(gl, skin!.texnum);

            // if (entity->flags & RF_TRANSLUCENT)
            // {
            //     glEnable(GL_BLEND);
            // }


            if ((entity.frame >= model.header.num_frames) ||
                (entity.frame < 0))
            {
                R_Printf(QShared.PRINT_DEVELOPER, $"R_DrawAliasModel {model.name}: no such frame {entity.frame}\n");
                entity.frame = 0;
                entity.oldframe = 0;
            }

            if ((entity.oldframe >= model.header.num_frames) ||
                (entity.oldframe < 0))
            {
                R_Printf(QShared.PRINT_DEVELOPER, $"R_DrawAliasModel {model.name}: no such oldframe {entity.oldframe}\n");
                entity.frame = 0;
                entity.oldframe = 0;
            }

            DrawAliasFrameLerp(gl, model, entity, shadelight);

            //glPopMatrix();
            gl3state.uni3DData.transModelMat4 = origModelMat;
            GL3_UpdateUBO3D(gl);

            if ((entity.flags & QShared.RF_WEAPONMODEL) != 0)
            {
                gl3state.uni3DData.transProjViewMat4 = origProjViewMat;
                GL3_UpdateUBO3D(gl);
            //     if(gl_lefthand->value == 1.0F)
            //         glCullFace(GL_FRONT);
            }

            // if (entity->flags & RF_TRANSLUCENT)
            // {
            //     glDisable(GL_BLEND);
            // }

            if ((entity.flags & QShared.RF_DEPTHHACK) != 0)
            {
                gl.DepthRange(gl3depthmin, gl3depthmax);
            }

            // if (gl_shadows->value && gl3config.stencil && !(entity->flags & (RF_TRANSLUCENT | RF_WEAPONMODEL | RF_NOSHADOW)))
            // {
            //     gl3_shadowinfo_t si = {0};
            //     VectorCopy(lightspot, si.lightspot);
            //     VectorCopy(shadevector, si.shadevector);
            //     si.paliashdr = paliashdr;
            //     si.entity = entity;

            //     da_push(shadowModels, si);
            // }
        }

    }
}