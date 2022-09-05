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
 * Connectionless server commands.
 *
 * =======================================================================
 */

namespace Quake2 {

    partial class QServer {

        /*
        * A connection request that did not come from the master
        */
        private void SVC_DirectConnect(in string[] args, in QCommon.netadr_t adr)
        {
            // char userinfo[MAX_INFO_STRING];
            // netadr_t adr;
            // int i;
            // client_t *cl, *newcl;
            // client_t temp;
            // edict_t *ent;
            // int edictnum;
            // int version;
            // int qport;
            // int challenge;

            // adr = net_from;

            common.Com_DPrintf("SVC_DirectConnect ()\n");

            var version = int.Parse(args[1]);

            if (version != QCommon.PROTOCOL_VERSION)
            {
                common.Netchan_OutOfBandPrint(QCommon.netsrc_t.NS_SERVER, adr, "print\nServer is protocol version 34.\n");
                common.Com_DPrintf($"    rejected connect from version {version}\n");
                return;
            }

            var qport = int.Parse(args[2]);

            var challenge = int.Parse(args[3]);

            var userinfo = args[4];

            /* force the IP key/value pair so the game can filter based on ip */
        //     Info_SetValueForKey(userinfo, "ip", NET_AdrToString(net_from));

            /* attractloop servers are ONLY for local clients */
            if (sv.attractloop)
            {
        //         if (!NET_IsLocalAddress(adr))
        //         {
        //             Com_Printf("Remote connect in attract loop.  Ignored.\n");
        //             Netchan_OutOfBandPrint(NS_SERVER, adr,
        //                     "print\nConnection refused.\n");
        //             return;
        //         }
            }

            /* see if the challenge is valid */
        //     if (!NET_IsLocalAddress(adr))
        //     {
        //         for (i = 0; i < MAX_CHALLENGES; i++)
        //         {
        //             if (NET_CompareBaseAdr(net_from, svs.challenges[i].adr))
        //             {
        //                 if (challenge == svs.challenges[i].challenge)
        //                 {
        //                     break; /* good */
        //                 }

        //                 Netchan_OutOfBandPrint(NS_SERVER, adr,
        //                         "print\nBad challenge.\n");
        //                 return;
        //             }
        //         }

        //         if (i == MAX_CHALLENGES)
        //         {
        //             Netchan_OutOfBandPrint(NS_SERVER, adr,
        //                     "print\nNo challenge for address.\n");
        //             return;
        //         }
        //     }

            int i;
        //     newcl = &temp;
        //     memset(newcl, 0, sizeof(client_t));

            /* if there is already a slot for this ip, reuse it */
            for (i = 0; i < svs.clients.Length; i++)
            {
                if (svs.clients[i].state < client_state_t.cs_connected)
                {
                    continue;
                }

                if (adr.CompareBaseAdr(svs.clients[i].netchan.remote_address) &&
                    ((svs.clients[i].netchan.qport == qport) ||
                    (adr.port == svs.clients[i].netchan.remote_address.port)))
                {
                    if (!adr.IsLocalAddress())
                    {
                        common.Com_DPrintf($"{adr}:reconnect rejected : too soon\n");
                        return;
                    }

                    common.Com_Printf($"{adr}:reconnect\n");
                    break;
                }
            }

            if (i >= svs.clients.Length)
            {
                for (i = 0; i < svs.clients.Length; i++)
                {
                    if (svs.clients[i].state == client_state_t.cs_free)
                    {
                        break;
                    }
                }
            }

            if (i >= svs.clients.Length)
            {
                common.Netchan_OutOfBandPrint(QCommon.netsrc_t.NS_SERVER, adr, "print\nServer is full.\n");
                common.Com_DPrintf("Rejected a connection.\n");
                return;
            }

        // gotnewcl:

            /* build a new connection  accept the new client this
            is the only place a client_t is ever initialized */
            svs.clients[i] = new client_t() { index = i };
            svs.clients[i].netchan = new QCommon.netchan_t(common);
            var ent = ge.getEdict(i + 1);
            svs.clients[i].edict = ent;
            svs.clients[i].challenge = challenge; /* save challenge for checksumming */

        //     /* get the game a chance to reject this connection or modify the userinfo */
        //     if (!(ge->ClientConnect(ent, userinfo)))
        //     {
        //         if (*Info_ValueForKey(userinfo, "rejmsg"))
        //         {
        //             Netchan_OutOfBandPrint(NS_SERVER, adr,
        //                     "print\n%s\nConnection refused.\n",
        //                     Info_ValueForKey(userinfo, "rejmsg"));
        //         }
        //         else
        //         {
        //             Netchan_OutOfBandPrint(NS_SERVER, adr,
        //                     "print\nConnection refused.\n");
        //         }

        //         Com_DPrintf("Game rejected a connection.\n");
        //         return;
        //     }

            /* parse some info from the info strings */
            svs.clients[i].userinfo = userinfo;
        //     SV_UserinfoChanged(newcl);

        //     /* send the connect packet to the client */
        //     if (sv_downloadserver->string[0])
        //     {
        //         Netchan_OutOfBandPrint(NS_SERVER, adr, "client_connect dlserver=%s", sv_downloadserver->string);
        //     }
        //     else
        //     {
                common.Netchan_OutOfBandPrint(QCommon.netsrc_t.NS_SERVER, adr, "client_connect");
        //     }

            svs.clients[i].netchan.Setup(QCommon.netsrc_t.NS_SERVER, adr, qport);

            svs.clients[i].state = client_state_t.cs_connected;

        //     SZ_Init(&newcl->datagram, newcl->datagram_buf, sizeof(newcl->datagram_buf));
            // svs.clients[i].datagram.allowoverflow = true;
            svs.clients[i].lastmessage = svs.realtime;  /* don't timeout */
            svs.clients[i].lastconnect = svs.realtime;
        }

        /*
        * A connectionless packet has four leading 0xff
        * characters to distinguish it from a game channel.
        * Clients that are in the game can still send
        * connectionless packets.
        */
        private void SV_ConnectionlessPacket(QReadbuf msg, in QCommon.netadr_t from)
        {
            msg.BeginReading();
            var m = msg.ReadLong(); /* skip the -1 marker */

            var s = msg.ReadStringLine();

            var args = common.Cmd_TokenizeString(s, false);

            common.Com_DPrintf($"Packet {from} : {args[0]}\n");

            // if (!strcmp(c, "ping"))
            // {
            //     SVC_Ping();
            // }
            // else if (!strcmp(c, "ack"))
            // {
            //     SVC_Ack();
            // }
            // else if (!strcmp(c, "status"))
            // {
            //     SVC_Status();
            // }
            // else if (!strcmp(c, "info"))
            // {
            //     SVC_Info();
            // }
            // else if (!strcmp(c, "getchallenge"))
            // {
            //     SVC_GetChallenge();
            // }
            // else 
            if (args[0].Equals("connect"))
            {
                SVC_DirectConnect(args, from);
            }
            // else if (!strcmp(c, "rcon"))
            // {
            //     SVC_RemoteCommand();
            // }
            else
            {
                common.Com_Printf($"bad connectionless packet from {from}:\n{s}\n");
            }
        }

    }
}