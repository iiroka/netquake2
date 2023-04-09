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
 * Misc. utility functions for the game logic.
 *
 * =======================================================================
 */

using System.Numerics;

namespace Quake2 {

    partial class QuakeGame
    {
        private const int MAXCHOICES = 8;

        private static void G_ProjectSource(in Vector3 point, in Vector3 distance, in Vector3 forward,
                in Vector3 right, ref Vector3 result)
        {
            result[0] = point[0] + forward[0] * distance[0] + right[0] * distance[1];
            result[1] = point[1] + forward[1] * distance[0] + right[1] * distance[1];
            result[2] = point[2] + forward[2] * distance[0] + right[2] * distance[1] +
                        distance[2];
        }

        /*
        * Searches all active entities for the next
        * one that holds the matching string at fieldofs
        * (use the FOFS() macro) in the structure.
        *
        * Searches beginning at the edict after from, or
        * the beginning. If NULL, NULL will be returned
        * if the end of the list is reached.
        */
        private edict_t? G_Find(in edict_t? from, string field, string match)
        {
            var edict_indx = 0;

            if (from == null)
            {
                edict_indx = 0;
            }
            else
            {
                edict_indx = from.index+1;
            }

            if (String.IsNullOrEmpty(match) || String.IsNullOrEmpty(field))
            {
                return null;
            }

            var finfo = typeof(edict_t).GetField(field);
            if (finfo == null)
            {
                return null;
            }


            for ( ; edict_indx < num_edicts; edict_indx++)
            {
                if (!g_edicts[edict_indx].inuse)
                {
                    continue;
                }

                object? obj = finfo.GetValue(g_edicts[edict_indx]);
                if (obj == null || !(obj is string))
                {
                    continue;
                }

                if (((string)obj).Equals(match))
                {
                    return g_edicts[edict_indx];
                }
            }

            return null;
        }

        /*
        * Returns entities that have origins
        * within a spherical area
        */
        private edict_t? findradius(edict_t? from, in Vector3 org, float rad)
        {
            var from_i = 0;
            if (from != null)
            {

                from_i = from.index + 1;
            }

            for ( ; from_i < global_num_ecicts; from_i++)
            {
                if (!g_edicts[from_i].inuse)
                {
                    continue;
                }

                if (g_edicts[from_i].solid == solid_t.SOLID_NOT)
                {
                    continue;
                }

                var eorg = new Vector3();
                for (int j = 0; j < 3; j++)
                {
                    eorg[j] = org[j] - (g_edicts[from_i].s.origin[j] +
                            (g_edicts[from_i].mins[j] + g_edicts[from_i].maxs[j]) * 0.5f);
                }

                if (eorg.Length() > rad)
                {
                    continue;
                }

                return g_edicts[from_i];
            }

            return null;
        }

        /*
        * Searches all active entities for
        * the next one that holds the matching
        * string at fieldofs (use the FOFS() macro)
        * in the structure.
        *
        * Searches beginning at the edict after from,
        * or the beginning. If NULL, NULL will be
        * returned if the end of the list is reached.
        */
        private edict_t? G_PickTarget(string targetname)
        {
            // edict_t *ent = NULL;
            int num_choices = 0;
            var choice = new edict_t?[MAXCHOICES];

            if (targetname == null)
            {
                gi.dprintf("G_PickTarget called with NULL targetname\n");
                return null;
            }

            edict_t? ent = null;
            while (true)
            {
                ent = G_Find(ent, "targetname", targetname);
                if (ent == null)
                {
                    break;
                }

                choice[num_choices++] = ent;

                if (num_choices == MAXCHOICES)
                {
                    break;
                }
            }

            if (num_choices == 0)
            {
                gi.dprintf($"G_PickTarget: target {targetname} not found\n");
                return null;
            }

            return choice[QShared.randk() % num_choices];
        }

        private void Think_Delay(edict_t ent)
        {
            if (ent == null)
            {
                return;
            }

            G_UseTargets(ent, ent.activator);
            G_FreeEdict(ent);
        }

