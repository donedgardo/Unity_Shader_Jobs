using UnityEngine;
using TMPro;

public class FrameRateCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField, Range(0.1f, 2f)] private float sampleDuration = 1f;

    private enum DisplayMode
    {
        MS,
        FPS
    }

    [SerializeField] private DisplayMode displayMode = DisplayMode.FPS;


    private int _frames;
    private float _duration, _bestDuration = float.MaxValue, _worstDuration;

    private void Update()
    {
        var frameDuration = Time.unscaledDeltaTime;
        _frames += 1;
        _duration += frameDuration;
        if (frameDuration < _bestDuration)
        {
            _bestDuration = frameDuration;
        }

        if (frameDuration > _worstDuration)
        {
            _worstDuration = frameDuration;
        }

        if (_duration <= sampleDuration)
            return;

        UpdateFrameRateText();
        ResetFrameStatistics();
    }


    private void UpdateFrameRateText()
    {
        var rateValues = new float[3];
        var displayString = "";

        if (displayMode == DisplayMode.FPS)
        {
            rateValues[0] = 1f / _bestDuration;
            rateValues[1] = _frames / _duration;
            rateValues[2] = 1f / _worstDuration;
            displayString = "FPS\n{0:0}\n{1:0}\n{2:0}";
        }
        else
        {
            rateValues[0] = 1000f * _bestDuration;
            rateValues[1] = 1000f * _duration / _frames;
            rateValues[2] = 1000f * _worstDuration;
            displayString = "MS\n{0:1}\n{1:1}\n{2:1}";
        }

        text.SetText(
            displayString,
            rateValues[0],
            rateValues[1],
            rateValues[2]
        );
    }

    private void ResetFrameStatistics()
    {
        _frames = 0;
        _duration = 0f;
        _bestDuration = float.MaxValue;
        _worstDuration = 0f;
    }
}