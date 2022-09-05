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
 * The PCX file format
 *
 * =======================================================================
 */

namespace Quake2 {

    class QPCX
    {
        private record struct pcx_t
        {
            public readonly byte manufacturer;
            public readonly byte version;
            public readonly byte encoding;
            public readonly byte bits_per_pixel;
            public ushort xmin, ymin, xmax, ymax;
            public ushort hres, vres;
            // unsigned char palette[48];
            // public readonly byte  reserved;
            public readonly byte color_planes;
            public ushort bytes_per_line;
            public ushort palette_type;
            // char filler[58];
            // unsigned char data;   /* unbounded */

            public pcx_t(byte[] buffer)
            {
                manufacturer = buffer[0];
                version = buffer[1];
                encoding = buffer[2];
                bits_per_pixel = buffer[3];
                xmin = BitConverter.ToUInt16(buffer, 4);
                ymin = BitConverter.ToUInt16(buffer, 6);
                xmax = BitConverter.ToUInt16(buffer, 8);
                ymax = BitConverter.ToUInt16(buffer, 10);
                hres = BitConverter.ToUInt16(buffer, 12);
                vres = BitConverter.ToUInt16(buffer, 14);
                color_planes = buffer[65];
                bytes_per_line = BitConverter.ToUInt16(buffer, 66);
                palette_type = BitConverter.ToUInt16(buffer, 68);
            }

            public static int size = 128;
        }

        public static void Load(refimport_t re, string filename, out byte[]? pic, out byte[]? palette, out int width, out int height)
        {
            /* Add the extension */
            if (!filename.EndsWith(".pcx"))
            {
                filename = filename + ".pcx";
            }

            pic = null;
            palette = null;
            width = -1;
            height = -1;

            /* load the file */
            var raw = re.FS_LoadFile(filename);
            if (raw == null || raw.Length < pcx_t.size)
            {
                re.Com_VPrintf(QShared.PRINT_DEVELOPER, $"Bad pcx file {filename}\n");
                return;
            }

            /* parse the PCX file */
            var pcx = new pcx_t(raw);

            // raw = &pcx->data;

            int pcx_width = pcx.xmax - pcx.xmin;
            int pcx_height = pcx.ymax - pcx.ymin;

            if ((pcx.manufacturer != 0x0a) || (pcx.version != 5) ||
                (pcx.encoding != 1) || (pcx.bits_per_pixel != 8) ||
                (pcx_width >= 4096) || (pcx_height >= 4096))
            {
                re.Com_VPrintf(QShared.PRINT_ALL, $"Bad pcx file {filename}\n");
                return;
            }

            int full_size = (pcx_height + 1) * (pcx_width + 1);
            pic = new byte[full_size];
            if (raw.Length - pcx_t.size - full_size >= 768)
            {
                palette = new byte[768];
                Array.Copy(raw, raw.Length-768, palette, 0, 768);
            }

            int pix_i = 0;
            int pcx_i = pcx_t.size;
            bool image_issues = false;
            for (int y = 0; y <= pcx_height; y++, pix_i += pcx_width + 1)
            {
                for (int x = 0; x <= pcx_width; )
                {
                    if (pcx_i > raw.Length)
                    {
                        // no place for read
                        image_issues = true;
                        x = pcx_width;
                        break;
                    }
                    var dataByte = raw[pcx_i++];
                    var runLength = 1;

                    if ((dataByte & 0xC0) == 0xC0)
                    {
                        runLength = dataByte & 0x3F;
                        if (pcx_i > raw.Length)
                        {
                            // no place for read
                            image_issues = true;
                            x = pcx_width;
                            break;
                        }
                        dataByte = dataByte = raw[pcx_i++];
                    }

                    while (runLength-- > 0)
                    {
                        if (full_size <= (pix_i + x))
                        {
                            // no place for write
                            image_issues = true;
                            x += runLength;
                            runLength = 0;
                        }
                        else
                        {
                            pic[pix_i + x++] = dataByte;
                        }
                    }
                }
            }

            if (pcx_i > raw.Length)
            {
                re.Com_VPrintf(QShared.PRINT_DEVELOPER, $"PCX file {filename} was malformed");
                pic = null;
            }
            // else if(pcx_width == 319 && pcx_height == 239
            //         && Q_strcasecmp(origname, "pics/quit.pcx") == 0
            //         && Com_BlockChecksum(pcx, len) == 3329419434u)
            // {
            //     // it's the quit screen, and the baseq2 one (identified by checksum)
            //     // so fix it
            //     fixQuitScreen(*pic);
            // }

            if (image_issues)
            {
                re.Com_VPrintf(QShared.PRINT_ALL, $"PCX file {filename} has possible size issues.\n");
            }

            width = pcx_width + 1;
            height = pcx_height + 1;

            // ri.FS_FreeFile(pcx);
        }        
    }
}