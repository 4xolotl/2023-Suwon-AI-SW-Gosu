using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Net.Sockets;
using System.Net;
using System.Text;

public class LoadingScene : MonoBehaviour
{
    public Slider progressbar;
    public TextMeshProUGUI loadtext;

    public static Socket sock;
    string serverIP = "172.20.10.13";
    //string serverIP = "192.168.123.110";
    int port = 9615;
    public static bool socketConnect = false;
    string sendStart = "start";

    public void StartNetwork()
    {
        try
        {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse(serverIP), port);
            sock.Connect(serverEP);
            socketConnect = true;
            Debug.Log("Connect Success");
            
            byte[] buff =  Encoding.UTF8.GetBytes(sendStart);

            sock.Send(buff);

        }
        catch (SocketException e)
        {
            Debug.Log("오류: " + e);
        }

    }
    private void Start()
    {     
        socketConnect = false;
        StartCoroutine(LoadScene());

    }
    IEnumerator LoadScene()
    {
        AsyncOperation operation;
        yield return null;
        Debug.Log("nextStageNum: " +  ScenesMove.nextStageNum);

        // StageNum에 따라서 로드하는 씬 구분
        if(ScenesMove.nextStageNum == 0)
        {
            operation = SceneManager.LoadSceneAsync("MyRoom");
        }
        else if (ScenesMove.nextStageNum == 1)
        {
            operation = SceneManager.LoadSceneAsync("Tutorials");
        }
        else if (ScenesMove.nextStageNum == 2)
        {
            operation = SceneManager.LoadSceneAsync("Tutorials 2");
        }
        else {
            operation = SceneManager.LoadSceneAsync("FinalTest");
        }

        operation.allowSceneActivation = false; // 씬 로딩 90퍼에서 멈춰두기

        while (!operation.isDone) { 
            yield return null;

            if (progressbar.value < 1f)
            {
                progressbar.value = Mathf.MoveTowards(progressbar.value, 1f, Time.deltaTime);
            }

            if (progressbar.value >= 1f && operation.progress >= 0.9f) {
                StartNetwork(); // 씬 넘어가기 전에 소켓 통신 연결
                if (socketConnect) {
                    operation.allowSceneActivation = true; //씬 로딩 활성화
                }
                //operation.allowSceneActivation = true;
            }
        }
        
    }
   
}
