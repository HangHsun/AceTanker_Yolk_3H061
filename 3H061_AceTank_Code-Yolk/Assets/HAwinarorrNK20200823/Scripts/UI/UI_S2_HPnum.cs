using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class UI_S2_HPnum : OBJECTS_PARENT_ON_WININFO
{
    //本腳本負責浮動位置的 攻擊傷害/恢復血量/狀態顯示 訊息

    [Header("被外部修改項目")]
    public float set_Num;
    

    [Header("顏色組合")]
    public Color[] ColorSet_Damage = new Color[2];
    public Color[] ColorSet_Heal = new Color[2];


    [Header("動畫調整項目")]
    public float set_TimeLength;
    public float set_UPspeed;
    public float set_UPdistance;


    private void OnEnable()
    {
        gameObject.GetComponent<RectTransform>().localScale = Vector3.one;
        StartCoroutine(AutoReturnOP());
    }

    //使用數字的版本
    public IEnumerator HPnumAnim(GameObject pos, int num) //由外部函數發動(使用者須先指定發動位置)
    {
        float temp_posY = pos.transform.position.y;   //暫存協成啟用時的位置
        float temp_goalY = temp_posY + set_UPdistance;

        if (num > 0f)
        {
            gameObject.GetComponent<Text>().color = ColorSet_Damage[0];
            gameObject.GetComponent<Outline>().effectColor = ColorSet_Damage[1];
            gameObject.GetComponent<Text>().text = "-" + Mathf.Abs(num).ToString();
        }
        else if (num <= 0f)
        {
            gameObject.GetComponent<Text>().color = ColorSet_Heal[0];
            gameObject.GetComponent<Outline>().effectColor = ColorSet_Heal[1];
            gameObject.GetComponent<Text>().text = "+" + Mathf.Abs(num).ToString();

        }


        for (float i = 0f; i <= 1f; i += 1f / (30f * set_TimeLength))
        {
            gameObject.GetComponent<CanvasGroup>().alpha = (1f - i);

            gameObject.transform.position = new Vector3(pos.transform.position.x, temp_posY, 0f);
            temp_posY = Mathf.Lerp(temp_posY, temp_goalY, (1f*set_UPspeed) / (30f * set_TimeLength));
            yield return new WaitForSeconds(1f / 30f);
        }
        GameManager.ObjectPool_ReturnTo(gameObject, GameManager.set_objectPool_prefab_HPnumText);
    }

    //使用文字的版本
    public IEnumerator HPinfoAnim(GameObject pos, string info,int colorSet) //由外部函數發動(使用者須先指定發動位置)
    {
        
        float temp_posY = pos.transform.position.y;   //暫存協成啟用時的位置
        float temp_goalY = temp_posY + set_UPdistance;

        if (colorSet < 0 || colorSet > 1) 
        {
            colorSet = 0;
        }

        if (colorSet == 0)
        {
            gameObject.GetComponent<Text>().color = ColorSet_Damage[0];
            gameObject.GetComponent<Outline>().effectColor = ColorSet_Damage[1];
        }
        else if (colorSet == 1)
        {
            gameObject.GetComponent<Text>().color = ColorSet_Heal[0];
            gameObject.GetComponent<Outline>().effectColor = ColorSet_Heal[1];
        }
        gameObject.GetComponent<Text>().text = info;


        for (float i = 0f; i <= 1f; i += 1f / (30f * set_TimeLength))
        {
            gameObject.GetComponent<CanvasGroup>().alpha = (1f - i);

            gameObject.transform.position = new Vector3(pos.transform.position.x, temp_posY, 0f);
            temp_posY = Mathf.Lerp(temp_posY, temp_goalY, (1f * set_UPspeed) / (30f * set_TimeLength));
            yield return new WaitForSeconds(1f / 30f);
        }
        GameManager.ObjectPool_ReturnTo(gameObject, GameManager.set_objectPool_prefab_HPnumText);
    }


    IEnumerator AutoReturnOP() 
    {
        yield return new WaitForSeconds(10f);
        GameManager.ObjectPool_ReturnTo(this.gameObject, GameManager.set_objectPool_prefab_HPnumText);
    }

}
