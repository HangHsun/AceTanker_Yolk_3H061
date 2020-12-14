using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//如果有出現啥模稜兩可參考的錯誤訊息，請從以上數項刪除多餘的東西

public class GenericTankController : MonoBehaviour
{

    /*戰車設置-控制腳本
     * 性能參數與控制系統設置
     * 總體/零件血量與狀態
     * 受傷數值計算
     * 輸出數值給予
     *依血量啟動車尾黑煙(預製物)
     * 裝填計時
     */

    [Header("戰車使用者")]
    public string set_user_ID; //使用者，方便更精確的判斷結算資料

    //外部設定項目(固定值) //不可以被演算或外訪腳本改動
    [Header("圖片設定")]
    public Color set_allPartColor = Color.white;
    public Transform set_gameObject_Hull;       //場景子物件
    public Transform set_gameObject_Turrent;
    public Transform set_gameObject_Gun;
    public Transform set_gameObject_FirePos;    //子彈射出點
    public bool set_turrent_IsActLimit;         //是否為非全周砲塔
    public Vector2 set_turrent_ActRange;        //砲塔旋轉限制範圍
    public TankExtensionArmorPart[] set_gameObjects_EXarmor;

    public float set_gun_MuzzleDistanceMax;     //槍管制退距離
    public float set_muzzle_HoldTime = 2f;      //制退底端維持時間
    public float set_muzzle_Speed_BackUp = 10f; //制退速度
    public float set_muzzle_Speed_Forward = 2f; //制進速度
    public Sprite set_spriteNormal_Hull, set_spriteNormal_Turrent, set_spriteNormal_Gun; //一般狀態使用圖片(車身/砲塔/砲管)
    public Sprite set_spriteDead_Hull, set_spriteDead_Turrent, set_spriteDead_Gun;       //爆車狀態使用圖片(車身/砲塔/砲管)
    public Sprite set_shell_Icon;                                                        //使用子彈圖片    
    public float set_fireVFX_HoldTime; //發砲特效持續時間
    public GameObject set_damage_VFXlight, set_damage_VFXmed, set_damage_VFXheavy, set_damage_VFXdead; //輕度受損特效, 中度受損特效, 高度受損特效, 爆車特效

    [Header("碰撞尺寸")]
    public float set_size_BoxPivotBiasY;
    public float set_size_BoxWidth, set_size_BoxLength;      //傷害碰撞檢測_車身軸心與尺寸設定
    public float set_size_WheelRadious, set_size_WheelPivot; //傷害碰撞檢測_動力輪軸心與尺寸設定
    

    [Header("體力參數")]
    public float set_Hull_HP, set_wheel_HP; //自身血量/輪子血量
    public float set_armor_Front, set_armor_Side, set_armor_Rear;//裝甲厚度設定
    public float set_wheel_armor; //要增設一項輪子防禦值
    enum AreaType { HULL, WHEEL };

    [Header("靈活參數")]
    public float set_rb_ForwardSpeed;
    public float set_rb_BackUpSpeed; //車身前進/後退速度
    public float set_hull_RotateSpeed, set_turrent_RotateSpeed;//砲塔/車身旋轉速度(度/s)
    public float set_wheel_RepairTime; //輪子修復時間

    [Header("火力參數")]
    public float set_gun_DispDeg;     //炮管發射精度(deg)
    public float set_gun_ReoladSetTime; //炮管裝填時間(sec)    
    public float set_gun_Diameter;    //砲管口徑
    //enum DamageType { AP, HE };
    public GenericObjectDamageSetting.DamageType set_shell_Type;     //砲彈種類
    public float set_shell_Speed;     //炮彈速度
    public float set_shell_Lifetime;  //砲彈壽命
    public float set_shell_Damage;    //砲彈傷害 
    public float set_shell_Penetrate; //砲彈穿深    
    public float set_shell_EXRadious; //砲彈暴風半徑

    public LayerMask objectsWithDamage; //可傷害本物件圖層

    [Header("外部控制即時參數")]
    //外部腳本及實時輸入項目
    public Vector2 input_MoveDirVector;                //移動輸入向量值
    public Vector2 input_TargetPosition;               //瞄準目標輸入位置
    public float input_TwitchState;                    //探戈狀態移動輸入值(1前進；0停止；-1後退)
    public bool input_isFireShell;                     //是否開火
    public bool input_IsLockTurrent;                   //是否鎖住砲塔
    public bool input_OriginalRotate_Right;            //是否原地旋轉(右轉
    public bool input_OriginalRotate_Left;             //是否原地旋轉(左轉
   



    //系統演算項目 //方便跨函數存取故放此 //或不想被頻繁重設者
    Rigidbody2D point_Rigidbody;                                          //自帶剛體
    public BoxCollider2D hull_Collider;                      //物理碰撞器
    float Driving_Rotation;                          //滑鼠/鍵盤指向方位
    float hull_Rotation, turrent_Rotation;           //圖片物件旋轉方位 (=圖片.localEulerAngles.z,但本值可編輯)
    bool isForward; //是否前進
    bool isOriginalRotate_Right, isOriginalRotate_Left;

    public bool isReloading; //供外部腳本判斷子彈是否確實射出(配合射擊指令)
    public float gun_ReloadCountTime;
    float gun_MovingPosY;
    float gun_PosHoldCountTime;
    bool muzzle_IsPush = false;
    bool muzzle_IsBottom = false;
    bool muzzle_IsReturn = false;

    public float updateHP_Hull;       //即時車身血量
    float temp_previousUpdateHP_Hull; //存取前幀血量，監測是否有血量增加
    public float updateHP_Wheel;      //即時輪子血量


    public bool wheel_IsRepairing; //輪子維修狀態
    public float wheel_RepairCountTime; //即時修復倒數
    public bool isDead; //是否爆車(血量0)

    //bool isDamageSourceRepeat;
    public float temp_ReceiveDamageValue; //攻擊者給予傷害暫存 //每次收到攻擊時供外部紀錄，並由外部進行重設
    public string temp_ReceiveName;       //攻擊者使用者名稱暫存 //每次收到攻擊時供外部紀錄，並由外部進行重設 //方便判斷重設時機

    //存取戰車聲效的播放建立器(也利於初期設置抱錯確認)
    private GenericTankAudioBuilder usingTankAudio;


