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
using System.Numerics;

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

        private class cdlight_t
        {
            public int		key; /* so entities can reuse same entry */
            public Vector3	color;
            public Vector3	origin;
            public float	radius;
            public float	die; /* stop lighting after this time */
            public float	decay; /* drop this each second */
            public float	minlight; /* don't add when contributing less */
        }

        private List<cdlight_t> cl_dlights = new List<cdlight_t>();

        void CL_ClearDlights()
        {
	        cl_dlights.Clear();
        }

        private cdlight_t CL_AllocDlight(int key)
        {
            /* first look for an exact key match */
            if (key != 0)
            {
                for (int i = 0; i < cl_dlights.Count; i++)
                {
                    if (cl_dlights[i].key == key)
                    {
                        return cl_dlights[i];
                    }
                }
            }

            /* then look for anything else */
            for (int i = 0; i < cl_dlights.Count; i++)
            {
                if (cl_dlights[i].die < cl.time)
                {
                    cl_dlights[i].key = key;
                    return cl_dlights[i];
                }
            }

            if (cl_dlights.Count < QRef.MAX_DLIGHTS)
            {
                var dl = new cdlight_t();
                dl.key = key;
                cl_dlights.Add(dl);
                return dl;
            }
            else
            {
                cl_dlights[0] = new cdlight_t();
                cl_dlights[0].key = key;
                return cl_dlights[0];
            }
        }

        private void CL_NewDlight(int key, float x, float y, float z, float radius, float time)
        {
            var dl = CL_AllocDlight(key);
            dl.origin = new Vector3(x, y, z);
            dl.radius = radius;
            dl.die = cl.time + time;
        }

        private void CL_RunDLights()
        {
            for (int i = 0; i < cl_dlights.Count(); i++)
            {
                if (cl_dlights[i].radius == 0)
                {
                    continue;
                }

                /* Vanilla Quake II had just cl.time. This worked in 1997
                when computers were slow and the game reached ~30 FPS
                on beefy hardware. Nowadays with 1000 FPS the dlights
                are often rendered just a fraction of a frame. Work
                around that by adding 32 ms, e.g. each dlight is shown
                for at least 32 ms. */
                if (cl_dlights[i].die < cl.time - 32)
                {
                    cl_dlights[i].radius = 0;
                    continue;
                }

                cl_dlights[i].radius -= cls.rframetime * cl_dlights[i].decay;

                if (cl_dlights[i].radius < 0)
                {
                    cl_dlights[i].radius = 0;
                }
            }
        }

        private void CL_AddDLights()
        {
            for (int i = 0; i < cl_dlights.Count(); i++)
            {
                if (cl_dlights[i].radius == 0)
                {
                    continue;
                }

                V_AddLight(cl_dlights[i].origin, cl_dlights[i].radius, cl_dlights[i].color.X, cl_dlights[i].color.Y, cl_dlights[i].color.Z);
            }
        }


    }
}