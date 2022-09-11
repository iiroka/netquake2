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
 * This file implements the input handling like mouse events and
 * keyboard strokes.
 *
 * =======================================================================
 */

namespace Quake2 {

    partial class QClient {

        private int frame_msec;
        private int old_sys_frame_time;

        private void CL_RefreshCmd()
        {
            // CMD to fill
            ref var cmd = ref cl.cmds[cls.netchan.outgoing_sequence & (CMD_BACKUP - 1)];

            // Calculate delta
            frame_msec = input.sys_frame_time - old_sys_frame_time;

            // Check bounds
            if (frame_msec < 1)
            {
                return;
            }
            else if (frame_msec > 200)
            {
                frame_msec = 200;
            }

            // Add movement
            // CL_BaseMove(cmd);
            // IN_Move(cmd);

            // Clamp angels for prediction
            // CL_ClampPitch();

            // cmd->angles[0] = ANGLE2SHORT(cl.viewangles[0]);
            // cmd->angles[1] = ANGLE2SHORT(cl.viewangles[1]);
            // cmd->angles[2] = ANGLE2SHORT(cl.viewangles[2]);

            // Update time for prediction
            var ms = (int)(cls.nframetime * 1000.0f);

            if (ms > 250)
            {
                ms = 100;
            }

            cmd.msec = (byte)ms;

            // Update frame time for the next call
            old_sys_frame_time = input.sys_frame_time;

            // // Important events are send immediately
            // if (((in_attack.state & 2)) || (in_use.state & 2))
            // {
            //     cls.forcePacket = true;
            // }
        }

        private void CL_RefreshMove()
        {
            // CMD to fill
            ref var cmd = ref cl.cmds[cls.netchan.outgoing_sequence & (CMD_BACKUP - 1)];

            // Calculate delta
            frame_msec = input.sys_frame_time - old_sys_frame_time;

            // Check bounds
            if (frame_msec < 1)
            {
                return;
            }
            else if (frame_msec > 200)
            {
                frame_msec = 200;
            }

            // Add movement
            // CL_BaseMove(cmd);
            // IN_Move(cmd);

            old_sys_frame_time = input.sys_frame_time;
        }

        private void CL_SendCmd()
        {
            // sizebuf_t buf;
            // byte data[128];
            // int i;
            // usercmd_t *cmd, *oldcmd;
            // usercmd_t nullcmd;
            // int checksumIndex;

            // memset(&buf, 0, sizeof(buf));

            /* save this command off for prediction */
            var i = cls.netchan.outgoing_sequence & (CMD_BACKUP - 1);
            ref var cmd = ref cl.cmds[i];
            // cl.cmd_time[i] = cls.realtime; /* for netgraph ping calculation */

            // CL_FinalizeCmd();

            cl.cmd = cmd;

            if ((cls.state == connstate_t.ca_disconnected) || (cls.state == connstate_t.ca_connecting))
            {
                return;
            }

            if (cls.state == connstate_t.ca_connected)
            {
                if (cls.netchan.message.Size > 0 || (common.curtime - cls.netchan.last_sent > 1000))
                {
                    cls.netchan.Transmit(null);
                }

                return;
            }

            // /* send a userinfo update if needed */
            // if (userinfo_modified)
            // {
            //     CL_FixUpGender();
            //     userinfo_modified = false;
            //     MSG_WriteByte(&cls.netchan.message, clc_userinfo);
            //     MSG_WriteString(&cls.netchan.message, Cvar_Userinfo());
            // }

            var buf = new QWritebuf(128);

            // if ((cls.realtime > abort_cinematic) && (cl.cinematictime > 0) &&
            //         !cl.attractloop && (cls.realtime - cl.cinematictime > 1000) &&
            //         (cls.key_dest == key_game))
            // {
            //     /* skip the rest of the cinematic */
            //     SCR_FinishCinematic();
            // }

            /* begin a client move command */
            buf.WriteByte((int)QCommon.clc_ops_e.clc_move);

            /* save the position for a checksum byte */
            // checksumIndex = buf.cursize;
            buf.WriteByte(0);

            /* let the server know what the last frame we
            got was, so the next message can be delta
            compressed */
            // if (cl_nodelta!.Bool || !cl.frame.valid || cls.demowaiting)
            if (!cl.frame.valid)
            {
                buf.WriteLong(-1); /* no compression */
            }
            else
            {
                buf.WriteLong(cl.frame.serverframe);
            }

            /* send this and the previous cmds in the message, so
               if the last packet was dropped, it can be recovered */
            // i = (cls.netchan.outgoing_sequence - 2) & (CMD_BACKUP - 1);
            // cmd = &cl.cmds[i];
            // memset(&nullcmd, 0, sizeof(nullcmd));
            // MSG_WriteDeltaUsercmd(&buf, &nullcmd, cmd);
            // oldcmd = cmd;

            // i = (cls.netchan.outgoing_sequence - 1) & (CMD_BACKUP - 1);
            // cmd = &cl.cmds[i];
            // MSG_WriteDeltaUsercmd(&buf, oldcmd, cmd);
            // oldcmd = cmd;

            // i = (cls.netchan.outgoing_sequence) & (CMD_BACKUP - 1);
            // cmd = &cl.cmds[i];
            // MSG_WriteDeltaUsercmd(&buf, oldcmd, cmd);

            /* calculate a checksum over the move commands */
            // buf.data[checksumIndex] = COM_BlockSequenceCRCByte(
            //         buf.data + checksumIndex + 1, buf.cursize - checksumIndex - 1,
            //         cls.netchan.outgoing_sequence);

            /* deliver the message */
            cls.netchan.Transmit(buf.Data);

            /* Reinit the current cmd buffer */
            // cmd = &cl.cmds[cls.netchan.outgoing_sequence & (CMD_BACKUP - 1)];
            // memset(cmd, 0, sizeof(*cmd));
        }


    }
}