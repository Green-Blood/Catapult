using System;
using System.Collections;
using System.Collections.Generic;
using EnableEducation;
using UnityEngine;
using UnityEditor;

public enum PhysicsMode
{
    Forces,
    Energy,
    FreePlayTarget,
    FreePlayBox,
    BasketballChallenge
}

public class GameManager : MonoBehaviour
{
    [Header("Start Params")]
    public const float DEFAULT_CANNON_BALL_MASS = 30;
    public const float DEFAULT_SPRING_FORCE = 600;
    public const float FORCE_SCALE_FACTOR = 1f; // Extra scaling needed to make the resultant force to shoot the ball
    public float DistanceFromGround_At_TimeOFLaunch;     // The minimum distance between the cannon ball and the ground to initiate the SlopeDown learning step
    public GameObject arrowBasicFrontPivotPrefab;
    public GameObject arrowBasicBackPivotPrefab;
    public UIController uiController;

    public List<GameObject> markers = new List<GameObject>();
    public GameObject terrain;

    public bool lessonStarted = false;
    private static GameManager instance;
    [SerializeField] private PopupNotification popup;

    [Header("Configuration Parameters")]
    [SerializeField] private float camLerpSpeed = 0.1f;

    Coroutine activeCoroutine;

    public enum LearningStep
    {
        NotStarted = -1,
        PreLaunch = 0,
        TimeOfLaunch = 1,
        MidAir = 2,
        Stopped = 3,
    }

    public enum MidAirStep
    {
        Undefined,
        Launching,
        Maxheight,
        SlopeDown,
        CompleteArc
    }

    [ReadOnly] [SerializeField] private PhysicsMode currentPhysicsMode = PhysicsMode.Forces;

    public PhysicsMode CurrentPhysicsMode
    {
        get
        {
            return currentPhysicsMode;
        }
        set
        {
            OnPhysicsModeChanged(value);
            currentPhysicsMode = value;
        }
    }

    [ReadOnly] public LearningStep currentStep = LearningStep.NotStarted;
    [ReadOnly] public MidAirStep midAirStep = MidAirStep.Undefined;

    [Header("Scene References")]
    public Camera mainCam;
    public Camera uiCam;

    public Action OnEditorFocus;

    public bool sliderLockOut = false;
    private bool canProcessNextStep = true;
    public bool CanProcessNextStep
    {
        get
        {
            return canProcessNextStep;
        }
        set
        {
            canProcessNextStep = value;
            if(OnEditorFocus!=null)
            {
                OnEditorFocus();
            }
        }
    }

    private bool failLaunch = false;
    [SerializeField] private CubeWall cubeWall;
    [SerializeField] public TargetBoard targetBoard;
    [SerializeField] public Catapult catapult;
    [SerializeField] public CannonBall cannonBall;
    [SerializeField] public BasketballHoop ballHoop;
    [SerializeField] private ArrowIndicator gravArrow;
    [SerializeField] private ArrowIndicator tensionArrow;
    [SerializeField] private ArrowIndicator springArrow;
    [SerializeField] private ArrowIndicator resultArrow;
    [SerializeField] private DistanceGizmo distanceGizmo;
    [SerializeField] private EnergyIndicator energyWidget;

    [ReadOnly] [SerializeField] private Transform tensionPoint;
    [ReadOnly] [SerializeField] private Transform springPoint;
    [ReadOnly] [SerializeField] private Transform weightPoint;
    [ReadOnly] [SerializeField] private Transform resultPoint;

    public LayerMask terrainLayer;
    [SerializeField] private List<ArrowIndicator> vectorArrows = new List<ArrowIndicator>();

    [ReadOnly] [SerializeField] private float springK;
    [ReadOnly] [SerializeField] private float springForce;

    public float SpringK
    {
        get
        {
            return springK;
        }
        set
        {
            springK = value;
            Vector3 springVector = CalculateSVector();
            SpringForce = springVector.magnitude;
        }
    }

    public float SpringForce
    {
        get
        {
            return springForce;
        }
        set
        {
            springForce = value;
            ShowSpringChange();
        }
    }

    [ReadOnly] [SerializeField] private float tensionForce;
    public float TensionForce
    {
        get
        {
            return tensionForce;
        }
        set
        {
            tensionForce = value;
            tensionArrow.ChangeArrowWeight(tensionForce);
        }
    }

    [ReadOnly] [SerializeField] private float resultForce;
    public float ResultForce
    {
        get
        {
            return resultForce;
        }
        set
        {
            resultForce = value;
            if (resultArrow.isActiveAndEnabled)
            {
                resultArrow.ChangeArrowWeight(resultForce);
            }
        }
    }

    #region Physics Formulas

    // The Elastic Potential Energy of the Cannonball as a result of the recoiled Catapult Arm 
    // Formula: ( 0.5 * springK * angle² )
    private float Delta_Elastic_Potential_Energy()
    {
        return 0.5f * SpringK * Mathf.Pow(catapult.DEFAULT_LAUNCH_ANGLE * Mathf.Deg2Rad, 2);
    }

    // The difference of Gravitational Potential Energy 
    // Formula: ( m * g * √2/2)
    private float Delta_Gravitational_Potential_Energy()
    {
        return cannonBall.WeightForce * (Mathf.Sqrt(2) / 2f);
    }

    // The ratio of (Delta Elastic Potential Energy) over (Delta Gravitation Potential Energy)
    // Formula: DEPE / DGPE
    private float ratio_Of_DEPE_Over_DGPE()
    {
        float ratio = Delta_Elastic_Potential_Energy() / Delta_Gravitational_Potential_Energy();
        return ratio;
    }