    private void OnEnable()
    {
        //存取戰車聲效的播放建立器，方便使用(也利於初期設置抱錯確認)
        usingTankAudio = gameObject.GetComponent<GenericTankAudioBuilder>();


        //設定開場套用圖片
        set_gameObject_Hull.gameObject.GetComponent<SpriteRenderer>().sprite = set_spriteNormal_Hull;
        set_gameObject_Turrent.gameObject.GetComponent<SpriteRenderer>().sprite = set_spriteNormal_Turrent;
        set_gameObject_Gun.gameObject.GetComponent<SpriteRenderer>().sprite = set_spriteNormal_Gun;

        //設定套用圖片之顏色
        set_gameObject_Hull.gameObject.GetComponent<SpriteRenderer>().color = set_allPartColor;
        set_gameObject_Turrent.gameObject.GetComponent<SpriteRenderer>().color = set_allPartColor;
        set_gameObject_Gun.gameObject.GetComponent<SpriteRenderer>().color = set_allPartColor;

        //找尋是否有擴充裝甲子物件，並加以啟用
        if (set_gameObjects_EXarmor.Length > 0) 
        {
            for (int i = 0; i < set_gameObjects_EXarmor.Length; i++) 
            {
                var EXunit = set_gameObjects_EXarmor[i];
                EXunit.gameObject.SetActive(true);
                EXunit.GetComponent<SpriteRenderer>().enabled = true;
                EXunit.GetComponent<BoxCollider2D>().enabled = true;
            }
        }

        updateHP_Hull = set_Hull_HP;
        temp_previousUpdateHP_Hull = updateHP_Hull;
        updateHP_Wheel = set_wheel_HP;
        isDead = false;
        wheel_IsRepairing = false;
        set_damage_VFXlight.SetActive(false);
        set_damage_VFXmed.SetActive(false);
        set_damage_VFXheavy.SetActive(false);
        set_damage_VFXdead.SetActive(false);
        point_Rigidbody = gameObject.GetComponent<Rigidbody2D>();
        hull_Collider = set_gameObject_Hull.GetComponent<BoxCollider2D>();
        hull_Collider.offset = new Vector2(0f,set_size_BoxPivotBiasY);
        hull_Collider.size = new Vector2(set_size_BoxWidth - 0.4f, set_size_BoxLength - 0.4f);
    }


    private void Update()
    {
        FireShell();
    }

    private void FixedUpdate()
    {
        if (GameManager.instance_ThisScript.isPaused == true) 
        {
            return;
        }
        if (isDead == true) { return; }

        ArmorRaycastSetting(); //必須要放在fixedUpdate，使得與碰撞的執行次數同步
        HullRotation();
        HullMove();
        TurrentRotation();
        ShellReload();
    }



    public void TurrentRotation() //定義砲塔轉向
    {
        //保持砲塔旋轉固定軸心
        var keepLocalPos = set_gameObject_Turrent.localPosition;

        var local_TurrentAng = set_gameObject_Turrent.localEulerAngles.z;                         //砲塔當地方位
        Vector2 TargetDistance = input_TargetPosition - (Vector2)set_gameObject_Turrent.position; //與目標距離
        

        Vector2 local_Aim_Vector = set_gameObject_Hull.InverseTransformDirection(TargetDistance); //瞄準當地方位
        var local_Aim_Angle = Vector2.SignedAngle(Vector2.up, local_Aim_Vector);                  //瞄準當地方位角//以利判定(-180~+180)

        if (input_IsLockTurrent)      //若按下滑鼠右鍵，砲塔鎖住不旋轉
        {
            set_gameObject_Turrent.localRotation = Quaternion.Euler(0f, 0f, set_gameObject_Turrent.localEulerAngles.z);
        }
        else
        {
            if (set_turrent_IsActLimit == true) //如果砲塔有射界限制採用
            {
                bool isAim_InRange = true;
                var local_ActBound_Min = -Mathf.Clamp(set_turrent_ActRange.y, 0f, 179.99f); //轉換射界最小值(0~-179.99)
                var local_ActBound_Max = Mathf.Clamp(set_turrent_ActRange.x, 0f, 179.99f);  //轉換射界最大值(0~+179.99)
                //var local_TurrentAng = set_gameObject_Turrent.localEulerAngles.z;
                if (local_TurrentAng > 180f)
                {
                    local_TurrentAng -= 360f;
                }//轉換砲塔旋轉值以利判定(-180~+180)

                if (local_Aim_Angle <= local_ActBound_Max && local_Aim_Angle >= local_ActBound_Min) //如果瞄準方位在射界內，判定為在射界內
                {
                    isAim_InRange = true;
                }
                else
                {
                    isAim_InRange = false;
                }

                if (isAim_InRange) //如果在射界內，砲塔跟隨目標
                {
                    local_TurrentAng = Mathf.MoveTowardsAngle(local_TurrentAng, local_Aim_Angle, set_turrent_RotateSpeed * Time.fixedDeltaTime); //更改砲塔旋轉值
                }
                else if (!isAim_InRange) //如果在射界外
                {
                    if (local_Aim_Angle >= local_ActBound_Max) //如果目標在左外界，砲塔逆時針旋轉
                    {
                        local_TurrentAng += set_turrent_RotateSpeed * Time.fixedDeltaTime;
                        if (local_TurrentAng > local_ActBound_Max)
                        {
                            local_TurrentAng = local_ActBound_Max; //更改砲塔旋轉值
                        }
                    }
                    else if (local_Aim_Angle <= local_ActBound_Min) //如果目標在右外界，砲塔順時針旋轉
                    {
                        local_TurrentAng -= set_turrent_RotateSpeed * Time.fixedDeltaTime;
                        if (local_TurrentAng < local_ActBound_Min)
                        {
                            local_TurrentAng = local_ActBound_Min; //更改砲塔旋轉值
                        }
                    }
                }

                local_TurrentAng += 360f;
                if (local_TurrentAng > 360f)
                {
                    local_TurrentAng %= 360f;
                }//將更改後的砲塔旋轉值轉為歐拉格式(0~360)
                set_gameObject_Turrent.localRotation = Quaternion.Euler(0f, 0f, local_TurrentAng);  //圖片旋轉套用本值
            }
            else if (set_turrent_IsActLimit == false)//如果砲塔無射界限制(全周旋轉)採用
            {
                local_TurrentAng = Mathf.MoveTowardsAngle(local_TurrentAng, local_Aim_Angle, set_turrent_RotateSpeed * Time.fixedDeltaTime);
                set_gameObject_Turrent.localRotation = Quaternion.Euler(0f, 0f, local_TurrentAng);
            }
        }
        //保持砲塔旋轉固定軸心
        set_gameObject_Turrent.localPosition = keepLocalPos;
    }

