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
 * Low level network code, based upon the BSD socket api.
 *
 * =======================================================================
 */

namespace Quake2 {

    partial class QCommon {

        private const int MAX_LOOPBACK = 4;

        private struct loopmsg_t
        {
            public byte[] data;
            public int datalen;

            public loopmsg_t() {
                data = new byte[QCommon.MAX_MSGLEN];
                datalen = 0;
            }
        }

        private struct loopback_t
        {
	        public loopmsg_t[] msgs;
	        public int get, send;
            public loopback_t() {
                msgs = new loopmsg_t[MAX_LOOPBACK];
                get = 0;
                send = 0;
                for (int i = 0; i < MAX_LOOPBACK; i++)
                {
                    msgs[i] = new loopmsg_t();
                }
            }
        }

        private loopback_t[] loopbacks = new loopback_t[2];

        public void NET_Init()
        {
            loopbacks = new loopback_t[2];
            loopbacks[0] = new loopback_t();
            loopbacks[1] = new loopback_t();
        }


        private ReadOnlySpan<byte> NET_GetLoopPacket(netsrc_t sock)
        {
            ref var loop = ref loopbacks[(int)sock];

            if (loop.send - loop.get > MAX_LOOPBACK)
            {
                loop.get = loop.send - MAX_LOOPBACK;
            }

            if (loop.get >= loop.send)
            {
                return null;
            }

            var i = loop.get & (MAX_LOOPBACK - 1);
            loop.get++;


            return new ReadOnlySpan<byte>(loop.msgs[i].data, 0, loop.msgs[i].datalen);
        }

        private void NET_SendLoopPacket(netsrc_t sock, in ReadOnlySpan<byte> data)
        {
            ref var loop = ref loopbacks[(int)sock ^ 1];

            var i = loop.send & (MAX_LOOPBACK - 1);
            loop.send++;

            Array.Copy(data.ToArray(), loop.msgs[i].data, data.Length);
            loop.msgs[i].datalen = data.Length;
        }

        public ReadOnlySpan<byte> NET_GetPacket(netsrc_t sock, ref netadr_t from)
        {
            // int ret;
            // struct sockaddr_storage from;
            // socklen_t fromlen;
            // int net_socket;
            // int protocol;
            // int err;

            var msg = NET_GetLoopPacket(sock);
            if (msg != null)
            {
                from.type = netadrtype_t.NA_LOOPBACK;
                from.port = 0;
                return msg;
            }

            // for (protocol = 0; protocol < 3; protocol++)
            // {
            //     if (protocol == 0)
            //     {
            //         net_socket = ip_sockets[sock];
            //     }
            //     else if (protocol == 1)
            //     {
            //         net_socket = ip6_sockets[sock];
            //     }
            //     else
            //     {
            //         net_socket = ipx_sockets[sock];
            //     }

            //     if (!net_socket)
            //     {
            //         continue;
            //     }

            //     fromlen = sizeof(from);
            //     ret = recvfrom(net_socket, net_message->data, net_message->maxsize,
            //             0, (struct sockaddr *)&from, &fromlen);

            //     SockadrToNetadr(&from, net_from);

            //     if (ret == -1)
            //     {
            //         err = errno;

            //         if ((err == EWOULDBLOCK) || (err == ECONNREFUSED))
            //         {
            //             continue;
            //         }

            //         Com_Printf("NET_GetPacket: %s from %s\n", NET_ErrorString(),
            //                 NET_AdrToString(*net_from));
            //         continue;
            //     }

            //     if (ret == net_message->maxsize)
            //     {
            //         Com_Printf("Oversize packet from %s\n", NET_AdrToString(*net_from));
            //         continue;
            //     }

            //     net_message->cursize = ret;
            //     return true;
            // }

            return null;
        }

        public void NET_SendPacket(netsrc_t sock, in ReadOnlySpan<byte> data, in netadr_t to)
        {
            // int ret;
            // struct sockaddr_storage addr;
            // int net_socket;
            // int addr_size = sizeof(struct sockaddr_in);

            switch (to.type)
            {
                case netadrtype_t.NA_LOOPBACK:
                    NET_SendLoopPacket(sock, data);
                    return;

                // case NA_BROADCAST:
                // case NA_IP:
                //     net_socket = ip_sockets[sock];

                //     if (!net_socket)
                //     {
                //         return;
                //     }

                //     break;

                // case NA_IP6:
                // case NA_MULTICAST6:
                //     net_socket = ip6_sockets[sock];
                //     addr_size = sizeof(struct sockaddr_in6);

                //     if (!net_socket)
                //     {
                //         return;
                //     }

                //     break;

                // case NA_IPX:
                // case NA_BROADCAST_IPX:
                //     net_socket = ipx_sockets[sock];

                //     if (!net_socket)
                //     {
                //         return;
                //     }

                //     break;

                default:
                    Com_Error(QShared.ERR_FATAL, "NET_SendPacket: bad address type");
                    return;
            }
        }

    }
}