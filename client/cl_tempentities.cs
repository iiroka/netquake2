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
 * This file implements all temporary (dynamic created) entities
 *
 * =======================================================================
 */
using System.Numerics;

namespace Quake2 {

    partial class QClient {

        private enum exptype_t
        {
            ex_free, ex_explosion, ex_misc, ex_flash, ex_mflash, ex_poly, ex_poly2
        }

        private struct explosion_t {
            public exptype_t type;
            public entity_t ent;

            public int frames;
            public float light;
            public Vector3 lightcolor;
            public float start;
            public int baseframe;

            public void Clear() {
                type = exptype_t.ex_free;
                ent.Clear();
                frames = 0;
                light = 0;
                lightcolor.X = 0;
                lightcolor.Y = 0;
                lightcolor.Z = 0;
                start = 0;
                baseframe = 0;
            }
        }

        const int MAX_EXPLOSIONS = 64;
        const int MAX_BEAMS = 64;
        const int MAX_LASERS = 64;

        private explosion_t[] cl_explosions = new explosion_t[MAX_EXPLOSIONS];

        private model_s? cl_mod_explode;
        private model_s? cl_mod_smoke;
        private model_s? cl_mod_flash;
        private model_s? cl_mod_explo4;
        private model_s? cl_mod_explo4_big;

        private void CL_RegisterTEntModels()
        {
            cl_mod_explode = vid.R_RegisterModel("models/objects/explode/tris.md2");
            cl_mod_smoke = vid.R_RegisterModel("models/objects/smoke/tris.md2");
            cl_mod_flash = vid.R_RegisterModel("models/objects/flash/tris.md2");
            // cl_mod_parasite_segment = R_RegisterModel("models/monsters/parasite/segment/tris.md2");
            // cl_mod_grapple_cable = R_RegisterModel("models/ctf/segment/tris.md2");
            // cl_mod_parasite_tip = R_RegisterModel("models/monsters/parasite/tip/tris.md2");
            cl_mod_explo4 = vid.R_RegisterModel("models/objects/r_explode/tris.md2");
            // cl_mod_bfg_explo = R_RegisterModel("sprites/s_bfg2.sp2");
            // cl_mod_powerscreen = R_RegisterModel("models/items/armor/effect/tris.md2");

            vid.R_RegisterModel("models/objects/laser/tris.md2");
            vid.R_RegisterModel("models/objects/grenade2/tris.md2");
            vid.R_RegisterModel("models/weapons/v_machn/tris.md2");
            vid.R_RegisterModel("models/weapons/v_handgr/tris.md2");
            vid.R_RegisterModel("models/weapons/v_shotg2/tris.md2");
            vid.R_RegisterModel("models/objects/gibs/bone/tris.md2");
            vid.R_RegisterModel("models/objects/gibs/sm_meat/tris.md2");
            vid.R_RegisterModel("models/objects/gibs/bone2/tris.md2");

            // Draw_FindPic("w_machinegun");
            // Draw_FindPic("a_bullets");
            // Draw_FindPic("i_health");
            // Draw_FindPic("a_grenades");

            cl_mod_explo4_big = vid.R_RegisterModel("models/objects/r_explode2/tris.md2");
            // cl_mod_lightning = R_RegisterModel("models/proj/lightning/tris.md2");
            // cl_mod_heatbeam = R_RegisterModel("models/proj/beam/tris.md2");
            // cl_mod_monster_heatbeam = R_RegisterModel("models/proj/widowbeam/tris.md2");
        }

        private void CL_ClearTEnts()
        {
            // memset(cl_beams, 0, sizeof(cl_beams));
            // memset(cl_explosions, 0, sizeof(cl_explosions));
            for (int i = 0; i < cl_explosions.Length; i++) cl_explosions[i].Clear();
            // memset(cl_lasers, 0, sizeof(cl_lasers));

            // memset(cl_playerbeams, 0, sizeof(cl_playerbeams));
            // memset(cl_sustains, 0, sizeof(cl_sustains));
        }


