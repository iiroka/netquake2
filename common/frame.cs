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
 * Platform independent initialization, main loop and frame handling.
 *
 * =======================================================================
 */

namespace Quake2 {

    partial class QCommon {

        public cvar_t? developer;
        public cvar_t? dedicated;
        public cvar_t? vid_maxfps;
        public cvar_t? host_speeds;
        public cvar_t? cl_maxfps;

        private QClient client;
        private QServer server;

        public int curtime;

        public QCommon()
        {
            client = new QClient(this);
            server = new QServer(this);

            QShared.provider.NumberDecimalSeparator = ".";
        }

        private void Qcommon_ExecConfigs(bool gameStartUp)
        {
            Cbuf_AddText("exec default.cfg\n");
            Cbuf_AddText("exec yq2.cfg\n");
            Cbuf_AddText("exec config.cfg\n");
            Cbuf_AddText("exec autoexec.cfg\n");

            if (gameStartUp)
            {
                /* Process cmd arguments only startup. */
                // Cbuf_AddEarlyCommands(true);
            }

            Cbuf_Execute();
        }

        public void Init(string[] args)
        {
            startTicks = DateTime.Now.Ticks;
            // Jump point used in emergency situations.
        //     if (setjmp(abortframe))
        //     {
        //         Sys_Error("Error during initialization");
        //     }

        //     if (checkForHelp(argc, argv))
        //     {
        //         // ok, --help or similar commandline option was given
        //         // and info was printed, exit the game now
        //         exit(1);
        //     }

        //     // Print the build and version string
        //     Qcommon_Buildstring();

        //     // Seed PRNG
        //     randk_seed();

            // Start early subsystems.
        //     COM_InitArgv(argc, argv);
        //     Swap_Init();
        //     Cbuf_Init();
            Cmd_Init();
            Cvar_Init();
            CM_Init();

            client.Key_Init();

            /* we need to add the early commands twice, because
            a basedir or cddir needs to be set before execing
            config files, but we want other parms to override
            the settings of the config files */
        //     Cbuf_AddEarlyCommands(false);
            Cbuf_Execute();

            // remember the initial game name that might have been set on commandline
        //     {
        //         cvar_t* gameCvar = Cvar_Get("game", "", CVAR_LATCH | CVAR_SERVERINFO);
        //         const char* game = "";

        //         if(gameCvar->string && gameCvar->string[0])
        //         {
        //             game = gameCvar->string;
        //         }

        //         Q_strlcpy(userGivenGame, game, sizeof(userGivenGame));
        //     }

            // The filesystems needs to be initialized after the cvars.
            FS_InitFilesystem();

            // Add and execute configuration files.
            Qcommon_ExecConfigs(true);

            // Zone malloc statistics.
        //     Cmd_AddCommand("z_stats", Z_Stats_f);

            // cvars

            cl_maxfps = Cvar_Get("cl_maxfps", "-1", cvar_t.CVAR_ARCHIVE);

            developer = Cvar_Get("developer", "0", 0);
        //     fixedtime = Cvar_Get("fixedtime", "0", 0);

        //     logfile_active = Cvar_Get("logfile", "1", CVAR_ARCHIVE);
        //     modder = Cvar_Get("modder", "0", 0);
        //     timescale = Cvar_Get("timescale", "1", 0);

        //     char *s;
        //     s = va("%s %s %s %s", YQ2VERSION, YQ2ARCH, BUILD_DATE, YQ2OSTYPE);
        //     Cvar_Get("version", s, CVAR_SERVERINFO | CVAR_NOSET);

        // #ifndef DEDICATED_ONLY
        //     busywait = Cvar_Get("busywait", "1", CVAR_ARCHIVE);
        //     cl_async = Cvar_Get("cl_async", "1", CVAR_ARCHIVE);
        //     cl_timedemo = Cvar_Get("timedemo", "0", 0);
            dedicated = Cvar_Get("dedicated", "0", cvar_t.CVAR_NOSET);
            vid_maxfps = Cvar_Get("vid_maxfps", "300", cvar_t.CVAR_ARCHIVE);
            host_speeds = Cvar_Get("host_speeds", "0", 0);
        //     log_stats = Cvar_Get("log_stats", "0", 0);
        //     showtrace = Cvar_Get("showtrace", "0", 0);
        // #else
        //     dedicated = Cvar_Get("dedicated", "1", CVAR_NOSET);
        // #endif

            // We can't use the clients "quit" command when running dedicated.
        //     if (dedicated->value)
        //     {
        //         Cmd_AddCommand("quit", Com_Quit);
        //     }

            // Start late subsystem.
        //     Sys_Init();
            NET_Init();
            Netchan_Init();
            server.Init();
        // #ifndef DEDICATED_ONLY
            client.Init();
        // #endif

            // Everythings up, let's add + cmds from command line.
        //     if (!Cbuf_AddLateCommands())
        //     {
                if (!(dedicated?.Bool ?? false))
                {
                    // Start demo loop...
                    Cbuf_AddText("d1\n");
                }
                else
                {
                    // ...or dedicated server.
                    Cbuf_AddText("dedicated_start\n");
                }

                Cbuf_Execute();
        //     }
        // #ifndef DEDICATED_ONLY
        //     else
        //     {
        //         /* the user asked for something explicit
        //         so drop the loading plaque */
        //         SCR_EndLoadingPlaque();
        //     }
        // #endif

            Com_Printf("==== Yamagi Quake II Initialized ====\n\n");
            Com_Printf("*************************************\n\n");

            // Call the main loop
        //     Qcommon_Mainloop();
            client.Start();
        }

