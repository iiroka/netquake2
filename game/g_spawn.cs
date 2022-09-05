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

namespace Quake2 {

    partial class QuakeGame
    {

        /*
        * Takes a key/value pair and sets
        * the binary values in an edict
        */
        private void ED_ParseField(in string key, in string value, ref edict_t ent)
        {
            // field_t *f;
            // byte *b;
            // float v;
            // vec3_t vec;

            if (key == null || value == null)
            {
                return;
            }

            // for (f = fields; f->name; f++)
            // {
            //     if (!(f->flags & FFL_NOSPAWN) && !Q_strcasecmp(f->name, (char *)key))
            //     {
            //         /* found it */
            //         if (f->flags & FFL_SPAWNTEMP)
            //         {
            //             b = (byte *)&st;
            //         }
            //         else
            //         {
            //             b = (byte *)ent;
            //         }

            //         switch (f->type)
            //         {
            //             case F_LSTRING:
            //                 *(char **)(b + f->ofs) = ED_NewString(value);
            //                 break;
            //             case F_VECTOR:
            //                 sscanf(value, "%f %f %f", &vec[0], &vec[1], &vec[2]);
            //                 ((float *)(b + f->ofs))[0] = vec[0];
            //                 ((float *)(b + f->ofs))[1] = vec[1];
            //                 ((float *)(b + f->ofs))[2] = vec[2];
            //                 break;
            //             case F_INT:
            //                 *(int *)(b + f->ofs) = (int)strtol(value, (char **)NULL, 10);
            //                 break;
            //             case F_FLOAT:
            //                 *(float *)(b + f->ofs) = (float)strtod(value, (char **)NULL);
            //                 break;
            //             case F_ANGLEHACK:
            //                 v = (float)strtod(value, (char **)NULL);
            //                 ((float *)(b + f->ofs))[0] = 0;
            //                 ((float *)(b + f->ofs))[1] = v;
            //                 ((float *)(b + f->ofs))[2] = 0;
            //                 break;
            //             case F_IGNORE:
            //                 break;
            //             default:
            //                 break;
            //         }

            //         return;
            //     }
            // }

            // gi.dprintf($"{key} is not a field\n");
        }

        /*
        * Parses an edict out of the given string,
        * returning the new position ed should be
        * a properly initialized empty edict.
        */
        private void ED_ParseEdict(string data, ref int index, ref edict_t ent)
        {
            // qboolean init;
            // char keyname[256];
            // const char *com_token;

            if (ent == null)
            {
                index = -1;
                return;
            }

            var init = false;
            // memset(&st, 0, sizeof(st));

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
            //     memset(ent, 0, sizeof(*ent));
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

            // skill_level = floor(skill->value);

            // if (skill_level < 0)
            // {
            //     skill_level = 0;
            // }

            // if (skill_level > 3)
            // {
            //     skill_level = 3;
            // }

            // if (skill->value != skill_level)
            // {
            //     gi.cvar_forceset("skill", va("%f", skill_level));
            // }

            // SaveClientData();

            // gi.FreeTags(TAG_LEVEL);

            // memset(&level, 0, sizeof(level));
            for (int i = 0; i < g_edicts.Length; i++)
            {
                g_edicts[i] = new edict_t();
            }

            // Q_strlcpy(level.mapname, mapname, sizeof(level.mapname));
            game.spawnpoint = spawnpoint;

            /* set client fields on player ents */
            // for (i = 0; i < game.maxclients; i++)
            // {
            //     g_edicts[i + 1].client = game.clients + i;
            // }

            edict_t? ent = null;
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

                if (ent == null)
                {
                    ent = g_edicts[0];
                }
                // else
                // {
                //     ent = G_Spawn();
                // }

                ED_ParseEdict(entities, ref index, ref ent);

                // /* yet another map hack */
                // if (!Q_stricmp(level.mapname, "command") &&
                //     !Q_stricmp(ent->classname, "trigger_once") &&
                //     !Q_stricmp(ent->model, "*27"))
                // {
                //     ent->spawnflags &= ~SPAWNFLAG_NOT_HARD;
                // }

                // /* remove things (except the world) from
                // different skill levels or deathmatch */
                // if (ent != g_edicts)
                // {
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
                //             (ent->spawnflags & SPAWNFLAG_NOT_EASY)) ||
                //             ((skill->value == SKILL_MEDIUM) &&
                //             (ent->spawnflags & SPAWNFLAG_NOT_MEDIUM)) ||
                //             (((skill->value == SKILL_HARD) ||
                //             (skill->value == SKILL_HARDPLUS)) &&
                //             (ent->spawnflags & SPAWNFLAG_NOT_HARD))
                //             )
                //         {
                //             G_FreeEdict(ent);
                //             inhibit++;
                //             continue;
                //         }
                //     }

                //     ent->spawnflags &=
                //         ~(SPAWNFLAG_NOT_EASY | SPAWNFLAG_NOT_MEDIUM |
                //         SPAWNFLAG_NOT_HARD |
                //         SPAWNFLAG_NOT_COOP | SPAWNFLAG_NOT_DEATHMATCH);
                // }

                // ED_CallSpawn(ent);
            }

            gi.dprintf($"{inhibit} entities inhibited.\n");

            // G_FindTeams();

            // PlayerTrail_Init();            
        }
    }
}