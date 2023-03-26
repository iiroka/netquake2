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

namespace Quake2 {

    partial class QuakeGame
    {

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

            //         if ((self->s.angles[YAW] != self->ideal_yaw) &&
            //             self->monsterinfo.aiflags & AI_TEMP_STAND_GROUND)
            //         {
            //             self->monsterinfo.aiflags &=
            //                 ~(AI_STAND_GROUND | AI_TEMP_STAND_GROUND);
            //             self->monsterinfo.run(self);
            //         }

                    g.M_ChangeYaw(self);
            //         ai_checkattack(self);
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

            // if (!(self->spawnflags & 1) && (self->monsterinfo.idle) &&
            //     (level.time > self->monsterinfo.idle_time))
            // {
            //     if (self->monsterinfo.idle_time)
            //     {
            //         self->monsterinfo.idle(self);
            //         self->monsterinfo.idle_time = level.time + 15 + random() * 15;
            //     }
            //     else
            //     {
            //         self->monsterinfo.idle_time = level.time + random() * 15;
            //     }
            // }
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
                Console.WriteLine("range");
            //     r = range(self, client);

            //     if (r == RANGE_FAR)
            //     {
                    return false;
            //     }

            //     /* is client in an spot too dark to be seen? */
            //     if (client->light_level <= 5)
            //     {
            //         return false;
            //     }

            //     if (!visible(self, client))
            //     {
            //         return false;
            //     }

            //     if (r == RANGE_NEAR)
            //     {
            //         if ((client->show_hostile < level.time) && !infront(self, client))
            //         {
            //             return false;
            //         }
            //     }
            //     else if (r == RANGE_MID)
            //     {
            //         if (!infront(self, client))
            //         {
            //             return false;
            //         }
            //     }

                // self.enemy = client;

            //     if (strcmp(self->enemy->classname, "player_noise") != 0)
            //     {
            //         self->monsterinfo.aiflags &= ~AI_SOUND_TARGET;

            //         if (!self->enemy->client)
            //         {
            //             self->enemy = self->enemy->enemy;

            //             if (!self->enemy->client)
            //             {
            //                 self->enemy = NULL;
            //                 return false;
            //             }
            //         }
            //     }
            }
            else /* heardit */
            {
            //     vec3_t temp;

            //     if (self->spawnflags & 1)
            //     {
            //         if (!visible(self, client))
            //         {
            //             return false;
            //         }
            //     }
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

            // FoundTarget(self);

            // if (!(self->monsterinfo.aiflags & AI_SOUND_TARGET) &&
            //     (self->monsterinfo.sight))
            // {
            //     self->monsterinfo.sight(self, self->enemy);
            // }

            return true;
        }

        /* ============================================================================= */

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
                    var v = self.s.origin - self.enemy.s.origin;

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

            // if (ai_checkattack(self))
            // {
            //     return;
            // }

            // if (self.monsterinfo.attack_state == AS_SLIDING)
            // {
            //     ai_run_slide(self, dist);
            //     return;
            // }

            // if (enemy_vis)
            // {
            //     M_MoveToGoal(self, dist);
            //     self->monsterinfo.aiflags &= ~AI_LOST_SIGHT;
            //     VectorCopy(self->enemy->s.origin, self->monsterinfo.last_sighting);
            //     self->monsterinfo.trail_time = level.time;
            //     return;
            // }

            // if ((self->monsterinfo.search_time) &&
            //     (level.time > (self->monsterinfo.search_time + 20)))
            // {
            //     M_MoveToGoal(self, dist);
            //     self->monsterinfo.search_time = 0;
            //     return;
            // }

            // tempgoal = G_SpawnOptional();

            // if (!tempgoal)
            // {
            //     M_MoveToGoal(self, dist);
            //     return;
            // }

            // save = self->goalentity;
            // self->goalentity = tempgoal;

            // new = false;

