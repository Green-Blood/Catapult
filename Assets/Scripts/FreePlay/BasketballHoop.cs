using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasketballHoop : MonoBehaviour
{
    public int hoopScore = 0;

    public void Reset()
    {
        hoopScore = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "CannonBall")
        {
            hoopScore++;
            GameManager.GetInstance().uiController.ballUIPanel.scoreText.text = hoopScore.ToString();
        }
    }
}
