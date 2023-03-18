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

        private static Vector3 _view_forward, _view_right, _view_up;
        private float xyspeed;

        private float bobmove;
        private int bobcycle; /* odd cycles are right foot going forward */
        private float bobfracsin; /* sin(bobfrac*M_PI) */

        private float SV_CalcRoll(in Vector3 angles, in Vector3 velocity)
        {
            var side = Vector3.Dot(velocity, _view_right);
            float sign = side < 0 ? -1 : 1;
            side = MathF.Abs(side);

            var value = sv_rollangle!.Float;

            if (side < sv_rollspeed!.Float)
            {
                side = side * value / sv_rollspeed!.Float;
            }
            else
            {
                side = value;
            }

            return side * sign;
        }

        /*
        * fall from 128: 400 = 160000
        * fall from 256: 580 = 336400
        * fall from 384: 720 = 518400
        * fall from 512: 800 = 640000
        * fall from 640: 960 =
        *
        * damage = deltavelocity*deltavelocity  * 0.0001
        */
        private void SV_CalcViewOffset(edict_t ent)
        {
            // float *angles;
            // float bob;
            float ratio;
            // float delta;
            // vec3_t v;

            /* base angles */
            ref var angles = ref ent.client!.ps.kick_angles;
            var client = (gclient_t)ent.client!;

            /* if dead, fix the angle and don't add any kick */
            if (ent.deadflag != 0)
            {
                angles = Vector3.Zero;

                ent.client.ps.viewangles.SetRoll(40);
                ent.client.ps.viewangles.SetPitch(-15);
                ent.client.ps.viewangles.SetYaw(client.killer_yaw);
            }
            else
            {
                /* add angles based on weapon kick */
                angles = client.kick_angles;

                /* add angles based on damage kick */
                ratio = (client.v_dmg_time - level.time) / DAMAGE_TIME;

                if (ratio < 0)
                {
                    ratio = 0;
                    client.v_dmg_pitch = 0;
                    client.v_dmg_roll = 0;
                }

                angles.SetPitch( angles.Pitch() + ratio * client.v_dmg_pitch);
                angles.SetRoll(angles.Roll() + ratio * client.v_dmg_roll);

                /* add pitch based on fall kick */
                ratio = (client.fall_time - level.time) / FALL_TIME;

                if (ratio < 0)
                {
                    ratio = 0;
                }

                angles.SetPitch(angles.Pitch() + ratio * client.fall_value);

                /* add angles based on velocity */
                var delta = Vector3.Dot(ent.velocity, _view_forward);
                angles.SetPitch(angles.Pitch() + delta * run_pitch!.Float);

                delta = Vector3.Dot(ent.velocity, _view_right);
                angles.SetRoll(angles.Roll() + delta * run_roll!.Float);

                /* add angles based on bob */
                delta = bobfracsin * bob_pitch!.Float * xyspeed;

                if ((ent.client.ps.pmove.pm_flags & QShared.PMF_DUCKED) != 0)
                {
                    delta *= 6; /* crouching */
                }

                angles.SetPitch(angles.Pitch() + delta);
                delta = bobfracsin * bob_roll!.Float * xyspeed;

                if ((ent.client.ps.pmove.pm_flags & QShared.PMF_DUCKED) != 0)
                {
                    delta *= 6; /* crouching */
                }

                if ((bobcycle & 1) != 0)
                {
                    delta = -delta;
                }

                angles.SetRoll(angles.Roll() + delta);
            }

            /* base origin */
            var v = Vector3.Zero;

            /* add view height */
            v.Z += ent.viewheight;

            /* add fall height */
            ratio = (client.fall_time - level.time) / FALL_TIME;

            if (ratio < 0)
            {
                ratio = 0;
            }

            v.Z -= ratio * client.fall_value * 0.4f;

            /* add bob height */
            var bob = bobfracsin * xyspeed * bob_up!.Float;

            if (bob > 6)
            {
                bob = 6;
            }

            v.Z += bob;

            /* add kick offset */
            v = v + client.kick_origin;

            /* absolutely bound offsets
            so the view can never be
            outside the player box */
            if (v.X < -14)
            {
                v.X = -14;
            }
            else if (v.X > 14)
            {
                v.X = 14;
            }

            if (v.Y < -14)
            {
                v.Y = -14;
            }
            else if (v.Y > 14)
            {
                v.Y = 14;
            }

            if (v.Z < -22)
            {
                v.Z = -22;
            }
            else if (v.Z > 30)
            {
                v.Z = 30;
            }

            ent.client.ps.viewoffset = v;
        }

        private void SV_CalcGunOffset(edict_t ent)
        {
            int i;
            float delta;

            if (ent == null || ent.client == null)
            {
                return;
            }

            /* gun angles from bobbing */
            ent.client.ps.gunangles[QShared.ROLL] = xyspeed * bobfracsin * 0.005f;
            ent.client.ps.gunangles[QShared.YAW] = xyspeed * bobfracsin * 0.01f;

            if ((bobcycle & 1) != 0)
            {
                ent.client.ps.gunangles[QShared.ROLL] = -ent.client.ps.gunangles[QShared.ROLL];
                ent.client.ps.gunangles[QShared.YAW] = -ent.client.ps.gunangles[QShared.YAW];
            }

            ent.client.ps.gunangles[QShared.PITCH] = xyspeed * bobfracsin * 0.005f;

            /* gun angles from delta movement */
            for (i = 0; i < 3; i++)
            {
                delta = ((gclient_t)ent.client).oldviewangles[i] - ent.client.ps.viewangles[i];

                if (delta > 180)
                {
                    delta -= 360;
                }

                if (delta < -180)
                {
                    delta += 360;
                }

                if (delta > 45)
                {
                    delta = 45;
                }

                if (delta < -45)
                {
                    delta = -45;
                }

                if (i == QShared.YAW)
                {
                    ent.client.ps.gunangles[QShared.ROLL] += 0.1f * delta;
                }

                ent.client.ps.gunangles[i] += 0.2f * delta;
            }

            /* gun height */
            ent.client.ps.gunoffset = Vector3.Zero;

            /* gun_x / gun_y / gun_z are development tools */
            for (i = 0; i < 3; i++)
            {
                ent.client.ps.gunoffset[i] += _view_forward[i] * (gun_y!.Float);
                ent.client.ps.gunoffset[i] += _view_right[i] * gun_x!.Float;
                ent.client.ps.gunoffset[i] += _view_up[i] * (-gun_z!.Float);
            }
        }

        private void G_SetClientFrame(edict_t ent)
        {
            // gclient_t *client;
            // qboolean duck, run;

            if (ent == null)
            {
                return;
            }

            if (ent.s.modelindex != 255)
            {
                return; /* not in the player model */
            }

            gclient_t client = (gclient_t)ent.client!;

            bool duck =  ((client.ps.pmove.pm_flags & QShared.PMF_DUCKED) != 0);

            bool run = (xyspeed != 0);
            bool newanim = false;

            /* check for stand/duck and stop/go transitions */
            if ((duck != client.anim_duck) && (client.anim_priority < ANIM_DEATH))
            {
                newanim = true;
            }

            if (!newanim && (run != client.anim_run) && (client.anim_priority == ANIM_BASIC))
            {
                newanim = true;
            }

            if (!newanim && ent.groundentity == null && (client.anim_priority <= ANIM_WAVE))
            {
                newanim = true;
            }

            if (!newanim) 
            {
                if (client.anim_priority == ANIM_REVERSE)
                {
                    if (ent.s.frame > client.anim_end)
                    {
                        ent.s.frame--;
                        return;
                    }
                }
                else if (ent.s.frame < client.anim_end)
                {
                    /* continue an animation */
                    ent.s.frame++;
                    return;
                }

                if (client.anim_priority == ANIM_DEATH)
                {
                    return; /* stay there */
                }

                if (client.anim_priority == ANIM_JUMP)
                {
                    if (ent.groundentity == null)
                    {
                        return; /* stay there */
                    }

                    client.anim_priority = ANIM_WAVE;
                    ent.s.frame = QuakeGamePlayer.FRAME_jump3;
                    client.anim_end = QuakeGamePlayer.FRAME_jump6;
                    return;
                }
            }

            /* return to either a running or standing frame */
            client.anim_priority = ANIM_BASIC;
            client.anim_duck = duck;
            client.anim_run = run;

            if (ent.groundentity == null)
            {
                client.anim_priority = ANIM_JUMP;

                if (ent.s.frame != QuakeGamePlayer.FRAME_jump2)
                {
                    ent.s.frame = QuakeGamePlayer.FRAME_jump1;
                }

                client.anim_end = QuakeGamePlayer.FRAME_jump2;
            }
            else if (run)
            {
                /* running */
                if (duck)
                {
                    ent.s.frame = QuakeGamePlayer.FRAME_crwalk1;
                    client.anim_end = QuakeGamePlayer.FRAME_crwalk6;
                }
                else
                {
                    ent.s.frame = QuakeGamePlayer.FRAME_run1;
                    client.anim_end = QuakeGamePlayer.FRAME_run6;
                }
            }
            else
            {
                /* standing */
                if (duck)
                {
                    ent.s.frame = QuakeGamePlayer.FRAME_crstnd01;
                    client.anim_end = QuakeGamePlayer.FRAME_crstnd19;
                }
                else
                {
                    ent.s.frame = QuakeGamePlayer.FRAME_stand01;
                    client.anim_end = QuakeGamePlayer.FRAME_stand40;
                }
            }
        }

        /*
        * Called for each player at the end of
        * the server frame and right after spawning
        */
        private void ClientEndServerFrame(edict_t ent)
        {
            float bobtime;

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
                G_SetStats(ent);
                return;
            }

            QShared.AngleVectors(current_client.v_angle, ref _view_forward, ref _view_right, ref _view_up);

            // /* burn from lava, etc */
            // P_WorldEffects();

            /* set model angles from view angles so other things in
               the world can tell which direction you are looking */
            if (current_client.v_angle.Pitch() > 180)
            {
                ent.s.angles.SetPitch((-360 + current_client.v_angle.Pitch()) / 3);
            }
            else
            {
                ent.s.angles.SetPitch(current_client.v_angle.Pitch() / 3);
            }

            ent.s.angles.SetYaw(current_client.v_angle.Yaw());
            ent.s.angles.SetRoll(0);
            ent.s.angles.SetRoll(SV_CalcRoll(ent.s.angles, ent.velocity) * 4);

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
            SV_CalcViewOffset(ent);

            /* determine the gun offsets */
            SV_CalcGunOffset(ent);

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
                G_SetStats(ent);
            // }

            // G_CheckChaseStats(ent);

            // G_SetClientEvent(ent);

            // G_SetClientEffects(ent);

            // G_SetClientSound(ent);

            G_SetClientFrame(ent);

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