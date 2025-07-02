using System;
using UnityEngine;
using UnityEngine.UIElements;
using static Player;
using static Unity.Burst.Intrinsics.X86;

public class Wave : MonoBehaviour
{
    private SignalType signal_received;
    private Vector3 orca_location;
    private Vector3 original_wave_pos;
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] Player player;

    private void Start()
    {
        original_wave_pos = transform.position;
        Transform sphere_trans = transform.Find("Sphere");
        if (sphere_trans == null)
        {
            Debug.LogWarning("Could not find 'sphere' as a direct child of " + transform.name);
        }
        else
        {
            Debug.Log("Found sphere at: " + sphere_trans.position);
        }

    }
    private void Update()
    {
        if(Input.GetKey(KeyCode.Alpha0))
        {
            Debug.Log("WaveStopped");
        }
        else if (signal_received == SignalType.Echolocation)
        {
            Debug.Log("echo location called!");
            // moving towards to orca
            transform.Find("Sphere").position = Vector3.MoveTowards(transform.Find("Sphere").position,
                orca_location, moveSpeed * Time.deltaTime);  
        }
        else if (signal_received == SignalType.Wave_PullOrca)
        {
            Debug.Log("wave taking the orca with it");
            transform.Find("Sphere").position = Vector3.MoveTowards(transform.Find("Sphere").position,
                original_wave_pos, moveSpeed * Time.deltaTime);
        }
        //Vector3 moveDir = new Vector3(moveVec.x, 0f, moveVec.y);
        float orcaSize = 1.0f;
        Ray ray = new Ray(transform.Find("Sphere").position, transform.Find("Sphere").forward);
        if (Physics.Raycast(ray, out RaycastHit hit, orcaSize))
        {
            Collider col = hit.collider;
            if (col is CapsuleCollider)
            {
                signal_received = SignalType.Wave_PullOrca;
                Debug.Log("Hit: " + hit.collider.name);
                player.SetTakenByWave(true, original_wave_pos);
            }
        }
        //transform.position += moveDir * moveSpeed * Time.deltaTime;
    }
    public SignalType Get_Signal_Received()
    {
        return signal_received;
    }

    public void SetSignal(SignalType signal, Vector3 player_location)
    {
        signal_received = signal;
        orca_location = player_location;
        //Debug.Log("Orca location received: " + orca_location.x +
            //" , " + orca_location.y + " , " + orca_location.z);
    }
}
