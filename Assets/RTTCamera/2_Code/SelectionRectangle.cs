using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

using static Unity.Mathematics.math;
using static Kaizerwald.RTTCamera.UGuiUtils;

namespace Kaizerwald.RTTCamera
{
    [RequireComponent(typeof(CameraController))]
    public class SelectionRectangle : MonoBehaviour, Controls.ISelectionRectangleActions
    {
        //╔════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
        //║                                            ◆◆◆◆◆◆ FIELD ◆◆◆◆◆◆                                             ║
        //╚════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        private CameraController cameraController;
        private Vector2 startLMouse, endLMouse;
        private bool clickDragPerformed;

        //╔════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
        //║                                         ◆◆◆◆◆◆ UNITY EVENTS ◆◆◆◆◆◆                                         ║
        //╚════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        
        //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
        //║ ◈◈◈◈◈◈ Awake | Start ◈◈◈◈◈◈                                                                                ║
        //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        private void Awake()
        {
            cameraController = GetComponent<CameraController>();
        }

        private void Start()
        {
            cameraController.Controls.SelectionRectangle.Enable();
            cameraController.Controls.SelectionRectangle.SetCallbacks(this);
        }

        //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
        //║ ◈◈◈◈◈◈ Enable | Disable ◈◈◈◈◈◈                                                                             ║
        //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        private void OnEnable()
        {
            if (cameraController.Controls == null) return;
            cameraController.Controls.SelectionRectangle.Enable();
            cameraController.Controls.SelectionRectangle.SetCallbacks(this);
        }

        private void OnDisable()
        {
            cameraController.Controls.SelectionRectangle.Disable();
        }
        
        //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
        //║ ◈◈◈◈◈◈ OnGUI: Rectangle OnScreen ◈◈◈◈◈◈                                                                    ║
        //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        private void OnGUI()
        {
            if (!clickDragPerformed) return;
            Rect rectangle = GetScreenRect(startLMouse, endLMouse);
            DrawScreenRect(rectangle);
            DrawScreenRectBorder(rectangle, 1);
        }
        
        //╔════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
        //║                                   ◆◆◆◆◆◆ INPUTS EVENTS CALLBACK ◆◆◆◆◆◆                                     ║
        //╚════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        private bool IsDragSelection() => Vector2.SqrMagnitude(endLMouse - startLMouse) >= 128;
        public void OnLeftMouseClickAndMove(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                startLMouse = endLMouse = context.ReadValue<Vector2>();
                clickDragPerformed = false;
            }
            else if(context.performed)
            {
                endLMouse = context.ReadValue<Vector2>();
                clickDragPerformed = IsDragSelection();
            }
            else
            {
                clickDragPerformed = false;
            }
        }
    }
}