        /*
        * The global "activator" should be set to
        * the entity that initiated the firing.
        *
        * If self.delay is set, a DelayedUse entity
        * will be created that will actually do the
        * SUB_UseTargets after that many seconds have passed.
        *
        * Centerprints any self.message to the activator.
        *
        * Search for (string)targetname in all entities that
        * match (string)self.target and call their .use function
        */
        private void G_UseTargets(edict_t ent, edict_t? activator)
        {
            if (ent == null)
            {
                return;
            }

            /* check for a delay */
            if (ent.delay > 0)
            {
                /* create a temp object to fire at a later time */
                var t = G_Spawn();
                t.classname = "DelayedUse";
                t.nextthink = level.time + ent.delay;
                t.think = Think_Delay;
                t.activator = activator;

                if (activator == null)
                {
                    gi.dprintf("Think_Delay with no activator\n");
                }

                t.message = ent.message;
                t.target = ent.target;
                t.killtarget = ent.killtarget;
                return;
            }

            /* print the message */
            if (activator != null && (ent.message != null) && (activator.svflags & QGameFlags.SVF_MONSTER) == 0)
            {
                // gi.centerprintf(activator, "%s", ent->message);

                // if (ent->noise_index)
                // {
                //     gi.sound(activator, CHAN_AUTO, ent->noise_index, 1, ATTN_NORM, 0);
                // }
                // else
                // {
                //     gi.sound(activator, CHAN_AUTO, gi.soundindex(
                //                     "misc/talk1.wav"), 1, ATTN_NORM, 0);
                // }
            }

            /* kill killtargets */
            if (ent.killtarget != null)
            {
                edict_t? t = null;

                while ((t = G_Find(t, "targetname", ent.killtarget)) != null)
                {
                    /* decrement secret count if target_secret is removed */
                    if (t.classname == "target_secret")
                    {
                        level.total_secrets--;
                    }
                    /* same deal with target_goal, but also turn off CD music if applicable */
                    else if (t.classname == "target_goal")
                    {
                        level.total_goals--;

                        // if (level.found_goals >= level.total_goals)
                        // {
                        //     gi.configstring (CS_CDTRACK, "0");
                        // }
                    }

                    G_FreeEdict(t);

                    if (!ent.inuse)
                    {
                        gi.dprintf("entity was removed while using killtargets\n");
                        return;
                    }
                }
            }

            /* fire targets */
            if (ent.target != null)
            {
                edict_t? t = null;

                while ((t = G_Find(t, "targetname", ent.target)) != null)
                {
                    /* doors fire area portals in a specific way */
                    if (t.classname == "func_areaportal" &&
                        (ent.classname == "func_door" ||
                        ent.classname == "func_door_rotating"))
                    {
                        continue;
                    }

                    if (t == ent)
                    {
                        gi.dprintf("WARNING: Entity used itself.\n");
                    }
                    else
                    {
                        if (t.use != null)
                        {
                            t.use!(t, ent, activator);
                        }
                    }

                    if (!ent.inuse)
                    {
                        gi.dprintf("entity was removed while using targets\n");
                        return;
                    }
                }
            }
        }


        static readonly Vector3 VEC_UP = new Vector3(0, -1, 0);
        static readonly Vector3 MOVEDIR_UP = new Vector3(0, 0, 1);
        static readonly Vector3 VEC_DOWN = new Vector3(0, -2, 0);
        static readonly Vector3 MOVEDIR_DOWN = new Vector3(0, 0, -1);

        private void G_SetMovedir(ref Vector3 angles, ref Vector3 movedir)
        {
            if (angles == VEC_UP)
            {
                movedir = MOVEDIR_UP;
            }
            else if (angles == VEC_DOWN)
            {
                movedir = MOVEDIR_DOWN;
            }
            else
            {
                QShared.AngleVectors(angles, out movedir, out var t1, out var t2);
            }

            angles = Vector3.Zero;
        }

        private float vectoyaw(in Vector3 vec)
        {
            float yaw;

            if (vec[QShared.PITCH] == 0)
            {
                yaw = 0;

                if (vec[QShared.YAW] > 0)
                {
                    yaw = 90;
                }
                else if (vec[QShared.YAW] < 0)
                {
                    yaw = -90;
                }
            }
            else
            {
                yaw = (int)(MathF.Atan2(vec[QShared.YAW], vec[QShared.PITCH]) * 180 / MathF.PI);

                if (yaw < 0)
                {
                    yaw += 360;
                }
            }

            return yaw;
        }

        private void vectoangles(in Vector3 value1, out Vector3 angles)
        {
            float forward;
            float yaw, pitch;

            if ((value1[1] == 0) && (value1[0] == 0))
            {
                yaw = 0;

                if (value1[2] > 0)
                {
                    pitch = 90;
                }
                else
                {
                    pitch = 270;
                }
            }
            else
            {
                if (value1[0] != 0)
                {
                    yaw = (int)(MathF.Atan2(value1[1], value1[0]) * 180 / MathF.PI);
                }
                else if (value1[1] > 0)
                {
                    yaw = 90;
                }
                else
                {
                    yaw = -90;
                }

                if (yaw < 0)
                {
                    yaw += 360;
                }

                forward = MathF.Sqrt(value1[0] * value1[0] + value1[1] * value1[1]);
                pitch = (int)(MathF.Atan2(value1[2], forward) * 180 / MathF.PI);

                if (pitch < 0)
                {
                    pitch += 360;
                }
            }

            angles = new Vector3();
            angles[QShared.PITCH] = -pitch;
            angles[QShared.YAW] = yaw;
            angles[QShared.ROLL] = 0;
        }

