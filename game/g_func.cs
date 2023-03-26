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
 * Level functions. Platforms, buttons, dooors and so on.
 *
 * =======================================================================
 */

using System.Numerics;

namespace Quake2 {

    partial class QuakeGame
    {
        /*
        * =========================================================
        *
        * PLATS
        *
        * movement options:
        *
        * linear
        * smooth start, hard stop
        * smooth start, smooth stop
        *
        * start
        * end
        * acceleration
        * speed
        * deceleration
        * begin sound
        * end sound
        * target fired when reaching end
        * wait at end
        *
        * object characteristics that use move segments
        * ---------------------------------------------
        * movetype_push, or movetype_stop
        * action when touched
        * action when blocked
        * action when used
        *  disabled?
        * auto trigger spawning
        *
        *
        * =========================================================
        */

        private const int PLAT_LOW_TRIGGER = 1;

        private const int STATE_TOP = 0;
        private const int STATE_BOTTOM = 1;
        private const int STATE_UP = 2;
        private const int STATE_DOWN = 3;

        private const int DOOR_START_OPEN = 1;
        private const int DOOR_REVERSE = 2;
        private const int DOOR_CRUSHER = 4;
        private const int DOOR_NOMONSTER = 8;
        private const int DOOR_TOGGLE = 32;
        private const int DOOR_X_AXIS = 64;
        private const int DOOR_Y_AXIS = 128;

        private void Move_Done(edict_t ent)
        {
            if (ent == null)
            {
                return;
            }

            ent.velocity = Vector3.Zero;
            ent.moveinfo.endfunc!(ent);
        }

        private void Move_Final(edict_t ent)
        {
            if (ent == null)
            {
                return;
            }

            if (ent.moveinfo.remaining_distance == 0)
            {
                Move_Done(ent);
                return;
            }

            ent.velocity = ent.moveinfo.remaining_distance / FRAMETIME * ent.moveinfo.dir;

            ent.think = Move_Done;
            ent.nextthink = level.time + FRAMETIME;
        }

        private void Move_Begin(edict_t ent)
        {
            if (ent == null)
            {
                return;
            }
            Console.WriteLine($"Move_Begin {ent.classname}");

            if ((ent.moveinfo.speed * FRAMETIME) >= ent.moveinfo.remaining_distance)
            {
                Move_Final(ent);
                return;
            }

            ent.velocity = ent.moveinfo.speed * ent.moveinfo.dir;
            var frames = MathF.Floor(
                    (ent.moveinfo.remaining_distance /
                    ent.moveinfo.speed) / FRAMETIME);
            ent.moveinfo.remaining_distance -= frames * ent.moveinfo.speed *
                                                FRAMETIME;
            ent.nextthink = level.time + (frames * FRAMETIME);
            ent.think = Move_Final;
        }

        private void Move_Calc(edict_t ent, in Vector3 dest, edict_delegate func)
        {
            if (ent == null)
            {
                return;
            }
            Console.WriteLine($"Move_Calc {ent.classname}");

            ent.velocity = Vector3.Zero;
            ent.moveinfo.dir = dest - ent.s.origin;
            ent.moveinfo.remaining_distance = ent.moveinfo.dir.Length();
            ent.moveinfo.dir = Vector3.Normalize(ent.moveinfo.dir);
            ent.moveinfo.endfunc = func;

            if ((ent.moveinfo.speed == ent.moveinfo.accel) &&
                (ent.moveinfo.speed == ent.moveinfo.decel))
            {
                if (level.current_entity ==
                    ((ent.flags & FL_TEAMSLAVE) != 0 ? ent.teammaster : ent))
                {
                    Move_Begin(ent);
                }
                else
                {
                    ent.nextthink = level.time + FRAMETIME;
                    ent.think = Move_Begin;
                }
            }
            else
            {
                /* accelerative */
                ent.moveinfo.current_speed = 0;
                ent.think = Think_AccelMove;
                ent.nextthink = level.time + FRAMETIME;
            }
        }

        /*
        * The team has completed a frame of movement,
        * so change the speed for the next frame
        */
        private void Think_AccelMove(edict_t ent)
        {
            if (ent == null)
            {
                return;
            }

            ent.moveinfo.remaining_distance -= ent.moveinfo.current_speed;

            // if (ent.moveinfo.current_speed == 0) /* starting or blocked */
            // {
            //     plat_CalcAcceleratedMove(&ent->moveinfo);
            // }

            // plat_Accelerate(&ent->moveinfo);

            /* will the entire move complete on next frame? */
            if (ent.moveinfo.remaining_distance <= ent.moveinfo.current_speed)
            {
                Move_Final(ent);
                return;
            }

            ent.velocity = ent.moveinfo.current_speed * 10 * ent.moveinfo.dir;
            ent.nextthink = level.time + FRAMETIME;
            ent.think = Think_AccelMove;
        }


