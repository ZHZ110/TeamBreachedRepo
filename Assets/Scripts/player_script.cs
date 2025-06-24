using UnityEngine;
using UnityEngine.EventSystems;

public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.0f;

    private bool s_pressed;
    private bool w_pressed;
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
            //Debug.Log("Wiggping Tail!");
            w_pressed = true;
            inputVector.x = -0.5f;
        }
        else
        {
            w_pressed = false;
        }
        if (Input.GetKey(KeyCode.S))
        {
            s_pressed = true;
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
        transform.position += moveDir * moveSpeed* Time.deltaTime;
        Debug.Log(transform.position);
    }

    public bool S_pressed()
    {
        return s_pressed;
    }

    public bool W_pressed()
    {
        return w_pressed;
    }
}
