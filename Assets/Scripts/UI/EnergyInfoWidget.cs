using System;
using UnityEngine;
using TMPro;

public class EnergyInfoWidget : MonoBehaviour
{
    [SerializeField] private Canvas _canvas;
    [SerializeField] private EnergyIndicator energyIndicator;
    [SerializeField] private GameObject gravEnergyWidget;
    [SerializeField] private GameObject elasticEnergyWidget;
    [SerializeField] private GameObject kineticEnergyWidget;
    [SerializeField] private TextMeshProUGUI gravitational_Potential_Energy_Text;
    [SerializeField] private TextMeshProUGUI elastic_Potential_Energy_Text;
    [SerializeField] private TextMeshProUGUI kinetic_Energy_Text;

    private void Awake()
    {
        _canvas.worldCamera = GameManager.GetInstance().uiCam;
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

    public void UpdateInfoWidget(float? gravPotential, float? elasticPotential = null, float? kineticEnergy = null)
    {
        // Gravity
        if(gravPotential != null)
        {
            energyIndicator.gravEnergy = (float)gravPotential;
            UpdateGravEnergy((float)gravPotential);
        }
        else
        {
            gravEnergyWidget.SetActive(false);
        }

        // Elastic
        if (elasticPotential != null)
        {
            energyIndicator.elasticEnergy = (float)elasticPotential;
            UpdateElasticEnergy((float)elasticPotential);
        }
        else
        {
            elasticEnergyWidget.SetActive(false);
        }

        // Kinetic
        if (kineticEnergy != null)
        {
            energyIndicator.kineticEnergy = (float)kineticEnergy;
            UpdateKineticEnergy((float)kineticEnergy);
        }
        else
        {
            kineticEnergyWidget.SetActive(false);
        }
    }

    public void UpdateGravEnergy(float gravPotential)
    {
        gravitational_Potential_Energy_Text.text = (Math.Round(gravPotential, 2)).ToString() + " J";
        gravEnergyWidget.SetActive(true);
    }

    public void UpdateElasticEnergy(float elasticPotential)
    {
        elastic_Potential_Energy_Text.text = (Math.Round(elasticPotential, 2)).ToString() + " J";
        elasticEnergyWidget.SetActive(true);
    }

    public void UpdateKineticEnergy(float kineticEnergy)
    {
        kinetic_Energy_Text.text = (Math.Round(kineticEnergy, 2)).ToString() + " J";
        kineticEnergyWidget.SetActive(true);
    }

    public void ToggleGravInfo(bool show)
    {
        gravEnergyWidget.SetActive(show);
    }

    public void ToggleElasticInfo(bool show)
    {
        elasticEnergyWidget.SetActive(show);
    }

    public void ToggleKineticInfo(bool show)
    {
        kineticEnergyWidget.SetActive(show);
    }
}
