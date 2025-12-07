using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [Header("Resources")]
    public SpriteList Avatars;
    public Sprite CorrectSprite;
    public Sprite IncorrectSprite;

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

        if (record.Correct)
        {
            CorrectnessImage.sprite = CorrectSprite;
            CorrectnessText.text = "<color=#070>Correct</color>";
        }
        else
        {
            CorrectnessImage.sprite = IncorrectSprite;
            CorrectnessText.text = "<color=#700>Incorrect</color>";
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
            SimulateButton(CloseButton, KeyCode.Return, KeyCode.KeypadEnter);
            SimulateButton(NextButton, KeyCode.RightArrow);
            SimulateButton(PrevButton, KeyCode.LeftArrow);
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
}
