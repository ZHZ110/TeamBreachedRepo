using UnityEngine;

public class PlayerAnimator: MonoBehaviour
{
    private Animator animator;
    private const string S_PRESSED = "S_pressed";
    private const string W_PRESSED = "W_pressed";
    [SerializeField] private Player player;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animator.SetBool(S_PRESSED, player.S_pressed());
        animator.SetBool(W_PRESSED, player.W_pressed());
    }

    private void Update()
    {
        animator.SetBool(S_PRESSED, player.S_pressed());
        animator.SetBool(W_PRESSED, player.W_pressed());
    }
}
