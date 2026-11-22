using System;
using System.Collections;
using UnityEngine;

public class GameEnemy : MonoBehaviour
{
    [SerializeField] private Freezable freezable;

    private void Update()
    {
        if (freezable && !freezable.IsFreezed)
        {
            // Move towards the players
        }
    }
}