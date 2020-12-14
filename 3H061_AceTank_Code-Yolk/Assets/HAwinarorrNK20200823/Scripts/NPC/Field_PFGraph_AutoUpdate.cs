using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Field_PFGraph_AutoUpdate : MonoBehaviour
{


    GridGraph GridGraph1031;

    //AstarPath.active


    // Start is called before the first frame update
    void Start()
    {
        //注意Invoke不可輸入引數和括號
        InvokeRepeating("ResetAstarGraph_Scan", 3f, 1.5f);        
    }

    public void ResetAstarGraph_Scan() 
    {
        
        if (GameManager.instance_ThisScript.isPaused == true ||
            GameManager.instance_ThisScript.isPlayerDead == true)
        {
            return;
        }
        AstarPath.active.Scan();
    }
}
