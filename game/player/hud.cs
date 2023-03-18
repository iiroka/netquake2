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
 * HUD, deathmatch scoreboard, help computer and intermission stuff.
 *
 * =======================================================================
 */
using System.Numerics;

namespace Quake2 {

    partial class QuakeGame
    {
        /* ======================================================================= */

        private void G_SetStats(edict_t ent)
        {
            // gitem_t *item;
            // int index, cells = 0;
            // int power_armor_type;

            if (ent == null || ent.client == null)
            {
                return;
            }

            /* health */
            ent.client.ps.stats[QShared.STAT_HEALTH_ICON] = (short)level.pic_health;
            ent.client.ps.stats[QShared.STAT_HEALTH] = (short)ent.health;
            var client = (gclient_t)ent.client;

            /* ammo */
            if (client.ammo_index == 0)
            {
                ent.client.ps.stats[QShared.STAT_AMMO_ICON] = 0;
                ent.client.ps.stats[QShared.STAT_AMMO] = 0;
            }
            else
            {
                ref var item = ref itemlist[client.ammo_index];
                // ent.client.ps.stats[QShared.STAT_AMMO_ICON] = gi.imageindex(item.icon);
                ent.client.ps.stats[QShared.STAT_AMMO] =
                    (short)client.pers.inventory[client.ammo_index];
            }

            // /* armor */
            // power_armor_type = PowerArmorType(ent);

            // if (power_armor_type)
            // {
            //     cells = ent->client->pers.inventory[ITEM_INDEX(FindItem("cells"))];

            //     if (cells == 0)
            //     {
            //         /* ran out of cells for power armor */
            //         ent->flags &= ~FL_POWER_ARMOR;
            //         gi.sound(ent, CHAN_ITEM, gi.soundindex(
            //                         "misc/power2.wav"), 1, ATTN_NORM, 0);
            //         power_armor_type = 0;
            //     }
            // }

            // index = ArmorIndex(ent);

            // if (power_armor_type && (!index || (level.framenum & 8)))
            // {
            //     /* flash between power armor and other armor icon */
            //     ent->client->ps.stats[STAT_ARMOR_ICON] = gi.imageindex("i_powershield");
            //     ent->client->ps.stats[STAT_ARMOR] = cells;
            // }
            // else if (index)
            // {
            //     item = GetItemByIndex(index);
            //     ent->client->ps.stats[STAT_ARMOR_ICON] = gi.imageindex(item->icon);
            //     ent->client->ps.stats[STAT_ARMOR] = ent->client->pers.inventory[index];
            // }
            // else
            // {
            //     ent->client->ps.stats[STAT_ARMOR_ICON] = 0;
            //     ent->client->ps.stats[STAT_ARMOR] = 0;
            // }

            // /* pickup message */
            // if (level.time > ent->client->pickup_msg_time)
            // {
            //     ent->client->ps.stats[STAT_PICKUP_ICON] = 0;
            //     ent->client->ps.stats[STAT_PICKUP_STRING] = 0;
            // }

            // /* timers */
            // if (ent->client->quad_framenum > level.framenum)
            // {
            //     ent->client->ps.stats[STAT_TIMER_ICON] = gi.imageindex("p_quad");
            //     ent->client->ps.stats[STAT_TIMER] =
            //         (ent->client->quad_framenum - level.framenum) / 10;
            // }
            // else if (ent->client->invincible_framenum > level.framenum)
            // {
            //     ent->client->ps.stats[STAT_TIMER_ICON] = gi.imageindex(
            //             "p_invulnerability");
            //     ent->client->ps.stats[STAT_TIMER] =
            //         (ent->client->invincible_framenum - level.framenum) / 10;
            // }
            // else if (ent->client->enviro_framenum > level.framenum)
            // {
            //     ent->client->ps.stats[STAT_TIMER_ICON] = gi.imageindex("p_envirosuit");
            //     ent->client->ps.stats[STAT_TIMER] =
            //         (ent->client->enviro_framenum - level.framenum) / 10;
            // }
            // else if (ent->client->breather_framenum > level.framenum)
            // {
            //     ent->client->ps.stats[STAT_TIMER_ICON] = gi.imageindex("p_rebreather");
            //     ent->client->ps.stats[STAT_TIMER] =
            //         (ent->client->breather_framenum - level.framenum) / 10;
            // }
            // else
            // {
            //     ent->client->ps.stats[STAT_TIMER_ICON] = 0;
            //     ent->client->ps.stats[STAT_TIMER] = 0;
            // }

            // /* selected item */
            // if (ent->client->pers.selected_item == -1)
            // {
            //     ent->client->ps.stats[STAT_SELECTED_ICON] = 0;
            // }
            // else
            // {
            //     ent->client->ps.stats[STAT_SELECTED_ICON] =
            //         gi.imageindex(itemlist[ent->client->pers.selected_item].icon);
            // }

            // ent->client->ps.stats[STAT_SELECTED_ITEM] = ent->client->pers.selected_item;

            // /* layouts */
            // ent->client->ps.stats[STAT_LAYOUTS] = 0;

            // if (deathmatch->value)
            // {
            //     if ((ent->client->pers.health <= 0) || level.intermissiontime ||
            //         ent->client->showscores)
            //     {
            //         ent->client->ps.stats[STAT_LAYOUTS] |= 1;
            //     }

            //     if (ent->client->showinventory && (ent->client->pers.health > 0))
            //     {
            //         ent->client->ps.stats[STAT_LAYOUTS] |= 2;
            //     }
            // }
            // else
            // {
            //     if (ent->client->showscores || ent->client->showhelp)
            //     {
            //         ent->client->ps.stats[STAT_LAYOUTS] |= 1;
            //     }

            //     if (ent->client->showinventory && (ent->client->pers.health > 0))
            //     {
            //         ent->client->ps.stats[STAT_LAYOUTS] |= 2;
            //     }
            // }

            // /* frags */
            // ent->client->ps.stats[STAT_FRAGS] = ent->client->resp.score;

            // /* help icon / current weapon if not shown */
            // if (ent->client->pers.helpchanged && (level.framenum & 8))
            // {
            //     ent->client->ps.stats[STAT_HELPICON] = gi.imageindex("i_help");
            // }
            // else if (((ent->client->pers.hand == CENTER_HANDED) ||
            //         (ent->client->ps.fov > 91)) &&
            //         ent->client->pers.weapon)
            // {
            //     cvar_t *gun;
            //     gun = gi.cvar("cl_gun", "2", 0);

            //     if (gun->value != 2)
            //     {
            //         ent->client->ps.stats[STAT_HELPICON] = gi.imageindex(
            //                 ent->client->pers.weapon->icon);
            //     }
            //     else
            //     {
            //         ent->client->ps.stats[STAT_HELPICON] = 0;
            //     }
            // }
            // else
            // {
            //     ent->client->ps.stats[STAT_HELPICON] = 0;
            // }

            ent.client.ps.stats[QShared.STAT_SPECTATOR] = 0;
        }

    }
}