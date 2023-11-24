using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Reflection;

public class MovementScorer : MonoBehaviour
{
    public Vector3[] mediaMarking; // [����Ʈ] �����ϴ� 1���� �迭
    public string hoonsuMessage;

    public MovementScorer(int level, int numberOfJoints)
    {
        LoadMovementData(level, numberOfJoints);
    }

    // ������ ������ �ҷ����� �޼ҵ�
    public void LoadMovementData(int level, int numberOfJoints)
    {
        string filePath = $"Assets/Avatar/JointTextFile2/Level_{level}.txt"; //Level���� ����� txt ���� �ҷ�����

        if (File.Exists(filePath)) //���� ��ȿ�� �˻�
        {
            string[] lines = File.ReadAllLines(filePath);
            mediaMarking = new Vector3[lines.Length - 1]; // media Marking�� �����Դϴ�~

            for (int i = 0; i < lines.Length - 1; i++)    // i == ���� ����
            {
                string[] jointData = lines[i].Split(' '); // x, y, z �ڸ��� (����ȯ�� ���߿�)

                mediaMarking[i].x = float.Parse(jointData[0]);
                mediaMarking[i].y = float.Parse(jointData[1]);
                mediaMarking[i].z = float.Parse(jointData[2]);

                //for (int j = 0; j < numberOfJoints; j++) // j == ����Ʈ �ε���, 13���� ���������� ó���մϴ�
                //{
                //    int index = i * numberOfJoints + j; // index == (j��° ����Ʈ)/(n��° ������ ������Ʈ)
                //    Debug.Log("jointData: " + jointData[0]);
                //    //x, y, z ����ȯ�ؼ� ���ͷ� ����
                //    mediaMarking[index].x = float.Parse(jointData[0]);
                //    mediaMarking[index].y = float.Parse(jointData[1]);
                //    mediaMarking[index].z = float.Parse(jointData[2]);
                //}
            }
        }
        else
        {
            Debug.LogError($"File not found: {filePath}"); // ���� �ڵ鸵
        }
    }

    // ä���ϴ� �޼ҵ�
    public int ScoreMovement(Vector3[] baselineData, Vector3[] targetData) // baseline == ����, target == ��� ������, �����Ӻ��� ȣ���ϸ� ��
    {
        int jointMatch = 0;
        string[] jointName = { "�Ӹ�", "���� ���", "������ ���", "���� �Ȳ�ġ", "������ �Ȳ�ġ", "���� �ո�", "������ �ո�", "��"/*���� ���*/, "��"/*������ ���*/, "���� ����", "������ ����", "���� �߸�", "������ �߸�" };
        string hoonsuWay;
        Vector3 hoonsu = new Vector3();
        int mostDis_i = 0;
        float mostDis_val = 0;
        float videoRate, camRate, correctionRate;
        videoRate = (baselineData[1].y + baselineData[2].y) / 2 - (baselineData[7].y + baselineData[8].y) / 2;
        camRate = (targetData[1].y + targetData[2].y) / 2 - (targetData[7].y + targetData[8].y) / 2;

        correctionRate = videoRate / camRate;


        //0: ��
        //1, 2: ���� ���, ������ ���
        //3, 4: ���� �Ȳ�ġ, ������ �Ȳ�ġ
        //5, 6: ���� �ո�, ������ �ո�
        //7, 8: ���� ���, ������ ���
        //9, 10: ���� ����, ������ ����
        //11, 12: ���� �߸�, ������ �߸�


        if (baselineData.Length != targetData.Length)
        {
            Debug.LogError("Invalid input"); // Data ��ȿ�� �˻�
            return 0;
        }


        for (int i = 1; i < baselineData.Length; i++) // i == ���� ���� ����
        {   
            float distance = correctionRate * Vector3.Distance(baselineData[i], targetData[i]);

            if (distance < 0.5f) // 0.2���� ����� ����Ʈ �� ī��Ʈ
            {
                jointMatch += 1;
            }
            else if (mostDis_val < distance)
            {
                hoonsu = targetData[i];
                mostDis_i = i;
                mostDis_val = distance;
            }
        }

        float xDif = hoonsu.x - baselineData[mostDis_i].x; // x ���� ����
        float yDif = hoonsu.y - baselineData[mostDis_i].y; // y ���� ����
        float zDif = hoonsu.z - baselineData[mostDis_i].z; // z ���� ����

        //���� ���� -> ���� �߸��� �κ� ã��
        float absXDif = Mathf.Abs(xDif);
        float absYDif = Mathf.Abs(yDif);
        float absZDif = Mathf.Abs(zDif);
        
        if (absXDif > absYDif && absXDif > absZDif)
        {
            if (xDif < 0)
            {
                hoonsuWay = "������";
            }
            else
            {
                hoonsuWay = "����";
            }
        }
        else if (absYDif > absXDif && absYDif > absZDif)
        {
            if (yDif < 0)
            {
                hoonsuWay = "��";
            }
            else
            {
                hoonsuWay = "�Ʒ�";
            }
        }
        else
        {
            if (zDif < 0)
            {
                hoonsuWay = "��";
            }
            else
            {
                hoonsuWay = "��";
            }
        }
        hoonsuMessage = jointName[mostDis_i] + "��/�� " + hoonsuWay + "(��)�� �̵��ϼ���.";

        return jointMatch;
    }
}
