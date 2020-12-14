using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericPrefab_AutoReturnOP : MonoBehaviour
{
    //藉由本腳本令物件基於時限自動回歸物件池 //優點：方便物件銷毀
    /*
     public GameObject set_prefab_Tetrapods;        //消波塊預製物
    public GameObject set_prefab_MedicalKit;       //醫藥包預製物   
    public GameObject set_prefab_Bomb;             //炸彈預製物
    public GameObject set_prefab_Shell;            //子彈預製物
    public GameObject set_prefab_VFXflash;         //閃光特效預製物(物品閃現/獲得治療)
    public GameObject set_prefab_VFXexplosion;     //爆炸特效預製物(炸彈爆炸/子彈爆炸)
    public GameObject set_prefab_VFXshockwave;     //震波特效預製物(發砲砲口)
     
     */

    public enum PrefabType
    {
        prefab_Tetrapods,
        prefab_MedicalKit,
        prefab_Bomb,
        prefab_Shell,
        prefab_VFXflash,
        prefab_VFXshockwave,
        prefab_VFXexplosion_AP,
        prefab_VFXexplosion_HE,
        prefab_VFXexplosion_Obj,
        prefab_VFXexplosion_BombBurst,
    }
    public PrefabType selectPrefab;
    
    public float onField_lifeTime = 10f;
    public bool isColorChangeWithLifeTime;
    public SpriteRenderer sprite_ColorChangeWithLifeTime;

    public float lifeTimeCountDown;
    bool isLifeTimeUP; //方便判定是被提前送回還是在本處自動送回

    //音效不應返回物件池之前才播放
    //因此設計上採用綁定特效的播放，發出聲音的是特效

    

    private void OnEnable()
    {
        isLifeTimeUP = false;
        lifeTimeCountDown = onField_lifeTime;
        if (isColorChangeWithLifeTime && sprite_ColorChangeWithLifeTime != null)
        {
            sprite_ColorChangeWithLifeTime.color = Color.white;
        }
    }

    void Update()
    {
        //如果遊戲暫停，停止壽命計算
        if (GameManager.instance_ThisScript.isPaused == true) 
        {
            return;        
        }
        if (isLifeTimeUP == true)
        {
            return;
        }
        else
        {
            if (isColorChangeWithLifeTime && sprite_ColorChangeWithLifeTime != null)
            {
                StartCoroutine(ColorChangeWithLifeTime());
            }
            LifeTimeEndEnqueue();
        }
        
    }


    void LifeTimeEndEnqueue()
    {
        if (lifeTimeCountDown > 0)
        {
            lifeTimeCountDown -= Time.deltaTime;
        }
        else if (lifeTimeCountDown <= 0)
        {            
            lifeTimeCountDown = onField_lifeTime;
            isLifeTimeUP = true;
            ReturnToObjectPool();
        }
    }

    public void ReturnToObjectPool() 
    {
        switch (selectPrefab) 
        {
            case PrefabType.prefab_Tetrapods: //消波塊送回物件池時叫出爆炸特效
                GameManager.instance_ThisScript.objStat_DestroyNum += 1;
                GameObject explosionOfTrtrapods = GameManager.ObjectPool_TakeFrom(GameManager.instance_ThisScript.set_prefab_VFXexplosion_Obj, GameManager.set_objectPool_VFXexplosion_Obj);
                explosionOfTrtrapods.transform.position = gameObject.transform.position;
                GameManager.ObjectPool_ReturnTo(this.gameObject, GameManager.set_objectPool_Tetrapods);
                break;
            case PrefabType.prefab_MedicalKit:
                GameManager.instance_ThisScript.objStat_DestroyNum += 1;
                if (isLifeTimeUP || gameObject.GetComponent<Objects_HealPackage>().isReCharge == false)
                {
                    GameObject flashOfMedicalKit = GameManager.ObjectPool_TakeFrom(GameManager.instance_ThisScript.set_prefab_VFXexplosion_Obj, GameManager.set_objectPool_VFXexplosion_Obj);
                    flashOfMedicalKit.transform.position = gameObject.transform.position;
                }
                else if (gameObject.GetComponent<Objects_HealPackage>().isReCharge == true) 
                {
                    GameObject flashOfMedicalKit = GameManager.ObjectPool_TakeFrom(GameManager.instance_ThisScript.set_prefab_VFXflash, GameManager.set_objectPool_VFXflash);
                    flashOfMedicalKit.transform.position = gameObject.transform.position;
                }
                GameManager.ObjectPool_ReturnTo(this.gameObject, GameManager.set_objectPool_MedicalKit);
                break;
            case PrefabType.prefab_Bomb:
                GameManager.instance_ThisScript.objStat_DestroyNum += 1;
                if (isLifeTimeUP) 
                {
                    GameObject explosionOfBomb = GameManager.ObjectPool_TakeFrom(GameManager.instance_ThisScript.set_prefab_VFXexplosion_Obj, GameManager.set_objectPool_VFXexplosion_Obj);
                    explosionOfBomb.transform.position = gameObject.transform.position;
                }
                GameManager.ObjectPool_ReturnTo(this.gameObject, GameManager.set_objectPool_Bomb);
                break;
            case PrefabType.prefab_Shell: //子彈送回物件池時，依照存在太久叫出爆炸特效；或依打擊到物體時依照設定彈種叫出爆炸特效
                gameObject.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
                if (gameObject.GetComponent<GenericObjectDamageSetting>() != null)
                {
                    GameObject explosionOfShell = null;
                    if (isLifeTimeUP)//如果本子彈是由存在太久而消滅 
                    {//子彈爆炸會產生特效，然後特效又會產生聲音
                        explosionOfShell = GameManager.ObjectPool_TakeFrom(GameManager.instance_ThisScript.set_prefab_VFXexplosion_Obj, GameManager.set_objectPool_VFXexplosion_Obj);
                    }
                    else if (!isLifeTimeUP && gameObject.GetComponent<GenericObjectDamageSetting>().info_DamageType == GenericObjectDamageSetting.DamageType.AP)//如果本子彈是由打到物體而消滅(AP)
                    {//子彈爆炸會產生特效，然後特效又會產生聲音
                        explosionOfShell = GameManager.ObjectPool_TakeFrom(GameManager.instance_ThisScript.set_prefab_VFXexplosion_AP, GameManager.set_objectPool_VFXexplosion_AP);                        
                    }
                    else if (!isLifeTimeUP && gameObject.GetComponent<GenericObjectDamageSetting>().info_DamageType == GenericObjectDamageSetting.DamageType.HE)//如果本子彈是由打到物體而消滅(HE)
                    {//子彈爆炸會產生特效，然後特效又會產生聲音
                        explosionOfShell = GameManager.ObjectPool_TakeFrom(GameManager.instance_ThisScript.set_prefab_VFXexplosion_HE, GameManager.set_objectPool_VFXexplosion_HE);
                    }
                    gameObject.GetComponentInChildren<TrailRenderer>().Clear();
                    gameObject.GetComponentInChildren<TrailRenderer>().enabled = false;
                    explosionOfShell.transform.position = gameObject.transform.position;
                }
                GameManager.ObjectPool_ReturnTo(this.gameObject, GameManager.set_objectPool_Shell);
                break;
            case PrefabType.prefab_VFXflash:
                GameManager.ObjectPool_ReturnTo(this.gameObject, GameManager.set_objectPool_VFXflash);
                break;
            case PrefabType.prefab_VFXshockwave:
                GameManager.ObjectPool_ReturnTo(this.gameObject, GameManager.set_objectPool_VFXshockwave);
                break;
            case PrefabType.prefab_VFXexplosion_AP:
                GameManager.ObjectPool_ReturnTo(this.gameObject, GameManager.set_objectPool_VFXexplosion_AP);
                break;
            case PrefabType.prefab_VFXexplosion_HE:
                GameManager.ObjectPool_ReturnTo(this.gameObject, GameManager.set_objectPool_VFXexplosion_HE);
                break;
            case PrefabType.prefab_VFXexplosion_Obj:
                GameManager.ObjectPool_ReturnTo(this.gameObject, GameManager.set_objectPool_VFXexplosion_Obj);
                break;
            case PrefabType.prefab_VFXexplosion_BombBurst:
                GameManager.ObjectPool_ReturnTo(this.gameObject, GameManager.set_objectPool_VFXexplosion_Bomb);
                break;
        }
    }


    IEnumerator ColorChangeWithLifeTime() //圖片顏色隨計時而變化
    {
        yield return new WaitForSeconds(2f);
        float ChannelRefValue = Mathf.Clamp((lifeTimeCountDown / onField_lifeTime), 0f, 1f);
        //var addColor = new Color(0.1f + 0.9f * ChannelRefValue, 0.1f + 0.9f * ChannelRefValue, 0.1f + 0.9f * ChannelRefValue, 1f); //顏色由白轉深黑
        var addColor = new Color(0.2f + 0.8f * ChannelRefValue, 0.2f + 0.8f * ChannelRefValue, 0.8f + 0.2f * ChannelRefValue, 1f); //顏色由白轉深藍
        sprite_ColorChangeWithLifeTime.color = addColor;
    }
}
