using System;
using System.Collections.Generic;
using UnityEngine;
using AssemblyCSharp;

namespace AssemblyCSharp
{
	public class ObstacleWall
	{
		private PolygonCollider2D collider = null;
		public GameObject gameObject = null;
		private WaveSector waveSector = null;
		private Vector2 pointA = Vector2.zero;
		private Vector2 pointB = Vector2.zero;
		private Vector2 globalA = Vector2.zero;
		private Vector2 globalB = Vector2.zero;

		private Vector2 pointAfromWavePerspective = Vector2.zero;
		private Vector2 pointBfromWavePerspective = Vector2.zero;

		public ObstacleWall (PolygonCollider2D parentCollider)
		{
			this.collider = parentCollider;
			this.gameObject = parentCollider.gameObject; 
			this.pointA = collider.points [1] ;
			this.pointB = collider.points [0] ;
			this.globalA = Trigonometrics.localPointToGlobal (gameObject, pointA);
			this.globalB = Trigonometrics.localPointToGlobal (gameObject, pointB);
		}

		public void defineAproachigWave(WaveSector collidingWaveSector){
			waveSector = collidingWaveSector;
			pointAfromWavePerspective = Trigonometrics.globalPointToLocal (waveSector.gameObject, globalA);
			pointBfromWavePerspective = Trigonometrics.globalPointToLocal (waveSector.gameObject, globalB);
			Debug.DrawLine (globalA,globalB,Color.yellow);
		}

		public bool isLeftExceedByWave(){
		float cornerAngle = getLeftBound();
	
		return  -1* waveSector.pointsDispersionAngle < cornerAngle;
		}

		public bool isRightExceedByWave(){
		float cornerAngle = getRightBound();
	

		return ( waveSector.pointsDispersionAngle) > cornerAngle;
		}

		public float getLeftBound(){
			return -1 * waveSector.getCircunferenceAngle(pointAfromWavePerspective)* Mathf.Deg2Rad;
		}
		public float getRightBound(){
			return -1 * waveSector.getCircunferenceAngle(pointBfromWavePerspective)* Mathf.Deg2Rad;
		}

		public float getDistanceFromCircunferenceToWall(float angle){
			Vector2 vectorAB = pointAfromWavePerspective - pointBfromWavePerspective;
			Vector2 noarmalAB = new Vector2 (vectorAB.y, -1 * vectorAB.x);

			Vector2 angleVector = new Vector2(Mathf.Sin(angle),Mathf.Cos(angle));
			Vector2 intersectionPoint = Trigonometrics.linesIntersection (pointAfromWavePerspective, vectorAB, waveSector.circunferenceCenter, angleVector);
			return (intersectionPoint - waveSector.circunferenceCenter).magnitude - waveSector.circunferenceRadius; 
		}

		public float firstImpactAngle(){
			Vector2 vectorAB = pointAfromWavePerspective - pointBfromWavePerspective;
			Vector2 normalAB = new Vector2 (vectorAB.y, -1 * vectorAB.x); 	
			Vector2 impactPoint = Trigonometrics.linesIntersection (pointAfromWavePerspective, vectorAB, waveSector.circunferenceCenter, normalAB);

			float normalImpactAngle = -1* Mathf.Deg2Rad * waveSector.getCircunferenceAngle(impactPoint);

			if (normalImpactAngle < getLeftBound ()) {
				return getLeftBound ();
			} else if (normalImpactAngle > getRightBound ()) {
				return getRightBound ();
			} else {
				return normalImpactAngle;
			}
		}

		public Vector3 getMirrorPosition(Vector3 position){
			Vector2 position2D = new Vector2 (position.x, position.y);
			Vector2 vectorAB = globalA - globalB;
			Vector2 normalAB = new Vector2 (vectorAB.y, -1 * vectorAB.x);
			Vector2 intersectionPoint = Trigonometrics.linesIntersection (globalA, vectorAB, position2D, normalAB);
			Vector2 vectorPositionToIntersection = intersectionPoint - position2D;
			Vector2 mirrorPosition = position2D + 2 * vectorPositionToIntersection;
			return new Vector3 (mirrorPosition.x, mirrorPosition.y, 0);
		}

		public Quaternion getMirrorRotation(Quaternion rotation){
			float degreeAngle = (rotation.eulerAngles.z - 90) * Mathf.Deg2Rad;
			Vector2 movementVector = new Vector2 (-1 * Mathf.Cos(degreeAngle), -1 * Mathf.Sin(degreeAngle));

			Vector2 vectorAB = globalA - globalB;
			Vector2 normalAB = new Vector2 (vectorAB.y, -1 * vectorAB.x);

			Vector2 mirrorDirection = Vector2.Reflect (movementVector.normalized,normalAB.normalized);

			float newAngle = Mathf.Atan2 (mirrorDirection.y, mirrorDirection.x);
			return Quaternion.Euler(0,0,newAngle * Mathf.Rad2Deg - 90);
		}

		

	}
}

