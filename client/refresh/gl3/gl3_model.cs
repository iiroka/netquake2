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
 * Model loading and caching for OpenGL3. Includes the .bsp file format
 *
 * =======================================================================
 */
using Silk.NET.OpenGL;
using Silk.NET.Maths;
using System.Numerics;

namespace Quake2 {

    partial class QRefGl3
    {
        private int registration_sequence = 0;

        private const int MAX_LBM_HEIGHT = 480;

        private enum modtype_t
        {
            mod_bad,
            mod_brush,
            mod_sprite,
            mod_alias
        }


        /* Whole model */

        // this, must be struct model_s, not gl3model_s,
        // because struct model_s* is returned by re.RegisterModel()
        private class gl3model_t : model_s
        {
            public string name;

            public int registration_sequence;

            public modtype_t type;
            public int numframes;

            public int flags;

            /* volume occupied by the model graphics */
            public Vector3 mins, maxs;
            public float radius;

            /* solid volume for clipping */
            public bool clipbox;
            public Vector3 clipmins, clipmaxs;

            protected gl3model_t(string name, modtype_t type)
            {
                this.name = name;
                this.type = type;
            }
        }

        private List<gl3model_t> mod_known = new List<gl3model_t>();

        private mleaf_t GL3_Mod_PointInLeaf(GL gl, in Vector3 p, gl3brushmodel_t? model)
        {
            // mnode_t *node;
            // float d;
            // cplane_t *plane;

            if (model == null || model.nodes.Length == 0)
            {
                ri.Sys_Error(QShared.ERR_DROP, "GL3_Mod_PointInLeaf: bad model");
            }

            mnode_or_leaf_t anode = model!.nodes[0];

            while (true)
            {
                if (anode.contents != -1)
                {
                    return (mleaf_t)anode;
                }

                var node = (mnode_t)anode;
                var plane = node.plane!;
                var d = Vector3.Dot(p, plane.normal) - plane.dist;

                if (d > 0)
                {
                    anode = node.children[0]!;
                }
                else
                {
                    anode = node.children[1]!;
                }
            }
        }

        /*
        * Loads in a model for the given name
        */
        private gl3model_t? Mod_ForName (GL gl, string name, gl3brushmodel_t? parent_model, bool crash)
        {
            if (String.IsNullOrEmpty(name))
            {
                ri.Sys_Error(QShared.ERR_DROP, "Mod_ForName: NULL name");
            }

            /* inline models are grabbed only from worldmodel */
            if (name[0] == '*' && parent_model != null)
            {
                var i = Int16.Parse(name.Substring(1));

                if (i < 1 || i >= parent_model.submodels.Length)
                {
                    ri.Sys_Error(QShared.ERR_DROP, $"Mod_ForName: bad inline model number {i} in {name}");
                }

                return parent_model.submodels[i];
            }

            /* search the currently loaded models */
            for (int i = 0; i < mod_known.Count; i++)
            {
                if (String.IsNullOrEmpty(mod_known[i].name))
                {
                    continue;
                }

                if (mod_known[i].name.Equals(name))
                {
                    return mod_known[i];
                }
            }

            /* load the file */
            var buf = ri.FS_LoadFile(name);

            if (buf == null)
            {
                if (crash)
                {
                    ri.Sys_Error(QShared.ERR_DROP, $"Mod_ForName: {name} not found");
                }

                return null;
            }

            /* call the apropriate loader */
            var id = BitConverter.ToInt32(buf);
            gl3model_t? mod = null;
            switch (id)
            {
                case QCommon.IDALIASHEADER:
                    mod = gl3aliasmodel_t.Load(this, gl, buf, name);
                    break;

                case QCommon.IDSPRITEHEADER:
            //         GL3_LoadSP2(mod, buf, modfilelen);
                    break;

                case QCommon.IDBSPHEADER:
                    mod = gl3brushmodel_t.Load(this, gl, buf, name);
                    break;

                default:
                    ri.Sys_Error(QShared.ERR_DROP, $"Mod_ForName: unknown fileid {id.ToString("X")} for {name}");
                    break;
            }

            if (mod != null)
            {
                mod_known.Add(mod);
            }

            return mod;
        }

