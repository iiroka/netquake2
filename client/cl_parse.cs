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
 * This file implements the entity and network protocol parsing
 *
 * =======================================================================
 */
using System.Numerics;

namespace Quake2 {

    partial class QClient {

        private readonly string[] svc_strings = {
            "svc_bad",

            "svc_muzzleflash",
            "svc_muzzlflash2",
            "svc_temp_entity",
            "svc_layout",
            "svc_inventory",

            "svc_nop",
            "svc_disconnect",
            "svc_reconnect",
            "svc_sound",
            "svc_print",
            "svc_stufftext",
            "svc_serverdata",
            "svc_configstring",
            "svc_spawnbaseline",
            "svc_centerprint",
            "svc_download",
            "svc_playerinfo",
            "svc_packetentities",
            "svc_deltapacketentities",
            "svc_frame"
        };

        /*
        * Returns the entity number and the header bits
        */
        private int CL_ParseEntityBits(ref QReadbuf msg, out uint total)
        {
            total = (uint)msg.ReadByte();

            if ((total & QCommon.U_MOREBITS1) != 0)
            {
                var b = (uint)msg.ReadByte();
                total |= b << 8;
            }

            if ((total & QCommon.U_MOREBITS2) != 0)
            {
                var b = (uint)msg.ReadByte();
                total |= b << 16;
            }

            if ((total & QCommon.U_MOREBITS3) != 0)
            {
                var b = (uint)msg.ReadByte();
                total |= b << 24;
            }

            /* count the bits for net profiling */
            // for (i = 0; i < 32; i++)
            // {
            //     if (total & (1u << i))
            //     {
            //         bitcounts[i]++;
            //     }
            // }

            int number;
            if ((total & QCommon.U_NUMBER16) != 0)
            {
                number = msg.ReadShort();
            }

            else
            {
                number = msg.ReadByte();
            }

            return number;
        }

        /*
        * Can go from either a baseline or a previous packet_entity
        */
        private void CL_ParseDelta(ref QReadbuf msg, in QShared.entity_state_t from, ref QShared.entity_state_t to, int number, uint bits)
        {
            /* set everything to the state we are delta'ing from */
            to = from;

            to.old_origin = from.origin;
            to.number = number;

            if ((bits & QCommon.U_MODEL) != 0)
            {
                to.modelindex = msg.ReadByte();
            }

            if ((bits & QCommon.U_MODEL2) != 0)
            {
                to.modelindex2 = msg.ReadByte();
            }

            if ((bits & QCommon.U_MODEL3) != 0)
            {
                to.modelindex3 = msg.ReadByte();
            }

            if ((bits & QCommon.U_MODEL4) != 0)
            {
                to.modelindex4 = msg.ReadByte();
            }

            if ((bits & QCommon.U_FRAME8) != 0)
            {
                to.frame = msg.ReadByte();
            }

            if ((bits & QCommon.U_FRAME16) != 0)
            {
                to.frame = msg.ReadShort();
            }

            /* used for laser colors */
            if ((bits & QCommon.U_SKIN8) != 0 && (bits & QCommon.U_SKIN16) != 0)
            {
                to.skinnum = msg.ReadLong();
            }
            else if ((bits & QCommon.U_SKIN8) != 0)
            {
                to.skinnum = msg.ReadByte();
            }
            else if ((bits & QCommon.U_SKIN16) != 0)
            {
                to.skinnum = msg.ReadShort();
            }

            if ((bits & (QCommon.U_EFFECTS8 | QCommon.U_EFFECTS16)) == (QCommon.U_EFFECTS8 | QCommon.U_EFFECTS16))
            {
                to.effects = (uint)msg.ReadLong();
            }
            else if ((bits & QCommon.U_EFFECTS8) != 0)
            {
                to.effects = (uint)msg.ReadByte();
            }
            else if ((bits & QCommon.U_EFFECTS16) != 0)
            {
                to.effects = (uint)msg.ReadShort();
            }

            if ((bits & (QCommon.U_RENDERFX8 | QCommon.U_RENDERFX16)) == (QCommon.U_RENDERFX8 | QCommon.U_RENDERFX16))
            {
                to.renderfx = msg.ReadLong();
            }
            else if ((bits & QCommon.U_RENDERFX8) != 0)
            {
                to.renderfx = msg.ReadByte();
            }
            else if ((bits & QCommon.U_RENDERFX16) != 0)
            {
                to.renderfx = msg.ReadShort();
            }

            if ((bits & QCommon.U_ORIGIN1) != 0)
            {
                to.origin.X = msg.ReadCoord();
            }

            if ((bits & QCommon.U_ORIGIN2) != 0)
            {
                to.origin.Y = msg.ReadCoord();
            }

            if ((bits & QCommon.U_ORIGIN3) != 0)
            {
                to.origin.Z = msg.ReadCoord();
            }

            if ((bits & QCommon.U_ANGLE1) != 0)
            {
                to.angles.X = msg.ReadAngle();
            }

            if ((bits & QCommon.U_ANGLE2) != 0)
            {
                to.angles.Y = msg.ReadAngle();
            }

            if ((bits & QCommon.U_ANGLE3) != 0)
            {
                to.angles.Z = msg.ReadAngle();
            }

            if ((bits & QCommon.U_OLDORIGIN) != 0)
            {
                to.old_origin = msg.ReadPos();
            }

            if ((bits & QCommon.U_SOUND) != 0)
            {
                to.sound = msg.ReadByte();
            }

            if ((bits & QCommon.U_EVENT) != 0)
            {
                to.ev = msg.ReadByte();
            }
            else
            {
                to.ev = 0;
            }

            if ((bits & QCommon.U_SOLID) != 0)
            {
                to.solid = msg.ReadShort();
            }
        }

