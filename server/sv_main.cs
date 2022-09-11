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
 * Server main function and correspondig stuff
 *
 * =======================================================================
 */

namespace Quake2 {

    partial class QServer {

        private QCommon common;

        public QServer(QCommon common)
        {
            this.common = common;
        }

        private cvar_t? sv_paused;
        private cvar_t? sv_timedemo;
        private cvar_t? sv_enforcetime;
        private cvar_t? timeout; /* seconds without any message */
        private cvar_t? zombietime; /* seconds to sink messages after disconnect */
        private cvar_t? rcon_password; /* password for remote server commands */
        private cvar_t? allow_download;
        private cvar_t? allow_download_players;
        private cvar_t? allow_download_models;
        private cvar_t? allow_download_sounds;
        private cvar_t? allow_download_maps;
        private cvar_t? sv_airaccelerate;
        private cvar_t? sv_noreload; /* don't reload level state when reentering */
        private cvar_t? maxclients; /* rename sv_maxclients */
        private cvar_t? sv_showclamp;
        private cvar_t? hostname;
        private cvar_t? public_server; /* should heartbeats be sent */
        private cvar_t? sv_entfile; /* External entity files. */
        private cvar_t? sv_downloadserver; /* Download server. */

        private void SV_ReadPackets()
        {
            var from = new QCommon.netadr_t();
            while (true)
            {
                var msg = common.NET_GetPacket(QCommon.netsrc_t.NS_SERVER, ref from);
                if (msg == null)
                {
                    break;
                }
                /* check for connectionless packet (0xffffffff) first */
                if (BitConverter.ToInt32(msg) == -1)
                {
                    SV_ConnectionlessPacket(new QReadbuf(msg), from);
                    continue;
                }

                var msgb = new QReadbuf(msg);
                /* read the qport out of the message so we can fix up
                    stupid address translating routers */
                msgb.BeginReading();
                msgb.ReadLong(); /* sequence number */
                msgb.ReadLong(); /* sequence number */
                var qport = msgb.ReadShort() & 0xffff;

                /* check for packets from connected clients */
                for (int i = 0; i < svs.clients.Length; i++)
                {
                    if (svs.clients[i].state == client_state_t.cs_free)
                    {
                        continue;
                    }

            //         if (!NET_CompareBaseAdr(net_from, cl->netchan.remote_address))
            //         {
            //             continue;
            //         }

                    if (svs.clients[i].netchan.qport != qport)
                    {
                        continue;
                    }

            //         if (cl->netchan.remote_address.port != net_from.port)
            //         {
            //             Com_Printf("SV_ReadPackets: fixing up a translated port\n");
            //             cl->netchan.remote_address.port = net_from.port;
            //         }

                    if (svs.clients[i].netchan.Process(ref msgb))
                    {
                        /* this is a valid, sequenced packet, so process it */
                        if (svs.clients[i].state != client_state_t.cs_zombie)
                        {
                            svs.clients[i].lastmessage = svs.realtime; /* don't timeout */

                            if (!(sv.demofile != null && (sv.state == server_state_t.ss_demo)))
                            {
                                SV_ExecuteClientMessage(ref msgb, ref svs.clients[i]);
                            }
                        }
                    }

                    break;
                }

            //     if (i != maxclients->value)
            //     {
            //         continue;
            //     }
            }
        }

        private void SV_RunGameFrame()
        {
            // if (host_speeds->value)
            // {
            //     time_before_game = Sys_Milliseconds();
            // }

            /* we always need to bump framenum, even if we
            don't run the world, otherwise the delta
            compression can get confused when a client
            has the "current" frame */
            sv.framenum++;
            sv.time = (uint)(sv.framenum * 100);

            /* don't run if paused */
            if (!(sv_paused?.Bool ?? false) || (maxclients!.Int > 1))
            {
                // ge->RunFrame();

                /* never get more than one tic behind */
                if (sv.time < svs.realtime)
                {
                    if (sv_showclamp?.Bool ?? false)
                    {
                        common.Com_Printf("sv highclamp\n");
                    }

                    svs.realtime = (int)sv.time;
                }
            }

            // if (host_speeds->value)
            // {
            //     time_after_game = Sys_Milliseconds();
            // }
        }

