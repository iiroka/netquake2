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

            // /* if not a team captain, so movement
            // will be handled elsewhere */
            // if (ent->flags & FL_TEAMSLAVE)
            // {
            //     return;
            // }

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
            //     SV_AddGravity(ent);
            }

            /* move angles */
            // VectorMA(ent->s.angles, FRAMETIME, ent->avelocity, ent->s.angles);

            // /* move origin */
            // VectorScale(ent->velocity, FRAMETIME, move);
            // trace = SV_PushEntity(ent, move);

            if (!ent.inuse)
            {
                return;
            }

            // if (trace.fraction < 1)
            // {
            //     if (ent->movetype == MOVETYPE_BOUNCE)
            //     {
            //         backoff = 1.5;
            //     }
            //     else
            //     {
            //         backoff = 1;
            //     }

            //     ClipVelocity(ent->velocity, trace.plane.normal, ent->velocity, backoff);

            //     /* stop if on ground */
            //     if (trace.plane.normal[2] > 0.7)
            //     {
            //         if ((ent->velocity[2] < 60) || (ent->movetype != MOVETYPE_BOUNCE))
            //         {
            //             ent->groundentity = trace.ent;
            //             ent->groundentity_linkcount = trace.ent->linkcount;
            //             VectorCopy(vec3_origin, ent->velocity);
            //             VectorCopy(vec3_origin, ent->avelocity);
            //         }
            //     }
            // }

            // /* check for water transition */
            // wasinwater = (ent->watertype & MASK_WATER);
            // ent->watertype = gi.pointcontents(ent->s.origin);
            // isinwater = ent->watertype & MASK_WATER;

            // if (isinwater)
            // {
            //     ent->waterlevel = 1;
            // }
            // else
            // {
            //     ent->waterlevel = 0;
            // }

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
            //     case movetype_t.MOVETYPE_STEP:
            //         SV_Physics_Step(ent);
            //         break;
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