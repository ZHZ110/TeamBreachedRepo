using UnityEngine;
using UnityEngine.UI;
public class Stamina_Bar_UI : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Image barImage;

    private void Start()
    {
        player.onStaminaChanged += Player_onStaminaChanged;
        barImage.fillAmount = 1.0f;
    }

    private void Player_onStaminaChanged(object sender, Player.OnStaminaChangedEventArgs e)
    {
        barImage.fillAmount = e.staminaNormalized;
    }
}
