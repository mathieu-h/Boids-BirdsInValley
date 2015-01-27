using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MoveBoids2 : MonoBehaviour {


	/*
	 * 1410-1-1-100-1-8 => small gravitation
	 * 1-1-1-100-1-8 => big gravition
	 * 1-1-100-100-5-8 => avancée en gardant une distance
	 * 1-1-100-100-10-8 => avancée en gardant une distance mais plus grande
	 * 1-1-100-100-2-8 => best config avancée avec distance
	 * 3-1-1-100-1-8 (avec 100 boids) => avancée dans une direction mais en allant vers le centre quand meme
	 * 1-1-1-100-1-8 => avancée dans direction mais allant vers le centre (léger) =====> COOL CONFIG (mais ils s'alignent légerement)
	 * 1-1-1-100-2-8 => avancée dans direction mais allant vers le centre (léger) et plus de 
	 *  keep distance=====> COOL CONFIG (mais ils s'alignent légerement)
	 * 1-1-2-100-2-8 => avancée++ dans direction mais allant vers le centre (léger) et plus de 
	 *  keep distance=====> COOL CONFIG (mais ils s'alignent légerement)
	 * 1-1-2-100-2-8 0.5/0.7 vitesse
	 * Bien désactiver la gestion d'évitement quand ils ne font pas des voyages sinon ils tournent en cercle
	 */

	// 2 meshes for animation
	/*
	[SerializeField]
	public Mesh boids_mesh_open;
	[SerializeField]
	public Mesh boids_mesh_closed;
	private bool mesh_state;
	*/ 

	[SerializeField]
	public LayerMask terrain_layer;

	[SerializeField]
	public int boids_number;
	[SerializeField]
	public Transform boids_prefab;
	[SerializeField]
	private float factor_rule1;
	[SerializeField]
	private float factor_rule2;
	[SerializeField]
	private float factor_rule3;

	[SerializeField] /*100*/
	public float constant_rule1;
	[SerializeField] /*100*/
	public float constant_rule2;
	[SerializeField] /*8*/
	public float constant_rule3;

	[SerializeField]
	public float vlim;
	[SerializeField]
	public float vlim_tendency_to_place;
	[SerializeField]
	public int distanceToReachGoal;
	[SerializeField]
	public float factor_avoidance;
	[SerializeField]
	public float detectionDistance;
	[SerializeField]
	public GameObject panel;

	/*
	[SerializeField]
	public int numberOfWaypoints;
	[SerializeField]
	public Transform waypoint1;
	[SerializeField]
	public Transform waypoint2;
	[SerializeField]
	public Transform waypoint3;
	[SerializeField]
	public Transform waypoint4;
	[SerializeField]
	public Transform waypoint5;
	*/
	[SerializeField]
	public Transform[] waypointsArray;

	[SerializeField]
	public Slider[] slidersArray;

	[SerializeField]
	public Camera mainCamera;

	[SerializeField]
	public float camera_distance;

	public Transform[] boids_array;

	private bool settingsMenuActivated = false;
	private bool cameraFollow = false;
	private int currWayPointNumber = 1;


	// Use this for initialization
	void Start () {
		initPositions ();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		//DrawBoids ();
		UpdateBoidsPosition ();
		if(cameraFollow){
			UpdateCameraPosition ();
		}
		//press shift to move faster
		if(Input.GetKey(KeyCode.C))
		{
			MonoBehaviour[] scriptComponents = mainCamera.GetComponents<MonoBehaviour>();    
			foreach(MonoBehaviour script in scriptComponents) {
				script.enabled = false;
			}
			cameraFollow = true;
		}
		if(Input.GetKey(KeyCode.V))
		{
			MonoBehaviour[] scriptComponents = mainCamera.GetComponents<MonoBehaviour>();    
			foreach(MonoBehaviour script in scriptComponents) {
				script.enabled = true;
			}
			cameraFollow = false;
		}
	}

	public void initPositions(){
		boids_array = new Transform[boids_number];
		for (int i = 0 ; i<boids_number ; i++){
			boids_array[i] = Instantiate(boids_prefab,new Vector3((Random.value*100) -1, 18+(Random.value * 100) -1, (Random.value * 100) -1), Quaternion.identity) as Transform;
		}
	}

	public void UpdateBoidsPosition(){
		Vector3 v1, v2, v3, v4, v5;

		foreach( Transform boid in boids_array){			
			//ChangeMesh (boid);
			v1 = Rule1(boid);
			v2 = Rule2(boid);
			v3 = Rule3(boid);
			//v4 = new Vector3();
			/*
			switch(currWayPointNumber){
				case 1:
						v4 = TendToPlace(boid, waypoint1);
						break;
				case 2:
						v4 = TendToPlace(boid, waypoint2);
						break;
				case 3:
						v4 = TendToPlace(boid, waypoint3);
						break;
				case 4:
						v4 = TendToPlace(boid, waypoint4);
						break;
				case 5:
						v4 = TendToPlace(boid, waypoint5);
						break;
				default:
						v4 = TendToPlace(boid, waypoint1);
						break;
			}
			*/
			v4 = TendToPlace(boid, waypointsArray[currWayPointNumber-1]);
			v5 = AvoidElements(boid);

			//boid.rigidbody.velocity = boid.rigidbody.velocity + v1 + v2 + v3 + v4;
			boid.rigidbody.AddForce (boid.rigidbody.velocity + v1 + v2 + v3 + v4 + v5);
			this.LimitVelocity(boid);
			boid.position = boid.position + boid.rigidbody.velocity;
			boid.rotation = SimpleCalcRotation(boid.rigidbody.velocity);
		}
	}
	/*
	private void ChangeMesh(Transform boid){
		if (mesh_state) {
			boid
			mesh_state = !mesh_state;
		} else {
			boid.	
			mesh_state = !mesh_state;
		}
	}
	*/
	public void LimitVelocity(Transform boid){
		Vector3 vLimit = new Vector3(1,1,1);

		if (Vector3.Magnitude (boid.rigidbody.velocity) > vlim) {
			boid.rigidbody.velocity = (boid.rigidbody.velocity / Vector3.Magnitude(boid.rigidbody.velocity)) * vlim;
		}

	}

	public Vector3 AvoidElements(Transform boid){
		RaycastHit rHit;
		Physics.Raycast (boid.position, boid.forward,out rHit, detectionDistance, terrain_layer);
		Vector3 avoidVector = ProjectVectorOnPlane (rHit.normal, boid.forward);
		return avoidVector*factor_avoidance;
	}

	//Projects a vector onto a plane. The output is not normalized.
	private static Vector3 ProjectVectorOnPlane(Vector3 planeNormal, Vector3 vector){
		
		return vector - (Vector3.Dot(vector, planeNormal) * planeNormal);
	}

	public Vector3 Rule1(Transform boid){
		Vector3 vector_r1 = new Vector3();
		foreach( Transform boid_r1 in boids_array){
			if(boid_r1 != boid){
				vector_r1 = vector_r1 + boid_r1.position;
			}		
		}
		vector_r1 = vector_r1 / (boids_number - 1);
		vector_r1 = (vector_r1-boid.position)/constant_rule1;
		return vector_r1*factor_rule1;
	}

	public Vector3 Rule2(Transform boid){
		Vector3 vector_r2 = new Vector3();
		foreach( Transform boid_r2 in boids_array){
			if(boid_r2 != boid){
				if(Vector3.Distance(boid_r2.position, boid.position) < constant_rule2)
					vector_r2 = vector_r2 - (boid_r2.position - boid.position);
			}		
		}
		return vector_r2*factor_rule2;
	}

	public Vector3 Rule3(Transform boid){
		Vector3 vector_r3 = new Vector3();
		foreach( Transform boid_r3 in boids_array){
			//TODO faire une vraie fonction permettant de différencier deux boids
			if(boid_r3 != boid){
				vector_r3 = vector_r3 + boid_r3.rigidbody.velocity;
			}		
		}
		vector_r3 = vector_r3 / (boids_number - 1);
		vector_r3 = (vector_r3-boid.rigidbody.velocity/constant_rule3);
		return vector_r3*factor_rule3;
	}
	
	public Vector3 TendToPlace(Transform boid, Transform currentWaypoint){
		//Vector3 destination = Input.mousePosition;
		if (Vector3.Distance(currentWaypoint.position, boid.position) < distanceToReachGoal 
		   	&& currWayPointNumber < waypointsArray.Length) {
			currWayPointNumber++;
		}
		Vector3 destination = (currentWaypoint.position - boid.position) / 100;
		if (Vector3.Magnitude (destination) > vlim_tendency_to_place) {
			destination = (destination / Vector3.Magnitude(destination)) * vlim_tendency_to_place;
		}
		return destination;
	}

	public void UpdateCameraPosition(){
		Vector3 vector_r1 = new Vector3();
		foreach( Transform boid_r1 in boids_array){
			vector_r1 = vector_r1 + boid_r1.position;
		}
		vector_r1 = vector_r1 / (boids_number - 1);
		Vector3 pos = Camera.main.transform.position;
		pos = vector_r1;
		pos.z -= camera_distance;
		Debug.Log (camera_distance);
		Camera.main.transform.position = pos;
		Camera.main.transform.LookAt(vector_r1);
	}

	static Quaternion SimpleCalcRotation( Vector3 velocity )
	{
		if (velocity != new Vector3 (0, 0, 0)) {
						return Quaternion.LookRotation (velocity);
		} else {
				return Quaternion.identity;
		}
	}

	public void SliderRule1(int sliderIndex){
		factor_rule1 = slidersArray[sliderIndex].value;
	}

	public void SliderRule2(int sliderIndex){
		factor_rule2 = slidersArray[sliderIndex].value;
	}

	public void SliderRule3(int sliderIndex){
		factor_rule3 = slidersArray[sliderIndex].value;
	}

	public void SliderVlim(int sliderIndex){
		vlim = slidersArray[sliderIndex].value;
	}

	public void SliderVlimTendencyToPlace(int sliderIndex){
		vlim_tendency_to_place = slidersArray[sliderIndex].value;
	}

	public void SliderAvoidance(int sliderIndex){
		factor_avoidance =  slidersArray[sliderIndex].value;
	}

	public void SliderDetectionDistance(int sliderIndex){
		detectionDistance =  slidersArray[sliderIndex].value;
	}

	public void SliderDistanceFromOtherBirds(int sliderIndex){
		constant_rule2 =  slidersArray[sliderIndex].value;
	}

	public void SliderCameraDistance(int sliderIndex){
		Debug.Log ("COUCOU"+camera_distance);
		camera_distance =  slidersArray[sliderIndex].value;
	}

	
	public void ActivateSettingsMenu(){		
		settingsMenuActivated = !settingsMenuActivated;
		panel.SetActive (settingsMenuActivated);
	}
}
