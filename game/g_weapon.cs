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
 * Weapon support functions.
 *
 * =======================================================================
 */
using System.Numerics;

namespace Quake2 {

    partial class QuakeGame
    {
        /*
        * Fires a single blaster bolt.
        * Used by the blaster and hyper blaster.
        */
        private void blaster_touch(edict_t self, edict_t other, QShared.cplane_t? plane, in QShared.csurface_t? surf)
        {
            // int mod;

            if (self == null)
            {
                return;
            }
            if (other == null) /* plane and surf can be NULL */
            {
                G_FreeEdict(self);
                return;
            }
            Console.WriteLine($"blaster_touch {other.classname}");

            if (other == self.owner)
            {
                return;
            }

            if (surf.HasValue && (surf.Value.flags & QCommon.SURF_SKY) != 0)
            {
                G_FreeEdict(self);
                return;
            }

            // if (self->owner && self->owner->client)
            // {
            //     PlayerNoise(self->owner, self->s.origin, PNOISE_IMPACT);
            // }

            if (other.takedamage != 0)
            {
            //     if (self->spawnflags & 1)
            //     {
            //         mod = MOD_HYPERBLASTER;
            //     }
            //     else
            //     {
            //         mod = MOD_BLASTER;
            //     }

            //     if (plane)
            //     {
            //         T_Damage(other, self, self->owner, self->velocity, self->s.origin,
            //                 plane->normal, self->dmg, 1, DAMAGE_ENERGY, mod);
            //     }
            //     else
            //     {
            //         T_Damage(other, self, self->owner, self->velocity, self->s.origin,
            //                 vec3_origin, self->dmg, 1, DAMAGE_ENERGY, mod);
            //     }
            }
            else
            {
            //     gi.WriteByte(svc_temp_entity);
            //     gi.WriteByte(TE_BLASTER);
            //     gi.WritePosition(self->s.origin);

            //     if (!plane)
            //     {
            //         gi.WriteDir(vec3_origin);
            //     }
            //     else
            //     {
            //         gi.WriteDir(plane->normal);
            //     }

            //     gi.multicast(self->s.origin, MULTICAST_PVS);
            }

            G_FreeEdict(self);
        }

        private void fire_blaster(edict_t self, in Vector3 start, in Vector3 idir, int damage,
                int speed, int effect, bool hyper)
        {
            // edict_t *bolt;
            // trace_t tr;

            if (self == null)
            {
                return;
            }

            var dir = Vector3.Normalize(idir);

            var bolt = G_Spawn();
            bolt.svflags = QGameFlags.SVF_DEADMONSTER;

            /* yes, I know it looks weird that projectiles are deadmonsters
            what this means is that when prediction is used against the object
            (blaster/hyperblaster shots), the player won't be solid clipped against
            the object.  Right now trying to run into a firing hyperblaster
            is very jerky since you are predicted 'against' the shots. */
            bolt.s.origin = start;
            bolt.s.old_origin = start;
            bolt.s.angles = dir;
            bolt.velocity = speed * dir;
            bolt.movetype = movetype_t.MOVETYPE_FLYMISSILE;
            bolt.clipmask = QShared.MASK_SHOT;
            bolt.solid = solid_t.SOLID_BBOX;
            bolt.s.effects |= (uint)effect;
            bolt.s.renderfx |= QShared.RF_NOSHADOW;
            bolt.mins = Vector3.Zero;
            bolt.maxs = Vector3.Zero;
            bolt.s.modelindex = gi.modelindex("models/objects/laser/tris.md2");
            // bolt.s.sound = gi.soundindex("misc/lasfly.wav");
            bolt.owner = self;
            bolt.touch = blaster_touch;
            bolt.nextthink = level.time + 2;
            bolt.think = G_FreeEdict;
            bolt.dmg = damage;
            bolt.classname = "bolt";

            if (hyper)
            {
                bolt.spawnflags = 1;
            }

            gi.linkentity(bolt);

            if (self.client != null)
            {
                // check_dodge(self, bolt->s.origin, dir, speed);
            }

            var tr = gi.trace(self.s.origin, null, null, bolt.s.origin, bolt, QShared.MASK_SHOT);

            if (tr.fraction < 1.0)
            {
                QShared.VectorMA(bolt.s.origin, -10, dir, out bolt.s.origin);
                bolt.touch(bolt, (edict_t)tr.ent!, null, null);
            }
        }

    }
}