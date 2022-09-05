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
using System.Text;

namespace Quake2 {

    partial class QCommon {

        private string map_name = "";
        private string map_entitystring = "";
        private QShared.cmodel_t[] map_cmodels = new QShared.cmodel_t[MAX_MAP_MODELS];
        private cvar_t? map_noareas;
        private uint last_checksum = 0;

        private void CM_Init()
        {
            for (int i = 0; i < map_cmodels.Length; i++){
                map_cmodels[i] = new QShared.cmodel_t();
            }
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
            //         FS_FreeFile(buffer);
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

            //     if (!clientload)
            //     {
            //         memset(portalopen, 0, sizeof(portalopen));
            //         FloodAreaConnections();
            //     }

                return map_cmodels[0]; /* still have the right version */
            }

            /* free old stuff */
            // numplanes = 0;
            // numnodes = 0;
            // numleafs = 0;
            // numcmodels = 0;
            // numvisibility = 0;
            // numentitychars = 0;
            map_entitystring = "";
            map_name = "";

            if (String.IsNullOrEmpty(name))
            {
                // numleafs = 1;
                // numclusters = 1;
                // numareas = 1;
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
            // CMod_LoadSurfaces(&header.lumps[LUMP_TEXINFO]);
            // CMod_LoadLeafs(&header.lumps[LUMP_LEAFS]);
            // CMod_LoadLeafBrushes(&header.lumps[LUMP_LEAFBRUSHES]);
            // CMod_LoadPlanes(&header.lumps[LUMP_PLANES]);
            // CMod_LoadBrushes(&header.lumps[LUMP_BRUSHES]);
            // CMod_LoadBrushSides(&header.lumps[LUMP_BRUSHSIDES]);
            // CMod_LoadSubmodels(&header.lumps[LUMP_MODELS]);
            // CMod_LoadNodes(&header.lumps[LUMP_NODES]);
            // CMod_LoadAreas(&header.lumps[LUMP_AREAS]);
            // CMod_LoadAreaPortals(&header.lumps[LUMP_AREAPORTALS]);
            // CMod_LoadVisibility(&header.lumps[LUMP_VISIBILITY]);
            // /* From kmquake2: adding an extra parameter for .ent support. */
            CMod_LoadEntityString(buf, header.lumps[LUMP_ENTITIES], name);

            // FS_FreeFile(buf);

            // CM_InitBoxHull();

            // memset(portalopen, 0, sizeof(portalopen));
            // FloodAreaConnections();

            map_name = name;

            return map_cmodels[0];
        }

        public string CM_EntityString()
        {
            return map_entitystring;
        }
    }
}