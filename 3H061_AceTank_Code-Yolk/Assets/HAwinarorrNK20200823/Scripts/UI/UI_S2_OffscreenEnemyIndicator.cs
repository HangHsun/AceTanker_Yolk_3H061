using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_S2_OffscreenEnemyIndicator : MonoBehaviour
{
    /*
     本腳本掛載物件，以相機為父物件
     不可以使用虛擬相機當座標參考(結果會錯誤)
     
     只能使用銀幕座標進行計算(優點=水平/垂直界線能保持等距)
     圖片層級在全場景物件之下
     */

    float set_posZ = 10f;
    public float set_SSboundShrink;
    public Color set_ifNPC_isMed;
    public Color set_ifNPC_isHeavy;
    public Color set_ifNPC_isSuperHeavy;



    public GameObject GMset_refNPC;
    public GameObject GMset_refPlayer;
    Camera CSset_refCamera;
    SpriteRenderer CSset_ArrowSprite;

    bool OEIisUsing = false;

    //螢幕空間座標(左下(0,0)；右上(寬,高))
    Vector2 SSpos_NPC, SSpos_Player;
    Vector2 SSbound_topRight;
    Vector2 SSbound_botLeft;
    Vector2 SScenter;
    Vector2 SSbound_arrowLimit;



    private void OnEnable()
    {
        OEIisUsing = true;

        CSset_refCamera = GameObject.FindObjectOfType<Camera>();
        gameObject.transform.SetParent(CSset_refCamera.transform);        
        CSset_ArrowSprite = gameObject.GetComponent<SpriteRenderer>();
        CSset_ArrowSprite.enabled = false;

        //設定瑩幕空間下指標活動範圍
        SSbound_topRight.x = CSset_refCamera.pixelWidth - set_SSboundShrink;
        SSbound_topRight.y = CSset_refCamera.pixelHeight - set_SSboundShrink;
        SSbound_botLeft = set_SSboundShrink * Vector2.one;

        SScenter.x = CSset_refCamera.pixelWidth / 2f;
        SScenter.y = CSset_refCamera.pixelHeight / 2f;

    }


    private void FixedUpdate()
    {


        OEI_CheckRefs_And_ReturnPool();
        OEI_SwitchSprite();
        OEI_LimitPos();
        OEI_Rotate();
    }


    private void OEI_CheckRefs_And_ReturnPool()
    {
        if (OEIisUsing == true)
        {
            //由於物件池生成至啟用期間，會多自動執行一次OnEnable，故此設定不可放在其他的OnEnable呼叫(否則參考對象為空)
            //依照設定NPC類型變更本指標顏色
            if (GMset_refNPC.GetComponent<AI_GenericSetting>().GMset_ActType_InGM == GameManager.TankActType.MediumTank)
            { CSset_ArrowSprite.color = set_ifNPC_isMed; }
            else if (GMset_refNPC.GetComponent<AI_GenericSetting>().GMset_ActType_InGM == GameManager.TankActType.HeavyTank)
            { CSset_ArrowSprite.color = set_ifNPC_isHeavy; }
            else if (GMset_refNPC.GetComponent<AI_GenericSetting>().GMset_ActType_InGM == GameManager.TankActType.SuperHeavyTank)
            { CSset_ArrowSprite.color = set_ifNPC_isSuperHeavy; }
        }

        if (GMset_refNPC.GetComponent<GenericTankController>().isDead || GMset_refPlayer.GetComponent<GenericTankController>().isDead)
        {
            OEIisUsing = false;
            GameManager.ObjectPool_ReturnTo(this.gameObject, GameManager.set_objectPool_OEIarrow);
            
            return;
        }
    }

    private void OEI_SwitchSprite()
    {
        SSpos_NPC = CSset_refCamera.WorldToScreenPoint(GMset_refNPC.transform.position);
        SSpos_Player = CSset_refCamera.WorldToScreenPoint(GMset_refPlayer.transform.position);
        //SSDist_NPC2Player = SSpos_NPC - SSpos_Player;

        //Debug.Log("SSpos_Player" + SSpos_Player);

        //基於雙方相對座標開關
        //檢視NPC在該坐標系是否超過邊界範圍
        if (SSpos_NPC.x <SSbound_botLeft.x || SSpos_NPC.x>SSbound_topRight.x || SSpos_NPC.y < SSbound_botLeft.y || SSpos_NPC.y > SSbound_topRight.y)
        {
            CSset_ArrowSprite.enabled = true;
        }
        else 
        {
            CSset_ArrowSprite.enabled = false;
        }
    }
    private void OEI_LimitPos() 
    {
        //圖片開啟才計算其位置
        if (CSset_ArrowSprite.enabled == false) { return; }


        Vector3 limitArrowPos = Vector3.zero;
        SSbound_arrowLimit = SSbound_topRight;

        if (SSpos_NPC.x < SScenter.x) 
        {
            SSbound_arrowLimit.x = SSbound_botLeft.x;
        }
        if (SSpos_NPC.y < SScenter.y) 
        {
            SSbound_arrowLimit.y = SSbound_botLeft.y;
        }

        float invLerp2center_X = Mathf.Clamp01(Mathf.InverseLerp(SSpos_NPC.x, SScenter.x, SSbound_arrowLimit.x));
        float invLerp2center_Y = Mathf.Clamp01(Mathf.InverseLerp(SSpos_NPC.y, SScenter.y, SSbound_arrowLimit.y));

        //inv值x>y，表示該箭頭應於水平側顯示，而非垂直側顯示；反之類推
        if (invLerp2center_X > invLerp2center_Y)
        {
            //套用界線的X值，並求出內插的Y值，以此類推
            limitArrowPos.x = SSbound_arrowLimit.x;

            float invLerp2player_X = Mathf.InverseLerp(SSpos_NPC.x, SSpos_Player.x, limitArrowPos.x); 
            limitArrowPos.y = Mathf.Lerp(SSpos_NPC.y, SSpos_Player.y, invLerp2player_X);
        } 
        else if (invLerp2center_Y > invLerp2center_X) 
        {
            limitArrowPos.y = SSbound_arrowLimit.y;

            float invLerp2player_Y = Mathf.InverseLerp(SSpos_NPC.y, SSpos_Player.y, limitArrowPos.y);
            limitArrowPos.x = Mathf.Lerp(SSpos_NPC.x, SSpos_Player.x, invLerp2player_Y);
            //limitArrowPos.x = Mathf.Lerp(SSpos_NPC.x, SSpos_Player.x, invLerp2center_Y);
            //limitArrowPos.y = SSbound_arrowLimit.y;
        }

        //似乎可刪除，但暫保留
        //如果視距外NPC，太接近中心點(衍生十字)，採用另一種計算方法
        Vector2 Center2NPC = SSpos_NPC - SScenter;
        if (Mathf.Abs(Center2NPC.x) < 10f || Mathf.Abs(Center2NPC.y) < 10f)
        {
            //先判斷在十字的水平向或垂直向，x>y則在水平向
            if (Mathf.Abs(Center2NPC.x) > Mathf.Abs(Center2NPC.y))
            {
                //在正x向，x固定，y內插
                limitArrowPos.x = SSbound_arrowLimit.x;
                limitArrowPos.y = 0.5f * (SSpos_NPC.y + SSpos_Player.y);
            }
            else 
            {
                limitArrowPos.x = 0.5f * (SSpos_NPC.x + SSpos_Player.x); 
                limitArrowPos.y = SSbound_arrowLimit.y;
            }
        }

        limitArrowPos.z = set_posZ;
        gameObject.transform.position = CSset_refCamera.ScreenToWorldPoint(limitArrowPos);
    }
    private void OEI_Rotate()
    {
        //圖片開啟才計算其旋轉
        if (CSset_ArrowSprite.enabled == false) { return; }

        var eulerRot = Vector2.SignedAngle(SSpos_NPC - SSpos_Player, Vector2.up);
        gameObject.transform.localRotation = Quaternion.Euler(0, 0, -eulerRot);
    }

    




}
