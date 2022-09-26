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
 * Server entity handling. Just encodes the entties of a client side
 * frame into network / local communication packages and sends them to
 * the appropriate clients.
 *
 * =======================================================================
 */

using System.Numerics;

namespace Quake2 {

    partial class QServer {

        private byte[] fatpvs;

        /*
        * Writes a delta update of an entity_state_t list to the message.
        */
        private void SV_EmitPacketEntities(in client_frame_t? from, ref client_frame_t to, ref QWritebuf msg)
        {
            // entity_state_t *oldent, *newent;
            // int oldindex, newindex;
            // int oldnum, newnum;
            // int from_num_entities;
            // int bits;

            msg.WriteByte((int)QCommon.svc_ops_e.svc_packetentities);
            int from_num_entities;
            if (from == null)
            {
                from_num_entities = 0;
            }
            else
            {
                from_num_entities = from.Value.num_entities;
            }

            int newindex = 0;
            int oldindex = 0;
            QShared.entity_state_t? newent = null;
            QShared.entity_state_t? oldent = null;
            int newnum, oldnum;

            while (newindex < to.num_entities || oldindex < from_num_entities)
            {
                if (msg.Size > QCommon.MAX_MSGLEN - 150)
                {
                    break;
                }

                if (newindex >= to.num_entities)
                {
                    newnum = 9999;
                }
                else
                {
                    newent = svs.client_entities[(to.first_entity + newindex) % svs.num_client_entities];
                    newnum = newent.number;
                }

                if (oldindex >= from_num_entities)
                {
                    oldnum = 9999;
                }
                else
                {
                    oldent = svs.client_entities[(from!.Value.first_entity + oldindex) % svs.num_client_entities];
                    oldnum = oldent.number;
                }

                if (newnum == oldnum)
                {
                    /* delta update from old position. because the force 
                    parm is false, this will not result in any bytes
                    being emited if the entity has not changed at all
                    note that players are always 'newentities', this
                    updates their oldorigin always and prevents warping */
                    msg.WriteDeltaEntity(oldent!, newent!, false, newent!.number <= maxclients!.Int, common);
                    oldindex++;
                    newindex++;
                    continue;
                }

                if (newnum < oldnum)
                {
                    /* this is a new entity, send it from the baseline */
                    msg.WriteDeltaEntity(sv.baselines[newnum], newent!, true, true, common);
                    newindex++;
                    continue;
                }

                if (newnum > oldnum)
                {
                    /* the old entity isn't present in the new message */
                    uint bits = QCommon.U_REMOVE;

                    if (oldnum >= 256)
                    {
                        bits |= QCommon.U_NUMBER16 | QCommon.U_MOREBITS1;
                    }

                    msg.WriteByte((int)(bits & 255));

                    if ((bits & 0x0000ff00) != 0)
                    {
                        msg.WriteByte((int)(bits >> 8) & 255);
                    }

                    if ((bits & QCommon.U_NUMBER16) != 0)
                    {
                        msg.WriteShort(oldnum);
                    }
                    else
                    {
                        msg.WriteByte(oldnum);
                    }

                    oldindex++;
                    continue;
                }
            }

            msg.WriteShort(0);
        }

