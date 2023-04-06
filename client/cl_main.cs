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
 * This is the clients main loop as well as some miscelangelous utility
 * and support functions
 *
 * =======================================================================
 */

namespace Quake2 {

    partial class QClient {

        private QCommon common;
        public QVid vid;
        private QInput input;

        public QClient(QCommon common)
        {
            this.common = common;
            this.input = new QInput(this, common);
            this.vid = new QVid(common, this.input);
            this.cl_entities = new centity_t[QShared.MAX_EDICTS];
            this.cl_parse_entities = new QShared.entity_state_t[MAX_PARSE_ENTITIES];
            for (int i = 0; i < this.cl_parse_entities.Length; i++)
            {
                this.cl_parse_entities[i] = new QShared.entity_state_t();
            }
            this.cl.cmds = new QShared.usercmd_t[CMD_BACKUP];
            for (int i = 0; i < this.cl.cmds.Length; i++)
            {
                this.cl.cmds[i].angles = new short[3];
            }
            this.cl.frame = new frame_t();
            this.cl.frame.playerstate.pmove.origin = new short[3];
            this.cl.frame.playerstate.pmove.velocity = new short[3];
            this.cl.frame.playerstate.pmove.delta_angles = new short[3];

            this.particles = new cparticle_t[QRef.MAX_PARTICLES];
            for (int i = 0; i < this.particles.Length; i++)
            {
                this.particles[i] = new cparticle_t();
            }
        }

        private client_state_t cl;
        private client_static_t cls;

        private cvar_t? rcon_client_password;
        private cvar_t? rcon_address;

        private cvar_t? cl_noskins;
        private cvar_t? cl_footsteps;
        private cvar_t? cl_timeout;
        private cvar_t? cl_predict;
        private cvar_t? cl_showfps;
        private cvar_t? cl_gun;
        private cvar_t? cl_add_particles;
        private cvar_t? cl_add_lights;
        private cvar_t? cl_add_entities;
        private cvar_t? cl_add_blend;
        private cvar_t? cl_kickangles;

        private cvar_t? cl_shownet;
        private cvar_t? cl_showmiss;
        private cvar_t? cl_showclamp;

        private cvar_t? cl_paused;
        private cvar_t? cl_loadpaused;

        private cvar_t? cl_lightlevel;
        private cvar_t? cl_r1q2_lightstyle;
        private cvar_t? cl_limitsparksounds;

        /* userinfo */
        private cvar_t? name;
        private cvar_t? skin;
        private cvar_t? rate;
        private cvar_t? fov;
        private cvar_t? horplus;
        private cvar_t? windowed_mouse;
        private cvar_t? msg;
        private cvar_t? hand;
        private cvar_t? gender;
        private cvar_t? gender_auto;

        private cvar_t? gl1_stereo;
        private cvar_t? gl1_stereo_separation;
        private cvar_t? gl1_stereo_convergence;

        private cvar_t? cl_vwep;

        // centity_t cl_entities[MAX_EDICTS];
        private centity_t[] cl_entities;

        // entity_state_t cl_parse_entities[MAX_PARSE_ENTITIES];
        private QShared.entity_state_t[] cl_parse_entities;

        private bool initPending;


        private void CL_ClearState()
        {
            // S_StopAllSounds();
            CL_ClearEffects();
            CL_ClearTEnts();

            /* wipe the entire cl structure */
            cl = new client_state_t();
            cl.configstrings = new string[QShared.MAX_CONFIGSTRINGS];
            cl.model_draw = new model_s?[QShared.MAX_MODELS];
            cl.model_clip = new QShared.cmodel_t?[QShared.MAX_MODELS];
            cl.frame = new frame_t();
            cl.frame.playerstate.pmove.origin = new short[3];
            cl.frame.playerstate.pmove.velocity = new short[3];
            cl.frame.playerstate.pmove.delta_angles = new short[3];
            cl.frames = new frame_t[QCommon.UPDATE_BACKUP];
            for (int i = 0; i < cl.frames.Length; i++)
            {
                cl.frames[i] = new frame_t();
            }
            cl.clientinfo = new clientinfo_t[QShared.MAX_CLIENTS];
            cl.cmds = new QShared.usercmd_t[CMD_BACKUP];
            for (int i = 0; i < cl.cmds.Length; i++)
            {
                cl.cmds[i].angles = new short[3];
            }
            cl.predicted_origins = new short[CMD_BACKUP][];
            for (int i = 0; i < cl.predicted_origins.Length; i++)
            {
                cl.predicted_origins[i] = new short[3];
            }

            cl_entities = new centity_t[QShared.MAX_EDICTS];
            for (int i = 0; i < cl_entities.Length; i++)
            {
                cl_entities[i] = new centity_t();
                cl_entities[i].baseline = new QShared.entity_state_t();
                cl_entities[i].current = new QShared.entity_state_t();
                cl_entities[i].prev = new QShared.entity_state_t();
            }

            cls.netchan.message.Clear();
        }

