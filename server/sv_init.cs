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
 * Server startup.
 *
 * =======================================================================
 */
using System.Numerics;

namespace Quake2 {

    partial class QServer {

        private const int GAMEMODE_SP = 0;
        private const int GAMEMODE_COOP = 1;
        private const int GAMEMODE_DM = 2;

        private server_static_t svs = new server_static_t(); /* persistant server info */
        private server_t sv = new server_t(); /* local server */
        
        private int SV_FindIndex(string name, int start, int max, bool create)
        {
            if (String.IsNullOrEmpty(name))
            {
                return 0;
            }

            int i;
            for (i = 1; i < max && !String.IsNullOrEmpty(sv.configstrings[start + i]); i++)
            {
                if (sv.configstrings[start + i].Equals(name))
                {
                    return i;
                }
            }

            if (!create)
            {
                return 0;
            }

            if (i == max)
            {
                common.Com_Error(QShared.ERR_DROP, "*Index: overflow");
            }

            sv.configstrings[start + i] = name;

            if (sv.state != server_state_t.ss_loading)
            {
                /* send the update to everyone */
                sv.multicast.WriteChar((int)QCommon.svc_ops_e.svc_configstring);
                sv.multicast.WriteShort(start + i);
                sv.multicast.WriteString(name);
                SV_Multicast(Vector3.Zero, QShared.multicast_t.MULTICAST_ALL_R);
            }

            return i;
        }        

       /*
        * Change the server to a new map, taking all connected
        * clients along with it.
        */
        private void SV_SpawnServer(string server, string spawnpoint, server_state_t serverstate,
                bool attractloop, bool loadgame, bool isautosave)
        {
            // int i;
            // unsigned checksum;

            if (attractloop)
            {
                common.Cvar_Set("paused", "0");
            }

            common.Com_Printf("------- server initialization ------\n");
            common.Com_DPrintf($"SpawnServer: {server}\n");

            sv.demofile?.Close();

            svs.spawncount++; /* any partially connected client will be restarted */
            sv.state = server_state_t.ss_dead;
            common.ServerState = ((int)sv.state);

            /* wipe the entire per-level structure */
            sv = new server_t();
            svs.realtime = 0;
            sv.loadgame = loadgame;
            sv.attractloop = attractloop;
            sv.configstrings = new string[QShared.MAX_CONFIGSTRINGS];
            sv.models = new QShared.cmodel_t?[QShared.MAX_MODELS];
            sv.multicast = new QWritebuf(QCommon.MAX_MSGLEN);
            sv.baselines = new QShared.entity_state_t[QShared.MAX_EDICTS];
            for (int i = 0; i < sv.baselines.Length; i++)
                sv.baselines[i] = new QShared.entity_state_t();

            /* save name for levels that don't set message */
            sv.configstrings[QShared.CS_NAME] = server;

            // if (Cvar_VariableValue("deathmatch"))
            // {
            //     sprintf(sv.configstrings[CS_AIRACCEL], "%g", sv_airaccelerate->value);
            //     pm_airaccelerate = sv_airaccelerate->value;
            // }
            // else
            // {
                sv.configstrings[QShared.CS_AIRACCEL] = "0";
                common.pm_airaccelerate = 0;
            // }

            sv.name = server;

            /* leave slots at start for clients only */
            for (int i = 0; i < svs.clients.Length; i++)
            {
                /* needs to reconnect */
                if (svs.clients[i].state > client_state_t.cs_connected)
                {
                    svs.clients[i].state = client_state_t.cs_connected;
                }

                svs.clients[i].lastframe = -1;
            }

            sv.time = 1000;

            uint checksum;
            if (serverstate != server_state_t.ss_game)
            {
                sv.models[1] = common.CM_LoadMap("", false, out checksum); /* no real map */
            }
            else
            {
                sv.configstrings[QShared.CS_MODELS + 1] = $"maps/{server}.bsp";
                sv.models[1] = common.CM_LoadMap(sv.configstrings[QShared.CS_MODELS + 1], false, out checksum);
            }

            sv.configstrings[QShared.CS_MAPCHECKSUM] = checksum.ToString();

            /* clear physics interaction links */
            SV_ClearWorld();

            for (int i = 1; i < common.CM_NumInlineModels(); i++)
            {
                sv.configstrings[QShared.CS_MODELS + 1 + i] = $"*{i}";
                sv.models[i + 1] = common.CM_InlineModel(sv.configstrings[QShared.CS_MODELS + 1 + i]);
            }

            /* spawn the rest of the entities on the map */
            sv.state = server_state_t.ss_loading;
            common.ServerState = ((int)sv.state);

            /* load and spawn all other entities */
            ge!.SpawnEntities(sv.name, common.CM_EntityString(), spawnpoint);

            /* run two frames to allow everything to settle */
            ge.RunFrame();
            ge.RunFrame();

            // /* verify game didn't clobber important stuff */
            // if ((int)checksum !=
            //     (int)strtol(sv.configstrings[CS_MAPCHECKSUM], (char **)NULL, 10))
            // {
            //     Com_Error(ERR_DROP, "Game DLL corrupted server configstrings");
            // }

            /* all precaches are complete */
            sv.state = serverstate;
            common.ServerState = ((int)sv.state);

            // /* create a baseline for more efficient communications */
            // SV_CreateBaseline();

            // /* check for a savegame */
            // SV_CheckForSavegame(isautosave);

            /* set serverinfo variable */
            common.Cvar_FullSet("mapname", sv.name, cvar_t.CVAR_SERVERINFO | cvar_t.CVAR_NOSET);

            common.Com_Printf("------------------------------------\n\n");
        }

