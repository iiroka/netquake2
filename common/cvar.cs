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
 * The Quake II CVAR subsystem. Implements dynamic variable handling.
 *
 * =======================================================================
 */

namespace Quake2 {

    partial class QCommon {

        private IDictionary<string, cvar_t> cvar_vars = new Dictionary<string, cvar_t>();

        /* An ugly hack to rewrite CVARs loaded from config.cfg */
        private readonly IDictionary<string, string> replacements = new Dictionary<string, string>(){
            {"cd_shuffle", "ogg_shuffle"},
            {"cl_anglekicks", "cl_kickangles"},
            {"cl_drawfps", "cl_showfps"},
            {"gl_drawentities", "r_drawentities"},
            {"gl_drawworld", "r_drawworld"},
            {"gl_fullbright", "r_fullbright"},
            {"gl_lerpmodels", "r_lerpmodels"},
            {"gl_lightlevel", "r_lightlevel"},
            {"gl_norefresh", "r_norefresh"},
            {"gl_novis", "r_novis"},
            {"gl_speeds", "r_speeds"},
            {"gl_clear", "r_clear"},
            {"gl_consolescale", "r_consolescale"},
            {"gl_hudscale", "r_hudscale"},
            {"gl_menuscale", "r_scale"},
            {"gl_customheight", "r_customheight"},
            {"gl_customwidth", "r_customheight"},
            {"gl_dynamic", "gl1_dynamic"},
            {"gl_farsee", "r_farsee"},
            {"gl_flashblend", "gl1_flashblend"},
            {"gl_lockpvs", "r_lockpvs"},
            {"gl_maxfps", "vid_maxfps"},
            {"gl_mode", "r_mode"},
            {"gl_modulate", "r_modulate"},
            {"gl_overbrightbits", "gl1_overbrightbits"},
            {"gl_palettedtextures", "gl1_palettedtextures"},
            {"gl_particle_min_size", "gl1_particle_min_size"},
            {"gl_particle_max_size", "gl1_particle_max_size"},
            {"gl_particle_size", "gl1_particle_size"},
            {"gl_particle_att_a", "gl1_particle_att_a"},
            {"gl_particle_att_b", "gl1_particle_att_b"},
            {"gl_particle_att_c", "gl1_particle_att_c"},
            {"gl_picmip", "gl1_picmip"},
            {"gl_pointparameters", "gl1_pointparameters"},
            {"gl_polyblend", "gl1_polyblend"},
            {"gl_round_down", "gl1_round_down"},
            {"gl_saturatelightning", "gl1_saturatelightning"},
            {"gl_stencilshadows", "gl1_stencilshadows"},
            {"gl_stereo", "gl1_stereo"},
            {"gl_stereo_separation", "gl1_stereo_separation"},
            {"gl_stereo_anaglyph_colors", "gl1_stereo_anaglyph_colors"},
            {"gl_stereo_convergence", "gl1_stereo_convergence"},
            {"gl_swapinterval", "r_vsync"},
            {"gl_texturealphamode", "gl1_texturealphamode"},
            {"gl_texturesolidmode", "gl1_texturesolidmode"},
            {"gl_ztrick", "gl1_ztrick"},
            {"gl_msaa_samples", "r_msaa_samples"},
            {"gl_nolerp_list", "r_nolerp_list"},
            {"gl_retexturing", "r_retexturing"},
            {"gl_shadows", "r_shadows"},
            {"gl_anisotropic", "r_anisotropic"},
            {"gl_lightmap", "r_lighmap"},
            {"intensity", "gl1_intensity"}
        };

        public bool userinfo_modified = true;


        private bool Cvar_InfoValidate(string s)
        {
            if (s.Contains("\\"))
            {
                return false;
            }

            if (s.Contains("\""))
            {
                return false;
            }

            if (s.Contains(";"))
            {
                return false;
            }

            return true;
        }

        private cvar_t? Cvar_FindVar(string var_name)
        {
            if (replacements.ContainsKey(var_name)) {
                var_name = replacements[var_name];
            }

            if (cvar_vars.ContainsKey(var_name)) {
                return cvar_vars[var_name];
            }

            return null;
        }

