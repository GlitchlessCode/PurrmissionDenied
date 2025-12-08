using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EODPromptController : Subscriber // same base class style as your other systems
{
    [SerializeField]
    private GameObject arrowPrompt; // your arrow or hint UI

    [SerializeField]
    private GameObject NMAPanel; // your NMAPanel

    [Header("Event Listeners")]
    public UnitGameEvent DayFinished; // assign this in Inspector

    public override void Subscribe()
    {
        DayFinished?.Subscribe(OnDayFinished);
    }

    private void Start()
    {
        if (arrowPrompt)
            arrowPrompt.SetActive(false);
    }

    private void OnDayFinished()
    {
        NMAPanel.GetComponent<Animator>().SetTrigger("NoMoreAppeals");
    }
}
