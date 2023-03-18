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
 * Item spawning.
 *
 * =======================================================================
 */
using System.Numerics; 

namespace Quake2 {

    partial class QuakeGame
    {

        private delegate void SpawnFunc(QuakeGame g, edict_t? e);

        private readonly IDictionary<string, SpawnFunc> spawns = new Dictionary<string, SpawnFunc>(){
            // {"item_health", SP_item_health},
            // {"item_health_small", SP_item_health_small},
            // {"item_health_large", SP_item_health_large},
            // {"item_health_mega", SP_item_health_mega},

            {"info_player_start", SP_info_player_start},
            // {"info_player_deathmatch", SP_info_player_deathmatch},
            // {"info_player_coop", SP_info_player_coop},
            // {"info_player_intermission", SP_info_player_intermission},

            // {"func_plat", SP_func_plat},
            // {"func_button", SP_func_button},
            {"func_door", SP_func_door},
            // {"func_door_secret", SP_func_door_secret},
            // {"func_door_rotating", SP_func_door_rotating},
            // {"func_rotating", SP_func_rotating},
            // {"func_train", SP_func_train},
            // {"func_water", SP_func_water},
            // {"func_conveyor", SP_func_conveyor},
            // {"func_areaportal", SP_func_areaportal},
            // {"func_clock", SP_func_clock},
            // {"func_wall", SP_func_wall},
            // {"func_object", SP_func_object},
            // {"func_timer", SP_func_timer},
            // {"func_explosive", SP_func_explosive},
            // {"func_killbox", SP_func_killbox},

            // {"trigger_always", SP_trigger_always},
            // {"trigger_once", SP_trigger_once},
            // {"trigger_multiple", SP_trigger_multiple},
            // {"trigger_relay", SP_trigger_relay},
            // {"trigger_push", SP_trigger_push},
            // {"trigger_hurt", SP_trigger_hurt},
            // {"trigger_key", SP_trigger_key},
            // {"trigger_counter", SP_trigger_counter},
            // {"trigger_elevator", SP_trigger_elevator},
            // {"trigger_gravity", SP_trigger_gravity},
            // {"trigger_monsterjump", SP_trigger_monsterjump},

            // {"target_temp_entity", SP_target_temp_entity},
            {"target_speaker", SP_target_speaker},
            // {"target_explosion", SP_target_explosion},
            // {"target_changelevel", SP_target_changelevel},
            // {"target_secret", SP_target_secret},
            // {"target_goal", SP_target_goal},
            // {"target_splash", SP_target_splash},
            // {"target_spawner", SP_target_spawner},
            // {"target_blaster", SP_target_blaster},
            // {"target_crosslevel_trigger", SP_target_crosslevel_trigger},
            // {"target_crosslevel_target", SP_target_crosslevel_target},
            // {"target_laser", SP_target_laser},
            // {"target_help", SP_target_help},
            // {"target_lightramp", SP_target_lightramp},
            // {"target_earthquake", SP_target_earthquake},
            // {"target_character", SP_target_character},
            // {"target_string", SP_target_string},

            {"worldspawn", SP_worldspawn},
            // {"viewthing", SP_viewthing},

            {"light", SP_light},
            // {"light_mine1", SP_light_mine1},
            // {"light_mine2", SP_light_mine2},
            // {"info_null", SP_info_null},
            // {"func_group", SP_info_null},
            // {"info_notnull", SP_info_notnull},
            {"path_corner", SP_path_corner},
            // {"point_combat", SP_point_combat},

            // {"misc_explobox", SP_misc_explobox},
            // {"misc_banner", SP_misc_banner},
            // {"misc_satellite_dish", SP_misc_satellite_dish},
            // {"misc_gib_arm", SP_misc_gib_arm},
            // {"misc_gib_leg", SP_misc_gib_leg},
            // {"misc_gib_head", SP_misc_gib_head},
            // {"misc_insane", SP_misc_insane},
            // {"misc_deadsoldier", SP_misc_deadsoldier},
            // {"misc_viper", SP_misc_viper},
            // {"misc_viper_bomb", SP_misc_viper_bomb},
            // {"misc_bigviper", SP_misc_bigviper},
            // {"misc_strogg_ship", SP_misc_strogg_ship},
            // {"misc_teleporter", SP_misc_teleporter},
            // {"misc_teleporter_dest", SP_misc_teleporter_dest},
            // {"misc_blackhole", SP_misc_blackhole},
            // {"misc_eastertank", SP_misc_eastertank},
            // {"misc_easterchick", SP_misc_easterchick},
            // {"misc_easterchick2", SP_misc_easterchick2},

            // {"monster_berserk", SP_monster_berserk},
            // {"monster_gladiator", SP_monster_gladiator},
            // {"monster_gunner", SP_monster_gunner},
            // {"monster_infantry", SP_monster_infantry},
            {"monster_soldier_light", SP_monster_soldier_light},
            {"monster_soldier", SP_monster_soldier}
            // {"monster_soldier_ss", SP_monster_soldier_ss},
            // {"monster_tank", SP_monster_tank},
            // {"monster_tank_commander", SP_monster_tank},
            // {"monster_medic", SP_monster_medic},
            // {"monster_flipper", SP_monster_flipper},
            // {"monster_chick", SP_monster_chick},
            // {"monster_parasite", SP_monster_parasite},
            // {"monster_flyer", SP_monster_flyer},
            // {"monster_brain", SP_monster_brain},
            // {"monster_floater", SP_monster_floater},
            // {"monster_hover", SP_monster_hover},
            // {"monster_mutant", SP_monster_mutant},
            // {"monster_supertank", SP_monster_supertank},
            // {"monster_boss2", SP_monster_boss2},
            // {"monster_boss3_stand", SP_monster_boss3_stand},
            // {"monster_makron", SP_monster_makron},
            // {"monster_jorg", SP_monster_jorg},

            // {"monster_commander_body", SP_monster_commander_body},

            // {"turret_breach", SP_turret_breach},
            // {"turret_base", SP_turret_base},
            // {"turret_driver", SP_turret_driver},
        };


