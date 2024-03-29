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

        private const int MELEE_DISTANCE = 80;
        private const int BODY_QUEUE_SIZE = 8;

        private enum damage_t
        {
            DAMAGE_NO,
            DAMAGE_YES, /* will take damage if hit */
            DAMAGE_AIM /* auto targeting recognizes this */
        }

        private enum weaponstate_t
        {
            WEAPON_READY,
            WEAPON_ACTIVATING,
            WEAPON_DROPPING,
            WEAPON_FIRING
        }

        /* deadflag */
        private const int DEAD_NO = 0;
        private const int DEAD_DYING = 1;
        private const int DEAD_DEAD = 2;
        private const int DEAD_RESPAWNABLE = 3;

        /* range */
        private const int RANGE_MELEE = 0;
        private const int RANGE_NEAR = 1;
        private const int RANGE_MID = 2;
        private const int RANGE_FAR = 3;


        /* monster attack state */
        private const int AS_STRAIGHT = 1;
        private const int AS_SLIDING = 2;
        private const int AS_MELEE = 3;
        private const int AS_MISSILE = 4;

        /* armor types */
        private const int ARMOR_NONE = 0;
        private const int ARMOR_JACKET = 1;
        private const int ARMOR_COMBAT = 2;
        private const int ARMOR_BODY = 3;
        private const int ARMOR_SHARD = 4;

        /* handedness values */
        private const int RIGHT_HANDED = 0;
        private const int LEFT_HANDED = 1;
        private const int CENTER_HANDED = 2;

        /* noise types for PlayerNoise */
        private const int PNOISE_SELF = 0;
        private const int PNOISE_WEAPON = 1;
        private const int PNOISE_IMPACT = 2;

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


        private class gitem_armor_t : item_info
        {
            public int base_count;
            public int max_count;
            public float normal_protection;
            public float energy_protection;
            public int armor;
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
        private delegate bool pickup_delegate(QuakeGame g, edict_t ent, edict_t other);

        private delegate void item_delegate(edict_t ent, gitem_t item);

        private interface item_info {}

        private class gitem_t : ICloneable
        {
            public int index;
            public string? classname { get; init; } /* spawning name */
            public pickup_delegate? pickup;
            public item_delegate? use;
            public item_delegate? drop;
            public edict_game_delegate? weaponthink  { get; init; }
            // char *pickup_sound;
            public string? world_model { get; init; }
            public int world_model_flags { get; init; }
            public string? view_model { get; init; }

            /* client side info */
            public string? icon { get; init; }
            public string? pickup_name { get; init; } /* for printing on pickup */
            public int count_width { get; init; } /* number of digits to display by icon */

            // int quantity; /* for ammo how much, for weapons how much is used per shot */
            // char *ammo; /* for weapons */
            // int flags; /* IT_* flags */

            // int weapmodel; /* weapon model index (for weapons) */

            public item_info? info { get; init; }
            public int tag { get; init; }

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
            public Vector3 start_origin;
            public Vector3 start_angles;
            public Vector3 end_origin;
            public Vector3 end_angles;

            public int sound_start;
            public int sound_middle;
            public int sound_end;

            public float accel;
            public float speed;
            public float decel;
            public float distance;

            public float wait;

             /* state data */
            public int state;
            public Vector3 dir;
            public float current_speed;
            public float move_speed;
            public float next_speed;
            public float remaining_distance;
            public float decel_distance;
            public edict_delegate? endfunc;
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

        private delegate bool checkattack_delegate(edict_t self);

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
            public checkattack_delegate? checkattack;

            public float pausetime;
            public float attack_finished;

            public Vector3 saved_goal;
            public float search_time;
            public float trail_time;
            public Vector3 last_sighting;
            public int attack_state;
            // int lefty;
            public float idle_time;
            public int linkcount;

            public int power_armor_type;
            public int power_armor_power;
        }

        /* means of death */;
        private const int MOD_UNKNOWN = 0;
        private const int MOD_BLASTER = 1;
        private const int MOD_SHOTGUN = 2;
        private const int MOD_SSHOTGUN = 3;
        private const int MOD_MACHINEGUN = 4;
        private const int MOD_CHAINGUN = 5;
        private const int MOD_GRENADE = 6;
        private const int MOD_G_SPLASH = 7;
        private const int MOD_ROCKET = 8;
        private const int MOD_R_SPLASH = 9;
        private const int MOD_HYPERBLASTER = 10;
        private const int MOD_RAILGUN = 11;
        private const int MOD_BFG_LASER = 12;
        private const int MOD_BFG_BLAST = 13;
        private const int MOD_BFG_EFFECT = 14;
        private const int MOD_HANDGRENADE = 15;
        private const int MOD_HG_SPLASH = 16;
        private const int MOD_WATER = 17;
        private const int MOD_SLIME = 18;
        private const int MOD_LAVA = 19;
        private const int MOD_CRUSH = 20;
        private const int MOD_TELEFRAG = 21;
        private const int MOD_FALLING = 22;
        private const int MOD_SUICIDE = 23;
        private const int MOD_HELD_GRENADE = 24;
        private const int MOD_EXPLOSIVE = 25;
        private const int MOD_BARREL = 26;
        private const int MOD_BOMB = 27;
        private const int MOD_EXIT = 28;
        private const int MOD_SPLASH = 29;
        private const int MOD_TARGET_LASER = 30;
        private const int MOD_TRIGGER_HURT = 31;
        private const int MOD_HIT = 32;
        private const int MOD_TARGET_BLASTER = 33;
        private const int MOD_FRIENDLY_FIRE = 0x8000000;

        /* Easier handling of AI skill levels */
        private const int SKILL_EASY = 0;
        private const int SKILL_MEDIUM = 1;
        private const int SKILL_HARD = 2;
        private const int SKILL_HARDPLUS = 3;

        /* item spawnflags */
        private const int ITEM_TRIGGER_SPAWN = 0x00000001;
        private const int ITEM_NO_TOUCH = 0x00000002;
        /* 6 bits reserved for editor flags */
        /* 8 bits used as power cube id bits for coop games */
        private const int DROPPED_ITEM = 0x00010000;
        private const int DROPPED_PLAYER_ITEM = 0x00020000;
        private const int ITEM_TARGETS_USED = 0x00040000;

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

        /* damage flags */
        private const int DAMAGE_RADIUS = 0x00000001; /* damage was indirect */
        private const int DAMAGE_NO_ARMOR = 0x00000002; /* armour does not protect from this damage */
        private const int DAMAGE_ENERGY = 0x00000004; /* damage is from an energy based weapon */
        private const int DAMAGE_NO_KNOCKBACK = 0x00000008; /* do not affect velocity, just view angles */
        private const int DAMAGE_BULLET = 0x00000010; /* damage is from a bullet (used for ricochets) */
        private const int DAMAGE_NO_PROTECTION = 0x00000020; /* armor, shields, invulnerability, and godmode have no effect */

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
            public int[] inventory;

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

            public weaponstate_t weaponstate;
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

            // Clear everything except pers
            public void Clear()
            {
                resp = new client_respawn_t();
                old_pmove = new QShared.pmove_state_t();
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
                weaponstate = weaponstate_t.WEAPON_READY;
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

        private delegate void touch_delegate(edict_t self, edict_t other, QShared.cplane_t? plane,
                    in QShared.csurface_t? surf);
        private delegate void use_delegate(edict_t self, edict_t other, edict_t? activator);
        private delegate void pain_delegate(edict_t self, edict_t other, float kick, int damage);
        private delegate void die_delegate(edict_t self,  edict_t inflictor, edict_t attacker,
                    int damage, in Vector3 point);

        private class edict_t : edict_s
        {
            public int index { get; init; }
            public movetype_t movetype;
            public uint flags;

            public string? model;
            public float freetime; /* sv.time when the object was freed */

            /* only used locally in game, not by server */
            public string? message;
            // public string? classname;
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
            public edict_t? target_ent;

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
            public touch_delegate? touch;
            public use_delegate? use;
            public pain_delegate? pain;
            public die_delegate? die;

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

            /* common data blocks */
            public moveinfo_t moveinfo;
            public monsterinfo_t monsterinfo = new monsterinfo_t();

            public void Clear() {
                this.next = null;
                this.prev = null;
                this.s.effects = 0;
                this.s.ev = 0;
                this.s.frame = 0;
                this.s.modelindex = 0;
                this.s.modelindex2 = 0;
                this.s.modelindex3 = 0;
                this.s.modelindex4 = 0;
                this.s.number = 0;
                this.s.old_origin = Vector3.Zero;
                this.s.origin = Vector3.Zero;
                this.s.renderfx = 0;
                this.s.skinnum = 0;
                this.s.solid = 0;
                this.s.sound = 0;
                this.client = null;
                this.inuse = false;
                this.linkcount = 0;
                this.area.next = null;
                this.area.prev = null;
                this.num_clusters = 0;
                Array.Fill(this.clusternums, 0);
                this.headnode = 0; 
                this.areanum = 0;
                this.areanum2 = 0;
                this.svflags = 0;
                this.mins = Vector3.Zero;
                this.maxs = Vector3.Zero;
                this.absmin = Vector3.Zero;
                this.absmax = Vector3.Zero;
                this.size = Vector3.Zero;
                this.solid = solid_t.SOLID_NOT;
                this.clipmask = 0;
                this.owner = null;

                this.movetype = movetype_t.MOVETYPE_NONE;
                this.flags = 0;
                this.model = null;
                this.freetime = 0;
                this.message = null;
                this.classname = null;
                this.spawnflags = 0;
                this.timestamp = 0;
                this.angle = 0;
                this.target = null;
                this.targetname = null;
                this.killtarget = null;
                this.team = null;
                this.pathtarget = null;
                this.deathtarget = null;
                this.combattarget = null;
                this.target_ent = null;
                this.speed = 0;
                this.accel = 0;
                this.decel = 0;
                this.movedir = Vector3.Zero;
                this.pos1 = Vector3.Zero;
                this.pos2 = Vector3.Zero;
                this.velocity = Vector3.Zero;
                this.avelocity = Vector3.Zero;
                this.mass = 0;
                this.air_finished = 0;
                this.gravity = 0;
                this.goalentity = null;
                this.movetarget = null;
                this.yaw_speed = 0;
                this.ideal_yaw = 0;
                this.nextthink = 0;
                this.prethink = null;
                this.think = null;
                // // void (*blocked)(edict_t *self, edict_t *other);
                this.touch = null;
                this.use = null;
                this.pain = null;
                this.die = null;
                this.touch_debounce_time = 0;
                this.pain_debounce_time = 0;
                this.damage_debounce_time = 0;
                this.fly_sound_debounce_time = 0;
                this.last_move_time = 0;
                this.health = 0;
                this.max_health = 0;
                this.gib_health = 0;
                this.deadflag = 0;
                this.show_hostile = 0;
                this.powerarmor_time = 0;
                this.map = "";
                this.viewheight = 0;
                this.takedamage = 0;
                this.dmg = 0;
                this.radius_dmg = 0;
                this.dmg_radius = 0;
                this.sounds = 0;
                this.count = 0;
                this.chain = null;
                this.enemy = null;
                this.oldenemy = null;
                this.activator = null;
                this.groundentity = null;
                this.groundentity_linkcount = 0;
                this.teamchain = null;
                this.teammaster = null;
                this.mynoise = null;
                this.mynoise2 = null;
                this.noise_index = 0;
                this.noise_index2 = 0;
                this.volume = 0;
                this.attenuation = 0;
                this.wait = 0;
                this.delay = 0;
                this.random = 0;
                this.last_sound_time = 0;
                this.watertype = 0;
                this.waterlevel = 0;
                this.move_origin = Vector3.Zero;
                this.move_angles = Vector3.Zero;;
                this.light_level = 0;
                this.style = 0;
                this.item = null;
                this.moveinfo = new moveinfo_t();
                this.monsterinfo = new monsterinfo_t();
            }
        }  
    }
}
