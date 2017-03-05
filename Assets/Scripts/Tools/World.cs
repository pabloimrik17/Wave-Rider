using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AssemblyCSharp;

public class World : MonoBehaviour  {
	private static Vector2 precision = new Vector2(1024,600);
	private static int numberOfMatrices = 5;

	private static List<Vector2[]> ObstPoints = new List<Vector2[]>();
	private static Vector2[][,] influenceMatrix = new Vector2[numberOfMatrices][,];
	private static int currentInfluenceMatrix = 0;

	void Start () {
		influenceMatrix[0] = new Vector2[(int)precision.x, (int)precision.y];
		fillObstaclePoints ();
	}

	// Update is called once per frame
	void FixedUpdate () {
		//setReadyNextInfluentMatrix ();
	}

	public static void setReadyNextInfluentMatrix (){
		currentInfluenceMatrix = (currentInfluenceMatrix + 1) % numberOfMatrices;
		influenceMatrix[currentInfluenceMatrix] = new Vector2[(int)precision.x, (int)precision.y];
	}

	public static void addInfluence(Vector2 position, Vector2 direction) {
		int X = (int)(precision.x/2) + (int)(position.x * 50);
		int Y = (int)(precision.y/2) + (int)(position.y * 50);
		influenceMatrix [currentInfluenceMatrix][X, Y] = influenceMatrix [currentInfluenceMatrix] [X, Y] + direction;
	}

	public static Vector2 getWorldInfluenceInArea(Vector2 position){
		Vector2 influenceSum = new Vector2 ();
		int dispersion = 10;
		int X = (int)(precision.x/2) + (int)(position.x * 50);
		int Y = (int)(precision.y/2) + (int)(position.y * 50);

		for (int x1 = -1 * dispersion; x1 <= dispersion; x1++) {
			for (int y1 = -1 * dispersion; y1 <= dispersion; y1++) {
				influenceSum = influenceSum + influenceMatrix [currentInfluenceMatrix] [X + x1, Y + y1];
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
			Vector2 intersection = Trigonometrics.linesIntersection (subPath.startPosition,subPath.velocity,points[0],points[1]-points[0]);
			float distance = (subPath.startPosition - intersection).magnitude;

			if (distance < collisionDistance &&
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