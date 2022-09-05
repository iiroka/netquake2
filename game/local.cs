namespace Quake2 {

    partial class QuakeGame
    {
        /* this structure is left intact through an entire game
        it should be initialized at dll load time, and read/written to
        the server.ssv file for savegames */
        private struct game_locals_t
        {
            public string helpmessage1;
            public string helpmessage2;
            public int helpchanged; /* flash F1 icon if non 0, play sound
                                and increment only if 1, 2, or 3 */

            public gclient_t[] clients; /* [maxclients] */

            /* can't store spawnpoint in level, because
            it would get overwritten by the savegame
            restore */
            public string spawnpoint; /* needed for coop respawns */

            /* store latched cvars here that we want to get at often */
            public int maxclients;
            public int maxentities;

            /* cross level triggers */
            public int serverflags;

            /* items */
            public int num_items;

            public bool autosaved;
        }

        /* this structure is cleared on each PutClientInServer(),
        except for 'client->pers' */
        private class gclient_t : gclient_s
        {
            /* private to game */
            // client_persistant_t pers;
            // client_respawn_t resp;
            // pmove_state_t old_pmove; /* for detecting out-of-pmove changes */

            // qboolean showscores; /* set layout stat */
            // qboolean showinventory; /* set layout stat */
            // qboolean showhelp;
            // qboolean showhelpicon;

            // int ammo_index;

            // int buttons;
            // int oldbuttons;
            // int latched_buttons;

            // qboolean weapon_thunk;

            // gitem_t *newweapon;

            // /* sum up damage over an entire frame, so
            // shotgun blasts give a single big kick */
            // int damage_armor; /* damage absorbed by armor */
            // int damage_parmor; /* damage absorbed by power armor */
            // int damage_blood; /* damage taken out of health */
            // int damage_knockback; /* impact damage */
            // vec3_t damage_from; /* origin for vector calculation */

            // float killer_yaw; /* when dead, look at killer */

            // weaponstate_t weaponstate;
            // vec3_t kick_angles; /* weapon kicks */
            // vec3_t kick_origin;
            // float v_dmg_roll, v_dmg_pitch, v_dmg_time; /* damage kicks */
            // float fall_time, fall_value; /* for view drop on fall */
            // float damage_alpha;
            // float bonus_alpha;
            // vec3_t damage_blend;
            // vec3_t v_angle; /* aiming direction */
            // float bobtime; /* so off-ground doesn't change it */
            // vec3_t oldviewangles;
            // vec3_t oldvelocity;

            // float next_drown_time;
            // int old_waterlevel;
            // int breather_sound;

            // int machinegun_shots; /* for weapon raising */

            // /* animation vars */
            // int anim_end;
            // int anim_priority;
            // qboolean anim_duck;
            // qboolean anim_run;

            // /* powerup timers */
            // float quad_framenum;
            // float invincible_framenum;
            // float breather_framenum;
            // float enviro_framenum;

            // qboolean grenade_blew_up;
            // float grenade_time;
            // int silencer_shots;
            // int weapon_sound;

            // float pickup_msg_time;

            // float flood_locktill; /* locked from talking */
            // float flood_when[10]; /* when messages were said */
            // int flood_whenhead; /* head pointer for when said */

            // float respawn_time; /* can respawn when time > this */

            // edict_t *chase_target; /* player we are chasing */
            // qboolean update_chase; /* need to update chase info? */
        }

        private class edict_t : edict_s
        {
            public int movetype;
            public int flags;

            // char *model;
            // float freetime; /* sv.time when the object was freed */

            // /* only used locally in game, not by server */
            // char *message;
            // char *classname;
            // int spawnflags;

            // float timestamp;

            // float angle; /* set in qe3, -1 = up, -2 = down */
            // char *target;
            // char *targetname;
            // char *killtarget;
            // char *team;
            // char *pathtarget;
            // char *deathtarget;
            // char *combattarget;
            // edict_t *target_ent;

            // float speed, accel, decel;
            // vec3_t movedir;
            // vec3_t pos1, pos2;

            // vec3_t velocity;
            // vec3_t avelocity;
            // int mass;
            // float air_finished;
            // float gravity; /* per entity gravity multiplier (1.0 is normal)
            //                 use for lowgrav artifact, flares */

            // edict_t *goalentity;
            // edict_t *movetarget;
            // float yaw_speed;
            // float ideal_yaw;

            // float nextthink;
            // void (*prethink)(edict_t *ent);
            // void (*think)(edict_t *self);
            // void (*blocked)(edict_t *self, edict_t *other);
            // void (*touch)(edict_t *self, edict_t *other, cplane_t *plane,
            //         csurface_t *surf);
            // void (*use)(edict_t *self, edict_t *other, edict_t *activator);
            // void (*pain)(edict_t *self, edict_t *other, float kick, int damage);
            // void (*die)(edict_t *self, edict_t *inflictor, edict_t *attacker,
            //         int damage, vec3_t point);

            // float touch_debounce_time;
            // float pain_debounce_time;
            // float damage_debounce_time;
            // float fly_sound_debounce_time;	/* now also used by insane marines to store pain sound timeout */
            // float last_move_time;

            // int health;
            // int max_health;
            // int gib_health;
            // int deadflag;

            // float show_hostile;
            // float powerarmor_time;

            // char *map; /* target_changelevel */

            // int viewheight; /* height above origin where eyesight is determined */
            // int takedamage;
            // int dmg;
            // int radius_dmg;
            // float dmg_radius;
            // int sounds; /* now also used for player death sound aggregation */
            // int count;

            // edict_t *chain;
            // edict_t *enemy;
            // edict_t *oldenemy;
            // edict_t *activator;
            // edict_t *groundentity;
            // int groundentity_linkcount;
            // edict_t *teamchain;
            // edict_t *teammaster;

            // edict_t *mynoise; /* can go in client only */
            // edict_t *mynoise2;

            // int noise_index;
            // int noise_index2;
            // float volume;
            // float attenuation;

            // /* timing variables */
            // float wait;
            // float delay; /* before firing targets */
            // float random;

            // float last_sound_time;

            // int watertype;
            // int waterlevel;

            // vec3_t move_origin;
            // vec3_t move_angles;

            // /* move this to clientinfo? */
            // int light_level;

            // int style; /* also used as areaportal number */

            // gitem_t *item; /* for bonus items */

            // /* common data blocks */
            // moveinfo_t moveinfo;
            // monsterinfo_t monsterinfo;
        }  
    }
}
