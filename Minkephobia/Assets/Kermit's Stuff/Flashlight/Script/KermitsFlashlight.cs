using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

public class KermitsFlashlight : MonoBehaviour
{
    [Header("Credits To DatK3rmitVR (Please Give Credits In Game If Used!!!)")]
    public string Credits = "Credits to DatK3rmitVR for creating this script";

    public Light flashlight; // Assign a Light component in the Inspector
    private InputDevice targetDevice;
    public AudioSource clickSound; // Optional sound effect for toggling
    private bool isFlashlightOn = false;
    public float cooldownTime = 2f; // Adjustable cooldown time in Unity Inspector
    private float lastToggleTime = -10f;

    void Start()
    {
        if (flashlight == null)
        {
            flashlight = GetComponentInChildren<Light>(); // Tries to find a child light
        }

        if (flashlight != null)
        {
            flashlight.enabled = false; // Starts off
        }

        InitializeController();
    }

    void InitializeController()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);

        if (devices.Count > 0)
        {
            targetDevice = devices[0];
        }
    }

    void Update()
    {
        if (!targetDevice.isValid)
        {
            InitializeController();
        }

        if (targetDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool isPressed) && isPressed)
        {
            TryToggleFlashlight();
        }
    }

    void TryToggleFlashlight()
    {
        if (Time.time - lastToggleTime >= cooldownTime)
        {
            ToggleFlashlight();
            lastToggleTime = Time.time;
        }
    }

    void ToggleFlashlight()
    {
        if (flashlight != null)
        {
            flashlight.enabled = !flashlight.enabled; // Toggle state
            isFlashlightOn = !isFlashlightOn;

            if (clickSound != null)
            {
                clickSound.Play(); // Play sound effect
            }
        }
    }

#if UNITY_EDITOR
    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 150, 50), "Test Flashlight") && Time.time - lastToggleTime >= cooldownTime)
        {
            TryToggleFlashlight();
        }
    }
#endif
}