        private ref explosion_t CL_AllocExplosion()
        {
            for (int i = 0; i < MAX_EXPLOSIONS; i++)
            {
                if (cl_explosions[i].type == exptype_t.ex_free)
                {
                    cl_explosions[i].Clear();
                    return ref cl_explosions[i];
                }
            }

            /* find the oldest explosion */
            float time = (float)cl.time;
            int index = 0;

            for (int i = 0; i < MAX_EXPLOSIONS; i++)
            {
                if (cl_explosions[i].start < time)
                {
                    time = cl_explosions[i].start;
                    index = i;
                }
            }

            cl_explosions[index].Clear();
            return ref cl_explosions[index];
        }

        private void CL_SmokeAndFlash(in Vector3 origin)
        {
            var ex = CL_AllocExplosion();
            ex.ent.origin = origin;
            ex.type = exptype_t.ex_misc;
            ex.frames = 4;
            ex.ent.flags = QShared.RF_TRANSLUCENT;
            ex.start = cl.frame.servertime - 100.0f;
            ex.ent.model = cl_mod_smoke;

            ex = CL_AllocExplosion();
            ex.ent.origin = origin;
            ex.type = exptype_t.ex_flash;
            ex.ent.flags = QShared.RF_FULLBRIGHT;
            ex.frames = 2;
            ex.start = cl.frame.servertime - 100.0f;
            ex.ent.model = cl_mod_flash;
        }


