using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyIndicator : MonoBehaviour
{
    public EnergyInfoWidget infoWidget;

    // Keep track of all energies
    public float gravEnergy;
    public float kineticEnergy;
    public float elasticEnergy;

    public void AttachToTransform(Transform _transform)
    {
        transform.position = _transform.position;
        transform.rotation = _transform.rotation;
        transform.localScale = _transform.localScale;
    }

    public void UpdateGravEnergy(float _gravEnergy)
    {
        gravEnergy = _gravEnergy;
        infoWidget.UpdateGravEnergy(_gravEnergy);
    }

    public void UpdateElasticEnergy(float _elasticEnergy)
    {
        elasticEnergy = _elasticEnergy;
        infoWidget.UpdateElasticEnergy(_elasticEnergy);
    }

    public void UpdateKineticEnergy(float _kineticEnergy)
    {
        kineticEnergy = _kineticEnergy;
        infoWidget.UpdateKineticEnergy(_kineticEnergy);
    }

    public void Show()
    {
        gameObject.SetActive(true);

        // Align the ui widget with the camera
        infoWidget.Align();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}