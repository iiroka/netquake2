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

    }
}