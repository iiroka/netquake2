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
 * The collision model. Slaps "boxes" through the world and checks if
 * they collide with the world model, entities or other boxes.
 *
 * =======================================================================
 */
using System.Numerics;
using System.Text;

namespace Quake2 {

    partial class QCommon {

        private struct cnode_t
        {
            public QShared.cplane_t	plane;
            public int[]			children; /* negative numbers are leafs */
        }

        private struct cbrushside_t
        {
            public QShared.cplane_t	plane;
            public QShared.mapsurface_t	surface;
        }

        private class cleaf_t
        {
            public int			contents;
            public int			cluster;
            public int			area;
            public ushort	firstleafbrush;
            public ushort	numleafbrushes;
        }

        private class cbrush_t
        {
            public int			contents;
            public int			numsides;
            public int			firstbrushside;
            public int			checkcount;	/* to avoid repeated testings */
        }

        private struct carea_t
        {
            public int		numareaportals;
            public int		firstareaportal;
            public int		floodnum; /* if two areas have equal floodnums, they are connected */
            public int		floodvalid;
        }

        private carea_t[] map_areas = new carea_t[MAX_MAP_AREAS];
        private cbrush_t[] map_brushes = new cbrush_t[MAX_MAP_BRUSHES];
        private cbrushside_t[] map_brushsides = new cbrushside_t[MAX_MAP_BRUSHSIDES];
        private string map_name = "";
        private string map_entitystring = "";
        private cleaf_t[] map_leafs = new cleaf_t[MAX_MAP_LEAFS];
        private QShared.cmodel_t[] map_cmodels = new QShared.cmodel_t[MAX_MAP_MODELS];
        private cnode_t[] map_nodes = new cnode_t[MAX_MAP_NODES+6]; /* extra for box hull */
        private QShared.cplane_t[] map_planes = new QShared.cplane_t[MAX_MAP_PLANES+6]; /* extra for box hull */
        private cvar_t? map_noareas;
        private byte[] map_visibility;
        private dvis_t map_vis;
        private dareaportal_t[] map_areaportals = new dareaportal_t[MAX_MAP_AREAPORTALS];
        private int box_headnode;
        private int box_headplane;
        private int	checkcount;
        private cbrush_t box_brush;
        private cleaf_t	box_leaf;

        private int	emptyleaf, solidleaf;
        private int	floodvalid;
        private Vector3 leaf_mins, leaf_maxs;
        private int leaf_count;
        private int[] leaf_list;
        private int leaf_topnode;
        
        private int	numareaportals;
        private int numareas = 1;
        private int	numbrushes;
        private int	numbrushsides;
        private int numclusters = 1;
        private int	numcmodels;
        // private int	numentitychars;
        private int	numleafbrushes;
        private int numleafs = 1; /* allow leaf funcs to be called without a map */
        private int	numnodes;
        private int	numplanes;
        private int	numtexinfo;
        private int	numvisibility;
        private int trace_contents;
        private QShared.mapsurface_t[] map_surfaces = new QShared.mapsurface_t[MAX_MAP_TEXINFO];
        private QShared.mapsurface_t nullsurface = new QShared.mapsurface_t();
        private bool[] portalopen = new bool[MAX_MAP_AREAPORTALS];
        private bool trace_ispoint;
        private QShared.trace_t trace_trace;
        private ushort[] map_leafbrushes = new ushort[MAX_MAP_LEAFBRUSHES];
        private Vector3 trace_start, trace_end;
        private Vector3 trace_mins, trace_maxs;
        private Vector3 trace_extents;

        private uint last_checksum = 0;

        /* 1/32 epsilon to keep floating point happy */
        private const float DIST_EPSILON = 0.03125f;

        private void CM_Init()
        {
            for (int i = 0; i < map_brushes.Length; i++){
                map_brushes[i] = new cbrush_t();
            }
            for (int i = 0; i < map_leafs.Length; i++){
                map_leafs[i] = new cleaf_t();
            }
            for (int i = 0; i < map_cmodels.Length; i++){
                map_cmodels[i] = new QShared.cmodel_t();
            }
            for (int i = 0; i < map_surfaces.Length; i++) {
                map_surfaces[i] = new QShared.mapsurface_t();
            }
            for (int i = 0; i < map_planes.Length; i++) {
                map_planes[i] = new QShared.cplane_t();
            }
            for (int i = 0; i < map_nodes.Length; i++) {
                map_nodes[i].children = new int[2];
            }
        }

        private void FloodArea_r(ref carea_t area, int floodnum)
        {
            if (area.floodvalid == floodvalid)
            {
                if (area.floodnum == floodnum)
                {
                    return;
                }

                Com_Error(QShared.ERR_DROP, "FloodArea_r: reflooded");
            }

            area.floodnum = floodnum;
            area.floodvalid = floodvalid;

            for (int i = 0; i < area.numareaportals; i++)
            {
                ref var p = ref map_areaportals[area.firstareaportal + i];
                if (portalopen[p.portalnum])
                {
                    FloodArea_r(ref map_areas[p.otherarea], floodnum);
                }
            }
        }

        private void FloodAreaConnections()
        {

            /* all current floods are now invalid */
            floodvalid++;
            int floodnum = 0;

            /* area 0 is not used */
            for (int i = 1; i < numareas; i++)
            {
                ref var area = ref map_areas[i];

                if (area.floodvalid == floodvalid)
                {
                    continue; /* already flooded into */
                }

                floodnum++;
                FloodArea_r(ref area, floodnum);
            }
        }

        public void CM_SetAreaPortalState(int portalnum, bool open)
        {
            if (portalnum > numareaportals)
            {
                Com_Error(QShared.ERR_DROP, "areaportal > numareaportals");
            }

            portalopen[portalnum] = open;
            FloodAreaConnections();
        }

