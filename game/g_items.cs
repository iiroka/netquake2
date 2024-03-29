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
 * Item handling and item definitions.
 *
 * =======================================================================
 */
using System.Numerics;

namespace Quake2 {

    partial class QuakeGame
    {
        private const int HEALTH_IGNORE_MAX = 1;
        private const int HEALTH_TIMED = 2;


        private static gitem_armor_t jacketarmor_info = new gitem_armor_t(){
            base_count = 25, max_count = 50, normal_protection= .30f, energy_protection = .00f, armor = ARMOR_JACKET};
        private static gitem_armor_t combatarmor_info = new gitem_armor_t(){
            base_count = 50, max_count = 100, normal_protection= .60f, energy_protection = .30f, armor = ARMOR_COMBAT};
        private static gitem_armor_t bodyarmor_info = new gitem_armor_t(){
            base_count = 100, max_count = 200, normal_protection= .80f, energy_protection = .60f, armor = ARMOR_BODY};


        private int jacket_armor_index;
        private int combat_armor_index;
        private int body_armor_index;
        private int power_screen_index;
        private int power_shield_index;

        private gitem_t? FindItem(string pickup_name)
        {
            // int i;
            // gitem_t *it;

            if (String.IsNullOrEmpty(pickup_name))
            {
                return null;
            }

            foreach (var it in itemlist)
            {
                if (String.IsNullOrEmpty(it.pickup_name))
                {
                    continue;
                }

                if (it.pickup_name.Equals(pickup_name))
                {
                    return it;
                }
            }

            return null;
        }

        /* ====================================================================== */

        private int ArmorIndex(edict_t ent)
        {
            if (ent == null)
            {
                return 0;
            }

            if (ent.client == null)
            {
                return 0;
            }

            var client = (gclient_t)ent.client;
            if (client.pers.inventory[jacket_armor_index] > 0)
            {
                return jacket_armor_index;
            }

            if (client.pers.inventory[combat_armor_index] > 0)
            {
                return combat_armor_index;
            }

            if (client.pers.inventory[body_armor_index] > 0)
            {
                return body_armor_index;
            }

            return 0;
        }        

        private static bool Pickup_Armor(QuakeGame g, edict_t ent, edict_t other)
        {
            // int old_armor_index;
            // gitem_armor_t *oldinfo;
            // gitem_armor_t *newinfo;
            // int newcount;
            // float salvage;
            // int salvagecount;

            if (ent == null || other == null || g == null)
            {
                return false;
            }

            /* get info on new armor */
            var newinfo = (gitem_armor_t)ent.item!.info!;

            var old_armor_index = g.ArmorIndex(other);
            gclient_t oclient = (gclient_t)other.client!;

            /* handle armor shards specially */
            if (ent.item.tag == ARMOR_SHARD)
            {
                if (old_armor_index == 0)
                {
                    oclient.pers.inventory[g.jacket_armor_index] = 2;
                }
                else
                {
                    oclient.pers.inventory[old_armor_index] += 2;
                }
            }
            else if (old_armor_index == 0) /* if player has no armor, just use it */
            {
                oclient.pers.inventory[ent.item.index] = newinfo.base_count;
            }
            else /* use the better armor */
            {
                /* get info on old armor */
                gitem_armor_t oldinfo;
                if (old_armor_index == g.jacket_armor_index)
                {
                    oldinfo = jacketarmor_info;
                }
                else if (old_armor_index == g.combat_armor_index)
                {
                    oldinfo = combatarmor_info;
                }
                else
                {
                    oldinfo = bodyarmor_info;
                }

                if (newinfo!.normal_protection > oldinfo.normal_protection)
                {
                    /* calc new armor values */
                    var salvage = oldinfo.normal_protection / newinfo.normal_protection;
                    var salvagecount = (int)(salvage *
                                oclient.pers.inventory[old_armor_index]);
                    var newcount = newinfo.base_count + salvagecount;

                    if (newcount > newinfo.max_count)
                    {
                        newcount = newinfo.max_count;
                    }

                    /* zero count of old armor so it goes away */
                    oclient.pers.inventory[old_armor_index] = 0;

                    /* change armor to new item with computed value */
                    oclient.pers.inventory[ent.item.index] = newcount;
                }
                else
                {
                    /* calc new armor values */
                    var salvage = newinfo.normal_protection / oldinfo.normal_protection;
                    var salvagecount = (int)(salvage * newinfo.base_count);
                    var newcount = oclient.pers.inventory[old_armor_index] +
                            salvagecount;

                    if (newcount > oldinfo.max_count)
                    {
                        newcount = oldinfo.max_count;
                    }

                    /* if we're already maxed out then we don't need the new armor */
                    if (oclient.pers.inventory[old_armor_index] >= newcount)
                    {
                        return false;
                    }

                    /* update current armor value */
                    oclient.pers.inventory[old_armor_index] = newcount;
                }
            }

            // if (!(ent->spawnflags & DROPPED_ITEM) && (deathmatch->value))
            // {
            //     SetRespawn(ent, 20);
            // }

            return true;
        }        

        /* ====================================================================== */

