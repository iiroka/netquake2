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
 * Here are the client, server and game are tied together.
 *
 * =======================================================================
 */
using System.Numerics;

/*
 * !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *
 * THIS FILE IS _VERY_ FRAGILE AND THERE'S NOTHING IN IT THAT CAN OR
 * MUST BE CHANGED. IT'S MOST LIKELY A VERY GOOD IDEA TO CLOSE THE
 * EDITOR NOW AND NEVER LOOK BACK. OTHERWISE YOU MAY SCREW UP EVERYTHING!
 *
 * !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 */

namespace Quake2 {

    internal struct QGameFlags
    {
        internal const int SVF_NOCLIENT = 0x00000001; /* don't send entity to clients, even if it has effects */
        internal const int SVF_DEADMONSTER = 0x00000002; /* treat as CONTENTS_DEADMONSTER for collision */
        internal const int SVF_MONSTER = 0x00000004; /* treat as CONTENTS_MONSTER for collision */

        internal const int MAX_ENT_CLUSTERS = 16;
    }

    internal enum solid_t
    {
        SOLID_NOT, /* no interaction with other objects */
        SOLID_TRIGGER, /* only touch when inside, after moving */
        SOLID_BBOX, /* touch on edge */
        SOLID_BSP /* bsp clip, touch on edge */
    }

    /* =============================================================== */

    /* link_t is only used for entity area links now */
    internal class link_t
    {
        public link_t? prev, next;
        public edict_s ent;
    }


    internal abstract class gclient_s
    {
        public QShared.player_state_t ps = new QShared.player_state_t();      /* communicated by server to clients */
        public int ping;
        /* the game dll can add anything it wants
        after  this point in the structure */
    };

    internal abstract class edict_s
    {
        public const int MAX_ENT_CLUSTERS = 16;
        
        public edict_s? next;
        public edict_s? prev;

        public QShared.entity_state_t s = new QShared.entity_state_t();
        public gclient_s? client;
        public bool inuse;
        public int linkcount;

        // public LinkedList<edict_s> area = new LinkedList<edict_s>();    /* linked to a division node or leaf */
        public link_t area;                    /* linked to a division node or leaf */

        public int num_clusters;               /* if -1, use headnode instead */
        public int[] clusternums = new int [MAX_ENT_CLUSTERS];
        // int clusternums[MAX_ENT_CLUSTERS];
        public int headnode;                   /* unused if num_clusters != -1 */
        public int areanum, areanum2;

        public int svflags;                    /* SVF_NOCLIENT, SVF_DEADMONSTER, SVF_MONSTER, etc */
        public Vector3 mins, maxs;
        public Vector3 absmin, absmax, size;
        public solid_t solid;
        public int clipmask;
        public edict_s? owner;

        /* the game dll can add anything it wants
        after this point in the structure */
    }

    /* functions provided by the main engine */
    internal interface game_import_t
    {
        /* special messages */
        // void (*bprintf)(int printlevel, char *fmt, ...);
        void dprintf(string msg);
        // void (*cprintf)(edict_t *ent, int printlevel, char *fmt, ...);
        // void (*centerprintf)(edict_t *ent, char *fmt, ...);
        // void (*sound)(edict_t *ent, int channel, int soundindex, float volume,
        //         float attenuation, float timeofs);
        // void (*positioned_sound)(vec3_t origin, edict_t *ent, int channel,
        //         int soundinedex, float volume, float attenuation, float timeofs);

        /* config strings hold all the index strings, the lightstyles,
        and misc data like the sky definition and cdtrack.
        All of the current configstrings are sent to clients when
        they connect, and changes are sent to all connected clients. */
        void configstring(int num, string str);

        void error(string msg);

        /* the *index functions create configstrings
           and some internal server state */
        int modelindex(string name);
        // int (*soundindex)(char *name);
        // int (*imageindex)(char *name);

        // void (*setmodel)(edict_t *ent, char *name);