        private void CL_ParseTEnt(ref QReadbuf msg)
        {
            // int type;
            // vec3_t pos, pos2, dir;
            // explosion_t *ex;
            // int cnt;
            // int color;
            // int r;
            // int ent;
            // int magnitude;

            var type = msg.ReadByte();

            switch (type)
            {
                case (int)QShared.temp_event_t.TE_BLOOD: /* bullet hitting flesh */
                    var pos = msg.ReadPos();
                    var dir = msg.ReadDir(common);
                    CL_ParticleEffect(pos, dir, 0xe8, 60);
                    break;

                case (int)QShared.temp_event_t.TE_GUNSHOT: /* bullet hitting wall */
                case (int)QShared.temp_event_t.TE_SPARKS:
                case (int)QShared.temp_event_t.TE_BULLET_SPARKS:
                    pos = msg.ReadPos();
                    dir = msg.ReadDir(common);

                    if (type == (int)QShared.temp_event_t.TE_GUNSHOT)
                    {
                        CL_ParticleEffect(pos, dir, 0, 40);
                    }
                    else
                    {
                        CL_ParticleEffect(pos, dir, 0xe0, 6);
                    }

                    if (type != (int)QShared.temp_event_t.TE_SPARKS)
                    {
                        CL_SmokeAndFlash(pos);
                        /* impact sound */
                //         cnt = randk() & 15;

                //         if (cnt == 1)
                //         {
                //             S_StartSound(pos, 0, 0, cl_sfx_ric1, 1, ATTN_NORM, 0);
                //         }
                //         else if (cnt == 2)
                //         {
                //             S_StartSound(pos, 0, 0, cl_sfx_ric2, 1, ATTN_NORM, 0);
                //         }
                //         else if (cnt == 3)
                //         {
                //             S_StartSound(pos, 0, 0, cl_sfx_ric3, 1, ATTN_NORM, 0);
                //         }
                    }

                    break;

                case (int)QShared.temp_event_t.TE_SCREEN_SPARKS:
                case (int)QShared.temp_event_t.TE_SHIELD_SPARKS:
                    pos = msg.ReadPos();
                    dir = msg.ReadDir(common);

                    if (type == (int)QShared.temp_event_t.TE_SCREEN_SPARKS)
                    {
                        CL_ParticleEffect(pos, dir, 0xd0, 40);
                    }

                    else
                    {
                        CL_ParticleEffect(pos, dir, 0xb0, 40);
                    }

                //     if (cl_limitsparksounds->value)
                //     {
                //         num_power_sounds++;

                //         /* If too many of these sounds are started in one frame
                //         * (for example if the player shoots with the super
                //         * shotgun into the power screen of a Brain) things get
                //         * too loud and OpenAL is forced to scale the volume of
                //         * several other sounds and the background music down.
                //         * That leads to a noticable and annoying drop in the
                //         * overall volume.
                //         *
                //         * Work around that by limiting the number of sounds
                //         * started.
                //         * 16 was choosen by empirical testing.
                //         *
                //         * This was fixed in openal-soft 0.19.0. We're keeping
                //         * the work around hidden behind a cvar and no longer
                //         * limited to OpenAL because a) some Linux distros may
                //         * still ship older openal-soft versions and b) some
                //         * player may like the changed behavior.
                //         */
                //         if (num_power_sounds < 16)
                //         {
                //             S_StartSound(pos, 0, 0, cl_sfx_lashit, 1, ATTN_NORM, 0);
                //         }
                //     }
                //     else
                //     {
                //         S_StartSound(pos, 0, 0, cl_sfx_lashit, 1, ATTN_NORM, 0);
                //     }

                    break;

                case (int)QShared.temp_event_t.TE_SHOTGUN: /* bullet hitting wall */
                    pos = msg.ReadPos();
                    dir = msg.ReadDir(common);
                    CL_ParticleEffect(pos, dir, 0, 20);
                    CL_SmokeAndFlash(pos);
                    break;

                // case TE_SPLASH: /* bullet hitting water */
                //     cnt = MSG_ReadByte(&net_message);
                //     MSG_ReadPos(&net_message, pos);
                //     MSG_ReadDir(&net_message, dir);
                //     r = MSG_ReadByte(&net_message);

                //     if (r > 6)
                //     {
                //         color = 0x00;
                //     }
                //     else
                //     {
                //         color = splash_color[r];
                //     }

                //     CL_ParticleEffect(pos, dir, color, cnt);

                //     if (r == SPLASH_SPARKS)
                //     {
                //         r = randk() & 3;

                //         if (r == 0)
                //         {
                //             S_StartSound(pos, 0, 0, cl_sfx_spark5, 1, ATTN_STATIC, 0);
                //         }
                //         else if (r == 1)
                //         {
                //             S_StartSound(pos, 0, 0, cl_sfx_spark6, 1, ATTN_STATIC, 0);
                //         }
                //         else
                //         {
                //             S_StartSound(pos, 0, 0, cl_sfx_spark7, 1, ATTN_STATIC, 0);
                //         }
                //     }

                //     break;

                // case TE_LASER_SPARKS:
                //     cnt = MSG_ReadByte(&net_message);
                //     MSG_ReadPos(&net_message, pos);
                //     MSG_ReadDir(&net_message, dir);
                //     color = MSG_ReadByte(&net_message);
                //     CL_ParticleEffect2(pos, dir, color, cnt);
                //     break;

                // case TE_BLUEHYPERBLASTER:
                //     MSG_ReadPos(&net_message, pos);
                //     MSG_ReadPos(&net_message, dir);
                //     CL_BlasterParticles(pos, dir);
                //     break;

                case (int)QShared.temp_event_t.TE_BLASTER: { /* blaster hitting wall */
                    pos = msg.ReadPos();
                    dir = msg.ReadDir(common);
                    CL_BlasterParticles(pos, dir);

                    var ex = CL_AllocExplosion();
                    ex.ent.origin = pos;
                    ex.ent.angles.X = MathF.Acos(dir.Z) / MathF.PI * 180;

                    if (dir.X != 0)
                    {
                        ex.ent.angles.Y = MathF.Atan2(dir.Y, dir.X) / MathF.PI * 180;
                    }

                    else if (dir.Y > 0)
                    {
                        ex.ent.angles.Y = 90;
                    }
                    else if (dir.Y < 0)
                    {
                        ex.ent.angles.Y = 270;
                    }
                    else
                    {
                        ex.ent.angles.Y = 0;
                    }

                    ex.type = exptype_t.ex_misc;
                    ex.ent.flags = 0;
                    ex.start = cl.frame.servertime - 100.0f;
                    ex.light = 150;
                    ex.lightcolor.X = 1;
                    ex.lightcolor.Y = 1;
                    ex.ent.model = cl_mod_explode;
                    ex.frames = 4;
                //     S_StartSound(pos, 0, 0, cl_sfx_lashit, 1, ATTN_NORM, 0);
                    break;
                }

                // case TE_RAILTRAIL: /* railgun effect */
                //     MSG_ReadPos(&net_message, pos);
                //     MSG_ReadPos(&net_message, pos2);
                //     CL_RailTrail(pos, pos2);
                //     S_StartSound(pos2, 0, 0, cl_sfx_railg, 1, ATTN_NORM, 0);
                //     break;

                case (int)QShared.temp_event_t.TE_EXPLOSION2:
                case (int)QShared.temp_event_t.TE_GRENADE_EXPLOSION:
                case (int)QShared.temp_event_t.TE_GRENADE_EXPLOSION_WATER: {
                    pos = msg.ReadPos();
                    var ex = CL_AllocExplosion();
                    ex.ent.origin = pos;
                    ex.type = exptype_t.ex_poly;
                    ex.ent.flags = QShared.RF_FULLBRIGHT | QShared.RF_NOSHADOW;
                    ex.start = cl.frame.servertime - 100.0f;
                    ex.light = 350;
                    ex.lightcolor.X = 1.0f;
                    ex.lightcolor.Y = 0.5f;
                    ex.lightcolor.Z = 0.5f;
                    ex.ent.model = cl_mod_explo4;
                    ex.frames = 19;
                    ex.baseframe = 30;
                    ex.ent.angles.Y = (float)(QShared.randk() % 360);
                    CL_ExplosionParticles(pos);

                //     if (type == TE_GRENADE_EXPLOSION_WATER)
                //     {
                //         S_StartSound(pos, 0, 0, cl_sfx_watrexp, 1, ATTN_NORM, 0);
                //     }
                //     else
                //     {
                //         S_StartSound(pos, 0, 0, cl_sfx_grenexp, 1, ATTN_NORM, 0);
                //     }

                    break;
                }

                // case TE_PLASMA_EXPLOSION:
                //     MSG_ReadPos(&net_message, pos);
                //     ex = CL_AllocExplosion();
                //     VectorCopy(pos, ex->ent.origin);
                //     ex->type = ex_poly;
                //     ex->ent.flags = RF_FULLBRIGHT | RF_NOSHADOW;
                //     ex->start = cl.frame.servertime - 100.0f;
                //     ex->light = 350;
                //     ex->lightcolor[0] = 1.0;
                //     ex->lightcolor[1] = 0.5;
                //     ex->lightcolor[2] = 0.5;
                //     ex->ent.angles[1] = (float)(randk() % 360);
                //     ex->ent.model = cl_mod_explo4;

                //     if (frandk() < 0.5)
                //     {
                //         ex->baseframe = 15;
                //     }

                //     ex->frames = 15;
                //     EXPLOSION_PARTICLES(pos);
                //     S_StartSound(pos, 0, 0, cl_sfx_rockexp, 1, ATTN_NORM, 0);
                //     break;

                case (int)QShared.temp_event_t.TE_EXPLOSION1_BIG:
                case (int)QShared.temp_event_t.TE_EXPLOSION1_NP:
                case (int)QShared.temp_event_t.TE_EXPLOSION1:
                case (int)QShared.temp_event_t.TE_ROCKET_EXPLOSION:
                case (int)QShared.temp_event_t.TE_ROCKET_EXPLOSION_WATER: {
                    pos = msg.ReadPos();
                    var ex = CL_AllocExplosion();
                    ex.ent.origin = pos;
                    ex.type = exptype_t.ex_poly;
                    ex.ent.flags = QShared.RF_FULLBRIGHT | QShared.RF_NOSHADOW;
                    ex.start = cl.frame.servertime - 100.0f;
                    ex.light = 350;
                    ex.lightcolor.X = 1.0f;
                    ex.lightcolor.Y = 0.5f;
                    ex.lightcolor.Z = 0.5f;
                    ex.ent.angles.X = (float)(QShared.randk() % 360);

                    if (type != (int)QShared.temp_event_t.TE_EXPLOSION1_BIG)
                    {
                        ex.ent.model = cl_mod_explo4;
                    }
                    else
                    {
                        ex.ent.model = cl_mod_explo4_big;
                    }

                    if (QShared.frandk() < 0.5)
                    {
                        ex.baseframe = 15;
                    }

                    ex.frames = 15;

                //     if ((type != TE_EXPLOSION1_BIG) && (type != TE_EXPLOSION1_NP))
                //     {
                //         CL_ExplosionParticles(pos);
                //     }

                //     if (type == TE_ROCKET_EXPLOSION_WATER)
                //     {
                //         S_StartSound(pos, 0, 0, cl_sfx_watrexp, 1, ATTN_NORM, 0);
                //     }
                //     else
                //     {
                //         S_StartSound(pos, 0, 0, cl_sfx_rockexp, 1, ATTN_NORM, 0);
                //     }

                    break;
                }

                // case TE_BFG_EXPLOSION:
                //     MSG_ReadPos(&net_message, pos);
                //     ex = CL_AllocExplosion();
                //     VectorCopy(pos, ex->ent.origin);
                //     ex->type = ex_poly;
                //     ex->ent.flags = RF_FULLBRIGHT | RF_NOSHADOW;
                //     ex->start = cl.frame.servertime - 100.0f;
                //     ex->light = 350;
                //     ex->lightcolor[0] = 0.0;
                //     ex->lightcolor[1] = 1.0;
                //     ex->lightcolor[2] = 0.0;
                //     ex->ent.model = cl_mod_bfg_explo;
                //     ex->ent.flags |= RF_TRANSLUCENT;
                //     ex->ent.alpha = 0.30f;
                //     ex->frames = 4;
                //     break;

                // case TE_BFG_BIGEXPLOSION:
                //     MSG_ReadPos(&net_message, pos);
                //     CL_BFGExplosionParticles(pos);
                //     break;

                // case TE_BFG_LASER:
                //     CL_ParseLaser(0xd0d1d2d3);
                //     break;

                // case TE_BUBBLETRAIL:
                //     MSG_ReadPos(&net_message, pos);
                //     MSG_ReadPos(&net_message, pos2);
                //     CL_BubbleTrail(pos, pos2);
                //     break;

                // case TE_PARASITE_ATTACK:
                // case TE_MEDIC_CABLE_ATTACK:
                //     CL_ParseBeam(cl_mod_parasite_segment);
                //     break;

                // case TE_BOSSTPORT: /* boss teleporting to station */
                //     MSG_ReadPos(&net_message, pos);
                //     CL_BigTeleportParticles(pos);
                //     S_StartSound(pos, 0, 0, S_RegisterSound(
                //                 "misc/bigtele.wav"), 1, ATTN_NONE, 0);
                //     break;

                // case TE_GRAPPLE_CABLE:
                //     CL_ParseBeam2(cl_mod_grapple_cable);
                //     break;

                // case TE_WELDING_SPARKS:
                //     cnt = MSG_ReadByte(&net_message);
                //     MSG_ReadPos(&net_message, pos);
                //     MSG_ReadDir(&net_message, dir);
                //     color = MSG_ReadByte(&net_message);
                //     CL_ParticleEffect2(pos, dir, color, cnt);

                //     ex = CL_AllocExplosion();
                //     VectorCopy(pos, ex->ent.origin);
                //     ex->type = ex_flash;
                //     ex->ent.flags = RF_BEAM;
                //     ex->start = cl.frame.servertime - 0.1f;
                //     ex->light = 100 + (float)(randk() % 75);
                //     ex->lightcolor[0] = 1.0f;
                //     ex->lightcolor[1] = 1.0f;
                //     ex->lightcolor[2] = 0.3f;
                //     ex->ent.model = cl_mod_flash;
                //     ex->frames = 2;
                //     break;

                // case TE_GREENBLOOD:
                //     MSG_ReadPos(&net_message, pos);
                //     MSG_ReadDir(&net_message, dir);
                //     CL_ParticleEffect2(pos, dir, 0xdf, 30);
                //     break;

                // case TE_TUNNEL_SPARKS:
                //     cnt = MSG_ReadByte(&net_message);
                //     MSG_ReadPos(&net_message, pos);
                //     MSG_ReadDir(&net_message, dir);
                //     color = MSG_ReadByte(&net_message);
                //     CL_ParticleEffect3(pos, dir, color, cnt);
                //     break;

                // case TE_BLASTER2:
                // case TE_FLECHETTE:
                //     MSG_ReadPos(&net_message, pos);
                //     MSG_ReadDir(&net_message, dir);

                //     if (type == TE_BLASTER2)
                //     {
                //         CL_BlasterParticles2(pos, dir, 0xd0);
                //     }
                //     else
                //     {
                //         CL_BlasterParticles2(pos, dir, 0x6f);
                //     }

                //     ex = CL_AllocExplosion();
                //     VectorCopy(pos, ex->ent.origin);
                //     ex->ent.angles[0] = (float)acos(dir[2]) / M_PI * 180;

                //     if (dir[0])
                //     {
                //         ex->ent.angles[1] = (float)atan2(dir[1], dir[0]) / M_PI * 180;
                //     }
                //     else if (dir[1] > 0)
                //     {
                //         ex->ent.angles[1] = 90;
                //     }

                //     else if (dir[1] < 0)
                //     {
                //         ex->ent.angles[1] = 270;
                //     }
                //     else
                //     {
                //         ex->ent.angles[1] = 0;
                //     }

                //     ex->type = ex_misc;
                //     ex->ent.flags = RF_FULLBRIGHT | RF_TRANSLUCENT;

                //     if (type == TE_BLASTER2)
                //     {
                //         ex->ent.skinnum = 1;
                //     }
                //     else /* flechette */
                //     {
                //         ex->ent.skinnum = 2;
                //     }

                //     ex->start = cl.frame.servertime - 100.0f;
                //     ex->light = 150;

                //     if (type == TE_BLASTER2)
                //     {
                //         ex->lightcolor[1] = 1;
                //     }
                //     else
                //     {
                //         /* flechette */
                //         ex->lightcolor[0] = 0.19f;
                //         ex->lightcolor[1] = 0.41f;
                //         ex->lightcolor[2] = 0.75f;
                //     }

                //     ex->ent.model = cl_mod_explode;
                //     ex->frames = 4;
                //     S_StartSound(pos, 0, 0, cl_sfx_lashit, 1, ATTN_NORM, 0);
                //     break;

                // case TE_LIGHTNING:
                //     ent = CL_ParseLightning(cl_mod_lightning);
                //     S_StartSound(NULL, ent, CHAN_WEAPON, cl_sfx_lightning,
                //         1, ATTN_NORM, 0);
                //     break;

                // case TE_DEBUGTRAIL:
                //     MSG_ReadPos(&net_message, pos);
                //     MSG_ReadPos(&net_message, pos2);
                //     CL_DebugTrail(pos, pos2);
                //     break;

                // case TE_PLAIN_EXPLOSION:
                //     MSG_ReadPos(&net_message, pos);

                //     ex = CL_AllocExplosion();
                //     VectorCopy(pos, ex->ent.origin);
                //     ex->type = ex_poly;
                //     ex->ent.flags = RF_FULLBRIGHT | RF_NOSHADOW;
                //     ex->start = cl.frame.servertime - 100.0f;
                //     ex->light = 350;
                //     ex->lightcolor[0] = 1.0;
                //     ex->lightcolor[1] = 0.5;
                //     ex->lightcolor[2] = 0.5;
                //     ex->ent.angles[1] = randk() % 360;
                //     ex->ent.model = cl_mod_explo4;

                //     if (frandk() < 0.5)
                //     {
                //         ex->baseframe = 15;
                //     }

                //     ex->frames = 15;

                //     S_StartSound(pos, 0, 0, cl_sfx_rockexp, 1, ATTN_NORM, 0);

                //     break;

                // case TE_FLASHLIGHT:
                //     MSG_ReadPos(&net_message, pos);
                //     ent = MSG_ReadShort(&net_message);
                //     CL_Flashlight(ent, pos);
                //     break;

                // case TE_FORCEWALL:
                //     MSG_ReadPos(&net_message, pos);
                //     MSG_ReadPos(&net_message, pos2);
                //     color = MSG_ReadByte(&net_message);
                //     CL_ForceWall(pos, pos2, color);
                //     break;

                // case TE_HEATBEAM:
                //     CL_ParsePlayerBeam(cl_mod_heatbeam);
                //     break;

                // case TE_MONSTER_HEATBEAM:
                //     CL_ParsePlayerBeam(cl_mod_monster_heatbeam);
                //     break;

                // case TE_HEATBEAM_SPARKS:
                //     cnt = 50;
                //     MSG_ReadPos(&net_message, pos);
                //     MSG_ReadDir(&net_message, dir);
                //     r = 8;
                //     magnitude = 60;
                //     color = r & 0xff;
                //     CL_ParticleSteamEffect(pos, dir, color, cnt, magnitude);
                //     S_StartSound(pos, 0, 0, cl_sfx_lashit, 1, ATTN_NORM, 0);
                //     break;

                // case TE_HEATBEAM_STEAM:
                //     cnt = 20;
                //     MSG_ReadPos(&net_message, pos);
                //     MSG_ReadDir(&net_message, dir);
                //     color = 0xe0;
                //     magnitude = 60;
                //     CL_ParticleSteamEffect(pos, dir, color, cnt, magnitude);
                //     S_StartSound(pos, 0, 0, cl_sfx_lashit, 1, ATTN_NORM, 0);
                //     break;

                // case TE_STEAM:
                //     CL_ParseSteam();
                //     break;

                // case TE_BUBBLETRAIL2:
                //     MSG_ReadPos(&net_message, pos);
                //     MSG_ReadPos(&net_message, pos2);
                //     CL_BubbleTrail2(pos, pos2, 8);
                //     S_StartSound(pos, 0, 0, cl_sfx_lashit, 1, ATTN_NORM, 0);
                //     break;

                // case TE_MOREBLOOD:
                //     MSG_ReadPos(&net_message, pos);
                //     MSG_ReadDir(&net_message, dir);
                //     CL_ParticleEffect(pos, dir, 0xe8, 250);
                //     break;

                // case TE_CHAINFIST_SMOKE:
                //     dir[0] = 0;
                //     dir[1] = 0;
                //     dir[2] = 1;
                //     MSG_ReadPos(&net_message, pos);
                //     CL_ParticleSmokeEffect(pos, dir, 0, 20, 20);
                //     break;

                // case TE_ELECTRIC_SPARKS:
                //     MSG_ReadPos(&net_message, pos);
                //     MSG_ReadDir(&net_message, dir);
                //     CL_ParticleEffect(pos, dir, 0x75, 40);
                //     S_StartSound(pos, 0, 0, cl_sfx_lashit, 1, ATTN_NORM, 0);
                //     break;

                // case TE_TRACKER_EXPLOSION:
                //     MSG_ReadPos(&net_message, pos);
                //     CL_ColorFlash(pos, 0, 150, -1, -1, -1);
                //     CL_ColorExplosionParticles(pos, 0, 1);
                //     S_StartSound(pos, 0, 0, cl_sfx_disrexp, 1, ATTN_NORM, 0);
                //     break;

                // case TE_TELEPORT_EFFECT:
                // case TE_DBALL_GOAL:
                //     MSG_ReadPos(&net_message, pos);
                //     CL_TeleportParticles(pos);
                //     break;

                // case TE_WIDOWBEAMOUT:
                //     CL_ParseWidow();
                //     break;

                // case TE_NUKEBLAST:
                //     CL_ParseNuke();
                //     break;

                // case TE_WIDOWSPLASH:
                //     MSG_ReadPos(&net_message, pos);
                //     CL_WidowSplash(pos);
                //     break;

                default:
                    common.Com_Error(QShared.ERR_DROP, $"CL_ParseTEnt: bad type {(QShared.temp_event_t)type}");
                    break;
            }
        }

