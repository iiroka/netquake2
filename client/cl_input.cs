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

        private struct kbutton_t
        {
            public int[]	down; /* key nums holding it down */
            public uint	    downtime; /* msec timestamp */
            public uint	    msec; /* msec down this frame */
            public int		state;
        }

        /*
        * KEY BUTTONS
        *
        * Continuous button event tracking is complicated by the fact that two different
        * input sources (say, mouse button 1 and the control key) can both press the
        * same button, but the button should only be released when both of the
        * pressing key have been released.
        *
        * When a key event issues a button command (+forward, +attack, etc), it appends
        * its key number as a parameter to the command so it can be matched up with
        * the release.
        *
        * state bit 0 is the current state of the key
        * state bit 1 is edge triggered on the up to down transition
        * state bit 2 is edge triggered on the down to up transition
        *
        *
        * Key_Event (int key, qboolean down, unsigned time);
        *
        *   +mlook src time
        */

        private kbutton_t in_klook;
        private kbutton_t in_left, in_right, in_forward, in_back;
        private kbutton_t in_lookup, in_lookdown, in_moveleft, in_moveright;
        private kbutton_t in_strafe, in_speed, in_use, in_attack;
        private kbutton_t in_up, in_down;

        private int in_impulse;

        private void KeyDown(ref kbutton_t b, string[] args)
        {
            int k;
            if (args.Length >= 2 && !String.IsNullOrEmpty(args[1]))
            {
                k = Int32.Parse(args[1]);
            }
            else
            {
                k = -1; /* typed manually at the console for continuous down */
            }
            if (b.down == null) {
                b.down = new int[2];
            }

            if ((k == b.down[0]) || (k == b.down[1]))
            {
                return; /* repeating key */
            }

            if (b.down[0] == 0)
            {
                b.down[0] = k;
            }

            else if (b.down[1] == 0)
            {
                b.down[1] = k;
            }

            else
            {
                common.Com_Printf("Three keys down for a button!\n");
                return;
            }

            if ((b.state & 1) != 0)
            {
                return; /* still down */
            }

            /* save timestamp */
            if (args.Length >= 3 && !String.IsNullOrEmpty(args[2]))
            {
                b.downtime = UInt32.Parse(args[2]);
            }

            if (b.downtime == 0)
            {
                b.downtime = (uint)input.sys_frame_time - 100;
            }

            b.state |= 1 + 2; /* down + impulse down */
        }

        private void KeyUp(ref kbutton_t b, string[] args)
        {
            int k;
            if (args.Length >= 2 && !String.IsNullOrEmpty(args[1]))
            {
                k = Int32.Parse(args[1]);
            }

            else
            {
                /* typed manually at the console, assume for unsticking, so clear all */
                b.down = new int[2]{0,0};
                b.state = 4; /* impulse up */
                return;
            }

            if (b.down == null) {
                b.down = new int[2];
            }

            if (b.down[0] == k)
            {
                b.down[0] = 0;
            }

            else if (b.down[1] == k)
            {
                b.down[1] = 0;
            }

            else
            {
                return; /* key up without coresponding down (menu pass through) */
            }

            if (b.down[0] != 0 || b.down[1] != 0)
            {
                return; /* some other key is still holding it down */
            }

            if ((b.state & 1) == 0)
            {
                return; /* still up (this should not happen) */
            }

            /* save timestamp */
            uint uptime= 0;
            if (args.Length >= 3 && !String.IsNullOrEmpty(args[2]))
            {
                uptime = UInt32.Parse(args[2]);
            }

            if (uptime != 0)
            {
                b.msec += uptime - b.downtime;
            }

            else
            {
                b.msec += 10;
            }

            b.state &= ~1; /* now up */
            b.state |= 4; /* impulse up */
        }

        private void IN_LeftDown(string[] args)
        {
            KeyDown(ref in_left, args);
        }

        private void IN_LeftUp(string[] args)
        {
            KeyUp(ref in_left, args);
        }

        private void IN_RightDown(string[] args)
        {
            KeyDown(ref in_right, args);
        }

        private void IN_RightUp(string[] args)
        {
            KeyUp(ref in_right, args);
        }

        private void IN_ForwardDown(string[] args)
        {
            KeyDown(ref in_forward, args);
        }
        private void IN_ForwardUp(string[] args)
        {
            KeyUp(ref in_forward, args);
        }
        private void IN_BackDown(string[] args)
        {
            KeyDown(ref in_back, args);
        }
        private void IN_BackUp(string[] args)
        {
            KeyUp(ref in_back, args);
        }

        /*
        * Returns the fraction of the
        * frame that the key was down
        */
        private float CL_KeyState(ref kbutton_t key)
        {
            key.state &= 1; /* clear impulses */

            var msec = key.msec;
            key.msec = 0;

            if (key.state != 0)
            {
                /* still down */
                msec += (uint)(input.sys_frame_time - key.downtime);
                key.downtime = (uint)input.sys_frame_time;
            }

            var v = (float)msec / frame_msec;

            if (v < 0)
            {
                v = 0;
            }

            if (v > 1)
            {
                v = 1;
            }

            return v;
        }

        private cvar_t? cl_upspeed;
        private cvar_t? cl_forwardspeed;
        private cvar_t? cl_sidespeed;
        private cvar_t? cl_yawspeed;
        private cvar_t? cl_pitchspeed;
        private cvar_t? cl_run;
        private cvar_t? cl_anglespeedkey;

        /*
        * Moves the local angle positions
        */
        private void CL_AdjustAngles()
        {
            float speed;
            float up, down;

            if ((in_speed.state & 1) != 0)
            {
                speed = cls.nframetime * cl_anglespeedkey!.Float;
            }

            else
            {
                speed = cls.nframetime;
            }

            var debug = (in_right.msec != 0 || in_left.msec != 0);

            if ((in_strafe.state & 1) == 0)
            {
                cl.viewangles.SetYaw(cl.viewangles.Yaw() - speed * cl_yawspeed!.Float * CL_KeyState(ref in_right));
                cl.viewangles.SetYaw(cl.viewangles.Yaw() + speed * cl_yawspeed!.Float * CL_KeyState(ref in_left));
            }

            if ((in_klook.state & 1) != 0)
            {
                cl.viewangles.SetPitch(cl.viewangles.Pitch() - speed * cl_pitchspeed!.Float * CL_KeyState(ref in_forward));
                cl.viewangles.SetPitch(cl.viewangles.Pitch() + speed * cl_pitchspeed!.Float * CL_KeyState(ref in_back));
            }

            up = CL_KeyState(ref in_lookup);
            down = CL_KeyState(ref in_lookdown);

            cl.viewangles.SetPitch(cl.viewangles.Pitch() - speed * cl_pitchspeed!.Float * up);
            cl.viewangles.SetPitch(cl.viewangles.Pitch() + speed * cl_pitchspeed!.Float * down);

        }

        /*
        * Send the intended movement message to the server
        */
        private void CL_BaseMove(ref QShared.usercmd_t cmd)
        {
            CL_AdjustAngles();

            cmd.Clear();

            cmd.angles = new short[3]{ (short)cl.viewangles.X, (short)cl.viewangles.Y, (short)cl.viewangles.Z };

            if ((in_strafe.state & 1) != 0)
            {
                cmd.sidemove += (short)(cl_sidespeed!.Float * CL_KeyState(ref in_right));
                cmd.sidemove -= (short)(cl_sidespeed!.Float * CL_KeyState(ref in_left));
            }

            cmd.sidemove += (short)(cl_sidespeed!.Float * CL_KeyState(ref in_moveright));
            cmd.sidemove -= (short)(cl_sidespeed!.Float * CL_KeyState(ref in_moveleft));

            // cmd->upmove += cl_upspeed->value * CL_KeyState(&in_up);
            // cmd->upmove -= cl_upspeed->value * CL_KeyState(&in_down);

            if ((in_klook.state & 1) == 0)
            {
                cmd.forwardmove += (short)(cl_forwardspeed!.Float * CL_KeyState(ref in_forward));
                cmd.forwardmove -= (short)(cl_forwardspeed!.Float * CL_KeyState(ref in_back));
            }

            /* adjust for speed key / running */
            // if ((in_speed.state & 1) ^ (int)(cl_run->value))
            // {
            //     cmd->forwardmove *= 2;
            //     cmd->sidemove *= 2;
            //     cmd->upmove *= 2;
            // }
        }

        private void CL_ClampPitch()
        {
            var pitch = QShared.SHORT2ANGLE(cl.frame.playerstate.pmove.delta_angles[QShared.PITCH]);

            if (pitch > 180)
            {
                pitch -= 360;
            }

            if (cl.viewangles.Pitch() + pitch < -360)
            {
                cl.viewangles.SetPitch(cl.viewangles.Pitch() + 360); /* wrapped */
            }

            if (cl.viewangles.Pitch() + pitch > 360)
            {
                cl.viewangles.SetPitch(cl.viewangles.Pitch() - 360); /* wrapped */
            }

            if (cl.viewangles.Pitch() + pitch > 89)
            {
                cl.viewangles.SetPitch(cl.viewangles.Pitch() + 89 - pitch);
            }

            if (cl.viewangles.Pitch() + pitch < -89)
            {
                cl.viewangles.SetPitch(cl.viewangles.Pitch() - 89 - pitch);
            }
        }

        private void CL_FinishMove(ref QShared.usercmd_t cmd)
        {
            // int ms;
            // int i;

            // /* figure button bits */
            // if (in_attack.state & 3)
            // {
            //     cmd->buttons |= BUTTON_ATTACK;
            // }

            in_attack.state &= ~2;

            // if (in_use.state & 3)
            // {
            //     cmd->buttons |= BUTTON_USE;
            // }

            in_use.state &= ~2;

            // if (anykeydown && (cls.key_dest == key_game))
            // {
            //     cmd->buttons |= BUTTON_ANY;
            // }

            /* send milliseconds of time to apply the move */
            int ms = (int)(cls.nframetime * 1000);

            if (ms > 250)
            {
                ms = 100; /* time was unreasonable */
            }

            cmd.msec = (byte)ms;

            CL_ClampPitch();

            cmd.angles[0] = QShared.ANGLE2SHORT(cl.viewangles.X);
            cmd.angles[1] = QShared.ANGLE2SHORT(cl.viewangles.Y);
            cmd.angles[2] = QShared.ANGLE2SHORT(cl.viewangles.Z);

            cmd.impulse = (byte)in_impulse;
            in_impulse = 0;

            /* send the ambient light level at the player's current position */
            cmd.lightlevel = (byte)cl_lightlevel!.Int;
        }

        private void CL_InitInput()
        {
            // Cmd_AddCommand("centerview", IN_CenterView);
            // Cmd_AddCommand("force_centerview", IN_ForceCenterView);

            // Cmd_AddCommand("+moveup", IN_UpDown);
            // Cmd_AddCommand("-moveup", IN_UpUp);
            // Cmd_AddCommand("+movedown", IN_DownDown);
            // Cmd_AddCommand("-movedown", IN_DownUp);
            common.Cmd_AddCommand("+left", IN_LeftDown);
            common.Cmd_AddCommand("-left", IN_LeftUp);
            common.Cmd_AddCommand("+right", IN_RightDown);
            common.Cmd_AddCommand("-right", IN_RightUp);
            common.Cmd_AddCommand("+forward", IN_ForwardDown);
            common.Cmd_AddCommand("-forward", IN_ForwardUp);
            common.Cmd_AddCommand("+back", IN_BackDown);
            common.Cmd_AddCommand("-back", IN_BackUp);
            // Cmd_AddCommand("+lookup", IN_LookupDown);
            // Cmd_AddCommand("-lookup", IN_LookupUp);
            // Cmd_AddCommand("+lookdown", IN_LookdownDown);
            // Cmd_AddCommand("-lookdown", IN_LookdownUp);
            // Cmd_AddCommand("+strafe", IN_StrafeDown);
            // Cmd_AddCommand("-strafe", IN_StrafeUp);
            // Cmd_AddCommand("+moveleft", IN_MoveleftDown);
            // Cmd_AddCommand("-moveleft", IN_MoveleftUp);
            // Cmd_AddCommand("+moveright", IN_MoverightDown);
            // Cmd_AddCommand("-moveright", IN_MoverightUp);
            // Cmd_AddCommand("+speed", IN_SpeedDown);
            // Cmd_AddCommand("-speed", IN_SpeedUp);
            // Cmd_AddCommand("+attack", IN_AttackDown);
            // Cmd_AddCommand("-attack", IN_AttackUp);
            // Cmd_AddCommand("+use", IN_UseDown);
            // Cmd_AddCommand("-use", IN_UseUp);
            // Cmd_AddCommand("impulse", IN_Impulse);
            // Cmd_AddCommand("+klook", IN_KLookDown);
            // Cmd_AddCommand("-klook", IN_KLookUp);

            // cl_nodelta = Cvar_Get("cl_nodelta", "0", 0);
        }

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
            CL_BaseMove(ref cmd);
            // IN_Move(cmd);

            // Clamp angels for prediction
            CL_ClampPitch();

            cmd.angles = new short[3]{
                QShared.ANGLE2SHORT(cl.viewangles.X),
                QShared.ANGLE2SHORT(cl.viewangles.Y),
                QShared.ANGLE2SHORT(cl.viewangles.Z)
            };

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
            CL_BaseMove(ref cmd);
            // IN_Move(cmd);

            old_sys_frame_time = input.sys_frame_time;
        }

        private void CL_FinalizeCmd()
        {
            // CMD to fill
            ref var cmd = ref cl.cmds[cls.netchan.outgoing_sequence & (CMD_BACKUP - 1)];

            // Mouse button events
            // if (in_attack.state & 3)
            // {
            //     cmd->buttons |= BUTTON_ATTACK;
            // }

            in_attack.state &= ~2;

            // if (in_use.state & 3)
            // {
            //     cmd->buttons |= BUTTON_USE;
            // }

            in_use.state &= ~2;

            // // Keyboard events
            // if (anykeydown && cls.key_dest == key_game)
            // {
            //     cmd->buttons |= BUTTON_ANY;
            // }

            cmd.impulse = (byte)in_impulse;
            in_impulse = 0;

            // Set light level for muzzle flash
            cmd.lightlevel = (byte)cl_lightlevel!.Int;
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

            CL_FinalizeCmd();

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
            i = (cls.netchan.outgoing_sequence - 2) & (CMD_BACKUP - 1);
            cmd = ref cl.cmds[i];
            var nullmsg = new QShared.usercmd_t();
            nullmsg.angles = new short[3];
            buf.WriteDeltaUsercmd(nullmsg, cmd);
            var oldcmd = cmd;

            i = (cls.netchan.outgoing_sequence - 1) & (CMD_BACKUP - 1);
            cmd = ref cl.cmds[i];
            buf.WriteDeltaUsercmd(oldcmd, cmd);
            oldcmd = cmd;

            i = (cls.netchan.outgoing_sequence) & (CMD_BACKUP - 1);
            cmd = ref cl.cmds[i];
            buf.WriteDeltaUsercmd(oldcmd, cmd);

            /* calculate a checksum over the move commands */
            // buf.data[checksumIndex] = COM_BlockSequenceCRCByte(
            //         buf.data + checksumIndex + 1, buf.cursize - checksumIndex - 1,
            //         cls.netchan.outgoing_sequence);

            /* deliver the message */
            cls.netchan.Transmit(buf.Data);

            /* Reinit the current cmd buffer */
            cl.cmds[cls.netchan.outgoing_sequence & (CMD_BACKUP - 1)].Clear();
        }


    }
}