        /*
        * Parses deltas from the given base and adds the resulting entity to
        * the current frame
        */
        private void CL_DeltaEntity(ref QReadbuf msg, ref frame_t frame, int newnum, in QShared.entity_state_t old, uint bits)
        {
            // centity_t *ent;
            // entity_state_t *state;

            ref var ent = ref cl_entities[newnum];

            ref var state = ref cl_parse_entities[cl.parse_entities & (MAX_PARSE_ENTITIES - 1)];
            cl.parse_entities++;
            frame.num_entities++;

            CL_ParseDelta(ref msg, old, ref state, newnum, bits);

            /* some data changes will force no lerping */
            if ((state.modelindex != ent.current.modelindex) ||
                (state.modelindex2 != ent.current.modelindex2) ||
                (state.modelindex3 != ent.current.modelindex3) ||
                (state.modelindex4 != ent.current.modelindex4) ||
                (state.ev == (int)QShared.entity_event_t.EV_PLAYER_TELEPORT) ||
                (state.ev == (int)QShared.entity_event_t.EV_OTHER_TELEPORT) ||
                (Math.Abs((int)(state.origin.X - ent.current.origin.X)) > 512) ||
                (Math.Abs((int)(state.origin.Y - ent.current.origin.Y)) > 512) ||
                (Math.Abs((int)(state.origin.Z - ent.current.origin.Z)) > 512)
                )
            {
                ent.serverframe = -99;
            }

            /* wasn't in last update, so initialize some things */
            if (ent.serverframe != cl.frame.serverframe - 1)
            {
                ent.trailcount = 1024; /* for diminishing rocket / grenade trails */

                /* duplicate the current state so
                lerping doesn't hurt anything */
                ent.prev = (QShared.entity_state_t)state.Clone();

                if (state.ev == (int)QShared.entity_event_t.EV_OTHER_TELEPORT)
                {
                    ent.prev.origin = state.origin;
                    ent.lerp_origin = state.origin;
                }
                else
                {
                    ent.prev.origin = state.old_origin;
                    ent.lerp_origin = state.old_origin;
                }
            }
            else
            {
                /* shuffle the last state to previous */
                ent.prev = (QShared.entity_state_t)ent.current.Clone();
            }

            ent.serverframe = cl.frame.serverframe;
            ent.current = (QShared.entity_state_t)state.Clone();
        }

