using System.Numerics;

namespace Quake2 {

    partial class QuakeGame
    {
        /* protocol bytes that can be directly added to messages */
        private const int svc_muzzleflash = 1;
        private const int svc_muzzleflash2 = 2;
        private const int svc_temp_entity = 3;
        private const int svc_layout = 4;
        private const int svc_inventory = 5;
        private const int svc_stufftext = 11;

        /* ================================================================== */

        /* view pitching times */
        private const float DAMAGE_TIME = 0.5f;
        private const float FALL_TIME = 0.3f;

        /* these are set with checkboxes on each entity in the map editor */
        private const int SPAWNFLAG_NOT_EASY = 0x00000100;
        private const int SPAWNFLAG_NOT_MEDIUM = 0x00000200;
        private const int SPAWNFLAG_NOT_HARD = 0x00000400;
        private const int SPAWNFLAG_NOT_DEATHMATCH = 0x00000800;
        private const int SPAWNFLAG_NOT_COOP = 0x00001000;

        /* edict->movetype values */
        private enum movetype_t
        {
            MOVETYPE_NONE, /* never moves */
            MOVETYPE_NOCLIP, /* origin and angles change with no interaction */
            MOVETYPE_PUSH, /* no clip to world, push on box contact */
            MOVETYPE_STOP, /* no clip to world, stops on box contact */

            MOVETYPE_WALK, /* gravity */
            MOVETYPE_STEP, /* gravity, special edge handling */
            MOVETYPE_FLY,
            MOVETYPE_TOSS, /* gravity */
            MOVETYPE_FLYMISSILE, /* extra size to monsters */
            MOVETYPE_BOUNCE
        }

        private struct gitem_armor_t
        {
            public int base_count;
            public int max_count;
            public float normal_protection;
            public float energy_protection;
            public int armor;
        }

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

        /* spawn_temp_t is only used to hold entity field values that
        can be set from the editor, but aren't actualy present
        in edict_t during gameplay */
        private struct spawn_temp_t
        {
            /* world vars */
            public string sky;
            public float skyrotate;
            public Vector3 skyaxis;
            public string nextmap;

            public int lip;
            public int distance;
            public int height;
            public string noise;
            public float pausetime;
            public string item;
            public string gravity;

            public float minyaw;
            public float maxyaw;
            public float minpitch;
            public float maxpitch;
        }


        /* fields are needed for spawning from the entity
        string and saving / loading games */
        private const int FFL_SPAWNTEMP = 1;
        private const int FFL_NOSPAWN = 2;
        private const int FFL_ENTITYSTATE = 4;

        private enum fieldtype_t
        {
            F_INT,
            F_FLOAT,
            F_LSTRING, /* string on disk, pointer in memory, TAG_LEVEL */
            F_GSTRING, /* string on disk, pointer in memory, TAG_GAME */
            F_VECTOR,
            F_ANGLEHACK,
            F_EDICT, /* index on disk, pointer in memory */
            F_ITEM, /* index on disk, pointer in memory */
            F_CLIENT, /* index on disk, pointer in memory */
            F_FUNCTION,
            F_MMOVE,
            F_IGNORE
        }

        private record struct field_t
        {
            public string name { get; init; }
            public string fname { get; init; }
            public fieldtype_t type { get; init; }
            public int flags { get; init; }
            public short save_ver { get; init; }
        }

        /* ============================================================================ */

        /* client_t->anim_priority */
        private const int ANIM_BASIC = 0; /* stand / run */
        private const int ANIM_WAVE = 1;
        private const int ANIM_JUMP = 2;
        private const int ANIM_PAIN = 3;
        private const int ANIM_ATTACK = 4;
        private const int ANIM_DEATH = 5;
        private const int ANIM_REVERSE = 6;

        /* client data that stays across multiple level loads */
        private struct client_persistant_t
        {
            public string userinfo;
            public string  netname;
            public int hand;

            public bool connected; /* a loadgame will leave valid entities that
                                just don't have a connection yet */

            /* values saved and restored
            from edicts when changing levels */
            public int health;
            public int max_health;
            public int savedFlags;

            public int selected_item;
            // int inventory[MAX_ITEMS];

            /* ammo capacities */
            public int max_bullets;
            public int max_shells;
            public int max_rockets;
            public int max_grenades;
            public int max_cells;
            public int max_slugs;

            // gitem_t *weapon;
            // gitem_t *lastweapon;

            public int power_cubes; /* used for tracking the cubes in coop games */
            public int score; /* for calculating total unit score in coop games */

            public int game_helpchanged;
            public int helpchanged;

            public bool spectator; /* client is a spectator */
        }

        /* this structure is cleared on each PutClientInServer(),
        except for 'client->pers' */
        private class gclient_t : gclient_s
        {
            /* private to game */
            public client_persistant_t pers;
            // client_respawn_t resp;
            // pmove_state_t old_pmove; /* for detecting out-of-pmove changes */

