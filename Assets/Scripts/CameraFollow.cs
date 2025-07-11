using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Mode Settings")]
    [Tooltip("The target Transform to follow (your player)")]
    public Transform target;

    [Tooltip("How quickly the camera catches up to the target")]
    public float smoothTime = 0.0f;

    [Tooltip("Offset from the target's position")]
    public Vector3 offset = new Vector3(0f, 1f, -5f);

    [Header("Free Camera Settings")]
    [Tooltip("Movement speed when in free camera mode")]
    public float freeCamMoveSpeed = 10f;
    
    [Tooltip("Rotation speed when in free camera mode")]
    public float freeCamRotateSpeed = 2f;
    
    [Tooltip("How fast the camera moves up/down with Q/E")]
    public float freeCamVerticalSpeed = 8f;
    
    [Tooltip("Mouse sensitivity for free camera rotation")]
    public float mouseSensitivity = 100f;
    
    [Tooltip("Enable 3D perspective when in free camera mode")]
    public bool enable3DPerspective = true;
    
    [Tooltip("Field of view for 3D perspective mode")]
    public float perspectiveFOV = 60f;
    
    [Tooltip("Orthographic size for 2D mode")]
    public float orthographicSize = 5f;

    private Vector3 velocity = Vector3.zero;
    private bool isFreeCameraMode = false;
    private Camera cam;
    private bool wasOrthographic;
    private float originalOrthographicSize;
    private Vector3 originalRotation;
    private Vector3 freeCamRotation;
    private bool cursorWasLocked;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("CameraFollow: No Camera component found!");
            return;
        }
        
        //  original camera settings
        wasOrthographic = cam.orthographic;
        originalOrthographicSize = cam.orthographicSize;
        originalRotation = transform.eulerAngles;
        freeCamRotation = originalRotation;
        
        
        if (orthographicSize <= 0)
            orthographicSize = cam.orthographic ? cam.orthographicSize : 5f;
    }

    void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.U))
        {
            ToggleFreeCameraMode();
        }
        
        if (isFreeCameraMode)
        {
            HandleFreeCameraInput();
        }
    }

    void LateUpdate()
    {
        if (isFreeCameraMode)
        {
            
            return;
        }
        
        if (target == null) return;

        
        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPosition, 
            ref velocity, 
            smoothTime
        );
    }
    
    private void ToggleFreeCameraMode()
    {
        isFreeCameraMode = !isFreeCameraMode;
        
        if (isFreeCameraMode)
        {
           
            Debug.Log("[CameraFollow] Entering free camera mode");
            
           
            cursorWasLocked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
           
            freeCamRotation = transform.eulerAngles;
            
            
            if (enable3DPerspective && cam != null)
            {
                cam.orthographic = false;
                cam.fieldOfView = perspectiveFOV;
            }
        }
        else
        {
           
            Debug.Log("[CameraFollow] Exiting free camera mode - returning to follow mode");
            
          
            if (!cursorWasLocked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            
           
            if (cam != null)
            {
                cam.orthographic = wasOrthographic;
                if (wasOrthographic)
                {
                    cam.orthographicSize = orthographicSize;
                }
            }
            
           
            transform.eulerAngles = originalRotation;
            
            
            velocity = Vector3.zero;
        }
    }
    
    private void HandleFreeCameraInput()
    {
        if (cam == null) return;
        
      
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        
        freeCamRotation.y += mouseX;
        freeCamRotation.x -= mouseY;
        
    
        freeCamRotation.x = Mathf.Clamp(freeCamRotation.x, -90f, 90f);
        
        transform.eulerAngles = freeCamRotation;
        
      
        float currentSpeed = freeCamMoveSpeed;
        float currentVerticalSpeed = freeCamVerticalSpeed;
        
      
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed *= 2f;
            currentVerticalSpeed *= 2f;
        }
        
      
        Vector3 moveDirection = Vector3.zero;
        
        if (Input.GetKey(KeyCode.W))
            moveDirection += transform.forward;
        if (Input.GetKey(KeyCode.S))
            moveDirection -= transform.forward;
        if (Input.GetKey(KeyCode.A))
            moveDirection -= transform.right;
        if (Input.GetKey(KeyCode.D))
            moveDirection += transform.right;
        
     
        if (Input.GetKey(KeyCode.Q))
            moveDirection -= transform.up;
        if (Input.GetKey(KeyCode.E))
            moveDirection += transform.up;
        
      
        if (moveDirection != Vector3.zero)
        {
            moveDirection.Normalize();
            
         
            bool usingVerticalMovement = Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E);
            
            if (usingVerticalMovement)
            {
               
                Vector3 horizontalMovement = new Vector3(moveDirection.x, 0, moveDirection.z);
                Vector3 verticalMovement = new Vector3(0, moveDirection.y, 0);
                
                transform.position += horizontalMovement * currentSpeed * Time.deltaTime;
                transform.position += verticalMovement * currentVerticalSpeed * Time.deltaTime;
            }
            else
            {
                
                transform.position += moveDirection * currentSpeed * Time.deltaTime;
            }
        }
        
       
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleFreeCameraMode();
        }
    }
    
    
    public bool IsFreeCameraMode()
    {
        return isFreeCameraMode;
    }
    
    
    public void SetFreeCameraMode(bool enabled)
    {
        if (isFreeCameraMode != enabled)
        {
            ToggleFreeCameraMode();
        }
    }
}
