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
 * Interface between client <-> game and client calculations.
 *
 * =======================================================================
 */

using System.Numerics;

namespace Quake2 {

    partial class QuakeGame
    {

        /*
        * Some maps have no unnamed (e.g. generic)
        * info_player_start. This is no problem in
        * normal gameplay, but if the map is loaded
        * via console there is a huge chance that
        * the player will spawn in the wrong point.
        * Therefore create an unnamed info_player_start
        * at the correct point.
        */
        private void SP_CreateUnnamedSpawn(edict_t self)
        {
            var spot = G_Spawn();

            if (self == null)
            {
                return;
            }

            /* mine1 */
            // if (Q_stricmp(level.mapname, "mine1") == 0)
            // {
            //     if (Q_stricmp(self->targetname, "mintro") == 0)
            //     {
            //         spot->classname = self->classname;
            //         spot->s.origin[0] = self->s.origin[0];
            //         spot->s.origin[1] = self->s.origin[1];
            //         spot->s.origin[2] = self->s.origin[2];
            //         spot->s.angles[1] = self->s.angles[1];
            //         spot->targetname = NULL;

            //         return;
            //     }
            // }

            // /* mine2 */
            // if (Q_stricmp(level.mapname, "mine2") == 0)
            // {
            //     if (Q_stricmp(self->targetname, "mine1") == 0)
            //     {
            //         spot->classname = self->classname;
            //         spot->s.origin[0] = self->s.origin[0];
            //         spot->s.origin[1] = self->s.origin[1];
            //         spot->s.origin[2] = self->s.origin[2];
            //         spot->s.angles[1] = self->s.angles[1];
            //         spot->targetname = NULL;

            //         return;
            //     }
            // }

            // /* mine3 */
            // if (Q_stricmp(level.mapname, "mine3") == 0)
            // {
            //     if (Q_stricmp(self->targetname, "mine2a") == 0)
            //     {
            //         spot->classname = self->classname;
            //         spot->s.origin[0] = self->s.origin[0];
            //         spot->s.origin[1] = self->s.origin[1];
            //         spot->s.origin[2] = self->s.origin[2];
            //         spot->s.angles[1] = self->s.angles[1];
            //         spot->targetname = NULL;

            //         return;
            //     }
            // }

            // /* mine4 */
            // if (Q_stricmp(level.mapname, "mine4") == 0)
            // {
            //     if (Q_stricmp(self->targetname, "mine3") == 0)
            //     {
            //         spot->classname = self->classname;
            //         spot->s.origin[0] = self->s.origin[0];
            //         spot->s.origin[1] = self->s.origin[1];
            //         spot->s.origin[2] = self->s.origin[2];
            //         spot->s.angles[1] = self->s.angles[1];
            //         spot->targetname = NULL;

            //         return;
            //     }
            // }

            // /* power2 */
            // if (Q_stricmp(level.mapname, "power2") == 0)
            // {
            //     if (Q_stricmp(self->targetname, "power1") == 0)
            //     {
            //         spot->classname = self->classname;
            //         spot->s.origin[0] = self->s.origin[0];
            //         spot->s.origin[1] = self->s.origin[1];
            //         spot->s.origin[2] = self->s.origin[2];
            //         spot->s.angles[1] = self->s.angles[1];
            //         spot->targetname = NULL;

            //         return;
            //     }
            // }

            // /* waste1 */
            // if (Q_stricmp(level.mapname, "waste1") == 0)
            // {
            //     if (Q_stricmp(self->targetname, "power2") == 0)
            //     {
            //         spot->classname = self->classname;
            //         spot->s.origin[0] = self->s.origin[0];
            //         spot->s.origin[1] = self->s.origin[1];
            //         spot->s.origin[2] = self->s.origin[2];
            //         spot->s.angles[1] = self->s.angles[1];
            //         spot->targetname = NULL;

            //         return;
            //     }
            // }

            // /* waste2 */
            // if (Q_stricmp(level.mapname, "waste2") == 0)
            // {
            //     if (Q_stricmp(self->targetname, "waste1") == 0)
            //     {
            //         spot->classname = self->classname;
            //         spot->s.origin[0] = self->s.origin[0];
            //         spot->s.origin[1] = self->s.origin[1];
            //         spot->s.origin[2] = self->s.origin[2];
            //         spot->s.angles[1] = self->s.angles[1];
            //         spot->targetname = NULL;

            //         return;
            //     }
            // }

            // /* city3 */
            // if (Q_stricmp(level.mapname, "city2") == 0)
            // {
            //     if (Q_stricmp(self->targetname, "city2NL") == 0)
            //     {
            //         spot->classname = self->classname;
            //         spot->s.origin[0] = self->s.origin[0];
            //         spot->s.origin[1] = self->s.origin[1];
            //         spot->s.origin[2] = self->s.origin[2];
            //         spot->s.angles[1] = self->s.angles[1];
            //         spot->targetname = NULL;

            //         return;
            //     }
            // }
        }

