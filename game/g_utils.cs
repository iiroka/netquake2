namespace Quake2 {

    partial class QuakeGame
    {
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
                // if (!g_edicts[i].inuse && (policy == POLICY_DESPERATE || g_edicts[i].freetime < 2.0f || (level.time - g_edicts[i].freetime) > 0.5f))
                if (!g_edicts[i].inuse)
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

            return e;
        }

        /*
        * Marks the edict as free
        */
        private void G_FreeEdict(edict_t ed)
        {
            gi.unlinkentity(ed); /* unlink from world */

            if (deathmatch!.Bool || coop!.Bool)
            {
                // if ((ed - g_edicts) <= (maxclients->value + BODY_QUEUE_SIZE))
                // {
                //     return;
                // }
            }
            else
            {
                if (ed.index <= maxclients!.Int)
                {
                    return;
                }
            }

            // memset(ed, 0, sizeof(*ed));
            ed.classname = "freed";
            ed.freetime = level.time;
            ed.inuse = false;
        }

    }
}