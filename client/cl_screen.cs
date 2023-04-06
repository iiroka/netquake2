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
 * This file implements the 2D stuff. For example the HUD and the
 * networkgraph.
 *
 * =======================================================================
 */

namespace Quake2 {

    partial class QClient {

        private float scr_con_current; /* aproaches scr_conlines at scr_conspeed */
        private float scr_conlines; /* 0.0 to 1.0 lines of console to display */

        private bool scr_initialized; /* ready to draw */

        private int scr_draw_loading;

        private struct vrect_t {
            public int	x,y,width,height;
        }

        private vrect_t scr_vrect; /* position of render window on screen */

        private cvar_t? scr_viewsize;
        private cvar_t? scr_conspeed;
        private cvar_t? scr_centertime;
        private cvar_t? scr_showturtle;
        private cvar_t? scr_showpause;

        private cvar_t? scr_netgraph;
        private cvar_t? scr_timegraph;
        private cvar_t? scr_debuggraph;
        private cvar_t? scr_graphheight;
        private cvar_t? scr_graphscale;
        private cvar_t? scr_graphshift;
        private cvar_t? scr_drawall;

        private cvar_t? r_hudscale; /* named for consistency with R1Q2 */
        private cvar_t? r_consolescale;
        private cvar_t? r_menuscale;

        private struct dirty_t
        {
            public int x1, y1, x2, y2;
        }

        private dirty_t scr_dirty;
        private dirty_t[] scr_old_dirty = new dirty_t[2];

        private string crosshair_pic = "";
        private int crosshair_width, crosshair_height;

        private struct graphsamp_t {
            public float value;
            public int color;
        }

        private int current = 0;
        private graphsamp_t[] values = new graphsamp_t[2024];

        private void SCR_DebugGraph(float value, int color)
        {
            values[current & 2023].value = value;
            values[current & 2023].color = color;
            current++;
        }

        private void SCR_DrawDebugGraph()
        {
            /* draw the graph */
            int w = scr_vrect.width;

            int x = scr_vrect.x;
            int y = scr_vrect.y + scr_vrect.height;
            vid.Draw_Fill(x, y - scr_graphheight!.Int, w, scr_graphheight!.Int, 8);

            for (int a = 0; a < w; a++)
            {
                int i = (current - 1 - a + 1024) & 1023;
                float v = values[i].value;
                int color = values[i].color;
                v = v * scr_graphscale!.Float + scr_graphshift!.Float;

                if (v < 0)
                {
                    v += scr_graphheight!.Float * (1 + (int)(-v / scr_graphheight!.Float));
                }

                int h = (int)v % scr_graphheight.Int;
                vid.Draw_Fill(x + w - 1 - a, y - h, 1, h, color);
            }
        }
        /*
        * Sets scr_vrect, the coordinates of the rendered window
        */
        private void SCR_CalcVrect()
        {
            /* bound viewsize */
            if (scr_viewsize!.Int < 40)
            {
                common.Cvar_Set("viewsize", "40");
            }

            if (scr_viewsize!.Int > 100)
            {
                common.Cvar_Set("viewsize", "100");
            }

            int size = scr_viewsize.Int;

            scr_vrect.width = vid.viddef.width * size / 100;
            scr_vrect.height = vid.viddef.height * size / 100;

            scr_vrect.x = (vid.viddef.width - scr_vrect.width) / 2;
            scr_vrect.y = (vid.viddef.height - scr_vrect.height) / 2;
        }

