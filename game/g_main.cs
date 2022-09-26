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
 * Jump in into the game.so and support functions.
 *
 * =======================================================================
 */

namespace Quake2 {

    partial class QuakeGame : game_export_t
    {

        private game_locals_t game;
        private level_locals_t level;
        private game_import_t gi;
        // game_export_t globals;
        private spawn_temp_t st;

        private int sm_meat_index;
        private int snd_fry;
        private int meansOfDeath;

        private edict_t[] g_edicts;

        private cvar_t? deathmatch;
        private cvar_t? coop;
        private cvar_t? coop_pickup_weapons;
        private cvar_t? coop_elevator_delay;
        private cvar_t? dmflags;
        private cvar_t? skill;
        private cvar_t? fraglimit;
        private cvar_t? timelimit;
        private cvar_t? password;
        private cvar_t? spectator_password;
        private cvar_t? needpass;
        private cvar_t? maxclients;
        private cvar_t? maxspectators;
        private cvar_t? maxentities;
        private cvar_t? g_select_empty;
        private cvar_t? dedicated;
        private cvar_t? g_footsteps;
        private cvar_t? g_fix_triggered;
        private cvar_t? g_commanderbody_nogod;

        private cvar_t? filterban;

        private cvar_t? sv_maxvelocity;
        private cvar_t? sv_gravity;

        private cvar_t? sv_rollspeed;
        private cvar_t? sv_rollangle;
        private cvar_t? gun_x;
        private cvar_t? gun_y;
        private cvar_t? gun_z;

        private cvar_t? run_pitch;
        private cvar_t? run_roll;
        private cvar_t? bob_up;
        private cvar_t? bob_pitch;
        private cvar_t? bob_roll;

        private cvar_t? sv_cheats;

        private cvar_t? flood_msgs;
        private cvar_t? flood_persecond;
        private cvar_t? flood_waitdelay;

        private cvar_t? sv_maplist;

        private cvar_t? gib_on;

        private cvar_t? aimfix;
        private cvar_t? g_machinegun_norecoil;        

        private int global_num_ecicts;

        // public edict_s[] edicts { get{ return g_edicts; } }
        public edict_s getEdict(int index) { return g_edicts[index]; }
        public int num_edicts { get { return global_num_ecicts; } }             /* current number, <= max_edicts */
        public int max_edicts { get { return game.maxentities; } }

        public QuakeGame(game_import_t gi)
        {
            this.gi = gi;
            this.g_edicts = new edict_t[0];
        }