        private void SV_WritePlayerstateToClient(in client_frame_t? from, ref client_frame_t to, ref QWritebuf msg)
        {
            // int i;
            // int pflags;
            // player_state_t *ps, *ops;
            // player_state_t dummy;
            // int statbits;

            ref var ps = ref to.ps;
            QShared.player_state_t ops;

            if (!from.HasValue)
            {
                ops = new QShared.player_state_t();
                ops.pmove = new QShared.pmove_state_t();
                ops.pmove.origin = new short[3];
                ops.pmove.velocity = new short[3];
                ops.pmove.delta_angles = new short[3];
            }
            else
            {
                ops = from.Value.ps;
            }

            /* determine what needs to be sent */
            uint pflags = 0;

            if (ps.pmove.pm_type != ops.pmove.pm_type)
            {
                pflags |= QCommon.PS_M_TYPE;
            }

            if ((ps.pmove.origin[0] != ops.pmove.origin[0]) ||
                (ps.pmove.origin[1] != ops.pmove.origin[1]) ||
                (ps.pmove.origin[2] != ops.pmove.origin[2]))
            {
                pflags |= QCommon.PS_M_ORIGIN;
            }

            if ((ps.pmove.velocity[0] != ops.pmove.velocity[0]) ||
                (ps.pmove.velocity[1] != ops.pmove.velocity[1]) ||
                (ps.pmove.velocity[2] != ops.pmove.velocity[2]))
            {
                pflags |= QCommon.PS_M_VELOCITY;
            }

            if (ps.pmove.pm_time != ops.pmove.pm_time)
            {
                pflags |= QCommon.PS_M_TIME;
            }

            if (ps.pmove.pm_flags != ops.pmove.pm_flags)
            {
                pflags |= QCommon.PS_M_FLAGS;
            }

            if (ps.pmove.gravity != ops.pmove.gravity)
            {
                pflags |= QCommon.PS_M_GRAVITY;
            }

            if ((ps.pmove.delta_angles[0] != ops.pmove.delta_angles[0]) ||
                (ps.pmove.delta_angles[1] != ops.pmove.delta_angles[1]) ||
                (ps.pmove.delta_angles[2] != ops.pmove.delta_angles[2]))
            {
                pflags |= QCommon.PS_M_DELTA_ANGLES;
            }

            if ((ps.viewoffset.X != ops.viewoffset.X) ||
                (ps.viewoffset.Y != ops.viewoffset.Y) ||
                (ps.viewoffset.Z != ops.viewoffset.Z))
            {
                pflags |= QCommon.PS_VIEWOFFSET;
            }

            if ((ps.viewangles.X != ops.viewangles.X) ||
                (ps.viewangles.Y != ops.viewangles.Y) ||
                (ps.viewangles.Z != ops.viewangles.Z))
            {
                pflags |= QCommon.PS_VIEWANGLES;
            }

            if ((ps.kick_angles.X != ops.kick_angles.X) ||
                (ps.kick_angles.Y != ops.kick_angles.Y) ||
                (ps.kick_angles.Z != ops.kick_angles.Z))
            {
                pflags |= QCommon.PS_KICKANGLES;
            }

            if ((ps.blend[0] != ops.blend[0]) ||
                (ps.blend[1] != ops.blend[1]) ||
                (ps.blend[2] != ops.blend[2]) ||
                (ps.blend[3] != ops.blend[3]))
            {
                pflags |= QCommon.PS_BLEND;
            }

            if (ps.fov != ops.fov)
            {
                pflags |= QCommon.PS_FOV;
            }

            if (ps.rdflags != ops.rdflags)
            {
                pflags |= QCommon.PS_RDFLAGS;
            }

            if ((ps.gunframe != ops.gunframe) ||
                /* added so weapon angle/offset update during pauseframes */
                (ps.gunoffset != ops.gunoffset) ||

                (ps.gunangles != ops.gunangles))
            {
                pflags |= QCommon.PS_WEAPONFRAME;
            }

            pflags |= QCommon.PS_WEAPONINDEX;

            /* write it */
            msg.WriteByte((int)QCommon.svc_ops_e.svc_playerinfo);
            msg.WriteShort((int)pflags);

            /* write the pmove_state_t */
            if ((pflags & QCommon.PS_M_TYPE) != 0)
            {
                msg.WriteByte((int)ps.pmove.pm_type);
            }

            if ((pflags & QCommon.PS_M_ORIGIN) != 0)
            {
                msg.WriteShort(ps.pmove.origin[0]);
                msg.WriteShort(ps.pmove.origin[1]);
                msg.WriteShort(ps.pmove.origin[2]);
            }

            if ((pflags & QCommon.PS_M_VELOCITY) != 0)
            {
                msg.WriteShort(ps.pmove.velocity[0]);
                msg.WriteShort(ps.pmove.velocity[1]);
                msg.WriteShort(ps.pmove.velocity[2]);
            }

            if ((pflags & QCommon.PS_M_TIME) != 0)
            {
                msg.WriteByte(ps.pmove.pm_time);
            }

            if ((pflags & QCommon.PS_M_FLAGS) != 0)
            {
                msg.WriteByte(ps.pmove.pm_flags);
            }

            if ((pflags & QCommon.PS_M_GRAVITY) != 0)
            {
                msg.WriteShort(ps.pmove.gravity);
            }

            if ((pflags & QCommon.PS_M_DELTA_ANGLES) != 0)
            {
                msg.WriteShort(ps.pmove.delta_angles[0]);
                msg.WriteShort(ps.pmove.delta_angles[1]);
                msg.WriteShort(ps.pmove.delta_angles[2]);
            }

            /* write the rest of the player_state_t */
            if ((pflags & QCommon.PS_VIEWOFFSET) != 0)
            {
                msg.WriteChar((int)(ps.viewoffset.X * 4));
                msg.WriteChar((int)(ps.viewoffset.Y * 4));
                msg.WriteChar((int)(ps.viewoffset.Z * 4));
            }

            if ((pflags & QCommon.PS_VIEWANGLES) != 0)
            {
                msg.WriteAngle16(ps.viewangles.X);
                msg.WriteAngle16(ps.viewangles.Y);
                msg.WriteAngle16(ps.viewangles.Z);
            }

            if ((pflags & QCommon.PS_KICKANGLES) != 0)
            {
                msg.WriteChar((int)(ps.kick_angles.X * 4));
                msg.WriteChar((int)(ps.kick_angles.Y * 4));
                msg.WriteChar((int)(ps.kick_angles.Z * 4));
            }

            if ((pflags & QCommon.PS_WEAPONINDEX) != 0)
            {
                msg.WriteByte(ps.gunindex);
            }

            if ((pflags & QCommon.PS_WEAPONFRAME) != 0)
            {
                msg.WriteByte(ps.gunframe);
                msg.WriteChar((int)(ps.gunoffset.X * 4));
                msg.WriteChar((int)(ps.gunoffset.Y * 4));
                msg.WriteChar((int)(ps.gunoffset.Z * 4));
                msg.WriteChar((int)(ps.gunangles.X * 4));
                msg.WriteChar((int)(ps.gunangles.Y * 4));
                msg.WriteChar((int)(ps.gunangles.Z * 4));
            }

            if ((pflags & QCommon.PS_BLEND) != 0)
            {
                msg.WriteByte((int)(ps.blend[0] * 255));
                msg.WriteByte((int)(ps.blend[1] * 255));
                msg.WriteByte((int)(ps.blend[2] * 255));
                msg.WriteByte((int)(ps.blend[3] * 255));
            }

            if ((pflags & QCommon.PS_FOV) != 0)
            {
                msg.WriteByte((int)ps.fov);
            }

            if ((pflags & QCommon.PS_RDFLAGS) != 0)
            {
                msg.WriteByte(ps.rdflags);
            }

            /* send stats */
            int statbits = 0;

            for (int i = 0; i < QShared.MAX_STATS; i++)
            {
                if (ps.stats[i] != ops.stats[i])
                {
                    statbits |= 1 << i;
                }
            }

            msg.WriteLong(statbits);

            for (int i = 0; i < QShared.MAX_STATS; i++)
            {
                if ((statbits & (1 << i)) != 0)
                {
                    msg.WriteShort(ps.stats[i]);
                }
            }
        }