        private void Touch_Item(edict_t ent, edict_t other, QShared.cplane_t? _plane /* unused */, in QShared.csurface_t? _surf /* unused */)
        {
            // qboolean taken;

            if (ent == null || other == null)
            {
                return;
            }

            if (other.client == null)
            {
                return;
            }
            var oclient = (gclient_t)other.client;

            if (other.health < 1)
            {
                return; /* dead people can't pickup */
            }

            if (ent.item!.pickup == null)
            {
                return; /* not a grabbable item? */
            }

            var taken = ent.item.pickup!(this, ent, other);

            if (taken)
            {
                /* flash the screen */
                oclient.bonus_alpha = 0.25f;

                /* show icon and name on status bar */
                oclient.ps.stats[QShared.STAT_PICKUP_ICON] = (short)gi.imageindex( ent.item.icon! );
                oclient.ps.stats[QShared.STAT_PICKUP_STRING] =
                    (short)(QShared.CS_ITEMS + ent.item.index);
                oclient.pickup_msg_time = level.time + 3.0f;

                /* change selected item */
                if (ent.item.use != null)
                {
                    oclient.pers.selected_item =
                        oclient.ps.stats[QShared.STAT_SELECTED_ITEM] =
                            (short)ent.item.index;
                }

                // if (ent.item.pickup == Pickup_Health)
                // {
                //     if (ent->count == 2)
                //     {
                //         gi.sound(other, CHAN_ITEM, gi.soundindex(
                //                         "items/s_health.wav"), 1, ATTN_NORM, 0);
                //     }
                //     else if (ent->count == 10)
                //     {
                //         gi.sound(other, CHAN_ITEM, gi.soundindex(
                //                         "items/n_health.wav"), 1, ATTN_NORM, 0);
                //     }
                //     else if (ent->count == 25)
                //     {
                //         gi.sound(other, CHAN_ITEM, gi.soundindex(
                //                         "items/l_health.wav"), 1, ATTN_NORM, 0);
                //     }
                //     else /* (ent->count == 100) */
                //     {
                //         gi.sound(other, CHAN_ITEM, gi.soundindex(
                //                         "items/m_health.wav"), 1, ATTN_NORM, 0);
                //     }
                // }
                // else if (ent->item->pickup_sound)
                // {
                //     gi.sound(other, CHAN_ITEM, gi.soundindex(
                //                     ent->item->pickup_sound), 1, ATTN_NORM, 0);
                // }

                /* activate item instantly if appropriate */
                /* moved down here so activation sounds override the pickup sound */
                // if (deathmatch->value)
                // {
                //     if ((((int)dmflags->value & DF_INSTANT_ITEMS) &&
                //         (ent->item->flags & IT_INSTANT_USE)) ||
                //         ((ent->item->use == Use_Quad) &&
                //         (ent->spawnflags & DROPPED_PLAYER_ITEM)))
                //     {
                //         if ((ent->item->use == Use_Quad) &&
                //             (ent->spawnflags & DROPPED_PLAYER_ITEM))
                //         {
                //             quad_drop_timeout_hack =
                //                 (ent->nextthink - level.time) / FRAMETIME;
                //         }

                //         if (ent->item->use)
                //         {
                //             ent->item->use(other, ent->item);
                //         }
                //     }
                // }
            }

            // if ((ent.spawnflags & ITEM_TARGETS_USED) == 0)
            // {
                // G_UseTargets(ent, other);
            //     ent->spawnflags |= ITEM_TARGETS_USED;
            // }

            if (!taken)
            {
                return;
            }

            // if (!(coop!.Bool &&
            //     (ent.item.flags & IT_STAY_COOP) != 0) ||
            //     (ent.spawnflags & (DROPPED_ITEM | DROPPED_PLAYER_ITEM)) != 0)
            // {
            //     if (ent->flags & FL_RESPAWN)
            //     {
            //         ent->flags &= ~FL_RESPAWN;
            //     }
            //     else
            //     {
                    G_FreeEdict(ent);
            //     }
            // }
        }

        private void Use_Item(edict_t ent, edict_t _other /* unused */, edict_t? _activator /* unused */)
        {
            if (ent == null)
            {
                return;
            }

            ent.svflags &= ~QGameFlags.SVF_NOCLIENT;
            ent.use = null;

            if ((ent.spawnflags & ITEM_NO_TOUCH) != 0)
            {
                ent.solid = solid_t.SOLID_BBOX;
                ent.touch = null;
            }
            else
            {
                ent.solid = solid_t.SOLID_TRIGGER;
                ent.touch = Touch_Item;
            }

            gi.linkentity(ent);
        }

        /* ====================================================================== */

        private void droptofloor(edict_t ent)
        {
            // trace_t tr;
            // vec3_t dest;
            // float *v;

            if (ent == null)
            {
                return;
            }

            ent.mins = new Vector3(-15, -15, -15);
            ent.maxs = new Vector3(15, 15, 15);

            if (!String.IsNullOrEmpty(ent.model))
            {
                gi.setmodel(ent, ent.model);
            }
            else
            {
                gi.setmodel(ent, ent.item!.world_model!);
            }

            ent.solid = solid_t.SOLID_TRIGGER;
            ent.movetype = movetype_t.MOVETYPE_TOSS;
            ent.touch = Touch_Item;

            var dest = ent.s.origin + new Vector3(0,0,-128);

            var tr = gi.trace(ent.s.origin, ent.mins, ent.maxs, dest, ent, QShared.MASK_SOLID);

            if (tr.startsolid)
            {
                gi.dprintf($"droptofloor: {ent.classname} startsolid at {ent.s.origin}\n");
                G_FreeEdict(ent);
                return;
            }

            ent.s.origin = tr.endpos;

            // if (ent->team)
            // {
            //     ent->flags &= ~FL_TEAMSLAVE;
            //     ent->chain = ent->teamchain;
            //     ent->teamchain = NULL;

            //     ent->svflags |= SVF_NOCLIENT;
            //     ent->solid = SOLID_NOT;

            //     if (ent == ent->teammaster)
            //     {
            //         ent->nextthink = level.time + FRAMETIME;
            //         ent->think = DoRespawn;
            //     }
            // }

            if ((ent.spawnflags & ITEM_NO_TOUCH) != 0)
            {
                ent.solid = solid_t.SOLID_BBOX;
                ent.touch = null;
                ent.s.effects &= uint.MaxValue ^ QShared.EF_ROTATE;
                ent.s.renderfx &= ~QShared.RF_GLOW;
            }

            if ((ent.spawnflags & ITEM_TRIGGER_SPAWN) != 0)
            {
                ent.svflags |= QGameFlags.SVF_NOCLIENT;
                ent.solid = solid_t.SOLID_NOT;
                ent.use = Use_Item;
            }

            gi.linkentity(ent);
        }

