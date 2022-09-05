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
 * Client / Server interactions
 *
 * =======================================================================
 */

namespace Quake2 {

    partial class QCommon {

        [Serializable]
        public class AbortFrame : Exception
        {
            public AbortFrame() : base() {}
        }

        /*
        * Both client and server can use this, and it will output
        * to the apropriate place.
        */
        public void Com_Printf(string msg)
        {
            Console.Write(msg);
        }

        public void Com_DPrintf(string msg)
        {
            Console.Write(msg);
        }


        /*
        * Both client and server can use this, and it will
        * do the apropriate things.
        */
        public void Com_Error(int code, string msg)
        {
        //     va_list argptr;
        //     static char msg[MAXPRINTMSG];
        //     static qboolean recursive;

        //     if (recursive)
        //     {
        //         Sys_Error("recursive error after: %s", msg);
        //     }

        //     recursive = true;

        //     va_start(argptr, fmt);
        //     vsnprintf(msg, MAXPRINTMSG, fmt, argptr);
        //     va_end(argptr);

            if (code == QShared.ERR_DISCONNECT)
            {
                client.CL_Drop();
                throw new AbortFrame();
            }
            else if (code == QShared.ERR_DROP)
            {
                Com_Printf($"********************\nERROR: {msg}\n********************\n");
                server.SV_Shutdown($"Server crashed: {msg}\n", false);
                client.CL_Drop();
                throw new AbortFrame();
            }
            else
            {
                server.SV_Shutdown($"Server fatal crashed: {msg}\n", false);
        //         CL_Shutdown();
            }

        //     if (logfile)
        //     {
        //         fclose(logfile);
        //         logfile = NULL;
        //     }

        //     Sys_Error("%s", msg);
        //     recursive = false;
            throw new Exception(msg);
        }

        private int server_state = 0;

        public int ServerState {
            get { return server_state; }
            set { server_state = value; }
        }

        private long startTicks;

        public int Sys_Milliseconds() {
            return (int)((DateTime.Now.Ticks - startTicks) / TimeSpan.TicksPerMillisecond);
        }
    }
}