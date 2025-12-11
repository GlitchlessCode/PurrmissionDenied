using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DMSPanelController : Subscriber
{
    [Header("UI")]
    public GameObject SettingsPanel;

    [Header("Events")]
    public AudioGameEvent AudioBus;

    [Header("Event Listeners")]
    public DirectMessageGameEvent DMSent;
    public BoolGameEvent DMTabClick;
    public UnitGameEvent AddTimestamp;

    [SerializeField]
    private TextMeshProUGUI textComponent;

    [SerializeField]
    private Image image;
    public GameObject container;
    public RectTransform content;
    public Transform DMPanel;
    private List<GameObject> containers = new List<GameObject>();
    private List<RectTransform> transforms = new List<RectTransform>();
    private List<float> heights = new List<float>();
    public Audio Scroll;
    private bool audioUpdate;
    private bool scrollable = true;
    public float scrollSpeed = 5000000f;
    private Vector3 initialPosition;

    [Header("Timestamps")]
    public Text TimestampPrefab;

    private void Start()
    {
        content.anchoredPosition = new Vector2(0, 0);
        audioUpdate = true;
    }

    public override void Subscribe()
    {
        DMSent?.Subscribe(OnDMSent);
        DMTabClick?.Subscribe(OnDMTabClick);
        AddTimestamp?.Subscribe(OnAddTimestamp);
    }

    public void OnDMSent(DirectMessage DM)
    {
        AddDM(DM);
    }

    public void OnDMTabClick(bool clicked)
    {
        if (clicked)
        {
            scrollable = true;
        }
        else
        {
            scrollable = false;
        }
    }

    private void OnAddTimestamp()
    {
        Text timestamp = Instantiate(TimestampPrefab, DMPanel);
        timestamp.text = $"  {System.DateTime.Now.ToString("t")}";
    }

    void Update()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(DMPanel.GetComponent<RectTransform>());
        scroll();
    }

    void AddDM(DirectMessage DM)
    {
        GameObject instantiatedObject = Instantiate(container, DMPanel);
        textComponent = instantiatedObject.GetComponentInChildren<TextMeshProUGUI>();
        textComponent.text = DM.message;

        containers.Add(instantiatedObject);

        RectTransform trans = instantiatedObject.GetComponent<RectTransform>();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(DMPanel.GetComponent<RectTransform>());
        transforms.Add(trans);
    }

    void scroll()
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

    void ResetDMHeight()
    {
        content.anchoredPosition = new Vector2(0, 0);
    }

    IEnumerator DelayAudio(float time)
    {
        audioUpdate = false;
        yield return new WaitForSeconds(time);
        audioUpdate = true;
    }
}