    public void HullRotation() //定義車身轉向
    {
        //保持車體旋轉固定軸心
        var keepLocalPos = set_gameObject_Hull.localPosition;

        if (wheel_IsRepairing) { return; } //輪子無維修中才可使用

        if (input_MoveDirVector.magnitude > 0f)
        {
            isForward = true;
        }
        else
        {
            isForward = false;
        }

        if (isForward)                                   //如果有輸入值，移動加轉動車身
        {
            Driving_Rotation = Mathf.Atan2(input_MoveDirVector.y, input_MoveDirVector.x) * Mathf.Rad2Deg - 90f; //最終車身方向   
            hull_Rotation = Mathf.MoveTowardsAngle(set_gameObject_Hull.localEulerAngles.z, Driving_Rotation, set_hull_RotateSpeed * Time.fixedDeltaTime); //車身轉向平滑計算
            input_OriginalRotate_Right = false;
            input_OriginalRotate_Left = false;
            isOriginalRotate_Right = false;
            isOriginalRotate_Left = false;
        }
        else if (!isForward)//如果沒有輸入值，固定車身
        {
            if (input_OriginalRotate_Right && input_OriginalRotate_Left)
            {
                isOriginalRotate_Right = false;
                isOriginalRotate_Left = false;
            }
            else if (!input_OriginalRotate_Right && !input_OriginalRotate_Left)
            {
                isOriginalRotate_Right = false;
                isOriginalRotate_Left = false;
            }
            else if (input_OriginalRotate_Right && !input_OriginalRotate_Left)
            {
                isOriginalRotate_Right = true;
                isOriginalRotate_Left = false;
            }
            else if (input_OriginalRotate_Left && !input_OriginalRotate_Right)
            {
                isOriginalRotate_Right = false;
                isOriginalRotate_Left = true;
            }

            if (isOriginalRotate_Right)
            {
                hull_Rotation -= set_hull_RotateSpeed * 2f * Time.fixedDeltaTime;
            }
            else if (isOriginalRotate_Left)
            {
                hull_Rotation += set_hull_RotateSpeed * 2f * Time.fixedDeltaTime;
            }
            else
            {
                hull_Rotation = set_gameObject_Hull.localEulerAngles.z;
            }

        }

        set_gameObject_Hull.localRotation = Quaternion.Euler(0f, 0f, hull_Rotation);//車身套用旋轉值

        //保持車體旋轉固定軸心
        set_gameObject_Hull.localPosition = keepLocalPos;
    }

    public void HullMove() //判定前進還是後退
    {
        if (wheel_IsRepairing) { return; } //輪子無維修中才可使用

        var moveDir = hull_Rotation + 90f;
        var moveDirVector = new Vector2(Mathf.Cos(moveDir * Mathf.Deg2Rad), Mathf.Sin(moveDir * Mathf.Deg2Rad));
        if (isForward)
        {
            //施力非物理運動，而是還用指令強制移動，待修
            point_Rigidbody.MovePosition(point_Rigidbody.position + moveDirVector * set_rb_ForwardSpeed * Time.fixedDeltaTime);
            //point_Rigidbody.AddRelativeForce(point_Rigidbody.position,moveDirVector * set_rb_ForwardSpeed * Time.fixedDeltaTime);
        }
        else
        {
            float rb_TwitchSpeed = 0f; //往返動作速度
            if (input_TwitchState == 0f)
            {
                rb_TwitchSpeed = 0f;
            }
            else if (input_TwitchState == 1f)
            {
                rb_TwitchSpeed = set_rb_ForwardSpeed;
            }
            else if (input_TwitchState == -1f)
            {
                rb_TwitchSpeed = (-1f) * set_rb_BackUpSpeed;
            }
            point_Rigidbody.MovePosition(point_Rigidbody.position + moveDirVector * rb_TwitchSpeed * Time.fixedDeltaTime);
            //point_Rigidbody.AddForce(point_Rigidbody.position + moveDirVector * rb_TwitchSpeed * Time.fixedDeltaTime);
        }
    }