        private void CL_AddExplosions()
        {
            for (int i = 0; i < MAX_EXPLOSIONS; i++)
            {
                ref var ex = ref cl_explosions[i];
                if (ex.type == exptype_t.ex_free)
                {
                    continue;
                }

                float frac = (cl.time - ex.start) / 100.0f;
                int f = (int)MathF.Floor(frac);

                ref var ent = ref ex.ent;

                switch (ex.type)
                {
                    case exptype_t.ex_mflash:

                        if (f >= ex.frames - 1)
                        {
                            ex.type = exptype_t.ex_free;
                        }

                        break;
                    case exptype_t.ex_misc:

                        if (f >= ex.frames - 1)
                        {
                            ex.type = exptype_t.ex_free;
                            break;
                        }

                        ent.alpha = 1.0f - frac / (ex.frames - 1);
                        break;
                    case exptype_t.ex_flash:

                        if (f >= 1)
                        {
                            ex.type = exptype_t.ex_free;
                            break;
                        }

                        ent.alpha = 1.0f;
                        break;
                    case exptype_t.ex_poly:

                        if (f >= ex.frames - 1)
                        {
                            ex.type = exptype_t.ex_free;
                            break;
                        }

                        ent.alpha = (16.0f - (float)f) / 16.0f;

                        if (f < 10)
                        {
                            ent.skinnum = (f >> 1);

                            if (ent.skinnum < 0)
                            {
                                ent.skinnum = 0;
                            }
                        }
                        else
                        {
                            ent.flags |= QShared.RF_TRANSLUCENT;

                            if (f < 13)
                            {
                                ent.skinnum = 5;
                            }

                            else
                            {
                                ent.skinnum = 6;
                            }
                        }

                        break;
                    case exptype_t.ex_poly2:

                        if (f >= ex.frames - 1)
                        {
                            ex.type = exptype_t.ex_free;
                            break;
                        }

                        ent.alpha = (5.0f - (float)f) / 5.0f;
                        ent.skinnum = 0;
                        ent.flags |= QShared.RF_TRANSLUCENT;
                        break;
                    default:
                        break;
                }

                if (ex.type == exptype_t.ex_free)
                {
                    continue;
                }

                if (ex.light != 0)
                {
                    V_AddLight(ent.origin, ex.light * ent.alpha,
                            ex.lightcolor.X, ex.lightcolor.Y, ex.lightcolor.Z);
                }

                ent.oldorigin = ent.origin;

                if (f < 0)
                {
                    f = 0;
                }

                ent.frame = ex.baseframe + f + 1;
                ent.oldframe = ex.baseframe + f;
                ent.backlerp = 1.0f - cl.lerpfrac;

                V_AddEntity(ent);
            }
        }

        private void CL_AddTEnts()
        {
            // CL_AddBeams();
            // CL_AddPlayerBeams();
            CL_AddExplosions();
            // CL_AddLasers();
            // CL_ProcessSustain();
        }


    }
}