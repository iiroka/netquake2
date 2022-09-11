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

            // ClearLink(&anode->trigger_edicts);
            // ClearLink(&anode->solid_edicts);

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

            // if (ent == ge.edicts)
            // {
            //     return; /* don't add the world */
            // }

            if (!ent.inuse)
            {
                return;
            }

            /* set the size */
            ent.size = ent.maxs - ent.mins;

            /* encode the size into the entity_state for client prediction */
            // if ((ent->solid == SOLID_BBOX) && !(ent->svflags & SVF_DEADMONSTER))
            // {
            //     /* assume that x/y are equal and symetric */
            //     i = (int)ent->maxs[0] / 8;

            //     if (i < 1)
            //     {
            //         i = 1;
            //     }

            //     if (i > 31)
            //     {
            //         i = 31;
            //     }

            //     /* z is not symetric */
            //     j = (int)(-ent->mins[2]) / 8;

            //     if (j < 1)
            //     {
            //         j = 1;
            //     }

            //     if (j > 31)
            //     {
            //         j = 31;
            //     }

            //     /* and z maxs can be negative... */
            //     k = (int)(ent->maxs[2] + 32) / 8;

            //     if (k < 1)
            //     {
            //         k = 1;
            //     }

            //     if (k > 63)
            //     {
            //         k = 63;
            //     }

            //     ent->s.solid = (k << 10) | (j << 5) | i;
            // }
            // else if (ent->solid == SOLID_BSP)
            // {
            //     ent->s.solid = 31; /* a solid_bbox will never create this value */
            // }
            // else
            // {
            //     ent->s.solid = 0;
            // }

            // /* set the abs box */
            // if ((ent->solid == SOLID_BSP) &&
            //     (ent->s.angles[0] || ent->s.angles[1] ||
            //     ent->s.angles[2]))
            // {
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
            // }
            // else
            // {
            //     /* normal */
            //     VectorAdd(ent->s.origin, ent->mins, ent->absmin);
            //     VectorAdd(ent->s.origin, ent->maxs, ent->absmax);
            // }

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

            // /* get all leafs, including solids */
            // num_leafs = CM_BoxLeafnums(ent->absmin, ent->absmax,
            //         leafs, MAX_TOTAL_ENT_LEAFS, &topnode);

            // /* set areas */
            // for (i = 0; i < num_leafs; i++)
            // {
            //     clusters[i] = CM_LeafCluster(leafs[i]);
            //     area = CM_LeafArea(leafs[i]);

            //     if (area)
            //     {
            //         /* doors may legally straggle two areas,
            //         but nothing should evern need more than that */
            //         if (ent->areanum && (ent->areanum != area))
            //         {
            //             if (ent->areanum2 && (ent->areanum2 != area) &&
            //                 (sv.state == ss_loading))
            //             {
            //                 Com_DPrintf("Object touching 3 areas at %f %f %f\n",
            //                         ent->absmin[0], ent->absmin[1], ent->absmin[2]);
            //             }

            //             ent->areanum2 = area;
            //         }
            //         else
            //         {
            //             ent->areanum = area;
            //         }
            //     }
            // }

            // if (num_leafs >= MAX_TOTAL_ENT_LEAFS)
            // {
            //     /* assume we missed some leafs, and mark by headnode */
            //     ent->num_clusters = -1;
            //     ent->headnode = topnode;
            // }
            // else
            // {
            //     ent->num_clusters = 0;

            //     for (i = 0; i < num_leafs; i++)
            //     {
            //         if (clusters[i] == -1)
            //         {
            //             continue; /* not a visible leaf */
            //         }

            //         for (j = 0; j < i; j++)
            //         {
            //             if (clusters[j] == clusters[i])
            //             {
            //                 break;
            //             }
            //         }

            //         if (j == i)
            //         {
            //             if (ent->num_clusters == MAX_ENT_CLUSTERS)
            //             {
            //                 /* assume we missed some leafs, and mark by headnode */
            //                 ent->num_clusters = -1;
            //                 ent->headnode = topnode;
            //                 break;
            //             }

            //             ent->clusternums[ent->num_clusters++] = clusters[i];
            //         }
            //     }
            // }

            // /* if first time, make sure old_origin is valid */
            // if (!ent->linkcount)
            // {
            //     VectorCopy(ent->s.origin, ent->s.old_origin);
            // }

            // ent->linkcount++;

            // if (ent->solid == SOLID_NOT)
            // {
            //     return;
            // }

            // /* find the first node that the ent's box crosses */
            // node = sv_areanodes;

            // while (1)
            // {
            //     if (node->axis == -1)
            //     {
            //         break;
            //     }

            //     if (ent->absmin[node->axis] > node->dist)
            //     {
            //         node = node->children[0];
            //     }
            //     else if (ent->absmax[node->axis] < node->dist)
            //     {
            //         node = node->children[1];
            //     }
            //     else
            //     {
            //         break; /* crosses the node */
            //     }
            // }

            // /* link it in */
            // if (ent->solid == SOLID_TRIGGER)
            // {
            //     InsertLinkBefore(&ent->area, &node->trigger_edicts);
            // }
            // else
            // {
            //     InsertLinkBefore(&ent->area, &node->solid_edicts);
            // }
        }

    }
}