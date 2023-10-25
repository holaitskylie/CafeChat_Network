using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class Buffer
{
    struct Packet
    {
        public int pos;
        public int size;
    };

    MemoryStream stream;
    List<Packet> list;
    int pos = 0;

    Object o = new Object();

    public Buffer()
    {
        stream = new MemoryStream();
        list = new List<Packet>();
    }

    public int Write(byte[] bytes, int length)
    {
        Packet packet = new Packet();

        packet.pos = pos;
        packet.size = length;

        lock (o)
        {
            list.Add(packet);

            stream.Position = pos;
            stream.Write(bytes, 0, length);
            stream.Flush();
            pos += length;
        }

        return length;
    }

    public int Read(ref byte[] bytes, int length)
    {
        if (list.Count <= 0)
            return -1;

        int ret = 0;
        lock (o)
        {
            Packet packet = list[0];

            int dataSize = Math.Min(length, packet.size);
            stream.Position = packet.pos;
            ret = stream.Read(bytes, 0, dataSize);

            if (ret > 0)
                list.RemoveAt(0);

            if (list.Count == 0)
            {
                byte[] b = stream.GetBuffer();
                Array.Clear(b, 0, b.Length);

                stream.Position = 0;
                stream.SetLength(0);

                pos = 0;
            }
        }

        return ret;
    }
}
