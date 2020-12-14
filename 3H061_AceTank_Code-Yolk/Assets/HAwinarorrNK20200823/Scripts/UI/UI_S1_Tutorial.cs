using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_S1_Tutorial : MonoBehaviour
{
    /*
    /*操作說明打字腳本
     *啟動時自動輸出長篇文字與說明圖
     *按任意鍵快速打出文字
     */


    public TextAsset txt_RawData;
    List<string> txt_SplitedSentance = new List<string>();
    public Text sentance_DisplayPanel;

    int sentance_RankNumber;
    bool IsTyping, IsFastTyping;


    //按切頁鈕快速跳到長篇特定位置

    public int page_SplitedNumber; //切頁數
    public ScrollRect page_Scroller; //滑動長篇文章
    public float page_ScrollSpeed; //滑動速度

    public float[] scroll_RefPositions; //滑動對位清單 =[],由腳本建置
    float scroll_CurrentPosition;//平滑移動即時位
    float scroll_TargetPosition; //平滑移動目標設定位
    bool scroll_HasTarget; //是否使用平滑移動


    private void Awake()
    {
        SplitRawTxt(txt_RawData);
        ScrollRefPositionsBuild();
    }

    private void Start()
    {
        IsTyping = false;
        IsFastTyping = false;
        sentance_DisplayPanel.text = "";
    }


    void Update()
    {
        if (IsTyping == false)
        {
            IsTyping = true;
            StartCoroutine(StartTypingTutorial()); //打字中，不重複啟用協程!
        }


        ScollTargetSet_WithMouseScroll();
        ScrollerSmoothToTarget();

    }


    void SplitRawTxt(TextAsset txt)  //拆解txt文字，將每一行轉為變成字串，存成list
    {
        txt_SplitedSentance.Clear();
        sentance_RankNumber = 0;

        var sentance_All = txt.text.Split('\n');
        foreach (var sentanceUnit in sentance_All)
        {
            txt_SplitedSentance.Add(sentanceUnit);
        }
    }

    IEnumerator StartTypingTutorial()
    {
        if (sentance_RankNumber < txt_SplitedSentance.Count) //打完全部文字後停止打字
        {

            for (int i = 0; i < txt_SplitedSentance[sentance_RankNumber].Length; i++) //依字串內容逐個字元打入text
            {
                sentance_DisplayPanel.text += txt_SplitedSentance[sentance_RankNumber][i];

                if (Input.anyKey) //若按任何鍵則加速文字顯示(是加速而不是同時顯示整篇)
                {
                    yield return null;
                    IsFastTyping = true; //啟動加速持續到整篇打完
                }
                else if (IsFastTyping == false) //非加速期間才是原速
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
            IsTyping = false;//停止目前打字狀態，供Update()再次啟用本協程
            sentance_DisplayPanel.text += "\n";//打完一個字串(行)，即換行
            sentance_RankNumber++;//換次行為輸入目標，
        }
        else if (sentance_RankNumber == txt_SplitedSentance.Count) //直到整篇打完，才把加速取消回去
        {
            IsFastTyping = false;
        }
    }

    void ScrollRefPositionsBuild()
    {
        float posInterval = 1f / (page_SplitedNumber - 1f);
        scroll_RefPositions = new float[page_SplitedNumber]; //陣列存取要先設定長度，不等於list
        for (int i = 0; i < page_SplitedNumber; i++)
        {
            scroll_RefPositions[i] = 1 - posInterval * (i);
            if (i == page_SplitedNumber - 1)
            {
                scroll_RefPositions[i] = 0;
            }
        }
    }

    void ScrollerSmoothToTarget()  //啟用時強制以腳本捲動頁面.需搭配拖曳或按鈕才可使用
    {
        if (scroll_HasTarget == true)
        {
            scroll_CurrentPosition = page_Scroller.verticalNormalizedPosition;
            page_Scroller.verticalNormalizedPosition = Mathf.MoveTowards(scroll_CurrentPosition, scroll_TargetPosition, page_ScrollSpeed);

            if (scroll_CurrentPosition == scroll_TargetPosition)
            {
                scroll_HasTarget = false;
            }
        }
    }

    public void ScrollTarget_WithSetButton(int inputPageNum) //以按鈕設定目標(int按鈕分頁編號) //本函數交由按鈕設定(最小值為1，最大值為分頁數)
    {
        scroll_HasTarget = true;
        var targetPageNumInArray = Mathf.Clamp(inputPageNum, 1, scroll_RefPositions.Length) - 1;
        scroll_TargetPosition = scroll_RefPositions[targetPageNumInArray];
    }

    void ScollTargetSet_WithMouseScroll() //以滑鼠滾輪設定目標
    {

        if (Input.mouseScrollDelta.y != 0)
        {
            var CloseTargetPos_InArray = Mathf.Clamp(Mathf.FloorToInt((1 - scroll_CurrentPosition) * (page_SplitedNumber)), 0, (page_SplitedNumber - 1)); //取得當前位置最接近的陣列值

            if (Input.mouseScrollDelta.y > 0) //滑動往上，目標為陣列上面的位置
            {
                CloseTargetPos_InArray -= 1;
            }

            if (Input.mouseScrollDelta.y < 0) //滑動往下，目標為陣列下面位置
            {
                CloseTargetPos_InArray += 1;
            }
            var newTargetPos_InArray = Mathf.Clamp(CloseTargetPos_InArray, 0, (page_SplitedNumber - 1)); //位置推算不超出陣列內容
            scroll_TargetPosition = scroll_RefPositions[newTargetPos_InArray];
            scroll_HasTarget = true;
        }
    }
}
