using UnityEngine;

/// <summary>
/// Forwards trigger events from a child collider to the parent WaterfallBarrier
/// This script is automatically added by WaterfallBarrier - don't attach it manually
/// </summary>
public class TriggerForwarder : MonoBehaviour
{
    private WaterfallBarrier parentBarrier;

    void Start()
    {
        // Find the WaterfallBarrier in parent objects
        parentBarrier = GetComponentInParent<WaterfallBarrier>();
        if (parentBarrier == null)
        {
            //Debug.LogError($"TriggerForwarder on {gameObject.name} could not find WaterfallBarrier in parent!");
        }
        else
        {
            //Debug.Log($"TriggerForwarder on {gameObject.name} found WaterfallBarrier on {parentBarrier.gameObject.name}");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"TriggerForwarder: OnTriggerEnter with {other.gameObject.name}");
        if (parentBarrier != null)
        {
            parentBarrier.OnChildTriggerEnter(other);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (parentBarrier != null)
        {
            parentBarrier.OnChildTriggerStay(other);
        }
    }

    void OnTriggerExit(Collider other)
    {
        //Debug.Log($"TriggerForwarder: OnTriggerExit with {other.gameObject.name}");
        if (parentBarrier != null)
        {
            parentBarrier.OnChildTriggerExit(other);
        }
    }
}