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
 * Monster utility functions.
 *
 * =======================================================================
 */
using System.Numerics;

namespace Quake2 {

    partial class QuakeGame
    {

        private void AttackFinished(edict_t self, float time)
        {
            if (self == null)
            {
                return;
            }

            self.monsterinfo.attack_finished = level.time + time;
        }


        private void M_CheckGround(edict_t ent)
        {
            if (ent == null)
            {
                return;
            }

            if ((ent.flags & (FL_SWIM | FL_FLY)) != 0)
            {
                return;
            }

            if (ent.velocity.Z > 100)
            {
                ent.groundentity = null;
                return;
            }

            /* if the hull point one-quarter unit down
            is solid the entity is on ground */
            var point = new Vector3(ent.s.origin.X, ent.s.origin.Y, ent.s.origin.Z - 0.25f);

            var trace = gi.trace(ent.s.origin, ent.mins, ent.maxs, point, ent, QShared.MASK_MONSTERSOLID);

            /* check steepness */
            if ((trace.plane.normal.Z < 0.7) && !trace.startsolid)
            {
                ent.groundentity = null;
                return;
            }

            if (!trace.startsolid && !trace.allsolid)
            {
                ent.s.origin = trace.endpos;
                ent.groundentity = (edict_t?)trace.ent;
                ent.groundentity_linkcount = trace.ent!.linkcount;
                ent.velocity.Z = 0;
            }
        }

        private void M_CatagorizePosition(edict_t ent)
        {
            // vec3_t point;
            // int cont;

            if (ent == null)
            {
                return;
            }

            /* get waterlevel */
            var point = new Vector3(
                (ent.absmax.X + ent.absmin.X)/2,
                (ent.absmax.Y + ent.absmin.Y)/2,
                ent.absmin.Z + 2);
            // var cont = gi.pointcontents(point);

            // if ((cont & MASK_WATER) == 0)
            // {
                ent.waterlevel = 0;
                ent.watertype = 0;
                return;
            // }

            // ent.watertype = cont;
            // ent.waterlevel = 1;
            // point.Z += 26;
            // cont = gi.pointcontents(point);

            // if ((cont & MASK_WATER) == 0)
            // {
            //     return;
            // }

            // ent.waterlevel = 2;
            // point[2] += 22;
            // cont = gi.pointcontents(point);

            // if ((cont & MASK_WATER) != 0)
            // {
            //     ent.waterlevel = 3;
            // }
        }

        private void M_droptofloor(edict_t ent)
        {
            // vec3_t end;
            // trace_t trace;

            if (ent == null)
            {
                return;
            }

            ent.s.origin.Z += 1;
            var end = ent.s.origin;
            end.Z -= 256;

            var trace = gi.trace(ent.s.origin, ent.mins, ent.maxs, end,
                    ent, QShared.MASK_MONSTERSOLID);

            if ((trace.fraction == 1) || trace.allsolid)
            {
                return;
            }

            ent.s.origin = trace.endpos;

            gi.linkentity(ent);
            M_CheckGround(ent);
            M_CatagorizePosition(ent);
        }

