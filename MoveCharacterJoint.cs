using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class MoveCharacterJoint : MonoBehaviour
{
    //private string fullpth = "Assets/Avartar/JointTextFile2/jabCross.txt";
    private string fullpth = "Assets/Avartar/JointTextFile2/tesst.txt";
    public int textCount = 0; //�� ����

    StreamReader sr; //��Ʈ�� ����
    string[] textValue; //�ؽ�Ʈ ���� ��ü �� ���� �迭
    string[] jointXYZ;  //�ؽ�Ʈ ���Ͽ��� ����Ʈ x,y,z�� �����ϴ� �迭
    Vector3[] realJoint = new Vector3[13]; // �ؽ�Ʈ ���Ͽ��� �о�� x,y,z�� ĳ���� ���� position�� ����

    Animator anim;

    private Vector3 virtualNeck;
    private Vector3 virtualHips;
    private Vector3 virtualUpperChest;

    private StoreJointData rightHip, leftHip, chestTwist, neckTwist, rightShoulder, leftShoulder;
    //parentModelJoint�� 
    private Dictionary<HumanBodyBones, StoreJointData> limbsJointData = new Dictionary<HumanBodyBones, StoreJointData>();

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();

        FileInfo fileInfo = new FileInfo(fullpth);

        if (fileInfo.Exists)
        {
            Debug.Log("���� ����");
            sr = new StreamReader(fullpth);
            textValue = File.ReadAllLines(fullpth); //�ؽ�Ʈ ������ ��� �� �о���̱�
            textCount = textValue.Length;

        }
        else
        {
            Debug.Log("���� ��ο� ������ �����ϴ�. ��ΰ� �߸��Ǿ����� Ȯ���ϼ���.");
        }
        //Debug.Log(anim.GetBoneTransform(HumanBodyBones.Hips).position);
    }

    //key: parent bone, value: StoreJointData Ŭ����
    private void AddModelJointData(HumanBodyBones parent, HumanBodyBones child, Vector3 trackParent, Vector3 trackChild) {
        limbsJointData.Add(parent,
            new StoreJointData(anim.GetBoneTransform(parent), anim.GetBoneTransform(child), trackParent, trackChild));
    }

    // Update is called once per frame
    void Update()
    {
        if (textCount > 1) //textCount�� 1�϶� line������ Null���� ���� ��
        {
            for (int i = 0; i < 13; i++)
            {
                string line = sr.ReadLine(); //������ ���پ� �޾ƿ��� \n����
                //Debug.Log(line);

                jointXYZ = line.Split(' '); //3���� ���� ' ' �� �������� ���� �迭�� ����

                //string���� ����Ǿ� �ִ� ���� float������ ��ȯ �� ����
                realJoint[i].x = float.Parse(jointXYZ[0]); 
                realJoint[i].y = float.Parse(jointXYZ[1]);
                realJoint[i].z = float.Parse(jointXYZ[2]);

                textCount--;
            }
        }
        else
        {
            sr.Close(); // streamReader ����
            UnityEditor.EditorApplication.isPlaying = false;
        }

        //������ ���� ������ �����
        virtualNeck = (realJoint[1] + realJoint[2]) / 2.0f; // ������ �� ������ ��ġ ���ϱ�
        virtualNeck.y += 0.05f;
        virtualHips = (realJoint[7] + realJoint[8]) / 2.0f;  // ������ �� ������ ��ġ ���ϱ�
        virtualHips.y += 0.075f;
        virtualUpperChest = (realJoint[1] + realJoint[2]) / 2.0f; //������ UpperChest ���� ��ġ ���ϱ�
        virtualUpperChest.y -= 0.1f;
        
        //��ü ���� �����̱� ���� ��ü ���� StoreJointData Ŭ������ ����
        rightHip = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.Hips), anim.GetBoneTransform(HumanBodyBones.RightUpperLeg), virtualHips, realJoint[8]);
        leftHip = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.Hips), anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg), virtualHips, realJoint[7]);

        neckTwist = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.Neck), anim.GetBoneTransform(HumanBodyBones.Head), virtualNeck, realJoint[0]);
        //chestTwist = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.UpperChest), anim.GetBoneTransform(HumanBodyBones.Neck), virtualUpperChest, virtualNeck);

        rightShoulder = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.UpperChest), anim.GetBoneTransform(HumanBodyBones.RightUpperArm), virtualUpperChest, realJoint[2]);
        leftShoulder = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.UpperChest), anim.GetBoneTransform(HumanBodyBones.LeftUpperArm), virtualUpperChest, realJoint[1]);

        limbsJointData.Clear(); //�迭 ��� �����
        //limbsJointData �迭�� �ȴٸ��� ���� ������(ĳ���� �ȴٸ� ����, ȸ�� ���� ��) ����
        AddModelJointData(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, realJoint[2], realJoint[4]);
        AddModelJointData(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, realJoint[4], realJoint[6]);

        AddModelJointData(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, realJoint[1], realJoint[3]);
        AddModelJointData(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, realJoint[3], realJoint[5]);

        AddModelJointData(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, realJoint[8], realJoint[10]);
        AddModelJointData(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, realJoint[10], realJoint[12]);

        AddModelJointData(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, realJoint[7], realJoint[9]);
        AddModelJointData(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, realJoint[9], realJoint[11]);

        //�迭�� ����� �ȴٸ��� ���� �����͸� �̿��� ����Ʈ ȸ������ �ٲٴ� �ڵ�
        foreach (var i in limbsJointData)
        {
            Quaternion changeRot = Quaternion.FromToRotation(i.Value.initialDir, Vector3.Slerp(i.Value.initialDir, i.Value.CurrentDirection, 0.3f));
            i.Value.parent.rotation = changeRot * i.Value.initialRotation;
        }

        rightHip.RotateTorso(rightHip, 0.5f);
        leftHip.RotateTorso(leftHip, 0.5f);
        neckTwist.RotateTorso(neckTwist, 0.5f);
        //chestTwist.RotateTorso(chestTwist, 0.2f);
        rightShoulder.RotateTorso(rightShoulder, 0.3f); //Hips ���� ȸ������ RightUpperArm�� LeftUpperArm�� ȸ���� �� ��
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

