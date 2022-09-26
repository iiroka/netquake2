/*
 * Copyright (C) 1997-2001 Id Software, Inc.
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
 * Interface to the world model. Clipping and stuff like that...
 *
 * =======================================================================
 */
using System.Numerics;

namespace Quake2 {

    partial class QServer {

        private const int AREA_DEPTH = 4;
        private const int AREA_NODES = 32;
        private const int MAX_TOTAL_ENT_LEAFS = 128;


        private class areanode_t
        {
            public int axis; /* -1 = leaf node */
            public float dist;
            public areanode_t?[] children = new areanode_t?[2];
            public LinkedList<edict_s> trigger_edicts = new LinkedList<edict_s>();
            public LinkedList<edict_s> solid_edicts = new LinkedList<edict_s>();
            // link_t trigger_edicts;
            // link_t solid_edicts;
        }

        private areanode_t[] sv_areanodes = {};

        /*
        * Builds a uniformly subdivided tree for the given world size
        */
        private areanode_t SV_CreateAreaNode(int depth, in Vector3 mins, in Vector3 maxs)
        {
            // areanode_t *anode;
            // vec3_t size;
            // vec3_t mins1, maxs1, mins2, maxs2;

            var anode = new areanode_t();
            sv_areanodes.Append(anode);

            anode.trigger_edicts.Clear();
            anode.solid_edicts.Clear();

            if (depth == AREA_DEPTH)
            {
                anode.axis = -1;
                anode.children[0] = anode.children[1] = null;
                return anode;
            }

            var size = maxs - mins;

            if (size.X > size.Y)
            {
                anode.axis = 0;
                anode.dist = 0.5f * (maxs.X + mins.X);
            }
            else
            {
                anode.axis = 1;
                anode.dist = 0.5f * (maxs.Y + mins.Y);
            }

            var mins1 = mins;
            var mins2 = mins;
            var maxs1 = maxs;
            var maxs2 = maxs;

            if (anode.axis == 0)
            {
               maxs1.X = mins2.X = anode.dist; 
            }
            else
            {
               maxs1.Y = mins2.Y = anode.dist; 
            }

            anode.children[0] = SV_CreateAreaNode(depth + 1, mins2, maxs2);
            anode.children[1] = SV_CreateAreaNode(depth + 1, mins1, maxs1);

            return anode;
        }

        private void SV_ClearWorld()
        {
            sv_areanodes = new areanode_t[0];
            SV_CreateAreaNode(0, sv.models[1]!.mins, sv.models[1]!.maxs);
        }