    //發射放在Update；裝填放在FixedUpdate
    public void FireShell()
    {
        if (input_isFireShell && isReloading) { } //如果有按下開火且在裝填中，無反應
        else if (input_isFireShell && !isReloading) //如果有按開火且未在裝填中
        {
            //使用物件池取出與放入的版本
            //注意生成位置在砲管軸心偏移x軸，x軸值同開火點轉換後的local值
            var shellGene_localPos = new Vector2(set_gameObject_FirePos.transform.localPosition.x, 0f);
            var shellGene_worldPos = set_gameObject_Gun.transform.TransformPoint(shellGene_localPos);
            
            //生成發射隨機偏差角度(世界座標，以利生成)
            var randomDispWorldAngle = Quaternion.Euler(0, 0, set_gameObject_FirePos.eulerAngles.z + Random.Range(-set_gun_DispDeg, set_gun_DispDeg));
            //轉換角度為向量(以利施力) //注意x軸由於Unity是逆時針旋轉，故要設定負值。
            Vector3 randomDispWorldVector = new Vector3(-Mathf.Sin(randomDispWorldAngle.eulerAngles.z * Mathf.Deg2Rad), Mathf.Cos(randomDispWorldAngle.eulerAngles.z * Mathf.Deg2Rad), 0);

            //發射子彈(位置在砲管軸心偏移x點(shellGene_worldPos))
            var instanceShell = GameManager.ObjectPool_TakeFrom(GameManager.instance_ThisScript.set_prefab_Shell, GameManager.set_objectPool_Shell);
            Vector3 forceForPushShell = set_shell_Speed * randomDispWorldVector; //設定發射推力，並依發砲點當地轉向換算世界轉向，計算推力向量
            instanceShell.transform.position = shellGene_worldPos;
            instanceShell.transform.rotation = randomDispWorldAngle;   //物件角度套用隨機決定的發射角度

            //對取出預製物進行設值；如果沒有該component則先檢查並添加，再設值
            if (instanceShell.GetComponent<Rigidbody2D>() == null) { instanceShell.AddComponent<Rigidbody2D>(); }                               //給予實例剛體以供施力
            if (instanceShell.GetComponent<GenericObjectDamageSetting>() == null) { instanceShell.AddComponent<GenericObjectDamageSetting>(); } //傷害設定腳本
            if (instanceShell.GetComponent<GenericShellSetting>() == null) { instanceShell.AddComponent<GenericShellSetting>(); }               //子彈狀態腳本
            if (instanceShell.GetComponent<SpriteRenderer>() == null) { instanceShell.AddComponent<SpriteRenderer>(); }               //預製物圖片

            instanceShell.GetComponent<GenericPrefab_AutoReturnOP>().onField_lifeTime = set_shell_Lifetime; //套用子彈壽命

            instanceShell.GetComponent<Rigidbody2D>().gravityScale = 0f; //設定實例剛體重力0防止下墜
            instanceShell.GetComponent<Rigidbody2D>().AddForce(forceForPushShell, ForceMode2D.Impulse); //將發射推力套於實例

            instanceShell.GetComponent<GenericObjectDamageSetting>().info_DamageType = set_shell_Type;
            instanceShell.GetComponent<GenericObjectDamageSetting>().info_GunDiameter = set_gun_Diameter;
            instanceShell.GetComponent<GenericObjectDamageSetting>().info_DamageValue = set_shell_Damage;
            instanceShell.GetComponent<GenericObjectDamageSetting>().info_PenetraionValue = set_shell_Penetrate;
            instanceShell.GetComponent<GenericObjectDamageSetting>().info_ExplosionRadious = set_shell_EXRadious;
            instanceShell.GetComponent<GenericObjectDamageSetting>().info_UserName = set_user_ID; //物品持有者供外訪
            instanceShell.GetComponent<SpriteRenderer>().sprite = set_shell_Icon;
            instanceShell.GetComponent<SpriteRenderer>().color = set_allPartColor;  //設定子彈顏色同車子

            usingTankAudio.play_TankFire();

            //開始隱藏圖片+至砲管前加速運動+發砲特效生成協程
            StartCoroutine(Shell_HideSprite_and_AccPos_BeforeFirePos(instanceShell, set_gameObject_FirePos.transform));

            //判斷參數初始化
            gun_ReloadCountTime = set_gun_ReoladSetTime; //倒數時間=裝填時間
            gun_MovingPosY = 0f;
            gun_PosHoldCountTime = set_muzzle_HoldTime;

            isReloading = true; //進入裝填狀態
            muzzle_IsPush = true; //進入制退狀態
        }
    }
    IEnumerator Shell_HideSprite_and_AccPos_BeforeFirePos(GameObject shell,Transform firePos) 
    {
        shell.GetComponent<SpriteRenderer>().enabled = false;
        shell.GetComponentInChildren<TrailRenderer>().enabled = false;


        //存取上幀位移 //如果此期間撞擊其他物體(速度改變)，解除位移拘束
        Vector2 lastShellVeolcity = shell.GetComponent<Rigidbody2D>().velocity;
        //Debug.Log(lastShellVeolcity);

        //計算子彈相對於開火點的地方軸向，若y>0，表示位置已超出炮口
        var shellLocalToFirePos = firePos.InverseTransformPoint(shell.transform.position);
        //設定限制位移，並包含加成係數，減少至炮口的時間差
        var shellAlongGunPos = new Vector2(0f, shellLocalToFirePos.y);


        //發射開火特效(位置在炮口)
        var instanceExplosion = GameManager.ObjectPool_TakeFrom(GameManager.instance_ThisScript.set_prefab_VFXshockwave, GameManager.set_objectPool_VFXshockwave);
        instanceExplosion.transform.position = set_gameObject_FirePos.position;
        instanceExplosion.transform.rotation = set_gameObject_FirePos.rotation;

        //未超出炮口期間，子彈沿砲管移動 //若是提前銷毀，或提前被跳彈，皆中斷該迴圈
        while (shell.activeInHierarchy == true && shellLocalToFirePos.y <= 0 && 
            Vector2.Angle(lastShellVeolcity, shell.GetComponent<Rigidbody2D>().velocity) < 5f) 
        {
            //Debug.Log("!!!");
            shellLocalToFirePos = firePos.InverseTransformPoint(shell.transform.position);
            shellAlongGunPos.y = shellLocalToFirePos.y + 0.2f;
            shell.transform.position = firePos.TransformPoint(shellAlongGunPos);

            lastShellVeolcity = shell.GetComponent<Rigidbody2D>().velocity;
            yield return new WaitForSeconds(1f / 30f);
        }
        //超出炮口顯示圖片
        shell.GetComponent<SpriteRenderer>().enabled = true;
        shell.GetComponentInChildren<TrailRenderer>().enabled = true;

        //未完成code
        //存取子彈相對速度，離開炮口再依炮口新方位改變其速度方向
        //Vector2 originalShell_localVelocity = firePos.InverseTransformVector(shell.GetComponent<Rigidbody2D>().velocity);
        //Debug.Log(originalShell_localVelocity);
        //超出炮口依新炮口方位調整砲彈速度方向
        //shell.GetComponent<Rigidbody2D>().velocity = 10f*firePos.TransformVector(originalShell_localVelocity);

        //yield return new WaitForSeconds(1f / 30f);
    }


    public void ShellReload() 
    {
        if (gun_ReloadCountTime > 0f) //如果計時尚未數完  
        {
            //裝填計時
            gun_ReloadCountTime -= Time.fixedDeltaTime;

            if (gun_ReloadCountTime <= 0f) //如果計時完成
            {
                gun_ReloadCountTime = 0f;    //計時歸0
                input_isFireShell = false; //按鈕重置為無觸發(防自動連續發射)
                isReloading = false;       //退出裝填狀態
            }
        }

        //注意砲管的pivot務必與砲塔的pivot重合! 圖片位置才會對!!!
        //圖片位置移動(僅移動砲管localY值)        
        if (muzzle_IsPush)//如果炮管開始制退
        {
            gun_MovingPosY = Mathf.MoveTowards(gun_MovingPosY, set_gun_MuzzleDistanceMax, set_muzzle_Speed_BackUp * Time.fixedDeltaTime);//等速推向極限
            if (gun_MovingPosY >= set_gun_MuzzleDistanceMax) //到達最深處時
            {
                gun_MovingPosY = set_gun_MuzzleDistanceMax; //且不超過極限深度
                muzzle_IsPush = false; //並且制退結束
                muzzle_IsBottom = true; //開始制止
            }
        }
        else if (muzzle_IsBottom)//如果炮管開始制止
        {
            gun_MovingPosY = set_gun_MuzzleDistanceMax;//深度為極限深度
            gun_PosHoldCountTime -= Time.fixedDeltaTime;//並開始制止到數
            if (gun_PosHoldCountTime <= 0)//直到制止倒數為0
            {
                muzzle_IsBottom = false;//結束制止
                muzzle_IsReturn = true;//開始制推
            }
        }
        else if (muzzle_IsReturn)//如果炮管開始制推
        {
            gun_MovingPosY = Mathf.Lerp(gun_MovingPosY, 0, set_muzzle_Speed_Forward * Time.fixedDeltaTime);//已曲線速度推向原位 //注意lerp會無窮盡的不等於目標值
        }

        if (!isReloading) //如果是非裝填中的狀態
        {
            gun_MovingPosY = 0f;//炮管維持原位
            muzzle_IsPush = false;//依此，制退、制止、制推流程結束
            muzzle_IsBottom = false;
            muzzle_IsReturn = false;
        }
        set_gameObject_Gun.localPosition = new Vector3(0f, -1f * gun_MovingPosY, 0f);//套用計算過程中的值至圖片位置
        //Debug.Log("裝填剩餘= " + gun_ReloadCountTime + "秒 ；砲管位置= " + gun_MovingPosY + "米");
    }

