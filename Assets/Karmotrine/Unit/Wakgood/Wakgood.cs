using Cinemachine;
using FMODUnity;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

public class Wakgood : MonoBehaviour, IHitable
{
    private static readonly int collapse = Animator.StringToHash("Collapse");
    public IntVariable criticalChance;
    public IntVariable criticalDamagePer;
    public Wakdu wakdu;
    public IntVariable exp, level;
    public IntVariable hpCur;
    [SerializeField] private IntVariable defence;
    [SerializeField] private IntVariable staticDefence;
    [SerializeField] private MaxHp hpMax;
    [SerializeField] private IntVariable powerInt;
    public TotalPower totalPower;
    public GameEvent useBaseAttack;
    public GameEvent equipWeapon;
    public IntVariable miss, reloadSpeed, bonusAmmo, skillCollBonus;
    public FloatVariable attackSpeed;
    [SerializeField] private FloatVariable moveSpeed, evasion;
    [SerializeField] private BoolVariable canEvasionOnDash;
    [SerializeField] private GameEvent onDamage, onCollapse, onLevelUp;
    public IntVariable BossDamage;
    public IntVariable MobDamage;

    public bool isHealthy;
    public WakgoodCollider wakgoodCollider;

    public Vector2 worldMousePoint;
    [SerializeField] private Weapon hochi, hand;

    public bool IsSwitching;
    public bool IsCollapsed;
    [SerializeField] private BoolVariable isFocusOnSomething;
    private Transform attackPositionParent;

    private GameObject chat;
    private TextMeshProUGUI chatText;
    private CinemachineTargetGroup cinemachineTargetGroup;

    private SpriteRenderer spriteRenderer;
    private Transform statellite;
    public static Wakgood Instance { get; private set; }
    public Transform AttackPosition { get; private set; }
    public Transform WeaponPosition { get; private set; }
    public WakgoodMove WakgoodMove { get; private set; }
    public int CurWeaponNumber { get; private set; }
    public Weapon[] Weapon { get; } = new Weapon[2];

    private void Awake()
    {
        Instance = this;
        hpMax.RuntimeValue = wakdu.baseHp;

        attackPositionParent = transform.Find("AttackPosParent");
        AttackPosition = attackPositionParent.GetChild(0);
        WeaponPosition = transform.Find("WeaponPos");
        statellite = transform.Find("SatelliteParent");
        spriteRenderer = GetComponent<SpriteRenderer>();
        wakgoodCollider = transform.GetChild(0).GetComponent<WakgoodCollider>();
        WakgoodMove = GetComponent<WakgoodMove>();
        cinemachineTargetGroup = GameObject.Find("CM TargetGroup").GetComponent<CinemachineTargetGroup>();

        chat = transform.Find("Canvas").Find("Chat").gameObject;
        chatText = chat.transform.GetChild(1).GetChild(2).GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (Time.timeScale == 0 || IsCollapsed)
        {
            return;
        }

        spriteRenderer.color = isHealthy ? Color.white : new Color(1, 1, 1, (float)100 / 255);
        if (isFocusOnSomething.RuntimeValue)
        {
            return;
        }

        spriteRenderer.flipX = transform.position.x > worldMousePoint.x;
        worldMousePoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Weapon[CurWeaponNumber].BaseAttack();
        }

        if (Input.GetMouseButtonDown(1))
        {
            Weapon[CurWeaponNumber].SpecialAttack();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Weapon[CurWeaponNumber].SkillQ();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Weapon[CurWeaponNumber].SkillE();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Weapon[CurWeaponNumber].Reload();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            wakgoodCollider.GetNearestInteractiveObject()?.Interaction();
        }

        if (Input.GetAxisRaw("Mouse ScrollWheel") != 0)
        {
            SwitchWeapon(CurWeaponNumber == 0 ? 1 : 0);
        }
        /*else if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchWeapon(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchWeapon(1);*/

        attackPositionParent.transform.rotation = Quaternion.Euler(0, 0,
            (Mathf.Atan2(worldMousePoint.y - (transform.position.y + 0.8f), worldMousePoint.x - transform.position.x) *
             Mathf.Rad2Deg) - 90);

