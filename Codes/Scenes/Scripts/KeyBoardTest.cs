using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyBoardTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log("您按下了W键");
        }

    }


}