        private void SV_LinkEdict(edict_s ent)
        {
            // areanode_t *node;
            // int leafs[MAX_TOTAL_ENT_LEAFS];
            // int clusters[MAX_TOTAL_ENT_LEAFS];
            // int num_leafs;
            // int i, j, k;
            // int area;
            // int topnode;

            // if (ent.area.prev)
            // {
            //     SV_UnlinkEdict(ent); /* unlink from old position */
            // }

            if (ent == ge!.getEdict(0))
            {
                return; /* don't add the world */
            }

            if (!ent.inuse)
            {
                return;
            }

            /* set the size */
            ent.size = ent.maxs - ent.mins;

            /* encode the size into the entity_state for client prediction */
            if ((ent.solid == solid_t.SOLID_BBOX) && (ent.svflags & QGameFlags.SVF_DEADMONSTER) == 0)
            {
                /* assume that x/y are equal and symetric */
                int i = (int)ent.maxs.X / 8;

                if (i < 1)
                {
                    i = 1;
                }

                if (i > 31)
                {
                    i = 31;
                }

                /* z is not symetric */
                int j = (int)(-ent.mins.Z) / 8;

                if (j < 1)
                {
                    j = 1;
                }

                if (j > 31)
                {
                    j = 31;
                }

                /* and z maxs can be negative... */
                int k = (int)(ent.maxs.Z + 32) / 8;

                if (k < 1)
                {
                    k = 1;
                }

                if (k > 63)
                {
                    k = 63;
                }

                ent.s.solid = (k << 10) | (j << 5) | i;
            }
            else if (ent.solid == solid_t.SOLID_BSP)
            {
                ent.s.solid = 31; /* a solid_bbox will never create this value */
            }
            else
            {
                ent.s.solid = 0;
            }

            /* set the abs box */
            if ((ent.solid == solid_t.SOLID_BSP) &&
                (ent.s.angles.X != 0 || ent.s.angles.Y != 0 || ent.s.angles.Z != 0))
            {
            //     /* expand for rotation */
            //     float max, v;
            //     int i;

            //     max = 0;

            //     for (i = 0; i < 3; i++)
            //     {
            //         v = (float)fabs(ent->mins[i]);

            //         if (v > max)
            //         {
            //             max = v;
            //         }

            //         v = (float)fabs(ent->maxs[i]);

            //         if (v > max)
            //         {
            //             max = v;
            //         }
            //     }

            //     for (i = 0; i < 3; i++)
            //     {
            //         ent->absmin[i] = ent->s.origin[i] - max;
            //         ent->absmax[i] = ent->s.origin[i] + max;
            //     }
            }
            else
            {
                /* normal */
                ent.absmin = ent.s.origin + ent.mins;
                ent.absmax = ent.s.origin + ent.maxs;
            }

            /* because movement is clipped an epsilon away from an actual edge,
            we must fully check even when bounding boxes don't quite touch */
            ent.absmin.X -= 1;
            ent.absmin.Y -= 1;
            ent.absmin.Z -= 1;
            ent.absmax.X += 1;
            ent.absmax.Y += 1;
            ent.absmax.Z += 1;

            /* link to PVS leafs */
            ent.num_clusters = 0;
            ent.areanum = 0;
            ent.areanum2 = 0;

            /* get all leafs, including solids */
            int num_leafs = common.CM_BoxLeafnums(ent.absmin, ent.absmax, out var leafs, out var topnode);

            /* set areas */
            var clusters = new int[num_leafs];
            for (int i = 0; i < num_leafs; i++)
            {
                clusters[i] = common.CM_LeafCluster(leafs[i]);
                int area = common.CM_LeafArea(leafs[i]);

                if (area != 0)
                {
                    /* doors may legally straggle two areas,
                    but nothing should evern need more than that */
                    if (ent.areanum != 0 && (ent.areanum != area))
                    {
            //             if (ent->areanum2 && (ent->areanum2 != area) &&
            //                 (sv.state == ss_loading))
            //             {
            //                 Com_DPrintf("Object touching 3 areas at %f %f %f\n",
            //                         ent->absmin[0], ent->absmin[1], ent->absmin[2]);
            //             }

                        ent.areanum2 = area;
                    }
                    else
                    {
                        ent.areanum = area;
                    }
                }
            }

            if (num_leafs >= MAX_TOTAL_ENT_LEAFS)
            {
                /* assume we missed some leafs, and mark by headnode */
                ent.num_clusters = -1;
                ent.headnode = topnode;
            }
            else
            {
                ent.num_clusters = 0;

                for (int i = 0; i < num_leafs; i++)
                {
                    if (clusters[i] == -1)
                    {
                        continue; /* not a visible leaf */
                    }

                    int j;
                    for (j = 0; j < i; j++)
                    {
                        if (clusters[j] == clusters[i])
                        {
                            break;
                        }
                    }

                    if (j == i)
                    {
                        if (ent.num_clusters == QGameFlags.MAX_ENT_CLUSTERS)
                        {
                            /* assume we missed some leafs, and mark by headnode */
                            ent.num_clusters = -1;
                            ent.headnode = topnode;
                            break;
                        }

                        ent.clusternums[ent.num_clusters++] = clusters[i];
                    }
                }
            }

            /* if first time, make sure old_origin is valid */
            if (ent.linkcount == 0)
            {
                ent.s.old_origin = ent.s.origin;
            }

            ent.linkcount++;

            if (ent.solid == solid_t.SOLID_NOT)
            {
                return;
            }

            /* find the first node that the ent's box crosses */
            ref var node = ref sv_areanodes[0];

            while (true)
            {
                if (node!.axis == -1)
                {
                    break;
                } 
                float max, min;
                if  (node.axis == 0)
                {
                    min = ent.absmin.X;
                    max = ent.absmax.X;
                } else if  (node.axis == 1)
                {
                    min = ent.absmin.Y;
                    max = ent.absmax.Y;
                } else
                {
                    min = ent.absmin.Z;
                    max = ent.absmax.Z;
                }
                if (min > node.dist)
                {
                    node = node.children[0];
                }
                else if (max < node.dist)
                {
                    node = node.children[1];
                }
                else
                {
                    break; /* crosses the node */
                }
            }

            /* link it in */
            if (ent.solid == solid_t.SOLID_TRIGGER)
            {
            //     InsertLinkBefore(&ent->area, &node->trigger_edicts);
            }
            else
            {
            //     InsertLinkBefore(&ent->area, &node->solid_edicts);
            }
        }

        private struct moveclip_t
        {
            public Vector3 boxmins, boxmaxs; /* enclose the test object along entire move */
            public Vector3 mins, maxs; /* size of the moving object */
            public Vector3 mins2, maxs2; /* size when clipping against mosnters */
            public Vector3 start, end;
            public QShared.trace_t trace;
            public edict_s passedict;
            public int contentmask;
        }

        /*
        * Moves the given mins/maxs volume through the world from start to end.
        * Passedict and edicts owned by passedict are explicitly not checked.
        */
        private QShared.trace_t SV_Trace(in Vector3 start, in Vector3? mins, in Vector3? maxs, in Vector3 end,
                edict_s passedict, int contentmask)
        {
            moveclip_t clip;


            // if (!mins)
            // {
            //     mins = vec3_origin;
            // }

            // if (!maxs)
            // {
            //     maxs = vec3_origin;
            // }

            clip = new moveclip_t();
            clip.trace = new QShared.trace_t();

            /* clip to world */
            // clip.trace = CM_BoxTrace(start, end, mins, maxs, 0, contentmask);
            // clip.trace.ent = ge->edicts;

            // if (clip.trace.fraction == 0)
            // {
            //     return clip.trace; /* blocked by the world */
            // }

            clip.contentmask = contentmask;
            clip.start = start;
            clip.end = end;
            clip.mins = mins ?? Vector3.Zero;
            clip.maxs = maxs ?? Vector3.Zero;
            clip.passedict = passedict;

            clip.mins2 = mins ?? Vector3.Zero;
            clip.maxs2 = mins ?? Vector3.Zero;

            /* create the bounding box of the entire move */
            // SV_TraceBounds(start, clip.mins2, clip.maxs2,
            //         end, clip.boxmins, clip.boxmaxs);

            // /* clip to other solid entities */
            // SV_ClipMoveToEntities(&clip);

            return clip.trace;
        }



    }
}