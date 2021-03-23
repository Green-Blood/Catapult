using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeWall : MonoBehaviour
{
    public List<Rigidbody> boxRigidBodies = new List<Rigidbody>();
    public Dictionary<Transform, Vector3> cubeWallPositions = new Dictionary<Transform, Vector3>();
    public List<GameObject> middleRowBoxes = new List<GameObject>();
    public Transform leftRaycaster;
    public Transform rightRaycaster;
    public LayerMask boxLayer;
    private int TotalKnockableBoxes;

    // Store all box locations in the cubewall
    private void Awake()
    {
        TotalKnockableBoxes = middleRowBoxes.Count;

        foreach(Transform child in transform)
        {
            if (child.gameObject.tag == "cube")
            {
                cubeWallPositions.Add(child.transform, child.position);
            }
        }
    }

    public void Reset()
    {
        foreach(KeyValuePair<Transform, Vector3> kpvCube in cubeWallPositions)
        {
            kpvCube.Key.position = kpvCube.Value;
            kpvCube.Key.rotation = Quaternion.identity;
            Rigidbody rigidBody = kpvCube.Key.GetComponent<Rigidbody>();
            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
        }
    }

    public void CalculateScore()
    {
        Ray leftRay = new Ray(leftRaycaster.position, leftRaycaster.up);
        RaycastHit[] leftHits = Physics.RaycastAll(leftRay, Mathf.Infinity, boxLayer);

        Ray rightRay = new Ray(rightRaycaster.position, rightRaycaster.up);
        RaycastHit[] rightHits = Physics.RaycastAll(rightRay, Mathf.Infinity, boxLayer);

        int totalFallen = TotalKnockableBoxes- (leftHits.Length + rightHits.Length);
        GameManager.GetInstance().uiController.boxesUI.scoreText.text = totalFallen.ToString() + "/" + TotalKnockableBoxes.ToString();
    }

    public void SetKinematic(bool enabled)
    {
        foreach(Rigidbody rigidBody in boxRigidBodies)
        {
            rigidBody.isKinematic = enabled;
        }
    }

    public void ShowCenterRowOnly()
    {
        Reset(); // reset all boxes back to initial positions

        // Hide all boxes first
        foreach (Rigidbody rigidBody in boxRigidBodies)
        {
            rigidBody.gameObject.SetActive(false);
        }

        foreach (GameObject centerBox in middleRowBoxes)
        {
            centerBox.SetActive(true);
        }
    }

    public void ShowAllBoxes()
    {
        // Show all boxes
        foreach (Rigidbody rigidBody in boxRigidBodies)
        {
            rigidBody.gameObject.SetActive(true);
        }
    }
}