        private void door_hit_top(edict_t self)
        {
            if (self == null)
            {
                return;
            }
            Console.WriteLine("door_hit_top");

            // if (!(self->flags & FL_TEAMSLAVE))
            // {
            //     if (self->moveinfo.sound_end)
            //     {
            //         gi.sound(self, CHAN_NO_PHS_ADD + CHAN_VOICE, self->moveinfo.sound_end,
            //                 1, ATTN_STATIC, 0);
            //     }

            //     self->s.sound = 0;
            // }

            self.moveinfo.state = STATE_TOP;

            if ((self.spawnflags & DOOR_TOGGLE) != 0)
            {
                return;
            }

            // if (self.moveinfo.wait >= 0)
            // {
            //     self.think = door_go_down;
            //     self.nextthink = level.time + self.moveinfo.wait;
            // }
        }

        private void door_go_up(edict_t  self, edict_t activator)
        {
            if (self == null)
            {
                return;
            }

            if (self.moveinfo.state == STATE_UP)
            {
                return; /* already going up */
            }

            if (self.moveinfo.state == STATE_TOP)
            {
                /* reset top wait time */
                if (self.moveinfo.wait >= 0)
                {
                    self.nextthink = level.time + self.moveinfo.wait;
                }

                return;
            }

            if ((self.flags & FL_TEAMSLAVE) == 0)
            {
                // if (self->moveinfo.sound_start)
                // {
                //     gi.sound(self, CHAN_NO_PHS_ADD + CHAN_VOICE,
                //             self->moveinfo.sound_start, 1,
                //             ATTN_STATIC, 0);
                // }

                // self->s.sound = self->moveinfo.sound_middle;
            }

            self.moveinfo.state = STATE_UP;

            if (self.classname == "func_door")
            {
                Move_Calc(self, self.moveinfo.end_origin, door_hit_top);
            }
            else if (self.classname == "func_door_rotating")
            {
                // AngleMove_Calc(self, door_hit_top);
            }

            G_UseTargets(self, activator);
            // door_use_areaportals(self, true);
        }

        private void door_use(edict_t self, edict_t _other, edict_t activator)
        {
            if (self == null || activator == null)
            {
                return;
            }

            if ((self.flags & FL_TEAMSLAVE) != 0)
            {
                return;
            }

            if ((self.spawnflags & DOOR_TOGGLE) != 0)
            {
                if ((self.moveinfo.state == STATE_UP) ||
                    (self.moveinfo.state == STATE_TOP))
                {
                    /* trigger all paired doors */
                    for (var ent = self; ent != null; ent = ent.teamchain)
                    {
                        ent.message = null;
                        ent.touch = null;
            //             door_go_down(ent);
                    }

                    return;
                }
            }

            /* trigger all paired doors */
            for (var ent = self; ent != null; ent = ent.teamchain)
            {
                ent.message = null;
                ent.touch = null;
                door_go_up(ent, activator);
            }
        }

        private void Touch_DoorTrigger(edict_t self, edict_t other, QShared.cplane_t? _plane,
                in QShared.csurface_t? _surf)
        {
            if (self == null || other == null)
            {
                return;
            }

            if (other.health <= 0)
            {
                return;
            }

            if ((other.svflags & QGameFlags.SVF_MONSTER) == 0 && (other.client == null))
            {
                return;
            }

            if ((((edict_t)self.owner!).spawnflags & DOOR_NOMONSTER) != 0 &&
                (other.svflags & QGameFlags.SVF_MONSTER) != 0)
            {
                return;
            }

            if (level.time < self.touch_debounce_time)
            {
                return;
            }

            self.touch_debounce_time = level.time + 1.0f;

            door_use((edict_t)self.owner!, other, other);
        }

        private void Think_CalcMoveSpeed(edict_t self)
        {
            if (self == null)
            {
                return;
            }
            
            if ((self.flags & FL_TEAMSLAVE) != 0)
            {
                return; /* only the team master does this */
            }

            /* find the smallest distance any member of the team will be moving */
            var min = MathF.Abs(self.moveinfo.distance);

            for (var ent = self.teamchain; ent != null; ent = ent.teamchain)
            {
                var dist = MathF.Abs(ent.moveinfo.distance);

                if (dist < min)
                {
                    min = dist;
                }
            }

            var time = min / self.moveinfo.speed;

            /* adjust speeds so they will all complete at the same time */
            for (edict_t? ent = self; ent != null; ent = ent.teamchain)
            {
                var newspeed = MathF.Abs(ent.moveinfo.distance) / time;
                var ratio = newspeed / ent.moveinfo.speed;

                if (ent.moveinfo.accel == ent.moveinfo.speed)
                {
                    ent.moveinfo.accel = newspeed;
                }
                else
                {
                    ent.moveinfo.accel *= ratio;
                }

                if (ent.moveinfo.decel == ent.moveinfo.speed)
                {
                    ent.moveinfo.decel = newspeed;
                }
                else
                {
                    ent.moveinfo.decel *= ratio;
                }

                ent.moveinfo.speed = newspeed;
            }
        }

