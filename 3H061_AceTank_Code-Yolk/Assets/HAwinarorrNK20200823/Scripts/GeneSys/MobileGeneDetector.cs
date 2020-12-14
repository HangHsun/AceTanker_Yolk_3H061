using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobileGeneDetector : MonoBehaviour
{
    //public float set_detectRadious;
    private bool is_detectObjectsRaw;
    public bool is_detected;
    private void Update()
    {
        is_detected = is_detectObjectsRaw;
        //Debug.Log(is_detectObjects);
    }




    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "BATTLEFIELD_BG")
        {
            is_detectObjectsRaw = false;
        }


        if (collision.tag == "PLAYER" || collision.tag == "ENEMY" || collision.tag == "BATTLEFIELD_OBJECT")
        {
            is_detectObjectsRaw = true;
        }
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {



    }
}
