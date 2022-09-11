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
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307,
 * USA.
 *
 * =======================================================================
 *
 * This file implements the console
 *
 * =======================================================================
 */

namespace Quake2 {

    partial class QClient {

        private const int NUM_CON_TIMES = 4;
        private const int CON_TEXTSIZE	= 32768;

        private struct console_t {
            public bool	initialized;

            // char	text[CON_TEXTSIZE];
            public char[] text;
            public int		current; /* line where next message will be printed */
            public int		x; /* offset in current line for next print */
            public int		display; /* bottom of console displays this line */

            public int		ormask; /* high bit mask for colored characters */

            public int 	linewidth; /* characters across screen */
            public int		totallines; /* total lines in console scrollback */

            public float	cursorspeed;

            public int		vislines;

            public float[]	times; // [NUM_CON_TIMES]; /* cls.realtime time the line was generated */
        }

        private console_t con;
        private cvar_t? con_notifytime;

        private void DrawStringScaled(int x, int y, string s, float factor)
        {
            int index = 0;
            while (index < s.Length)
            {
                vid.Draw_CharScaled(x, y, s[index], factor);
                x += (int)(8*factor);
                index++;
            }
        }

        /*
        * If the line width has changed, reformat the buffer.
        */
        private void Con_CheckResize()
        {
            // int i, j, width, oldwidth, oldtotallines, numlines, numchars;
            // char tbuf[CON_TEXTSIZE];
            // float scale = SCR_GetConsoleScale();
            float scale = 1.0f;

            /* We need to clamp the line width to MAXCMDLINE - 2,
            otherwise we may overflow the text buffer if the
            vertical resultion / 8 (one char == 8 pixels) is
            bigger then MAXCMDLINE.
            MAXCMDLINE - 2 because 1 for the prompt and 1 for
            the terminating \0. */
            int width = ((int)(vid.viddef.width / scale) / 8) - 2;
            // width = width > MAXCMDLINE - 2 ? MAXCMDLINE - 2 : width;

            if (width == con.linewidth)
            {
                return;
            }

            /* video hasn't been initialized yet */
            if (width < 1)
            {
                width = 38;
                con.linewidth = width;
                con.totallines = CON_TEXTSIZE / con.linewidth;
                Array.Fill(con.text, ' ');
            }
            else
            {
                int oldwidth = con.linewidth;
                con.linewidth = width;
                int oldtotallines = con.totallines;
                con.totallines = CON_TEXTSIZE / con.linewidth;
                int numlines = oldtotallines;

                if (con.totallines < numlines)
                {
                    numlines = con.totallines;
                }

                int numchars = oldwidth;

                if (con.linewidth < numchars)
                {
                    numchars = con.linewidth;
                }

                char[] tbuf = new char[CON_TEXTSIZE];
                con.text.CopyTo(tbuf, 0);
                Array.Fill(con.text, ' ');

                for (int i = 0; i < numlines; i++)
                {
                    for (int j = 0; j < numchars; j++)
                    {
                        con.text[(con.totallines - 1 - i) * con.linewidth + j] =
                            tbuf[((con.current - i + oldtotallines) %
                                oldtotallines) * oldwidth + j];
                    }
                }

                // Con_ClearNotify();
            }

            con.current = con.totallines - 1;
            con.display = con.current;
        }

        private void Con_Init()
        {
            con.linewidth = -1;

            con.text = new char[CON_TEXTSIZE];
            con.times = new float[NUM_CON_TIMES];

            Con_CheckResize();

            common.Com_Printf("Console initialized.\n");

            /* register our commands */
            con_notifytime = common.Cvar_Get("con_notifytime", "3", 0);

            // Cmd_AddCommand("toggleconsole", Con_ToggleConsole_f);
            // Cmd_AddCommand("togglechat", Con_ToggleChat_f);
            // Cmd_AddCommand("messagemode", Con_MessageMode_f);
            // Cmd_AddCommand("messagemode2", Con_MessageMode2_f);
            // Cmd_AddCommand("clear", Con_Clear_f);
            // Cmd_AddCommand("condump", Con_Dump_f);
            con.initialized = true;
        }

