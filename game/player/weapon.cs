using System.Numerics;

namespace Quake2 {

    partial class QuakeGame
    {
        private void P_ProjectSource(edict_t ent, in Vector3 distance,
                in Vector3 forward, in Vector3 right, ref Vector3 result)
        {
            if (ent.client == null)
            {
                return;
            }
            var client = (gclient_t)ent.client!;
            // float     *point  = ent.s.origin;
            // vec3_t     _distance;


            var _distance = distance;

            if (client.pers.hand == LEFT_HANDED)
            {
                _distance[1] *= -1;
            }
            else if (client.pers.hand == CENTER_HANDED)
            {
                _distance[1] = 0;
            }

            G_ProjectSource(ent.s.origin, _distance, forward, right, ref result);

            // Berserker: fix - now the projectile hits exactly where the scope is pointing.
            // if (aimfix!.Bool)
            // {
            //     vec3_t start, end;
            //     VectorSet(start, ent->s.origin[0], ent->s.origin[1], ent->s.origin[2] + ent->viewheight);
            //     VectorMA(start, 8192, forward, end);

            //     trace_t	tr = gi.trace(start, NULL, NULL, end, ent, MASK_SHOT);
            //     if (tr.fraction < 1)
            //     {
            //         VectorSubtract(tr.endpos, result, forward);
            //         VectorNormalize(forward);
            //     }
            // }
        }

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

            client.weaponstate = weaponstate_t.WEAPON_ACTIVATING;
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
                ent.s.frame = QuakeGamePlayer.FRAME_pain301;
                client.anim_end = QuakeGamePlayer.FRAME_pain304;
            // }
        }

        /*
        * Called by ClientBeginServerFrame and ClientThink
        */
        private void Think_Weapon(edict_t ent)
        {
            if (ent == null)
            {
                return;
            }
            var client = (gclient_t)(ent.client!);

            /* if just died, put the weapon away */
            if (ent.health < 1)
            {
                client.newweapon = null;
                ChangeWeapon(ent);
            }

            /* call active weapon think routine */
            if (client.pers.weapon != null && client.pers.weapon.weaponthink != null)
            {
                var is_quad = (client.quad_framenum > level.framenum);

                // if (client.silencer_shots)
                // {
                //     is_silenced = MZ_SILENCED;
                // }
                // else
                // {
                //     is_silenced = 0;
                // }

                client.pers.weapon.weaponthink(this, ent);
            }
        }

