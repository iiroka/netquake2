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
 * This file implements all client side lighting
 *
 * =======================================================================
 */

namespace Quake2 {

    partial class QClient {

        private struct clightstyle_t
        {
            public float[] value;
            public float[] map;
        }

        private clightstyle_t[] cl_lightstyle = new clightstyle_t[QRef.MAX_LIGHTSTYLES];
        private int lastofs;


        private void CL_ClearLightStyles()
        {
            for (int i = 0; i < cl_lightstyle.Length; i++) {
                cl_lightstyle[i].value = new float[3];
                cl_lightstyle[i].map = new float[0];
            }
            lastofs = -1;
        }

        private void CL_RunLightStyles()
        {
            int ofs = cl.time / 100;

            if (ofs == lastofs)
            {
                return;
            }

            lastofs = ofs;

            for (int i = 0; i < QRef.MAX_LIGHTSTYLES; i++)
            {
                float v;
                if (cl_lightstyle[i].map == null || cl_lightstyle[i].map.Length == 0)
                {
                    v = 1;
                } 
                else if ( cl_lightstyle[i].map.Length == 1)
                {
                    v = cl_lightstyle[i].map[0];
                }
                else
                {
                    v = cl_lightstyle[i].map[ofs % cl_lightstyle[i].map.Length];
                }
                cl_lightstyle[i].value = new float[3]{ v, v, v};
            }
        }

        private void CL_SetLightstyle(int i)
        {
            var s = cl.configstrings[i + QShared.CS_LIGHTS];

            cl_lightstyle[i].map = new float[s.Length];

            for (int k = 0; k < s.Length; k++)
            {
                cl_lightstyle[i].map[k] = (float)(s[k] - 'a') / (float)('m' - 'a');
            }
        }

        private void CL_AddLightStyles()
        {
            for (int i = 0; i < QRef.MAX_LIGHTSTYLES; i++)
            {
                V_AddLightStyle(i, cl_lightstyle[i].value[0], cl_lightstyle[i].value[1], cl_lightstyle[i].value[2]);
            }
        }

    }
}