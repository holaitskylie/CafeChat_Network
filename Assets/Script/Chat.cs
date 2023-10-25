using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Chat : MonoBehaviour
{
    Network network;

    public InputField id;
    public InputField chat;

    List<string> list;
    public Text[] text;
    public Image backUI;

    public GameObject[] player;
    public GameObject[] NPC;
    

    void Start()
    {
        network = GetComponent<Network>();
        list = new List<string>();
    }

    public void BeginServer()
    {
        // 서버 시작	
        network.ServerStart(10000, 10);

        //서버가 시작되면 Player0 활성화
        player[0].SetActive(true);

        network.name = id.text;
        
    }

    public void BeginClient()
    {
        // 클라이언트 시작
        network.ClientStart("127.0.0.1", 10000);
        network.name = id.text;
        
    }

    void Update()
    {
        if (network != null && network.IsConnect())
        {
            byte[] bytes = new byte[1024];
            int length = network.Receive(ref bytes, bytes.Length);
            if (length > 0)
            {
                string str = System.Text.Encoding.UTF8.GetString(bytes);
                
		        // 채팅 데이터 받았을 때
                AddTalk(str);
                SetAnimation(false);
            }

            UpdateUI();
        }

        //Enter 키를 치면 메세지 전송
        if (Input.GetKeyDown(KeyCode.Return))
        {
            SendTalk();
        }
    }

    void SetAnimation(bool bSend)
    {
        int iPlayer;

        if (bSend)
            iPlayer = network.IsServer() ? 0 : 1;
        else
            iPlayer = network.IsServer() ? 1 : 0;


        // 애니메이션 갱신
        player[iPlayer].GetComponent<Animator>().SetTrigger("dance");
    }

    void AddTalk(string str)
    {
        while (list.Count >= 5)
        {
            list.RemoveAt(0);
        }

        list.Add(str);
        UpdateTalk();
    }

    public void SendTalk()
    {
        string str = network.name + ": " + chat.text;
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);

        // 채팅 보낼 때        
        network.Send(bytes, bytes.Length);
             

        // 채팅 보낼 때
        AddTalk(str);       
       

        //애니메이션 재생
        SetAnimation(true);
    }

    void UpdateTalk()
    {
        for (int i = 0; i < list.Count; i++)
        {
            text[i].text = list[i];
        }
    }

    void UpdateUI()
    {
        int index = 0;

        if (!backUI.IsActive())
        {
            backUI.gameObject.SetActive(true);
            player[0].SetActive(true);
            player[1].SetActive(true);

            for(int i = 0; i < NPC.Length; i++)
            {
                NPC[i].SetActive(true);
            }
           
        }
    }
}
