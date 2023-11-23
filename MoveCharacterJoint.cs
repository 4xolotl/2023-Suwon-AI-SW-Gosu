using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System;
using UnityEngine.Video;
using System.Globalization;
using System.Linq;
using TMPro;

public class MoveCharacterJoint : MonoBehaviour
{
    public int numberOfJoints = 13; // 관절 개수
    public int jointMatch; // 채점 맞는 개수
    public MovementScorer scorer;
    private int frameNum = 0;
    public TextMeshProUGUI hoonsuText;

    //public VideoPlayer video; //비디오 플레이어
    int playCount = 0;        //비디오 재생 수
    float startTime = 4f;     //타이머 설정 시간
    bool startPlay;          
    public TextMeshProUGUI timertext; //타이머 표시 텍스트 오브젝트 

    Socket socket;
    public int textCount = 0; //행 개수

    private Vector3 virtualNeck;
    private Vector3 virtualHips;
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
        if (LoadingScene.sock != null) {
            socket = LoadingScene.sock; //로딩씬에서 연결한 소켓 이어받기
            Debug.Log("연결 완료");
            StartCoroutine(PoseUpdate());   
        }
        playCount = 1;

        scorer = new MovementScorer(ScenesMove.nextStageNum, numberOfJoints);
        
            
    }

    //key: parent bone, value: StoreJointData 클래스
    private void AddModelJointData(HumanBodyBones parent, HumanBodyBones child, Vector3 trackParent, Vector3 trackChild)
    {   
        limbsJointData.Add(parent,
            new StoreJointData(anim.GetBoneTransform(parent), anim.GetBoneTransform(child), trackParent, trackChild));
    }

    //private void Update()
    //{
    //    if (!video.isPlaying && playCount < 4)
    //    {
    //        frameNum = 0;
    //        timertext.gameObject.SetActive(true);

    //        if (startTime > 1f)
    //        {
    //            startTime -= Time.deltaTime;
    //            int seconds = Mathf.FloorToInt(startTime % 60);
    //            timertext.text = seconds.ToString(); // 몇초인지 텍스트 오브젝트에 표시
    //        }
    //        else
    //        {
    //            video.Play();
    //            Debug.Log("비디오 재생");
    //            playCount++;
    //            startTime = 4f;
    //        }
    //    }
    //}

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

    private IEnumerator PoseUpdate() {
        
        while (poseUpdateCheck) {
            realJoint = new Vector3[13]; // 텍스트 파일에서 읽어온 x,y,z로 캐릭터 실제 position값 저장
            string[] textLine; 
            string[] splitXYZ;
            Vector3[] baseLineDataArray = new Vector3[13];

            if (LoadingScene.socketConnect)
            {
                byte[] buffer = new byte[4];
                int byteCount = socket.Receive(buffer); //수신된 바이트 수를 정수로 전환한다. -> 3
                //Debug.Log("받을 데이터 바이트 수: " + byteCount);
                Array.Reverse(buffer);

               
                int dataByteCount = BitConverter.ToInt32(buffer, 0);
                //Debug.Log("dataByteCount: " + dataByteCount);
                byte[] receivedBuffer = new byte[dataByteCount]; // 받으려고 하는 데이터의 수만큼 byte 배열 선언
                int dataCount = socket.Receive(receivedBuffer);
            

                string msg = Encoding.UTF8.GetString(receivedBuffer, 0, dataCount); // byte[] to string

                textLine = msg.Split("\n");

                //Debug.Log("textLine.Length: " + textLine.Length);

                for (int i = 0; i < textLine.Length - 1; i++)
                {
                    string line = textLine[i];

                    splitXYZ = line.Split(' ');
                    if (splitXYZ.All(x => x is string))
                    {
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

            CharactorMove(); // 캐릭터 움직이는 코드

            //if (!video.isPlaying && playCount < 4)
            //{
            //    video.Play();
            //    playCount++;

            //    frameNum = 0;
            //    timertext.gameObject.SetActive(true);


            //    if (startTime > 1f)
            //    {
            //        startTime -= Time.deltaTime;
            //        int seconds = Mathf.FloorToInt(startTime % 60);
            //        timertext.text = seconds.ToString(); // 몇초인지 텍스트 오브젝트에 표시
            //    }
            //    else
            //    {
            //        video.Play();
            //        Debug.Log("비디오 재생");
            //        playCount++;
            //        startTime = 4f;
            //    }
            //}

            //if (video.isPlaying)
            //{
            //    timertext.gameObject.SetActive(false);
            //    baseLineDataArray = MakeBaseLineDataArray(frameNum);
            //    jointMatch = scorer.ScoreMovement(baseLineDataArray, realJoint);
            //    frameNum++;
            //    Debug.Log(scorer.hoonsuMessage);
            //}

           
            //hoonsuText.text = scorer.hoonsuMessage;
          

            yield return new WaitForSeconds(0.046f); // 0.044f로 했을 때 안튕김

            virtualNeck = Vector3.zero;
            virtualUpperChest = Vector3.zero;
            virtualHips = Vector3.zero;
            limbsJointData.Clear();
            
        }
    }

    public void CharactorMove() {
        
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
            Quaternion changeRot = Quaternion.FromToRotation(i.Value.initialDir, Vector3.Slerp(i.Value.initialDir, i.Value.CurrentDirection, 0.2f));
            i.Value.parent.rotation = changeRot * i.Value.initialRotation;
        }

        rightHip.RotateTorso(rightHip, 0.5f);
        leftHip.RotateTorso(leftHip, 0.5f);
        //neckTwist.RotateTorso(neckTwist, 0.05f);
        //chestTwist.RotateTorso(chestTwist, 0.2f);
        rightShoulder.RotateTorso(rightShoulder, 0.2f); //Hips 관절 회전으로 RightUpperArm과 LeftUpperArm의 회전이 더 들어감
        leftShoulder.RotateTorso(leftShoulder, 0.2f);

        anim.GetBoneTransform(HumanBodyBones.Hips).position = virtualHips;
    }
}

class StoreJointData //avatar joint관련 데이터 저장하는 클래스
{
    public Transform parent, child; // 캐릭터 부모 조인트, 자식 조인트 transform 저장
    public Vector3 tParent, tChild; //포즈 추정 모델로 측정한 부모 조인트 x,y,z 위치값, 자식 조인트 x,y,z 위치값
    public Vector3 initialDir;         // 캐릭터 조인트 회전하기 전 자식, 부모조인트의 상대 위치 = 방향
    public Quaternion initialRotation; //캐릭터 조인트 회전하기 전 조인트 회전값

    public Quaternion targetRotation;
    float speed = 10f;

    //일정 비율만큼 몸통 회전시키는 함수
    public void RotateTorso(StoreJointData avatarJoint, float amount)
    {
        this.targetRotation = Quaternion.FromToRotation(avatarJoint.initialDir, Vector3.Slerp(avatarJoint.initialDir, avatarJoint.CurrentDirection, amount));
        this.targetRotation *= avatarJoint.initialRotation;
        this.Make(this.targetRotation, speed);
    }

    //원래 회전 각도에서 target 각도까지 회전시키는 함수
    public void Make(Quaternion newTarget, float speed)
    {
        targetRotation = newTarget;
        parent.rotation = Quaternion.Lerp(parent.rotation, targetRotation, Time.deltaTime * speed);
    }

    //모델로 측정한 조인트의 방향계산
    public Vector3 CurrentDirection => (tChild - tParent).normalized;

    public StoreJointData(Transform mParent, Transform mChild, Vector3 tParent, Vector3 tChild)
    {
        initialDir = (mChild.position - mParent.position).normalized;
        initialRotation = mParent.rotation;
        this.parent = mParent;
        this.child = mChild;
        this.tParent = tParent;
        this.tChild = tChild;
    }

}
