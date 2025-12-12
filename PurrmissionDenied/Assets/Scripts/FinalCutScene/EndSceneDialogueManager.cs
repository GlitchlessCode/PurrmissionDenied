using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One line of end-scene dialogue, loaded from JSON
[System.Serializable]
public struct EndSceneLine
{
    public string message;
    public int spriteIndex; // -1 if no sprite change on this line
    public bool fadeFromBlack;
}

public class EndSceneDialogueManager : Subscriber
{
    [Header("JSON Loading")]
    [Tooltip(
        "Subfolder inside StreamingAssets, e.g. 'EndScene' -> Assets/StreamingAssets/EndScene"
    )]
    public string directory = "EndScene";

    [Tooltip("JSON filenames WITHOUT .json, in the order you want them played.")]
    public string[] dialogueFiles;

    private Dictionary<string, EndSceneLine> lines;
    private List<EndSceneLine> orderedLines = new List<EndSceneLine>();
    private int currentLineIndex = -1;
    private bool hasRequestedLoad = false;

    [Header("Event Listeners")]
    [Tooltip("Emitted by your async loading system when everything is ready.")]
    public UnitGameEvent AsyncComplete;

    [Header("UI References")]
    public CanvasGroup textBoxGroup; // CanvasGroup on the text box panel
    public Text dialogueText; // regular UnityEngine.UI.Text
    public Button nextButton;
    public Image characterImage; // character portrait image
    public Animator credits;
    public Button playAgain;
    public Animator powerOffPanel;

    [Header("Character Sprites")]
    public Sprite[] characterSprites; // indices used by spriteIndex

    [Header("Typing Settings")]
    public float charactersPerSecond = 30f;

    [Tooltip("Play typing SFX every N characters (1 = every char).")]
    public int charsPerTypingSfx = 2;

    [Header("Audio Buses")]
    public AudioGameEvent MusicBus; // background music bus
    public AudioGameEvent SfxBus; // SFX bus (typing, click, etc.)

    [Header("Audio Clips")]
    public Audio TypingSfx; // small bleep for typing
    public Audio NextLineSfx; // optional click when moving to next line
    public Audio PowerOffSfx;

    // --------- NEW: Background swapping stuff ---------
    [Header("Background Swap")]
    [Tooltip("The UI Image used for the background.")]
    public Image backgroundImage;

    [Tooltip("Sprite used at scene start.")]
    public Sprite startBackgroundSprite;

    [Tooltip("Sprite to switch to after the 5th dialogue line.")]
    public Sprite swappedBackgroundSprite;

    [Tooltip("Optional panel with Animator that plays the power-on effect.")]
    public GameObject PowerOnPanel;

    [Tooltip("Animator trigger name for the power-on animation.")]
    public string powerOnTriggerName = "PlayPowerOn";

    [Tooltip("0-based index of the line at which to swap the background (4 = 5th line).")]
    public int backgroundSwapLineIndex = 4;

    private bool hasSwappedBackground = false;

    // --------------------------------------------------

    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool canUpdate = false;
    private bool textBoxVisible = false;

    public Button playButton; // NEW: Hook up your Play button here in the Inspector
    public float playAnimationDelay = 0.3f; // NEW: How long to wait for the Play button fade anim

    private bool linesLoaded = false; // NEW: JSON data is ready
    private bool hasPressedPlay = false; // NEW: Prevent double-start

    public Animator fadeAnimator; // Drag the fade image’s Animator here
    public string fadeTriggerName = "FadeFromBlack"; // Trigger on the Animator
    public string fadeStateName = "FadeFromBlack"; // State/clip name to time
    public float fallbackFadeDuration = 0.25f; // Used if timing lookup fails

    private bool isTransitioning = false;

    // ---------------- Subscriber setup ----------------

