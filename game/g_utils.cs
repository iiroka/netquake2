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
                var t1 = new Vector3();
                var t2 = new Vector3();
                QShared.AngleVectors(angles, ref movedir, ref t1, ref t2);
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

    }
}