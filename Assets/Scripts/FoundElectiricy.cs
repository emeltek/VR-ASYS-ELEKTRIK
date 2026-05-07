using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoundElectiricy : MonoBehaviour
{
    public AudioSource foundElectircyAudio;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && ScenarioManager.Instance.currentState == ScenarioManager.ScenarioState.InProgress)
        {
            foundElectircyAudio.Play();
        }
    }
}