class StoreJointData //avatar joint���� ������ �����ϴ� Ŭ����
{
    public Transform parent, child; // ĳ���� �θ� ����Ʈ, �ڽ� ����Ʈ transform ����
    public Vector3 tParent, tChild; //���� ���� �𵨷� ������ �θ� ����Ʈ x,y,z ��ġ��, �ڽ� ����Ʈ x,y,z ��ġ��
    public Vector3 initialDir;         // ĳ���� ����Ʈ ȸ���ϱ� �� �ڽ�, �θ�����Ʈ�� ��� ��ġ = ����
    public Quaternion initialRotation; //ĳ���� ����Ʈ ȸ���ϱ� �� ����Ʈ ȸ����

    public Quaternion targetRotation;
    float speed = 10f;

    public void RotateTorso(StoreJointData avatarJoint, float amount) {
        this.targetRotation = Quaternion.FromToRotation(avatarJoint.initialDir, Vector3.Slerp(avatarJoint.initialDir, avatarJoint.CurrentDirection, amount));
        this.targetRotation *= avatarJoint.initialRotation;
        this.Make(this.targetRotation, speed);
    }

    //���� ȸ�� �������� target �������� ȸ����Ű�� �Լ�
    public void Make(Quaternion newTarget, float speed)
    {
        targetRotation = newTarget;
        parent.rotation = Quaternion.Lerp(parent.rotation, targetRotation, Time.deltaTime * speed);
    }

    //�𵨷� ������ ����Ʈ�� ������
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
    private Vector3[] mediaMarking; // [����Ʈ] �����ϴ� 1���� �迭

    public MovementScorer(int numberOfLevels, int numberOfJoints)
    {
        for (int i = 0; i < numberOfLevels; i++)
        {
            LoadMovementData(i + 1, numberOfJoints);
        }
    }

    // ������ ������ �ҷ����� �޼ҵ�
    private void LoadMovementData(int level, int numberOfJoints)
    {
        string filePath = $"Assets/Avartar/JointTextFile2/Level_{level}.txt"; //Level���� ����� txt ���� �ҷ�����
#define numOfJoints 

        if (File.Exists(filePath)) //���� ��ȿ�� �˻�
        {
            string[] lines = File.ReadAllLines(filePath);
            mediaMarking = new Vector3[lines.Length]; // media Marking�� �����Դϴ�~

            for (int i = 0; i < lines.Length; i++) // i == ���� ����
            {
                string[] jointData = lines[i].Split(' '); // x, y, z �ڸ��� (����ȯ�� ���߿�)

                for (int j = 0; j < numberOfJoints; j++) // j == ����Ʈ �ε���, 13���� ���������� ó���մϴ�
                {
                    int index = i * numberOfJoints + j; // index == (j��° ����Ʈ)/(n��° ������ ������Ʈ)
                    // x, y, z �Ľ� & ����ȯ�ؼ� ���ͷ� ����
                    mediaMarking[index].x = float.Parse(jointData[0]);
                    mediaMarking[index].y = float.Parse(jointData[1]);
                    mediaMarking[index].z = float.Parse(jointData[2]);
                }
            }
        }
        else
        {
            Debug.LogError($"File not found: {filePath}"); // ���� �ڵ鸵
        }
    }

    // ä���ϴ� �޼ҵ�
    public int ScoreMovement(Vector3[] baselineData, Vector3[] targetData) // baseline == ����, target == ��� ������, �����Ӻ��� �ҷ����� �ߴµ� �����ص� �ɵ�
    {
        int jointMatch = 0;

        if (baselineData.Length != targetData.Length)
        {
            Debug.LogError("Invalid input"); // Data ��ȿ�� �˻�
            return 0.0f;
        }

        for (int i = 0; i < baselineData.Length; i++) // i == ���� ���� ����
        {
            float distance = Vector3.Distance(baselineData[i], targetData[i]);
            float xDif, yDif, zDif, most; //x, y, z ����, ���� ū �� ����

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

            if (distance < 0.2f) // 0.2���� ����� ����Ʈ �� ī��Ʈ
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