        public bool CM_AreasConnected(int area1, int area2)
        {
            if (map_noareas!.Bool)
            {
                return true;
            }

            if ((area1 > numareas) || (area2 > numareas))
            {
                Com_Error(QShared.ERR_DROP, "area > numareas");
            }

            if (map_areas[area1].floodnum == map_areas[area2].floodnum)
            {
                return true;
            }

            return false;
        }

        /*
        * Writes a length byte followed by a bit vector of all the areas
        * that area in the same flood as the area parameter
        *
        * This is used by the client refreshes to cull visibility
        */
        public int CM_WriteAreaBits(ref byte[] buffer, int area)
        {
            var bytes = (numareas + 7) >> 3;
            buffer = new byte[bytes];

            if (map_noareas!.Bool)
            {
                /* for debugging, send everything */
                Array.Fill(buffer, (byte)255, 0, bytes);
            }

            else
            {
                Array.Fill(buffer, (byte)0, 0, bytes);

                var floodnum = map_areas[area].floodnum;

                for (int i = 0; i < numareas; i++)
                {
                    if ((map_areas[i].floodnum == floodnum) || area == 0)
                    {
                        buffer[i >> 3] |= (byte)(1 << (i & 7));
                    }
                }
            }

            return bytes;
        }

        /*
        * Returns true if any leaf under headnode has a cluster that
        * is potentially visible
        */
        public bool CM_HeadnodeVisible(int nodenum, byte[] visbits)
        {
            if (nodenum < 0)
            {
                int leafnum1 = -1 - nodenum;
                int cluster = map_leafs[leafnum1].cluster;

                if (cluster == -1)
                {
                    return false;
                }

                if ((visbits[cluster >> 3] & (1 << (cluster & 7))) != 0)
                {
                    return true;
                }

                return false;
            }

            ref var node = ref map_nodes[nodenum];

            if (CM_HeadnodeVisible(node.children[0], visbits))
            {
                return true;
            }

            return CM_HeadnodeVisible(node.children[1], visbits);
        }


        /*
        * Set up the planes and nodes so that the six floats of a bounding box
        * can just be stored out and get a proper clipping hull structure.
        */
        private void CM_InitBoxHull()
        {
            // int i;
            // int side;
            // cnode_t *c;
            // cplane_t *p;
            // cbrushside_t *s;

            box_headnode = numnodes;
            box_headplane = numplanes;

            if ((numnodes + 6 > MAX_MAP_NODES) ||
                (numbrushes + 1 > MAX_MAP_BRUSHES) ||
                (numleafbrushes + 1 > MAX_MAP_LEAFBRUSHES) ||
                (numbrushsides + 6 > MAX_MAP_BRUSHSIDES) ||
                (numplanes + 12 > MAX_MAP_PLANES))
            {
                Com_Error(QShared.ERR_DROP, "Not enough room for box tree");
            }

            box_brush = map_brushes[numbrushes];
            box_brush.numsides = 6;
            box_brush.firstbrushside = numbrushsides;
            box_brush.contents = CONTENTS_MONSTER;

            box_leaf = map_leafs[numleafs];
            box_leaf.contents = CONTENTS_MONSTER;
            box_leaf.firstleafbrush = (ushort)numleafbrushes;
            box_leaf.numleafbrushes = 1;

            map_leafbrushes[numleafbrushes] = (ushort)numbrushes;

            for (int i = 0; i < 6; i++)
            {
                int side = i & 1;

                /* brush sides */
                ref var s = ref map_brushsides[numbrushsides + i];
                s.plane = map_planes[numplanes + i * 2 + side];
                s.surface = nullsurface;

                /* nodes */
                ref var c = ref map_nodes[box_headnode + i];
                c.plane = map_planes[numplanes + i * 2];
                c.children[side] = -1 - emptyleaf;

                if (i != 5)
                {
                    c.children[side ^ 1] = box_headnode + i + 1;
                }

                else
                {
                    c.children[side ^ 1] = -1 - numleafs;
                }

                /* planes */
                var p = map_planes[box_headplane + i * 2];
                p.type = (byte)(i >> 1);
                p.signbits = 0;
                p.normal = new Vector3();
                switch (i >> 1)
                {
                    case 0: p.normal.X = 1; break;
                    case 1: p.normal.Y = 1; break;
                    default: p.normal.Z = 1; break;
                }

                p = map_planes[box_headplane + i * 2 + 1];
                p.type = (byte)(3 + (i >> 1));
                p.signbits = 0;
                p.normal = new Vector3();
                switch (i >> 1)
                {
                    case 0: p.normal.X = -1; break;
                    case 1: p.normal.Y = -1; break;
                    default: p.normal.Z = -1; break;
                }
            }
        }

        private int CM_PointLeafnum_r(in Vector3 p, int num)
        {
            // float d;
            // cnode_t *node;
            // cplane_t *plane;

            while (num >= 0)
            {
                ref var node = ref map_nodes[num];
                var plane = node.plane;

                float d;
                if (plane.type == 0)
                {
                    d = p.X - plane.dist;
                }
                else if (plane.type == 1)
                {
                    d = p.Y - plane.dist;
                }
                else if (plane.type == 2)
                {
                    d = p.Z - plane.dist;
                }

                else
                {
                    d = Vector3.Dot(plane.normal, p) - plane.dist;
                }

                if (d < 0)
                {
                    num = node.children[1];
                }

                else
                {
                    num = node.children[0];
                }
            }

        // #ifndef DEDICATED_ONLY
            // c_pointcontents++; /* optimize counter */
        // #endif

            return -1 - num;
        }

        public int CM_PointLeafnum(in Vector3 p)
        {
            if (numplanes == 0)
            {
                return 0; /* sound may call this without map loaded */
            }

            return CM_PointLeafnum_r(p, 0);
        }