        /*
        * A brand new game has been started
        */
        private void SV_ClearGamemodeCvar(string name, ref string msg, int flags)
        {
            common.Cvar_FullSet(name, "0", flags);

            msg = msg + name + " ";
        }

        private int SV_ChooseGamemode()
        {
            // char msg[32], *choice;
            int gamemode = GAMEMODE_SP;

            string msg = "";
            string choice = "";

            if (common.Cvar_VariableBool("deathmatch"))
            {
                if (common.Cvar_VariableBool("coop"))
                {
                    SV_ClearGamemodeCvar("coop", ref msg, cvar_t.CVAR_SERVERINFO | cvar_t.CVAR_LATCH);
                }

                if (common.Cvar_VariableBool("singleplayer"))
                {
                    SV_ClearGamemodeCvar("singleplayer", ref msg, 0);
                }

                choice = "deathmatch";
                gamemode = GAMEMODE_DM;
            }
            else if (common.Cvar_VariableBool("coop"))
            {
                if (common.Cvar_VariableBool("singleplayer"))
                {
                    SV_ClearGamemodeCvar("singleplayer", ref msg, 0);
                }

                choice = "coop";
                gamemode = GAMEMODE_COOP;
            }
            else
            {
                if (common.dedicated?.Bool ?? false && !common.Cvar_VariableBool("singleplayer"))
                {
                    common.Cvar_FullSet("deathmatch", "1", cvar_t.CVAR_SERVERINFO | cvar_t.CVAR_LATCH);

                    choice = "deathmatch";
                    gamemode = GAMEMODE_DM;
                }
                else
                {
                    common.Cvar_FullSet("singleplayer", "1", cvar_t.CVAR_SERVERINFO | cvar_t.CVAR_LATCH);

                    choice = "singleplayer";
                    gamemode = GAMEMODE_SP;
                }
            }

            if (msg.Length > 0)
            {
                common.Com_Printf($"Gamemode ambiguity: Chose: {choice}, ignored: {msg}\n");
            }

            return gamemode;
        }

        private void SV_InitGame()
        {
        //     int i, gamemode;
        //     edict_t *ent;
        //     char idmaster[32];

            if (svs.initialized)
            {
                /* cause any connected clients to reconnect */
                SV_Shutdown("Server restarted\n", true);
            }
            else
            {
        //         /* make sure the client is down */
        //         CL_Drop();
        //         SCR_BeginLoadingPlaque();
            }

            /* get any latched variable changes (maxclients, etc) */
        //     Cvar_GetLatchedVars();

            svs.initialized = true;

            var gamemode = SV_ChooseGamemode();

            /* init clients */
            if (gamemode == GAMEMODE_DM)
            {
                if (maxclients!.Int <= 1)
                {
                    common.Cvar_FullSet("maxclients", "8", cvar_t.CVAR_SERVERINFO | cvar_t.CVAR_LATCH);
                }
                else if (maxclients!.Int > QShared.MAX_CLIENTS)
                {
                    common.Cvar_FullSet("maxclients", QShared.MAX_CLIENTS.ToString(), cvar_t.CVAR_SERVERINFO | cvar_t.CVAR_LATCH);
                }
            }
            else if (gamemode == GAMEMODE_COOP)
            {
                if ((maxclients!.Int <= 1) || (maxclients!.Int > 4))
                {
                    common.Cvar_FullSet("maxclients", "4", cvar_t.CVAR_SERVERINFO | cvar_t.CVAR_LATCH);
                }
            }
            else /* non-deathmatch, non-coop is one player */
            {
                common.Cvar_FullSet("maxclients", "1", cvar_t.CVAR_SERVERINFO | cvar_t.CVAR_LATCH);
            }

            svs.spawncount = QShared.rand.Next();
            svs.clients = new client_t[maxclients!.Int];
            svs.num_client_entities = maxclients!.Int * QCommon.UPDATE_BACKUP * 64;
            svs.client_entities = new QShared.entity_state_t[svs.num_client_entities];
            for (int i = 0; i < svs.num_client_entities; i++)
                svs.client_entities[i] = new QShared.entity_state_t();

            /* init network stuff */
        //     if (dedicated->value)
        //     {
        //         if (gamemode == GAMEMODE_SP)
        //         {
        //             NET_Config(true);
        //         }
        //         else
        //         {
        //             NET_Config((maxclients->value > 1));
        //         }
        //     }
        //     else
        //     {
        //         NET_Config((maxclients->value > 1));
        //     }

            /* heartbeats will always be sent to the id master */
            svs.last_heartbeat = -99999; /* send immediately */
        //     Com_sprintf(idmaster, sizeof(idmaster), "192.246.40.37:%i", PORT_MASTER);
        //     NET_StringToAdr(idmaster, &master_adr[0]);

            /* init game */
            SV_InitGameProgs();

            for (int i = 0; i < maxclients!.Int; i++)
            {
                var ent = ge!.getEdict(i+1);
                ent.s.number = i + 1;
                svs.clients[i].edict = ent;
        //         memset(&svs.clients[i].lastcmd, 0, sizeof(svs.clients[i].lastcmd));
            }
        }

