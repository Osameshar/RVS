using UnityEngine;
using System.Collections;

public class CharacterControllerLogic : MonoBehaviour
{
	//inspector serialized
	[SerializeField]
	private Animator animator;
	[SerializeField]
	private float directionDampTime = .25f;
	[SerializeField]
	private float speedDampTime = .05f;
	[SerializeField]
	private ThirdPersonCamera gamecam;
	[SerializeField]
	private float directionSpeed = 1.5f;
	[SerializeField]
	private float rotationDegreePerSecond = 120f;
	[SerializeField]
	private float fovDampTime = 3f;
	private float jumpMultiplier = 1f;
	[SerializeField]
	private CapsuleCollider capCollider;
	[SerializeField]
	private float jumpDist = 1f;
	
	//Private global only
	private float speed = 0.0f;
	private float direction = 0f;
	private float horizontal = 0.0f;
	private float vertical = 0.0f;
	private float charAngle = 0f;
	private AnimatorTransitionInfo transInfo;
	private AnimatorStateInfo stateInfo;
	
	//hashes
	private int m_LocomotionID = 0;
	private int m_LocomotionPivotLId = 0;
	private int m_LocomotionPivotRId = 0;	
	private int m_LocomotionPivotLTransId = 0;	
	private int m_LocomotionPivotRTransId = 0;	
	
	private const float SPRINT_SPEED = 2.0f;	
	private const float SPRINT_FOV = 75.0f;
	private const float NORMAL_FOV = 60.0f;
	private float capsuleHeight;
	
	public Animator Animator
	{
		get
		{
			return this.animator;
		}
	}
	public float Speed
	{
		get
		{
			return this.speed;
		}
	}
	public float LocomotionThreshold{ get {return 0.2f;}}
	
	// Use this for initialization
	void Start ()
	{
		animator = GetComponent<Animator> ();
		
		if (animator.layerCount >= 2) 
		{
			animator.SetLayerWeight (1, 1);
		}
		
		capCollider = GetComponent<CapsuleCollider>();
		capsuleHeight = capCollider.height;
		//Hash all animation names for performance.
		m_LocomotionID = Animator.StringToHash ("Base Layer.Locomotion");
		m_LocomotionPivotLId = Animator.StringToHash("Base Layer.LocomotionPivotL");
		m_LocomotionPivotRId = Animator.StringToHash("Base Layer.LocomotionPivotR");
		m_LocomotionPivotLTransId = Animator.StringToHash("Base Layer.Locomotion -> Base Layer.LocomotionPivotL");
		m_LocomotionPivotRTransId = Animator.StringToHash("Base Layer.Locomotion -> Base Layer.LocomotionPivotR");
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (animator && gamecam.CamState != ThirdPersonCamera.CamStates.FirstPerson)
		{
			stateInfo = animator.GetCurrentAnimatorStateInfo(0);
			transInfo = animator.GetAnimatorTransitionInfo(0);
			
			// Press A to jump
			if (Input.GetButton("Jump"))
			{
				animator.SetBool("Jump", true);
			}
			else
			{
				animator.SetBool("Jump", false);
			}	
			
			horizontal = Input.GetAxis ("Horizontal");
			vertical = Input.GetAxis ("Vertical");
			
			charAngle = 0f;
			direction = 0f;
			float charSpeed = 0f;
			
			StickToWorldspace(this.transform,gamecam.transform, ref direction, ref charSpeed, ref charAngle, IsInPivot());
			
			// Press B to sprint
			if (Input.GetButton("Sprint"))
			{
				speed = Mathf.Lerp(speed, SPRINT_SPEED, Time.deltaTime);
				gamecam.camera.fieldOfView = Mathf.Lerp(gamecam.camera.fieldOfView, SPRINT_FOV, fovDampTime * Time.deltaTime);
			}
			else
			{
				speed = charSpeed;
				gamecam.camera.fieldOfView = Mathf.Lerp(gamecam.camera.fieldOfView, NORMAL_FOV, fovDampTime * Time.deltaTime);		
			}

			animator.SetFloat("Speed", speed, speedDampTime, Time.deltaTime);
			animator.SetFloat("Direction", direction, directionDampTime, Time.deltaTime);
			
			if (speed > LocomotionThreshold)	// Dead zone
			{
				if (!IsInPivot())
				{
					Animator.SetFloat("Angle", charAngle);
				}
			}
			if (speed < LocomotionThreshold && Mathf.Abs(horizontal) < 0.05f)    // Dead zone
			{
				animator.SetFloat("Direction", 0f);
				animator.SetFloat("Angle", 0f);
			}		
		}
	}
	
	void FixedUpdate()
	{
		if (IsInLocomotion () && ((direction >= 0 && horizontal >= 0) || (direction < 0 && horizontal < 0)))
		{
			Vector3 rotationAmount = Vector3.Lerp(Vector3.zero, new Vector3(0f, rotationDegreePerSecond * (horizontal < 0f ? -1f : 1f), 0f), Mathf.Abs (horizontal));
			Quaternion deltaRotation = Quaternion.Euler(rotationAmount * Time.deltaTime);
			this.transform.rotation = (this.transform.rotation * deltaRotation);
		}
		
		if (IsInJump())
		{
			float oldY = transform.position.y;
			transform.Translate(Vector3.up * jumpMultiplier * animator.GetFloat("JumpCurve"));
			if (IsInLocomotionJump())
			{
				transform.Translate(Vector3.forward * Time.deltaTime * jumpDist);
			}
			capCollider.height = capsuleHeight + (animator.GetFloat("CapsuleCurve") * 0.5f);
			if (gamecam.CamState != ThirdPersonCamera.CamStates.Free)
			{
				gamecam.ParentRig.Translate(Vector3.up * (transform.position.y - oldY));
			}
		}
	}
	public void StickToWorldspace (Transform root, Transform camera, ref float directionOut, ref float speedOut, ref float angleOut, bool isPivoting)
	{
		Vector3 rootDirection = root.forward;
		
		Vector3 stickDirection = new Vector3 (horizontal, 0, vertical);
		
		speedOut = stickDirection.sqrMagnitude;
		
		Vector3 CameraDirection = camera.forward;
		CameraDirection.y = 0.0f;
		
		Quaternion referentialShift = Quaternion.FromToRotation (Vector3.forward, CameraDirection);
		
		Vector3 moveDirection = referentialShift * stickDirection;
		Vector3 axisSign = Vector3.Cross (moveDirection, rootDirection);
		float angleRootToMove = Vector3.Angle (rootDirection, moveDirection) * (axisSign.y >= 0 ? -1f : 1f);
		
		if (!isPivoting) 
		{
			angleOut = angleRootToMove;
		}
		angleRootToMove /= 180f;
		
		directionOut = angleRootToMove * directionSpeed;
		
	}
	public bool IsInJump()
	{
		return (IsInIdleJump() || IsInLocomotionJump());
	}
	
	public bool IsInIdleJump()
	{
		return animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.IdleJump");
	}
	public bool IsInLocomotionJump()
	{
		return animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.LocomotionJump");
	}
	
	public bool IsInLocomotion()
	{
		return stateInfo.nameHash == m_LocomotionID;
	}
	public bool IsInPivot()
	{
		return stateInfo.nameHash == m_LocomotionPivotLId || 
				stateInfo.nameHash == m_LocomotionPivotRId || 
				transInfo.nameHash == m_LocomotionPivotLTransId || 
				transInfo.nameHash == m_LocomotionPivotRTransId;
	}
}