        /*
        * Finds the spawn function for
        * the entity and calls it
        */
        private void ED_CallSpawn(ref edict_t ent)
        {
            if (ent == null)
            {
                return;
            }

            if (String.IsNullOrEmpty(ent.classname))
            {
                gi.dprintf("ED_CallSpawn: NULL classname\n");
                G_FreeEdict(ent);
                return;
            }

            /* check item spawn functions */
            for (int i = 0; i < itemlist.Length; i++)
            {
                if (String.IsNullOrEmpty(itemlist[i].classname))
                {
                    continue;
                }

                if (itemlist[i].classname.Equals(ent.classname))
                {
                    /* found it */
                    SpawnItem(ent, itemlist[i]);
                    return;
                }
            }

            /* check normal spawn functions */
            if (spawns.ContainsKey(ent.classname))
            {
                /* found it */
                spawns[ent.classname](this, ent);
                return;
            }

            gi.dprintf(ent.classname + " doesn't have a spawn function\n");
        }

        /*
        * Takes a key/value pair and sets
        * the binary values in an edict
        */
        private void ED_ParseField(in string key, in string value, ref edict_t ent)
        {
            if (key == null || value == null)
            {
                return;
            }

            foreach (var f in fields)
            {
                if ((f.flags & FFL_NOSPAWN) == 0 && f.name.Equals(key))
                {
                    /* found it */
                    Type t;
                    object src = ent;
                    if ((f.flags & FFL_SPAWNTEMP) != 0)
                    {
                        t = st.GetType();
                        src = st;
                    }
                    else
                    {
                        t = ent.GetType();
                        if ((f.flags & FFL_ENTITYSTATE) != 0) {
                            var fi = t.GetField("s");
                            t = fi!.FieldType;
                            src = ent.s;
                        }
                    }

                    if (t.GetField(f.fname) == null && f.type != fieldtype_t.F_IGNORE) {
                        Console.WriteLine($"***** No field called {f.name} *****");
                    }

                    switch (f.type)
                    {
                        case fieldtype_t.F_LSTRING: {
                            var finfo = t.GetField(f.fname);
                            finfo!.SetValue(src, value);
                            break;
                        }
                        case fieldtype_t.F_VECTOR: {
                            var parts = value.Split(" ");
                            Vector3 v = new Vector3();
                            v.X = Convert.ToSingle(parts[0], QShared.provider);
                            v.Y = Convert.ToSingle(parts[1], QShared.provider);
                            v.Z = Convert.ToSingle(parts[2], QShared.provider);
                            var finfo = t.GetField(f.fname);
                            finfo!.SetValue(src, v);
                            break;
                        }
                        case fieldtype_t.F_INT: {
                            var finfo = t.GetField(f.fname);
                            finfo!.SetValue(src, Int32.Parse(value));
                            break;
                        }
                        case fieldtype_t.F_FLOAT: {
                            var finfo = t.GetField(f.fname);
                            finfo!.SetValue(ent, Convert.ToSingle(value, QShared.provider));
                            break;
                        }
                        case fieldtype_t.F_ANGLEHACK: {
                            Vector3 v = new Vector3();
                            v.X = 0;
                            v.Y = Convert.ToSingle(value, QShared.provider);
                            v.Z = 0;
                            var finfo = t.GetField(f.fname);
                            finfo!.SetValue(src, v);
                            break;
                        }
                        default:
                            break;
                    }

                    return;
                }
            }

            gi.dprintf($"{key} is not a field\n");
        }

