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
 * Player movement code. This is the core of Quake IIs legendary physics
 * engine
 *
 * =======================================================================
 */
using System.Numerics;

namespace Quake2 {

    partial class QCommon {

        private const int STEPSIZE = 18;

        /* all of the locals will be zeroed before each
        * pmove, just to make damn sure we don't have
        * any differences when running on client or server */

        private struct pml_t
        {
            public Vector3 origin; /* full float precision */
            public Vector3 velocity; /* full float precision */

            public Vector3 forward, right, up;
            public float frametime;

            // public QShared.csurface_t? groundsurface;
            public QShared.cplane_t groundplane;
            public int groundcontents;

            public Vector3 previous_origin;
            public bool ladder;
        } ;

        private pml_t pml;

        private void PM_CatagorizePosition(ref QShared.pmove_t pm)
        {
            // vec3_t point;
            // int cont;
            // trace_t trace;
            // float sample1;
            // float sample2;

            /* if the player hull point one unit down
            is solid, the player is on ground */

            /* see if standing on something solid */
            var point = pml.origin;
            point.Z -= 0.25f;

            if (pml.velocity.Z > 180)
            {
                // pm.s.pm_flags &= ~QShared.PMF_ON_GROUND;
                pm.groundentity = null;
            }
            else
            {
            //     trace = pm->trace(pml.origin, pm->mins, pm->maxs, point);
            //     pml.groundplane = trace.plane;
            //     pml.groundsurface = trace.surface;
            //     pml.groundcontents = trace.contents;

            //     if (!trace.ent || ((trace.plane.normal[2] < 0.7) && !trace.startsolid))
            //     {
            //         pm->groundentity = NULL;
            //         pm->s.pm_flags &= ~PMF_ON_GROUND;
            //     }
            //     else
            //     {
            //         pm->groundentity = trace.ent;

            //         /* hitting solid ground will end a waterjump */
            //         if (pm->s.pm_flags & PMF_TIME_WATERJUMP)
            //         {
            //             pm->s.pm_flags &=
            //                 ~(PMF_TIME_WATERJUMP | PMF_TIME_LAND | PMF_TIME_TELEPORT);
            //             pm->s.pm_time = 0;
            //         }

            //         if (!(pm->s.pm_flags & PMF_ON_GROUND))
            //         {
            //             /* just hit the ground */
            //             pm->s.pm_flags |= PMF_ON_GROUND;

            //             /* don't do landing time if we were just going down a slope */
            //             if (pml.velocity[2] < -200)
            //             {
            //                 pm->s.pm_flags |= PMF_TIME_LAND;

            //                 /* don't allow another jump for a little while */
            //                 if (pml.velocity[2] < -400)
            //                 {
            //                     pm->s.pm_time = 25;
            //                 }
            //                 else
            //                 {
            //                     pm->s.pm_time = 18;
            //                 }
            //             }
            //         }
            //     }

            //     if ((pm->numtouch < MAXTOUCH) && trace.ent)
            //     {
            //         pm->touchents[pm->numtouch] = trace.ent;
            //         pm->numtouch++;
            //     }
            }

            /* get waterlevel, accounting for ducking */
            pm.waterlevel = 0;
            pm.watertype = 0;

            // sample2 = pm->viewheight - pm->mins[2];
            // sample1 = sample2 / 2;

            // point[2] = pml.origin[2] + pm->mins[2] + 1;
            // cont = pm->pointcontents(point);

            // if (cont & MASK_WATER)
            // {
            //     pm->watertype = cont;
            //     pm->waterlevel = 1;
            //     point[2] = pml.origin[2] + pm->mins[2] + sample1;
            //     cont = pm->pointcontents(point);

            //     if (cont & MASK_WATER)
            //     {
            //         pm->waterlevel = 2;
            //         point[2] = pml.origin[2] + pm->mins[2] + sample2;
            //         cont = pm->pointcontents(point);

            //         if (cont & MASK_WATER)
            //         {
            //             pm->waterlevel = 3;
            //         }
            //     }
            // }
        }

        private void PM_ClampAngles(ref QShared.pmove_t pm)
        {
            if ((pm.s.pm_flags & QShared.PMF_TIME_TELEPORT) != 0)
            {
                pm.viewangles = new Vector3(0);
                pm.viewangles.SetYaw(QShared.SHORT2ANGLE(
                        pm.cmd.angles[QShared.YAW] + pm.s.delta_angles[QShared.YAW]));
            }
            else
            {
                /* circularly clamp the angles with deltas */
                pm.viewangles = new Vector3(
                    QShared.SHORT2ANGLE(pm.cmd.angles[0] + pm.s.delta_angles[0]),
                    QShared.SHORT2ANGLE(pm.cmd.angles[1] + pm.s.delta_angles[1]),
                    QShared.SHORT2ANGLE(pm.cmd.angles[2] + pm.s.delta_angles[2])
                );

                /* don't let the player look up or down more than 90 degrees */
                if ((pm.viewangles.Pitch() > 89) && (pm.viewangles.Pitch() < 180))
                {
                    pm.viewangles.SetPitch(89);
                }
                else if ((pm.viewangles.Pitch() < 271) && (pm.viewangles.Pitch() >= 180))
                {
                    pm.viewangles.SetPitch(271);
                }
            }

            QShared.AngleVectors(pm.viewangles, ref pml.forward, ref pml.right, ref pml.up);
        }