    // Equation for Kinetic Energy at time of launch 
    // Formula: (0.5 * m * vI²)
    private float Kinetic_Energy_At_Launch()
    {
        float vel = Velocity_At_Time_Of_Launch();
        return 0.5f * cannonBall.Mass * Mathf.Pow(vel, 2);
    }

    // Find the instantaneous velocity at the time of the cannonball's launch from the Catapault Arm
    // Formula: √(springK / m) * angle² - (g * √2)
    public float Velocity_At_Time_Of_Launch()
    {
        float velocity = Mathf.Sqrt(((springK / cannonBall.Mass) * Mathf.Pow((catapult.DEFAULT_LAUNCH_ANGLE * Mathf.Deg2Rad), 2)) - (Physics.gravity.y * Mathf.Sqrt(2f)));

        return velocity;
    }

    // Delta Time is equal to the Delta Vertical Velocity divided by the vertical acceleration (gravity)
    // Formula: ((vVertF - vVertI) / g)
    private float CalculateDeltaTime()
    {
        float velocity = Velocity_At_Time_Of_Launch();
        float vertVelocity = velocity * Mathf.Cos(catapult.DEFAULT_LAUNCH_ANGLE * Mathf.Deg2Rad);
        float vI = vertVelocity;
        float vF = -vertVelocity;
        float deltaV = vF - vI;
        float deltaTime = deltaV / Physics.gravity.y;
        uiController.physicsUIPanel.timeText.text = Math.Round(deltaTime, 2).ToString() + " s";
        return deltaTime;
    }

    /// <summary>
    /// Distance is calculated using the horizontal component of velocity multiplies by delta time 
    /// Formula: ( vIh * dt )
    /// </summary>
    public void CalculateDistance()
    {
        float horizVelocity = Velocity_At_Time_Of_Launch() * Mathf.Sin(catapult.DEFAULT_LAUNCH_ANGLE * Mathf.Deg2Rad);
        float deltaTime = CalculateDeltaTime();
        float horizontalDistance = horizVelocity * deltaTime;

        // Stretch the distance gizmo to show the pre-calculated horizontal distance
        distanceGizmo.StretchGizmo(horizontalDistance);

        // Update the Distance Text on the UI Physics Info (Top Left UI)  
        uiController.physicsUIPanel.distanceText.text = Math.Round(horizontalDistance, 2).ToString() + " m";
    }

    // Force that is the combined Normal and Centrifugal force of the catapult spoon on the cannonball as the arm rises
    // Formula: (Nx+Cx, Ny+Cy)
    private Vector3 CalculateSVector()
    {
        return (NormalVector() + CentrifugalVector()) * new Vector3(-1, 1, 0); // Multiply x by -1 to horizontally flip the S Vector to the catapult's local orientation
    }

    // Spring Force vector that is perpendicular to the catapault arm (Force that acts on the cannonball from the catapult spoon as the arm rises)
    private Vector2 NormalVector()
    {
        float angle = catapult.ArmAngleRadians;
        Vector2 normalizedNormal = new Vector2(0, 1);   // Perpendicular 90 degrees to catapault arm

        return normalizedNormal * ratio_Of_DEPE_Over_DGPE() * cannonBall.WeightForce * (1 - (2 * angle) / Mathf.PI);
    }

    // Centrifugal Force vector that is Parallel to Catapault arm (Force that keeps the cannonball horizontally in the spoon as the arm rises)
    private Vector2 CentrifugalVector()
    {
        float angle = catapult.ArmAngleRadians;
        Vector2 normalizedCentrifugal = new Vector2(1, 0);  // Parallel 0 degrees to catapault arm

        return normalizedCentrifugal * (ratio_Of_DEPE_Over_DGPE() * cannonBall.WeightForce * ((2 * angle) / Mathf.PI));
    }

