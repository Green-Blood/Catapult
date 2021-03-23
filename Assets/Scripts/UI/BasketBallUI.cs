using TMPro;
using UnityEngine;

public class BasketBallUI : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    public void Reset()
    {
        scoreText.text = "0";
    }
}
