using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour
{
    public TargetBoardUI targetUI;
    public FreePlayBoxesUI boxesUI;
    public BasketBallUI ballUIPanel;
    public PhysicsInfoUIPanel physicsUIPanel;

    public void Reset()
    {
        ballUIPanel.Reset();
        boxesUI.Reset();
        targetUI.Reset();
        physicsUIPanel.Reset();
    }

    public void HideFreeplayUIPanels()
    {
        targetUI.gameObject.SetActive(false);
        boxesUI.gameObject.SetActive(false);
        ballUIPanel.gameObject.SetActive(false);
    }

    public void OnPhysicsModeChanged(PhysicsMode physicsMode)
    {
        Reset();

        HideFreeplayUIPanels();

        switch(physicsMode)
        {
            case PhysicsMode.FreePlayTarget:
                targetUI.gameObject.SetActive(true);
                break;

            case PhysicsMode.FreePlayBox:
                boxesUI.gameObject.SetActive(true);
                break;

            case PhysicsMode.BasketballChallenge:
                ballUIPanel.gameObject.SetActive(true);
                break;
        }
    }
}