        /*
        * An svc_packetentities has just been
        * parsed, deal with the rest of the
        * data stream.
        */
        private void CL_ParsePacketEntities(ref QReadbuf msg, in frame_t? oldframe, ref frame_t newframe)
        {
            // unsigned int newnum;
            // unsigned bits;
            // entity_state_t
            // *oldstate = NULL;
            // int oldindex, oldnum;

            newframe.parse_entities = cl.parse_entities;
            newframe.num_entities = 0;

            /* delta from the entities present in oldframe */
            int oldindex = 0;
            int oldnum;
            QShared.entity_state_t? oldstate = null;

            if (oldframe == null)
            {
                oldnum = 99999;
            }

            else
            {
                if (oldindex >= oldframe.num_entities)
                {
                    oldnum = 99999;
                }

                else
                {
                    oldstate = cl_parse_entities[(oldframe.parse_entities + oldindex) & (MAX_PARSE_ENTITIES - 1)];
                    oldnum = oldstate!.number;
                }
            }

            while (true)
            {
                var newnum = CL_ParseEntityBits(ref msg, out var bits);

                if (newnum >= QShared.MAX_EDICTS)
                {
                    common.Com_Error(QShared.ERR_DROP, $"CL_ParsePacketEntities: bad number:{newnum}");
                }

                if (msg.Count > msg.Size)
                {
                    common.Com_Error(QShared.ERR_DROP, "CL_ParsePacketEntities: end of message");
                }

                if (newnum == 0)
                {
                    break;
                }

                while (oldnum < newnum)
                {
                    /* one or more entities from the old packet are unchanged */
                    if (cl_shownet?.Int == 3)
                    {
                        common.Com_Printf($"   unchanged: {oldnum}\n");
                    }

                    CL_DeltaEntity(ref msg, ref newframe, oldnum, oldstate!, 0);

                    oldindex++;

                    if (oldframe == null || oldindex >= oldframe.num_entities)
                    {
                        oldnum = 99999;
                    }

                    else
                    {
                        oldstate = cl_parse_entities[(oldframe.parse_entities + oldindex) & (MAX_PARSE_ENTITIES - 1)];
                        oldnum = oldstate.number;
                    }
                }

                if ((bits & QCommon.U_REMOVE) != 0)
                {
                    /* the entity present in oldframe is not in the current frame */
                    if (cl_shownet?.Int == 3)
                    {
                        common.Com_Printf($"   remove: {newnum}\n");
                    }

                    if (oldnum != newnum)
                    {
                        common.Com_Printf("U_REMOVE: oldnum != newnum\n");
                    }

                    oldindex++;

                    if (oldframe == null || oldindex >= oldframe.num_entities)
                    {
                        oldnum = 99999;
                    }

                    else
                    {
                        oldstate = cl_parse_entities[(oldframe.parse_entities + oldindex) & (MAX_PARSE_ENTITIES - 1)];
                        oldnum = oldstate.number;
                    }

                    continue;
                }

                if (oldnum == newnum)
                {
                    /* delta from previous state */
                    if (cl_shownet?.Int == 3)
                    {
                        common.Com_Printf($"   delta: {newnum}\n");
                    }

                    CL_DeltaEntity(ref msg, ref newframe, newnum, oldstate!, bits);

                    oldindex++;

                    if (oldframe == null || oldindex >= oldframe.num_entities)
                    {
                        oldnum = 99999;
                    }

                    else
                    {
                        oldstate = cl_parse_entities[(oldframe.parse_entities + oldindex) & (MAX_PARSE_ENTITIES - 1)];
                        oldnum = oldstate.number;
                    }

                    continue;
                }

                if (oldnum > newnum)
                {
                    /* delta from baseline */
                    if (cl_shownet?.Int == 3)
                    {
                        common.Com_Printf($"   baseline: {newnum}\n");
                    }

                    CL_DeltaEntity(ref msg, ref newframe, newnum, cl_entities[newnum].baseline, bits);
                    continue;
                }
            }

            /* any remaining entities in the old frame are copied over */
            while (oldnum != 99999)
            {
                /* one or more entities from the old packet are unchanged */
                if (cl_shownet?.Int == 3)
                {
                    common.Com_Printf($"   unchanged: {oldnum}\n");
                }

                CL_DeltaEntity(ref msg, ref newframe, oldnum, oldstate!, 0);

                oldindex++;

                if (oldframe == null || oldindex >= oldframe.num_entities)
                {
                    oldnum = 99999;
                }

                else
                {
                    oldstate = cl_parse_entities[(oldframe.parse_entities + oldindex) & (MAX_PARSE_ENTITIES - 1)];
                    oldnum = oldstate.number;
                }
            }
        }