        /*
        * QUAKED info_player_start (1 0 0) (-16 -16 -24) (16 16 32)
        * The normal starting point for a level.
        */
        private static void SP_info_player_start(QuakeGame g, edict_t self)
        {
            if (g == null || self == null)
            {
                return;
            }

            /* Call function to hack unnamed spawn points */
            self.think = g.SP_CreateUnnamedSpawn;
            self.nextthink = g.level.time + FRAMETIME;

            if (!g.coop!.Bool)
            {
                return;
            }

            if (g.level.mapname.CompareTo("security") == 0)
            {
                /* invoke one of our gross, ugly, disgusting hacks */
                // self.think = SP_CreateCoopSpots;
                // self.nextthink = g.level.time + FRAMETIME;
            }
        }        
        /* ======================================================================= */

        /*
        * This is only called when the game first
        * initializes in single player, but is called
        * after each death and level change in deathmatch
        */
        private void InitClientPersistant(gclient_t client)
        {
            if (client == null)
            {
                return;
            }

            client.pers = new client_persistant_t();

            var item = FindItem("Blaster");
            client.pers.selected_item = item!.index;
            // client.pers.inventory[client.pers.selected_item] = 1;

            client.pers.weapon = item;

            client.pers.health = 100;
            client.pers.max_health = 100;

            client.pers.max_bullets = 200;
            client.pers.max_shells = 100;
            client.pers.max_rockets = 50;
            client.pers.max_grenades = 50;
            client.pers.max_cells = 200;
            client.pers.max_slugs = 50;

            client.pers.connected = true;
        }

        private void InitClientResp(gclient_t client)
        {
            if (client == null)
            {
                return;
            }

            client.resp = new client_respawn_t();
            client.resp.enterframe = level.framenum;
            client.resp.coop_respawn = client.pers;
        }