    #endregion

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        Reset();
    }

    public void Update()
    {
        // Update UI
        if (cannonBall.paused)
        {
            uiController.physicsUIPanel.velocityText.text = (float)Math.Round(cannonBall.currentVelocity.magnitude, 2) + " m/s";
        }
        else
        {
            uiController.physicsUIPanel.velocityText.text = (float)Math.Round(cannonBall.rigidBody.velocity.magnitude, 2) + " m/s";
        }

        if (catapult.launched)
        {
            // Constantly raycast to detect knocked over boxes, once the cannonball is launched in Freeplay Box Mode
            if (CurrentPhysicsMode == PhysicsMode.FreePlayBox)
            {
                cubeWall.CalculateScore();
            }
        }
    }

    public void Reset()
    {
        failLaunch = false;
        CanProcessNextStep = true;
        sliderLockOut = false;
        popup.Reset();
        HideGizmoAndWidgets();

        if(activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }

        foreach(GameObject marker in markers)
        {
            Destroy(marker);
        }
        markers.Clear();
        currentStep = LearningStep.NotStarted;
        midAirStep = MidAirStep.Undefined;

        foreach(ArrowIndicator arrow in vectorArrows)
        {
            Destroy(arrow.gameObject);
        }
        vectorArrows.Clear();

        // Reset Camera view transform
        mainCam.transform.position = catapult.startCamTransform.position;
        mainCam.transform.rotation = catapult.startCamTransform.rotation;

        catapult.Reset();
        cannonBall.Reset();
        cubeWall.Reset();
    }

    private void OnPhysicsModeChanged(PhysicsMode physicsMode)
    {
        Reset();

        uiController.OnPhysicsModeChanged(physicsMode);

        switch (physicsMode)
        {
            case PhysicsMode.Forces:
                ExecuteVectorLearningMode();
                break;

            case PhysicsMode.Energy:
                ExecuteEnergyLearningMode();
                break;

            case PhysicsMode.FreePlayTarget:
                ExecuteFreePlayTargetMode();
                break;

            case PhysicsMode.FreePlayBox:
                ExecuteFreePlayBoxMode();
                break;

            case PhysicsMode.BasketballChallenge:
                ExecuteBasketballMode();
                break;
        }
    }

    private void HideGizmoAndWidgets()
    {
        gravArrow.Hide();
        springArrow.Hide();
        tensionArrow.Hide();
        resultArrow.Hide();
        distanceGizmo.Hide();
        energyWidget.Hide();

        foreach (ArrowIndicator arrow in vectorArrows)
        {
            arrow.Hide();
        }
    }

    public static GameManager GetInstance()
    {
        return instance;
    }

    public static bool HasInstance()
    {
        return instance;
    }

    public void ExecuteVectorLearningMode()
    {
        cubeWall.gameObject.SetActive(false);
        targetBoard.gameObject.SetActive(false);
        ballHoop.gameObject.SetActive(false);

        uiController.physicsUIPanel.ShowAllUIs();

        catapult.launchSpeed = Catapult.LAUNCH_SPEED_LESSON;
    }

    public void ExecuteEnergyLearningMode()
    {
        cubeWall.gameObject.SetActive(false);
        targetBoard.gameObject.SetActive(false);
        ballHoop.gameObject.SetActive(false);

        uiController.physicsUIPanel.ShowAllUIs();

        catapult.launchSpeed = Catapult.LAUNCH_SPEED_LESSON;
    }

    // Reset defaults and initiate Free Play Target Mode
    public void ExecuteFreePlayTargetMode()
    {
        cubeWall.gameObject.SetActive(true);
        cubeWall.ShowAllBoxes();

        targetBoard.gameObject.SetActive(true);
        ballHoop.gameObject.SetActive(false);
        cubeWall.SetKinematic(true);

        // Hide Distance UI during Free play
        uiController.physicsUIPanel.ShowAllUIs();
        uiController.physicsUIPanel.distanceWidget.SetActive(false);

        catapult.launchSpeed = Catapult.LAUNCH_SPEED_FREEPLAY;

        InitializeFreePlayTargetMode();
    }

    // Initialize FreePlay Target Mode
    public void InitializeFreePlayTargetMode()
    {
        Vector3 SVector = CalculateSVector();

        springArrow.SetArrowTransform(cannonBall.SpringPoint_Centered);
        springArrow.transform.up = SVector.normalized;
        SpringK = SVector.magnitude;

        gravArrow.SetArrowTransform(cannonBall.CenterWeightPoint);

        springArrow.Show();
        gravArrow.Show();

        CalculateForces();
    }

    // Reset defaults and initiate Free Play Box Mode
    public void ExecuteFreePlayBoxMode()
    {
        cubeWall.gameObject.SetActive(true);
        cubeWall.ShowCenterRowOnly();

        targetBoard.gameObject.SetActive(false);
        ballHoop.gameObject.SetActive(false);
        cubeWall.SetKinematic(false);

        // Hide Distance UI during Free play
        uiController.physicsUIPanel.ShowAllUIs();
        uiController.physicsUIPanel.distanceWidget.SetActive(false);
        uiController.boxesUI.Reset();

        catapult.launchSpeed = Catapult.LAUNCH_SPEED_FREEPLAY;

        springArrow.SetArrowTransform(cannonBall.SpringPoint_Centered);
        gravArrow.SetArrowTransform(cannonBall.CenterWeightPoint);

        springArrow.Show();
        gravArrow.Show();

        CalculateForces();
    }

    // Reset defaults and initiate Free Play Basketball Mode
    public void ExecuteBasketballMode()
    {
        cubeWall.gameObject.SetActive(true);
        cubeWall.ShowAllBoxes();
        targetBoard.gameObject.SetActive(false);
        ballHoop.gameObject.SetActive(true);
        ballHoop.Reset();
        cubeWall.SetKinematic(true);

        // Hide Distance UI during Free play
        uiController.physicsUIPanel.ShowAllUIs();
        uiController.physicsUIPanel.distanceWidget.SetActive(false);

        catapult.launchSpeed = Catapult.LAUNCH_SPEED_FREEPLAY;

        springArrow.SetArrowTransform(cannonBall.SpringPoint_Centered);
        gravArrow.SetArrowTransform(cannonBall.CenterWeightPoint);

        springArrow.Show();
        gravArrow.Show();

        CalculateForces();
    }

    public void LaunchFreePlayCannonBall()
    {
        catapult.throwCalled = true;
        activeCoroutine = StartCoroutine(DoProcessFreePlayLaunch());
    }

    public void ExecuteLearningStep()
    {
        switch (currentStep)
        {
            case LearningStep.PreLaunch:
                if (CurrentPhysicsMode == PhysicsMode.Forces)
                {
                    activeCoroutine = StartCoroutine(DoProcessPreLaunch_Forces());
                }
                else if(CurrentPhysicsMode == PhysicsMode.Energy)
                {
                    activeCoroutine = StartCoroutine(DoProcessPreLaunch_Energy());
                }
                break;

            case LearningStep.TimeOfLaunch:
                if (CurrentPhysicsMode == PhysicsMode.Forces)
                {
                    activeCoroutine = StartCoroutine(DoProcessTimeOfLaunch_Forces());
                }
                else if (CurrentPhysicsMode == PhysicsMode.Energy)
                {
                    activeCoroutine = StartCoroutine(DoProcessTimeOfLaunch_Energy());
                }
                break;

            case LearningStep.MidAir:
                if (CurrentPhysicsMode == PhysicsMode.Forces)
                {
                    activeCoroutine = StartCoroutine(DoProcessMidAirState_Forces());
                }
                else if (CurrentPhysicsMode == PhysicsMode.Energy)
                {
                    activeCoroutine = StartCoroutine(DoProcessMidAirState_Energy());
                }
                break;

            case LearningStep.Stopped:
                if (CurrentPhysicsMode == PhysicsMode.Forces)
                {
                    activeCoroutine = StartCoroutine(DoProcessStoppedState_Forces());
                }
                else
                {
                    activeCoroutine = StartCoroutine(DoProcessStoppedState_Energy());
                }
                break;
        }
    }

    #region Forces Lessons
    public IEnumerator DoProcessPreLaunch_Forces()
    {
        Debug.Log("Observe Forces of Gravity, Tension and Spring before launch");

        tensionPoint = cannonBall.TensionPoint;
        weightPoint = cannonBall.LeftWeightPoint;
        springPoint = cannonBall.SpringPoint_LeftOffset;

        // Set all arrow starting positions
        Vector3 SVector = CalculateSVector();

        springArrow.SetArrowTransform(springPoint);
        springArrow.transform.up = SVector.normalized;

        tensionArrow.SetArrowTransform(tensionPoint);
        gravArrow.SetArrowTransform(weightPoint);

        SpringK = SVector.magnitude;

        // Set default spring force
        springArrow.Show();

        // Show Tensions force
        tensionArrow.Show();

        CalculateForces();  // To ensure latest forces are calculated

        // Set Gravity Force and show
        gravArrow.ChangeArrowWeight(cannonBall.WeightForce);
        gravArrow.Show();

        CanProcessNextStep = false;
        yield return StartCoroutine(DoLerpCameraToTransform(catapult.step1CamTransform));
        CanProcessNextStep = true;

        // Position the Distance Gizmo and Rotate it around the Catapault arm to match the time at launch position of the cannonball
        distanceGizmo.transform.position = cannonBall.transform.position;
        distanceGizmo.transform.RotateAround(catapult.catapultArm.transform.position, -catapult.catapultArm.transform.up, catapult.DEFAULT_LAUNCH_ANGLE);
        distanceGizmo.transform.rotation = Quaternion.identity;
        DistanceFromGround_At_TimeOFLaunch = distanceGizmo.transform.position.y - terrain.transform.position.y; // update the distance of the cannonball from the ground at time of launch
        distanceGizmo.Show();
    }

    public IEnumerator DoProcessTimeOfLaunch_Forces()
    {
        CanProcessNextStep = false;

        yield return StartCoroutine(DoLerpCameraToTransform(catapult.step2CamTransform));

        Debug.Log("Observe Forces of Gravity, Spring and Resultant Force at time of launch");

        catapult.rope.gameObject.SetActive(false);

        catapult.launchAngle = catapult.DEFAULT_LAUNCH_ANGLE;

        tensionPoint = cannonBall.TensionPoint;
        weightPoint = cannonBall.LeftWeightPoint;
        springPoint = cannonBall.SpringPoint_LeftOffset;
        resultPoint = cannonBall.ResultPoint_RightOffset;

        resultArrow.SetArrowTransform(resultPoint);
        resultArrow.Show();

        ResultForce = 0;

        /** Smooth lerp the draining of tensions and adding of resultant force over constant time **/
        const float drainSpeed = 0.035f;
        float initialTension = TensionForce;
        float lerpTension = TensionForce;
        while (!EEMath.Approximately(lerpTension, 0, 0.1f))
        {
            lerpTension = Mathf.Lerp(lerpTension, 0, drainSpeed);
            TensionForce = lerpTension;
            ResultForce = initialTension - TensionForce;
            yield return null;
        }

        yield return new WaitForSeconds(2); // Wait to process the changes of forces that was animated

        if (ResultForce <= 0)
        {
            popup.Show();
            yield break;
        }

        tensionArrow.Hide();
        springArrow.SetArrowTransform(catapult.springVector);
        gravArrow.SetArrowTransform(cannonBall.CenterWeightPoint);
        springArrow.bobbing = gravArrow.bobbing = resultArrow.bobbing = false;
        catapult.throwCalled = true;

        failLaunch = false;
        float ratio = ratio_Of_DEPE_Over_DGPE();
        if (ratio <= 1f)
        {
            catapult.launchAngle *= ratio;
            failLaunch = true;
        }

        while (catapult.throwCalled)
        {
            yield return null;

            // Set all arrow starting positions
            Vector3 SVector = CalculateSVector();

            springArrow.SetArrowTransform(catapult.springVector);
            catapult.springVector.transform.up = SVector.normalized;

            // Update Spring Force using KForce
            SpringK = springK;

            CalculateResultVector();    // calculate the result vector, as Spring

            resultArrow.SetArrowTransform(catapult.resultVector);
            gravArrow.SetArrowTransform(cannonBall.CenterWeightPoint);
        }

        springArrow.bobbing = gravArrow.bobbing = resultArrow.bobbing = true;
        springArrow.Show();
        gravArrow.Show();
        resultArrow.Show();

        CanProcessNextStep = true;

        activeCoroutine = null;
    }

    public IEnumerator DoProcessMidAirState_Forces()
    {
        sliderLockOut = true;   // Ensure that we cannot manipulate physics attributes after launch

        Debug.Log("Launch Cannon Ball and Observe Forces while in air");

        if (failLaunch)
        {
            popup.Show();
            yield break;
        }

        HideGizmoAndWidgets();
        distanceGizmo.Show();

        CanProcessNextStep = false;
        midAirStep = MidAirStep.Launching;

        yield return StartCoroutine(DoLerpCameraToTransform(catapult.step3CamTransform));

        float velocity = Velocity_At_Time_Of_Launch();
        float ratio = ratio_Of_DEPE_Over_DGPE();
        catapult.ThrowBall(catapult.launchVector.up, velocity);

        while (!cannonBall.paused)
        {
            yield return null;

            Transform riseMarker = Utils.CreateMarker(null, 0.1f, Color.red);
            riseMarker.position = cannonBall.transform.position;
            markers.Add(riseMarker.gameObject);

            // Catch when the ball has left max arc height
            if (cannonBall.rigidBody.velocity.y < 0)
            {
                if (!cannonBall.maxHeightReached)
                {
                    cannonBall.maxHeightReached = true;

                    Transform peakMarker = Utils.CreateMarker(null, 0.2f, Color.blue);
                    peakMarker.position = cannonBall.prevPosition;
                    markers.Add(peakMarker.gameObject);

                    cannonBall.PauseInAir();
                    yield return StartCoroutine(DoLerpCameraToTransform(cannonBall.step4CamTransform));
                }
            }
        }

        //yield return new WaitUntil(() => { return !camLerping; });  // wait for camera to complete lerping to Ball mid air position

        midAirStep = MidAirStep.Maxheight;
        weightPoint = cannonBall.LeftWeightPoint;
        resultPoint = catapult.springVector;

        ArrowIndicator maxHeightVelocityArrow = Instantiate(arrowBasicFrontPivotPrefab).GetComponent<ArrowIndicator>();
        maxHeightVelocityArrow.SetArrowTransform(cannonBall.PeakHightVelocityMarker);
        maxHeightVelocityArrow.bobbing = true;
        maxHeightVelocityArrow.Show();
        vectorArrows.Add(maxHeightVelocityArrow);
        maxHeightVelocityArrow.infoWidget.UpdateInfoWidget(null, (float)Math.Round(cannonBall.currentVelocity.magnitude, 2), "Velocity", true);

        gravArrow.SetArrowTransform(cannonBall.CenterWeightPoint);
        gravArrow.Show();

        CanProcessNextStep = true;
        yield return new WaitUntil(() => { return (midAirStep == MidAirStep.SlopeDown); });  // wait for camera to complete lerping to Ball mid air position
        CanProcessNextStep = false;

        HideGizmoAndWidgets();  // Hide all force arrows in preperation for next view step
        distanceGizmo.Show();

        yield return StartCoroutine(DoLerpCameraToTransform(cannonBall.step5CamTransform));

        cannonBall.Resume();

        // Cannonball falling downward
        while (!cannonBall.paused)
        {
            Transform downMarker = Utils.CreateMarker(null, 0.1f, Color.green);
            downMarker.position = cannonBall.transform.position;
            markers.Add(downMarker.gameObject);

            yield return null;

            RaycastHit hitResult;
            if (RaycastFromPos(cannonBall.transform.position, -cannonBall.transform.up, Mathf.Infinity, out hitResult, true))
            {
                float diff = cannonBall.transform.position.y - hitResult.point.y;
                if (diff < DistanceFromGround_At_TimeOFLaunch)
                {
                    cannonBall.PauseInAir();
                }
            }
        }

        yield return StartCoroutine(DoLerpCameraToTransform(cannonBall.step4CamTransform));

        gravArrow.SetArrowTransform(cannonBall.CenterWeightPoint);
        gravArrow.Show();

        ArrowIndicator slopeDownVelocityArrow = Instantiate(arrowBasicBackPivotPrefab).GetComponent<ArrowIndicator>();
        slopeDownVelocityArrow.bobbing = true;
        vectorArrows.Add(slopeDownVelocityArrow);
        OrientTransformToVector(ref cannonBall.ResultPoint_Centered, cannonBall.currentVelocity);
        slopeDownVelocityArrow.SetArrowTransform(cannonBall.ResultPoint_Centered);
        slopeDownVelocityArrow.Show();
        slopeDownVelocityArrow.infoWidget.UpdateInfoWidget(null, (float)Math.Round(cannonBall.currentVelocity.magnitude, 2), "Velocity", true);

        CanProcessNextStep = true;
        yield return new WaitUntil(() => { return (midAirStep == MidAirStep.CompleteArc); });  // wait for camera to complete lerping to Ball mid air position
        CanProcessNextStep = false;

        HideGizmoAndWidgets();
        distanceGizmo.Show();

        yield return StartCoroutine(DoLerpCameraToTransform(cannonBall.step5CamTransform)); // RE focus camera

        cannonBall.Resume();    // resume cannon ball trajectory

        // Draw the rest of the arc markers
        while (!cannonBall.paused && cannonBall.inAir)
        {
            Transform downMarker = Utils.CreateMarker(null, 0.1f, Color.green);
            downMarker.position = cannonBall.transform.position;
            markers.Add(downMarker.gameObject);
            yield return null;
        }

        yield return new WaitUntil(() => { return (cannonBall.rigidBody.velocity == Vector3.zero); });  // wait for camera to complete lerping to Ball mid air position
        CanProcessNextStep = true;
        cannonBall.transform.rotation = Quaternion.identity;
        midAirStep = MidAirStep.Undefined;  // Mid air step is over;

        activeCoroutine = null;
    }

    public IEnumerator DoProcessStoppedState_Forces()
    {
        Debug.Log("Observe Cannon Ball after landing");

        CanProcessNextStep = false;

        yield return StartCoroutine(DoLerpCameraToTransform(cannonBall.step4CamTransform)); // RE focus camera

        CanProcessNextStep = true;
        weightPoint = cannonBall.CenterWeightPoint;

        gravArrow.SetArrowTransform(weightPoint);
        gravArrow.Show();

        OrientTransformToVector(ref cannonBall.ResultPoint_Centered, Vector3.up);
        resultArrow.SetArrowTransform(cannonBall.ResultPoint_Centered);
        resultArrow.Show();

        CalculateForces();

        activeCoroutine = null;
    }
    #endregion

    #region Energy
    public IEnumerator DoProcessPreLaunch_Energy()
    {
        Debug.Log("Observe Energy of Gravity and Elastic Energy before launch");

        energyWidget.AttachToTransform(cannonBall.EnergyWidgetMarker_TopOffset);
        energyWidget.infoWidget.ToggleKineticInfo(false);
        energyWidget.Show();

        energyWidget.infoWidget.UpdateInfoWidget(cannonBall.Gravitational_Potential_Energy(), Kinetic_Energy_At_Launch(), null);

        CanProcessNextStep = false;
        yield return StartCoroutine(DoLerpCameraToTransform(catapult.step1CamTransform));
        CanProcessNextStep = true;

        // Position the Distance Gizmo and Rotate it around the Catapault arm to match the time at launch position of the cannonball
        distanceGizmo.transform.position = cannonBall.transform.position;
        distanceGizmo.transform.RotateAround(catapult.catapultArm.transform.position, -catapult.catapultArm.transform.up, catapult.DEFAULT_LAUNCH_ANGLE);
        distanceGizmo.transform.rotation = Quaternion.identity;
        DistanceFromGround_At_TimeOFLaunch = distanceGizmo.transform.position.y - terrain.transform.position.y; // update the distance of the cannonball from the ground at time of launch
        distanceGizmo.Show();

        activeCoroutine = null;
    }

    public IEnumerator DoProcessTimeOfLaunch_Energy()
    {
        CanProcessNextStep = false;

        yield return StartCoroutine(DoLerpCameraToTransform(catapult.step2CamTransform));

        Debug.Log("Observe Potential Energy of Gravity as the Cannon Ball is raised for Launch");

        catapult.rope.gameObject.SetActive(false);

        catapult.launchAngle = catapult.DEFAULT_LAUNCH_ANGLE;

        energyWidget.AttachToTransform(cannonBall.EnergyWidgetMarker_TopOffset);
        energyWidget.Show();

        yield return null;

        catapult.throwCalled = true;

        failLaunch = false;
        float ratio = ratio_Of_DEPE_Over_DGPE();
        if (ratio <= 1f)
        {
            catapult.launchAngle *= ratio;
            failLaunch = true;
        }

        while (catapult.throwCalled)
        {
            yield return null;

            // Keep positionig the Energy Info Widget with respect to the CannonBall as the cannonball is raised
            energyWidget.AttachToTransform(cannonBall.EnergyWidgetMarker_TopOffset);

            // Update the increase of Gravitational Potential Energy of Cannon Ball as the Arm Raises the Ball
            energyWidget.UpdateGravEnergy(cannonBall.Gravitational_Potential_Energy());
        }

        CanProcessNextStep = true;

        activeCoroutine = null;
    }

    public IEnumerator DoProcessMidAirState_Energy()
    {
        Debug.Log("Show transfer of Elastic Potential Energy to Kinetic Energy that Launches the Cannon Ball and Observe Forces while in air");

        if (failLaunch)
        {
            popup.Show();
            yield break;
        }

        CanProcessNextStep = false;
        sliderLockOut = true;   // Lockout physics config sliders

        midAirStep = MidAirStep.Launching;

        energyWidget.Show();

        // Show Kinetic Energy Info
        float currentKineticEnergy = 0;
        energyWidget.UpdateKineticEnergy(currentKineticEnergy);

        // Show Draining of Elastic Potential Energy To Kinetic Energy (Kinetic Energy will equal the Elastic Potential Energy)
        float kineticEnergy = Kinetic_Energy_At_Launch();
        float elasticEnergy = Kinetic_Energy_At_Launch();

        const float drainSpeed = 0.05f;

        // Animate the depletion of Elastic Potential Energy and Gain of Kinetic Energy prior to Launch
        while (!EEMath.Approximately(elasticEnergy, 0, 0.001f) || !EEMath.Approximately(currentKineticEnergy, kineticEnergy, 0.001f))
        {
            yield return null;

            elasticEnergy = Mathf.Lerp(elasticEnergy, 0, drainSpeed);
            energyWidget.UpdateElasticEnergy(elasticEnergy);

            currentKineticEnergy = Mathf.Lerp(currentKineticEnergy, kineticEnergy, drainSpeed);
            energyWidget.UpdateKineticEnergy(currentKineticEnergy);
        }

        yield return StartCoroutine(DoLerpCameraToTransform(catapult.step3CamTransform));

        float velocity = Velocity_At_Time_Of_Launch();
        float ratio = ratio_Of_DEPE_Over_DGPE();
        catapult.ThrowBall(catapult.launchVector.up, velocity);

        while (!cannonBall.paused)
        {
            yield return null;

            Transform riseMarker = Utils.CreateMarker(null, 0.1f, Color.red);
            riseMarker.position = cannonBall.transform.position;
            markers.Add(riseMarker.gameObject);

            // Catch when the ball has left max arc height
            if (cannonBall.rigidBody.velocity.y < 0)
            {
                if (!cannonBall.maxHeightReached)
                {
                    cannonBall.maxHeightReached = true;

                    Transform peakMarker = Utils.CreateMarker(null, 0.2f, Color.blue);
                    peakMarker.position = cannonBall.prevPosition;
                    markers.Add(peakMarker.gameObject);

                    cannonBall.PauseInAir();
                    yield return StartCoroutine(DoLerpCameraToTransform(cannonBall.step4CamTransform));
                }
            }
        }
               
        midAirStep = MidAirStep.Maxheight;
        weightPoint = cannonBall.LeftWeightPoint;
        resultPoint = catapult.springVector;

        ArrowIndicator maxHeightVelocityArrow = Instantiate(arrowBasicFrontPivotPrefab).GetComponent<ArrowIndicator>();
        maxHeightVelocityArrow.SetArrowTransform(cannonBall.PeakHightVelocityMarker);
        maxHeightVelocityArrow.bobbing = true;
        maxHeightVelocityArrow.Show();
        vectorArrows.Add(maxHeightVelocityArrow);
        maxHeightVelocityArrow.infoWidget.UpdateInfoWidget(null, (float)Math.Round(cannonBall.currentVelocity.magnitude, 2), "Velocity", true);

        energyWidget.AttachToTransform(cannonBall.EnergyWidgetMarker_LeftOffset);
        energyWidget.infoWidget.ToggleElasticInfo(false);
        energyWidget.infoWidget.ToggleKineticInfo(false);
        energyWidget.UpdateGravEnergy(cannonBall.Gravitational_Potential_Energy());
             
        CanProcessNextStep = true;
        yield return new WaitUntil(() => { return (midAirStep == MidAirStep.SlopeDown); });  // wait for camera to complete lerping to Ball mid air position
        CanProcessNextStep = false;

        HideGizmoAndWidgets();  // Hide all force arrows in preperation for next view step
        distanceGizmo.Show();

        yield return StartCoroutine(DoLerpCameraToTransform(cannonBall.step5CamTransform));

        cannonBall.Resume();

        // Cannonball falling downward
        while (!cannonBall.paused)
        {
            Transform downMarker = Utils.CreateMarker(null, 0.1f, Color.green);
            downMarker.position = cannonBall.transform.position;
            markers.Add(downMarker.gameObject);

            yield return null;

            RaycastHit hitResult;
            if (RaycastFromPos(cannonBall.transform.position, -cannonBall.transform.up, Mathf.Infinity, out hitResult, true))
            {
                float diff = cannonBall.transform.position.y - hitResult.point.y;
                if (diff < DistanceFromGround_At_TimeOFLaunch)
                {
                    cannonBall.PauseInAir();
                }
            }
        }

        yield return StartCoroutine(DoLerpCameraToTransform(cannonBall.step4CamTransform));

        ArrowIndicator slopeDownVelocityArrow = Instantiate(arrowBasicBackPivotPrefab).GetComponent<ArrowIndicator>();
        slopeDownVelocityArrow.bobbing = true;
        vectorArrows.Add(slopeDownVelocityArrow);
        OrientTransformToVector(ref cannonBall.ResultPoint_Centered, cannonBall.currentVelocity);
        slopeDownVelocityArrow.SetArrowTransform(cannonBall.ResultPoint_Centered);
        slopeDownVelocityArrow.Show();
        slopeDownVelocityArrow.infoWidget.UpdateInfoWidget(null, (float)Math.Round(cannonBall.currentVelocity.magnitude, 2), "Velocity", true);

        energyWidget.AttachToTransform(cannonBall.EnergyWidgetMarker_RightOffset);
        energyWidget.infoWidget.ToggleElasticInfo(false);
        energyWidget.UpdateGravEnergy(cannonBall.Gravitational_Potential_Energy());
        energyWidget.Show();

        CanProcessNextStep = true;
        yield return new WaitUntil(() => { return (midAirStep == MidAirStep.CompleteArc); });  // wait for camera to complete lerping to Ball mid air position
        CanProcessNextStep = false;

        HideGizmoAndWidgets();
        distanceGizmo.Show();

        yield return StartCoroutine(DoLerpCameraToTransform(cannonBall.step5CamTransform)); // RE focus camera

        cannonBall.Resume();    // resume cannon ball trajectory

        // Draw the rest of the arc markers
        while (!cannonBall.paused && cannonBall.inAir)
        {
            Transform downMarker = Utils.CreateMarker(null, 0.1f, Color.green);
            downMarker.position = cannonBall.transform.position;
            markers.Add(downMarker.gameObject);
            yield return null;
        }

        cannonBall.transform.rotation = Quaternion.identity;
        midAirStep = MidAirStep.Undefined;  // Mid air step is over;

        // Freeze the ball's movement and rotation as soon as it makes contact with the ground;
        cannonBall.transform.rotation = Quaternion.identity;
        cannonBall.rigidBody.constraints = RigidbodyConstraints.FreezeAll;

        // Cheat to make Cannonball touch ground
        cannonBall.transform.position = new Vector3(cannonBall.transform.position.x, 0.425f, cannonBall.transform.position.z);
        yield return StartCoroutine(DoProcessStoppedState_Energy());

        activeCoroutine = null;
    }

    public IEnumerator DoProcessStoppedState_Energy()
    {
        Debug.Log("Observe Cannon Ball at time of landing");

        CanProcessNextStep = false;

        yield return StartCoroutine(DoLerpCameraToTransform(cannonBall.step4CamTransform)); // RE focus camera

        CanProcessNextStep = true;

        energyWidget.AttachToTransform(cannonBall.EnergyWidgetMarker_TopOffset);
        energyWidget.Show();

        energyWidget.UpdateGravEnergy(0);   // Cheat (make this show real grav potential energy)

        activeCoroutine = null;
    }

    #endregion

    #region FreePlay
    public IEnumerator DoProcessFreePlayLaunch()
    {
        CanProcessNextStep = false;

        yield return new WaitWhile(() => { return catapult.throwCalled; });

        float velocity = Velocity_At_Time_Of_Launch();
        float ratio = ratio_Of_DEPE_Over_DGPE();
        catapult.ThrowBall(catapult.launchVector.up, velocity);

        activeCoroutine = null;
    }
    #endregion

    private bool RaycastFromPos(Vector3 pos, Vector3 dir, float distance, out RaycastHit hitResult, bool debug = false)
    {
        if (debug)
        {
            Debug.DrawLine(pos, pos + (dir * distance), Color.red, 10);
        }
        Ray ray = new Ray(pos, dir);
        return Physics.Raycast(ray, out hitResult, distance, terrainLayer);
    }   

    public void UpdateCannonBallMass(float _mass)
    {
        if (!cannonBall.Mass.Equals(_mass))
        {
            cannonBall.Mass = _mass;
        }
    }

    public void UpdateSpringForce(float _springK)
    {
        if (!springK.Equals(_springK))
        {
            SpringK = _springK;
        }
    }

    // Show animation related to change of mass, also update opposing and like forces
    public void ShowMassChange()
    {
        switch(currentPhysicsMode)
        {
            case PhysicsMode.Forces:
                gravArrow.ChangeArrowWeight(cannonBall.WeightForce);
                CalculateForces();
                CalculateDistance();
                break;

            case PhysicsMode.FreePlayTarget:
            case PhysicsMode.FreePlayBox:
            case PhysicsMode.BasketballChallenge:
                gravArrow.ChangeArrowWeight(cannonBall.WeightForce);
                CalculateForces();
                break;

            case PhysicsMode.Energy:
                energyWidget.UpdateGravEnergy(cannonBall.Gravitational_Potential_Energy());
                CalculateDistance();
                break;
        }
    }

    public void ShowSpringChange()
    {
        switch (currentPhysicsMode)
        {
            case PhysicsMode.Forces:
                springArrow.ChangeArrowWeight(SpringForce);
                CalculateForces();
                CalculateDistance();
                break;

            case PhysicsMode.FreePlayTarget:
            case PhysicsMode.FreePlayBox:
            case PhysicsMode.BasketballChallenge:
                springArrow.ChangeArrowWeight(SpringForce);
                CalculateForces();
                break;

            case PhysicsMode.Energy:
                energyWidget.UpdateElasticEnergy(Kinetic_Energy_At_Launch());
                CalculateDistance();
                break;
        }
    }

    private void CalculateResultVector()
    {
        // Scale the Spring and Weight Unit Vectors by their force
        Vector3 springVector = catapult.springVector.transform.up * SpringForce;
        Vector3 weightVector = cannonBall.CenterWeightPoint.transform.up * cannonBall.WeightForce;

        // Combine both scaled vectors to get the scaled Resultant Force
        Vector3 resultVector = springVector + weightVector;

        // Orient the Result Gizmo Arrow to align with the resultant vector above (visual)
        catapult.resultVector.up = resultVector.normalized;

        // Update resultant force (magnitude of the result vector calculated above)
        ResultForce = resultVector.magnitude;
    }


    // calculate effect on all forces
    public void CalculateForces()
    {
        switch (currentPhysicsMode)
        {
            case PhysicsMode.Forces:
            case PhysicsMode.Energy:

                switch (currentStep)
                {
                    case LearningStep.PreLaunch:
                        // Update tensions force and clamp between 0 and infinity
                        TensionForce = Mathf.Clamp((SpringForce - cannonBall.WeightForce), 0, Mathf.Infinity);
                        break;

                    case LearningStep.TimeOfLaunch:
                        // Update resultant force and clamp between 0 and infinity
                        ResultForce = Mathf.Clamp((SpringForce - cannonBall.WeightForce), 0, Mathf.Infinity);
                        CalculateResultVector();
                        resultArrow.SetArrowTransform(catapult.resultVector);
                        break;

                    case LearningStep.Stopped:
                        // Update resultant force and clamp between 0 and infinity
                        ResultForce = cannonBall.WeightForce;
                        break;
                }

                break;

            case PhysicsMode.FreePlayTarget: 
            case PhysicsMode.FreePlayBox:
            case PhysicsMode.BasketballChallenge:
                CalculateDeltaTime();
                break;
        }
    }

    // Orient the local up vector of a transform to align with a specific Vector
    private void OrientTransformToVector(ref Transform _transform, Vector3 vec)
    {
        _transform.up = vec.normalized;
    }

    private IEnumerator DoLerpCameraToTransform(Transform transformPoint)
    {
        bool posReached = false;
        bool rotReached = false;

        while (!posReached || !rotReached)
        {
            yield return null;

            mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, transformPoint.position, camLerpSpeed);
            mainCam.transform.rotation = Quaternion.Slerp(mainCam.transform.rotation, transformPoint.rotation, camLerpSpeed);

            posReached = EEMath.Approximately(mainCam.transform.position, transformPoint.position, 0.01f);
            rotReached = EEMath.Approximately(mainCam.transform.rotation.eulerAngles, transformPoint.transform.rotation.eulerAngles, 0.01f);
        }
    }

   
}
