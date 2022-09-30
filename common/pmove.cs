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

            public QShared.csurface_t? groundsurface;
            public QShared.cplane_t groundplane;
            public int groundcontents;

            public Vector3 previous_origin;
            public bool ladder;
        } ;

        private pml_t pml;

        /* movement parameters */
        private float pm_stopspeed = 100;
        private float pm_maxspeed = 300;
        private float pm_duckspeed = 100;
        private float pm_accelerate = 10;
        private float pm_airaccelerate = 0;
        private float pm_wateraccelerate = 10;
        private float pm_friction = 6;
        private float pm_waterfriction = 1;
        private float pm_waterspeed = 400;

        private const float STOP_EPSILON = 0.1f; /* Slide off of the impacting object returns the blocked flags (1 = floor, 2 = step / wall) */
        private const float MIN_STEP_NORMAL = 0.7f; /* can't step up onto very steep slopes */
        private const int MAX_CLIP_PLANES = 5;

        /*
        * Each intersection will try to step over the obstruction instead of
        * sliding along it.
        *
        * Returns a new origin, velocity, and contact entity
        * Does not modify any world state?
        */
        private void PM_StepSlideMove_(ref QShared.pmove_t pm)
        {
            // int bumpcount, numbumps;
            // vec3_t dir;
            // float d;
            // int numplanes;
            // vec3_t planes[MAX_CLIP_PLANES];
            // vec3_t primal_velocity;
            // int i, j;
            // trace_t trace;
            // vec3_t end;
            // float time_left;

            int numbumps = 4;

            var primal_velocity = pml.velocity;
            int numplanes = 0;

            var planes = new Vector3[MAX_CLIP_PLANES];

            float time_left = pml.frametime;

            for (int bumpcount = 0; bumpcount < numbumps; bumpcount++)
            {
                var end = pml.origin + time_left * pml.velocity;

                var trace = pm.trace!(pml.origin, pm.mins, pm.maxs, end);

                if (trace.allsolid)
                {
                    /* entity is trapped in another solid */
                    pml.velocity.Z = 0; /* don't build up falling damage */
                    return;
                }

                if (trace.fraction > 0)
                {
                    /* actually covered some distance */
                    pml.origin = trace.endpos;
                    numplanes = 0;
                }

                if (trace.fraction == 1)
                {
                    break; /* moved the entire distance */
                }

            //     /* save entity for contact */
            //     if ((pm->numtouch < MAXTOUCH) && trace.ent)
            //     {
            //         pm->touchents[pm->numtouch] = trace.ent;
            //         pm->numtouch++;
            //     }

                time_left -= time_left * trace.fraction;

                /* slide along this plane */
                if (numplanes >= MAX_CLIP_PLANES)
                {
                    /* this shouldn't really happen */
                    pml.velocity = Vector3.Zero;
                    break;
                }

                planes[numplanes] = trace.plane.normal;
                numplanes++;

                /* modify original_velocity so it parallels all of the clip planes */
                int i;
                for (i = 0; i < numplanes; i++)
                {
            //         PM_ClipVelocity(pml.velocity, planes[i], pml.velocity, 1.01f);

            //         for (j = 0; j < numplanes; j++)
            //         {
            //             if (j != i)
            //             {
            //                 if (DotProduct(pml.velocity, planes[j]) < 0)
            //                 {
            //                     break; /* not ok */
            //                 }
            //             }
            //         }

            //         if (j == numplanes)
            //         {
            //             break;
            //         }
                }

            //     if (i != numplanes)
            //     {
            //         /* go along this plane */
            //     }
            //     else
            //     {
            //         /* go along the crease */
            //         if (numplanes != 2)
            //         {
            //             VectorCopy(vec3_origin, pml.velocity);
            //             break;
            //         }

            //         CrossProduct(planes[0], planes[1], dir);
            //         d = DotProduct(dir, pml.velocity);
            //         VectorScale(dir, d, pml.velocity);
            //     }

            //     /* if velocity is against the original velocity, stop dead
            //     to avoid tiny occilations in sloping corners */
            //     if (DotProduct(pml.velocity, primal_velocity) <= 0)
            //     {
            //         VectorCopy(vec3_origin, pml.velocity);
            //         break;
            //     }
            }

            if (pm.s.pm_time != 0)
            {
                pml.velocity = primal_velocity;
            }
        }

        private void PM_StepSlideMove(ref QShared.pmove_t pm)
        {
            // vec3_t start_o, start_v;
            // vec3_t down_o, down_v;
            // trace_t trace;
            // float down_dist, up_dist;
            // vec3_t up, down;

            var start_o = pml.origin;
            var start_v = pml.velocity;

            PM_StepSlideMove_(ref pm);

            var down_o = pml.origin;
            var down_v = pml.velocity;

            var up = start_o;
            up.Z += STEPSIZE;

            var trace = pm.trace!(up, pm.mins, pm.maxs, up);

            if (trace.allsolid)
            {
                return; /* can't step up */
            }

            /* try sliding above */
            pml.origin = up;
            pml.velocity = start_v;

            PM_StepSlideMove_(ref pm);

            /* push down the final amount */
            var down = pml.origin;
            down.Z -= STEPSIZE;
            trace = pm.trace!(pml.origin, pm.mins, pm.maxs, down);

            if (!trace.allsolid)
            {
                pml.origin = trace.endpos;
            }

            up = pml.origin;

            // /* decide which one went farther */
            // down_dist = (down_o[0] - start_o[0]) * (down_o[0] - start_o[0])
            //             + (down_o[1] - start_o[1]) * (down_o[1] - start_o[1]);
            // up_dist = (up[0] - start_o[0]) * (up[0] - start_o[0])
            //         + (up[1] - start_o[1]) * (up[1] - start_o[1]);

            // if ((down_dist > up_dist) || (trace.plane.normal[2] < MIN_STEP_NORMAL))
            // {
            //     VectorCopy(down_o, pml.origin);
            //     VectorCopy(down_v, pml.velocity);
            //     return;
            // }

            pml.velocity.Z = down_v.Z;
        }

        /*
        * Handles user intended acceleration
        */
        private void PM_Accelerate(in Vector3 wishdir, float wishspeed, float accel)
        {
            float currentspeed = Vector3.Dot(pml.velocity, wishdir);
            float addspeed = wishspeed - currentspeed;

            if (addspeed <= 0)
            {
                return;
            }

            float accelspeed = accel * pml.frametime * wishspeed;

            if (accelspeed > addspeed)
            {
                accelspeed = addspeed;
            }

            pml.velocity += accelspeed * wishdir;
        }

        private void PM_AirMove(ref QShared.pmove_t pm)
        {
            // int i;
            // vec3_t wishvel;
            // float fmove, smove;
            // vec3_t wishdir;
            // float wishspeed;
            // float maxspeed;

            float fmove = pm.cmd.forwardmove;
            float smove = pm.cmd.sidemove;

            var wishvel = new Vector3(
                pml.forward.X * fmove + pml.right.X * smove,
                pml.forward.Y * fmove + pml.right.Y * smove,
                0
            );

            // PM_AddCurrents(wishvel);

            var wishdir = wishvel;
            var wishspeed = wishdir.Length();
            wishdir = Vector3.Normalize(wishdir);

            /* clamp to server defined max speed */
            var maxspeed = (pm.s.pm_flags & QShared.PMF_DUCKED) != 0 ? pm_duckspeed : pm_maxspeed;

            if (wishspeed > maxspeed)
            {
                wishvel = maxspeed / wishspeed * wishvel;
                wishspeed = maxspeed;
            }

            if (pml.ladder)
            {
                PM_Accelerate(wishdir, wishspeed, pm_accelerate);

            //     if (!wishvel[2])
            //     {
            //         if (pml.velocity[2] > 0)
            //         {
            //             pml.velocity[2] -= pm->s.gravity * pml.frametime;

            //             if (pml.velocity[2] < 0)
            //             {
            //                 pml.velocity[2] = 0;
            //             }
            //         }
            //         else
            //         {
            //             pml.velocity[2] += pm->s.gravity * pml.frametime;

            //             if (pml.velocity[2] > 0)
            //             {
            //                 pml.velocity[2] = 0;
            //             }
            //         }
            //     }

                PM_StepSlideMove(ref pm);
            }
            else if (pm.groundentity != null)
            {
                /* walking on ground */
                pml.velocity.Z = 0;
                PM_Accelerate(wishdir, wishspeed, pm_accelerate);

            //     if (pm->s.gravity > 0)
            //     {
            //         pml.velocity[2] = 0;
            //     }
            //     else
            //     {
            //         pml.velocity[2] -= pm->s.gravity * pml.frametime;
            //     }

                if (pml.velocity.X == 0 && pml.velocity.Y == 0)
                {
                    return;
                }

                PM_StepSlideMove(ref pm);
            }
            else
            {
                /* not on ground, so little effect on velocity */
                // if (pm_airaccelerate)
                // {
                //     PM_AirAccelerate(wishdir, wishspeed, pm_accelerate);
                // }
                // else
                {
                    PM_Accelerate(wishdir, wishspeed, 1);
                }

                /* add gravity */
                pml.velocity.Z -= pm.s.gravity * pml.frametime;
                PM_StepSlideMove(ref pm);
            }
        }

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
                pm.s.pm_flags &= byte.MaxValue ^ QShared.PMF_ON_GROUND;
                pm.groundentity = null;
            }
            else
            {
                var trace = pm.trace!(pml.origin, pm.mins, pm.maxs, point);
                pml.groundplane = trace.plane;
                pml.groundsurface = trace.surface;
                pml.groundcontents = trace.contents;

                if (trace.ent == null || ((trace.plane.normal.Z < 0.7) && !trace.startsolid))
                {
                    pm.groundentity = null;
                    pm.s.pm_flags &= byte.MaxValue ^ QShared.PMF_ON_GROUND;
                }
                else
                {
                    pm.groundentity = trace.ent;

                    /* hitting solid ground will end a waterjump */
                    if ((pm.s.pm_flags & QShared.PMF_TIME_WATERJUMP) != 0)
                    {
                        pm.s.pm_flags &= byte.MaxValue ^ 
                            (QShared.PMF_TIME_WATERJUMP | QShared.PMF_TIME_LAND | QShared.PMF_TIME_TELEPORT);
                        pm.s.pm_time = 0;
                    }

                    if ((pm.s.pm_flags & QShared.PMF_ON_GROUND) == 0)
                    {
                        /* just hit the ground */
                        pm.s.pm_flags |= QShared.PMF_ON_GROUND;

                        /* don't do landing time if we were just going down a slope */
                        if (pml.velocity.Z < -200)
                        {
                            pm.s.pm_flags |= QShared.PMF_TIME_LAND;

                            /* don't allow another jump for a little while */
                            if (pml.velocity.Z < -400)
                            {
                                pm.s.pm_time = 25;
                            }
                            else
                            {
                                pm.s.pm_time = 18;
                            }
                        }
                    }
                }

                if ((pm.numtouch < QShared.MAXTOUCH) && trace.ent != null)
                {
                    pm.touchents[pm.numtouch] = trace.ent;
                    pm.numtouch++;
                }
            }

            /* get waterlevel, accounting for ducking */
            pm.waterlevel = 0;
            pm.watertype = 0;

            float sample2 = pm.viewheight - pm.mins.Z;
            float sample1 = sample2 / 2;

            point.Z = pml.origin.Z + pm.mins.Z + 1;
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

        private bool PM_GoodPosition(in QShared.pmove_t pm)
        {
            if (pm.s.pm_type == QShared.pmtype_t.PM_SPECTATOR)
            {
                return true;
            }

            var end = new Vector3(pm.s.origin[0]*0.125f, pm.s.origin[1]*0.125f, pm.s.origin[2]*0.125f);
            var origin = end;

            var trace = pm.trace!(origin, pm.mins, pm.maxs, end);

            return !trace.allsolid;
        }


        private void PM_InitialSnapPosition(ref QShared.pmove_t pm)
        {
            // int x, y, z;
            // short base[3];
            short[] offset = new short[3]{0, -1, 1};

            var b = new short[3];
            Array.Copy(pm.s.origin, b, 3);

            for (int z = 0; z < 3; z++)
            {
                pm.s.origin[2] = (short)(b[2] + offset[z]);

                for (int y = 0; y < 3; y++)
                {
                    pm.s.origin[1] = (short)(b[1] + offset[y]);

                    for (int x = 0; x < 3; x++)
                    {
                        pm.s.origin[0] = (short)(b[0] + offset[x]);

                        if (PM_GoodPosition(pm))
                        {
                            pml.origin.X = pm.s.origin[0] * 0.125f;
                            pml.origin.Y = pm.s.origin[1] * 0.125f;
                            pml.origin.Z = pm.s.origin[2] * 0.125f;
                            pml.previous_origin = new Vector3(pm.s.origin[0], pm.s.origin[1], pm.s.origin[2]);
                            return;
                        }
                    }
                }
            }

            Com_DPrintf("Bad InitialSnapPosition\n");
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
                PM_InitialSnapPosition(ref pm);
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
                // Console.WriteLine("PM_CheckJump");
                // PM_CheckJump();

                // PM_Friction();

                // if (pm->waterlevel >= 2)
                // {
                //     PM_WaterMove();
                // }
                // else
                // {
                    var angles = pm.viewangles;

                //     if (angles[PITCH] > 180)
                //     {
                //         angles[PITCH] = angles[PITCH] - 360;
                //     }

                    angles.SetPitch(angles.Pitch() / 3);

                    QShared.AngleVectors(angles, ref pml.forward, ref pml.right, ref pml.up);

                    PM_AirMove(ref pm);
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