        /*
        * Chooses a player start, deathmatch start, coop start, etc
        */
        private void SelectSpawnPoint(in edict_t ent, out Vector3 origin, out Vector3 angles)
        {
            // edict_t *spot = NULL;
            // edict_t *coopspot = NULL;
            // int index;
            // int counter = 0;
            // vec3_t d;

            if (ent == null)
            {
                origin = new Vector3();
                angles = new Vector3();
                return;
            }

            edict_t? spot = null;
            // if (deathmatch!.Bool)
            // {
            //     spot = SelectDeathmatchSpawnPoint();
            // }
            // else if (coop->value)
            // {
            //     spot = SelectCoopSpawnPoint(ent);
            // }

            /* find a single player start spot */
            if (spot == null)
            {
                while ((spot = G_Find(spot, "classname", "info_player_start")) != null)
                {
                    if (String.IsNullOrEmpty(game.spawnpoint) && String.IsNullOrEmpty(spot.targetname))
                    {
                        break;
                    }

                    if (String.IsNullOrEmpty(game.spawnpoint) || String.IsNullOrEmpty(spot.targetname))
                    {
                        continue;
                    }

                    if (game.spawnpoint.Equals(spot.targetname))
                    {
                        break;
                    }
                }

                if (spot == null)
                {
                    if (String.IsNullOrEmpty(game.spawnpoint))
                    {
                        /* there wasn't a spawnpoint without a target, so use any */
                        spot = G_Find(spot, "classname", "info_player_start");
                    }

                    if (spot == null)
                    {
                        gi.error($"Couldn't find spawn point {game.spawnpoint}\n");
                    }
                }
            }

            /* If we are in coop and we didn't find a coop
            spawnpoint due to map bugs (not correctly
            connected or the map was loaded via console
            and thus no previously map is known to the
            client) use one in 550 units radius. */
            // if (coop->value)
            // {
            //     index = ent->client - game.clients;

            //     if (Q_stricmp(spot->classname, "info_player_start") == 0 && index != 0)
            //     {
            //         while(counter < 3)
            //         {
            //             coopspot = G_Find(coopspot, FOFS(classname), "info_player_coop");

            //             if (!coopspot)
            //             {
            //                 break;
            //             }

            //             VectorSubtract(coopspot->s.origin, spot->s.origin, d);

            //             if ((VectorLength(d) < 550))
            //             {
            //                 if (index == counter)
            //                 {
            //                     spot = coopspot;
            //                     break;
            //                 }
            //                 else
            //                 {
            //                     counter++;
            //                 }
            //             }
            //         }
            //     }
            // }

            origin = spot!.s.origin;
            origin.Z += 9;
            angles = spot!.s.angles;
        }

        /* ============================================================== */

