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
 * Monster movement support functions.
 *
 * =======================================================================
 */
using System.Numerics;

namespace Quake2 {

    partial class QuakeGame
    {
        private const int STEPSIZE = 18;
        private const int DI_NODIR = -1;

        private int c_yes, c_no;

        /*
        * Called by monster program code.
        * The move will be adjusted for slopes
        * and stairs, but if the move isn't
        * possible, no move is done, false is
        * returned, and pr_global_struct->trace_normal
        * is set to the normal of the blocking wall
        */
        private bool SV_movestep(edict_t ent, in Vector3 move, bool relink)
        {
            // float dz;
            // vec3_t oldorg, neworg, end;
            // trace_t trace;
            // int i;
            // float stepsize;
            // vec3_t test;
            // int contents;

            if (ent == null)
            {
                return false;
            }

            /* try the move */
            var oldorg = ent.s.origin;
            var neworg = ent.s.origin + move;

            /* flying monsters don't step up */
            // if (ent->flags & (FL_SWIM | FL_FLY))
            // {
            //     /* try one move with vertical motion, then one without */
            //     for (i = 0; i < 2; i++)
            //     {
            //         VectorAdd(ent->s.origin, move, neworg);

            //         if ((i == 0) && ent->enemy)
            //         {
            //             if (!ent->goalentity)
            //             {
            //                 ent->goalentity = ent->enemy;
            //             }

            //             dz = ent->s.origin[2] - ent->goalentity->s.origin[2];

            //             if (ent->goalentity->client)
            //             {
            //                 if (dz > 40)
            //                 {
            //                     neworg[2] -= 8;
            //                 }

            //                 if (!((ent->flags & FL_SWIM) && (ent->waterlevel < 2)))
            //                 {
            //                     if (dz < 30)
            //                     {
            //                         neworg[2] += 8;
            //                     }
            //                 }
            //             }
            //             else
            //             {
            //                 if (dz > 8)
            //                 {
            //                     neworg[2] -= 8;
            //                 }
            //                 else if (dz > 0)
            //                 {
            //                     neworg[2] -= dz;
            //                 }
            //                 else if (dz < -8)
            //                 {
            //                     neworg[2] += 8;
            //                 }
            //                 else
            //                 {
            //                     neworg[2] += dz;
            //                 }
            //             }
            //         }

            //         trace = gi.trace(ent->s.origin, ent->mins, ent->maxs,
            //                 neworg, ent, MASK_MONSTERSOLID);

            //         /* fly monsters don't enter water voluntarily */
            //         if (ent->flags & FL_FLY)
            //         {
            //             if (!ent->waterlevel)
            //             {
            //                 test[0] = trace.endpos[0];
            //                 test[1] = trace.endpos[1];
            //                 test[2] = trace.endpos[2] + ent->mins[2] + 1;
            //                 contents = gi.pointcontents(test);

            //                 if (contents & MASK_WATER)
            //                 {
            //                     return false;
            //                 }
            //             }
            //         }

            //         /* swim monsters don't exit water voluntarily */
            //         if (ent->flags & FL_SWIM)
            //         {
            //             if (ent->waterlevel < 2)
            //             {
            //                 test[0] = trace.endpos[0];
            //                 test[1] = trace.endpos[1];
            //                 test[2] = trace.endpos[2] + ent->mins[2] + 1;
            //                 contents = gi.pointcontents(test);

            //                 if (!(contents & MASK_WATER))
            //                 {
            //                     return false;
            //                 }
            //             }
            //         }

            //         if (trace.fraction == 1)
            //         {
            //             VectorCopy(trace.endpos, ent->s.origin);

            //             if (relink)
            //             {
            //                 gi.linkentity(ent);
            //                 G_TouchTriggers(ent);
            //             }

            //             return true;
            //         }

            //         if (!ent->enemy)
            //         {
            //             break;
            //         }
            //     }

            //     return false;
            // }

            /* push down from a step height above the wished position */
            float stepsize = 1;
            // if ((ent.monsterinfo.aiflags & AI_NOSTEP) == 0)
            // {
            //     stepsize = STEPSIZE;
            // }

            neworg.Z += stepsize;
            var end = neworg;
            end.Z -= stepsize * 2;

            var trace = gi.trace(neworg, ent.mins, ent.maxs, end, ent, QShared.MASK_MONSTERSOLID);

            if (trace.allsolid)
            {
                return false;
            }

            if (trace.startsolid)
            {
                neworg.Z -= stepsize;
                trace = gi.trace(neworg, ent.mins, ent.maxs,
                        end, ent, QShared.MASK_MONSTERSOLID);

                if (trace.allsolid || trace.startsolid)
                {
                    return false;
                }
            }

            // /* don't go in to water */
            // if (ent->waterlevel == 0)
            // {
            //     test[0] = trace.endpos[0];
            //     test[1] = trace.endpos[1];
            //     test[2] = trace.endpos[2] + ent->mins[2] + 1;
            //     contents = gi.pointcontents(test);

            //     if (contents & MASK_WATER)
            //     {
            //         return false;
            //     }
            // }

            if (trace.fraction == 1)
            {
            //     /* if monster had the ground pulled out, go ahead and fall */
            //     if (ent->flags & FL_PARTIALGROUND)
            //     {
            //         VectorAdd(ent->s.origin, move, ent->s.origin);

            //         if (relink)
            //         {
            //             gi.linkentity(ent);
            //             G_TouchTriggers(ent);
            //         }

            //         ent->groundentity = NULL;
            //         return true;
            //     }

                return false; /* walked off an edge */
            }

            /* check point traces down for dangling corners */
            ent.s.origin = trace.endpos;

            // if (!M_CheckBottom(ent))
            // {
            //     if (ent->flags & FL_PARTIALGROUND)
            //     {   /* entity had floor mostly pulled out
            //         from underneath it and is trying to
            //         correct */
            //         if (relink)
            //         {
            //             gi.linkentity(ent);
            //             G_TouchTriggers(ent);
            //         }

            //         return true;
            //     }

            //     VectorCopy(oldorg, ent->s.origin);
            //     return false;
            // }

            // if (ent->flags & FL_PARTIALGROUND)
            // {
            //     ent->flags &= ~FL_PARTIALGROUND;
            // }

            ent.groundentity = (edict_t?)trace.ent;
            ent.groundentity_linkcount = trace.ent!.linkcount;

            /* the move is ok */
            if (relink)
            {
                gi.linkentity(ent);
                G_TouchTriggers(ent);
            }

            return true;
        }