        private void SCR_Init()
        {
            scr_viewsize = common.Cvar_Get("viewsize", "100", cvar_t.CVAR_ARCHIVE);
            scr_conspeed = common.Cvar_Get("scr_conspeed", "3", 0);
            scr_centertime = common.Cvar_Get("scr_centertime", "2.5", 0);
            scr_showturtle = common.Cvar_Get("scr_showturtle", "0", 0);
            scr_showpause = common.Cvar_Get("scr_showpause", "1", 0);
            scr_netgraph = common.Cvar_Get("netgraph", "0", 0);
            scr_timegraph = common.Cvar_Get("timegraph", "0", 0);
            scr_debuggraph = common.Cvar_Get("debuggraph", "0", 0);
            scr_graphheight = common.Cvar_Get("graphheight", "32", 0);
            scr_graphscale = common.Cvar_Get("graphscale", "1", 0);
            scr_graphshift = common.Cvar_Get("graphshift", "0", 0);
            scr_drawall = common.Cvar_Get("scr_drawall", "0", 0);
            r_hudscale = common.Cvar_Get("r_hudscale", "-1", cvar_t.CVAR_ARCHIVE);
            r_consolescale = common.Cvar_Get("r_consolescale", "-1", cvar_t.CVAR_ARCHIVE);
            r_menuscale = common.Cvar_Get("r_menuscale", "-1", cvar_t.CVAR_ARCHIVE);

            /* register our commands */
            // Cmd_AddCommand("timerefresh", SCR_TimeRefresh_f);
            // Cmd_AddCommand("loading", SCR_Loading_f);
            // Cmd_AddCommand("sizeup", SCR_SizeUp_f);
            // Cmd_AddCommand("sizedown", SCR_SizeDown_f);
            // Cmd_AddCommand("sky", SCR_Sky_f);

            scr_initialized = true;
        }


        private void SCR_DrawNet()
        {
            // float scale = SCR_GetMenuScale();
            var scale = 1.0f;

            if (cls.netchan.outgoing_sequence - cls.netchan.incoming_acknowledged < CMD_BACKUP - 1)
            {
                return;
            }

            vid.Draw_PicScaled((int)(scr_vrect.x + 64 * scale), scr_vrect.y, "net", scale);
        }

        private void SCR_DrawConsole()
        {
            Con_CheckResize();

            if ((cls.state == connstate_t.ca_disconnected) || (cls.state == connstate_t.ca_connecting))
            {
                /* forced full screen console */
                Con_DrawConsole(1.0f);
                return;
            }

            if ((cls.state != connstate_t.ca_active) || !cl.refresh_prepped)
            {
                /* connected, but can't render */
                Con_DrawConsole(0.5f);
                // Draw_Fill(0, viddef.height / 2, viddef.width, viddef.height / 2, 0);
                return;
            }

            if (scr_con_current > 0)
            {
                Con_DrawConsole(scr_con_current);
            }
            else
            {
                // if ((cls.key_dest == key_game) || (cls.key_dest == key_message))
                // {
                //     Con_DrawNotify(); /* only draw notify in game */
                // }
            }
        }

        private void SCR_EndLoadingPlaque()
        {
            cls.disable_screen = 0;
            // Con_ClearNotify();
        }


        private void SCR_AddDirtyPoint(int x, int y)
        {
            if (x < scr_dirty.x1)
            {
                scr_dirty.x1 = x;
            }

            if (x > scr_dirty.x2)
            {
                scr_dirty.x2 = x;
            }

            if (y < scr_dirty.y1)
            {
                scr_dirty.y1 = y;
            }

            if (y > scr_dirty.y2)
            {
                scr_dirty.y2 = y;
            }
        }

        private void SCR_DirtyScreen()
        {
            SCR_AddDirtyPoint(0, 0);
            SCR_AddDirtyPoint(vid.viddef.width - 1, vid.viddef.height - 1);
        }

        private const int STAT_MINUS = 10;
        private readonly string[][] sb_nums = new string[2][]{
            new string[] {
                "num_0", "num_1", "num_2", "num_3", "num_4", "num_5",
                "num_6", "num_7", "num_8", "num_9", "num_minus"
            },
            new string[] {
                "anum_0", "anum_1", "anum_2", "anum_3", "anum_4", "anum_5",
                "anum_6", "anum_7", "anum_8", "anum_9", "anum_minus"
            }
        };

        private const int ICON_WIDTH = 24;
        private const int ICON_HEIGHT = 24;
        private const int CHAR_WIDTH = 16;
        private const int ICON_SPACE = 8;