        public void Frame(int usec)
        {
        // #ifndef DEDICATED_ONLY
        //     time_before_game = time_after_game = 0;
        // #endif

            /* if server is not active, do nothing */
            if (!svs.initialized)
            {
                return;
            }

            svs.realtime += usec / 1000;

            /* keep the random time dependent */
            QShared.rand.Next();

            /* check timeouts */
            // SV_CheckTimeouts();

            /* get packets from clients */
            SV_ReadPackets();

            /* move autonomous things around if enough time has passed */
            if (!(sv_timedemo?.Bool ?? false) && (svs.realtime < sv.time))
            {
                /* never let the time get too far off */
                if (sv.time - svs.realtime > 100)
                {
                    if (sv_showclamp?.Bool ?? false)
                    {
                        common.Com_Printf("sv lowclamp\n");
                    }

                    svs.realtime = (int)sv.time - 100;
                }

            //     NET_Sleep(sv.time - svs.realtime);
                return;
            }

            /* update ping based on the last known frame from all clients */
            // SV_CalcPings();

            /* give the clients some timeslices */
            // SV_GiveMsec();

            /* let everything in the world think and move */
            SV_RunGameFrame();

            /* send messages back to the clients that had packets read this frame */
            SV_SendClientMessages();

            /* save the entire world state if recording a serverdemo */
            // SV_RecordDemoMessage();

            /* send a heartbeat to the master if needed */
            // Master_Heartbeat();

            /* clear teleport flags, etc for next frame */
            // SV_PrepWorldFrame();
        }

        /*
        * Pull specific info from a newly changed userinfo string
        * into a more C freindly form.
        */
        private void SV_UserinfoChanged(ref client_t cl)
        {
            // char *val;
            // int i;

            // /* call prog code to allow overrides */
            // ge->ClientUserinfoChanged(cl->edict, cl->userinfo);

            /* name for C code */
            cl.name = QShared.Info_ValueForKey(cl.userinfo, "name");

            // /* mask off high bit */
            // for (i = 0; i < sizeof(cl->name); i++)
            // {
            //     cl->name[i] &= 127;
            // }

            // /* rate command */
            // val = Info_ValueForKey(cl->userinfo, "rate");

            // if (strlen(val))
            // {
            //     i = (int)strtol(val, (char **)NULL, 10);
            //     cl->rate = i;

            //     if (cl->rate < 100)
            //     {
            //         cl->rate = 100;
            //     }

            //     if (cl->rate > 15000)
            //     {
            //         cl->rate = 15000;
            //     }
            // }
            // else
            // {
            //     cl->rate = 5000;
            // }

            // /* msg command */
            // val = Info_ValueForKey(cl->userinfo, "msg");

            // if (strlen(val))
            // {
            //     cl->messagelevel = (int)strtol(val, (char **)NULL, 10);
            // }
        }

        /*
        * Only called at quake2.exe startup, not for each game
        */
        public void Init()
        {
            InitOperatorCommands();

            rcon_password = common.Cvar_Get("rcon_password", "", 0);
            common.Cvar_Get("skill", "1", 0);
            common.Cvar_Get("singleplayer", "0", cvar_t.CVAR_SERVERINFO | cvar_t.CVAR_LATCH);
            common.Cvar_Get("deathmatch", "0", cvar_t.CVAR_SERVERINFO | cvar_t.CVAR_LATCH);
            common.Cvar_Get("coop", "0", cvar_t.CVAR_SERVERINFO | cvar_t.CVAR_LATCH);
            // common.Cvar_Get("dmflags", va("%i", DF_INSTANT_ITEMS), cvar_t.CVAR_SERVERINFO);
            common.Cvar_Get("fraglimit", "0", cvar_t.CVAR_SERVERINFO);
            common.Cvar_Get("timelimit", "0", cvar_t.CVAR_SERVERINFO);
            common.Cvar_Get("cheats", "0", cvar_t.CVAR_SERVERINFO | cvar_t.CVAR_LATCH);
            common.Cvar_Get("protocol", QCommon.PROTOCOL_VERSION.ToString(), cvar_t.CVAR_SERVERINFO | cvar_t.CVAR_NOSET);
            maxclients = common.Cvar_Get("maxclients", "1", cvar_t.CVAR_SERVERINFO | cvar_t.CVAR_LATCH);
            hostname = common.Cvar_Get("hostname", "noname", cvar_t.CVAR_SERVERINFO | cvar_t.CVAR_ARCHIVE);
            timeout = common.Cvar_Get("timeout", "125", 0);
            zombietime = common.Cvar_Get("zombietime", "2", 0);
            sv_showclamp = common.Cvar_Get("showclamp", "0", 0);
            sv_paused = common.Cvar_Get("paused", "0", 0);
            sv_timedemo = common.Cvar_Get("timedemo", "0", 0);
            sv_enforcetime = common.Cvar_Get("sv_enforcetime", "0", 0);
            allow_download = common.Cvar_Get("allow_download", "1", cvar_t.CVAR_ARCHIVE);
            allow_download_players = common.Cvar_Get("allow_download_players", "0", cvar_t.CVAR_ARCHIVE);
            allow_download_models = common.Cvar_Get("allow_download_models", "1", cvar_t.CVAR_ARCHIVE);
            allow_download_sounds = common.Cvar_Get("allow_download_sounds", "1", cvar_t.CVAR_ARCHIVE);
            allow_download_maps = common.Cvar_Get("allow_download_maps", "1", cvar_t.CVAR_ARCHIVE);
            sv_downloadserver = common.Cvar_Get ("sv_downloadserver", "", 0);

            sv_noreload = common.Cvar_Get("sv_noreload", "0", 0);

            sv_airaccelerate = common.Cvar_Get("sv_airaccelerate", "0", cvar_t.CVAR_LATCH);

            public_server = common.Cvar_Get("public", "0", 0);

            sv_entfile = common.Cvar_Get("sv_entfile", "1", cvar_t.CVAR_ARCHIVE);

            // SZ_Init(&net_message, net_message_buffer, sizeof(net_message_buffer));
        }