        /*
        * Parses an edict out of the given string,
        * returning the new position ed should be
        * a properly initialized empty edict.
        */
        private void ED_ParseEdict(string data, ref int index, ref edict_t ent)
        {
            if (ent == null)
            {
                index = -1;
                return;
            }

            var init = false;
            st = new spawn_temp_t();

            /* go through all the dictionary pairs */
            while (true)
            {
                /* parse key */
                var keyname = QShared.COM_Parse(data, ref index);

                if (keyname[0] == '}')
                {
                    break;
                }

                if (index < 0)
                {
                    gi.error("ED_ParseEntity: EOF without closing brace");
                }

                /* parse value */
                var com_token = QShared.COM_Parse(data, ref index);

                if (index < 0)
                {
                    gi.error("ED_ParseEntity: EOF without closing brace");
                }

                if (com_token[0] == '}')
                {
                    gi.error("ED_ParseEntity: closing brace without data");
                }

                init = true;

                /* keynames with a leading underscore are
                used for utility comments, and are
                immediately discarded by quake */
                if (keyname[0] == '_')
                {
                    continue;
                }

                ED_ParseField(keyname, com_token, ref ent);
            }

            if (!init)
            {
                ent.inuse = false;
                ent.classname = "";
            }
        }

        public void SpawnEntities(string mapname, string entities, string spawnpoint)
        {
            // edict_t *ent;
            // int inhibit;
            // const char *com_token;
            // int i;
            // float skill_level;

            if (mapname == null || entities == null || spawnpoint == null)
            {
                return;
            }

            int skill_level = skill!.Int;

            if (skill_level < 0)
            {
                skill_level = 0;
            }

            if (skill_level > 3)
            {
                skill_level = 3;
            }

            if (skill!.Int != skill_level)
            {
                // gi.cvar_forceset("skill", va("%f", skill_level));
            }

            // SaveClientData();

            // gi.FreeTags(TAG_LEVEL);

            level = new level_locals_t();
            for (int i = 0; i < g_edicts.Length; i++)
            {
                g_edicts[i] = new edict_t() { index = i, area = new link_t() { ent = g_edicts[i] } };
            }

            level.mapname = mapname;
            game.spawnpoint = spawnpoint;

            /* set client fields on player ents */
            for (int i = 0; i < game.maxclients; i++)
            {
                g_edicts[i + 1].client = game.clients[i];
            }

            var isFirst = true;
            int inhibit = 0;

            /* parse ents */
            int index = 0;
            while (true)
            {
                /* parse the opening brace */
                var com_token = QShared.COM_Parse(entities, ref index);
                if (index < 0)
                {
                    break;
                }

                if (com_token[0] != '{')
                {
                    gi.error("ED_LoadFromFile: found " + com_token + " when expecting {");
                }

                var ent = isFirst ? g_edicts[0] : G_Spawn();
                isFirst = false;

                ED_ParseEdict(entities, ref index, ref ent);

                // /* yet another map hack */
                // if (!Q_stricmp(level.mapname, "command") &&
                //     !Q_stricmp(ent->classname, "trigger_once") &&
                //     !Q_stricmp(ent->model, "*27"))
                // {
                //     ent->spawnflags &= ~SPAWNFLAG_NOT_HARD;
                // }

                /* remove things (except the world) from
                   different skill levels or deathmatch */
                if (ent != g_edicts[0])
                {
                //     if (deathmatch->value)
                //     {
                //         if (ent->spawnflags & SPAWNFLAG_NOT_DEATHMATCH)
                //         {
                //             G_FreeEdict(ent);
                //             inhibit++;
                //             continue;
                //         }
                //     }
                //     else
                //     {
                //         if (((skill->value == SKILL_EASY) &&
                //             (ent->spawnflags & SPAWNFLAG_NOT_EASY) != 0) ||
                //             ((skill->value == SKILL_MEDIUM) &&
                //             (ent->spawnflags & SPAWNFLAG_NOT_MEDIUM) != 0) ||
                //             (((skill->value == SKILL_HARD) ||
                //             (skill->value == SKILL_HARDPLUS)) &&
                //             (ent->spawnflags & SPAWNFLAG_NOT_HARD) != 0)
                //             )
                //         {
                //             G_FreeEdict(ent);
                //             inhibit++;
                //             continue;
                //         }
                //     }

                    ent.spawnflags &=
                        ~(SPAWNFLAG_NOT_EASY | SPAWNFLAG_NOT_MEDIUM |
                        SPAWNFLAG_NOT_HARD |
                        SPAWNFLAG_NOT_COOP | SPAWNFLAG_NOT_DEATHMATCH);
                }

                ED_CallSpawn(ref ent);
            }

            gi.dprintf($"{inhibit} entities inhibited.\n");

            // G_FindTeams();

            // PlayerTrail_Init();            
        }

