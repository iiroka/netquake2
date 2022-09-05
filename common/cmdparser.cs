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
 * This file implements the Quake II command processor. Every command
 * which is send via the command line at startup, via the console and
 * via rcon is processed here and send to the apropriate subsystem.
 *
 * =======================================================================
 */
using System.Text;

namespace Quake2 {

    partial class QCommon {

        private const int ALIAS_LOOP_COUNT = 16;


        public delegate void CommandHandler(string[] args);

        private string cmd_text = "";
        private int alias_count;
        private int cmd_wait = 0;

        private IDictionary<string, CommandHandler?> cmd_functions = new Dictionary<string, CommandHandler?>();
        private IDictionary<string, string> cmd_alias = new Dictionary<string, string>();

        /*
        * Adds command text at the end of the buffer
        */
        public void Cbuf_AddText(string text)
        {
            cmd_text = cmd_text + text;
        }

        /*
        * Adds command text immediately after the current command
        * Adds a \n to the text
        */
        public void Cbuf_InsertText(string text)
        {
            cmd_text = text + "\n" + cmd_text;
        }


        public void Cbuf_Execute()
        {
            // int i;
            // char *text;
            // char line[1024];
            // int quotes;

            if(cmd_wait > 0)
            {
                // make sure that "wait" in scripts waits for ~16.66ms (1 frame at 60fps)
                // regardless of framerate
                if (Sys_Milliseconds() - cmd_wait <= 16)
                {
                    return;
                }
                cmd_wait = 0;
            }

            alias_count = 0; /* don't allow infinite alias loops */

            while (cmd_text.Length > 0)
            {
                /* find a \n or ; line break */
                var quotes = 0;
                int i;
                for (i = 0; i < cmd_text.Length; i++)
                {
                    if (cmd_text[i] == '"')
                    {
                        quotes++;
                    }

                    if ((quotes & 1) == 0 && (cmd_text[i] == ';'))
                    {
                        break; /* don't break if inside a quoted string */
                    }

                    if (cmd_text[i] == '\n')
                    {
                        break;
                    }
                }

            //     if (i > sizeof(line) - 1)
            //     {
            //         memcpy(line, text, sizeof(line) - 1);
            //         line[sizeof(line) - 1] = 0;
            //     }
            //     else
            //     {
            //         memcpy(line, text, i);
            //         line[i] = 0;
            //     }

                /* delete the text from the command buffer and move remaining
                commands down this is necessary because commands (exec,
                alias) can insert data at the beginning of the text buffer */
                string line;
                if (i == cmd_text.Length)
                {
                    line = cmd_text;
                    cmd_text = "";
                }
                else
                {
                    line = cmd_text.Substring(0, i);
                    cmd_text = cmd_text.Substring(i + 1);
                }

                /* execute the command line */
                Cmd_ExecuteString(line);

                if (cmd_wait > 0)
                {
                    /* skip out while text still remains in buffer,
                    leaving it for after we're done waiting */
                    break;
                }
            }
        }

        /*
        * Parses the given string into command line tokens.
        * $Cvars will be expanded unless they are in a quoted token
        */
        public string[] Cmd_TokenizeString(string text, bool macroExpand)
        {
            // int i;
            // const char *com_token;

            // /* clear the args from the last string */
            // for (i = 0; i < cmd_argc; i++)
            // {
            //     Z_Free(cmd_argv[i]);
            // }

            var args = new List<string>();
            // cmd_argc = 0;
            // cmd_args[0] = 0;

            // /* macro expand the text */
            // if (macroExpand)
            // {
            //     text = Cmd_MacroExpandString(text);
            // }

            if (String.IsNullOrEmpty(text))
            {
                return new string[]{};
            }

            int index = 0;
            while (index >= 0)
            {
                /* skip whitespace up to a /n */
                while (index < text.Length && text[index] <= ' ' && text[index] != '\n')
                {
                    index++;
                }
                if (index >= text.Length)
                {
                    return args.ToArray();
                }

                if (text[index] == '\n')
                {
                    /* a newline seperates commands in the buffer */
                    return args.ToArray();
                }

            //     /* set cmd_args to everything after the first arg */
            //     if (cmd_argc == 1)
            //     {
            //         int l;

            //         strcpy(cmd_args, text);

            //         /* strip off any trailing whitespace */
            //         l = strlen(cmd_args) - 1;

            //         for ( ; l >= 0; l--)
            //         {
            //             if (cmd_args[l] <= ' ')
            //             {
            //                 cmd_args[l] = 0;
            //             }

            //             else
            //             {
            //                 break;
            //             }
            //         }
            //     }

                var com_token = QShared.COM_Parse(text, ref index);

                if (index < 0)
                {
                    return args.ToArray();
                }

                args.Add(com_token);
            }
            return args.ToArray();
        }