        /*
        * ============
        * Sets the clipping size and
        * plants the object on the floor.
        *
        * Items can't be immediately dropped
        * to floor, because they might be on
        * an entity that hasn't spawned yet.
        * ============
        */
        private void SpawnItem(edict_t ent, gitem_t item)
        {
            if (ent == null || item == null)
            {
                return;
            }

            // PrecacheItem(item);

            // if (ent->spawnflags)
            // {
            //     if (strcmp(ent->classname, "key_power_cube") != 0)
            //     {
            //         ent->spawnflags = 0;
            //         gi.dprintf("%s at %s has invalid spawnflags set\n",
            //                 ent->classname, vtos(ent->s.origin));
            //     }
            // }

            // /* some items will be prevented in deathmatch */
            // if (deathmatch->value)
            // {
            //     if ((int)dmflags->value & DF_NO_ARMOR)
            //     {
            //         if ((item->pickup == Pickup_Armor) ||
            //             (item->pickup == Pickup_PowerArmor))
            //         {
            //             G_FreeEdict(ent);
            //             return;
            //         }
            //     }

            //     if ((int)dmflags->value & DF_NO_ITEMS)
            //     {
            //         if (item->pickup == Pickup_Powerup)
            //         {
            //             G_FreeEdict(ent);
            //             return;
            //         }
            //     }

            //     if ((int)dmflags->value & DF_NO_HEALTH)
            //     {
            //         if ((item->pickup == Pickup_Health) ||
            //             (item->pickup == Pickup_Adrenaline) ||
            //             (item->pickup == Pickup_AncientHead))
            //         {
            //             G_FreeEdict(ent);
            //             return;
            //         }
            //     }

            //     if ((int)dmflags->value & DF_INFINITE_AMMO)
            //     {
            //         if ((item->flags == IT_AMMO) ||
            //             (strcmp(ent->classname, "weapon_bfg") == 0))
            //         {
            //             G_FreeEdict(ent);
            //             return;
            //         }
            //     }
            // }

            // if (coop->value && (strcmp(ent->classname, "key_power_cube") == 0))
            // {
            //     ent->spawnflags |= (1 << (8 + level.power_cubes));
            //     level.power_cubes++;
            // }

            // /* don't let them drop items that stay in a coop game */
            // if ((coop->value) && (item->flags & IT_STAY_COOP))
            // {
            //     item->drop = NULL;
            // }

            ent.item = item;
            ent.nextthink = level.time + 2 * FRAMETIME; /* items start after other solids */
            ent.think = droptofloor;
            ent.s.effects = (uint)item.world_model_flags;
            ent.s.renderfx = QShared.RF_GLOW;

            if (ent.model != null)
            {
                gi.modelindex(ent.model);
            }
        }        

        /* ====================================================================== */

