﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageManager : MonoBehaviour
{
    private static StageManager instance;
    [HideInInspector] public static StageManager Instance { get { return instance; } }

    public int currentStageID = -1;
    [SerializeField] private StageDataBuffer stageDataBuffer;

    [SerializeField] private int roomCount = 0;
    [SerializeField] private int stageEdgeLength = 0;
    private Dictionary<Vector2, Room> roomDic = new();
    private List<RoomMold> roomMolds = new();
    private class RoomMold
    {
        public Vector2 coordinate;
        public bool[] isConnectToNearbyRoom = new bool[4];
    }
    private List<Room> roomDatas = new();
    public Room CurrentRoom { get; private set; }
    [SerializeField] private GameObject stageGrid;

    [SerializeField] private GameObject miniMapCamera;

    public GameObject mapPanel;
    [SerializeField] private GridLayoutGroup mapGridLayoutGroup;
    [SerializeField] private RectTransform scrollRectBackGround;
    private Dictionary<Vector2, GameObject> roomUiDic = new();

    [SerializeField] private GameObject fadePanel;
    [SerializeField] private Animator fadePanelAnimator;
    [SerializeField] private TextMeshProUGUI noticeText;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && mapPanel.activeSelf == false) MapDoor(true);
        else if (Input.GetKeyUp(KeyCode.Tab) && mapPanel.activeSelf == true) MapDoor(false);
    }

    public void GenerateStage()
    {
        DestroyStage();
        roomMolds.Clear();
        roomDic.Clear();

        roomDatas = new List<Room>(stageDataBuffer.Items[++currentStageID].roomDatas);
        roomMolds.Add(new RoomMold() { coordinate = Vector2.zero });

        while (roomMolds.Count < roomCount)
        {
            RoomMold originalRoomMold = roomMolds[Random.Range(0, roomMolds.Count)];
            int i = Random.Range(0, 4);
            Vector2 totalRoomMoldCoordinante = originalRoomMold.coordinate + ((i == 0) ? Vector2.up : (i == 1) ? Vector2.down : (i == 2) ? Vector2.left : Vector2.right);

            if (roomMolds.Find(x => x.coordinate == totalRoomMoldCoordinante) != null) continue;
            if (i == 0 && originalRoomMold.coordinate.y == (stageEdgeLength - 1) / 2) continue;
            else if (i == 1 && originalRoomMold.coordinate.y == -(stageEdgeLength - 1) / 2) continue;
            else if (i == 2 && originalRoomMold.coordinate.x == -(stageEdgeLength - 1) / 2) continue;
            else if (i == 3 && originalRoomMold.coordinate.x == (stageEdgeLength - 1) / 2) continue;

            RoomMold totalRoomMold = new() { coordinate = totalRoomMoldCoordinante };

            originalRoomMold.isConnectToNearbyRoom[i] = true;
            totalRoomMold.isConnectToNearbyRoom[(i == 0) ? 1 : (i == 1) ? 0 : (i == 2) ? 3 : 2] = true;

            roomMolds.Add(totalRoomMold);
        }

        for (int i = 0; i < roomCount; i++)
        {
            int roomMoldIndex = (i == 0) ? 0 : Random.Range(0, roomMolds.Count);
            int roomDataIndex = (i <= 2) ? 0 : Random.Range((DataManager.Instance.curGameData.isNPCRescued) ? 1 : 0, roomDatas.Count);

            // 스테이지 별 룸 데이타 딕셔너리 혹은 배열만들어야 함
            Room r = Instantiate(roomDatas[roomDataIndex].gameObject, stageGrid.transform).GetComponent<Room>();
            r.Initialize(roomMolds[roomMoldIndex].coordinate, roomMolds[roomMoldIndex].isConnectToNearbyRoom);

            roomMolds.RemoveAt(roomMoldIndex);
            roomDatas.RemoveAt(roomDataIndex);
            roomDic.Add(r.Coordinate, r);
        }

        StartCoroutine(StartStage());
    }

    public void DestroyStage()
    {
        for (int i = 0; i < stageGrid.transform.childCount; i++)
        {
            Destroy(stageGrid.transform.GetChild(i).gameObject);
        }
    }

    private IEnumerator StartStage()
    {
        CurrentRoom = roomDic[Vector2.zero];
        CurrentRoom.Enter();
        InitialzeMap();

        Wakgood.Instance.transform.position = new Vector3(CurrentRoom.Coordinate.x, CurrentRoom.Coordinate.y, 0) * 100;
        miniMapCamera.transform.position = new Vector3(CurrentRoom.Coordinate.x, CurrentRoom.Coordinate.y, -1) * 100;

        fadePanelAnimator.SetTrigger("FadeIn");
        StartCoroutine("StageSpeedWagon");
        yield return new WaitForSeconds(0.2f);
        fadePanel.SetActive(false);
    }

    private void InitialzeMap()
    {
        mapGridLayoutGroup.constraintCount = stageEdgeLength;
        roomUiDic = new Dictionary<Vector2, GameObject>();

        int x = -(stageEdgeLength - 1) / 2;
        int y = (stageEdgeLength - 1) / 2;
        for (int i = 0; i < mapGridLayoutGroup.transform.childCount; i++)
        {
            if (i <= stageEdgeLength * stageEdgeLength - 1)
            {
                GameObject targetRoomUI = mapGridLayoutGroup.transform.GetChild(i).gameObject;
                Vector2 targetRoomCoordinate = new(x, y);
                // Debug.Log(targetRoomCoordinate);
                targetRoomUI.SetActive(true);
                targetRoomUI.GetComponent<Image>().enabled = false;
                targetRoomUI.transform.GetChild(0).gameObject.SetActive(false);

                if (roomDic.ContainsKey(targetRoomCoordinate))
                {
                    Room targetRoom = roomDic[targetRoomCoordinate];
                    roomUiDic.Add(targetRoomCoordinate, targetRoomUI);
                    targetRoomUI.transform.GetChild(0).Find("Boss").gameObject.SetActive(targetRoom.roomType == RoomType.Boss);
                    targetRoomUI.transform.GetChild(0).Find("Spawn").gameObject.SetActive(targetRoom.roomType == RoomType.Spawn);
                    targetRoomUI.transform.GetChild(0).Find("Shop").gameObject.SetActive(targetRoom.roomType == RoomType.Shop);
                    targetRoomUI.transform.GetChild(0).Find("Up").gameObject.SetActive(targetRoom.IsConnectToNearbyRoom[0]);
                    targetRoomUI.transform.GetChild(0).Find("Down").gameObject.SetActive(targetRoom.IsConnectToNearbyRoom[1]);
                    targetRoomUI.transform.GetChild(0).Find("Left").gameObject.SetActive(targetRoom.IsConnectToNearbyRoom[2]);
                    targetRoomUI.transform.GetChild(0).Find("Right").gameObject.SetActive(targetRoom.IsConnectToNearbyRoom[3]);
                    targetRoomUI.transform.GetChild(0).Find("CurrentRoom").GetComponent<Image>().color = new Color(200f / 255f, 200f / 255f, 200f / 255f);
                }

                x++;
                if (x > (stageEdgeLength - 1) / 2)
                {
                    x = -(stageEdgeLength - 1) / 2;
                    y--;
                }
            }
            else if (i > stageEdgeLength * stageEdgeLength - 1)
            {
                mapGridLayoutGroup.transform.GetChild(i).gameObject.SetActive(false);
            }
        }

        UpdateMap();
    }

    private void UpdateMap()
    {
        scrollRectBackGround.localPosition = -CurrentRoom.Coordinate * (mapGridLayoutGroup.cellSize.x + mapGridLayoutGroup.spacing.x);
        roomUiDic[CurrentRoom.Coordinate].GetComponent<Image>().enabled = true;
        roomUiDic[CurrentRoom.Coordinate].transform.GetChild(0).gameObject.SetActive(true);
        roomUiDic[CurrentRoom.Coordinate].transform.GetChild(0).Find("CurrentRoom").GetComponent<Image>().color = new Color(0f / 255f, 200f / 255f, 255f / 255f);
    }

    public IEnumerator MigrateRoom(Vector2 moveDirection, int spawnDirection)
    {
        fadePanel.SetActive(true);
        yield return new WaitForSeconds(0.2f);

        roomUiDic[CurrentRoom.Coordinate].transform.GetChild(0).Find("CurrentRoom").GetComponent<Image>().color = new Color(200f / 255f, 200f / 255f, 200f / 255f);

        CurrentRoom = roomDic[CurrentRoom.Coordinate + moveDirection];
        Wakgood.Instance.transform.position = CurrentRoom.Doors[spawnDirection].transform.position + (Vector3)moveDirection * 4;
        miniMapCamera.transform.position = new Vector3(CurrentRoom.Coordinate.x, CurrentRoom.Coordinate.y, -1) * 100;

        UpdateMap();

        if (CurrentRoom.IsVisited == false) mapPanel.SetActive(false);

        CurrentRoom.Enter();

        yield return new WaitForSeconds(0.1f);
        fadePanelAnimator.SetTrigger("FadeIn");
        yield return new WaitForSeconds(0.2f);
        fadePanel.SetActive(false);
    }

    private void MapDoor(bool bOpen)
    {
        if (GameManager.Instance.IsFighting)
        {
            StopCoroutine("CantOpenText");
            StartCoroutine("CantOpenText");
        }
        else
        {
            StopCoroutine("CantOpenText");

            scrollRectBackGround.localPosition = -CurrentRoom.Coordinate * (mapGridLayoutGroup.cellSize.x + mapGridLayoutGroup.spacing.x);
            mapPanel.SetActive(bOpen);
        }
    }

    [SerializeField] private TextMeshProUGUI stageNumberText, stageNameCommentText;
    [SerializeField] private GameObject stageSpeedWagon;

    private IEnumerator StageSpeedWagon()
    {
        stageSpeedWagon.SetActive(true);
        stageNumberText.text = $"1-{stageDataBuffer.Items[currentStageID].id}";
        stageNameCommentText.text = $"{stageDataBuffer.Items[currentStageID].name} : {stageDataBuffer.Items[currentStageID].comment}";
        yield return new WaitForSeconds(2f);
        stageSpeedWagon.SetActive(false);
    }

    private IEnumerator CantOpenText()
    {
        noticeText.text = "전투 중에는 열 수 없습니다.";
        noticeText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1f);
        noticeText.gameObject.SetActive(false);
    }

    public void StopAllSpeedWagons()
    {
        StopCoroutine("StageSpeedWagon");
        stageSpeedWagon.SetActive(false);
        StopCoroutine("CantOpenText");
        noticeText.gameObject.SetActive(false);
    }

    public void CheckMonsterCount()
    {
        if (CurrentRoom is NormalRoom) (CurrentRoom as NormalRoom).CheckMonsterCount();
    }
}