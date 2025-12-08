using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonAudio : MonoBehaviour
{
    public Audio Hover;
    public Audio ClickStart;
    public Audio ClickRelease;

    [Header("Events")]
    public AudioGameEvent AudioBus;

    private Button SelfButton;

    void Awake()
    {
        gameObject.TryGetComponent(out Button btn);
        if (btn != null)
        {
            SelfButton = btn;
        }
        gameObject.TryGetComponent(out EventTrigger trigger);
        if (trigger == null)
        {
            trigger = gameObject.AddComponent<EventTrigger>();
        }
        if (Hover.clip != null)
        {
            trigger.triggers.Add(createEntry(EventTriggerType.PointerEnter, OnHover));
        }
        if (ClickStart.clip != null)
        {
            trigger.triggers.Add(createEntry(EventTriggerType.PointerDown, OnClickStart));
        }
        if (ClickRelease.clip != null)
        {
            trigger.triggers.Add(createEntry(EventTriggerType.PointerUp, OnClickRelease));
        }
        trigger.triggers.Add(createEntry(EventTriggerType.PointerExit, OnPointerExit));
    }

    private void OnHover(PointerEventData _)
    {
        AudioBus?.Emit(Hover);
        CursorManager.Instance.Clickable();
    }

    private void OnPointerExit(PointerEventData _)
    {
        CursorManager.Instance.Default();
    }

    private void OnClickStart(PointerEventData _)
    {
        if (SelfButton != null)
        {
            if (SelfButton.interactable)
                AudioBus?.Emit(ClickRelease);
        }
        else
        {
            AudioBus?.Emit(ClickRelease);
        }
    }

    private void OnClickRelease(PointerEventData _)
    {
        if (SelfButton != null)
        {
            if (SelfButton.interactable)
                AudioBus?.Emit(ClickStart);
        }
        else
        {
            AudioBus?.Emit(ClickStart);
        }
    }

    private EventTrigger.Entry createEntry(EventTriggerType kind, Action<PointerEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.callback.AddListener((data) => callback((PointerEventData)data));
        entry.eventID = kind;
        return entry;
    }
}
