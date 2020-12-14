using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Objects_Tetrapods : OBJECTS_PARENT_ON_FIELD
{
    [Header("消波塊設定參數")]
    public float set_HP;                 //設定血量
    public float set_quakeTimeLength;    //震動設定持續時間
    public float set_quakeDisMax;        //震動設定最大位移
    public float set_quakeFeq;           //震動每秒頻率;
    public List<Sprite> set_HPratioSprite;   //各階段血量使用圖片
    public SpriteRenderer set_DisplaySprite; //圖片子物件
    //public float set_OnFieldMaxTime;     //場上滯留壽命(自行逐漸扣血)

    [Header("消波塊即時演算")]
    float quakeWave_InputDegForCosine = 0f;  //震動波cos輸入角度值
    public float update_HP;                         //即時血量
    bool isQuake;                            //受到衝擊中
    float quakeDirEuler;                     //受到衝擊方位角
    float update_quakeTime;                  //即時震動持續計時
    float update_quakeDis;                   //即時震動位移
    bool isHPautoDecreasing;


    //本物件使用音源
    private AudioSource AudioSource_theTetrapods;
    private void Start()
    {
        AudioSource_theTetrapods = gameObject.AddComponent<AudioSource>();
        AudioSource_theTetrapods.loop = false;
        AudioSource_theTetrapods.playOnAwake = false;
        AudioSource_theTetrapods.outputAudioMixerGroup = AudioManager.instanceAudioManager.AudioFroup_BF_Objects;

    }


    private void OnEnable()
    {
        gameObject.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        set_DisplaySprite.sprite = set_HPratioSprite[0];
        set_DisplaySprite.transform.rotation = Quaternion.Euler(0f, 0f, 90f * (float)Random.Range((int)0, (int)4));
        update_HP = set_HP;
        quakeWave_InputDegForCosine = 0f;
        isQuake = false;
        set_quakeFeq *= (360 / 25); //震動函式放在fixedupdate，因每秒呼叫25次，頻率值需乘上360/25，使其1=一秒內一震動週期之意義
        isHPautoDecreasing = false;
    }

    void Update()
    {
        QuakeStateOfRock();
        if (isHPautoDecreasing == false)
        {
            isHPautoDecreasing = true;
            StartCoroutine(RockHPautoDecrease());
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //Debug.Log("!!!");
        if (collision.gameObject.tag == "BATTLEFIELD_SHELL" || collision.gameObject.tag == "BATTLEFIELD_OBJECT")
        {
            if (collision.gameObject.GetComponent<GenericShellSetting>() != null) 
            {
                collision.gameObject.GetComponent<GenericPrefab_AutoReturnOP>().ReturnToObjectPool();//   GenericShellSetting>().ShellDestroyAndExplosion(); //GetComponent<GenericShellSetting>().ShellDestroyAndExplosion();
            }
            //遭受打擊時播放音效 //直接存取source
            AudioManager.instanceAudioManager.GetClip_AndUsingSourcePlay
                (
                AudioSource_theTetrapods,
                AudioManager.instanceAudioManager.set_Rock_Break
                );


            isQuake = true;
            quakeDirEuler = collision.gameObject.GetComponent<Transform>().rotation.eulerAngles.z; //直接存取剛體的速度值會不正確(因為至此已經過碰撞改變方向)
            //Debug.Log("quakeDirEuler=" + quakeDirEuler);
            update_quakeTime = set_quakeTimeLength;
            update_quakeDis = set_quakeDisMax;
            quakeWave_InputDegForCosine = 0f;

            if (collision.gameObject.GetComponent<GenericObjectDamageSetting>())
            {
                RockReceiveDamage(collision.gameObject.GetComponent<GenericObjectDamageSetting>().info_DamageValue);
            }
        }
    }
    void QuakeStateOfRock() //sine型波動且逐漸衰減的動畫
    {
        if (isQuake == true)
        {
            update_quakeDis -= (Time.fixedDeltaTime / set_quakeTimeLength) * set_quakeDisMax;
            update_quakeDis = Mathf.Clamp(update_quakeDis, 0, set_quakeDisMax);



            var quakeDirVector = new Vector2(-Mathf.Sin(quakeDirEuler * Mathf.Deg2Rad), Mathf.Cos(quakeDirEuler * Mathf.Deg2Rad));

            var quakeWavePos = Mathf.Cos(quakeWave_InputDegForCosine * Mathf.Deg2Rad);
            set_DisplaySprite.transform.localPosition = quakeDirVector * quakeWavePos * update_quakeDis;

            quakeWave_InputDegForCosine += set_quakeFeq;
            if (update_quakeTime <= 0) { isQuake = false; }
            update_quakeTime -= Time.fixedDeltaTime;
        }
        else if (isQuake == false)
        {
            quakeWave_InputDegForCosine = 0f;
        }
    }

    void RockReceiveDamage(float inputDamage)
    {
        update_HP -= inputDamage;
        var HPratio = Mathf.Clamp(update_HP / set_HP, 0f, 1f);
        if (HPratio > 0.67f)
        {
            set_DisplaySprite.sprite = set_HPratioSprite[0];
        }
        else if (HPratio > 0.33f)
        {
            set_DisplaySprite.sprite = set_HPratioSprite[1];
        }
        else if (HPratio > 0f)
        {
            set_DisplaySprite.sprite = set_HPratioSprite[2];
        }
        else if (HPratio <= 0f)
        {
            gameObject.GetComponent<GenericPrefab_AutoReturnOP>().ReturnToObjectPool();
            //RockDestroy();
        }
    }
       
    IEnumerator RockHPautoDecrease() 
    {
        yield return new WaitForSeconds(1f);
        var decreaseUnit = set_HP / gameObject.GetComponent<GenericPrefab_AutoReturnOP>().onField_lifeTime;
        RockReceiveDamage(decreaseUnit);
        isHPautoDecreasing = false;
    }
}


