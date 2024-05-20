using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenesMove : MonoBehaviour
{  
    static public int nextStageNum = -1;
    public void GameSceneCtrl() {

        if (LoadingScene.sock != null) { 
            LoadingScene.sock.Close();
            Debug.Log("소켓 연결 끊음");
            new WaitForSeconds(1f);
        }
        nextStageNum++;

        if (nextStageNum == 4)
        {
            SceneManager.LoadScene("Intro");
        }
        else {
            SceneManager.LoadScene("Loading");
        }
    }

}
