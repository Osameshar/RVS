using UnityEngine;
using System.Collections;

struct CameraPosition
{
	private Vector3 position;
	private Transform xForm;
	
	public Vector3 Position { get { return position; } set { position = value; } }
	public Transform XForm { get { return xForm; } set { xForm = value; } }
	
	public void Init (string camName, Vector3 pos, Transform transform, Transform parent)
	{
		position = pos;
		xForm = transform;
		xForm.name = camName;
		xForm.parent = parent;
		xForm.localPosition = Vector3.zero;
		xForm.localPosition = position;
	}
}


[RequireComponent (typeof (BarsEffect))]
public class ThirdPersonCamera : MonoBehaviour {

	[SerializeField]
	private Transform cameraXform;
	[SerializeField]
	private Transform parentRig;
	[SerializeField]
	private float distanceAway;
	[SerializeField]
	private float distanceUp;
	[SerializeField]
	private float smooth;
	[SerializeField]
	private Transform followXform;
	[SerializeField]
	private float widescreen = 0.2f;
	[SerializeField]
	private float targetingTime = 0.5f;
	[SerializeField]
	private float firstPersonThreshold = 0.5f;
	[SerializeField]
	private CharacterControllerLogic follow;
	[SerializeField]
	private Vector2 firstPersonXAxisClamp = new Vector2 (-70.0f, 90.0f);
	[SerializeField]
	private float firstPersonLookSpeed = 1.5f;
	[SerializeField]
	private float fPSRotationDegreePerSecond = 120f;
	
	
	private Vector3 lookDir;
	private Vector3 curLookDir;
	private Vector3 targetPosition;
	private BarsEffect barEffect;
	private CamStates camState = CamStates.Behind;
	private float xAxisRot = 0.0f;
	private CameraPosition firstPersonCamPos;
	private float lookWeight;
	private float TARGETING_THRESHOLD = 0.01f;
	float rightX;
	float rightY;
	
	private Vector3 velocityCamSmooth = Vector3.zero;
	[SerializeField]
	private float camSmoothDampTime = 0.1f;
	private Vector3 velocityLookDir = Vector3.zero;
	[SerializeField]
	private float lookDirDampTime = 0.1f;
	// Use this for initialization

	public Transform CameraXform
	{
		get
		{
			return this.cameraXform;
		}
	}
	public Transform ParentRig
	{
		get
		{
			return this.parentRig;
		}
	}
	public CamStates CamState
	{
		get
		{
			return this.camState;
		}
	}
	public enum CamStates
	{
		Behind,
		FirstPerson,
		Target,
		Free
	}
	void Start () 
	{
		cameraXform = this.transform;//.parent;
		if (cameraXform == null)
		{
			Debug.LogError("Parent camera to empty GameObject.", this);
		}

		follow = GameObject.FindWithTag("Player").GetComponent<CharacterControllerLogic>();
		followXform = GameObject.FindWithTag ("Player").transform;
		lookDir = followXform.forward;
		curLookDir = followXform.forward;
		rightX = 0f;
		rightY = 0f;
		barEffect = GetComponent<BarsEffect> ();
		if (barEffect == null)
		{
			Debug.LogError("Attach a widescreen BarsEffect script to the camera.",this);
		}
	}
	
	// Update is called once per frame
	void Update () 
	{


	}
	
	void LateUpdate()
	{

		if (Input.GetAxis ("RightStickX") != 0)
			rightX = Input.GetAxis ("RightStickX");
		else
			rightX = 0;
		if (Input.GetAxis ("RightStickY") != 0 )
			rightY = Input.GetAxis ("RightStickY");
		else
			rightY = 0;
		//rightY += Input.GetAxis ("RightStickY");
		
		
		Vector3 characterOffset = followXform.position + new Vector3(0f, distanceUp, 0f);
		Vector3 lookAt = characterOffset;
		targetPosition = Vector3.zero;

		curLookDir = characterOffset - cameraXform.position;
		curLookDir.y = 0;
		Debug.DrawRay (cameraXform.position, curLookDir, Color.green);
 
			rightY = ClampAngle (rightY, -180f, 180f);
			rightX = ClampAngle (rightX, -360f, 360f);

			Quaternion rotation  = Quaternion.Euler(rightY,rightX,0f);
			transform.rotation = rotation;			
			
			targetPosition = characterOffset + followXform.up * distanceUp - Vector3.Normalize(curLookDir) * distanceAway;
			
			Vector3 position = rotation * targetPosition;
			
			cameraXform.RotateAround(characterOffset, followXform.up, rightX);
			cameraXform.RotateAround(characterOffset, followXform.right, rightY);
			//Debug.DrawLine(followXform.position, targetPosition, Color.magenta);
			

		CompensateForWalls (characterOffset, ref targetPosition);
		//transform.position = position;
		smoothPosition(cameraXform.position, targetPosition);
		transform.LookAt(lookAt);
	}
	private float ClampAngle (float angle, float min, float max) {
		if (angle < -360)
			angle += 360;
		if (angle > 360)
			angle -= 360;
		return Mathf.Clamp (angle, min, max);
	}
	private void smoothPosition(Vector3 fromPos, Vector3 toPos)
	{
		cameraXform.position = Vector3.SmoothDamp (fromPos, toPos, ref velocityCamSmooth, camSmoothDampTime);
	}
	
	private void CompensateForWalls(Vector3 fromObject, ref Vector3 toTarget)
	{
		Debug.DrawLine (fromObject, toTarget, Color.cyan);
		RaycastHit wallHit = new RaycastHit ();
		if (Physics.Linecast (fromObject, toTarget, out wallHit))
		{
			Debug.DrawRay(wallHit.point, Vector3.left, Color.red);
			toTarget = new Vector3(wallHit.point.x, toTarget.y, wallHit.point.z);
		}
	}
	
	private void ResetCamera()
	{
		lookWeight = Mathf.Lerp(lookWeight, 0.0f, Time.deltaTime * firstPersonLookSpeed);
		transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime);
	}
	
	
}