        public void Cmd_AddCommand(string cmd_name, CommandHandler? function)
        {
            var cmd = cmd_name.ToLower();

            /* fail if the command is a variable name */
            // if (Cvar_VariableString(cmd_name)[0])
            // {
            //     Cmd_RemoveCommand(cmd_name);
            // }

            /* fail if the command already exists */
            if (cmd_functions.ContainsKey(cmd))
            {
                Com_Printf($"Cmd_AddCommand: {cmd_name} already defined\n");
                return;
            }

            cmd_functions[cmd] = function;

        }


        /* ugly hack to suppress warnings from default.cfg in Key_Bind_f() */
        public bool doneWithDefaultCfg;

        /*
        * A complete command line has been parsed, so try to execute it
        */
        public void Cmd_ExecuteString(string text)
        {
        //     cmd_function_t *cmd;
        //     cmdalias_t *a;

            var args = Cmd_TokenizeString(text, true);

            /* execute the command line */
            if (args.Length == 0)
            {
                return; /* no tokens */
            }

            if(args.Length > 1 && args[0].CompareTo("exec") == 0 && args[1].CompareTo("yq2.cfg") == 0)
            {
                /* exec yq2.cfg is done directly after exec default.cfg, see Qcommon_Init() */
                doneWithDefaultCfg = true;
            }
            var cmd = args[0].ToLower();

            /* check functions */
            if (cmd_functions.ContainsKey(cmd))
            {
                var func = cmd_functions[cmd];
                if (func == null)
                {
                    /* forward to server command */
                    Cmd_ExecuteString($"cmd {text}");
                }
                else
                {
                    func(args);
                }
                return;
            }

            /* check alias */
            if (cmd_alias.ContainsKey(cmd))
            {
                if (++alias_count == ALIAS_LOOP_COUNT)
                {
                    Com_Printf("ALIAS_LOOP_COUNT\n");
                    return;
                }
                Cbuf_InsertText(cmd_alias[cmd]);
                return;
            }

            /* check cvars */
            if (Cvar_Command(args))
            {
                return;
            }

        // #ifndef DEDICATED_ONLY
        //     /* send it as a server command if we are connected */
        //     Cmd_ForwardToServer();
        // #endif
            Console.WriteLine($"Unknown command \"{args[0]}\"");
        }

        /*
        * Execute a script file
        */
        private void Cmd_Exec_f(string[] args)
        {
            if (args.Length != 2)
            {
                Com_Printf("exec <filename> : execute a script file\n");
                return;
            }

            var buf = FS_LoadFile(args[1]);

            if (buf == null)
            {
                Com_Printf($"couldn't exec {args[1]}\n");
                return;
            }

            Com_Printf($"execing {args[1]}.\n");

            Cbuf_InsertText(Encoding.UTF8.GetString(buf));
        }

        /*
        * Just prints the rest of the line to the console
        */
        private void Cmd_Echo_f(string[] args)
        {
            for (int i = 1; i < args.Length; i++)
            {
                Com_Printf($"{args[i]} ");
            }

            Com_Printf("\n");
        }

        /*
        * Creates a new command that executes
        * a command string (possibly ; seperated)
        */
        private void Cmd_Alias_f(string[] args)
        {
            if (args.Length == 1)
            {
                Com_Printf("Current alias commands:\n");

                foreach (var a in cmd_alias)
                {
                    Com_Printf($"{a.Key} : {a.Value}\n");
                }

                return;
            }

            /* copy the rest of the command line */
            var cmd = new StringBuilder(); /* start out with a null string */

            for (int i = 2; i < args.Length; i++)
            {
                cmd.Append(args[i]);

                if (i != (args.Length - 1))
                {
                    cmd.Append(" ");
                }
            }

            cmd.Append("\n");

            cmd_alias[args[1]] = cmd.ToString();
        }

        /*
        * Causes execution of the remainder of the command buffer to be delayed
        * until next frame.  This allows commands like: bind g "impulse 5 ;
        * +attack ; wait ; -attack ; impulse 2"
        */
        private void Cmd_Wait_f(string[] args)
        {
            cmd_wait = Sys_Milliseconds();
        }


        private void Cmd_Init()
        {
            /* register our commands */
            // Cmd_AddCommand("cmdlist", Cmd_List_f);
            Cmd_AddCommand("exec", Cmd_Exec_f);
            // Cmd_AddCommand("vstr", Cmd_Vstr_f);
            Cmd_AddCommand("echo", Cmd_Echo_f);
            Cmd_AddCommand("alias", Cmd_Alias_f);
            Cmd_AddCommand("wait", Cmd_Wait_f);
        }


    }
}