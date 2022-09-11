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
 * Message sending and multiplexing.
 *
 * =======================================================================
 */
using System.Numerics;

namespace Quake2 {

    partial class QServer {

        /*
        * Sends text to all active clients
        */
        private void SV_BroadcastCommand(string msg)
        {
            if (sv.multicast != null)
            {
                sv.multicast.WriteByte((int)QCommon.svc_ops_e.svc_stufftext);
                sv.multicast.WriteString(msg);
                SV_Multicast(null, QShared.multicast_t.MULTICAST_ALL_R);
            }
        }

        /*
        * Sends the contents of sv.multicast to a subset of the clients,
        * then clears sv.multicast.
        *
        * MULTICAST_ALL	same as broadcast (origin can be NULL)
        * MULTICAST_PVS	send to clients potentially visible from org
        * MULTICAST_PHS	send to clients potentially hearable from org
        */
        private void SV_Multicast(in Vector3? origin, QShared.multicast_t to)
        {
            // client_t *client;
            // byte *mask;
            // int leafnum = 0, cluster;
            // int j;
            // qboolean reliable;
            // int area1, area2;

            var reliable = false;

            if ((to != QShared.multicast_t.MULTICAST_ALL_R) && (to != QShared.multicast_t.MULTICAST_ALL))
            {
                // leafnum = CM_PointLeafnum(origin);
                // area1 = CM_LeafArea(leafnum);
            }
            else
            {
                // area1 = 0;
            }

            /* if doing a serverrecord, store everything */
            // if (svs.demofile)
            // {
            //     SZ_Write(&svs.demo_multicast, sv.multicast.data, sv.multicast.cursize);
            // }

            switch (to)
            {
                case QShared.multicast_t.MULTICAST_ALL_R:
                    reliable = true; /* intentional fallthrough */
                    goto case QShared.multicast_t.MULTICAST_ALL;
                case QShared.multicast_t.MULTICAST_ALL:
                    // mask = NULL;
                    break;

                // case MULTICAST_PHS_R:
                //     reliable = true; /* intentional fallthrough */
                // case MULTICAST_PHS:
                //     leafnum = CM_PointLeafnum(origin);
                //     cluster = CM_LeafCluster(leafnum);
                //     mask = CM_ClusterPHS(cluster);
                //     break;

                // case MULTICAST_PVS_R:
                //     reliable = true; /* intentional fallthrough */
                // case MULTICAST_PVS:
                //     leafnum = CM_PointLeafnum(origin);
                //     cluster = CM_LeafCluster(leafnum);
                //     mask = CM_ClusterPVS(cluster);
                //     break;

                default:
                    // mask = NULL;
                    common.Com_Error(QShared.ERR_FATAL, "SV_Multicast: bad to:" + to);
                    break;
            }

            /* send the data to all relevent clients */
            for (int j = 0; j < svs.clients.Length; j++)
            {
                if ((svs.clients[j].state == client_state_t.cs_free) || (svs.clients[j].state == client_state_t.cs_zombie))
                {
                    continue;
                }

                if ((svs.clients[j].state != client_state_t.cs_spawned) && !reliable)
                {
                    continue;
                }

                // if (mask)
                // {
                //     leafnum = CM_PointLeafnum(client->edict->s.origin);
                //     cluster = CM_LeafCluster(leafnum);
                //     area2 = CM_LeafArea(leafnum);

                //     if (!CM_AreasConnected(area1, area2))
                //     {
                //         continue;
                //     }

                //     if (!(mask[cluster >> 3] & (1 << (cluster & 7))))
                //     {
                //         continue;
                //     }
                // }

                if (reliable)
                {
                    svs.clients[j].netchan.message.Write(sv.multicast.Data);
                }
                else
                {
                    // svs.clients[j].datagram.Write(sv.multicast.Data);
                }
            }

            sv.multicast.Clear();
        }