        private static readonly gitem_t[] gameitemlist = new gitem_t[]{
            new gitem_t(){
            }, /* leave index 0 alone */

            /* QUAKED item_armor_body (.3 .3 1) (-16 -16 -16) (16 16 16) */
            new gitem_t(){
                classname = "item_armor_body",
                pickup = Pickup_Armor,
                use = null,
                drop = null,
                weaponthink = null,
                // "misc/ar1_pkup.wav",
                world_model = "models/items/armor/body/tris.md2", 
                world_model_flags = QShared.EF_ROTATE,
                icon = "i_bodyarmor",
                pickup_name = "Body Armor",
                count_width = 3,
                // 0,
                // NULL,
                // IT_ARMOR,
                // 0,
                // &bodyarmor_info,
                tag = ARMOR_BODY,
                // ""
            },

            /* QUAKED item_armor_combat (.3 .3 1) (-16 -16 -16) (16 16 16) */
            new gitem_t(){
                classname = "item_armor_combat",
                pickup = Pickup_Armor,
                use = null,
                drop = null,
                weaponthink = null,
                // "misc/ar1_pkup.wav",
                world_model = "models/items/armor/combat/tris.md2", 
                world_model_flags = QShared.EF_ROTATE,
                icon = "i_combatarmor",
                pickup_name = "Combat Armor",
                count_width = 3,
                // 0,
                // NULL,
                // IT_ARMOR,
                // 0,
                // &combatarmor_info,
                tag = ARMOR_COMBAT,
                // ""
            },

            /* QUAKED item_armor_jacket (.3 .3 1) (-16 -16 -16) (16 16 16) */
            new gitem_t(){
                classname = "item_armor_jacket",
                pickup = Pickup_Armor,
                use = null,
                drop = null,
                weaponthink = null,
                // "misc/ar1_pkup.wav",
                world_model = "models/items/armor/jacket/tris.md2", 
                world_model_flags = QShared.EF_ROTATE,
                icon = "i_jacketarmor",
                pickup_name = "Jacket Armor",
                count_width = 3,
                // 0,
                // NULL,
                // IT_ARMOR,
                // 0,
                // &jacketarmor_info,
                tag = ARMOR_JACKET,
                // ""
            },

            /* QUAKED item_armor_shard (.3 .3 1) (-16 -16 -16) (16 16 16) */
            new gitem_t(){
                classname = "item_armor_shard",
                pickup = Pickup_Armor,
                use = null,
                drop = null,
                weaponthink = null,
                // "misc/ar2_pkup.wav",
                world_model = "models/items/armor/shard/tris.md2", 
                world_model_flags = QShared.EF_ROTATE,
                icon = "i_jacketarmor",
                pickup_name = "Armor Shard",
                count_width = 3,
                // 0,
                // NULL,
                // IT_ARMOR,
                // 0,
                // NULL,
                tag = ARMOR_SHARD,
                // ""
            },

            /* QUAKED item_power_screen (.3 .3 1) (-16 -16 -16) (16 16 16) */
            new gitem_t(){
                classname = "item_power_screen",
                // Pickup_PowerArmor,
                // Use_PowerArmor,
                // Drop_PowerArmor,
                weaponthink = null,
                // "misc/ar3_pkup.wav",
                world_model = "models/items/armor/screen/tris.md2", 
                world_model_flags = QShared.EF_ROTATE,
                icon = "i_powerscreen",
                pickup_name = "Power Screen",
                count_width = 0,
                // 60,
                // NULL,
                // IT_ARMOR,
                // 0,
                // NULL,
                // 0,
                // ""
            },

            /* QUAKED item_power_shield (.3 .3 1) (-16 -16 -16) (16 16 16) */
            new gitem_t(){
                classname = "item_power_shield",
                // Pickup_PowerArmor,
                // Use_PowerArmor,
                // Drop_PowerArmor,
                weaponthink = null,
                // "misc/ar3_pkup.wav",
                world_model = "models/items/armor/shield/tris.md2", 
                world_model_flags = QShared.EF_ROTATE,
                icon = "i_powershield",
                pickup_name = "Power Shield",
                count_width = 0,
                // 60,
                // NULL,
                // IT_ARMOR,
                // 0,
                // NULL,
                // 0,
                // "misc/power2.wav misc/power1.wav"
            },

            /* weapon_blaster (.3 .3 1) (-16 -16 -16) (16 16 16)
            always owned, never in the world */
            new gitem_t(){
                classname = "weapon_blaster",
                pickup = null,
                // Use_Weapon,
                drop = null,
                weaponthink = Weapon_Blaster,
                // "misc/w_pkup.wav",
                world_model = null, 
                world_model_flags = 0,
                view_model = "models/weapons/v_blast/tris.md2",
                icon = "w_blaster",
                pickup_name = "Blaster",
                count_width = 0,
                // 0,
                // NULL,
                // IT_WEAPON | IT_STAY_COOP,
                // WEAP_BLASTER,
                // NULL,
                // 0,
                // "weapons/blastf1a.wav misc/lasfly.wav"
            },

            /* QUAKED weapon_shotgun (.3 .3 1) (-16 -16 -16) (16 16 16) */
            new gitem_t(){
                classname = "weapon_shotgun",
                // Pickup_Weapon,
                // Use_Weapon,
                // Drop_Weapon,
                // Weapon_Shotgun,
                // "misc/w_pkup.wav",
                world_model = "models/weapons/g_shotg/tris.md2", 
                world_model_flags = QShared.EF_ROTATE,
                view_model = "models/weapons/v_shotg/tris.md2",
                icon = "w_shotgun",
                pickup_name = "Shotgun",
                count_width = 0,
                // 1,
                // "Shells",
                // IT_WEAPON | IT_STAY_COOP,
                // WEAP_SHOTGUN,
                // NULL,
                // 0,
                // "weapons/shotgf1b.wav weapons/shotgr1b.wav"
            },

            /* QUAKED weapon_supershotgun (.3 .3 1) (-16 -16 -16) (16 16 16) */
            new gitem_t(){
                classname = "weapon_supershotgun",
                // Pickup_Weapon,
                // Use_Weapon,
                // Drop_Weapon,
                // Weapon_SuperShotgun,
                // "misc/w_pkup.wav",
                world_model = "models/weapons/g_shotg2/tris.md2", 
                world_model_flags = QShared.EF_ROTATE,
                view_model = "models/weapons/v_shotg2/tris.md2",
                icon = "w_sshotgun",
                pickup_name = "Super Shotgun",
                count_width = 0,
                // 2,
                // "Shells",
                // IT_WEAPON | IT_STAY_COOP,
                // WEAP_SUPERSHOTGUN,
                // NULL,
                // 0,
                // "weapons/sshotf1b.wav"
            },

            /* QUAKED weapon_machinegun (.3 .3 1) (-16 -16 -16) (16 16 16) */
            new gitem_t(){
                classname = "weapon_machinegun",
                // Pickup_Weapon,
                // Use_Weapon,
                // Drop_Weapon,
                // Weapon_Machinegun,
                // "misc/w_pkup.wav",
                world_model = "models/weapons/g_machn/tris.md2", 
                world_model_flags = QShared.EF_ROTATE,
                view_model = "models/weapons/v_machn/tris.md2",
                icon = "w_machinegun",
                pickup_name = "Machinegun",
                count_width = 0,
                // 1,
                // "Bullets",
                // IT_WEAPON | IT_STAY_COOP,
                // WEAP_MACHINEGUN,
                // NULL,
                // 0,
                // "weapons/machgf1b.wav weapons/machgf2b.wav weapons/machgf3b.wav weapons/machgf4b.wav weapons/machgf5b.wav"
            },

            /* QUAKED weapon_chaingun (.3 .3 1) (-16 -16 -16) (16 16 16) */
            new gitem_t(){
                classname = "weapon_chaingun",
                // Pickup_Weapon,
                // Use_Weapon,
                // Drop_Weapon,
                // Weapon_Chaingun,
                // "misc/w_pkup.wav",
                world_model = "models/weapons/g_chain/tris.md2", 
                world_model_flags = QShared.EF_ROTATE,
                view_model = "models/weapons/v_chain/tris.md2",
                icon = "w_chaingun",
                pickup_name = "Chaingun",
                count_width = 0,
                // 1,
                // "Bullets",
                // IT_WEAPON | IT_STAY_COOP,
                // WEAP_CHAINGUN,
                // NULL,
                // 0,
                // "weapons/chngnu1a.wav weapons/chngnl1a.wav weapons/machgf3b.wav` weapons/chngnd1a.wav"
            },

            /* QUAKED ammo_grenades (.3 .3 1) (-16 -16 -16) (16 16 16) */
            new gitem_t(){
                classname = "ammo_grenades",
                // Pickup_Ammo,
                // Use_Weapon,
                // Drop_Ammo,
                // Weapon_Grenade,
                // "misc/am_pkup.wav",
                world_model = "models/items/ammo/grenades/medium/tris.md2", 
                world_model_flags = 0,
                view_model = "models/weapons/v_handgr/tris.md2",
                icon = "a_grenades",
                pickup_name = "Grenades",
                count_width = 3,
                // 5,
                // "grenades",
                // IT_AMMO | IT_WEAPON,
                // WEAP_GRENADES,
                // NULL,
                // AMMO_GRENADES,
                // "weapons/hgrent1a.wav weapons/hgrena1b.wav weapons/hgrenc1b.wav weapons/hgrenb1a.wav weapons/hgrenb2a.wav "
            },

            /* QUAKED weapon_grenadelauncher (.3 .3 1) (-16 -16 -16) (16 16 16) */
            new gitem_t(){
                classname = "weapon_grenadelauncher",
                // Pickup_Weapon,
                // Use_Weapon,
                // Drop_Weapon,
                // Weapon_GrenadeLauncher,
                // "misc/w_pkup.wav",
                world_model = "models/weapons/g_launch/tris.md2", 
                world_model_flags = QShared.EF_ROTATE,
                view_model = "models/weapons/v_launch/tris.md2",
                icon = "w_glauncher",
                pickup_name = "Grenade Launcher",
                count_width = 0,
                // 1,
                // "Grenades",
                // IT_WEAPON | IT_STAY_COOP,
                // WEAP_GRENADELAUNCHER,
                // NULL,
                // 0,
                // "models/objects/grenade/tris.md2 weapons/grenlf1a.wav weapons/grenlr1b.wav weapons/grenlb1b.wav"
            },

            /* QUAKED weapon_rocketlauncher (.3 .3 1) (-16 -16 -16) (16 16 16) */
            new gitem_t(){
                classname = "weapon_rocketlauncher",
                // Pickup_Weapon,
                // Use_Weapon,
                // Drop_Weapon,
                // Weapon_RocketLauncher,
                // "misc/w_pkup.wav",
                world_model = "models/weapons/g_rocket/tris.md2", 
                world_model_flags = QShared.EF_ROTATE,
                view_model = "models/weapons/v_rocket/tris.md2",
                icon = "w_rlauncher",
                pickup_name = "Rocket Launcher",
                count_width = 0,
                // 1,
                // "Rockets",
                // IT_WEAPON | IT_STAY_COOP,
                // WEAP_ROCKETLAUNCHER,
                // NULL,
                // 0,
                // "models/objects/rocket/tris.md2 weapons/rockfly.wav weapons/rocklf1a.wav weapons/rocklr1b.wav models/objects/debris2/tris.md2"
            },

            /* QUAKED weapon_hyperblaster (.3 .3 1) (-16 -16 -16) (16 16 16) */
            // {
            //     "weapon_hyperblaster",
            //     Pickup_Weapon,
            //     Use_Weapon,
            //     Drop_Weapon,
            //     Weapon_HyperBlaster,
            //     "misc/w_pkup.wav",
            //     "models/weapons/g_hyperb/tris.md2", EF_ROTATE,
            //     "models/weapons/v_hyperb/tris.md2",
            //     "w_hyperblaster",
            //     "HyperBlaster",
            //     0,
            //     1,
            //     "Cells",
            //     IT_WEAPON | IT_STAY_COOP,
            //     WEAP_HYPERBLASTER,
            //     NULL,
            //     0,
            //     "weapons/hyprbu1a.wav weapons/hyprbl1a.wav weapons/hyprbf1a.wav weapons/hyprbd1a.wav misc/lasfly.wav"
            // },

            // /* QUAKED weapon_railgun (.3 .3 1) (-16 -16 -16) (16 16 16) */
            // {
            //     "weapon_railgun",
            //     Pickup_Weapon,
            //     Use_Weapon,
            //     Drop_Weapon,
            //     Weapon_Railgun,
            //     "misc/w_pkup.wav",
            //     "models/weapons/g_rail/tris.md2", EF_ROTATE,
            //     "models/weapons/v_rail/tris.md2",
            //     "w_railgun",
            //     "Railgun",
            //     0,
            //     1,
            //     "Slugs",
            //     IT_WEAPON | IT_STAY_COOP,
            //     WEAP_RAILGUN,
            //     NULL,
            //     0,
            //     "weapons/rg_hum.wav"
            // },

            // /* QUAKED weapon_bfg (.3 .3 1) (-16 -16 -16) (16 16 16) */
            // {
            //     "weapon_bfg",
            //     Pickup_Weapon,
            //     Use_Weapon,
            //     Drop_Weapon,
            //     Weapon_BFG,
            //     "misc/w_pkup.wav",
            //     "models/weapons/g_bfg/tris.md2", EF_ROTATE,
            //     "models/weapons/v_bfg/tris.md2",
            //     "w_bfg",
            //     "BFG10K",
            //     0,
            //     50,
            //     "Cells",
            //     IT_WEAPON | IT_STAY_COOP,
            //     WEAP_BFG,
            //     NULL,
            //     0,
            //     "sprites/s_bfg1.sp2 sprites/s_bfg2.sp2 sprites/s_bfg3.sp2 weapons/bfg__f1y.wav weapons/bfg__l1a.wav weapons/bfg__x1b.wav weapons/bfg_hum.wav"
            // },

            // /* QUAKED ammo_shells (.3 .3 1) (-16 -16 -16) (16 16 16) */
            // {
            //     "ammo_shells",
            //     Pickup_Ammo,
            //     NULL,
            //     Drop_Ammo,
            //     NULL,
            //     "misc/am_pkup.wav",
            //     "models/items/ammo/shells/medium/tris.md2", 0,
            //     NULL,
            //     "a_shells",
            //     "Shells",
            //     3,
            //     10,
            //     NULL,
            //     IT_AMMO,
            //     0,
            //     NULL,
            //     AMMO_SHELLS,
            //     ""
            // },

            // /* QUAKED ammo_bullets (.3 .3 1) (-16 -16 -16) (16 16 16) */
            // {
            //     "ammo_bullets",
            //     Pickup_Ammo,
            //     NULL,
            //     Drop_Ammo,
            //     NULL,
            //     "misc/am_pkup.wav",
            //     "models/items/ammo/bullets/medium/tris.md2", 0,
            //     NULL,
            //     "a_bullets",
            //     "Bullets",
            //     3,
            //     50,
            //     NULL,
            //     IT_AMMO,
            //     0,
            //     NULL,
            //     AMMO_BULLETS,
            //     ""
            // },

            // /* QUAKED ammo_cells (.3 .3 1) (-16 -16 -16) (16 16 16) */
            // {
            //     "ammo_cells",
            //     Pickup_Ammo,
            //     NULL,
            //     Drop_Ammo,
            //     NULL,
            //     "misc/am_pkup.wav",
            //     "models/items/ammo/cells/medium/tris.md2", 0,
            //     NULL,
            //     "a_cells",
            //     "Cells",
            //     3,
            //     50,
            //     NULL,
            //     IT_AMMO,
            //     0,
            //     NULL,
            //     AMMO_CELLS,
            //     ""
            // },

            // /* QUAKED ammo_rockets (.3 .3 1) (-16 -16 -16) (16 16 16) */
            // {
            //     "ammo_rockets",
            //     Pickup_Ammo,
            //     NULL,
            //     Drop_Ammo,
            //     NULL,
            //     "misc/am_pkup.wav",
            //     "models/items/ammo/rockets/medium/tris.md2", 0,
            //     NULL,
            //     "a_rockets",
            //     "Rockets",
            //     3,
            //     5,
            //     NULL,
            //     IT_AMMO,
            //     0,
            //     NULL,
            //     AMMO_ROCKETS,
            //     ""
            // },

            // /* QUAKED ammo_slugs (.3 .3 1) (-16 -16 -16) (16 16 16) */
            // {
            //     "ammo_slugs",
            //     Pickup_Ammo,
            //     NULL,
            //     Drop_Ammo,
            //     NULL,
            //     "misc/am_pkup.wav",
            //     "models/items/ammo/slugs/medium/tris.md2", 0,
            //     NULL,
            //     "a_slugs",
            //     "Slugs",
            //     3,
            //     10,
            //     NULL,
            //     IT_AMMO,
            //     0,
            //     NULL,
            //     AMMO_SLUGS,
            //     ""
            // },

            // /* QUAKED item_quad (.3 .3 1) (-16 -16 -16) (16 16 16) */
            // {
            //     "item_quad",
            //     Pickup_Powerup,
            //     Use_Quad,
            //     Drop_General,
            //     NULL,
            //     "items/pkup.wav",
            //     "models/items/quaddama/tris.md2", EF_ROTATE,
            //     NULL,
            //     "p_quad",
            //     "Quad Damage",
            //     2,
            //     60,
            //     NULL,
            //     IT_POWERUP | IT_INSTANT_USE,
            //     0,
            //     NULL,
            //     0,
            //     "items/damage.wav items/damage2.wav items/damage3.wav"
            // },

            // /* QUAKED item_invulnerability (.3 .3 1) (-16 -16 -16) (16 16 16) */
            // {
            //     "item_invulnerability",
            //     Pickup_Powerup,
            //     Use_Invulnerability,
            //     Drop_General,
            //     NULL,
            //     "items/pkup.wav",
            //     "models/items/invulner/tris.md2", EF_ROTATE,
            //     NULL,
            //     "p_invulnerability",
            //     "Invulnerability",
            //     2,
            //     300,
            //     NULL,
            //     IT_POWERUP | IT_INSTANT_USE,
            //     0,
            //     NULL,
            //     0,
            //     "items/protect.wav items/protect2.wav items/protect4.wav"
            // },

            // /* QUAKED item_silencer (.3 .3 1) (-16 -16 -16) (16 16 16) */
            // {
            //     "item_silencer",
            //     Pickup_Powerup,
            //     Use_Silencer,
            //     Drop_General,
            //     NULL,
            //     "items/pkup.wav",
            //     "models/items/silencer/tris.md2", EF_ROTATE,
            //     NULL,
            //     "p_silencer",
            //     "Silencer",
            //     2,
            //     60,
            //     NULL,
            //     IT_POWERUP | IT_INSTANT_USE,
            //     0,
            //     NULL,
            //     0,
            //     ""
            // },

            // /* QUAKED item_breather (.3 .3 1) (-16 -16 -16) (16 16 16) */
            // {
            //     "item_breather",
            //     Pickup_Powerup,
            //     Use_Breather,
            //     Drop_General,
            //     NULL,
            //     "items/pkup.wav",
            //     "models/items/breather/tris.md2", EF_ROTATE,
            //     NULL,
            //     "p_rebreather",
            //     "Rebreather",
            //     2,
            //     60,
            //     NULL,
            //     IT_STAY_COOP | IT_POWERUP | IT_INSTANT_USE,
            //     0,
            //     NULL,
            //     0,
            //     "items/airout.wav"
            // },

            // /* QUAKED item_enviro (.3 .3 1) (-16 -16 -16) (16 16 16) */
            // {
            //     "item_enviro",
            //     Pickup_Powerup,
            //     Use_Envirosuit,
            //     Drop_General,
            //     NULL,
            //     "items/pkup.wav",
            //     "models/items/enviro/tris.md2", EF_ROTATE,
            //     NULL,
            //     "p_envirosuit",
            //     "Environment Suit",
            //     2,
            //     60,
            //     NULL,
            //     IT_STAY_COOP | IT_POWERUP | IT_INSTANT_USE,
            //     0,
            //     NULL,
            //     0,
            //     "items/airout.wav"
            // },

            // /* QUAKED item_ancient_head (.3 .3 1) (-16 -16 -16) (16 16 16)
            // Special item that gives +2 to maximum health */
            // {
            //     "item_ancient_head",
            //     Pickup_AncientHead,
            //     NULL,
            //     NULL,
            //     NULL,
            //     "items/pkup.wav",
            //     "models/items/c_head/tris.md2", EF_ROTATE,
            //     NULL,
            //     "i_fixme",
            //     "Ancient Head",
            //     2,
            //     60,
            //     NULL,
            //     0,
            //     0,
            //     NULL,
            //     0,
            //     ""
            // },

            // /* QUAKED item_adrenaline (.3 .3 1) (-16 -16 -16) (16 16 16)
            // gives +1 to maximum health */
            // {
            //     "item_adrenaline",
            //     Pickup_Adrenaline,
            //     NULL,
            //     NULL,
            //     NULL,
            //     "items/pkup.wav",
            //     "models/items/adrenal/tris.md2", EF_ROTATE,
            //     NULL,
            //     "p_adrenaline",
            //     "Adrenaline",
            //     2,
            //     60,
            //     NULL,
            //     0,
            //     0,
            //     NULL,
            //     0,
            //     ""
            // },

            // /* QUAKED item_bandolier (.3 .3 1) (-16 -16 -16) (16 16 16) */
            // {
            //     "item_bandolier",
            //     Pickup_Bandolier,
            //     NULL,
            //     NULL,
            //     NULL,
            //     "items/pkup.wav",
            //     "models/items/band/tris.md2", EF_ROTATE,
            //     NULL,
            //     "p_bandolier",
            //     "Bandolier",
            //     2,
            //     60,
            //     NULL,
            //     0,
            //     0,
            //     NULL,
            //     0,
            //     ""
            // },

            // /* QUAKED item_pack (.3 .3 1) (-16 -16 -16) (16 16 16) */
            // {
            //     "item_pack",
            //     Pickup_Pack,
            //     NULL,
            //     NULL,
            //     NULL,
            //     "items/pkup.wav",
            //     "models/items/pack/tris.md2", EF_ROTATE,
            //     NULL,
            //     "i_pack",
            //     "Ammo Pack",
            //     2,
            //     180,
            //     NULL,
            //     0,
            //     0,
            //     NULL,
            //     0,
            //     ""
            // },

            // /* QUAKED key_data_cd (0 .5 .8) (-16 -16 -16) (16 16 16)
            // key for computer centers */
            // {
            //     "key_data_cd",
            //     Pickup_Key,
            //     NULL,
            //     Drop_General,
            //     NULL,
            //     "items/pkup.wav",
            //     "models/items/keys/data_cd/tris.md2", EF_ROTATE,
            //     NULL,
            //     "k_datacd",
            //     "Data CD",
            //     2,
            //     0,
            //     NULL,
            //     IT_STAY_COOP | IT_KEY,
            //     0,
            //     NULL,
            //     0,
            //     ""
            // },

            // /* QUAKED key_power_cube (0 .5 .8) (-16 -16 -16) (16 16 16) TRIGGER_SPAWN NO_TOUCH
            // warehouse circuits */
            // {
            //     "key_power_cube",
            //     Pickup_Key,
            //     NULL,
            //     Drop_General,
            //     NULL,
            //     "items/pkup.wav",
            //     "models/items/keys/power/tris.md2", EF_ROTATE,
            //     NULL,
            //     "k_powercube",
            //     "Power Cube",
            //     2,
            //     0,
            //     NULL,
            //     IT_STAY_COOP | IT_KEY,
            //     0,
            //     NULL,
            //     0,
            //     ""
            // },

            // /* QUAKED key_pyramid (0 .5 .8) (-16 -16 -16) (16 16 16)
            // key for the entrance of jail3 */
            // {
            //     "key_pyramid",
            //     Pickup_Key,
            //     NULL,
            //     Drop_General,
            //     NULL,
            //     "items/pkup.wav",
            //     "models/items/keys/pyramid/tris.md2", EF_ROTATE,
            //     NULL,
            //     "k_pyramid",
            //     "Pyramid Key",
            //     2,
            //     0,
            //     NULL,
            //     IT_STAY_COOP | IT_KEY,
            //     0,
            //     NULL,
            //     0,
            //     ""
            // },

            // /* QUAKED key_data_spinner (0 .5 .8) (-16 -16 -16) (16 16 16)
            // key for the city computer */
            // {
            //     "key_data_spinner",
            //     Pickup_Key,
            //     NULL,
            //     Drop_General,
            //     NULL,
            //     "items/pkup.wav",
            //     "models/items/keys/spinner/tris.md2", EF_ROTATE,
            //     NULL,
            //     "k_dataspin",
            //     "Data Spinner",
            //     2,
            //     0,
            //     NULL,
            //     IT_STAY_COOP | IT_KEY,
            //     0,
            //     NULL,
            //     0,
            //     ""
            // },

            // /* QUAKED key_pass (0 .5 .8) (-16 -16 -16) (16 16 16)
            // security pass for the security level */
            // {
            //     "key_pass",
            //     Pickup_Key,
            //     NULL,
            //     Drop_General,
            //     NULL,
            //     "items/pkup.wav",
            //     "models/items/keys/pass/tris.md2", EF_ROTATE,
            //     NULL,
            //     "k_security",
            //     "Security Pass",
            //     2,
            //     0,
            //     NULL,
            //     IT_STAY_COOP | IT_KEY,
            //     0,
            //     NULL,
            //     0,
            //     ""
            // },

            // /* QUAKED key_blue_key (0 .5 .8) (-16 -16 -16) (16 16 16)
            // normal door key - blue */
            // {
            //     "key_blue_key",
            //     Pickup_Key,
            //     NULL,
            //     Drop_General,
            //     NULL,
            //     "items/pkup.wav",
            //     "models/items/keys/key/tris.md2", EF_ROTATE,
            //     NULL,
            //     "k_bluekey",
            //     "Blue Key",
            //     2,
            //     0,
            //     NULL,
            //     IT_STAY_COOP | IT_KEY,
            //     0,
            //     NULL,
            //     0,
            //     ""
            // },

            // /* QUAKED key_red_key (0 .5 .8) (-16 -16 -16) (16 16 16)
            // normal door key - red */
            // {
            //     "key_red_key",
            //     Pickup_Key,
            //     NULL,
            //     Drop_General,
            //     NULL,
            //     "items/pkup.wav",
            //     "models/items/keys/red_key/tris.md2", EF_ROTATE,
            //     NULL,
            //     "k_redkey",
            //     "Red Key",
            //     2,
            //     0,
            //     NULL,
            //     IT_STAY_COOP | IT_KEY,
            //     0,
            //     NULL,
            //     0,
            //     ""
            // },

            // /* QUAKED key_commander_head (0 .5 .8) (-16 -16 -16) (16 16 16)
            // tank commander's head */
            // {
            //     "key_commander_head",
            //     Pickup_Key,
            //     NULL,
            //     Drop_General,
            //     NULL,
            //     "items/pkup.wav",
            //     "models/monsters/commandr/head/tris.md2", EF_GIB,
            //     NULL,
            //     "k_comhead",
            //     "Commander's Head",
            //     2,
            //     0,
            //     NULL,
            //     IT_STAY_COOP | IT_KEY,
            //     0,
            //     NULL,
            //     0,
            //     ""
            // },

            // /* QUAKED key_airstrike_target (0 .5 .8) (-16 -16 -16) (16 16 16) */
            // {
            //     "key_airstrike_target",
            //     Pickup_Key,
            //     NULL,
            //     Drop_General,
            //     NULL,
            //     "items/pkup.wav",
            //     "models/items/keys/target/tris.md2", EF_ROTATE,
            //     NULL,
            //     "i_airstrike",
            //     "Airstrike Marker",
            //     2,
            //     0,
            //     NULL,
            //     IT_STAY_COOP | IT_KEY,
            //     0,
            //     NULL,
            //     0,
            //     ""
            // },

            // {
            //     NULL,
            //     Pickup_Health,
            //     NULL,
            //     NULL,
            //     NULL,
            //     "items/pkup.wav",
            //     NULL, 0,
            //     NULL,
            //     "i_health",
            //     "Health",
            //     3,
            //     0,
            //     NULL,
            //     0,
            //     0,
            //     NULL,
            //     0,
            //     "items/s_health.wav items/n_health.wav items/l_health.wav items/m_health.wav"
            // },

            // /* end of list marker */
            // {NULL}
        };  

