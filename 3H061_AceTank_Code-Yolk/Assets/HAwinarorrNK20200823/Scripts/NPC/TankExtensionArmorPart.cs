using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankExtensionArmorPart : MonoBehaviour
{
    //戰車衍生裝甲腳本
    //本裝甲僅考慮穿深(沿用參考控制器的裝甲值)，並不在乎入射角度


    public float set_ExtensionArmorHP;

    //脫落時套用不同貼圖
    public Sprite set_Sprite_Normal;
    public int set_OrderInLayer_Normal;
    public Sprite set_Sprite_Detach;
    public int set_OrderInLayer_Detach;

    public GameObject set_AttachParent;
    public Vector2[] set_DetachVFX_localPos;
    public GenericTankController set_refTankController;

    public float update_ExtensionArmorHP;
    Vector3 backup_OriginalLocalPosition;
    Vector3 backup_OriginalLocalRotation;

    bool checkDead;


    //注意腳本在音效上沿用主車體的音源
    //存取戰車聲效的播放建立器(也利於初期設置抱錯確認) //也利於和主體之間重複的語音互相覆蓋
    private GenericTankAudioBuilder usingTankAudio;

    private void Awake()
    {
        //備份local座標，以利初始化對位參考
        backup_OriginalLocalPosition = gameObject.transform.localPosition;
        backup_OriginalLocalRotation = gameObject.transform.localRotation.eulerAngles;

        //注意腳本在音效上沿用主車體的音源
        //存取戰車聲效的播放建立器(也利於初期設置抱錯確認) //也利於和主體之間重複的語音互相覆蓋
        usingTankAudio = set_refTankController.gameObject.GetComponent<GenericTankAudioBuilder>();
    }

    private void OnEnable()
    {
        //設定標籤
        gameObject.tag = "ENEMY";
        //綁定父物件並對位
        gameObject.transform.SetParent(set_AttachParent.transform);
        gameObject.transform.localPosition = backup_OriginalLocalPosition;
        gameObject.transform.localRotation = Quaternion.Euler(backup_OriginalLocalRotation);
        //顯示圖片
        gameObject.GetComponent<SpriteRenderer>().enabled = true;
        GetComponent<SpriteRenderer>().sprite = set_Sprite_Normal;
        GetComponent<SpriteRenderer>().sortingOrder = set_OrderInLayer_Normal;
        //套用控制器設定顏色
        gameObject.GetComponent<SpriteRenderer>().color = set_refTankController.set_allPartColor;
        //重設血量
        update_ExtensionArmorHP = set_ExtensionArmorHP;
        checkDead = false;
    }

    private void FixedUpdate()
    {
        //如果控制器已顯示陣亡，則連帶消失
        if (set_refTankController.isDead == true && checkDead==false) 
        {
            checkDead = true;
            StartCoroutine(EXarmor_DetachHull());
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //如果受到子彈撞擊，摧毀子彈
        if (collision.gameObject.GetComponent<GenericShellSetting>())
        {
            collision.gameObject.GetComponent<GenericPrefab_AutoReturnOP>().ReturnToObjectPool();
        }

        //受到任何傷害進行判斷
        if (collision.gameObject.GetComponent<GenericObjectDamageSetting>())
        {
            //如果是炸彈物件，引爆狀態才接收傷害
            if (collision.GetComponent<Objects_TNTBomb>())
            {
                if (collision.GetComponent<Objects_TNTBomb>().isExplosion == true) 
                {
                    EXarmor_ReceiveDamage(collision.gameObject.GetComponent<GenericObjectDamageSetting>());
                }
            }
            else //非炸彈物件直接接收碰撞傷害
            {
                EXarmor_ReceiveDamage(collision.gameObject.GetComponent<GenericObjectDamageSetting>());
            }
        }
    }


    //傷害接收判斷(沿用參考控制器的裝甲厚度)
    void EXarmor_ReceiveDamage(GenericObjectDamageSetting damageSource) 
    {
        //血量見底至協程期間，不再接收傷害
        if (update_ExtensionArmorHP == 0f) { return; }

        var receiveDMG = 0f;


        //如果是受到穿透的傷害，接收全傷
        if (set_refTankController.set_armor_Side < damageSource.info_PenetraionValue)
        {
            receiveDMG = damageSource.info_DamageValue;
        }//如果是受到為穿透的HE類型傷害，計算穿深並減傷
        else if (damageSource.info_DamageType == GenericObjectDamageSetting.DamageType.HE) 
        {
            receiveDMG = damageSource.info_DamageValue * 0.5f - set_refTankController.set_armor_Side;
        }

        //最後受到的傷害又再隨機浮動
        receiveDMG *= Random.Range(0.75f, 1.25f);

        //如果判斷傷害大於0，顯示數字；0則顯示文字訊息
        var instanceText = GameManager.ObjectPool_TakeFrom(GameManager.instance_ThisScript.set_prefab_HPnumText, GameManager.set_objectPool_prefab_HPnumText);        
        if (receiveDMG > 0f)
        {
            //播放外掛裝甲受擊音效
            //接受到是玩家的子彈，才能觸發此音效
            usingTankAudio.play_Shell_noPeneButDamage_isNPC(damageSource);

            if (receiveDMG > update_ExtensionArmorHP)
            {
                receiveDMG = update_ExtensionArmorHP;
                //傷害大於剩餘血量，開啟零件脫落協程
                StartCoroutine(EXarmor_DetachHull());

                if (set_refTankController.isDead == false)
                {
                    //播放脫落音效(主體需存活才使用)
                    usingTankAudio.play_EXarmorDetach();
                }
            }

            update_ExtensionArmorHP = Mathf.Clamp(update_ExtensionArmorHP - receiveDMG, 0f, set_ExtensionArmorHP);
            StartCoroutine(instanceText.GetComponent<UI_S2_HPnum>().HPnumAnim(gameObject, (int)receiveDMG));
        }
        else 
        {
            //播放外掛裝甲無傷音效
            //接受到是玩家的子彈，才能觸發此音效
            usingTankAudio.play_Shell_noPenetrate_isNPC(damageSource);

            StartCoroutine(instanceText.GetComponent<UI_S2_HPnum>().HPinfoAnim(gameObject, "Hit ExArmor" + "\n" + "No Damage", 0));
        }
    }

    IEnumerator EXarmor_DetachHull() 
    {

        //暫時脫落與車體的關係
        gameObject.transform.parent = null;
        //暫時加深相機深度，確保物件在車身之下
        gameObject.transform.localPosition += new Vector3(0, 0, 0.1f);
        GetComponent<SpriteRenderer>().sprite = set_Sprite_Detach;
        GetComponent<SpriteRenderer>().sortingOrder = set_OrderInLayer_Detach;
        for (int i = 0; i < set_DetachVFX_localPos.Length; i++) 
        {
            var instanceVFX = GameManager.ObjectPool_TakeFrom(GameManager.instance_ThisScript.set_prefab_VFXexplosion_Obj, GameManager.set_objectPool_VFXexplosion_Obj);
            instanceVFX.transform.position = gameObject.transform.TransformPoint((Vector3)set_DetachVFX_localPos[i]);
        }
        yield return new WaitForSeconds(3f);

        //零件脫落消失觸發特效
        var DetachDisappearVFX = GameManager.ObjectPool_TakeFrom(GameManager.instance_ThisScript.set_prefab_VFXexplosion_Obj, GameManager.set_objectPool_VFXexplosion_Obj);
        DetachDisappearVFX.transform.position = gameObject.transform.position;

        //隱藏零件圖片
        gameObject.GetComponent<SpriteRenderer>().enabled = false;
        //關閉零件碰撞
        gameObject.GetComponent<BoxCollider2D>().enabled = false;
        //重新綁定父物件
        gameObject.transform.SetParent(set_AttachParent.transform);
        //關閉本物件(避免重複協程)
        gameObject.SetActive(false);
    }
}
