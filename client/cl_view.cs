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
 *  =======================================================================
 *
 * This file implements the camera, e.g the player's view
 *
 * =======================================================================
 */
using System.Numerics;

namespace Quake2 {

    partial class QClient {

        private lightstyle_t[] r_lightstyles = new lightstyle_t[QRef.MAX_LIGHTSTYLES];
        private int r_numdlights;
        private dlight_t[] r_dlights = new dlight_t[QRef.MAX_DLIGHTS];

        private entity_t[] r_entities = new entity_t[QRef.MAX_ENTITIES];
        private int r_numentities;
        private int r_numparticles;
        private particle_t[] r_particles = new particle_t[QRef.MAX_PARTICLES];

        private string[] cl_weaponmodels = {};

        /*
        * Specifies the model that will be used as the world
        */
        private void V_ClearScene()
        {
            r_numdlights = 0;
            r_numentities = 0;
            r_numparticles = 0;
        }

        private void V_AddEntity(in entity_t ent)
        {
            if (r_numentities >= QRef.MAX_ENTITIES)
            {
                return;
            }

            r_entities[r_numentities++] = ent;
        }

        private void V_AddParticle(in Vector3 org, uint color, float alpha)
        {
            if (r_numparticles >= QRef.MAX_PARTICLES)
            {
                return;
            }

            r_particles[r_numparticles].origin = org;
            r_particles[r_numparticles].color = (int)color;
            r_particles[r_numparticles].alpha = alpha;
            r_numparticles += 1;
        }

        private void V_AddLightStyle(int style, float r, float g, float b)
        {
            if ((style < 0) || (style > QRef.MAX_LIGHTSTYLES))
            {
                common.Com_Error(QShared.ERR_DROP, $"Bad light style {style}");
            }

            r_lightstyles[style].white = r + g + b;
            r_lightstyles[style].rgb[0] = r;
            r_lightstyles[style].rgb[1] = g;
            r_lightstyles[style].rgb[2] = b;
        }

        private void V_AddLight(in Vector3 org, float intensity, float r, float g, float b)
        {
            if (r_numdlights >= QRef.MAX_DLIGHTS)
            {
                return;
            }

            ref var dl = ref r_dlights[r_numdlights++];
            dl.origin = org;
            dl.intensity = intensity;
            dl.color.X = r;
            dl.color.Y = g;
            dl.color.Z = b;
        }