        private void CL_ParsePlayerstate(ref QReadbuf msg, in frame_t? oldframe, ref frame_t newframe)
        {
            // int flags;
            // player_state_t *state;
            // int i;
            // int statbits;

            ref var state = ref newframe.playerstate;

            /* clear to old value before delta parsing */
            if (oldframe != null)
            {
                state = (QShared.player_state_t)oldframe.playerstate.Clone();
            }

            else
            {
                state = new QShared.player_state_t();
                state.pmove.origin = new short[3];
                state.pmove.velocity = new short[3];
                state.pmove.delta_angles = new short[3];
            }

            var flags = msg.ReadShort();

            /* parse the pmove_state_t */
            if ((flags & QCommon.PS_M_TYPE) != 0)
            {
                state.pmove.pm_type = (QShared.pmtype_t)msg.ReadByte();
            }

            if ((flags & QCommon.PS_M_ORIGIN) != 0)
            {
                state.pmove.origin[0] = (short)msg.ReadShort();
                state.pmove.origin[1] = (short)msg.ReadShort();
                state.pmove.origin[2] = (short)msg.ReadShort();
            }

            if ((flags & QCommon.PS_M_VELOCITY) != 0)
            {
                state.pmove.velocity[0] = (short)msg.ReadShort();
                state.pmove.velocity[1] = (short)msg.ReadShort();
                state.pmove.velocity[2] = (short)msg.ReadShort();
            }

            if ((flags & QCommon.PS_M_TIME) != 0)
            {
                state.pmove.pm_time = (byte)msg.ReadByte();
            }

            if ((flags & QCommon.PS_M_FLAGS) != 0)
            {
                state.pmove.pm_flags = (byte)msg.ReadByte();
            }

            if ((flags & QCommon.PS_M_GRAVITY) != 0)
            {
                state.pmove.gravity = (short)msg.ReadShort();
            }

            if ((flags & QCommon.PS_M_DELTA_ANGLES) != 0)
            {
                state.pmove.delta_angles[0] = (short)msg.ReadShort();
                state.pmove.delta_angles[1] = (short)msg.ReadShort();
                state.pmove.delta_angles[2] = (short)msg.ReadShort();
            }

            if (cl.attractloop)
            {
                state.pmove.pm_type = QShared.pmtype_t.PM_FREEZE; /* demo playback */
            }

            /* parse the rest of the player_state_t */
            if ((flags & QCommon.PS_VIEWOFFSET) != 0)
            {
                state.viewoffset.X = msg.ReadChar() * 0.25f;
                state.viewoffset.Y = msg.ReadChar() * 0.25f;
                state.viewoffset.Z = msg.ReadChar() * 0.25f;
            }

            if ((flags & QCommon.PS_VIEWANGLES) != 0)
            {
                state.viewangles.X = msg.ReadAngle16();
                state.viewangles.Y = msg.ReadAngle16();
                state.viewangles.Z = msg.ReadAngle16();
            }

            if ((flags & QCommon.PS_KICKANGLES) != 0)
            {
                state.kick_angles.X = msg.ReadChar() * 0.25f;
                state.kick_angles.Y = msg.ReadChar() * 0.25f;
                state.kick_angles.Z = msg.ReadChar() * 0.25f;
            }

            if ((flags & QCommon.PS_WEAPONINDEX) != 0)
            {
                state.gunindex = msg.ReadByte();
            }

            if ((flags & QCommon.PS_WEAPONFRAME) != 0)
            {
                state.gunframe = msg.ReadByte();
                state.gunoffset.X = msg.ReadChar() * 0.25f;
                state.gunoffset.Y = msg.ReadChar() * 0.25f;
                state.gunoffset.Z = msg.ReadChar() * 0.25f;
                state.gunangles.X = msg.ReadChar() * 0.25f;
                state.gunangles.Y = msg.ReadChar() * 0.25f;
                state.gunangles.Z = msg.ReadChar() * 0.25f;
            }

            if ((flags & QCommon.PS_BLEND) != 0)
            {
                state.blend[0] = msg.ReadByte() / 255.0f;
                state.blend[1] = msg.ReadByte() / 255.0f;
                state.blend[2] = msg.ReadByte() / 255.0f;
                state.blend[3] = msg.ReadByte() / 255.0f;
            }

            if ((flags & QCommon.PS_FOV) != 0)
            {
                state.fov = (float)msg.ReadByte();
            }

            if ((flags & QCommon.PS_RDFLAGS) != 0)
            {
                state.rdflags = msg.ReadByte();
            }

            /* parse stats */
            int statbits = msg.ReadLong();

            for (int i = 0; i < QShared.MAX_STATS; i++)
            {
                if ((statbits & (1u << i)) != 0)
                {
                    state.stats[i] = (short)msg.ReadShort();
                }
            }
        }

