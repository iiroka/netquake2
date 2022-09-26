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
 * Main header file for the client
 *
 * =======================================================================
 */

namespace Quake2 {

    partial class QServer {

        private enum server_state_t
        {
            ss_dead = 0,            /* no map loaded */
            ss_loading,         /* spawning level edicts */
            ss_game,            /* actively running */
            ss_cinematic,
            ss_demo,
            ss_pic
        }

        private struct server_t
        {
            public server_state_t state;           /* precache commands are only valid during load */

            public bool attractloop;           /* running cinematics and demos for the local system only */
            public bool loadgame;              /* client begins should reuse existing entity */

            public uint time;                  /* always sv.framenum * 100 msec */
            public int framenum;

            public string name;           /* map name, or cinematic name */
            public QShared.cmodel_t?[] models;

            public string[] configstrings;
            public QShared.entity_state_t[] baselines;

            /* the multicast buffer is used to send a message to a set of clients
            it is only used to marshall data until SV_Multicast is called */
            public QWritebuf multicast;
            // byte multicast_buf[MAX_MSGLEN];

            /* demo server information */
            public QCommon.IFileHandle? demofile;
            // qboolean timedemo; /* don't time sync */
        }

        private enum client_state_t
        {
            cs_free,        /* can be reused for a new connection */
            cs_zombie,      /* client has been disconnected, but don't reuse 
                            connection for a couple seconds */
            cs_connected,   /* has been assigned to a client_t, but not in game yet */
            cs_spawned      /* client is fully in game */
        }

        private struct client_frame_t
        {
            public int areabytes;
            public byte[] areabits; // [MAX_MAP_AREAS / 8];       /* portalarea visibility bits */
            public QShared.player_state_t ps;
            public int num_entities;
            public int first_entity;                       /* into the circular sv_packet_entities[] */
            public int senttime;                           /* for ping calculations */
        }

        private struct client_t
        {
            public int index { get; init; }
            public client_state_t state;

            public string userinfo;     /* name, etc */

            public int lastframe;                      /* for delta compression */
            public QShared.usercmd_t lastcmd;                  /* for filling in big drops */

            public int commandMsec;                    /* every seconds this is reset, if user */
                                                /* commands exhaust it, assume time cheating */

            // int frame_latency[LATENCY_COUNTS];
            public int ping;

            // int message_size[RATE_MESSAGES];    /* used to rate drop packets */
            public int rate;
            public int surpressCount;                  /* number of messages rate supressed */

            public edict_s? edict;                     /* EDICT_NUM(clientnum+1) */
            public string name;                      /* extracted from userinfo, high bits masked */
            public int messagelevel;                   /* for filtering printed messages */

            /* The datagram is written to by sound calls, prints, 
            temp ents, etc. It can be harmlessly overflowed. */
            public QWritebuf datagram;
            // sizebuf_t datagram;
            // byte datagram_buf[MAX_MSGLEN];

            public client_frame_t[] frames; //[UPDATE_BACKUP];     /* updates can be delta'd from here */

            // byte *download;                     /* file being downloaded */
            // int downloadsize;                   /* total bytes (can't use EOF because of paks) */
            // int downloadcount;                  /* bytes sent */

            public int lastmessage;                    /* sv.framenum when packet was last received */
            public int lastconnect;

            public int challenge;                      /* challenge of this user, randomly generated */

            public QCommon.netchan_t netchan;
        }

        private struct server_static_t
        {
            public bool initialized;               /* sv_init has completed */
            public int realtime;                       /* always increasing, no clamping, etc */

            public string mapcmd;  /* ie: *intro.cin+base */

            public int spawncount;                     /* incremented each server start */
                                                /* used to check late spawns */

            public client_t[] clients;                  /* [maxclients->value]; */
            public int num_client_entities;            /* maxclients->value*UPDATE_BACKUP*MAX_PACKET_ENTITIES */
            public int next_client_entities;           /* next client_entity to use */
            public QShared.entity_state_t[] client_entities;    /* [num_client_entities] */

            public int last_heartbeat;

            // public challenge_t challenges[MAX_CHALLENGES];    /* to prevent invalid IPs from connecting */

            /* serverrecord values */
            // public FILE *demofile;
            // public sizebuf_t demo_multicast;
            // public byte demo_multicast_buf[MAX_MSGLEN];
        }

    }
}