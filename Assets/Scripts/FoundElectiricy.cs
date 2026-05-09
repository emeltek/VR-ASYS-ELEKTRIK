using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoundElectiricy : MonoBehaviour
{
    public AudioSource foundElectircyAudio;
    private bool isVoicePlaying = false;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && ScenarioManager.Instance.currentState == ScenarioManager.ScenarioState.InProgress && isVoicePlaying == false)
        {
            isVoicePlaying = true;
            foundElectircyAudio.Play();
        }
    }
}
