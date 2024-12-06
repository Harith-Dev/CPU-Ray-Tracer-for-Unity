using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;




public class FreeLook : MonoBehaviour
{
    public RayTracingCamera rayTracingCamera;
    [Header("Move & Looking")]
    public bool MoveWithOutUpdate;
    public float MoveSpeed = 2.5f;
    public float SpeedLook = 2;
    float rotationX;
    float rotationY;
    public bool IsMoving;
    public bool Zooming;
    
    

    void Start()
    {
        rayTracingCamera = GetComponent<RayTracingCamera>();
    }

    
    void Update()
    {

        Vector3 LastP = transform.position;
        Quaternion LastR = transform.rotation;
        
        Vector3 movement = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) // Move forward
        {
            movement += Vector3.forward;
        }
        if (Input.GetKey(KeyCode.S)) // Move backward
        {
            movement += Vector3.back;
        }
        if (Input.GetKey(KeyCode.A)) // Move left
        {
            movement += Vector3.left;
        }
        if (Input.GetKey(KeyCode.D)) // Move right
        {
            movement += Vector3.right;
        }
        if (Input.GetKey(KeyCode.E)) // Move forward
        {
            movement += Vector3.up;
        }
        if (Input.GetKey(KeyCode.Q)) // Move backward
        {
            movement -= Vector3.up;
        }
        float MulplerSpeed = Input.GetKey(KeyCode.LeftShift) ? 2 : 1;
        transform.Translate(movement * (MoveSpeed * MulplerSpeed) );
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * SpeedLook;
            float mouseY = Input.GetAxis("Mouse Y") * SpeedLook;

            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);
            rotationY += mouseX * SpeedLook;
            Camera.main.transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
        }
        //Zooming
        if (Zooming)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                if (scroll > 0f)
                {
                    Camera.main.fieldOfView -= 3;
                }
                else if (scroll < 0f)
                {
                    Camera.main.fieldOfView += 3;
                }
            }
        }
        if (LastP != transform.position|| LastR != transform.rotation)
        {
            Debug.Log("Ismove");
            IsMoving = true;
            if (!MoveWithOutUpdate)
            {
                rayTracingCamera.Refrash = true;
                rayTracingCamera.StaticScene = false;
                rayTracingCamera.AccumulatedFrames = 1;
            }
            
            
        }
        else
        {
            IsMoving = false;
          
            rayTracingCamera.StaticScene = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            rayTracingCamera.Refrash = true;
            rayTracingCamera.StaticScene = false;
        }



    }


}