        /*
        * To keep everything totally uniform, bounding boxes are turned into
        * small BSP trees instead of being compared directly.
        */
        public int CM_HeadnodeForBox(in Vector3 mins, in Vector3 maxs)
        {
            map_planes[numplanes+0].dist = maxs.X;
            map_planes[numplanes+1].dist = -maxs.X;
            map_planes[numplanes+2].dist = mins.X;
            map_planes[numplanes+3].dist = -mins.X;
            map_planes[numplanes+4].dist = maxs.Y;
            map_planes[numplanes+5].dist = -maxs.Y;
            map_planes[numplanes+6].dist = mins.Y;
            map_planes[numplanes+7].dist = -mins.Y;
            map_planes[numplanes+8].dist = maxs.Z;
            map_planes[numplanes+9].dist = -maxs.Z;
            map_planes[numplanes+10].dist = mins.Z;
            map_planes[numplanes+11].dist = -mins.Z;

            return box_headnode;
        }


        /*
        * Fills in a list of all the leafs touched
        */

        private void CM_BoxLeafnums_r(int nodenum)
        {
            // cplane_t *plane;
            // cnode_t *node;
            // int s;

            while (true)
            {
                if (nodenum < 0)
                {
                    if (leaf_count >= leaf_list.Length)
                    {
                        return;
                    }

                    leaf_list[leaf_count++] = -1 - nodenum;
                    return;
                }

                ref var node = ref map_nodes[nodenum];
                var plane = node.plane;
                var s = QShared.BoxOnPlaneSide(leaf_mins, leaf_maxs, plane);

                if (s == 1)
                {
                    nodenum = node.children[0];
                }

                else if (s == 2)
                {
                    nodenum = node.children[1];
                }

                else
                {
                    /* go down both */
                    if (leaf_topnode == -1)
                    {
                        leaf_topnode = nodenum;
                    }

                    CM_BoxLeafnums_r(node.children[0]);
                    nodenum = node.children[1];
                }
            }
        }

        private int CM_BoxLeafnums_headnode(in Vector3 mins, in Vector3 maxs, out int[] list, int headnode, out int topnode)
        {
            leaf_list = new int[64];
            leaf_count = 0;
            leaf_mins = mins;
            leaf_maxs = maxs;

            leaf_topnode = -1;

            CM_BoxLeafnums_r(headnode);

            topnode = leaf_topnode;
            list = leaf_list;

            return leaf_count;
        }

        public int CM_BoxLeafnums(in Vector3 mins, in Vector3 maxs, out int[] list, out int topnode)
        {
            return CM_BoxLeafnums_headnode(mins, maxs, out list, map_cmodels[0].headnode, out topnode);
        }
        
        private void CM_ClipBoxToBrush(in Vector3 mins, in Vector3 maxs, in Vector3 p1,
                in Vector3 p2, ref QShared.trace_t trace, cbrush_t brush)
        {
            float enterfrac = -1;
            float leavefrac = 1;
            QShared.cplane_t? clipplane = null;

            if (brush.numsides == 0)
            {
                return;
            }

        // #ifndef DEDICATED_ONLY
        //     c_brush_traces++;
        // #endif

            var getout = false;
            var startout = false;
            // leadside = NULL;
            var leadside_i = -1;

            for (int i = 0; i < brush.numsides; i++)
            {
                ref var side = ref map_brushsides[brush.firstbrushside + i];
                var plane = side.plane;
                float dist;

                if (!trace_ispoint)
                {
                    /* general box case
                    push the plane out
                    apropriately for mins/maxs */
                    var ofs = new Vector3(
                        plane.normal.X < 0 ? maxs.X : mins.X,
                        plane.normal.Y < 0 ? maxs.Y : mins.Y,
                        plane.normal.Z < 0 ? maxs.Z : mins.Z
                    );

                    dist = Vector3.Dot(ofs, plane.normal);
                    dist = plane.dist - dist;
                }

                else
                {
                    /* special point case */
                    dist = plane.dist;
                }

                var d1 = Vector3.Dot(p1, plane.normal) - dist;
                var d2 = Vector3.Dot(p2, plane.normal) - dist;

                if (d2 > 0)
                {
                    getout = true; /* endpoint is not in solid */
                }

                if (d1 > 0)
                {
                    startout = true;
                }

                /* if completely in front of face, no intersection */
                if ((d1 > 0) && (d2 >= d1))
                {
                    return;
                }

                if ((d1 <= 0) && (d2 <= 0))
                {
                    continue;
                }

                /* crosses face */
                if (d1 > d2)
                {
                    /* enter */
                    var f = (d1 - DIST_EPSILON) / (d1 - d2);

                    if (f > enterfrac)
                    {
                        enterfrac = f;
                        clipplane = plane;
                        // leadside = side;
                        leadside_i = brush.firstbrushside + i;
                    }
                }

                else
                {
                    /* leave */
                    var f = (d1 + DIST_EPSILON) / (d1 - d2);

                    if (f < leavefrac)
                    {
                        leavefrac = f;
                    }
                }
            }

            if (!startout)
            {
                /* original point was inside brush */
                trace.startsolid = true;

                if (!getout)
                {
                    trace.allsolid = true;
                }

                return;
            }

            if (enterfrac < leavefrac)
            {
                if ((enterfrac > -1) && (enterfrac < trace.fraction))
                {
                    if (enterfrac < 0)
                    {
                        enterfrac = 0;
                    }

                    if (clipplane == null)
                    {
                        Com_Error(QShared.ERR_FATAL, "clipplane was NULL!\n");
                    }

                    trace.fraction = enterfrac;
                    trace.plane = clipplane!;
                    trace.surface = map_brushsides[leadside_i].surface.c;
                    trace.contents = brush.contents;
                }
            }
        }

