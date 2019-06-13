using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AssemblyCSharp;

public class World : MonoBehaviour  {
	
	public GameObject worldDisplayObject;
	private static SpriteRenderer worldRenderer = null;
	private static List<Vector4> dummyInfluenceList = new List<Vector4>();

	private static readonly int precisionX = 1920;
	private static readonly int precisionY = 1080;
	public static float pixelPerUnit = 100;
	private static int numberOfMatrices = 1;

	private static List<Vector2[]> ObstPoints = new List<Vector2[]>();
	private static Vector2[][,] influenceMatrixB = new Vector2[numberOfMatrices][,];
	private static int dirtyInterator = 0;
	private static int dirtyMaxIndex = 0;
	private static List<int[]> dirtyInfluencePoints = new List<int[]>();

	private static float[][,,] influenceMatrix = new float[numberOfMatrices][,,];
	private static int currentInfluenceMatrix = 0;

	void Start () {
		//influenceMatrixB[0] = new Vector2[precisionX, precisionY];
		influenceMatrix[0] = new float[precisionX, precisionY,2];
		fillObstaclePoints ();
		worldRenderer = worldDisplayObject.GetComponent<SpriteRenderer> ();
		if (dummyInfluenceList.Count < 1) {
			for (int i = 0; i < 1023; i++) {
				dummyInfluenceList.Add (new Vector4());
			}
		}

	}

	// Update is called once per frame
	void FixedUpdate () {
		//setReadyNextInfluentMatrix ();
	}
	private static void cleanInfluencePoint(int[] dirtyPoint){
		influenceMatrix [currentInfluenceMatrix] [dirtyPoint[0], dirtyPoint[1], 0] = 0;
		influenceMatrix [currentInfluenceMatrix] [dirtyPoint[0], dirtyPoint[1], 1] = 0; 
	}

	public static void setReadyNextInfluentMatrix (){
		//currentInfluenceMatrix = (currentInfluenceMatrix + 1) % numberOfMatrices;
		dirtyInfluencePoints.ForEach( dirtyPoint => cleanInfluencePoint(dirtyPoint) );

		//sendDataToGPU();
		dirtyInterator = 0;
	}

	private static void sendDataToGPU(){
		for (int i = 0; i < 100; i++) {
			worldRenderer.material.SetVectorArray("_testArray"+i,dummyInfluenceList);
		}
	}

	public static void addInfluence(Vector2 position, Vector2 direction) {
		
		int X = (int)(precisionX/2) + (int)(position.x * pixelPerUnit);
		int Y = (int)(precisionY/2) + (int)(position.y * pixelPerUnit);

		influenceMatrix [currentInfluenceMatrix][X, Y, 0] += direction.x;
		influenceMatrix [currentInfluenceMatrix][X, Y, 1] += direction.y;

		if (dirtyInterator <  dirtyMaxIndex ) {
			dirtyInfluencePoints [dirtyInterator] [0] = X;
			dirtyInfluencePoints [dirtyInterator] [1] = Y;
		} else {
			dirtyInfluencePoints.Add (new int[2]{ X, Y });
			dirtyMaxIndex++;
		}
		dirtyInterator++;
	}

	public static Vector2 getWorldInfluenceInArea(Vector2 position){
		Vector2 influenceSum = new Vector2 ();
		int dispersion = 10;
		int X = (int)(precisionX/2) + (int)(position.x * pixelPerUnit);
		int Y = (int)(precisionY/2) + (int)(position.y * pixelPerUnit);

		for (int x1 = -1 * dispersion; x1 <= dispersion; x1++) {
			for (int y1 = -1 * dispersion; y1 <= dispersion; y1++) {
				//influenceSum = influenceSum + influenceMatrixB [currentInfluenceMatrix] [X + x1, Y + y1];
				influenceSum.x = influenceSum.x + influenceMatrix [currentInfluenceMatrix] [X + x1, Y + y1 , 0];
				influenceSum.y = influenceSum.y + influenceMatrix [currentInfluenceMatrix] [X + x1, Y + y1 , 1];
			}
		}
		return influenceSum;
	}

	private static void fillObstaclePoints(){ 
		if (ObstPoints.Count > 0) {
			return;
		}
		GameObject[] obstacles = GameObject.FindGameObjectsWithTag ("Obstacle");
	
		for (int i =  obstacles.Length - 1; i >= 0; i--) {
			GameObject currentObstacle = obstacles [i];
			PolygonCollider2D obstacleCollider =  currentObstacle.GetComponent<PolygonCollider2D> ();
			Vector2[] points = obstacleCollider.points;
			int pointsCuantity = points.Length;
			for (int j =  pointsCuantity - 1; j >= 0; j--) {
				Vector2[] obst = new Vector2[2];
				obst [0] = Trigonometrics.localPointToGlobal (currentObstacle, points [j]);
				obst [1] = Trigonometrics.localPointToGlobal (currentObstacle, points [(j + 1) % pointsCuantity]);
				ObstPoints.Add (obst);	
			}
		}
	}
		

	public static SubPathWave findNextSubPath (SubPathWave subPath){
		fillObstaclePoints ();
		float collisionDistance = 9000;
		Vector2[] collisionObstaclePoints = new Vector2[2];
		Vector2 collisionPosition = new Vector2 ();
		for (int i =  ObstPoints.Count - 1; i >= 0; i--) {
			Vector2[] points = ObstPoints[i];	
			if (Trigonometrics.areParallels(subPath.velocity , points[1]-points[0])) {
				continue;
			}
	
			Vector2 intersection = Trigonometrics.linesIntersection (subPath.startPosition,subPath.velocity,points[0],points[1]-points[0]);
			float distance = (subPath.startPosition - intersection).magnitude;

			if (distance > 0.05f && distance < collisionDistance &&
			Trigonometrics.pointIsInSemiSegment (subPath.startPosition, subPath.velocity, intersection) &&
			Trigonometrics.pointIsInSegment (points[0], points[1], intersection)) {
				collisionObstaclePoints = points;
				collisionDistance = distance;
				collisionPosition = intersection;
			}
		}
		if (collisionDistance == 9000) {
			return null;
		} else {
			Vector2 obstacleVector = collisionObstaclePoints[1]-collisionObstaclePoints[0];
			Vector2 newVelocity = Vector2.Reflect (subPath.velocity, new Vector2 (-1 * obstacleVector.y, obstacleVector.x).normalized);
			float newTime = (collisionDistance / subPath.velocity.magnitude ) + subPath.startTime;
			return new SubPathWave (collisionPosition,newVelocity,newTime);
		}
	}
}