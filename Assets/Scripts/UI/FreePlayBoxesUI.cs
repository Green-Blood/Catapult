using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FreePlayBoxesUI : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    public void Reset()
    {
        scoreText.text = "0/12";
    }
}