        private void PM_CalculateViewHeightForDemo(ref QShared.pmove_t pm)
        {
            if (pm.s.pm_type == QShared.pmtype_t.PM_GIB)
                pm.viewheight = 8;
            else {
                if ((pm.s.pm_flags & QShared.PMF_DUCKED) != 0)
                    pm.viewheight = -2;
                else
                    pm.viewheight = 22;
            }
        }

        /*
        * Can be called by either the server or the client
        */
        public void Pmove(ref QShared.pmove_t pm)
        {
            /* clear results */
            pm.numtouch = 0;
            pm.viewangles = new Vector3();
            pm.viewheight = 0;
            pm.groundentity = null;
            pm.watertype = 0;
            pm.waterlevel = 0;

            /* clear all pmove local vars */
            pml = new pml_t();

            /* convert origin and velocity to float values */
            pml.origin.X = pm.s.origin[0] * 0.125f;
            pml.origin.Y = pm.s.origin[1] * 0.125f;
            pml.origin.Z = pm.s.origin[2] * 0.125f;

            pml.velocity.X = pm.s.velocity[0] * 0.125f;
            pml.velocity.Y = pm.s.velocity[1] * 0.125f;
            pml.velocity.Z = pm.s.velocity[2] * 0.125f;

            /* save old org in case we get stuck */
            pml.previous_origin = new Vector3(pm.s.origin[0], pm.s.origin[1], pm.s.origin[2]);

            pml.frametime = pm.cmd.msec * 0.001f;

            PM_ClampAngles(ref pm);

            if (pm.s.pm_type == QShared.pmtype_t.PM_SPECTATOR)
            {
                // PM_FlyMove(false);
                // PM_SnapPosition();
                return;
            }

            if (pm.s.pm_type >= QShared.pmtype_t.PM_DEAD)
            {
                pm.cmd.forwardmove = 0;
                pm.cmd.sidemove = 0;
                pm.cmd.upmove = 0;
            }

            if (pm.s.pm_type == QShared.pmtype_t.PM_FREEZE)
            {
        // #if !defined(DEDICATED_ONLY)
                if (client.attractloop) {
                    PM_CalculateViewHeightForDemo(ref pm);
                    // PM_CalculateWaterLevelForDemo();
                    // PM_UpdateUnderwaterSfx();
                }
        // #endif

                return; /* no movement at all */
            }

            /* set mins, maxs, and viewheight */
            // PM_CheckDuck();

            if (pm.snapinitial)
            {
                // PM_InitialSnapPosition();
            }

            /* set groundentity, watertype, and waterlevel */
            PM_CatagorizePosition(ref pm);

            if (pm.s.pm_type == QShared.pmtype_t.PM_DEAD)
            {
                // PM_DeadMove();
            }

            // PM_CheckSpecialMovement();

            /* drop timing counter */
            if (pm.s.pm_time != 0)
            {
                int msec = pm.cmd.msec >> 3;

                if (msec == 0)
                {
                    msec = 1;
                }

                if (msec >= pm.s.pm_time)
                {
                    pm.s.pm_flags &= byte.MaxValue ^ (byte)(QShared.PMF_TIME_WATERJUMP | QShared.PMF_TIME_LAND | QShared.PMF_TIME_TELEPORT);
                    pm.s.pm_time = 0;
                }
                else
                {
                    pm.s.pm_time -= (byte)msec;
                }
            }

            if ((pm.s.pm_flags & QShared.PMF_TIME_TELEPORT) != 0)
            {
                /* teleport pause stays exactly in place */
            }
            else if ((pm.s.pm_flags & QShared.PMF_TIME_WATERJUMP) != 0)
            {
                /* waterjump has no control, but falls */
                pml.velocity.Z -= pm.s.gravity * pml.frametime;

                if (pml.velocity.Z < 0)
                {
                    /* cancel as soon as we are falling down again */
                    pm.s.pm_flags &= byte.MaxValue ^ (byte)(QShared.PMF_TIME_WATERJUMP | QShared.PMF_TIME_LAND | QShared.PMF_TIME_TELEPORT);
                    pm.s.pm_time = 0;
                }

                // PM_StepSlideMove();
            }
            else
            {
                Console.WriteLine("PM_CheckJump");
                // PM_CheckJump();

                // PM_Friction();

                // if (pm->waterlevel >= 2)
                // {
                //     PM_WaterMove();
                // }
                // else
                // {
                //     vec3_t angles;

                //     VectorCopy(pm->viewangles, angles);

                //     if (angles[PITCH] > 180)
                //     {
                //         angles[PITCH] = angles[PITCH] - 360;
                //     }

                //     angles[PITCH] /= 3;

                //     AngleVectors(angles, pml.forward, pml.right, pml.up);

                //     PM_AirMove();
                // }
            }

            /* set groundentity, watertype, and waterlevel for final spot */
            // PM_CatagorizePosition();

        // #if !defined(DEDICATED_ONLY)
            // PM_UpdateUnderwaterSfx();
        // #endif

            // PM_SnapPosition();
        }



    }
}