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
 * This file implements generic network functions
 *
 * =======================================================================
 */
using System.Text;

namespace Quake2 {

    partial class QClient {

        /*
        * Goes from a connected state to full screen
        * console state Sends a disconnect message to
        * the server This is also called on Com_Error, so
        * it shouldn't cause any errors
        */
        private void CL_Disconnect()
        {
            // byte final[32];

            if (cls.state == connstate_t.ca_disconnected)
            {
                return;
            }

        //     if (cl_timedemo && cl_timedemo->value)
        //     {
        //         int time;

        //         time = Sys_Milliseconds() - cl.timedemo_start;

        //         if (time > 0)
        //         {
        //             Com_Printf("%i frames, %3.1f seconds: %3.1f fps\n",
        //                     cl.timedemo_frames, time / 1000.0,
        //                     cl.timedemo_frames * 1000.0 / time);
        //         }
        //     }

        //     VectorClear(cl.refdef.blend);

        //     R_SetPalette(NULL);

        //     M_ForceMenuOff();

            cls.connect_time = 0;

        //     SCR_StopCinematic();

        //     OGG_Stop();

        //     if (cls.demorecording)
        //     {
        //         CL_Stop_f();
        //     }

            /* send a disconnect message to the server */
            var final = new QWritebuf(32);
            final.WriteByte((int)QCommon.clc_ops_e.clc_stringcmd);
            final.WriteString("disconnect");

            cls.netchan.Transmit(final.Data);
            cls.netchan.Transmit(final.Data);
            cls.netchan.Transmit(final.Data);

        //     CL_ClearState();

        //     /* stop file download */
        //     if (cls.download)
        //     {
        //         fclose(cls.download);
        //         cls.download = NULL;
        //     }

        // #ifdef USE_CURL
        //     CL_CancelHTTPDownloads(true);
        //     cls.downloadReferer[0] = 0;
        //     cls.downloadname[0] = 0;
        //     cls.downloadposition = 0;
        // #endif

            cls.state = connstate_t.ca_disconnected;

        //     snd_is_underwater = false;

        //     // save config for old game/mod
        //     CL_WriteConfiguration();

        //     // we disconnected, so revert to default game/mod (might have been different mod on MP server)
        //     Cvar_Set("game", userGivenGame);
        }

        /*
        * Just sent as a hint to the client that they should
        * drop to full console
        */
        private void CL_Changing_f(string[] args)
        {
            /* if we are downloading, we don't change!
            This so we don't suddenly stop downloading a map */
            // if (cls.download)
            // {
            //     return;
            // }

            // SCR_BeginLoadingPlaque();
            cls.state = connstate_t.ca_connected; /* not active anymore, but not disconnected */
            common.Com_Printf("\nChanging map...\n");

        // #ifdef USE_CURL
        //     if (cls.downloadServerRetry[0] != 0)
        //     {
        //         CL_SetHTTPServer(cls.downloadServerRetry);
        //     }
        // #endif
        }

        /*
        * The server is changing levels
        */
        private void CL_Reconnect_f(string[] args)
        {
            /* if we are downloading, we don't change!
            This so we don't suddenly stop downloading a map */
            // if (cls.download)
            // {
            //     return;
            // }

            // S_StopAllSounds();

            if (cls.state == connstate_t.ca_connected)
            {
                common.Com_Printf("reconnecting...\n");
                cls.state = connstate_t.ca_connected;
                cls.netchan.message.WriteChar((int)QCommon.clc_ops_e.clc_stringcmd);
                cls.netchan.message.WriteString("new");
                return;
            }

            if (!String.IsNullOrEmpty(cls.servername))
            {
                if (cls.state >= connstate_t.ca_connected)
                {
                    CL_Disconnect();
                    cls.connect_time = cls.realtime - 1500;
                }

                else
                {
                    cls.connect_time = -99999; /* Hack: fire immediately */
                }

                cls.state = connstate_t.ca_connecting;

                common.Com_Printf("reconnecting...\n");
            }
        }