            // if (!(self->monsterinfo.aiflags & AI_LOST_SIGHT))
            // {
            //     /* just lost sight of the player, decide where to go first */
            //     self->monsterinfo.aiflags |= (AI_LOST_SIGHT | AI_PURSUIT_LAST_SEEN);
            //     self->monsterinfo.aiflags &= ~(AI_PURSUE_NEXT | AI_PURSUE_TEMP);
            //     new = true;
            // }

            // if (self->monsterinfo.aiflags & AI_PURSUE_NEXT)
            // {
            //     self->monsterinfo.aiflags &= ~AI_PURSUE_NEXT;

            //     /* give ourself more time since we got this far */
            //     self->monsterinfo.search_time = level.time + 5;

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
            // }

            // VectorSubtract(self->s.origin, self->monsterinfo.last_sighting, v);
            // d1 = VectorLength(v);

            // if (d1 <= dist)
            // {
            //     self->monsterinfo.aiflags |= AI_PURSUE_NEXT;
            //     dist = d1;
            // }

            // VectorCopy(self->monsterinfo.last_sighting, self->goalentity->s.origin);

            // if (new)
            // {
            //     tr = gi.trace(self->s.origin, self->mins, self->maxs,
            //             self->monsterinfo.last_sighting, self,
            //             MASK_PLAYERSOLID);

            //     if (tr.fraction < 1)
            //     {
            //         VectorSubtract(self->goalentity->s.origin, self->s.origin, v);
            //         d1 = VectorLength(v);
            //         center = tr.fraction;
            //         d2 = d1 * ((center + 1) / 2);
            //         self->s.angles[YAW] = self->ideal_yaw = vectoyaw(v);
            //         AngleVectors(self->s.angles, v_forward, v_right, NULL);

            //         VectorSet(v, d2, -16, 0);
            //         G_ProjectSource(self->s.origin, v, v_forward, v_right, left_target);
            //         tr = gi.trace(self->s.origin, self->mins, self->maxs, left_target,
            //                 self, MASK_PLAYERSOLID);
            //         left = tr.fraction;

            //         VectorSet(v, d2, 16, 0);
            //         G_ProjectSource(self->s.origin, v, v_forward, v_right, right_target);
            //         tr = gi.trace(self->s.origin, self->mins, self->maxs, right_target,
            //                 self, MASK_PLAYERSOLID);
            //         right = tr.fraction;

            //         center = (d1 * center) / d2;

            //         if ((left >= center) && (left > right))
            //         {
            //             if (left < 1)
            //             {
            //                 VectorSet(v, d2 * left * 0.5, -16, 0);
            //                 G_ProjectSource(self->s.origin, v, v_forward,
            //                         v_right, left_target);
            //             }

            //             VectorCopy(self->monsterinfo.last_sighting,
            //                     self->monsterinfo.saved_goal);
            //             self->monsterinfo.aiflags |= AI_PURSUE_TEMP;
            //             VectorCopy(left_target, self->goalentity->s.origin);
            //             VectorCopy(left_target, self->monsterinfo.last_sighting);
            //             VectorSubtract(self->goalentity->s.origin, self->s.origin, v);
            //             self->s.angles[YAW] = self->ideal_yaw = vectoyaw(v);
            //         }
            //         else if ((right >= center) && (right > left))
            //         {
            //             if (right < 1)
            //             {
            //                 VectorSet(v, d2 * right * 0.5, 16, 0);
            //                 G_ProjectSource(self->s.origin, v, v_forward, v_right,
            //                         right_target);
            //             }

            //             VectorCopy(self->monsterinfo.last_sighting,
            //                     self->monsterinfo.saved_goal);
            //             self->monsterinfo.aiflags |= AI_PURSUE_TEMP;
            //             VectorCopy(right_target, self->goalentity->s.origin);
            //             VectorCopy(right_target, self->monsterinfo.last_sighting);
            //             VectorSubtract(self->goalentity->s.origin, self->s.origin, v);
            //             self->s.angles[YAW] = self->ideal_yaw = vectoyaw(v);
            //         }
            //     }
            // }

            // M_MoveToGoal(self, dist);

            // G_FreeEdict(tempgoal);

            // self->goalentity = save;
        }

    }
}