        private int precache_check;
        private int precache_spawncount;
        private int precache_tex;
        private int precache_model_skin;
        private byte[]? precache_model;

        /*
        * The server will send this command right
        * before allowing the client into the server
        */
        private void CL_Precache_f(string[] args)
        {
            /* Yet another hack to let old demos work */
            if (args.Length < 2)
            {
                uint map_checksum;    /* for detecting cheater maps */

                common.CM_LoadMap(cl.configstrings[QShared.CS_MODELS + 1], true, out map_checksum);
                // CL_RegisterSounds();
                CL_PrepRefresh();
                return;
            }

            precache_check = QShared.CS_MODELS;

            precache_spawncount = Int32.Parse(args[1]);
            precache_model = null;
            precache_model_skin = 0;

            CL_RequestNextDownload();
        }        

        private void CL_InitLocal()
        {
            cls.netchan = new QCommon.netchan_t(common);
            cls.state = connstate_t.ca_disconnected;
            cls.realtime = common.Sys_Milliseconds();

            CL_InitInput();

            /* register our variables */
            // cin_force43 = common.Cvar_Get("cin_force43", "1", 0);

            cl_add_blend = common.Cvar_Get("cl_blend", "1", 0);
            cl_add_lights = common.Cvar_Get("cl_lights", "1", 0);
            cl_add_particles = common.Cvar_Get("cl_particles", "1", 0);
            cl_add_entities = common.Cvar_Get("cl_entities", "1", 0);
            cl_kickangles = common.Cvar_Get("cl_kickangles", "1", 0);
            cl_gun = common.Cvar_Get("cl_gun", "2", cvar_t.CVAR_ARCHIVE);
            cl_footsteps = common.Cvar_Get("cl_footsteps", "1", 0);
            cl_noskins = common.Cvar_Get("cl_noskins", "0", 0);
            cl_predict = common.Cvar_Get("cl_predict", "1", 0);
            cl_showfps = common.Cvar_Get("cl_showfps", "0", cvar_t.CVAR_ARCHIVE);

            cl_upspeed = common.Cvar_Get("cl_upspeed", "200", 0);
            cl_forwardspeed = common.Cvar_Get("cl_forwardspeed", "200", 0);
            cl_sidespeed = common.Cvar_Get("cl_sidespeed", "200", 0);
            cl_yawspeed = common.Cvar_Get("cl_yawspeed", "140", 0);
            cl_pitchspeed = common.Cvar_Get("cl_pitchspeed", "150", 0);
            cl_anglespeedkey = common.Cvar_Get("cl_anglespeedkey", "1.5", 0);

            cl_run = common.Cvar_Get("cl_run", "0", cvar_t.CVAR_ARCHIVE);

            cl_shownet = common.Cvar_Get("cl_shownet", "0", 0);
            cl_showmiss = common.Cvar_Get("cl_showmiss", "0", 0);
            cl_showclamp = common.Cvar_Get("showclamp", "0", 0);
            cl_timeout = common.Cvar_Get("cl_timeout", "120", 0);
            cl_paused = common.Cvar_Get("paused", "0", 0);
            cl_loadpaused = common.Cvar_Get("cl_loadpaused", "1", cvar_t.CVAR_ARCHIVE);

            gl1_stereo = common.Cvar_Get( "gl1_stereo", "0", cvar_t.CVAR_ARCHIVE );
            gl1_stereo_separation = common.Cvar_Get( "gl1_stereo_separation", "1", cvar_t.CVAR_ARCHIVE );
            gl1_stereo_convergence = common.Cvar_Get( "gl1_stereo_convergence", "1.4", cvar_t.CVAR_ARCHIVE );

            rcon_client_password = common.Cvar_Get("rcon_password", "", 0);
            rcon_address = common.Cvar_Get("rcon_address", "", 0);

            cl_lightlevel = common.Cvar_Get("r_lightlevel", "0", 0);
            cl_r1q2_lightstyle = common.Cvar_Get("cl_r1q2_lightstyle", "1", cvar_t.CVAR_ARCHIVE);
            cl_limitsparksounds = common.Cvar_Get("cl_limitsparksounds", "0", cvar_t.CVAR_ARCHIVE);

            /* userinfo */
            name = common.Cvar_Get("name", "unnamed", cvar_t.CVAR_USERINFO | cvar_t.CVAR_ARCHIVE);
            skin = common.Cvar_Get("skin", "male/grunt", cvar_t.CVAR_USERINFO | cvar_t.CVAR_ARCHIVE);
            rate = common.Cvar_Get("rate", "8000", cvar_t.CVAR_USERINFO | cvar_t.CVAR_ARCHIVE);
            msg = common.Cvar_Get("msg", "1", cvar_t.CVAR_USERINFO | cvar_t.CVAR_ARCHIVE);
            hand = common.Cvar_Get("hand", "0", cvar_t.CVAR_USERINFO | cvar_t.CVAR_ARCHIVE);
            fov = common.Cvar_Get("fov", "90", cvar_t.CVAR_USERINFO | cvar_t.CVAR_ARCHIVE);
            horplus = common.Cvar_Get("horplus", "1", cvar_t.CVAR_ARCHIVE);
            windowed_mouse = common.Cvar_Get("windowed_mouse", "1", cvar_t.CVAR_USERINFO | cvar_t.CVAR_ARCHIVE);
            gender = common.Cvar_Get("gender", "male", cvar_t.CVAR_USERINFO | cvar_t.CVAR_ARCHIVE);
            gender_auto = common.Cvar_Get("gender_auto", "1", cvar_t.CVAR_ARCHIVE);
            gender!.modified = false;

            // USERINFO cvars are special, they just need to be registered
            common.Cvar_Get("password", "", cvar_t.CVAR_USERINFO);
            common.Cvar_Get("spectator", "0", cvar_t.CVAR_USERINFO);

            cl_vwep = common.Cvar_Get("cl_vwep", "1", cvar_t.CVAR_ARCHIVE);

        // #ifdef USE_CURL
        //     cl_http_proxy = Cvar_Get("cl_http_proxy", "", 0);
        //     cl_http_filelists = Cvar_Get("cl_http_filelists", "1", 0);
        //     cl_http_downloads = Cvar_Get("cl_http_downloads", "1", CVAR_ARCHIVE);
        //     cl_http_max_connections = Cvar_Get("cl_http_max_connections", "4", 0);
        //     cl_http_show_dw_progress = Cvar_Get("cl_http_show_dw_progress", "0", 0);
        //     cl_http_bw_limit_rate = Cvar_Get("cl_http_bw_limit_rate", "0", 0);
        //     cl_http_bw_limit_tmout = Cvar_Get("cl_http_bw_limit_tmout", "0", 0);
        // #endif

            /* register our commands */
            common.Cmd_AddCommand("cmd", CL_ForwardToServer_f);
        //     Cmd_AddCommand("pause", CL_Pause_f);
        //     Cmd_AddCommand("pingservers", CL_PingServers_f);
        //     Cmd_AddCommand("skins", CL_Skins_f);

        //     Cmd_AddCommand("userinfo", CL_Userinfo_f);
        //     Cmd_AddCommand("snd_restart", CL_Snd_Restart_f);

            common.Cmd_AddCommand("changing", CL_Changing_f);
        //     Cmd_AddCommand("disconnect", CL_Disconnect_f);
        //     Cmd_AddCommand("record", CL_Record_f);
        //     Cmd_AddCommand("stop", CL_Stop_f);

        //     Cmd_AddCommand("quit", CL_Quit_f);

        //     Cmd_AddCommand("connect", CL_Connect_f);
            common.Cmd_AddCommand("reconnect", CL_Reconnect_f);

        //     Cmd_AddCommand("rcon", CL_Rcon_f);

        //     Cmd_AddCommand("setenv", CL_Setenv_f);

            common.Cmd_AddCommand("precache", CL_Precache_f);

        //     Cmd_AddCommand("download", CL_Download_f);

        //     Cmd_AddCommand("currentmap", CL_CurrentMap_f);

            /* forward to server commands
            * the only thing this does is allow command completion
            * to work -- all unknown commands are automatically
            * forwarded to the server */
            common.Cmd_AddCommand("wave", null);
            common.Cmd_AddCommand("inven", null);
            common.Cmd_AddCommand("kill", null);
            common.Cmd_AddCommand("use", null);
            common.Cmd_AddCommand("drop", null);
            common.Cmd_AddCommand("say", null);
            common.Cmd_AddCommand("say_team", null);
            common.Cmd_AddCommand("info", null);
            common.Cmd_AddCommand("prog", null);
            common.Cmd_AddCommand("give", null);
            common.Cmd_AddCommand("god", null);
            common.Cmd_AddCommand("notarget", null);
            common.Cmd_AddCommand("noclip", null);
            common.Cmd_AddCommand("invuse", null);
            common.Cmd_AddCommand("invprev", null);
            common.Cmd_AddCommand("invnext", null);
            common.Cmd_AddCommand("invdrop", null);
            common.Cmd_AddCommand("weapnext", null);
            common.Cmd_AddCommand("weapprev", null);
            common.Cmd_AddCommand("listentities", null);
            common.Cmd_AddCommand("teleport", null);
            common.Cmd_AddCommand("cycleweap", null);
        }

