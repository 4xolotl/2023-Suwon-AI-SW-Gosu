using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class FinalTest : MonoBehaviour
{
    public int numberOfJoints = 13; // 관절 개수
    public int jointMatch; // 채점 맞는 개수
    public int totalScore; // 전체 점수(맞은 관절 개수)

    public MovementScorer scorer;      //채점 기능을 위한 클래스
    private int frameNum = 0;          //영상 프레임 개수

    public VideoPlayer video; //비디오 플레이어
    int playCount = 0;        //비디오 재생 수
    float startTime = 4f;     //타이머 설정 시간        
    public TextMeshProUGUI timertext; //타이머 표시 텍스트 오브젝트 
    public TextMeshProUGUI resultText; // 합/불 표시

    Socket socket;

    private Vector3 virtualNeck; //목 가상 좌표
    private Vector3 virtualHips; //골반 사이 기준점 가상 좌표
    private Vector3 virtualUpperChest;
    Vector3[] realJoint;

    Animator anim;
    bool poseUpdateCheck = true;

    private StoreJointData rightHip, leftHip, chestTwist, neckTwist, rightShoulder, leftShoulder; // 상체 몸통 움직이는데 사용
    private Dictionary<HumanBodyBones, StoreJointData> limbsJointData = new Dictionary<HumanBodyBones, StoreJointData>(); //팔다리 움직이기 위한 관절값 저장


    //Start is called before the first frame update
    void Start()
    {
        socket = null;
        anim = GetComponent<Animator>();
        if (LoadingScene.sock != null)
        {
            socket = LoadingScene.sock; //로딩씬에서 연결한 소켓 이어받기
            Debug.Log("연결 완료");
            StartCoroutine(PoseUpdate());
        }

        scorer = new MovementScorer(ScenesMove.nextStageNum, numberOfJoints);


    }

    //key: parent bone, value: StoreJointData 클래스
    private void AddModelJointData(HumanBodyBones parent, HumanBodyBones child, Vector3 trackParent, Vector3 trackChild)
    {
        limbsJointData.Add(parent,
            new StoreJointData(anim.GetBoneTransform(parent), anim.GetBoneTransform(child), trackParent, trackChild));
    }

    private void Update()
    {
        if (!video.isPlaying && playCount < 2)
        {
            frameNum = 0;
            timertext.gameObject.SetActive(true);

            if (startTime > 1f)
            {
                startTime -= Time.deltaTime;
                int seconds = Mathf.FloorToInt(startTime % 60);
                timertext.text = seconds.ToString(); // 몇초인지 텍스트 오브젝트에 표시
            }
            else
            {
                playCount++;
                video.Play();
                Debug.Log("비디오 재생");
                startTime = 4f;
            }
        }
        else if (!video.isPlaying && playCount == 2)
        {
            if (totalScore >= 200)
            {
                resultText.text = "합격";
            }
            else
            {
                resultText.text = "불합격";
            }
        }
    }

    //답안지 배열 프레임 별로 나눠서 baseLineDataArray에 저장
    private Vector3[] MakeBaseLineDataArray(int num)
    {
        Vector3[] baseLineDataArray = new Vector3[13];

        for (int i = 0; i < 13; i++)
        {
            baseLineDataArray[i] = scorer.mediaMarking[13 * num + i];
        }

        return baseLineDataArray;
    }

    private IEnumerator PoseUpdate()
    {

        while (poseUpdateCheck)
        {
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
            catch (Exception e)
            {
                Debug.Log("오류: " + e);
            }


            if (video.isPlaying)
            {
                if (playCount != 1) //영상 첫번째 재생은 그냥 보고 두번째 재생부터 채점 기능 들어감
                {
                    timertext.gameObject.SetActive(false);
                    baseLineDataArray = MakeBaseLineDataArray(frameNum); //영상 프레임 별 관절 좌표 13개를 저장한 배열 만들기
                    jointMatch = scorer.ScoreMovement(baseLineDataArray, realJoint, 0.48 / Vector3.Distance(CheckCharacterJoint.storeJointData[1], CheckCharacterJoint.storeJointData[7])); //답안지 배열과 실시간 통신으로 얻은 배열 채점 함수에 전달

                    if (jointMatch >= 6)
                    {
                        totalScore++;
                    }
                    frameNum++; //답안지 배열 업데이트를 위한 프레임 수 증가
                }
                else
                {
                    timertext.gameObject.SetActive(false);
                }
            }

            virtualHips = (realJoint[7] + realJoint[8]) / 2.0f;// 가상의 힙 관절의 위치 구하기
            virtualHips.y += 0.075f;
            virtualHips.y += 0.95f;
            virtualHips.x -= 0.5f;
            virtualHips.z += 0.5f;

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
            rightShoulder.RotateTorso(rightShoulder, 0.4f); //Hips 관절 회전으로 RightUpperArm과 LeftUpperArm의 회전이 더 들어감
            leftShoulder.RotateTorso(leftShoulder, 0.4f);

            anim.GetBoneTransform(HumanBodyBones.Hips).position = virtualHips;

            yield return new WaitForSeconds(0.048f); // 파이썬 서버와의 통신 속도를 맞추기 위한 딜레이

            //좌표 초기화
            virtualNeck = Vector3.zero;
            virtualUpperChest = Vector3.zero;
            virtualHips = Vector3.zero;
            limbsJointData.Clear();
        }
    }

}


