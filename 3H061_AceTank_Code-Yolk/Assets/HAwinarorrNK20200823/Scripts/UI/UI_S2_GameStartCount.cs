using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_S2_GameStartCount : MonoBehaviour
{

    public static UI_S2_GameStartCount instance_StartCount;

    public UI_ButtonList useUI;
    public Image set_Frame;
    public GameObject set_CountNum;
    public GameObject set_ReadyTitle;
    public int set_CountTimeLength; //設定倒數秒數   


    public float set_AnimSizeSpeed;
    public float set_ReadyTitle_animScaleMax;
    //public float set_AnimFadeSpeed;
    //public float set_FadeScale; //設定物件消散時的放大倍數
    //public float set_FadeScaleFrame;


    public int update_countTime; //即時倒數秒數
    Color backup_CountNum_Color;

    bool isCountTimeUp;        //確認倒數是否結束
    bool isFadeAnimed = false; //確保動畫不重複啟用

    AudioSource AudioSource_StartCountTempo; //開場倒數節拍另外採用音源


    private void AudioSourceIntialize_StartTempo()
    {
        //進行音源組件的建立、指定分群與初始化
        AudioSource_StartCountTempo = gameObject.AddComponent<AudioSource>();
        AudioSource_StartCountTempo.loop = false;
        AudioSource_StartCountTempo.playOnAwake = false;
        AudioSource_StartCountTempo.clip = AudioManager.instanceAudioManager.set_UI_showResult_CountScore;
        AudioSource_StartCountTempo.outputAudioMixerGroup = AudioManager.instanceAudioManager.AudioGroup_Main_UI;
    }


    private void Awake()
    {
        if (instance_StartCount != null)
        {
            Destroy(gameObject);
            return;
        }

        instance_StartCount = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        //初始化音源
        AudioSourceIntialize_StartTempo();

        //確認所有物件開啟
        gameObject.SetActive(true);
        set_Frame.gameObject.SetActive(true);
        set_ReadyTitle.SetActive(true);
        set_CountNum.gameObject.SetActive(true);

        backup_CountNum_Color = set_CountNum.GetComponent<Text>().color;
        StartCoroutine(CountClock()); //開始進行倒數
        //使用GM的暫停暫停遊戲功能，不然本腳本協程也會影響
    }

    // Update is called once per frame
    void Update()
    {
        if (isCountTimeUp==false)
        {
            TitleAnim();
            CountNumAnim();
        }
        else if (isCountTimeUp==true) 
        {
            UseFadeAnim();
        }        
    }

    //使用協程進行倒數計時
    IEnumerator CountClock()
    {
        GameManager.instance_ThisScript.isPaused = true;
        isCountTimeUp = false;
        isFadeAnimed = false;
        update_countTime = set_CountTimeLength;
        for (int t = set_CountTimeLength; t > 0; t--) 
        {
            update_countTime = t;
            //每個倒數加個節拍音
            AudioManager.instanceAudioManager.GetClip_AndUsingSourcePlay
                (
                AudioSource_StartCountTempo,
                AudioManager.instanceAudioManager.set_UI_showResult_PasteItem
                );
            yield return new WaitForSeconds(1f);
        }
        isCountTimeUp = true;


        //使用音效管理員的source撥放開戰音效
        AudioManager.instanceAudioManager.GetRandomClip_AndUsingSourcePlay(AudioManager.Source_HumanVoice, AudioManager.instanceAudioManager.set_HumanVoice_BattleStart);

        //取消GM的暫停遊戲，開始遊戲
        Debug.Log("開始!");
        GameManager.instance_ThisScript.isPaused = false;
    }

    private void TitleAnim()
    {
        float scaleValue = Mathf.PingPong(Time.time * set_AnimSizeSpeed, set_ReadyTitle_animScaleMax - 1f) + 1f;

        set_ReadyTitle.GetComponent<Transform>().localScale = new Vector3(scaleValue, scaleValue, 1f);
    }

    private void CountNumAnim() 
    {
        set_CountNum.GetComponent<Text>().text = update_countTime.ToString();
        set_CountNum.GetComponent<Text>().color = Vector4.Lerp(Color.red, backup_CountNum_Color, (float)update_countTime / (float)set_CountTimeLength);
    }


    private void UseFadeAnim() 
    {

        if (isFadeAnimed == false)
        {
            isFadeAnimed = true;
        }
        else if (isFadeAnimed == true) 
        {
            return;
        }
        //直接使用其他腳本的縮放窗框方法 //受干擾原因還要再查
        StartCoroutine(useUI.S2_ScaleAndFade_WIN_CanvasGroup(false, useUI.S2_WIN_GameStartCount));

    }
}