        public void BeginRegistration (Silk.NET.Windowing.IWindow window, string model)
        {
            var gl = GL.GetApi(window);

            // char fullname[MAX_QPATH];
            // cvar_t *flushmap;

            registration_sequence++;
            gl3_oldviewcluster = -1; /* force markleafs */

            gl3state.currentlightmap = -1;

            var fullname = $"maps/{model}.bsp";

            /* explicitly free the old map if different
            this guarantees that mod_known[0] is the
            world map */
            var flushmap = ri.Cvar_Get("flushmap", "0", 0);

            if (mod_known.Count > 0 && (!mod_known[0].name.Equals(fullname) || (flushmap?.Bool ?? false)))
            {
                mod_known.Clear();
            }

            gl3_worldmodel = (gl3brushmodel_t?)Mod_ForName(gl, fullname, null, true);

            gl3_viewcluster = -1;            
        }

        public model_s? RegisterModel (Silk.NET.Windowing.IWindow window, string name)
        {
            var gl = GL.GetApi(window);
            // gl3model_t *mod;
            // int i;
            // dsprite_t *sprout;
            // dmdl_t *pheader;

            var mod = Mod_ForName(gl, name, gl3_worldmodel, false);

            if (mod != null)
            {
                mod.registration_sequence = registration_sequence;

                /* register any images used by the models */
                if (mod.type == modtype_t.mod_sprite)
                {
                //     sprout = (dsprite_t *)mod->extradata;

                //     for (i = 0; i < sprout->numframes; i++)
                //     {
                //         mod->skins[i] = GL3_FindImage(sprout->frames[i].name, it_sprite);
                //     }
                }
                else if (mod.type == modtype_t.mod_alias)
                {
                    gl3aliasmodel_t amod = (gl3aliasmodel_t)mod;
                    for (int i = 0; i < amod.skins.Length; i++)
                    {
                        amod.skins[i] = GL3_FindImage(gl, amod.skinnames[i], imagetype_t.it_skin);
                    }
                    amod.numframes = amod.header.num_frames;
                }
                else if (mod.type == modtype_t.mod_brush)
                {
                    gl3brushmodel_t bmod = (gl3brushmodel_t)mod;
                    for (int i = 0; i < bmod.texinfo.Length; i++)
                    {
                        bmod.texinfo[i].image!.registration_sequence = registration_sequence;
                    }
                }
            }

            return mod;
        }


        private const int SIDE_FRONT = 0;
        private const int SIDE_BACK = 1;
        private const int SIDE_ON = 2;

        private const int SURF_PLANEBACK = 2;
        private const int SURF_DRAWSKY = 4;
        private const int SURF_DRAWTURB = 0x10;
        private const int SURF_DRAWBACKGROUND = 0x40;
        private const int SURF_UNDERWATER = 0x80;

        // used for vertex array elements when drawing brushes, sprites, sky and more
        // (ok, it has the layout used for rendering brushes, but is not used there)
        private struct gl3_3D_vtx_t {
            public Vector3D<float> pos;
            public Vector2D<float> texCoord;
            public Vector2D<float> lmTexCoord; // lightmap texture coordinate (sometimes unused)
            public Vector3D<float> normal;
            public uint lightFlags; // bit i set means: dynlight i affects surface
        }

        // used for vertex array elements when drawing models
        private struct gl3_alias_vtx_t {
            public Vector3D<float> pos;
            public Vector2D<float> texCoord;
            public Vector4D<float> color;
        }

        /* in memory representation */
        private class mvertex_t
        {
            public Vector3 position;
        }

        private class mmodel_t
        {
            public Vector3 mins, maxs;
            public Vector3 origin; /* for sounds or lights */
            public float radius;
            public int headnode;
            public int visleafs; /* not including the solid leaf 0 */
            public int firstface, numfaces;
        }

        private class medge_t
        {
            public ushort[] v = new ushort[2];
            public uint cachededgeoffset;
        }

        private class mtexinfo_t
        {
            public Vector4[] vecs = new Vector4[2];
            public int flags;
            public int numframes;
            public mtexinfo_t? next; /* animation chain */
            public gl3image_t? image;
        }

        private class glpoly_t
        {
            public glpoly_t? next;
            public glpoly_t? chain;
            public int flags; /* for SURF_UNDERWATER (not needed anymore?) */
            public gl3_3D_vtx_t[] vertices = new gl3_3D_vtx_t[0]; /* variable sized */
        }

        private class msurface_t
        {
            public int visframe; /* should be drawn when node is crossed */

            public QShared.cplane_t? plane;
            public int flags;

            public int firstedge;          /* look up in model->surfedges[], negative numbers */
            public int numedges;           /* are backwards edges */

            public short[] texturemins = new short[2];
            public short[] extents = new short[2];

