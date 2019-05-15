using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AssemblyCSharp;
using System;
using System.Linq;

public class World : MonoBehaviour {

    public GameObject worldDisplayObject;
    private static SpriteRenderer worldRenderer = null;

    private static readonly int precisionX = 192;
    private static readonly int precisionY = 108;
    public static float pixelPerUnit = 10;
    private static float precisionMeshRatio = 5;

    private static int dirtyInterator = 0;
    private static int dirtyMaxIndex = 0;
    private static List<int[]> dirtyInfluencePoints = new List<int[]>();

    private static List<Vector2[]> ObstPoints = new List<Vector2[]>();

    private static float[,,] influenceMatrix = new float[precisionX, precisionY, 2];
    private Mesh waterMesh;
    private static Mesh fragmentMesh;
    private static Material fragmentMaterial;



    private static Vector3[] waterNormalsArray;
    private static Vector3[] waterNormalsBaseArray;


    // Propiedades para renderizado por instancias en GPU extraer clase
    private static Vector3 compensationVector = new Vector3(0, 0, -0.05f);
    private MeshFilter meshFilter;

    public Shader waveFragmentShader;
    private static List<Matrix4x4> positions = new List<Matrix4x4>();
    private static int positionIndex = 0;
    private static int positionMaxIndex = 0;
    private static int positionCurrentMaxIndex = 0;

    private static List<Matrix4x4> tempPositions = new List<Matrix4x4>();

    private static uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    void Start() {
        fillObstaclePoints();
        worldRenderer = worldDisplayObject.GetComponent<SpriteRenderer>();

        meshFilter = GetComponent<MeshFilter>();
        waterMesh = MeshCreator.Water(precisionX *10, precisionY * 10, (int)(precisionX / precisionMeshRatio), (int)(precisionY / precisionMeshRatio));
        meshFilter.mesh = waterMesh;
        waterNormalsArray = Enumerable.Repeat(Vector3.back, waterMesh.vertices.Length).ToArray<Vector3>();


        fragmentMesh = MeshCreator.Water(0.1f,0.1f,1, 1);
        fragmentMaterial = new Material(waveFragmentShader);// GetComponent<MeshRenderer>().material; //new Material(waveFragmentShader);
        fragmentMaterial.enableInstancing = true;
        fragmentMaterial.renderQueue = 2100;


    }

    // Update is called once per frame
    void Update() {
        ShowWavesInGPU();
    }

    private void FixedUpdate()
    {
       setReadyNextInfluentMatrix();
    
        if (positionIndex > 0)
        {
            positionCurrentMaxIndex = positionIndex;
        }
        positionIndex = 0;
        
        //Matrix4x4[] positionBuffer = positions.ToArray();
    }

    private void ShowWavesInGPU()
    {
        //tempPositions = positions;
        int currentCount = 0;
       
        while (positionCurrentMaxIndex - currentCount > 0)
        {
            int batchSize = Math.Min(1023, positionCurrentMaxIndex - currentCount);
            Graphics.DrawMeshInstanced(fragmentMesh, 0, fragmentMaterial, positions.Skip(currentCount).Take(batchSize).ToArray(), batchSize, new MaterialPropertyBlock(), UnityEngine.Rendering.ShadowCastingMode.Off,false,11);
            currentCount += batchSize;
        }
        
        /*
        waterMesh.normals = waterNormalsArray;

        for (int i = waterNormalsArray.Length - 1; i >= 0; i--)
        {
            waterNormalsArray[i] = (waterNormalsArray[i] + compensationVector)/1.2f;
        }
        */


    }

