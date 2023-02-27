using System.Numerics;

namespace Quake2 {

    partial class QuakeGame
    {
        /*
        * The old weapon has been dropped all
        * the way, so make the new one current
        */
        private void ChangeWeapon(edict_t ent)
        {
            int i;

            if (ent == null)
            {
                return;
            }

            var client = (gclient_t)ent.client!;
            if (client.grenade_time != 0)
            {
                client.grenade_time = level.time;
                client.weapon_sound = 0;
                // weapon_grenade_fire(ent, false);
                client.grenade_time = 0;
            }

            client.pers.lastweapon = client.pers.weapon;
            client.pers.weapon = client.newweapon;
            client.newweapon = null;
            client.machinegun_shots = 0;

            /* set visible model */
            if (ent.s.modelindex == 255)
            {
                // if (client.pers.weapon != null)
                // {
                    // i = ((client.pers.weapon.weapmodel & 0xff) << 8);
                // }
                // else
                {
                    i = 0;
                }

                ent.s.skinnum = ent.index | i;
            }

            // if (client.pers.weapon != null && client.pers.weapon.ammo)
            // {
            //     ent->client->ammo_index =
            //         ITEM_INDEX(FindItem(ent->client->pers.weapon->ammo));
            // }
            // else
            // {
            //     ent->client->ammo_index = 0;
            // }

            if (client.pers.weapon == null)
            {
                /* dead */
                client.ps.gunindex = 0;
                return;
            }

            // client.weaponstate = WEAPON_ACTIVATING;
            client.ps.gunframe = 0;
            client.ps.gunindex = gi.modelindex(client.pers.weapon.view_model);

            client.anim_priority = ANIM_PAIN;

            // if (client.ps.pmove.pm_flags & PMF_DUCKED)
            // {
            //     ent.s.frame = FRAME_crpain1;
            //     ent.client->anim_end = FRAME_crpain4;
            // }
            // else
            // {
            //     ent->s.frame = FRAME_pain301;
            //     ent->client->anim_end = FRAME_pain304;
            // }
        }
    }
}