            public int light_s, light_t;           /* gl lightmap coordinates */
            public int dlight_s, dlight_t;         /* gl lightmap coordinates for dynamic lightmaps */

            public glpoly_t? polys;                /* multiple if warped */
            public msurface_t? texturechain;
            // struct  msurface_s *lightmapchain; not used/needed anymore

            public mtexinfo_t? texinfo;

            /* lighting info */
            public int dlightframe;
            public int dlightbits;

            public int lightmaptexturenum;
            public byte[] styles = new byte[QCommon.MAXLIGHTMAPS]; // MAXLIGHTMAPS = MAX_LIGHTMAPS_PER_SURFACE (defined in local.h)
            // I think cached_light is not used/needed anymore
            //float cached_light[MAXLIGHTMAPS];       /* values currently used in lightmap */
            public byte[]? samples_b = null;                          /* [numstyles*surfsize] */
            public int samples_i;
        }

        public class mnode_or_leaf_t
        {
            /* common with leaf */
            public int contents;               /* -1, to differentiate from leafs */
            public int visframe;               /* node needs to be traversed if current */

            public Vector3 mins, maxs;           /* for bounding box culling */

            public mnode_t? parent;
        }

        public class mnode_t : mnode_or_leaf_t
        {
            public QShared.cplane_t? plane;
            public mnode_or_leaf_t?[] children = new mnode_or_leaf_t?[2];

            public ushort firstsurface;
            public ushort numsurfaces;
        }

        public class mleaf_t : mnode_or_leaf_t
        {
            public int cluster;
            public int area;

            public int firstmarksurface_i;
            // msurface_t **firstmarksurface;
            public int nummarksurfaces;
        }

        private class gl3brushmodel_t : gl3model_t, ICloneable
        {
	        public int firstmodelsurface, nummodelsurfaces;
	        public int lightmap; /* only for submodels */

            public QShared.cplane_t[] planes;
            public int numleafs;
            public mleaf_t[] leafs;
            public mvertex_t[] vertexes;
            public medge_t[] edges;
            public int firstnode;
            public mnode_t[] nodes;
            public mtexinfo_t[] texinfo;
            public msurface_t[] surfaces;
            public gl3brushmodel_t[] submodels;
            public int[] surfedges;
            public short[] marksurfaces;
            public byte[]? lightdata;

            public Vector3		origin;	// for sounds or lights

            private gl3brushmodel_t(string name) : base(name, modtype_t.mod_brush)
            {
                planes = new QShared.cplane_t[0];
                leafs = new mleaf_t[0];
                vertexes = new mvertex_t[0];
                edges = new medge_t[0];
                nodes = new mnode_t[0];
                texinfo = new mtexinfo_t[0];
                surfaces = new msurface_t[0];
                submodels = new gl3brushmodel_t[0];
                surfedges = new int[0];
                marksurfaces = new short[0];
                lightdata = null;
            }

            public object Clone()
            {
                var model = (gl3brushmodel_t)MemberwiseClone();
                return model;
            }


            public static gl3brushmodel_t Load(QRefGl3 qref, GL gl, byte[] buffer, string name)
            {
                if (qref.mod_known.Count > 0)
                {
                    qref.ri.Sys_Error(QShared.ERR_DROP, "Loaded a brush model after the world");
                }
                var mod = new gl3brushmodel_t(name);

                var header = new QCommon.dheader_t(buffer, 0);

                if (header.version != QCommon.BSPVERSION)
                {
                    qref.ri.Sys_Error(QShared.ERR_DROP, $"gl3brushmodel_t: {name} has wrong version number ({header.version} should be {QCommon.BSPVERSION})");
                }

                /* load into heap */
                mod.LoadVertexes(qref, buffer, header.lumps[QCommon.LUMP_VERTEXES]);
                mod.LoadEdges(qref, buffer, header.lumps[QCommon.LUMP_EDGES]);
                mod.LoadSurfedges(qref, buffer, header.lumps[QCommon.LUMP_SURFEDGES]);
                mod.LoadLighting(buffer, header.lumps[QCommon.LUMP_LIGHTING]);
                mod.LoadPlanes(qref, buffer, header.lumps[QCommon.LUMP_PLANES]);
                mod.LoadTexinfo(qref, gl, buffer, header.lumps[QCommon.LUMP_TEXINFO]);
                mod.LoadFaces(qref, gl, buffer, header.lumps[QCommon.LUMP_FACES]);
                mod.LoadMarksurfaces(qref, buffer, header.lumps[QCommon.LUMP_LEAFFACES]);
                // Mod_LoadVisibility(qref, buffer, header.lumps[QCommon.LUMP_VISIBILITY]);
                mod.LoadLeafs(qref, buffer, header.lumps[QCommon.LUMP_LEAFS]);
                mod.LoadNodes(qref, buffer, header.lumps[QCommon.LUMP_NODES]);
                mod.LoadSubmodels(qref, buffer, header.lumps[QCommon.LUMP_MODELS]);
                mod.numframes = 2; /* regular and alternate animation */

                return mod;
            }

