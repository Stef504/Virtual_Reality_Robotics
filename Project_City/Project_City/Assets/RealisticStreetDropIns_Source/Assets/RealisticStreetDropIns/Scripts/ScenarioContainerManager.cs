using System.Collections.Generic;
using UnityEngine;

public class ScenarioContainerManager : MonoBehaviour
{
    [Header("Scenario Parent Objects")]
    [Tooltip("Drag your scenario parent GameObjects from the Hierarchy into this list.")]
    public List<GameObject> scenarios = new List<GameObject>();

    [Header("Settings")]
    [Tooltip("Which scenario to activate automatically on Play mode.")]
    public int defaultScenarioIndex = 0;

    void Start()
    {
        if (scenarios.Count > 0)
        {
            ActivateScenario(defaultScenarioIndex);
        }
    }

    /// <summary>
    /// Activates the chosen scenario index and disables all others.
    /// </summary>
    public void ActivateScenario(int index)
    {
        if (index < 0 || index >= scenarios.Count)
        {
            Debug.LogWarning($"[SCENARIO MANAGER] Index {index} is out of bounds!");
            return;
        }

        for (int i = 0; i < scenarios.Count; i++)
        {
            if (scenarios[i] == null) continue;

            bool shouldBeActive = (i == index);
            scenarios[i].SetActive(shouldBeActive);
        }

        Debug.Log($"[SCENARIO MANAGER] Active Scenario: '{scenarios[index].name}'");
    }
}