            public bool showscores; /* set layout stat */
            public bool showinventory; /* set layout stat */
            public bool showhelp;
            public bool showhelpicon;

            public int ammo_index;

            public int buttons;
            public int oldbuttons;
            public int latched_buttons;

            public bool weapon_thunk;

            // gitem_t *newweapon;

            /* sum up damage over an entire frame, so
            shotgun blasts give a single big kick */
            public int damage_armor; /* damage absorbed by armor */
            public int damage_parmor; /* damage absorbed by power armor */
            public int damage_blood; /* damage taken out of health */
            public int damage_knockback; /* impact damage */
            // vec3_t damage_from; /* origin for vector calculation */

            public float killer_yaw; /* when dead, look at killer */

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

            public float next_drown_time;
            public int old_waterlevel;
            public int breather_sound;

            public int machinegun_shots; /* for weapon raising */

            /* animation vars */
            public int anim_end;
            public int anim_priority;
            public bool anim_duck;
            public bool anim_run;

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

            public void Clear()
            {
            // client_respawn_t resp;
            // pmove_state_t old_pmove; /* for detecting out-of-pmove changes */
                showscores = false;
                showinventory = false;
                showhelp = false;
                showhelpicon = false;
                ammo_index = 0;
                buttons = 0;
                oldbuttons = 0;
                latched_buttons = 0;
                weapon_thunk = false;
                // gitem_t *newweapon;
                damage_armor = 0;
                damage_parmor = 0; /* damage absorbed by power armor */
                damage_blood = 0; /* damage taken out of health */
                damage_knockback = 0; /* impact damage */
                // vec3_t damage_from; /* origin for vector calculation */
                killer_yaw = 0; /* when dead, look at killer */
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
                next_drown_time = 0;
                old_waterlevel = 0;
                breather_sound = 0;
                machinegun_shots = 0; /* for weapon raising */
                anim_end = 0;
                anim_priority = 0;
                anim_duck = false;
                anim_run = false;
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
        }

        private class edict_t : edict_s
        {
            public int index { get; init; }
            public movetype_t movetype;
            public int flags;

            public string model;
            public float freetime; /* sv.time when the object was freed */

            /* only used locally in game, not by server */
            public string message;
            public string classname;
            public int spawnflags;

            public float timestamp;

            public float angle; /* set in qe3, -1 = up, -2 = down */
            public string target;
            public string targetname;
            public string killtarget;
            public string team;
            public string pathtarget;
            public string deathtarget;
            public string combattarget;
            // edict_t *target_ent;

            public float speed, accel, decel;
            public Vector3 movedir;
            public Vector3 pos1, pos2;

            public Vector3 velocity;
            public Vector3 avelocity;
            public int mass;
            public float air_finished;
            public float gravity; /* per entity gravity multiplier (1.0 is normal)
                                     use for lowgrav artifact, flares */

            // edict_t *goalentity;
            // edict_t *movetarget;
            public float yaw_speed;
            public float ideal_yaw;

            public float nextthink;
            // void (*prethink)(edict_t *ent);
            // void (*think)(edict_t *self);
            // void (*blocked)(edict_t *self, edict_t *other);
            // void (*touch)(edict_t *self, edict_t *other, cplane_t *plane,
            //         csurface_t *surf);
            // void (*use)(edict_t *self, edict_t *other, edict_t *activator);
            // void (*pain)(edict_t *self, edict_t *other, float kick, int damage);
            // void (*die)(edict_t *self, edict_t *inflictor, edict_t *attacker,
            //         int damage, vec3_t point);

            public float touch_debounce_time;
            public float pain_debounce_time;
            public float damage_debounce_time;
            public float fly_sound_debounce_time;	/* now also used by insane marines to store pain sound timeout */
            public float last_move_time;

            public int health;
            public int max_health;
            public int gib_health;
            public int deadflag;

            public float show_hostile;
            public float powerarmor_time;

            public string map; /* target_changelevel */

            public int viewheight; /* height above origin where eyesight is determined */
            public int takedamage;
            public int dmg;
            public int radius_dmg;
            public float dmg_radius;
            public int sounds; /* now also used for player death sound aggregation */
            public int count;

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

            public int noise_index;
            public int noise_index2;
            public float volume;
            public float attenuation;

            /* timing variables */
            public float wait;
            public float delay; /* before firing targets */
            public float random;

            public float last_sound_time;

            public int watertype;
            public int waterlevel;

            // vec3_t move_origin;
            // vec3_t move_angles;

            /* move this to clientinfo? */
            public int light_level;

            public int style; /* also used as areaportal number */

            // gitem_t *item; /* for bonus items */

            // /* common data blocks */
            // moveinfo_t moveinfo;
            // monsterinfo_t monsterinfo;
        }  
    }
}
