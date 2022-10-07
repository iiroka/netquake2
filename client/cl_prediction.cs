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
 * This file implements interpolation between two frames. This is used
 * to smooth down network play
 *
 * =======================================================================
 */
using System.Numerics;

namespace Quake2 {

    partial class QClient {

        private void CL_CheckPredictionError()
        {
            if (!(cl_predict?.Bool ?? false) ||
                ((cl.frame.playerstate.pmove.pm_flags & QShared.PMF_NO_PREDICTION)) != 0)
            {
                return;
            }

            /* calculate the last usercmd_t we sent that the server has processed */
            var frame = cls.netchan.incoming_acknowledged;
            frame &= (CMD_BACKUP - 1);

            /* compare what the server returned with what we had predicted it to be */
            var delta = new int[3];
            for (int i = 0; i < 3; i++)
                delta[i] = cl.frame.playerstate.pmove.origin[i] - cl.predicted_origins[frame][i];

            /* save the prediction error for interpolation */
            var len = Math.Abs(delta[0]) + Math.Abs(delta[1]) + Math.Abs(delta[2]);

            /* 80 world units */
            if (len > 640)
            {
                /* a teleport or something */
                cl.prediction_error = new Vector3();
            }
            else
            {
                if (cl_showmiss!.Bool && (delta[0] != 0 || delta[1] != 0 || delta[2] != 0))
                {
                    common.Com_Printf($"prediction miss on {cl.frame.serverframe}: {delta[0] + delta[1] + delta[2]}\n");
                }

                Array.Copy(cl.frame.playerstate.pmove.origin, cl.predicted_origins[frame], 3);

                /* save for error itnerpolation */
                cl.prediction_error = new Vector3(
                    delta[0] * 0.125f,
                    delta[1] * 0.125f,
                    delta[2] * 0.125f
                );
            }
        }

        private class dummy_edict : edict_s {}
        private class state_edict : edict_s {
            public readonly QShared.entity_state_t state;
            public state_edict(QShared.entity_state_t state)
            {
                this.state = state;
            }
        }

        private void CL_ClipMoveToEntities(in Vector3 start, in Vector3 mins, in Vector3 maxs,
                in Vector3 end, ref QShared.trace_t tr)
        {
            int headnode;
            Vector3 angles;

            for (int i = 0; i < cl.frame.num_entities; i++)
            {
                int num = (cl.frame.parse_entities + i) & (MAX_PARSE_ENTITIES - 1);
                ref var ent = ref cl_parse_entities[num];

                if (ent.solid == 0)
                {
                    continue;
                }

                if (ent.number == cl.playernum + 1)
                {
                    continue;
                }

                if (ent.solid == 31)
                {
                    /* special value for bmodel */
                    var cmodel = cl.model_clip[ent.modelindex];

                    if (cmodel == null)
                    {
                        continue;
                    }

                    headnode = cmodel.headnode;
                    angles = ent.angles;
                }
                else
                {
                    /* encoded bbox */
                    int x = 8 * (ent.solid & 31);
                    int zd = 8 * ((ent.solid >> 5) & 31);
                    int zu = 8 * ((ent.solid >> 10) & 63) - 32;

                    var bmins = new Vector3(-x, -x, -zd);
                    var bmaxs = new Vector3(x, x, zu);

                    headnode = common.CM_HeadnodeForBox(bmins, bmaxs);
                    angles = Vector3.Zero; /* boxes don't rotate */
                }

                if (tr.allsolid)
                {
                    return;
                }

                var trace = common.CM_TransformedBoxTrace(start, end, mins, maxs, headnode, QShared.MASK_PLAYERSOLID, ent.origin, angles);

                if (trace.allsolid || trace.startsolid ||
                    (trace.fraction < tr.fraction))
                {
                    trace.ent = new state_edict(ent);

                    if (tr.startsolid)
                    {
                        tr = trace;
                        tr.startsolid = true;
                    }
                    else
                    {
                        tr = trace;
                    }
                }
            }
        }


        private QShared.trace_t CL_PMTrace(in Vector3 start, in Vector3 mins, in Vector3 maxs, in Vector3 end)
        {
            /* check against world */
            var t = common.CM_BoxTrace(start, end, mins, maxs, 0, QShared.MASK_PLAYERSOLID);

            if (t.fraction < 1.0)
            {
                t.ent = new dummy_edict();
            }

            /* check all other solid models */
            CL_ClipMoveToEntities(start, mins, maxs, end, ref t);

            return t;
        }


        /*
        * Sets cl.predicted_origin and cl.predicted_angles
        */
        private void CL_PredictMovement()
        {
            // int ack, current;
            // int frame;
            // usercmd_t *cmd;
            // pmove_t pm;
            // int i;
            // int step;
            // vec3_t tmp;

            if (cls.state != connstate_t.ca_active)
            {
                return;
            }

            if (cl_paused!.Bool)
            {
                return;
            }

            if (!(cl_predict?.Bool ?? false) ||
                (cl.frame.playerstate.pmove.pm_flags & QShared.PMF_NO_PREDICTION) != 0)
            {
                /* just set angles */
                cl.predicted_angles = new Vector3(
                    cl.viewangles.X + QShared.SHORT2ANGLE(cl.frame.playerstate.pmove.delta_angles[0]),
                    cl.viewangles.Y + QShared.SHORT2ANGLE(cl.frame.playerstate.pmove.delta_angles[1]),
                    cl.viewangles.Z + QShared.SHORT2ANGLE(cl.frame.playerstate.pmove.delta_angles[2])
                );

                return;
            }

            int ack = cls.netchan.incoming_acknowledged;
            int current = cls.netchan.outgoing_sequence;

            /* if we are too far out of date, just freeze */
            if (current - ack >= CMD_BACKUP)
            {
                if (cl_showmiss!.Bool)
                {
                    common.Com_Printf("exceeded CMD_BACKUP\n");
                }

                return;
            }

            /* copy current state to pmove */
            var pm = new QShared.pmove_t();
            pm.touchents = new edict_s?[QShared.MAXTOUCH];
            pm.trace = CL_PMTrace;
            // pm.pointcontents = CL_PMpointcontents;
            common.pm_airaccelerate = Convert.ToSingle(cl.configstrings[QShared.CS_AIRACCEL], QShared.provider);
            pm.s = cl.frame.playerstate.pmove;

            /* run frames */
            while (++ack <= current)
            {
                int frame = ack & (CMD_BACKUP - 1);
                ref var cmd = ref cl.cmds[frame];

                // Ignore null entries
                if (cmd.msec == 0)
                {
                    continue;
                }

                pm.cmd = cmd;
                common.Pmove(ref pm);

                /* save for debug checking */
                Array.Copy(pm.s.origin, cl.predicted_origins[frame], 3);
            }

            int step = pm.s.origin[2] - (int)(cl.predicted_origin.Z * 8);
            // VectorCopy(pm.s.velocity, tmp);

            // if (((step > 126 && step < 130))
            //     && !VectorCompare(tmp, vec3_origin)
            //     && (pm.s.pm_flags & PMF_ON_GROUND))
            // {
            //     cl.predicted_step = step * 0.125f;
            //     cl.predicted_step_time = cls.realtime - (int)(cls.nframetime * 500);
            // }

            /* copy results out for rendering */
            cl.predicted_origin.X = pm.s.origin[0] * 0.125f;
            cl.predicted_origin.Y = pm.s.origin[1] * 0.125f;
            cl.predicted_origin.Z = pm.s.origin[2] * 0.125f;

            cl.predicted_angles = pm.viewangles;
        }

    }
}