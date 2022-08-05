﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class UIInputController : MonoBehaviour
{
    private PlayerInput playerInput;

    public Vector2 MoveInput { get; private set; } = Vector2.zero;

    private void MoveInput_Performed(InputAction.CallbackContext ctx)
    {
        Vector2 newMoveInput = ctx.ReadValue<Vector2>();
        if (newMoveInput.magnitude <= 0.1f // acts as a joystick deadzone
            // Ensures that a joystick has to be either reset to neutral position or moved from a southward to a northward direction (or vice versa),
            // before a button press is handled below
            // (this logic only works for vertical menus - with buttons placed on top of each other)
            || MathUtils.StrictSign(newMoveInput.y) == MathUtils.StrictSign(MoveInput.y))
            return;

        MoveInput = newMoveInput;
        Move();
    }

    private void MoveInput_Canceled(InputAction.CallbackContext ctx) => MoveInput = Vector2.zero;
    private void InteractInput_Performed(InputAction.CallbackContext ctx) => Select();

    public void SetUpInput(PlayerInput playerInput)
    {
        this.playerInput = playerInput;
        AddListeners();
    }

    void OnDestroy()
    {
        if (playerInput)
            RemoveListeners();
    }

    private void AddListeners()
    {
        playerInput.actions["Move"].performed += MoveInput_Performed;
        playerInput.actions["Move"].canceled += MoveInput_Canceled;
        playerInput.actions["Interact"].performed += InteractInput_Performed;
    }

    private void RemoveListeners()
    {
        playerInput.actions["Move"].performed -= MoveInput_Performed;
        playerInput.actions["Move"].canceled -= MoveInput_Canceled;
        playerInput.actions["Interact"].performed -= InteractInput_Performed;
    }

    protected virtual void Move()
    {
        if (PauseManager.Singleton.IsPaused || BaseController.Singleton.isGameOver)
            DetermineDirection();
    }

    protected void DetermineDirection()
    {
        if (MoveInput.magnitude <= 0.1f)
            return;

        if (MoveInput.y > 0)
            ControllerButtonNavigator.currentButton.buttonUp.SetCurrentButton();
        else if (MoveInput.y < 0f)
            ControllerButtonNavigator.currentButton.buttonDown.SetCurrentButton();
    }

    protected virtual void Select()
    {
        if (PauseManager.Singleton.IsPaused || BaseController.Singleton.isGameOver)
            ControllerButtonNavigator.currentButton.PressButton();
    }
}
