using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Catapult : MonoBehaviour
{
    public CannonBall cannonBall;
    public GameObject catapultArm;
    public Transform launchVector;
    public Transform springVector;
    public Transform resultVector;
    public GameObject rope;

    public const float LAUNCH_SPEED_LESSON = 0.5f;
    public const float LAUNCH_SPEED_FREEPLAY = 5f;
    public float DEFAULT_LAUNCH_ANGLE = 45;
    [SerializeField] private const float cannonBallWeight = 1f;
    public float launchSpeed = 0.5f;

    public float currentArmAngle = 0f;
    public float launchAngle;

    public float ArmAngleRadians
    {
        get
        {
            return currentArmAngle * Mathf.Deg2Rad;
        }
    }

    private Quaternion armInitRotation;
    [ReadOnly] public bool throwCalled = false;
    public bool launched = false;

    [Header("Internal References")]
    public Transform cannonBallPos;
    public Transform startCamTransform;
    public Transform step1CamTransform;
    public Transform step2CamTransform;
    public Transform step3CamTransform;

    private void Awake()
    {
        cannonBall.rigidBody.mass = cannonBallWeight;
        armInitRotation = catapultArm.transform.rotation;
    }

    public void Reset()
    {
        launched = throwCalled = false;
        launchAngle = DEFAULT_LAUNCH_ANGLE;
        rope.SetActive(true);
        currentArmAngle = 0;
        cannonBall.rigidBody.constraints = RigidbodyConstraints.FreezeAll;
        cannonBall.transform.parent = catapultArm.transform;
        catapultArm.transform.rotation = armInitRotation;
        cannonBall.transform.position = cannonBallPos.position;
    }

    public void ThrowBall(Vector3 forceVector, float velocity)
    {
        launched = true;
        cannonBall.transform.SetParent(null);
        cannonBall.rigidBody.constraints = RigidbodyConstraints.None;
        if(GameManager.GetInstance().CurrentPhysicsMode == PhysicsMode.Forces || GameManager.GetInstance().CurrentPhysicsMode == PhysicsMode.Energy)
        {
            cannonBall.rigidBody.useGravity = true;
        }
        cannonBall.rigidBody.AddForce(forceVector * (velocity * cannonBall.Mass), ForceMode.Impulse);
        cannonBall.inAir = true;
    }

    private void Update()
    {
        if(cannonBall.inAir)
        {
            /** (Uncomment Below) to Print velocity and magnitude of velocity as the ball is in the air **/

            // Utils.PrintVec3(cannonBall.rigidBody.velocity, "Cannon Vel ~ ");
            // Debug.Log("vel mag [" + cannonBall.rigidBody.velocity.magnitude + "]");
        }       
    }


    private void LateUpdate()
    {
        if (throwCalled)
        {
             if (currentArmAngle >= launchAngle)
             {
                throwCalled = false;
                cannonBall.transform.rotation = Quaternion.identity;
                return;
             }

            currentArmAngle += (Time.deltaTime * DEFAULT_LAUNCH_ANGLE) * launchSpeed;
            catapultArm.transform.Rotate(-Vector3.up, (Time.deltaTime * DEFAULT_LAUNCH_ANGLE) * launchSpeed );

            if(GameManager.GetInstance().CurrentPhysicsMode == PhysicsMode.Forces 
                || GameManager.GetInstance().CurrentPhysicsMode == PhysicsMode.Energy)
            {
                cannonBall.transform.rotation = Quaternion.identity;
            }
            //cannonBall.transform.rotation = Quaternion.identity;
        }
    }
}