        /*
        * the full syntax is:
        *
        * map [*]<map>$<startspot>+<nextserver>
        *
        * command from the console or progs.
        * Map can also be a.cin, .pcx, or .dm2 file
        * Nextserver is used to allow a cinematic to play, then proceed to
        * another level:
        *
        *  map tram.cin+jail_e3
        */
        private void SV_Map(bool attractloop, string levelstring, bool loadgame, bool isautosave)
        {
            sv.loadgame = loadgame;
            sv.attractloop = attractloop;

            if ((sv.state == server_state_t.ss_dead) && !sv.loadgame)
            {
                SV_InitGame(); /* the game is just starting */
            }

            var level = levelstring;

            /* if there is a + in the map, set nextserver to the remainder */
            var ch = level.IndexOf("+");
            if (ch > 0)
            {
                common.Cvar_Set("nextserver", $"gamemap \"{level.Substring(ch + 1)}\"");
                level = level.Substring(0, ch);
            }
            else
            {
                // use next demo command if list of map commands as empty
                common.Cvar_Set("nextserver", common.Cvar_VariableString("nextdemo"));
                // and cleanup nextdemo
                common.Cvar_Set("nextdemo", "");
            }

        //     /* hack for end game screen in coop mode */
        //     if (Cvar_VariableValue("coop") && !Q_stricmp(level, "victory.pcx"))
        //     {
        //         Cvar_Set("nextserver", "gamemap \"*base1\"");
        //     }

            /* if there is a $, use the remainder as a spawnpoint */
            ch = level.IndexOf("$");
            string spawnpoint = "";
            if (ch > 0)
            {
                spawnpoint = level.Substring(ch + 1);
                level = level.Substring(0, ch);
            }

        //     /* skip the end-of-unit flag if necessary */
        //     l = strlen(level);

            if (level[0] == '*')
            {
                level = level.Substring(1);
            }

            Console.WriteLine($"Level: \"{level}\" spawnpoint: \"{spawnpoint}\"");

            if (level.EndsWith(".cin"))
            {
        // #ifndef DEDICATED_ONLY
        //         SCR_BeginLoadingPlaque(); /* for local system */
        // #endif
                SV_BroadcastCommand("changing\n");
                SV_SpawnServer(level, spawnpoint, server_state_t.ss_cinematic, attractloop, loadgame, isautosave);
            }
            else if (level.EndsWith(".dm2"))
            {
        // #ifndef DEDICATED_ONLY
        //         SCR_BeginLoadingPlaque(); /* for local system */
        // #endif
                SV_BroadcastCommand("changing\n");
                SV_SpawnServer(level, spawnpoint, server_state_t.ss_demo, attractloop, loadgame, isautosave);
            }
            else if (level.EndsWith(".pcx"))
            {
        // #ifndef DEDICATED_ONLY
        //         SCR_BeginLoadingPlaque(); /* for local system */
        // #endif
                SV_BroadcastCommand("changing\n");
                SV_SpawnServer(level, spawnpoint, server_state_t.ss_pic, attractloop, loadgame, isautosave);
            }
            else
            {
        // #ifndef DEDICATED_ONLY
        //         SCR_BeginLoadingPlaque(); /* for local system */
        // #endif
                SV_BroadcastCommand("changing\n");
        //         SV_SendClientMessages();
                SV_SpawnServer(level, spawnpoint, server_state_t.ss_game, attractloop, loadgame, isautosave);
        //         Cbuf_CopyToDefer();
            }

            SV_BroadcastCommand("reconnect\n");
        }


    }
}