    //1.由本處raycast獲得碰撞物的資訊並回傳不同的部位資訊給傷害計算函數(1發砲彈限制僅1部位回傳)
    public void ArmorRaycastSetting()  //碰撞判定
    {
        //射線檢測參考點生成
        Vector3 box_FR = set_gameObject_Hull.TransformPoint(set_size_BoxWidth / 2f, set_size_BoxLength / 2f + set_size_BoxPivotBiasY, 0f);
        Vector3 box_FL = set_gameObject_Hull.TransformPoint(-set_size_BoxWidth / 2f, set_size_BoxLength / 2f + set_size_BoxPivotBiasY, 0f);
        Vector3 box_RR = set_gameObject_Hull.TransformPoint(set_size_BoxWidth / 2f, -set_size_BoxLength / 2f + set_size_BoxPivotBiasY, 0f);
        Vector3 box_RL = set_gameObject_Hull.TransformPoint(-set_size_BoxWidth / 2f, -set_size_BoxLength / 2f + set_size_BoxPivotBiasY, 0f);

        Vector3 wheel_R = set_gameObject_Hull.TransformPoint(set_size_BoxWidth / 2f, set_size_WheelPivot, 0f);
        Vector3 wheel_L = set_gameObject_Hull.TransformPoint(-set_size_BoxWidth / 2f, set_size_WheelPivot, 0f);

        //射線檢測線條生成
        RaycastHit2D hit_HullRight = Physics2D.Raycast(box_FR, Vector3.Normalize(box_RR - box_FR), set_size_BoxLength, objectsWithDamage);
        RaycastHit2D hit_HullLeft = Physics2D.Raycast(box_FL, Vector3.Normalize(box_RL - box_FL), set_size_BoxLength, objectsWithDamage);
        RaycastHit2D hit_HullFront = Physics2D.Raycast(box_FR, Vector3.Normalize(box_FL - box_FR), set_size_BoxWidth, objectsWithDamage);
        RaycastHit2D hit_HullRear = Physics2D.Raycast(box_RR, Vector3.Normalize(box_RL - box_RR), set_size_BoxWidth, objectsWithDamage);

        RaycastHit2D hit_Wheel_R = Physics2D.Raycast(wheel_R + (Vector3.Normalize(box_FR - box_RR) * 0.5f * set_size_WheelRadious), Vector3.Normalize(box_RR - box_FR), set_size_WheelRadious, objectsWithDamage);
        RaycastHit2D hit_Wheel_L = Physics2D.Raycast(wheel_L + (Vector3.Normalize(box_FR - box_RR) * 0.5f * set_size_WheelRadious), Vector3.Normalize(box_RR - box_FR), set_size_WheelRadious, objectsWithDamage);


        //除錯用顯示模組
        Color checkColor_Right = hit_HullRight ? Color.red : Color.green;
        Color checkColor_Left = hit_HullLeft ? Color.red : Color.green;
        Color checkColor_Front = hit_HullFront ? Color.red : Color.green;
        Color checkColor_Rear = hit_HullRear ? Color.red : Color.green;
        Color checkColor_Wheel_FR = hit_Wheel_R ? Color.red : Color.green;
        Color checkColor_Wheel_FL = hit_Wheel_L ? Color.red : Color.green;

        
        UnityEngine.Debug.DrawRay(box_FR, (box_RR - box_FR) * set_size_BoxLength / Vector3.Distance(box_FR, box_RR), checkColor_Right); //右側
        UnityEngine.Debug.DrawRay(box_FL, (box_RL - box_FL) * set_size_BoxLength / Vector3.Distance(box_FL, box_RL), checkColor_Left); //左側
        UnityEngine.Debug.DrawRay(box_FR, (box_FL - box_FR) * set_size_BoxWidth / Vector3.Distance(box_FL, box_FR), checkColor_Front); //前方
        UnityEngine.Debug.DrawRay(box_RR, (box_RL - box_RR) * set_size_BoxWidth / Vector3.Distance(box_RL, box_RR), checkColor_Rear);//後方

        UnityEngine.Debug.DrawRay(wheel_R + (Vector3.Normalize(box_FR - box_RR) * 0.5f * set_size_WheelRadious), Vector3.Normalize(box_RR - box_FR) * set_size_WheelRadious, checkColor_Wheel_FR); //右前輪
        UnityEngine.Debug.DrawRay(wheel_L + (Vector3.Normalize(box_FR - box_RR) * 0.5f * set_size_WheelRadious), Vector3.Normalize(box_RR - box_FR) * set_size_WheelRadious, checkColor_Wheel_FL); //左前輪


        GameObject hitSource;
        //允許一幀內接收一次傷害
        //if(hitSource.GetComponent<Rigidbody2D>()==null)

            //注意一幀僅使用一次傷害計算函數，不可以一顆子彈打多個部位
            //否則造成同物件重複被回收於物件池，使發射中的子彈因此又被重複改動!!!

        if (hit_Wheel_R)
        {
            //Debug.Log("右輪受擊");
            hitSource = hit_Wheel_R.collider.gameObject;
            HitDamageCalculator(AreaType.WHEEL, set_wheel_armor, box_FR, box_RR, hitSource);
        }
        else if (hit_Wheel_L)
        {
            //Debug.Log("左輪受擊");
            hitSource = hit_Wheel_L.collider.gameObject;
            HitDamageCalculator(AreaType.WHEEL, set_wheel_armor, box_FL, box_RL, hitSource);
        }
        else if (hit_HullFront)  
        {
            //Debug.Log("正面受擊");
            hitSource = hit_HullFront.collider.gameObject;
            HitDamageCalculator(AreaType.HULL, set_armor_Front, box_FR, box_FL, hitSource);                
        }
        else if (hit_HullRear)
        {
            //Debug.Log("後面受擊");
            hitSource = hit_HullRear.collider.gameObject;
            HitDamageCalculator(AreaType.HULL, set_armor_Rear, box_RR, box_RL, hitSource);
        }
        else if (hit_HullRight)
        {
            //Debug.Log("右側受擊");
            hitSource = hit_HullRight.collider.gameObject;
            HitDamageCalculator(AreaType.HULL, set_armor_Side, box_FR, box_RR, hitSource);
        }
        else if (hit_HullLeft)
        {
            //Debug.Log("左側受擊");
            hitSource = hit_HullLeft.collider.gameObject;
            HitDamageCalculator(AreaType.HULL, set_armor_Side, box_FL, box_RL, hitSource);
        }
    }

