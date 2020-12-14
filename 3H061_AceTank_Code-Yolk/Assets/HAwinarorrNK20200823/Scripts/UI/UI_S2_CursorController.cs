using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_S2_CursorController : MonoBehaviour
{

    public Canvas set_actCanvas;
    public GameObject set_cursorCenter;    //玩家瞄準心
    public GameObject set_cursorFrame;    //玩家瞄準外框
    public Image set_cursorFrame_LoadingBar; //玩家瞄準框裝填動態欄

    public GenericTankController set_PlayerTank;      //玩家操控車輛
    public Transform set_PlayerFirePoint;  //玩家控制車輛開火點
    public float set_circleDelay;

    


    void Start()
    {
        set_cursorFrame.GetComponent<Image>().color = Color.red;
        set_cursorFrame_LoadingBar.color = Color.green;
        set_cursorFrame.transform.SetParent(set_actCanvas.transform); //綁定外框於canvas顯示
        
    }

    // Update is called once per frame
    void Update()
    {
        CursorCenterControl();
    }
    private void FixedUpdate()
    {
        CursorFrame_MoveControl();
        CursorFrame_ColorControl();
    }

    void CursorCenterControl() 
    {
        var ScreenPoint = Input.mousePosition;
        ScreenPoint.z = Camera.main.transform.position.z * (-1f);
        transform.position = Camera.main.ScreenToWorldPoint(ScreenPoint);
    }

    void CursorFrame_ColorControl()
    {
        //如果玩家陣亡，變色並且停止後面的功能
        if (set_PlayerTank.isDead == true)
        {
            set_cursorFrame_LoadingBar.color = Color.gray;
            set_cursorFrame.GetComponent<Image>().color = Color.black;
            return;
        }

        //如果玩家裝填中，進行填充值動畫
        if (set_PlayerTank.isReloading)
        {
            float cutNum = 12f;
            set_cursorFrame_LoadingBar.color = Color.yellow;
            var barFill = 1 - (Mathf.Round(cutNum * (set_PlayerTank.gun_ReloadCountTime / set_PlayerTank.set_gun_ReoladSetTime)) / cutNum);
            set_cursorFrame_LoadingBar.fillAmount = barFill;
        }//如果專填完畢，切回原顏色
        else if (set_PlayerTank.isReloading == false)
        {
            set_cursorFrame_LoadingBar.fillAmount = 1f;
            set_cursorFrame_LoadingBar.color = Color.green;
        }

    }
    void CursorFrame_MoveControl()  
    {        
        var circlePos = set_cursorFrame.transform.position;                                                                    //取得外框世界座標
        circlePos = Vector2.Lerp(circlePos, set_cursorCenter.transform.position, set_circleDelay * Time.fixedDeltaTime); //計算一般漸進位置

        //遊玩模式限制瞄準框沿砲線移動
        if (GameManager.instance_ThisScript.isPaused == false)
        {
            //參考玩家開火點，限制瞄準框相對橫向移動
            circlePos = set_PlayerFirePoint.InverseTransformPoint(circlePos);
            circlePos.x = 0f;
            //參考玩家開火點，限制瞄準框穿過車體
            if (circlePos.y < 0f)
            {
                circlePos.y = 0f;
            }
            circlePos = set_PlayerFirePoint.TransformPoint(circlePos);  //解除參考開火點取得世界座標
        }
        //暫停模式不限制移動，漸進即可
        set_cursorFrame.transform.position = circlePos;            //套用計算座標於外框


    }
}
