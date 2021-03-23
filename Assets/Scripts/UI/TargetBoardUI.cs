using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TargetBoardUI : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    public void Reset()
    {
        scoreText.text = "0";
    }
}
