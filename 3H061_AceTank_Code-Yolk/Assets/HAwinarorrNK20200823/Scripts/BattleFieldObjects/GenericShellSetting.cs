using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class GenericShellSetting : OBJECTS_PARENT_ON_FIELD
{
    //本腳本只需負責子彈貼圖的淡出與關閉拖尾視覺效果
    //注意設置不可用OnEnable，以免物件池呼叫造成效果錯誤

    bool isShellStartUpdate;

    public TrailRenderer shellTrail;
    //public bool isStopTrail;

    private void OnEnable()
    {
        isShellStartUpdate = true;
    }

    private void FixedUpdate()
    {
        if (isShellStartUpdate) 
        {
            isShellStartUpdate = false;

            shellTrail.time = 0f;
            gameObject.GetComponent<BoxCollider2D>().enabled = true;   //確保使用時碰撞器開啟(因為會被戰車控制器關閉)

            StartCoroutine(ActiviteTrail());
        }        
    }



    IEnumerator ActiviteTrail() 
    {        
        yield return new WaitForSeconds(0.01f);
        shellTrail.time = 0.5f;   
    }
}