        private void CM_TestBoxInBrush(in Vector3 mins, in Vector3 maxs, in Vector3 p1,
                ref QShared.trace_t trace, cbrush_t brush)
        {
            if (brush.numsides == 0)
            {
                return;
            }

            for (int i = 0; i < brush.numsides; i++)
            {
                ref var side = ref map_brushsides[brush.firstbrushside + i];
                var plane = side.plane;

                /* general box case
                push the plane out
                apropriately for mins/maxs */
                var ofs = new Vector3(
                    plane.normal.X < 0 ? maxs.X : mins.X,
                    plane.normal.Y < 0 ? maxs.Y : mins.Y,
                    plane.normal.Z < 0 ? maxs.Z : mins.Z
                );


                var dist = Vector3.Dot(ofs, plane.normal);
                dist = plane.dist - dist;

                var d1 = Vector3.Dot(p1, plane.normal) - dist;

                /* if completely in front of face, no intersection */
                if (d1 > 0)
                {
                    return;
                }
            }

            /* inside this brush */
            trace.startsolid = trace.allsolid = true;
            trace.fraction = 0;
            trace.contents = brush.contents;
        }

        private void CM_TraceToLeaf(int leafnum)
        {
            ref var leaf = ref map_leafs[leafnum];

            if ((leaf.contents & trace_contents) == 0)
            {
                return;
            }

            /* trace line against all brushes in the leaf */
            for (int k = 0; k < leaf.numleafbrushes; k++)
            {
                int brushnum = map_leafbrushes[leaf.firstleafbrush + k];
                ref var b = ref map_brushes[brushnum];

                if (b.checkcount == checkcount)
                {
                    continue; /* already checked this brush in another leaf */
                }

                b.checkcount = checkcount;

                if ((b.contents & trace_contents) == 0)
                {
                    continue;
                }

                CM_ClipBoxToBrush(trace_mins, trace_maxs, trace_start, trace_end, ref trace_trace, b);

                if (trace_trace.fraction == 0)
                {
                    return;
                }
            }
        }

        private void CM_TestInLeaf(int leafnum)
        {
            ref var leaf = ref map_leafs[leafnum];

            if ((leaf.contents & trace_contents) == 0)
            {
                return;
            }

            /* trace line against all brushes in the leaf */
            for (int k = 0; k < leaf.numleafbrushes; k++)
            {
                int brushnum = map_leafbrushes[leaf.firstleafbrush + k];
                ref var b = ref map_brushes[brushnum];

                if (b.checkcount == checkcount)
                {
                    continue; /* already checked this brush in another leaf */
                }

                b.checkcount = checkcount;

                if ((b.contents & trace_contents) == 0)
                {
                    continue;
                }

                CM_TestBoxInBrush(trace_mins, trace_maxs, trace_start, ref trace_trace, b);

                if (trace_trace.fraction == 0)
                {
                    return;
                }
            }
        }

        private void CM_RecursiveHullCheck(int num, float p1f, float p2f, in Vector3 p1, in Vector3 p2)
        {
            if (trace_trace.fraction <= p1f)
            {
                return; /* already hit something nearer */
            }

            /* if < 0, we are in a leaf node */
            if (num < 0)
            {
                CM_TraceToLeaf(-1 - num);
                return;
            }

            /* find the point distances to the seperating plane
            and the offset for the size of the box */
            ref var node = ref map_nodes[num];
            var plane = node.plane;

            float t1, t2, offset;
            if (plane.type < 3)
            {
                t1 = p1.Get(plane.type) - plane.dist;
                t2 = p2.Get(plane.type) - plane.dist;
                offset = trace_extents.Get(plane.type);
            }
            else
            {
                t1 = Vector3.Dot(plane.normal, p1) - plane.dist;
                t2 = Vector3.Dot(plane.normal, p2) - plane.dist;

                if (trace_ispoint)
                {
                    offset = 0;
                }

                else
                {
                    offset = MathF.Abs(trace_extents.X * plane.normal.X) +
                            MathF.Abs(trace_extents.Y * plane.normal.Y) +
                            MathF.Abs(trace_extents.Z * plane.normal.Z);
                }
            }

            /* see which sides we need to consider */
            if ((t1 >= offset) && (t2 >= offset))
            {
                CM_RecursiveHullCheck(node.children[0], p1f, p2f, p1, p2);
                return;
            }

            if ((t1 < -offset) && (t2 < -offset))
            {
                CM_RecursiveHullCheck(node.children[1], p1f, p2f, p1, p2);
                return;
            }

            /* put the crosspoint DIST_EPSILON pixels on the near side */
            float frac, frac2;
            int side;
            if (t1 < t2)
            {
                var idist = 1.0f / (t1 - t2);
                side = 1;
                frac2 = (t1 + offset + DIST_EPSILON) * idist;
                frac = (t1 - offset + DIST_EPSILON) * idist;
            }

            else if (t1 > t2)
            {
                var idist = 1.0f / (t1 - t2);
                side = 0;
                frac2 = (t1 - offset - DIST_EPSILON) * idist;
                frac = (t1 + offset + DIST_EPSILON) * idist;
            }

            else
            {
                side = 0;
                frac = 1;
                frac2 = 0;
            }

            /* move up to the node */
            if (frac < 0)
            {
                frac = 0;
            }

            if (frac > 1)
            {
                frac = 1;
            }

            var midf = p1f + (p2f - p1f) * frac;

            var mid = p1 + frac * (p2 - p1);

            CM_RecursiveHullCheck(node.children[side], p1f, midf, p1, mid);

            /* go past the node */
            if (frac2 < 0)
            {
                frac2 = 0;
            }

            if (frac2 > 1)
            {
                frac2 = 1;
            }

            midf = p1f + (p2f - p1f) * frac2;

            mid = p1 + frac2 * (p2 - p1);

            CM_RecursiveHullCheck(node.children[side ^ 1], midf, p2f, mid, p2);
        }

