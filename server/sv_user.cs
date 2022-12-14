namespace Quake2 {

    partial class QServer {

        private const int MAX_STRINGCMDS = 8;

        private delegate void UserCommandHandler(string[] args, ref client_t client);

        private void SV_BeginDemoserver()
        {
            var name = $"demos/{sv.name}";
            sv.demofile = common.FS_FOpenFile(name, false);

            if (sv.demofile == null)
            {
                common.Com_Error(QShared.ERR_DROP, $"Couldn't open {name}\n");
            }
        }

        /*
        * Sends the first message from the server to a connected client.
        * This will be sent on the initial connection and upon each server load.
        */
        private void SV_New_f(string[] args, ref client_t client)
        {
            // static char *gamedir;
            // int playernum;
            // edict_t *ent;

            common.Com_DPrintf($"New() from {client.name}\n");

            if (client.state != client_state_t.cs_connected)
            {
                common.Com_Printf("New not valid -- already spawned\n");
                return;
            }

            /* demo servers just dump the file message */
            if (sv.state == server_state_t.ss_demo)
            {
                SV_BeginDemoserver();
                return;
            }

            /* serverdata needs to go over for all types of servers
            to make sure the protocol is right, and to set the gamedir */
            // gamedir = (char *)Cvar_VariableString("gamedir");

            /* send the serverdata */
            client.netchan.message.WriteByte((int)QCommon.svc_ops_e.svc_serverdata);
            client.netchan.message.WriteLong(QCommon.PROTOCOL_VERSION);
            client.netchan.message.WriteLong(svs.spawncount);
            client.netchan.message.WriteByte(sv.attractloop ? 1 : 0);
            client.netchan.message.WriteString(common.Cvar_VariableString("gamedir"));

            int playernum;
            if ((sv.state == server_state_t.ss_cinematic) || (sv.state == server_state_t.ss_pic))
            {
                playernum = -1;
            }
            else
            {
                playernum = client.index;
            }

            client.netchan.message.WriteShort(playernum);

            /* send full levelname */
            client.netchan.message.WriteString(sv.configstrings[QShared.CS_NAME]);

            /* game server */
            if (sv.state == server_state_t.ss_game)
            {
                /* set up the entity for the client */
                var ent = ge!.getEdict(playernum + 1);
                ent.s.number = playernum + 1;
                client.edict = ent;
            //     memset(&sv_client->lastcmd, 0, sizeof(sv_client->lastcmd));

                /* begin fetching configstrings */
                client.netchan.message.WriteByte((int)QCommon.svc_ops_e.svc_stufftext);
                client.netchan.message.WriteString($"cmd configstrings {svs.spawncount} 0\n");
            }
        }

        private void SV_Nextserver()
        {
            if ((sv.state == server_state_t.ss_game) ||
                ((sv.state == server_state_t.ss_pic) &&
                !common.Cvar_VariableBool("coop")))
            {
                return; /* can't nextserver while playing a normal game */
            }

            svs.spawncount++; /* make sure another doesn't sneak in */
            var v = common.Cvar_VariableString("nextserver");

            if (String.IsNullOrEmpty(v))
            {
                common.Cbuf_AddText("killserver\n");
            }
            else
            {
                common.Cbuf_AddText(v);
                common.Cbuf_AddText("\n");
            }

            common.Cvar_Set("nextserver", "");
        }

        /*
        * A cinematic has completed or been aborted by a client, so move
        * to the next server,
        */
        private void SV_Nextserver_f(string[] args, ref client_t client)
        {
            if (Int32.Parse(args[1]) != svs.spawncount)
            {
                common.Com_DPrintf($"Nextserver() from wrong level, from {client.name}\n");
                return; /* leftover from last server */
            }

            common.Com_DPrintf($"Nextserver() from {client.name}\n");

            SV_Nextserver();
        }

        private void SV_ExecuteUserCommand(string s, ref client_t cl)
        {
            // ucmd_t *u;

            /* Security Fix... This is being set to false so that client's can't
            macro expand variables on the server.  It seems unlikely that a
            client ever ought to need to be able to do this... */
            var args = common.Cmd_TokenizeString(s, false);
            // sv_player = sv_client->edict;

            switch (args[0])
            {
                case "new":
                    SV_New_f(args, ref cl);
                    return;
                // {"configstrings", SV_Configstrings_f},
                // {"baselines", SV_Baselines_f},
                // {"begin", SV_Begin_f},
                case "nextserver":
                    SV_Nextserver_f(args, ref cl);
                    return;
                // {"disconnect", SV_Disconnect_f},
                // {"info", SV_ShowServerinfo_f},
                // {"download", SV_BeginDownload_f},
                // {"nextdl", SV_NextDownload_f},
            }
            // for (u = ucmds; u->name; u++)
            // {
            //     if (!strcmp(Cmd_Argv(0), u->name))
            //     {
            //         u->func();
            //         break;
            //     }
            // }

            Console.WriteLine($"Unknown user command {args[0]}");
            // if (!u->name && (sv.state == ss_game))
            // {
            //     ge->ClientCommand(sv_player);
            // }
        }

        /*
        * The current net_message is parsed for the given client
        */
        private void SV_ExecuteClientMessage(ref QReadbuf msg, ref client_t cl)
        {
            // int c;
            // char *s;

            // usercmd_t nullcmd;
            // usercmd_t oldest, oldcmd, newcmd;
            // int net_drop;
            // int stringCmdCount;
            // int checksum, calculatedChecksum;
            // int checksumIndex;
            // qboolean move_issued;
            // int lastframe;

            // sv_client = cl;
            // sv_player = sv_client->edict;

            /* only allow one move command */
            // move_issued = false;
            var stringCmdCount = 0;

            while (true)
            {
                if (msg.Count > msg.Size)
                {
                    common.Com_Printf("SV_ReadClientMessage: badread\n");
                    // SV_DropClient(cl);
                    return;
                }

                var c = msg.ReadByte();

                if (c == -1)
                {
                    break;
                }

                switch (c)
                {

            //         case clc_nop:
            //             break;

            //         case clc_userinfo:
            //             Q_strlcpy(cl->userinfo, MSG_ReadString(&net_message), sizeof(cl->userinfo));
            //             SV_UserinfoChanged(cl);
            //             break;

            //         case clc_move:

            //             if (move_issued)
            //             {
            //                 return; /* someone is trying to cheat... */
            //             }

            //             move_issued = true;
            //             checksumIndex = net_message.readcount;
            //             checksum = MSG_ReadByte(&net_message);
            //             lastframe = MSG_ReadLong(&net_message);

            //             if (lastframe != cl->lastframe)
            //             {
            //                 cl->lastframe = lastframe;

            //                 if (cl->lastframe > 0)
            //                 {
            //                     cl->frame_latency[cl->lastframe & (LATENCY_COUNTS - 1)] =
            //                         svs.realtime - cl->frames[cl->lastframe & UPDATE_MASK].senttime;
            //                 }
            //             }

            //             memset(&nullcmd, 0, sizeof(nullcmd));
            //             MSG_ReadDeltaUsercmd(&net_message, &nullcmd, &oldest);
            //             MSG_ReadDeltaUsercmd(&net_message, &oldest, &oldcmd);
            //             MSG_ReadDeltaUsercmd(&net_message, &oldcmd, &newcmd);

            //             if (cl->state != cs_spawned)
            //             {
            //                 cl->lastframe = -1;
            //                 break;
            //             }

            //             /* if the checksum fails, ignore the rest of the packet */
            //             calculatedChecksum = COM_BlockSequenceCRCByte(
            //                 net_message.data + checksumIndex + 1,
            //                 net_message.readcount - checksumIndex - 1,
            //                 cl->netchan.incoming_sequence);

            //             if (calculatedChecksum != checksum)
            //             {
            //                 Com_DPrintf("Failed command checksum for %s (%d != %d)/%d\n",
            //                         cl->name, calculatedChecksum, checksum,
            //                         cl->netchan.incoming_sequence);
            //                 return;
            //             }

            //             if (!sv_paused->value)
            //             {
            //                 net_drop = cl->netchan.dropped;

            //                 if (net_drop < 20)
            //                 {
            //                     while (net_drop > 2)
            //                     {
            //                         SV_ClientThink(cl, &cl->lastcmd);

            //                         net_drop--;
            //                     }

            //                     if (net_drop > 1)
            //                     {
            //                         SV_ClientThink(cl, &oldest);
            //                     }

            //                     if (net_drop > 0)
            //                     {
            //                         SV_ClientThink(cl, &oldcmd);
            //                     }
            //                 }

            //                 SV_ClientThink(cl, &newcmd);
            //             }

            //             cl->lastcmd = newcmd;
            //             break;

                    case (int)QCommon.clc_ops_e.clc_stringcmd:
                        var s = msg.ReadString();

                        /* malicious users may try using too many string commands */
                        if (++stringCmdCount < MAX_STRINGCMDS)
                        {
                            SV_ExecuteUserCommand(s, ref cl);
                        }

                        if (cl.state == client_state_t.cs_zombie)
                        {
                            return; /* disconnect command */
                        }

                        break;
                    default:
                        common.Com_Printf($"SV_ReadClientMessage: unknown command char {c}\n");
                        // SV_DropClient(cl);
                        return;
                }
            }
        }


    }
}