        /*
        * A generic function to handle
        * the basics of weapon thinking
        */
        private void Weapon_Generic(edict_t ent, int FRAME_ACTIVATE_LAST, int FRAME_FIRE_LAST,
                int FRAME_IDLE_LAST, int FRAME_DEACTIVATE_LAST, int[] pause_frames,
                int[] fire_frames, edict_delegate fire)
        {
            // int n;
            int FRAME_FIRE_FIRST = (FRAME_ACTIVATE_LAST + 1);
            int FRAME_IDLE_FIRST = (FRAME_FIRE_LAST + 1);
            int FRAME_DEACTIVATE_FIRST = (FRAME_IDLE_LAST + 1);


            if (ent == null || fire_frames == null)
            {
                return;
            }

            if (ent.deadflag != 0 || (ent.s.modelindex != 255)) /* VWep animations screw up corpses */
            {
                return;
            }

            var client = (gclient_t)ent.client!;

            // if (ent->client->weaponstate == WEAPON_DROPPING)
            // {
            //     if (ent->client->ps.gunframe == FRAME_DEACTIVATE_LAST)
            //     {
            //         ChangeWeapon(ent);
            //         return;
            //     }
            //     else if ((FRAME_DEACTIVATE_LAST - ent->client->ps.gunframe) == 4)
            //     {
            //         ent->client->anim_priority = ANIM_REVERSE;

            //         if (ent->client->ps.pmove.pm_flags & PMF_DUCKED)
            //         {
            //             ent->s.frame = FRAME_crpain4 + 1;
            //             ent->client->anim_end = FRAME_crpain1;
            //         }
            //         else
            //         {
            //             ent->s.frame = FRAME_pain304 + 1;
            //             ent->client->anim_end = FRAME_pain301;
            //         }
            //     }

            //     ent->client->ps.gunframe++;
            //     return;
            // }

            if (client.weaponstate == weaponstate_t.WEAPON_ACTIVATING)
            {
                if (client.ps.gunframe == FRAME_ACTIVATE_LAST)
                {
                    client.weaponstate = weaponstate_t.WEAPON_READY;
                    client.ps.gunframe = FRAME_IDLE_FIRST;
                    return;
                }

                client.ps.gunframe++;
                return;
            }

            // if ((ent->client->newweapon) && (ent->client->weaponstate != WEAPON_FIRING))
            // {
            //     ent->client->weaponstate = WEAPON_DROPPING;
            //     ent->client->ps.gunframe = FRAME_DEACTIVATE_FIRST;

            //     if ((FRAME_DEACTIVATE_LAST - FRAME_DEACTIVATE_FIRST) < 4)
            //     {
            //         ent->client->anim_priority = ANIM_REVERSE;

            //         if (ent->client->ps.pmove.pm_flags & PMF_DUCKED)
            //         {
            //             ent->s.frame = FRAME_crpain4 + 1;
            //             ent->client->anim_end = FRAME_crpain1;
            //         }
            //         else
            //         {
            //             ent->s.frame = FRAME_pain304 + 1;
            //             ent->client->anim_end = FRAME_pain301;
            //         }
            //     }

            //     return;
            // }

            if (client.weaponstate == weaponstate_t.WEAPON_READY)
            {
                if (((client.latched_buttons |
                    client.buttons) & QShared.BUTTON_ATTACK) != 0)
                {
                    client.latched_buttons &= ~QShared.BUTTON_ATTACK;

            //         if ((!ent->client->ammo_index) ||
            //             (ent->client->pers.inventory[ent->client->ammo_index] >=
            //             ent->client->pers.weapon->quantity))
            //         {
                        client.ps.gunframe = FRAME_FIRE_FIRST;
                        client.weaponstate = weaponstate_t.WEAPON_FIRING;

                        /* start the animation */
                        client.anim_priority = ANIM_ATTACK;

            //             if (ent->client->ps.pmove.pm_flags & PMF_DUCKED)
            //             {
            //                 ent->s.frame = FRAME_crattak1 - 1;
            //                 ent->client->anim_end = FRAME_crattak9;
            //             }
            //             else
            //             {
                            ent.s.frame = QuakeGamePlayer.FRAME_attack1 - 1;
                            client.anim_end = QuakeGamePlayer.FRAME_attack8;
            //             }
            //         }
            //         else
            //         {
            //             if (level.time >= ent->pain_debounce_time)
            //             {
            //                 gi.sound(ent, CHAN_VOICE, gi.soundindex(
            //                             "weapons/noammo.wav"), 1, ATTN_NORM, 0);
            //                 ent->pain_debounce_time = level.time + 1;
            //             }

            //             NoAmmoWeaponChange(ent);
            //         }
                }
                else
                {
                    if (client.ps.gunframe == FRAME_IDLE_LAST)
                    {
                        client.ps.gunframe = FRAME_IDLE_FIRST;
                        return;
                    }

                    if (pause_frames != null)
                    {
            //             for (n = 0; pause_frames[n]; n++)
            //             {
            //                 if (ent->client->ps.gunframe == pause_frames[n])
            //                 {
            //                     if (randk() & 15)
            //                     {
            //                         return;
            //                     }
            //                 }
            //             }
                    }

                    client.ps.gunframe++;
                    return;
                }
            }

            if (client.weaponstate == weaponstate_t.WEAPON_FIRING)
            {
                int n;
                for (n = 0; fire_frames[n] != 0; n++)
                {
                    if (client.ps.gunframe == fire_frames[n])
                    {
            //             if (ent->client->quad_framenum > level.framenum)
            //             {
            //                 gi.sound(ent, CHAN_ITEM, gi.soundindex(
            //                             "items/damage3.wav"), 1, ATTN_NORM, 0);
            //             }

                        fire(ent);
                        break;
                    }
                }

                if (fire_frames[n] == 0)
                {
                    client.ps.gunframe++;
                }

                if (client.ps.gunframe == FRAME_IDLE_FIRST + 1)
                {
                    client.weaponstate = weaponstate_t.WEAPON_READY;
                }
            }
        }

        /* ====================================================================== */

        /* BLASTER / HYPERBLASTER */

        private void Blaster_Fire(edict_t ent, in Vector3 g_offset, int damage,
                bool hyper, int effect)
        {
            // vec3_t forward, right;
            // vec3_t start;
            // vec3_t offset;

            if (ent == null)
            {
                return;
            }

            // if (is_quad)
            // {
            //     damage *= 4;
            // }

            var forward = new Vector3();
            var right = new Vector3();
            var up = new Vector3();
            QShared.AngleVectors(((gclient_t)ent.client!).v_angle, ref forward, ref right, ref up);
            var offset = new Vector3(24, 8, ent.viewheight - 8);
            offset += g_offset;
            var start = new Vector3();
            P_ProjectSource(ent, offset, forward, right, ref start);

            ((gclient_t)ent.client).kick_origin = -2 * forward;
            ((gclient_t)ent.client).kick_angles[0] = -1;

            fire_blaster(ent, start, forward, damage, 1000, effect, hyper);

            // /* send muzzle flash */
            // gi.WriteByte(svc_muzzleflash);
            // gi.WriteShort(ent - g_edicts);

            // if (hyper)
            // {
            //     gi.WriteByte(MZ_HYPERBLASTER | is_silenced);
            // }
            // else
            // {
            //     gi.WriteByte(MZ_BLASTER | is_silenced);
            // }

            // gi.multicast(ent->s.origin, MULTICAST_PVS);

            // PlayerNoise(ent, start, PNOISE_WEAPON);
        }

        private void Weapon_Blaster_Fire(edict_t ent)
        {
            if (ent == null)
            {
                return;
            }

            int damage;
            if (deathmatch!.Bool)
            {
                damage = 15;
            }
            else
            {
                damage = 10;
            }

            Blaster_Fire(ent, Vector3.Zero, damage, false, QShared.EF_BLASTER);
            ent.client!.ps.gunframe++;
        }        

        private static int[] _blaster_pause_frames = {19, 32, 0};
        private static int[] _blaster_fire_frames = {5, 0};

        private static void Weapon_Blaster(QuakeGame g, edict_t ent)
        {

            if (ent == null)
            {
                return;
            }

            g.Weapon_Generic(ent, 4, 8, 52, 55, _blaster_pause_frames,
                    _blaster_fire_frames, g.Weapon_Blaster_Fire);
        }

    }
}