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
 * Common argument processing
 *
 * =======================================================================
 */
using System.Text;

namespace Quake2 {

    partial class QCommon {

        private string[] com_args = {};

        public int COM_Argc() {
	        return com_args.Length;
        }

        public string COM_Argv(int arg) {
	        if ((arg < 0) || (arg >= com_args.Length) || String.IsNullOrEmpty(com_args[arg]))
    	    {
		        return "";
	        }

	        return com_args[arg];
        }

        void COM_ClearArgv(int arg)
        {
            if ((arg < 0) || (arg >= com_args.Length) || String.IsNullOrEmpty(com_args[arg]))
            {
                return;
            }

            com_args[arg] = "";
        }

        void COM_InitArgv(string[] args)
        {
            com_args = args;
        }

    }
}