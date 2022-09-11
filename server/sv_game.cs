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
 * Interface between the server and the game module.
 *
 * =======================================================================
 */

namespace Quake2 {

    partial class QServer {

        private game_export_t? ge;

        private class GameExports : game_import_t
        {
            private QServer server;

            public GameExports(QServer server)
            {
                this.server = server;
            }

            public void dprintf(string msg)
            {
                server.common.Com_Printf(msg);
            }

            public void error(string msg)
            {
                server.common.Com_Error(QShared.ERR_DROP, "Game Error: " + msg);
            }

            public cvar_t? cvar(string var_name, string? value, int flags)
            {
                return server.common.Cvar_Get(var_name, value, flags);
            }

            public void linkentity(edict_s ent)
            {
                server.SV_LinkEdict(ent);
            }

            public void configstring(int index, string str)
            {
                if ((index < 0) || (index >= QShared.MAX_CONFIGSTRINGS))
                {
                    server.common.Com_Error(QShared.ERR_DROP, $"configstring: bad index {index}\n");
                }

                if (str == null)
                {
                    str = "";
                }

                /* change the string in sv */
                server.sv.configstrings[index] = str;

                // if (sv.state != ss_loading)
                // {
                //     /* send the update to everyone */
                //     SZ_Clear(&sv.multicast);
                //     MSG_WriteChar(&sv.multicast, svc_configstring);
                //     MSG_WriteShort(&sv.multicast, index);
                //     MSG_WriteString(&sv.multicast, val);

                //     SV_Multicast(vec3_origin, MULTICAST_ALL_R);
                // }                
            }
        }

        /*
        * Called when either the entire server is being killed, or
        * it is changing to a different game directory.
        */
        private void SV_ShutdownGameProgs()
        {
            // ge->Shutdown();
            // Sys_UnloadGame();
            ge = null;
        }


        /*
        * Init the game subsystem for a new map
        */
        private void SV_InitGameProgs()
        {
        //     game_import_t import;

            /* unload anything we have now */
            if (ge != null)
            {
                SV_ShutdownGameProgs();
            }

            common.Com_Printf("-------- game initialization -------\n");

        //     /* load a new game dll */
        //     import.multicast = SV_Multicast;
        //     import.unicast = PF_Unicast;
        //     import.bprintf = SV_BroadcastPrintf;
        //     import.dprintf = PF_dprintf;
        //     import.cprintf = PF_cprintf;
        //     import.centerprintf = PF_centerprintf;
        //     import.error = PF_error;

        //     import.linkentity = SV_LinkEdict;
        //     import.unlinkentity = SV_UnlinkEdict;
        //     import.BoxEdicts = SV_AreaEdicts;
        //     import.trace = SV_Trace;
        //     import.pointcontents = SV_PointContents;
        //     import.setmodel = PF_setmodel;
        //     import.inPVS = PF_inPVS;
        //     import.inPHS = PF_inPHS;
        //     import.Pmove = Pmove;

        //     import.modelindex = SV_ModelIndex;
        //     import.soundindex = SV_SoundIndex;
        //     import.imageindex = SV_ImageIndex;

        //     import.configstring = PF_Configstring;
        //     import.sound = PF_StartSound;
        //     import.positioned_sound = SV_StartSound;

        //     import.WriteChar = PF_WriteChar;
        //     import.WriteByte = PF_WriteByte;
        //     import.WriteShort = PF_WriteShort;
        //     import.WriteLong = PF_WriteLong;
        //     import.WriteFloat = PF_WriteFloat;
        //     import.WriteString = PF_WriteString;
        //     import.WritePosition = PF_WritePos;
        //     import.WriteDir = PF_WriteDir;
        //     import.WriteAngle = PF_WriteAngle;

        //     import.TagMalloc = Z_TagMalloc;
        //     import.TagFree = Z_Free;
        //     import.FreeTags = Z_FreeTags;

        //     import.cvar = Cvar_Get;
        //     import.cvar_set = Cvar_Set;
        //     import.cvar_forceset = Cvar_ForceSet;

        //     import.argc = Cmd_Argc;
        //     import.argv = Cmd_Argv;
        //     import.args = Cmd_Args;
        //     import.AddCommandString = Cbuf_AddText;

        // #ifndef DEDICATED_ONLY
        //     import.DebugGraph = SCR_DebugGraph;
        // #endif

        //     import.SetAreaPortalState = CM_SetAreaPortalState;
        //     import.AreasConnected = CM_AreasConnected;

        //     ge = (game_export_t *)Sys_GetGameAPI(&import);
            ge = new QuakeGame(new GameExports(this));

            if (ge == null)
            {
                common.Com_Error(QShared.ERR_DROP, "failed to load game");
                return;
            }

        //     if (ge->apiversion != GAME_API_VERSION)
        //     {
        //         Com_Error(ERR_DROP, "game is version %i, not %i", ge->apiversion,
        //                 GAME_API_VERSION);
        //     }

            ge.Init();

            common.Com_Printf("------------------------------------\n\n");
        }


    }
}