    void HitDamageCalculator(AreaType areaName, float areaArmor, Vector2 areaPointStart, Vector2 areaPointEnd, GameObject sourceOfDamage) //傷害扣寫演算(打擊部位, 回傳裝甲厚度, 回傳射線起點, 回傳射線終點, 回傳碰觸物件)
    {
        //自己發射的子彈無法擊傷自己，但由自己造成的間接傷害仍可以接收
        if (sourceOfDamage.GetComponent<GenericShellSetting>() && sourceOfDamage.GetComponent<GenericObjectDamageSetting>().info_UserName == set_user_ID)
        {
            return;
        }

        //Debug.Log("收到碰撞!");
        //傷害與穿深的接收值小幅度變動(RNG)
        float sourceDamageValue = sourceOfDamage.GetComponent<GenericObjectDamageSetting>().info_DamageValue * Random.Range(0.75f, 1.25f);
        float sourcePenetration = sourceOfDamage.GetComponent<GenericObjectDamageSetting>().info_PenetraionValue * Random.Range(0.75f, 1.25f);
        GenericObjectDamageSetting.DamageType sourceDamageType = sourceOfDamage.GetComponent<GenericObjectDamageSetting>().info_DamageType;

        
        //設定穿透檢定參數，若非子彈物件則使用預設值
        Vector2 areaSurfaceNormal = Vector2.up;        
        Vector2 sourceVelocityVector = Vector2.down;        
        float hitAngle = 0f;

        
        //若是收到的是炸彈傷害，未引爆期間無傷
        if (sourceOfDamage.GetComponent<Objects_TNTBomb>() != null)
        {
            if (sourceOfDamage.GetComponent<Objects_TNTBomb>().isExplosion == false)
            {
                //Debug.Log("貼觸炸彈!");
                return;
            }
        } 

        if (sourceOfDamage.GetComponent<GenericShellSetting>() != null)
        {
            areaSurfaceNormal = Vector2.Perpendicular(areaPointEnd - areaPointStart).normalized; //取得裝甲法線(未修正)
            sourceVelocityVector = sourceOfDamage.GetComponent<Rigidbody2D>().velocity;          //取得入射速度向量
            hitAngle = Vector2.Angle(areaSurfaceNormal, sourceVelocityVector);                   //取得入射角度(相對於裝甲法線)


            if (hitAngle < 90f)
            {
                areaSurfaceNormal *= (-1f); //入射角度異常則翻轉法線 
            }
            if (hitAngle > 90f)
            {
                hitAngle = 180f - hitAngle;
            }
            //如果砲彈口徑兩倍壓制裝甲厚度，則入射角度扣減(轉正效應)
            if (sourceOfDamage.GetComponent<GenericObjectDamageSetting>().info_GunDiameter > areaArmor * 2f)
            {
                hitAngle = Mathf.Clamp((hitAngle - 5f), 0f, 90f);
            }
            //萃取完資料後反轉碰撞速度，使其挪離原位置(防止施力期間碰撞多次判定)

            sourceOfDamage.GetComponent<Rigidbody2D>().velocity = Vector3.zero;
            sourceOfDamage.GetComponent<Transform>().position += (Vector3)areaSurfaceNormal * 0.1f;
            //暫時關閉碰撞器(記得要重新開啟)
            sourceOfDamage.GetComponent<BoxCollider2D>().enabled = false;
        }
        //sourceOfDamage.GetComponent<Transform>().position.

        if (sourceDamageType == GenericObjectDamageSetting.DamageType.HE)  //如果是高爆傷害，穿深和傷害要隨距離衰減
        {
            float hullRadious = (set_size_BoxLength + set_size_BoxLength) * 0.5f;
            float hitDistance = Vector2.Distance(gameObject.transform.position, sourceOfDamage.transform.position) - hullRadious;
            hitDistance = Mathf.Clamp(hitDistance, 0, Mathf.Infinity);

            if(hitDistance>=0)
            sourceDamageValue /= Mathf.Pow(2, (hitDistance / 5f));
            sourcePenetration /= Mathf.Pow(2, (hitDistance / 5f));
            //Debug.Log(sourcePenetration + "/" + sourceOfDamage.GetComponent<GenericObjectDamageSetting>().info_PenetraionValue);
        }
        

        bool isPenetrate = false;
        bool isRiochet = false;
        //穿透判定條件 //1.口徑三倍壓制 //2.入射角要小於45度 //3.等效穿深要高於裝甲厚度
        if (sourceOfDamage.GetComponent<GenericObjectDamageSetting>().info_GunDiameter > areaArmor * 3) //如果有口徑壓制，直接穿透
        {
            isPenetrate = true;
            isRiochet = false;
        }        
        else if (hitAngle >= 45f && sourceDamageType == GenericObjectDamageSetting.DamageType.AP) //如果是AP彈且入射角度超過45，判定跳彈 
        {
            isPenetrate = false;
            if (areaName == AreaType.HULL)        //只有車身才可以跳彈
            {
                isRiochet = true;
            }
            else if (areaName == AreaType.WHEEL)  //履帶無法跳彈，直接判為沒穿透
            {
                isRiochet = false;
            }
        }
        else if (areaArmor < sourcePenetration * Mathf.Cos(hitAngle * Mathf.Deg2Rad))          //入射角度沒太大，等效穿深夠，就可以穿透
        {
            isPenetrate = true;
            isRiochet = false;
        }
        //Debug.Log("穿透="+isPenetrate);
        //Debug.Log("跳彈=" + isRiochet);

        //-----------傷害計算部分------------
        float damageToHP = 0f; //演算後實際扣減的有效傷害(預設0，造成有效傷害才給值)
        if (sourceDamageType == GenericObjectDamageSetting.DamageType.AP)
        {
            if (isPenetrate == true)
            {
                //Debug.Log("擊穿!");
                damageToHP = sourceDamageValue;
                sourceOfDamage.GetComponent<GenericPrefab_AutoReturnOP>().ReturnToObjectPool();//   GenericShellSetting>().ShellDestroyAndExplosion();

                if (areaName == AreaType.HULL)
                {
                    usingTankAudio.play_Shell_Penetrate_isNPC(sourceOfDamage.GetComponent<GenericObjectDamageSetting>());
                }
            }
            else if (isRiochet == true && sourceOfDamage.GetComponent<GenericShellSetting>() != null)  //若為車身，且可跳彈&&是子彈物體，可跳彈
            {
                //Debug.Log("發生跳彈!"); 
                sourceOfDamage.GetComponent<Rigidbody2D>().velocity = Vector2.Reflect(sourceVelocityVector, areaSurfaceNormal); //賦予子彈物件新速度與沿速度之旋轉值
                sourceOfDamage.GetComponent<Transform>().rotation = Quaternion.Euler(0, 0, Mathf.Atan2(-sourceOfDamage.GetComponent<Rigidbody2D>().velocity.x, sourceOfDamage.GetComponent<Rigidbody2D>().velocity.y) * Mathf.Rad2Deg);
                sourceOfDamage.GetComponent<GenericObjectDamageSetting>().info_PenetraionValue *= 0.75f; //跳彈之後穿深衰減25%
                ////跳彈之後重新開啟該彈的碰撞器 //零距離貼炮仍會造成錯誤，待修
                sourceOfDamage.GetComponent<BoxCollider2D>().enabled = true;

                //如果被對方插入砲管0距離射擊，可能子彈圖片未開啟，固本處執行開啟保障
                sourceOfDamage.GetComponent<SpriteRenderer>().enabled = true;
                sourceOfDamage.GetComponentInChildren<TrailRenderer>().enabled = true;

                //產生跳彈火花
                var riochetSpark = GameManager.ObjectPool_TakeFrom(GameManager.instance_ThisScript.set_prefab_VFXexplosion_AP, GameManager.set_objectPool_VFXexplosion_AP);
                riochetSpark.transform.position = sourceOfDamage.transform.position;


                //若是車體受擊展示跳彈訊息
                if (areaName == AreaType.HULL)
                {
                    DelayTextInfo(0f, "Riochet!", 0);
                    //接受到是玩家的子彈，才能觸發此音效
                    usingTankAudio.play_Shell_Riochet_isNPC(sourceOfDamage.GetComponent<GenericObjectDamageSetting>());
                }                
            }
            else if (isPenetrate == false)
            {
                sourceOfDamage.GetComponent<GenericPrefab_AutoReturnOP>().ReturnToObjectPool();//   GenericShellSetting>().ShellDestroyAndExplosion();

                //接受到是玩家的子彈，才能觸發此音效
                usingTankAudio.play_Shell_noPenetrate_isNPC(sourceOfDamage.GetComponent<GenericObjectDamageSetting>());
                
                //Debug.Log("未穿透!");
                //展示未穿透訊息
                /*if (areaName == AreaType.HULL)
                {
                    DelayTextInfo(0f, "Critical Hit!", 0);
                }
                else if (areaName == AreaType.WHEEL) 
                {
                    DelayTextInfo(0.2f, "Hit Track No Damage!", 0);
                }*/
            }
        }
        else if (sourceDamageType == GenericObjectDamageSetting.DamageType.HE) 
        {
            if (isPenetrate == true)
            {
                damageToHP = sourceDamageValue;
                if (areaName == AreaType.HULL)
                {
                    DelayTextInfo(0.2f, "HE Penetration!", 0);
                    usingTankAudio.play_Shell_Penetrate_isNPC(sourceOfDamage.GetComponent<GenericObjectDamageSetting>());
                }
                //Debug.Log("HE穿透!,傷害值=" + damageToHP) ;
            }
            else if (isPenetrate == false) 
            {
                damageToHP = Mathf.Clamp((sourceDamageValue / 2f) - areaArmor, 0f, sourceDamageValue);
                //Debug.Log("HE未穿透!,傷害值=" + damageToHP);
                //展示未穿透訊息
                if (areaName == AreaType.HULL && damageToHP > 0f)
                {
                    DelayTextInfo(0.2f, "Critical Hit!", 0);
                    usingTankAudio.play_Shell_noPeneButDamage_isNPC(sourceOfDamage.GetComponent<GenericObjectDamageSetting>());
                }
            }

            if (sourceOfDamage.GetComponent<GenericShellSetting>() != null)
            {
                sourceOfDamage.GetComponent<GenericPrefab_AutoReturnOP>().ReturnToObjectPool();//   GenericShellSetting>().ShellDestroyAndExplosion();
            }
        }

        if (damageToHP <= 0)
        {
            if (areaName == AreaType.WHEEL && updateHP_Wheel > 0)
            {
                DelayTextInfo(0.2f, "Hit Track!", 0);
            }
            DelayTextInfo(0.2f, "No Damage!", 0);
        }

        //設定扣血後血量套用和暫存資訊更新(放在這邊進行，因為本函才有接收資料)
        if (areaName == AreaType.HULL)
        {
            HullHP_DamageAfterCalculate_DisplayAndRecord(sourceOfDamage, damageToHP);
        }
        if (areaName == AreaType.WHEEL)
        {
            //輪子受損，車身亦扣血
            HullHP_DamageAfterCalculate_DisplayAndRecord(sourceOfDamage, damageToHP);

            //履帶已損毀狀態再受到攻擊，則顯示提醒訊息
            if (updateHP_Wheel == 0) 
            {
                DelayTextInfo(0.2f, "Hit Track was Broken!", 0);
            }
            //輪子扣血不顯示受到傷害
            //Debug.Log("輪子受到攻擊!，傷害=" + damageToHP);
            updateHP_Wheel -= damageToHP;   //進行輪子血量扣減
            SetWheel_HPstate();             //調整輪子血量狀態
        }      
    }
    //車身扣血函數，不論是單純車身被打中，還是輪子也被打中，都使用
    private void HullHP_DamageAfterCalculate_DisplayAndRecord(GameObject sourceOfDamage,float damageToHP) 
    {

        //Debug.Log("車身受到攻擊!，傷害=" + damageToHP);
        if ((updateHP_Hull - damageToHP) <= 0)       //如果血量低於受到傷害
        {
            //接受到是玩家的子彈，才能觸發此音效
            usingTankAudio.play_KillEnemy_isNPC(sourceOfDamage.GetComponent<GenericObjectDamageSetting>());

            temp_ReceiveDamageValue = updateHP_Hull; //紀錄傷害則是血量值
            updateHP_Hull = 0;
        }
        else                                         //如果血量高於受到傷害
        {
            temp_ReceiveDamageValue = damageToHP;    //紀錄傷害則是傷害值
            updateHP_Hull -= damageToHP;             //並進行車身血量扣減
        }
        //不論是否有造成傷害，都記錄攻擊對象資料
        temp_ReceiveName = sourceOfDamage.GetComponent<GenericObjectDamageSetting>().info_UserName;
        //調整車身血量狀態
        SetHull_HPstate();

        //車身有扣血則展示受到傷害數字
        if (damageToHP > 0f)
        {
            var tempDMGInfo = GameManager.ObjectPool_TakeFrom(GameManager.instance_ThisScript.set_prefab_HPnumText, GameManager.set_objectPool_prefab_HPnumText);
            StartCoroutine(tempDMGInfo.GetComponent<UI_S2_HPnum>().HPnumAnim(gameObject, (int)temp_ReceiveDamageValue));
        }
    }