        /*
        * Called when a player connects to
        * a server or respawns in a deathmatch.
        */
        private void PutClientInServer(ref edict_t ent)
        {
            // char userinfo[MAX_INFO_STRING];

            if (ent == null)
            {
                return;
            }

            var mins = new Vector3(-16f, -16f, -24f);
            var  maxs = new Vector3(16f, 16f, 32f);
            // int index;
            // vec3_t spawn_origin, spawn_angles;
            // gclient_t *client;
            // int i;
            // client_persistant_t saved;
            var resp = new client_respawn_t();

            /* find a spawn point do it before setting
               health back up, so farthest ranging
               doesn't count this client */
            SelectSpawnPoint(ent, out var spawn_origin, out var spawn_angles);

            var index = ent.index - 1;
            var client = (gclient_t)ent.client!;

            // /* deathmatch wipes most client data every spawn */
            // if (deathmatch->value)
            // {
            //     resp = client->resp;
            //     memcpy(userinfo, client->pers.userinfo, sizeof(userinfo));
            //     InitClientPersistant(client);
            //     ClientUserinfoChanged(ent, userinfo);
            // }
            // else if (coop->value)
            // {
            //     resp = client->resp;
            //     memcpy(userinfo, client->pers.userinfo, sizeof(userinfo));
            //     resp.coop_respawn.game_helpchanged = client->pers.game_helpchanged;
            //     resp.coop_respawn.helpchanged = client->pers.helpchanged;
            //     client->pers = resp.coop_respawn;
            //     ClientUserinfoChanged(ent, userinfo);

            //     if (resp.score > client->pers.score)
            //     {
            //         client->pers.score = resp.score;
            //     }
            // }
            // else
            // {
            //     memset(&resp, 0, sizeof(resp));
            // }

            var userinfo = client.pers.userinfo;
            ClientUserinfoChanged(ent, userinfo);

            /* clear everything but the persistant data */
            client.Clear();

            if (client.pers.health <= 0)
            {
                InitClientPersistant(client);
            }

            client.resp = resp;

            // /* copy some data from the client to the entity */
            // FetchClientEntData(ent);

            /* clear entity values */
            ent.groundentity = null;
            ent.client = game.clients[index];
            ent.takedamage = (int)damage_t.DAMAGE_AIM;
            ent.movetype = movetype_t.MOVETYPE_WALK;
            ent.viewheight = 22;
            ent.inuse = true;
            ent.classname = "player";
            ent.mass = 200;
            ent.solid = solid_t.SOLID_BBOX;
            ent.deadflag = DEAD_NO;
            ent.air_finished = level.time + 12;
            ent.clipmask = QShared.MASK_PLAYERSOLID;
            ent.model = "players/male/tris.md2";
            // ent->pain = player_pain;
            // ent->die = player_die;
            ent.waterlevel = 0;
            ent.watertype = 0;
            ent.flags &= ~FL_NO_KNOCKBACK;
            ent.svflags = 0;

            ent.mins = mins;
            ent.maxs = maxs;
            ent.velocity = new Vector3();

            /* clear playerstate values */
            ent.client.ps = new QShared.player_state_t();
            ent.client.ps.pmove = new QShared.pmove_state_t();
            ent.client.ps.pmove.origin = new short[3];
            ent.client.ps.pmove.velocity = new short[3];
            ent.client.ps.pmove.delta_angles = new short[3];

            client!.ps.pmove.origin = new short[3]{
                (short)(spawn_origin.X * 8),
                (short)(spawn_origin.Y * 8),
                (short)(spawn_origin.Z * 8)};

            // if (deathmatch->value && ((int)dmflags->value & DF_FIXED_FOV))
            // {
                // client.ps.fov = 90;
            // }
            // else
            // {
                client.ps.fov = Int32.Parse(QShared.Info_ValueForKey(client.pers.userinfo, "fov"));

                if (client.ps.fov < 1)
                {
                    client.ps.fov = 90;
                }
                else if (client.ps.fov > 160)
                {
                    client.ps.fov = 160;
                }
            // }

            client.ps.gunindex = gi.modelindex(client.pers.weapon!.view_model);

            /* clear entity state values */
            ent.s.effects = 0;
            ent.s.modelindex = 255; /* will use the skin specified model */
            ent.s.modelindex2 = 255; /* custom gun model */

            /* sknum is player num and weapon number
            weapon number will be added in changeweapon */
            ent.s.skinnum = ent.index - 1;

            ent.s.frame = 0;
            ent.s.origin = spawn_origin;
            ent.s.origin.Z += 1;  /* make sure off ground */
            ent.s.old_origin = ent.s.origin;

            /* set the delta angle */
            for (int i = 0; i < 3; i++)
            {
                client.ps.pmove.delta_angles[i] = QShared.ANGLE2SHORT(
                        spawn_angles.Get(i) - client.resp.cmd_angles.Get(i));
            }

            ent.s.angles.SetPitch(0);
            ent.s.angles.SetYaw(spawn_angles.Yaw());
            ent.s.angles.SetRoll(0);
            client.ps.viewangles = ent.s.angles;
            client.v_angle = ent.s.angles;

            /* spawn a spectator */
            // if (client->pers.spectator)
            // {
            //     client->chase_target = NULL;

            //     client->resp.spectator = true;

            //     ent->movetype = MOVETYPE_NOCLIP;
            //     ent->solid = SOLID_NOT;
            //     ent->svflags |= SVF_NOCLIENT;
            //     ent->client->ps.gunindex = 0;
            //     gi.linkentity(ent);
            //     return;
            // }
            // else
            // {
                client.resp.spectator = false;
            // }

            // if (!KillBox(ent))
            // {
            //     /* could't spawn in? */
            // }

            gi.linkentity(ent);

            /* force the current weapon up */
            client.newweapon = client.pers.weapon;
            ChangeWeapon(ent);
        }

