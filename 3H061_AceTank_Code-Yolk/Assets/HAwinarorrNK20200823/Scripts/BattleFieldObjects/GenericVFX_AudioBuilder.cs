using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericVFX_AudioBuilder : MonoBehaviour
{
    public enum VFXtype { VFXflash , VFXshockwave , VFXexplosionAP , VFXexplosionHE , VFXexplosionObj , VFXexplosion_BombBurst }
    public VFXtype set_Prefab_AudioClipType; //依照Inspector設定之列舉決定使用之音效


    //本物件使用音源
    private AudioSource AudioSource_theVFX;
    private AudioClip CSset_Audioclip;

    private void Awake() //onEnable執行序於Start之前，故需設awake
    {
        AudioSource_theVFX = gameObject.AddComponent<AudioSource>();
        AudioSource_theVFX.loop = false;
        AudioSource_theVFX.playOnAwake = false;
        AudioSource_theVFX.outputAudioMixerGroup = AudioManager.instanceAudioManager.AudioFroup_BF_Objects;

        GetAudioClip_FromEnumAndAudioManager();
    }

    private void OnEnable()
    {
        AudioManager.instanceAudioManager.GetClip_AndUsingSourcePlay(AudioSource_theVFX, CSset_Audioclip);
    }


    private void GetAudioClip_FromEnumAndAudioManager() 
    {
        switch(set_Prefab_AudioClipType) 
        {
            case VFXtype.VFXflash:
                CSset_Audioclip = AudioManager.instanceAudioManager.set_Player_HPrecovery;
                break;
            case VFXtype.VFXshockwave:
                CSset_Audioclip = AudioManager.instanceAudioManager.set_Tank_Fire[1];
                break;
            case VFXtype.VFXexplosionAP:
                CSset_Audioclip = AudioManager.instanceAudioManager.set_Tank_Riochet;
                break;
            case VFXtype.VFXexplosionHE:
                CSset_Audioclip = AudioManager.instanceAudioManager.set_Tank_Fire[2];
                break;
            case VFXtype.VFXexplosionObj:
                CSset_Audioclip = AudioManager.instanceAudioManager.set_Rock_Break;
                break;
            case VFXtype.VFXexplosion_BombBurst:
                CSset_Audioclip = AudioManager.instanceAudioManager.set_Tank_Fire[2];
                break;
        }
    }
    
}