        public bool Cvar_VariableBool(string var_name)
        {
            return Cvar_FindVar(var_name)?.Bool ?? false;
        }

        public int Cvar_VariableInt(string var_name)
        {
            return Cvar_FindVar(var_name)?.Int ?? 0;
        }

        public string Cvar_VariableString(string var_name)
        {
            return Cvar_FindVar(var_name)?.str ?? "";
        }

        /*
        * If the variable already exists, the value will not be set
        * The flags will be or'ed in if the variable exists.
        */
        public cvar_t? Cvar_Get(string var_name, string? var_value, int flags)
        {
            if ((flags & (cvar_t.CVAR_USERINFO | cvar_t.CVAR_SERVERINFO)) != 0)
            {
                if (!Cvar_InfoValidate(var_name))
                {
                    Com_Printf("invalid info cvar name\n");
                    return null;
                }
            }

            var v = Cvar_FindVar(var_name);

            if (v != null)
            {
                v.flags |= flags;

                if (var_value == null)
                {
                    v.default_string = "";
                }
                else
                {
                    v.default_string = var_value;
                }

                return v;
            }

            if (var_value == null)
            {
                return null;
            }

            if ((flags & (cvar_t.CVAR_USERINFO | cvar_t.CVAR_SERVERINFO)) != 0)
            {
                if (!Cvar_InfoValidate(var_value))
                {
                    Com_Printf("invalid info cvar value\n");
                    return null;
                }
            }

            // if $game is the default one ("baseq2"), then use "" instead because
            // other code assumes this behavior (e.g. FS_BuildGameSpecificSearchPath())
            // if(strcmp(var_name, "game") == 0 && strcmp(var_value, BASEDIRNAME) == 0)
            // {
            //     var_value = "";
            // }

            v = new cvar_t(var_name, var_value, flags);

            cvar_vars[var_name] = v;

            return v;
        }

        private cvar_t? Cvar_Set2(string var_name, string value, bool force)
        {
            var v = Cvar_FindVar(var_name);

            if (v == null)
            {
                return Cvar_Get(var_name, value, 0);
            }

            if ((v.flags & (cvar_t.CVAR_USERINFO | cvar_t.CVAR_SERVERINFO)) != 0)
            {
                if (!Cvar_InfoValidate(value))
                {
                    Com_Printf("invalid info cvar value\n");
                    return v;
                }
            }

            // if $game is the default one ("baseq2"), then use "" instead because
            // other code assumes this behavior (e.g. FS_BuildGameSpecificSearchPath())
            // if(strcmp(var_name, "game") == 0 && strcmp(value, BASEDIRNAME) == 0)
            // {
            //     value = "";
            // }

            if (!force)
            {
                if ((v.flags & cvar_t.CVAR_NOSET) != 0)
                {
                    Com_Printf($"{var_name} is write protected.\n");
                    return v;
                }

                if ((v.flags & cvar_t.CVAR_LATCH) != 0)
                {
                    if (v.latched_string != null)
                    {
                        if (value.CompareTo( v.latched_string) == 0)
                        {
                            return v;
                        }

                        v.latched_string = null;
                    }
                    else
                    {
                        if (value.CompareTo( v.str) == 0)
                        {
                            return v;
                        }
                    }

                    // if (Com_ServerState())
                    // {
                    //     Com_Printf("%s will be changed for next game.\n", var_name);
                    //     var->latched_string = CopyString(value);
                    // }
                    // else
                    // {
                        v.str = value;
                    //     var->value = (float)strtod(var->string, (char **)NULL);

                    //     if (!strcmp(var->name, "game"))
                    //     {
                    //         FS_BuildGameSpecificSearchPath(var->string);
                    //     }
                    // }

                    return v;
                }
            }
            else
            {
                v.latched_string = null;
            }

            if (value.CompareTo(v.str) == 0)
            {
                return v;
            }

            v.modified = true;

            if ((v.flags & cvar_t.CVAR_USERINFO) != 0)
            {
                userinfo_modified = true;
            }

            v.str = value;

            return v;
        }

