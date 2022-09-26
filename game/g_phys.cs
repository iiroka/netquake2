namespace Quake2 {

    partial class QuakeGame
    {
        /*
        * Runs thinking code for
        * this frame if necessary
        */
        private bool SV_RunThink(edict_t ent)
        {
            if (ent == null)
            {
                return false;
            }

            float thinktime = ent.nextthink;

            if (thinktime <= 0)
            {
                return true;
            }

            if (thinktime > level.time + 0.001)
            {
                return true;
            }

            ent.nextthink = 0;

            // if (ent.think == null)
            // {
            //     gi.error("NULL ent->think");
            // }

            // ent->think(ent);

            return false;
        }

        /* ================================================================== */

        /*
        * Non moving objects can only think
        */
        private void SV_Physics_None(edict_t ent)
        {
            if (ent == null)
            {
                return;
            }

            /* regular thinking */
            SV_RunThink(ent);
        }

        /* ================================================================== */

        private void G_RunEntity(edict_t ent)
        {
            if (ent == null)
            {
                return;
            }

            // if (ent->prethink)
            // {
            //     ent->prethink(ent);
            // }

            switch (ent.movetype)
            {
                case movetype_t.MOVETYPE_PUSH:
                    goto case movetype_t.MOVETYPE_STOP;
                case movetype_t.MOVETYPE_STOP:
                    Console.WriteLine("PUSH");
            //         SV_Physics_Pusher(ent);
                    break;
                case movetype_t.MOVETYPE_NONE:
                    SV_Physics_None(ent);
                    break;
                // case movetype_t.MOVETYPE_NOCLIP:
            //         SV_Physics_Noclip(ent);
            //         break;
            //     case movetype_t.MOVETYPE_STEP:
            //         SV_Physics_Step(ent);
            //         break;
            //     case movetype_t.MOVETYPE_TOSS:
            //     case movetype_t.MOVETYPE_BOUNCE:
            //     case movetype_t.MOVETYPE_FLY:
            //     case movetype_t.MOVETYPE_FLYMISSILE:
            //         SV_Physics_Toss(ent);
            //         break;
                default:
                    gi.error($"SV_Physics: bad movetype {ent.movetype}");
                    break;
            }
        }

    }
}