        private void CL_ForwardToServer_f(string[] args)
        {
            if ((cls.state != connstate_t.ca_connected) && (cls.state != connstate_t.ca_active))
            {
                common.Com_Printf($"Can't \"{args[0]}\", not connected\n");
                return;
            }

            /* don't forward the first argument */
            if (args.Length > 1)
            {
                cls.netchan.message.WriteByte((int)QCommon.clc_ops_e.clc_stringcmd);
                var sb = new StringBuilder();
                for (int i = 1; i < args.Length; i++)
                {
                    sb.Append(args[i]);
                    if (i < (args.Length - 1))
                        sb.Append(" ");
                }
                cls.netchan.message.Print(sb.ToString());
            }
        }
        /*
        * Called after an ERR_DROP was thrown
        */
        public void CL_Drop()
        {
            if (cls.state == connstate_t.ca_uninitialized)
            {
                return;
            }

            if (cls.state == connstate_t.ca_disconnected)
            {
                return;
            }

            CL_Disconnect();

            /* drop loading plaque unless this is the initial game start */
            if (cls.disable_servercount != -1)
            {
                SCR_EndLoadingPlaque();  /* get rid of loading plaque */
            }
        }


        /*
        * We have gotten a challenge from the server, so try and
        * connect.
        */
        private void CL_SendConnectPacket()
        {
            QCommon.netadr_t? adr = QCommon.netadr_t.FromString(cls.servername);
            if (adr == null)
            {
                common.Com_Printf("Bad server address\n");
                cls.connect_time = 0;
                return;
            }

            if (adr.port == 0)
            {
                adr!.port = (ushort)QCommon.PORT_SERVER;
            }

            var port = common.Cvar_VariableInt("qport");

            common.userinfo_modified = false;

            common.Netchan_OutOfBandPrint(QCommon.netsrc_t.NS_CLIENT, adr, $"connect {QCommon.PROTOCOL_VERSION} {port} {cls.challenge} \"{common.Cvar_Userinfo()}\"\n");
        }

        /*
        * Resend a connect message if the last one has timed out
        */
        private void CL_CheckForResend()
        {
            /* if the local server is running and we aren't just connect */
            if ((cls.state == connstate_t.ca_disconnected) && common.ServerState != 0)
            {
                cls.state = connstate_t.ca_connecting;
                cls.servername = "localhost";
                /* we don't need a challenge on the localhost */
                CL_SendConnectPacket();
                return;
            }

            /* resend if we haven't gotten a reply yet */
            if (cls.state != connstate_t.ca_connecting)
            {
                return;
            }

            if (cls.realtime - cls.connect_time < 3000)
            {
                return;
            }


            QCommon.netadr_t? adr = QCommon.netadr_t.FromString(cls.servername);
            if (adr == null)
            {
                common.Com_Printf("Bad server address\n");
                cls.connect_time = 0;
                return;
            }

            if (adr.port == 0)
            {
                adr.port = (ushort)QCommon.PORT_SERVER;
            }

            cls.connect_time = cls.realtime;

            common.Com_Printf($"Connecting to {cls.servername}...\n");

            common.Netchan_OutOfBandPrint(QCommon.netsrc_t.NS_CLIENT, adr, "getchallenge\n");
        }