        private void M_MoveFrame(edict_t self)
        {
            if (self == null)
            {
                return;
            }

            var move = self.monsterinfo.currentmove!;
            self.nextthink = level.time + FRAMETIME;

            if ((self.monsterinfo.nextframe != 0) &&
                (self.monsterinfo.nextframe >= move.firstframe) &&
                (self.monsterinfo.nextframe <= move.lastframe))
            {
                if (self.s.frame != self.monsterinfo.nextframe)
                {
                    self.s.frame = self.monsterinfo.nextframe;
                    self.monsterinfo.aiflags &= ~AI_HOLD_FRAME;
                }

                self.monsterinfo.nextframe = 0;
            }
            else
            {
                /* prevent nextframe from leaking into a future move */
                self.monsterinfo.nextframe = 0;

                if (self.s.frame == move.lastframe)
                {
                    if (move.endfunc != null)
                    {
                        move.endfunc!(this, self);

                        /* regrab move, endfunc is very likely to change it */
                        move = self.monsterinfo.currentmove;

                        /* check for death */
                        if ((self.svflags & QGameFlags.SVF_DEADMONSTER) != 0)
                        {
                            return;
                        }
                    }
                }

                if ((self.s.frame < move!.firstframe) ||
                    (self.s.frame > move.lastframe))
                {
                    self.monsterinfo.aiflags &= ~AI_HOLD_FRAME;
                    self.s.frame = move.firstframe;
                }
                else
                {
                    if ((self.monsterinfo.aiflags & AI_HOLD_FRAME) == 0)
                    {
                        self.s.frame++;

                        if (self.s.frame > move.lastframe)
                        {
                            self.s.frame = move.firstframe;
                        }
                    }
                }
            }

            var index = self.s.frame - move.firstframe;

            if (move.frame[index].aifunc != null)
            {
                if ((self.monsterinfo.aiflags & AI_HOLD_FRAME) == 0)
                {
                    move.frame[index].aifunc!(this, self,
                            move.frame[index].dist * self.monsterinfo.scale);
                }
                else
                {
                    move.frame[index].aifunc!(this, self, 0);
                }
            }

            if (move.frame[index].thinkfunc != null)
            {
                move.frame[index].thinkfunc!(this, self);
            }
        }

        private void monster_think(edict_t self)
        {
            if (self == null)
            {
                return;
            }

            M_MoveFrame(self);

            if (self.linkcount != self.monsterinfo.linkcount)
            {
                self.monsterinfo.linkcount = self.linkcount;
                M_CheckGround(self);
            }

            M_CatagorizePosition(self);
            // M_WorldEffects(self);
            // M_SetEffects(self);
        }


        private void walkmonster_start_go(edict_t self)
        {
            if (self == null)
            {
                return;
            }

            if ((self.spawnflags & 2) == 0 && (level.time < 1))
            {
                M_droptofloor(self);

                if (self.groundentity != null)
                {
                    if (!M_walkmove(self, 0, 0))
                    {
                        gi.dprintf($"{self.classname} in solid at {self.s.origin}\n");
                    }
                }
            }

            if (self.yaw_speed == 0)
            {
                self.yaw_speed = 20;
            }

            if (self.viewheight == 0)
            {
                self.viewheight = 25;
            }

            // if ((self.spawnflags & 2) != 0)
            // {
                // monster_triggered_start(self);
            // }
            // else
            // {
                monster_start_go(self);
            // }
        }        

        /* ================================================================== */

        private bool monster_start(edict_t self)
        {
            if (self == null)
            {
                return false;
            }

            if (deathmatch!.Bool)
            {
                G_FreeEdict(self);
                return false;
            }

            if ((self.spawnflags & 4) != 0 && (self.monsterinfo.aiflags & AI_GOOD_GUY) == 0)
            {
                self.spawnflags &= ~4;
                self.spawnflags |= 1;
            }

            // if ((self->spawnflags & 2) && !self->targetname)
            // {
            //     if (g_fix_triggered->value)
            //     {
            //         self->spawnflags &= ~2;
            //     }

            //     gi.dprintf ("triggered %s at %s has no targetname\n", self->classname, vtos (self->s.origin));
            // }

            // if (!(self->monsterinfo.aiflags & AI_GOOD_GUY))
            // {
            //     level.total_monsters++;
            // }

            self.nextthink = level.time + FRAMETIME;
            self.svflags |= QGameFlags.SVF_MONSTER;
            self.s.renderfx |= QShared.RF_FRAMELERP;
            self.takedamage = (int)damage_t.DAMAGE_AIM;
            self.air_finished = level.time + 12;
            // self.use = monster_use;

            if(self.max_health == 0)
            {
                self.max_health = self.health;
            }

            self.clipmask = QShared.MASK_MONSTERSOLID;

            self.s.skinnum = 0;
            self.deadflag = DEAD_NO;
            self.svflags &= ~QGameFlags.SVF_DEADMONSTER;

            if (self.monsterinfo.checkattack == null)
            {
                self.monsterinfo.checkattack = M_CheckAttack;
            }

            self.s.old_origin = self.s.origin;

            // if (st.item)
            // {
            //     self->item = FindItemByClassname(st.item);

            //     if (!self->item)
            //     {
            //         gi.dprintf("%s at %s has bad item: %s\n", self->classname,
            //                 vtos(self->s.origin), st.item);
            //     }
            // }

            /* randomize what frame they start on */
            if (self.monsterinfo.currentmove != null)
            {
                self.s.frame = self.monsterinfo.currentmove.firstframe +
                    (QShared.randk() % (self.monsterinfo.currentmove.lastframe -
                            self.monsterinfo.currentmove.firstframe + 1));
            }

            return true;
        }

