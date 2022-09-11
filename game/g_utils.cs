namespace Quake2 {

    partial class QuakeGame
    {
        private void G_InitEdict(ref edict_t e, int num)
        {
            e.inuse = true;
            e.classname = "noclass";
            // e.gravity = 1.0;
            e.s.number = num;
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
                    G_InitEdict (ref g_edicts[i], i);
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
            G_InitEdict (ref e2, n);

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
            // gi.unlinkentity(ed); /* unlink from world */

            if (deathmatch!.Bool || coop!.Bool)
            {
                // if ((ed - g_edicts) <= (maxclients->value + BODY_QUEUE_SIZE))
                // {
                //     return;
                // }
            }
            else
            {
                // if ((ed - g_edicts) <= maxclients->value)
                // {
                //     return;
                // }
            }

            // memset(ed, 0, sizeof(*ed));
            ed.classname = "freed";
            // ed.freetime = level.time;
            ed.inuse = false;
        }

    }
}