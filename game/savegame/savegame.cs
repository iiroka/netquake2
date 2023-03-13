/*
 * Copyright (C) 1997-2001 Id Software, Inc.
 * Copyright (C) 2011 Knightmare
 * Copyright (C) 2011 Yamagi Burmeister
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
 * The savegame system.
 *
 * =======================================================================
 */

/*
 * This is the Quake 2 savegame system, fixed by Yamagi
 * based on an idea by Knightmare of kmquake2. This major
 * rewrite of the original g_save.c is much more robust
 * and portable since it doesn't use any function pointers.
 *
 * Inner workings:
 * When the game is saved all function pointers are
 * translated into human readable function definition strings.
 * The same way all mmove_t pointers are translated. This
 * human readable strings are then written into the file.
 * At game load the human readable strings are retranslated
 * into the actual function pointers and struct pointers. The
 * pointers are generated at each compilation / start of the
 * client, thus the pointers are always correct.
 *
 * Limitations:
 * While savegames survive recompilations of the game source
 * and bigger changes in the source, there are some limitation
 * which a nearly impossible to fix without a object orientated
 * rewrite of the game.
 *  - If functions or mmove_t structs that a referencenced
 *    inside savegames are added or removed (e.g. the files
 *    in tables/ are altered) the load functions cannot
 *    reconnect all pointers and thus not restore the game.
 *  - If the operating system is changed internal structures
 *    may change in an unrepairable way.
 *  - If the architecture is changed pointer length and
 *    other internal datastructures change in an
 *    incompatible way.
 *  - If the edict_t struct is changed, savegames
 *    will break.
 * This is not so bad as it looks since functions and
 * struct won't be added and edict_t won't be changed
 * if no big, sweeping changes are done. The operating
 * system and architecture are in the hands of the user.
 */

namespace Quake2 {