        private void SCR_DrawFieldScaled(int x, int y, int color, int width, int value, float factor)
        {
            if (width < 1)
            {
                return;
            }

            /* draw number string */
            if (width > 5)
            {
                width = 5;
            }

            SCR_AddDirtyPoint(x, y);
            SCR_AddDirtyPoint(x + (int)((width * CHAR_WIDTH + 2)*factor), y + (int)(factor*24));

            var num = value.ToString();
            var l = num.Length;

            if (l > width)
            {
                l = width;
            }

            x += (int)((2 + CHAR_WIDTH * (width - l)) * factor);

            for (int i = 0; i < l; i++)
            {
                int frame;
                if (num[i] == '-')
                {
                    frame = STAT_MINUS;
                }

                else
                {
                    frame = num[i] - '0';
                }

                vid.Draw_PicScaled(x, y, sb_nums[color][frame], factor);
                x += (int)(CHAR_WIDTH*factor);
            }
        }

        private void SCR_DrawField(int x, int y, int color, int width, int value)
        {
            SCR_DrawFieldScaled(x, y, color, width, value, 1.0f);
        }

        private void SCR_ExecuteLayoutString(string s)
        {
            // int x, y;
            // int value;
            // char *token;
            // int width;
            // int index;
            // clientinfo_t *ci;

            // float scale = SCR_GetHUDScale();
            float scale = 1;

            if ((cls.state != connstate_t.ca_active) || !cl.refresh_prepped)
            {
                return;
            }

            if (String.IsNullOrEmpty(s))
            {
                return;
            }

            int x = 0;
            int y = 0;
            int index = 0;

            while (index >= 0 && index < s.Length)
            {
                var token = QShared.COM_Parse(s, ref index);
                if (index < 0) break;

            //     if (!strcmp(token, "xl"))
            //     {
            //         token = COM_Parse(&s);
            //         x = scale*(int)strtol(token, (char **)NULL, 10);
            //         continue;
            //     }

            //     if (!strcmp(token, "xr"))
            //     {
            //         token = COM_Parse(&s);
            //         x = viddef.width + scale*(int)strtol(token, (char **)NULL, 10);
            //         continue;
            //     }

                if (token.Equals("xv"))
                {
                    token = QShared.COM_Parse(s, ref index);
                    x = vid.viddef.width / 2 - (int)(scale*160) + (int)(scale*Int32.Parse(token));
                    continue;
                }

            //     if (!strcmp(token, "yt"))
            //     {
            //         token = COM_Parse(&s);
            //         y = scale*(int)strtol(token, (char **)NULL, 10);
            //         continue;
            //     }

                if (token.Equals("yb"))
                {
                    token = QShared.COM_Parse(s, ref index);
                    y = vid.viddef.height + (int)(scale*Int32.Parse(token));
                    continue;
                }

            //     if (!strcmp(token, "yv"))
            //     {
            //         token = COM_Parse(&s);
            //         y = viddef.height / 2 - scale*120 + scale*(int)strtol(token, (char **)NULL, 10);
            //         continue;
            //     }

                if (token.Equals("pic"))
                {
                    /* draw a pic from a stat number */
                    token = QShared.COM_Parse(s, ref index);
                    var idx = Int32.Parse(token);

                    if ((idx < 0) || (idx >= cl.frame.playerstate.stats.Length))
                    {
                        common.Com_Error(QShared.ERR_DROP, $"bad stats index {idx} (0x{idx.ToString("X")})");
                    }

                    int value = cl.frame.playerstate.stats[idx];

                    if (value >= QShared.MAX_IMAGES)
                    {
                        common.Com_Error(QShared.ERR_DROP, "Pic >= MAX_IMAGES");
                    }

                    if (!String.IsNullOrEmpty(cl.configstrings[QShared.CS_IMAGES + value]))
                    {
                        SCR_AddDirtyPoint(x, y);
                        SCR_AddDirtyPoint(x + (int)(23*scale), y + (int)(23*scale));
                        vid.Draw_PicScaled(x, y, cl.configstrings[QShared.CS_IMAGES + value], scale);
                    }

                    continue;
                }

            //     if (!strcmp(token, "client"))
            //     {
            //         /* draw a deathmatch client block */
            //         int score, ping, time;

            //         token = COM_Parse(&s);
            //         x = viddef.width / 2 - scale*160 + scale*(int)strtol(token, (char **)NULL, 10);
            //         token = COM_Parse(&s);
            //         y = viddef.height / 2 - scale*120 + scale*(int)strtol(token, (char **)NULL, 10);
            //         SCR_AddDirtyPoint(x, y);
            //         SCR_AddDirtyPoint(x + scale*159, y + scale*31);

            //         token = COM_Parse(&s);
            //         value = (int)strtol(token, (char **)NULL, 10);

            //         if ((value >= MAX_CLIENTS) || (value < 0))
            //         {
            //             Com_Error(ERR_DROP, "client >= MAX_CLIENTS");
            //         }

            //         ci = &cl.clientinfo[value];

            //         token = COM_Parse(&s);
            //         score = (int)strtol(token, (char **)NULL, 10);

            //         token = COM_Parse(&s);
            //         ping = (int)strtol(token, (char **)NULL, 10);

            //         token = COM_Parse(&s);
            //         time = (int)strtol(token, (char **)NULL, 10);

            //         DrawAltStringScaled(x + scale*32, y, ci->name, scale);
            //         DrawAltStringScaled(x + scale*32, y + scale*8, "Score: ", scale);
            //         DrawAltStringScaled(x + scale*(32 + 7 * 8), y + scale*8, va("%i", score), scale);
            //         DrawStringScaled(x + scale*32, y + scale*16, va("Ping:  %i", ping), scale);
            //         DrawStringScaled(x + scale*32, y + scale*24, va("Time:  %i", time), scale);

            //         if (!ci->icon)
            //         {
            //             ci = &cl.baseclientinfo;
            //         }

            //         Draw_PicScaled(x, y, ci->iconname, scale);
            //         continue;
            //     }

            //     if (!strcmp(token, "ctf"))
            //     {
            //         /* draw a ctf client block */
            //         int score, ping;
            //         char block[80];

            //         token = COM_Parse(&s);
            //         x = viddef.width / 2 - scale*160 + scale*(int)strtol(token, (char **)NULL, 10);
            //         token = COM_Parse(&s);
            //         y = viddef.height / 2 - scale*120 + scale*(int)strtol(token, (char **)NULL, 10);
            //         SCR_AddDirtyPoint(x, y);
            //         SCR_AddDirtyPoint(x + scale*159, y + scale*31);

            //         token = COM_Parse(&s);
            //         value = (int)strtol(token, (char **)NULL, 10);

            //         if ((value >= MAX_CLIENTS) || (value < 0))
            //         {
            //             Com_Error(ERR_DROP, "client >= MAX_CLIENTS");
            //         }

            //         ci = &cl.clientinfo[value];

            //         token = COM_Parse(&s);
            //         score = (int)strtol(token, (char **)NULL, 10);

            //         token = COM_Parse(&s);
            //         ping = (int)strtol(token, (char **)NULL, 10);

            //         if (ping > 999)
            //         {
            //             ping = 999;
            //         }

            //         sprintf(block, "%3d %3d %-12.12s", score, ping, ci->name);

            //         if (value == cl.playernum)
            //         {
            //             DrawAltStringScaled(x, y, block, scale);
            //         }

            //         else
            //         {
            //             DrawStringScaled(x, y, block, scale);
            //         }

            //         continue;
            //     }

                if (token.Equals("picn"))
                {
                    /* draw a pic from a name */
                    token = QShared.COM_Parse(s, ref index);
                    SCR_AddDirtyPoint(x, y);
                    SCR_AddDirtyPoint(x + (int)(scale*23), y + (int)(scale*23));
                    vid.Draw_PicScaled(x, y, token, scale);
                    continue;
                }

            //     if (!strcmp(token, "num"))
            //     {
            //         /* draw a number */
            //         token = COM_Parse(&s);
            //         width = (int)strtol(token, (char **)NULL, 10);
            //         token = COM_Parse(&s);
            //         value = cl.frame.playerstate.stats[(int)strtol(token, (char **)NULL, 10)];
            //         SCR_DrawFieldScaled(x, y, 0, width, value, scale);
            //         continue;
            //     }

                if (token.Equals("hnum"))
                {
                    /* health number */
                    int color;

                    var value = cl.frame.playerstate.stats[QShared.STAT_HEALTH];

                    if (value > 25)
                    {
                        color = 0;  /* green */
                    }
                    else if (value > 0)
                    {
                        color = (cl.frame.serverframe >> 2) & 1; /* flash */
                    }
                    else
                    {
                        color = 1;
                    }

                    if ((cl.frame.playerstate.stats[QShared.STAT_FLASHES] & 1) != 0)
                    {
                        vid.Draw_PicScaled(x, y, "field_3", scale);
                    }

                    SCR_DrawFieldScaled(x, y, color, 3, value, scale);
                    continue;
                }

                if (token.Equals("anum"))
                {
                    /* ammo number */
                    int color;

                    int value = cl.frame.playerstate.stats[QShared.STAT_AMMO];

                    if (value > 5)
                    {
                        color = 0; /* green */
                    }
                    else if (value >= 0)
                    {
                        color = (cl.frame.serverframe >> 2) & 1; /* flash */
                    }
                    else
                    {
                        continue; /* negative number = don't show */
                    }

                    if ((cl.frame.playerstate.stats[QShared.STAT_FLASHES] & 4) != 0)
                    {
                        vid.Draw_PicScaled(x, y, "field_3", scale);
                    }

                    SCR_DrawFieldScaled(x, y, color, 3, value, scale);
                    continue;
                }

            //     if (!strcmp(token, "rnum"))
            //     {
            //         /* armor number */
            //         int color;

            //         width = 3;
            //         value = cl.frame.playerstate.stats[STAT_ARMOR];

            //         if (value < 1)
            //         {
            //             continue;
            //         }

            //         color = 0; /* green */

            //         if (cl.frame.playerstate.stats[STAT_FLASHES] & 2)
            //         {
            //             Draw_PicScaled(x, y, "field_3", scale);
            //         }

            //         SCR_DrawFieldScaled(x, y, color, width, value, scale);
            //         continue;
            //     }

                if (token.Equals("stat_string"))
                {
                    token = QShared.COM_Parse(s, ref index);
                    var indx = Int32.Parse(token);

                    if ((indx < 0) || (indx >= QShared.MAX_CONFIGSTRINGS))
                    {
                        common.Com_Error(QShared.ERR_DROP, "Bad stat_string index");
                    }

                    indx = cl.frame.playerstate.stats[indx];

                    if ((indx < 0) || (indx >= QShared.MAX_CONFIGSTRINGS))
                    {
                        common.Com_Error(QShared.ERR_DROP, "Bad stat_string index");
                    }

                    DrawStringScaled(x, y, cl.configstrings[indx], scale);
                    continue;
                }

            //     if (!strcmp(token, "cstring"))
            //     {
            //         token = COM_Parse(&s);
            //         DrawHUDStringScaled(token, x, y, 320, 0, scale); // FIXME: or scale 320 here?
            //         continue;
            //     }

            //     if (!strcmp(token, "string"))
            //     {
            //         token = COM_Parse(&s);
            //         DrawStringScaled(x, y, token, scale);
            //         continue;
            //     }

            //     if (!strcmp(token, "cstring2"))
            //     {
            //         token = COM_Parse(&s);
            //         DrawHUDStringScaled(token, x, y, 320, 0x80, scale); // FIXME: or scale 320 here?
            //         continue;
            //     }

            //     if (!strcmp(token, "string2"))
            //     {
            //         token = COM_Parse(&s);
            //         DrawAltStringScaled(x, y, token, scale);
            //         continue;
            //     }

                if (token.Equals("if"))
                {
                    /* draw a number */
                    token = QShared.COM_Parse(s, ref index);
                    var value = cl.frame.playerstate.stats[Int32.Parse(token)];

                    if (value == 0)
                    {
                        /* skip to endif */
                        while (index > 0 && !token.Equals("endif"))
                        {
                            token = QShared.COM_Parse(s, ref index);
                        }
                    }

                    continue;
                }
                if (!token.Equals("endif"))
                {
                    Console.WriteLine(token);
                }
            }
        }

