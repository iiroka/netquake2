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
 * The low level, platform independant network code
 *
 * =======================================================================
 */
using System.Text;

namespace Quake2 {

    partial class QCommon {

        /*
        * packet header
        * -------------
        * 31	sequence
        * 1	does this message contain a reliable payload
        * 31	acknowledge sequence
        * 1	acknowledge receipt of even/odd message
        * 16	qport
        *
        * The remote connection never knows if it missed a reliable message,
        * the local side detects that it has been dropped by seeing a sequence
        * acknowledge higher thatn the last reliable sequence, but without the
        * correct even/odd bit for the reliable set.
        *
        * If the sender notices that a reliable message has been dropped, it
        * will be retransmitted.  It will not be retransmitted again until a
        * message after the retransmit has been acknowledged and the reliable
        * still failed to get there.
        *
        * if the sequence number is -1, the packet should be handled without a
        * netcon
        *
        * The reliable message can be added to at any time by doing MSG_Write*
        * (&netchan->message, <data>).
        *
        * If the message buffer is overflowed, either by a single message, or
        * by multiple frames worth piling up while the last reliable transmit
        * goes unacknowledged, the netchan signals a fatal error.
        *
        * Reliable messages are always placed first in a packet, then the
        * unreliable message is included if there is sufficient room.
        *
        * To the receiver, there is no distinction between the reliable and
        * unreliable parts of the message, they are just processed out as a
        * single larger message.
        *
        * Illogical packet sequence numbers cause the packet to be dropped, but
        * do not kill the connection.  This, combined with the tight window of
        * valid reliable acknowledgement numbers provides protection against
        * malicious address spoofing.
        *
        * The qport field is a workaround for bad address translating routers
        * that sometimes remap the client's source port on a packet during
        * gameplay.
        *
        * If the base part of the net address matches and the qport matches,
        * then the channel matches even if the IP port differs.  The IP port
        * should be updated to the new value before sending out any replies.
        *
        * If there is no information that needs to be transfered on a given
        * frame, such as during the connection stage while waiting for the
        * client to load, then a packet only needs to be delivered if there is
        * something in the unacknowledged reliable
        */

        public cvar_t? showpackets;
        public cvar_t? showdrop;
        public cvar_t? qport;

        private void Netchan_Init()
        {
            /* This is a little bit fishy:

            The original code used Sys_Milliseconds() as base. It worked
            because the original Sys_Milliseconds included some amount of
            random data (Windows) or was dependend on seconds since epoche
            (Unix). Our Sys_Milliseconds() always starts at 0, so there's a
            very high propability - nearly 100 percent for something like
            `./quake2 +connect example.com - that two or more clients end up
            with the same qport.

            We can't use rand() because we're always starting with the same
            seed. So right after client start we'll nearly always get the
            same random numbers. Again there's a high propability that two or
            more clients end up with the same qport.

            Just calling time() should be portable and is more less what
            Windows did in the original code. There's still a rather small
            propability that two clients end up with the same qport, but
            that needs to fixed somewhere else with some kind of fallback
            logic. */
            int port = (int)(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) & 0xffff;

            showpackets = Cvar_Get("showpackets", "0", 0);
            showdrop = Cvar_Get("showdrop", "0", 0);
            qport = Cvar_Get("qport", port.ToString(), cvar_t.CVAR_NOSET);
        }

        /*
        * Sends an out-of-band datagram
        */
        public void Netchan_OutOfBand(QCommon.netsrc_t net_socket, in QCommon.netadr_t adr, in byte[] data)
        {
            /* write the packet header */
            QWritebuf send = new QWritebuf(QCommon.MAX_MSGLEN);

            send.WriteLong(-1); /* -1 sequence means out of band */
            send.Write(data);

            /* send the datagram */
            NET_SendPacket(net_socket, send.Data, adr);
        }

        /*
        * Sends a text message in an out-of-band datagram
        */
        public void Netchan_OutOfBandPrint(QCommon.netsrc_t net_socket, in QCommon.netadr_t adr, in string msg)
        {
            Console.WriteLine($"Netchan_OutOfBandPrint \"{msg}\"");
            Netchan_OutOfBand(net_socket, adr, Encoding.UTF8.GetBytes(msg));
        }