        /*
        * called when a client has finished connecting, and is ready
        * to be placed into the game.  This will happen every level load.
        */
        public void ClientBegin(edict_s sent)
        {
            int i;
            Console.WriteLine("ClientBegin");

            if (sent == null || !(sent is edict_t))
            {
                return;
            }
            var ent = (edict_t)sent;

            ent.client = game.clients[ent.index - 1];

            // if (deathmatch->value)
            // {
            //     ClientBeginDeathmatch(ent);
            //     return;
            // }

            /* if there is already a body waiting for us (a loadgame),
            just take it, otherwise spawn one from scratch */
            Console.WriteLine($"ent.inuse {ent.inuse}");
            Console.WriteLine($"ent.index {ent.index}");
            Console.WriteLine($"ent.client {ent.client}");
            if (ent.inuse == true)
            {
                /* the client has cleared the client side viewangles upon
                connecting to the server, which is different than the
                state when the game is saved, so we need to compensate
                with deltaangles */
                ent.client.ps.pmove.delta_angles[0] = QShared.ANGLE2SHORT(ent.client.ps.viewangles.X);
                ent.client.ps.pmove.delta_angles[1] = QShared.ANGLE2SHORT(ent.client.ps.viewangles.Y);
                ent.client.ps.pmove.delta_angles[2] = QShared.ANGLE2SHORT(ent.client.ps.viewangles.Z);
            }
            else
            {
                /* a spawn point will completely reinitialize the entity
                except for the persistant data that was initialized at
                ClientConnect() time */
                G_InitEdict(ref ent);
                ent.classname = "player";
                InitClientResp((gclient_t)ent.client!);
                PutClientInServer(ref ent);
            }

            // if (level.intermissiontime)
            // {
            //     MoveClientToIntermission(ent);
            // }
            // else
            // {
            //     /* send effect if in a multiplayer game */
            //     if (game.maxclients > 1)
            //     {
            //         gi.WriteByte(svc_muzzleflash);
            //         gi.WriteShort(ent - g_edicts);
            //         gi.WriteByte(MZ_LOGIN);
            //         gi.multicast(ent->s.origin, MULTICAST_PVS);

            //         gi.bprintf(PRINT_HIGH, "%s entered the game\n",
            //                 ent->client->pers.netname);
            //     }
            // }

            /* make sure all view stuff is valid */
            ClientEndServerFrame(ent);
        }

        /*
        * Called whenever the player updates a userinfo variable.
        * The game can override any of the settings in place
        * (forcing skins or names, etc) before copying it off.
        */
        private void ClientUserinfoChanged(edict_t ent, string userinfo)
        {
            // char *s;
            // int playernum;

            if (ent == null || String.IsNullOrEmpty(userinfo))
            {
                return;
            }

            /* check for malformed or illegal info strings */
            if (!QShared.Info_Validate(userinfo))
            {
                userinfo = "\\name\\badinfo\\skin\\male/grunt";
            }

            var client = (gclient_t)ent.client!;

            /* set name */
            var s = QShared.Info_ValueForKey(userinfo, "name");
            client.pers.netname = s;

            /* set spectator */
            s = QShared.Info_ValueForKey(userinfo, "spectator");

            /* spectators are only supported in deathmatch */
            // if (deathmatch->value && *s && strcmp(s, "0"))
            // {
            //     ent->client->pers.spectator = true;
            // }
            // else
            // {
                client.pers.spectator = false;
            // }

            /* set skin */
            s = QShared.Info_ValueForKey(userinfo, "skin");

            var playernum = ent.index - 1;

            /* combine name and skin into a configstring */
            gi.configstring(QShared.CS_PLAYERSKINS + playernum, $"{client.pers.netname}\\{s}");

            /* fov */
            // if (deathmatch->value && ((int)dmflags->value & DF_FIXED_FOV))
            // {
            //     ((gclient_t)(ent.client!)).ps.fov = 90;
            // }
            // else
            {
                client.ps.fov = Int32.Parse(QShared.Info_ValueForKey(userinfo, "fov"));

                if (client.ps.fov < 1)
                {
                    client.ps.fov = 90;
                }
                else if (client.ps.fov > 160)
                {
                    client.ps.fov = 160;
                }
            }

            /* handedness */
            s = QShared.Info_ValueForKey(userinfo, "hand");

            if (s.Length > 0)
            {
                client.pers.hand = Int32.Parse(s);
            }

            /* save off the userinfo in case we want to check something later */
            client.pers.userinfo = userinfo;
        }