        private void monster_start_go(edict_t self)
        {
            // vec3_t v;

            if (self == null)
            {
                return;
            }

            if (self.health <= 0)
            {
                return;
            }

            /* check for target to combat_point and change to combattarget */
            if (self.target != null)
            {
                // qboolean notcombat;
                // qboolean fixup;
                // edict_t *target;

                // target = NULL;
                // notcombat = false;
                // fixup = false;

                // while ((target = G_Find(target, FOFS(targetname), self->target)) != NULL)
                // {
                //     if (strcmp(target->classname, "point_combat") == 0)
                //     {
                //         self->combattarget = self->target;
                //         fixup = true;
                //     }
                //     else
                //     {
                //         notcombat = true;
                //     }
                // }

                // if (notcombat && self->combattarget)
                // {
                //     gi.dprintf("%s at %s has target with mixed types\n",
                //             self->classname, vtos(self->s.origin));
                // }

                // if (fixup)
                // {
                //     self->target = NULL;
                // }
            }

            /* validate combattarget */
            if (self.combattarget != null)
            {
            //     edict_t *target;

            //     target = NULL;

            //     while ((target = G_Find(target, FOFS(targetname),
            //                     self->combattarget)) != NULL)
            //     {
            //         if (strcmp(target->classname, "point_combat") != 0)
            //         {
            //             gi.dprintf( "%s at (%i %i %i) has a bad combattarget %s : %s at (%i %i %i)\n",
            //                     self->classname, (int)self->s.origin[0], (int)self->s.origin[1],
            //                     (int)self->s.origin[2], self->combattarget, target->classname,
            //                     (int)target->s.origin[0], (int)target->s.origin[1],
            //                     (int)target->s.origin[2]);
            //         }
            //     }
            }

            if (self.target != null)
            {
                self.goalentity = self.movetarget = G_PickTarget(self.target);

                if (self.movetarget == null)
                {
                    gi.dprintf($"{self.classname} can't find target {self.target} at {self.s.origin}\n");
                    self.target = null;
                    self.monsterinfo.pausetime = 100000000;
                    self.monsterinfo.stand!(self);
                }
                else if (self.movetarget.classname == "path_corner")
                {
                    self.ideal_yaw = self.s.angles[QShared.YAW] = vectoyaw(self.goalentity!.s.origin - self.s.origin);
                    self.monsterinfo.walk!(self);
                    self.target = null;
                }
                else
                {
                    self.goalentity = self.movetarget = null;
                    self.monsterinfo.pausetime = 100000000;
                    self.monsterinfo.stand!(self);
                }
            }
            else
            {
                self.monsterinfo.pausetime = 100000000;
                self.monsterinfo.stand!(self);
            }

            self.think = monster_think;
            self.nextthink = level.time + FRAMETIME;
        }

        private void walkmonster_start(edict_t self)
        {
            if (self == null)
            {
                return;
            }

            self.think = walkmonster_start_go;
            monster_start(self);
        }

    }
}