        /*
        * Draws the console with the solid background
        */
        private void Con_DrawConsole(float frac)
        {
            // int i, j, x, y, n;
            // int rows;
            // int verLen;
            // char *text;
            // int row;
            // int lines;
            // float scale;
            // char version[48];
            // char dlbar[1024];
            // char timebuf[48];
            // char tmpbuf[48];

            // time_t t;
            // struct tm *today;

            // scale = SCR_GetConsoleScale();
            int lines = (int)(vid.viddef.height * frac);

            if (lines <= 0)
            {
                return;
            }

            if (lines > vid.viddef.height)
            {
                lines = vid.viddef.height;
            }

            /* draw the background */
            vid.Draw_StretchPic(0, -vid.viddef.height + lines, vid.viddef.width,
                    vid.viddef.height, "conback");
            SCR_AddDirtyPoint(0, 0);
            SCR_AddDirtyPoint(vid.viddef.width - 1, lines - 1);

        //     Com_sprintf(version, sizeof(version), "Yamagi Quake II v%s", YQ2VERSION);

        //     verLen = strlen(version);

        //     for (x = 0; x < verLen; x++)
        //     {
        //         Draw_CharScaled(viddef.width - ((verLen*8+5) * scale) + x * 8 * scale, lines - 35 * scale, 128 + version[x], scale);
        //     }

        //     t = time(NULL);
        //     today = localtime(&t);
        //     strftime(timebuf, sizeof(timebuf), "%H:%M:%S - %m/%d/%Y", today);

        //     Com_sprintf(tmpbuf, sizeof(tmpbuf), "%s", timebuf);

        //     for (x = 0; x < 21; x++)
        //     {
        //         Draw_CharScaled(viddef.width - (173 * scale) + x * 8 * scale, lines - 25 * scale, 128 + tmpbuf[x], scale);
        //     }

        //     /* draw the text */
        //     con.vislines = lines;

        //     rows = (lines - 22) >> 3; /* rows of text to draw */
        //     y = (lines - 30 * scale) / scale;

        //     /* draw from the bottom up */
        //     if (con.display != con.current)
        //     {
        //         /* draw arrows to show the buffer is backscrolled */
        //         for (x = 0; x < con.linewidth; x += 4)
        //         {
        //             Draw_CharScaled(((x + 1) << 3) * scale, y * scale, '^', scale);
        //         }

        //         y -= 8;
        //         rows--;
        //     }

        //     row = con.display;

        //     for (i = 0; i < rows; i++, y -= 8, row--)
        //     {
        //         if (row < 0)
        //         {
        //             break;
        //         }

        //         if (con.current - row >= con.totallines)
        //         {
        //             break; /* past scrollback wrap point */
        //         }

        //         text = con.text + (row % con.totallines) * con.linewidth;

        //         for (x = 0; x < con.linewidth; x++)
        //         {
        //             Draw_CharScaled(((x + 1) << 3) * scale, y * scale, text[x], scale);
        //         }
        //     }

        //     /* draw the download bar, figure out width */
        // #ifdef USE_CURL
        //     if (cls.downloadname[0] && (cls.download || cls.downloadposition))
        // #else
        //     if (cls.download)
        // #endif
        //     {
        //         if ((text = strrchr(cls.downloadname, '/')) != NULL)
        //         {
        //             text++;
        //         }

        //         else
        //         {
        //             text = cls.downloadname;
        //         }

        //         x = con.linewidth - ((con.linewidth * 7) / 40);
        //         y = x - strlen(text) - 8;
        //         i = con.linewidth / 3;

        //         if (strlen(text) > i)
        //         {
        //             y = x - i - 11;
        //             memcpy(dlbar, text, i);
        //             dlbar[i] = 0;
        //             strcat(dlbar, "...");
        //         }
        //         else
        //         {
        //             strcpy(dlbar, text);
        //         }

        //         strcat(dlbar, ": ");
        //         i = strlen(dlbar);
        //         dlbar[i++] = '\x80';

        //         /* where's the dot gone? */
        //         if (cls.downloadpercent == 0)
        //         {
        //             n = 0;
        //         }

        //         else
        //         {
        //             n = y * cls.downloadpercent / 100;
        //         }

        //         for (j = 0; j < y; j++)
        //         {
        //             if (j == n)
        //             {
        //                 dlbar[i++] = '\x83';
        //             }

        //             else
        //             {
        //                 dlbar[i++] = '\x81';
        //             }
        //         }

        //         dlbar[i++] = '\x82';
        //         dlbar[i] = 0;

        //         sprintf(dlbar + strlen(dlbar), " %02d%%", cls.downloadpercent);

        //         /* draw it */
        //         y = con.vislines - 12;

        //         for (i = 0; i < strlen(dlbar); i++)
        //         {
        //             Draw_CharScaled(((i + 1) << 3) * scale, y * scale, dlbar[i], scale);
        //         }
        //     }

        //     /* draw the input prompt, user text, and cursor if desired */
        //     Con_DrawInput();
        }

    }
}