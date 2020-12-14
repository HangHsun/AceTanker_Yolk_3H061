using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI_ButtonList : MonoBehaviour
{
    /*按鈕清單腳本
  * 所有UI按鍵功能
  * 所有UI快捷鍵功能
  * 場景切換動畫 */
    //標題頁面

    //預設開局設定 //(方便GM檢查存取與取代套用)
    public int defaultGAMESETTING_playDiff = 0;
    public string defaultGAMESETTING_playerName = "Player";

    public Image set_ST_DisplayImage;
    public Sprite[] set_ST_GrayTexture;
    private float set_ST_TimeLength = 1.2f; //轉換場景等待延時
    private Vector2 set_ST_clipValue = new Vector2(-0.2f, 1.2f); //轉場遮罩裁切參數(min,max)


    public int set_S1_setDiff;        //暫存的遊戲開局難度(以利儲存前檢視)
    public string set_S1_setID;      //暫存的遊戲開局難度(以利儲存前檢視)



    private void Start()
    {
        Time.timeScale = 1;
        StartCoroutine(STmask_Open());
    }


    public void randomSceneTransitionMask() //隨機切換過場遮罩
    {
        int randomSpriteNum = Random.Range(0, set_ST_GrayTexture.Length);                                            //隨機不包含最大值，所以可以用長度當最大值
        set_ST_DisplayImage.material.SetTexture("refGrayscaleTexture", set_ST_GrayTexture[randomSpriteNum].texture); //隨機指定圖片
    }

    IEnumerator STmask_CloseAndST(int goalScene_Index) //max2min //關上轉場遮罩並轉場 
    {
        
        set_ST_DisplayImage.gameObject.SetActive(true); //啟用物件
        randomSceneTransitionMask();                    //隨機選擇遮罩貼圖
        for (float i = set_ST_clipValue.y; i > set_ST_clipValue.x; i -= (set_ST_clipValue.y - set_ST_clipValue.x) / (set_ST_TimeLength * 60f))
        {
            set_ST_DisplayImage.material.SetFloat("refTransitionClipValue", i);
            yield return new WaitForSeconds(1f / 60f);
        }
        set_ST_DisplayImage.material.SetFloat("refTransitionClipValue", set_ST_clipValue.x);
        SceneManager.LoadScene(goalScene_Index);
    }

    IEnumerator STmask_Open() //min2max //解開轉場遮罩
    {
        set_ST_DisplayImage.gameObject.SetActive(true);  //啟用物件
        randomSceneTransitionMask();                     //隨機選擇遮罩貼圖
        set_ST_DisplayImage.material.SetFloat("refTransitionClipValue", set_ST_clipValue.x); //逐漸打開遮罩
        for (float i = set_ST_clipValue.x; i < set_ST_clipValue.y; i += (set_ST_clipValue.y - set_ST_clipValue.x) / (set_ST_TimeLength * 60f))
        {
            set_ST_DisplayImage.material.SetFloat("refTransitionClipValue", i);
            yield return new WaitForSeconds(1f / 60f);
        }
        set_ST_DisplayImage.material.SetFloat("refTransitionClipValue", set_ST_clipValue.y); //遮罩全開之後固定其值
        set_ST_DisplayImage.gameObject.SetActive(false); //關閉物件
    }

    public void SwitchScene(int goalIndex) //通用轉換場景執行 
    {
        Time.timeScale = 1;                             //因為是用協程轉場，必須要先確保時間運行
        StartCoroutine(STmask_CloseAndST(goalIndex));
    }

    public void S0_QuitGame() //要退出遊戲必須在開頭退出
    {
        Application.Quit();
    }


    public void ResetS1_ToggleAndInputSetting() //是否要重製選擇難度與輸入ID的重製
    {
        set_S1_setDiff = defaultGAMESETTING_playDiff;
        set_S1_setID = defaultGAMESETTING_playerName;
        PlayerPrefs.SetInt("SAVED_GAMESETTING_DIFF", set_S1_setDiff);
        PlayerPrefs.SetString("SAVED_GAMESETTING_ID", set_S1_setID);
    }


    public void S1_ToggleSet_BFstartDiff(int setLevel) //隨使用改變遊戲初始難度並儲存
    {
        set_S1_setDiff = setLevel;
        PlayerPrefs.SetInt("SAVED_GAMESETTING_DIFF", set_S1_setDiff);
        //存入資料以供S2取用
    }

    public void S1_InputFieldSet_BFplayerID(string setID) //隨使用改變遊戲玩家ID並儲存
    {
        set_S1_setID = setID;
        PlayerPrefs.SetString("SAVED_GAMESETTING_ID", set_S1_setID);
        //存入資料以供S2取用
    }





    public CanvasGroup S2_WIN_CanvasGroup;   //設定視窗群組統一縮放和顏色(子視窗的開關尚需客製化開關)
    public GameObject S2_WIN_PauseAndStat;
    public GameObject S2_WIN_EndResult;
    public GameObject S2_WIN_GameStartCount;

    float set_S2_WINframe_AnimLength = 0.35f;
    public void S2_PauseGame() //暫停時間並進入資訊頁面，同時更新頁面資料
    {
        GameManager.instance_ThisScript.isPaused = true;

        S2_WIN_PauseAndStat.SetActive(true);                         //開啟暫停視窗物件
        GameManager.instance_ThisScript.Update_PageUIs_PausePage();  //開啟視窗後手動更新獲得資料

        //視窗進行縮放
        StartCoroutine(S2_ScaleAndFade_WIN_CanvasGroup(true,S2_WIN_PauseAndStat));

    }

    public void S2_ContinueGame() //解除暫停並關閉資訊業面
    {
        GameManager.instance_ThisScript.isPaused = false;
        S2_WIN_PauseAndStat.SetActive(false);
        //視窗進行縮放
        StartCoroutine(S2_ScaleAndFade_WIN_CanvasGroup(false,S2_WIN_PauseAndStat));
    }

    public void S2_PressQuit_WithResultShow_OrGameOver() //退出遊戲或遊戲結束時跳出結算視窗
    {
        GameManager.instance_ThisScript.isPaused = true;


        S2_WIN_PauseAndStat.SetActive(false); //其他視窗務必隱藏
        S2_WIN_EndResult.SetActive(true); //開啟結算視窗物件 //開啟後自動更新獲得資料
        
        //視窗進行縮放
        StartCoroutine(S2_ScaleAndFade_WIN_CanvasGroup(true, S2_WIN_EndResult));
    }


    //視窗縮放動畫協程(可供其他腳本使用)
    public IEnumerator S2_ScaleAndFade_WIN_CanvasGroup(bool WINswitch,GameObject useWIN) 
    {
        Time.timeScale = 1f;
        S2_WIN_CanvasGroup.gameObject.SetActive(true);
        S2_WIN_CanvasGroup.interactable = true;
        Color winColor = Color.white;
        Vector3 winScale = Vector3.one;

        if (WINswitch==true)         //顯示窗框(開啟+淡入+縮小)
        {
            S2_WIN_CanvasGroup.alpha = 0f;
            useWIN.SetActive(true);
            for (float i = 0f; i <= 1f; i += (1f / (set_S2_WINframe_AnimLength * 60f)))
            {
                winScale= new Vector3(2.5f - 1.5f * i, 2.5f - 1.5f * i, 1f);
                S2_WIN_CanvasGroup.gameObject.GetComponent<Transform>().localScale = winScale;
                S2_WIN_CanvasGroup.alpha = i;                
                yield return new WaitForSeconds(1f / 60f);
            }
            useWIN.SetActive(true);
        }
        else if(WINswitch==false)   //顯示窗框(關閉+淡出+放大)
        {
            S2_WIN_CanvasGroup.alpha = 1f;
            useWIN.SetActive(true);
            for (float i = 1f; i >= 0f; i -= (1f / (set_S2_WINframe_AnimLength * 60f)))
            {
                winScale = new Vector3(2.5f - 1.5f * i, 2.5f - 1.5f * i, 1f);
                S2_WIN_CanvasGroup.gameObject.GetComponent<Transform>().localScale = winScale;
                S2_WIN_CanvasGroup.alpha = i;
                yield return new WaitForSeconds(1f / 60f);
            }
            useWIN.SetActive(false);
        }
        
        
    }



    //介面按鈕參考AudioManager函數 (因為預設OnClick()函數無法自行以實例訪問跨場景AudioManager物件
    //需由非跨場景的本腳掛載物件輔助訪問

    //按鈕敲擊時漸關BGM
    public void ForButton_UseAudioManager_BGM_SmoothTurnOff_AndReset()
    {
        AudioManager.instanceAudioManager.BGM_SmoothTurnOff_AndReset();
    }

    //按鈕敲擊時發出按下音
    public void ForButton_UseAudioManager_ClickSound() 
    {
        AudioManager.instanceAudioManager.GetClip_AndUsingSourcePlay(AudioManager.Source_UI, AudioManager.instanceAudioManager.set_Button_click);
    }

    public void ForButton_UseAudioManager_ClickSound_Light()
    {
        AudioManager.instanceAudioManager.GetClip_AndUsingSourcePlay(AudioManager.Source_UI, AudioManager.instanceAudioManager.set_Button_click_Light);
    }


    //按鈕敲擊時發出選關音(不使用人聲)
    public void ForButton_S1_UseAudioManager_SelectDiff() 
    {
        AudioManager.instanceAudioManager.GetClip_AndUsingSourcePlay(AudioManager.Source_UI, AudioManager.instanceAudioManager.set_Track_Repaired);
    }


    //按鈕敲擊時發出結算音 //改由結算頁面叫出，而非按鈕使用
    /*public void ForButton_S2_UseAudioManager_GameResult()
    {
        AudioManager.instanceAudioManager.GetClip_AndUsingSourcePlay(AudioManager.Source_UI, AudioManager.instanceAudioManager.set_Button_showResult);
    }*/




    //public GameObject S3_win_QuitWarning;


    /*
    public void S3_StopQuitGame() //開啟工作人員頁面，並延遲退出遊戲
    {

        S3_win_QuitWarning.SetActive(true);
        StartCoroutine(QuitGame());
    }*/

    

}