        private void CL_ParseFrame(ref QReadbuf msg)
        {
            frame_t? old = null;

            cl.frame = new frame_t();

            cl.frame.serverframe = msg.ReadLong();
            cl.frame.deltaframe = msg.ReadLong();
            cl.frame.servertime = cl.frame.serverframe * 100;

            /* BIG HACK to let old demos continue to work */
            if (cls.serverProtocol != 26)
            {
                cl.surpressCount = msg.ReadByte();
            }

            if (cl_shownet?.Int == 3)
            {
                common.Com_Printf($"   frame:{cl.frame.serverframe}  delta:{cl.frame.deltaframe}\n");
            }

            /* If the frame is delta compressed from data that we
            no longer have available, we must suck up the rest of
            the frame, but not use it, then ask for a non-compressed
            message */
            if (cl.frame.deltaframe <= 0)
            {
                cl.frame.valid = true; /* uncompressed frame */
            //     old = NULL;
            //     cls.demowaiting = false; /* we can start recording now */
            }
            else
            {
                old = cl.frames[cl.frame.deltaframe & QCommon.UPDATE_MASK];

                if (!old.valid)
                {
                    /* should never happen */
                    common.Com_Printf("Delta from invalid frame (not supposed to happen!).\n");
                }

                if (old.serverframe != cl.frame.deltaframe)
                {
                    /* The frame that the server did the delta from
                    is too old, so we can't reconstruct it properly. */
                    common.Com_Printf("Delta frame too old.\n");
                }
                else if (cl.parse_entities - old.parse_entities > MAX_PARSE_ENTITIES - 128)
                {
                    common.Com_Printf("Delta parse_entities too old.\n");
                }
                else
                {
                    cl.frame.valid = true; /* valid delta parse */
                }
            }

            /* clamp time */
            if (cl.time > cl.frame.servertime)
            {
                cl.time = cl.frame.servertime;
            }

            else if (cl.time < cl.frame.servertime - 100)
            {
                cl.time = cl.frame.servertime - 100;
            }

            /* read areabits */
            var len = msg.ReadByte();
            cl.frame.areabits = msg.ReadData(len);

            /* read playerinfo */
            var cmd = msg.ReadByte();
            SHOWNET(svc_strings[cmd], msg);

            if (cmd != (int)QCommon.svc_ops_e.svc_playerinfo)
            {
                common.Com_Error(QShared.ERR_DROP, $"CL_ParseFrame: 0x{cmd.ToString("X")} not playerinfo");
            }

            CL_ParsePlayerstate(ref msg, old, ref cl.frame);

            /* read packet entities */
            cmd = msg.ReadByte();
            SHOWNET(svc_strings[cmd], msg);

            if (cmd != (int)QCommon.svc_ops_e.svc_packetentities)
            {
                common.Com_Error(QShared.ERR_DROP, $"CL_ParseFrame: 0x{cmd.ToString("X")} not packetentities");
            }

            CL_ParsePacketEntities(ref msg, old, ref cl.frame);

            /* save the frame off in the backup array for later delta comparisons */
            cl.frames[cl.frame.serverframe & QCommon.UPDATE_MASK] = (frame_t)cl.frame.Clone();

            if (cl.frame.valid)
            {
                /* getting a valid frame message ends the connection process */
                if (cls.state != connstate_t.ca_active)
                {
                    cls.state = connstate_t.ca_active;
                    cl.force_refdef = true;
                    cl.predicted_origin.X = cl.frame.playerstate.pmove.origin[0] * 0.125f;
                    cl.predicted_origin.Y = cl.frame.playerstate.pmove.origin[1] * 0.125f;
                    cl.predicted_origin.Z = cl.frame.playerstate.pmove.origin[2] * 0.125f;
                    cl.predicted_angles = cl.frame.playerstate.viewangles;

                    if ((cls.disable_servercount != cl.servercount) && cl.refresh_prepped)
                    {
                        SCR_EndLoadingPlaque();  /* get rid of loading plaque */
                    }

            //         cl.sound_prepped = true;

            //         if (paused_at_load)
            //         {
            //             if (cl_loadpaused->value == 1)
            //             {
            //                 Cvar_Set("paused", "0");
            //             }

            //             paused_at_load = false;
            //         }
                }

            //     /* fire entity events */
            //     CL_FireEntityEvents(&cl.frame);

                if (!(!(cl_predict?.Bool ?? false) ||
                    ((cl.frame.playerstate.pmove.pm_flags &
                    QShared.PMF_NO_PREDICTION)) != 0))
                {
                    CL_CheckPredictionError();
                }
            }
        }

        private void CL_ParseServerData(ref QReadbuf msg)
        {
            /* Clear all key states */
            // In_FlushQueue();

            common.Com_DPrintf("Serverdata packet received.\n");

            /* wipe the  struct */
            CL_ClearState();
            cls.state = connstate_t.ca_connected;

            /* parse protocol version number */
            cls.serverProtocol = msg.ReadLong();

            /* another demo hack */
            if (common.ServerState != 0 && (QCommon.PROTOCOL_VERSION == 34))
            {
            }
            else if (cls.serverProtocol != QCommon.PROTOCOL_VERSION)
            {
                common.Com_Error(QShared.ERR_DROP, $"Server returned version {cls.serverProtocol}, not {QCommon.PROTOCOL_VERSION}");
            }

            cl.servercount = msg.ReadLong();
            cl.attractloop = msg.ReadByte() != 0;

            /* game directory */
            var str = msg.ReadString();
            cl.gamedir = str;

            /* set gamedir */
            // if ((*str && (!fs_gamedirvar->string || !*fs_gamedirvar->string ||
            //     strcmp(fs_gamedirvar->string, str))) ||
            //     (!*str && (fs_gamedirvar->string && !*fs_gamedirvar->string)))
            // {
            //     Cvar_Set("game", str);
            // }

            /* parse player entity number */
            cl.playernum = msg.ReadShort();

            /* get the full level name */
            str = msg.ReadString();

            if (cl.playernum == -1)
            {
                /* playing a cinematic or showing a pic, not a level */
                SCR_PlayCinematic(str);
            }
            else
            {
                /* seperate the printfs so the server
                * message can have a color */
                // Com_Printf("\n\n\35\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\37\n\n");
                common.Com_Printf($"\u0002{str}\n");

                /* need to prep refresh at next oportunity */
                cl.refresh_prepped = false;
            }
        }

