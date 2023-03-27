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
 * Combat code like damage, death and so on.
 *
 * =======================================================================
 */

using System.Numerics;

namespace Quake2 {

    partial class QuakeGame
    {
        private void Killed(edict_t targ, edict_t inflictor, edict_t attacker,
                int damage, in Vector3 point)
        {
            if (targ == null || inflictor == null || attacker == null)
            {
                return;
            }

            if (targ.health < -999)
            {
                targ.health = -999;
            }

            targ.enemy = attacker;

            if ((targ.svflags & QGameFlags.SVF_MONSTER) != 0 && (targ.deadflag != DEAD_DEAD))
            {
                if ((targ.monsterinfo.aiflags & AI_GOOD_GUY) == 0)
                {
                    level.killed_monsters++;

                    if (coop!.Bool && attacker.client != null)
                    {
                        ((gclient_t)attacker.client).resp.score++;
                    }

                    /* medics won't heal monsters that they kill themselves */
                    if (attacker.classname != null && attacker.classname == "monster_medic")
                    {
                        targ.owner = attacker;
                    }
                }
            }

            if ((targ.movetype == movetype_t.MOVETYPE_PUSH) ||
                (targ.movetype == movetype_t.MOVETYPE_STOP) ||
                (targ.movetype == movetype_t.MOVETYPE_NONE))
            {
                /* doors, triggers, etc */
                targ.die!(targ, inflictor, attacker, damage, point);
                return;
            }

            if ((targ.svflags & QGameFlags.SVF_MONSTER) != 0 && (targ.deadflag != DEAD_DEAD))
            {
                // targ.touch = NULL;
                // monster_death_use(targ);
            }

            targ.die!(targ, inflictor, attacker, damage, point);
        }

        private void M_ReactToDamage(edict_t targ, edict_t attacker)
        {
            if (targ == null || attacker == null)
            {
                return;
            }

            if (targ.health <= 0)
            {
                return;
            }

            if ((attacker.client == null) && (attacker.svflags & QGameFlags.SVF_MONSTER) != 0)
            {
                return;
            }

            if ((attacker == targ) || (attacker == targ.enemy))
            {
                return;
            }

            /* if we are a good guy monster and our attacker is a player
            or another good guy, do not get mad at them */
            if ((targ.monsterinfo.aiflags & AI_GOOD_GUY) != 0)
            {
                if (attacker.client != null || (attacker.monsterinfo.aiflags & AI_GOOD_GUY) != 0)
                {
                    return;
                }
            }

            /* if attacker is a client, get mad at
               them because he's good and we're not */
            if (attacker.client != null)
            {
                targ.monsterinfo.aiflags &= ~AI_SOUND_TARGET;

            //     /* this can only happen in coop (both new and old
            //     enemies are clients)  only switch if can't see
            //     the current enemy */
            //     if (targ.enemy != null && targ.enemy.client != null)
            //     {
            //         if (visible(targ, targ->enemy))
            //         {
            //             targ->oldenemy = attacker;
            //             return;
            //         }

            //         targ->oldenemy = targ->enemy;
            //     }

                targ.enemy = attacker;

            //     if (!(targ->monsterinfo.aiflags & AI_DUCKED))
            //     {
            //         FoundTarget(targ);
            //     }

                return;
            }

            // /* it's the same base (walk/swim/fly) type and a
            // different classname and it's not a tank
            // (they spray too much), get mad at them */
            // if (((targ->flags & (FL_FLY | FL_SWIM)) ==
            //     (attacker->flags & (FL_FLY | FL_SWIM))) &&
            //     (strcmp(targ->classname, attacker->classname) != 0) &&
            //     (strcmp(attacker->classname, "monster_tank") != 0) &&
            //     (strcmp(attacker->classname, "monster_supertank") != 0) &&
            //     (strcmp(attacker->classname, "monster_makron") != 0) &&
            //     (strcmp(attacker->classname, "monster_jorg") != 0))
            // {
            //     if (targ->enemy && targ->enemy->client)
            //     {
            //         targ->oldenemy = targ->enemy;
            //     }

            //     targ->enemy = attacker;

            //     if (!(targ->monsterinfo.aiflags & AI_DUCKED))
            //     {
            //         FoundTarget(targ);
            //     }
            // }
            // /* if they *meant* to shoot us, then shoot back */
            // else if (attacker->enemy == targ)
            // {
            //     if (targ->enemy && targ->enemy->client)
            //     {
            //         targ->oldenemy = targ->enemy;
            //     }

            //     targ->enemy = attacker;

            //     if (!(targ->monsterinfo.aiflags & AI_DUCKED))
            //     {
            //         FoundTarget(targ);
            //     }
            // }
            // /* otherwise get mad at whoever they are mad
            // at (help our buddy) unless it is us! */
            // else if (attacker->enemy)
            // {
            //     if (targ->enemy && targ->enemy->client)
            //     {
            //         targ->oldenemy = targ->enemy;
            //     }

            //     targ->enemy = attacker->enemy;

            //     if (!(targ->monsterinfo.aiflags & AI_DUCKED))
            //     {
            //         FoundTarget(targ);
            //     }
            // }
        }
        