    private static void DrawInfluence(float x, float y, Vector2 direction)
    {
        if (positionIndex < positionMaxIndex)
        {

            //   Matrix4x4 auxMatrix;
            //auxMatrix = positions[positionIndex];
            ///auxMatrix.SetColumn(3, new Vector4(x, y, 10, 1));
            //auxMatrix[3,1] = y;
            //auxMatrix[3,2] = x;
            Vector2 perpendicularSpeed = Vector2.Perpendicular(direction).normalized * -1 / direction.magnitude;

            positions[positionIndex] = new Matrix4x4
            {
                m00 = perpendicularSpeed.x,     m10 = perpendicularSpeed.y,     m20 = 0,                m30 = 0,
                m01 = direction.x *2,   m11 = direction.y *2  ,   m21 = 0,                m31 = 0,
                m02 = 0,                        m12 = 0,                        m22 = 1,                m32 = 0,
                m03 = x,                        m13 = y,                        m23 = 10,               m33 = 1,
            };

            // 0.0006949

           // positions[positionIndex] = positions[positionIndex].SetColumn(3, new Vector4(x, y, 10, 1));


            positionIndex++;
        }
        else
        {
            positions.Add(new Matrix4x4
            {
                m00 = 1/direction.magnitude,m10 = 0,m20 = 0,m30 = 0,
                m01 = 0,m11 = direction.magnitude,m21 = 0,m31 = 0,
                m02 = 0,m12 = 0,m22 = 1,m32 = 0,
                m03 = x,m13 = y,m23 = 10,m33 = 1,
            });

            positionIndex++;
            positionMaxIndex++;
        }

;
        
        /*
        int rowSize = (int)(precisionX / precisionMeshRatio )+ 1;
        float meshYf = y / precisionMeshRatio;
        float meshXf = x / precisionMeshRatio;

        int meshY = Mathf.RoundToInt(y / precisionMeshRatio);
        int meshX = Mathf.RoundToInt(x / precisionMeshRatio);

        float baseMultiplicatorY = 1 - Math.Abs(meshY- meshYf );
        float baseMultiplicatorX = 1 - Math.Abs(meshX - meshXf);


        waterNormalsArray[rowSize * meshY + meshX] +=  new Vector3(direction.x, direction.y, 0);
    */
        // waterNormalsArray[rowSize * (meshY +1) + meshX] += Mathf.Max(0,(1 - Math.Abs(meshY + 1  - meshYf))) * (1- baseMultiplicatorX) * new Vector3(direction.x, direction.y, 0);
        // waterNormalsArray[rowSize * (meshY -1) + meshX] += Mathf.Max(0, (1 - Math.Abs(meshY - 1 - meshYf))) * (1 - baseMultiplicatorX) * new Vector3(direction.x, direction.y, 0);
        // waterNormalsArray[rowSize * meshY + meshX + 1] += Mathf.Max(0, (1 - Math.Abs(meshX + 1 - meshXf)))* (1 - baseMultiplicatorY) * new Vector3(direction.x, direction.y, 0);
        // waterNormalsArray[rowSize * meshY + meshX - 1] += Mathf.Max(0, (1 - Math.Abs(meshX - 1 - meshXf)))* (1 - baseMultiplicatorY) * new Vector3(direction.x, direction.y, 0);
    }


    private static void cleanInfluencePoint(int[] dirtyPoint){
		influenceMatrix [dirtyPoint[0], dirtyPoint[1], 0] = 0;
		influenceMatrix [dirtyPoint[0], dirtyPoint[1], 1] = 0; 
	}

	public static void addInfluence(Vector2 position, Vector2 direction) {
		
		int X = (int)(precisionX/2) + (int)(position.x * pixelPerUnit);
		int Y = (int)(precisionY/2) + (int)(position.y * pixelPerUnit);

		influenceMatrix [X, Y, 0] += direction.x;
		influenceMatrix [X, Y, 1] += direction.y;

        DrawInfluence(position.x , position.y , direction);

        if (dirtyInterator < dirtyMaxIndex)
        {
            dirtyInfluencePoints[dirtyInterator][0] = X;
            dirtyInfluencePoints[dirtyInterator][1] = Y;
        }
        else
        {
            dirtyInfluencePoints.Add(new int[2] { X, Y });
            dirtyMaxIndex++;
        }
        dirtyInterator++;
    }
 
    public static void setReadyNextInfluentMatrix()
    {

        dirtyInfluencePoints.ForEach(dirtyPoint => cleanInfluencePoint(dirtyPoint));
        dirtyInterator = 0;
    }

    private static void ResetBuffers()
    {
        args[0] = (uint)fragmentMesh.GetIndexCount(0);
        args[2] = (uint)fragmentMesh.GetIndexStart(0);
        args[3] = (uint)fragmentMesh.GetBaseVertex(0);
    }


    public static Vector2 getWorldInfluenceInArea(Vector2 position){
		Vector2 influenceSum = new Vector2 ();
		int dispersion = 5;
		int X = (int)(precisionX/2) + (int)(position.x * pixelPerUnit);
		int Y = (int)(precisionY/2) + (int)(position.y * pixelPerUnit);

		for (int x1 = -1 * dispersion; x1 <= dispersion; x1++) {
			for (int y1 = -1 * dispersion; y1 <= dispersion; y1++) {
                int currentXIndex = Mathf.Clamp(X + x1, 0, precisionX -1 );
                int currentYIndex = Mathf.Clamp( Y + y1, 0, precisionY -1 );
                
                influenceSum.x = influenceSum.x + influenceMatrix [currentXIndex, currentYIndex, 0];
				influenceSum.y = influenceSum.y + influenceMatrix [currentXIndex, currentYIndex, 1];
			}
		}
		return influenceSum / 5;
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