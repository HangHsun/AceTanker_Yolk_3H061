using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControlInput : MonoBehaviour
{
    /*玩家腳本
     * 操作鍵連結控制
     * 快速修復計時
     * 為子彈預製物 *  
     * 紀錄接收*/

    PlayerControlInput instance_PlayerInput;            //宣告實例：玩家輸入;singleton，跨場景"可"破壞
    GenericTankController instance_TankController;      //宣告實例：戰車控制面板;

    [Header("玩家設定參數")]
    //玩家操控才會用到之值
    public Camera playerSet_FollowCamera;         //跟隨相機
    public float playerSet_ScrollSensitive;       //滑鼠滾輪感應延遲
    
    
    [Header("技能相關參數")]
    public float playerSet_Skill_CDtime;          //修復能量時間長度    
    public float playerSet_repairHPvalue;         //單次修復HP幅度

    [Header("玩家專用視覺軌跡")]
    public TrailRenderer[] setTrackTrails;        //掛載履帶拖痕
    public float playerSet_TT_StopEmitSensitive;  //履帶拖痕停止計時

    //玩家控制參數
    bool is_Skill_CD;                           //當使用技能後即進入cd時間
    float updateCount_skill_CDtime;             //即使倒數的cd時間

    //系統演算項目
    float scroll_twitchValue;                   //滑鼠滾輪感應值
    float scroll_twitchToZeroCountTime;         //滑鼠滾輪感應延遲計時(系統演算)
    bool getkey_IsOriginalRotate_Holding;       //操作是否維持自動迴轉


    float trackTrail_EmitCloseToZeroCountTime;    //履帶拖痕停止延遲計時(系統演算)
    bool isAlreadyEmit;                           //是否已經開啟拖痕(不再重複開啟)
    Vector3 lastUpdate_gameOnjectPosition;        //紀錄上幀玩家位置(位置有變動才開啟拖尾)

    //存取戰車聲效的播放建立器，方便使用(也利於初期設置抱錯確認)
    private GenericTankAudioBuilder usingTankAudio;

    private void Awake()
    {
        if (instance_PlayerInput != null) 
        {
            Destroy(gameObject);
            return;
        }
        instance_PlayerInput = this;
    }

    void Start()
    {
        //存取戰車聲效的播放建立器，方便使用(也利於初期設置抱錯確認)
        usingTankAudio = gameObject.GetComponent<GenericTankAudioBuilder>();

        updateCount_skill_CDtime = playerSet_Skill_CDtime; //開場即時修復能量全滿;
        is_Skill_CD = false;                               //並且未消耗可立即使用;
        instance_TankController = gameObject.GetComponent<GenericTankController>();//抓取車輛控制器

        //接收GM設定腳本玩家命名並傳給控制器(注意執行序)
        instance_TankController.set_user_ID = GameManager.instance_ThisScript.GAMESETTING_GamePlayerName; 

        GameManager.instance_ThisScript.stat_playerSetHP = instance_TankController.set_Hull_HP;   //將控制器設定車子滿血量傳給GM
        GameManager.instance_ThisScript.stat_playerUpdateHP = instance_TankController.updateHP_Hull;   //將控制器設定車子滿血量傳給GM
        GameManager.instance_ThisScript.SkillIcon_CDpercent = 1f;

        //for (int i = 0; i < setTrackTrails.Length; i++) { }

    }
    
    // Update is called once per frame
    void Update()
    {
        if (GameManager.instance_ThisScript.isPlayerDead == false) 
        {
            //玩家快捷鍵暫停/繼續遊戲
            PlayerQuickKey_PauseAndContinue();
        }

        //如果遊戲暫停或玩家陣亡，停止戰車控制與統計傳送
        if (GameManager.instance_ThisScript.isPaused == true||
            GameManager.instance_ThisScript.isPlayerDead==true)
        { return; }

        PlayerSkill_UseRepairEnergy();
        PlayerInputToTankController();
        PlayerMessageToGM();
    }

    private void FixedUpdate()
    {
        //限玩家操控車輛才有履帶拖尾，故放於此
        TrackTrailRenderer_EmitSwitch();
    }

    /*
    //通用控制面板由外部實時輸入項目
    Vector2 input_MoveDirVector; //移動輸入向量值
    Vector2 input_TargetPosition; //瞄準目標輸入位置
    float input_TwitchState; //探戈狀態移動輸入值(1前進；0停止；-1後退)
    bool input_isFireShell; //是否開火
    bool input_IsLockTurrent; //是否鎖住砲塔
    bool input_OriginalRotate_Right; //是否原地旋轉(右轉
    bool input_OriginalRotate_Left; //是否原地旋轉(左轉
    */

    public void PlayerMessageToGM()//傳送資料至GM實例
    {
        //如果有按出有效射擊，修改GM腳本統計：開火次數
        if (instance_TankController.input_isFireShell && !instance_TankController.isReloading && Input.GetKeyDown(KeyCode.Mouse0))
        {
            GameManager.instance_ThisScript.stat_playerFireNum += 1; 
        }

        //如果有受到任何損傷，修改GM腳本統計：承受傷害與目前血量；並且清空控制器相關暫存 (受傷才存取，避免每幀頻繁存取)
        if (instance_TankController.temp_ReceiveDamageValue > 0) 
        {
            GameManager.instance_ThisScript.stat_playerTotalDamage += instance_TankController.temp_ReceiveDamageValue;
            GameManager.instance_ThisScript.stat_playerUpdateHP = instance_TankController.updateHP_Hull;
            Impluse2Cinemachine();

            instance_TankController.temp_ReceiveDamageValue = 0f;
            instance_TankController.temp_ReceiveName = "";
        }

        //如果玩家遭到擊破，告知GM腳本，使其切換戰鬥結束
        if (instance_TankController.isDead == true) 
        {
            GameManager.instance_ThisScript.isPlayerDead = true;
            GameObject.FindObjectOfType<UI_ButtonList>().S2_PressQuit_WithResultShow_OrGameOver(); //藉由仲介腳本啟用結算視窗
        }
    }

    private void Impluse2Cinemachine() 
    {
        var impluseNoise = GetComponent<CinemachineImpulseSource>();
        impluseNoise.GenerateImpulse();
    }

    public void PlayerQuickKey_PauseAndContinue() 
    {
        //快捷鍵暫停設置於此，按下即可暫停或繼續遊戲
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.instance_ThisScript.isPaused == false)
            {
                GameObject.FindObjectOfType<UI_ButtonList>().S2_PauseGame();
            }
            else if (GameManager.instance_ThisScript.isPaused == true)
            {
                GameObject.FindObjectOfType<UI_ButtonList>().S2_ContinueGame();
            }
        }
    }

    public void PlayerInputToTankController() //即時接收玩家介面輸入，供值給控制器演算
    {
        //(1of7))
        //Vector2 input_MoveDirVector; //移動輸入向量值
        instance_TankController.input_MoveDirVector = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        //(2of7)) 
        //Vector2 input_TargetPosition; //瞄準目標輸入位置
        var ScreenPoint = Input.mousePosition;
        ScreenPoint.z = Camera.main.transform.position.z * (-1f);
        instance_TankController.input_TargetPosition = playerSet_FollowCamera.ScreenToWorldPoint(ScreenPoint);
        //instance_TankController.input_TargetPosition = playerSet_FollowCamera.ScreenToWorldPoint(Input.mousePosition);

        //(3of7))
        //float input_TwitchState; //探戈狀態移動輸入值
        scroll_twitchValue = Input.mouseScrollDelta.y;
        if (scroll_twitchValue > 0 && instance_TankController.input_MoveDirVector.magnitude <= 0)
        {
            instance_TankController.input_TwitchState = 1f;
            scroll_twitchToZeroCountTime = playerSet_ScrollSensitive;
        }
        else if (scroll_twitchValue < 0 && instance_TankController.input_MoveDirVector.magnitude <= 0)
        {
            instance_TankController.input_TwitchState = -1f;
            scroll_twitchToZeroCountTime = playerSet_ScrollSensitive;
        }
        else if (scroll_twitchValue == 0 && instance_TankController.input_MoveDirVector.magnitude <= 0)
        {
            if (scroll_twitchToZeroCountTime <= 0f)
            {
                instance_TankController.input_TwitchState = 0f;
                scroll_twitchToZeroCountTime = 0;
            }
            else
            {
                scroll_twitchToZeroCountTime -= Time.deltaTime;
            }
        }

        //(4of7))
        //bool input_IsFire; //是否開火
        if (Input.GetKeyDown(KeyCode.Mouse0)) 
        {
            instance_TankController.input_isFireShell = true; //按下開火
        }

        //(5of7))
        //bool input_IsLockTurrent; //是否鎖住砲塔
        if (Input.GetKey(KeyCode.Mouse1))
        { instance_TankController.input_IsLockTurrent = true; }
        else { instance_TankController.input_IsLockTurrent = false; }

        //(6of7)) //(7of7))
        //public bool input_OriginalRotate_Right; //是否原地旋轉(右轉
        //public bool input_OriginalRotate_Left; //是否原地旋轉(左轉
        if (instance_TankController.input_MoveDirVector.magnitude != 0 || scroll_twitchValue != 0) 
        {
            getkey_IsOriginalRotate_Holding = false;
            instance_TankController.input_OriginalRotate_Right = false;
            instance_TankController.input_OriginalRotate_Left = false;
        }
        else if (Input.GetKeyDown(KeyCode.E) && !getkey_IsOriginalRotate_Holding)
        {
            getkey_IsOriginalRotate_Holding = true;
            instance_TankController.input_OriginalRotate_Right = true;
            instance_TankController.input_OriginalRotate_Left = false;
        }
        else if (Input.GetKeyDown(KeyCode.E) && getkey_IsOriginalRotate_Holding)
        {
            if (instance_TankController.input_OriginalRotate_Left == true)
            {
                getkey_IsOriginalRotate_Holding = true;
                instance_TankController.input_OriginalRotate_Right = true;
                instance_TankController.input_OriginalRotate_Left = false;
            }
            else if (instance_TankController.input_OriginalRotate_Right == true)
            {
                getkey_IsOriginalRotate_Holding = false;
                instance_TankController.input_OriginalRotate_Right = false;
                instance_TankController.input_OriginalRotate_Left = false;
            }
        }
        else if (Input.GetKeyDown(KeyCode.Q) && !getkey_IsOriginalRotate_Holding)
        {
            getkey_IsOriginalRotate_Holding = true;
            instance_TankController.input_OriginalRotate_Right = false;
            instance_TankController.input_OriginalRotate_Left = true;
        }
        else if (Input.GetKeyDown(KeyCode.Q) && getkey_IsOriginalRotate_Holding)
        {
            if (instance_TankController.input_OriginalRotate_Right == true)
            {
                getkey_IsOriginalRotate_Holding = true;
                instance_TankController.input_OriginalRotate_Right = false;
                instance_TankController.input_OriginalRotate_Left = true;
            }
            else if (instance_TankController.input_OriginalRotate_Left == true)
            {
                getkey_IsOriginalRotate_Holding = false;
                instance_TankController.input_OriginalRotate_Right = false;
                instance_TankController.input_OriginalRotate_Left = false;
            }
        }


    }



    //玩家使用技能恢復血量or瞬間維修 //注意UI都是由GM控制，本處僅傳送資料
    public void PlayerSkill_UseRepairEnergy() 
    {
        //如果在冷卻時間，此函數不可使用
        if (is_Skill_CD == true) { return; }

        //如果在血量全滿的正常狀態，此函數亦不可使用
        if (instance_TankController.wheel_IsRepairing == false && instance_TankController.updateHP_Hull == instance_TankController.set_Hull_HP)
        { return; }

        //Debug.Log("技能可以用囉");

        //當技能可使用且按下使用，進入cd時間
        if (Input.GetKeyDown(KeyCode.Space))
        {
            is_Skill_CD = true;
            //取出恢復特效並對位
            var instanceFlash = GameManager.ObjectPool_TakeFrom(GameManager.instance_ThisScript.set_prefab_VFXflash, GameManager.set_objectPool_VFXflash);
            instanceFlash.transform.position = gameObject.transform.position;
            instanceFlash.transform.rotation = Quaternion.identity;

            StartCoroutine(PlayerSkill_EnergyAutoRecovery()); //冷卻時間倒數
            //////////////////////////
            //技能效果沒有套用...待修
            if (instance_TankController.wheel_IsRepairing == true)         //如果情形是輪子在維修中
            {
                //Debug.Log("解除維修狀態");
                instance_TankController.wheel_RepairCountTime = 0; //並立刻將輪子維修倒數歸0//輪子狀態改變交給控制器腳本進行
            }
            else if (instance_TankController.wheel_IsRepairing == false && instance_TankController.updateHP_Hull < instance_TankController.set_Hull_HP) //如果輪子無維修但車身血量未滿 
            {
                //Debug.Log("恢復玩家血量");
                instance_TankController.updateHP_Hull += playerSet_repairHPvalue;      //並立刻將即時車身血量加上此處設定恢復量//車身狀態改變交給控制器腳本進行(優點：遇到加血道具可以共用)   
                instance_TankController.SetHull_HPstate();                             //啟用控制器腳本的血量狀態更新
                GameManager.instance_ThisScript.stat_playerUpdateHP = instance_TankController.updateHP_Hull; //更新GM監控的玩家即時血量狀態            
            }
        }  
    }

    IEnumerator PlayerSkill_EnergyAutoRecovery() 
    {
        is_Skill_CD = true;
        GameManager.instance_ThisScript.isRepairSkill_Anim = true;
        updateCount_skill_CDtime = 0;
        for (float i = 0; i <= playerSet_Skill_CDtime; i++) 
        {
            updateCount_skill_CDtime = Mathf.Clamp(i, 0, playerSet_Skill_CDtime);
            GameManager.instance_ThisScript.SkillIcon_CDpercent = updateCount_skill_CDtime / playerSet_Skill_CDtime;
            yield return new WaitForSeconds(1.0f);
        }
        is_Skill_CD = false;        
    }

    //依位置判斷是否開啟履帶車痕拖尾
    private void TrackTrailRenderer_EmitSwitch() 
    {
        //不能使用鋼體速度當判斷條件，因為本專案移動並非透過施力進行(速度恆0)
        //Debug.Log(gameObject.GetComponent<Rigidbody2D>().velocity.magnitude);
        if(lastUpdate_gameOnjectPosition != gameObject.transform.position) 
        {
            trackTrail_EmitCloseToZeroCountTime = playerSet_TT_StopEmitSensitive;

            if (isAlreadyEmit == false) 
            {
                for (int i = 0; i < setTrackTrails.Length; i++)
                {
                    setTrackTrails[i].emitting = true;
                }
                isAlreadyEmit = true;
            }
        }
        else if (lastUpdate_gameOnjectPosition == gameObject.transform.position)
        {            
            if (trackTrail_EmitCloseToZeroCountTime <= 0f)
            {
                isAlreadyEmit = false;
                for (int i = 0; i < setTrackTrails.Length; i++)
                {
                    setTrackTrails[i].emitting = false;
                }
            }
            else
            {
                trackTrail_EmitCloseToZeroCountTime -= Time.fixedDeltaTime;
            }
        }
        lastUpdate_gameOnjectPosition = gameObject.transform.position;
    }

}