        private void SV_WriteFrameToClient(ref client_t client, ref QWritebuf msg)
        {
            // client_frame_t *frame, *oldframe;
            // int lastframe;

            /* this is the frame we are creating */
            ref var frame = ref client.frames[sv.framenum & QCommon.UPDATE_MASK];

            int lastframe;
            client_frame_t? oldframe;
            if (client.lastframe <= 0)
            {
                /* client is asking for a retransmit */
                oldframe = null;
                lastframe = -1;
            }
            else if (sv.framenum - client.lastframe >= (QCommon.UPDATE_BACKUP - 3))
            {
                /* client hasn't gotten a good message through in a long time */
                oldframe = null;
                lastframe = -1;
            }
            else
            {
                /* we have a valid message to delta from */
                oldframe = client.frames[client.lastframe & QCommon.UPDATE_MASK];
                lastframe = client.lastframe;
            }

            msg.WriteByte((int)QCommon.svc_ops_e.svc_frame);
            msg.WriteLong(sv.framenum);
            msg.WriteLong(lastframe); /* what we are delta'ing from */
            msg.WriteByte(client.surpressCount); /* rate dropped packets */
            client.surpressCount = 0;

            /* send over the areabits */
            msg.WriteByte(frame.areabytes);
            msg.Write(new ReadOnlySpan<byte>(frame.areabits, 0, frame.areabytes));

            /* delta encode the playerstate */
            SV_WritePlayerstateToClient(oldframe, ref frame, ref msg);

            /* delta encode the entities */
            SV_EmitPacketEntities(oldframe, ref frame, ref msg);
        }

