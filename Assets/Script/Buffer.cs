using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

//데이터를 읽고 쓰기 위한 버퍼 클래스
//메모리 스트림을 사용하여 데이터를 저장하고
//데이터가 쓰여지고 읽혀질 때마다 패킷 정보를 추적
public class Buffer
{
    //버퍼에 저장된 데이터의 위치와 크기 추적
    struct Packet
    {
        public int pos; //데이터의 시작 위치
        public int size; //데이터의 크기
    };

    MemoryStream stream; //실제 데이터 저장
    List<Packet> list; 
    int pos = 0;

    Object o = new Object();

    public Buffer()
    {
        stream = new MemoryStream(); //새로운 메모리 스트림 생성
        list = new List<Packet>(); //데이터 패킷을 추적하는 리스트
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
        //여러 스레드가 동시에 메서도 호출하는 경우를 대비하여 lock
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
