using System;
using UnityEditor;
using UnityEngine;

public class EditorPhysicsConfig : EditorWindow
{
    public static EditorPhysicsConfig instance;
    public PhysicsMode physicsMode;
    public PhysicsMode PhysicsMode
    {
        get
        {
            return physicsMode;
        }
        set
        {
            if (physicsMode != value)
            {
                OnEditorPhysicsModeChanged(value);
            }
        }
    }

    public static float CannonBallMass;
    public static float SpringK;

    const float massSliderMin = 50;
    const float massSliderMax = 100;

    public static int LearningStep = 0;

    // Make Window accessible from Window Panel
    [MenuItem("Window/Physics Config Window")]
    public static void ShowWindow()
    {
        GetWindow(typeof(EditorPhysicsConfig));
    }

    private void Awake()
    {
        instance = this;
        CannonBallMass = GameManager.DEFAULT_CANNON_BALL_MASS;

         // Forces the Editor to the aspect ratio that is best used to view the learning module
        GameViewUtils.SwitchToSize(new Vector2(16, 9), GameViewUtils.GameViewSizeType.AspectRatio, "Unity Physics Aspect");
    }

    const float vertStart = 140;
    private void OnGUI()
    {
#if UNITY_EDITOR

        Rect enumPopRect = new Rect(5, vertStart, 270, 30);
        GUILayoutOption[] enumPopLayout = { GUILayout.Width(100), GUILayout.Height(30) };
        PhysicsMode = (PhysicsMode)EditorGUI.EnumPopup(enumPopRect, "Physics Lesson", PhysicsMode);

        if (GUI.Button(new Rect(10, 10, 80, 50), "Lesson 1\n[Forces]"))
        {
            StartForcesLesson();
        }

        if (GUI.Button(new Rect(110, 10, 80, 50), "Lesson 2\n[Energy]"))
        {
            StartEnergyLesson();
        }

        if (GUI.Button(new Rect(10, 70, 80, 50), "Free Play\n[Target]"))
        {
            StartFreePlayTarget();
        }

        if (GUI.Button(new Rect(110, 70, 80, 50), "Free Play\n[Boxes]"))
        {
            StartFreePlayBox();
        }

        if (GUI.Button(new Rect(210, 70, 80, 50), "Basketball\n[Challenge]"))
        {
            StartBasketballChallenge();
        }

        if (EditorApplication.isPlaying && GameManager.HasInstance())
        {
            GameManager gm = GameManager.GetInstance();

            GUILayout.Space(vertStart + 40f);

            GUI.enabled = !gm.sliderLockOut && gm.CanProcessNextStep;

            GUILayoutOption[] massSliderLayout = { GUILayout.Width(350), GUILayout.Height(18) };
            CannonBallMass = EditorGUILayout.Slider("Cannonball Mass", CannonBallMass, 1f, 20, massSliderLayout);

            GUILayoutOption[] springSliderLayout = { GUILayout.Width(350), GUILayout.Height(18) };
            SpringK = EditorGUILayout.Slider("Spring Force", SpringK, 250, 15000, springSliderLayout);

            GUI.enabled = IsFreePlay() || GameManager.GetInstance().CanProcessNextStep;

            if (GUI.Button(new Rect(60, 240, 150, 50), GetExecutionButtonText(gm)))
            {
                Action callBack = GetExecutionButtonCallBack(gm);
                
                if(callBack != null)
                {
                    callBack();
                }
            }           
        }
        else
        {
            if (GUI.Button(new Rect(60, 240, 150, 50), "Start Playing"))
            {
                StartPlayEditor();
            }
        }

#endif
    
    }

