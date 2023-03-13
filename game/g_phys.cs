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
 * Quake IIs legendary physic engine.
 *
 * =======================================================================
 */
using System.Numerics;

namespace Quake2 {

    partial class QuakeGame
    {
        private const float STOP_EPSILON = 0.1f;
        private const int MAX_CLIP_PLANES = 5;
        private const int STOPSPEED = 100;
        private const int FRICTION = 6;
        private const int WATERFRICTION = 1;


        private void SV_CheckVelocity(edict_t ent)
        {
            if (ent == null)
            {
                return;
            }

            if (ent.velocity.Length() > sv_maxvelocity!.Float)
            {
                ent.velocity = Vector3.Normalize(ent.velocity);
                ent.velocity = ent.velocity * sv_maxvelocity!.Float;
            }
        }

        /*
        * Runs thinking code for
        * this frame if necessary
        */
        private bool SV_RunThink(edict_t ent)
        {
            if (ent == null)
            {
                return false;
            }

            float thinktime = ent.nextthink;

            if (thinktime <= 0)
            {
                return true;
            }

            if (thinktime > level.time + 0.001)
            {
                return true;
            }

            ent.nextthink = 0;

            if (ent.think == null)
            {
                gi.error($"NULL ent->think {ent.classname}");
            }

            ent.think!(ent);

            return false;
        }

        /*
        * Slide off of the impacting object
        * returns the blocked flags (1 = floor,
        * 2 = step / wall)
        */
        private int ClipVelocity(in Vector3 ind, in Vector3 normal, ref Vector3 outd, float overbounce)
        {
            var blocked = 0;

            if (normal.Z > 0)
            {
                blocked |= 1; /* floor */
            }

            if (normal.Z == 0)
            {
                blocked |= 2; /* step */
            }

            var backoff = Vector3.Dot(ind, normal) * overbounce;

            for (int i = 0; i < 3; i++)
            {
                var change = normal.Get(i) * backoff;
                outd.Set(i, ind.Get(i) - change);

                if ((outd.Get(i) > -STOP_EPSILON) && (outd.Get(i) < STOP_EPSILON))
                {
                    outd.Set(i, 0);
                }
            }

            return blocked;
        }

        /* ================================================================== */

        /*
        * Non moving objects can only think
        */
        private void SV_Physics_None(edict_t ent)
        {
            if (ent == null)
            {
                return;
            }

            /* regular thinking */
            SV_RunThink(ent);
        }

