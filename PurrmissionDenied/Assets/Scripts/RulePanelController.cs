using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RulePanelController : Subscriber
{
    public float scrollSpeed = 5000000f;
    public RectTransform content;

    [Header("UI")]
    public Text RulesText;
    public GameObject SettingsPanel;

    [Header("Audio")]
    public Audio Scroll;

    [Header("Events")]
    public AudioGameEvent AudioBus;

    [Header("Event Listeners")]
    public StringGameEvent RuleText;
    public BoolGameEvent RulePanelActive;

    private bool audioUpdate;
    private bool scrollable;

    public override void Subscribe()
    {
        RuleText?.Subscribe(OnRuleText);
        RulePanelActive?.Subscribe(OnRulePanelActive);
        audioUpdate = true;
        scrollable = false;
        OnRuleText("");
    }

    public void Update()
    {
        if (SettingsPanel.activeSelf)
        {
            scrollable = false;
        }
        else
        {
            scrollable = true;
        }

        if ((Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) && scrollable)
        {
            content.anchoredPosition -= new Vector2(0, scrollSpeed);
            if (audioUpdate)
            {
                AudioBus?.Emit(Scroll);
                StartCoroutine(DelayAudio(0.01f));
            }
        }
        else if ((Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) && scrollable)
        {
            content.anchoredPosition += new Vector2(0, scrollSpeed);
            if (audioUpdate)
            {
                AudioBus?.Emit(Scroll);
                StartCoroutine(DelayAudio(0.01f));
            }
        }
    }

    private void OnRulePanelActive(bool active)
    {
        scrollable = active;
    }

    public void OnRuleText(string text)
    {
        if (text != "")
        {
            RulesText.text = text;
        }
        else
        {
            RulesText.text = "No conditions available.";
        }
    }

    IEnumerator DelayAudio(float time)
    {
        audioUpdate = false;
        yield return new WaitForSeconds(time);
        audioUpdate = true;
    }
}
