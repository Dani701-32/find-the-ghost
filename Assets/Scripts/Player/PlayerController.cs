using UnityEngine;
using UnityEngine.InputSystem; 


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


    private Camera mainCamera;
    private CharacterController characterController;


    private InputAction moveAction;
    private InputAction sprintAction;

    private InputAction jumpAction;
    private bool jumped = false;

    private InputAction interactAction;
    private OutlineHighlight currentTarget;


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
        interactAction.started += Interacted;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentMoveSpeed = walkSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 moveVector = moveAction.ReadValue<Vector2>();
        Vector2 mouseDelta = new Vector2(Mouse.current.delta.x.ReadValue(), Mouse.current.delta.y.ReadValue());

        if(!characterController.isGrounded)
            jumped = false;

        currentMoveSpeed = sprintAction.IsPressed() ? runSpeed : walkSpeed;

        HandleLookings(mouseDelta);
        HandleMovement(moveVector);
        UpdateSelection(); 
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
        jumped = true;
        
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
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionLayerMask))
            nextTarget = hit.collider.GetComponentInParent<OutlineHighlight>();

        if (nextTarget == currentTarget)
            return;

        if (currentTarget != null)
            currentTarget.SetHighlighted(false);

        currentTarget = nextTarget;

        if (currentTarget != null)
            currentTarget.SetHighlighted(true);
    }

    private void Interacted(InputAction.CallbackContext context)
    {
        if (currentTarget == null)
        return;

        IInteractable interactable = currentTarget.GetComponentInParent<IInteractable>();

        interactable?.Interact();
    }

    private void OnDestroy()
    {
        if(interactAction != null)
            interactAction.started -= Interacted;
    }
}