        // Time since last packetframe in microsec.
        private int packetdelta = 1000000;

        // Time since last renderframe in microsec.
        private int renderdelta = 1000000;

        // Accumulated time since last client run.
        private int clienttimedelta = 0;

        // Accumulated time since last server run.
        private int servertimedelta = 0;

        public void Qcommon_Frame(double sec)
        {
            int usec = (int)(sec * 1000000);

            curtime = Sys_Milliseconds();
            // Used for the dedicated server console.
            // char *s;

            // Statistics.
            int time_before = 0;
            int time_between = 0;
            int time_after;

            // Target packetframerate.
            float pfps;

            // Target renderframerate.
            float rfps;

            /* A packetframe runs the server and the client,
            but not the renderer. The minimal interval of
            packetframes is about 10.000 microsec. If run
            more often the movement prediction in pmove.c
            breaks. That's the Q2 variant if the famous
            125hz bug. */
            bool packetframe = true;

            /* A rendererframe runs the renderer, but not the
            client or the server. The minimal interval is
            about 1000 microseconds. */
            bool renderframe = true;


            // /* Tells the client to shutdown.
            // Used by the signal handlers. */
            // if (quitnextframe)
            // {
            //     Cbuf_AddText("quit");
            // }


            // /* In case of ERR_DROP we're jumping here. Don't know
            // if that's really save but it seems to work. So leave
            // it alone. */
            // if (setjmp(abortframe))
            // {
            //     return;
            // }


            // if (log_stats->modified)
            // {
            //     log_stats->modified = false;

            //     if (log_stats->value)
            //     {
            //         if (log_stats_file)
            //         {
            //             fclose(log_stats_file);
            //             log_stats_file = 0;
            //         }

            //         log_stats_file = Q_fopen("stats.log", "w");

            //         if (log_stats_file)
            //         {
            //             fprintf(log_stats_file, "entities,dlights,parts,frame time\n");
            //         }
            //     }
            //     else
            //     {
            //         if (log_stats_file)
            //         {
            //             fclose(log_stats_file);
            //             log_stats_file = 0;
            //         }
            //     }
            // }


            // // Timing debug crap. Just for historical reasons.
            // if (fixedtime->value)
            // {
            //     usec = (int)fixedtime->value;
            // }
            // else if (timescale->value)
            // {
            //     usec *= timescale->value;
            // }


            // if (showtrace->value)
            // {
            //     extern int c_traces, c_brush_traces;
            //     extern int c_pointcontents;

            //     Com_Printf("%4i traces  %4i points\n", c_traces, c_pointcontents);
            //     c_traces = 0;
            //     c_brush_traces = 0;
            //     c_pointcontents = 0;
            // }


            // /* We can render 1000 frames at maximum, because the minimum
            // frametime of the client is 1 millisecond. And of course we
            // need to render something, the framerate can never be less
            // then 1. Cap vid_maxfps between 1 and 999. */
            if (vid_maxfps!.Int > 999 || vid_maxfps!.Int < 1)
            {
                Cvar_Set("vid_maxfps", "999");
            }

            if (cl_maxfps!.Int > 250)
            {
                Cvar_Set("cl_maxfps", "250");
            }

            // // Calculate target and renderframerate.
            // if (R_IsVSyncActive())
            // {
            //     int refreshrate = GLimp_GetRefreshRate();

            //     // using refreshRate - 2, because targeting a value slightly below the
            //     // (possibly not 100% correctly reported) refreshRate would introduce jittering, so only
            //     // use vid_maxfps if it looks like the user really means it to be different from refreshRate
            //     if (vid_maxfps->value < refreshrate - 2 )
            //     {
            //         rfps = vid_maxfps->value;
            //         // we can't have more packet frames than render frames, so limit pfps to rfps
            //         pfps = (cl_maxfps->value > rfps) ? rfps : cl_maxfps->value;
            //     }
            //     else // target refresh rate, not vid_maxfps
            //     {
            //         /* if vsync is active, we increase the target framerate a bit for two reasons
            //         1. On Windows, GLimp_GetFrefreshRate() (or the SDL counterpart, or even
            //             the underlying WinAPI function) often returns a too small value,
            //             like 58 or 59 when it's really 59.95 and thus (as integer) should be 60
            //         2. vsync will throttle us to refreshrate anyway, so there is no harm
            //             in starting the frame *a bit* earlier, instead of risking starting
            //             it too late */
            //         rfps = refreshrate * 1.2f;
            //         // we can't have more packet frames than render frames, so limit pfps to rfps
            //         // but in this case use tolerance for comparison and assign rfps with tolerance
            //         pfps = (cl_maxfps->value < refreshrate - 2) ? cl_maxfps->value : rfps;
            //     }
            // }
            // else
            // {
                rfps = vid_maxfps!.Float;
                // we can't have more packet frames than render frames, so limit pfps to rfps
                pfps = (cl_maxfps!.Float > rfps) ? rfps : cl_maxfps!.Float;
            // }

            // cl_maxfps <= 0 means: automatically choose a packet framerate that should work
            // well with the render framerate, which is the case if rfps is a multiple of pfps
            // if (cl_maxfps!.Float <= 0.0f && cl_async!.Float != 0.0f)
            // {
            //     // packet framerates between about 45 and 90 should be ok,
            //     // with other values the game (esp. movement/clipping) can become glitchy
            //     // as pfps must be <= rfps, for rfps < 90 just use that as pfps
            //     if (rfps < 90.0f)
            //     {
            //         pfps = rfps;
            //     }
            //     else
            //     {
            //         /* we want an integer divider, so every div-th renderframe is a packetframe.
            //         this formula gives nice dividers that keep pfps as close as possible
            //         to 60 (which seems to be ideal):
            //         - for < 150 rfps div will be 2, so pfps will be between 45 and ~75
            //             => exactly every second renderframe we also run a packetframe
            //         - for < 210 rfps div will be 3, so pfps will be between 50 and ~70
            //             => exactly every third renderframe we also run a packetframe
            //         - etc, the higher the rfps, the closer the pfps-range will be to 60
            //             (and you probably get the very best results by running at a
            //             render framerate that's a multiple of 60) */
            //         float div = round(rfps/60);
            //         pfps = rfps/div;
            //     }
            // }

            // Calculate timings.
            packetdelta += usec;
            renderdelta += usec;
            clienttimedelta += usec;
            servertimedelta += usec;

            // if (!cl_timedemo->value)
            // {
            //     if (cl_async->value)
            //     {
            //         // Render frames.
            //         if (renderdelta < (1000000.0f / rfps))
            //         {
            //             renderframe = false;
            //         }

            //         // Network frames.
            //         float packettargetdelta = 1000000.0f / pfps;
            //         // "packetdelta + renderdelta/2 >= packettargetdelta" if now we're
            //         // closer to when we want to run the next packetframe than we'd
            //         // (probably) be after the next render frame
            //         // also, we only run packetframes together with renderframes,
            //         // because we must have at least one render frame between two packet frames
            //         // TODO: does it make sense to use the average renderdelta of the last X frames
            //         //       instead of just the last renderdelta?
            //         if (!renderframe || packetdelta + renderdelta/2 < packettargetdelta)
            //         {
            //             packetframe = false;
            //         }
            //     }
            //     else
            //     {
                    // Cap frames at target framerate.
                    if (renderdelta < (1000000.0f / rfps)) {
                        renderframe = false;
                        packetframe = false;
                    }
            //     }
            // }

            // // Dedicated server terminal console.
            // do {
            //     s = Sys_ConsoleInput();

            //     if (s) {
            //         Cbuf_AddText(va("%s\n", s));
            //     }
            // } while (s);

            Cbuf_Execute();


            // if (host_speeds->value)
            // {
            //     time_before = Sys_Milliseconds();
            // }


            // Run the serverframe.
            if (packetframe) {
                server.Frame(servertimedelta);
                servertimedelta = 0;
            }


            // if (host_speeds->value)
            // {
            //     time_between = Sys_Milliseconds();
            // }


            // Run the client frame.
            if (packetframe || renderframe) {
                client.Frame(packetdelta, renderdelta, clienttimedelta, packetframe, renderframe);
                clienttimedelta = 0;
            }


            // if (host_speeds->value)
            // {
            //     int all, sv, gm, cl, rf;

            //     time_after = Sys_Milliseconds();
            //     all = time_after - time_before;
            //     sv = time_between - time_before;
            //     cl = time_after - time_between;
            //     gm = time_after_game - time_before_game;
            //     rf = time_after_ref - time_before_ref;
            //     sv -= gm;
            //     cl -= rf;
            //     Com_Printf("all:%3i sv:%3i gm:%3i cl:%3i rf:%3i\n", all, sv, gm, cl, rf);
            // }


            // Reset deltas and mark frame.
            if (packetframe) {
                packetdelta = 0;
            }

            if (renderframe) {
                renderdelta = 0;
            }
        }
    }
}