        private void CL_ParseBaseline(ref QReadbuf msg)
        {
            int newnum = CL_ParseEntityBits(ref msg, out var bits);
            CL_ParseDelta(ref msg, new QShared.entity_state_t(), ref cl_entities[newnum].baseline, newnum, bits);
        }

        private void CL_LoadClientinfo(ref clientinfo_t ci, in string str)
        {
            var s = str;
            ci.cinfo = str;

            /* isolate the player's name */
            var t = s.IndexOf("\\");
            // Q_strlcpy(ci->name, s, sizeof(ci->name));
            // t = strstr(s, "\\");

            if (t >= 0)
            {
                ci.name = s.Substring(0, t);
                s = s.Substring(t + 1);
            }

            if (cl_noskins!.Bool || t < 0 || s.Length == 0)
            {
            //     strcpy(weapon_filename, "players/male/weapon.md2");
            //     strcpy(skin_filename, "players/male/grunt.pcx");
                ci.iconname = "/players/male/grunt_i.pcx";
                ci.model = vid.R_RegisterModel("players/male/tris.md2");
                ci.weaponmodel = new model_s?[1];
                ci.weaponmodel[0] = vid.R_RegisterModel("players/male/weapon.md2");
                ci.skin = vid.R_RegisterSkin("players/male/grunt.pcx");
            //     ci->icon = Draw_FindPic(ci->iconname);
            }
            else
            {
                /* isolate the model name */
                string model_name, skin_name;
            //     strcpy(model_name, s);
                t = s.IndexOf("/");
                if (t < 0)
                {
                    t = s.IndexOf("\\");
                }
                if (t < 0)
                {
                    model_name = "";
                    skin_name = s;
                }
                else
                {
                    model_name = s.Substring(0, t);
                    skin_name = s.Substring(t + 1);
                }

                /* isolate the skin name */

                /* model file */
                var model_filename = $"players/{model_name}/tris.md2";
                ci.model = vid.R_RegisterModel(model_filename);

                if (ci.model == null)
                {
                    model_name = "male";
                    model_filename = "players/male/tris.md2";
                    ci.model = vid.R_RegisterModel(model_filename);
                }

                /* skin file */
                var skin_filename = $"players/{model_name}/{skin_name}.pcx";
                ci.skin = vid.R_RegisterSkin(skin_filename);

                /* if we don't have the skin and the model wasn't male,
                * see if the male has it (this is for CTF's skins) */
            //     if (!ci->skin && Q_stricmp(model_name, "male"))
            //     {
            //         /* change model to male */
            //         strcpy(model_name, "male");
            //         Com_sprintf(model_filename, sizeof(model_filename),
            //                 "players/male/tris.md2");
            //         ci->model = R_RegisterModel(model_filename);

            //         /* see if the skin exists for the male model */
            //         Com_sprintf(skin_filename, sizeof(skin_filename),
            //                 "players/%s/%s.pcx", model_name, skin_name);
            //         ci->skin = R_RegisterSkin(skin_filename);
            //     }

                /* if we still don't have a skin, it means that the male model didn't have
                * it, so default to grunt */
                if (ci.skin == null)
                {
                    /* see if the skin exists for the male model */
                    skin_filename = $"players/{model_name}/grunt.pcx";
                    ci.skin = vid.R_RegisterSkin(skin_filename);
                }

                /* weapon file */
                for (int i = 0; i < cl_weaponmodels.Length; i++)
                {
                    var weapon_filename = $"players/{model_name}/{cl_weaponmodels[i]}";
                    ci.weaponmodel[i] = vid.R_RegisterModel(weapon_filename);

            //         if (!ci->weaponmodel[i] && (strcmp(model_name, "cyborg") == 0))
            //         {
            //             /* try male */
            //             Com_sprintf(weapon_filename, sizeof(weapon_filename),
            //                     "players/male/%s", cl_weaponmodels[i]);
            //             ci->weaponmodel[i] = R_RegisterModel(weapon_filename);
            //         }

                    if (!cl_vwep!.Bool)
                    {
                        break; /* only one when vwep is off */
                    }
                }

            //     /* icon file */
            //     Com_sprintf(ci->iconname, sizeof(ci->iconname),
            //             "/players/%s/%s_i.pcx", model_name, skin_name);
            //     ci->icon = Draw_FindPic(ci->iconname);
            }

            /* must have loaded all data types to be valid */
            // if (!ci->skin || !ci->icon || !ci->model || !ci->weaponmodel[0])
            // {
            //     ci->skin = NULL;
            //     ci->icon = NULL;
            //     ci->model = NULL;
            //     ci->weaponmodel[0] = NULL;
            //     return;
            // }
        }

