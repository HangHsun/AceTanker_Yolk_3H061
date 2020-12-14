using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_S2_EndResult : MonoBehaviour
{
    /*
     * 本腳本負責結算畫面動畫
     * 被啟動時先接收GM資料
     * 縮小視窗+淡入+不暫停遊戲
     * 首先顯示存取的玩家名
     * 單資料行先顯示項目名，在顯示資料；才在繼續下一行
     * 所有資料行打完之後，在顯示結算分數標題
     * 以數字飆升的方式計算成績
     * 成績顯示完畢，才顯示勳章
     * 顯示勳章之後才出現選擇重啟/退出按鈕
     * 以上所有文字共享text (優點：編輯排版方便)
     * 
     * 動畫期間按任何鍵則加速動畫
     */

    [Header("使用物件設定")]
    public Sprite[] set_MarkIcon; //成就勳章陣列
    public Text set_display_Title;
    public Text set_display_Stats;
    public GameObject set_hideBottons_Restart;
    public GameObject set_hideBottons_Quit;
    public Image set_displayFINAL_Mark; //展示根據分數獲得的勳章

    public string rawForDisplay_Score_String; //展示公式計算分數的字串 (用於加入text展示)
    public float rawForDisplay_Score_Num;   //展示公式計算分數 (用於經過處理變成字串格式)  (除錯用)

    //系統計算參數
    bool isFastAnim;                  //是否加速動畫完成(按任何鍵即可

    public string[] getAndSet_StatsDetail;
    public string get_PlayerName;
    string stats_Gap = " // ";
    //由GM腳本獲得資料並設定字串 (各項目分開處理以利維護
    string get_TimeLength;
    string get_PlayerATK;
    string get_PlayerDMG;
    string get_PlayerHitRatio;
    string get_PlayerHitRatio_Detail;
    string get_PlayerKLN;
    string get_PlayerKLN_Detail;

    AudioSource AudioSource_ScoreCount;      //計分另外採用音源
    int CSset_CodeOfAudioClip_ForScoreMark;  //計分勳章此用音效編號


    private void Awake() //注意Onenable順序在Start之前，計分時首先使用自身音源的話要放awake
    {
        AudioSourceIntialize_ofScoreCount();
    }
    private void AudioSourceIntialize_ofScoreCount()
    {
        //進行音源組件的建立、指定分群與初始化
        AudioSource_ScoreCount = gameObject.AddComponent<AudioSource>();
        AudioSource_ScoreCount.loop = false;
        AudioSource_ScoreCount.playOnAwake = false;
        AudioSource_ScoreCount.clip = AudioManager.instanceAudioManager.set_UI_showResult_CountScore;
        AudioSource_ScoreCount.outputAudioMixerGroup = AudioManager.instanceAudioManager.AudioGroup_Main_UI;
    }


    private void OnEnable()
    {
        isFastAnim = false;

        //1.關閉或清空物件等待協程開啟
        set_display_Title.text = "";
        set_display_Stats.text = "";
        set_displayFINAL_Mark.gameObject.SetActive(false);
        //跑完全部文字動畫才顯示按鈕，故先隱藏
        set_hideBottons_Quit.SetActive(false);
        set_hideBottons_Restart.SetActive(false);

        //2.從GM取得資料並處理，以供後續展示
        GetData_andCalculateStatsDetail_From_GameManager(); 

        //3.根據資料設定標題區塊顯示玩家名稱
        set_display_Title.text = "TANKER : " + get_PlayerName;        

        //4.根據資料設定統計區塊顯示內容(切成陣列以利動畫)
        Set_StatsDetail_Array();

        //5.根據資料計算最終成績與獲得勳章
        CalculateFinalScore_and_SetDisplayMark();

        //6.啟用協程開始動畫(可跳過)
        StartCoroutine(TypingAllResult_andShowBotton());
    }

    /*
    get_PlayerName;
     * 由GM取得的資料並轉為字串
    string get_TimeLength;                   //時間長度
    string get_PlayerATK;                    //總輸出(可以靠炸彈間接造傷)
    string get_PlayerDMG;                    //總承傷
    string get_PlayerHitRatio;               //命中率(有傷害的才會算入)
    string get_PlayerHitRatio_Detail;        //命中率詳細數字
    string get_PlayerKLN;                    //擊殺數
    string get_PlayerKLN_Detail;             //擊殺數詳細數字
    */
    //2.從GM取得資料並處理，以供後續展示
    private void GetData_andCalculateStatsDetail_From_GameManager() 
    {
        //get_PlayerName;
        get_PlayerName = GameManager.instance_ThisScript.GAMESETTING_GamePlayerName;

        //string get_TimeLength;
        int timeMinute = (int)(GameManager.instance_ThisScript.stat_sysPlayTimeLength / 60);
        int timeSecond = (int)(GameManager.instance_ThisScript.stat_sysPlayTimeLength % 60);
        get_TimeLength = timeMinute.ToString("00") + ":" + timeSecond.ToString("00");
       
        //string get_PlayerATK; //顯示上限五位數，超過變成無限符號
        float ATK = GameManager.instance_ThisScript.stat_playerTotalAttack;
        get_PlayerATK = Ckeck_NumOverLimit_and_GiveDigit2String(ATK, 99999);

        //string get_PlayerDMG; //顯示上限五位數，超過變成無限符號
        float DMG = GameManager.instance_ThisScript.stat_playerTotalDamage;
        get_PlayerDMG = Ckeck_NumOverLimit_and_GiveDigit2String(DMG, 99999);

        //string get_PlayerHitRatio;
        float HIT = GameManager.instance_ThisScript.stat_playerHitNum;
        float FIRE = GameManager.instance_ThisScript.stat_playerFireNum;
        if (FIRE > 0)
        {
            get_PlayerHitRatio = (HIT * 100f / FIRE).ToString("00.0") + "%";
        }
        else 
        {
            get_PlayerHitRatio = 0.ToString("00.0") + "%";
        }


        //string get_PlayerHitRatio_Detail;(999/999 shot)
        get_PlayerHitRatio_Detail = "(" + HIT.ToString("000") + "/" + FIRE.ToString("000") + " shot)";

        //string get_PlayerKLN;
        float KLN_MT = GameManager.instance_ThisScript.stat_playerKillNum_MT;
        float KLN_HT = GameManager.instance_ThisScript.stat_playerKillNum_HT;
        float KLN_SHT = GameManager.instance_ThisScript.stat_playerKillNum_SHT;
        get_PlayerKLN = Ckeck_NumOverLimit_and_GiveDigit2String((KLN_MT + KLN_HT + KLN_SHT),999);

        //Debug.Log(KLN_MT + KLN_HT + KLN_SHT);

        //string get_PlayerKLN_Derail;(m:99 / h:99 / sh:99)
        get_PlayerKLN_Detail = "(M:" + Ckeck_NumOverLimit_and_GiveDigit2String(KLN_MT, 99)
            + " / H:" + Ckeck_NumOverLimit_and_GiveDigit2String(KLN_HT, 99)
            + " / SH:" + Ckeck_NumOverLimit_and_GiveDigit2String(KLN_SHT, 99)
            + ")";
    }

    //EX.針對有上限的數字資料做格式字串處理(位數不超過輸入設定，並依千分位加逗號)
    private string Ckeck_NumOverLimit_and_GiveDigit2String(float theNumValue, float theLimitValue)
    {
        int powerOf10 = 1;
        string digitOfLimit = "0";

        //計算上限值的位數
        while (Mathf.Pow(10, powerOf10) < theLimitValue)
        {           
            powerOf10 += 1;
            if (powerOf10 % 3 == 1 && powerOf10 > 3) //每過3位數加逗號
            {
                digitOfLimit += ",";
            }
            digitOfLimit += "0";           
        }

        if (theNumValue > theLimitValue)
        {
            //字體不支援無限大的符號，只會顯示括號
            return "(0.0)b";
        }
        else
        {
            return theNumValue.ToString(digitOfLimit);
        }
    }


    //4.根據資料設定統計區塊顯示內容(切成陣列以利動畫) //最終結算分數等待第6步驟協程才加上
    void Set_StatsDetail_Array()
    {
        /*結果應該如下
        * Battle Time 00:00
        * Fire Damage : 00000 // Bear Damage : 00000
        * Hit Ratio : 99.9%  //(999/999 shot)
        * kill total:99 //(m:99 / h:99 / sh:99)
        >>>game score = 999,999,999 !!!
        */
        getAndSet_StatsDetail = new string[]
        {
            "* Battle Time ", get_TimeLength + "\n" ,
            "* Fire Damage : ", get_PlayerATK , stats_Gap ,
            "Bear Damage : ", get_PlayerDMG + "\n" ,
            "* Hit Ratio : ", get_PlayerHitRatio , stats_Gap ,
            get_PlayerHitRatio_Detail + "\n",
            "* kill total:", get_PlayerKLN , stats_Gap ,
            get_PlayerKLN_Detail + "\n",
            ">>>game score = "
        };
    }

//5.根據資料計算最終成績與獲得勳章
void CalculateFinalScore_and_SetDisplayMark() 
    {
        /*
        //分數上限=10億 (M級>上限；S級=85000；A級=50000；B級=25000；C級=12500)                               //10億=100*一千萬
        string get_TimeLength;                   //時間長度         //上限1小時   //指數//占分比重：5%   //五千萬 = 一千萬*2.5*log60(秒)       //設定上限
        string get_PlayerATK;                    //總輸出(可靠炸彈) //上限10萬    //線性//占分比重：25%  //兩億五 = 2500*(輸出)
        string get_PlayerDMG;                    //總承傷           //上限10萬    //指數//占分比重：15%  //一億五 = 三千萬*log10(承傷)         //設定上限
        string get_PlayerHitRatio;               //命中率(有傷才算) //上限100%    //線性//占分比重：10%  //一億 = 一億*(命中率)                //設定上限
        string get_PlayerHitRatio_Detail;        //命中率詳細數字   //上限1000    //----------------------
        string get_PlayerKLN;                    //擊殺數           //上限1000    //線性//占分比重：40%  //四億 = 40萬*(擊殺數)
        string get_PlayerKLN_Detail;             //擊殺數詳細數字   //上限100     //加權：(0.7/2.0/10.0)
        */
        float score_TimeLength = 2500 * Mathf.Log(GameManager.instance_ThisScript.stat_sysPlayTimeLength, 60);
        score_TimeLength = Mathf.Clamp(score_TimeLength, 0f, 5000f);

        float score_ATK = 0.4f*GameManager.instance_ThisScript.stat_playerTotalAttack;

        float score_DMG = 3000 * Mathf.Log10(GameManager.instance_ThisScript.stat_playerTotalDamage);
        score_DMG = Mathf.Clamp(score_DMG, 0f, 3000f);


        int GetFireNum = GameManager.instance_ThisScript.stat_playerFireNum;
        if (GetFireNum <= 0) { GetFireNum = 1; }

        float score_HitRatio = 10000 * (GameManager.instance_ThisScript.stat_playerHitNum / (float)GetFireNum);
        score_HitRatio = Mathf.Clamp(score_HitRatio, 0f, 1f);

        float score_KLN = 40f *
            (GameManager.instance_ThisScript.stat_playerKillNum_MT * 0.7f +
            GameManager.instance_ThisScript.stat_playerKillNum_HT * 2.0f +
            GameManager.instance_ThisScript.stat_playerKillNum_SHT * 10.0f);

        //還要再乘一萬才是顯示的分數
        float rawForDisplay_Score_Num_DivideWithW = (score_TimeLength + score_ATK + score_DMG + score_HitRatio + score_HitRatio + score_KLN);
        rawForDisplay_Score_Num = 10000 * rawForDisplay_Score_Num_DivideWithW;


        //轉換字串顯示的分數
        rawForDisplay_Score_String = Ckeck_NumOverLimit_and_GiveDigit2String(rawForDisplay_Score_Num_DivideWithW * 10000f, 999999999f);

        //根據分數設定勳章 C>B>A>S
        if (rawForDisplay_Score_Num_DivideWithW < 10000)        //C
        {
            set_displayFINAL_Mark.sprite = set_MarkIcon[0];
            CSset_CodeOfAudioClip_ForScoreMark = 0;
        }
        else if (rawForDisplay_Score_Num_DivideWithW < 30000)    //B
        {
            set_displayFINAL_Mark.sprite = set_MarkIcon[1];
            CSset_CodeOfAudioClip_ForScoreMark = 1;
        }
        else if (rawForDisplay_Score_Num_DivideWithW < 60000)    //A
        {
            set_displayFINAL_Mark.sprite = set_MarkIcon[2];
            CSset_CodeOfAudioClip_ForScoreMark = 2;
        }
        else                                                     //S
        {
            set_displayFINAL_Mark.sprite = set_MarkIcon[3];
            CSset_CodeOfAudioClip_ForScoreMark = 3;
        }
    }




    //6.啟用協程開始動畫(可跳過)
    IEnumerator TypingAllResult_andShowBotton()
    {
        //撥放結算開始音效(使用本物件的音源，而不沿用音效管理員的)
        AudioManager.instanceAudioManager.GetClip_AndUsingSourcePlay
            (
            AudioSource_ScoreCount,
            AudioManager.instanceAudioManager.set_Button_showResult
            );

        //開視窗縮放動畫由PlayerControlInput呼叫
        //GameObject.FindObjectOfType<UI_ButtonList>().S2_ScaleAndFade_WIN_CanvasGroup(true, gameObject);

        yield return new WaitForSeconds(1.2f);
        set_display_Title.gameObject.SetActive(true);
        set_display_Stats.gameObject.SetActive(true);

        for (int i = 0; i < getAndSet_StatsDetail.Length; i++) 
        {
            set_display_Stats.text += getAndSet_StatsDetail[i];

            //隨項目更新，撥放結算項目貼上音效
            AudioManager.instanceAudioManager.GetClip_AndUsingSourcePlay
                (
                AudioManager.Source_UI,
                AudioManager.instanceAudioManager.set_UI_showResult_PasteItem
                );

            //如果緊壓任何鍵，加速結算
            if (Input.anyKey) 
            {
                isFastAnim = true;
            }
            if (isFastAnim == false)
            {
                yield return new WaitForSeconds(0.5f);
            }
            else if (isFastAnim == true) 
            {
                yield return null;
            }
        }

        float tempScore = 0f;
        string tempText = set_display_Stats.text;



        float scoreJump =
            rawForDisplay_Score_Num / (20 + 10 * Mathf.Clamp((Mathf.Log10(rawForDisplay_Score_Num)) - 3f, 0f, 6f));
        //算分動畫數字跳動幅度(依分數越高越長，限制在2秒~8秒)


        //撥放結算總計分音效(使用本物件的音源，而不沿用音效管理員的)
        AudioManager.instanceAudioManager.GetClip_AndUsingSourcePlay
            (
            AudioSource_ScoreCount,
            AudioManager.instanceAudioManager.set_UI_showResult_CountScore
            );

        while (tempScore < rawForDisplay_Score_Num) 
        {
            rawForDisplay_Score_String = Ckeck_NumOverLimit_and_GiveDigit2String(tempScore, 999999999);
            set_display_Stats.text = tempText + rawForDisplay_Score_String;

            tempScore = Mathf.MoveTowards(tempScore, rawForDisplay_Score_Num, scoreJump);
            if (Input.anyKey)
            {
                isFastAnim = true;
            }

            if (isFastAnim == false)
            {
                yield return new WaitForSeconds(0.03f);
            }
            else if (isFastAnim == true)
            {
                yield return null;
            }
        }
        tempScore = rawForDisplay_Score_Num;
        rawForDisplay_Score_String = Ckeck_NumOverLimit_and_GiveDigit2String(tempScore, 999999999);
        set_display_Stats.text = tempText + rawForDisplay_Score_String;




        //勳章動畫-先放大(4)並淡入，再縮小(2)
        set_displayFINAL_Mark.gameObject.SetActive(true);
        Color markColor = new Color(1f,1f,1f,0f);
        Vector3 markScale = new Vector3(0f, 0f, 1f);
        set_displayFINAL_Mark.color = markColor;
        set_displayFINAL_Mark.gameObject.transform.localScale = markScale;

        float animPercent = 0f; //勳章動畫秒數
        for (animPercent = 0; animPercent <= 1f; animPercent += 1f/30f) 
        {
            //Debug.Log("有放大");

            markColor.a = Mathf.Lerp(markColor.a, 1f, 0.5f);
            var tempScale = Mathf.Lerp(markScale.x, 4f, 0.8f);
            markScale.x = tempScale;
            markScale.y = tempScale;

            set_displayFINAL_Mark.color = markColor;
            set_displayFINAL_Mark.gameObject.transform.localScale = markScale;
            yield return new WaitForSeconds(0.5f/30f);
        }

        markColor.a = 1f;
        markScale = new Vector3(4f, 4f, 1f);
        set_displayFINAL_Mark.color = markColor;
        set_displayFINAL_Mark.gameObject.transform.localScale = markScale;

        for (animPercent = 0; animPercent <= 1; animPercent += 1f/30f) 
        {
            //Debug.Log("有縮小");
            var tempScale= Mathf.MoveTowards(markScale.x, 2f, 2f/30f);
            markScale.x = tempScale;
            markScale.y = tempScale;
            set_displayFINAL_Mark.gameObject.transform.localScale = markScale;
            yield return new WaitForSeconds(0.1f/30f);
        }

        markScale = new Vector3(2f, 2f, 1f);
        set_displayFINAL_Mark.gameObject.transform.localScale = markScale;

        //撥放徽章貼上音效
        AudioManager.instanceAudioManager.GetClip_AndUsingSourcePlay
            (
            AudioManager.Source_UI,
            AudioManager.instanceAudioManager.set_UI_showResult_PasteItem
            );
        //依照對應的計分與徽章，播放對應的音效
        AudioManager.instanceAudioManager.GetClip_AndUsingSourcePlay
            (
            AudioManager.Source_HumanVoice,
            AudioManager.instanceAudioManager.set_HumanVoice_ResultScoreMark[CSset_CodeOfAudioClip_ForScoreMark]
            );


        set_hideBottons_Restart.SetActive(true);
        set_hideBottons_Quit.SetActive(true);
    }
}
