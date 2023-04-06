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
 * Targets.
 *
 * =======================================================================
 */

using System.Numerics; 

namespace Quake2 {

    partial class QuakeGame
    {

        /* ========================================================== */

        /*
        * QUAKED target_speaker (1 0 0) (-8 -8 -8) (8 8 8) looped-on looped-off reliable
        *
        * "noise" wav file to play
        *
        * "attenuation"
        *   -1 = none, send to whole level
        *    1 = normal fighting sounds
        *    2 = idle sound level
        *    3 = ambient sound level
        *
        * "volume"	0.0 to 1.0
        *
        * Normal sounds play each time the target is used.
        * The reliable flag can be set for crucial voiceovers.
        *
        * Looped sounds are always atten 3 / vol 1, and the use function toggles it on/off.
        * Multiple identical looping sounds will just increase volume without any speed cost.
        */
        private void Use_Target_Speaker(edict_t ent, edict_t _other, edict_t? _activator)
        {
            // int chan;

            if (ent == null)
            {
                return;
            }

            if ((ent.spawnflags & 3) != 0)
            {
                /* looping sound toggles */
                if (ent.s.sound != 0)
                {
                    ent.s.sound = 0; /* turn it off */
                }
                else
                {
                    ent.s.sound = ent.noise_index; /* start it */
                }
            }
            else
            {
                /* normal sound */
                // if ((ent.spawnflags & 4) != 0)
                // {
                //     chan = CHAN_VOICE | CHAN_RELIABLE;
                // }
                // else
                // {
                //     chan = CHAN_VOICE;
                // }

                // /* use a positioned_sound, because this entity won't
                // normally be sent to any clients because it is invisible */
                // gi.positioned_sound(ent->s.origin, ent, chan, ent->noise_index,
                //         ent->volume, ent->attenuation, 0);
            }
        }

        private static void SP_target_speaker(QuakeGame g, edict_t ent)
        {
            // char buffer[MAX_QPATH];

            if (ent == null)
            {
                return;
            }

            // if (!st.noise)
            // {
            //     g.gi.dprintf("target_speaker with no noise set at %s\n",
            //             vtos(ent->s.origin));
            //     return;
            // }

            // if (st.noise == ".wav")
            // {
            //     Com_sprintf(buffer, sizeof(buffer), "%s.wav", st.noise);
            // }
            // else
            // {
            //     Q_strlcpy(buffer, st.noise, sizeof(buffer));
            // }

            // ent->noise_index = gi.soundindex(buffer);

            // if (!ent->volume)
            // {
            //     ent->volume = 1.0;
            // }

            // if (!ent->attenuation)
            // {
            //     ent->attenuation = 1.0;
            // }
            // else if (ent->attenuation == -1) /* use -1 so 0 defaults to 1 */
            // {
            //     ent->attenuation = 0;
            // }

            // /* check for prestarted looping sound */
            // if (ent->spawnflags & 1)
            // {
            //     ent->s.sound = ent->noise_index;
            // }

            ent.use = g.Use_Target_Speaker;

            /* must link the entity so we get areas and clusters so
            the server can determine who to send updates to */
            g.gi.linkentity(ent);
        }

        /* ========================================================== */

        /*
        * QUAKED target_explosion (1 0 0) (-8 -8 -8) (8 8 8)
        * Spawns an explosion temporary entity when used.
        *
        * "delay"		wait this long before going off
        * "dmg"		how much radius damage should be done, defaults to 0
        */
        private void target_explosion_explode(edict_t self)
        {
            // float save;

            if (self == null)
            {
                return;
            }

            gi.WriteByte(svc_temp_entity);
            gi.WriteByte((int)QShared.temp_event_t.TE_EXPLOSION1);
            gi.WritePosition(self.s.origin);
            gi.multicast(self.s.origin, QShared.multicast_t.MULTICAST_PHS);

            // T_RadiusDamage(self, self->activator, self->dmg, NULL,
            //         self->dmg + 40, MOD_EXPLOSIVE);

            var save = self.delay;
            self.delay = 0;
            G_UseTargets(self, self.activator);
            self.delay = save;
        }

        private void use_target_explosion(edict_t self, edict_t _other /* unused */, edict_t? activator)
        {
            if (self == null)
            {
                return;
            }
            self.activator = activator;

            if (activator == null)
            {
                return;
            }

            if (self.delay == 0)
            {
                target_explosion_explode(self);
                return;
            }

            self.think = target_explosion_explode;
            self.nextthink = level.time + self.delay;
        }

        private static void SP_target_explosion(QuakeGame g, edict_t ent) {
            if (ent == null || g == null)
            {
                return;
            }

            ent.use = g.use_target_explosion;
            ent.svflags = QGameFlags.SVF_NOCLIENT;
        }

    }
}