        private void G_InitEdict(ref edict_t e)
        {
            e.inuse = true;
            e.classname = "noclass";
            e.gravity = 1.0f;
            e.s.number = e.index;
        }

        /*
        * Either finds a free edict, or allocates a
        * new one.  Try to avoid reusing an entity
        * that was recently freed, because it can
        * cause the client to think the entity
        * morphed into something else instead of
        * being removed and recreated, which can
        * cause interpolated angles and bad trails.
        */
        private const int POLICY_DEFAULT	= 0;
        private const int POLICY_DESPERATE	= 1;

        private edict_t? G_FindFreeEdict(int policy)
        {
            for (int i = game.maxclients + 1 ; i < global_num_ecicts ; i++)
            {
                /* the first couple seconds of server time can involve a lot of
                freeing and allocating, so relax the replacement policy
                */
                if (!g_edicts[i].inuse && (policy == POLICY_DESPERATE || g_edicts[i].freetime < 2.0f || (level.time - g_edicts[i].freetime) > 0.5f))
                {
                    G_InitEdict (ref g_edicts[i]);
                    return g_edicts[i];
                }
            }

            return null;
        }

        private edict_t G_SpawnOptional()
        {
            var e = G_FindFreeEdict (POLICY_DEFAULT);

            if (e != null)
            {
                return e;
            }

            if (global_num_ecicts >= game.maxentities)
            {
                return G_FindFreeEdict (POLICY_DESPERATE)!;
            }

            int n = global_num_ecicts++;
            ref var e2 = ref g_edicts[n];
            G_InitEdict (ref e2);

            return e2;
        }

        private edict_t G_Spawn()
        {
            var e = G_SpawnOptional();

            if (e == null)
                gi.error ("ED_Alloc: no free edicts");

            return e!;
        }

        /*
        * Marks the edict as free
        */
        private void G_FreeEdict(edict_t ed)
        {
            gi.unlinkentity(ed); /* unlink from world */

            if (deathmatch!.Bool || coop!.Bool)
            {
                if (ed.index <= (maxclients!.Int + BODY_QUEUE_SIZE))
                {
                    return;
                }
            }
            else
            {
                if (ed.index <= maxclients!.Int)
                {
                    return;
                }
            }
        
            ed.Clear();
            ed.classname = "freed";
            ed.freetime = level.time;
            ed.inuse = false;
        }

        private void G_TouchTriggers(edict_t ent)
        {
            // int i, num;
            // edict_t *touch[MAX_EDICTS], *hit;

            if (ent == null)
            {
                return;
            }

            /* dead things don't activate triggers! */
            if ((ent.client != null || (ent.svflags & QGameFlags.SVF_MONSTER) != 0) && (ent.health <= 0))
            {
                return;
            }

            var touch = new edict_t[QShared.MAX_EDICTS];
            var num = gi.BoxEdicts(ent.absmin, ent.absmax, touch, QShared.AREA_TRIGGERS);

            /* be careful, it is possible to have an entity in this
            list removed before we get to it (killtriggered) */
            for (int i = 0; i < num; i++)
            {
                var hit = touch[i];

                if (!hit.inuse)
                {
                    continue;
                }

                if (hit.touch == null)
                {
                    continue;
                }

                hit.touch(hit, ent, null, null);
            }
        }

        /*
        * Kills all entities that would touch the
        * proposed new positioning of ent. Ent s
        * hould be unlinked before calling this!
        */
        private bool KillBox(edict_t ent)
        {
            // trace_t tr;

            if (ent == null)
            {
                return false;
            }

            while (true)
            {
                var tr = gi.trace(ent.s.origin, ent.mins, ent.maxs, ent.s.origin,
                        null, QShared.MASK_PLAYERSOLID);

                if (tr.ent == null)
                {
                    break;
                }

                /* nail it */
                T_Damage((edict_t)tr.ent, ent, ent, Vector3.Zero, ent.s.origin, Vector3.Zero,
                        100000, 0, DAMAGE_NO_PROTECTION, MOD_TELEFRAG);

                /* if we didn't kill it, fail */
                if (tr.ent.solid != solid_t.SOLID_NOT)
                {
                    return false;
                }
            }

            return true; /* all clear */
        }

    }
}