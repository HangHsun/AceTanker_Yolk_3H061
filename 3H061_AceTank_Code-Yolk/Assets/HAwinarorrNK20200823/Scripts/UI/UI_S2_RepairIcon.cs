using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_S2_RepairIcon : OBJECTS_PARENT_ON_WININFO 
{
    //private Image setRepairIcon;
    public Image set_RepairIconBar;  //即時維修進度圖示
    //public Image set_RepairIconBase; //維修底圖圖示
    private CanvasGroup set_CanvasGroup; //整體維修圖示

    public Color[] set_repairStageColor = new Color[3];
    public float[] set_repairStageRatio = new float[2];
    public float set_repairFadeSpeed;

    //此變數必須由使用者設定!才能有正確位置
    public GenericTankController IconUser;
    //此兩個變數本處不改寫其值
    float user_MaxTime;    //存取使用者設定的最大維修需時
    float user_UpdateTime; //存取使用者即時的維修需時
    //Color iconBaseColor;


    private void OnEnable()
    {
        gameObject.GetComponent<RectTransform>().localScale = Vector3.one;
        set_CanvasGroup = gameObject.GetComponent<CanvasGroup>();
        set_CanvasGroup.alpha = 1f;

        set_RepairIconBar.color = set_repairStageColor[0];
    }

    private void FixedUpdate()
    {
        if (IconUser==null) { return; } //若無手動開啟與指定使用者則不可執行功能
        //以介面為父物件，會隨畫面挪動，需實時跟上使用者位置
        gameObject.transform.position = IconUser.transform.position;

        IconAnim_ColorAndFill();
        IconAnim_FadeAfterRepair();
    }

    void IconAnim_ColorAndFill() 
    {
        if (IconUser.wheel_IsRepairing==false) { return; } //如果是非維修狀態則不執行

        user_MaxTime = IconUser.set_wheel_RepairTime;
        user_UpdateTime = IconUser.wheel_RepairCountTime;
        var repairRatio = Mathf.Clamp(user_UpdateTime / user_MaxTime, 0f, 1f);

        set_RepairIconBar.fillAmount = repairRatio;
        if (repairRatio > set_repairStageRatio[0])
        {
            set_RepairIconBar.color = set_repairStageColor[0];
        }
        else if (repairRatio > set_repairStageRatio[1])
        {
            set_RepairIconBar.color = set_repairStageColor[1];
        }
        else 
        {
            set_RepairIconBar.color = set_repairStageColor[2];
        }
    }

    void IconAnim_FadeAfterRepair()
    {
        //如果是維修中且戰車存活則不才執行
        if (IconUser.wheel_IsRepairing == true && IconUser.isDead == false) { return; }

        //如果是維修完畢或陣亡狀態才執行
        //跟隨玩家位置，並逐漸淡出圖示
        
        gameObject.transform.position = IconUser.transform.position;
        set_CanvasGroup.alpha = Mathf.MoveTowards(set_CanvasGroup.alpha, 0, Time.fixedDeltaTime * set_repairFadeSpeed);


        if (set_CanvasGroup.alpha == 0)
        {

            set_RepairIconBar.fillAmount = 0f;
            IconUser = null;                                                                         //清空使用者欄位
            GameManager.ObjectPool_ReturnTo(gameObject, GameManager.set_objectPool_WheelRepairIcon); //此段動畫播畢後才返回物件池
        }
    }



}