        private gitem_t[] itemlist = new gitem_t[0];

        /*
        * QUAKED item_health (.3 .3 1) (-16 -16 -16) (16 16 16)
        */
        private static void SP_item_health(QuakeGame g, edict_t self)
        {
            if (self == null || g == null)
            {
                return;
            }

            if (g.deathmatch!.Bool && (g.dmflags!.Int & QShared.DF_NO_HEALTH) != 0)
            {
                g.G_FreeEdict(self);
                return;
            }

            self.model = "models/items/healing/medium/tris.md2";
            self.count = 10;
            g.SpawnItem(self, g.FindItem("Health")!);
            // gi.soundindex("items/n_health.wav");
        }

        /*
        * QUAKED item_health_small (.3 .3 1) (-16 -16 -16) (16 16 16)
        */
        private static void SP_item_health_small(QuakeGame g, edict_t self)
        {
            if (self == null || g == null)
            {
                return;
            }

            if (g.deathmatch!.Bool && (g.dmflags!.Int & QShared.DF_NO_HEALTH) != 0)
            {
                g.G_FreeEdict(self);
                return;
            }

            self.model = "models/items/healing/stimpack/tris.md2";
            self.count = 2;
            g.SpawnItem(self, g.FindItem("Health")!);
            self.style = HEALTH_IGNORE_MAX;
            // gi.soundindex("items/s_health.wav");
        }

