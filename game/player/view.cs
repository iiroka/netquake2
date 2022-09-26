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
 * The "camera" through which the player looks into the game.
 *
 * =======================================================================
 */
using System.Numerics;

namespace Quake2 {

    partial class QuakeGame
    {
        private edict_t? current_player;
        private gclient_t? current_client;

        private static Vector3 forward, right, up;
        private float xyspeed;

        private float bobmove;
        private int bobcycle; /* odd cycles are right foot going forward */
        private float bobfracsin; /* sin(bobfrac*M_PI) */

        /*
        * Called for each player at the end of
        * the server frame and right after spawning
        */
        private void ClientEndServerFrame(edict_t ent)
        {
            float bobtime;
            int i;

            if (ent == null)
            {
                return;
            }

            current_player = ent;
            current_client = (gclient_t)ent.client!;

            /* If the origin or velocity have changed since ClientThink(),
            update the pmove values. This will happen when the client
            is pushed by a bmodel or kicked by an explosion.
            If it wasn't updated here, the view position would lag a frame
            behind the body position when pushed -- "sinking into plats" */
            current_client.ps.pmove.origin[0] = (short)(ent.s.origin.X * 8.0);
            current_client.ps.pmove.origin[1] = (short)(ent.s.origin.Y * 8.0);
            current_client.ps.pmove.origin[2] = (short)(ent.s.origin.Z * 8.0);
            current_client.ps.pmove.velocity[0] = (short)(ent.velocity.X * 8.0);
            current_client.ps.pmove.velocity[1] = (short)(ent.velocity.Y * 8.0);
            current_client.ps.pmove.velocity[2] = (short)(ent.velocity.Z * 8.0);

            /* If the end of unit layout is displayed, don't give
            the player any normal movement attributes */
            if (level.intermissiontime > 0)
            {
                current_client.ps.blend[3] = 0;
                current_client.ps.fov = 90;
            //     G_SetStats(ent);
                return;
            }

            QShared.AngleVectors(current_client.v_angle, ref forward, ref right, ref up);

            // /* burn from lava, etc */
            // P_WorldEffects();

            // /* set model angles from view angles so other things in
            // the world can tell which direction you are looking */
            // if (ent->client->v_angle[PITCH] > 180)
            // {
            //     ent->s.angles[PITCH] = (-360 + ent->client->v_angle[PITCH]) / 3;
            // }
            // else
            // {
                ent.s.angles.SetPitch(current_client.v_angle.Pitch() / 3);
            // }

            ent.s.angles.SetYaw(current_client.v_angle.Yaw());
            ent.s.angles.SetRoll(0);
            // ent->s.angles[ROLL] = SV_CalcRoll(ent->s.angles, ent->velocity) * 4;

            /* calculate speed and cycle to be used for
            all cyclic walking effects */
            xyspeed = MathF.Sqrt(ent.velocity.X * ent.velocity.X + ent.velocity.Y * ent.velocity.Y);

            if (xyspeed < 5)
            {
                bobmove = 0;
                current_client.bobtime = 0; /* start at beginning of cycle again */
            }
            else if (ent.groundentity != null)
            {
                /* so bobbing only cycles when on ground */
                if (xyspeed > 210)
                {
                    bobmove = 0.25f;
                }
                else if (xyspeed > 100)
                {
                    bobmove = 0.125f;
                }
                else
                {
                    bobmove = 0.0625f;
                }
            }

            bobtime = (current_client.bobtime += bobmove);

            if ((current_client.ps.pmove.pm_flags & QShared.PMF_DUCKED) != 0)
            {
                bobtime *= 4;
            }

            bobcycle = (int)bobtime;
            bobfracsin = MathF.Abs(MathF.Sin(bobtime * MathF.PI));

            /* detect hitting the floor */
            // P_FallingDamage(ent);

            /* apply all the damage taken this frame */
            // P_DamageFeedback(ent);

            /* determine the view offsets */
            // SV_CalcViewOffset(ent);

            /* determine the gun offsets */
            // SV_CalcGunOffset(ent);

            /* determine the full screen color blend
            must be after viewoffset, so eye contents
            can be accurately determined */
            // SV_CalcBlend(ent);

            /* chase cam stuff */
            // if (ent->client->resp.spectator)
            // {
            //     G_SetSpectatorStats(ent);
            // }
            // else
            // {
            //     G_SetStats(ent);
            // }

            // G_CheckChaseStats(ent);

            // G_SetClientEvent(ent);

            // G_SetClientEffects(ent);

            // G_SetClientSound(ent);

            // G_SetClientFrame(ent);

            current_client.oldvelocity = ent.velocity;
            current_client.oldviewangles = ent.client!.ps.viewangles;

            /* clear weapon kicks */
            current_client.kick_origin = new Vector3();
            current_client.kick_angles = new Vector3();

            if ((level.framenum & 31) == 0)
            {
            //     /* if the scoreboard is up, update it */
            //     if (ent->client->showscores)
            //     {
            //         DeathmatchScoreboardMessage(ent, ent->enemy);
            //         gi.unicast(ent, false);
            //     }

            //     /* if the help computer is up, update it */
            //     if (ent->client->showhelp)
            //     {
            //         ent->client->pers.helpchanged = 0;
            //         HelpComputerMessage(ent);
            //         gi.unicast(ent, false);
            //     }
            }

            // /* if the inventory is up, update it */
            // if (ent->client->showinventory)
            // {
            //     InventoryMessage(ent);
            //     gi.unicast(ent, false);
            // }
        }

    }
}