        private void T_Damage(edict_t targ, edict_t inflictor, edict_t attacker,
                in Vector3 idir, in Vector3 point, in Vector3 normal, int damage,
                int knockback, int dflags, int mod)
        {
            if (targ == null || inflictor == null || attacker == null)
            {
                return;
            }

            if (targ.takedamage == 0)
            {
                return;
            }

            /* friendly fire avoidance if enabled you
            can't hurt teammates (but you can hurt
            yourself) knockback still occurs */
            if ((targ != attacker) && ((deathmatch!.Bool &&
                (dmflags!.Int & (QShared.DF_MODELTEAMS | QShared.DF_SKINTEAMS)) != 0) ||
                coop!.Bool))
            {
                // if (OnSameTeam(targ, attacker))
                // {
                //     if ((int)(dmflags->value) & DF_NO_FRIENDLY_FIRE)
                //     {
                //         damage = 0;
                //     }
                //     else
                //     {
                //         mod |= MOD_FRIENDLY_FIRE;
                //     }
                // }
            }

            meansOfDeath = mod;

            /* easy mode takes half damage */
            // if ((skill!.Int == SKILL_EASY) && !deathmatch!.Bool && targ.client != 0)
            // {
            //     damage *= 0.5;

            //     if (!damage)
            //     {
            //         damage = 1;
            //     }
            // }

            var client = (gclient_t)targ.client!;

            // if ((dflags & DAMAGE_BULLET) != 0)
            // {
            //     te_sparks = TE_BULLET_SPARKS;
            // }
            // else
            // {
            //     te_sparks = TE_SPARKS;
            // }

            var dir = Vector3.Normalize(idir);

            /* bonus damage for suprising a monster */
            // if (!(dflags & DAMAGE_RADIUS) && (targ->svflags & SVF_MONSTER) &&
            //     (attacker->client) && (!targ->enemy) && (targ->health > 0))
            // {
            //     damage *= 2;
            // }

            // if (targ->flags & FL_NO_KNOCKBACK)
            // {
            //     knockback = 0;
            // }

            // /* figure momentum add */
            // if (!(dflags & DAMAGE_NO_KNOCKBACK))
            // {
            //     if ((knockback) && (targ->movetype != MOVETYPE_NONE) &&
            //         (targ->movetype != MOVETYPE_BOUNCE) &&
            //         (targ->movetype != MOVETYPE_PUSH) &&
            //         (targ->movetype != MOVETYPE_STOP))
            //     {
            //         vec3_t kvel;
            //         float mass;

            //         if (targ->mass < 50)
            //         {
            //             mass = 50;
            //         }
            //         else
            //         {
            //             mass = targ->mass;
            //         }

            //         if (targ->client && (attacker == targ))
            //         {
            //             /* This allows rocket jumps */
            //             VectorScale(dir, 1600.0 * (float)knockback / mass, kvel);
            //         }
            //         else
            //         {
            //             VectorScale(dir, 500.0 * (float)knockback / mass, kvel);
            //         }

            //         VectorAdd(targ->velocity, kvel, targ->velocity);
            //     }
            // }

            var take = damage;
            var save = 0;

            // /* check for godmode */
            // if ((targ->flags & FL_GODMODE) && !(dflags & DAMAGE_NO_PROTECTION))
            // {
            //     take = 0;
            //     save = damage;
            //     SpawnDamage(te_sparks, point, normal);
            // }

            // /* check for invincibility */
            // if ((client && (client->invincible_framenum > level.framenum)) &&
            //     !(dflags & DAMAGE_NO_PROTECTION))
            // {
            //     if (targ->pain_debounce_time < level.time)
            //     {
            //         gi.sound(targ, CHAN_ITEM, gi.soundindex(
            //                     "items/protect4.wav"), 1, ATTN_NORM, 0);
            //         targ->pain_debounce_time = level.time + 2;
            //     }

            //     take = 0;
            //     save = damage;
            // }

            // psave = CheckPowerArmor(targ, point, normal, take, dflags);
            // take -= psave;

            // asave = CheckArmor(targ, point, normal, take, te_sparks, dflags);
            // take -= asave;

            // /* treat cheat/powerup savings the same as armor */
            // asave += save;

            /* do the damage */
            if (take > 0)
            {
            //     if ((targ->svflags & SVF_MONSTER) || (client))
            //     {
            //         SpawnDamage(TE_BLOOD, point, normal);
            //     }
            //     else
            //     {
            //         SpawnDamage(te_sparks, point, normal);
            //     }

                targ.health = targ.health - take;

                if (targ.health <= 0)
                {
            //         if ((targ->svflags & SVF_MONSTER) || (client))
            //         {
            //             targ->flags |= FL_NO_KNOCKBACK;
            //         }

                    Killed(targ, inflictor, attacker, take, point);
                    return;
                }
            }

            if ((targ.svflags & QGameFlags.SVF_MONSTER) != 0)
            {
                M_ReactToDamage(targ, attacker);

            //     if (!(targ->monsterinfo.aiflags & AI_DUCKED) && (take))
            //     {
                    targ.pain!(targ, attacker, knockback, take);

            //         /* nightmare mode monsters don't go into pain frames often */
            //         if (skill->value == SKILL_HARDPLUS)
            //         {
            //             targ->pain_debounce_time = level.time + 5;
            //         }
            //     }
            }
            else if (client != null)
            {
            //     if (!(targ->flags & FL_GODMODE) && (take))
            //     {
            //         targ->pain(targ, attacker, knockback, take);
            //     }
            }
            else if (take > 0)
            {
                if (targ.pain != null)
                {
                    targ.pain(targ, attacker, knockback, take);
                }
            }

            // /* add to the damage inflicted on a player this frame
            // the total will be turned into screen blends and view
            // angle kicks at the end of the frame */
            // if (client)
            // {
            //     client->damage_parmor += psave;
            //     client->damage_armor += asave;
            //     client->damage_blood += take;
            //     client->damage_knockback += knockback;
            //     VectorCopy(point, client->damage_from);
            // }
        }

    }
}