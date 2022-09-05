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
 * Prototypes witch are shared between the client, the server and the
 * game. This is the main game API, changes here will most likely
 * requiere changes to the game ddl.
 *
 * =======================================================================
 */

namespace Quake2 {

    partial class QCommon {

        internal static string YQ2VERSION = "8.11pre";
        internal static string BASEDIRNAME = "baseq2";

        internal static int PORT_ANY = -1;
        internal static int MAX_MSGLEN = 1400;             /* max length of a message */
        internal static int PACKET_HEADER = 10;            /* two ints and a short */

        internal enum netadrtype_t
        {
            NA_LOOPBACK,
            NA_BROADCAST,
            NA_IP,
            NA_IPX,
            NA_BROADCAST_IPX,
            NA_IP6,
            NA_MULTICAST6
        }

        internal enum netsrc_t {NS_CLIENT = 0, NS_SERVER};

        internal class netadr_t
        {
            public netadrtype_t type;
            // byte ip[16];
            // unsigned int scope_id;
            // byte ipx[10];

            public ushort port;

            public static netadr_t? FromString(string s)
            {
                var a = new netadr_t();
            	if (s.Equals("localhost"))
	            {
		            a.type = netadrtype_t.NA_LOOPBACK;
                    return a;
	            }
                return null;
            }

            /*
            * Compares without the port
            */
            public bool CompareBaseAdr(in netadr_t other)
            {
                if (type != other.type)
                {
                    return false;
                }

                if (type == netadrtype_t.NA_LOOPBACK)
                {
                    return true;
                }

                // if (a.type == NA_IP)
                // {
                //     if ((a.ip[0] == b.ip[0]) && (a.ip[1] == b.ip[1]) &&
                //         (a.ip[2] == b.ip[2]) && (a.ip[3] == b.ip[3]))
                //     {
                //         return true;
                //     }

                //     return false;
                // }

                // if (a.type == NA_IP6)
                // {
                //     if ((memcmp(a.ip, b.ip, 16) == 0))
                //     {
                //         return true;
                //     }

                //     return false;
                // }

                // if (a.type == NA_IPX)
                // {
                //     if ((memcmp(a.ipx, b.ipx, 10) == 0))
                //     {
                //         return true;
                //     }

                //     return false;
                // }

                return false;
            }

            public bool IsLocalAddress()
            {
                return type == netadrtype_t.NA_LOOPBACK;
            }


        }

        /* PROTOCOL */

        internal static int PROTOCOL_VERSION = 34;

        /* ========================================= */

        internal static int PORT_MASTER = 27900;
        internal static int PORT_CLIENT = 27901;
        internal static int PORT_SERVER = 27910;

        /* ========================================= */

        internal static int UPDATE_BACKUP = 16;    /* copies of entity_state_t to keep buffered */
        internal static int UPDATE_MASK = (UPDATE_BACKUP - 1);

        /* server to client */
        internal enum svc_ops_e
        {
            svc_bad = 0,

            /* these ops are known to the game dll */
            svc_muzzleflash,
            svc_muzzleflash2,
            svc_temp_entity,
            svc_layout,
            svc_inventory,

            /* the rest are private to the client and server */
            svc_nop,
            svc_disconnect,
            svc_reconnect,
            svc_sound,                  /* <see code> */
            svc_print,                  /* [byte] id [string] null terminated string */
            svc_stufftext,              /* [string] stuffed into client's console buffer, should be \n terminated */
            svc_serverdata,             /* [long] protocol ... */
            svc_configstring,           /* [short] [string] */
            svc_spawnbaseline,
            svc_centerprint,            /* [string] to put in center of the screen */
            svc_download,               /* [short] size [size bytes] */
            svc_playerinfo,             /* variable */
            svc_packetentities,         /* [...] */
            svc_deltapacketentities,    /* [...] */
            svc_frame
        };

        /* ============================================== */

        /* client to server */
        internal enum clc_ops_e
        {
            clc_bad = 0,
            clc_nop,
            clc_move,               /* [[usercmd_t] */
            clc_userinfo,           /* [[userinfo string] */
            clc_stringcmd           /* [string] message */
        };

        /* ============================================== */