        public cvar_t? Cvar_ForceSet(string var_name, string value)
        {
            return Cvar_Set2(var_name, value, true);
        }

        public cvar_t? Cvar_Set(string var_name, string value)
        {
            return Cvar_Set2(var_name, value, false);
        }

        public cvar_t? Cvar_FullSet(string var_name,string value, int flags)
        {
            var v = Cvar_FindVar(var_name);

            if (v == null)
            {
                return Cvar_Get(var_name, value, flags);
            }

            v.modified = true;

            if ((v.flags & cvar_t.CVAR_USERINFO) != 0)
            {
                userinfo_modified = true;
            }

            // if $game is the default one ("baseq2"), then use "" instead because
            // other code assumes this behavior (e.g. FS_BuildGameSpecificSearchPath())
            // if(strcmp(var_name, "game") == 0 && strcmp(value, BASEDIRNAME) == 0)
            // {
            //     value = "";
            // }

            v.str = value;
            v.flags = flags;

            return v;
        }

        /*
        * Handles variable inspection and changing from the console
        */
        private bool Cvar_Command(string[] args)
        {
            /* check variables */
            var v = Cvar_FindVar(args[0]);

            if (v == null)
            {
                return false;
            }

            /* perform a variable print or set */
            if (args.Length == 1)
            {
                Com_Printf($"\"{v.name}\" is \"{v.str}\"\n");
                return true;
            }

            /* Another evil hack: The user has just changed 'game' trough
            the console. We reset userGivenGame to that value, otherwise
            we would revert to the initialy given game at disconnect. */
            // if (strcmp(v->name, "game") == 0)
            // {
            //     Q_strlcpy(userGivenGame, Cmd_Argv(1), sizeof(userGivenGame));
            // }

            Cvar_Set(v.name, args[1]);
            return true;
        }

        private string Cvar_BitInfo(int bit)
        {
            var info = "";

            foreach (var v in cvar_vars)
            {
                if ((v.Value.flags & bit) != 0)
                {
                    info = QShared.Info_SetValueForKey(this, info, v.Key, v.Value.str);
                }
            }

            return info;
        }

        /*
        * returns an info string containing
        * all the CVAR_USERINFO cvars
        */
        public string Cvar_Userinfo()
        {
            return Cvar_BitInfo(cvar_t.CVAR_USERINFO);
        }

        /*
        * returns an info string containing
        * all the CVAR_SERVERINFO cvars
        */
        public string Cvar_Serverinfo()
        {
            return Cvar_BitInfo(cvar_t.CVAR_SERVERINFO);
        }

        /*
        * Allows setting and defining of arbitrary cvars from console
        */
        private void Cvar_Set_f(string[] args)
        {
            if ((args.Length != 3) && (args.Length != 4))
            {
                Com_Printf("usage: set <variable> <value> [u / s]\n");
                return;
            }

            var firstarg = args[1].ToLower();

            /* An ugly hack to rewrite changed CVARs */
            if (replacements.ContainsKey(firstarg))
            {
                firstarg = replacements[firstarg];
            }

            if (args.Length == 4)
            {
                int flags = 0;

                if (args[3].Equals("u"))
                {
                    flags = cvar_t.CVAR_USERINFO;
                }

                else if (args[3].Equals("s"))
                {
                    flags = cvar_t.CVAR_SERVERINFO;
                }

                else
                {
                    Com_Printf("flags can only be 'u' or 's'\n");
                    return;
                }

                Cvar_FullSet(firstarg, args[2], flags);
            }
            else
            {
                Cvar_Set(firstarg, args[2]);
            }
        }

        /*
        * Reads in all archived cvars
        */
        private void Cvar_Init()
        {
            // Cmd_AddCommand("cvarlist", Cvar_List_f);
            // Cmd_AddCommand("dec", Cvar_Inc_f);
            // Cmd_AddCommand("inc", Cvar_Inc_f);
            // Cmd_AddCommand("reset", Cvar_Reset_f);
            // Cmd_AddCommand("resetall", Cvar_ResetAll_f);
            Cmd_AddCommand("set", Cvar_Set_f);
            // Cmd_AddCommand("toggle", Cvar_Toggle_f);
        }

    }
}