        /*
        * The basic solid body movement clip
        * that slides along multiple planes
        * Returns the clipflags if the velocity
        * was modified (hit something solid)
        *
        * 1 = floor
        * 2 = wall / step
        * 4 = dead stop
        */
        private int SV_FlyMove(edict_t ent, float time, int mask)
        {
            // edict_t *hit;
            // int bumpcount, numbumps;
            // vec3_t dir;
            // float d;
            // int numplanes;
            // vec3_t planes[MAX_CLIP_PLANES];
            // vec3_t primal_velocity, original_velocity, new_velocity;
            // int i, j;
            // trace_t trace;
            // vec3_t end;
            // float time_left;
            // int blocked;

            if (ent == null)
            {
                return 0;
            }

            int numbumps = 4;
            var planes = new Vector3[MAX_CLIP_PLANES];

            int blocked = 0;
            var original_velocity = ent.velocity;
            var primal_velocity = ent.velocity;
            int numplanes = 0;

            float time_left = time;

            ent.groundentity = null;

            for (int bumpcount = 0; bumpcount < numbumps; bumpcount++)
            {
                var end = ent.s.origin + time_left * ent.velocity;

                var trace = gi.trace(ent.s.origin, ent.mins, ent.maxs, end, ent, mask);

                if (trace.allsolid)
                {
                    /* entity is trapped in another solid */
                    ent.velocity = Vector3.Zero;
                    return 3;
                }

                if (trace.fraction > 0)
                {
                    /* actually covered some distance */
                    ent.s.origin = trace.endpos;
                    original_velocity = ent.velocity;
                    numplanes = 0;
                }

                if (trace.fraction == 1)
                {
                    break; /* moved the entire distance */
                }

                var hit = (edict_t)trace.ent!;

                if (trace.plane.normal.Z > 0.7)
                {
                    blocked |= 1; /* floor */

                    if (hit.solid == solid_t.SOLID_BSP)
                    {
                        ent.groundentity = hit;
                        ent.groundentity_linkcount = hit.linkcount;
                    }
                }

                if (trace.plane.normal.Z == 0)
                {
                    blocked |= 2; /* step */
                }

                /* run the impact function */
                // SV_Impact(ent, &trace);

                if (!ent.inuse)
                {
                    break; /* removed by the impact function */
                }

                time_left -= time_left * trace.fraction;

                /* cliped to another plane */
                if (numplanes >= MAX_CLIP_PLANES)
                {
                    /* this shouldn't really happen */
                    ent.velocity = Vector3.Zero;
                    return 3;
                }

                planes[numplanes] = trace.plane.normal;
                numplanes++;

                /* modify original_velocity so it
                parallels all of the clip planes */
                int i;
                var new_velocity = new Vector3();
                for (i = 0; i < numplanes; i++)
                {
                    ClipVelocity(original_velocity, planes[i], ref new_velocity, 1);

                    int j;
                    for (j = 0; j < numplanes; j++)
                    {
                        if ((j != i) && planes[i] != planes[j])
                        {
                            if (Vector3.Dot(new_velocity, planes[j]) < 0)
                            {
                                break; /* not ok */
                            }
                        }
                    }

                    if (j == numplanes)
                    {
                        break;
                    }
                }

                if (i != numplanes)
                {
                    /* go along this plane */
                    ent.velocity = new_velocity;
                }
                else
                {
                    /* go along the crease */
                    if (numplanes != 2)
                    {
                        ent.velocity = Vector3.Zero;
                        return 7;
                    }

                    var dir = Vector3.Cross(planes[0], planes[1]);
                    var d =  Vector3.Dot(dir, ent.velocity);
                    ent.velocity = d * dir;
                }

                /* if original velocity is against the original
                velocity, stop dead to avoid tiny occilations
                in sloping corners */
                if (Vector3.Dot(ent.velocity, primal_velocity) <= 0)
                {
                    ent.velocity = Vector3.Zero;
                    return blocked;
                }
            }

            return blocked;
        }


        private void SV_AddGravity(edict_t ent)
        {
            if (ent == null)
            {
                return;
            }

            ent.velocity.Z -= ent.gravity * sv_gravity!.Float * FRAMETIME;
        }

        /* ================================================================== */

        /* PUSHMOVE */

        /*
        * Does not change the entities velocity at all
        */
        private QShared.trace_t SV_PushEntity(edict_t ent, in Vector3 push)
        {
            // var trace = new QShared.trace_t();
        //     vec3_t start;
        //     vec3_t end;
        //     int mask;

            var start = ent.s.origin;
            var end = start + push;

        // retry:

            int mask;
            if (ent.clipmask != 0)
            {
                mask = ent.clipmask;
            }
            else
            {
                mask = QShared.MASK_SOLID;
            }

            var trace = gi.trace(start, ent.mins, ent.maxs, end, ent, mask);

            if (trace.startsolid || trace.allsolid)
            {
                mask ^= QCommon.CONTENTS_DEADMONSTER;
                trace = gi.trace (start, ent.mins, ent.maxs, end, ent, mask);
            }

            ent.s.origin = trace.endpos;
            gi.linkentity(ent);

            /* Push slightly away from non-horizontal surfaces,
               prevent origin stuck in the plane which causes
               the entity to be rendered in full black. */
            if (trace.plane.type != 2)
            {
        //         /* Limit the fix to gibs, debris and dead monsters.
        //         Everything else may break existing maps. Items
        //         may slide to unreachable locations, monsters may
        //         get stuck, etc. */
        //         if (((strncmp(ent->classname, "monster_", 8) == 0) && ent->health < 1) ||
        //                 (strcmp(ent->classname, "debris") == 0) || (ent->s.effects & EF_GIB))
        //         {
        //             VectorAdd(ent->s.origin, trace.plane.normal, ent->s.origin);
        //         }
            }

            if (trace.fraction != 1.0)
            {
                Console.WriteLine("SV_Impact");
        //         SV_Impact(ent, &trace);

        //         /* if the pushed entity went away
        //         and the pusher is still there */
        //         if (!trace.ent->inuse && ent->inuse)
        //         {
        //             /* move the pusher back and try again */
        //             VectorCopy(start, ent->s.origin);
        //             gi.linkentity(ent);
        //             goto retry;
        //         }
            }

            if (ent.inuse)
            {
                // Console.WriteLine("G_TouchTriggers");
        //         G_TouchTriggers(ent);
            }

            return trace;
        }