        /*
        * Call before entering a new level, or after changing dlls
        */
        private void CL_PrepRefresh()
        {
            // char mapname[MAX_QPATH];
            // int i;
            // char name[MAX_QPATH];
            // float rotate;
            // vec3_t axis;

            if (String.IsNullOrEmpty(cl.configstrings[QShared.CS_MODELS + 1]))
            {
                return;
            }

            SCR_AddDirtyPoint(0, 0);
            SCR_AddDirtyPoint(vid.viddef.width - 1, vid.viddef.height - 1);

            /* let the refresher load the map */
            var mapname = cl.configstrings[QShared.CS_MODELS + 1].Substring(5); /* skip "maps/" */
            mapname = mapname.Substring(0, mapname.Length - 4); /* cut off ".bsp" */

            /* register models, pics, and skins */
            common.Com_Printf($"Map: {mapname}\r");
            SCR_UpdateScreen();
            vid.R_BeginRegistration (mapname);
            common.Com_Printf("                                     \r");

            /* precache status bar pics */
            common.Com_Printf("pics\r");
            SCR_UpdateScreen();
            // SCR_TouchPics();
            common.Com_Printf("                                     \r");

            CL_RegisterTEntModels();

            // num_cl_weaponmodels = 1;
            // strcpy(cl_weaponmodels[0], "weapon.md2");

            for (int i = 1; i < QShared.MAX_MODELS && !String.IsNullOrEmpty(cl.configstrings[QShared.CS_MODELS + i]); i++)
            {
                var name = cl.configstrings[QShared.CS_MODELS + i];

                if (name[0] != '*')
                {
                    common.Com_Printf($"{name}\r");
                }

                SCR_UpdateScreen();
                input.Update();

                if (name[0] == '#')
                {
                    /* special player weapon model */
            //         if (num_cl_weaponmodels < MAX_CLIENTWEAPONMODELS)
            //         {
                    cl_weaponmodels.Append(cl.configstrings[QShared.CS_MODELS + i].Substring(1));
            //             Q_strlcpy(cl_weaponmodels[num_cl_weaponmodels],
            //                     cl.configstrings[CS_MODELS + i] + 1,
            //                     sizeof(cl_weaponmodels[num_cl_weaponmodels]));
            //             num_cl_weaponmodels++;
            //         }
                }
                else
                {
                    cl.model_draw[i] = vid.R_RegisterModel(cl.configstrings[QShared.CS_MODELS + i]);

                    if (name[0] == '*')
                    {
                        cl.model_clip[i] = common.CM_InlineModel(cl.configstrings[QShared.CS_MODELS + i]);
                    }

                    else
                    {
                        cl.model_clip[i] = null;
                    }
                }

                if (name[0] != '*')
                {
                    common.Com_Printf("                                     \r");
                }
            }

            common.Com_Printf("images\r");
            SCR_UpdateScreen();

            // for (i = 1; i < MAX_IMAGES && cl.configstrings[CS_IMAGES + i][0]; i++)
            // {
            //     cl.image_precache[i] = Draw_FindPic(cl.configstrings[CS_IMAGES + i]);
            //     input.Update();
            // }

            common.Com_Printf("                                     \r");

            for (int i = 0; i < QShared.MAX_CLIENTS; i++)
            {
                if (String.IsNullOrEmpty(cl.configstrings[QShared.CS_PLAYERSKINS + i]))
                {
                    continue;
                }

                common.Com_Printf($"client {i}\r");
                SCR_UpdateScreen();
                input.Update();
                CL_ParseClientinfo(i);
                common.Com_Printf("                                     \r");
            }

            CL_LoadClientinfo(ref cl.baseclientinfo, "unnamed\\male/grunt");

            /* set sky textures and speed */
            common.Com_Printf("sky\r");
            SCR_UpdateScreen();
            var rotate = Convert.ToSingle(cl.configstrings[QShared.CS_SKYROTATE], QShared.provider);
            var axisStrs = cl.configstrings[QShared.CS_SKYAXIS].Split(' ');
            var axis = new Vector3(
                Convert.ToSingle(axisStrs[0], QShared.provider),
                Convert.ToSingle(axisStrs[1], QShared.provider),
                Convert.ToSingle(axisStrs[2], QShared.provider)
            );
            vid.R_SetSky(cl.configstrings[QShared.CS_SKY], rotate, axis);
            common.Com_Printf("                                     \r");

            /* the renderer can now free unneeded stuff */
            // R_EndRegistration();

            /* clear any lines of console text */
            // Con_ClearNotify();

            SCR_UpdateScreen();
            cl.refresh_prepped = true;
            cl.force_refdef = true; /* make sure we have a valid refdef */

            // /* start the cd track */
            // int track = (int)strtol(cl.configstrings[CS_CDTRACK], (char **)NULL, 10);

            // OGG_PlayTrack(track);
        }

        private float CalcFov(float fov_x, float width, float height)
        {
            float a;
            float x;

            if ((fov_x < 1) || (fov_x > 179))
            {
                common.Com_Error(QShared.ERR_DROP, $"Bad fov: {fov_x}");
            }

            x = width / MathF.Tan(fov_x / 360 * MathF.PI);

            a = MathF.Atan(height / x);

            a = a * 360 / MathF.PI;

            return a;
        }