        public QShared.trace_t CM_BoxTrace(in Vector3 start, in Vector3 end, in Vector3 mins, in Vector3 maxs,
                int headnode, int brushmask)
        {
            // int i;

            checkcount++; /* for multi-check avoidance */

        // #ifndef DEDICATED_ONLY
        //     c_traces++; /* for statistics, may be zeroed */
        // #endif

            /* fill in a default trace */
            trace_trace = new QShared.trace_t();
            trace_trace.plane = new QShared.cplane_t();
            trace_trace.fraction = 1;
            trace_trace.surface = nullsurface.c;

            if (numnodes == 0)  /* map not loaded */
            {
                return trace_trace;
            }

            trace_contents = brushmask;
            trace_start = start;
            trace_end = end;
            trace_mins = mins;
            trace_maxs = maxs;

            /* check for position test special case */
            if (start == end)
            {
                // int leafs[1024];
                // int i, numleafs;
                // vec3_t c1, c2;
                // int topnode;

                var c1 = start + mins;
                var c2 = start + maxs;

                c1 -= new Vector3(1);
                c2 += new Vector3(1);

                int numleafs = CM_BoxLeafnums_headnode(c1, c2, out var leafs, headnode, out var topnode);

                for (int i = 0; i < numleafs; i++)
                {
                    CM_TestInLeaf(leafs[i]);

                    if (trace_trace.allsolid)
                    {
                        break;
                    }
                }

                trace_trace.endpos = start;
                return trace_trace;
            }

            /* check for point special case */
            if ((mins.X == 0) && (mins.Y == 0) && (mins.Z == 0) &&
                (maxs.X == 0) && (maxs.Y == 0) && (maxs.Z == 0))
            {
                trace_ispoint = true;
                trace_extents = Vector3.Zero;
            }

            else
            {
                trace_ispoint = false;
                trace_extents.X = -mins.X > maxs.X ? -mins.X : maxs.X;
                trace_extents.Y = -mins.Y > maxs.Y ? -mins.Y : maxs.Y;
                trace_extents.Z = -mins.Z > maxs.Z ? -mins.Z : maxs.Z;
            }

            /* general sweeping through world */
            CM_RecursiveHullCheck(headnode, 0, 1, start, end);

            if (trace_trace.fraction == 1)
            {
                trace_trace.endpos = end;
            }

            else
            {
                trace_trace.endpos = start + trace_trace.fraction * (end - start);
            }

            return trace_trace;
        }


        /*
        * Handles offseting and rotation of the end points for moving and
        * rotating entities
        */
        public QShared.trace_t CM_TransformedBoxTrace(in Vector3 start, in Vector3 end, in Vector3 mins, in Vector3 maxs,
                int headnode, int brushmask, in Vector3 origin, in Vector3 angles)
        {
            /* subtract origin offset */
            var start_l = start - origin;
            var end_l = end - origin;

            /* rotate start and end into the models frame of reference */
            bool rotated;
            if ((headnode != box_headnode) &&
                (angles.X != 0 || angles.Y != 0 || angles.Z != 0))
            {
                rotated = true;
            }

            else
            {
                rotated = false;
            }

            Vector3 forward = new Vector3(), right = new Vector3(), up = new Vector3();
            if (rotated)
            {
                QShared.AngleVectors(angles, ref forward, ref right, ref up);

                var temp = start_l;
                start_l.X = Vector3.Dot(temp, forward);
                start_l.Y = -Vector3.Dot(temp, right);
                start_l.Z = Vector3.Dot(temp, up);

                temp = end_l;
                end_l.X = Vector3.Dot(temp, forward);
                end_l.Y = -Vector3.Dot(temp, right);
                end_l.Z = Vector3.Dot(temp, up);
            }

            /* sweep the box through the model */
            var trace = CM_BoxTrace(start_l, end_l, mins, maxs, headnode, brushmask);

            if (rotated && (trace.fraction != 1.0))
            {
                var a = Vector3.Negate(angles);
                QShared.AngleVectors(a, ref forward, ref right, ref up);

                var temp = trace.plane.normal;
                trace.plane.normal.X = Vector3.Dot(temp, forward);
                trace.plane.normal.Y = -Vector3.Dot(temp, right);
                trace.plane.normal.Z = Vector3.Dot(temp, up);
            }

            trace.endpos = start + trace.fraction * (end - start);

            return trace;
        }

        private void CMod_LoadSubmodels(byte[] buf, in lump_t l)
        {
            // dmodel_t *in;
            // cmodel_t *out;
            // int i, j, count;

            // in = (void *)(cmod_base + l->fileofs);

            if ((l.filelen % dmodel_t.size) != 0)
            {
                Com_Error(QShared.ERR_DROP, "Mod_LoadSubmodels: funny lump size");
            }

            int count = l.filelen / dmodel_t.size;

            if (count < 1)
            {
                Com_Error(QShared.ERR_DROP, "Map with no models");
            }

            if (count > MAX_MAP_MODELS)
            {
                Com_Error(QShared.ERR_DROP, "Map has too many models");
            }

            numcmodels = count;

            for (int i = 0; i < count; i++)
            {
                var ind = new dmodel_t(buf, l.fileofs + i * dmodel_t.size);

                /* spread the mins / maxs by a pixel */
                map_cmodels[i].mins = new Vector3(ind.mins[0]-1, ind.mins[1]-1, ind.mins[2]-1);
                map_cmodels[i].maxs = new Vector3(ind.maxs[0]+1, ind.maxs[1]+1, ind.maxs[2]+1);
                map_cmodels[i].origin = new Vector3(ind.origin);

                map_cmodels[i].headnode = ind.headnode;
            }
        }

