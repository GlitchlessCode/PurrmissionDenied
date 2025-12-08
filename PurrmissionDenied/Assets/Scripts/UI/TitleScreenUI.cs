using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScreenUI : MonoBehaviour
{
    private float time;

    [SerializeField]
    public float radius = 15;
    public float loopTime = 4;

    [Header("Panels")]
    public GameObject PowerOff;
    public GameObject Title;

    [Header("Buttons")]
    public Button StartButton;

    [Header("Audio")]
    public Audio ShutdownAudio;

    [Header("Events")]
    public AudioGameEvent AudioBus;

    // Start is called before the first frame update
    void Start()
    {
        PowerOff.SetActive(false);
        StartButton.onClick.AddListener(OnStartButton);
    }

    private void TitleAnimation()
    {
        double angle = time % loopTime / loopTime * 2 * Math.PI;
        double x = Math.Cos(angle) * radius;
        double y = Math.Sin(angle) * radius;
        Title.transform.localPosition = new Vector2((float)x, (float)y);
    }

    private void OnStartButton()
    {
        StartCoroutine(Shutdown());
    }

    private IEnumerator Shutdown()
    {
        yield return new WaitForSeconds(0.15f);
        if (ShutdownAudio.clip != null)
        {
            AudioBus?.Emit(ShutdownAudio);
        }
        PowerOff.SetActive(true);
        PowerOff.GetComponent<Animator>().SetTrigger("PlayPowerOff");
    }

    private void Update()
    {
        if (StartButton != null && StartButton.gameObject.activeSelf && StartButton.interactable)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                // Visually simulate the button press
                StartCoroutine(SimulateButtonPress());
            }
        }

        TitleAnimation();
        time += 1 * Time.deltaTime;
        if (time >= 4)
        {
            time = 0;
        }
    }

    private IEnumerator SimulateButtonPress()
    {
        var btn = StartButton;
        if (btn == null)
            yield break;

        // Ensure there’s an EventSystem (your scene should already have one;
        // but this makes it robust if it’s missing).
        if (EventSystem.current == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            // Optionally: yield return null; // give it a frame to initialize
        }

        // Create a fake left-click event
        var ped = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            clickCount = 1,
        };

        // Visually go to Pressed state
        ExecuteEvents.Execute(btn.gameObject, ped, ExecuteEvents.pointerDownHandler);

        // Brief pressed time so the sprite/transition is visible
        yield return new WaitForSeconds(0.1f);

        // Release and invoke the click
        ExecuteEvents.Execute(btn.gameObject, ped, ExecuteEvents.pointerUpHandler);
        btn.onClick.Invoke();
    }
}
