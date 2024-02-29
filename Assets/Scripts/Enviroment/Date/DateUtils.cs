using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DateUtils : MonoBehaviour
{
    public float TimeScale => timeScale;

    public int Year => year;

    public int Month => month;

    public int Day => day;

    public int Hour => hour;

    public int Minute => minute;

    public float Second => second;
    
    public const int DayLength = 25;
    
    public const int MonthLength = 43;

    public const int YearLength = 16;

    public float TimeOfDay => timeOfDay;

    [SerializeField]
    private float timeScale = 60.0f / 1.0f;
    [SerializeField]
    private int year;
    [SerializeField]
    private int month;
    [SerializeField]
    private int day;
    [SerializeField]
    private int hour;
    [SerializeField]
    private int minute;
    [SerializeField]
    private float second;

    private float timeOfDay;

    void Update()
    {
        second += timeScale * Time.deltaTime;

        minute += (int)(second / 60);
        second %= 60; 

        hour += minute / 60;
        minute %= 60; 
        
        day += hour / DayLength;
        hour %= DayLength;

        month += day / MonthLength;
        day %= MonthLength;
        
        year += month / YearLength;
        month %= YearLength;
        
        timeOfDay = hour + minute / 60.0f + second / 3600.0f;
    }

    public string GetDateString() {
        return $"{day}/{month}/{year} {hour}:{minute}:{second}";
    }
}