        private void CMod_LoadSurfaces(byte[] buf, in lump_t l)
        {
            if ((l.filelen % texinfo_t.size) != 0)
            {
                Com_Error(QShared.ERR_DROP, "Mod_LoadSurfaces: funny lump size");
            }

            int count = l.filelen / texinfo_t.size;

            if (count < 1)
            {
                Com_Error(QShared.ERR_DROP, "Map with no surfaces");
            }

            if (count > MAX_MAP_TEXINFO)
            {
                Com_Error(QShared.ERR_DROP, "Map has too many surfaces");
            }

            numtexinfo = count;

            for (int i = 0; i < count; i++)
            {
                var ind = new texinfo_t(buf, l.fileofs + i * texinfo_t.size);
                map_surfaces[i].c.name = ind.texture;
                map_surfaces[i].rname = ind.texture;
                map_surfaces[i].c.flags = ind.flags;
                map_surfaces[i].c.value = ind.value;
            }
        }

        private void CMod_LoadNodes(byte[] buf, in lump_t l)
        {
            if ((l.filelen % dnode_t.size) != 0)
            {
                Com_Error(QShared.ERR_DROP, "CMod_LoadNodes: funny lump size");
            }

            int count = l.filelen / dnode_t.size;

            if (count < 1)
            {
                Com_Error(QShared.ERR_DROP, "Map has no nodes");
            }

            if (count > MAX_MAP_NODES)
            {
                Com_Error(QShared.ERR_DROP, "Map has too many nodes");
            }

            numnodes = count;

            for (int i = 0; i < count; i++)
            {
                var ind = new dnode_t(buf, l.fileofs + i * dnode_t.size);
                map_nodes[i].plane = map_planes[ind.planenum];
                for (int j = 0; j < 2; j++)
                {
                    map_nodes[i].children[j] = ind.children[j];
                }
            }
        }

        private void CMod_LoadBrushes(byte[] buf, in lump_t l)
        {
            if ((l.filelen % dbrush_t.size) != 0)
            {
                Com_Error(QShared.ERR_DROP, "CMod_LoadBrushes: funny lump size");
            }

            int count = l.filelen / dbrush_t.size;

            if (count > MAX_MAP_BRUSHES)
            {
                Com_Error(QShared.ERR_DROP, "Map has too many brushes");
            }

            numbrushes = count;

            for (int i = 0; i < count; i++)
            {
                var ind = new dbrush_t(buf, l.fileofs + i * dbrush_t.size);
                map_brushes[i].firstbrushside = ind.firstside;
                map_brushes[i].numsides = ind.numsides;
                map_brushes[i].contents = ind.contents;
            }
        }

        private void CMod_LoadLeafs(byte[] buf, in lump_t l)
        {
            if ((l.filelen % dleaf_t.size) != 0)
            {
                Com_Error(QShared.ERR_DROP, "CMod_LoadLeafs: funny lump size");
            }

            int count = l.filelen / dleaf_t.size;

            if (count < 1)
            {
                Com_Error(QShared.ERR_DROP, "Map with no leafs");
            }

            if (count > MAX_MAP_LEAFS)
            {
                Com_Error(QShared.ERR_DROP, "Map has too many leafs");
            }

            numleafs = count;
            numclusters = 0;

            for (int i = 0; i < count; i++)
            {
                var ind = new dleaf_t(buf, l.fileofs + i * dleaf_t.size);
                map_leafs[i].contents = ind.contents;
                map_leafs[i].cluster = ind.cluster;
                map_leafs[i].area = ind.area;
                map_leafs[i].firstleafbrush = ind.firstleafbrush;
                map_leafs[i].numleafbrushes = ind.numleafbrushes;

                if (map_leafs[i].cluster >= numclusters)
                {
                    numclusters = map_leafs[i].cluster + 1;
                }
            }

            if (map_leafs[0].contents != CONTENTS_SOLID)
            {
                Com_Error(QShared.ERR_DROP, "Map leaf 0 is not CONTENTS_SOLID");
            }

            solidleaf = 0;
            emptyleaf = -1;

            for (int i = 1; i < numleafs; i++)
            {
                if (map_leafs[i].contents == 0)
                {
                    emptyleaf = i;
                    break;
                }
            }

            if (emptyleaf == -1)
            {
                Com_Error(QShared.ERR_DROP, "Map does not have an empty leaf");
            }
        }

        private void CMod_LoadPlanes(byte[] buf, in lump_t l)
        {
            if ((l.filelen % dplane_t.size) != 0)
            {
                Com_Error(QShared.ERR_DROP, "CMod_LoadPlanes: funny lump size");
            }

            int count = l.filelen / dplane_t.size;

            if (count < 1)
            {
                Com_Error(QShared.ERR_DROP, "Map with no planes");
            }

            if (count > MAX_MAP_PLANES)
            {
                Com_Error(QShared.ERR_DROP, "Map has too many planes");
            }

            numplanes = count;

            for (int i = 0; i < count; i++)
            {
                var ind = new dplane_t(buf, l.fileofs + i * dplane_t.size);
                map_planes[i].normal = new Vector3( ind.normal[0], ind.normal[1], ind.normal[1]);

		        byte bits = 0;
                for (int j = 0; j < 3; j++)
                {
                    if (ind.normal[j] < 0)
                    {
                        bits |= (byte)(1 << j);
                    }
                }

                map_planes[i].dist = ind.dist;
                map_planes[i].type = (byte)ind.type;
                map_planes[i].signbits = bits;
            }
        }