        /* plyer_state_t communication */
        internal const uint PS_M_TYPE = (1 << 0);
        internal const uint PS_M_ORIGIN = (1 << 1);
        internal const uint PS_M_VELOCITY = (1 << 2);
        internal const uint PS_M_TIME = (1 << 3);
        internal const uint PS_M_FLAGS = (1 << 4);
        internal const uint PS_M_GRAVITY = (1 << 5);
        internal const uint PS_M_DELTA_ANGLES = (1 << 6);

        internal const uint PS_VIEWOFFSET = (1 << 7);
        internal const uint PS_VIEWANGLES = (1 << 8);
        internal const uint PS_KICKANGLES = (1 << 9);
        internal const uint PS_BLEND = (1 << 10);
        internal const uint PS_FOV = (1 << 11);
        internal const uint PS_WEAPONINDEX = (1 << 12);
        internal const uint PS_WEAPONFRAME = (1 << 13);
        internal const uint PS_RDFLAGS = (1 << 14);

        /*============================================== */

        /* user_cmd_t communication */

        /* ms and light always sent, the others are optional */
        internal const uint CM_ANGLE1 = (1 << 0);
        internal const uint CM_ANGLE2 = (1 << 1);
        internal const uint CM_ANGLE3 = (1 << 2);
        internal const uint CM_FORWARD = (1 << 3);
        internal const uint CM_SIDE = (1 << 4);
        internal const uint CM_UP = (1 << 5);
        internal const uint CM_BUTTONS = (1 << 6);
        internal const uint CM_IMPULSE = (1 << 7);

        /*============================================== */

        /* a sound without an ent or pos will be a local only sound */
        internal const uint SND_VOLUME = (1 << 0);         /* a byte */
        internal const uint SND_ATTENUATION = (1 << 1);      /* a byte */
        internal const uint SND_POS = (1 << 2);            /* three coordinates */
        internal const uint SND_ENT = (1 << 3);            /* a short 0-2: channel, 3-12: entity */
        internal const uint SND_OFFSET = (1 << 4);         /* a byte, msec offset from frame start */

        internal const float DEFAULT_SOUND_PACKET_VOLUME = 1.0f;
        internal const float DEFAULT_SOUND_PACKET_ATTENUATION = 1.0f;

        /*============================================== */

        /* entity_state_t communication */

        /* try to pack the common update flags into the first byte */
        internal const uint U_ORIGIN1 = (1 << 0);
        internal const uint U_ORIGIN2 = (1 << 1);
        internal const uint U_ANGLE2 = (1 << 2);
        internal const uint U_ANGLE3 = (1 << 3);
        internal const uint U_FRAME8 = (1 << 4);       /* frame is a byte */
        internal const uint U_EVENT = (1 << 5);
        internal const uint U_REMOVE = (1 << 6);       /* REMOVE this entity, don't add it */
        internal const uint U_MOREBITS1 = (1 << 7);      /* read one additional byte */

        /* second byte */
        internal const uint U_NUMBER16 = (1 << 8);      /* NUMBER8 is implicit if not set */
        internal const uint U_ORIGIN3 = (1 << 9);
        internal const uint U_ANGLE1 = (1 << 10);
        internal const uint U_MODEL = (1 << 11);
        internal const uint U_RENDERFX8 = (1 << 12);     /* fullbright, etc */
        internal const uint U_EFFECTS8 = (1 << 14);     /* autorotate, trails, etc */
        internal const uint U_MOREBITS2 = (1 << 15);     /* read one additional byte */

        /* third byte */
        internal const uint U_SKIN8 = (1 << 16);
        internal const uint U_FRAME16 = (1 << 17);     /* frame is a short */
        internal const uint U_RENDERFX16 = (1 << 18);    /* 8 + 16 = 32 */
        internal const uint U_EFFECTS16 = (1 << 19);     /* 8 + 16 = 32 */
        internal const uint U_MODEL2 = (1 << 20);      /* weapons, flags, etc */
        internal const uint U_MODEL3 = (1 << 21);
        internal const uint U_MODEL4 = (1 << 22);
        internal const uint U_MOREBITS3 = (1 << 23);     /* read one additional byte */

        /* fourth byte */
        internal const uint U_OLDORIGIN = (1 << 24);
        internal const uint U_SKIN16 = (1 << 25);
        internal const uint U_SOUND = (1 << 26);
        internal const uint U_SOLID = (1 << 27);

    }
}