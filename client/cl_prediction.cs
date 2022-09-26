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
            // pm.trace = CL_PMTrace;
            // pm.pointcontents = CL_PMpointcontents;
            // pm_airaccelerate = atof(cl.configstrings[CS_AIRACCEL]);
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