            private void LoadLighting(byte[] mod_base, in QCommon.lump_t l)
            {
                if (l.filelen == 0)
                {
                    lightdata = null;
                    return;
                }

                lightdata = new byte[l.filelen];
                Array.Copy(mod_base, l.fileofs, lightdata, 0, l.filelen);
            }

            private void LoadVertexes(QRefGl3 qref, byte[] mod_base, in QCommon.lump_t l)
            {
                if ((l.filelen % QCommon.dvertex_t.size) != 0)
                {
                    qref.ri.Sys_Error(QShared.ERR_DROP, $"LoadVertexes: funny lump size in {name}");
                }

                var count = l.filelen / QCommon.dvertex_t.size;

                vertexes = new mvertex_t[count];

                for (int i = 0; i < count; i++)
                {
                    var ind = new QCommon.dvertex_t(mod_base, l.fileofs + i * QCommon.dvertex_t.size);
                    vertexes[i] = new mvertex_t();
                    vertexes[i].position = new Vector3(ind.point[0], ind.point[1], ind.point[2]);
                }
            }

            private void LoadSubmodels(QRefGl3 qref, byte[] mod_base, in QCommon.lump_t l)
            {
                if ((l.filelen % QCommon.dmodel_t.size) != 0)
                {
                    qref.ri.Sys_Error(QShared.ERR_DROP, $"LoadSubmodels: funny lump size in {name}");
                }

                var count = l.filelen / QCommon.dmodel_t.size;

                submodels = new gl3brushmodel_t[count];

                for (int i = 0; i < count; i++)
                {
                    var ind = new QCommon.dmodel_t(mod_base, l.fileofs + i * QCommon.dmodel_t.size);
                    if (i == 0)
                    {
                        // copy parent as template for first model
                        submodels[i] = (gl3brushmodel_t)this.Clone();
                    }
                    else
                    {
                        // copy first as template for model
                        submodels[i] = (gl3brushmodel_t)submodels[0].Clone();
                    }

                    submodels[i].name = $"*{i}";

                    /* spread the mins / maxs by a pixel */
                    submodels[i].mins = new Vector3(ind.mins[0] - 1, ind.mins[1] - 1, ind.mins[2] - 1);
                    submodels[i].maxs = new Vector3(ind.maxs[0] + 1, ind.maxs[1] + 1, ind.maxs[2] + 1);
                    submodels[i].origin = new Vector3(ind.origin);

                    submodels[i].radius = QPVS.Mod_RadiusFromBounds(submodels[i].mins, submodels[i].maxs);
                    submodels[i].firstnode = ind.headnode;
                    submodels[i].firstmodelsurface = ind.firstface;
                    submodels[i].nummodelsurfaces = ind.numfaces;
                    // visleafs
                    submodels[i].numleafs = 0;
                    //  check limits
                    if (submodels[i].firstnode >= nodes.Length)
                    {
                        qref.ri.Sys_Error(QShared.ERR_DROP, $"LoadSubmodels: Inline model {i} has bad firstnode");
                    }
                }
            }

            private void LoadEdges(QRefGl3 qref, byte[] mod_base, in QCommon.lump_t l)
            {
                if ((l.filelen % QCommon.dedge_t.size) != 0)
                {
                    qref.ri.Sys_Error(QShared.ERR_DROP, $"LoadEdges: funny lump size in {name}");
                }

                var count = l.filelen / QCommon.dedge_t.size;

                edges = new medge_t[count];

                for (int i = 0; i < count; i++)
                {
                    var ind = new QCommon.dedge_t(mod_base, l.fileofs + i * QCommon.dedge_t.size);
                    edges[i] = new medge_t();
                    edges[i].v = new ushort[2]{ ind.v[0], ind.v[1] };
                }
            }