        if (transform.position.x < worldMousePoint.x)
        {
            WeaponPosition.localScale = Vector3.one;
            WeaponPosition.localPosition = new Vector3(.3f, .5f, 0);
            WeaponPosition.rotation = Quaternion.Euler(0, 0,
                Mathf.Atan2(worldMousePoint.y - WeaponPosition.position.y,
                    worldMousePoint.x - WeaponPosition.position.x) * Mathf.Rad2Deg);
        }
        else
        {
            WeaponPosition.localScale = new Vector3(-1, 1, 1);
            WeaponPosition.localPosition = new Vector3(-.3f, .5f, 0);
            WeaponPosition.rotation = Quaternion.Euler(0, 0,
                Mathf.Atan2(WeaponPosition.position.y - worldMousePoint.y,
                    WeaponPosition.position.x - worldMousePoint.x) * Mathf.Rad2Deg);
        }
    }

    private void OnEnable()
    {
        Initialize();
    }

    public void ReceiveHit(int damage, HitType hitType = HitType.Normal)
    {
        if (IsCollapsed || !isHealthy || (WakgoodMove.MbDashing && canEvasionOnDash.RuntimeValue))
        {
            return;
        }

        if (100 / (evasion.RuntimeValue + 100) < Random.Range(0f, 1f))
        {
            RuntimeManager.PlayOneShot("event:/SFX/Wakgood/Evasion", transform.position);
            ObjectManager.Instance.PopObject("AnimatedText", transform).GetComponent<AnimatedText>()
                .SetText("ȸ��!", Color.yellow);
        }
        else
        {
            damage = (int)Math.Round(damage * (100 / (float)(defence.RuntimeValue + 100)),
                MidpointRounding.AwayFromZero);
            damage -= staticDefence.RuntimeValue;
            if (damage <= 0)
            {
                damage = 1;
            }

            RuntimeManager.PlayOneShot("event:/SFX/Wakgood/Ahya", transform.position);
            onDamage.Raise();

            if ((hpCur.RuntimeValue -= damage) > 0)
            {
                isHealthy = false;
                StartCoroutine(TtmdaclExtension.ChangeWithDelay(true, .2f, value => isHealthy = value));
            }
            else
            {
                hpCur.RuntimeValue = 0;

                if (GameManager.Instance.isRealBossing.RuntimeValue && GameManager.Instance.isRealBossFirstDeath)
                {
                    FakeCollapse();
                }
                else
                {
                    Collapse();
                }
            }
        }
    }

    public void Initialize()
    {
        StopAllCoroutines();

        hpMax.RuntimeValue = wakdu.baseHp;
        hpCur.RuntimeValue = hpMax.RuntimeValue;
        powerInt.RuntimeValue = wakdu.basePower;
        attackSpeed.RuntimeValue = wakdu.baseAttackSpeed;
        moveSpeed.RuntimeValue = wakdu.baseMoveSpeed;
        level.RuntimeValue = DataManager.Instance.CurGameData.level;
        exp.RuntimeValue = DataManager.Instance.CurGameData.exp;
        isHealthy = true;
        IsSwitching = false;
        IsCollapsed = false;

        cinemachineTargetGroup.m_Targets[0].target = transform;

        if (WeaponPosition.childCount > 0)
        {
            for (int i = 0; i < WeaponPosition.childCount; i++)
            {
                Destroy(WeaponPosition.GetChild(0).gameObject);
            }
        }

        if (Weapon[CurWeaponNumber] != null)
        {
            Weapon[CurWeaponNumber].OnRemove();
        }

        UIManager.Instance.SetWeaponUI(0, Weapon[0] = hochi);
        UIManager.Instance.SetWeaponUI(1, Weapon[1] = hand);

        foreach (Transform child in statellite)
        {
            Destroy(child.gameObject);
        }

        if (CurWeaponNumber != 0)
        {
            CurWeaponNumber = 0;
            UIManager.Instance.StartCoroutine(UIManager.Instance.SwitchWeapon());
        }

        Weapon[CurWeaponNumber].OnEquip();
        Instantiate(Weapon[CurWeaponNumber].resource, WeaponPosition);

        wakgoodCollider.enabled = true;
        WakgoodMove.enabled = true;
        WakgoodMove.StopAllCoroutines();
        WakgoodMove.Initialize();

        UIManager.Instance.SetWakduSprite(wakdu.sprite);
    }

    public void ChangeWakdu(Wakdu _wakdu)
    {
        wakdu = _wakdu;
        UIManager.Instance.SetWakduSprite(wakdu.sprite);
        WakgoodMove.Animator.runtimeAnimatorController = Instance.wakdu.controller;
        WakgoodMove.Animator.SetTrigger("WakeUp");
        WakgoodMove.Animator.SetBool("Move", false);
    }

    public void SwitchWeapon(int targetWeaponNum, Weapon targetWeapon = null)
    {
        if (IsSwitching)
        {
            return;
        }

        IsSwitching = true;
        StartCoroutine(TtmdaclExtension.ChangeWithDelay(false, .3f, value => IsSwitching = value));

        if (targetWeapon == null)
        {
            Weapon[CurWeaponNumber].OnRemove();
            for (int i = 0; i < WeaponPosition.childCount; i++)
            {
                Destroy(WeaponPosition.GetChild(0).gameObject);
            }

            CurWeaponNumber = targetWeaponNum;

            Instantiate(Weapon[CurWeaponNumber].resource, WeaponPosition);
            Weapon[CurWeaponNumber].OnEquip();

            UIManager.Instance.StartCoroutine(UIManager.Instance.SwitchWeapon());
        }
        else
        {
            if (CurWeaponNumber != targetWeaponNum)
            {
                targetWeapon.Initialize();
                Weapon[targetWeaponNum] = targetWeapon;
            }
            else
            {
                Weapon[CurWeaponNumber].OnRemove();
                for (int i = 0; i < WeaponPosition.childCount; i++)
                {
                    Destroy(WeaponPosition.GetChild(0).gameObject);
                }

                Weapon[CurWeaponNumber] = targetWeapon;

                Instantiate(Weapon[CurWeaponNumber].resource, WeaponPosition);
                Weapon[CurWeaponNumber].OnEquip();
            }

            equipWeapon.Raise();
            UIManager.Instance.SetWeaponUI(targetWeaponNum, targetWeapon);
        }
    }

    public void ReceiveHeal(int amount)
    {
        hpCur.RuntimeValue = Mathf.Clamp(hpCur.RuntimeValue + amount, 0, hpMax.RuntimeValue);
        ObjectManager.Instance.PopObject("AnimatedText", transform).GetComponent<AnimatedText>()
            .SetText(amount.ToString(), Color.green);
    }

    public void FakeCollapse()
    {
        IsCollapsed = true;

        WakgoodMove.PlayerRb.bodyType = RigidbodyType2D.Static;
        WakgoodMove.Animator.SetTrigger(collapse);
        WakgoodMove.enabled = false;
        wakgoodCollider.enabled = false;

        GameManager.Instance.StartCoroutine(GameManager.Instance.FakeEnding());
    }

    public void Collapse()
    {
        DataManager.Instance.CurGameData.deathCount++;

        StopAllCoroutines();
        WakgoodMove.StopAllCoroutines();
        ObjectManager.Instance.PopObject("Zeolite", transform);

        IsCollapsed = true;

        WakgoodMove.PlayerRb.bodyType = RigidbodyType2D.Static;
        WakgoodMove.Animator.SetTrigger(collapse);
        WakgoodMove.enabled = false;
        wakgoodCollider.enabled = false;

        GameManager.Instance.StartCoroutine(GameManager.Instance._GameOverAndRecall());
        onCollapse.Raise();
        enabled = false;
    }

    public void CheckCanLevelUp()
    {
        if (exp.RuntimeValue >= 300 * level.RuntimeValue)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        hpMax.RuntimeValue += wakdu.growthHp;
        powerInt.RuntimeValue += wakdu.growthPower;
        attackSpeed.RuntimeValue += wakdu.growthAttackSpeed;
        DataManager.Instance.CurGameData.masteryStack++;
        exp.RuntimeValue -= 300 * level.RuntimeValue;
        level.RuntimeValue++;
        onLevelUp.Raise();

        ObjectManager.Instance.PopObject("LevelUpEffect", transform);
        ObjectManager.Instance.PopObject("AnimatedText", transform).GetComponent<AnimatedText>()
            .SetText("Level Up!", Color.yellow);
    }

    public void SetRigidBodyType(RigidbodyType2D rigidbodyType2D)
    {
        WakgoodMove.PlayerRb.bodyType = rigidbodyType2D;
    }

    public IEnumerator ShowChat(string msg)
    {
        chatText.text = msg;
        chat.SetActive(true);
        yield return new WaitForSeconds(3f);
        chat.SetActive(false);
    }
}