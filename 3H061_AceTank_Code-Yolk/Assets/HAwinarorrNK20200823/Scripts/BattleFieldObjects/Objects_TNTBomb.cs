using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Objects_TNTBomb : OBJECTS_PARENT_ON_FIELD
{    
    /*炸彈物件腳本
     *受子彈或炸彈爆炸
     *爆炸範圍以內扣血
     *阻礙玩家/敵人通行
     */
    public bool isExplosion; //設定引爆狀態，令戰車碰撞時非爆炸不受傷害
    CircleCollider2D explosionWind;

    

    private void OnEnable()
    {
        gameObject.transform.rotation = Quaternion.Euler(0f, 0f, 90f * (float)Random.Range((int)0, (int)4));
        isExplosion = false;
        explosionWind = gameObject.GetComponent<CircleCollider2D>();
        explosionWind.radius = 0f;

        //關閉暴風碰撞體；開啟外盒碰撞體
        explosionWind.enabled = false;
        gameObject.GetComponent<BoxCollider2D>().enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isExplosion == false)
        {
            if (collision.gameObject.tag == "BATTLEFIELD_SHELL" || collision.gameObject.tag == "BATTLEFIELD_OBJECT") //如果碰觸到特定對象即引爆
            {
                if (collision.gameObject.GetComponent<GenericShellSetting>()!=null)
                {
                    gameObject.GetComponent<GenericObjectDamageSetting>().info_UserName = collision.GetComponent<GenericObjectDamageSetting>().info_UserName; //把攻擊此物件者的名稱設為本物使用者名稱(達成間接傷害亦可計入)                    
                    collision.gameObject.GetComponent<GenericPrefab_AutoReturnOP>().ReturnToObjectPool();
                    //Debug.Log(gameObject.GetComponent<GenericObjectDamageSetting>().info_UserName);
                }
                isExplosion = true;

                StartCoroutine(BombExplosion());
            }
        }
        else if (isExplosion == true) 
        {
            explosionWind.radius = 0f; //令暴風若被激發則只維持一幀(暴風縮至最小)
        }
    }

    IEnumerator BombExplosion() //產生爆炸並返回物件池
    {
        //關閉外盒碰撞體，開啟暴風碰撞體
        gameObject.GetComponent<BoxCollider2D>().enabled = false;
        explosionWind.enabled = true;
        explosionWind.radius = gameObject.GetComponent<GenericObjectDamageSetting>().info_ExplosionRadious; //擴大暴風半徑至設定值
        GameObject instanceExplosion = GameManager.ObjectPool_TakeFrom(GameManager.instance_ThisScript.set_prefab_VFXexplosion_Bomb, GameManager.set_objectPool_VFXexplosion_Bomb);
        instanceExplosion.transform.position = gameObject.transform.position;

        yield return new WaitForSeconds(0.5f);
        gameObject.GetComponent<GenericPrefab_AutoReturnOP>().ReturnToObjectPool();
    }
}