            private void LoadTexinfo(QRefGl3 qref, GL gl, byte[] mod_base, in QCommon.lump_t l)
            {
                if ((l.filelen % QCommon.texinfo_t.size) != 0)
                {
                    qref.ri.Sys_Error(QShared.ERR_DROP, $"LoadTexinfo: funny lump size in {name}");
                }

                var count = l.filelen / QCommon.texinfo_t.size;

                texinfo = new mtexinfo_t[count];
                for (int i = 0; i < count; i++)
                {
                    texinfo[i] = new mtexinfo_t();
                }

                for (int i = 0; i < count; i++)
                {
                    var ind = new QCommon.texinfo_t(mod_base, l.fileofs + i * QCommon.texinfo_t.size);

                    texinfo[i].vecs[0] = new Vector4(ind.vecs[0]);
                    texinfo[i].vecs[1] = new Vector4(ind.vecs[1]);

                    texinfo[i].flags = ind.flags;
                    var next = ind.nexttexinfo;

                    if (next > 0)
                    {
                        texinfo[i].next = texinfo[next];
                    }
                    else
                    {
                        texinfo[i].next = null;
                    }

                    var name = $"textures/{ind.texture}.wal";

                    texinfo[i].image = qref.GL3_FindImage(gl, name, imagetype_t.it_wall);

                    if (texinfo[i].image == null || texinfo[i].image == qref.gl3_notexture)
                    {
                        name = $"textures/{ind.texture}.m8";
                        texinfo[i].image = qref.GL3_FindImage(gl, name, imagetype_t.it_wall);
                    }

                    if (texinfo[i].image == null)
                    {
                        qref.R_Printf(QShared.PRINT_ALL, $"Couldn't load {name}\n");
                        texinfo[i].image = qref.gl3_notexture;
                    }
                }

                /* count animation frames */
                for (int i = 0; i < count; i++)
                {
                    ref var outd = ref texinfo[i];
                    outd.numframes = 1;

                    for (var step = outd.next; step != null && step != outd; step = step.next)
                    {
                        outd.numframes++;
                    }
                }
            }

            /*
            * Fills in s->texturemins[] and s->extents[]
            */
            private void Mod_CalcSurfaceExtents(ref msurface_t s)
            {
                // float mins[2], maxs[2], val;
                // int i, j, e;
                // mvertex_t *v;
                // mtexinfo_t *tex;
                // int bmins[2], bmaxs[2];

                float[] mins = new float[2]{999999, 999999};
                float[] maxs = new float[2]{-99999, -99999};

                var tex = s.texinfo;

                for (int i = 0; i < s.numedges; i++)
                {
                    var e = surfedges[s.firstedge + i];

                    mvertex_t v;
                    if (e >= 0)
                    {
                        v = vertexes[edges[e].v[0]];
                    }
                    else
                    {
                        v = vertexes[edges[-e].v[1]];
                    }

                    for (int j = 0; j < 2; j++)
                    {
                        var vv = v.position.X * tex!.vecs[j].X +
                            v.position.Y * tex.vecs[j].Y +
                            v.position.Z * tex.vecs[j].Z +
                            tex.vecs[j].W;

                        if (vv < mins[j])
                        {
                            mins[j] = vv;
                        }

                        if (vv > maxs[j])
                        {
                            maxs[j] = vv;
                        }
                    }
                }

                var bmins = new int[2];
                var bmaxs = new int[2];
                for (int i = 0; i < 2; i++)
                {
                    bmins[i] = (int)MathF.Floor(mins[i] / 16);
                    bmaxs[i] = (int)MathF.Ceiling(maxs[i] / 16);

                    s.texturemins[i] = (short)(bmins[i] * 16);
                    s.extents[i] = (short)((bmaxs[i] - bmins[i]) * 16);
                }
            }


