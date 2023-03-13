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

        private const uint FL_FLY = 0x00000001;
        private const uint FL_SWIM = 0x00000002; /* implied immunity to drowining */
        private const uint FL_IMMUNE_LASER = 0x00000004;
        private const uint FL_INWATER = 0x00000008;
        private const uint FL_GODMODE = 0x00000010;
        private const uint FL_NOTARGET = 0x00000020;
        private const uint FL_IMMUNE_SLIME = 0x00000040;
        private const uint FL_IMMUNE_LAVA = 0x00000080;
        private const uint FL_PARTIALGROUND = 0x00000100; /* not all corners are valid */
        private const uint FL_WATERJUMP = 0x00000200; /* player jumping out of water */
        private const uint FL_TEAMSLAVE = 0x00000400; /* not the first on the team */
        private const uint FL_NO_KNOCKBACK = 0x00000800;
        private const uint FL_POWER_ARMOR = 0x00001000; /* power armor (if any) is active */
        private const uint FL_COOP_TAKEN = 0x00002000; /* Another client has already taken it */
        private const uint FL_RESPAWN = 0x80000000; /* used for item respawning */

        private const float FRAMETIME = 0.1f;

        private enum damage_t
        {
            DAMAGE_NO,
            DAMAGE_YES, /* will take damage if hit */
            DAMAGE_AIM /* auto targeting recognizes this */
        }

        /* deadflag */
        private const int DEAD_NO = 0;
        private const int DEAD_DYING = 1;
        private const int DEAD_DEAD = 2;
        private const int DEAD_RESPAWNABLE = 3;

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

        /* monster ai flags */
        private const int AI_STAND_GROUND = 0x00000001;
        private const int AI_TEMP_STAND_GROUND = 0x00000002;
        private const int AI_SOUND_TARGET = 0x00000004;
        private const int AI_LOST_SIGHT = 0x00000008;
        private const int AI_PURSUIT_LAST_SEEN = 0x00000010;
        private const int AI_PURSUE_NEXT = 0x00000020;
        private const int AI_PURSUE_TEMP = 0x00000040;
        private const int AI_HOLD_FRAME = 0x00000080;
        private const int AI_GOOD_GUY = 0x00000100;
        private const int AI_BRUTAL = 0x00000200;
        private const int AI_NOSTEP = 0x00000400;
        private const int AI_DUCKED = 0x00000800;
        private const int AI_COMBAT_POINT = 0x00001000;
        private const int AI_MEDIC = 0x00002000;
        private const int AI_RESURRECTING = 0x00004000;

        private struct gitem_armor_t
        {
            public int base_count;
            public int max_count;
            public float normal_protection;
            public float energy_protection;
            public int armor;
        }

        private const int IT_WEAPON = 1;  /* use makes active weapon */
        private const int IT_AMMO = 2;
        private const int IT_ARMOR = 4;
        private const int IT_STAY_COOP = 8;
        private const int IT_KEY = 16;
        private const int IT_POWERUP = 32;
        private const int IT_INSTANT_USE = 64; /* item is insta-used on pickup if dmflag is set */

        /* gitem_t->weapmodel for weapons indicates model index */
        private const int WEAP_BLASTER = 1;
        private const int WEAP_SHOTGUN = 2;
        private const int WEAP_SUPERSHOTGUN = 3;
        private const int WEAP_MACHINEGUN = 4;
        private const int WEAP_CHAINGUN = 5;
        private const int WEAP_GRENADES = 6;
        private const int WEAP_GRENADELAUNCHER = 7;
        private const int WEAP_ROCKETLAUNCHER = 8;
        private const int WEAP_HYPERBLASTER = 9;
        private const int WEAP_RAILGUN = 10;
        private const int WEAP_BFG = 11;

        private delegate void edict_delegate(edict_t ent);
        private delegate void edict_game_delegate(QuakeGame g, edict_t ent);
 
        private class gitem_t : ICloneable
        {
            public int index;
            public string classname { get; init; } /* spawning name */
            // qboolean (*pickup)(struct edict_s *ent, struct edict_s *other);
            // void (*use)(struct edict_s *ent, struct gitem_s *item);
            // void (*drop)(struct edict_s *ent, struct gitem_s *item);
            public edict_delegate? weaponthink  { get; init; }
            // char *pickup_sound;
            public string world_model;
            // int world_model_flags;
            public string view_model { get; init; }

            // /* client side info */
            // char *icon;
            public string pickup_name { get; init; } /* for printing on pickup */
            // int count_width; /* number of digits to display by icon */

            // int quantity; /* for ammo how much, for weapons how much is used per shot */
            // char *ammo; /* for weapons */
            // int flags; /* IT_* flags */

            // int weapmodel; /* weapon model index (for weapons) */

            // void *info;
            // int tag;

            // char *precaches; /* string of all models, sounds, and images this item will use */

            public object Clone()
            {
                return MemberwiseClone();
            }

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

        /* this structure is cleared as each map is entered
        it is read/written to the level.sav file for savegames */
        private struct level_locals_t
        {
            public int framenum;
            public float time;

            public string level_name; /* the descriptive name (Outer Base, etc) */
            public string mapname; /* the server name (base1, etc) */
            public string nextmap; /* go here when fraglimit is hit */

            /* intermission state */
            public float intermissiontime; /* time the intermission was started */
            public string changemap;
            public int exitintermission;
            public Vector3 intermission_origin;
            public Vector3 intermission_angle;

            public edict_t? sight_client; /* changed once each frame for coop games */

            public edict_t? sight_entity;
            public int sight_entity_framenum;
            public edict_t? sound_entity;
            public int sound_entity_framenum;
            public edict_t? sound2_entity;
            public int sound2_entity_framenum;

            public int pic_health;

            public int total_secrets;
            public int found_secrets;

            public int total_goals;
            public int found_goals;

            public int total_monsters;
            public int killed_monsters;

            public edict_t? current_entity; /* entity running from G_RunFrame */
            public int body_que; /* dead bodies */

            public int power_cubes; /* ugly necessity for coop */
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

        private struct moveinfo_t
        {
            /* fixed data */
            // vec3_t start_origin;
            // vec3_t start_angles;
            // vec3_t end_origin;
            // vec3_t end_angles;

            // int sound_start;
            // int sound_middle;
            // int sound_end;

            // float accel;
            // float speed;
            // float decel;
            // float distance;

            // float wait;

            // /* state data */
            // int state;
            // vec3_t dir;
            // float current_speed;
            // float move_speed;
            // float next_speed;
            // float remaining_distance;
            // float decel_distance;
            // void (*endfunc)(edict_t *);
        }

        private delegate void dist_game_delegate(QuakeGame g, edict_t self, float dist);
        private struct mframe_t
        {
            public dist_game_delegate? aifunc;
            public float dist;
            public edict_game_delegate? thinkfunc;

            public mframe_t(dist_game_delegate? ai, float d, edict_game_delegate? t) {
                this.aifunc = ai;
                this.dist = d;
                this.thinkfunc = t;
            }
        }

        private class mmove_t
        {
            public int firstframe;
            public int lastframe;
            public mframe_t[] frame;
            public edict_game_delegate? endfunc;

            public mmove_t(int first, int last, mframe_t[] frames, edict_game_delegate? end) {
                this.firstframe = first;
                this.lastframe = last;
                this.frame = frames;
                this.endfunc = end;
            }
        };

        private struct monsterinfo_t
        {
            public mmove_t? currentmove;
            public int aiflags;
            public int nextframe;
            public float scale;

            public edict_delegate? stand;
            public edict_delegate? idle;
            public edict_delegate? search;
            public edict_delegate? walk;
            public edict_delegate? run;
            // void (*dodge)(edict_t *self, edict_t *other, float eta);
            public edict_delegate? attack;
            public edict_delegate? melee;
            // void (*sight)(edict_t *self, edict_t *other);
            // qboolean (*checkattack)(edict_t *self);

            public float pausetime;
            public float attack_finished;

            // vec3_t saved_goal;
            public float search_time;
            public float trail_time;
            // vec3_t last_sighting;
            public int attack_state;
            // int lefty;
            public float idle_time;
            public int linkcount;

            public int power_armor_type;
            public int power_armor_power;
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

            public gitem_t? weapon;
            public gitem_t? lastweapon;

            public int power_cubes; /* used for tracking the cubes in coop games */
            public int score; /* for calculating total unit score in coop games */

            public int game_helpchanged;
            public int helpchanged;

            public bool spectator; /* client is a spectator */
        }

        /* client data that stays across deathmatch respawns */
        private struct client_respawn_t
        {
            public client_persistant_t coop_respawn; /* what to set client->pers to on a respawn */
            public int enterframe; /* level.framenum the client entered the game */
            public int score; /* frags, etc */
            public Vector3 cmd_angles; /* angles sent over in the last command */

            public bool spectator; /* client is a spectator */
        }

        /* this structure is cleared on each PutClientInServer(),
        except for 'client->pers' */
        private class gclient_t : gclient_s
        {
            /* private to game */
            public client_persistant_t pers;
            public client_respawn_t resp;
            public QShared.pmove_state_t old_pmove; /* for detecting out-of-pmove changes */

            public bool showscores; /* set layout stat */
            public bool showinventory; /* set layout stat */
            public bool showhelp;
            public bool showhelpicon;

            public int ammo_index;

            public int buttons;
            public int oldbuttons;
            public int latched_buttons;

            public bool weapon_thunk;

            public gitem_t? newweapon;

            /* sum up damage over an entire frame, so
            shotgun blasts give a single big kick */
            public int damage_armor; /* damage absorbed by armor */
            public int damage_parmor; /* damage absorbed by power armor */
            public int damage_blood; /* damage taken out of health */
            public int damage_knockback; /* impact damage */
            public Vector3 damage_from; /* origin for vector calculation */

            public float killer_yaw; /* when dead, look at killer */

            // weaponstate_t weaponstate;
            public Vector3 kick_angles; /* weapon kicks */
            public Vector3 kick_origin;
            public float v_dmg_roll, v_dmg_pitch, v_dmg_time; /* damage kicks */
            public float fall_time, fall_value; /* for view drop on fall */
            public float damage_alpha;
            public float bonus_alpha;
            public Vector3 damage_blend;
            public Vector3 v_angle; /* aiming direction */
            public float bobtime; /* so off-ground doesn't change it */
            public Vector3 oldviewangles;
            public Vector3 oldvelocity;

            public float next_drown_time;
            public int old_waterlevel;
            public int breather_sound;

            public int machinegun_shots; /* for weapon raising */

            /* animation vars */
            public int anim_end;
            public int anim_priority;
            public bool anim_duck;
            public bool anim_run;

            /* powerup timers */
            public float quad_framenum;
            public float invincible_framenum;
            public float breather_framenum;
            public float enviro_framenum;

            public bool grenade_blew_up;
            public float grenade_time;
            public int silencer_shots;
            public int weapon_sound;

            public float pickup_msg_time;

            public float flood_locktill; /* locked from talking */
            // float flood_when[10]; /* when messages were said */
            public int flood_whenhead; /* head pointer for when said */

            public float respawn_time; /* can respawn when time > this */

            public edict_t? chase_target; /* player we are chasing */
            public bool update_chase; /* need to update chase info? */

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
                newweapon = null;
                damage_armor = 0;
                damage_parmor = 0;
                damage_blood = 0;
                damage_knockback = 0;
                damage_from = new Vector3();
                killer_yaw = 0;
                // weaponstate_t weaponstate;
                kick_angles = new Vector3();
                kick_origin = new Vector3();
                v_dmg_roll = 0;
                v_dmg_pitch = 0;
                v_dmg_time = 0;
                fall_time = 0;
                fall_value = 0;
                damage_alpha = 0;
                bonus_alpha = 0;
                damage_blend = new Vector3();
                v_angle = new Vector3();
                bobtime = 0;
                oldviewangles = new Vector3();
                oldvelocity = new Vector3();
                next_drown_time = 0;
                old_waterlevel = 0;
                breather_sound = 0;
                machinegun_shots = 0; /* for weapon raising */
                anim_end = 0;
                anim_priority = 0;
                anim_duck = false;
                anim_run = false;
                quad_framenum = 0;
                invincible_framenum = 0;
                breather_framenum = 0;
                enviro_framenum = 0;
                grenade_blew_up = false;
                grenade_time = 0;
                silencer_shots = 0;
                weapon_sound = 0;
                pickup_msg_time = 0;
                flood_locktill = 0; /* locked from talking */
                // float flood_when[10]; /* when messages were said */
                flood_whenhead = 0; /* head pointer for when said */
                respawn_time = 0; /* can respawn when time > this */
                chase_target = null;
                update_chase = false;
            }
        }

        private class edict_t : edict_s
        {
            public int index { get; init; }
            public movetype_t movetype;
            public uint flags;

            public string model;
            public float freetime; /* sv.time when the object was freed */

            /* only used locally in game, not by server */
            public string message;
            public string classname;
            public int spawnflags;

            public float timestamp;

            public float angle; /* set in qe3, -1 = up, -2 = down */
            public string? target;
            public string? targetname;
            public string? killtarget;
            public string? team;
            public string? pathtarget;
            public string? deathtarget;
            public string? combattarget;
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

            public edict_t? goalentity;
            public edict_t? movetarget;
            public float yaw_speed;
            public float ideal_yaw;

            public float nextthink;
            public edict_delegate? prethink;
            public edict_delegate? think;
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

            public edict_t? chain;
            public edict_t? enemy;
            public edict_t? oldenemy;
            public edict_t? activator;
            public edict_t? groundentity;
            public int groundentity_linkcount;
            public edict_t? teamchain;
            public edict_t? teammaster;

            public edict_t? mynoise; /* can go in client only */
            public edict_t? mynoise2;

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

            public Vector3 move_origin;
            public Vector3 move_angles;

            /* move this to clientinfo? */
            public int light_level;

            public int style; /* also used as areaportal number */

            public gitem_t? item; /* for bonus items */

            // /* common data blocks */
            // moveinfo_t moveinfo;
            public monsterinfo_t monsterinfo = new monsterinfo_t();
        }  
    }
}