        private void V_RenderView(float stereo_separation)
        {
            if (cls.state != connstate_t.ca_active)
            {
                // R_EndWorldRenderpass();
                return;
            }

            if (!cl.refresh_prepped)
            {
                // R_EndWorldRenderpass();
                return;			// still loading
            }

            // if (cl_timedemo->value)
            // {
            //     if (!cl.timedemo_start)
            //     {
            //         cl.timedemo_start = Sys_Milliseconds();
            //     }

            //     cl.timedemo_frames++;
            // }

            /* an invalid frame will just use the exact previous refdef
            we can't use the old frame if the video mode has changed, though... */
            if (cl.frame.valid && (cl.force_refdef || !(cl_paused?.Bool ?? false)))
            {
                cl.force_refdef = false;

                V_ClearScene();

                /* build a refresh entity list and calc cl.sim*
                this also calls CL_CalcViewValues which loads
                v_forward, etc. */
                CL_AddEntities();

                // before changing viewport we should trace the crosshair position
                // V_Render3dCrosshair();

                // if (cl_testparticles->value)
                // {
                //     V_TestParticles();
                // }

                // if (cl_testentities->value)
                // {
                //     V_TestEntities();
                // }

                // if (cl_testlights->value)
                // {
                //     V_TestLights();
                // }

                // if (cl_testblend->value)
                // {
                //     cl.refdef.blend[0] = 1;
                //     cl.refdef.blend[1] = 0.5;
                //     cl.refdef.blend[2] = 0.25;
                //     cl.refdef.blend[3] = 0.5;
                // }

                /* offset vieworg appropriately if
                // we're doing stereo separation */

                // if (stereo_separation != 0)
                // {
                //     vec3_t tmp;

                //     VectorScale(cl.v_right, stereo_separation, tmp);
                //     VectorAdd(cl.refdef.vieworg, tmp, cl.refdef.vieworg);
                // }

                /* never let it sit exactly on a node line, because a water plane can
                dissapear when viewed with the eye exactly on it. the server protocol
                only specifies to 1/8 pixel, so add 1/16 in each axis */
                cl.refdef.vieworg.X += 1.0f / 16;
                cl.refdef.vieworg.Y += 1.0f / 16;
                cl.refdef.vieworg.Z += 1.0f / 16;

                cl.refdef.time = cl.time * 0.001f;

                cl.refdef.areabits = cl.frame.areabits;

                // if (!cl_add_entities->value)
                // {
                //     r_numentities = 0;
                // }

                // if (!cl_add_particles->value)
                // {
                //     r_numparticles = 0;
                // }

                // if (!cl_add_lights->value)
                // {
                //     r_numdlights = 0;
                // }

                // if (!cl_add_blend->value)
                // {
                //     VectorClear(cl.refdef.blend);
                // }

                cl.refdef.num_entities = r_numentities;
                cl.refdef.entities = r_entities;
                cl.refdef.num_particles = r_numparticles;
                cl.refdef.particles = r_particles;
                cl.refdef.num_dlights = r_numdlights;
                cl.refdef.dlights = r_dlights;
                cl.refdef.lightstyles = r_lightstyles;

                cl.refdef.rdflags = cl.frame.playerstate.rdflags;

                /* sort entities for better cache locality */
                // qsort(cl.refdef.entities, cl.refdef.num_entities,
                //         sizeof(cl.refdef.entities[0]), (int (*)(const void *, const void *))
                //         entitycmpfnc);
            } else if (cl.frame.valid && (cl_paused?.Bool ?? false) && (gl1_stereo?.Bool ?? false)) {
                // We need to adjust the refdef in stereo mode when paused.
                // vec3_t tmp;
                CL_CalcViewValues();
                // VectorScale( cl.v_right, stereo_separation, tmp );
                // VectorAdd( cl.refdef.vieworg, tmp, cl.refdef.vieworg );

                cl.refdef.vieworg.X += 1.0f/16;
                cl.refdef.vieworg.Y += 1.0f/16;
                cl.refdef.vieworg.Z += 1.0f/16;

                cl.refdef.time = cl.time*0.001f;
            }

            cl.refdef.x = scr_vrect.x;
            cl.refdef.y = scr_vrect.y;
            cl.refdef.width = scr_vrect.width;
            cl.refdef.height = scr_vrect.height;
            cl.refdef.fov_y = CalcFov(cl.refdef.fov_x, (float)cl.refdef.width, (float)cl.refdef.height);

            vid.R_RenderFrame(cl.refdef);

            // if (cl_stats->value)
            // {
            //     Com_Printf("ent:%i  lt:%i  part:%i\n", r_numentities,
            //             r_numdlights, r_numparticles);
            // }

            // if (log_stats->value && (log_stats_file != 0))
            // {
            //     fprintf(log_stats_file, "%i,%i,%i,", r_numentities,
            //             r_numdlights, r_numparticles);
            // }

            // SCR_AddDirtyPoint(scr_vrect.x, scr_vrect.y);
            // SCR_AddDirtyPoint(scr_vrect.x + scr_vrect.width - 1,
            //         scr_vrect.y + scr_vrect.height - 1);

            // SCR_DrawCrosshair();
        }

        private void V_Init()
        {
            for (int i = 0; i < r_lightstyles.Length; i++) {
                r_lightstyles[i].rgb = new float[3]{ 1, 1, 1 };
                r_lightstyles[i].white = 3;
            }
        //     Cmd_AddCommand("gun_next", V_Gun_Next_f);
        //     Cmd_AddCommand("gun_prev", V_Gun_Prev_f);
        //     Cmd_AddCommand("gun_model", V_Gun_Model_f);

        //     Cmd_AddCommand("viewpos", V_Viewpos_f);

        //     crosshair = Cvar_Get("crosshair", "0", CVAR_ARCHIVE);
        //     crosshair_scale = Cvar_Get("crosshair_scale", "-1", CVAR_ARCHIVE);
        //     cl_testblend = Cvar_Get("cl_testblend", "0", 0);
        //     cl_testparticles = Cvar_Get("cl_testparticles", "0", 0);
        //     cl_testentities = Cvar_Get("cl_testentities", "0", 0);
        //     cl_testlights = Cvar_Get("cl_testlights", "0", 0);

        //     cl_stats = Cvar_Get("cl_stats", "0", 0);
        }

    }
}