using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Objects_HealPackage : OBJECTS_PARENT_ON_FIELD
{

    /*
    /*醫藥包物件腳本
     *計時消失
     *碰到敵人消失
     *碰到彈藥消失
     *碰到玩家補血*/
    public float set_RrechargeHP; // 補給血量
    public List<Sprite> set_Anim_Frame;  // 閃爍動畫使用圖片
    public float set_Anim_IntervalTime;  //閃爍間隔時間

    SpriteRenderer displaySprite; //圖片子物件
    int useSpriteNum;
    bool isSpriteChange;
    public bool isReCharge;


    private void OnEnable()
    {
        gameObject.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        useSpriteNum = 0;
        isReCharge = false;
        displaySprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (gameObject.activeInHierarchy == true && isSpriteChange == false) 
        {
            StartCoroutine(KitSpriteAnim());
        }
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "PLAYER" && collision.gameObject.GetComponentInParent<GenericTankController>())
        {
            isReCharge = true;
            collision.gameObject.GetComponentInParent<GenericTankController>().updateHP_Hull += set_RrechargeHP; //設定戰車控制器的即時血量增加
            collision.gameObject.GetComponentInParent<GenericTankController>().SetHull_HPstate();                //使用戰車控制器的血量狀態函數，限制血量不超過設定上限 (此函數原本只在受傷才發動
            GameManager.instance_ThisScript.stat_playerUpdateHP = collision.GetComponentInParent<GenericTankController>().updateHP_Hull; //更新GM監控的玩家即時血量狀態

            //顯示補血訊息與數字交由玩家控制器腳本自行判斷

            gameObject.GetComponent<GenericPrefab_AutoReturnOP>().ReturnToObjectPool();
        }
        else if (collision.gameObject.tag == "BATTLEFIELD_SHELL" || collision.gameObject.tag == "BATTLEFIELD_OBJECT")
        {
            isReCharge = false;
            if (collision.gameObject.GetComponent<GenericShellSetting>() != null)
            {
                collision.gameObject.GetComponent<GenericPrefab_AutoReturnOP>().ReturnToObjectPool();
            }
            gameObject.GetComponent<GenericPrefab_AutoReturnOP>().ReturnToObjectPool();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "BATTLEFIELD_SHELL" || collision.gameObject.tag == "BATTLEFIELD_OBJECT" || collision.gameObject.tag == "ENEMY")
        {
            isReCharge = false;
            if (collision.gameObject.GetComponent<GenericShellSetting>() != null)
            {
                collision.gameObject.GetComponent<GenericPrefab_AutoReturnOP>().ReturnToObjectPool();
            }
            gameObject.GetComponent<GenericPrefab_AutoReturnOP>().ReturnToObjectPool();
        }
    }


    IEnumerator KitSpriteAnim()
    {
        isSpriteChange = true;
        yield return new WaitForSeconds(set_Anim_IntervalTime);
        if (useSpriteNum < set_Anim_Frame.Count-1)
        {
            useSpriteNum += 1;
            displaySprite.sprite = set_Anim_Frame[useSpriteNum];
        }
        else
        {
            useSpriteNum = 0;
            displaySprite.sprite = set_Anim_Frame[useSpriteNum];
        }
        isSpriteChange = false;
    }

}