        /*
        * Load the skin, icon, and model for a client
        */
        private void CL_ParseClientinfo(int player)
        {
            CL_LoadClientinfo(ref cl.clientinfo[player], cl.configstrings[player + QShared.CS_PLAYERSKINS]);
        }

        private void CL_ParseConfigString(ref QReadbuf msg)
        {
            // int i, length;
            // char *s;
            // char olds[MAX_QPATH];

            var i = msg.ReadShort();

            if ((i < 0) || (i >= QShared.MAX_CONFIGSTRINGS))
            {
                common.Com_Error(QShared.ERR_DROP, "configstring > MAX_CONFIGSTRINGS");
            }

            var s = msg.ReadString();

            var olds = cl.configstrings[i];

            cl.configstrings[i] = s;

            /* do something apropriate */
            if ((i >= QShared.CS_LIGHTS) && (i < QShared.CS_LIGHTS + QShared.MAX_LIGHTSTYLES))
            {
                CL_SetLightstyle(i - QShared.CS_LIGHTS);
            }
            // else if (i == CS_CDTRACK)
            // {
            //     if (cl.refresh_prepped)
            //     {
            //         OGG_PlayTrack((int)strtol(cl.configstrings[CS_CDTRACK], (char **)NULL, 10));
            //     }
            // }
            else if ((i >= QShared.CS_MODELS) && (i < QShared.CS_MODELS + QShared.MAX_MODELS))
            {
                if (cl.refresh_prepped)
                {
                    cl.model_draw[i - QShared.CS_MODELS] = vid.R_RegisterModel(cl.configstrings[i]);

                    if (cl.configstrings[i][0] == '*')
                    {
                        // cl.model_clip[i - QShared.CS_MODELS] = CM_InlineModel(cl.configstrings[i]);
                    }

                    else
                    {
                        // cl.model_clip[i - CS_MODELS] = NULL;
                    }
                }
            }
            // else if ((i >= CS_SOUNDS) && (i < CS_SOUNDS + MAX_MODELS))
            // {
            //     if (cl.refresh_prepped)
            //     {
            //         cl.sound_precache[i - CS_SOUNDS] =
            //             S_RegisterSound(cl.configstrings[i]);
            //     }
            // }
            // else if ((i >= CS_IMAGES) && (i < CS_IMAGES + MAX_MODELS))
            // {
            //     if (cl.refresh_prepped)
            //     {
            //         cl.image_precache[i - CS_IMAGES] = Draw_FindPic(cl.configstrings[i]);
            //     }
            // }
            // else if ((i >= CS_PLAYERSKINS) && (i < CS_PLAYERSKINS + MAX_CLIENTS))
            // {
            //     if (cl.refresh_prepped && strcmp(olds, s))
            //     {
            //         CL_ParseClientinfo(i - CS_PLAYERSKINS);
            //     }
            // }
        }

        private void CL_ParseStartSoundPacket(ref QReadbuf msg)
        {
            // vec3_t pos_v;
            // float *pos;
            // int channel, ent;
            // int sound_num;
            // float volume;
            // float attenuation;
            // int flags;
            // float ofs;

            var flags = msg.ReadByte();
            var sound_num = msg.ReadByte();

            var volume = QCommon.DEFAULT_SOUND_PACKET_VOLUME;
            if ((flags & QCommon.SND_VOLUME) != 0)
            {
                volume = msg.ReadByte() / 255.0f;
            }

            var attenuation = QCommon.DEFAULT_SOUND_PACKET_ATTENUATION;
            if ((flags & QCommon.SND_ATTENUATION) != 0)
            {
                attenuation = msg.ReadByte() / 64.0f;
            }

            float ofs = 0;
            if ((flags & QCommon.SND_OFFSET) != 0)
            {
                ofs = msg.ReadByte() / 1000.0f;
            }

            int channel = 0;
            int ent = 0;
            if ((flags & QCommon.SND_ENT) != 0)
            {
                /* entity reletive */
                channel = msg.ReadShort();
                ent = channel >> 3;

                if (ent > QShared.MAX_EDICTS)
                {
                    common.Com_Error(QShared.ERR_DROP, $"CL_ParseStartSoundPacket: ent = {ent}");
                }

                channel &= 7;
            }

            Vector3? pos = null;
            if ((flags & QCommon.SND_POS) != 0)
            {
                /* positioned in space */
                pos = msg.ReadPos();
            }

            // if (!cl.sound_precache[sound_num])
            // {
            //     return;
            // }

            // S_StartSound(pos, ent, channel, cl.sound_precache[sound_num],
            //         volume, attenuation, ofs);
        }


        private void SHOWNET(string s, in QReadbuf msg)
        {
            if (cl_shownet!.Int >= 2)
            {
                common.Com_Printf($"{msg.Count-1}:{s}\n");
            }
        }