        /* collision detection */
        QShared.trace_t trace(in Vector3 start, in Vector3? mins, in Vector3? maxs, in Vector3 end,
                edict_s passent, int contentmask);
        // int (*pointcontents)(vec3_t point);
        // qboolean (*inPVS)(vec3_t p1, vec3_t p2);
        // qboolean (*inPHS)(vec3_t p1, vec3_t p2);
        // void (*SetAreaPortalState)(int portalnum, qboolean open);
        // qboolean (*AreasConnected)(int area1, int area2);

        // /* an entity will never be sent to a client or used for collision
        // if it is not passed to linkentity. If the size, position, or
        // solidity changes, it must be relinked. */
        void linkentity(edict_s ent);
        // void (*unlinkentity)(edict_t *ent); /* call before removing an interactive edict */
        // int (*BoxEdicts)(vec3_t mins, vec3_t maxs, edict_t **list, int maxcount,
        //         int areatype);
        void Pmove(ref QShared.pmove_t pmove); /* player movement code common with client prediction */

        // /* network messaging */
        // void (*multicast)(vec3_t origin, multicast_t to);
        // void (*unicast)(edict_t *ent, qboolean reliable);
        // void (*WriteChar)(int c);
        // void (*WriteByte)(int c);
        // void (*WriteShort)(int c);
        // void (*WriteLong)(int c);
        // void (*WriteFloat)(float f);
        // void (*WriteString)(char *s);
        // void (*WritePosition)(vec3_t pos); /* some fractional bits */
        // void (*WriteDir)(vec3_t pos); /* single byte encoded, very coarse */
        // void (*WriteAngle)(float f);

        // /* managed memory allocation */
        // void *(*TagMalloc)(int size, int tag);
        // void (*TagFree)(void *block);
        // void (*FreeTags)(int tag);

        /* console variable interaction */
        cvar_t? cvar(string var_name, string? value, int flags);
        // cvar_t *(*cvar_set)(char *var_name, char *value);
        // cvar_t *(*cvar_forceset)(char *var_name, char *value);

        // /* ClientCommand and ServerCommand parameter access */
        // int (*argc)(void);
        // char *(*argv)(int n);
        // char *(*args)(void); /* concatenation of all argv >= 1 */

        // /* add commands to the server console as if
        // they were typed in for map changing, etc */
        // void (*AddCommandString)(char *text);

        // void (*DebugGraph)(float value, int color);
    }

    /* functions exported by the game subsystem */
    internal interface game_export_t
    {
        // public readonly int apiversion;

        /* the init function will only be called when a game starts,
        not each time a level is loaded.  Persistant data for clients
        and the server can be allocated in init */
        void Init();
        // void (*Shutdown)(void);

        /* each new level entered will cause a call to SpawnEntities */
        void SpawnEntities(string mapname, string entstring, string spawnpoint);

        // /* Read/Write Game is for storing persistant cross level information
        // about the world state and the clients.
        // WriteGame is called every time a level is exited.
        // ReadGame is called on a loadgame. */
        // void (*WriteGame)(char *filename, qboolean autosave);
        // void (*ReadGame)(char *filename);

        // /* ReadLevel is called after the default
        // map information has been loaded with
        // SpawnEntities */
        // void (*WriteLevel)(char *filename);
        // void (*ReadLevel)(char *filename);

        bool ClientConnect(edict_s ent, string userinfo);
        void ClientBegin(edict_s ent);
        // void (*ClientUserinfoChanged)(edict_t *ent, char *userinfo);
        // void (*ClientDisconnect)(edict_t *ent);
        // void (*ClientCommand)(edict_t *ent);
        void ClientThink(edict_s ent, in QShared.usercmd_t cmd);

        void RunFrame();

        // /* ServerCommand will be called when an "sv <command>"
        // command is issued on the  server console. The game can
        // issue gi.argc() / gi.argv() commands to get the rest
        // of the parameters */
        // void (*ServerCommand)(void);

        // /* global variables shared between game and server */

        /* The edict array is allocated in the game dll so it
        can vary in size from one game to another.
        The size will be fixed when ge->Init() is called */
        edict_s getEdict(int index);
        int num_edicts { get; }             /* current number, <= max_edicts */
        int max_edicts { get; }
    }

}