            private void LoadFaces(QRefGl3 qref, GL gl, byte[] mod_base, in QCommon.lump_t l)
            {
                if ((l.filelen % QCommon.dface_t.size) != 0)
                {
                    qref.ri.Sys_Error(QShared.ERR_DROP, $"LoadFaces: funny lump size in {name}");
                }

                var count = l.filelen / QCommon.dface_t.size;

                surfaces = new msurface_t[count];

                qref.GL3_LM_BeginBuildingLightmaps(this);

                for (int surfnum = 0; surfnum < count; surfnum++)
                {
                    var ind = new QCommon.dface_t(mod_base, l.fileofs + surfnum * QCommon.dface_t.size);
                    surfaces[surfnum] = new msurface_t();
                    ref var outd = ref surfaces[surfnum];
                    outd.firstedge = ind.firstedge;
                    outd.numedges = ind.numedges;
                    outd.flags = 0;
                    outd.polys = null;

                    var planenum = ind.planenum;
                    var side = ind.side;

                    if (side != 0)
                    {
                        outd.flags |= SURF_PLANEBACK;
                    }

                    outd.plane = planes[planenum];

                    var ti = ind.texinfo;

                    if ((ti < 0) || (ti >= texinfo.Length))
                    {
                        qref.ri.Sys_Error(QShared.ERR_DROP, "LoadFaces: bad texinfo number");
                    }

                    outd.texinfo = texinfo[ti];

                    Mod_CalcSurfaceExtents(ref surfaces[surfnum]);

                    /* lighting info */
                    int i;
                    for (i = 0; i < MAX_LIGHTMAPS_PER_SURFACE; i++)
                    {
                        outd.styles[i] = ind.styles[i];
                    }

                    i = ind.lightofs;

                    if (i == -1)
                    {
                        outd.samples_b = null;
                    }
                    else
                    {
                        outd.samples_b = lightdata;
                        outd.samples_i = i;
                    }

                    /* set the drawing flags */
                    if ((outd.texinfo.flags & QCommon.SURF_WARP) != 0)
                    {
                        outd.flags |= SURF_DRAWTURB;

                        for (i = 0; i < 2; i++)
                        {
                            outd.extents[i] = 16384;
                            outd.texturemins[i] = -8192;
                        }

                        qref.GL3_SubdivideSurface(ref surfaces[surfnum], this); /* cut up polygon for warps */
                    }

                    // if (r_fixsurfsky?.Bool ?? false)
                    // {
                    //     if ((outd.texinfo.flags & QCommon.SURF_SKY) != 0)
                    //     {
                    //         outd.flags |= SURF_DRAWSKY;
                    //     }
                    // }

                    // /* create lightmaps and polygons */
                    if ((outd.texinfo.flags & (QCommon.SURF_SKY | QCommon.SURF_TRANS33 | QCommon.SURF_TRANS66 | QCommon.SURF_WARP)) == 0)
                    {
                        qref.GL3_LM_CreateSurfaceLightmap(gl, ref surfaces[surfnum]);
                    }

                    if ((outd.texinfo.flags & QCommon.SURF_WARP) == 0)
                    {
                        qref.GL3_LM_BuildPolygonFromSurface(this, ref surfaces[surfnum]);
                    }
                }

                qref.GL3_LM_EndBuildingLightmaps(gl);
            }

            private void Mod_SetParent(mnode_or_leaf_t anode, mnode_t? parent)
            {
                anode.parent = parent;

                if (anode.contents != -1)
                {
                    return;
                }

                var node = (mnode_t)anode;
                Mod_SetParent(node.children[0]!, node);
                Mod_SetParent(node.children[1]!, node);
            }

            private void LoadNodes(QRefGl3 qref, byte[] mod_base, in QCommon.lump_t l)
            {
                if ((l.filelen % QCommon.dnode_t.size) != 0)
                {
                    qref.ri.Sys_Error(QShared.ERR_DROP, $"LoadNodes: funny lump size in {name}");
                }

                var count = l.filelen / QCommon.dnode_t.size;

                nodes = new mnode_t[count];
                for (int i = 0; i < count; i++)
                {
                    nodes[i] = new mnode_t();
                }

                for (int i = 0; i < count; i++)
                {
                    var ind = new QCommon.dnode_t(mod_base, l.fileofs + i * QCommon.dnode_t.size);

                    nodes[i].mins = new Vector3(ind.mins[0], ind.mins[1], ind.mins[2]);
                    nodes[i].maxs = new Vector3(ind.maxs[0], ind.maxs[1], ind.maxs[2]);

                    nodes[i].plane = planes[ind.planenum];

                    nodes[i].firstsurface = ind.firstface;
                    nodes[i].numsurfaces = ind.numfaces;
                    nodes[i].contents = -1; /* differentiate from leafs */

                    for (int j = 0; j < 2; j++)
                    {
                        var p = ind.children[j];

                        if (p >= 0)
                        {
                            nodes[i].children[j] = nodes[p];
                        }
                        else
                        {
                            nodes[i].children[j] = leafs[-1 - p];
                        }
                    }
                }

                Mod_SetParent(nodes[0], null); /* sets nodes and leafs */
            }