        /* ============================================================================ */

        private void M_ChangeYaw(edict_t ent)
        {
            // float ideal;
            // float current;
            // float move;
            // float speed;

            if (ent == null)
            {
                return;
            }

            var current = QShared.anglemod(ent.s.angles[QShared.YAW]);
            var ideal = ent.ideal_yaw;

            if (current == ideal)
            {
                return;
            }

            var move = ideal - current;
            var speed = ent.yaw_speed;

            if (ideal > current)
            {
                if (move >= 180)
                {
                    move = move - 360;
                }
            }
            else
            {
                if (move <= -180)
                {
                    move = move + 360;
                }
            }

            if (move > 0)
            {
                if (move > speed)
                {
                    move = speed;
                }
            }
            else
            {
                if (move < -speed)
                {
                    move = -speed;
                }
            }

            ent.s.angles[QShared.YAW] = QShared.anglemod(current + move);
        }

        /*
        * Turns to the movement direction, and
        * walks the current distance if facing it.
        */
        private bool SV_StepDirection(edict_t ent, float yaw, float dist)
        {
            if (ent == null)
            {
                return false;
            }

            ent.ideal_yaw = yaw;
            M_ChangeYaw(ent);

            yaw = yaw * MathF.PI * 2 / 360;
            var move = new Vector3(
                MathF.Cos(yaw) * dist,
                MathF.Sin(yaw) * dist,
                0);

            var oldorigin = ent.s.origin;

            if (SV_movestep(ent, move, false))
            {
                var delta = ent.s.angles[QShared.YAW] - ent.ideal_yaw;

                if ((delta > 45) && (delta < 315))
                {
                    /* not turned far enough, so don't take the step */
                    ent.s.origin = oldorigin;
                }

                gi.linkentity(ent);
                G_TouchTriggers(ent);
                return true;
            }

            gi.linkentity(ent);
            G_TouchTriggers(ent);
            return false;
        }

        private void SV_FixCheckBottom(edict_t ent)
        {
            if (ent == null)
            {
                return;
            }

            ent.flags |= FL_PARTIALGROUND;
        }