        private bool SV_SendClientDatagram(ref client_t client)
        {
            var msg = new QWritebuf(QCommon.MAX_MSGLEN);

            // SV_BuildClientFrame(client);

            msg.allowoverflow = true;

            /* send over all the relevant entity_state_t
            and the player_state_t */
            // SV_WriteFrameToClient(client, &msg);

            // /* copy the accumulated multicast datagram
            // for this client out to the message
            // it is necessary for this to be after the WriteEntities
            // so that entity references will be current */
            // if (client->datagram.overflowed)
            // {
            //     Com_Printf("WARNING: datagram overflowed for %s\n", client->name);
            // }
            // else
            // {
            //     SZ_Write(&msg, client->datagram.data, client->datagram.cursize);
            // }

            // SZ_Clear(&client->datagram);

            if (msg.overflowed)
            {
                /* must have room left for the packet header */
                common.Com_Printf($"WARNING: msg overflowed for {client.name}\n");
                msg.Clear();
            }

            /* send the datagram */
            client.netchan.Transmit(msg.Data);

            // /* record the size for rate estimation */
            // client->message_size[sv.framenum % RATE_MESSAGES] = msg.cursize;

            return true;
        }

        private void SV_DemoCompleted()
        {
            sv.demofile?.Close();
            sv.demofile = null;
            // SV_Nextserver();
        }

        private void SV_SendClientMessages()
        {
            byte[]? msgbuf = null;

            /* read the next demo message if needed */
            if (sv.demofile != null && (sv.state == server_state_t.ss_demo))
            {
                if (sv_paused?.Bool ?? false)
                {
                    msgbuf = null;
                }
                else
                {
                    /* get the next message */
                    byte[] bfr = new byte[4];
                    int r = sv.demofile.Read(bfr, 0, 4);
                    if (r != 4)
                    {
                        SV_DemoCompleted();
                        return;
                    }

                    int msglen = BitConverter.ToInt32(bfr);

                    if (msglen < 0)
                    {
                        SV_DemoCompleted();
                        return;
                    }

                    if (msglen > QCommon.MAX_MSGLEN)
                    {
                        common.Com_Error(QShared.ERR_DROP,
                                "SV_SendClientMessages: msglen > MAX_MSGLEN");
                    }

                    msgbuf = new byte[msglen];
                    r = sv.demofile.Read(msgbuf, 0, msglen);

                    if (r != msglen)
                    {
                        SV_DemoCompleted();
                        return;
                    }
                }
            }

            /* send a message to each connected client */
            for (int i = 0; i < svs.clients.Length; i++)
            {
                if (svs.clients[i].state == client_state_t.cs_free)
                {
                    continue;
                }

                /* if the reliable message 
                overflowed, drop the 
                client */
                if (svs.clients[i].netchan.message.overflowed)
                {
                    svs.clients[i].netchan.message.Clear();
                    // SZ_Clear(&c->datagram);
                    // SV_BroadcastPrintf(PRINT_HIGH, "%s overflowed\n", c->name);
                    // SV_DropClient(c);
                }

                if ((sv.state == server_state_t.ss_cinematic) ||
                    (sv.state == server_state_t.ss_demo) ||
                    (sv.state == server_state_t.ss_pic))
                {
                    svs.clients[i].netchan.Transmit(msgbuf);
                }
                else if (svs.clients[i].state == client_state_t.cs_spawned)
                {
                    /* don't overrun bandwidth */
                    // if (SV_RateDrop(c))
                    // {
                    //     continue;
                    // }

                    SV_SendClientDatagram(ref svs.clients[i]);
                }
                else
                {
                    /* just update reliable	if needed */
                    if (svs.clients[i].netchan.message.Size > 0 ||
                        (common.curtime - svs.clients[i].netchan.last_sent > 1000))
                    {
                        svs.clients[i].netchan.Transmit(null);
                    }
                }
            }
        }

    }
}