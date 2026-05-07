using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FalseBehaviourController : MonoBehaviour
{
    public AudioSource wrongBehaviourAudio;
    private void OnTriggerEnter(Collider other)
    {
        if ((other.CompareTag("LeftHand") || other.CompareTag("RightHand")) && ScenarioManager.Instance.currentState == ScenarioManager.ScenarioState.InProgress)
        {
            wrongBehaviourAudio.Play();
        }
    }
}
