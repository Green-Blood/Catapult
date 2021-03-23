using UnityEngine;
using TMPro;

public class PhysicsInfoUIPanel : MonoBehaviour
{
    public GameObject distanceWidget;
    public GameObject gravityWidget;
    public GameObject velocityWidget;
    public GameObject timeWidget;

    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI gravityText;
    public TextMeshProUGUI velocityText;
    public TextMeshProUGUI timeText;

    private void Start()
    {
        gravityText.text = Mathf.Abs(Physics.gravity.y) + " m/s²";
    }

    public void ShowAllUIs()
    {
        distanceWidget.SetActive(true);
        gravityWidget.SetActive(true);
        velocityWidget.SetActive(true);
        timeWidget.SetActive(true);
    }

    public void Reset()
    {
        distanceText.text = "0 m";
        velocityText.text = "0 m/s";
        timeText.text = "0 s";
    }

}
