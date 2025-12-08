using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TitleSettingsButtonController : Subscriber
{
    [Header("Buttons")]
    public Button SettingsOpenButton;
    public Button SettingsCloseButton;

    [Header("Panel")]
    public GameObject SettingsPanel;

    [Header("Audio")]
    public Audio TabSwitch;

    [Header("Event Listeners")]
    public AudioGameEvent AudioBus;
    private bool canUpdate = true;

    void Start()
    {
        SettingsCloseButton.onClick.AddListener(OnSettingsClosed);
        SettingsOpenButton.onClick.AddListener(ToggleSettings);

        SetupAnimatedSettingsButton(SettingsOpenButton);
        SetupHoverOnlyButton(SettingsCloseButton);
    }

    void OnSettingsOpen()
    {
        SettingsPanel.SetActive(true);
        AudioBus?.Emit(TabSwitch);
    }

    void OnSettingsClosed()
    {
        SettingsPanel.SetActive(false);
        AudioBus?.Emit(TabSwitch);
        CursorManager.Instance.Default();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            if (canUpdate && !SettingsPanel.activeSelf)
            {
                OnSettingsOpen();
                StartCoroutine(DelayAction(0.5f));
            }
            else if (canUpdate && SettingsPanel.activeSelf)
            {
                OnSettingsClosed();
                StartCoroutine(DelayAction(0.5f));
            }
        }
    }

    IEnumerator DelayAction(float time)
    {
        canUpdate = false;
        yield return new WaitForSeconds(time);
        canUpdate = true;
    }

    public void OnHover(PointerEventData _)
    {
        CursorManager.Instance.Clickable();
    }

    public void OnPointerExit(PointerEventData _)
    {
        CursorManager.Instance.Default();
    }

    private void OnButtonClick(Animator animator)
    {
        animator.ResetTrigger("PlayReset");
        animator.ResetTrigger("PlayReleased");
        animator.SetTrigger("PlayPressed");
    }

    private void OnButtonReset(Animator animator)
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("ButtonPressed"))
        {
            animator.SetTrigger("PlayReset");
        }
        CursorManager.Instance.Default();
    }

    private void SetupAnimatedSettingsButton(Button button)
    {
        button.TryGetComponent<EventTrigger>(out EventTrigger trigger);
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        trigger.triggers.Add(createEntry(EventTriggerType.PointerEnter, OnHover));
        trigger.triggers.Add(createEntry(EventTriggerType.PointerExit, OnPointerExit));
    }

    private void ToggleSettings()
    {
        if (SettingsPanel.activeSelf)
        {
            OnSettingsClosed();
        }
        else
        {
            OnSettingsOpen();
        }
    }

    private void SetupHoverOnlyButton(Button button)
    {
        button.TryGetComponent<EventTrigger>(out EventTrigger trigger);
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        trigger.triggers.Add(createEntry(EventTriggerType.PointerEnter, OnHover));
        trigger.triggers.Add(createEntry(EventTriggerType.PointerExit, OnPointerExit));
    }

    private EventTrigger.Entry createEntry(EventTriggerType kind, Action<PointerEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.callback.AddListener((data) => callback((PointerEventData)data));
        entry.eventID = kind;
        return entry;
    }
}