        private void Think_SpawnDoorTrigger(edict_t ent)
        {
            // edict_t *other;
            // vec3_t mins, maxs;

            if (ent == null)
            {
                return;
            }
            Console.WriteLine($"Think_SpawnDoorTrigger {ent.classname}");

            if ((ent.flags & FL_TEAMSLAVE) != 0)
            {
                return; /* only the team leader spawns a trigger */
            }

            var mins = ent.absmin;
            var maxs = ent.absmax;

            // for (other = ent->teamchain; other; other = other->teamchain)
            // {
            //     AddPointToBounds(other->absmin, mins, maxs);
            //     AddPointToBounds(other->absmax, mins, maxs);
            // }

            /* expand */
            mins[0] -= 60;
            mins[1] -= 60;
            maxs[0] += 60;
            maxs[1] += 60;

            var other = G_Spawn();
            other.mins = mins;
            other.maxs = maxs;
            other.owner = ent;
            other.solid = solid_t.SOLID_TRIGGER;
            other.movetype = movetype_t.MOVETYPE_NONE;
            other.touch = Touch_DoorTrigger;
            gi.linkentity(other);

            if ((ent.spawnflags & DOOR_START_OPEN) != 0)
            {
            //     door_use_areaportals(ent, true);
            }

            Think_CalcMoveSpeed(ent);
        }

        private static void SP_func_door(QuakeGame g, edict_t ent)
        {
            // vec3_t abs_movedir;

            if (ent == null)
            {
                return;
            }

            // if (ent.sounds != 1)
            // {
            //     ent->moveinfo.sound_start = gi.soundindex("doors/dr1_strt.wav");
            //     ent->moveinfo.sound_middle = gi.soundindex("doors/dr1_mid.wav");
            //     ent->moveinfo.sound_end = gi.soundindex("doors/dr1_end.wav");
            // }

            g.G_SetMovedir(ref ent.s.angles, ref ent.movedir);
            ent.movetype = movetype_t.MOVETYPE_PUSH;
            ent.solid = solid_t.SOLID_BSP;
            g.gi.setmodel(ent, ent.model!);

        //     ent->blocked = door_blocked;
            ent.use = g.door_use;

            if (ent.speed == 0)
            {
                ent.speed = 100;
            }

        //     if (deathmatch->value)
        //     {
        //         ent->speed *= 2;
        //     }

            if (ent.accel == 0)
            {
                ent.accel = ent.speed;
            }

            if (ent.decel == 0)
            {
                ent.decel = ent.speed;
            }

            if (ent.wait == 0)
            {
                ent.wait = 3;
            }

            if (g.st.lip == 0)
            {
                g.st.lip = 8;
            }

            if (ent.dmg == 0)
            {
                ent.dmg = 2;
            }

            /* calculate second position */
            ent.pos1 = ent.s.origin;
            var abs_movedir = new Vector3(
                MathF.Abs(ent.movedir[0]),
                MathF.Abs(ent.movedir[1]),
                MathF.Abs(ent.movedir[2]));
            ent.moveinfo.distance = abs_movedir[0] * ent.size[0] + abs_movedir[1] *
                                    ent.size[1] + abs_movedir[2] * ent.size[2] -
                                    g.st.lip;
            QShared.VectorMA(ent.pos1, ent.moveinfo.distance, ent.movedir, out ent.pos2);

            /* if it starts open, switch the positions */
            if ((ent.spawnflags & DOOR_START_OPEN) != 0)
            {
                ent.s.origin = ent.pos2;
                ent.pos2 = ent.pos1;
                ent.pos1 = ent.s.origin;
            }

            ent.moveinfo.state = STATE_BOTTOM;

            if (ent.health != 0)
            {
        //         ent->takedamage = DAMAGE_YES;
        //         ent->die = door_killed;
        //         ent->max_health = ent->health;
            }
        //     else if (ent->targetname && ent->message)
        //     {
        //         gi.soundindex("misc/talk.wav");
        //         ent->touch = door_touch;
        //     }

            ent.moveinfo.speed = ent.speed;
            ent.moveinfo.accel = ent.accel;
            ent.moveinfo.decel = ent.decel;
            ent.moveinfo.wait = ent.wait;
            ent.moveinfo.start_origin = ent.pos1;
            ent.moveinfo.start_angles = ent.s.angles;
            ent.moveinfo.end_origin = ent.pos2;
            ent.moveinfo.end_angles = ent.s.angles;

        //     if (ent->spawnflags & 16)
        //     {
        //         ent->s.effects |= EF_ANIM_ALL;
        //     }

        //     if (ent->spawnflags & 64)
        //     {
        //         ent->s.effects |= EF_ANIM_ALLFAST;
        //     }

            /* to simplify logic elsewhere, make non-teamed doors into a team of one */
            if (ent.team == null)
            {
                ent.teammaster = ent;
            }

            g.gi.linkentity(ent);

            ent.nextthink = g.level.time + FRAMETIME;

            if (ent.health != 0 || ent.targetname != null)
            {
                ent.think = g.Think_CalcMoveSpeed;
            }
            else
            {
                ent.think = g.Think_SpawnDoorTrigger;
            }
        }

    }
}