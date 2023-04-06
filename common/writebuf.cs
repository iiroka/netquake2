using System.Numerics;
using System.Text;

namespace Quake2 {
    internal class QWritebuf
    {
        public bool allowoverflow;     /* if false, do a Com_Error */
        public bool overflowed;        /* set to true if the buffer size failed */
        private byte[] data;
        private int cursize;

        public ReadOnlySpan<byte> Data {
            get { return new ReadOnlySpan<byte>(data, 0, cursize); }
        }

        public int Size {
            get { return cursize; }
        }

        public QWritebuf(int size)
        {
            allowoverflow = false;
            overflowed = false;
            data = new byte[size];
            cursize = 0;
        }

        public void Clear() 
        {
            cursize = 0;
        }

        private int GetSpace(int length)
        {
            if (cursize + length > data.Length)
            {
                if (!allowoverflow)
                {
                    throw new Exception("SZ_GetSpace: overflow without allowoverflow set");
                }

                if (length > data.Length)
                {
                    throw new Exception($"SZ_GetSpace:{length} is > full buffer size");
                }

                cursize = 0;
                overflowed = true;
                Console.WriteLine("SZ_GetSpace: overflow");
            }

            var index = cursize;
            cursize += length;

            return index;
        }

        public void WriteChar(int c)
        {
            var indx = GetSpace(1);
            data[indx] = (byte)c;
        }

        public void WriteByte(int c)
        {
            var indx = GetSpace(1);
            data[indx] = (byte)(c & 0xFF);
        }

        public void WriteShort(int c)
        {
            var indx = GetSpace(2);
            data[indx + 0] = (byte)(c & 0xff);
            data[indx + 1] = (byte)(c >> 8);
        }

        public void WriteLong(int c)
        {
            var indx = GetSpace(4);
            data[indx + 0] = (byte)(c & 0xff);
            data[indx + 1] = (byte)((c >> 8) & 0xff);
            data[indx + 2] = (byte)((c >> 16) & 0xff);
            data[indx + 3] = (byte)(c >> 24);
        }

        public void WriteFloat(float f)
        {
            var indx = GetSpace(4);
            var v = BitConverter.SingleToUInt32Bits(f);
            data[indx + 0] = (byte)(v & 0xff);
            data[indx + 1] = (byte)((v >> 8) & 0xff);
            data[indx + 2] = (byte)((v >> 16) & 0xff);
            data[indx + 3] = (byte)(v >> 24);
        }

        public void Write(in byte[] buf)
        {
            var indx = GetSpace(buf.Length);
            Array.Copy(buf, 0, data, indx, buf.Length);
        }

        public void Write(in ReadOnlySpan<byte> buf)
        {
            var indx = GetSpace(buf.Length);
            for (int i = 0; i < buf.Length; i++)
            {
                data[indx + i] = buf[i];
            }
        }

        public void Print(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            if (cursize > 0)
            {
                if (data[cursize - 1] != 0)
                {
                    Write(bytes);
                    WriteByte(0);
                }
                else
                {
                    var indx = GetSpace(bytes.Length);
                    Array.Copy(bytes, 0, data, indx-1, bytes.Length);
                    data[indx + bytes.Length] = 0;
                }
            }
            else
            {
                Write(bytes);
                WriteByte(0);
            }
        }


        public void WriteString(string s)
        {
            if (!String.IsNullOrEmpty(s))
            {
                Write(Encoding.UTF8.GetBytes(s));
            }
            WriteByte(0);
        }

        public void WriteCoord(float f)
        {
            WriteShort((int)(f * 8));
        }

        public void WritePos(in Vector3 pos)
        {
            WriteShort((int)(pos.X * 8));
            WriteShort((int)(pos.Y * 8));
            WriteShort((int)(pos.Z * 8));
        }

        public void WriteAngle(float f)
        {
            WriteByte((int)(f * 256 / 360) & 255);
        }

        public void WriteAngle16(float f)
        {
            WriteShort(QShared.ANGLE2SHORT(f));
        }

        public void WriteDir(in Vector3 dir)
        {
            if (dir == null)
            {
                WriteByte(0);
                return;
            }

            var bestd = 0f;
            var best = 0;

            for (var i = 0; i < QShared.bytedirs.Length; i++)
            {
                var d = Vector3.Dot(dir, QShared.bytedirs[i]);

                if (d > bestd)
                {
                    bestd = d;
                    best = i;
                }
            }

            WriteByte(best);
        }

