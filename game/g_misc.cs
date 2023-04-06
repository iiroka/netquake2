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
 * Miscellaneos entities, functs and functions.
 *
 * =======================================================================
 */
using System.Numerics;

namespace Quake2 {

    partial class QuakeGame
    {
        private const int START_OFF = 1;

        /* ===================================================== */

        /*
        * QUAKED path_corner (.5 .3 0) (-8 -8 -8) (8 8 8) TELEPORT
        * Target: next path corner
        * Pathtarget: gets used when an entity that has
        *             this path_corner targeted touches it
        */
        private void path_corner_touch(edict_t self, edict_t other, QShared.cplane_t? _plane,
                in QShared.csurface_t? _surf)
        {
            // vec3_t v;
            // edict_t *next;
            if (self == null || other == null)
            {
                return;
            }

            if (other.movetarget != self)
            {
                return;
            }

            if (other.enemy != null)
            {
                return;
            }

            if (self.pathtarget != null)
            {
                var savetarget = self.target;
                self.target = self.pathtarget;
                G_UseTargets(self, other);
                self.target = savetarget;
            }

            edict_t? next = null;
            if (self.target != null)
            {
                next = G_PickTarget(self.target);
            }

            if ((next != null) && (next.spawnflags & 1) != 0)
            {
                var v = next.s.origin;
                v[2] += next.mins[2];
                v[2] -= other.mins[2];
                other.s.origin = v;
                next = G_PickTarget(next.target!);
                other.s.ev = (int)QShared.entity_event_t.EV_OTHER_TELEPORT;
            }

            other.goalentity = other.movetarget = next;

            if (self.wait != 0)
            {
                other.monsterinfo.pausetime = level.time + self.wait;
                other.monsterinfo.stand!(other);
                return;
            }

            if (other.movetarget == null)
            {
                other.monsterinfo.pausetime = level.time + 100000000;
                other.monsterinfo.stand!(other);
            }
            else
            {
                other.ideal_yaw = vectoyaw(other.groundentity!.s.origin - other.s.origin);
            }
        }


        private static void SP_path_corner(QuakeGame g, edict_t? self)
        {
            if (self == null)
            {
                return;
            }

            if (self.targetname == null)
            {
                g.gi.dprintf($"path_corner with no targetname at {self.s.origin}\n");
                g.G_FreeEdict(self);
                return;
            }

            self.solid = solid_t.SOLID_TRIGGER;
            self.touch = g.path_corner_touch;
            self.mins = new Vector3(-8, -8, -8);
            self.maxs = new Vector3(8, 8, 8);
            self.svflags |= QGameFlags.SVF_NOCLIENT;
            g.gi.linkentity(self);
        }

        private static void SP_light(QuakeGame g, edict_t? self)
        {
            if (self == null)
            {
                return;
            }

            /* no targeted lights in deathmatch, because they cause global messages */
            if (String.IsNullOrEmpty(self.targetname) || g.deathmatch!.Bool)
            {
                g.G_FreeEdict(self);
                return;
            }

            if (self.style >= 32)
            {
                // self.use = light_use;

                if ((self.spawnflags & START_OFF) != 0)
                {
                    g.gi.configstring(QShared.CS_LIGHTS + self.style, "a");
                }
                else
                {
                    g.gi.configstring(QShared.CS_LIGHTS + self.style, "m");
                }
            }
        }

    }
}