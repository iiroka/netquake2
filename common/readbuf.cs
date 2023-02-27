using System.Numerics;
using System.Text;

namespace Quake2 {
    internal ref struct QReadbuf
    {
        private ReadOnlySpan<byte> data;
        private int readcount;

        public QReadbuf(ReadOnlySpan<byte> buf)
        {
            data = buf;
            readcount = 0;
        }

        public int Size
        {
	        get { return data.Length; }
        }

        public int Count
        {
	        get { return readcount; }
        }

        public void BeginReading()
        {
	        readcount = 0;
        }

        public int ReadChar()
        {
            int c;

            if (readcount + 1 > data.Length)
            {
                c = -1;
            }

            else
            {
                c = (sbyte)data[readcount];
            }

            readcount++;

            return c;
        }

        public int ReadByte()
        {
            int c;

            if (readcount + 1 > data.Length)
            {
                c = -1;
            }

            else
            {
                c = data[readcount];
            }

            readcount++;

            return c;
        }

        public int ReadShort()
        {
            int c;

            if (readcount + 2 > data.Length)
            {
                c = -1;
            }

            else
            {
                c = (short)(data[readcount]
                            + (data[readcount + 1] << 8));
            }

            readcount += 2;

            return c;
        }

        public int ReadLong()
        {
            int c;

            if (readcount + 4 > data.Length)
            {
                c = -1;
            }

            else
            {
                c = data[readcount]
                    + (data[readcount + 1] << 8)
                    + (data[readcount + 2] << 16)
                    + (data[readcount + 3] << 24);
            }

            readcount += 4;

            return c;
        }

        public string ReadString()
        {
            var sb = new StringBuilder();
            while (true)
            {
                var c = ReadByte();

                if ((c == -1) || (c == 0))
                {
                    break;
                }

                sb.Append((char)c);
            }

            return sb.ToString();
        }

        public string ReadStringLine()
        {
            var sb = new StringBuilder();
            while (true)
            {
                var c = ReadByte();

                if ((c == -1) || (c == 0) || (c == '\n'))
                {
                    break;
                }

                sb.Append((char)c);
            }

            return sb.ToString();
        }

        public float ReadCoord()
        {
            return ReadShort() * 0.125f;
        }

        public Vector3 ReadPos()
        {
            return new Vector3(
                ReadShort() * 0.125f,
                ReadShort() * 0.125f,
                ReadShort() * 0.125f);
        }

        public float ReadAngle()
        {
            return ReadChar() * 1.40625f;
        }

        public float ReadAngle16()
        {
            return QShared.SHORT2ANGLE(ReadShort());
        }

        public Vector3 ReadDir(QCommon common)
        {
            var b = ReadByte();

            if (b < 0 || b >= QShared.bytedirs.Length)
            {
                common.Com_Error(QShared.ERR_DROP, "MSF_ReadDir: out of range");
            }

            return QShared.bytedirs[b];
        }

        public void ReadDeltaUsercmd(in QShared.usercmd_t from, out QShared.usercmd_t move)
        {
            move = from;

            var bits = ReadByte();

            /* read current angles */
            if ((bits & QCommon.CM_ANGLE1) != 0)
            {
                move.angles[0] = (short)ReadShort();
            }

            if ((bits & QCommon.CM_ANGLE2) != 0)
            {
                move.angles[1] = (short)ReadShort();
            }

            if ((bits & QCommon.CM_ANGLE3) != 0)
            {
                move.angles[2] = (short)ReadShort();
            }

            /* read movement */
            if ((bits & QCommon.CM_FORWARD) != 0)
            {
                move.forwardmove = (short)ReadShort();
            }

            if ((bits & QCommon.CM_SIDE) != 0)
            {
                move.sidemove = (short)ReadShort();
            }

            if ((bits & QCommon.CM_UP) != 0)
            {
                move.upmove = (short)ReadShort();
            }

            /* read buttons */
            if ((bits & QCommon.CM_BUTTONS) != 0)
            {
                move.buttons = (byte)ReadByte();
            }

            if ((bits & QCommon.CM_IMPULSE) != 0)
            {
                move.impulse = (byte)ReadByte();
            }

            /* read time to run command */
            move.msec = (byte)ReadByte();

            /* read the light level */
            move.lightlevel = (byte)ReadByte();
        }


        public byte[] ReadData(int len)
        {
            var r = new byte[len];
            for (int i = 0; i < len; i++) {
                r[i] = (byte)ReadByte();
            }
            return r;
        }

    }
}