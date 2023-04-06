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
 * The basic AI functions like enemy detection, attacking and so on.
 *
 * =======================================================================
 */
using System.Numerics;

namespace Quake2 {

    partial class QuakeGame
    {

        private bool _ai_enemy_vis;
        private bool _ai_enemy_infront;
        private int _ai_enemy_range;
        private float _ai_enemy_yaw;


        /*
        * Move the specified distance at current facing.
        */
        private static void ai_move(QuakeGame g, edict_t self, float dist)
        {
            if (self == null)
            {
                return;
            }

            g.M_walkmove(self, self.s.angles[QShared.YAW], dist);
        }
        /*
        *
        * Used for standing around and looking
        * for players Distance is for slight
        * position adjustments needed by the
        * animations
        */
        private static void ai_stand(QuakeGame g, edict_t self, float dist)
        {
            // vec3_t v;

            if (g == null || self == null)
            {
                return;
            }

            if (dist != 0)
            {
                g.M_walkmove(self, self.s.angles.Yaw(), dist);
            }

            if ((self.monsterinfo.aiflags & AI_STAND_GROUND) != 0)
            {
                if (self.enemy != null)
                {
                    self.ideal_yaw = g.vectoyaw(self.enemy.s.origin - self.s.origin);

                    if ((self.s.angles[QShared.YAW] != self.ideal_yaw) &&
                        (self.monsterinfo.aiflags & AI_TEMP_STAND_GROUND) != 0)
                    {
                        self.monsterinfo.aiflags &=
                            ~(AI_STAND_GROUND | AI_TEMP_STAND_GROUND);
                        self.monsterinfo.run!(self);
                    }

                    g.M_ChangeYaw(self);
                    g.ai_checkattack(self);
                }
                else
                {
                    g.FindTarget(self);
                }

                return;
            }

            if (g.FindTarget(self))
            {
                return;
            }

            if (g.level.time > self.monsterinfo.pausetime)
            {
                self.monsterinfo.walk!(self);
                return;
            }

            if ((self.spawnflags & 1) == 0 && (self.monsterinfo.idle != null) &&
                (g.level.time > self.monsterinfo.idle_time))
            {
                if (self.monsterinfo.idle_time != 0)
                {
                    self.monsterinfo.idle(self);
                    self.monsterinfo.idle_time = g.level.time + 15 + QShared.frandk() * 15;
                }
                else
                {
                    self.monsterinfo.idle_time = g.level.time + QShared.frandk() * 15;
                }
            }
        }

        /*
        * The monster is walking it's beat
        */
        private static void ai_walk(QuakeGame g, edict_t self, float dist)
        {
            if (g == null || self == null)
            {
                return;
            }

            g.M_MoveToGoal(self, dist);

            /* check for noticing a player */
            if (g.FindTarget(self))
            {
                return;
            }

            // if ((self->monsterinfo.search) && (level.time > self->monsterinfo.idle_time))
            // {
            //     if (self->monsterinfo.idle_time)
            //     {
            //         self->monsterinfo.search(self);
            //         self->monsterinfo.idle_time = level.time + 15 + random() * 15;
            //     }
            //     else
            //     {
            //         self->monsterinfo.idle_time = level.time + random() * 15;
            //     }
            // }
        }

        /*
        * Turns towards target and advances
        * Use this call with a distance of 0
        * to replace ai_face
        */
        private static void ai_charge(QuakeGame g, edict_t self, float dist)
        {
            if (self == null || g == null)
            {
                return;
            }

            var v = new Vector3();
            if(self.enemy != null)
            {
                v = self.enemy.s.origin - self.s.origin;
            }

            self.ideal_yaw = g.vectoyaw(v);
            g.M_ChangeYaw(self);

            if (dist != 0)
            {
                g.M_walkmove(self, self.s.angles[QShared.YAW], dist);
            }
        }

        /* ============================================================================ */

