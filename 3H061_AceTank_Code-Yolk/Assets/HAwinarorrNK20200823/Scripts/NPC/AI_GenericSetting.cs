using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI_GenericSetting : OBJECTS_PARENT_ON_FIELD
{
    /*NPC父腳本AI
     * 統計功能
     * 受到子彈攻擊，判斷子彈使用者並將傷害值回傳於gm腳本
     * 回傳GM腳本被消滅數+1
     * 
     * 開火AI
     * 砲管對準玩家
     * 檢測炮線障礙物是否阻礙打到玩家
     * 避免打擊友軍、炸彈
     * 
     * 移動AI
     * 迴避友軍相撞與撞牆
     */

    [Header("車輛客製化參數")]
    public float set_TimeOfDeadBodyOnField; //車輛遺骸滯留時間
    public float set_FireSensorDiamater;    //火線感測器縱深值

    //腳本或外部自動設定參數(不可手動)
    GenericTankController CSset_UsingTankController;      //宣告實例：戰車控制面板;
    public int GMset_IndexOfTankPrefab_InGM;           //儲存自己在GM預製物的編號，以利回歸物件池
    public GameManager.TankActType GMset_ActType_InGM; //儲存自己在GM定義的類型，以利統計資料傳送
    PolygonCollider2D CSset_FireRangeSensor; //由腳本自動定義的射擊感測器
    
    //腳本即時演算參數
    bool isThisScript_DeadCheck;    //確認本車已被即殺，且等候回歸物件池協程期間，停止本腳本功能   



    private void OnEnable()
    {
        gameObject.GetComponent<GenericTankController>().temp_ReceiveDamageValue = 0f;
        gameObject.GetComponent<GenericTankController>().temp_ReceiveName = null;
        //isReadyToFire = true;
        //isKillByPlayer=false;
        isThisScript_DeadCheck = false;
        CSset_UsingTankController = gameObject.GetComponent<GenericTankController>();
        OnEnable_FireSensorSetting();

        FSM_TurrentUsingState = TURRENT_ActState.isUnactive;
        FSM_HullUsingState = HULL_ActState.isVectorMove;
        Astar_PathSeeker_OnEnable_Initalize();

        //StartCoroutine(GameManager.instance_ThisScript.AutoGene_OEI_callForTankGene(gameObject, GameManager.instance_ThisScript.GAMESETTING_GamePlayer));
    }

    private void FixedUpdate()
    {
        FixedUpdate_SendMessage2GM();
        //若是玩家陣亡則可持續攻擊

        //如果遊戲暫停，停止ai控制
        if (GameManager.instance_ThisScript.isPaused == true)
        { return; }

        //如果腳本確認戰車已擊破，停止ai控制
        if (isThisScript_DeadCheck) { return; }
        //AI_GeneticType_Input();
        //SwitchFireLine_withRangeAndReload();
        Astar_PathSeeker_PathNextPosSet_FixedUpdate();
        Updated_FSMF_inFixedUpdate_previousInput();
        Updated_GeneticTypeFSM_SetStateInput2TankController();
        Updated_FSMF_inFixedUpdate_afterInput();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        Updated_FSMF_OnCollisionStay(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        Updated_FSMF_OnTriggerStay(collision);
    }


    void FixedUpdate_SendMessage2GM()
    {
        if (gameObject.GetComponent<GenericTankController>().temp_ReceiveDamageValue == 0)
        {
            return;
        }
        else
        {
            GameManager.instance_ThisScript.stat_playerTotalAttack += gameObject.GetComponent<GenericTankController>().temp_ReceiveDamageValue; //接收傷害加入結算統計
            GameManager.instance_ThisScript.stat_playerHitNum += 1;

            if (gameObject.GetComponent<GenericTankController>().isDead == true && isThisScript_DeadCheck == false)
            {
                isThisScript_DeadCheck = true;   //isThisScript_DeadCheck由本處變數做死亡確認，避免死體在場上期間重複協程
                if (gameObject.GetComponent<GenericTankController>().temp_ReceiveName == GameManager.instance_ThisScript.GAMESETTING_GamePlayerName)  //如果攻擊對象是玩家才計入
                {
                    //若是由玩家擊破，依照設定的車種加入玩家的成績統計
                    switch (GMset_ActType_InGM)
                    {
                        case GameManager.TankActType.MediumTank:
                            GameManager.instance_ThisScript.stat_playerKillNum_MT += 1;
                            break;
                        case GameManager.TankActType.HeavyTank:
                            GameManager.instance_ThisScript.stat_playerKillNum_HT += 1;
                            break;
                        case GameManager.TankActType.SuperHeavyTank:
                            GameManager.instance_ThisScript.stat_playerKillNum_SHT += 1;
                            break;
                    }
                }
                //凡是被擊破，系統擊破數+1
                GameManager.instance_ThisScript.stat_sysKillNum += 1;
                StartCoroutine(ReturnToTankPool());
            }
            gameObject.GetComponent<GenericTankController>().temp_ReceiveDamageValue = 0f;
            gameObject.GetComponent<GenericTankController>().temp_ReceiveName = null;
        }
    }

    IEnumerator ReturnToTankPool()  //依照設定條件回歸回歸物件池
    {
        yield return new WaitForSeconds(set_TimeOfDeadBodyOnField);
        GameObject VFX_vanishOnField = GameManager.ObjectPool_TakeFrom(GameManager.instance_ThisScript.set_prefab_VFXexplosion_Obj, GameManager.set_objectPool_VFXexplosion_Obj);
        VFX_vanishOnField.transform.position = gameObject.transform.position;
        GameManager.ObjectPool_ReturnTo(this.gameObject, GameManager.set_objectPool_Tank[GMset_IndexOfTankPrefab_InGM]);
    }


    void OnEnable_FireSensorSetting()  //初始化開火判斷的感應區域(依照火炮準度設定梯形範圍)
    {
        //將感應器綁定於炮管上隨其移動
        CSset_FireRangeSensor = CSset_UsingTankController.set_gameObject_FirePos.gameObject.AddComponent<PolygonCollider2D>();
        CSset_FireRangeSensor.isTrigger = true; //並設定其屬性與數量
        CSset_FireRangeSensor.pathCount = 1;
        //設定梯形範圍座標(component內部座標)
        Vector2 sensorFarCorner = new Vector2(set_FireSensorDiamater * Mathf.Atan(CSset_UsingTankController.set_gun_DispDeg * Mathf.Deg2Rad), set_FireSensorDiamater);
        Vector2[] sensorPath = new Vector2[4];
        sensorPath[0].x = 0.1f;
        sensorPath[0].y = 0f;
        sensorPath[1] = sensorFarCorner;
        sensorPath[2].x = -sensorFarCorner.x;
        sensorPath[2].y = sensorFarCorner.y;
        sensorPath[3].x = -sensorPath[0].x;
        sensorPath[3].y = sensorPath[0].y;
        CSset_FireRangeSensor.SetPath(0, sensorPath);
    }




    //有限狀態機撰寫中----------------------------------------------------------------------------------------------------------
    //定義有限狀態機_砲塔與車身的狀態 (=Animator動畫片段)
    //砲塔狀態：關機模式 / 執行射擊 / 開鏡模式 / 障礙檢視
    private enum TURRENT_ActState { isUnactive, isShot, isOpenSensor, isCheckSensor }
    //車身狀態：向量移動 / 立地旋轉 / 立地待機 / 擺角架式 / 小幅後退 / 小幅前進
    private enum HULL_ActState { isVectorMove, isIdleRotate, isIdle, isDef, isScollMove_Back, isScollMove_Forward }

    private TURRENT_ActState FSM_TurrentUsingState;
    private HULL_ActState FSM_HullUsingState;

    //定義有限狀態機_狀態切換條件 (=Animator動畫切換條件)
    //FSMF = FSM Factor    
    public bool FSMF_isPlayerClose;       //切換條件=玩家距離夠進 
    public bool FSMF_isPlayerInRange;     //切換條件=玩家在火線範圍內
    public bool FSMF_isObjectInRange;     //切換條件=障礙物在火線範圍內
    public bool FSMF_isHullTouchObject;   //切換條件=車身碰到障礙物
    public bool FSMF_isPlayerAtSide;      //切換條件=檢視至玩家方位角
    public bool FSMF_isPlayerAimYou;      //切換條件=檢視玩家炮管是否瞄準自己
    public bool FSMF_isHullDirAsPath;     //切換條件=檢視車身轉向同於尋路指向


    //set_StateRef_設定純狀態使用的參考門檻
    //public float set_StateRef_DefenseAng;
    public float set_StateRef_ScrollMoveTime;

    //set_FSMFref = 設定的切換條件參考門檻      
    public float set_FSMFref_2playerDistLimit;   //參考門檻 -至玩家距離
    public float set_FSMFref_defPlayerAngLimit;  //參考門檻 -至玩家方位角(相對車身正前方)
    public float set_FSMFref_playerGunAnimError; //參考門檻 -玩家瞄準誤差
    private Vector2 CSset_FSMFref_pathFindDir;   //參考門檻 -尋路方向

    //跨函數切換條件之間的計算傳遞：
    float tempRef2Ref_playerDist;              //跨函數切換條件之間的計算傳遞：與玩家距離
    float tempRef2Ref_objDist;
    //跨函數狀態與條件之間的計算傳遞：
    float tempRef2State_signedDeg_hullVSpath;       //跨函數狀態與條件之間的計算傳遞：車身與路徑夾角
    float tempRef2State_signedDeg_hull2Player;      //跨函數狀態與條件之間的計算傳遞：車身與玩家夾角
    //跨狀態之間的計算傳遞：
    float tempState2State_scrollTimeCount;

    //(FSM條件檢測函數1/4)
    //實時檢測狀態切換條件並設定其值(由本處計算判定)
    //注意此函數要放在Input之後(因為包含重置值)
    private void Updated_FSMF_inFixedUpdate_afterInput() //尋路連動未完成
    {
        //bool FSMF_isPlayerClose;       //切換條件=玩家距離夠進 
        var pos_Player = GameManager.instance_ThisScript.GAMESETTING_GamePlayer.transform.position;
        float dist2Player = Vector2.Distance(gameObject.transform.position, pos_Player);

        if (dist2Player >= set_FSMFref_2playerDistLimit) { FSMF_isPlayerClose = false; }
        else { FSMF_isPlayerClose = true; }
        //跨函數設定
        tempRef2Ref_playerDist = dist2Player;


        //bool FSMF_isPlayerAtSide;      //切換條件=檢視至玩家方位角
        var vector_world_2player_normalize = (Vector2)(pos_Player - gameObject.transform.position).normalized;
        var vector_world_HullFront = (Vector2)CSset_UsingTankController.set_gameObject_Hull.transform.TransformDirection(Vector2.up).normalized;
        var deg2player = Vector2.SignedAngle(vector_world_HullFront, vector_world_2player_normalize);

        if (Mathf.Abs(deg2player) >= set_FSMFref_defPlayerAngLimit) { FSMF_isPlayerAtSide = true; }
        else { FSMF_isPlayerAtSide = false; }
        //跨函數設定(逆時針為正)
        tempRef2State_signedDeg_hull2Player = deg2player;


        //bool FSMF_isPlayerAnimYou;     //切換條件=檢視玩家炮管是否瞄準自己
        Vector2 vector_world_PlayerTurrentRot = GameManager.instance_ThisScript.GAMESETTING_GamePlayerTurrent.transform.TransformDirection(Vector2.up).normalized;
        var deg_playerAimVS2player = Mathf.Acos(Vector2.Dot(-vector_world_PlayerTurrentRot, vector_world_2player_normalize)) * Mathf.Rad2Deg;

        if (deg_playerAimVS2player <= set_FSMFref_playerGunAnimError) { FSMF_isPlayerAimYou = true; }
        else { FSMF_isPlayerAimYou = false; }


        //bool FSMF_isHullDirAsPath;     //切換條件=檢視車身轉向同於尋路指向
        CSset_FSMFref_pathFindDir = (CSset_pathNextPos - (Vector2)gameObject.transform.position);
        var deg_hullDirVSpathDir = Vector2.SignedAngle(vector_world_HullFront, CSset_FSMFref_pathFindDir);
        if (Mathf.Abs(deg_hullDirVSpathDir) <= 5f) { FSMF_isHullDirAsPath = true;  }
        else { FSMF_isHullDirAsPath = false; }
        //跨函數設定
        tempRef2State_signedDeg_hullVSpath = deg_hullDirVSpathDir;


        //重置碰撞器檢定之切換條件，若符合條件再由其開啟
        FSMF_isHullTouchObject = false;
        FSMF_isPlayerInRange = false;
        FSMF_isObjectInRange = false;
    }

    //(FSM條件檢測函數2/4)
    //實時檢測狀態切換條件並設定其值(由碰撞觸發判定)
    //注意此函數要放在OnCollisionStay
    private void Updated_FSMF_OnCollisionStay(Collision2D collObj) 
    {
        //bool FSMF_HullTouchObject;     //切換條件=車身碰到障礙物
        if (collObj.gameObject.GetComponent<Objects_Tetrapods>() || collObj.gameObject.GetComponent<Objects_TNTBomb>() || collObj.gameObject.CompareTag("ENEMY"))
        {//只有炸彈或消波塊的碰撞，才可開啟此條件
            FSMF_isHullTouchObject = true;
        }
    }

    //(FSM條件檢測函數3/4)
    //實時檢測狀態切換條件並設定其值(由碰撞觸發判定)
    //注意此函數要放在OnTriggerStay
    private void Updated_FSMF_OnTriggerStay(Collider2D collObj)
    {
        //此兩切換條件交由碰撞檢測判定開啟，並由Update被重置值   
        //bool FSMF_PlayerInRange;       //切換條件=玩家在火線範圍內
        if (collObj.CompareTag("PLAYER"))
        {
            FSMF_isPlayerInRange = true;
        }

        //bool FSMF_ObjectInRange;       //切換條件=障礙物在火線範圍內(1/2)
        if (collObj.CompareTag("BATTLEFIELD_OBJECT") || collObj.CompareTag("ENEMY"))
        {
            //遇到醫藥包或消波塊照射不誤
            if (!collObj.GetComponent<Objects_HealPackage>() || !collObj.GetComponent<Objects_Tetrapods>())
            {
                var dist2obj = Vector2.Distance(gameObject.transform.position, collObj.transform.position);
                if (dist2obj < tempRef2Ref_objDist)
                {
                    tempRef2Ref_objDist = dist2obj;
                }
            }
        }       
    }

    //(FSM條件檢測函數4/4)
    //實時檢測狀態切換條件並設定其值(由本處計算判定)
    //注意此函數要放在Input之前(因為包含碰撞檢測的再處理)
    private void Updated_FSMF_inFixedUpdate_previousInput()
    {
        //bool FSMF_ObjectInRange;       //切換條件=障礙物在火線範圍內(2/2)
        if (FSMF_isPlayerInRange)
        {
            if (tempRef2Ref_objDist < tempRef2Ref_playerDist)
            {
                //注意由於此函數緊接就是輸出與重置，因此不會顯示結果於Inspector;
                FSMF_isObjectInRange = true;  
            }
        }
        tempRef2Ref_objDist = Mathf.Infinity;
    }

    //設定當前使用狀態，並輸入值至車輛控制器
    //注意此函數必放在Updated_FSMF_inFixedUpdate()之前 
    private void Updated_GeneticTypeFSM_SetStateInput2TankController()
    {
        //Debug.Log(FSM_TurrentUsingState);
        //Debug.Log(FSM_HullUsingState);
        //FSM_TurrentUsingState = 
        
        switch (FSM_TurrentUsingState)
        {
            case TURRENT_ActState.isUnactive:
                FSMS_TURRENT_Unactive();
                break;
            case TURRENT_ActState.isOpenSensor:
                FSMS_TURRENT_isOpenSensor();
                break;
            case TURRENT_ActState.isCheckSensor:
                FSMS_TURRENT_isCheckSensor();
                break;
            case TURRENT_ActState.isShot:
                FSMS_TURRENT_isShot();
                break;
        }
        
        switch (FSM_HullUsingState)
        {
            case HULL_ActState.isVectorMove:
                FSMS_HULL_isVectorMove();
                break;
            case HULL_ActState.isIdle:
                FSMS_HULL_isIdle();
                break;
            case HULL_ActState.isDef:
                FSMS_HULL_isDef();
                break;
            case HULL_ActState.isIdleRotate:
                FSMS_HULL_isIdleRotate();
                break;
            case HULL_ActState.isScollMove_Back:
                FSMS_HULL_isScollMove_Back();
                break;
            case HULL_ActState.isScollMove_Forward:
                FSMS_HULL_isScollMove_Forward();
                break;
        }
    }

    /*
    //戰車通用控制面板外部輸入項目
    /*FSM砲塔狀態_使用項目
      (本腳本)PolygonCollider2D CSset_FireRangeSensor; //由腳本自動定義的射擊感測器
      (控制器)Vector2 input_TargetPosition;  //瞄準目標輸入位置    
      (控制器)bool input_isFireShell;        //是否開火
      (控制器)bool input_IsLockTurrent;      //是否鎖住砲塔

    //FSM車身狀態_使用項目
      (控制器)Vector2 input_MoveDirVector; //移動輸入向量值
      (控制器)float input_TwitchState; //探戈狀態移動輸入值(1前進；0停止；-1後退)
      (控制器)bool input_OriginalRotate_Right; //是否原地旋轉(右轉
      (控制器)bool input_OriginalRotate_Left; //是否原地旋轉(左轉
    */


    //FSMS = 有限狀態機包含狀態類別------------------------------------------------------------------------------------------------
    private void FSMS_TURRENT_Unactive()
    {
        /*FSM砲塔狀態_使用項目
        (本腳本)PolygonCollider2D CSset_FireRangeSensor; //由腳本自動定義的射擊感測器
        (控制器)Vector2 input_TargetPosition;  //瞄準目標輸入位置    
        (控制器)bool input_isFireShell;        //是否開火
        (控制器)bool input_IsLockTurrent;      //是否鎖住砲塔*/

        CSset_FireRangeSensor.enabled = false;
        CSset_UsingTankController.input_TargetPosition = GameManager.instance_ThisScript.GAMESETTING_GamePlayer.transform.position;
        CSset_UsingTankController.input_isFireShell = false;
        CSset_UsingTankController.input_IsLockTurrent = false;

        /*FSM狀態切換條件*/
        if (FSMF_isPlayerClose)
        {
            FSM_TurrentUsingState = TURRENT_ActState.isOpenSensor;
        }
    }
    private void FSMS_TURRENT_isOpenSensor()
    {
        /*FSM砲塔狀態_使用項目
        (本腳本)PolygonCollider2D CSset_FireRangeSensor; //由腳本自動定義的射擊感測器
        (控制器)Vector2 input_TargetPosition;  //瞄準目標輸入位置    
        (控制器)bool input_isFireShell;        //是否開火
        (控制器)bool input_IsLockTurrent;      //是否鎖住砲塔*/

        CSset_FireRangeSensor.enabled = true;
        CSset_UsingTankController.input_TargetPosition = GameManager.instance_ThisScript.GAMESETTING_GamePlayer.transform.position;
        CSset_UsingTankController.input_isFireShell = false;
        CSset_UsingTankController.input_IsLockTurrent = false;

        /*FSM狀態切換條件*/
        if (FSMF_isPlayerInRange)
        {
            FSM_TurrentUsingState = TURRENT_ActState.isCheckSensor;
        }
        else if(!FSMF_isPlayerClose) 
        { FSM_TurrentUsingState = TURRENT_ActState.isUnactive; }

    }
    private void FSMS_TURRENT_isCheckSensor()
    {
        /*FSM砲塔狀態_使用項目
        (本腳本)PolygonCollider2D CSset_FireRangeSensor; //由腳本自動定義的射擊感測器
        (控制器)Vector2 input_TargetPosition;  //瞄準目標輸入位置    
        (控制器)bool input_isFireShell;        //是否開火
        (控制器)bool input_IsLockTurrent;      //是否鎖住砲塔*/

        //CSset_FireRangeSensor.enabled = true;
        CSset_UsingTankController.input_TargetPosition = GameManager.instance_ThisScript.GAMESETTING_GamePlayer.transform.position;
        CSset_UsingTankController.input_isFireShell = false;
        CSset_UsingTankController.input_IsLockTurrent = false;


        //Debug.Log("FSMF_isPlayerInRangee" + FSMF_isPlayerInRange);
        //Debug.Log("FSMF_isObjectInRange" + FSMF_isObjectInRange);

        /*FSM狀態切換條件*/
        if (!FSMF_isPlayerClose)
        {
            FSM_TurrentUsingState = TURRENT_ActState.isUnactive;
        }
        else if (FSMF_isPlayerInRange && !FSMF_isObjectInRange)
        {
            FSM_TurrentUsingState = TURRENT_ActState.isShot;
        }
    }
    private void FSMS_TURRENT_isShot()
    {
        /*FSM砲塔狀態_使用項目
        (本腳本)PolygonCollider2D CSset_FireRangeSensor; //由腳本自動定義的射擊感測器
        (控制器)Vector2 input_TargetPosition;  //瞄準目標輸入位置    
        (控制器)bool input_isFireShell;        //是否開火
        (控制器)bool input_IsLockTurrent;      //是否鎖住砲塔*/

        //CSset_FireRangeSensor.enabled = false;
        CSset_UsingTankController.input_TargetPosition = GameManager.instance_ThisScript.GAMESETTING_GamePlayer.transform.position;
        CSset_UsingTankController.input_isFireShell = true;
        CSset_UsingTankController.input_IsLockTurrent = false;

        /*FSM狀態切換條件*/
        if (!FSMF_isPlayerClose)
        {
            FSM_TurrentUsingState = TURRENT_ActState.isUnactive;
        }
        if (FSMF_isPlayerClose)
        {
            FSM_TurrentUsingState = TURRENT_ActState.isOpenSensor;
        }
    }


    private void FSMS_HULL_isVectorMove()
    {
        /*FSM車身狀態_使用項目
        (控制器)Vector2 input_MoveDirVector; //移動輸入向量值
        (控制器)float input_TwitchState; //探戈狀態移動輸入值(1前進；0停止；-1後退)
        (控制器)bool input_OriginalRotate_Right; //是否原地旋轉(右轉
        (控制器)bool input_OriginalRotate_Left; //是否原地旋轉(左轉*/

        CSset_UsingTankController.input_MoveDirVector = CSset_FSMFref_pathFindDir.normalized;
        CSset_UsingTankController.input_TwitchState = 0f;
        CSset_UsingTankController.input_OriginalRotate_Right = false;
        CSset_UsingTankController.input_OriginalRotate_Left = false;

        /*FSM狀態切換條件*/
        if (FSMF_isPlayerClose) { FSM_HullUsingState = HULL_ActState.isIdle; }
    }
    private void FSMS_HULL_isIdle()
    {
        /*FSM車身狀態_使用項目
        (控制器)Vector2 input_MoveDirVector; //移動輸入向量值
        (控制器)float input_TwitchState; //探戈狀態移動輸入值(1前進；0停止；-1後退)
        (控制器)bool input_OriginalRotate_Right; //是否原地旋轉(右轉
        (控制器)bool input_OriginalRotate_Left; //是否原地旋轉(左轉*/

        CSset_UsingTankController.input_MoveDirVector = Vector2.zero;
        CSset_UsingTankController.input_TwitchState = Mathf.RoundToInt(Random.Range(-1f, 1f));// 0f;
        CSset_UsingTankController.input_OriginalRotate_Right = false;
        CSset_UsingTankController.input_OriginalRotate_Left = false;

        /*FSM狀態切換條件*/
        if (FSMF_isPlayerAtSide && !FSMF_isObjectInRange) { FSM_HullUsingState = HULL_ActState.isDef; }
        else if (FSMF_isPlayerAimYou) { FSM_HullUsingState = HULL_ActState.isScollMove_Back; }
        else if (FSMF_isObjectInRange || !FSMF_isPlayerClose) { FSM_HullUsingState = HULL_ActState.isIdleRotate; }
    }
    private void FSMS_HULL_isDef()
    {
        /*FSM車身狀態_使用項目
        (控制器)Vector2 input_MoveDirVector; //移動輸入向量值
        (控制器)float input_TwitchState; //探戈狀態移動輸入值(1前進；0停止；-1後退)
        (控制器)bool input_OriginalRotate_Right; //是否原地旋轉(右轉
        (控制器)bool input_OriginalRotate_Left; //是否原地旋轉(左轉*/

        CSset_UsingTankController.input_MoveDirVector = Vector2.zero;
        CSset_UsingTankController.input_TwitchState = 0f;
        //如果原本擺角過偏，轉回至設定範圍(逆時針為正)
        //Debug.Log("tempRef2State_signedDeg_hull2Player" + tempRef2State_signedDeg_hull2Player);
        if (Mathf.Abs(tempRef2State_signedDeg_hull2Player) > set_FSMFref_defPlayerAngLimit) // set_StateRef_DefenseAng)
        {
            if (tempRef2State_signedDeg_hull2Player < 0f)                   //過偏左，則轉右
            {
                //Debug.Log("過偏左");
                CSset_UsingTankController.input_OriginalRotate_Right = true;
                CSset_UsingTankController.input_OriginalRotate_Left = false;
            }
            else if (tempRef2State_signedDeg_hull2Player > 0f)              //過偏右，則轉左
            {
                //Debug.Log("過偏右");
                CSset_UsingTankController.input_OriginalRotate_Right = false;
                CSset_UsingTankController.input_OriginalRotate_Left = true;
            }
        }//如果無過偏，隨機擺動        
        else 
        {
            //沒有在旋轉的話則隨機指定一轉向
            if (CSset_UsingTankController.input_OriginalRotate_Right == false
                && CSset_UsingTankController.input_OriginalRotate_Left == false) 
            {
                //Debug.Log("隨機指向");
                var randomRot = Mathf.RoundToInt(Mathf.PingPong(Time.timeSinceLevelLoad, 1f));
                if (randomRot > 0) { CSset_UsingTankController.input_OriginalRotate_Right = true; }
                else { CSset_UsingTankController.input_OriginalRotate_Left = true; }
            }

            //隨機時間間隔，轉反方向
            if (Time.timeSinceLevelLoad % 1 < Random.Range(1f/45f, 1f/90f))   //大概1.5~3秒一次 
            {
                CSset_UsingTankController.input_OriginalRotate_Left = !CSset_UsingTankController.input_OriginalRotate_Left;
                CSset_UsingTankController.input_OriginalRotate_Right = !CSset_UsingTankController.input_OriginalRotate_Left;
            }
        }
        /*FSM狀態切換條件*/
        if (FSMF_isPlayerAimYou) //如果被玩家瞄準，經過多重判斷再決定是否倒車迴避
        {
            var selfArmorFront = CSset_UsingTankController.set_armor_Front;
            var playerTank = GameManager.instance_ThisScript.GAMESETTING_GamePlayer.GetComponent<GenericTankController>();
            //正面裝甲大於玩家穿深餘旋，不進行倒車迴避
            if (selfArmorFront > playerTank.set_shell_Penetrate * Mathf.Cos(set_FSMFref_defPlayerAngLimit * Mathf.Deg2Rad))
            {
                return;
            }
            //或玩家使用HE時，預判傷害小於100，不進行倒車迴避
            else if (playerTank.set_shell_Type == GenericObjectDamageSetting.DamageType.HE && (playerTank.set_shell_Damage * 0.5f - selfArmorFront) < 100f) 
            {
                return;
            }
            FSM_HullUsingState = HULL_ActState.isScollMove_Back;            
        }
        else if (!FSMF_isPlayerClose) { FSM_HullUsingState = HULL_ActState.isIdleRotate; }
    }
    private void FSMS_HULL_isIdleRotate()
    {
        /*FSM車身狀態_使用項目
        (控制器)Vector2 input_MoveDirVector; //移動輸入向量值
        (控制器)float input_TwitchState; //探戈狀態移動輸入值(1前進；0停止；-1後退)
        (控制器)bool input_OriginalRotate_Right; //是否原地旋轉(右轉
        (控制器)bool input_OriginalRotate_Left; //是否原地旋轉(左轉*/

        CSset_UsingTankController.input_MoveDirVector = Vector2.zero;
        CSset_UsingTankController.input_TwitchState = 0f;

        //Debug.Log("tempRef2State_signedDeg_hullVSpath" + tempRef2State_signedDeg_hullVSpath);
        //依照指定路徑轉動車身
        if (tempRef2State_signedDeg_hullVSpath > 0f)                         //偏離路徑左，
        {
            CSset_UsingTankController.input_OriginalRotate_Right = false;
            CSset_UsingTankController.input_OriginalRotate_Left = true;
        }
        else if (tempRef2State_signedDeg_hullVSpath < 0f)
        {
            CSset_UsingTankController.input_OriginalRotate_Right = true;
            CSset_UsingTankController.input_OriginalRotate_Left = false;
        }

        /*FSM狀態切換條件*/
        /*if (FSMF_isPlayerClose)
        {
            FSM_HullUsingState = HULL_ActState.isIdle;
        }*/
        if(FSMF_isHullDirAsPath) //如果路徑轉向
        {
            FSM_HullUsingState = HULL_ActState.isScollMove_Forward;
            //CSset_FSMFref_pathFindDir = Vector2.right;
        }
    }
    private void FSMS_HULL_isScollMove_Back()
    {
        /*FSM車身狀態_使用項目
        (控制器)Vector2 input_MoveDirVector; //移動輸入向量值
        (控制器)float input_TwitchState; //探戈狀態移動輸入值(1前進；0停止；-1後退)
        (控制器)bool input_OriginalRotate_Right; //是否原地旋轉(右轉
        (控制器)bool input_OriginalRotate_Left; //是否原地旋轉(左轉*/

        CSset_UsingTankController.input_MoveDirVector = Vector2.zero;
        if (tempState2State_scrollTimeCount < set_StateRef_ScrollMoveTime)
        {
            CSset_UsingTankController.input_TwitchState = -1f;
            tempState2State_scrollTimeCount += (1f / 30f);

            CSset_UsingTankController.input_OriginalRotate_Right = false;
            CSset_UsingTankController.input_OriginalRotate_Left = false;
        }
        else 
        {
            tempState2State_scrollTimeCount = 0f;
            /*FSM狀態切換條件*/
            if (!FSMF_isHullTouchObject) { FSM_HullUsingState = HULL_ActState.isDef; }
            else if (FSMF_isHullTouchObject) { FSM_HullUsingState = HULL_ActState.isIdleRotate; }
        }
    }
    private void FSMS_HULL_isScollMove_Forward()
    {
        /*FSM車身狀態_使用項目
        (控制器)Vector2 input_MoveDirVector; //移動輸入向量值
        (控制器)float input_TwitchState; //探戈狀態移動輸入值(1前進；0停止；-1後退)
        (控制器)bool input_OriginalRotate_Right; //是否原地旋轉(右轉
        (控制器)bool input_OriginalRotate_Left; //是否原地旋轉(左轉*/

        CSset_UsingTankController.input_MoveDirVector = Vector2.zero;
        if (tempState2State_scrollTimeCount < set_StateRef_ScrollMoveTime)
        {
            CSset_UsingTankController.input_TwitchState = 1f;
            tempState2State_scrollTimeCount += (1f / 30f);
            CSset_UsingTankController.input_OriginalRotate_Right = false;
            CSset_UsingTankController.input_OriginalRotate_Left = false;
        }
        else 
        {
            CSset_UsingTankController.input_TwitchState = 0f;
            tempState2State_scrollTimeCount = 0f;
            /*FSM狀態切換條件*/
            if (!FSMF_isPlayerClose) { FSM_HullUsingState = HULL_ActState.isVectorMove; }
            else if (FSMF_isPlayerClose) { FSM_HullUsingState = HULL_ActState.isIdle; }
        }
    }



    //A*尋路設定 (有限狀態機參考路徑)------------------------------------------------------------------------------------------------
    Path CSset_pathUsing;
    Seeker CSset_pathSeeker;
    int CSset_pathNextPos_Code;
    Vector2 CSset_pathNextPos;
    //(尋路函數1/4)初始化與重複啟用設定
    private void Astar_PathSeeker_OnEnable_Initalize() 
    {
        CSset_pathSeeker = gameObject.GetComponent<Seeker>();
        InvokeRepeating("Astar_PathSeeker_Reset_OnEnable_forInvoke", 5f, 2f);
    }

    //(尋路函數2/4)定時檢查
    private void Astar_PathSeeker_Reset_OnEnable_forInvoke()
    {
        if (CSset_pathSeeker.IsDone()) 
        {
            CSset_pathSeeker.StartPath(gameObject.transform.position, GameManager.instance_ThisScript.GAMESETTING_GamePlayer.transform.position, Astar_PathSeeker_ResetIfError_forCall);
        }
    }
    //(尋路函數3/4)檢查確認重設
    private void Astar_PathSeeker_ResetIfError_forCall(Path updatedPath) //函式引數由插件自動給值，本處毋須設定
    {
        if (!updatedPath.error) 
        {
            CSset_pathUsing = updatedPath;
            CSset_pathNextPos_Code = 0;
        }
    }
    //(尋路函數1/4)路徑更新
    private void Astar_PathSeeker_PathNextPosSet_FixedUpdate() 
    {
        if (CSset_pathUsing == null) 
        {
            return;
        }

        if (CSset_pathNextPos_Code >= CSset_pathUsing.vectorPath.Count) { return; }
        CSset_pathNextPos = (Vector2)CSset_pathUsing.vectorPath[CSset_pathNextPos_Code];

        //(現有位置,目標位置)足夠靠近，才更新目標位置
        if (Vector2.Distance(gameObject.transform.position, CSset_pathNextPos) < 5f)
        {
            CSset_pathNextPos_Code++;
        }
    }
}
