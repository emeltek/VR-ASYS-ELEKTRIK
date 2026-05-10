using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FalseBehaviourController : MonoBehaviour
{
    public AudioSource wrongBehaviourAudio;
    public CalculateScore calculateScore;
    public int negativeScore = 15;
    private string sceneName;

    private void Start()
    {
        sceneName = SceneManager.GetActiveScene().name;
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((other.CompareTag("LeftHand") || other.CompareTag("RightHand")) && ScenarioManager.Instance.currentState == ScenarioManager.ScenarioState.InProgress)
        {
            if (sceneName== "VR-ASYS-ELEKTIRIK")
            {
                wrongBehaviourAudio.Play();

            }
            calculateScore.ScoreManagement(negativeScore);
        }
    }
}
