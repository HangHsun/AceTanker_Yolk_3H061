using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    /*
     * 音效管理員
     * 本腳本需獨自掛載物件
     * 遊戲全程跨場景沿用
     * 欲使用本腳本函數者，需以實例訪問之
     * (OnClick等按鈕則需以其他腳本透過撰寫本實例訪問之，否則場景往返丟失參考 
     */

    //手動調音介面
    public GameObject set_AudioSettingWindow;
    public Slider[] set_SavedVolumeSlider; //注意順序需與存檔的順序相同

    //存取混合器，藉此操控音量
    public AudioMixer AudioMixer_MainGame;
    public AudioMixer AudioMixer_BattleField;

    //存取混合群組，方便各物件音源component對接
    public AudioMixerGroup AudioGroup_Main_BGM;
    public AudioMixerGroup AudioGroup_Main_UI;
    public AudioMixerGroup AudioGroup_Main_HumanVoice;
    public AudioMixerGroup AudioGroup_BF_PlayerTank;
    public AudioMixerGroup AudioGroup_BF_NPCtank;
    public AudioMixerGroup AudioFroup_BF_Objects;

    //本腳本音源 (其他須由物件自行建立-藉由透過本腳本的方法)
    private static AudioSource Source_BGM;
    public static AudioSource Source_UI;         //此音源可供外訪播放(如按鈕使用)
    public static AudioSource Source_HumanVoice; //此音源可供外訪播放(如玩家使用)

    //音效清單-其他腳本欲撥放音效接由此存取
    //非人聲音效清單 //因為需由Inspector拖入，故不加Static
    public AudioClip[] set_BGMs;
    public AudioClip set_Button_click;
    public AudioClip set_Button_click_Light;
    public AudioClip set_Button_showResult;
    public AudioClip set_UI_showResult_PasteItem;
    public AudioClip set_UI_showResult_CountScore;
    public AudioClip set_Player_HPrecovery;
    public AudioClip set_Rock_Break;
    public AudioClip[] set_Tank_Fire;
    public AudioClip set_Tank_Riochet;
    public AudioClip set_Tank_Break;
    public AudioClip set_Track_Break;
    public AudioClip set_Track_Repaired;
    //人聲音效清單
    public AudioClip[] set_HumanVoice_Shell_AP_noPenetrate;
    public AudioClip[] set_HumanVoice_Shell_AP_Riochet;
    public AudioClip[] set_HumanVoice_Shell_Penetrate;
    public AudioClip[] set_HumanVoice_Shell_noPeneButDamage;
    public AudioClip[] set_HumanVoice_KillEnemy;
    public AudioClip[] set_HumanVoice_PlayerDead;
    public AudioClip[] set_HumanVoice_ResultScoreMark;
    public AudioClip[] set_HumanVoice_LevelSelect;
    public AudioClip[] set_HumanVoice_BattleStart;
    public AudioClip[] set_HumanVoice_Track_Broken;
    public AudioClip[] set_HumanVoice_Track_Repaired;


    public int BGM_PlayingCode; //紀錄背景音樂在資料庫的順序(方便下次切換保證不同首
    public float BGMendTime_SinceRealtimeStartUP; //目前使用的BGM結束時刻
    bool isBGM_StopAutoSwitch; //使用BGM漸關功能時，停止BGM自動切換

    

    public static AudioManager instanceAudioManager;
    private void Awake()
    {
        if (instanceAudioManager != null) 
        {
            Destroy(gameObject);
            return;
        }
        instanceAudioManager = this;
        DontDestroyOnLoad(gameObject);

        Source_BGM = gameObject.AddComponent<AudioSource>();
        Source_UI = gameObject.AddComponent<AudioSource>();
        Source_HumanVoice = gameObject.AddComponent<AudioSource>();


        //進行soucrce對接group
        Source_BGM.outputAudioMixerGroup = AudioGroup_Main_BGM;
        Source_UI.outputAudioMixerGroup = AudioGroup_Main_UI;
        Source_HumanVoice.outputAudioMixerGroup = AudioGroup_Main_HumanVoice;

        //背景音樂不循環播放，而是時間到換新的一首
        var randomNum = Random.Range(0, set_BGMs.Length);
        BGM_PlayingCode = randomNum;

        Source_BGM.clip = set_BGMs[BGM_PlayingCode];
        Source_BGM.loop = false;
        Source_BGM.playOnAwake = false;  //換場景亦沿用同首歌
        Source_BGM.Play();

        BGMendTime_SinceRealtimeStartUP = Source_BGM.clip.length;
        isBGM_StopAutoSwitch = false;

    }

    void Start()
    {
        IntializeVolumeSlider_bySAVEDpref();
    }

    private void FixedUpdate()
    {
        BGM_AutoNoRepeatSwitch();        
    }


    private void Update()
    {
        SetVolume_byWindowSlider();
        //GetKeyDown_SetVolumes();
    }





    //初始設定相關函數------------------------------------------------------------------------------------------------------------ 

    //滑桿套用儲存的音量設定
    //此函數務必放在Start而非Awake，才可初始化有效設置Mixer
    private void IntializeVolumeSlider_bySAVEDpref()
    {
        //將滑桿值依序套用儲存值
        for (int i = 0; i < set_SavedVolumeSlider.Length; i++)
        {

            //如果沒有過儲存值，則套用預設音量值
            if (PlayerPrefs.GetFloat(SavedVolumePrefKey[i]).Equals(null))
            {
                PlayerPrefs.SetFloat(SavedVolumePrefKey[i], 0f);
            } else if (PlayerPrefs.GetFloat(SavedVolumePrefKey[i]) < -10f)
            {//如果有過儲存值但很小，則套用預設稍低的音量值
                PlayerPrefs.SetFloat(SavedVolumePrefKey[i], -10f);
            }
            set_SavedVolumeSlider[i].value = PlayerPrefs.GetFloat(SavedVolumePrefKey[i]);
        }

        //再利用滑桿設置函數令內部值再套用滑桿值
        SetVolumeSlider_MAIN_volume_WholeGame(set_SavedVolumeSlider[0].value);
        SetVolumeSlider_MAIN_volume_BGM(set_SavedVolumeSlider[1].value);
        SetVolumeSlider_MAIN_volume_HumanVoice(set_SavedVolumeSlider[2].value);
        SetVolumeSlider_BF_volume_PlayerTank(set_SavedVolumeSlider[3].value);
        SetVolumeSlider_BF_volume_NPCtank(set_SavedVolumeSlider[4].value);
        SetVolumeSlider_BF_volume_Objects(set_SavedVolumeSlider[5].value);
    }


    /*無法正常使用
    //對戰車控制器建立Source和預設Clip
    public void IntializeTankController_AudioSource(GameObject theTank, AudioSource tank_GunAndHull, AudioSource tank_Track)
    {
        tank_GunAndHull = theTank.AddComponent<AudioSource>();
        tank_GunAndHull.clip = set_Tank_Fire;
        tank_GunAndHull.loop = false;
        tank_GunAndHull.playOnAwake = false;

        tank_Track = theTank.AddComponent<AudioSource>();
        tank_Track.clip = set_Track_Break;
        tank_Track.loop = false;
        tank_Track.playOnAwake = false;
    }*/




    //音效播放相關函數------------------------------------------------------------------------------------------------------------
    //方便播放指定音效

    //BGM自動不連續重複且有間隔的切換
    private void BGM_AutoNoRepeatSwitch()
    {
        if (isBGM_StopAutoSwitch == true) 
        {
            return;
        }
        //每當目前背景音樂結束後5秒，才開始播放下一首背景音樂
        if (Time.realtimeSinceStartup >= BGMendTime_SinceRealtimeStartUP + 0f)
        {
            BGM_NoRepeatSwitch();
        }
    }

    private void BGM_NoRepeatSwitch() 
    {

        var randomNum = Random.Range(0, set_BGMs.Length);

        while (randomNum == BGM_PlayingCode)
        {
            randomNum = Random.Range(0, set_BGMs.Length);
        }
        BGM_PlayingCode = randomNum;

        Source_BGM.clip = set_BGMs[BGM_PlayingCode];
        BGMendTime_SinceRealtimeStartUP = Time.realtimeSinceStartup + Source_BGM.clip.length;
        Source_BGM.Play();
    }



    //方便播放指定音效
    public void GetClip_AndUsingSourcePlay(AudioSource GameObject_ASource, AudioClip Manager_AClip) 
    {
        GameObject_ASource.clip = Manager_AClip;
        GameObject_ASource.loop = false;
        GameObject_ASource.Play();
    }


    //方便播放陣列隨機音效
    public void GetRandomClip_AndUsingSourcePlay(AudioSource GameObject_ASource, AudioClip[] Manager_ACarray) 
    {
        //int randomCode = Random.Range(0, Manager_ACarray.Length);

        //整數範圍太小不要直接用隨機，而是用更大的值域作隨機
        float randomCode = Random.Range(0f, 100f);
        randomCode = Mathf.Clamp(randomCode % Manager_ACarray.Length, 0, Manager_ACarray.Length - 1);

        AudioClip randomAC = Manager_ACarray[Mathf.RoundToInt(randomCode)];


        GameObject_ASource.clip = randomAC;
        GameObject_ASource.loop = false;
        GameObject_ASource.Play();
    }

    //供按鈕外部進行BGM漸關
    public void BGM_SmoothTurnOff_AndReset() 
    {
        StartCoroutine(BGM_FadeWithDuration_thenResetVolumeAndBGM(-80f, 5f));
    }
    IEnumerator BGM_FadeWithDuration_thenResetVolumeAndBGM(float minVolume, float FadeTime)
    {
        isBGM_StopAutoSwitch = true;
        //暫存原本音量
        float originalVoulme = set_SavedVolumeSlider[1].value;

        for (float i = 0f; i < FadeTime; i += 1f / 30f) 
        {
            var fadePercent = (i / FadeTime);
            var useVolume = originalVoulme * (1f - fadePercent) + minVolume * (fadePercent);
            useVolume = Mathf.Clamp(useVolume, -80f, 20f);

            //利用滑桿函數令內部值再套用本處設定值(而非滑桿值)
            SetVolumeSlider_MAIN_volume_BGM(useVolume);

            yield return new WaitForFixedUpdate();
        }

        yield return new WaitForSeconds(3f);
        //再利用滑桿函數令內部值再套用原本值(而非滑桿值)
        SetVolumeSlider_MAIN_volume_BGM(originalVoulme);
        //再利用BGM切換函數重設BGM
        BGM_NoRepeatSwitch();
        isBGM_StopAutoSwitch = false;

    }

    //音量調整相關函數------------------------------------------------------------------------------------------------------------

    //視窗滑桿版本
    private void SetVolume_byWindowSlider() 
    {
        if (Input.GetKeyDown(KeyCode.Backspace)) 
        {
            bool windowState = !set_AudioSettingWindow.activeInHierarchy;
            set_AudioSettingWindow.SetActive(windowState);
        }
    }



    private string[] SavedVolumePrefKey = {
        "SAVED_MAIN_volume_WholeGame",
        "SAVED_MAIN_volume_BGM",
        "SAVED_MAIN_volume_HumanVoice",
        "SAVED_BF_volume_PlayerTank",
        "SAVED_BF_volume_NPCtank",
        "BF_volume_Objects" };
    public void SetVolumeSlider_MAIN_volume_WholeGame(float sliderValue) 
    {
        AudioMixer_MainGame.SetFloat("MAIN_volume_WholeGame", Mathf.Clamp(sliderValue, -30f, 20f));
        PlayerPrefs.SetFloat(SavedVolumePrefKey[0], sliderValue);
    }
    public void SetVolumeSlider_MAIN_volume_BGM(float sliderValue)
    {
        AudioMixer_MainGame.SetFloat("MAIN_volume_BGM", Mathf.Clamp(sliderValue, -30f, 20f));
        PlayerPrefs.SetFloat(SavedVolumePrefKey[1], sliderValue);
    }
    public void SetVolumeSlider_MAIN_volume_HumanVoice(float sliderValue)
    {
        AudioMixer_MainGame.SetFloat("MAIN_volume_HumanVoice", Mathf.Clamp(sliderValue, -30f, 20f));
        PlayerPrefs.SetFloat(SavedVolumePrefKey[2], sliderValue);
    }
    public void SetVolumeSlider_BF_volume_PlayerTank(float sliderValue)
    {
        AudioMixer_BattleField.SetFloat("BF_volume_PlayerTank", Mathf.Clamp(sliderValue, -30f, 20f));
        PlayerPrefs.SetFloat(SavedVolumePrefKey[3], sliderValue);
    }
    public void SetVolumeSlider_BF_volume_NPCtank(float sliderValue)
    {
        AudioMixer_BattleField.SetFloat("BF_volume_NPCtank", Mathf.Clamp(sliderValue, -30f, 20f));
        PlayerPrefs.SetFloat(SavedVolumePrefKey[4], sliderValue);
    }
    public void SetVolumeSlider_BF_volume_Objects(float sliderValue)
    {
        AudioMixer_BattleField.SetFloat("BF_volume_Objects", Mathf.Clamp(sliderValue, -30f, 20f));
        PlayerPrefs.SetFloat(SavedVolumePrefKey[5], sliderValue);
    }

    /*
    //(快捷鍵版本...暫時停用)
    private void GetKeyDown_SetVolumes() 
    {        
        if (Input.GetKeyDown(KeyCode.F2))
        { SetVolume(VolumeOption.main_WholeGame, 5f); }
        else if (Input.GetKeyDown(KeyCode.F1))
        { SetVolume(VolumeOption.main_WholeGame, -5f); }

        if (Input.GetKeyDown(KeyCode.F4))
        { SetVolume(VolumeOption.main_BGM, 5f); }
        else if (Input.GetKeyDown(KeyCode.F3))
        { SetVolume(VolumeOption.main_BGM, -5f); }

        if (Input.GetKeyDown(KeyCode.F5))
        { SetVolume(VolumeOption.main_HumanVoice, 5f); }
        else if (Input.GetKeyDown(KeyCode.F6))
        { SetVolume(VolumeOption.main_HumanVoice, -5f); }

        if (Input.GetKeyDown(KeyCode.F7))
        { SetVolume(VolumeOption.BF_Player, 5f); }
        else if (Input.GetKeyDown(KeyCode.F8))
        { SetVolume(VolumeOption.BF_Player, -5f); }

        if (Input.GetKeyDown(KeyCode.F10))
        { SetVolume(VolumeOption.BF_NPC, 5f); }
        else if (Input.GetKeyDown(KeyCode.F9))
        { SetVolume(VolumeOption.BF_NPC, -5f); }

        if (Input.GetKeyDown(KeyCode.F12))
        { SetVolume(VolumeOption.BF_Objects, 5f); }
        else if (Input.GetKeyDown(KeyCode.F11))
        { SetVolume(VolumeOption.BF_Objects, -5f); }
    }

    private enum VolumeOption { main_WholeGame, main_BGM , main_HumanVoice , BF_Player , BF_NPC , BF_Objects }

    private void SetVolume(VolumeOption setOption, float addValue)
    {
        float currentVolume = 0f;

        switch (setOption)
        {
            case VolumeOption.main_WholeGame:
                MainGame_AudioMixer.GetFloat("MAIN_volume_WholeGame", out currentVolume);
                MainGame_AudioMixer.SetFloat("MAIN_volume_WholeGame", Mathf.Clamp(currentVolume + addValue, -80f, 20f));
                break;
            case VolumeOption.main_BGM:
                MainGame_AudioMixer.GetFloat("MAIN_volume_BGM", out currentVolume);
                MainGame_AudioMixer.SetFloat("MAIN_volume_BGM", Mathf.Clamp(currentVolume + addValue, -80f, 20f));
                break;
            case VolumeOption.main_HumanVoice:
                MainGame_AudioMixer.GetFloat("MAIN_volume_HumanVoice", out currentVolume);
                MainGame_AudioMixer.SetFloat("MAIN_volume_HumanVoice", Mathf.Clamp(currentVolume + addValue, -80f, 20f));
                break;
            case VolumeOption.BF_Player:
                MainGame_AudioMixer.GetFloat("BF_volume_PlayerTank", out currentVolume);
                MainGame_AudioMixer.SetFloat("BF_volume_PlayerTank", Mathf.Clamp(currentVolume + addValue, -80f, 20f));
                break;
            case VolumeOption.BF_NPC:
                MainGame_AudioMixer.GetFloat("BF_volume_NPCtank", out currentVolume);
                MainGame_AudioMixer.SetFloat("BF_volume_NPCtank", Mathf.Clamp(currentVolume + addValue, -80f, 20f));
                break;
            case VolumeOption.BF_Objects:
                MainGame_AudioMixer.GetFloat("BF_volume_Objects", out currentVolume);
                MainGame_AudioMixer.SetFloat("BF_volume_Objects", Mathf.Clamp(currentVolume + addValue, -80f, 20f));
                break;
        }
    }*/



}
