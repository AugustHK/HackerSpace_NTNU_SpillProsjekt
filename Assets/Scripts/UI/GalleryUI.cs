﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GalleryUI : MonoBehaviour
{
    public GalleryEntry[] entries = default;

    public GameObject galleryView = default;

    public GameObject button = default;

    public GameObject buttonHolder = default;

    private List<GameObject> buttons = default;

    // Start is called before the first frame update
    void Start()
    {
        buttons = new List<GameObject>();
        for (int i = 0; i < entries.Length; i++)
        {
            var newButton = Instantiate(button, buttonHolder.transform);
            buttons.Add(newButton);
            var gb = newButton.GetComponent<GalleryButton>();
            var cbn = newButton.GetComponent<ControllerButtonNavigator>();
            if (i == 0) 
            {
                //setup first button
                gb.SetGalleryView(galleryView); 
                cbn.defaultButton = cbn; 
                cbn.buttonUp = cbn;
            }
            else
            {
                //every other button
                cbn.buttonUp = buttons[i - 1].GetComponent<ControllerButtonNavigator>();
                buttons[i - 1].GetComponent<ControllerButtonNavigator>().buttonDown = cbn;
                //last set downbutton to itself
                if (i == entries.Length - 1) { cbn.buttonDown = cbn; }
            }
            gb.SetScriptableObject(entries[i]);
            
        }
    }

}