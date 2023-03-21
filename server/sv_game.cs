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
using System.Numerics;

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

            public void unlinkentity(edict_s ent)
            {
                server.SV_UnlinkEdict(ent);
            }

            public void Pmove(ref QShared.pmove_t pmove)
            {
                server.common.Pmove(ref pmove);
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

                if (server.sv.state != server_state_t.ss_loading)
                {
                    /* send the update to everyone */
                    server.sv.multicast.Clear();
                    server.sv.multicast.WriteChar((int)QCommon.svc_ops_e.svc_configstring);
                    server.sv.multicast.WriteShort(index);
                    server.sv.multicast.WriteString(str);

                    server.SV_Multicast(Vector3.Zero, QShared.multicast_t.MULTICAST_ALL_R);
                }                
            }

            public int modelindex(string name)
            {
                return server.SV_FindIndex(name, QShared.CS_MODELS, QShared.MAX_MODELS, true);
            }

            public void setmodel(edict_s ent, string name)
            {
                if (String.IsNullOrEmpty(name))
                {
                    server.common.Com_Error(QShared.ERR_DROP, "PF_setmodel: NULL");
                }

                var i = server.SV_FindIndex(name, QShared.CS_MODELS, QShared.MAX_MODELS, true);

                ent.s.modelindex = i;

                /* if it is an inline model, get
                the size information for it */
                if (name[0] == '*')
                {
                    var mod = server.common.CM_InlineModel(name);
                    if (ent.classname == "func_door") {
                        Console.WriteLine("=== setmodel DOOR ===");
                        Console.WriteLine($" mins: {mod.mins}");
                        Console.WriteLine($" maxs: {mod.maxs}");
                        Console.WriteLine($" model: {name}");
                    }
                    ent.mins = mod.mins;
                    ent.maxs = mod.maxs;
                    server.SV_LinkEdict(ent);
                }
            }

            public QShared.trace_t trace(in Vector3 start, in Vector3? mins, in Vector3? maxs, in Vector3 end,
                            edict_s passent, int contentmask)
            {
                return server.SV_Trace(start, mins, maxs, end, passent, contentmask);
            }

            public int BoxEdicts(in Vector3 mins, in Vector3 maxs, edict_s[] list, int areatype)
            {
                return server.SV_AreaEdicts(mins, maxs, list, areatype);
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
            // import.AreasConnected = CM_AreasConnected;

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