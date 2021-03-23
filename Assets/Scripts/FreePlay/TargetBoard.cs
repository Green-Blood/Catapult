using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetBoard : MonoBehaviour
{
    [SerializeField] private Canvas scoreCanvas;
    [SerializeField] private ParticleSystem blackParticles;
    [SerializeField] private ParticleSystem whiteParticles;
    [SerializeField] private ParticleSystem blueParticles;
    [SerializeField] private ParticleSystem redParticles;
    [SerializeField] private ParticleSystem yellowParticles;
    public int totalScore;

    public bool hit = false;

    private void Awake()
    {
        scoreCanvas.worldCamera = GameManager.GetInstance().mainCam;
        Reset();
    }

    private void Reset()
    {
        totalScore = 0;
        hit = false;
    }

    public void PlayBlackHit()
    {
        blackParticles.Stop();
        blackParticles.Play();
        totalScore += 5;
    }

    public void PlayWhiteHit()
    {
        whiteParticles.Stop();
        whiteParticles.Play(); 
        totalScore += 15;
    }

    public void PlayBlueHit()
    {
        blueParticles.Stop();
        blueParticles.Play();
        totalScore += 25;
    }

    public void PlayRedHit()
    {
        redParticles.Stop();
        redParticles.Play();
        totalScore += 50;
    }

    public void PlayYellowHit()
    {
        yellowParticles.Stop();
        yellowParticles.Play();
        totalScore += 100;
    }
}