        /*
        * QUAKED item_health_large (.3 .3 1) (-16 -16 -16) (16 16 16)
        */
        private static void SP_item_health_large(QuakeGame g, edict_t self)
        {
            if (self == null || g == null)
            {
                return;
            }

            if (g.deathmatch!.Bool && (g.dmflags!.Int & QShared.DF_NO_HEALTH) != 0)
            {
                g.G_FreeEdict(self);
                return;
            }

            self.model = "models/items/healing/large/tris.md2";
            self.count = 25;
            g.SpawnItem(self, g.FindItem("Health")!);
            // gi.soundindex("items/l_health.wav");
        }

        /*
        * QUAKED item_health_mega (.3 .3 1) (-16 -16 -16) (16 16 16)
        */
        private static void SP_item_health_mega(QuakeGame g, edict_t self)
        {
            if (self == null || g == null)
            {
                return;
            }

            if (g.deathmatch!.Bool && (g.dmflags!.Int & QShared.DF_NO_HEALTH) != 0)
            {
                g.G_FreeEdict(self);
                return;
            }

            self.model = "models/items/mega_h/tris.md2";
            self.count = 100;
            g.SpawnItem(self, g.FindItem("Health")!);
            // gi.soundindex("items/m_health.wav");
            self.style = HEALTH_IGNORE_MAX | HEALTH_TIMED;
        }

        private void InitItems()
        {
            itemlist = new gitem_t[gameitemlist.Length];
            for (int i = 0; i < itemlist.Length; i++)
            {
                itemlist[i] = (gitem_t)gameitemlist[i].Clone();
                itemlist[i].index = i;
            }
        }

        /*
        * Called by worldspawn
        */
        private void SetItemNames()
        {
            for (int i = 0; i < itemlist.Length; i++)
            {
                ref var it = ref itemlist[i];
                gi.configstring(QShared.CS_ITEMS + i, it.pickup_name!);
            }

            jacket_armor_index = FindItem("Jacket Armor")?.index ?? 0;
            combat_armor_index = FindItem("Combat Armor")?.index ?? 0;
            body_armor_index = FindItem("Body Armor")?.index ?? 0;
            power_screen_index = FindItem("Power Screen")?.index ?? 0;
            power_shield_index = FindItem("Power Shield")?.index ?? 0;

        }

    }
}