        public bool ClientConnect(edict_s sent, string userinfo)
        {
            if (sent == null || userinfo == null || !(sent is edict_t))
            {
                return false;
            }
            var ent = (edict_t)sent;

            // /* check to see if they are on the banned IP list */
            // value = Info_ValueForKey(userinfo, "ip");

            // if (SV_FilterPacket(value))
            // {
            //     Info_SetValueForKey(userinfo, "rejmsg", "Banned.");
            //     return false;
            // }

            // /* check for a spectator */
            // value = Info_ValueForKey(userinfo, "spectator");

            // if (deathmatch->value && *value && strcmp(value, "0"))
            // {
            //     int i, numspec;

            //     if (*spectator_password->string &&
            //         strcmp(spectator_password->string, "none") &&
            //         strcmp(spectator_password->string, value))
            //     {
            //         Info_SetValueForKey(userinfo, "rejmsg",
            //                 "Spectator password required or incorrect.");
            //         return false;
            //     }

            //     /* count spectators */
            //     for (i = numspec = 0; i < maxclients->value; i++)
            //     {
            //         if (g_edicts[i + 1].inuse && g_edicts[i + 1].client->pers.spectator)
            //         {
            //             numspec++;
            //         }
            //     }

            //     if (numspec >= maxspectators->value)
            //     {
            //         Info_SetValueForKey(userinfo, "rejmsg",
            //                 "Server spectator limit is full.");
            //         return false;
            //     }
            // }
            // else
            // {
            //     /* check for a password */
            //     value = Info_ValueForKey(userinfo, "password");

            //     if (*password->string && strcmp(password->string, "none") &&
            //         strcmp(password->string, value))
            //     {
            //         Info_SetValueForKey(userinfo, "rejmsg",
            //                 "Password required or incorrect.");
            //         return false;
            //     }
            // }

            /* they can connect */
            ent.client = game.clients[ent.index - 1];

            /* if there is already a body waiting for us (a loadgame),
            just take it, otherwise spawn one from scratch */
            if (ent.inuse == false)
            {
                /* clear the respawning variables */
                InitClientResp((gclient_t)ent.client);

                if (!game.autosaved || game.clients[ent.index - 1].pers.weapon == null)
                {
                    InitClientPersistant(game.clients[ent.index - 1]);
                }
            }

            ClientUserinfoChanged(ent, userinfo);

            // if (game.maxclients > 1)
            // {
            //     gi.dprintf("%s connected\n", ent->client->pers.netname);
            // }

            ent.svflags = 0; /* make sure we start with known default */
            game.clients[ent.index - 1].pers.connected = true;
            return true;
        }

        /* ============================================================== */

        private edict_t? pm_passent;

        /*
        * pmove doesn't need to know
        * about passent and contentmask
        */
        private QShared.trace_t PM_trace(in Vector3 start, in Vector3 mins, in Vector3 maxs, in Vector3 end)
        {
            if (pm_passent!.health > 0)
            {
                return gi.trace(start, mins, maxs, end, pm_passent, QShared.MASK_PLAYERSOLID);
            }
            else
            {
                return gi.trace(start, mins, maxs, end, pm_passent, QShared.MASK_DEADSOLID);
            }
        }

