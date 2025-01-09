using System;
using UnityEngine;

public class Clock : MonoBehaviour
{
    [SerializeField]
    private Transform hoursPivot,  minutesPivot, secondsPivot;
    const float _anglePerHour = -30f, _anglePerMinute = -6f, _anglePerSecond = -6f;

    private void Update()
    {
        var time = DateTime.Now.TimeOfDay;
        hoursPivot.localRotation = Quaternion.Euler(0f, 0f, _anglePerHour * (float) time.TotalHours);
        minutesPivot.localRotation = Quaternion.Euler(0f, 0f, _anglePerMinute * (float) time.TotalMinutes);
        secondsPivot.localRotation = Quaternion.Euler(0f, 0f, _anglePerSecond * (float) time.TotalSeconds);   
    }
}

