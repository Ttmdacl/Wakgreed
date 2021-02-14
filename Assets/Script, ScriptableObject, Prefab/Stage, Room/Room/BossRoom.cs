using UnityEngine;

public class BossRoom : Room
{
    [SerializeField] private PoolType boss;
    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] EnemyRunTimeSet EnemyRunTimeSet;

    public override void Enter()
    {
        if (isVisited == false) isVisited = true;
        doorHiders[0].SetActive(!isConnectToNearbyRoom[0]);
        doorHiders[1].SetActive(!isConnectToNearbyRoom[1]);
        doorHiders[2].SetActive(!isConnectToNearbyRoom[2]);
        doorHiders[3].SetActive(!isConnectToNearbyRoom[3]);
    }   

    public void SummonBoss()
    {
        GameManager.Instance.SetFighting(true);

        doorHiders[0].SetActive(true);
        doorHiders[1].SetActive(true);
        doorHiders[2].SetActive(true);
        doorHiders[3].SetActive(true);

        EnemyRunTimeSet.Add(ObjectManager.Instance.GetQueue(boss, bossSpawnPoint.position));
        StartCoroutine(GameManager.Instance.BossSpeedWagon());
    }

    public void CheckMonsterCount()
    {
        if (GameManager.Instance.currentRoom != this) return;
        // 페이즈 체크
        RoomClear();
    }

    private void RoomClear()
    {
        if (Random.Range(0, 100) < 30)
            ObjectManager.Instance.GetQueue(PoolType.Item, transform.position);

        // 보스 클리어 연출
        transform.Find("Portal").gameObject.SetActive(true);

        GameManager.Instance.SetFighting(false);

        doorHiders[0].SetActive(!isConnectToNearbyRoom[0]);
        doorHiders[1].SetActive(!isConnectToNearbyRoom[1]);
        doorHiders[2].SetActive(!isConnectToNearbyRoom[2]);
        doorHiders[3].SetActive(!isConnectToNearbyRoom[3]);       

        StartCoroutine(GameManager.Instance.RoomClearSpeedWagon());
    }
}