        /*
        * The client will interpolate the view position,
        * so we can't use a single PVS point
        */
        private void SV_FatPVS(in Vector3 org)
        {
            // int leafs[64];
            // int i, j, count;
            // // DG: used to be called "longs" and long was used which isn't really correct on 64bit
            // int32_t numInt32s;
            // byte *src;
            // vec3_t mins, maxs;

            var mins = new Vector3(org.X - 8, org.Y - 8, org.Z - 8);
            var maxs = new Vector3(org.X + 8, org.Y + 8, org.Z + 8);
            // for (i = 0; i < 3; i++)
            // {
            //     mins[i] = org[i] - 8;
            //     maxs[i] = org[i] + 8;
            // }

            int count = common.CM_BoxLeafnums(mins, maxs, out var leafs, out var ignored);

            if (count < 1)
            {
                common.Com_Error(QShared.ERR_FATAL, "SV_FatPVS: count < 1");
            }

            // numInt32s = (common.CM_NumClusters() + 31) >> 5;
            var numBytes = (common.CM_NumClusters() + 7) / 8;

            /* convert leafs to clusters */
            for (int i = 0; i < count; i++)
            {
                leafs[i] = common.CM_LeafCluster(leafs[i]);
            }

            fatpvs = common.CM_ClusterPVS(leafs[0]);
            // memcpy(fatpvs, CM_ClusterPVS(leafs[0]), numInt32s << 2);

            /* or in all the other leaf bits */
            for (int i = 1; i < count; i++)
            {
                int j;
                for (j = 0; j < i; j++)
                {
                    if (leafs[i] == leafs[j])
                    {
                        break;
                    }
                }

                if (j != i)
                {
                    continue; /* already have the cluster we want */
                }

                byte[] src = common.CM_ClusterPVS(leafs[i]);

                for (j = 0; j < numBytes; j++)
                {
                    // ((int32_t *)fatpvs)[j] |= ((int32_t *)src)[j];
                    fatpvs[j] |= src[j];
                }
            }
        }