    //此函數只有受傷才使用，不放在update()
    //修復圖示需用物件池，本處不進行其動畫
    void SetWheel_HPstate() 
    {
        updateHP_Wheel = Mathf.Clamp(updateHP_Wheel, 0f, set_wheel_HP);

        if (wheel_IsRepairing == true) //輪子未修好狀態若再被攻擊，倒數重置(=延長)
        {
            wheel_RepairCountTime = set_wheel_RepairTime;
        }        
        else if (wheel_IsRepairing == false && updateHP_Wheel <= 0f) //如果輪子未故障但血量見底            
        {
            //進入維修倒數迴圈(單次激活)
            StartCoroutine(WheelHPrecovery());
        }
    }
    
    IEnumerator WheelHPrecovery() //進入維修倒數迴圈
    {
        //展示維修訊息
        DelayTextInfo(0.5f, "Track Broken!", 0);
        usingTankAudio.play_TrackBreak_isPlayer();

        //叫出維修圖示
        var icon = GameManager.ObjectPool_TakeFrom(GameManager.instance_ThisScript.set_prefab_WheelRepairIcon, GameManager.set_objectPool_WheelRepairIcon);
        icon.transform.position = transform.position;
        icon.transform.rotation = Quaternion.identity;

        //圖示需先設定使用者，以利其存取資料並實時追蹤位置
        icon.GetComponent<UI_S2_RepairIcon>().IconUser = gameObject.GetComponent<GenericTankController>();

        updateHP_Wheel = 0f;
        wheel_IsRepairing = true;                         //進入維修狀態(車身不能移動和轉動)
        wheel_RepairCountTime = set_wheel_RepairTime;     //倒數重置為設定秒數

        //for (float i = wheel_RepairCountTime; i > 0; i--)
        //for迴圈初始值只能設一次，改用while
        while (wheel_RepairCountTime > 0 && wheel_IsRepairing) //每秒扣減倒數秒數(期間秒數可被重設) 
        {
            icon.transform.position = transform.position;
            wheel_RepairCountTime -= 1f;
            wheel_RepairCountTime = Mathf.Clamp(wheel_RepairCountTime, 0, set_wheel_RepairTime);

            yield return new WaitForSeconds(1f);
        }
        //倒數完成
        //Debug.Log("倒數完成!");
        updateHP_Wheel = set_wheel_HP;                //修復完成恢復滿血
        wheel_RepairCountTime = set_wheel_RepairTime; //倒數重置為設定秒數
        wheel_IsRepairing = false;                    //結束維修狀態 //注意維修狀態也影響圖示的動畫狀態

        if (!isDead)
        { //未被擊破才可使用此訊息與音效

            DelayTextInfo(0.5f, "Track Repaired!"+"\n"+"Go On!", 1);
            usingTankAudio.play_TrackRepaired_isPlayer();
        }
    }

