using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsController : Subscriber
{
    [SerializeField]
    private Text musicSliderText = null;

    [SerializeField]
    private Text soundSliderText = null;

    [SerializeField]
    private float maxSliderAmount = 100.0f;

    [Header("Sliders")]
    public Slider musicSlider;
    public Slider soundSlider;

    [Header("Audio")]
    public Audio SoundSample;

    [Header("Events")]
    public AudioGameEvent SoundSampleBus;

    [Header("Event Transponders")]
    public FloatGameEvent ChangeMusic;
    public FloatGameEvent ChangeSound;

    private bool shouldPlaySample = true;

    public override void Subscribe()
    {
        ChangeMusic?.Subscribe(OnChangeMusic);
        ChangeSound?.Subscribe(OnChangeSound);
    }

    public void OnChangeMusic(float value)
    {
        if (musicSliderText != null)
        {
            float localValue = value * 100;
            musicSliderText.text = localValue.ToString("0");
            musicSlider.value = value * 100;
        }
        ChangeMusic?.Unsubscribe(OnChangeMusic);
    }

    public void OnChangeSound(float value)
    {
        if (soundSliderText != null)
        {
            float localValue = value * 100;
            soundSliderText.text = localValue.ToString("0");
            soundSlider.value = value * 100;
        }
        ChangeSound?.Unsubscribe(OnChangeSound);
    }

    public void MusicValueChange(float value)
    {
        changeValue(value, ChangeMusic, musicSliderText);
    }

    public void SoundValueChange(float value)
    {
        changeValue(value, ChangeSound, soundSliderText);
        if (SoundSample.clip != null && shouldPlaySample)
        {
            shouldPlaySample = false;
            SoundSampleBus?.Emit(SoundSample);
            StartCoroutine(ResetSamplePlayer());
        }
    }

    IEnumerator ResetSamplePlayer()
    {
        yield return new WaitForSeconds(0.08f);
        shouldPlaySample = true;
    }

    private void changeValue(float value, FloatGameEvent channel, Text sliderText)
    {
        channel?.Emit(value / maxSliderAmount);
        if (sliderText != null)
        {
            float localValue = value;
            sliderText.text = localValue.ToString("0");
        }
    }
}