    partial class QuakeGame
    {
        /*
        * Fields to be saved
        */
        private readonly field_t[] fields = {
            new field_t(){ name = "classname", fname="classname", type = fieldtype_t.F_LSTRING },
            new field_t(){ name = "model", fname="model", type = fieldtype_t.F_LSTRING },
            new field_t(){ name = "spawnflags", fname="spawnflags", type = fieldtype_t.F_INT },
            new field_t(){ name = "speed", fname="speed", type = fieldtype_t.F_FLOAT },
            new field_t(){ name = "accel", fname="accel", type = fieldtype_t.F_FLOAT },
            new field_t(){ name = "decel", fname="decel", type = fieldtype_t.F_FLOAT },
            new field_t(){ name = "target", fname="target", type = fieldtype_t.F_LSTRING },
            new field_t(){ name = "targetname", fname="targetname", type = fieldtype_t.F_LSTRING },
            new field_t(){ name = "pathtarget", fname="pathtarget", type = fieldtype_t.F_LSTRING },
            new field_t(){ name = "deathtarget", fname="deathtarget", type = fieldtype_t.F_LSTRING },
            new field_t(){ name = "killtarget", fname="killtarget", type = fieldtype_t.F_LSTRING },
            new field_t(){ name = "combattarget", fname="combattarget", type = fieldtype_t.F_LSTRING },
            new field_t(){ name = "message", fname="message", type = fieldtype_t.F_LSTRING },
            new field_t(){ name = "team", fname="team", type = fieldtype_t.F_LSTRING },
            new field_t(){ name = "wait", fname="wait", type = fieldtype_t.F_FLOAT },
            // {"wait", FOFS(wait), F_FLOAT},
            // {"delay", FOFS(delay), F_FLOAT},
            // {"random", FOFS(random), F_FLOAT},
            // {"move_origin", FOFS(move_origin), F_VECTOR},
            // {"move_angles", FOFS(move_angles), F_VECTOR},
            // {"style", FOFS(style), F_INT},
            // {"count", FOFS(count), F_INT},
            // {"health", FOFS(health), F_INT},
            // {"sounds", FOFS(sounds), F_INT},
            new field_t(){ name = "light", fname="light", type = fieldtype_t.F_IGNORE },
            // {"dmg", FOFS(dmg), F_INT},
            // {"mass", FOFS(mass), F_INT},
            // {"volume", FOFS(volume), F_FLOAT},
            // {"attenuation", FOFS(attenuation), F_FLOAT},
            new field_t(){ name = "map", fname="map", type = fieldtype_t.F_LSTRING },
            new field_t(){ name = "origin", fname="origin", type = fieldtype_t.F_VECTOR, flags = FFL_ENTITYSTATE },
            new field_t(){ name = "angles", fname="angles", type = fieldtype_t.F_VECTOR, flags = FFL_ENTITYSTATE },
            new field_t(){ name = "angle", fname="angles", type = fieldtype_t.F_ANGLEHACK, flags = FFL_ENTITYSTATE },
            // {"goalentity", FOFS(goalentity), F_EDICT, FFL_NOSPAWN},
            // {"movetarget", FOFS(movetarget), F_EDICT, FFL_NOSPAWN},
            // {"enemy", FOFS(enemy), F_EDICT, FFL_NOSPAWN},
            // {"oldenemy", FOFS(oldenemy), F_EDICT, FFL_NOSPAWN},
            // {"activator", FOFS(activator), F_EDICT, FFL_NOSPAWN},
            // {"groundentity", FOFS(groundentity), F_EDICT, FFL_NOSPAWN},
            // {"teamchain", FOFS(teamchain), F_EDICT, FFL_NOSPAWN},
            // {"teammaster", FOFS(teammaster), F_EDICT, FFL_NOSPAWN},
            // {"owner", FOFS(owner), F_EDICT, FFL_NOSPAWN},
            // {"mynoise", FOFS(mynoise), F_EDICT, FFL_NOSPAWN},
            // {"mynoise2", FOFS(mynoise2), F_EDICT, FFL_NOSPAWN},
            // {"target_ent", FOFS(target_ent), F_EDICT, FFL_NOSPAWN},
            // {"chain", FOFS(chain), F_EDICT, FFL_NOSPAWN},
            // {"prethink", FOFS(prethink), F_FUNCTION, FFL_NOSPAWN},
            // {"think", FOFS(think), F_FUNCTION, FFL_NOSPAWN},
            // {"blocked", FOFS(blocked), F_FUNCTION, FFL_NOSPAWN},
            // {"touch", FOFS(touch), F_FUNCTION, FFL_NOSPAWN},
            // {"use", FOFS(use), F_FUNCTION, FFL_NOSPAWN},
            // {"pain", FOFS(pain), F_FUNCTION, FFL_NOSPAWN},
            // {"die", FOFS(die), F_FUNCTION, FFL_NOSPAWN},
            // {"stand", FOFS(monsterinfo.stand), F_FUNCTION, FFL_NOSPAWN},
            // {"idle", FOFS(monsterinfo.idle), F_FUNCTION, FFL_NOSPAWN},
            // {"search", FOFS(monsterinfo.search), F_FUNCTION, FFL_NOSPAWN},
            // {"walk", FOFS(monsterinfo.walk), F_FUNCTION, FFL_NOSPAWN},
            // {"run", FOFS(monsterinfo.run), F_FUNCTION, FFL_NOSPAWN},
            // {"dodge", FOFS(monsterinfo.dodge), F_FUNCTION, FFL_NOSPAWN},
            // {"attack", FOFS(monsterinfo.attack), F_FUNCTION, FFL_NOSPAWN},
            // {"melee", FOFS(monsterinfo.melee), F_FUNCTION, FFL_NOSPAWN},
            // {"sight", FOFS(monsterinfo.sight), F_FUNCTION, FFL_NOSPAWN},
            // {"checkattack", FOFS(monsterinfo.checkattack), F_FUNCTION, FFL_NOSPAWN},
            // {"currentmove", FOFS(monsterinfo.currentmove), F_MMOVE, FFL_NOSPAWN},
            // {"endfunc", FOFS(moveinfo.endfunc), F_FUNCTION, FFL_NOSPAWN},
            // {"lip", STOFS(lip), F_INT, FFL_SPAWNTEMP},
            // {"distance", STOFS(distance), F_INT, FFL_SPAWNTEMP},
            // {"height", STOFS(height), F_INT, FFL_SPAWNTEMP},
            new field_t(){name="noise", fname="noise", type=fieldtype_t.F_LSTRING, flags=FFL_SPAWNTEMP},
            // {"pausetime", STOFS(pausetime), F_FLOAT, FFL_SPAWNTEMP},
            // {"item", STOFS(item), F_LSTRING, FFL_SPAWNTEMP},
            // {"item", FOFS(item), F_ITEM},
            new field_t(){name="gravity", fname="gravity", type=fieldtype_t.F_LSTRING, flags=FFL_SPAWNTEMP},
            new field_t(){name="sky", fname="sky", type=fieldtype_t.F_LSTRING, flags=FFL_SPAWNTEMP},
            new field_t(){name="skyrotate", fname="skyrotate", type=fieldtype_t.F_FLOAT, flags=FFL_SPAWNTEMP},
            new field_t(){name="skyaxis", fname="skyaxis", type=fieldtype_t.F_VECTOR, flags=FFL_SPAWNTEMP},
            // {"minyaw", STOFS(minyaw), F_FLOAT, FFL_SPAWNTEMP},
            // {"maxyaw", STOFS(maxyaw), F_FLOAT, FFL_SPAWNTEMP},
            // {"minpitch", STOFS(minpitch), F_FLOAT, FFL_SPAWNTEMP},
            // {"maxpitch", STOFS(maxpitch), F_FLOAT, FFL_SPAWNTEMP},
            new field_t(){name="nextmap", fname="nextmap", type=fieldtype_t.F_LSTRING, flags=FFL_SPAWNTEMP}
        };

    }
}