        /*
        * Bmodel objects don't interact with each
        * other, but push all box objects
        */
        private void SV_Physics_Pusher(edict_t ent)
        {
            // vec3_t move, amove;
            // edict_t *part, *mv;

            if (ent == null)
            {
                return;
            }

            /* if not a team captain, so movement
            will be handled elsewhere */
            if ((ent.flags & FL_TEAMSLAVE) != 0)
            {
                return;
            }

            /* make sure all team slaves can move before commiting
            any moves or calling any think functions if the move
            is blocked, all moved objects will be backed out */
            // pushed_p = pushed;

            edict_t? part = null;
            for (part = ent; part != null; part = part.teamchain)
            {
            //     if (part->velocity[0] || part->velocity[1] || part->velocity[2] ||
            //         part->avelocity[0] || part->avelocity[1] || part->avelocity[2])
            //     {
            //         /* object is moving */
            //         VectorScale(part->velocity, FRAMETIME, move);
            //         VectorScale(part->avelocity, FRAMETIME, amove);

            //         if (!SV_Push(part, move, amove))
            //         {
            //             break; /* move was blocked */
            //         }
            //     }
            }

            // if (pushed_p > &pushed[MAX_EDICTS -1 ])
            // {
            //     gi.error("pushed_p > &pushed[MAX_EDICTS - 1], memory corrupted");
            // }

            if (part != null)
            {
                /* the move failed, bump all nextthink
                times and back out moves */
            //     for (mv = ent; mv; mv = mv->teamchain)
            //     {
            //         if (mv->nextthink > 0)
            //         {
            //             mv->nextthink += FRAMETIME;
            //         }
            //     }

            //     /* if the pusher has a "blocked" function, call it
            //     otherwise, just stay in place until the obstacle
            //     is gone */
            //     if (part->blocked)
            //     {
            //         part->blocked(part, obstacle);
            //     }
            }
            else
            {
                /* the move succeeded, so call all think functions */
                for (part = ent; part != null; part = part.teamchain)
                {
                    SV_RunThink(part);
                }
            }
        }


        /* ================================================================== */

        /* TOSS / BOUNCE */