        /* =================================================================== */

        private readonly string single_statusbar =
            "yb	-24 " +

        /* health */
            "xv	0 " +
            "hnum " +
            "xv	50 " +
            "pic 0 " +

        /* ammo */
            "if 2 " +
            "	xv	100 " +
            "	anum " +
            "	xv	150 " +
            "	pic 2 " +
            "endif " +

        /* armor */
            "if 4 " +
            "	xv	200 " +
            "	rnum " +
            "	xv	250 " +
            "	pic 4 " +
            "endif " +

        /* selected item */
            "if 6 " +
            "	xv	296 " +
            "	pic 6 " +
            "endif " +

            "yb	-50 " +

        /* picked up item */
            "if 7 " +
            "	xv	0 " +
            "	pic 7 " +
            "	xv	26 " +
            "	yb	-42 " +
            "	stat_string 8 " +
            "	yb	-50 " +
            "endif " +

        /* timer */
            "if 9 " +
            "	xv	262 " +
            "	num	2	10 " +
            "	xv	296 " +
            "	pic	9 " +
            "endif " +

        /*  help / weapon icon */
            "if 11 " +
            "	xv	148 " +
            "	pic	11 " +
            "endif "
        ;

        private readonly string dm_statusbar =
            "yb	-24 " +

        /* health */
            "xv	0 " +
            "hnum " +
            "xv	50 " +
            "pic 0 " +

        /* ammo */
            "if 2 " +
            "	xv	100 " +
            "	anum " +
            "	xv	150 " +
            "	pic 2 " +
            "endif " +

        /* armor */
            "if 4 " +
            "	xv	200 " +
            "	rnum " +
            "	xv	250 " +
            "	pic 4 " +
            "endif " +

        /* selected item */
            "if 6 " +
            "	xv	296 " +
            "	pic 6 " +
            "endif " +

            "yb	-50 " +

        /* picked up item */
            "if 7 " +
            "	xv	0 " +
            "	pic 7 " +
            "	xv	26 " +
            "	yb	-42 " +
            "	stat_string 8 " +
            "	yb	-50 " +
            "endif " +

        /* timer */
            "if 9 " +
            "	xv	246 " +
            "	num	2	10 " +
            "	xv	296 " +
            "	pic	9 " +
            "endif " +

        /*  help / weapon icon */
            "if 11 " +
            "	xv	148 " +
            "	pic	11 " +
            "endif " +

        /*  frags */
            "xr	-50 " +
            "yt 2 " +
            "num 3 14 " +

        /* spectator */
            "if 17 " +
            "xv 0 " +
            "yb -58 " +
            "string2 \"SPECTATOR MODE\" " +
            "endif " +

        /* chase camera */
            "if 16 " +
            "xv 0 " +
            "yb -68 " +
            "string \"Chasing\" " +
            "xv 64 " +
            "stat_string 16 " +
            "endif "
        ;

