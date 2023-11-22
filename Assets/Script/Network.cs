using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using UnityEngine;


public class Network : MonoBehaviour
{
    //서버와 클라이언트의 연결 상태를 나타내는 플래그 변수
    bool bServer = false;
    bool bConnect = false;

    //TCP 소켓을 나타내는 변수
    Socket socketListen = null;
    Socket socket = null;

    Thread thread = null; //네트워크 통신을 처리하는 스레드
    bool bThreadBegin = false;

    //송수신 데이터를 저장하는 버퍼 객체 
    Buffer bufferSend;
    Buffer bufferReceive;

    public string name;

    void Start()
    {        
        //Buffer 인스턴스 생성, 데이터를 송수신하는 데 필요한 버퍼를 초기화한다        
        bufferSend = new Buffer();
        bufferReceive = new Buffer();
    }

    public void ServerStart(int port, int backlog)
    {
	    // 서버 시작
        //TCP 소켓 생성(listening socket)
        //Socket 클래스를 사용하여 TCP 소켓 생성
        socketListen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
        //엔드포인트 지정
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);

        //Bind 및 Listen
        //Bind() : IP주소와 socket을 묶는다
        socketListen.Bind(ep);
        //클라이언트의 연결을 수락하기 위해 소켓을 대기 상태로 설정
        socketListen.Listen(backlog);

        bServer = true;
        Debug.Log("Server Start");

        StartThread();

    }

    public bool IsServer() //Network 객체가 서버인지 여부를 확인한다
    {
        //Chat 클래스의 SetAnimation()에서 반환값에 따라
        //서버인지 클라이언트인지를 판단한다
        return bServer;
    }

    bool StartThread()
    {
        //ThreadProc메서드를 가리키는 ThreadStart 객체 생성
        ThreadStart threadDelegate = new ThreadStart(ThreadProc);

        //Thread 클래스를 사용하여 새로운 스레드 생성
        //생성된 스레드는 threadDelegate가 가리키는 메서드 실행
        thread = new Thread(threadDelegate);

        //스레드 시작
        //ThreadProc()가 새로운 스레드에서 실행
        thread.Start();

        //스레드 시작 플래그 설정
        bThreadBegin = true;

        return true;
    }

    public void ThreadProc()
    {
        while (bThreadBegin)
        {
            //클라이언트 연결 수락
            //서버 소켓이 클라이언트 연결 요청을 대기
            //요청이 들어오면 클라이언트와의 연결 수락
            AcceptClient();

            if (socket != null && bConnect == true)
            {
                // 네트워크 업데이트
                SendUpdate(); //송신 버퍼의 데이터를 클라이언트에게 전송
                ReceiveUpdate(); //수신 버퍼에서 클라이언트로부터 데이터를 받아옴
            }

            //스레드를 10밀리초 동안 일시 정지
            //스레드가 지나치게 많은 리소스를 소비하지 않고 대기 상태에 머무를 수 있게 함
            Thread.Sleep(10);
        }
    }

    //클라이언트를 시작하기 위한 메서드
    //주어진 서버 주소와 포트에 연결하여 클라이언트 활성화
    public void ClientStart(string address, int port)
    {
    	// 클라이언트 시작
        // socket 생성
        socket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);

        //클라이언트 소켓을 주어진 서버에 연결 시도
        //address : 연결하는 서버 주소, port : 어플리케이션 지정
        socket.Connect(address, port);

        bConnect = true;
        Debug.Log("Client Start");

        StartThread();
    }

    //클라이언트의 연결을 수락
    void AcceptClient()
    {
        //소켓이 유효하고 데이터가 도착했는지 확인
        if (socketListen != null && socketListen.Poll(0, SelectMode.SelectRead))
        {
            //Accept() 메서드를 사용하여 클라이언트의 연결 수락
            socket = socketListen.Accept();
            bConnect = true;

            Debug.Log("Client Connect");
        }
    }

    //클라이언트가 서버에 연결되어 있는지 확인
    public bool IsConnect()
    {
        //Chat 클래스의 Update()에서 채팅 데이터 수신 및 처리
        return bConnect;
    }

    //채팅 메시지를 송신
    public int Send(byte[] bytes, int length)
    {
        //Buffer 클래스의 Write() 반환값을 그대로 반환
        //실제 송신은 SendUpdate()에서 수행되며
        //Send()에서는 송신할 데이터를 송신 버퍼에 기록하는 역할
        return bufferSend.Write(bytes, length);
    }

    //채팅 메시지를 수신
    public int Receive(ref byte[] bytes, int length)
    {
        return bufferReceive.Read(ref bytes, length);
    }

    //송신 버퍼에서 데이터를 읽어와 소켓을 통해 클라이언트가 서버로 전송
    void SendUpdate()
    {
        //소켓이 쓰기 가능한 상태인지 확인
        //쓰기 가능 상태 = 데이터 전송 가능 상태
        if (socket.Poll(0, SelectMode.SelectWrite))
        {
            //송신할 데이터를 담을 바이트 배열
            byte[] bytes = new byte[1024];

            //송신 버퍼에서 최대 1024 바이트만큼의 데이터를 읽어온다
            int length = bufferSend.Read(ref bytes, bytes.Length);

            //읽어 온 데이터가 있다면 계속 반복
            while (length > 0)
            {
                //클라이언트 소켓을 통해 읽어온 데이터를 서버에게 전송
                socket.Send(bytes, length, SocketFlags.None);
                length = bufferSend.Read(ref bytes, bytes.Length);
            }
        }
    }

    //서버에서 클라이언트로부터 데이터를 수신
    //데이터를 수신 버퍼(bufferReceive)에 저장
    void ReceiveUpdate()
    {
        //서버 소켓이 읽기 가능한 상태인지를 확인
        //읽기 가능 상태 = 클라이언트에서 데이터를 읽을 수 있는 상태
        while (socket.Poll(0, SelectMode.SelectRead))
        {
            //수신 데이터를 저장할 바이트 배열
            byte[] bytes = new byte[1024];

            //클라이언트에서 최대 1024바이트 만큼의 데이터 수신
            int length = socket.Receive(bytes, bytes.Length, SocketFlags.None);
            
            if (length > 0) //데이터를 수신했다
            {
                //수신한 데이터가 수신 버퍼에 저장
                bufferReceive.Write(bytes, length);
            }
        }
    }
}