            private void LoadLeafs(QRefGl3 qref, byte[] mod_base, in QCommon.lump_t l)
            {
                if ((l.filelen % QCommon.dleaf_t.size) != 0)
                {
                    qref.ri.Sys_Error(QShared.ERR_DROP, $"LoadLeafs: funny lump size in {name}");
                }

                var count = l.filelen / QCommon.dleaf_t.size;

                leafs = new mleaf_t[count];
                numleafs = count;

                for (int i = 0; i < count; i++)
                {
                    var ind = new QCommon.dleaf_t(mod_base, l.fileofs + i * QCommon.dleaf_t.size);
                    leafs[i] = new mleaf_t();
                    // unsigned firstleafface;

                    leafs[i].mins = new Vector3(ind.mins[0], ind.mins[1], ind.mins[2]);
                    leafs[i].maxs = new Vector3(ind.maxs[0], ind.maxs[1], ind.maxs[2]);

                    leafs[i].contents = ind.contents;

                    leafs[i].cluster = ind.cluster;
                    leafs[i].area = ind.area;

                    // make unsigned long from signed short
                    // firstleafface = LittleShort(in->firstleafface) & 0xFFFF;
                    leafs[i].nummarksurfaces = ind.numleaffaces & 0xFFFF;

                    leafs[i].firstmarksurface_i = ind.firstleafface & 0xFFFF;;

                    // out->firstmarksurface = loadmodel->marksurfaces + firstleafface;
                    if ((leafs[i].firstmarksurface_i + leafs[i].nummarksurfaces) > marksurfaces.Length)
                    {
                        qref.ri.Sys_Error(QShared.ERR_DROP, $"LoadLeafs: wrong marksurfaces position in {name} {leafs[i].firstmarksurface_i} {leafs[i].nummarksurfaces} {marksurfaces.Length}");
                    }
                }
            }

            private void LoadMarksurfaces(QRefGl3 qref, byte[] mod_base, in QCommon.lump_t l)
            {
                if ((l.filelen % 2) != 0)
                {
                    qref.ri.Sys_Error(QShared.ERR_DROP, $"LoadMarksurfaces: funny lump size in {name}");
                }

                var count = l.filelen / 2;

                marksurfaces = new short[count];

                for (int i = 0; i < count; i++)
                {
                    marksurfaces[i] = BitConverter.ToInt16(mod_base, l.fileofs + i * 2);
                    if ((marksurfaces[i] < 0) || (marksurfaces[i] >= surfaces.Length))
                    {
                        qref.ri.Sys_Error(QShared.ERR_DROP, "LoadMarksurfaces: bad surface number");
                    }
                }
            }


            private void LoadSurfedges(QRefGl3 qref, byte[] mod_base, in QCommon.lump_t l)
            {
                if ((l.filelen % 4) != 0)
                {
                    qref.ri.Sys_Error(QShared.ERR_DROP, $"LoadSurfedges: funny lump size in {name}");
                }

                var count = l.filelen / 4;

                if ((count < 1) || (count >= QCommon.MAX_MAP_SURFEDGES))
                {
                    qref.ri.Sys_Error(QShared.ERR_DROP, $"LoadSurfedges: bad surfedges count in {name}: {count}");
                }

                surfedges = new int[count];

                for (int i = 0; i < count; i++)
                {
                    surfedges[i] = BitConverter.ToInt32(mod_base, l.fileofs + i * 4);
                }
            }

            private void LoadPlanes(QRefGl3 qref, byte[] mod_base, in QCommon.lump_t l)
            {
                if ((l.filelen % QCommon.dplane_t.size) != 0)
                {
                    qref.ri.Sys_Error(QShared.ERR_DROP, $"LoadPlanes: funny lump size in {name}");
                }

                var count = l.filelen / QCommon.dplane_t.size;

                planes = new QShared.cplane_t[count];

                for (int i = 0; i < count; i++)
                {
                    var ind = new QCommon.dplane_t(mod_base, l.fileofs + i * QCommon.dplane_t.size);
                    planes[i] = new QShared.cplane_t();
                    byte bits = 0;
                    planes[i].normal = new Vector3(ind.normal);
                    if (planes[i].normal.X < 0) bits |= 1;
                    if (planes[i].normal.Y < 0) bits |= 2;
                    if (planes[i].normal.Z < 0) bits |= 4;

                    planes[i].dist = ind.dist;
                    planes[i].type = (byte)ind.type;
                    planes[i].signbits = bits;
                }
            }

        }

        private class gl3aliasmodel_t : gl3model_t
        {
            public QCommon.dmdl_t header;
            public int[] glcmds;
            public QCommon.daliasframe_t[] frames;
            public QCommon.dtriangle_t[] tris;
            public string[] skinnames;
            public gl3image_t?[] skins;

