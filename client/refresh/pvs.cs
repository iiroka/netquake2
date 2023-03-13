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
 * The PVS Decompress
 *
 * =======================================================================
 */

using System.Numerics;

namespace Quake2 {

    class QPVS
    {
        /*
        ===================
        Mod_DecompressVis
        ===================
        */
        public static byte[] Mod_DecompressVis(ReadOnlySpan<byte> ind, int row)
        {
            var decompressed = new byte[QCommon.MAX_MAP_LEAFS / 8];

            if (ind == null)
            {
                /* no vis info, so make all visible */
                Array.Fill(decompressed, (byte)0xFF);
                return decompressed;
            }

            var index = 0;
            var out_i = 0;
            do
            {
                if (ind[index] != 0)
                {
                    decompressed[out_i++] = ind[index++];
                    continue;
                }

                var c = ind[index + 1];
                index += 2;

                while (c > 0)
                {
                    decompressed[out_i++] = 0;
                    c--;
                }
            } while (out_i < row);

            return decompressed;
        }

        public static float Mod_RadiusFromBounds(in Vector3 mins, in Vector3 maxs)
        {
            Vector3 corner = new Vector3();
            corner.X = MathF.Abs(mins.X) > MathF.Abs(maxs.X) ? MathF.Abs(mins.X) : MathF.Abs(maxs.X);
            corner.Y = MathF.Abs(mins.Y) > MathF.Abs(maxs.Y) ? MathF.Abs(mins.Y) : MathF.Abs(maxs.Y);
            corner.Z = MathF.Abs(mins.Z) > MathF.Abs(maxs.Z) ? MathF.Abs(mins.Z) : MathF.Abs(maxs.Z);

            return corner.Length();
        }

    }
}