﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GalleryButton : MonoBehaviour
{
    private static float positionOffset = default;

    public static GameObject currentViewObject = default;

    [SerializeField]
    private float offsetIncrement = default;

    private static GameObject galleryView = default;

    private static Transform objectView = default;

    private GalleryEntry scriptableObject = default;

    [SerializeField]
    private TextMeshProUGUI textMesh = default;

    void Start()
    {
        GetComponent<RectTransform>().LeanMoveLocalY(positionOffset, 0.2f);
        positionOffset += offsetIncrement;
        var color = gameObject.GetComponent<Image>().color;
        gameObject.GetComponent<Image>().color = new Color(color.r, color.g, color.b, 1);
        textMesh.text = scriptableObject.name;
        objectView = galleryView.transform.Find("ObjectView");
    }

    private void OnDestroy()
    {
        positionOffset = 0f;
    }

    public void SetScriptableObject(GalleryEntry scObj)
    {
        scriptableObject = scObj;
    }

    public void SetGalleryView(GameObject obj)
    {
        galleryView = obj;
    }

    private bool IsUnlocked()
    {
        return SteamManager.Singleton.IsAchievementUnlocked(scriptableObject.achievementId);
        //Use return statement below instead of the above when in development and you don't want steam to keep pestering you about achievements to view gallery.
        //return true;
    }

    public void SelectButton()
    {
        galleryView.transform.Find("Description").GetComponent<TextMeshProUGUI>().text = "Unlock the related achievement to view!";
        if (currentViewObject) { Destroy(currentViewObject); }
        if (IsUnlocked())
        {
            galleryView.transform.Find("Description").GetComponent<TextMeshProUGUI>().text = scriptableObject.description;
            currentViewObject = Instantiate(scriptableObject.prefab, objectView);
            currentViewObject.transform.localScale = new Vector3(100f, 100f, 100f);
            currentViewObject.transform.parent = null;
        }
        
    }

}
