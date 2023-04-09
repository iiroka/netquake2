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

        private void BecomeExplosion1(edict_t self)
        {
            if (self == null)
            {
                return;
            }

            gi.WriteByte(svc_temp_entity);
            gi.WriteByte((int)QShared.temp_event_t.TE_EXPLOSION1);
            gi.WritePosition(self.s.origin);
            gi.multicast(self.s.origin, QShared.multicast_t.MULTICAST_PVS);

            G_FreeEdict(self);
        }

        private void BecomeExplosion2(edict_t self)
        {
            if (self == null)
            {
                return;
            }

            gi.WriteByte(svc_temp_entity);
            gi.WriteByte((int)QShared.temp_event_t.TE_EXPLOSION2);
            gi.WritePosition(self.s.origin);
            gi.multicast(self.s.origin, QShared.multicast_t.MULTICAST_PVS);

            G_FreeEdict(self);
        }

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

        /* ===================================================== */

        /*
        * QUAKED misc_explobox (0 .5 .8) (-16 -16 0) (16 16 40)
        * Large exploding box.  You can override its mass (100),
        * health (80), and dmg (150).
        */

        private void barrel_touch(edict_t self, edict_t other, QShared.cplane_t? _plane /* unused */, in QShared.csurface_t? _surf /*unused */)
        {
            if (self == null || other == null)
            {
                return;
            }

            if ((other.groundentity == null) || (other.groundentity == self))
            {
                return;
            }

            var ratio = (float)other.mass / (float)self.mass;
            var v = self.s.origin - other.s.origin;
            M_walkmove(self, vectoyaw(v), 20 * ratio * FRAMETIME);
        }

        private void barrel_explode(edict_t self)
        {
            if (self == null)
            {
                return;
            }

            T_RadiusDamage(self, self.activator, self.dmg, null,
                    self.dmg + 40, MOD_BARREL);
            var save = self.s.origin;
            QShared.VectorMA(self.absmin, 0.5f, self.size, out self.s.origin);

            /* a few big chunks */
            var spd = 1.5f * (float)self.dmg / 200.0f;
            var org = new Vector3();
            org[0] = self.s.origin[0] + QShared.crandk() * self.size[0];
            org[1] = self.s.origin[1] + QShared.crandk() * self.size[1];
            org[2] = self.s.origin[2] + QShared.crandk() * self.size[2];
            // ThrowDebris(self, "models/objects/debris1/tris.md2", spd, org);
            org[0] = self.s.origin[0] + QShared.crandk() * self.size[0];
            org[1] = self.s.origin[1] + QShared.crandk() * self.size[1];
            org[2] = self.s.origin[2] + QShared.crandk() * self.size[2];
            // ThrowDebris(self, "models/objects/debris1/tris.md2", spd, org);

            /* bottom corners */
            spd = 1.75f * (float)self.dmg / 200.0f;
            org = self.absmin;
            // ThrowDebris(self, "models/objects/debris3/tris.md2", spd, org);
            org = self.absmin;
            // VectorCopy(self->absmin, org);
            org[0] += self.size[0];
            // ThrowDebris(self, "models/objects/debris3/tris.md2", spd, org);
            org = self.absmin;
            org[1] += self.size[1];
            // ThrowDebris(self, "models/objects/debris3/tris.md2", spd, org);
            org = self.absmin;
            org[0] += self.size[0];
            org[1] += self.size[1];
            // ThrowDebris(self, "models/objects/debris3/tris.md2", spd, org);

            /* a bunch of little chunks */
            spd = 2 * self.dmg / 200;
            org[0] = self.s.origin[0] + QShared.crandk() * self.size[0];
            org[1] = self.s.origin[1] + QShared.crandk() * self.size[1];
            org[2] = self.s.origin[2] + QShared.crandk() * self.size[2];
            // ThrowDebris(self, "models/objects/debris2/tris.md2", spd, org);
            org[0] = self.s.origin[0] + QShared.crandk() * self.size[0];
            org[1] = self.s.origin[1] + QShared.crandk() * self.size[1];
            org[2] = self.s.origin[2] + QShared.crandk() * self.size[2];
            // ThrowDebris(self, "models/objects/debris2/tris.md2", spd, org);
            org[0] = self.s.origin[0] + QShared.crandk() * self.size[0];
            org[1] = self.s.origin[1] + QShared.crandk() * self.size[1];
            org[2] = self.s.origin[2] + QShared.crandk() * self.size[2];
            // ThrowDebris(self, "models/objects/debris2/tris.md2", spd, org);
            org[0] = self.s.origin[0] + QShared.crandk() * self.size[0];
            org[1] = self.s.origin[1] + QShared.crandk() * self.size[1];
            org[2] = self.s.origin[2] + QShared.crandk() * self.size[2];
            // ThrowDebris(self, "models/objects/debris2/tris.md2", spd, org);
            org[0] = self.s.origin[0] + QShared.crandk() * self.size[0];
            org[1] = self.s.origin[1] + QShared.crandk() * self.size[1];
            org[2] = self.s.origin[2] + QShared.crandk() * self.size[2];
            // ThrowDebris(self, "models/objects/debris2/tris.md2", spd, org);
            org[0] = self.s.origin[0] + QShared.crandk() * self.size[0];
            org[1] = self.s.origin[1] + QShared.crandk() * self.size[1];
            org[2] = self.s.origin[2] + QShared.crandk() * self.size[2];
            // ThrowDebris(self, "models/objects/debris2/tris.md2", spd, org);
            org[0] = self.s.origin[0] + QShared.crandk() * self.size[0];
            org[1] = self.s.origin[1] + QShared.crandk() * self.size[1];
            org[2] = self.s.origin[2] + QShared.crandk() * self.size[2];
            // ThrowDebris(self, "models/objects/debris2/tris.md2", spd, org);
            org[0] = self.s.origin[0] + QShared.crandk() * self.size[0];
            org[1] = self.s.origin[1] + QShared.crandk() * self.size[1];
            org[2] = self.s.origin[2] + QShared.crandk() * self.size[2];
            // ThrowDebris(self, "models/objects/debris2/tris.md2", spd, org);

            self.s.origin = save;

            if (self.groundentity != null)
            {
                BecomeExplosion2(self);
            }
            else
            {
                BecomeExplosion1(self);
            }
        }

        private void barrel_delay(edict_t self, edict_t _inflictor /* unused */, edict_t attacker,
                int _damage /* unused */, in Vector3 _point /* unused */)
        {
            if (self == null || attacker == null)
            {
                return;
            }

            self.takedamage = (int)damage_t.DAMAGE_NO;
            self.nextthink = level.time + 2 * FRAMETIME;
            self.think = barrel_explode;
            self.activator = attacker;
        }

        private static void SP_misc_explobox(QuakeGame g, edict_t self)
        {
            if (self == null)
            {
                return;
            }

            if (g.deathmatch!.Bool)
            {
                /* auto-remove for deathmatch */
                g.G_FreeEdict(self);
                return;
            }

            g.gi.modelindex("models/objects/debris1/tris.md2");
            g.gi.modelindex("models/objects/debris2/tris.md2");
            g.gi.modelindex("models/objects/debris3/tris.md2");

            self.solid = solid_t.SOLID_BBOX;
            self.movetype = movetype_t.MOVETYPE_STEP;

            self.model = "models/objects/barrels/tris.md2";
            self.s.modelindex = g.gi.modelindex(self.model!);
            self.mins = new Vector3(-16, -16, 0);
            self.maxs = new Vector3(16, 16, 40);

            if (self.mass == 0)
            {
                self.mass = 400;
            }

            if (self.health == 0)
            {
                self.health = 10;
            }

            if (self.dmg == 0)
            {
                self.dmg = 150;
            }

            self.die = g.barrel_delay;
            self.takedamage = (int)damage_t.DAMAGE_YES;
            self.monsterinfo.aiflags = AI_NOSTEP;

            self.touch = g.barrel_touch;

            self.think = g.M_droptofloor;
            self.nextthink = g.level.time + 2 * FRAMETIME;

            g.gi.linkentity(self);
        }

    }
}