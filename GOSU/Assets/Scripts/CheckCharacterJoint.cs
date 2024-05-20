using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using TMPro;
using System.Linq;
using System.Net.Sockets;
using System;
using System.IO;
using UnityEngine.UI;

public class CheckCharacterJoint : MonoBehaviour
{
    Animator anim;


    public float time = 3f;
    public TextMeshProUGUI waitingTimeText;
    public TextMeshProUGUI comment;
    public Button nextButton;

    bool updateCheck = false;

    Socket socket;
    Vector3[] realJoint;
    static public Vector3[] storeJointData = new Vector3[13]; 
    Vector3[] previousData;

    public int frameCount = 100;
    public int count;

    private Vector3 virtualNeck; //목 가상 좌표
    private Vector3 virtualHips; //골반 사이 기준점 가상 좌표
    private Vector3 virtualUpperChest;
    private StoreJointData rightHip, leftHip, chestTwist, neckTwist, rightShoulder, leftShoulder; // 상체 몸통 움직이는데 사용
    private Dictionary<HumanBodyBones, StoreJointData> limbsJointData = new Dictionary<HumanBodyBones, StoreJointData>(); //팔다리 움직이기 위한 관절값 저장


    // Start is called before the first frame update
    void Start()
    {
        socket = null;
        anim = GetComponent<Animator>();
        if (LoadingScene.sock != null)
        {
            socket = LoadingScene.sock; //로딩씬에서 연결한 소켓 이어받기
            Debug.Log("연결 완료");
            StartCoroutine(JointUpdate());
        }

        //FileInfo fileInfo = new FileInfo(fullpth);

        //if (fileInfo.Exists)
        //{
        //    Debug.Log("파일 존재");
        //    sr = new StreamReader(fullpth);
        //    textValue = File.ReadAllLines(fullpth); //텍스트 파일의 모든 행 읽어들이기
        //    textCount = textValue.Length;

        //}
        //else
        //{
        //    Debug.Log("파일 경로에 파일이 없습니다. 경로가 잘못되었는지 확인하세요.");
        //}

        for (int i = 0; i < 13; i++) {
            storeJointData[i] = Vector3.zero;
        }
        count = frameCount;
    }

    private void AddModelJointData(HumanBodyBones parent, HumanBodyBones child, Vector3 trackParent, Vector3 trackChild)
    {
        limbsJointData.Add(parent,
            new StoreJointData(anim.GetBoneTransform(parent), anim.GetBoneTransform(child), trackParent, trackChild));
    }
    // Update is called once per frame
    void Update()
    {
        if (time >= 1f)
        {
            time -= Time.deltaTime;
            int seconds = Mathf.FloorToInt(time % 60);
            waitingTimeText.text = seconds.ToString();
        }
        else
        {
            waitingTimeText.gameObject.SetActive(false);
            time = 0f;
            updateCheck = true;
        }
    }

