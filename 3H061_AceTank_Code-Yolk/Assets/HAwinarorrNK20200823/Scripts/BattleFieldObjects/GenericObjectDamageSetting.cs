using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericObjectDamageSetting : MonoBehaviour
{

    //設定物件腳本狀態(純數據)
    public float info_GunDiameter;      //砲管口徑 (如果物件為戰車子彈的話需設值)
    public float info_DamageValue;      //傷害
    public float info_PenetraionValue;  //穿深    
    public float info_ExplosionRadious; //暴風半徑
    public enum DamageType { AP, HE };
    public DamageType info_DamageType;      //傷害特性 (穿甲或高爆)
    public string info_UserName;        //物品持有者 //方便腳本抓持有者資料   

    private void Update()
    {/*
        Debug.Log("砲管口徑=" + info_GunDiameter);
        Debug.Log("傷害值=" + info_DamageValue);
        Debug.Log("穿深值=" + info_PenetraionValue);
        Debug.Log("發射者名稱=" + info_UserName);
        Debug.Log("傷害特性="+info_DamageType);*/
    }
}
