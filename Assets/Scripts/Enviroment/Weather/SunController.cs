using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunController : MonoBehaviour {
    public float latitude = 0.0f;
    private const float planetTilt = 23.5f;
    private float anglePerHour;

    [SerializeField]
    private Light sun;

    [SerializeField] private DateUtils dateUtils;

    private void Start() {
        anglePerHour = 360 / DateUtils.DayLength;
    }

    void Update() {

        float timeOfDay = dateUtils.TimeOfDay;

        float declinationAngle = CalculateDeclinationAngle();
        
        float hourAngle = (timeOfDay - 12) * anglePerHour;

        float altitude = CalculateAltitude(declinationAngle, hourAngle);
        
        float azimuth = CalculateAzimuth(declinationAngle, altitude);

        if (timeOfDay < 12) {
            azimuth = 2 * Mathf.PI - azimuth;
        }

        Quaternion rot = Quaternion.Euler(altitude * Mathf.Rad2Deg, azimuth * Mathf.Rad2Deg, 0);
        sun.transform.rotation = rot;
        
        AdjustLightingBasedOnAltitude(altitude);
    }

    private float CalculateDeclinationAngle() {
        return planetTilt * Mathf.Sin(2 * Mathf.PI * (dateUtils.Day + 10) / DateUtils.YearLength);
    }

    private float CalculateAltitude(float declinationAngle, float hourAngle) {
        return Mathf.Asin(Mathf.Sin(latitude * Mathf.Deg2Rad) * Mathf.Sin(declinationAngle * Mathf.Deg2Rad) +
                          Mathf.Cos(latitude * Mathf.Deg2Rad) * Mathf.Cos(declinationAngle * Mathf.Deg2Rad) *
                          Mathf.Cos(hourAngle * Mathf.Deg2Rad));
    }

    private float CalculateAzimuth(float declinationAngle, float altitude) {
        float cosAzimuth = (Mathf.Sin(declinationAngle * Mathf.Deg2Rad) - Mathf.Sin(altitude) * Mathf.Sin(latitude * Mathf.Deg2Rad)) /
                           (Mathf.Cos(altitude) * Mathf.Cos(latitude * Mathf.Deg2Rad));
       
        cosAzimuth = Mathf.Clamp(cosAzimuth, -1f, 1f);

        return Mathf.Acos(cosAzimuth);
    }
    
    private void AdjustLightingBasedOnAltitude(float altitude) {
        float normalizedAltitude = Mathf.InverseLerp(0, Mathf.PI / 2, altitude);

        float intensityCurve = normalizedAltitude * normalizedAltitude; 

        float maxIntensity = 1.0f;
        float minIntensity = 0.0f;
        float maxShadowStrength = 1.0f;
        float minShadowStrength = 0.2f;

        sun.intensity = Mathf.Lerp(minIntensity, maxIntensity, intensityCurve);
        sun.shadowStrength = Mathf.Lerp(minShadowStrength, maxShadowStrength, normalizedAltitude);
        
        float maxTemperature = 6500; 
        float minTemperature = 2000; 
        sun.colorTemperature = Mathf.Lerp(minTemperature, maxTemperature, normalizedAltitude);
    }
}
