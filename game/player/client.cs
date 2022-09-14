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
        private void InitClientResp(gclient_t client)
        {
            if (client == null)
            {
                return;
            }

            // memset(&client->resp, 0, sizeof(client->resp));
            // client->resp.enterframe = level.framenum;
            // client->resp.coop_respawn = client->pers;
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
            // client_respawn_t resp;

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

            // memcpy(userinfo, client->pers.userinfo, sizeof(userinfo));
            // ClientUserinfoChanged(ent, userinfo);

            /* clear everything but the persistant data */
            client.Clear();

            // if (client->pers.health <= 0)
            // {
            //     InitClientPersistant(client);
            // }

            // client.resp = resp;

            // /* copy some data from the client to the entity */
            // FetchClientEntData(ent);

            /* clear entity values */
            // ent.groundentity = NULL;
            ent.client = game.clients[index];
            // ent.takedamage = DAMAGE_AIM;
            ent.movetype = movetype_t.MOVETYPE_WALK;
            ent.viewheight = 22;
            ent.inuse = true;
            ent.classname = "player";
            ent.mass = 200;
            // ent->solid = SOLID_BBOX;
            // ent->deadflag = DEAD_NO;
            // ent->air_finished = level.time + 12;
            // ent->clipmask = MASK_PLAYERSOLID;
            // ent->model = "players/male/tris.md2";
            // ent->pain = player_pain;
            // ent->die = player_die;
            ent.waterlevel = 0;
            ent.watertype = 0;
            // ent->flags &= ~FL_NO_KNOCKBACK;
            ent.svflags = 0;

            ent.mins = mins;
            ent.maxs = maxs;
            ent.velocity = new Vector3();

            /* clear playerstate values */
            ent.client.ps = new QShared.player_state_t();


            client!.ps.pmove.origin = new short[3]{
                (short)(spawn_origin.X * 8),
                (short)(spawn_origin.Y * 8),
                (short)(spawn_origin.Z * 8)};

            // if (deathmatch->value && ((int)dmflags->value & DF_FIXED_FOV))
            // {
            //     client->ps.fov = 90;
            // }
            // else
            // {
            //     client->ps.fov = (int)strtol(Info_ValueForKey(client->pers.userinfo, "fov"), (char **)NULL, 10);

            //     if (client->ps.fov < 1)
            //     {
            //         client->ps.fov = 90;
            //     }
            //     else if (client->ps.fov > 160)
            //     {
            //         client->ps.fov = 160;
            //     }
            // }

            // client->ps.gunindex = gi.modelindex(client->pers.weapon->view_model);

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

            // /* set the delta angle */
            // for (i = 0; i < 3; i++)
            // {
            //     client->ps.pmove.delta_angles[i] = ANGLE2SHORT(
            //             spawn_angles[i] - client->resp.cmd_angles[i]);
            // }

            ent.s.angles.SetPitch(0);
            ent.s.angles.SetYaw(spawn_angles.Yaw());
            ent.s.angles.SetRoll(0);
            client.ps.viewangles = ent.s.angles;
            // client.v_angle = ent.s.angles;

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
                // client.resp.spectator = false;
            // }

            // if (!KillBox(ent))
            // {
            //     /* could't spawn in? */
            // }

            gi.linkentity(ent);

            // /* force the current weapon up */
            // client->newweapon = client->pers.weapon;
            // ChangeWeapon(ent);
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
                // for (i = 0; i < 3; i++)
                // {
                //     ent->client->ps.pmove.delta_angles[i] = ANGLE2SHORT(
                //             ent->client->ps.viewangles[i]);
                // }
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
            // ClientEndServerFrame(ent);
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
            //     /* clear the respawning variables */
            //     InitClientResp(ent->client);

            //     if (!game.autosaved || !ent->client->pers.weapon)
            //     {
            //         InitClientPersistant(ent->client);
            //     }
            }

            // ClientUserinfoChanged(ent, userinfo);

            // if (game.maxclients > 1)
            // {
            //     gi.dprintf("%s connected\n", ent->client->pers.netname);
            // }

            ent.svflags = 0; /* make sure we start with known default */
            // ent->client->pers.connected = true;
            return true;
        }
    }
}