        public void Frame(int packetdelta, int renderdelta, int timedelta, bool packetframe, bool renderframe)
        {
        //     static int lasttimecalled;
        //     // Dedicated?
        //     if (dedicated->value)
        //     {
        //         return;
        //     }
            if (initPending)
            {
                M_Init();
                initPending = false;
            }

            // Calculate simulation time.
            cls.nframetime = packetdelta / 1000000.0f;
            cls.rframetime = renderdelta / 1000000.0f;
            cls.realtime = common.curtime;
            cl.time += timedelta / 1000;

            // Don't extrapolate too far ahead.
            if (cls.nframetime > 0.5f)
            {
                cls.nframetime = 0.5f;
            }

            if (cls.rframetime > 0.5f)
            {
                cls.rframetime = 0.5f;
            }

        //     // if in the debugger last frame, don't timeout.
        //     if (timedelta > 5000000)
        //     {
        //         cls.netchan.last_received = Sys_Milliseconds();
        //     }

        //     // Reset power shield / power screen sound counter.
        //     num_power_sounds = 0;

            if (!common.cl_timedemo!.Bool)
            {
                // Don't throttle too much when connecting / loading.
                if ((cls.state == connstate_t.ca_connected) && (packetdelta > 100000))
                {
                    packetframe = true;
                }
            }

        //     // Run HTTP downloads more often while connecting.
        // #ifdef USE_CURL
        //     if (cls.state == ca_connected)
        //     {
        //         CL_RunHTTPDownloads();
        //     }
        // #endif

            // Update input stuff.
            if (packetframe || renderframe)
            {
                CL_ReadPackets();
        //         CL_UpdateWindowedMouse();
                input.Update();
                common.Cbuf_Execute();
        //         CL_FixCvarCheats();

                if (cls.state > connstate_t.ca_connecting)
                {
                    CL_RefreshCmd();
                }
                else
                {
                    CL_RefreshMove();
                }
            }

            if (cls.forcePacket || common.userinfo_modified)
            {
                packetframe = true;
                cls.forcePacket = false;
            }

            if (packetframe)
            {
                CL_SendCmd();
                CL_CheckForResend();

                // Run HTTP downloads during game.
        // #ifdef USE_CURL
        //         CL_RunHTTPDownloads();
        // #endif
            }

            if (renderframe)
            {
                vid.VID_CheckChanges();
                CL_PredictMovement();

                if (!cl.refresh_prepped && (cls.state == connstate_t.ca_active))
                {
                    CL_PrepRefresh();
                }

                /* update the screen */
                if (common.host_speeds!.Bool)
                {
                    common.time_before_ref = common.Sys_Milliseconds();
                }

                SCR_UpdateScreen();

                if (common.host_speeds!.Bool)
                {
                    common.time_after_ref = common.Sys_Milliseconds();
                }

        //         /* update audio */
        //         S_Update(cl.refdef.vieworg, cl.v_forward, cl.v_right, cl.v_up);

                /* advance local effects for next frame */
                CL_RunDLights();
                CL_RunLightStyles();
        //         SCR_RunCinematic();
        //         SCR_RunConsole();

                /* Update framecounter */
                cls.framecount++;

        //         if (log_stats->value)
        //         {
        //             if (cls.state == ca_active)
        //             {
        //                 if (!lasttimecalled)
        //                 {
        //                     lasttimecalled = Sys_Milliseconds();

        //                     if (log_stats_file)
        //                     {
        //                         fprintf(log_stats_file, "0\n");
        //                     }
        //                 }

        //                 else
        //                 {
        //                     int now = Sys_Milliseconds();

        //                     if (log_stats_file)
        //                     {
        //                         fprintf(log_stats_file, "%d\n", now - lasttimecalled);
        //                     }

        //                     lasttimecalled = now;
        //                 }
        //             }
        //         }
            }
        }

        public void Init()
        {
            if (common.dedicated?.Bool ?? false)
            {
                return; /* nothing running on the client */
            }

            initPending = true;

            /* all archived variables will now be loaded */
            Con_Init();

        //     S_Init();

            SCR_Init();

            vid.Init();

            V_Init();

        //     net_message.data = net_message_buffer;

        //     net_message.maxsize = sizeof(net_message_buffer);

        // #ifdef USE_CURL
        //     CL_InitHTTPDownloads();
        // #endif

            cls.disable_screen = 1.0f; /* don't draw yet */

            CL_InitLocal();

            common.Cbuf_Execute();

        //     Key_ReadConsoleHistory();
        }

        public void Start()
        {
            vid.Start();
        }

    }
}