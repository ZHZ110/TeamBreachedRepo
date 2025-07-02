using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private Wave wave1;
    [SerializeField] private Wave wave2;
    [SerializeField] private Wave wave3;

    public enum SignalType
    {
        Wave_Stop,
        Echolocation,
        Orca_approached,
        Wave_PullOrca,
    }

    public event EventHandler<OnStaminaChangedEventArgs> onStaminaChanged;
    public class OnStaminaChangedEventArgs: EventArgs
    {
        public float staminaNormalized;
    }

    private bool s_pressed;
    private bool w_pressed;
    private int stamina = 1000;
    private int STAMINA_MAX = 1000;
    private float temperature = 0;
    private float TEMPERATURE_MAX = 30;
    private bool taken_by_wave = false;
    private Vector3 wave_location;

    private void Start()
    {
        Transform childTransform = transform.Find("Capsule");
        childTransform.localPosition = Vector3.zero;
        childTransform.localRotation = Quaternion.identity;
        childTransform.localScale = Vector3.one;
    }
    private void Update()
    {
        Vector2 inputVector = new Vector2(0, 0);
        float rotateSpeed = 1.0f;
        if (Input.GetKeyDown(KeyCode.A))
        {
            //Debug.Log("Arching!");
            inputVector.x = -1f;
        }
        if (Input.GetKey(KeyCode.W))
        {
            //Debug.Log("Wigging Tail!");
            w_pressed = true;
            inputVector.x = -0.5f;
            stamina = stamina - 1;
            onStaminaChanged?.Invoke(this, new OnStaminaChangedEventArgs{
                staminaNormalized = (float)stamina / STAMINA_MAX
            });
        }
        else
        {
            w_pressed = false;
        }
        if (Input.GetKey(KeyCode.S))
        {
            s_pressed = true;
            --temperature;
            --stamina;
            onStaminaChanged?.Invoke(this, new OnStaminaChangedEventArgs
            {
                staminaNormalized = (float)stamina / STAMINA_MAX
            });
            //Debug.Log("Splashing water with tail!");
        }
        else
        {
            s_pressed = false;
        }
        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);
        if (Input.GetKeyDown(KeyCode.R))
        {
            //Debug.Log("Rotate!");
            Vector3 rotateDir = new Vector3(0.01f, 0f, 0f);
            //transform.forward += new Vector3(0.01f, 0f, 0f);
            transform.forward = Vector3.Slerp(transform.forward, rotateDir, Time.deltaTime * rotateSpeed);
        }
        //transform.position += moveDir * moveSpeed* Time.deltaTime;
        temperature += 0.01f;

        Debug.Log(transform.position);
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            wave1.SetSignal(SignalType.Echolocation, transform.Find("Capsule").position);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            wave2.SetSignal(SignalType.Echolocation, transform.Find("Capsule").position);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            wave3.SetSignal(SignalType.Echolocation, transform.Find("Capsule").position);
        }
        if(taken_by_wave)
        {
            transform.position = Vector3.MoveTowards(transform.position,
                wave_location, moveSpeed * Time.deltaTime);
        }
    }

    public bool S_pressed()
    {
        return s_pressed;
    }

    public bool W_pressed()
    {
        return w_pressed;
    }

    public int GetStamina()
    {
        return stamina;
    }

    public float GetTemperature()
    {
        return temperature;
    }

    public void SetTakenByWave(bool _takenByWave, Vector3 _waveLocation)
    {
        taken_by_wave = _takenByWave;
        wave_location = _waveLocation;
    }
}
