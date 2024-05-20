import mediapipe as mp
import socket
import time
import cv2


mpPose = mp.solutions.pose
pose = mpPose.Pose()

# 0은 웹캡
cap = cv2.VideoCapture(0)
# 이전 프레임 처리 시간 저장 변수 초기화
pTime = 0
# 출력하고 싶은 관절들의 인덱스
desired_indices = [0, 11, 12, 13, 14, 15, 16, 23, 24, 25, 26, 27, 28]

# TCP 서버 생성
server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server_socket.bind(('localhost', 포트번호))
server_socket.listen(1)  # 최대 1개의 연결을 기다림

print("서버가 연결을 대기 중입니다.")

while True:
    
    # 클라이언트 연결 대기
    try:
        client_socket, addr = server_socket.accept()
        print(f"{addr}에서 연결이 수락되었습니다.")

        # 문자열 대기
        start_message = client_socket.recv(5).decode()
        if start_message == 'start':
            start_message = ''


        # 실시간을 위해 무한 루프 돌려 줍니다
        while True:
            
            # 바이트 배열 초기화
            coordinates_bytes = bytearray()
            
            # 성공여부, 이미지
            success, img = cap.read()
            
            # 이미지 크기 축소
            new_width = 480  # 가로 크기
            new_height = 640  # 세로 크기
            img_small = cv2.resize(img, (new_width, new_height))
            
            #rgb로 변경 (mediapipe는 rgb 이미지를 사용)
            imgRGB = cv2.cvtColor(img_small, cv2.COLOR_BGR2RGB)
            results = pose.process(imgRGB)
            # print("시작")
            
            if results.pose_world_landmarks:
                
                for desired_idx in desired_indices:
                    landmark = results.pose_world_landmarks.landmark[desired_idx]
                    x = landmark.x
                    y = landmark.y
                    z = landmark.z

                    # 선택한 관절 지점의 정보 출력
                    print(f"{x:.5f} {y:.5f} {z:.5f}".format(x,y,z))
                    
                    # 좌표값을 바이트 배열에 추가
                    coordinates_bytes += f"{x:.5f} {y:.5f} {z:.5f}\n".format(x, y, z).encode()

                coordinates_bytes = coordinates_bytes.rstrip(b'\0')
                
                # 바이트 배열로 변환된 좌표값을 Unity 서버로 전송
                coordinates_length = len(coordinates_bytes)
                length_bytes = coordinates_length.to_bytes(4, byteorder='big')  # 4바이트로 나타냄 (임의로 조절 가능) 여기서 주의.. c#으로 보낼 땐.. c#에서 array_reverse를 해줘야 한다..

                # 길이 정보 전송

                client_socket.sendall(length_bytes)
                        
                # 바이트 배열로 변환된 좌표값을 Unity 서버로 전송
                client_socket.sendall(coordinates_bytes)
            
                

            cTime = time.time()
            fps = 1 / (cTime - pTime)
            pTime = cTime

            # 이미지에 프레임 속도 표시
            # cv2.putText(img, str(int(fps)), (70, 50), cv2.FONT_HERSHEY_PLAIN, 3, (255, 0, 0), 3)
            cv2.imshow("image", img)

            if cv2.waitKey(10) & 0xFF == ord('q'):  # 'q' 키를 누르면 루프 종료
                break
            
    except Exception as e:
        print(f"연결 수락 중 오류 발생: {e}")
        

# 연결 종료
client_socket.close()
server_socket.close()
cap.release()
cv2.destroyAllWindows()
