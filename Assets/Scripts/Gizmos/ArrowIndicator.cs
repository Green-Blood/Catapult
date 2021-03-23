using System.Collections;
using EnableEducation;
using UnityEngine;

public class ArrowIndicator : MonoBehaviour
{
    private const float WeightToScale = 200;  // a scale of 1 corresponds to a value of 200 Newtons
    private const float BodySizeToWorldSize = 0.591f; // The worldpoint size of the body collider, at a scale of 1
    private const float bobbedDist = 0.05f;
    private const float bobbingSpeed = 0.025f;
    private const float MIN_ARROW_BODY_SCALE = 0.1f;
    private const float MAX_ARROW_BODY_SCALE = 4f;
    public bool bobbing = true;

    public float arrowWeight = 100;
    public GameObject ArrowBody;
    public ArrowInfoWidget infoWidget;

    private bool animatingForward = false;
    Transform origTransform;

    // The bobbing arrow's start and end position
    [SerializeField] private Vector3 frontPos;
    [SerializeField] Vector3 backPos;
      
    public void Show()
    {
        gameObject.SetActive(true);

        // Store initial position and bobbed position
        UpdateFrontBackAnimPos();

        // Align the ui widget with the camera
        infoWidget.Align();

        // Start the bobbing animation tween
        StartCoroutine(TweenArrow());
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void ScaleWithValue()
    {
        Vector3 bodyScale = ArrowBody.transform.localScale;
        float scaleZ = Mathf.Clamp((arrowWeight / WeightToScale), MIN_ARROW_BODY_SCALE, MAX_ARROW_BODY_SCALE);
        ArrowBody.transform.localScale = new Vector3(bodyScale.x, bodyScale.y, scaleZ);
        UpdateFrontBackAnimPos();
    }

    public void UpdateFrontBackAnimPos()
    {
        float scaleDiff = ArrowBody.transform.localScale.z - 1f;
        frontPos = origTransform.position + transform.up * (scaleDiff * BodySizeToWorldSize);
        backPos = frontPos + (transform.up * bobbedDist);
        transform.position = frontPos;
        animatingForward = false;
    }


    public void SetArrowTransform(Transform _transform)
    {
        origTransform = _transform;
        transform.position = _transform.position;
        transform.rotation = _transform.rotation;
        transform.localScale = _transform.localScale;

        ScaleWithValue(); // Scale the arrow based on its weight value
    }

    public void ChangeArrowWeight(float weight)
    {
        arrowWeight = weight;
        ScaleWithValue();
        infoWidget.UpdateInfoWidget(Mathf.Round(weight));
    }


    // Bob the arrows up and down for aesthetic
    private IEnumerator TweenArrow()
    {
        // Bobb Backwards
        while (bobbing)
        {
            yield return null;

            bool backPosReached = EEMath.Approximately(transform.position, backPos, 0.01f);
            bool frontPosReached = EEMath.Approximately(transform.position, frontPos, 0.01f);

            if (backPosReached)
            {
                animatingForward = true;
            }

            if (frontPosReached)
            {
                animatingForward = false;
            }

            if (!animatingForward)
            {               
                transform.position = Vector3.Lerp(transform.position, backPos, bobbingSpeed);
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, frontPos, bobbingSpeed);
            }
        }

        if (!bobbing)
        {
            transform.position = frontPos;
            yield break;
        }
    }
}
