using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class MoveCharacterJoint : MonoBehaviour
{
    //private string fullpth = "Assets/Avartar/JointTextFile2/jabCross.txt";
    private string fullpth = "Assets/Avartar/JointTextFile2/tesst.txt";
    public int textCount = 0; //행 개수

    StreamReader sr; //스트림 리더
    string[] textValue; //텍스트 파일 전체 행 저장 배열
    string[] jointXYZ;  //텍스트 파일에서 조인트 x,y,z값 저장하는 배열
    Vector3[] realJoint = new Vector3[13]; // 텍스트 파일에서 읽어온 x,y,z로 캐릭터 실제 position값 저장

    Animator anim;

    private Vector3 virtualNeck;
    private Vector3 virtualHips;
    private Vector3 virtualUpperChest;

    private StoreJointData rightHip, leftHip, chestTwist, neckTwist, rightShoulder, leftShoulder;
    //parentModelJoint는 
    private Dictionary<HumanBodyBones, StoreJointData> limbsJointData = new Dictionary<HumanBodyBones, StoreJointData>();

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();

        FileInfo fileInfo = new FileInfo(fullpth);

        if (fileInfo.Exists)
        {
            Debug.Log("파일 존재");
            sr = new StreamReader(fullpth);
            textValue = File.ReadAllLines(fullpth); //텍스트 파일의 모든 행 읽어들이기
            textCount = textValue.Length;

        }
        else
        {
            Debug.Log("파일 경로에 파일이 없습니다. 경로가 잘못되었는지 확인하세요.");
        }
        //Debug.Log(anim.GetBoneTransform(HumanBodyBones.Hips).position);
    }

    //key: parent bone, value: StoreJointData 클래스
    private void AddModelJointData(HumanBodyBones parent, HumanBodyBones child, Vector3 trackParent, Vector3 trackChild) {
        limbsJointData.Add(parent,
            new StoreJointData(anim.GetBoneTransform(parent), anim.GetBoneTransform(child), trackParent, trackChild));
    }

    // Update is called once per frame
    void Update()
    {
        if (textCount > 1) //textCount가 1일때 line변수에 Null값이 들어가게 됨
        {
            for (int i = 0; i < 13; i++)
            {
                string line = sr.ReadLine(); //파일의 한줄씩 받아오기 \n까지
                //Debug.Log(line);

                jointXYZ = line.Split(' '); //3개의 값을 ' ' 을 기준으로 나눠 배열에 저장

                //string으로 저장되어 있는 값을 float형으로 변환 후 저장
                realJoint[i].x = float.Parse(jointXYZ[0]); 
                realJoint[i].y = float.Parse(jointXYZ[1]);
                realJoint[i].z = float.Parse(jointXYZ[2]);

                textCount--;
            }
        }
        else
        {
            sr.Close(); // streamReader 닫음
            UnityEditor.EditorApplication.isPlaying = false;
        }

        //가상의 관절 데이터 만들기
        virtualNeck = (realJoint[1] + realJoint[2]) / 2.0f; // 가상의 목 관절의 위치 구하기
        virtualNeck.y += 0.05f;
        virtualHips = (realJoint[7] + realJoint[8]) / 2.0f;  // 가상의 힙 관절의 위치 구하기
        virtualHips.y += 0.075f;
        virtualUpperChest = (realJoint[1] + realJoint[2]) / 2.0f; //가상의 UpperChest 관절 위치 구하기
        virtualUpperChest.y -= 0.1f;
        
        //상체 몸통 움직이기 위한 상체 관절 StoreJointData 클래스에 저장
        rightHip = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.Hips), anim.GetBoneTransform(HumanBodyBones.RightUpperLeg), virtualHips, realJoint[8]);
        leftHip = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.Hips), anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg), virtualHips, realJoint[7]);

        neckTwist = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.Neck), anim.GetBoneTransform(HumanBodyBones.Head), virtualNeck, realJoint[0]);
        //chestTwist = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.UpperChest), anim.GetBoneTransform(HumanBodyBones.Neck), virtualUpperChest, virtualNeck);

        rightShoulder = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.UpperChest), anim.GetBoneTransform(HumanBodyBones.RightUpperArm), virtualUpperChest, realJoint[2]);
        leftShoulder = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.UpperChest), anim.GetBoneTransform(HumanBodyBones.LeftUpperArm), virtualUpperChest, realJoint[1]);

        limbsJointData.Clear(); //배열 요소 지우기
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
            Quaternion changeRot = Quaternion.FromToRotation(i.Value.initialDir, Vector3.Slerp(i.Value.initialDir, i.Value.CurrentDirection, 0.3f));
            i.Value.parent.rotation = changeRot * i.Value.initialRotation;
        }

        rightHip.RotateTorso(rightHip, 0.5f);
        leftHip.RotateTorso(leftHip, 0.5f);
        neckTwist.RotateTorso(neckTwist, 0.5f);
        //chestTwist.RotateTorso(chestTwist, 0.2f);
        rightShoulder.RotateTorso(rightShoulder, 0.3f); //Hips 관절 회전으로 RightUpperArm과 LeftUpperArm의 회전이 더 들어감
        leftShoulder.RotateTorso(leftShoulder, 0.3f);
    }

    private void LateUpdate()
    {   
        anim.GetBoneTransform(HumanBodyBones.Hips).position = virtualHips;
        //anim.GetBoneTransform(HumanBodyBones.RightUpperLeg).position = realJoint[2];
        //anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position = realJoint[5];
        //anim.GetBoneTransform(HumanBodyBones.Head).position = realJoint[6];
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

    public void RotateTorso(StoreJointData avatarJoint, float amount) {
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

public class MovementScorer
{
    private Vector3[] mediaMarking; // [조인트] 저장하는 1차원 배열

    public MovementScorer(int numberOfLevels, int numberOfJoints)
    {
        for (int i = 0; i < numberOfLevels; i++)
        {
            LoadMovementData(i + 1, numberOfJoints);
        }
    }

    // 레벨별 데이터 불러오는 메소드
    private void LoadMovementData(int level, int numberOfJoints)
    {
        string filePath = $"Assets/Avartar/JointTextFile2/Level_{level}.txt"; //Level별로 저장된 txt 파일 불러오기
#define numOfJoints 

        if (File.Exists(filePath)) //파일 유효성 검사
        {
            string[] lines = File.ReadAllLines(filePath);
            mediaMarking = new Vector3[lines.Length]; // media Marking이 답지입니다~

            for (int i = 0; i < lines.Length; i++) // i == 라인 순번
            {
                string[] jointData = lines[i].Split(' '); // x, y, z 자르기 (형변환은 나중에)

                for (int j = 0; j < numberOfJoints; j++) // j == 조인트 인덱스, 13개를 순차적으로 처리합니다
                {
                    int index = i * numberOfJoints + j; // index == (j번째 조인트)/(n번째 프레임 관절세트)
                    // x, y, z 파싱 & 형변환해서 벡터로 저장
                    mediaMarking[index].x = float.Parse(jointData[0]);
                    mediaMarking[index].y = float.Parse(jointData[1]);
                    mediaMarking[index].z = float.Parse(jointData[2]);
                }
            }
        }
        else
        {
            Debug.LogError($"File not found: {filePath}"); // 예외 핸들링
        }
    }

    // 채점하는 메소드
    public int ScoreMovement(Vector3[] baselineData, Vector3[] targetData) // baseline == 답지, target == 사람 데이터, 프레임별로 불러오게 했는데 수정해도 될듯
    {
        int jointMatch = 0;

        if (baselineData.Length != targetData.Length)
        {
            Debug.LogError("Invalid input"); // Data 유효성 검사
            return 0.0f;
        }

        for (int i = 0; i < baselineData.Length; i++) // i == 답지 라인 순번
        {
            float distance = Vector3.Distance(baselineData[i], targetData[i]);
            float xDif, yDif, zDif, most; //x, y, z 차이, 가장 큰 값 저장

            xDif = targetData.x - baselineData.x;
            yDif = targetData.y - baselineData.y;
            zDif = targetData.z - baselineData.z;

            if (xDif < yDif)
            {
                if (zDif < yDif)
                {
                    most = yDif;
                }
                else
                {
                    most = xDif < zDif ? zDif : xDif;
                }
            }
            else if (xDif < zDif)
            {
                if (yDif < zDif)
                {
                    most = zDif;
                }
                else
                {
                    most = xDif < yDif ? yDif : xDif;
                }
            }

            if (distance < 0.2f) // 0.2보다 가까운 조인트 수 카운트
            {
                jointMatch += 1;
            }

            else if (baselineData[0])
            {

            }
        }

        return jointMatch;
    }
}