    private void StartPlayEditor()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = true;
#endif       
    }

    private string GetExecutionButtonText(GameManager gm)
    {
        switch (PhysicsMode)
        {
            case PhysicsMode.Forces:
            case PhysicsMode.Energy:
                switch(gm.currentStep)
                {
                    case GameManager.LearningStep.MidAir:
                        return "Continue";

                    case GameManager.LearningStep.Stopped:
                        return "Lesson Complete\n(Restart Lesson)";

                    default:
                        return "Proceed to Step " + (((int)GameManager.GetInstance().currentStep) + 2);
                }               

            case PhysicsMode.FreePlayTarget:
            case PhysicsMode.FreePlayBox:
            case PhysicsMode.BasketballChallenge:
                if (gm.catapult.launched)
                {
                    return "Restart";
                }
                else
                {
                    return "Launch Catapault";
                }

            default:
                return "";

        }    
    }

    // Returns the correct callback for the execution button based on state of game
    private Action GetExecutionButtonCallBack(GameManager gm)
    {
        switch (PhysicsMode)
        {
            case PhysicsMode.Forces:
            case PhysicsMode.Energy:
                if (gm.currentStep == GameManager.LearningStep.Stopped)
                {
                   return StartLesson;
                }
                else
                {
                    if (gm.midAirStep == GameManager.MidAirStep.Undefined)
                    {
                        return ProceedToNextStep;
                    }
                    else
                    {
                        return ProcessNextMidAirStep;
                    }
                }

            case PhysicsMode.FreePlayTarget:
            case PhysicsMode.FreePlayBox:
            case PhysicsMode.BasketballChallenge:
                if (gm.catapult.launched)
                {
                    return ResetFreePlayEnvironment;
                }
                else
                {
                    return LaunchCatapult;
                }

            default:
                return null;
        }
    }

    private bool IsFreePlay()
    {
        return (GameManager.GetInstance().CurrentPhysicsMode == PhysicsMode.FreePlayTarget
             || GameManager.GetInstance().CurrentPhysicsMode == PhysicsMode.FreePlayBox
             || GameManager.GetInstance().CurrentPhysicsMode == PhysicsMode.BasketballChallenge);
    }

    private void Update()
    {
        GameManager gm = GameManager.GetInstance();
        if (gm != null)
        {
            // Attach Focus Callback
            if (gm.OnEditorFocus == null)
            {
                gm.OnEditorFocus = OnEditorFocus;
            }

            if (!gm.lessonStarted)
            {
                StartLesson();
            }

            if (IsFreePlay() || gm.currentStep != GameManager.LearningStep.NotStarted)
            {
                gm.UpdateCannonBallMass(CannonBallMass);
                gm.UpdateSpringForce(SpringK);
            }
        }
    }

    private void OnEditorFocus()
    {
        Focus();
    }

    // Starts a physics lesson that is chosen
    private void StartLesson()
    {
        switch (PhysicsMode)
        {
            case PhysicsMode.Forces:
                StartForcesLesson();
                break;

            case PhysicsMode.Energy:
                StartEnergyLesson();
                break;

            case PhysicsMode.FreePlayTarget:
                StartFreePlayTarget();
                break;

            case PhysicsMode.FreePlayBox:
                StartFreePlayBox();
                break;

            case PhysicsMode.BasketballChallenge:
                StartBasketballChallenge();
                break;
        }
    }

    private void OnEditorPhysicsModeChanged(PhysicsMode _physicsMode)
    {
        physicsMode = _physicsMode;

        if (GameManager.GetInstance() != null)
        {
            StartLesson();
        }
    }

    private void ResetFreePlayEnvironment()
    {
        GameManager.GetInstance().Reset();

        if (GameManager.GetInstance().CurrentPhysicsMode == PhysicsMode.FreePlayTarget)
        {
            ResetFreePlayTarget();
        }
        else if(GameManager.GetInstance().CurrentPhysicsMode == PhysicsMode.FreePlayBox)
        {
            ResetFreePlayBoxes();
        }
    }

    private void LaunchCatapult()
    {
        GameManager.GetInstance().LaunchFreePlayCannonBall();
    }

    private void StartForcesLesson()
    {
        LearningStep = 0;
        if (GameManager.GetInstance() != null)
        {
            GameManager.GetInstance().lessonStarted = true;
            GameManager.GetInstance().CurrentPhysicsMode = PhysicsMode = PhysicsMode.Forces;
        }
        else
        {
            Debug.Log("Click [Start Playing] Button on the Physics Config Panel to Start the lesson");
        }
    }

    private void StartEnergyLesson()
    {
        LearningStep = 0;
        if (GameManager.GetInstance() != null)
        {
            GameManager.GetInstance().lessonStarted = true;
            GameManager.GetInstance().CurrentPhysicsMode = PhysicsMode = PhysicsMode.Energy;
        }
        else
        {
            Debug.Log("Click [Start Playing] Button on the Physics Config Panel to Start the lesson");
        }
    }

    private void StartFreePlayTarget()
    {
        LearningStep = 0;
        if (GameManager.GetInstance() != null)
        {
            GameManager.GetInstance().lessonStarted = true;
            GameManager.GetInstance().CurrentPhysicsMode = PhysicsMode = PhysicsMode.FreePlayTarget;
        }
        else
        {
            Debug.Log("Click [Start Playing] Button on the Physics Config Panel to Start the lesson");
        }
    }

    private void StartFreePlayBox()
    {
        LearningStep = 0;
        if (GameManager.GetInstance() != null)
        {
            GameManager.GetInstance().lessonStarted = true;
            GameManager.GetInstance().CurrentPhysicsMode = PhysicsMode = PhysicsMode.FreePlayBox;
        }
        else
        {
            Debug.Log("Click [Start Playing] Button on the Physics Config Panel to Start the lesson");
        }
    }

    private void StartBasketballChallenge()
    {
        LearningStep = 0;
        if (GameManager.GetInstance() != null)
        {
            GameManager.GetInstance().lessonStarted = true;
            GameManager.GetInstance().CurrentPhysicsMode = PhysicsMode = PhysicsMode.BasketballChallenge;
        }
        else
        {
            Debug.Log("Click [Start Playing] Button on the Physics Config Panel to Start the lesson");
        }
    }

    private void ResetFreePlayTarget()
    {
        GameManager.GetInstance().InitializeFreePlayTargetMode();
    }

    private void ResetFreePlayBoxes()
    {
        GameManager.GetInstance().ExecuteFreePlayBoxMode();
    }

    private void ProceedToNextStep()
    {
        GameManager.GetInstance().currentStep = (GameManager.LearningStep)LearningStep;
        GameManager.GetInstance().ExecuteLearningStep();
        LearningStep++;
    }

    private void ProcessNextMidAirStep()
    {
        GameManager.GetInstance().midAirStep += 1;
    }

}
