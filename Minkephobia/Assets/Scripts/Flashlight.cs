using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using easyInputs;

public class Flashlight : MonoBehaviour
{
    public GameObject fLASHON;
    public GameObject FLASHOFF;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        if (EasyInputs.GetTriggerButtonDown(EasyHand.RightHand))
        {
            fLASHON.SetActive(true);
        }

        if (EasyInputs.GetTriggerButtonDown(EasyHand.LeftHand))
        {
            FLASHOFF.SetActive(false) ;
        }
    }
}
