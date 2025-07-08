using UnityEngine;

public class CapsuleWaterDetector : MonoBehaviour
{
    private Player parentPlayer;

    private void Start()
    {
        // Get the Player script from the parent
        parentPlayer = GetComponentInParent<Player>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Capsule trigger entered with: " + other.gameObject.name + " tagged as: " + other.gameObject.tag);
        if (other.gameObject.tag == "Water")
        {
            // Tell the parent Player that the capsule entered water
            parentPlayer.OnCapsuleEnteredWater();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.tag == "Water")
        {
            parentPlayer.OnCapsuleInWater();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Water")
        {
            parentPlayer.OnCapsuleExitedWater();
        }
    }
}