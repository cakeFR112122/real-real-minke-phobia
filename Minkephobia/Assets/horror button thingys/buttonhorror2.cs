using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class buttonhorror2 : MonoBehaviour
{
    public buttonmanager buttonmanager;
    public bool Pressed;
    public GameObject NextButton;

    public void OnTriggerEnter(Collider other)
    {
        if(!Pressed)
        {
            Pressed = true;
            buttonmanager.PressedButton();
            gameObject.SetActive(false);
            NextButton.SetActive(true);
        }
    }
}