    //此函數只有受傷才使用，不放在update()
    //可被外部腳本訪問
    public void SetHull_HPstate() 
    {
        updateHP_Hull = Mathf.Clamp(updateHP_Hull, 0f, set_Hull_HP);


        if (temp_previousUpdateHP_Hull < updateHP_Hull) 
        {
            var HPadd = Mathf.CeilToInt(updateHP_Hull - temp_previousUpdateHP_Hull);
            DelayTextInfo(0.2f, "HP +"+HPadd.ToString(), 1);
        }
        temp_previousUpdateHP_Hull = updateHP_Hull;

        var HPratio = updateHP_Hull / set_Hull_HP;

        if (HPratio > 0.67f)
        {
            set_damage_VFXlight.SetActive(false);
            set_damage_VFXmed.SetActive(false);
            set_damage_VFXheavy.SetActive(false);
        }
        else if (HPratio > 0.33f)
        {
            set_damage_VFXlight.SetActive(true);
            set_damage_VFXmed.SetActive(false);
            set_damage_VFXheavy.SetActive(false);
        }
        else if (HPratio > 0.10f)
        {
            set_damage_VFXlight.SetActive(false);
            set_damage_VFXmed.SetActive(true);
            set_damage_VFXheavy.SetActive(false);
        }
        else if (HPratio > 0f)
        {
            set_damage_VFXlight.SetActive(false);
            set_damage_VFXmed.SetActive(false);
            set_damage_VFXheavy.SetActive(true);
        }
        else if (HPratio <= 0.0f) 
        {
            isDead = true;
            set_damage_VFXlight.SetActive(false);
            set_damage_VFXmed.SetActive(false);
            set_damage_VFXheavy.SetActive(false);
            set_damage_VFXdead.SetActive(true);
            //置換貼圖
            set_gameObject_Hull.gameObject.GetComponent<SpriteRenderer>().sprite = set_spriteDead_Hull;
            set_gameObject_Turrent.gameObject.GetComponent<SpriteRenderer>().sprite = set_spriteDead_Turrent;
            set_gameObject_Gun.gameObject.GetComponent<SpriteRenderer>().sprite = set_spriteDead_Gun;

            usingTankAudio.play_PlayerDead_isPlayer();
        }
    }

    //方便多筆資料使用時排序輸出
    float geneText_WaitingTime = 0f;
    private void DelayTextInfo(float delayTime, string inputInfo, int colorSet) 
    {
        geneText_WaitingTime += delayTime;
        StartCoroutine(GeneText(geneText_WaitingTime, inputInfo ,colorSet));
    }

    IEnumerator GeneText(float delayTime, string inputInfo, int colorSet) 
    {
        yield return new WaitForSeconds(delayTime);
        geneText_WaitingTime -= delayTime;
        var Info = GameManager.ObjectPool_TakeFrom(GameManager.instance_ThisScript.set_prefab_HPnumText, GameManager.set_objectPool_prefab_HPnumText);
        StartCoroutine(Info.GetComponent<UI_S2_HPnum>().HPinfoAnim(gameObject, inputInfo, colorSet));
    }

}