        /*
        * The status bar is a small layout program that
        * is based on the stats array
        */
        private void SCR_DrawStats()
        {
            if (cl.configstrings != null)
                SCR_ExecuteLayoutString(cl.configstrings[QShared.CS_STATUSBAR]);
        }

        private const int STAT_LAYOUTS = 13;

        private void SCR_DrawLayout()
        {
            if (cl.frame.playerstate.stats[STAT_LAYOUTS] == 0)
            {
                return;
            }

            SCR_ExecuteLayoutString(cl.layout);
        }

        // ----
        /*
        * This is called every frame, and can also be called
        * explicitly to flush text to the screen.
        */
        private void SCR_UpdateScreen()
        {
            // int numframes;
            // int i;
            // float separation[2] = {0, 0};
            // float scale = SCR_GetMenuScale();

            /* if the screen is disabled (loading plaque is
            up, or vid mode changing) do nothing at all */
            if (cls.disable_screen != 0)
            {
                if (common.Sys_Milliseconds() - cls.disable_screen > 120000)
                {
                    cls.disable_screen = 0;
                    common.Com_Printf("Loading plaque timed out.\n");
                }

                return;
            }

            if (!scr_initialized || !con.initialized)
            {
                return; /* not initialized yet */
            }

            var numframes = 1;
            float[] separation = new float[2];
            // if ( gl1_stereo->value )
            // {
            //     numframes = 2;
            //     separation[0] = -gl1_stereo_separation->value / 2;
            //     separation[1] = +gl1_stereo_separation->value / 2;
            // }

            for (int i = 0; i < numframes; i++)
            {
                vid.R_BeginFrame(separation[i]);

            //     if (scr_draw_loading == 2)
            //     {
            //         /* loading plaque over black screen */
            //         int w, h;

            //         R_EndWorldRenderpass();
            //         if(i == 0){
            //             R_SetPalette(NULL);
            //         }

            //         if(i == numframes - 1){
            //             scr_draw_loading = false;
            //         }

            //         Draw_GetPicSize(&w, &h, "loading");
            //         Draw_PicScaled((viddef.width - w * scale) / 2, (viddef.height - h * scale) / 2, "loading", scale);
            //     }

            //     /* if a cinematic is supposed to be running,
            //     handle menus and console specially */
            //     else if (cl.cinematictime > 0)
            //     {
            //         if (cls.key_dest == key_menu)
            //         {
            //             if (cl.cinematicpalette_active)
            //             {
            //                 R_SetPalette(NULL);
            //                 cl.cinematicpalette_active = false;
            //             }

            //             R_EndWorldRenderpass();
            //             M_Draw();
            //         }
            //         else if (cls.key_dest == key_console)
            //         {
            //             if (cl.cinematicpalette_active)
            //             {
            //                 R_SetPalette(NULL);
            //                 cl.cinematicpalette_active = false;
            //             }

            //             R_EndWorldRenderpass();
            //             SCR_DrawConsole();
            //         }
            //         else
            //         {
            //             R_EndWorldRenderpass();
            //             SCR_DrawCinematic();
            //         }
            //     }
            //     else
            //     {
            //         /* make sure the game palette is active */
            //         if (cl.cinematicpalette_active)
            //         {
            //             R_SetPalette(NULL);
            //             cl.cinematicpalette_active = false;
            //         }

                    /* do 3D refresh drawing, and then update the screen */
                    SCR_CalcVrect();

                    /* clear any dirty part of the background */
            //         SCR_TileClear();

                    V_RenderView(separation[i]);

                    SCR_DrawStats();

                    if (cl.frame !=  null && cl.frame.playerstate != null && cl.frame.playerstate.stats != null) {
                        if ((cl.frame.playerstate.stats[QShared.STAT_LAYOUTS] & 1) != 0)
                        {
                            SCR_DrawLayout();
                        }

                        if ((cl.frame.playerstate.stats[QShared.STAT_LAYOUTS] & 2) != 0)
                        {
                //             CL_DrawInventory();
                        }
                    }

                    SCR_DrawNet();
            //         SCR_CheckDrawCenterString();

                    if (scr_timegraph!.Bool)
                    {
                        SCR_DebugGraph(cls.rframetime * 300, 0);
                    }

                    if (scr_debuggraph!.Bool || scr_timegraph!.Bool || scr_netgraph!.Bool)
                    {
                        SCR_DrawDebugGraph();
                    }

            //         SCR_DrawPause();

                    SCR_DrawConsole();

                    M_Draw();

            //         SCR_DrawLoading();
            //     }
            }

            // SCR_Framecounter();
            vid.R_EndFrame();
        }


    }
}