using UnityEngine;

namespace Invector.vCharacterController
{
    public class vThirdPersonInput : MonoBehaviour
    {
        #region Variables

        [Header("Controller Input")]
        public string horizontalInput = "Horizontal";
        public string verticallInput = "Vertical";
        public KeyCode jumpInput = KeyCode.Space;
        public KeyCode strafeInput = KeyCode.Tab;
        public KeyCode sprintInput = KeyCode.LeftShift;

        [Header("Camera Input")]
        public string rotateCameraXInput = "Mouse X";
        public string rotateCameraYInput = "Mouse Y";

        [Header("Mobile Input")]
        [Tooltip("True while the mobile joystick is being used.")]
        public bool mobileMoveActive;

        [Tooltip("Mobile joystick input.")]
        public Vector2 mobileMoveInput;

        [Tooltip("True while the mobile sprint button is pressed.")]
        public bool mobileSprintPressed;

        [HideInInspector] public vThirdPersonController cc;
        [HideInInspector] public vThirdPersonCamera tpCamera;
        [HideInInspector] public Camera cameraMain;

        bool canJump = false;
        bool mobileSprintState;

        #endregion

        protected virtual void Start()
        {
            InitilizeController();
            InitializeTpCamera();
        }

        protected virtual void FixedUpdate()
        {
            cc.UpdateMotor();
            cc.ControlLocomotionType();
            cc.ControlRotationType();
        }

        protected virtual void Update()
        {
            InputHandle();
            cc.UpdateAnimator();
        }

        public virtual void OnAnimatorMove()
        {
            cc.ControlAnimatorRootMotion();
        }

        #region Basic Locomotion Inputs

        protected virtual void InitilizeController()
        {
            cc = GetComponent<vThirdPersonController>();

            if (cc != null)
                cc.Init();
        }

        protected virtual void InitializeTpCamera()
        {
            if (tpCamera == null)
            {
                tpCamera = FindObjectOfType<vThirdPersonCamera>();

                if (tpCamera == null)
                    return;

                if (tpCamera)
                {
                    tpCamera.SetMainTarget(this.transform);
                    tpCamera.Init();
                }
            }
        }

        protected virtual void InputHandle()
        {
            MoveInput();
            CameraInput();
            SprintInput();
            StrafeInput();
            JumpInput();
        }

        // =========================================================
        // MOVEMENT
        // =========================================================

        public virtual void MoveInput()
        {
            // MOBILE
            if (mobileMoveActive)
            {
                cc.input.x = mobileMoveInput.x;
                cc.input.z = mobileMoveInput.y;
                return;
            }

            // PC
            cc.input.x = Input.GetAxis(horizontalInput);
            cc.input.z = Input.GetAxis(verticallInput);
        }

        // Called by mobile joystick
        public virtual void SetMobileMove(Vector2 value)
        {
            mobileMoveInput = Vector2.ClampMagnitude(value, 1f);
            mobileMoveActive = true;
        }

        // Called when joystick is released
        public virtual void ReleaseMobileMove()
        {
            mobileMoveInput = Vector2.zero;
            mobileMoveActive = false;
        }

        // =========================================================
        // CAMERA
        // =========================================================

        protected virtual void CameraInput()
        {
            if (!PlayerAttackController.CursorLocked)
                return;

            if (!cameraMain)
            {
                if (!Camera.main)
                {
                    Debug.Log("Missing a Camera with the tag MainCamera, please add one.");
                }
                else
                {
                    cameraMain = Camera.main;
                    cc.rotateTarget = cameraMain.transform;
                }
            }

            if (cameraMain)
            {
                cc.UpdateMoveDirection(cameraMain.transform);
            }

            if (tpCamera == null)
                return;

            // PC camera
            var Y = Input.GetAxis(rotateCameraYInput);
            var X = Input.GetAxis(rotateCameraXInput);

            tpCamera.RotateCamera(X, Y);
        }

        // =========================================================
        // STRAFE / LOCK BUTTON
        // =========================================================

        protected virtual void StrafeInput()
        {
            // PC
            if (Input.GetKeyDown(strafeInput))
                cc.Strafe();
        }

        // Mobile button
        public virtual void MobileStrafe()
        {
            if (cc != null)
                cc.Strafe();
        }

        // =========================================================
        // SPRINT
        // =========================================================

        protected virtual void SprintInput()
        {
            Stamina stamina = GetComponent<Stamina>();
            MoveManager moveManager = GetComponent<MoveManager>();

            if (stamina != null && moveManager != null)
            {
                if (stamina.stamina < moveManager.staminaLost)
                {
                    cc.Sprint(false);
                    mobileSprintState = false;
                    return;
                }
            }

            // MOBILE
            if (mobileSprintPressed != mobileSprintState)
            {
                mobileSprintState = mobileSprintPressed;
                cc.Sprint(mobileSprintState);
            }

            // PC
            if (Input.GetKeyDown(sprintInput))
                cc.Sprint(true);
            else if (Input.GetKeyUp(sprintInput))
                cc.Sprint(false);
        }

        // Mobile button DOWN
        public virtual void MobileSprintDown()
        {
            mobileSprintPressed = true;
        }

        // Mobile button UP
        public virtual void MobileSprintUp()
        {
            mobileSprintPressed = false;
        }

        // =========================================================
        // JUMP
        // =========================================================

        protected virtual bool JumpConditions()
        {
            return cc.isGrounded &&
                   cc.GroundAngle() < cc.slopeLimit &&
                   !cc.isJumping &&
                   !cc.stopMove;
        }

        protected virtual void JumpInput()
        {
            if (!canJump)
                return;

            if (PlayerAttackController.Instance.isAttacking ||
                GetComponent<PlayerTakeDamge>().isBlock)
                return;

            // PC
            if (Input.GetKeyDown(jumpInput) && JumpConditions())
                cc.Jump();
        }

        // Mobile jump button
        public virtual void MobileJump()
        {
            if (!canJump)
                return;

            if (PlayerAttackController.Instance.isAttacking ||
                GetComponent<PlayerTakeDamge>().isBlock)
                return;

            if (JumpConditions())
                cc.Jump();
        }

        #endregion
    }
}