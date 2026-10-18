using UnityEngine;
using UnityEngine.InputSystem; 
using UnityEngine.UI;


[RequireComponent(typeof(CharacterController))]

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 5.5f;
    [SerializeField] private float runSpeed = 9.0f;


    [SerializeField] private float jumpForce = 8.0f;
    [SerializeField] private float gravity = 20.0f;


    [SerializeField] private float lookAngleLimit = 90.0f;
    [SerializeField] private float lookSensitivity = 0.2f;

    //Interação
    [SerializeField] private float interactionDistance = 3.0f;
    [SerializeField] private LayerMask interactionLayerMask;
    [SerializeField, Min(0.01f)] private float interactionDuration = 1.0f;
    [SerializeField] private Slider interactionSlider;


    private Camera mainCamera;
    private CharacterController characterController;


    private InputAction moveAction;
    private InputAction sprintAction;

    private InputAction jumpAction;
    private bool jumped = false;

    private InputAction interactAction;
    private OutlineHighlight currentTarget;
    private OutlineHighlight progressTarget;
    private float interactionProgress;
    private bool interactionCompleted;
    private bool controlsEnabled = true;
    private int EffectiveInteractionLayerMask =>
        interactionLayerMask.value == 0 ? Physics.DefaultRaycastLayers : interactionLayerMask.value;


    private float currentMoveSpeed = 0.0f;
    private Vector3 moveDirection = Vector3.zero;
    private float lookAngle = 0.0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();

        moveAction = InputSystem.actions.FindAction("Move");
        sprintAction = InputSystem.actions.FindAction("Sprint");

        jumpAction = InputSystem.actions.FindAction("Jump");
        jumpAction.started += Jumped;

        interactAction = InputSystem.actions.FindAction("Interact");
        if (interactionSlider == null)
            Debug.LogError("Configure o slider de interação no PlayerController.", this);
        else
        {
            interactionSlider.value = 0f;
            interactionSlider.gameObject.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentMoveSpeed = walkSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        if (!controlsEnabled)
            return;

        Vector2 moveVector = moveAction.ReadValue<Vector2>();
        Vector2 mouseDelta = new Vector2(Mouse.current.delta.x.ReadValue(), Mouse.current.delta.y.ReadValue());

        if(!characterController.isGrounded)
            jumped = false;

        currentMoveSpeed = sprintAction.IsPressed() ? runSpeed : walkSpeed;

        HandleLookings(mouseDelta);
        HandleMovement(moveVector);
        UpdateSelection(); 
        UpdateInteraction();
    }

    private void HandleMovement(Vector2 moveVector)
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        float oldY = moveDirection.y;

        Vector2 newSpeed = new Vector2(moveVector.y * currentMoveSpeed, moveVector.x * currentMoveSpeed);

        moveDirection = (forward * newSpeed.x) + (right * newSpeed.y);
        moveDirection.y = (jumped && characterController.isGrounded) ? jumpForce : oldY;


        if(!characterController.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private void Jumped(InputAction.CallbackContext context)
    {
        if (controlsEnabled)
            jumped = true;
    }

    public void SetControlsEnabled(bool isEnabled)
    {
        controlsEnabled = isEnabled;
        if (!isEnabled)
        {
            moveDirection = Vector3.zero;
            jumped = false;
            ResetInteractionProgress();

            if (currentTarget != null)
                currentTarget.SetHighlighted(false);
            currentTarget = null;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    private void HandleLookings(Vector2 mouseDelta)
    {
        lookAngle += -mouseDelta.y * lookSensitivity;
        lookAngle = Mathf.Clamp(lookAngle, -lookAngleLimit, lookAngleLimit);

        mainCamera.transform.localRotation = Quaternion.Euler(lookAngle, 0.0f, 0.0f);
        transform.rotation *= Quaternion.Euler(0.0f, mouseDelta.x * lookSensitivity, 0.0f);
    }
    private void UpdateSelection()
    {
        OutlineHighlight nextTarget = null; 
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, EffectiveInteractionLayerMask))
            nextTarget = hit.collider.GetComponentInParent<OutlineHighlight>();

        IInteractable nextInteractable = GetInteractable(nextTarget);
        GameController controller = GameController.Instance;
        if (nextTarget != null && controller != null && !controller.CanInteract(nextInteractable))
            nextTarget = null;

        if (nextTarget == currentTarget)
            return;

        if (currentTarget != null)
            currentTarget.SetHighlighted(false);

        currentTarget = nextTarget;

        if (currentTarget != null)
            currentTarget.SetHighlighted(true);
    }

    private void UpdateInteraction()
    {
        if (interactionSlider == null)
            return;

        if (!interactAction.IsPressed() || currentTarget == null)
        {
            ResetInteractionProgress();
            return;
        }

        IInteractable interactable = GetInteractable(currentTarget);
        GameController controller = GameController.Instance;
        if (interactable == null || (controller != null && !controller.CanInteract(interactable)))
        {
            ResetInteractionProgress();
            return;
        }

        if (progressTarget != currentTarget)
        {
            ResetInteractionProgress();
            progressTarget = currentTarget;
        }

        if (interactionCompleted)
            return;

        interactionSlider.gameObject.SetActive(true);
        interactionProgress += Time.deltaTime;
        interactionSlider.value = Mathf.Clamp01(interactionProgress / Mathf.Max(0.01f, interactionDuration));

        if (interactionSlider.value < 1f)
            return;

        interactionCompleted = true;
        interactable.Interact();
    }

    private void ResetInteractionProgress()
    {
        interactionProgress = 0f;
        progressTarget = null;
        interactionCompleted = false;

        if (interactionSlider == null)
            return;

        interactionSlider.value = 0f;
        interactionSlider.gameObject.SetActive(false);
    }

    private static IInteractable GetInteractable(OutlineHighlight target)
    {
        if (target == null)
            return null;

        IInteractable interactable = target.GetComponentInParent<IInteractable>();
        if (interactable == null)
            interactable = target.GetComponentInChildren<IInteractable>();

        return interactable;
    }

    public void TeleportTo(Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("Não é possível teleportar o jogador para um spawn nulo.", this);
            return;
        }

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (currentTarget != null)
            currentTarget.SetHighlighted(false);
        currentTarget = null;

        bool wasEnabled = characterController != null && characterController.enabled;
        if (wasEnabled)
            characterController.enabled = false;

        transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        moveDirection = Vector3.zero;
        jumped = false;

        if (wasEnabled)
            characterController.enabled = true;
    }

    private void OnDestroy()
    {
        if (jumpAction != null)
            jumpAction.started -= Jumped;
    }
}