        private void CMod_LoadLeafBrushes(byte[] buf, in lump_t l)
        {
            if ((l.filelen % 2) != 0)
            {
                Com_Error(QShared.ERR_DROP, "CMod_LoadLeafBrushes: funny lump size");
            }

            int count = l.filelen / 2;

            if (count < 1)
            {
                Com_Error(QShared.ERR_DROP, "Map with no leafbrushes");
            }

            if (count > MAX_MAP_LEAFBRUSHES)
            {
                Com_Error(QShared.ERR_DROP, "Map has too many leafbrushes");
            }

            numleafbrushes = count;

            for (int i = 0; i < count; i++)
            {
                map_leafbrushes[i] = BitConverter.ToUInt16(buf, l.fileofs + i * 2);
            }
        }

        private void CMod_LoadBrushSides(byte[] buf, in lump_t l)
        {
            if ((l.filelen % dbrushside_t.size) != 0)
            {
                Com_Error(QShared.ERR_DROP, "CMod_LoadBrushSides: funny lump size");
            }

            int count = l.filelen / dbrushside_t.size;

            if (count > MAX_MAP_BRUSHSIDES)
            {
                Com_Error(QShared.ERR_DROP, "Map has too many brush sides");
            }

            numbrushsides = count;

            for (int i = 0; i < count; i++)
            {
                var ind = new dbrushside_t(buf, l.fileofs + i * dbrushside_t.size);
                map_brushsides[i].plane = map_planes[ind.planenum];
                if (ind.texinfo >= numtexinfo)
                {
                    Com_Error(QShared.ERR_DROP, "Bad brushside texinfo");
                }

                map_brushsides[i].surface = (ind.texinfo >= 0) ? map_surfaces[ind.texinfo] : nullsurface;
            }
        }

        private void CMod_LoadAreas(byte[] buf, in lump_t l)
        {
            if ((l.filelen % darea_t.size) != 0)
            {
                Com_Error(QShared.ERR_DROP, "CMod_LoadAreas: funny lump size");
            }

            int count = l.filelen / darea_t.size;

            if (count > MAX_MAP_AREAS)
            {
                Com_Error(QShared.ERR_DROP, "Map has too many areas");
            }

            numareas = count;

            for (int i = 0; i < count; i++)
            {
                var ind = new darea_t(buf, l.fileofs + i * darea_t.size);
                map_areas[i].numareaportals = ind.numareaportals;
                map_areas[i].firstareaportal = ind.firstareaportal;
                map_areas[i].floodvalid = 0;
                map_areas[i].floodnum = 0;
            }
        }

        private void CMod_LoadAreaPortals(byte[] buf, in lump_t l)
        {
            if ((l.filelen % dareaportal_t.size) != 0)
            {
                Com_Error(QShared.ERR_DROP, "CMod_LoadAreaPortals: funny lump size");
            }

            int count = l.filelen / dareaportal_t.size;

            if (count > MAX_MAP_AREAS)
            {
                Com_Error(QShared.ERR_DROP, "Map has too many areas");
            }

            numareaportals = count;

            for (int i = 0; i < count; i++)
            {
                map_areaportals[i] = new dareaportal_t(buf, l.fileofs + i * dareaportal_t.size);
            }
        }

        private void CMod_LoadVisibility(byte[] buf, in lump_t l)
        {
            numvisibility = l.filelen;

            if (l.filelen > MAX_MAP_VISIBILITY)
            {
                Com_Error(QShared.ERR_DROP, "Map has too large visibility lump");
            }

            map_visibility = new byte[l.filelen];
            Array.Copy(buf, l.fileofs, map_visibility, 0, l.filelen);
            map_vis = new dvis_t(buf, l.fileofs);
        }

        private void CMod_LoadEntityString(byte[] buf, in lump_t l, string name)
        {
            // if (sv_entfile->value)
            // {
            //     char s[MAX_QPATH];
            //     char *buffer = NULL;
            //     int nameLen, bufLen;

            //     nameLen = strlen(name);
            //     strcpy(s, name);
            //     s[nameLen-3] = 'e';	s[nameLen-2] = 'n';	s[nameLen-1] = 't';
            //     bufLen = FS_LoadFile(s, (void **)&buffer);

            //     if (buffer != NULL && bufLen > 1)
            //     {
            //         if (bufLen + 1 > sizeof(map_entitystring))
            //         {
            //             Com_Printf("CMod_LoadEntityString: .ent file %s too large: %i > %lu.\n", s, bufLen, (unsigned long)sizeof(map_entitystring));
            //             FS_FreeFile(buffer);
            //         }
            //         else
            //         {
            //             Com_Printf ("CMod_LoadEntityString: .ent file %s loaded.\n", s);
            //             numentitychars = bufLen;
            //             memcpy(map_entitystring, buffer, bufLen);
            //             map_entitystring[bufLen] = 0; /* jit entity bug - null terminate the entity string! */
            //             FS_FreeFile(buffer);
            //             return;
            //         }
            //     }
            //     else if (bufLen != -1)
            //     {
            //         /* If the .ent file is too small, don't load. */
            //         Com_Printf("CMod_LoadEntityString: .ent file %s too small.\n", s);
            //         FS_FreeFile(bufer);
            //     }
            // }

            // numentitychars = l->filelen;

            // if (l->filelen + 1 > sizeof(map_entitystring))
            // {
            //     Com_Error(ERR_DROP, "Map has too large entity lump");
            // }

            map_entitystring = ReadString(buf, l.fileofs, l.filelen);
        }