        public void WriteDeltaUsercmd(in QShared.usercmd_t from, in QShared.usercmd_t cmd)
        {
            /* Movement messages */
            uint bits = 0;

            if (cmd.angles[0] != from.angles[0])
            {
                bits |= QCommon.CM_ANGLE1;
            }

            if (cmd.angles[1] != from.angles[1])
            {
                bits |= QCommon.CM_ANGLE2;
            }

            if (cmd.angles[2] != from.angles[2])
            {
                bits |= QCommon.CM_ANGLE3;
            }

            if (cmd.forwardmove != from.forwardmove)
            {
                bits |= QCommon.CM_FORWARD;
            }

            if (cmd.sidemove != from.sidemove)
            {
                bits |= QCommon.CM_SIDE;
            }

            if (cmd.upmove != from.upmove)
            {
                bits |= QCommon.CM_UP;
            }

            if (cmd.buttons != from.buttons)
            {
                bits |= QCommon.CM_BUTTONS;
            }

            if (cmd.impulse != from.impulse)
            {
                bits |= QCommon.CM_IMPULSE;
            }

            WriteByte((int)bits);

            if ((bits & QCommon.CM_ANGLE1) != 0)
            {
                WriteShort(cmd.angles[0]);
            }

            if ((bits & QCommon.CM_ANGLE2) != 0)
            {
                WriteShort(cmd.angles[1]);
            }

            if ((bits & QCommon.CM_ANGLE3) != 0)
            {
                WriteShort(cmd.angles[2]);
            }

            if ((bits & QCommon.CM_FORWARD) != 0)
            {
                WriteShort(cmd.forwardmove);
            }

            if ((bits & QCommon.CM_SIDE) != 0)
            {
                WriteShort(cmd.sidemove);
            }

            if ((bits & QCommon.CM_UP) != 0)
            {
                WriteShort(cmd.upmove);
            }

            if ((bits & QCommon.CM_BUTTONS) != 0)
            {
                WriteByte(cmd.buttons);
            }

            if ((bits & QCommon.CM_IMPULSE) != 0)
            {
                WriteByte(cmd.impulse);
            }

            WriteByte(cmd.msec);
            WriteByte(cmd.lightlevel);
        }