        /*
        * Toss, bounce, and fly movement.
        * When onground, do nothing.
        */
        private void SV_Physics_Toss(edict_t ent)
        {
            // trace_t trace;
            // vec3_t move;
            // float backoff;
            // edict_t *slave;
            // qboolean wasinwater;
            // qboolean isinwater;
            // vec3_t old_origin;

            if (ent == null)
            {
                return;
            }

            /* regular thinking */
            SV_RunThink(ent);

            /* entities are very often freed during thinking */
            if (!ent.inuse)
            {
                return;
            }

            /* if not a team captain, so movement
            will be handled elsewhere */
            if ((ent.flags & FL_TEAMSLAVE) != 0)
            {
                return;
            }

            if (ent.velocity.Z > 0)
            {
                ent.groundentity = null;
            }

            /* check for the groundentity going away */
            if (ent.groundentity != null)
            {
                if (!ent.groundentity.inuse)
                {
                    ent.groundentity = null;
                }
            }

            /* if onground, return without moving */
            if (ent.groundentity != null)
            {
                return;
            }

            var old_origin = ent.s.origin;

            SV_CheckVelocity(ent);

            /* add gravity */
            if ((ent.movetype != movetype_t.MOVETYPE_FLY) &&
                (ent.movetype != movetype_t.MOVETYPE_FLYMISSILE))
            {
                SV_AddGravity(ent);
            }

            /* move angles */
            QShared.VectorMA(ent.s.angles, FRAMETIME, ent.avelocity, out ent.s.angles);

            /* move origin */
            var move = ent.velocity * FRAMETIME;
            var trace = SV_PushEntity(ent, move);

            if (!ent.inuse)
            {
                return;
            }

            if (trace.fraction < 1)
            {
                var backoff = 1.0f;
                if (ent.movetype == movetype_t.MOVETYPE_BOUNCE)
                {
                    backoff = 1.5f;
                }

                ClipVelocity(ent.velocity, trace.plane.normal, ref ent.velocity, backoff);

                /* stop if on ground */
                if (trace.plane.normal.Z > 0.7)
                {
                    if ((ent.velocity.Z < 60) || (ent.movetype != movetype_t.MOVETYPE_BOUNCE))
                    {
                        ent.groundentity = (edict_t)trace.ent!;
                        ent.groundentity_linkcount = trace.ent!.linkcount;
                        ent.velocity = Vector3.Zero;
                        ent.avelocity = Vector3.Zero;
                    }
                }
            }

            /* check for water transition */
            var wasinwater = (ent.watertype & QShared.MASK_WATER) != 0;
            // ent->watertype = gi.pointcontents(ent->s.origin);
            var isinwater = (ent.watertype & QShared.MASK_WATER) != 0;

            if (isinwater)
            {
                 ent.waterlevel = 1;
            }
            else
            {
                ent.waterlevel = 0;
            }

            // if (!wasinwater && isinwater)
            // {
            //     gi.positioned_sound(old_origin, g_edicts, CHAN_AUTO,
            //             gi.soundindex("misc/h2ohit1.wav"), 1, 1, 0);
            // }
            // else if (wasinwater && !isinwater)
            // {
            //     gi.positioned_sound(ent->s.origin, g_edicts, CHAN_AUTO,
            //             gi.soundindex("misc/h2ohit1.wav"), 1, 1, 0);
            // }

            // /* move teamslaves */
            // for (slave = ent->teamchain; slave; slave = slave->teamchain)
            // {
            //     VectorCopy(ent->s.origin, slave->s.origin);
            //     gi.linkentity(slave);
            // }
        }

        /* ================================================================== */

        /* STEPPING MOVEMENT */


