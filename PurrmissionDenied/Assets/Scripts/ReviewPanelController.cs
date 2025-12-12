using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ReviewPanelController : Subscriber
{
    [Header("Elements")]
    public Button ReviewButton;
    public Button CloseButton;
    public GameObject ReviewPanelRoot;
    public Text RulesText;
    public Text TotalAppealsText;
    public Text CurrentAppealText;
    public Button PrevButton;
    public Button NextButton;
    public Image CorrectnessImage;
    public Text CorrectnessText;
    public Animator StreakImage;
    public Text PointText;
    public Image AvatarImage;
    public Text NameText;
    public Text BioText;
    public RectTransform UserMessages;
    public Text AppealText;

    public GameObject RulesScrollView;
    public GameObject AppealScrollView;

    public List<RectTransform> ContentBoxes;

    [Header("Resources")]
    public GameObject MessageContainer;
    public SpriteList Avatars;
    public Sprite DefaultAvatar;
    public Sprite CorrectSprite;
    public Sprite IncorrectSprite;
    public Audio ScrollAudio;

    [Header("Event Listeners")]
    public SolidifiedGameEvent RecordsSolidified;
    public AppealRecordGameEvent RecordLoaded;

    [Header("Events")]
    public IntGameEvent LoadRecord;
    public BoolGameEvent ReviewPanelActive;

    private int currentRecord = 0;
    private int recordCount = 0;

    private bool visible = false;

    public override void Subscribe()
    {
        RecordsSolidified?.Subscribe(OnRecordsSolidified);
        RecordLoaded?.Subscribe(OnRecordLoaded);
        ReviewPanelRoot?.SetActive(false);
    }

    public void SetReviewPanelState(bool state)
    {
        visible = state;
        ReviewPanelRoot?.SetActive(state);
        Canvas.ForceUpdateCanvases();
        foreach (RectTransform rect in ContentBoxes)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
        ReviewButton.interactable = !state;
        CloseButton.interactable = state;
        ReviewPanelActive?.Emit(state);
    }

    public void PrevRecord()
    {
        if (currentRecord > 0 && recordCount > 0)
        {
            LoadRecord?.Emit(--currentRecord);
        }
    }

    public void NextRecord()
    {
        if (currentRecord < (recordCount - 1) && recordCount > 0)
        {
            LoadRecord?.Emit(++currentRecord);
        }
    }

    private void OnRecordsSolidified(RecordKeeper.Solidified data)
    {
        RulesText.text = data.RuleText;
        recordCount = data.RecordCount;
        TotalAppealsText.text = $"/ {data.RecordCount}";
        if (recordCount > 0)
            LoadRecord?.Emit(0);
    }

    private void OnRecordLoaded(RecordKeeper.AppealRecord record)
    {
        CurrentAppealText.text = $"{record.Index + 1} ";
        PrevButton.interactable = record.Index != 0;
        NextButton.interactable = record.Index != recordCount - 1;

        NameText.text = record.User.name;
        BioText.text = record.User.bio;

        if (Avatars != null)
        {
            if (Avatars.Sprites.Count > record.User.image_index)
            {
                AvatarImage.sprite = Avatars.Sprites[record.User.image_index];
            }
            else if (DefaultAvatar != null)
            {
                AvatarImage.sprite = DefaultAvatar;
            }
            else if (Avatars.Sprites.Count > 0)
            {
                AvatarImage.sprite = Avatars.Sprites[0];
            }
        }
        else if (DefaultAvatar != null)
        {
            AvatarImage.sprite = DefaultAvatar;
        }

        foreach (Transform child in UserMessages.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (string msg in record.User.messages)
        {
            GameObject instantiatedObject = Instantiate(MessageContainer, UserMessages);
            TextMeshProUGUI textComponent =
                instantiatedObject.GetComponentInChildren<TextMeshProUGUI>();
            textComponent.text = msg;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(UserMessages);
        }

        AppealText.text = record.User.appeal_message;

        if (record.Correct)
        {
            CorrectnessImage.sprite = CorrectSprite;
            CorrectnessText.text = $"<color=#070>{record.Choice} Correctly</color>";
            if (record.Streak > 2)
            {
                PointText.text =
                    $"<color=#070>   x{record.Streak} Streak   {record.Score} points</color>";
                StreakImage.gameObject.SetActive(true);
                switch (record.Streak)
                {
                    case 3:
                        StreakImage.SetTrigger("Red");
                        break;
                    case 4:
                        StreakImage.SetTrigger("Orange");
                        break;
                    default:
                        StreakImage.SetTrigger("Blue");
                        break;
                }
            }
            else
            {
                PointText.text =
                    $"<color=#070>x{record.Streak} Streak   {record.Score} points</color>";
                StreakImage.SetTrigger("Red");
                StreakImage.gameObject.SetActive(false);
            }
        }
        else
        {
            CorrectnessImage.sprite = IncorrectSprite;
            CorrectnessText.text = $"<color=#700>{record.Choice} Incorrectly</color>";
            StreakImage.SetTrigger("Red");
            StreakImage.gameObject.SetActive(false);
            PointText.text = $"<color=#700>{record.MistakesText}</color>";
        }
    }

    void Update()
    {
        if (!visible)
        {
            SimulateButton(ReviewButton, KeyCode.R);
        }
        else
        {
            SimulateButton(CloseButton, KeyCode.R);
            SimulateButton(NextButton, KeyCode.RightArrow, KeyCode.D);
            SimulateButton(PrevButton, KeyCode.LeftArrow, KeyCode.A);

            SimulateScrolling(RulesScrollView, KeyCode.W, KeyCode.S);
            SimulateScrolling(AppealScrollView, KeyCode.UpArrow, KeyCode.DownArrow);
        }
    }

    void SimulateButton(Button btn, params KeyCode[] codes)
    {
        if (
            btn.gameObject.activeSelf
            && btn.interactable
            && codes.Any((code) => Input.GetKeyDown(code))
        )
        {
            StartCoroutine(SimulateButtonPress(btn));
        }
    }

    private const float SCROLL_PER_SECOND = 10.0f;

    void SimulateScrolling(GameObject scrollview, KeyCode up, KeyCode down)
    {
        if (scrollview.gameObject.activeSelf)
        {
            if (Input.GetKey(up))
                SimulateScrollEvent(scrollview, Time.deltaTime * SCROLL_PER_SECOND);
            else if (Input.GetKey(down))
                SimulateScrollEvent(scrollview, -Time.deltaTime * SCROLL_PER_SECOND);
        }
    }

    private IEnumerator SimulateButtonPress(Button btn)
    {
        if (btn == null)
            yield break;

        // Ensure there’s an EventSystem (if the scene doesn’t already have one)
        if (EventSystem.current == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // Create a fake pointer event to simulate a mouse click
        var ped = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            clickCount = 1,
        };

        // Trigger the button's "pressed" visual state
        ExecuteEvents.Execute(btn.gameObject, ped, ExecuteEvents.pointerDownHandler);

        // Wait briefly so the pressed sprite is visible
        yield return new WaitForSeconds(0.1f);

        // Release and invoke the click
        ExecuteEvents.Execute(btn.gameObject, ped, ExecuteEvents.pointerUpHandler);
        btn.onClick.Invoke();
    }

    private void SimulateScrollEvent(GameObject scrollview, float delta)
    {
        if (scrollview == null)
            return;

        // Ensure there’s an EventSystem (if the scene doesn’t already have one)
        if (EventSystem.current == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // Create a fake pointer event to simulate a scroll event
        var data = new PointerEventData(EventSystem.current)
        {
            scrollDelta = new Vector2(0, delta),
        };

        // Trigger the button's "pressed" visual state
        ExecuteEvents.Execute(scrollview.gameObject, data, ExecuteEvents.scrollHandler);
    }
}