        /*
        * Responses to broadcasts, etc
        */
        private void CL_ConnectionlessPacket(QReadbuf msg, in QCommon.netadr_t from)
        {
            msg.BeginReading();
            msg.ReadLong(); /* skip the -1 */

            var s = msg.ReadStringLine();

            var args = common.Cmd_TokenizeString(s, false);

            // c = Cmd_Argv(0);

            common.Com_Printf($"{from}: {args[0]}\n");

            /* server connection */
            if (args[0].Equals("client_connect"))
            {
                if (cls.state == connstate_t.ca_connected)
                {
                    common.Com_Printf("Dup connect received.  Ignored.\n");
                    return;
                }

                cls.netchan.Setup(QCommon.netsrc_t.NS_CLIENT, from, cls.quakePort);
        //         char *buff = NET_AdrToString(cls.netchan.remote_address);

        //         for(int i = 1; i < Cmd_Argc(); i++)
        //         {
        //             char *p = Cmd_Argv(i);

        //             if(!strncmp(p, "dlserver=", 9))
        //             {
        // #ifdef USE_CURL
        //                 p += 9;
        //                 Com_sprintf(cls.downloadReferer, sizeof(cls.downloadReferer), "quake2://%s", buff);
        //                 CL_SetHTTPServer (p);

        //                 if (cls.downloadServer[0])
        //                 {
        //                     Com_Printf("HTTP downloading enabled, URL: %s\n", cls.downloadServer);
        //                 }
        // #else
        //                 Com_Printf("HTTP downloading supported by server but not the client.\n");
        // #endif
        //             }
        //         }

                /* Put client into pause mode when connecting to a local server.
                This prevents the world from being forwarded while the client
                is connecting, loading assets, etc. It's not 100%, there're
                still 4 world frames (for baseq2) processed in the game and
                100 frames by the server if the player enters a level that he
                or she already visited. In practise both shouldn't be a big
                problem. 4 frames are hardly enough for monsters staring to
                attack and in most levels the starting area in unreachable by
                monsters and free from environmental effects.

                Com_Serverstate() returns 2 if the server is local and we're
                running a real game and no timedemo, cinematic, etc. The 2 is
                taken from the server_state_t enum value 'ss_game'. If it's a
                local server, maxclients aus either 0 (for single player), or
                2 to 8 (coop and deathmatch) if we're reaching this code.
                For remote servers it's always 1. So this should trigger only
                if it's a local single player server.

                Since the player can load savegames from a paused state (e.g.
                through the console) we'll need to communicate if we entered
                paused mode (and it should left as soon as the player joined
                the server) or if it was already there.

                Last but not least this can be disabled by cl_loadpaused 0. */
        //         if (Com_ServerState() == 2 && (Cvar_VariableValue("maxclients") <= 1))
        //         {
        //             if (cl_loadpaused->value)
        //             {
        //                 if (!cl_paused->value)
        //                 {
        //                     paused_at_load = true;
        //                     Cvar_Set("paused", "1");
        //                 }
        //             }
        //         }

                cls.netchan.message.WriteChar((int)QCommon.clc_ops_e.clc_stringcmd);
                cls.netchan.message.WriteString("new");
                cls.state = connstate_t.ca_connected;
                return;
            }

        //     /* server responding to a status broadcast */
        //     if (!strcmp(c, "info"))
        //     {
        //         CL_ParseStatusMessage();
        //         return;
        //     }

        //     /* remote command from gui front end */
        //     if (!strcmp(c, "cmd"))
        //     {
        //         if (!NET_IsLocalAddress(net_from))
        //         {
        //             Com_Printf("Command packet from remote host.  Ignored.\n");
        //             return;
        //         }

        //         s = MSG_ReadString(&net_message);
        //         Cbuf_AddText(s);
        //         Cbuf_AddText("\n");
        //         return;
        //     }

            /* print command from somewhere */
            if (args[0].Equals("print"))
            {
                s = msg.ReadString();
                common.Com_Printf(s);
                return;
            }

        //     /* ping from somewhere */
        //     if (!strcmp(c, "ping"))
        //     {
        //         Netchan_OutOfBandPrint(NS_CLIENT, net_from, "ack");
        //         return;
        //     }

        //     /* challenge from the server we are connecting to */
        //     if (!strcmp(c, "challenge"))
        //     {
        //         cls.challenge = (int)strtol(Cmd_Argv(1), (char **)NULL, 10);
        //         CL_SendConnectPacket();
        //         return;
        //     }

        //     /* echo request from server */
        //     if (!strcmp(c, "echo"))
        //     {
        //         Netchan_OutOfBandPrint(NS_CLIENT, net_from, "%s", Cmd_Argv(1));
        //         return;
        //     }

            common.Com_Printf("Unknown command.\n");
        }


        private void CL_ReadPackets()
        {
            var from = new QCommon.netadr_t();
            while (true)
            {
                var msg = common.NET_GetPacket(QCommon.netsrc_t.NS_CLIENT, ref from);
                if (msg == null)
                {
                    break;
                }
                /* remote command packet */
                if (BitConverter.ToInt32(msg) == -1)
                {
                    CL_ConnectionlessPacket(new QReadbuf(msg), from);
                    continue;
                }

                if ((cls.state == connstate_t.ca_disconnected) || (cls.state == connstate_t.ca_connecting))
                {
                    continue; /* dump it if not connected */
                }

                if (msg.Length < 8)
                {
                    common.Com_Printf($"{from}: Runt packet\n");
                    continue;
                }

                /* packet from server */
                // if (!NET_CompareAdr(net_from, cls.netchan.remote_address))
                // {
                //     Com_DPrintf("%s:sequenced packet without connection\n",
                //             NET_AdrToString(net_from));
                //     continue;
                // }

                var qmsg = new QReadbuf(msg);
                if (!cls.netchan.Process(ref qmsg))
                {
                    continue; /* wasn't accepted for some reason */
                }

                CL_ParseServerMessage(ref qmsg);
            }

            /* check timeout */
            // if ((cls.state >= ca_connected) &&
            //     (cls.realtime - cls.netchan.last_received > cl_timeout->value * 1000))
            // {
            //     if (++cl.timeoutcount > 5)
            //     {
            //         Com_Printf("\nServer connection timed out.\n");
            //         CL_Disconnect();
            //         return;
            //     }
            // }

            // else
            // {
            //     cl.timeoutcount = 0;
            // }
        }

    }
}