        /*
        * .enemy
        * Will be world if not currently angry at anyone.
        *
        * .movetarget
        * The next path spot to walk toward.  If .enemy, ignore .movetarget.
        * When an enemy is killed, the monster will try to return to it's path.
        *
        * .hunt_time
        * Set to time + something when the player is in sight, but movement straight for
        * him is blocked.  This causes the monster to use wall following code for
        * movement direction instead of sighting on the player.
        *
        * .ideal_yaw
        * A yaw angle of the intended direction, which will be turned towards at up
        * to 45 deg / state.  If the enemy is in view and hunt_time is not active,
        * this will be the exact line towards the enemy.
        *
        * .pausetime
        * A monster will leave it's stand state and head towards it's .movetarget when
        * time > .pausetime.
        */

        /* ============================================================================ */

        /*
        * returns the range categorization of an entity relative to self
        * 0	melee range, will become hostile even if back is turned
        * 1	visibility and infront, or visibility and show hostile
        * 2	infront and show hostile
        * 3	only triggered by damage
        */
        private int range(edict_t self, edict_t other)
        {
            if (self == null || other == null)
            {
                return 0;
            }

            var v = self.s.origin - other.s.origin;
            var len = v.Length();

            if (len < MELEE_DISTANCE)
            {
                return RANGE_MELEE;
            }

            if (len < 500)
            {
                return RANGE_NEAR;
            }

            if (len < 1000)
            {
                return RANGE_MID;
            }

            return RANGE_FAR;
        }

        /*
        * returns 1 if the entity is visible
        * to self, even if not infront
        */
        private bool visible(edict_t self, edict_t other)
        {

            if (self == null || other == null)
            {
                return false;
            }

            var spot1 = self.s.origin;
            spot1[2] += self.viewheight;
            var spot2 = self.s.origin;
            spot2[2] += other.viewheight;
            var trace = gi.trace(spot1, Vector3.Zero, Vector3.Zero, spot2, self, QShared.MASK_OPAQUE);

            if (trace.fraction == 1.0)
            {
                return true;
            }

            return false;
        }

        /*
        * returns 1 if the entity is in
        * front (in sight) of self
        */
        private bool infront(edict_t self, edict_t other)
        {
            if (self == null || other == null)
            {
                return false;
            }

            var forward = new Vector3();
            var d1 = new Vector3();
            var d2 = new Vector3();
            QShared.AngleVectors(self.s.angles, ref forward, ref d1, ref d2);

            var vec = other.s.origin - self.s.origin;
            vec = Vector3.Normalize(vec);
            var dot = Vector3.Dot(vec, forward);

            if (dot > 0.3)
            {
                return true;
            }

            return false;
        }

        /* ============================================================================ */

        private void HuntTarget(edict_t self)
        {
            if (self == null)
            {
                return;
            }

            self.goalentity = self.enemy;

            if ((self.monsterinfo.aiflags & AI_STAND_GROUND) != 0)
            {
                self.monsterinfo.stand!(self);
            }
            else
            {
                self.monsterinfo.run!(self);
            }

            var vec = new Vector3();
            if(visible(self, self.enemy!))
            {
                vec = self.enemy!.s.origin - self.s.origin;
            }

            self.ideal_yaw = vectoyaw(vec);

            /* wait a while before first attack */
            if ((self.monsterinfo.aiflags & AI_STAND_GROUND) == 0)
            {
                AttackFinished(self, 1);
            }
        }

        private void FoundTarget(edict_t self)
        {
            if (self == null || self.enemy == null || !self.enemy.inuse)
            {
                return;
            }

            /* let other monsters see this monster for a while */
            if (self.enemy.client != null)
            {
                level.sight_entity = self;
                level.sight_entity_framenum = level.framenum;
                level.sight_entity.light_level = 128;
            }

            self.show_hostile = level.time + 1; /* wake up other monsters */

            self.monsterinfo.last_sighting = self.enemy.s.origin;
            self.monsterinfo.trail_time = level.time;

            if (self.combattarget == null)
            {
                HuntTarget(self);
                return;
            }

            self.goalentity = self.movetarget = G_PickTarget(self.combattarget);

            if (self.movetarget == null)
            {
                self.goalentity = self.movetarget = self.enemy;
                HuntTarget(self);
                gi.dprintf($"{self.classname} at {self.s.origin}, combattarget {self.combattarget} not found\n");
                return;
            }

            /* clear out our combattarget, these are a one shot deal */
            self.combattarget = null;
            self.monsterinfo.aiflags |= AI_COMBAT_POINT;

            /* clear the targetname, that point is ours! */
            self.movetarget.targetname = null;
            self.monsterinfo.pausetime = 0;

            /* run for it */
            self.monsterinfo.run!(self);
        }