    private IEnumerator JointUpdate()
    {   
        while (true)
        {
            //realJoint = new Vector3[13];

            //if (textCount > 1) //textCount가 1일때 line변수에 Null값이 들어가게 됨
            //{
            //    for (int i = 0; i < 13; i++)
            //    {
            //        string line = sr.ReadLine(); //파일의 한줄씩 받아오기 \n까지


            //        jointXYZ = line.Split(' '); //3개의 값을 ' ' 을 기준으로 나눠 배열에 저장

            //        //string으로 저장되어 있는 값을 float형으로 변환 후 저장
            //        realJoint[i].x = float.Parse(jointXYZ[0]);
            //        realJoint[i].y = float.Parse(jointXYZ[1]);
            //        realJoint[i].z = float.Parse(jointXYZ[2]);

            //        textCount--;
            //    }
            //}

            //else
            //{
            //    sr.Close(); // streamReader 닫음
            //    Debug.Log("스트림 리더 닫음");
            //    UnityEditor.EditorApplication.isPlaying = false;
            //}

            //if (UnityEditor.EditorApplication.isPlaying == false)
            //{
            //    sr.Close();
            //}

            realJoint = new Vector3[13]; // 텍스트 파일에서 읽어온 x,y,z로 캐릭터 실제 position값 저장
            string[] textLine;
            string[] splitXYZ;
            Vector3[] baseLineDataArray = new Vector3[13];

            try
            {
                if (LoadingScene.socketConnect)
                {
                    byte[] buffer = new byte[4];
                    int byteCount = socket.Receive(buffer); //수신된 바이트 수를 정수로 전환한다. -> 3
                    Array.Reverse(buffer);


                    int dataByteCount = BitConverter.ToInt32(buffer, 0);
                    byte[] receivedBuffer = new byte[dataByteCount]; // 받으려고 하는 데이터의 수만큼 byte 배열 선언
                    int dataCount = socket.Receive(receivedBuffer);


                    string msg = Encoding.UTF8.GetString(receivedBuffer, 0, dataCount); // byte[] to string

                    textLine = msg.Split("\n");

                    for (int i = 0; i < textLine.Length - 1; i++)
                    {
                        string line = textLine[i];

                        splitXYZ = line.Split(' ');
                        if (splitXYZ.All(x => x is string))
                        {
                            //Debug.Log("realJoint: " + realJoint[i].x);
                            realJoint[i].x = float.Parse(splitXYZ[0]);
                            realJoint[i].y = float.Parse(splitXYZ[1]);
                            realJoint[i].z = float.Parse(splitXYZ[2]);
                        }
                        else
                        {
                            realJoint[i].x = 0f;
                            realJoint[i].y = 0f;
                            realJoint[i].z = 0f;
                        }
                    }
                }
            }
            catch(Exception e) {
                Debug.Log("오류: "+ e);
            }
            
            if (updateCheck) {
                if (count > 0)
                {
                    comment.text = "사용자의 관절 위치 감지 중 ...";
                    for (int i = 0; i < 13; i++)
                    {
                        storeJointData[i] += (realJoint[i] / frameCount);
                    }
                    count--;
                }
                else if (count == 0)
                {
                    nextButton.gameObject.SetActive(true);
                    comment.text = "사용자 관절 위치 감지 완료!";
                    count = -1;
                }
            }
            
            virtualHips = (realJoint[7] + realJoint[8]) / 2.0f;// 가상의 힙 관절의 위치 구하기
            virtualHips.y += 0.075f;
            virtualHips.y += 0.95f;
            virtualHips.x -= 0.5f;
            virtualHips.z -= 0.5f;

            virtualNeck = (realJoint[1] + realJoint[2]) / 2.0f; // 가상의 목 관절의 위치 구하기
            virtualNeck.y += 0.05f;

            virtualUpperChest = (realJoint[1] + realJoint[2]) / 2.0f; //가상의 UpperChest 관절 위치 구하기
            virtualUpperChest.y -= 0.1f;

            for (int i = 0; i < 13; i++)
            {
                realJoint[i].y *= -1f; // 전달 받은 조인트 값의 y좌표가 땅과 반대로 되어있음
                realJoint[i] += virtualHips; // pose_world_landmarks는 엉덩이 중간 포인트를 기준으로 상대좌표이므로 Hips의 위치를 더해 절대 좌표를 구해준다.
                                             //Debug.Log("realJoint: " + realJoint[i]);
            }


            virtualNeck += virtualHips;
            virtualUpperChest += virtualHips;

            //상체 몸통 움직이기 위한 상체 관절 StoreJointData 클래스에 저장
            rightHip = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.Hips), anim.GetBoneTransform(HumanBodyBones.RightUpperLeg), virtualHips, realJoint[8]);
            leftHip = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.Hips), anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg), virtualHips, realJoint[7]);

            neckTwist = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.Neck), anim.GetBoneTransform(HumanBodyBones.Head), virtualNeck, realJoint[0]);

            rightShoulder = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.UpperChest), anim.GetBoneTransform(HumanBodyBones.RightUpperArm), virtualUpperChest, realJoint[2]);
            leftShoulder = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.UpperChest), anim.GetBoneTransform(HumanBodyBones.LeftUpperArm), virtualUpperChest, realJoint[1]);


            //limbsJointData 배열에 팔다리에 관한 데이터(캐릭터 팔다리 방향, 회전 각도 등) 저장
            AddModelJointData(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, realJoint[2], realJoint[4]);
            AddModelJointData(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, realJoint[4], realJoint[6]);

            AddModelJointData(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, realJoint[1], realJoint[3]);
            AddModelJointData(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, realJoint[3], realJoint[5]);

            AddModelJointData(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, realJoint[8], realJoint[10]);
            AddModelJointData(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, realJoint[10], realJoint[12]);

            AddModelJointData(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, realJoint[7], realJoint[9]);
            AddModelJointData(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, realJoint[9], realJoint[11]);

            //배열에 저장된 팔다리에 관한 데이터를 이용해 조인트 회전각도 바꾸는 코드
            foreach (var i in limbsJointData)
            {
                Quaternion changeRot = Quaternion.FromToRotation(i.Value.initialDir, Vector3.Slerp(i.Value.initialDir, i.Value.CurrentDirection, 0.5f));
                i.Value.parent.rotation = changeRot * i.Value.initialRotation;
            }

            rightHip.RotateTorso(rightHip, 0.5f); 
            leftHip.RotateTorso(leftHip, 0.5f);
            //neckTwist.RotateTorso(neckTwist, 0.05f);
            //chestTwist.RotateTorso(chestTwist, 0.2f);
            rightShoulder.RotateTorso(rightShoulder, 0.5f); //Hips 관절 회전으로 RightUpperArm과 LeftUpperArm의 회전이 더 들어감
            leftShoulder.RotateTorso(leftShoulder, 0.5f);

            anim.GetBoneTransform(HumanBodyBones.Hips).position = virtualHips;

            yield return new WaitForSeconds(0.048f);
            
            virtualNeck = Vector3.zero;
            virtualUpperChest = Vector3.zero;
            virtualHips = Vector3.zero;
            limbsJointData.Clear();
        }
    }

}
    
