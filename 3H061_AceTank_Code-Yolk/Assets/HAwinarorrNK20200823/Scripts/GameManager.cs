using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{

    /*
     /*遊戲管理員本腳本
     * 物件池功能(消波塊/炸彈/補血包/爆炸/閃光(恢復效果)/各式戰車/各式子彈)
     * 
     * 
     * 統計輸出傷害
     * 統計受到傷害
     * 統計命中率
     * 統計擊穿率
     * 統計子彈反彈率
     * 戰鬥歷時計算(單局，戰到HP歸0)
     * 
     * 
     * 
     * 
     * 
     * 敵人生成-強度調整(要避開重疊位子生成)
     * 障礙/炸彈/補血包生成(要避開戰車位子且夠遠)
     * 障礙/炸彈/補血包生成動畫
     * 
     * 終場成績加權、加總、自動截圖
     * 玩家射擊鼠標、瞄準雷射
     * 控制UI顯示玩家詳細資訊
     */
    /*
     * 




    
     * 
     
     宣告 玩家腳本 玩家實例     
     */
    [Header("開局資料設定")]
    public string GAMESETTING_GamePlayerName;               //存取玩家名稱以設定與判定計算資料
    public int GAMESETTING_LevelStartNum;                   //開局難度設定//由按鈕給值，並在此start()存取
    public static GameManager instance_ThisScript;

    //金手指模式設定參數
    private string GoldFinger_Name = "HAWINARORRNK";
    private float GoldFinger_ShellAtk = 1250f;
    private float GoldFinger_ShellReload = 1f;
    private float GoldFinger_ArmorTrible = 2f;
    private float GoldFinger_Penetration = 52f;
    public Sprite GoldFinger_ShellSprite;
    public Color GoldFinger_TankColor = Color.white;             //金手指開啟時的車子顏色(以利視覺化
    private float GoldFinger_ObjGeneNumTime = 3f;

    private float set_Normal_ObjGeneNumTime = 1f;         //場景物品生成倍數

    [Header("NPC_FSM存取參數")]                           //方便全AI共用存取，毋須每個AI都執行搜尋
    public GameObject GAMESETTING_GamePlayer;             //玩家物件
    public GameObject GAMESETTING_GamePlayerHull;         //玩家子物件_車身
    public GameObject GAMESETTING_GamePlayerTurrent;      //玩家子物件_砲塔
    public GameObject GAMESETTING_GamePlayerGun;          //玩家子物件_砲管

    [Header("連結UI物件與參數")]
    public Image set_ICON_SkillIcon;             //場景介面_修復圖示
    public Text set_ICON_SkillText;              //場景介面_修復圖示之文字
    public Color set_ICON_SkillText_colorNormal; //場景介面_修復圖示之文字顏色
    public Image set_ICON_HPbar;      //場景介面_玩家血條
    public Color[] set_ICON_HPbar_colorStage = new Color[3];     //血量變化階段-顏色
    public float[] set_ICON_HPbar_thresholdRatio = new float[2]; //血量變化階段-門檻值
    public Color set_ICON_colorFlash;                      //血條顏色動畫閃爍顏色
    public float set_ICON_animSpeed;                       //血量變化動畫速度
    public Text set_WIN_Clock;     //視窗介面_戰鬥計時
    public Text set_WIN_HPratio;   //視窗介面_玩家血量比
    public Text set_WIN_DMGstat;   //視窗介面_傷害統計
    public Text set_WIN_KILstat;   //視窗介面_擊殺統計
    public Text set_WIN_PlayerNameDisplay; //視窗介面-設定玩家名





    //預製物
    [Header("使用預製物")]
    public GameObject set_prefab_Tetrapods;        //消波塊預製物
    public GameObject set_prefab_MedicalKit;       //醫藥包預製物   
    public GameObject set_prefab_Bomb;             //炸彈預製物
    public GameObject set_prefab_Shell;            //子彈預製物
    public GameObject set_prefab_VFXshockwave;     //震波特效預製物(發砲砲口)
    public GameObject set_prefab_VFXflash;         //閃光特效預製物(物品閃現/獲得治療)
    public GameObject set_prefab_VFXexplosion_AP;  //爆炸特效預製物(穿甲彈)
    public GameObject set_prefab_VFXexplosion_HE;  //爆炸特效預製物(高爆彈)
    public GameObject set_prefab_VFXexplosion_Obj; //爆炸特效預製物(物件壽限消滅：包含子彈/消波塊/醫藥包/炸彈)
    public GameObject set_prefab_VFXexplosion_Bomb;//爆炸特效預製物(炸彈範圍式爆炸)
    public GameObject set_prefab_WheelRepairIcon;  //浮動UI_輪子維修圖示預製物
    public GameObject set_prefab_HPnumText;        //浮動UI_傷害/補血數字預製物
    public GameObject set_prefab_OEIarrow;         //浮動UI_屏外敵人箭頭預製物


    public List<SpriteRenderer> set_lightOfGeneDock;   //戰車生成點光照(提示即將升成)
    public List<GameObject> set_prefab_Tank;           //各種戰車預製物
    public List<string> set_prefab_TankName;               //各種戰車預製物名稱
    public enum TankActType
    {
        MediumTank,
        HeavyTank,
        SuperHeavyTank,
    }
    public List<TankActType> set_prefab_TankActType;   //各種戰車預製物類型標籤


    [Header("使用物件池")] //因為Queue無法在Inspector賦值，故需在此給值
    public static Queue<GameObject> set_objectPool_Tetrapods;                                  //消波塊
    public static Queue<GameObject> set_objectPool_MedicalKit;                                 //醫藥包
    public static Queue<GameObject> set_objectPool_Bomb;                                       //炸彈
    public static Queue<GameObject> set_objectPool_Shell;                                      //子彈
    public static Queue<GameObject> set_objectPool_VFXshockwave;                               //震波特效
    public static Queue<GameObject> set_objectPool_VFXflash;                                   //閃光特效
    public static Queue<GameObject> set_objectPool_VFXexplosion_AP;                            //爆炸特效(穿甲彈)
    public static Queue<GameObject> set_objectPool_VFXexplosion_HE;                            //爆炸特效(高爆彈)
    public static Queue<GameObject> set_objectPool_VFXexplosion_Obj;                           //爆炸特效(物件壽限消滅)
    public static Queue<GameObject> set_objectPool_VFXexplosion_Bomb;                          //爆炸特效(炸彈範圍式爆炸)
    public static List<Queue<GameObject>> set_objectPool_Tank;                                 //各種戰車[] //單獨一種創建一池 //需照設定強度排序(遞增)
    public static Queue<GameObject> set_objectPool_WheelRepairIcon;                            //浮動UI_輪子維修圖示 
    public static Queue<GameObject> set_objectPool_prefab_HPnumText;                           //浮動UI_傷害/補血數字預製物
    public static Queue<GameObject> set_objectPool_OEIarrow;                                   //浮動UI_屏外敵人箭頭預製物

    [Header("生成範圍與間距設定")]
    public int addVolume_PerOnce;                    //物件池單次擴充
    public Vector2 set_geneObjBorder_CornerPos;      //生成戰車-生成點X/Y值                       //共有(x,0,-x)*(y,-y)六個生成點
    public float set_geneTankBorder_BufferWidth;
    public float set_geneTimeGap_Tank;               //生成戰車間隔時間
    public float set_geneTimpGap_Obj;                //生成物品間隔時間
    public MobileGeneDetector set_geneDet_Obj;       //生成範圍探測器
    public MobileGeneDetector set_geneDet_Tank;

    [Header("難度調整階段參數")]                    //注意此四個陣列長度務必相同
    public int[] set_levelDiff_EnemyNumMax;         //難度階段 - 敵人數量上限數
    public Vector2[] set_levelDiff_EnemyTypeRange;  //難度階段 - 敵人種類範圍//(預製物陣列編號下限,上限)
    public int[] set_levelRef_PlayerKillNum;        //難度階段變化參考變數 - 敵人擊破數
    public float[] set_LevelRef_PlayTimeLength;     //難度階段變化參考變數 - 玩家存活時間



    //系統運算變數
    public bool isPaused; //是否被暫停
    public bool isPlayerDead;             //玩家遭擊破(被動判定)   
    bool isGene_Obj_ing;             //生成未完成則不再接收新的生成指令
    bool isGene_TankNPC_ing;           //生成未完成則不再接收新的生成指令


    public List<Vector2> geneTank_PointPos = new List<Vector2>();//生成位置陣列
    int updateLevelNum_NumMaxWithTimeLength;     //即時難度階段編號 - 敵人數量上限數
    int updateLevelNum_TypeRangeWithKillNum;     //即時難度階段編號 - 敵人種類範圍

    //統計累加參數    
    public float stat_sysPlayTimeLength;      //遊玩戰鬥時間長度 (配合難度調整與結算)
    public int stat_sysGeneNum; //生成累加數             //取出時使用,
    public int stat_sysKillNum; //消滅累加數             //不限制消滅原因
    public int stat_playerKillNum_MT, stat_playerKillNum_HT, stat_playerKillNum_SHT; //被消滅中坦數, 被消滅數重坦數, 被消滅數超重坦數 (僅供結算展示)
    public float stat_playerTotalAttack;  //玩家全程命中總輸出
    public float stat_playerTotalDamage;  //玩家全程承受總傷害
    public int stat_playerHitNum;  //玩家擊中數
    public int stat_playerFireNum; //玩家射擊數

    //UI外接參數
    public float stat_playerUpdateHP; //玩家即時血量資料
    public float stat_playerSetHP;    //玩家設定血量資料
    float ICON_HPbar_AnimHPratio = 1f;//變動中血條值
    public float SkillIcon_CDpercent;
    public bool isRepairSkill_Anim;





    private void Awake()
    {
        if (instance_ThisScript != null)
        {
            Destroy(gameObject);
            return;
        }
        instance_ThisScript = this;
        GAMESETTING_CHECK();
    }

    void Start()
    {
        Start_SceneUI_StartIntialize(); //初始化血條參數

        isPlayerDead = false;
        ObjectPoolIntialize();

        ClearStats();                 //難度初始設置函式()
        DefineGeneTankPosAndPool();   //戰車生成區域定義()
    }

    private void FixedUpdate() //功能置入於此，使可被暫停
    {
        Update_SceneUIs_andClock();
        //Debug.Log(isPaused);
        if (isPaused == true)
        {
            return;
        }
        else if (isPaused == false)
        {
            UpdateLevelDiff();
            AutoGeneObjects();
            AutoGeneTank();
        }
    }

    //物件池相關區塊---------------------------------------------------------------------------------------------
    //通用池子加量函式(使用者須小心輸入值)
    private void ObjectPool_AddVolume(GameObject ref_Prefab, Queue<GameObject> addOn_Pool)
    {
        for (int i = 0; i < addVolume_PerOnce; i++)
        {
            GameObject addPrefab = Instantiate(ref_Prefab);
            addPrefab.SetActive(false);
            addOn_Pool.Enqueue(addPrefab);
        }
    }

    //通用取出池子函式(使用者須小心輸入值)
    public static GameObject ObjectPool_TakeFrom(GameObject take_Prefab, Queue<GameObject> fromThePool)
    {
        if (fromThePool.Count == 0)
        {

            instance_ThisScript.ObjectPool_AddVolume(take_Prefab, fromThePool);
        }
            var prefab_TakeFromPool = fromThePool.Dequeue();
            prefab_TakeFromPool.SetActive(true);

            return prefab_TakeFromPool;
    }

    //通用回歸池子函式(使用者須小心輸入值)
    public static void ObjectPool_ReturnTo(GameObject return_Prefab, Queue<GameObject> toThePool)
    {
        return_Prefab.SetActive(false);
        toThePool.Enqueue(return_Prefab);
    }



    //初始化相關區塊---------------------------------------------------------------------------------------------
    private void GAMESETTING_CHECK() //遊戲最開始執行的函式與參數設定
    {
        //存取前場景設定的難度與ID
        GAMESETTING_GamePlayerName = PlayerPrefs.GetString("SAVED_GAMESETTING_ID",
            GetComponent<UI_ButtonList>().defaultGAMESETTING_playerName);

        GAMESETTING_LevelStartNum = PlayerPrefs.GetInt("SAVED_GAMESETTING_DIFF",
            GetComponent<UI_ButtonList>().set_S1_setDiff);

        GAMESETTING_LevelStartNum = Mathf.Clamp(GAMESETTING_LevelStartNum, 0, set_LevelRef_PlayTimeLength.Length - 1);

        updateLevelNum_NumMaxWithTimeLength = GAMESETTING_LevelStartNum;
        updateLevelNum_TypeRangeWithKillNum = GAMESETTING_LevelStartNum;

        GAMESETTING_GamePlayer = GameObject.FindObjectOfType<PlayerControlInput>().gameObject;
        GAMESETTING_GamePlayerHull = GAMESETTING_GamePlayer.transform.Find("Part_Hull").gameObject;
        GAMESETTING_GamePlayerTurrent = GAMESETTING_GamePlayerHull.transform.Find("Part_Turrent").gameObject;
        GAMESETTING_GamePlayerGun = GAMESETTING_GamePlayerTurrent.transform.Find("Part_Gun").gameObject;
        


        //檢查生成戰車種類陣列長度，限制隨機的編號上限
        //Debug.Log(updateLevelNum_NumMaxWithTimeLength);

        //如果輸入ID=金手指ID，開啟金手指(自動轉換成全大寫)
        if (GAMESETTING_GamePlayerName.ToUpper() == GoldFinger_Name.ToUpper()) 
        {
            //調高攻擊力
            GAMESETTING_GamePlayer.GetComponent<GenericTankController>().set_shell_Damage = GoldFinger_ShellAtk;
            //改變車子裝甲厚度3倍
            GAMESETTING_GamePlayer.GetComponent<GenericTankController>().set_armor_Front *= GoldFinger_ArmorTrible;
            GAMESETTING_GamePlayer.GetComponent<GenericTankController>().set_armor_Rear *= GoldFinger_ArmorTrible;
            GAMESETTING_GamePlayer.GetComponent<GenericTankController>().set_armor_Side *= GoldFinger_ArmorTrible;
            //改變裝填速度
            GAMESETTING_GamePlayer.GetComponent<GenericTankController>().set_gun_ReoladSetTime = GoldFinger_ShellReload;
            //改變子彈穿深
            GAMESETTING_GamePlayer.GetComponent<GenericTankController>().set_shell_Penetrate = GoldFinger_Penetration;
            //改變子彈屬性為HE彈
            GAMESETTING_GamePlayer.GetComponent<GenericTankController>().set_shell_Type = GenericObjectDamageSetting.DamageType.HE;
            //改變子彈圖案
            GAMESETTING_GamePlayer.GetComponent<GenericTankController>().set_shell_Icon = GoldFinger_ShellSprite;
            //改變車子與子彈顏色
            GAMESETTING_GamePlayer.GetComponent<GenericTankController>().set_allPartColor = GoldFinger_TankColor;
            GAMESETTING_GamePlayerHull.GetComponent<SpriteRenderer>().color = GoldFinger_TankColor;
            GAMESETTING_GamePlayerTurrent.GetComponent<SpriteRenderer>().color = GoldFinger_TankColor;
            GAMESETTING_GamePlayerGun.GetComponent<SpriteRenderer>().color = GoldFinger_TankColor;
            //改變物品生成數量
            set_Normal_ObjGeneNumTime = GoldFinger_ObjGeneNumTime;
        }

    }

    private void ObjectPoolIntialize() //重讀場景必須使用，否則回傳報錯
    {
        set_objectPool_Tetrapods = new Queue<GameObject>();        //消波塊
        set_objectPool_MedicalKit = new Queue<GameObject>();       //醫藥包
        set_objectPool_Bomb = new Queue<GameObject>();             //炸彈
        set_objectPool_Shell = new Queue<GameObject>();            //子彈
        set_objectPool_VFXshockwave = new Queue<GameObject>();     //震波特效
        set_objectPool_VFXflash = new Queue<GameObject>();         //閃光特效
        set_objectPool_VFXexplosion_AP = new Queue<GameObject>();  //爆炸特效(穿甲彈)
        set_objectPool_VFXexplosion_HE = new Queue<GameObject>();  //爆炸特效(高爆彈)
        set_objectPool_VFXexplosion_Obj = new Queue<GameObject>(); //爆炸特效(物件壽限消滅)
        set_objectPool_VFXexplosion_Bomb = new Queue<GameObject>();//爆炸特效(炸彈範圍式爆炸)
        set_objectPool_Tank = new List<Queue<GameObject>>();       //各種戰車[]
        set_objectPool_WheelRepairIcon = new Queue<GameObject>();  //浮動UI_輪子維修圖示 
        set_objectPool_prefab_HPnumText = new Queue<GameObject>(); //浮動UI_傷害/補血數字預製物
        set_objectPool_OEIarrow = new Queue<GameObject>();         //浮動UI_屏外敵人箭頭預製物
    }

    void ClearStats() //開場清空成績統計
    {
        stat_sysPlayTimeLength = 0;
        stat_sysGeneNum = 0;
        stat_sysKillNum = 0;
        stat_playerKillNum_MT = 0;
        stat_playerKillNum_HT = 0;
        stat_playerKillNum_SHT = 0;
        stat_playerTotalAttack = 0;
        stat_playerTotalDamage = 0;
        stat_playerHitNum = 0;
        stat_playerFireNum = 0;
    }
    void DefineGeneTankPosAndPool() //開場定義戰車生成點與物件池數量*/
    {
        geneTank_PointPos.Add(new Vector2(-(set_geneObjBorder_CornerPos.x + set_geneTankBorder_BufferWidth), set_geneObjBorder_CornerPos.y));
        geneTank_PointPos.Add(new Vector2(-(set_geneObjBorder_CornerPos.x + set_geneTankBorder_BufferWidth), 0f));
        geneTank_PointPos.Add(new Vector2(-(set_geneObjBorder_CornerPos.x + set_geneTankBorder_BufferWidth), -set_geneObjBorder_CornerPos.y));
        geneTank_PointPos.Add(new Vector2((set_geneObjBorder_CornerPos.x + set_geneTankBorder_BufferWidth), set_geneObjBorder_CornerPos.y));
        geneTank_PointPos.Add(new Vector2((set_geneObjBorder_CornerPos.x + set_geneTankBorder_BufferWidth), 0f));
        geneTank_PointPos.Add(new Vector2((set_geneObjBorder_CornerPos.x + set_geneTankBorder_BufferWidth), -set_geneObjBorder_CornerPos.y));

        for (int i = 0; i < set_prefab_Tank.Count; i++)
        {
            set_objectPool_Tank.Add(new Queue<GameObject>());
        }

    }

    //場景-血量條動畫初始化
    void Start_SceneUI_StartIntialize()
    {
        //場景血條
        ICON_HPbar_AnimHPratio = 1f;
        set_ICON_HPbar.color = set_ICON_HPbar_colorStage[0];
        set_ICON_HPbar.fillAmount = ICON_HPbar_AnimHPratio;

        //場景技能攔
        isRepairSkill_Anim = true;
        set_ICON_SkillIcon.fillAmount = SkillIcon_CDpercent;
    }
    /*
    [Header("連結UI物件與參數")]
    public Image set_ICON_RepairIcon; //場景介面_修復圖示
    public Image set_ICON_HPbar;      //場景介面_玩家血條
    public Text set_WIN_Clock;     //視窗介面_戰鬥計時
    public Text set_WIN_HPratio;   //視窗介面_玩家血量比
    public Text set_WIN_DMGstat;   //視窗介面_傷害統計
    public Text set_WIN_KILstat;   //視窗介面_擊殺統計

     */

    //場景介面相關區塊---------------------------------------------------------------------------------------------
    private void Update_SceneUIs_andClock() //場景UI與戰鬥計時的資訊更新 (區分更新區塊以減少更新工作量)
    {
        //if (isPaused) { return; } //暫停模式停止更新介面

        if (isPaused == false) //暫停時停止計時
        {
            //視窗-時鐘
            Update_PageUI_WIN_Clock();
        }

        //場景-血量條動畫
        Update_SceneUI_ICON_HPbar();
        //場景-維修圖示動畫
        Update_SceneUI_ICON_Skill();
    }

        
    //場景-血量條動畫即時化
    private void Update_SceneUI_ICON_HPbar()
    {
        var HPratio = stat_playerUpdateHP / stat_playerSetHP;
        if (ICON_HPbar_AnimHPratio == HPratio) //如果發現血量無變化，不進行動畫
        {
            return;
        }
        //依照顏色變化stageColo值
        var stageColor = set_ICON_HPbar_colorStage[0];
        if (ICON_HPbar_AnimHPratio >= set_ICON_HPbar_thresholdRatio[0])
        {
            stageColor = set_ICON_HPbar_colorStage[0];
        }
        else if (ICON_HPbar_AnimHPratio >= set_ICON_HPbar_thresholdRatio[1])
        {
            stageColor = set_ICON_HPbar_colorStage[1];
        }
        else
        {
            stageColor = set_ICON_HPbar_colorStage[2];
        }

        //漸變物件填充值
        ICON_HPbar_AnimHPratio = Mathf.MoveTowards(ICON_HPbar_AnimHPratio, HPratio, set_ICON_animSpeed * Time.fixedDeltaTime / 100);
        set_ICON_HPbar.fillAmount = ICON_HPbar_AnimHPratio;

        //閃爍物件顏色
        var BarAnimColor = Mathf.PingPong(Time.timeSinceLevelLoad * set_ICON_animSpeed, 1);
        set_ICON_HPbar.color = Vector4.Lerp(stageColor, set_ICON_colorFlash, BarAnimColor);

        if (ICON_HPbar_AnimHPratio == HPratio) //如果發現血量到達目標值
        {
            set_ICON_HPbar.color = stageColor;      //血條顏色套用stageColo值
        }    
    }
    
    //維修技能圖示動畫即時化
    private void Update_SceneUI_ICON_Skill()
    {
        //動畫開關啟用時才可進行動畫
        if (isRepairSkill_Anim==false) { return; }

        set_ICON_SkillIcon.fillAmount = SkillIcon_CDpercent;       


        //閃爍物件顏色
        set_ICON_SkillText.text = "CD...";
        var TextAnimColor = Mathf.PingPong(Time.timeSinceLevelLoad * set_ICON_animSpeed, 1);
        set_ICON_SkillText.color = Vector4.Lerp(set_ICON_SkillText_colorNormal, set_ICON_colorFlash, TextAnimColor);

        if (SkillIcon_CDpercent == 1f)
        {
            set_ICON_SkillText.color = set_ICON_SkillText_colorNormal;
            set_ICON_SkillText.text = "Space";
            isRepairSkill_Anim = false;
        }
    }

    //視窗介面相關區塊---------------------------------------------------------------------------------------------
    public void Update_PageUIs_PausePage() //暫停頁面的UI資訊更新(應僅限暫停時使用，可按的按鈕交由UI_ButtonList腳本提供)
    {
        //視窗-時鐘
        Update_PageUI_WIN_Clock();

        //視窗-血量比
        set_WIN_HPratio.text = "HP : " + stat_playerUpdateHP.ToString("0000") + "/" + stat_playerSetHP.ToString("0000");
        //視窗-傷害統計
        set_WIN_DMGstat.text = "Fire Damage : " + stat_playerTotalAttack.ToString("0000") + "\r\n" + "Bear Damage : " + stat_playerTotalDamage.ToString("0000");
        //視窗-擊殺統計
        set_WIN_KILstat.text = "-kill Stats - Total:" + (stat_playerKillNum_MT + stat_playerKillNum_HT + stat_playerKillNum_SHT).ToString("00") + "\r\n" +
            "(M:" + stat_playerKillNum_MT.ToString("00") + " / H:" + stat_playerKillNum_HT.ToString("00") + " / SH:" + stat_playerKillNum_SHT.ToString("00") + ")";
        //視窗-玩家名稱設定
        set_WIN_PlayerNameDisplay.text = "Tanker:" + GAMESETTING_GamePlayerName.ToString();
    }

    private void Update_PageUI_WIN_Clock() //暫停時不得算入，這樣暫停後的敵人生成強度才能延續
    {
        //stat_playTimeLength = Time.timeSinceLevelLoad;
        stat_sysPlayTimeLength += Time.fixedDeltaTime;
        int clockMinute = (int)(stat_sysPlayTimeLength / 60);
        float clockSecond = (int)(stat_sysPlayTimeLength % 60);
        set_WIN_Clock.text = "BATTLE TIME " + clockMinute.ToString("00") + ":" + clockSecond.ToString("00");
    }



    //難度調整相關區塊---------------------------------------------------------------------------------------------
    void UpdateLevelDiff() // 難度自動調整函式()    //依據設置結果調整參考難度陣列編號
    {
        if ((updateLevelNum_NumMaxWithTimeLength == set_LevelRef_PlayTimeLength.Length - 1)
            && (updateLevelNum_TypeRangeWithKillNum == set_levelRef_PlayerKillNum.Length - 1))
        {
            return;
        }


        var currentLevel_PTL = updateLevelNum_NumMaxWithTimeLength;
        var nextLevel_PTL = Mathf.Clamp(updateLevelNum_NumMaxWithTimeLength + 1, 0, set_LevelRef_PlayTimeLength.Length - 1);

        var currentLevel_PKN = updateLevelNum_TypeRangeWithKillNum;
        var nextLevel_PKN = Mathf.Clamp(updateLevelNum_TypeRangeWithKillNum + 1, 0, set_levelRef_PlayerKillNum.Length - 1);
                

        //難度調整要減去開局設定門檻
        //如果玩家存活夠久，則增加場上敵人數量上限
        if ((stat_sysPlayTimeLength - set_LevelRef_PlayTimeLength[currentLevel_PTL])
            > (set_LevelRef_PlayTimeLength[nextLevel_PTL] - set_LevelRef_PlayTimeLength[currentLevel_PTL]))
        {
            updateLevelNum_NumMaxWithTimeLength = Mathf.Clamp(updateLevelNum_NumMaxWithTimeLength + 1, 0, set_LevelRef_PlayTimeLength.Length - 1);
            //Debug.Log("數量上限難度=" + updateLevelNum_NumMaxWithTimeLength);
        }
        //如果玩家擊殺數夠多，則更換新生成的敵人種類
        if ((stat_playerKillNum_MT + stat_playerKillNum_HT + stat_playerKillNum_SHT - set_levelRef_PlayerKillNum[currentLevel_PKN])
            > (set_levelRef_PlayerKillNum[nextLevel_PKN] - set_levelRef_PlayerKillNum[currentLevel_PKN]))
        {
            updateLevelNum_TypeRangeWithKillNum = Mathf.Clamp(updateLevelNum_TypeRangeWithKillNum + 1, 0, set_levelRef_PlayerKillNum.Length - 1);
            //Debug.Log("種類隨機難度=" + updateLevelNum_TypeRangeWithKillNum);
        }
    }

    //生成功能相關區塊---------------------------------------------------------------------------------------------
    int objStat_GeneNum;
    public int objStat_DestroyNum;
    //偵測位置周遭，令物品出現保持夠遠距離
    //隨機出現物品並隨難度限制數量
    void AutoGeneObjects() //判斷何時生成物品 //隨機種類隨機位置
    {
        int fieldObjNum = objStat_GeneNum - objStat_DestroyNum;
        if (isGene_Obj_ing == true)  //場上物品存在數
        {
            return;        
        }

        //如果物品小於人數上限倍數且未在生成中，即可定時補充
        if (fieldObjNum < set_Normal_ObjGeneNumTime*set_levelDiff_EnemyNumMax[updateLevelNum_NumMaxWithTimeLength] && isGene_Obj_ing == false) 
        {
            isGene_Obj_ing = true;
            int randomCode = (int)Random.Range(1, 100 + 1);
            int obj_GeneType = 1;
            if (randomCode < 21) { obj_GeneType = 1; }
            else if (randomCode < 51) { obj_GeneType = 2; }
            else { obj_GeneType = 3; }

            switch (obj_GeneType) 
            {
                case 1:
                    ObjGene_PosCheck(set_prefab_Tetrapods, set_objectPool_Tetrapods);
                    break;
                case 2:
                    ObjGene_PosCheck(set_prefab_MedicalKit, set_objectPool_MedicalKit);
                    break;
                case 3:
                    ObjGene_PosCheck(set_prefab_Bomb, set_objectPool_Bomb);
                    break;
            }            
        }

    }

    private void ObjGene_PosCheck(GameObject prefab, Queue<GameObject> objectPool) 
    {
        //每次接收生成指令，隨機變動檢測器位置
        Vector2 genePos = Vector2.zero;
        genePos.x = Random.Range(-set_geneObjBorder_CornerPos.x, set_geneObjBorder_CornerPos.x);
        genePos.y = Random.Range(-set_geneObjBorder_CornerPos.y, set_geneObjBorder_CornerPos.y);
        set_geneDet_Obj.transform.position = genePos;

        StartCoroutine(ObjGene_ResultDecide(prefab, objectPool));
    }
    IEnumerator ObjGene_ResultDecide(GameObject prefab, Queue<GameObject> objectPool) 
    {
        yield return new WaitForSeconds(0.2f);         //等待檢測器1秒之後再看碰撞判定
        if (set_geneDet_Obj.is_detected == true)
        {
            isGene_Obj_ing = false;                    //該位置已有東西，重啟生成指令
        }
        else if (set_geneDet_Obj.is_detected == false) //表示該位置無東西；並過數秒才生成，完成後重啟生成指令
        {
            objStat_GeneNum += 1;
            yield return new WaitForSeconds(set_geneTimpGap_Obj);
            var InstanceOfObject = ObjectPool_TakeFrom(prefab, objectPool);
            InstanceOfObject.transform.position = set_geneDet_Obj.transform.position;
            //InstanceOfObject.transform.rotation = Quaternion.identity;
            isGene_Obj_ing = false;
        }
    }

    void AutoGeneTank() //判斷何時生成戰車與其種類、位置
    {
        int fieldEnemyNum = stat_sysGeneNum - stat_sysKillNum;         //場上存活敵人數
        if (isGene_TankNPC_ing == true) 
        {
            return;
        }
        
        if (fieldEnemyNum < set_levelDiff_EnemyNumMax[updateLevelNum_NumMaxWithTimeLength] && isGene_TankNPC_ing == false)  //如果敵人數小於上限且未在生成中，即可定時補充
        {
            isGene_TankNPC_ing = true; //開啟生成中狀態
            //補充種類依照難度調整隨機選取可選種類
            int code_GeneType = (int)Random.Range(set_levelDiff_EnemyTypeRange[updateLevelNum_TypeRangeWithKillNum].x, set_levelDiff_EnemyTypeRange[updateLevelNum_TypeRangeWithKillNum].y+1);            
            int code_GenePos = Random.Range(0, geneTank_PointPos.Count);

            TankGene_PosCheck(code_GeneType, code_GenePos);
        }
    }

    private void TankGene_PosCheck(int prefabCode, int posCode)
    {
        //每次接收生成指令，隨機變動檢測器位置
        Vector2 pos = geneTank_PointPos[posCode];
        set_geneDet_Tank.transform.position = pos;

        StartCoroutine(TankGene_ResultDecide(prefabCode, posCode));
    }
    IEnumerator TankGene_ResultDecide(int prefabCode, int posCode)
    {
        yield return new WaitForSeconds(1f);              //等待檢測器1秒之後再看碰撞判定，並關閉檢測器
        if (set_geneDet_Tank.is_detected == true)
        {
            isGene_TankNPC_ing = false;                     //該位置已有東西，重啟生成指令
        }
        else if (set_geneDet_Tank.is_detected == false)     //表示該位置無東西；並過數秒才生成，完成後才重啟生成指令
        {
            stat_sysGeneNum += 1;                                                                                  //生成數加1放在此處，避免物件池建立時由預製物多加
            yield return new WaitForSeconds(set_geneTimeGap_Tank);
            var instanceTank = GameManager.ObjectPool_TakeFrom(set_prefab_Tank[prefabCode], set_objectPool_Tank[prefabCode]);
            instanceTank.transform.position = geneTank_PointPos[posCode];
            instanceTank.GetComponent<GenericTankController>().set_user_ID = set_prefab_TankName[prefabCode] + stat_sysGeneNum.ToString(); //加上編號，才不會同類車都共用id
            instanceTank.GetComponent<AI_GenericSetting>().GMset_IndexOfTankPrefab_InGM = prefabCode;               //設定預製物編號，以利回歸物件池
            instanceTank.GetComponent<AI_GenericSetting>().GMset_ActType_InGM = set_prefab_TankActType[prefabCode]; //設定車子類型，以利統計資料收集

            if (posCode < 3)
            {
                instanceTank.transform.Find("Part_Hull").gameObject.transform.rotation = Quaternion.Euler(0, 0, -90f); //如果生成位置在左側，面朝右 //存取子物件位置需用transform函數
            }
            else
            {
                instanceTank.transform.Find("Part_Hull").gameObject.transform.rotation = Quaternion.Euler(0, 0, 90f); //如果生成位置在右側，面朝左 //存取子物件位置需用transform函數
            }
            StartCoroutine(AutoGene_OEI_callForTankGene(instanceTank, GAMESETTING_GamePlayer));
            isGene_TankNPC_ing = false;
        }
    }


    //OEI =  OffscreenEnemyIindicator
    //此協程需在
    public IEnumerator AutoGene_OEI_callForTankGene(GameObject refNPC, GameObject refPlayer)
    {
        yield return new WaitForSeconds(0.5f);
        GameObject instance_OEIarrow = GameManager.ObjectPool_TakeFrom(set_prefab_OEIarrow, set_objectPool_OEIarrow);
        instance_OEIarrow.GetComponent<UI_S2_OffscreenEnemyIndicator>().GMset_refNPC = refNPC;
        instance_OEIarrow.GetComponent<UI_S2_OffscreenEnemyIndicator>().GMset_refPlayer = refPlayer;
    }

}