        private void SV_Physics_Step(edict_t ent)
        {
            // qboolean wasonground;
            // qboolean hitsound = false;
            // float *vel;
            // float speed, newspeed, control;
            // float friction;
            // edict_t *groundentity;
            // int mask;
            // vec3_t oldorig;
            // trace_t tr;

            if (ent == null)
            {
                return;
            }

            /* airborn monsters should always check for ground */
            if (ent.groundentity == null)
            {
                M_CheckGround(ent);
            }

            var groundentity = ent.groundentity;

            SV_CheckVelocity(ent);

            bool wasonground;
            if (groundentity != null)
            {
                wasonground = true;
            }
            else
            {
                wasonground = false;
            }

            // if (ent->avelocity[0] || ent->avelocity[1] || ent->avelocity[2])
            // {
            //     SV_AddRotationalFriction(ent);
            // }

            /* add gravity except:
                flying monsters
                swimming monsters who are in the water */
            if (!wasonground)
            {
                if ((ent.flags & FL_FLY) == 0)
                {
            //         if (!((ent->flags & FL_SWIM) && (ent->waterlevel > 2)))
            //         {
            //             if (ent->velocity[2] < sv_gravity->value * -0.1)
            //             {
            //                 hitsound = true;
            //             }

            //             if (ent->waterlevel == 0)
            //             {
                            SV_AddGravity(ent);
            //             }
            //         }
                }
            }

            /* friction for flying monsters that have been given vertical velocity */
            // if ((ent->flags & FL_FLY) && (ent->velocity[2] != 0))
            // {
            //     speed = fabs(ent->velocity[2]);
            //     control = speed < STOPSPEED ? STOPSPEED : speed;
            //     friction = FRICTION / 3;
            //     newspeed = speed - (FRAMETIME * control * friction);

            //     if (newspeed < 0)
            //     {
            //         newspeed = 0;
            //     }

            //     newspeed /= speed;
            //     ent->velocity[2] *= newspeed;
            // }

            /* friction for flying monsters that have been given vertical velocity */
            // if ((ent->flags & FL_SWIM) && (ent->velocity[2] != 0))
            // {
            //     speed = fabs(ent->velocity[2]);
            //     control = speed < STOPSPEED ? STOPSPEED : speed;
            //     newspeed = speed - (FRAMETIME * control * WATERFRICTION * ent->waterlevel);

            //     if (newspeed < 0)
            //     {
            //         newspeed = 0;
            //     }

            //     newspeed /= speed;
            //     ent->velocity[2] *= newspeed;
            // }

            if (ent.velocity.Length() != 0)
            {
                /* apply friction: let dead monsters who
                aren't completely onground slide */
            //     if ((wasonground) || (ent->flags & (FL_SWIM | FL_FLY)))
            //     {
            //         if (!((ent->health <= 0.0) && !M_CheckBottom(ent)))
            //         {
            //             vel = ent->velocity;
            //             speed = sqrt(vel[0] * vel[0] + vel[1] * vel[1]);

            //             if (speed)
            //             {
            //                 friction = FRICTION;

            //                 control = speed < STOPSPEED ? STOPSPEED : speed;
            //                 newspeed = speed - FRAMETIME * control * friction;

            //                 if (newspeed < 0)
            //                 {
            //                     newspeed = 0;
            //                 }

            //                 newspeed /= speed;

            //                 vel[0] *= newspeed;
            //                 vel[1] *= newspeed;
            //             }
            //         }
            //     }

                var mask = QShared.MASK_SOLID;
                if ((ent.svflags & QGameFlags.SVF_MONSTER) != 0)
                {
                    mask = QShared.MASK_MONSTERSOLID;
                }

            //     VectorCopy(ent->s.origin, oldorig);
                SV_FlyMove(ent, FRAMETIME, mask);

            //     /* Evil hack to work around dead parasites (and maybe other monster)
            //     falling through the worldmodel into the void. We copy the current
            //     origin (see above) and after the SV_FlyMove() was performend we
            //     checl if we're stuck in the world model. If yes we're undoing the
            //     move. */
            //     if (!VectorCompare(ent->s.origin, oldorig))
            //     {
            //         tr = gi.trace(ent->s.origin, ent->mins, ent->maxs, ent->s.origin, ent, mask);

            //         if (tr.startsolid)
            //         {
            //             VectorCopy(oldorig, ent->s.origin);
            //         }
            //     }

                gi.linkentity(ent);
            //     G_TouchTriggers(ent);

                if (!ent.inuse)
                {
                    return;
                }

            //     if (ent->groundentity)
            //     {
            //         if (!wasonground)
            //         {
            //             if (hitsound)
            //             {
            //                 gi.sound(ent, 0, gi.soundindex("world/land.wav"), 1, 1, 0);
            //             }
            //         }
            //     }
            }

            /* regular thinking */
            SV_RunThink(ent);
        }

        /* ================================================================== */

        private void G_RunEntity(edict_t ent)
        {
            if (ent == null)
            {
                return;
            }

            if (ent.prethink != null)
            {
                ent.prethink(ent);
            }

            switch (ent.movetype)
            {
                case movetype_t.MOVETYPE_PUSH:
                    goto case movetype_t.MOVETYPE_STOP;
                case movetype_t.MOVETYPE_STOP:
                    SV_Physics_Pusher(ent);
                    break;
                case movetype_t.MOVETYPE_NONE:
                    SV_Physics_None(ent);
                    break;
                // case movetype_t.MOVETYPE_NOCLIP:
            //         SV_Physics_Noclip(ent);
            //         break;
                case movetype_t.MOVETYPE_STEP:
                    SV_Physics_Step(ent);
                    break;
                case movetype_t.MOVETYPE_TOSS:
                    goto case movetype_t.MOVETYPE_FLYMISSILE;
                case movetype_t.MOVETYPE_BOUNCE:
                    goto case movetype_t.MOVETYPE_FLYMISSILE;
                case movetype_t.MOVETYPE_FLY:
                    goto case movetype_t.MOVETYPE_FLYMISSILE;
                case movetype_t.MOVETYPE_FLYMISSILE:
                    SV_Physics_Toss(ent);
                    break;
                default:
                    gi.error($"SV_Physics: bad movetype {ent.movetype}");
                    break;
            }
        }

    }
}