using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ArrowInfoWidget : MonoBehaviour
{
    [SerializeField] private Canvas InfoWidget;
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI forceText;
    [SerializeField] private TextMeshProUGUI velocityText;
    [SerializeField] private TextMeshProUGUI footerText;

    private void Awake()
    {
        InfoWidget.worldCamera = GameManager.GetInstance().uiCam;
    }

    // Update is called once per frame
    void Update()
    {
        Align();
    }

    public void Align()
    {
        Camera mainCam = GameManager.GetInstance().mainCam;

        // Align the forward vector of the Canvas with the forward vector of the camera, so it always faces the camera
        transform.forward = mainCam.transform.forward;
    }

    public void UpdateInfoWidget(float? force = null, float? velocity = null, string header = null, bool hideFooter = false)
    {
        if(force != null)
        {
            forceText.text = force.ToString() + " N";
            forceText.gameObject.SetActive(true);
        }
        else
        {
            forceText.gameObject.SetActive(false);
        }

        // Set Header
        if(!string.IsNullOrEmpty(header))
        {
            headerText.text = header;
        }

        // Set velocity
        if(velocity == null)
        {
            velocityText.gameObject.SetActive(false);
        }
        else
        {
            velocityText.gameObject.SetActive(true);
            velocityText.text = velocity + " m/s";
        }

        footerText.gameObject.SetActive(!hideFooter);
    }
}
