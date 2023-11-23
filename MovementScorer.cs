using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Reflection;

public class MovementScorer : MonoBehaviour
{
    public Vector3[] mediaMarking; // [조인트] 저장하는 1차원 배열
    public string hoonsuMessage;

    public MovementScorer(int level, int numberOfJoints)
    {
        LoadMovementData(level, numberOfJoints);
    }

    // 레벨별 데이터 불러오는 메소드
    public void LoadMovementData(int level, int numberOfJoints)
    {
        string filePath = $"Assets/Avatar/JointTextFile2/Level_{level}.txt"; //Level별로 저장된 txt 파일 불러오기

        if (File.Exists(filePath)) //파일 유효성 검사
        {
            string[] lines = File.ReadAllLines(filePath);
            mediaMarking = new Vector3[lines.Length - 1]; // media Marking이 답지입니다~

            for (int i = 0; i < lines.Length - 1; i++)    // i == 라인 순번
            {
                string[] jointData = lines[i].Split(' '); // x, y, z 자르기 (형변환은 나중에)

                mediaMarking[i].x = float.Parse(jointData[0]);
                mediaMarking[i].y = float.Parse(jointData[1]);
                mediaMarking[i].z = float.Parse(jointData[2]);

                //for (int j = 0; j < numberOfJoints; j++) // j == 조인트 인덱스, 13개를 순차적으로 처리합니다
                //{
                //    int index = i * numberOfJoints + j; // index == (j번째 조인트)/(n번째 프레임 관절세트)
                //    Debug.Log("jointData: " + jointData[0]);
                //    //x, y, z 형변환해서 벡터로 저장
                //    mediaMarking[index].x = float.Parse(jointData[0]);
                //    mediaMarking[index].y = float.Parse(jointData[1]);
                //    mediaMarking[index].z = float.Parse(jointData[2]);
                //}
            }
        }
        else
        {
            Debug.LogError($"File not found: {filePath}"); // 예외 핸들링
        }
    }

    // 채점하는 메소드
    public int ScoreMovement(Vector3[] baselineData, Vector3[] targetData) // baseline == 답지, target == 사람 데이터, 프레임별로 호출하면 됨
    {
        int jointMatch = 0;

        if (baselineData.Length != targetData.Length)
        {
            Debug.LogError("Invalid input"); // Data 유효성 검사
            return 0;
        }

        Vector3 hoonsu = new Vector3();
        int mostDis_i = 0;
        float mostDis_val = 0;

        for (int i = 1; i < baselineData.Length; i++) // i == 답지 라인 순번
        {   
            float distance = Vector3.Distance(baselineData[i], targetData[i]);

            if (distance < 0.2f) // 0.2보다 가까운 조인트 수 카운트
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

        float xDif = hoonsu.x - baselineData[mostDis_i].x; // x 값의 차이
        float yDif = hoonsu.y - baselineData[mostDis_i].y; // y 값의 차이
        float zDif = hoonsu.z - baselineData[mostDis_i].z; // z 값의 차이

        //절댓값 저장 -> 가장 잘못된 부분 찾기
        float absXDif = Mathf.Abs(xDif);
        float absYDif = Mathf.Abs(yDif);
        float absZDif = Mathf.Abs(zDif);
        string[] jointName = { "머리", "왼쪽 어깨", "오른쪽 어깨", "왼쪽 팔꿈치", "오른쪽 팔꿈치", "왼쪽 손목", "오른쪽 손목", "몸"/*왼쪽 골반*/, "몸"/*오른쪽 골반*/, "왼쪽 무릎", "오른쪽 무릎", "왼쪽 발목", "오른쪽 발목" };
        string hoonsuWay;

        if (absXDif > absYDif && absXDif > absZDif)
        {
            if (xDif < 0)
            {
                hoonsuWay = "오른쪽";
            }
            else
            {
                hoonsuWay = "왼쪽";
            }
        }
        else if (absYDif > absXDif && absYDif > absZDif)
        {
            if (yDif < 0)
            {
                hoonsuWay = "위";
            }
            else
            {
                hoonsuWay = "아래";
            }
        }
        else
        {
            if (zDif < 0)
            {
                hoonsuWay = "뒤";
            }
            else
            {
                hoonsuWay = "앞";
            }
        }
        hoonsuMessage = jointName[mostDis_i] + "을/를 " + hoonsuWay + "(으)로 이동하세요.";

        return jointMatch;
    }
}