        public class netchan_t
        {
            private QCommon common;
            public bool fatal_error;

            public netsrc_t sock;

            public int dropped;                    /* between last packet and previous */

            public int last_received;              /* for timeouts */
            public int last_sent;                  /* for retransmits */

            public netadr_t remote_address;
            public int qport;                      /* qport value to write when transmitting */

            /* sequencing variables */
            public int incoming_sequence;
            public int incoming_acknowledged;
            public int incoming_reliable_acknowledged;         /* single bit */

            public int incoming_reliable_sequence;             /* single bit, maintained local */

            public int outgoing_sequence;
            public int reliable_sequence;                  /* single bit */
            public int last_reliable_sequence;             /* sequence number of last send */

            /* reliable staging and holding areas */
            public QWritebuf message;          /* writing buffer to send to server */
            // byte message_buf[MAX_MSGLEN - 16];          /* leave space for header */

            /* message is copied to this buffer when it is first transfered */
            public int reliable_length;
            public byte[] reliable_buf;         /* unacked reliable message */

            public netchan_t(QCommon common)
            {
                this.common = common;
                this.remote_address = new netadr_t();
                this.reliable_buf = new byte[MAX_MSGLEN - 16];
                this.message = new QWritebuf(MAX_MSGLEN - 16);
            }

            /*
            * called to open a channel to a remote system
            */
            public void Setup(netsrc_t sock, in netadr_t adr, int qport)
            {
                this.fatal_error = false;
                this.sock = sock;
                this.dropped = 0;
                this.last_received = common.curtime;
                this.last_sent = 0;
                this.remote_address = adr;
                this.qport = qport;
                this.incoming_sequence = 0;
                this.incoming_acknowledged = 0;
                this.incoming_reliable_acknowledged = 0;
                this.incoming_reliable_sequence = 0;
                this.outgoing_sequence = 1;
                this.reliable_sequence = 0;
                this.last_reliable_sequence = 0;
                this.reliable_length = 0;

                this.message.Clear();
                this.message.allowoverflow = true;
            }

            /*
            * Returns true if the last reliable message has acked
            */
            private bool CanReliable()
            {
                if (reliable_length > 0)
                {
                    return false; /* waiting for ack */
                }

                return true;
            }

            private bool NeedReliable()
            {
                if ((incoming_acknowledged > last_reliable_sequence) &&
                    (incoming_reliable_acknowledged != reliable_sequence))
                {
                    return true;
                }

                /* if the reliable transmit buffer is empty, copy the current message out */
                if (reliable_length == 0 && message.Size > 0)
                {
                    return true;
                }

                return false;
            }