        /*
        * Used by SV_Shutdown to send a final message to all
        * connected clients before the server goes down. The 
        * messages are sent immediately, not just stuck on the
        * outgoing message list, because the server is going
        * to totally exit after returning from this function.
        */
        private void SV_FinalMessage(string message, bool reconnect)
        {
            var msg = new QWritebuf(QCommon.MAX_MSGLEN);
            msg.WriteByte((int)QCommon.svc_ops_e.svc_print);
            msg.WriteByte(QShared.PRINT_HIGH);
            msg.WriteString(message);

            if (reconnect)
            {
                msg.WriteByte((int)QCommon.svc_ops_e.svc_reconnect);
            }
            else
            {
                msg.WriteByte((int)QCommon.svc_ops_e.svc_disconnect);
            }

            /* stagger the packets to crutch operating system limited buffers */
            /* DG: we can't just use the maxclients cvar here for the number of clients,
            *     because this is called by SV_Shutdown() and the shut down server might have
            *     a different number of clients (e.g. 1 if it's single player), when maxclients
            *     has already been set to a higher value for multiplayer (e.g. 4 for coop)
            *     Luckily, svs.num_client_entities = maxclients->value * UPDATE_BACKUP * 64;
            *     with the maxclients value from when the current server was started (see SV_InitGame())
            *     so we can just calculate the right number of clients from that
            */
            // int numClients = svs.num_client_entities / ( UPDATE_BACKUP * 64 );
            for (int i = 0; i < svs.clients.Length; i++)
            {
                if (svs.clients[i].state >= client_state_t.cs_connected)
                {
                    svs.clients[i].netchan.Transmit(msg.Data);
                }
            }

            for (int i = 0; i < svs.clients.Length; i++)
            {
                if (svs.clients[i].state >= client_state_t.cs_connected)
                {
                    svs.clients[i].netchan.Transmit(msg.Data);
                }
            }
        }

        /*
        * Called when each game quits,
        * before Sys_Quit or Sys_Error
        */
        public void SV_Shutdown(string finalmsg, bool reconnect)
        {
            if (svs.clients != null)
            {
                SV_FinalMessage(finalmsg, reconnect);
            }

            // Master_Shutdown();
            // SV_ShutdownGameProgs();

            /* free current level */
            sv.demofile?.Close();

            sv = new server_t();
            sv.configstrings = new string[QShared.MAX_CONFIGSTRINGS];
            sv.multicast = new QWritebuf(QCommon.MAX_MSGLEN);
            common.ServerState = (int)sv.state;

            // /* free server static data */
            // if (svs.clients)
            // {
            //     Z_Free(svs.clients);
            // }

            // if (svs.client_entities)
            // {
            //     Z_Free(svs.client_entities);
            // }

            // if (svs.demofile)
            // {
            //     fclose(svs.demofile);
            // }

            svs = new server_static_t();
        }


    }
}