        /*
        * Self is currently not attacking anything,
        * so try to find a target
        *
        * Returns TRUE if an enemy was sighted
        *
        * When a player fires a missile, the point
        * of impact becomes a fakeplayer so that
        * monsters that see the impact will respond
        * as if they had seen the player.
        *
        * To avoid spending too much time, only
        * a single client (or fakeclient) is
        * checked each frame. This means multi
        * player games will have slightly
        * slower noticing monsters.
        */
        private bool FindTarget(edict_t self)
        {
            // edict_t *client;
            // qboolean heardit;
            // int r;

            if (self == null)
            {
                return false;
            }

            if ((self.monsterinfo.aiflags & AI_GOOD_GUY) != 0)
            {
                return false;
            }

            /* if we're going to a combat point, just proceed */
            if ((self.monsterinfo.aiflags & AI_COMBAT_POINT) != 0)
            {
                return false;
            }

            /* if the first spawnflag bit is set, the monster
            will only wake up on really seeing the player,
            not another monster getting angry or hearing
            something */

            var heardit = false;
            edict_t? client = null;

            if ((level.sight_entity_framenum >= (level.framenum - 1)) &&
                (self.spawnflags & 1) == 0)
            {
                client = level.sight_entity;

                if (client!.enemy == self.enemy)
                {
                    return false;
                }
            }
            else if (level.sound_entity_framenum >= (level.framenum - 1))
            {
                client = level.sound_entity;
                heardit = true;
            }
            else if ((self.enemy == null) &&
                    (level.sound2_entity_framenum >= (level.framenum - 1)) &&
                    (self.spawnflags & 1) == 0)
            {
                client = level.sound2_entity;
                heardit = true;
            }
            else
            {
                client = level.sight_client;

                if (client == null)
                {
                    return false; /* no clients to get mad at */
                }
            }

            /* if the entity went away, forget it */
            if (!(client?.inuse ?? false))
            {
                return false;
            }

            if (client == self.enemy)
            {
                return true;
            }

            if (client.client == null)
            {
                if ((client.flags & FL_NOTARGET) != 0)
                {
                    return false;
                }
            }
            else if ((client.svflags & QGameFlags.SVF_MONSTER) != 0)
            {
                if (client.enemy == null)
                {
                    return false;
                }

                if ((client.enemy.flags & FL_NOTARGET) != 0)
                {
                    return false;
                }
            }
            else if (heardit)
            {
                if ((((edict_t)client.owner!).flags & FL_NOTARGET) != 0)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            if (!heardit)
            {
                var r = range(self, client);

                if (r == RANGE_FAR)
                {
                    return false;
                }

                /* is client in an spot too dark to be seen? */
                if (client.light_level <= 5)
                {
                    return false;
                }

                if (!visible(self, client))
                {
                    return false;
                }

                if (r == RANGE_NEAR)
                {
                    if ((client.show_hostile < level.time) && !infront(self, client))
                    {
                        return false;
                    }
                }
                else if (r == RANGE_MID)
                {
                    if (!infront(self, client))
                    {
                        return false;
                    }
                }

                self.enemy = client;

                if (self.enemy.classname != "player_noise")
                {
                    self.monsterinfo.aiflags &= ~AI_SOUND_TARGET;

                    if (self.enemy.client == null)
                    {
                        self.enemy = self.enemy.enemy;

                        if (self.enemy!.client == null)
                        {
                            self.enemy = null;
                            return false;
                        }
                    }
                }
            }
            else /* heardit */
            {
            //     vec3_t temp;

                if ((self.spawnflags & 1) != 0)
                {
                    if (!visible(self, client))
                    {
                        return false;
                    }
                }
            //     else
            //     {
            //         if (!gi.inPHS(self->s.origin, client->s.origin))
            //         {
            //             return false;
            //         }
            //     }

                var temp = client.s.origin - self.s.origin;

                if (temp.Length() > 1000) /* too far to hear */
                {
                    return false;
                }

                /* check area portals - if they are different
                and not connected then we can't hear it */
                if (client.areanum != self.areanum)
                {
            //         if (!gi.AreasConnected(self->areanum, client->areanum))
            //         {
            //             return false;
            //         }
                }

                self.ideal_yaw = vectoyaw(temp);
                M_ChangeYaw(self);

                /* hunt the sound for a bit; hopefully find the real player */
                self.monsterinfo.aiflags |= AI_SOUND_TARGET;
                self.enemy = client;
            }

            FoundTarget(self);

            // if (!(self->monsterinfo.aiflags & AI_SOUND_TARGET) &&
            //     (self->monsterinfo.sight))
            // {
            //     self->monsterinfo.sight(self, self->enemy);
            // }

            return true;
        }

        /* ============================================================================= */

        /* ============================================================================= */

        private bool FacingIdeal(edict_t self)
        {
            if (self == null)
            {
                return false;
            }

            var delta = QShared.anglemod(self.s.angles[QShared.YAW] - self.ideal_yaw);

            if ((delta > 45) && (delta < 315))
            {
                return false;
            }

            return true;
        }

        /* ============================================================================= */

        private bool M_CheckAttack(edict_t self)
        {
            // vec3_t spot1, spot2;
            // float chance;
            // trace_t tr;

            if (self == null || self.enemy == null || !self.enemy.inuse)
            {
                return false;
            }

            if (self.enemy.health > 0)
            {
                /* see if any entities are in the way of the shot */
                var spot1 = self.s.origin;
                spot1[2] += self.viewheight;
                var spot2 = self.s.origin;
                spot2[2] += self.enemy.viewheight;

                var tr = gi.trace(spot1, null, null, spot2, self,
                        QCommon.CONTENTS_SOLID | QCommon.CONTENTS_MONSTER | QCommon.CONTENTS_SLIME |
                        QCommon.CONTENTS_LAVA | QCommon.CONTENTS_WINDOW);

                /* do we have a clear shot? */
                if (tr.ent != self.enemy)
                {
                    return false;
                }
            }

            /* melee attack */
            if (_ai_enemy_range == RANGE_MELEE)
            {
                /* don't always melee in easy mode */
                if ((skill!.Int == SKILL_EASY) && (QShared.randk() & 3) != 0)
                {
                    return false;
                }

                if (self.monsterinfo.melee != null)
                {
                    self.monsterinfo.attack_state = AS_MELEE;
                }
                else
                {
                    self.monsterinfo.attack_state = AS_MISSILE;
                }

                return true;
            }

            /* missile attack */
            if (self.monsterinfo.attack == null)
            {
                return false;
            }

            if (level.time < self.monsterinfo.attack_finished)
            {
                return false;
            }

            if (_ai_enemy_range == RANGE_FAR)
            {
                return false;
            }

            float chance;
            if ((self.monsterinfo.aiflags & AI_STAND_GROUND) != 0)
            {
                chance = 0.4f;
            }
            else if (_ai_enemy_range == RANGE_NEAR)
            {
                chance = 0.1f;
            }
            else if (_ai_enemy_range == RANGE_MID)
            {
                chance = 0.02f;
            }
            else
            {
                return false;
            }

            if (skill!.Int == SKILL_EASY)
            {
                chance *= 0.5f;
            }
            else if (skill!.Int >= SKILL_HARD)
            {
                chance *= 2;
            }

            if (QShared.frandk() < chance)
            {
                self.monsterinfo.attack_state = AS_MISSILE;
                self.monsterinfo.attack_finished = level.time + 2 * QShared.frandk();
                return true;
            }

            if ((self.flags & FL_FLY) != 0)
            {
                if (QShared.frandk() < 0.3f)
                {
                    self.monsterinfo.attack_state = AS_SLIDING;
                }
                else
                {
                    self.monsterinfo.attack_state = AS_STRAIGHT;
                }
            }

            return false;
        }

        /*
        * Turn and close until within an
        * angle to launch a melee attack
        */
        private void ai_run_melee(edict_t self)
        {
            if (self == null)
            {
                return;
            }

            self.ideal_yaw = _ai_enemy_yaw;
            M_ChangeYaw(self);

            if (FacingIdeal(self))
            {
                if (self.monsterinfo.melee != null) {
                    self.monsterinfo.melee(self);
                    self.monsterinfo.attack_state = AS_STRAIGHT;
                }
            }
        }

        /*
        * Turn in place until within an
        * angle to launch a missile attack
        */
        private void ai_run_missile(edict_t self)
        {
            if (self == null)
            {
                return;
            }

            self.ideal_yaw = _ai_enemy_yaw;
            M_ChangeYaw(self);

            if (FacingIdeal(self))
            {
                if (self.monsterinfo.attack != null) {
                    self.monsterinfo.attack(self);
                    self.monsterinfo.attack_state = AS_STRAIGHT;
                }
            }
        }

        /*
        * Decides if we're going to attack
        * or do something else used by
        * ai_run and ai_stand
        */
        private bool ai_checkattack(edict_t self)
        {
            if (self == null)
            {
                _ai_enemy_vis = false;

                return false;
            }


            /* this causes monsters to run blindly
            to the combat point w/o firing */
            if (self.goalentity != null)
            {
                if ((self.monsterinfo.aiflags & AI_COMBAT_POINT) != 0)
                {
                    return false;
                }

                if ((self.monsterinfo.aiflags & AI_SOUND_TARGET) != 0 && !visible(self, self.goalentity))
                {
                    if ((level.time - self.enemy!.last_sound_time) > 5.0)
                    {
                        if (self.goalentity == self.enemy)
                        {
                            if (self.movetarget != null)
                            {
                                self.goalentity = self.movetarget;
                            }
                            else
                            {
                                self.goalentity = null;
                            }
                        }

                        self.monsterinfo.aiflags &= ~AI_SOUND_TARGET;

                        if ((self.monsterinfo.aiflags & AI_TEMP_STAND_GROUND) != 0)
                        {
                            self.monsterinfo.aiflags &=
                                    ~(AI_STAND_GROUND | AI_TEMP_STAND_GROUND);
                        }
                    }
                    else
                    {
                        self.show_hostile = level.time + 1;
                        return false;
                    }
                }
            }

            _ai_enemy_vis = false;

            /* see if the enemy is dead */
            var hesDeadJim = false;

            if ((self.enemy == null) || (!self.enemy.inuse))
            {
                hesDeadJim = true;
            }
            else if ((self.monsterinfo.aiflags & AI_MEDIC) != 0)
            {
                if (self.enemy.health > 0)
                {
                    hesDeadJim = true;
                    self.monsterinfo.aiflags &= ~AI_MEDIC;
                }
            }
            else
            {
                if ((self.monsterinfo.aiflags & AI_BRUTAL) != 0)
                {
                    if (self.enemy.health <= -80)
                    {
                        hesDeadJim = true;
                    }
                }
                else
                {
                    if (self.enemy.health <= 0)
                    {
                        hesDeadJim = true;
                    }
                }
            }

            if (hesDeadJim)
            {
                self.enemy = null;

                if (self.oldenemy != null && (self.oldenemy.health > 0))
                {
                    self.enemy = self.oldenemy;
                    self.oldenemy = null;
                    HuntTarget(self);
                }
                else
                {
                    if (self.movetarget != null)
                    {
                        self.goalentity = self.movetarget;
                        self.monsterinfo.walk!(self);
                    }
                    else
                    {
                        /* we need the pausetime otherwise the stand code
                        will just revert to walking with no target and
                        the monsters will wonder around aimlessly trying
                        to hunt the world entity */
                        self.monsterinfo.pausetime = level.time + 100000000;
                        self.monsterinfo.stand!(self);
                    }

                    return true;
                }
            }

            /* wake up other monsters */
            self.show_hostile = level.time + 1;

            /* check knowledge of enemy */
            _ai_enemy_vis = visible(self, self.enemy!);

            if (_ai_enemy_vis)
            {
                self.monsterinfo.search_time = level.time + 5;
                self.monsterinfo.last_sighting = self.enemy!.s.origin;
            }

            /* look for other coop players here */
            if (coop!.Bool && (self.monsterinfo.search_time < level.time))
            {
                if (FindTarget(self))
                {
                    return true;
                }
            }

            if (self.enemy != null)
            {
                _ai_enemy_infront = infront(self, self.enemy);
                _ai_enemy_range = range(self, self.enemy);
                var temp = self.enemy.s.origin - self.s.origin;
                _ai_enemy_yaw = vectoyaw(temp);
            }

            if (self.monsterinfo.attack_state == AS_MISSILE)
            {
                ai_run_missile(self);
                return true;
            }

            if (self.monsterinfo.attack_state == AS_MELEE)
            {
                ai_run_melee(self);
                return true;
            }

            /* if enemy is not currently visible,
            we will never attack */
            if (!_ai_enemy_vis)
            {
                return false;
            }

            return self.monsterinfo.checkattack!(self);
        }

        /*
        * The monster has an enemy
        * it is trying to kill
        */
        private static void ai_run(QuakeGame g, edict_t self, float dist)
        {
            // vec3_t v;
            // edict_t *tempgoal;
            // edict_t *save;
            // qboolean new;
            // edict_t *marker;
            // float d1, d2;
            // trace_t tr;
            // vec3_t v_forward, v_right;
            // float left, center, right;
            // vec3_t left_target, right_target;

            if (self == null)
            {
                return;
            }

            /* if we're going to a combat point, just proceed */
            if ((self.monsterinfo.aiflags & AI_COMBAT_POINT) != 0)
            {
                g.M_MoveToGoal(self, dist);
                return;
            }

            var v = new Vector3();
            if ((self.monsterinfo.aiflags & AI_SOUND_TARGET) != 0)
            {
                /* Special case: Some projectiles like grenades or rockets are
                classified as an enemy. When they explode they generate a
                sound entity, triggering this code path. Since they're gone
                after the explosion their entity pointer is NULL. Therefor
                self->enemy is also NULL and we're crashing. Work around
                this by predending that the enemy is still there, and move
                to it. */
                if (self.enemy != null)
                {
                    v = self.s.origin - self.enemy.s.origin;

                    if (v.Length() < 64)
                    {
                        self.monsterinfo.aiflags |= (AI_STAND_GROUND | AI_TEMP_STAND_GROUND);
                        self.monsterinfo.stand!(self);
                        return;
                    }
                }

                g.M_MoveToGoal(self, dist);

                if (!g.FindTarget(self))
                {
                    return;
                }
            }

            if (g.ai_checkattack(self))
            {
                return;
            }

            // if (self.monsterinfo.attack_state == AS_SLIDING)
            // {
            //     ai_run_slide(self, dist);
            //     return;
            // }

            if (g._ai_enemy_vis)
            {
                g.M_MoveToGoal(self, dist);
                self.monsterinfo.aiflags &= ~AI_LOST_SIGHT;
                self.monsterinfo.last_sighting = self.enemy!.s.origin;
                self.monsterinfo.trail_time = g.level.time;
                return;
            }

            if ((self.monsterinfo.search_time != 0) &&
                (g.level.time > (self.monsterinfo.search_time + 20)))
            {
                g.M_MoveToGoal(self, dist);
                self.monsterinfo.search_time = 0;
                return;
            }

            var tempgoal = g.G_SpawnOptional();

            if (tempgoal == null)
            {
                g.M_MoveToGoal(self, dist);
                return;
            }

            var save = self.goalentity;
            self.goalentity = tempgoal;

            var isNew = false;

            if ((self.monsterinfo.aiflags & AI_LOST_SIGHT) == 0)
            {
                /* just lost sight of the player, decide where to go first */
                self.monsterinfo.aiflags |= (AI_LOST_SIGHT | AI_PURSUIT_LAST_SEEN);
                self.monsterinfo.aiflags &= ~(AI_PURSUE_NEXT | AI_PURSUE_TEMP);
                isNew = true;
            }

            if ((self.monsterinfo.aiflags & AI_PURSUE_NEXT) != 0)
            {
                self.monsterinfo.aiflags &= ~AI_PURSUE_NEXT;

                /* give ourself more time since we got this far */
                self.monsterinfo.search_time = g.level.time + 5;

                Console.WriteLine("AI_PURSUE_NEXT");
            //     if (self->monsterinfo.aiflags & AI_PURSUE_TEMP)
            //     {
            //         self->monsterinfo.aiflags &= ~AI_PURSUE_TEMP;
            //         marker = NULL;
            //         VectorCopy(self->monsterinfo.saved_goal,
            //                 self->monsterinfo.last_sighting);
            //         new = true;
            //     }
            //     else if (self->monsterinfo.aiflags & AI_PURSUIT_LAST_SEEN)
            //     {
            //         self->monsterinfo.aiflags &= ~AI_PURSUIT_LAST_SEEN;
            //         marker = PlayerTrail_PickFirst(self);
            //     }
            //     else
            //     {
            //         marker = PlayerTrail_PickNext(self);
            //     }

            //     if (marker)
            //     {
            //         VectorCopy(marker->s.origin, self->monsterinfo.last_sighting);
            //         self->monsterinfo.trail_time = marker->timestamp;
            //         self->s.angles[YAW] = self->ideal_yaw = marker->s.angles[YAW];
            //         new = true;
            //     }
            }

            v = self.s.origin - self.monsterinfo.last_sighting;
            var d1 = v.Length();

            if (d1 <= dist)
            {
                self.monsterinfo.aiflags |= AI_PURSUE_NEXT;
                dist = d1;
            }

            self.goalentity.s.origin = self.monsterinfo.last_sighting;

            if (isNew)
            {
                var tr = g.gi.trace(self.s.origin, self.mins, self.maxs,
                        self.monsterinfo.last_sighting, self,
                        QShared.MASK_PLAYERSOLID);

                if (tr.fraction < 1)
                {
                    v = self.goalentity.s.origin - self.s.origin;
                    d1 = v.Length();
                    var center = tr.fraction;
                    var d2 = d1 * ((center + 1) / 2);
                    self.s.angles[QShared.YAW] = self.ideal_yaw = g.vectoyaw(v);
                    var v_forward = new Vector3();
                    var v_right = new Vector3();
                    var tmp = new Vector3();
                    QShared.AngleVectors(self.s.angles, ref v_forward, ref v_right, ref tmp);

                    v = new Vector3(d2, -16, 0);
                    var left_target = new Vector3();
                    G_ProjectSource(self.s.origin, v, v_forward, v_right, ref left_target);
                    tr = g.gi.trace(self.s.origin, self.mins, self.maxs, left_target,
                            self, QShared.MASK_PLAYERSOLID);
                    var left = tr.fraction;

                    v = new Vector3(d2, 16, 0);
                    var right_target = new Vector3();
                    G_ProjectSource(self.s.origin, v, v_forward, v_right, ref right_target);
                    tr = g.gi.trace(self.s.origin, self.mins, self.maxs, right_target,
                            self, QShared.MASK_PLAYERSOLID);
                    var right = tr.fraction;

                    center = (d1 * center) / d2;

                    if ((left >= center) && (left > right))
                    {
                        if (left < 1)
                        {
                            v = new Vector3(d2 * left * 0.5f, -16, 0);
                            G_ProjectSource(self.s.origin, v, v_forward,
                                    v_right, ref left_target);
                        }

                        self.monsterinfo.saved_goal = self.monsterinfo.last_sighting;
                        self.monsterinfo.aiflags |= AI_PURSUE_TEMP;
                        self.goalentity.s.origin = left_target;
                        self.monsterinfo.last_sighting = left_target;
                        v = self.goalentity.s.origin - self.s.origin;
                        self.s.angles[QShared.YAW] = self.ideal_yaw = g.vectoyaw(v);
                    }
                    else if ((right >= center) && (right > left))
                    {
                        if (right < 1)
                        {
                            v = new Vector3(d2 * right * 0.5f, 16, 0);
                            G_ProjectSource(self.s.origin, v, v_forward,
                                    v_right, ref right_target);
                        }

                        self.monsterinfo.saved_goal = self.monsterinfo.last_sighting;
                        self.monsterinfo.aiflags |= AI_PURSUE_TEMP;
                        self.goalentity.s.origin = right_target;
                        self.monsterinfo.last_sighting = right_target;
                        v = self.goalentity.s.origin - self.s.origin;
                        self.s.angles[QShared.YAW] = self.ideal_yaw = g.vectoyaw(v);
                    }
                }
            }

            g.M_MoveToGoal(self, dist);

            g.G_FreeEdict(tempgoal);

            self.goalentity = save;
        }

    }
}