        /*
        * This will be called once for each client frame, which will
        * usually be a couple times for each server frame.
        */
        public void ClientThink(edict_s sent, in QShared.usercmd_t ucmd)
        {
            // gclient_t *client;
            // edict_t *other;
            // int i, j;
            // pmove_t pm;

            if (sent == null || !(sent is edict_t))
            {
                return;
            }
            var ent = (edict_t)sent;

            level.current_entity = ent;
            var client = (gclient_t)ent.client!;

            if (level.intermissiontime > 0)
            {
                client.ps.pmove.pm_type = QShared.pmtype_t.PM_FREEZE;

                /* can exit intermission after five seconds */
                if ((level.time > level.intermissiontime + 5.0) &&
                    (ucmd.buttons & QShared.BUTTON_ANY) != 0)
                {
                    level.exitintermission = 1;
                }

                return;
            }

            pm_passent = ent;

            if (client.chase_target != null)
            {
                client.resp.cmd_angles.X = QShared.SHORT2ANGLE(ucmd.angles[0]);
                client.resp.cmd_angles.Y = QShared.SHORT2ANGLE(ucmd.angles[1]);
                client.resp.cmd_angles.Z = QShared.SHORT2ANGLE(ucmd.angles[2]);
            }
            else
            {
                /* set up for pmove */
                var pm = new QShared.pmove_t();
                pm.touchents = new edict_s?[QShared.MAXTOUCH];

                if (ent.movetype == movetype_t.MOVETYPE_NOCLIP)
                {
                    client.ps.pmove.pm_type = QShared.pmtype_t.PM_SPECTATOR;
                }
                else if (ent.s.modelindex != 255)
                {
                    client.ps.pmove.pm_type = QShared.pmtype_t.PM_GIB;
                }
                else if (ent.deadflag != 0)
                {
                    client.ps.pmove.pm_type = QShared.pmtype_t.PM_DEAD;
                }
                else
                {
                    client.ps.pmove.pm_type = QShared.pmtype_t.PM_NORMAL;
                }

                client.ps.pmove.gravity = (short)sv_gravity!.Int;
                pm.s = client.ps.pmove;

                pm.s.origin[0] = (short)(ent.s.origin.X * 8);
                pm.s.origin[1] = (short)(ent.s.origin.Y * 8);
                pm.s.origin[2] = (short)(ent.s.origin.Z * 8);
                pm.s.velocity[0] = (short)(ent.velocity.X * 8);
                pm.s.velocity[1] = (short)(ent.velocity.Y * 8);
                pm.s.velocity[2] = (short)(ent.velocity.Z * 8);

                if (!client.old_pmove.Equals(pm.s))
                {
                    pm.snapinitial = true;
                }

                pm.cmd = ucmd;

                pm.trace = PM_trace; /* adds default parms */
            //     pm.pointcontents = gi.pointcontents;

                /* perform a pmove */
                gi.Pmove(ref pm);

                /* save results of pmove */
                client.ps.pmove = pm.s;
                client.old_pmove = pm.s;

                ent.s.origin = new Vector3(pm.s.origin[0]*0125f, pm.s.origin[1]*0125f, pm.s.origin[2]*0125f);
                ent.velocity = new Vector3(pm.s.velocity[0]*0125f, pm.s.velocity[1]*0125f, pm.s.velocity[2]*0125f);

                ent.mins = pm.mins;
                ent.maxs = pm.maxs;

                client.resp.cmd_angles.X = QShared.SHORT2ANGLE(ucmd.angles[0]);
                client.resp.cmd_angles.Y = QShared.SHORT2ANGLE(ucmd.angles[1]);
                client.resp.cmd_angles.Z = QShared.SHORT2ANGLE(ucmd.angles[2]);

            //     if (ent->groundentity && !pm.groundentity && (pm.cmd.upmove >= 10) &&
            //         (pm.waterlevel == 0))
            //     {
            //         gi.sound(ent, CHAN_VOICE, gi.soundindex(
            //                         "*jump1.wav"), 1, ATTN_NORM, 0);
            //         PlayerNoise(ent, ent->s.origin, PNOISE_SELF);
            //     }

                ent.viewheight = (int)pm.viewheight;
                ent.waterlevel = pm.waterlevel;
                ent.watertype = pm.watertype;
                ent.groundentity = (edict_t?)pm.groundentity;

                if (pm.groundentity != null)
                {
                    ent.groundentity_linkcount = pm.groundentity.linkcount;
                }

            //     if (ent->deadflag)
            //     {
            //         client->ps.viewangles[ROLL] = 40;
            //         client->ps.viewangles[PITCH] = -15;
            //         client->ps.viewangles[YAW] = client->killer_yaw;
            //     }
            //     else
            //     {
                client.v_angle = pm.viewangles;
                client.ps.viewangles = pm.viewangles;
            //     }

                gi.linkentity(ent);

            //     if (ent->movetype != MOVETYPE_NOCLIP)
            //     {
            //         G_TouchTriggers(ent);
            //     }

            //     /* touch other objects */
            //     for (i = 0; i < pm.numtouch; i++)
            //     {
            //         other = pm.touchents[i];

            //         for (j = 0; j < i; j++)
            //         {
            //             if (pm.touchents[j] == other)
            //             {
            //                 break;
            //             }
            //         }

            //         if (j != i)
            //         {
            //             continue; /* duplicated */
            //         }

            //         if (!other->touch)
            //         {
            //             continue;
            //         }

            //         other->touch(other, ent, NULL, NULL);
            //     }
            }

            client.oldbuttons = client.buttons;
            client.buttons = ucmd.buttons;
            client.latched_buttons |= client.buttons & ~client.oldbuttons;

            /* save light level the player is standing
               on for monster sighting AI */
            ent.light_level = ucmd.lightlevel;

            // /* fire weapon from final position if needed */
            // if (client->latched_buttons & BUTTON_ATTACK)
            // {
            //     if (client->resp.spectator)
            //     {
            //         client->latched_buttons = 0;

            //         if (client->chase_target)
            //         {
            //             client->chase_target = NULL;
            //             client->ps.pmove.pm_flags &= ~PMF_NO_PREDICTION;
            //         }
            //         else
            //         {
            //             GetChaseTarget(ent);
            //         }
            //     }
            //     else if (!client->weapon_thunk)
            //     {
            //         client->weapon_thunk = true;
            //         Think_Weapon(ent);
            //     }
            // }

            // if (client->resp.spectator)
            // {
            //     if (ucmd->upmove >= 10)
            //     {
            //         if (!(client->ps.pmove.pm_flags & PMF_JUMP_HELD))
            //         {
            //             client->ps.pmove.pm_flags |= PMF_JUMP_HELD;

            //             if (client->chase_target)
            //             {
            //                 ChaseNext(ent);
            //             }
            //             else
            //             {
            //                 GetChaseTarget(ent);
            //             }
            //         }
            //     }
            //     else
            //     {
            //         client->ps.pmove.pm_flags &= ~PMF_JUMP_HELD;
            //     }
            // }

            // /* update chase cam if being followed */
            // for (i = 1; i <= maxclients->value; i++)
            // {
            //     other = g_edicts + i;

            //     if (other->inuse && (other->client->chase_target == ent))
            //     {
            //         UpdateChaseCam(other);
            //     }
            // }
        }

