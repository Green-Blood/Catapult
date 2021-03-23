using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PopupNotification : MonoBehaviour
{
    private const string NOT_ENOUGH_TENSION_MSG = "Not Enough Spring Force (K) to lift the Catapault Arm";
    private const string NOT_ENOUGH_DEPE_MSG = "Not Enough Spring Force (K) to lift the Catapault Arm";

    [SerializeField] private CanvasGroup canvasGroup;
    public TextMeshProUGUI text;

    private bool show = false;

    private void Awake()
    {
        Reset();
    }

    public void Reset()
    {
        canvasGroup.alpha = 0;
        show = false;
    }

    public void Show()
    {
        show = true;
    }

    public void Hide()
    {
        show = false;
    }

    private void Update()
    {
        if(show)
        {
            if(canvasGroup.alpha < 1f)
            {
                canvasGroup.alpha += Time.deltaTime;
            }
        }
        else
        {
            if(canvasGroup.alpha > 0)
            {
                canvasGroup.alpha -= Time.deltaTime;
            }
        }
    }
}
