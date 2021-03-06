﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class RadialOptionsMenu : MonoBehaviour {

    public RadialToggle[] buttons;

    float defaultSize = 0.02f;
    float maxSize = 0.03f;

    //whether this menu can have multple options enabled at once
    public bool selector = false;

    //true if on the right controller, false if on the left
    public bool onRightController = true;

    public GameObject buttonPrefab;

    public Sprite[] selectedImages;
    public Sprite[] deselectedImages;

    //radius of the circle that the buttons are on
    public float buttonDistanceAway = 0.04f;

    //float position;

    void Start () {
        //generate buttons
        buttons = new RadialToggle[selectedImages.Length];

        for (int i = 0; i < buttons.Length; i++) {
            buttons[i] = Instantiate(buttonPrefab, transform).GetComponent<RadialToggle>();

            float angle = 360 / buttons.Length * i;

            //Sin and Cos flipped so that the first button is at the top
            Vector3 position = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * buttonDistanceAway, 0, Mathf.Cos(angle * Mathf.Deg2Rad) * buttonDistanceAway);

            buttons[i].transform.localPosition = position;

            buttons[i].selected = selectedImages[i];
            buttons[i].deselected = deselectedImages[i];
        }
	}
	
	void Update () {
        GameController gameController = GameController.instance;

        if (GetController().GetTouch(SteamVR_Controller.ButtonMask.Touchpad)) {
            float vertical = GetController().GetAxis().y;
            float horizontal = GetController().GetAxis().x;

            //find position on a circle perimeter(angle)
            //vertical and horizontal flipped so that the first button is at the top
            float position = Mathf.Rad2Deg * Mathf.Atan2(horizontal, vertical);

            SetButtonSizes(position);

            if (GetController().GetPressDown(SteamVR_Controller.ButtonMask.Touchpad) || Input.anyKeyDown) {

                if (selector) {
                    //set everything to false first if only one option can be selected at a time
                    foreach (RadialToggle button in buttons) {
                        button.Toggle(false);
                    }
                }

                GetSelectedToggle().Toggle();
            }

            foreach (RadialToggle button in buttons) {
                if (!button.gameObject.activeSelf) {
                    button.gameObject.SetActive(true);
                }
            }

        } else {
            //the menu can disappear now that the touchpad is not being touched
            //TODO: maybe make a disapearing animation
            foreach (RadialToggle button in buttons) {
                if (button.gameObject.activeSelf) {
                    button.gameObject.SetActive(false);
                }
            }
        }

    }

    public SteamVR_Controller.Device GetController() {
        if (onRightController) {
            return GameController.instance.rightController;
        } else {
            return GameController.instance.leftController;
        }
    }

    //the largest toggle would be the one selected
    public RadialToggle GetSelectedToggle() {
        RadialToggle largestToggle = null;

        foreach (RadialToggle radialToggle in buttons) {
            if (largestToggle == null || radialToggle.rectTransform.sizeDelta.x > largestToggle.rectTransform.sizeDelta.x) {
                largestToggle = radialToggle;
            }
        }

        return largestToggle;
    }

    public void SetButtonSizes(float position) {
        float[] positions = new float[buttons.Length];

        for (int i = 0; i < positions.Length; i++) {
            positions[i] = (360 / buttons.Length) * i;
        }

        for (int i = 0; i < buttons.Length; i++) {

            float difference = Mathf.Abs(position - positions[i]);

            //make sure it is the actual difference (difference between 359 and 0 is 1 not 359)
            if (difference > 180) {
                difference = Mathf.Abs(360 - difference);
            }
            if (difference < 90) {
                float size = defaultSize + (maxSize - defaultSize) * (1 - difference / 90);

                buttons[i].rectTransform.sizeDelta = new Vector2(1, 1) * size;
            } else {
                buttons[i].rectTransform.sizeDelta = new Vector2(1, 1) * defaultSize;
            }
        }
    }
}
