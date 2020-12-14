using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OBJECTS_PARENT_ON_WININFO : MonoBehaviour
{    private void Awake() 
    {
        //綁定在OBJECTS_PARENT_ON_WININFO位置之下，即所有介面最下方
        gameObject.transform.SetParent(GameObject.Find("OBJECTS_PARENT_ON_WININFO").transform);
    }
}
