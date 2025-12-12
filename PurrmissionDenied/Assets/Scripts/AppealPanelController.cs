using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AppealPanelController : Subscriber
{
    [Header("UI")]
    public Text NameText;
    public Text DateText;
    public Text BioText;
    public Text AppealText;
    public Button AcceptButton;
    public Button DenyButton;
    public GameObject AppealPanel;
    public GameObject ScrollView;
    public GameObject SettingsPanel;

    [Header("Chat")]
    public Text ChatLogText;
    public ScrollRect ChatScroll;

    [SerializeField]
    private TextMeshProUGUI textComponent;
    public GameObject container;
    public RectTransform content;
    public Transform Panel;
    public Audio Scroll;
    private bool scrollable = false;
    private bool audioUpdate = false;
    public float scrollSpeed = 5f;
    private List<GameObject> containers = new List<GameObject>();
    private List<RectTransform> transforms = new List<RectTransform>();

    [Header("Delay Time")]
    public float DelayTime = 1f;

    private bool canUpdate = false;

    [Header("Event Listeners")]
    public UserEntryGameEvent RefreshUserInfo;
    public BoolGameEvent AppealPanelActive;
    public UnitGameEvent DayFinished;

    [Header("Events")]
    public BoolGameEvent ResolveAppeal;
    public UnitGameEvent RequestUser;
    public AudioGameEvent AudioBus;

    public override void Subscribe()
    {
        RefreshUserInfo?.Subscribe(OnRefreshUserInfo);
        AppealPanelActive?.Subscribe(OnAppealPanelActive);
        DayFinished?.Subscribe(OnDayFinished);
    }

    public override void AfterSubscribe()
    {
        AcceptButton.enabled = false;
        DenyButton.enabled = false;
        scrollable = false;

        SetupButton(AcceptButton);
        SetupButton(DenyButton);
    }

    void OnDayFinished()
    {
        canUpdate = false;
        AcceptButton.gameObject.SetActive(false);
        DenyButton.gameObject.SetActive(false);
        ScrollView.gameObject.SetActive(false);
        scrollable = false;
    }

    void OnEnable()
    {
        RefreshUI(null);
        RequestUser?.Emit();
        canUpdate = true;
        audioUpdate = true;
    }

    void OnAppealPanelActive(bool isActive)
    {
        if (isActive && !SettingsPanel.activeSelf)
        {
            Debug.Log("Appeal active");
            AcceptButton.gameObject.SetActive(true);
            DenyButton.gameObject.SetActive(true);
            scrollable = true;
        }
        else
        {
            Debug.Log("Appeal not active");
            AcceptButton.gameObject.SetActive(false);
            DenyButton.gameObject.SetActive(false);
            scrollable = false;
        }
    }

    void RefreshUI(UserEntry? userOption)
    {
        if (userOption == null)
        {
            NameText.text = "No appeals loaded";
            DateText.text = BioText.text = AppealText.text = "";
            if (ChatLogText)
                ChatLogText.text = "";
            return;
        }

        var user = userOption.Value;
        NameText.text = user.name;
        DateText.text = user.date;
        BioText.text = user.bio;
        AppealText.text = user.appeal_message;

        updateMessages(userOption);
    }

    void updateMessages(UserEntry? user)
    {
        foreach (string msg in user.Value.messages)
        {
            GameObject instantiatedObject = Instantiate(container, Panel);
            textComponent = instantiatedObject.GetComponentInChildren<TextMeshProUGUI>();
            textComponent.text = msg;

            containers.Add(instantiatedObject);

            RectTransform trans = instantiatedObject.GetComponent<RectTransform>();

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(Panel.GetComponent<RectTransform>());
            transforms.Add(trans);
        }

        content.anchoredPosition = new Vector2(0, 0);
    }

    void scroll()
    {
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

    void killChat()
    {
        foreach (GameObject con in containers)
        {
            Destroy(con);
        }
        containers.Clear();
        transforms.Clear();
    }

    void OnRefreshUserInfo(UserEntry user)
    {
        killChat();
        RefreshUI(user);
    }

    public void OnButtonRelease(bool decision)
    {
        Animator animator;
        if (decision)
        {
            animator = AcceptButton.GetComponent<Animator>();
        }
        else
        {
            animator = DenyButton.GetComponent<Animator>();
        }
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("ButtonPressed"))
        {
            ResolveAppeal?.Emit(decision);

            animator.SetTrigger("PlayReleased");
        }
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
            if (Input.GetMouseButton(0))
            {
                animator.SetTrigger("PlayReset");
            }
        }
        CursorManager.Instance.Default();
    }

    public void OnHover(Animator animator)
    {
        animator.SetTrigger("PlayHighlighted");
        CursorManager.Instance.Clickable();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.A))
        {
            if (canUpdate == true && AcceptButton.enabled && AppealPanel.activeSelf)
            {
                AcceptButton.GetComponent<Animator>().SetTrigger("PlayFullPress");
                ResolveAppeal?.Emit(true);
                StartCoroutine(DelayAction(DelayTime));
            }
        }
        if (Input.GetKey(KeyCode.D))
        {
            if (canUpdate == true && DenyButton.enabled && AppealPanel.activeSelf)
            {
                DenyButton.GetComponent<Animator>().SetTrigger("PlayFullPress");
                ResolveAppeal?.Emit(false);
                StartCoroutine(DelayAction(DelayTime));
            }
        }
        if (SettingsPanel.activeSelf)
        {
            AcceptButton.enabled = false;
            DenyButton.enabled = false;
            scrollable = false;
        }
        else
        {
            AcceptButton.enabled = true;
            DenyButton.enabled = true;
            scrollable = true;
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(Panel.GetComponent<RectTransform>());
        scroll();
    }

    IEnumerator DelayAction(float time)
    {
        canUpdate = false;
        yield return new WaitForSeconds(time);
        canUpdate = true;
    }

    IEnumerator DelayAudio(float time)
    {
        audioUpdate = false;
        yield return new WaitForSeconds(time);
        audioUpdate = true;
    }

    private void SetupButton(Button button)
    {
        button.TryGetComponent<EventTrigger>(out EventTrigger trigger);
        Animator animator = button.GetComponent<Animator>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }
        trigger.triggers.Add(createEntry(EventTriggerType.PointerEnter, (_) => OnHover(animator)));
        trigger.triggers.Add(
            createEntry(EventTriggerType.PointerDown, (_) => OnButtonClick(animator))
        );
        trigger.triggers.Add(
            createEntry(EventTriggerType.PointerExit, (_) => OnButtonReset(animator))
        );
    }

    private EventTrigger.Entry createEntry(EventTriggerType kind, Action<PointerEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.callback.AddListener((data) => callback((PointerEventData)data));
        entry.eventID = kind;
        return entry;
    }
}