            /*
            * tries to send an unreliable message to a connection, and handles the
            * transmition / retransmition of the reliable messages.
            *
            * A 0 length will still generate a packet and deal with the reliable messages.
            */
            public void Transmit(ReadOnlySpan<byte> data)
            {
                /* check for message overflow */
                if (message.overflowed)
                {
                    fatal_error = true;
                    common.Com_Printf($"{remote_address}:Outgoing message overflow\n");
                    return;
                }

                var send_reliable = NeedReliable();

                if (reliable_length == 0 && message.Size > 0)
                {
                    Array.Copy(message.Data.ToArray(), reliable_buf, message.Size);
                    reliable_length = message.Size;
                    message.Clear();
                    reliable_sequence ^= 1;
                }

                /* write the packet header */
                var send = new QWritebuf(QCommon.MAX_MSGLEN);

                int w1 = (outgoing_sequence & 0x7FFFFFFF) | (int)(send_reliable ? 0x80000000u : 0);
                int w2 = (incoming_sequence & 0x7FFFFFFF) | (incoming_reliable_sequence << 31);

                outgoing_sequence++;
                last_sent = common.curtime;

                send.WriteLong(w1);
                send.WriteLong(w2);

                /* send the qport if we are a client */
                if (sock == netsrc_t.NS_CLIENT)
                {
                    send.WriteShort(common.qport!.Int);
                }

                /* copy the reliable message to the packet first */
                if (send_reliable)
                {
                    send.Write(new ReadOnlySpan<byte>(reliable_buf, 0, reliable_length));
                    last_reliable_sequence = outgoing_sequence;
                }

                /* add the unreliable part if space is available */
                if (data != null)
                {
                    if (QCommon.MAX_MSGLEN - send.Size >= data.Length)
                    {
                        send.Write(data);
                    }
                    else
                    {
                        common.Com_Printf("Netchan_Transmit: dumped unreliable\n");
                    }
                }

                /* send the datagram */
                common.NET_SendPacket(sock, send.Data, remote_address);

                if (common.showpackets?.Bool ?? false)
                {
                    if (send_reliable)
                    {
                        // Com_Printf($"send %4i : s=%i reliable=%i ack=%i rack=%i\n",
                        //         send.cursize, chan->outgoing_sequence - 1,
                        //         chan->reliable_sequence, chan->incoming_sequence,
                        //         chan->incoming_reliable_sequence);
                    }
                    else
                    {
                        // Com_Printf("send %4i : s=%i ack=%i rack=%i\n",
                        //         send.cursize, chan->outgoing_sequence - 1,
                        //         chan->incoming_sequence,
                        //         chan->incoming_reliable_sequence);
                    }
                }
            }

            /*
            * called when the current net_message is from remote_address
            * modifies net_message so that it points to the packet payload
            */
            public bool Process(ref QReadbuf msg)
            {
                // unsigned sequence, sequence_ack;
                // unsigned reliable_ack, reliable_message;

                /* get sequence numbers */
                msg.BeginReading();
                int sequence = msg.ReadLong();
                int sequence_ack = msg.ReadLong();

                /* read the qport if we are a server */
                if (sock == netsrc_t.NS_SERVER)
                {
                    msg.ReadShort();
                }

                var reliable_message = (sequence >> 31) & 1;
                var reliable_ack = (sequence_ack >> 31) & 1;

                sequence &= ~(1 << 31);
                sequence_ack &= ~(1 << 31);

                if (common.showpackets?.Bool ?? false)
                {
                    if (reliable_message != 0)
                    {
                        // Com_Printf("recv %4i : s=%i reliable=%i ack=%i rack=%i\n",
                        //         msg->cursize, sequence,
                        //         chan->incoming_reliable_sequence ^ 1,
                        //         sequence_ack, reliable_ack);
                    }
                    else
                    {
                        // Com_Printf("recv %4i : s=%i ack=%i rack=%i\n",
                        //         msg->cursize, sequence, sequence_ack,
                        //         reliable_ack);
                    }
                }

                /* discard stale or duplicated packets */
                if (sequence <= incoming_sequence)
                {
                    if (common.showdrop?.Bool ?? false)
                    {
                        // Com_Printf("%s:Out of order packet %i at %i\n",
                        //         NET_AdrToString(chan->remote_address),
                        //         sequence, chan->incoming_sequence);
                    }

                    return false;
                }

                /* dropped packets don't keep the message from being used */
                dropped = sequence - (incoming_sequence + 1);

                // if (chan->dropped > 0)
                // {
                //     if (showdrop->value)
                //     {
                //         Com_Printf("%s:Dropped %i packets at %i\n",
                //                 NET_AdrToString(chan->remote_address),
                //                 chan->dropped, sequence);
                //     }
                // }

                /* if the current outgoing reliable message has been acknowledged
                * clear the buffer to make way for the next */
                if (reliable_ack == reliable_sequence)
                {
                    reliable_length = 0; /* it has been received */
                }

                /* if this message contains a reliable message, bump incoming_reliable_sequence */
                incoming_sequence = sequence;
                incoming_acknowledged = sequence_ack;
                incoming_reliable_acknowledged = reliable_ack;

                if (reliable_message != 0)
                {
                    incoming_reliable_sequence ^= 1;
                }

                /* the message can now be read from the current message pointer */
                last_received = common.curtime;

                return true;
            }


        }


    }
}