        public void Init()
        {
            gi.dprintf("Game is starting up.\n");
            // gi.dprintf("Game is %s built on %s.\n", GAMEVERSION, BUILD_DATE);

            gun_x = gi.cvar("gun_x", "0", 0);
            gun_y = gi.cvar("gun_y", "0", 0);
            gun_z = gi.cvar("gun_z", "0", 0);
            sv_rollspeed = gi.cvar("sv_rollspeed", "200", 0);
            sv_rollangle = gi.cvar("sv_rollangle", "2", 0);
            sv_maxvelocity = gi.cvar("sv_maxvelocity", "2000", 0);
            sv_gravity = gi.cvar("sv_gravity", "800", 0);

            /* noset vars */
            dedicated = gi.cvar("dedicated", "0", cvar_t.CVAR_NOSET);

            /* latched vars */
            sv_cheats = gi.cvar("cheats", "0", cvar_t.CVAR_SERVERINFO | cvar_t.CVAR_LATCH);
            // gi.cvar("gamename", GAMEVERSION, cvar_t.CVAR_SERVERINFO | cvar_t.CVAR_LATCH);
            // gi.cvar("gamedate", BUILD_DATE, cvar_t.CVAR_SERVERINFO | cvar_t.CVAR_LATCH);
            maxclients = gi.cvar("maxclients", "4", cvar_t.CVAR_SERVERINFO | cvar_t.CVAR_LATCH);
            maxspectators = gi.cvar("maxspectators", "4", cvar_t.CVAR_SERVERINFO);
            deathmatch = gi.cvar("deathmatch", "0", cvar_t.CVAR_LATCH);
            coop = gi.cvar("coop", "0", cvar_t.CVAR_LATCH);
            coop_pickup_weapons = gi.cvar("coop_pickup_weapons", "1", cvar_t.CVAR_ARCHIVE);
            coop_elevator_delay = gi.cvar("coop_elevator_delay", "1.0", cvar_t.CVAR_ARCHIVE);
            skill = gi.cvar("skill", "1", cvar_t.CVAR_LATCH);
            maxentities = gi.cvar("maxentities", "1024", cvar_t.CVAR_LATCH);
            g_footsteps = gi.cvar("g_footsteps", "1", cvar_t.CVAR_ARCHIVE);
            g_fix_triggered = gi.cvar ("g_fix_triggered", "0", 0);
            g_commanderbody_nogod = gi.cvar("g_commanderbody_nogod", "0", cvar_t.CVAR_ARCHIVE);

            /* change anytime vars */
            dmflags = gi.cvar("dmflags", "0", cvar_t.CVAR_SERVERINFO);
            fraglimit = gi.cvar("fraglimit", "0", cvar_t.CVAR_SERVERINFO);
            timelimit = gi.cvar("timelimit", "0", cvar_t.CVAR_SERVERINFO);
            password = gi.cvar("password", "", cvar_t.CVAR_USERINFO);
            spectator_password = gi.cvar("spectator_password", "", cvar_t.CVAR_USERINFO);
            needpass = gi.cvar("needpass", "0", cvar_t.CVAR_SERVERINFO);
            filterban = gi.cvar("filterban", "1", 0);
            g_select_empty = gi.cvar("g_select_empty", "0", cvar_t.CVAR_ARCHIVE);
            run_pitch = gi.cvar("run_pitch", "0.002", 0);
            run_roll = gi.cvar("run_roll", "0.005", 0);
            bob_up = gi.cvar("bob_up", "0.005", 0);
            bob_pitch = gi.cvar("bob_pitch", "0.002", 0);
            bob_roll = gi.cvar("bob_roll", "0.002", 0);

            /* flood control */
            flood_msgs = gi.cvar("flood_msgs", "4", 0);
            flood_persecond = gi.cvar("flood_persecond", "4", 0);
            flood_waitdelay = gi.cvar("flood_waitdelay", "10", 0);

            /* dm map list */
            sv_maplist = gi.cvar("sv_maplist", "", 0);

            /* others */
            aimfix = gi.cvar("aimfix", "0", cvar_t.CVAR_ARCHIVE);
            g_machinegun_norecoil = gi.cvar("g_machinegun_norecoil", "0", cvar_t.CVAR_ARCHIVE);

            /* items */
            InitItems();

            game.helpmessage1 = "";
            game.helpmessage2 = "";

            /* initialize all entities for this game */
            game.maxentities = maxentities!.Int;
            g_edicts = new edict_t[game.maxentities];
            for (int i = 0; i < g_edicts.Length; i++) {
                g_edicts[i] = new edict_t() { index = i };
            }

            /* initialize all clients for this game */
            game.maxclients = maxclients!.Int;
            game.clients = new gclient_t[game.maxclients];
            for (int i = 0; i < game.clients.Length; i++) {
                game.clients[i] = new gclient_t();
            }
            global_num_ecicts =  game.maxclients + 1;
        }

        /* ====================================================================== */

        private void ClientEndServerFrames()
        {
            /* calc the player views now that all
            pushing  and damage has been added */
            for (int i = 0; i < maxclients!.Int; i++)
            {
                ref var ent = ref g_edicts[1 + i];

                if (!ent.inuse || ent.client == null)
                {
                    continue;
                }

                ClientEndServerFrame(ent);
            }
        }

        /*
        * Advances the world by 0.1 seconds
        */
        public void RunFrame()
        {
            // int i;
            // edict_t *ent;

            level.framenum++;
            level.time = level.framenum * FRAMETIME;

            // gibsthisframe = 0;
            // debristhisframe = 0;

            // /* choose a client for monsters to target this frame */
            // AI_SetSightClient();

            // /* exit intermissions */
            // if (level.exitintermission)
            // {
            //     ExitLevel();
            //     return;
            // }

            /* treat each object in turn
            even the world gets a chance
            to think */

            for (int i = 0; i < num_edicts; i++)
            {
                ref var ent = ref g_edicts[i];
                if (!ent.inuse)
                {
                    continue;
                }

                level.current_entity = ent;

                ent.s.old_origin = ent.s.origin;

            //     /* if the ground entity moved, make sure we are still on it */
            //     if ((ent->groundentity) &&
            //         (ent->groundentity->linkcount != ent->groundentity_linkcount))
            //     {
            //         ent->groundentity = NULL;

            //         if (!(ent->flags & (FL_SWIM | FL_FLY)) &&
            //             (ent->svflags & SVF_MONSTER))
            //         {
            //             M_CheckGround(ent);
            //         }
            //     }

                if ((i > 0) && (i <= maxclients!.Int))
                {
                    // ClientBeginServerFrame(ent);
                    continue;
                }

                G_RunEntity(ent);
            }

            // /* see if it is time to end a deathmatch */
            // CheckDMRules();

            // /* see if needpass needs updated */
            // CheckNeedPass();

            /* build the playerstate_t structures for all players */
            ClientEndServerFrames();
        }
    }
}