            private gl3aliasmodel_t(string name) : base(name, modtype_t.mod_alias)
            {
                glcmds = new int[0];
                frames = new  QCommon.daliasframe_t[0];
                tris = new QCommon.dtriangle_t[0];
                skinnames = new string[0];
                skins = new gl3image_t?[0];
            }

            public static gl3aliasmodel_t Load(QRefGl3 qref, GL gl, byte[] buffer, string name)
            {
                var mod = new gl3aliasmodel_t(name);

                mod.header = new QCommon.dmdl_t(buffer, 0);

                if (mod.header.version != QCommon.ALIAS_VERSION)
                {
                    qref.ri.Sys_Error(QShared.ERR_DROP, $"{name} has wrong version number ({mod.header.version} should be {QCommon.ALIAS_VERSION})");
                }

                if (mod.header.ofs_end < 0 || mod.header.ofs_end > buffer.Length)
                    qref.ri.Sys_Error (QShared.ERR_DROP, $"model {name} file size({buffer.Length}) too small, should be {mod.header.ofs_end}");

                if (mod.header.skinheight > MAX_LBM_HEIGHT)
                {
                    qref.ri.Sys_Error(QShared.ERR_DROP, $"model {name} has a skin taller than {MAX_LBM_HEIGHT}");
                }

                if (mod.header.num_xyz <= 0)
                {
                    qref.ri.Sys_Error(QShared.ERR_DROP, $"model {name} has no vertices");
                }

                if (mod.header.num_xyz > QCommon.MAX_VERTS)
                {
                    qref.ri.Sys_Error(QShared.ERR_DROP, $"model {name} has too many vertices");
                }

                if (mod.header.num_st <= 0)
                {
                    qref.ri.Sys_Error(QShared.ERR_DROP, $"model {name} has no st vertices");
                }

                if (mod.header.num_tris <= 0)
                {
                    qref.ri.Sys_Error(QShared.ERR_DROP, $"model {name} has no triangles");
                }

                if (mod.header.num_frames <= 0)
                {
                    qref.ri.Sys_Error(QShared.ERR_DROP, $"model {name} has no frames");
                }

                // /* load base s and t vertices (not used in gl version) */
                // pinst = (dstvert_t *)((byte *)pinmodel + pheader->ofs_st);
                // poutst = (dstvert_t *)((byte *)pheader + pheader->ofs_st);

                // for (i = 0; i < pheader->num_st; i++)
                // {
                //     poutst[i].s = LittleShort(pinst[i].s);
                //     poutst[i].t = LittleShort(pinst[i].t);
                // }

                /* load triangle lists */
                mod.tris = new QCommon.dtriangle_t[mod.header.num_tris];
                for (int i = 0; i < mod.header.num_tris; i++)
                {
                    mod.tris[i] = new QCommon.dtriangle_t(buffer, mod.header.ofs_tris + i * QCommon.dtriangle_t.size);
                }

                /* load the frames */
                mod.frames = new QCommon.daliasframe_t[mod.header.num_frames];
                for (int i = 0; i < mod.header.num_frames; i++)
                {
                    mod.frames[i] = new QCommon.daliasframe_t(buffer, mod.header.ofs_frames + i * mod.header.framesize, mod.header.framesize);
                }

                /* load the glcmds */
                mod.glcmds = new int[mod.header.num_glcmds];
                for (int i = 0; i < mod.header.num_glcmds; i++)
                {
                    mod.glcmds[i] = BitConverter.ToInt32(buffer, mod.header.ofs_glcmds + i * 4);
                }

                if (mod.glcmds[mod.header.num_glcmds-1] != 0)
                {
                    qref.R_Printf(QShared.PRINT_ALL, $"gl3aliasmodel_t: Entity {name} has possible last element issues with {mod.glcmds[mod.header.num_glcmds-1]} verts.\n");
                }

                /* register all skins */
                mod.skinnames = new string[mod.header.num_skins];
                mod.skins = new gl3image_t?[mod.header.num_skins];

                for (int i = 0; i < mod.header.num_skins; i++)
                {
                    mod.skinnames[i] = QCommon.ReadString(buffer, mod.header.ofs_skins + i * QCommon.MAX_SKINNAME, QCommon.MAX_SKINNAME);
                    mod.skins[i] = qref.GL3_FindImage(gl, mod.skinnames[i], imagetype_t.it_skin);
                }

                mod.mins = new Vector3(-32, -32, -32);
                mod.maxs = new Vector3(32, 32, 32);

                return mod;
            }
        }
    }
}