        private void SV_NewChaseDir(edict_t actor, edict_t enemy, float dist)
        {
            // float deltax, deltay;
            // float d[3];
            // float tdir, olddir, turnaround;

            if (actor == null || enemy == null)
            {
                return;
            }

            var olddir = QShared.anglemod((int)(actor.ideal_yaw / 45) * 45);
            var turnaround = QShared.anglemod(olddir - 180);

            var deltax = enemy.s.origin[0] - actor.s.origin[0];
            var deltay = enemy.s.origin[1] - actor.s.origin[1];

            var d = new Vector3();
            if (deltax > 10)
            {
                d[1] = 0;
            }
            else if (deltax < -10)
            {
                d[1] = 180;
            }
            else
            {
                d[1] = DI_NODIR;
            }

            if (deltay < -10)
            {
                d[2] = 270;
            }
            else if (deltay > 10)
            {
                d[2] = 90;
            }
            else
            {
                d[2] = DI_NODIR;
            }

            /* try direct route */
            if ((d[1] != DI_NODIR) && (d[2] != DI_NODIR))
            {
                float tdir;
                if (d[1] == 0)
                {
                    tdir = d[2] == 90 ? 45 : 315;
                }
                else
                {
                    tdir = d[2] == 90 ? 135 : 215;
                }

                if ((tdir != turnaround) && SV_StepDirection(actor, tdir, dist))
                {
                    return;
                }
            }

            /* try other directions */
            if (((QShared.randk() & 3) & 1) != 0 || (MathF.Abs(deltay) > MathF.Abs(deltax)))
            {
                var tdir = d[1];
                d[1] = d[2];
                d[2] = tdir;
            }

            if ((d[1] != DI_NODIR) && (d[1] != turnaround) &&
                SV_StepDirection(actor, d[1], dist))
            {
                return;
            }

            if ((d[2] != DI_NODIR) && (d[2] != turnaround) &&
                SV_StepDirection(actor, d[2], dist))
            {
                return;
            }

            /* there is no direct path to the player, so pick another direction */
            if ((olddir != DI_NODIR) && SV_StepDirection(actor, olddir, dist))
            {
                return;
            }

            if ((QShared.randk() & 1) != 0) /* randomly determine direction of search */
            {
                for (float tdir = 0; tdir <= 315; tdir += 45)
                {
                    if ((tdir != turnaround) && SV_StepDirection(actor, tdir, dist))
                    {
                        return;
                    }
                }
            }
            else
            {
                for (float tdir = 315; tdir >= 0; tdir -= 45)
                {
                    if ((tdir != turnaround) && SV_StepDirection(actor, tdir, dist))
                    {
                        return;
                    }
                }
            }

            if ((turnaround != DI_NODIR) && SV_StepDirection(actor, turnaround, dist))
            {
                return;
            }

            actor.ideal_yaw = olddir; /* can't move */

            /* if a bridge was pulled out from underneath
            a monster, it may not have a valid standing
            position at all */
            // if (!M_CheckBottom(actor))
            // {
            //     SV_FixCheckBottom(actor);
            // }
        }

        private bool SV_CloseEnough(edict_t ent, edict_t goal, float dist)
        {
            if (ent == null || goal == null)
            {
                return false;
            }

            for (int i = 0; i < 3; i++)
            {
                if (goal.absmin[i] > ent.absmax[i] + dist)
                {
                    return false;
                }

                if (goal.absmax[i] < ent.absmin[i] - dist)
                {
                    return false;
                }
            }

            return true;
        }

        private void M_MoveToGoal(edict_t ent, float dist)
        {
            if (ent == null)
            {
                return;
            }

            var goal = ent.goalentity!;

            if (ent.groundentity == null && (ent.flags & (FL_FLY | FL_SWIM)) == 0)
            {
                return;
            }

            /* if the next step hits the enemy, return immediately */
            if (ent.enemy != null && SV_CloseEnough(ent, ent.enemy, dist))
            {
                return;
            }

            /* bump around... */
            if (((QShared.randk() & 3) == 1) || !SV_StepDirection(ent, ent.ideal_yaw, dist))
            {
                if (ent.inuse)
                {
                    SV_NewChaseDir(ent, goal, dist);
                }
            }
        }

        private bool M_walkmove(edict_t ent, float yaw, float dist)
        {
            // vec3_t move;

            if (ent == null)
            {
                return false;
            }

            if (ent.groundentity == null && (ent.flags & (FL_FLY | FL_SWIM)) == 0)
            {
                return false;
            }

            yaw = yaw * MathF.PI * 2 / 360;

            var move = new Vector3(
                MathF.Cos(yaw) * dist,
                MathF.Sin(yaw) * dist,
                0);

            return SV_movestep(ent, move, true);
        }

    }
}