    public override void Subscribe()
    {
        // Listen for the async-complete signal
        AsyncComplete?.Subscribe(OnAsyncComplete);

        // Basic UI init
        if (textBoxGroup != null)
        {
            textBoxGroup.alpha = 0f;
            textBoxVisible = false;
        }

        if (dialogueText != null)
            dialogueText.text = string.Empty;

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextClicked);
            nextButton.interactable = false; // we enable this once lines are loaded
        }

        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked); // NEW
            nextButton.interactable = false; // NEW
        }

        // NEW: Set starting background and play PowerOn after a short delay
        if (backgroundImage != null && startBackgroundSprite != null)
        {
            backgroundImage.sprite = startBackgroundSprite;
        }

        if (PowerOnPanel != null)
        {
            StartCoroutine(PowerOnDelayed());
        }

        // Start loading our end-scene JSON via the existing async importer
        StartCoroutine(
            JSONImporter.ImportFiles<EndSceneLine>(
                Path.Combine("lang", "en", directory),
                dialogueFiles,
                (dialogueOut) =>
                {
                    lines = dialogueOut;
                    AsyncComplete?.Emit();
                }
            )
        );
    }

    // NEW: Power-on animation coroutine (from BackgroundSwapper)
    private IEnumerator PowerOnDelayed()
    {
        yield return new WaitForSeconds(0.2f);

        Animator anim = PowerOnPanel.GetComponent<Animator>();
        if (anim != null && !string.IsNullOrEmpty(powerOnTriggerName))
        {
            anim.SetTrigger(powerOnTriggerName);
        }
    }

    // ---------------- JSON load callback ----------------
    private void OnAsyncComplete()
    {
        if (hasRequestedLoad)
            return;
        hasRequestedLoad = true;

        // Preserve the order specified in dialogueFiles
        foreach (string file in lines.Keys)
        {
            if (lines.TryGetValue(file, out EndSceneLine line))
            {
                orderedLines.Add(line);
            }
            else
            {
                Debug.LogWarning("EndSceneDialogueManager: Missing dialogue file: " + file);
            }
        }

        if (lines.Count == 0)
        {
            Debug.LogError("EndSceneDialogueManager: No dialogue lines loaded!");
            return;
        }

        linesLoaded = true; // NEW
        canUpdate = false; // NEW: wait for Play
        currentLineIndex = -1; // keep reset

        // if (nextButton != null)
        // {
        //     nextButton.interactable = true;
        //     canUpdate = true;
        // }

        // currentLineIndex = -1;
        // ShowNextLine();
    }

    void Update()
    {
        if (canUpdate)
        {
            if (Input.GetKeyUp(KeyCode.KeypadEnter) || Input.GetKeyUp(KeyCode.Return))
            {
                if (currentLineIndex < orderedLines.Count)
                {
                    OnNextClicked();
                }
                else
                {
                    StartCoroutine(SimulateButtonPress(playAgain));
                }
                StartCoroutine(DelayAction(0.5f));
            }
        }
        else if (!hasPressedPlay)
        {
            if (Input.GetKeyUp(KeyCode.KeypadEnter) || Input.GetKeyUp(KeyCode.Return))
            {
                StartCoroutine(SimulateButtonPress(playButton));
            }
        }
    }

    // ---------------- Input / flow ----------------

    private void OnNextClicked()
    {
        // Optional SFX for going to next line (only when not skipping)
        if (!isTyping && SfxBus != null && NextLineSfx.clip != null) // CHANGED
        {
            SfxBus.Emit(NextLineSfx);
        }

        if (isTyping)
        {
            FinishCurrentLineInstantly();
        }
        else
        {
            StartCoroutine(DelayedNextLine());
        }
    }

    private IEnumerator DelayedNextLine()
    {
        dialogueText.text = string.Empty;
        yield return new WaitForSeconds(0.2f);
        ShowNextLine();
    }

    // REPLACE your ShowNextLine() with this:
    private void ShowNextLine()
    {
        // Bounds check should use orderedLines.Count (not lines.Count)
        if (currentLineIndex + 1 >= orderedLines.Count)
        {
            // No more lines: fade out textbox and disable button
            if (nextButton != null)
                nextButton.interactable = false;
            if (textBoxGroup != null)
                StartCoroutine(FadeTextBox(1f, 0f, 0.5f));
            if (credits != null)
                credits.SetTrigger("FadeIn");
            if (playAgain != null)
                playAgain.onClick.AddListener(PlayAgain);
            currentLineIndex++;
            return;
        }

        currentLineIndex++;

        // One-shot background swap (kept as-is)
        if (!hasSwappedBackground && currentLineIndex >= backgroundSwapLineIndex)
        {
            SwapBackgroundOnce();
        }

        // Kick off the robust, timed sequence for this line
        var line = orderedLines[currentLineIndex];
        StartCoroutine(ShowLineWithOptionalFade(line));
    }

    private void PlayAgain()
    {
        if (!isTransitioning)
        {
            if (powerOffPanel != null)
            {
                powerOffPanel.gameObject.SetActive(true);
                powerOffPanel.SetTrigger("PlayPowerOff");
                if (PowerOffSfx.clip != null)
                {
                    SfxBus?.Emit(PowerOffSfx);
                }
            }
            isTransitioning = true;
        }
    }

    // ADD this helper coroutine (works with your existing fields/methods)
    private IEnumerator ShowLineWithOptionalFade(EndSceneLine line)
    {
        // Lock input while we transition
        canUpdate = false;
        if (nextButton != null)
            nextButton.interactable = false;

        // Ensure textbox is visible before we type (and wait for the fade to finish)
        if (!textBoxVisible && textBoxGroup != null)
            yield return StartCoroutine(FadeTextBox(0f, 1f, 0.5f));

        // Optional: quick fade-from-black before this specific line
        if (line.fadeFromBlack && fadeAnimator != null)
        {
            // Play the fade clip safely from the start
            fadeAnimator.ResetTrigger(fadeTriggerName);
            fadeAnimator.Play(fadeStateName, 0, 0f);
            fadeAnimator.SetTrigger(fadeTriggerName);

            // Wait for the actual clip length (fallback if lookup fails)
            float dur = GetClipLength(fadeAnimator, fadeStateName, fallbackFadeDuration);
            yield return new WaitForSeconds(dur);
        }

        // Apply sprite change (kept from your code)
        if (
            characterImage != null
            && characterSprites != null
            && line.spriteIndex >= 0
            && line.spriteIndex < characterSprites.Length
        )
        {
            characterImage.sprite = characterSprites[line.spriteIndex];
        }

        // Start typing (stop any prior typing first)
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        dialogueText.text = string.Empty;
        typingCoroutine = StartCoroutine(TypeLine(line.message));

        // Re-enable advancing AFTER we kick off typing (or when typing completes if you gate it there)
        if (nextButton != null)
            nextButton.interactable = true;
        canUpdate = true;
    }

    private void SwapBackgroundOnce()
    {
        if (backgroundImage != null && swappedBackgroundSprite != null)
        {
            backgroundImage.sprite = swappedBackgroundSprite;
            hasSwappedBackground = true;
        }
    }

    // ---------------- Typing effect ----------------

    private IEnumerator TypeLine(string fullText)
    {
        isTyping = true;

        if (dialogueText != null)
            dialogueText.text = string.Empty;

        float delay = 1f / Mathf.Max(1f, charactersPerSecond);
        int charCount = 0;

        foreach (char c in fullText)
        {
            if (dialogueText != null)
                dialogueText.text += c;

            charCount++;

            // Play typing sound every few chars
            if (
                SfxBus != null
                && TypingSfx.clip != null
                && charCount % Mathf.Max(1, charsPerTypingSfx) == 0
            )
            {
                SfxBus.Emit(TypingSfx);
            }

            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
        typingCoroutine = null;
    }

    private void FinishCurrentLineInstantly()
    {
        if (currentLineIndex < 0 || currentLineIndex >= lines.Count)
            return;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (dialogueText != null)
            dialogueText.text = orderedLines[currentLineIndex].message;

        isTyping = false;
    }

    // ---------------- Fading textbox ----------------

    private IEnumerator FadeTextBox(float from, float to, float duration)
    {
        if (textBoxGroup == null)
            yield break;

        textBoxVisible = true;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);
            textBoxGroup.alpha = Mathf.Lerp(from, to, lerp);
            yield return null;
        }

        textBoxGroup.alpha = to;

        if (Mathf.Approximately(to, 0f))
        {
            textBoxVisible = false;
        }
    }

    IEnumerator DelayAction(float time)
    {
        canUpdate = false;
        yield return new WaitForSeconds(time);
        canUpdate = true;
    }

    //---------------------- Next Button --------------------
    private void OnPlayClicked() // NEW
    {
        if (!linesLoaded || hasPressedPlay)
            return;
        hasPressedPlay = true;

        // Optional: hide/disable Play after click so it can’t be pressed again
        if (playButton != null)
        {
            playButton.interactable = false;
            // Optionally: playButton.gameObject.SetActive(false);
        }

        StartCoroutine(BeginDialogueAfterPlay());
    }

    private IEnumerator BeginDialogueAfterPlay() // NEW
    {
        // Give your Play button’s animator time to run its short fade
        yield return new WaitForSeconds(Mathf.Max(0f, playAnimationDelay));

        // Fade in the text box now
        if (textBoxGroup != null && !textBoxVisible)
            StartCoroutine(FadeTextBox(0f, 1f, 0.5f));

        if (nextButton != null)
            nextButton.interactable = true;

        canUpdate = true; // allow Enter/Return to advance
        currentLineIndex = -1;
        ShowNextLine(); // finally start dialogue
    }

    //----------------Fade From Black --------------------------------
    private void PlayFadeFromBlack()
    {
        if (fadeAnimator == null)
            return;
        // Reset and play from start to avoid re-entry issues
        fadeAnimator.ResetTrigger(fadeTriggerName);
        fadeAnimator.Play(fadeStateName, 0, 0f);
        fadeAnimator.SetTrigger(fadeTriggerName);
    }

    // OPTIONAL: put this utility somewhere in your class if you don't have it yet
    private float GetClipLength(Animator animator, string stateOrClipName, float fallback)
    {
        if (animator == null)
            return fallback;
        var ac = animator.runtimeAnimatorController;
        if (ac != null)
        {
            foreach (var clip in ac.animationClips)
            {
                if (clip != null && clip.name == stateOrClipName)
                    return clip.length;
            }
        }
        return fallback;
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
