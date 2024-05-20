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
    public int numberOfJoints = 13; // ���� ����
    public int jointMatch; // ä�� �´� ����
    public int totalScore; // ��ü ����(���� ���� ����)

    public MovementScorer scorer;      //ä�� ����� ���� Ŭ����
    private int frameNum = 0;          //���� ������ ����

    public VideoPlayer video; //���� �÷��̾�
    int playCount = 0;        //���� ��� ��
    float startTime = 4f;     //Ÿ�̸� ���� �ð�        
    public TextMeshProUGUI timertext; //Ÿ�̸� ǥ�� �ؽ�Ʈ ������Ʈ 
    public TextMeshProUGUI resultText; // ��/�� ǥ��

    Socket socket;

    private Vector3 virtualNeck; //�� ���� ��ǥ
    private Vector3 virtualHips; //��� ���� ������ ���� ��ǥ
    private Vector3 virtualUpperChest;
    Vector3[] realJoint;

    Animator anim;
    bool poseUpdateCheck = true;

    private StoreJointData rightHip, leftHip, chestTwist, neckTwist, rightShoulder, leftShoulder; // ��ü ���� �����̴µ� ���
    private Dictionary<HumanBodyBones, StoreJointData> limbsJointData = new Dictionary<HumanBodyBones, StoreJointData>(); //�ȴٸ� �����̱� ���� ������ ����


    //Start is called before the first frame update
    void Start()
    {
        socket = null;
        anim = GetComponent<Animator>();
        if (LoadingScene.sock != null)
        {
            socket = LoadingScene.sock; //�ε������� ������ ���� �̾�ޱ�
            Debug.Log("���� �Ϸ�");
            StartCoroutine(PoseUpdate());
        }

        scorer = new MovementScorer(ScenesMove.nextStageNum, numberOfJoints);


    }

    //key: parent bone, value: StoreJointData Ŭ����
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
                timertext.text = seconds.ToString(); // �������� �ؽ�Ʈ ������Ʈ�� ǥ��
            }
            else
            {
                playCount++;
                video.Play();
                Debug.Log("���� ���");
                startTime = 4f;
            }
        }
        else if (!video.isPlaying && playCount == 2)
        {
            if (totalScore >= 200)
            {
                resultText.text = "�հ�";
            }
            else
            {
                resultText.text = "���հ�";
            }
        }
    }

    //����� �迭 ������ ���� ������ baseLineDataArray�� ����
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
            realJoint = new Vector3[13]; // �ؽ�Ʈ ���Ͽ��� �о�� x,y,z�� ĳ���� ���� position�� ����
            string[] textLine;
            string[] splitXYZ;
            Vector3[] baseLineDataArray = new Vector3[13];

            try
            {
                if (LoadingScene.socketConnect)
                {
                    byte[] buffer = new byte[4];
                    int byteCount = socket.Receive(buffer); //���ŵ� ����Ʈ ���� ������ ��ȯ�Ѵ�. -> 3
                    Array.Reverse(buffer);


                    int dataByteCount = BitConverter.ToInt32(buffer, 0);
                    byte[] receivedBuffer = new byte[dataByteCount]; // �������� �ϴ� �������� ����ŭ byte �迭 ����
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
                Debug.Log("����: " + e);
            }


            if (video.isPlaying)
            {
                if (playCount != 1) //���� ù��° ����� �׳� ���� �ι�° ������� ä�� ��� ��
                {
                    timertext.gameObject.SetActive(false);
                    baseLineDataArray = MakeBaseLineDataArray(frameNum); //���� ������ �� ���� ��ǥ 13���� ������ �迭 �����
                    jointMatch = scorer.ScoreMovement(baseLineDataArray, realJoint, 0.48 / Vector3.Distance(CheckCharacterJoint.storeJointData[1], CheckCharacterJoint.storeJointData[7])); //����� �迭�� �ǽð� ������� ���� �迭 ä�� �Լ��� ����

                    if (jointMatch >= 6)
                    {
                        totalScore++;
                    }
                    frameNum++; //����� �迭 ������Ʈ�� ���� ������ �� ����
                }
                else
                {
                    timertext.gameObject.SetActive(false);
                }
            }

            virtualHips = (realJoint[7] + realJoint[8]) / 2.0f;// ������ �� ������ ��ġ ���ϱ�
            virtualHips.y += 0.075f;
            virtualHips.y += 0.95f;
            virtualHips.x -= 0.5f;
            virtualHips.z += 0.5f;

            virtualNeck = (realJoint[1] + realJoint[2]) / 2.0f; // ������ �� ������ ��ġ ���ϱ�
            virtualNeck.y += 0.05f;

            virtualUpperChest = (realJoint[1] + realJoint[2]) / 2.0f; //������ UpperChest ���� ��ġ ���ϱ�
            virtualUpperChest.y -= 0.1f;

            for (int i = 0; i < 13; i++)
            {
                realJoint[i].y *= -1f; // ���� ���� ����Ʈ ���� y��ǥ�� ���� �ݴ�� �Ǿ�����
                realJoint[i] += virtualHips; // pose_world_landmarks�� ������ �߰� ����Ʈ�� �������� �����ǥ�̹Ƿ� Hips�� ��ġ�� ���� ���� ��ǥ�� �����ش�.
                                             //Debug.Log("realJoint: " + realJoint[i]);
            }

            virtualNeck += virtualHips;
            virtualUpperChest += virtualHips;

            //��ü ���� �����̱� ���� ��ü ���� StoreJointData Ŭ������ ����
            rightHip = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.Hips), anim.GetBoneTransform(HumanBodyBones.RightUpperLeg), virtualHips, realJoint[8]);
            leftHip = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.Hips), anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg), virtualHips, realJoint[7]);

            neckTwist = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.Neck), anim.GetBoneTransform(HumanBodyBones.Head), virtualNeck, realJoint[0]);

            rightShoulder = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.UpperChest), anim.GetBoneTransform(HumanBodyBones.RightUpperArm), virtualUpperChest, realJoint[2]);
            leftShoulder = new StoreJointData(anim.GetBoneTransform(HumanBodyBones.UpperChest), anim.GetBoneTransform(HumanBodyBones.LeftUpperArm), virtualUpperChest, realJoint[1]);


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
                Quaternion changeRot = Quaternion.FromToRotation(i.Value.initialDir, Vector3.Slerp(i.Value.initialDir, i.Value.CurrentDirection, 0.5f));
                i.Value.parent.rotation = changeRot * i.Value.initialRotation;
            }

            rightHip.RotateTorso(rightHip, 0.5f);
            leftHip.RotateTorso(leftHip, 0.5f);
            //neckTwist.RotateTorso(neckTwist, 0.05f);
            //chestTwist.RotateTorso(chestTwist, 0.2f);
            rightShoulder.RotateTorso(rightShoulder, 0.4f); //Hips ���� ȸ������ RightUpperArm�� LeftUpperArm�� ȸ���� �� ��
            leftShoulder.RotateTorso(leftShoulder, 0.4f);

            anim.GetBoneTransform(HumanBodyBones.Hips).position = virtualHips;

            yield return new WaitForSeconds(0.048f); // ���̽� �������� ��� �ӵ��� ���߱� ���� ������

            //��ǥ �ʱ�ȭ
            virtualNeck = Vector3.zero;
            virtualUpperChest = Vector3.zero;
            virtualHips = Vector3.zero;
            limbsJointData.Clear();
        }
    }

}