        /*QUAKED worldspawn (0 0 0) ?
        *
        * Only used for the world.
        *  "sky"		environment map name
        *  "skyaxis"	vector axis for rotating sky
        *  "skyrotate"	speed of rotation in degrees/second
        *  "sounds"	music cd track number
        *  "gravity"	800 is default gravity
        *  "message"	text to print at user logon
        */
        private static void SP_worldspawn(QuakeGame g, edict_t? ent)
        {
            if (ent == null)
            {
                return;
            }

            ent.movetype = movetype_t.MOVETYPE_PUSH;
            ent.solid = solid_t.SOLID_BSP;
            ent.inuse = true; /* since the world doesn't use G_Spawn() */
            ent.s.modelindex = 1; /* world model is always index 1 */

            /* --------------- */

            /* reserve some spots for dead
            player bodies for coop / deathmatch */
            // InitBodyQue();

            // /* set configstrings for items */
            // SetItemNames();

            if (!String.IsNullOrEmpty(g.st.nextmap))
            {
                g.level.nextmap = g.st.nextmap;
            }

            /* make some data visible to the server */
            if (!String.IsNullOrEmpty(ent.message))
            {
                g.gi.configstring(QShared.CS_NAME, ent.message);
                g.level.level_name = ent.message;
            }
            else
            {
                g.level.level_name = g.level.mapname;
            }

            if (!String.IsNullOrEmpty(g.st.sky))
            {
                g.gi.configstring(QShared.CS_SKY, g.st.sky);
            }
            else
            {
                g.gi.configstring(QShared.CS_SKY, "unit1_");
            }

            g.gi.configstring(QShared.CS_SKYROTATE, g.st.skyrotate.ToString());

            g.gi.configstring(QShared.CS_SKYAXIS, $"{g.st.skyaxis.X} {g.st.skyaxis.Y} {g.st.skyaxis.Z}");

            // gi.configstring(CS_CDTRACK, va("%i", ent->sounds));

            g.gi.configstring(QShared.CS_MAXCLIENTS, g.maxclients!.str);

            /* status bar program */
            if (g.deathmatch!.Bool)
            {
                g.gi.configstring(QShared.CS_STATUSBAR, g.dm_statusbar);
            }
            else
            {
                g.gi.configstring(QShared.CS_STATUSBAR, g.single_statusbar);
            }

            /* --------------- */

            /* help icon for statusbar */
            // gi.imageindex("i_help");
            // g.level.pic_health = g.gi.imageindex("i_health");
            // gi.imageindex("help");
            // gi.imageindex("field_3");

            // if (!st.gravity)
            // {
            //     gi.cvar_set("sv_gravity", "800");
            // }
            // else
            // {
            //     gi.cvar_set("sv_gravity", st.gravity);
            // }

            // snd_fry = gi.soundindex("player/fry.wav"); /* standing in lava / slime */

            // PrecacheItem(FindItem("Blaster"));

            // gi.soundindex("player/lava1.wav");
            // gi.soundindex("player/lava2.wav");

            // gi.soundindex("misc/pc_up.wav");
            // gi.soundindex("misc/talk1.wav");

            // gi.soundindex("misc/udeath.wav");

            // /* gibs */
            // gi.soundindex("items/respawn1.wav");

            // /* sexed sounds */
            // gi.soundindex("*death1.wav");
            // gi.soundindex("*death2.wav");
            // gi.soundindex("*death3.wav");
            // gi.soundindex("*death4.wav");
            // gi.soundindex("*fall1.wav");
            // gi.soundindex("*fall2.wav");
            // gi.soundindex("*gurp1.wav"); /* drowning damage */
            // gi.soundindex("*gurp2.wav");
            // gi.soundindex("*jump1.wav"); /* player jump */
            // gi.soundindex("*pain25_1.wav");
            // gi.soundindex("*pain25_2.wav");
            // gi.soundindex("*pain50_1.wav");
            // gi.soundindex("*pain50_2.wav");
            // gi.soundindex("*pain75_1.wav");
            // gi.soundindex("*pain75_2.wav");
            // gi.soundindex("*pain100_1.wav");
            // gi.soundindex("*pain100_2.wav");

            // /* sexed models: THIS ORDER MUST MATCH THE DEFINES IN g_local.h
            // you can add more, max 19 (pete change)these models are only
            // loaded in coop or deathmatch. not singleplayer. */
            // if (coop->value || deathmatch->value)
            // {
            //     gi.modelindex("#w_blaster.md2");
            //     gi.modelindex("#w_shotgun.md2");
            //     gi.modelindex("#w_sshotgun.md2");
            //     gi.modelindex("#w_machinegun.md2");
            //     gi.modelindex("#w_chaingun.md2");
            //     gi.modelindex("#a_grenades.md2");
            //     gi.modelindex("#w_glauncher.md2");
            //     gi.modelindex("#w_rlauncher.md2");
            //     gi.modelindex("#w_hyperblaster.md2");
            //     gi.modelindex("#w_railgun.md2");
            //     gi.modelindex("#w_bfg.md2");
            // }

            // /* ------------------- */

            // gi.soundindex("player/gasp1.wav"); /* gasping for air */
            // gi.soundindex("player/gasp2.wav"); /* head breaking surface, not gasping */

            // gi.soundindex("player/watr_in.wav"); /* feet hitting water */
            // gi.soundindex("player/watr_out.wav"); /* feet leaving water */

            // gi.soundindex("player/watr_un.wav"); /* head going underwater */

            // gi.soundindex("player/u_breath1.wav");
            // gi.soundindex("player/u_breath2.wav");

            // gi.soundindex("items/pkup.wav"); /* bonus item pickup */
            // gi.soundindex("world/land.wav"); /* landing thud */
            // gi.soundindex("misc/h2ohit1.wav"); /* landing splash */

            // gi.soundindex("items/damage.wav");
            // gi.soundindex("items/protect.wav");
            // gi.soundindex("items/protect4.wav");
            // gi.soundindex("weapons/noammo.wav");

            // gi.soundindex("infantry/inflies1.wav");

            g.sm_meat_index = g.gi.modelindex("models/objects/gibs/sm_meat/tris.md2");
            g.gi.modelindex("models/objects/gibs/arm/tris.md2");
            g.gi.modelindex("models/objects/gibs/bone/tris.md2");
            g.gi.modelindex("models/objects/gibs/bone2/tris.md2");
            g.gi.modelindex("models/objects/gibs/chest/tris.md2");
            g.gi.modelindex("models/objects/gibs/skull/tris.md2");
            g.gi.modelindex("models/objects/gibs/head2/tris.md2");

            /* Setup light animation tables. 'a'
            is total darkness, 'z' is doublebright. */

            /* 0 normal */
            g.gi.configstring(QShared.CS_LIGHTS + 0, "m");

            // /* 1 FLICKER (first variety) */
            g.gi.configstring(QShared.CS_LIGHTS + 1, "mmnmmommommnonmmonqnmmo");

            /* 2 SLOW STRONG PULSE */
            g.gi.configstring(QShared.CS_LIGHTS + 2, "abcdefghijklmnopqrstuvwxyzyxwvutsrqponmlkjihgfedcba");

            /* 3 CANDLE (first variety) */
            g.gi.configstring(QShared.CS_LIGHTS + 3, "mmmmmaaaaammmmmaaaaaabcdefgabcdefg");

            /* 4 FAST STROBE */
            g.gi.configstring(QShared.CS_LIGHTS + 4, "mamamamamama");

            /* 5 GENTLE PULSE 1 */
            g.gi.configstring(QShared.CS_LIGHTS + 5, "jklmnopqrstuvwxyzyxwvutsrqponmlkj");

            /* 6 FLICKER (second variety) */
            g.gi.configstring(QShared.CS_LIGHTS + 6, "nmonqnmomnmomomno");

            /* 7 CANDLE (second variety) */
            g.gi.configstring(QShared.CS_LIGHTS + 7, "mmmaaaabcdefgmmmmaaaammmaamm");

            /* 8 CANDLE (third variety) */
            g.gi.configstring(QShared.CS_LIGHTS + 8, "mmmaaammmaaammmabcdefaaaammmmabcdefmmmaaaa");

            /* 9 SLOW STROBE (fourth variety) */
            g.gi.configstring(QShared.CS_LIGHTS + 9, "aaaaaaaazzzzzzzz");

            /* 10 FLUORESCENT FLICKER */
            g.gi.configstring(QShared.CS_LIGHTS + 10, "mmamammmmammamamaaamammma");

            /* 11 SLOW PULSE NOT FADE TO BLACK */
            g.gi.configstring(QShared.CS_LIGHTS + 11, "abcdefghijklmnopqrrqponmlkjihgfedcba");

            /* styles 32-62 are assigned by the light program for switchable lights */

            /* 63 testing */
            g.gi.configstring(QShared.CS_LIGHTS + 63, "a");
        }

    }
}