        /*
        * Loads in the map and all submodels
        */
        public QShared.cmodel_t? CM_LoadMap(string name, bool clientload, out uint checksum)
        {
            // unsigned *buf;
            // int i;
            // dheader_t header;
            // int length;
            // static unsigned last_checksum;

            map_noareas = Cvar_Get("map_noareas", "0", 0);

            if (map_name.Equals(name) && (clientload || !Cvar_VariableBool("flushmap")))
            {
                checksum = last_checksum;

                if (!clientload)
                {
                    Array.Fill(portalopen, false);
                    FloodAreaConnections();
                }

                return map_cmodels[0]; /* still have the right version */
            }

            /* free old stuff */
            numplanes = 0;
            numnodes = 0;
            numleafs = 0;
            numcmodels = 0;
            numvisibility = 0;
            // numentitychars = 0;
            map_entitystring = "";
            map_name = "";

            if (String.IsNullOrEmpty(name))
            {
                numleafs = 1;
                numclusters = 1;
                numareas = 1;
                checksum = 0;
                return map_cmodels[0]; /* cinematic servers won't have anything at all */
            }

            var buf = FS_LoadFile(name);
            if (buf == null)
            {
                Com_Error(QShared.ERR_DROP, $"Couldn't load {name}");
                checksum = 0;
                return map_cmodels[0];
            }

            // last_checksum = LittleLong(Com_BlockChecksum(buf, length));
            checksum = last_checksum;

            var header = new dheader_t(buf!, 0);

            if (header.version != BSPVERSION)
            {
                Com_Error(QShared.ERR_DROP,
                        $"CMod_LoadBrushModel: {name} has wrong version number ({header.version} should be {BSPVERSION})");
            }

            /* load into heap */
            CMod_LoadSurfaces(buf, header.lumps[LUMP_TEXINFO]);
            CMod_LoadLeafs(buf, header.lumps[LUMP_LEAFS]);
            CMod_LoadLeafBrushes(buf, header.lumps[LUMP_LEAFBRUSHES]);
            CMod_LoadPlanes(buf, header.lumps[LUMP_PLANES]);
            CMod_LoadBrushes(buf, header.lumps[LUMP_BRUSHES]);
            CMod_LoadBrushSides(buf, header.lumps[LUMP_BRUSHSIDES]);
            CMod_LoadSubmodels(buf, header.lumps[LUMP_MODELS]);
            CMod_LoadNodes(buf, header.lumps[LUMP_NODES]);
            CMod_LoadAreas(buf, header.lumps[LUMP_AREAS]);
            CMod_LoadAreaPortals(buf, header.lumps[LUMP_AREAPORTALS]);
            CMod_LoadVisibility(buf, header.lumps[LUMP_VISIBILITY]);
            /* From kmquake2: adding an extra parameter for .ent support. */
            CMod_LoadEntityString(buf, header.lumps[LUMP_ENTITIES], name);

            // FS_FreeFile(buf);

            CM_InitBoxHull();

            Array.Fill(portalopen, false);
            FloodAreaConnections();

            map_name = name;

            return map_cmodels[0];
        }

        public QShared.cmodel_t CM_InlineModel(string name)        {
            if (String.IsNullOrEmpty(name) || (name[0] != '*'))
            {
                Com_Error(QShared.ERR_DROP, "CM_InlineModel: bad name");
            }

            var num = Int32.Parse(name.Substring(1));

            if ((num < 1) || (num >= numcmodels))
            {
                Com_Error(QShared.ERR_DROP, "CM_InlineModel: bad number");
            }

            return map_cmodels[num];
        }

        public int CM_NumClusters()
        {
            return numclusters;
        }

        public int CM_NumInlineModels()
        {
            return numcmodels;
        }

        public string CM_EntityString()
        {
            return map_entitystring;
        }

        public int CM_LeafContents(int leafnum)
        {
            if ((leafnum < 0) || (leafnum >= numleafs))
            {
                Com_Error(QShared.ERR_DROP, "CM_LeafContents: bad number");
            }

            return map_leafs[leafnum].contents;
        }

        public int CM_LeafCluster(int leafnum)
        {
            if ((leafnum < 0) || (leafnum >= numleafs))
            {
                Com_Error(QShared.ERR_DROP, $"CM_LeafCluster: bad number {leafnum} {numleafs}");
            }

            return map_leafs[leafnum].cluster;
        }

        public int CM_LeafArea(int leafnum)
        {
            if ((leafnum < 0) || (leafnum >= numleafs))
            {
                Com_Error(QShared.ERR_DROP, "CM_LeafArea: bad number");
            }

            return map_leafs[leafnum].area;
        }

        private void CM_DecompressVis(in byte[] ind, int in_i, ref byte[] outd)
        {
            // int c;
            // byte *out_p;
            // int row;

            var row = (numclusters + 7) >> 3;
            var out_i = 0;

            if (ind == null || in_i < 0 || in_i >= ind.Length || map_visibility == null)
            {
                /* no vis info, so make all visible */
                Array.Fill(outd, (byte)0xFF, in_i, row);
                return;
            }

            do
            {
                if (ind[in_i] != 0)
                {
                    outd[out_i++] = ind[in_i++];
                    continue;
                }

                var c = ind[in_i+1];
                in_i += 2;

                if (out_i + c > row)
                {
                    Com_DPrintf($"warning: Vis decompression overrun {out_i} {c} > {row}\n");
                    c = (byte)(row - out_i);
                }

                Array.Fill(outd, (byte)0, out_i, c);
                out_i += c;
            } while (out_i < row);
        }

        public byte[] CM_ClusterPVS(int cluster)
        {
            byte[] pvsrow = new byte[(numclusters + 7) >> 3];
            if (cluster == -1)
            {
                Array.Fill(pvsrow, (byte)0);
            }
            else
            {
                CM_DecompressVis(map_visibility, map_vis.bitofs[cluster][DVIS_PVS], ref pvsrow);
            }

            return pvsrow;
        }

        public byte[] CM_ClusterPHS(int cluster)
        {
            byte[] phsrow = new byte[(numclusters + 7) >> 3];
            if (cluster == -1)
            {
                Array.Fill(phsrow, (byte)0);
            }
            else
            {
                CM_DecompressVis(map_visibility, map_vis.bitofs[cluster][DVIS_PHS], ref phsrow);
            }

            return phsrow;
        }
    }
}