        /*
        * Writes part of a packetentities message.
        * Can delta from either a baseline or a previous packet_entity
        */
        public void WriteDeltaEntity(in QShared.entity_state_t from,
                in QShared.entity_state_t to,
                bool force,
                bool newentity,
                QCommon common)
        {
            if (to.number == 0)
            {
                common.Com_Error(QShared.ERR_FATAL, "Unset entity number");
            }

            if (to.number >= QShared.MAX_EDICTS)
            {
                common.Com_Error(QShared.ERR_FATAL, "Entity number >= MAX_EDICTS");
            }

            /* send an update */
            uint bits = 0;

            if (to.number >= 256)
            {
                bits |= QCommon.U_NUMBER16; /* number8 is implicit otherwise */
            }

            if (to.origin.X != from.origin.X)
            {
                bits |= QCommon.U_ORIGIN1;
            }

            if (to.origin.Y != from.origin.Y)
            {
                bits |= QCommon.U_ORIGIN2;
            }

            if (to.origin.Z != from.origin.Z)
            {
                bits |= QCommon.U_ORIGIN3;
            }

            if (to.angles.X != from.angles.X)
            {
                bits |= QCommon.U_ANGLE1;
            }

            if (to.angles.Y != from.angles.Y)
            {
                bits |= QCommon.U_ANGLE2;
            }

            if (to.angles.Z != from.angles.Z)
            {
                bits |= QCommon.U_ANGLE3;
            }

            if (to.skinnum != from.skinnum)
            {
                if ((uint)to.skinnum < 256)
                {
                    bits |= QCommon.U_SKIN8;
                }

                else if ((uint)to.skinnum < 0x10000)
                {
                    bits |= QCommon.U_SKIN16;
                }

                else
                {
                    bits |= (QCommon.U_SKIN8 | QCommon.U_SKIN16);
                }
            }

            if (to.frame != from.frame)
            {
                if (to.frame < 256)
                {
                    bits |= QCommon.U_FRAME8;
                }

                else
                {
                    bits |= QCommon.U_FRAME16;
                }
            }

            if (to.effects != from.effects)
            {
                if (to.effects < 256)
                {
                    bits |= QCommon.U_EFFECTS8;
                }

                else if (to.effects < 0x8000)
                {
                    bits |= QCommon.U_EFFECTS16;
                }

                else
                {
                    bits |= QCommon.U_EFFECTS8 | QCommon.U_EFFECTS16;
                }
            }

            if (to.renderfx != from.renderfx)
            {
                if (to.renderfx < 256)
                {
                    bits |= QCommon.U_RENDERFX8;
                }

                else if (to.renderfx < 0x8000)
                {
                    bits |= QCommon.U_RENDERFX16;
                }

                else
                {
                    bits |= QCommon.U_RENDERFX8 | QCommon.U_RENDERFX16;
                }
            }

            if (to.solid != from.solid)
            {
                bits |= QCommon.U_SOLID;
            }

            /* event is not delta compressed, just 0 compressed */
            if (to.ev != 0)
            {
                bits |= QCommon.U_EVENT;
            }

            if (to.modelindex != from.modelindex)
            {
                bits |= QCommon.U_MODEL;
            }

            if (to.modelindex2 != from.modelindex2)
            {
                bits |= QCommon.U_MODEL2;
            }

            if (to.modelindex3 != from.modelindex3)
            {
                bits |= QCommon.U_MODEL3;
            }

            if (to.modelindex4 != from.modelindex4)
            {
                bits |= QCommon.U_MODEL4;
            }

            if (to.sound != from.sound)
            {
                bits |= QCommon.U_SOUND;
            }

            if (newentity || (to.renderfx & QShared.RF_BEAM) != 0)
            {
                bits |= QCommon.U_OLDORIGIN;
            }

            /* write the message */
            if (bits == 0 && !force)
            {
                return; /* nothing to send! */
            }

            if ((bits & 0xff000000) != 0)
            {
                bits |= QCommon.U_MOREBITS3 | QCommon.U_MOREBITS2 | QCommon.U_MOREBITS1;
            }

            else if ((bits & 0x00ff0000) != 0)
            {
                bits |= QCommon.U_MOREBITS2 | QCommon.U_MOREBITS1;
            }

            else if ((bits & 0x0000ff00) != 0)
            {
                bits |= QCommon.U_MOREBITS1;
            }

            WriteByte((int)(bits & 255));

            if ((bits & 0xff000000) != 0)
            {
                WriteByte((byte)((bits >> 8) & 255));
                WriteByte((byte)((bits >> 16) & 255));
                WriteByte((byte)((bits >> 24) & 255));
            }

            else if ((bits & 0x00ff0000) != 0)
            {
                WriteByte((byte)((bits >> 8) & 255));
                WriteByte((byte)((bits >> 16) & 255));
            }

            else if ((bits & 0x0000ff00) != 0)
            {
                WriteByte((byte)((bits >> 8) & 255));
            }

            if ((bits & QCommon.U_NUMBER16) != 0)
            {
                WriteShort(to.number);
            }

            else
            {
                WriteByte(to.number);
            }

            if ((bits & QCommon.U_MODEL) != 0)
            {
                WriteByte(to.modelindex);
            }

            if ((bits & QCommon.U_MODEL2) != 0)
            {
                WriteByte(to.modelindex2);
            }

            if ((bits & QCommon.U_MODEL3) != 0)
            {
                WriteByte(to.modelindex3);
            }

            if ((bits & QCommon.U_MODEL4) != 0)
            {
                WriteByte(to.modelindex4);
            }

            if ((bits & QCommon.U_FRAME8) != 0)
            {
                WriteByte(to.frame);
            }

            if ((bits & QCommon.U_FRAME16) != 0)
            {
                WriteShort(to.frame);
            }

            if ((bits & QCommon.U_SKIN8) != 0 && (bits & QCommon.U_SKIN16) != 0) /*used for laser colors */
            {
                WriteLong(to.skinnum);
            }

            else if ((bits & QCommon.U_SKIN8) != 0)
            {
                WriteByte(to.skinnum);
            }

            else if ((bits & QCommon.U_SKIN16) != 0)
            {
                WriteShort(to.skinnum);
            }

            if ((bits & (QCommon.U_EFFECTS8 | QCommon.U_EFFECTS16)) == (QCommon.U_EFFECTS8 | QCommon.U_EFFECTS16))
            {
                WriteLong((int)to.effects);
            }

            else if ((bits & QCommon.U_EFFECTS8) != 0)
            {
                WriteByte((int)to.effects);
            }

            else if ((bits & QCommon.U_EFFECTS16) != 0)
            {
                WriteShort((int)to.effects);
            }

            if ((bits & (QCommon.U_RENDERFX8 | QCommon.U_RENDERFX16)) == (QCommon.U_RENDERFX8 | QCommon.U_RENDERFX16))
            {
                WriteLong(to.renderfx);
            }

            else if ((bits & QCommon.U_RENDERFX8) != 0)
            {
                WriteByte(to.renderfx);
            }

            else if ((bits & QCommon.U_RENDERFX16) != 0)
            {
                WriteShort(to.renderfx);
            }

            if ((bits & QCommon.U_ORIGIN1) != 0)
            {
                WriteCoord(to.origin.X);
            }

            if ((bits & QCommon.U_ORIGIN2) != 0)
            {
                WriteCoord(to.origin.Y);
            }

            if ((bits & QCommon.U_ORIGIN3) != 0)
            {
                WriteCoord(to.origin.Z);
            }

            if ((bits & QCommon.U_ANGLE1) != 0)
            {
                WriteAngle(to.angles.X);
            }

            if ((bits & QCommon.U_ANGLE2) != 0)
            {
                WriteAngle(to.angles.Y);
            }

            if ((bits & QCommon.U_ANGLE3) != 0)
            {
                WriteAngle(to.angles.Z);
            }

            if ((bits & QCommon.U_OLDORIGIN) != 0)
            {
                WriteCoord(to.old_origin.X);
                WriteCoord(to.old_origin.Y);
                WriteCoord(to.old_origin.Z);
            }

            if ((bits & QCommon.U_SOUND) != 0)
            {
                WriteByte(to.sound);
            }

            if ((bits & QCommon.U_EVENT) != 0)
            {
                WriteByte(to.ev);
            }

            if ((bits & QCommon.U_SOLID) != 0)
            {
                WriteShort(to.solid);
            }
        }
    }
}