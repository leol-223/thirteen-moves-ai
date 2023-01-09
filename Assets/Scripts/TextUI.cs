using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TextUI
{
    // convert from gameobject scale -> UI scale
    private static float _unitToPos = (816.25f / 5f);
    // private static float _unitToPosError = (816.25f / 5f) / 1.191012f;
    private static float _unitToPosError = (816.25f / 5f) * (Screen.width/(1.191012f*2339f));

    public static void SetTextSize(TMP_Text text, float size) {
        text.fontSize = size;
    }

    public static void SetTextY(TMP_Text text, float y)
    {
        // this technically ADDS y, but setting it is impossible (it breaks it)
        // so just add it once, to emulate setting it
        text.transform.position += new Vector3(0, y*_unitToPosError, 0);
    }

    public static void SetButtonY(Button button, float y) {
        button.GetComponent<RectTransform>().transform.position += new Vector3(0, y * _unitToPosError, 0);
    }

    public static void SetSliderY(Slider slider, float y)
    {
        slider.GetComponent<RectTransform>().transform.position += new Vector3(0, y * _unitToPosError, 0);
    }

    public static void SetButtonScale(Button button, float w, float h) {
        button.GetComponent<RectTransform>().sizeDelta = new Vector2(w * _unitToPos, h * _unitToPos);
    }

    public static void SetSliderScale(Slider slider, float w, float h)
    {
         slider.GetComponent<RectTransform>().sizeDelta = new Vector2(w * _unitToPos, h * _unitToPos);
    }

    // doesn't work cause unity's a little bitch
    public static Text TextFromButton(Button button) {
        return button.GetComponentInChildren<Text>();//.GetComponent<TMP_Text>();
    }
}