        /*
        * This will be called once for each server
        * frame, before running any other entities
        * in the world.
        */
        private void ClientBeginServerFrame(edict_t ent)
        {
            if (ent == null)
            {
                return;
            }

            if (level.intermissiontime > 0)
            {
                return;
            }

            var client = (gclient_t)ent.client!;

            if (deathmatch!.Bool &&
                (client.pers.spectator != client.resp.spectator) &&
                ((level.time - client.respawn_time) >= 5))
            {
                // spectator_respawn(ent);
                return;
            }

            /* run weapon animations if it hasn't been done by a ucmd_t */
            if (!client.weapon_thunk && !client.resp.spectator)
            {
                // Think_Weapon(ent);
            }
            else
            {
                client.weapon_thunk = false;
            }

            if (ent.deadflag != 0)
            {
                /* wait for any button just going down */
                // if (level.time > client.respawn_time)
                // {
                //     /* in deathmatch, only wait for attack button */
                //     if (deathmatch!.Bool)
                //     {
                //         buttonMask = BUTTON_ATTACK;
                //     }
                //     else
                //     {
                //         buttonMask = -1;
                //     }

                //     if ((client.latched_buttons & buttonMask) != 0 ||
                //         (deathmatch!.Bool && (dmflags!.Int & DF_FORCE_RESPAWN) != 0))
                //     {
                //         // respawn(ent);
                //         client.latched_buttons = 0;
                //     }
                // }

                return;
            }

            /* add player trail so monsters can follow */
            if (!deathmatch!.Bool)
            {
                // if (!visible(ent, PlayerTrail_LastSpot()))
                // {
                //     PlayerTrail_Add(ent->s.old_origin);
                // }
            }

            client.latched_buttons = 0;
        }

    }
}