        private void CL_ParseServerMessage(ref QReadbuf msg)
        {
            // int cmd;
            // char *s;
            // int i;

            /* if recording demos, copy the message out */
            if (cl_shownet!.Int == 1)
            {
                common.Com_Printf($"{msg.Size} ");
            }

            else if (cl_shownet!.Int >= 2)
            {
                common.Com_Printf("------------------\n");
            }

            /* parse the message */
            while (true)
            {
                if (msg.Count > msg.Size)
                {
                    common.Com_Error(QShared.ERR_DROP, "CL_ParseServerMessage: Bad server message");
                    break;
                }

                var cmd = msg.ReadByte();

                if (cmd == -1)
                {
                    SHOWNET("END OF MESSAGE", msg);
                    break;
                }

                if (cl_shownet!.Int >= 2)
                {
                    if (cmd < 0 || cmd >= svc_strings.Length)
                    {
                        common.Com_Printf($"{msg.Count - 1}:BAD CMD {cmd}\n");
                    }

                    else
                    {
                        SHOWNET(svc_strings[cmd], msg);
                    }
                }

                /* other commands */
                switch (cmd)
                {
                    case (int)QCommon.svc_ops_e.svc_nop:
                        break;

                    case (int)QCommon.svc_ops_e.svc_disconnect:
                        common.Com_Error(QShared.ERR_DISCONNECT, "Server disconnected\n");
                        break;

                    case (int)QCommon.svc_ops_e.svc_reconnect:
                        common.Com_Printf("Server disconnected, reconnecting\n");

                    //     if (cls.download)
                    //     {
                    //         /* close download */
                    //         fclose(cls.download);
                    //         cls.download = NULL;
                    //     }

                        cls.state = connstate_t.ca_connecting;
                        cls.connect_time = -99999; /* CL_CheckForResend() will fire immediately */
                        break;

                    case (int)QCommon.svc_ops_e.svc_print:
                        var i = msg.ReadByte();

                        if (i == QShared.PRINT_CHAT)
                        {
                            // S_StartLocalSound("misc/talk.wav");
                            con.ormask = 128;
                        }

                        common.Com_Printf(msg.ReadString());
                        con.ormask = 0;
                        break;

                    // case (int)QCommon.svc_ops_e.svc_centerprint:
                    //     SCR_CenterPrint(MSG_ReadString(&net_message));
                    //     break;

                    case (int)QCommon.svc_ops_e.svc_stufftext:
                        var s = msg.ReadString();
                        common.Com_DPrintf($"stufftext: {s}\n");
                        common.Cbuf_AddText(s);
                        break;

                    case (int)QCommon.svc_ops_e.svc_serverdata:
                        common.Cbuf_Execute();  /* make sure any stuffed commands are done */
                        CL_ParseServerData(ref msg);
                        break;

                    case (int)QCommon.svc_ops_e.svc_configstring:
                        CL_ParseConfigString(ref msg);
                        break;

                    case (int)QCommon.svc_ops_e.svc_sound:
                        CL_ParseStartSoundPacket(ref msg);
                        break;

                    case (int)QCommon.svc_ops_e.svc_spawnbaseline:
                        CL_ParseBaseline(ref msg);
                        break;

                    case (int)QCommon.svc_ops_e.svc_temp_entity:
                        CL_ParseTEnt(ref msg);
                        break;

                    case (int)QCommon.svc_ops_e.svc_muzzleflash:
                        CL_AddMuzzleFlash(ref msg);
                        break;

                    case (int)QCommon.svc_ops_e.svc_muzzleflash2:
                        CL_AddMuzzleFlash2(ref msg);
                        break;

                    // case svc_download:
                    //     CL_ParseDownload();
                    //     break;

                    case (int)QCommon.svc_ops_e.svc_frame:
                        CL_ParseFrame(ref msg);
                        break;

                    // case (int)QCommon.svc_ops_e.svc_inventory:
                    //     CL_ParseInventory();
                    //     break;

                    // case (int)QCommon.svc_ops_e.svc_layout:
                    //     s = MSG_ReadString(&net_message);
                    //     Q_strlcpy(cl.layout, s, sizeof(cl.layout));
                    //     break;

                    case (int)QCommon.svc_ops_e.svc_playerinfo:
                    case (int)QCommon.svc_ops_e.svc_packetentities:
                    case (int)QCommon.svc_ops_e.svc_deltapacketentities:
                        common.Com_Error(QShared.ERR_DROP, "Out of place frame data");
                        break;

                    default:
                        common.Com_Error(QShared.ERR_DROP, "CL_ParseServerMessage: Illegible server message\n");
                        break;
                }
            }

            // CL_AddNetgraph();

            // /* we don't know if it is ok to save a demo message
            // until after we have parsed the frame */
            // if (cls.demorecording && !cls.demowaiting)
            // {
            //     CL_WriteDemoMessage();
            // }
        }



    }
}
