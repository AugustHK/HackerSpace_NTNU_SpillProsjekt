﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Singleton { get; private set; }

    public TextMeshProUGUI resourceText;
    public TextMeshProUGUI scoreText;
    public Scrollbar healthSlider;
    public RectTransform startHelpPanel;

    private InventoryManager inventory;
    private BaseController baseController;

    private float baseMaxHealth;

    public int score;

    void Awake()
    {
        #region Singleton boilerplate

        if (Singleton != null)
        {
            if (Singleton != this)
            {
                Debug.LogWarning($"There's more than one {Singleton.GetType()} in the scene!");
                Destroy(gameObject);
            }

            return;
        }

        Singleton = this;

        #endregion Singleton boilerplate
    }

    void Start()
    {
        inventory = InventoryManager.Singleton;
        baseController = BaseController.Singleton;

        UpdateResourceUI();
        baseController.HealthController.onDamage += UpdateBaseHealth;
        baseMaxHealth = baseController.HealthController.maxHealth;
    }

    void OnDestroy()
    {
        baseController.HealthController.onDamage -= UpdateBaseHealth;
    }

    public void UpdateResourceUI()
    {
        resourceText.text = inventory.ResourceAmount.ToString();
    }

    public void IncreaseScore(int increase)
    {
        score += increase;
        scoreText.text = score.ToString();
    }

    private void UpdateBaseHealth(DamageInfo damage)
    {
        healthSlider.size = damage.RemainingHealth / baseMaxHealth;
    }

    public void DisableTutorial()
    {
        startHelpPanel.gameObject.SetActive(false);
    }
}
