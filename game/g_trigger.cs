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
 * Trigger.
 *
 * =======================================================================
 */

using System.Numerics; 

namespace Quake2 {

    partial class QuakeGame
    {
        private const int PUSH_ONCE = 1;

        private int _trigger_windsound;

        private void InitTrigger(edict_t self)
        {
            if (self == null)
            {
                return;
            }

            if (self.s.angles != Vector3.Zero)
            {
                G_SetMovedir(ref self.s.angles, ref self.movedir);
            }

            self.solid = solid_t.SOLID_TRIGGER;
            self.movetype = movetype_t.MOVETYPE_NONE;
            gi.setmodel(self, self.model!);
            self.svflags = QGameFlags.SVF_NOCLIENT;
        }

        /*
        * The wait time has passed, so
        * set back up for another activation
        */
        private void multi_wait(edict_t ent)
        {
            if (ent == null)
            {
                return;
            }

            ent.nextthink = 0;
        }

        /*
        * The trigger was just activated
        * ent->activator should be set to
        * the activator so it can be held
        * through a delay so wait for the
        * delay time before firing
        */
        private void multi_trigger(edict_t ent)
        {
            if (ent == null)
            {
                return;
            }

            if (ent.nextthink != 0)
            {
                return; /* already been triggered */
            }

            G_UseTargets(ent, ent.activator);

            if (ent.wait > 0)
            {
                ent.think = multi_wait;
                ent.nextthink = level.time + ent.wait;
            }
            else
            {
                /* we can't just remove (self) here,
                because this is a touch function
                called while looping through area
                links... */
                ent.touch = null;
                ent.nextthink = level.time + FRAMETIME;
                ent.think = G_FreeEdict;
            }
        }

        private void Use_Multi(edict_t ent, edict_t _other /* unused */, edict_t? activator)
        {
            if (ent == null || activator == null)
            {
                return;
            }

            ent.activator = activator;
            multi_trigger(ent);
        }

        private void Touch_Multi(edict_t self, edict_t other, QShared.cplane_t? _plane /* unused */,
                in QShared.csurface_t? _surf /* unused */)
        {
            if (self == null || other == null)
            {
                return;
            }

            if (other.client != null)
            {
                if ((self.spawnflags & 2) != 0)
                {
                    return;
                }
            }
            else if ((other.svflags & QGameFlags.SVF_MONSTER) != 0)
            {
                if ((self.spawnflags & 1) == 0)
                {
                    return;
                }
            }
            else
            {
                return;
            }

            if (self.movedir != Vector3.Zero)
            {
                var forward = new Vector3();
                var t1 = new Vector3();
                var t2 = new Vector3();
                QShared.AngleVectors(other.s.angles, ref forward, ref t1, ref t2);

                if (Vector3.Dot(forward, self.movedir) < 0)
                {
                    return;
                }
            }

            self.activator = other;
            multi_trigger(self);
        }

        /*
        * QUAKED trigger_multiple (.5 .5 .5) ? MONSTER NOT_PLAYER TRIGGERED
        * Variable sized repeatable trigger.  Must be targeted at one or more
        * entities. If "delay" is set, the trigger waits some time after
        * activating before firing.
        *
        * "wait" : Seconds between triggerings. (.2 default)
        *
        * sounds
        * 1)	secret
        * 2)	beep beep
        * 3)	large switch
        * 4)
        *
        * set "message" to text string
        */
        private void trigger_enable(edict_t self, edict_t _other /* unused */,
                edict_t? _activator /* unused */)
        {
            if (self == null)
            {
                return;
            }


            self.solid = solid_t.SOLID_TRIGGER;
            self.use = Use_Multi;
            gi.linkentity(self);
        }

        private static void SP_trigger_multiple(QuakeGame g, edict_t ent)
        {
            if (ent == null)
            {
                return;
            }

            // if (ent.sounds == 1)
            // {
            //     ent.noise_index = gi.soundindex("misc/secret.wav");
            // }
            // else if (ent.sounds == 2)
            // {
            //     ent.noise_index = gi.soundindex("misc/talk.wav");
            // }
            // else if (ent.sounds == 3)
            // {
            //     ent.noise_index = gi.soundindex ("misc/trigger1.wav");
            // }

            if (ent.wait == 0)
            {
                ent.wait = 0.2f;
            }

            ent.touch = g.Touch_Multi;
            ent.movetype = movetype_t.MOVETYPE_NONE;
            ent.svflags |= QGameFlags.SVF_NOCLIENT;

            if ((ent.spawnflags & 4) != 0)
            {
                ent.solid = solid_t.SOLID_NOT;
                ent.use = g.trigger_enable;
            }
            else
            {
                ent.solid = solid_t.SOLID_TRIGGER;
                ent.use = g.Use_Multi;
            }

            if (ent.s.angles != Vector3.Zero)
            {
                g.G_SetMovedir(ref ent.s.angles, ref ent.movedir);
            }

            g.gi.setmodel(ent, ent.model!);
            g.gi.linkentity(ent);
        }

        /*
        * QUAKED trigger_once (.5 .5 .5) ? x x TRIGGERED
        * Triggers once, then removes itself.
        *
        * You must set the key "target" to the name of another
        * object in the level that has a matching "targetname".
        *
        * If TRIGGERED, this trigger must be triggered before it is live.
        *
        * sounds
        *  1) secret
        *  2) beep beep
        *  3) large switch
        *
        * "message" string to be displayed when triggered
        */

        private static void SP_trigger_once(QuakeGame g, edict_t ent)
        {
            if (ent == null)
            {
                return;
            }

            /* make old maps work because I
            messed up on flag assignments here
            triggered was on bit 1 when it
            should have been on bit 4 */
            if ((ent.spawnflags & 1) != 0)
            {
                QShared.VectorMA(ent.mins, 0.5f, ent.size, out var v);
                ent.spawnflags &= ~1;
                ent.spawnflags |= 4;
                g.gi.dprintf($"fixed TRIGGERED flag on {ent.classname} at {v}\n");
            }

            ent.wait = -1;
            SP_trigger_multiple(g, ent);
        }

    }
}