using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OBJECTS_PARENT_ON_FIELD : MonoBehaviour
{    private void Awake()
    {
        gameObject.transform.SetParent(GameObject.Find("OBJECTS_PARENT_ON_FIELD").transform);
    }
}
