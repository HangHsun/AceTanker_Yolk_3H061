using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericTankAudioBuilder : MonoBehaviour
{
    /*
     *藉由本腳本建立戰車物件音源，
     *並由使用端(玩家輸入/ai/控制器)決定使用時機
     *由本腳本經過判斷後，輸出對應之內容至對應的音源
     *(含音效管理員之音源，並多使用其函式)     
     */



    //設定使用的開火音效編號 //不自動給予，由開發者依其特性設定
    public int set_GunAudioClipCode_inAudioManager;

    private AudioSource AudioSource_TankGun;
    private AudioSource AudioSource_TankHull;
    private AudioSource AudioSource_Tank_Track;

    //先判斷是玩家使用或AI使用
    private bool isPlayerTank;


    // Start is called before the first frame update
    void Start()
    {
        AudioSourceIntialize_ofTank();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    //使用音效管理員建立本車音源(主要供AI或玩家腳本使用) //初始化放在start
    private void AudioSourceIntialize_ofTank()
    {
        //進行音源組件的建立、指定分群與初始化

        //初始化槍管音源，並依照設定編號從Manager取得對應clip
        AudioSource_TankGun = gameObject.AddComponent<AudioSource>();
        AudioSource_TankGun.loop = false;
        AudioSource_TankGun.playOnAwake = false;
        AudioSource_TankGun.clip = AudioManager.instanceAudioManager.set_Tank_Fire[set_GunAudioClipCode_inAudioManager];

        //初始化車身與履帶音源
        AudioSource_TankHull = gameObject.AddComponent<AudioSource>();
        AudioSource_TankHull.loop = false;
        AudioSource_TankHull.playOnAwake = false;

        AudioSource_Tank_Track = gameObject.AddComponent<AudioSource>();
        AudioSource_Tank_Track.loop = false;
        AudioSource_Tank_Track.playOnAwake = false;

        if (gameObject.GetComponent<PlayerControlInput>() && !gameObject.GetComponent<AI_GenericSetting>())
        {
            isPlayerTank = true;
            AudioSource_TankGun.outputAudioMixerGroup = AudioManager.instanceAudioManager.AudioGroup_BF_PlayerTank;
            AudioSource_TankHull.outputAudioMixerGroup = AudioManager.instanceAudioManager.AudioGroup_BF_PlayerTank;
            AudioSource_Tank_Track.outputAudioMixerGroup = AudioManager.instanceAudioManager.AudioGroup_BF_PlayerTank;
        }
        else if (gameObject.GetComponent<AI_GenericSetting>() && !gameObject.GetComponent<PlayerControlInput>())
        {
            isPlayerTank = false;
            AudioSource_TankGun.outputAudioMixerGroup = AudioManager.instanceAudioManager.AudioGroup_BF_NPCtank;
            AudioSource_TankHull.outputAudioMixerGroup = AudioManager.instanceAudioManager.AudioGroup_BF_NPCtank;
            AudioSource_Tank_Track.outputAudioMixerGroup = AudioManager.instanceAudioManager.AudioGroup_BF_NPCtank;
        }
    }








    //子彈消滅的音效交由其腳本決定，本處僅處理其跳彈音效
    //非人聲音效分類------------------------------------------------------------------------------------------------

    //戰車開火
    public void play_TankFire() 
    {    
        AudioManager.instanceAudioManager.GetClip_AndUsingSourcePlay
            (
            AudioSource_TankGun, 
            AudioManager.instanceAudioManager.set_Tank_Fire[set_GunAudioClipCode_inAudioManager]
            );
    }

    //外掛裝甲脫落
    public void play_EXarmorDetach()
    {
        AudioManager.instanceAudioManager.GetClip_AndUsingSourcePlay
            (
            AudioSource_TankHull,
            AudioManager.instanceAudioManager.set_Track_Break
            );
    }


    //人聲音效------------------------------------------------------------------------------------------------------
    //玩家穿透敵方
    public void play_Shell_Penetrate_isNPC(GenericObjectDamageSetting DamageSourceData)
    {
        //由於發音是由敵人發送，因此需先確認該傷害確實屬於玩家輸出
        if (DamageSourceData.info_UserName != GameManager.instance_ThisScript.GAMESETTING_GamePlayerName) { return; }
        if (!isPlayerTank)
        {
            AudioManager.instanceAudioManager.GetRandomClip_AndUsingSourcePlay
                (
                AudioManager.Source_HumanVoice,
                AudioManager.instanceAudioManager.set_HumanVoice_Shell_Penetrate
                );
        }
    }

    //玩家未穿透敵方
    public void play_Shell_noPenetrate_isNPC(GenericObjectDamageSetting DamageSourceData)
    {
        //由於發音是由敵人發送，因此需先確認該傷害確實屬於玩家輸出
        if (DamageSourceData.info_UserName != GameManager.instance_ThisScript.GAMESETTING_GamePlayerName) { return; }
        if (!isPlayerTank)
        {
            AudioManager.instanceAudioManager.GetRandomClip_AndUsingSourcePlay
                (
                AudioManager.Source_HumanVoice,
                AudioManager.instanceAudioManager.set_HumanVoice_Shell_AP_noPenetrate
                );
        }
    }

    //玩家未穿透但敵方有傷
    public void play_Shell_noPeneButDamage_isNPC(GenericObjectDamageSetting DamageSourceData)
    {
        //由於發音是由敵人發送，因此需先確認該傷害確實屬於玩家輸出
        if (DamageSourceData.info_UserName != GameManager.instance_ThisScript.GAMESETTING_GamePlayerName) { return; }
        if (!isPlayerTank)
        {
            AudioManager.instanceAudioManager.GetRandomClip_AndUsingSourcePlay
                (
                AudioManager.Source_HumanVoice,
                AudioManager.instanceAudioManager.set_HumanVoice_Shell_noPeneButDamage
                );
        }
    }

    //玩家擊殺敵方 //注意當擊毀敵人時會覆蓋人聲音源(即上兩個函數之音源輸出)
    public void play_KillEnemy_isNPC(GenericObjectDamageSetting DamageSourceData)
    {
        //由於發音是由敵人發送，因此需先確認該傷害確實屬於玩家輸出
        if (DamageSourceData.info_UserName != GameManager.instance_ThisScript.GAMESETTING_GamePlayerName) { return; }
        if (!isPlayerTank)
        {
            AudioManager.instanceAudioManager.GetRandomClip_AndUsingSourcePlay
                (
                AudioManager.Source_HumanVoice,
                AudioManager.instanceAudioManager.set_HumanVoice_KillEnemy
                );
        }
    }

    //綜合音效------------------------------------------------------------------------------------------------------

    //玩家履帶毀損
    public void play_TrackBreak_isPlayer()
    {
        if (isPlayerTank)
        {
            AudioManager.instanceAudioManager.GetRandomClip_AndUsingSourcePlay
                (
                AudioManager.Source_HumanVoice,
                AudioManager.instanceAudioManager.set_HumanVoice_Track_Broken
                );

            AudioManager.instanceAudioManager.GetClip_AndUsingSourcePlay
                (
                AudioSource_Tank_Track,
                AudioManager.instanceAudioManager.set_Tank_Break
                );            
        }
    }

    //玩家履帶修復完畢
    public void play_TrackRepaired_isPlayer()
    {
        if (isPlayerTank)
        {
            AudioManager.instanceAudioManager.GetRandomClip_AndUsingSourcePlay
                (
                AudioManager.Source_HumanVoice,
                AudioManager.instanceAudioManager.set_HumanVoice_Track_Repaired
                );

            AudioManager.instanceAudioManager.GetClip_AndUsingSourcePlay
                (
                AudioSource_Tank_Track,
                AudioManager.instanceAudioManager.set_Track_Repaired
                );
        }
    }

    //玩家AP跳彈
    public void play_Shell_Riochet_isNPC(GenericObjectDamageSetting DamageSourceData)
    {
        //由於發音是由敵人發送，因此需先確認該傷害確實屬於玩家輸出
        if (DamageSourceData.info_UserName != GameManager.instance_ThisScript.GAMESETTING_GamePlayerName) {return;}
        if (!isPlayerTank)
        {
            AudioManager.instanceAudioManager.GetRandomClip_AndUsingSourcePlay
                (
                AudioManager.Source_HumanVoice,
                AudioManager.instanceAudioManager.set_HumanVoice_Shell_AP_Riochet
                );

            AudioManager.instanceAudioManager.GetClip_AndUsingSourcePlay
                (
                AudioSource_TankHull,
                AudioManager.instanceAudioManager.set_Tank_Riochet
                );
        }
    }

    //玩家掛掉
    public void play_PlayerDead_isPlayer()
    {
        if (isPlayerTank)
        {
            AudioManager.instanceAudioManager.GetRandomClip_AndUsingSourcePlay
                (
                AudioManager.Source_HumanVoice,
                AudioManager.instanceAudioManager.set_HumanVoice_PlayerDead
                );

            AudioManager.instanceAudioManager.GetClip_AndUsingSourcePlay
                (
                AudioSource_TankHull,
                AudioManager.instanceAudioManager.set_Tank_Break
                );
        }
    }



}