        /*
        * Decides which entities are going to be visible to the client, and
        * copies off the playerstat and areabits.
        */
        private void SV_BuildClientFrame(ref client_t client)
        {
            // int e, i;
            // vec3_t org;
            // edict_t *ent;
            // edict_t *clent;
            // client_frame_t *frame;
            // entity_state_t *state;
            // int l;
            // int clientarea, clientcluster;
            // int leafnum;
            // int c_fullsend;
            // byte *clientphs;
            // byte *bitvector;

            var clent = client.edict;

            if (clent == null || clent.client == null)
            {
                return; /* not in game yet */
            }

            /* this is the frame we are creating */
            ref var frame = ref client.frames[sv.framenum & QCommon.UPDATE_MASK];

            frame.senttime = svs.realtime; /* save it for ping calc later */

            /* find the client's PVS */
            var org = new Vector3(
                clent.client.ps.pmove.origin[0] * 0.125f + clent.client.ps.viewoffset.X,
                clent.client.ps.pmove.origin[1] * 0.125f + clent.client.ps.viewoffset.Y,
                clent.client.ps.pmove.origin[2] * 0.125f + clent.client.ps.viewoffset.Z);

            var leafnum = common.CM_PointLeafnum(org);
            var clientarea = common.CM_LeafArea(leafnum);
            var clientcluster = common.CM_LeafCluster(leafnum);

            /* calculate the visible areas */
            frame.areabytes = common.CM_WriteAreaBits(ref frame.areabits, clientarea);

            /* grab the current player_state_t */
            frame.ps = clent.client.ps;

            SV_FatPVS(org);
            byte[] clientphs = common.CM_ClusterPHS(clientcluster);

            /* build up the list of visible entities */
            frame.num_entities = 0;
            frame.first_entity = svs.next_client_entities;

            var c_fullsend = 0;

            for (int e = 1; e < ge!.num_edicts; e++)
            {
                var ent = ge.getEdict(e);

                /* ignore ents without visible models */
                if ((ent.svflags & QGameFlags.SVF_NOCLIENT) != 0)
                {
                    continue;
                }

                /* ignore ents without visible models unless they have an effect */
                if (ent.s.modelindex == 0 && ent.s.effects == 0 && 
                    ent.s.sound == 0 && ent.s.ev == 0)
                {
                    continue;
                }

                /* ignore if not touching a PV leaf */
                if (ent != clent)
                {
                    /* check area */
                    if (!common.CM_AreasConnected(clientarea, ent.areanum))
                    {
                        /* doors can legally straddle two areas,
                        so we may need to check another one */
                        if (ent.areanum2 == 0 ||
                            !common.CM_AreasConnected(clientarea, ent.areanum2))
                        {
                            continue; /* blocked by a door */
                        }
                    }

            //         /* beams just check one point for PHS */
            //         if (ent->s.renderfx & RF_BEAM)
            //         {
            //             l = ent->clusternums[0];

            //             if (!(clientphs[l >> 3] & (1 << (l & 7))))
            //             {
            //                 continue;
            //             }
            //         }
            //         else
            //         {
                        // bitvector = fatpvs;

                        if (ent.num_clusters == -1)
                        {
                            /* too many leafs for individual check, go by headnode */
                            if (!common.CM_HeadnodeVisible(ent.headnode, fatpvs))
                            {
                                continue;
                            }

                            c_fullsend++;
                        }
                        else
                        {
                            /* check individual leafs */
                            int i;
                            for (i = 0; i < ent.num_clusters; i++)
                            {
                                int l = ent.clusternums[i];

                                if ((fatpvs[l >> 3] & (1 << (l & 7))) != 0)
                                {
                                    break;
                                }
                            }

                            if (i == ent.num_clusters)
                            {
                                continue; /* not visible */
                            }
                        }

                        if (ent.s.modelindex == 0)
                        {
                            /* don't send sounds if they 
                            will be attenuated away */
                            var delta = org - ent.s.origin;
                            var len = delta.Length();

                            if (len > 400)
                            {
                                continue;
                            }
                        }
                    // }
                }

                /* add it to the circular client_entities array */
                ref var state = ref svs.client_entities[svs.next_client_entities % svs.num_client_entities];

                if (ent.s.number != e)
                {
                    common.Com_DPrintf("FIXING ENT->S.NUMBER!!!\n");
                    ent.s.number = e;
                }

                state = ent.s;

            //     /* don't mark players missiles as solid */
            //     if (ent->owner == client->edict)
            //     {
            //         state->solid = 0;
            //     }

                svs.next_client_entities++;
                frame.num_entities++;
            }
        }


    }
}