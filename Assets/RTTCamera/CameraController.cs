using UnityEngine;
using UnityEngine.InputSystem;

using static UnityEngine.Mathf;
using static UnityEngine.Vector3;
using static Unity.Mathematics.math;

namespace Kaizerwald.RTTCamera
{
    [DisallowMultipleComponent]
    public class CameraController : MonoBehaviour, Controls.ICameraControlActions
    {
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                              ◆◆◆◆◆◆ PROPERTIES ◆◆◆◆◆◆                                              ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        [SerializeField, Min(0.1f)] private float BaseMoveSpeed = 1;
        [SerializeField, Min(0.1f)] private float RotationSpeed = 1;
        [SerializeField, Min(0.1f)] private float ZoomSpeed = 1;

        [Tooltip("How far in degrees can you move the camera Down")]
        [SerializeField] private float MaxClamp = 70.0f;
        [Tooltip("How far in degrees can you move the camera Top")]
        [SerializeField] private float MinClamp = -30.0f;
        
        [SerializeField] private bool DontDestroy;
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                                ◆◆◆◆◆◆ FIELD ◆◆◆◆◆◆                                                 ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        public Controls Controls;
        private Transform cameraTransform;
        
        private bool isMoving, isRotating, isZooming, isSprinting;

        private float zoomValue;
        private Vector2 mouseStartPosition, mouseEndPosition;
        private Vector2 moveAxisValue;
        
        //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
        //║ ◈◈◈◈◈◈ Accessors ◈◈◈◈◈◈                                                                                    ║
        //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        private float SprintSpeed => BaseMoveSpeed * 2;
        private float MoveSpeed => isSprinting ? SprintSpeed : BaseMoveSpeed;
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                             ◆◆◆◆◆◆ UNITY EVENTS ◆◆◆◆◆◆                                             ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        
        //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
        //║ ◈◈◈◈◈◈ Awake | Start ◈◈◈◈◈◈                                                                                ║
        //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        private void Awake()
        {
            Controls ??= new Controls();
            if (!Controls.CameraControl.enabled)
            {
                Controls.CameraControl.Enable();
                Controls.CameraControl.SetCallbacks(this);
            }
            cameraTransform = transform;
            if(DontDestroy) DontDestroyOnLoad(gameObject);
        }
        
        //╓────────────────────────────────────────────────────────────────────────────────────────────────────────────╖
        //║ ◈◈◈◈◈◈ Update | Late Update ◈◈◈◈◈◈                                                                         ║
        //╙────────────────────────────────────────────────────────────────────────────────────────────────────────────╜
        private void Update()
        {
            if (!isRotating && !isMoving && !isZooming) return;
            // Rotation
            Quaternion newRotation = isRotating ? GetCameraRotation() : cameraTransform.rotation;
            // Position Left/Right/Front/Back
            Vector3 newPosition = isMoving ? GetCameraPosition(cameraTransform.position) : cameraTransform.position;
            // Position Up/Down
            newPosition += ZoomSpeed * zoomValue * Vector3.up;// isZooming check not needed since we add 0 if zoom == 0
            cameraTransform.SetPositionAndRotation(newPosition, newRotation);
        }

//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                            ◆◆◆◆◆◆ CLASS METHODS ◆◆◆◆◆◆                                             ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        private Vector3 GetCameraPosition(in Vector3 cameraPosition)
        {
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;
            //real forward of the camera (aware of the rotation)
            Vector3 cameraForwardXZ = new (cameraForward.x, 0, cameraForward.z);
            
            Vector3 xDirection = Approximately(moveAxisValue.x,0) ? zero : (moveAxisValue.x > 0 ? cameraRight : -cameraRight);
            Vector3 zDirection = Approximately(moveAxisValue.y,0) ? zero : (moveAxisValue.y > 0 ? cameraForwardXZ : -cameraForwardXZ);

            float heightMultiplier = max(1f, cameraPosition.y); //plus la caméra est haute, plus elle est rapide
            float speedMultiplier  = heightMultiplier * MoveSpeed * Time.deltaTime;
            
            return cameraPosition + (xDirection + zDirection) * speedMultiplier;
        }

        private Quaternion GetCameraRotation()
        {
            //prevent calculation if middle mouse button hold without moving
            Quaternion rotation = cameraTransform.rotation;
            if (mouseEndPosition == mouseStartPosition) return rotation;
            Vector2 distanceXY = (mouseEndPosition - mouseStartPosition) * (RotationSpeed * Time.deltaTime);
            
            rotation = Utils.RotateFWorld(rotation, 0f, distanceXY.x, 0f);//Rotation Horizontal
            rotation = Utils.RotateFSelf(rotation, -distanceXY.y, 0f, 0f);//Rotation Vertical
            
            float clampedXAxis = Utils.ClampAngle(rotation.eulerAngles.x, MinClamp, MaxClamp);
            rotation.eulerAngles = new Vector3(clampedXAxis, rotation.eulerAngles.y, 0);
            
            mouseStartPosition = mouseEndPosition; //reset start position
            return rotation;
        }
        
//╔════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
//║                                       ◆◆◆◆◆◆ INPUTS EVENTS CALLBACK ◆◆◆◆◆◆                                         ║
//╚════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        public void OnMouvement(InputAction.CallbackContext context)
        {
            isMoving = !context.canceled;
            moveAxisValue = isMoving ? context.ReadValue<Vector2>() : Vector2.zero;
        }

        public void OnRotation(InputAction.CallbackContext context)
        {
            switch (context.phase)
            {
                case InputActionPhase.Started:
                    mouseStartPosition = context.ReadValue<Vector2>();
                    isRotating = true;
                    return;
                case InputActionPhase.Performed:
                    mouseEndPosition = context.ReadValue<Vector2>();
                    return;
                case InputActionPhase.Canceled:
                    isRotating = false;
                    return;
                default:
                    return;
            }
        }

        public void OnZoom(InputAction.CallbackContext context)
        {
            isZooming = !context.canceled;
            zoomValue = isZooming ? context.ReadValue<float>() : 0;
        }

        public void OnFaster(InputAction.CallbackContext context)
        {
            isSprinting = !context.canceled;
        }
    }
}
