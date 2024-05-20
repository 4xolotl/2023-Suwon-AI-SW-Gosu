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

    private Vector3 virtualNeck; //�� ���� ��ǥ
    private Vector3 virtualHips; //��� ���� ������ ���� ��ǥ
    private Vector3 virtualUpperChest;
    private StoreJointData rightHip, leftHip, chestTwist, neckTwist, rightShoulder, leftShoulder; // ��ü ���� �����̴µ� ���
    private Dictionary<HumanBodyBones, StoreJointData> limbsJointData = new Dictionary<HumanBodyBones, StoreJointData>(); //�ȴٸ� �����̱� ���� ������ ����


    // Start is called before the first frame update
    void Start()
    {
        socket = null;
        anim = GetComponent<Animator>();
        if (LoadingScene.sock != null)
        {
            socket = LoadingScene.sock; //�ε������� ������ ���� �̾�ޱ�
            Debug.Log("���� �Ϸ�");
            StartCoroutine(JointUpdate());
        }

        //FileInfo fileInfo = new FileInfo(fullpth);

        //if (fileInfo.Exists)
        //{
        //    Debug.Log("���� ����");
        //    sr = new StreamReader(fullpth);
        //    textValue = File.ReadAllLines(fullpth); //�ؽ�Ʈ ������ ��� �� �о���̱�
        //    textCount = textValue.Length;

        //}
        //else
        //{
        //    Debug.Log("���� ��ο� ������ �����ϴ�. ��ΰ� �߸��Ǿ����� Ȯ���ϼ���.");
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

            //if (textCount > 1) //textCount�� 1�϶� line������ Null���� ���� ��
            //{
            //    for (int i = 0; i < 13; i++)
            //    {
            //        string line = sr.ReadLine(); //������ ���پ� �޾ƿ��� \n����


            //        jointXYZ = line.Split(' '); //3���� ���� ' ' �� �������� ���� �迭�� ����

            //        //string���� ����Ǿ� �ִ� ���� float������ ��ȯ �� ����
            //        realJoint[i].x = float.Parse(jointXYZ[0]);
            //        realJoint[i].y = float.Parse(jointXYZ[1]);
            //        realJoint[i].z = float.Parse(jointXYZ[2]);

            //        textCount--;
            //    }
            //}

            //else
            //{
            //    sr.Close(); // streamReader ����
            //    Debug.Log("��Ʈ�� ���� ����");
            //    UnityEditor.EditorApplication.isPlaying = false;
            //}

            //if (UnityEditor.EditorApplication.isPlaying == false)
            //{
            //    sr.Close();
            //}

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
            catch(Exception e) {
                Debug.Log("����: "+ e);
            }
            
            if (updateCheck) {
                if (count > 0)
                {
                    comment.text = "������� ���� ��ġ ���� �� ...";
                    for (int i = 0; i < 13; i++)
                    {
                        storeJointData[i] += (realJoint[i] / frameCount);
                    }
                    count--;
                }
                else if (count == 0)
                {
                    nextButton.gameObject.SetActive(true);
                    comment.text = "����� ���� ��ġ ���� �Ϸ�!";
                    count = -1;
                }
            }
            
            virtualHips = (realJoint[7] + realJoint[8]) / 2.0f;// ������ �� ������ ��ġ ���ϱ�
            virtualHips.y += 0.075f;
            virtualHips.y += 0.95f;
            virtualHips.x -= 0.5f;
            virtualHips.z -= 0.5f;

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
            rightShoulder.RotateTorso(rightShoulder, 0.5f); //Hips ���� ȸ������ RightUpperArm�� LeftUpperArm�� ȸ���� �� ��
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
    
