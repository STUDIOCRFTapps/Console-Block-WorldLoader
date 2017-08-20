using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class WorldLoader : MonoBehaviour {

	public Text[] DebugingText;

	public Transform[] Colliders;

	public int SimulatedChunkSize;
	public GameObject MeshTemplate;

	WorldManager worldManager;
	public WorldTexture[] worldTextures;

	public Vector2 TestStartPos = new Vector2(0,0);

	public float LOD0EndChunkDistance = 4;
	public float LOD1EndChunkDistance = 8;
	public float LOD2EndChunkDistance = 11;
	public float LOD3EndChunkDistance = 15;
	public float LOD4EndChunkDistance = 18;

	public float EndOfChunkMesh = 2;

	public WorldCreator worldCreator;

	public Transform Player;
	public Rigidbody PlayerBody;
	Vector2 OldChunkPos;
	Vector2 NewChunkPos;

	public Biome[] biomes;

	List<ChunkParameters> NewChunkRequirement;
	//List<ChunkParameters> NewCRequirement0;
	//List<ChunkParameters> NewCRequirement1;
	//List<ChunkParameters> NewCRequirement2;
	//List<ChunkParameters> NewCRequirement3;

	List<Vector2> DoNotRecreate = new List<Vector2>();
	List<int> GarbadgeChunk = new List<int>();
	Chunk[] ChunkList;

	void Start () {
		worldCreator.Execute();

		PlayerBody = Player.GetComponent<Rigidbody>();
		worldManager = new WorldManager(worldCreator,biomes);
		OldChunkPos = new Vector2(Mathf.Floor(Player.position.x/SimulatedChunkSize),Mathf.Floor(Player.position.z/SimulatedChunkSize));
		PlayerBlockPos = new Vector3(Mathf.Floor(Player.transform.position.x),Mathf.Floor(Player.transform.position.y),Mathf.Floor(Player.transform.position.z));

		int ChunkCount = 0;
		for(int y = -Mathf.CeilToInt(LOD4EndChunkDistance); y <= Mathf.Ceil(LOD4EndChunkDistance); y++) {
			for(int x = -Mathf.CeilToInt(LOD4EndChunkDistance); x <= Mathf.Ceil(LOD4EndChunkDistance); x++) {
				float Distance = Vector2.Distance(Vector2.zero,new Vector2(x,y));

				if(Distance <= LOD4EndChunkDistance) {
					ChunkCount++;
				}
			}
		}

		ChunkList = new Chunk[ChunkCount];
		int clc = 0;

		for(int y = -Mathf.CeilToInt(LOD4EndChunkDistance); y <= Mathf.Ceil(LOD4EndChunkDistance); y++) {
			for(int x = -Mathf.CeilToInt(LOD4EndChunkDistance); x <= Mathf.Ceil(LOD4EndChunkDistance); x++) {
				float Distance = Vector2.Distance(Vector2.zero,new Vector2(x,y));

				if(Distance <= LOD0EndChunkDistance) {
					ChunkList[clc++] = new Chunk(new Vector2(NewChunkPos.x+x,NewChunkPos.y+y),Mathf.RoundToInt(Mathf.Pow(2,0)),SimulatedChunkSize,worldTextures,worldManager,MeshTemplate,(Distance <= EndOfChunkMesh && EndOfChunkMesh > 0));
					ChunkList[clc-1].PrepareGeneration();
					ChunkList[clc-1].GenerateWorldObject();
					//NewChunkRequirement.Add(new ChunkParameters(0,new Vector2(NewChunkPos.x+x,NewChunkPos.y+y),(Distance <= EndOfChunkMesh)));
				} else if(Distance <= LOD1EndChunkDistance) {
					ChunkList[clc++] = new Chunk(new Vector2(NewChunkPos.x+x,NewChunkPos.y+y),Mathf.RoundToInt(Mathf.Pow(2,1)),SimulatedChunkSize,worldTextures,worldManager,MeshTemplate,false);
					ChunkList[clc-1].PrepareGeneration();
					ChunkList[clc-1].GenerateWorldObject();
					//NewChunkRequirement.Add(new ChunkParameters(1,new Vector2(NewChunkPos.x+x,NewChunkPos.y+y),false));
				} else if(Distance <= LOD2EndChunkDistance) {
					ChunkList[clc++] = new Chunk(new Vector2(NewChunkPos.x+x,NewChunkPos.y+y),Mathf.RoundToInt(Mathf.Pow(2,2)),SimulatedChunkSize,worldTextures,worldManager,MeshTemplate,false);
					ChunkList[clc-1].PrepareGeneration();
					ChunkList[clc-1].GenerateWorldObject();
					//NewChunkRequirement.Add(new ChunkParameters(2,new Vector2(NewChunkPos.x+x,NewChunkPos.y+y),false));
				} else if(Distance <= LOD3EndChunkDistance) {
					ChunkList[clc++] = new Chunk(new Vector2(NewChunkPos.x+x,NewChunkPos.y+y),Mathf.RoundToInt(Mathf.Pow(2,3)),SimulatedChunkSize,worldTextures,worldManager,MeshTemplate,false);
					ChunkList[clc-1].PrepareGeneration();
					ChunkList[clc-1].GenerateWorldObject();
					//NewChunkRequirement.Add(new ChunkParameters(3,new Vector2(NewChunkPos.x+x,NewChunkPos.y+y),false));
				} else if(Distance <= LOD4EndChunkDistance) {
					ChunkList[clc++] = new Chunk(new Vector2(NewChunkPos.x+x,NewChunkPos.y+y),Mathf.RoundToInt(Mathf.Pow(2,4)),SimulatedChunkSize,worldTextures,worldManager,MeshTemplate,false);
					ChunkList[clc-1].PrepareGeneration();
					ChunkList[clc-1].GenerateWorldObject();
					//NewChunkRequirement.Add(new ChunkParameters(4,new Vector2(NewChunkPos.x+x,NewChunkPos.y+y),false));
				}
			}
		}
	}

	bool AlreadyLoading = false;
	Vector2 ChunkToLoad = Vector2.zero;

	bool ActionRunning = false;

	List<Action> PrepareActions = new List<Action>();
	List<Action> ThreadingActions = new List<Action>();
	//List<Action> PreThreadingActions = new List<Action>();
	Chunk[] NewChunk;
	Thread PreparingThread;
	Thread CleaningThread;
	Thread GeneratingThread;
	Thread ActionThread;

	Vector3 PlayerBlockPos;

	void Update () {
		DebugingText[0].text = "Current Biome: " +  worldManager.biomes[worldManager.GetBiomeAt(Player.position.x,Player.position.z,1)].name;
		DebugingText[1].text = worldCreator.GetTemperature(Player.position.x/worldCreator.WorldCreationRation,Player.position.z/worldCreator.WorldCreationRation).ToString();

		if(Player.transform.position.y < -17) {
			PlayerBody.velocity += Vector3.up * 0.4f;
		}

		PlayerBlockPos = new Vector3(Mathf.Floor(Player.transform.position.x),Mathf.Floor(Player.transform.position.y),Mathf.Floor(Player.transform.position.z));

		int index = 0;
		for(int x = -1; x < 2; x++) {
			for(int y = -1; y < 2; y++) {
				float Height = Chunk.GetValueAtPixelParams(PlayerBlockPos.x+x,PlayerBlockPos.z+y,16,worldManager);
				float Difference = 0;

				for(int x2 = -1; x2 < 2; x2++) {
					for(int y2 = -1; y2 < 2; y2++) {
						if(x2 != 0 && y2 != 0) {
							if(Mathf.Abs(Difference) < Mathf.Abs(Chunk.GetValueAtPixelParams(PlayerBlockPos.x+x+x2,PlayerBlockPos.z+y+y2,16,worldManager)-Height)) {
								Difference = Chunk.GetValueAtPixelParams(PlayerBlockPos.x+x+x2,PlayerBlockPos.z+y+y2,16,worldManager)-Height;
							}
						}
					}
				}

				Colliders[index].position = new Vector3(PlayerBlockPos.x+x,Height*16f,PlayerBlockPos.z+y);
				Colliders[index].localScale = new Vector3(1,Mathf.Clamp(Mathf.Abs(Difference),0.05f,Mathf.Infinity)*16,1);

				index++;
			}
		}

		if(!ActionRunning) {
			NewChunkPos = new Vector2(Mathf.Floor(Player.position.x/SimulatedChunkSize),Mathf.Floor(Player.position.z/SimulatedChunkSize));
		}

		if(NewChunkPos != OldChunkPos || Input.GetKeyDown(KeyCode.Return)) {
			StartCoroutine(LoadNewChunk());
		}

		OldChunkPos = NewChunkPos;
	}

	IEnumerator LoadNewChunk () {
		ActionRunning = false;

		if(AlreadyLoading) {
			ChunkToLoad = NewChunkPos;
			yield break;
		}

		AlreadyLoading = true;
		Vector2 PlayerPos = ChunkToLoad;

		PrepareActions.Clear();
		ThreadingActions.Clear();

		//NewChunk = new Chunk[0];

		Action Preparing = () => {
			NewChunkRequirement = new List<ChunkParameters>();
			//NewCRequirement0 = new List<ChunkParameters>();
			//NewCRequirement1 = new List<ChunkParameters>();
			//NewCRequirement2 = new List<ChunkParameters>();
			//NewCRequirement3 = new List<ChunkParameters>();

			for(int y = -Mathf.CeilToInt(LOD4EndChunkDistance); y <= Mathf.Ceil(LOD4EndChunkDistance); y++) {
				for(int x = -Mathf.CeilToInt(LOD4EndChunkDistance); x <= Mathf.Ceil(LOD4EndChunkDistance); x++) {
					float Distance = Vector2.Distance(Vector2.zero,new Vector2(x,y));

					if(Distance <= LOD0EndChunkDistance) {
						//NewCRequirement0.Add(new ChunkParameters(0,new Vector2(NewChunkPos.x+x,NewChunkPos.y+y),(Distance <= EndOfChunkMesh)));
						NewChunkRequirement.Add(new ChunkParameters(0,new Vector2(NewChunkPos.x+x,NewChunkPos.y+y),(Distance <= EndOfChunkMesh && EndOfChunkMesh > 0)));
					} else if(Distance <= LOD1EndChunkDistance) {
						//NewCRequirement1.Add(new ChunkParameters(1,new Vector2(NewChunkPos.x+x,NewChunkPos.y+y),false));
						NewChunkRequirement.Add(new ChunkParameters(1,new Vector2(NewChunkPos.x+x,NewChunkPos.y+y),false));
					} else if(Distance <= LOD2EndChunkDistance) {
						//NewCRequirement2.Add(new ChunkParameters(2,new Vector2(NewChunkPos.x+x,NewChunkPos.y+y),false));
						NewChunkRequirement.Add(new ChunkParameters(2,new Vector2(NewChunkPos.x+x,NewChunkPos.y+y),false));
					} else if(Distance <= LOD3EndChunkDistance) {
						//NewCRequirement3.Add(new ChunkParameters(3,new Vector2(NewChunkPos.x+x,NewChunkPos.y+y),false));
						NewChunkRequirement.Add(new ChunkParameters(3,new Vector2(NewChunkPos.x+x,NewChunkPos.y+y),false));
					} else if(Distance <= LOD4EndChunkDistance) {
						//NewCRequirement3.Add(new ChunkParameters(4,new Vector2(NewChunkPos.x+x,NewChunkPos.y+y),false));
						NewChunkRequirement.Add(new ChunkParameters(4,new Vector2(NewChunkPos.x+x,NewChunkPos.y+y),false));
					}
				}
			}
			//NewChunkRequirement.AddRange(NewCRequirement3);
			//NewChunkRequirement.AddRange(NewCRequirement2);
			//NewChunkRequirement.AddRange(NewCRequirement1);
			//NewChunkRequirement.AddRange(NewCRequirement0);

			GarbadgeChunk.Clear();
			DoNotRecreate.Clear();
			NewChunk = new Chunk[NewChunkRequirement.Count];
		};

		PreparingThread = new Thread(new ThreadStart(Preparing));
		PreparingThread.Start();
		yield return new WaitUntil(() => !PreparingThread.IsAlive);

		Action Cleaning = () => {
			for(int i = 0; i < ChunkList.Length; i++) {
				ChunkParameters cparams = new ChunkParameters(ChunkList[i].GetLODLevel(),ChunkList[i].GetChunkPosition(),ChunkList[i].IsColliderActive());

				bool IsFound = false;

				for(int s = 0; s < NewChunkRequirement.Count; s++) {
					if(NewChunkRequirement[s].ChunkPos == cparams.ChunkPos && NewChunkRequirement[s].ColliderMesh == cparams.ColliderMesh && NewChunkRequirement[s].LevelOfDetails == cparams.LevelOfDetails) {
						IsFound = true;
						//NewChunk[s] = ChunkList[i];

						DoNotRecreate.Add(cparams.ChunkPos);
						break;
					}
				}

				if(!IsFound) {
					GarbadgeChunk.Add(i);
				}
			}
		};

		CleaningThread = new Thread(new ThreadStart(Cleaning));
		CleaningThread.Start();
		yield return new WaitUntil(() => !CleaningThread.IsAlive);

		Action Generating = () => {
			for(int i = 0; i < NewChunkRequirement.Count; i++) {

				bool DoNotRecreateGate = DoNotRecreate.Contains(NewChunkRequirement[i].ChunkPos);
				/*for(int s = 0; s < DoNotRecreate.Count; s++) {
					if(DoNotRecreate[s] == NewChunkRequirement[i].ChunkPos) {
						DoNotRecreateGate = true;
					}
				}*/
				if(!DoNotRecreateGate) {
					if(GarbadgeChunk.Count <= 0) {
						break;
					}
					int c = GarbadgeChunk[0];
					GarbadgeChunk.RemoveAt(0);

					int c2 = i;

					ChunkList[c].ReconditionMesh(NewChunkRequirement[c2].ChunkPos,Mathf.RoundToInt(Mathf.Pow(2,NewChunkRequirement[c2].LevelOfDetails)),NewChunkRequirement[c2].ColliderMesh);

					/*Action PrepareChunk = () => {
						ChunkList[c].PrepareGeneration();
					};
					ThreadingActions.Add(PrepareChunk);	*/

					ChunkList[c].PrepareGeneration();

					Action GenerateChunk = () => {
						ChunkList[c].GenerateWorldObject();

					};
					PrepareActions.Add(GenerateChunk);
				} else {
					
				}
			}
		};

		GeneratingThread = new Thread(new ThreadStart(Generating));
		GeneratingThread.Start();
		yield return new WaitUntil(() => !GeneratingThread.IsAlive);

		/*while(PreThreadingActions.Count > 0) {
			Action action = PreThreadingActions[PreThreadingActions.Count-1];
			PreThreadingActions.RemoveAt(PreThreadingActions.Count-1);

			action();
		}
		PreThreadingActions.Clear();*/

		Action ExecuteThreadingActions = () => {
			while(ThreadingActions.Count > 0) {
				Action action = ThreadingActions[ThreadingActions.Count-1];
				ThreadingActions.RemoveAt(ThreadingActions.Count-1);

				action();
			}
			ThreadingActions.Clear();
		};

		ActionThread = new Thread(new ThreadStart(ExecuteThreadingActions));
		ActionThread.Start();
		yield return new WaitUntil(() => !ActionThread.IsAlive);

		while(PrepareActions.Count > 0) {
			Action action = PrepareActions[PrepareActions.Count-1];
			PrepareActions.RemoveAt(PrepareActions.Count-1);

			action();
		}
		PrepareActions.Clear();

		//ChunkList = NewChunk;
		AlreadyLoading = false;

		yield return null;
	}

}

public class Chunk {
	WorldManager worldM;

	Vector2 TestStartPos = new Vector2(0,0);
	public GameObject ChunkObject;
	public Transform ChunkTransform;

	MeshCollider mc;
	MeshFilter mf;
	MeshFilter mw;

	WorldTexture[] worldTextures;
	int BlockSize = 1;
	int SimulatedChunkSize = 16;
	GameObject MeshTemplate;

	bool GenerateCollider = false;

	public Vector2 GetChunkPosition () {
		return TestStartPos;
	}

	public int GetLODLevel () {
		return Mathf.RoundToInt(Mathf.Log(BlockSize,2));
	}

	public bool IsColliderActive () {
		return GenerateCollider;
	}

	public Chunk(Vector2 Position, int blockSize, int simulatedChunkSize, WorldTexture[] textures, WorldManager wm, GameObject mesh, bool CreateCollider) {
		worldTextures = textures;
		BlockSize = blockSize;
		SimulatedChunkSize = simulatedChunkSize;
		GenerateCollider = CreateCollider;

		TestStartPos = Position;
		MeshTemplate = mesh;
		worldM = wm;
	}

	public void SetNewValues (Vector2 Position, int blockSize, int simulatedChunkSize, WorldTexture[] textures, GameObject mesh, bool CreateCollider) {
		worldTextures = textures;
		BlockSize = blockSize;
		SimulatedChunkSize = simulatedChunkSize;
		GenerateCollider = CreateCollider;

		TestStartPos = Position;
		MeshTemplate = mesh;

		mf = null;
		mc = null;
		mw = null;
		DestroyChunk();
		ChunkTransform = null;
	}

	bool Token = false;
	Vector3 TokenSize;
	Vector3 TokenPos;

	public void ReconditionMesh (Vector2 Position, int blockSize, bool CreateCollider) {
		if(ChunkObject == null) {
			return;
		}
		BlockSize = blockSize;
		TestStartPos = Position;

		Token = true;
		TokenSize = new Vector3(BlockSize,BlockSize,BlockSize);
		TokenPos = new Vector3(TestStartPos.x*SimulatedChunkSize,0,TestStartPos.y*SimulatedChunkSize);

		GenerateCollider = CreateCollider;
	}

	public void DestroyChunk () {
		UnityEngine.Object.Destroy(ChunkObject);
	}

	Vector2 VCode1 = new Vector2(0,0);
	Vector2 VCode2 = new Vector2(1,0);
	Vector2 VCode3 = new Vector2(0,1);
	Vector2 VCode4 = new Vector2(1,1);

	public bool IsPreparing = false;

	//Declaring for the preparation
	Vector3[] vertices = new Vector3[0];
	int[] triangles = new int[0];
	Vector2[] UVs = new Vector2[0];

	List<Vector3> physXverts = new List<Vector3>();
	List<int> physXtris = new List<int>();

	List<Vector3> WaterVerts = new List<Vector3>();
	List<int> WaterTris = new List<int>();
	List<Vector2> WaterUVs = new List<Vector2>();
	List<Vector3> WaterNormal = new List<Vector3>();

	Mesh cMesh;
	Mesh pMesh;
	Mesh wMesh;

	public void PrepareGeneration () {
		physXtris.Clear();
		physXverts.Clear();
		WaterNormal.Clear();
		WaterTris.Clear();
		WaterUVs.Clear();
		WaterVerts.Clear();

		IsPreparing = true;

		int[,] xWallHeight = new int[SimulatedChunkSize/BlockSize,SimulatedChunkSize/BlockSize];
		int[,] yWallHeight = new int[SimulatedChunkSize/BlockSize,SimulatedChunkSize/BlockSize];

		float nY = 0f;
		float nX = 0f;
		float nZ = 0f;

		float wY = 0f;

		float xCoord = 0f;
		float yCoord = 0f;

		float xPos = 0f;
		float yPos = 0f;

		int c = 0;
		int v = 0;
		int c2 = 0;
		int neg = 0;
		int p = 0;
		int p2 = 0;

		p = 0;
		for(int y = 0; y < SimulatedChunkSize/BlockSize; y++) {
			for(int x = 0; x < SimulatedChunkSize/BlockSize; x++) {
				p = ((x+y*SimulatedChunkSize/BlockSize)*4);
				nY = GetValueAtPixel(x*BlockSize+(TestStartPos.x*(SimulatedChunkSize)),y*BlockSize+(TestStartPos.y*(SimulatedChunkSize)));
//				wY = worldM.GetWaterHeight(x*BlockSize+(TestStartPos.x*(SimulatedChunkSize)),y*BlockSize+(TestStartPos.y*(SimulatedChunkSize)),BlockSize);
//
//				WaterVerts.Add(new Vector3(x,wY,y));
//				WaterUVs.Add(VCode1);
//				WaterNormal.Add(Vector3.up);
//				WaterVerts.Add(new Vector3(x+1,wY,y));
//				WaterUVs.Add(VCode2);
//				WaterNormal.Add(Vector3.up);
//				WaterVerts.Add(new Vector3(x,wY,y+1));
//				WaterUVs.Add(VCode3);
//				WaterNormal.Add(Vector3.up);
//				WaterVerts.Add(new Vector3(x+1,wY,y+1));
//				WaterUVs.Add(VCode4);
//				WaterNormal.Add(Vector3.up);
//
//				WaterTris.Add(p+1);
//				WaterTris.Add(p+0);
//				WaterTris.Add(p+2);
//				WaterTris.Add(p+1);
//				WaterTris.Add(p+2);
//				WaterTris.Add(p+3);

				physXverts.Add(new Vector3(x,nY,y));
				physXverts.Add(new Vector3(x+1,nY,y));
				physXverts.Add(new Vector3(x,nY,y+1));
				physXverts.Add(new Vector3(x+1,nY,y+1));

				physXtris.Add(p+1);
				physXtris.Add(p+0);
				physXtris.Add(p+2);

				physXtris.Add(p+1);
				physXtris.Add(p+2);
				physXtris.Add(p+3);
			}
		}

		p = 0;
		p2 = physXverts.Count;
		for(int y = 0; y < SimulatedChunkSize/BlockSize; y++) {
			for(int x = 0; x < SimulatedChunkSize/BlockSize; x++) {
				p = ((x+y*SimulatedChunkSize/BlockSize)*4);
				nY = GetValueAtPixel(x*BlockSize+(TestStartPos.x*SimulatedChunkSize),y*BlockSize+(TestStartPos.y*SimulatedChunkSize));
				nX = GetValueAtPixel(x*BlockSize+(TestStartPos.x*SimulatedChunkSize)+BlockSize,y*BlockSize+(TestStartPos.y*SimulatedChunkSize));
				nZ = GetValueAtPixel(x*BlockSize+(TestStartPos.x*SimulatedChunkSize),y*BlockSize+(TestStartPos.y*SimulatedChunkSize)+BlockSize);

				physXverts.Add(new Vector3(x+1,nX,y));
				physXverts.Add(new Vector3(x+1,nX,y+1));

				physXverts.Add(new Vector3(x+1,nZ,y+1));
				physXverts.Add(new Vector3(x,nZ,y+1));

				physXtris.Add(p+3);
				physXtris.Add(p+p2);
				physXtris.Add(p+1);

				physXtris.Add(p+p2);
				physXtris.Add(p+p2+1);
				physXtris.Add(p+3);

				physXtris.Add(p+2);
				physXtris.Add(p+p2+3);
				physXtris.Add(p+3);

				physXtris.Add(p+p2+3);
				physXtris.Add(p+p2+2);
				physXtris.Add(p+3);
			}
		}


		int Dispacement = 0;
		for(int y = 0; y < SimulatedChunkSize/BlockSize; y++) {
			for(int x = 0; x < SimulatedChunkSize/BlockSize; x++) {
				nX = GetValueAtPixel(x*BlockSize+(TestStartPos.x*SimulatedChunkSize)+BlockSize,y*BlockSize+(TestStartPos.y*SimulatedChunkSize));
				nY = GetValueAtPixel(x*BlockSize+(TestStartPos.x*SimulatedChunkSize),y*BlockSize+(TestStartPos.y*SimulatedChunkSize));
				nZ = GetValueAtPixel(x*BlockSize+(TestStartPos.x*SimulatedChunkSize),y*BlockSize+(TestStartPos.y*SimulatedChunkSize)+BlockSize);

				neg = 1;
				if(nX-nY < 0) {
					neg = -1;
				}

				if(neg == 1) {
					Dispacement += Mathf.Abs(Mathf.FloorToInt(nY-nX));
				} else {
					Dispacement += Mathf.Abs(Mathf.CeilToInt(nY-nX));
				}

				neg = 1;
				if(nZ-nY < 0) {
					neg = -1;
				}

				if(neg == 1) {
					Dispacement += Mathf.Abs(Mathf.FloorToInt(nY-nZ));
				} else {
					Dispacement += Mathf.Abs(Mathf.CeilToInt(nY-nZ));
				}
			}
		}

		int[] Triangles = new int[(Dispacement + (SimulatedChunkSize/BlockSize * SimulatedChunkSize/BlockSize))*6];
		int tri = 0;
		Vector3[] Vertices = new Vector3[(Dispacement + (SimulatedChunkSize/BlockSize * SimulatedChunkSize/BlockSize))*4];
		int ver = 0;
		Vector2[] VerticesUVs = new Vector2[(Dispacement + (SimulatedChunkSize/BlockSize * SimulatedChunkSize/BlockSize))*4];
		int uv = 0;
		UVs = new Vector2[(Dispacement + (SimulatedChunkSize/BlockSize * SimulatedChunkSize/BlockSize))*6];

		for(int y = 0; y < SimulatedChunkSize/BlockSize; y++) {
			for(int x = 0; x < SimulatedChunkSize/BlockSize; x++) {
				c = ((x+y*SimulatedChunkSize/BlockSize)*4);

				wY = worldM.GetWaterHeight(x*BlockSize+(TestStartPos.x*(SimulatedChunkSize)),y*BlockSize+(TestStartPos.y*(SimulatedChunkSize)),BlockSize);

				WaterVerts.Add(new Vector3(x,wY,y));
				WaterUVs.Add(VCode1);
				WaterNormal.Add(Vector3.up);
				WaterVerts.Add(new Vector3(x+1,wY,y));
				WaterUVs.Add(VCode2);
				WaterNormal.Add(Vector3.up);
				WaterVerts.Add(new Vector3(x,wY,y+1));
				WaterUVs.Add(VCode3);
				WaterNormal.Add(Vector3.up);
				WaterVerts.Add(new Vector3(x+1,wY,y+1));
				WaterUVs.Add(VCode4);
				WaterNormal.Add(Vector3.up);

				WaterTris.Add(c+1);
				WaterTris.Add(c+0);
				WaterTris.Add(c+2);
				WaterTris.Add(c+1);
				WaterTris.Add(c+2);
				WaterTris.Add(c+3);

				nY = GetValueAtPixel(x*BlockSize+(TestStartPos.x*(SimulatedChunkSize)),y*BlockSize+(TestStartPos.y*(SimulatedChunkSize)));

				xCoord = x*BlockSize+(TestStartPos.x*(SimulatedChunkSize));
				yCoord = y*BlockSize+(TestStartPos.y*(SimulatedChunkSize));


				Vertices[ver++] = (new Vector3(x,nY,y));
				VerticesUVs[uv++] = ComplexTextureInfoToUV(GetTextureTypeAtPixel(xCoord,yCoord),0,VCode1);
				Vertices[ver++] = (new Vector3(x+1,nY,y));
				VerticesUVs[uv++] = ComplexTextureInfoToUV(GetTextureTypeAtPixel(xCoord,yCoord),0,VCode2);
				Vertices[ver++] = (new Vector3(x,nY,y+1));
				VerticesUVs[uv++] = ComplexTextureInfoToUV(GetTextureTypeAtPixel(xCoord,yCoord),0,VCode3);
				Vertices[ver++] = (new Vector3(x+1,nY,y+1));
				VerticesUVs[uv++] = ComplexTextureInfoToUV(GetTextureTypeAtPixel(xCoord,yCoord),0,VCode4);

				Triangles[tri] = (c+1);
				tri++;
				Triangles[tri] = (c+0);
				tri++;
				Triangles[tri] = (c+2);
				tri++;

				Triangles[tri] = (c+1);
				tri++;
				Triangles[tri] = (c+2);
				tri++;
				Triangles[tri] = (c+3);
				tri++;
			}
		}

		v = ver;
		c2 = v;
		for(int y = 0; y < SimulatedChunkSize/BlockSize; y++) {
			for(int x = 0; x < SimulatedChunkSize/BlockSize; x++) {
				c = 0;

				nX = GetValueAtPixel(x*BlockSize+(TestStartPos.x*SimulatedChunkSize)+BlockSize,y*BlockSize+(TestStartPos.y*SimulatedChunkSize));
				nY = GetValueAtPixel(x*BlockSize+(TestStartPos.x*SimulatedChunkSize),y*BlockSize+(TestStartPos.y*SimulatedChunkSize));

				xPos = x*BlockSize+(TestStartPos.x*SimulatedChunkSize);
				yPos = y*BlockSize+(TestStartPos.y*SimulatedChunkSize);

				neg = 1;
				if(nX-nY < 0) {
					neg = -1;
				}

				if(neg == 1) {
					xWallHeight[x,y] = Mathf.FloorToInt(nY-nX);
				} else {
					xWallHeight[x,y] = Mathf.CeilToInt(nY-nX);
				}

				for(int i = 0; i < Mathf.Abs(xWallHeight[x,y]); i++) {
					int TextureT = 0;
					if(nX-nY < 0) {
						TextureT = GetTextureTypeAtPixel(xPos,yPos);
					} else {
						TextureT = GetTextureTypeAtPixel(xPos+1,yPos);
					}

					int tp;
					if(i == 0) {
						tp = 1;
					} else if(i == 1) {
						tp = 2;
					} else {
						tp = 3;
					}

					c = ver;

					if(neg == 1) {
						Vertices[ver++] = (new Vector3(x+1,nX-i,y));
						VerticesUVs[uv++] = ComplexTextureInfoToUV(TextureT,tp,VCode1);
						Vertices[ver++] = (new Vector3(x+1,nX-i,y+1));
						VerticesUVs[uv++] = ComplexTextureInfoToUV(TextureT,tp,VCode2);
						Vertices[ver++] = (new Vector3(x+1,nX-i-1,y));
						VerticesUVs[uv++] = ComplexTextureInfoToUV(TextureT,tp,VCode3);
						Vertices[ver++] = (new Vector3(x+1,nX-i-1,y+1));
						VerticesUVs[uv++] = ComplexTextureInfoToUV(TextureT,tp,VCode4);

						Triangles[tri] = (c+3);
						tri++;
						Triangles[tri] = (c+1);
						tri++;
						Triangles[tri] = (c+0);
						tri++;

						Triangles[tri] = (c+2);
						tri++;
						Triangles[tri] = (c+3);
						tri++;
						Triangles[tri] = (c+0);
						tri++;
					} else {
						Vertices[ver++] = (new Vector3(x+1,nY-i,y));
						VerticesUVs[uv++] = ComplexTextureInfoToUV(TextureT,tp,VCode1);
						Vertices[ver++] = (new Vector3(x+1,nY-i,y+1));
						VerticesUVs[uv++] = ComplexTextureInfoToUV(TextureT,tp,VCode2);
						Vertices[ver++] = (new Vector3(x+1,nY-i-1,y));
						VerticesUVs[uv++] = ComplexTextureInfoToUV(TextureT,tp,VCode3);
						Vertices[ver++] = (new Vector3(x+1,nY-i-1,y+1));
						VerticesUVs[uv++] = ComplexTextureInfoToUV(TextureT,tp,VCode4);

						Triangles[tri] = (c+0);
						tri++;
						Triangles[tri] = (c+1);
						tri++;
						Triangles[tri] = (c+3);
						tri++;

						Triangles[tri] = (c+0);
						tri++;
						Triangles[tri] = (c+3);
						tri++;
						Triangles[tri] = (c+2);
						tri++;
					}
				}

				c2+=yWallHeight[x,y]*4;
			}
		}

		v = ver;
		c2 = v;
		for(int y = 0; y < SimulatedChunkSize/BlockSize; y++) {
			for(int x = 0; x < SimulatedChunkSize/BlockSize; x++) {
				c = 0;

				nY = GetValueAtPixel(x*BlockSize+(TestStartPos.x*SimulatedChunkSize),y*BlockSize+(TestStartPos.y*SimulatedChunkSize));
				nZ = GetValueAtPixel(x*BlockSize+(TestStartPos.x*SimulatedChunkSize),y*BlockSize+(TestStartPos.y*SimulatedChunkSize)+BlockSize);

				xPos = x*BlockSize+(TestStartPos.x*SimulatedChunkSize);
				yPos = y*BlockSize+(TestStartPos.y*SimulatedChunkSize);

				neg = 1;
				if(nZ-nY < 0) {
					neg = -1;
				}

				if(neg == 1) {
					yWallHeight[x,y] = Mathf.FloorToInt(nY-nZ);
				} else {
					yWallHeight[x,y] = Mathf.CeilToInt(nY-nZ);
				}

				for(int i = 0; i < Mathf.Abs(yWallHeight[x,y]); i++) {
					int TextureT = 0;
					if(nZ-nY < 0) {
						TextureT = GetTextureTypeAtPixel(xPos,yPos);
					} else {
						TextureT = GetTextureTypeAtPixel(xPos,yPos+1);
					}

					int tp;
					if(i == 0) {
						tp = 1;
					} else if(i == 1) {
						tp = 2;
					} else {
						tp = 3;
					}

					c = ver;

					if(neg == 1) {
						Vertices[ver++] = (new Vector3(x,nZ-i,y+1));
						VerticesUVs[uv++] = ComplexTextureInfoToUV(TextureT,tp,VCode1);
						Vertices[ver++] = (new Vector3(x+1,nZ-i,y+1));
						VerticesUVs[uv++] = ComplexTextureInfoToUV(TextureT,tp,VCode2);
						Vertices[ver++] = (new Vector3(x,nZ-i-1,y+1));
						VerticesUVs[uv++] = ComplexTextureInfoToUV(TextureT,tp,VCode3);
						Vertices[ver++] = (new Vector3(x+1,nZ-i-1,y+1));
						VerticesUVs[uv++] = ComplexTextureInfoToUV(TextureT,tp,VCode4);

						Triangles[tri] = (c+0);
						tri++;
						Triangles[tri] = (c+1);
						tri++;
						Triangles[tri] = (c+3);
						tri++;

						Triangles[tri] = (c+0);
						tri++;
						Triangles[tri] = (c+3);
						tri++;
						Triangles[tri] = (c+2);
						tri++;
					} else {
						Vertices[ver++] = (new Vector3(x,nY-i,y+1));
						VerticesUVs[uv++] = ComplexTextureInfoToUV(TextureT,tp,VCode1);
						Vertices[ver++] = (new Vector3(x+1,nY-i,y+1));
						VerticesUVs[uv++] = ComplexTextureInfoToUV(TextureT,tp,VCode2);
						Vertices[ver++] = (new Vector3(x,nY-i-1,y+1));
						VerticesUVs[uv++] = ComplexTextureInfoToUV(TextureT,tp,VCode3);
						Vertices[ver++] = (new Vector3(x+1,nY-i-1,y+1));
						VerticesUVs[uv++] = ComplexTextureInfoToUV(TextureT,tp,VCode4);

						Triangles[tri] = (c+3);
						tri++;
						Triangles[tri] = (c+1);
						tri++;
						Triangles[tri] = (c+0);
						tri++;

						Triangles[tri] = (c+2);
						tri++;
						Triangles[tri] = (c+3);
						tri++;
						Triangles[tri] = (c+0);
						tri++;
					}

				}

				c2+=yWallHeight[x,y]*4;
			}
		}

		Vector3[] oldVerts = Vertices;
		triangles = Triangles;
		vertices = new Vector3[triangles.Length];

		for(int i = 0; i < triangles.Length; i++) {
			vertices[i] = oldVerts[triangles[i]];
			UVs[i] = VerticesUVs[triangles[i]];
			triangles[i] = i;
		}

		IsPreparing = false;
	}

	public void GenerateWorldObject () {
		if(mf == null || mc == null || ChunkObject == null) {
			GameObject cChunk = (GameObject)GameObject.Instantiate(MeshTemplate,new Vector3(TestStartPos.x*SimulatedChunkSize,0,TestStartPos.y*SimulatedChunkSize),Quaternion.identity);
			//cChunk.GetComponent<ChunkOcclusion>().Player = Camera.main.transform;

			ChunkTransform = cChunk.transform;
			ChunkTransform.localScale = new Vector3(BlockSize,BlockSize,BlockSize);

			cMesh = new Mesh();
			cMesh.vertices = vertices;
			cMesh.triangles = triangles;
			cMesh.uv = UVs;

			cMesh.RecalculateNormals();

			mf = cChunk.GetComponent<MeshFilter>();
			mf.mesh = cMesh;

			mc = cChunk.GetComponent<MeshCollider>();
			if(GenerateCollider) {
				pMesh = new Mesh();
				pMesh.SetVertices(physXverts);
				pMesh.SetTriangles(physXtris,0);

				pMesh.RecalculateBounds();

				mc.sharedMesh = pMesh;
			}
				
			mw = cChunk.transform.GetChild(1).GetComponent<MeshFilter>();
			wMesh = new Mesh();
			wMesh.SetVertices(WaterVerts);
			wMesh.SetTriangles(WaterTris,0);
			wMesh.SetUVs(0,WaterUVs);
			wMesh.SetNormals(WaterNormal);
			mw.mesh = wMesh;

			ChunkObject = cChunk;
		} else {
			if(Token) {
				ChunkTransform.position = TokenPos;
				ChunkTransform.localScale = TokenSize;

				Token = false;
			}

			cMesh.Clear();

			cMesh.vertices = vertices;
			cMesh.triangles = triangles;
			cMesh.uv = UVs;

			cMesh.RecalculateNormals();

			mf.mesh = cMesh;
			if(GenerateCollider) {
				pMesh.Clear();
				pMesh.SetVertices(physXverts);
				pMesh.SetTriangles(physXtris,0);

				pMesh.RecalculateBounds();

				mc.sharedMesh = pMesh;
			}

			wMesh.Clear();
			wMesh.SetVertices(WaterVerts);
			wMesh.SetTriangles(WaterTris,0);
			wMesh.SetUVs(0,WaterUVs);
			wMesh.SetNormals(WaterNormal);
			mw.mesh = wMesh;
		}
	}

	//Texture Placement Rules:
	// 0 : Top
	// 1 : UpperWall
	// 2 : LowerWall
	// 3 : UnderWall

	Vector2 Results = Vector2.zero;

	Vector2 ComplexTextureInfoToUV (int TextureType, int texturePlacement, Vector2 verticesUV) {
		switch(texturePlacement) {
		case 0:
			if(verticesUV.x == 0) {
				Results.x = worldTextures[TextureType].TopCoords.x;
			} else {
				Results.x = worldTextures[TextureType].TopCoords.z;
			}
			if(verticesUV.y == 0) {
				Results.y = worldTextures[TextureType].TopCoords.y;
			} else {
				Results.y = worldTextures[TextureType].TopCoords.w;
			}
			break;
		case 1:
			if(verticesUV.x == 0) {
				Results.x = worldTextures[TextureType].UpperWallCoords.x;
			} else {
				Results.x = worldTextures[TextureType].UpperWallCoords.z;
			}
			if(verticesUV.y == 0) {
				Results.y = worldTextures[TextureType].UpperWallCoords.y;
			} else {
				Results.y = worldTextures[TextureType].UpperWallCoords.w;
			}
			break;
		case 2:
			if(verticesUV.x == 0) {
				Results.x = worldTextures[TextureType].LowerWallCoords.x;
			} else {
				Results.x = worldTextures[TextureType].LowerWallCoords.z;
			}
			if(verticesUV.y == 0) {
				Results.y = worldTextures[TextureType].LowerWallCoords.y;
			} else {
				Results.y = worldTextures[TextureType].LowerWallCoords.w;
			}
			break;
		case 3:
			if(verticesUV.x == 0) {
				Results.x = worldTextures[TextureType].UnderWallCoords.x;
			} else {
				Results.x = worldTextures[TextureType].UnderWallCoords.z;
			}
			if(verticesUV.y == 0) {
				Results.y = worldTextures[TextureType].UnderWallCoords.y;
			} else {
				Results.y = worldTextures[TextureType].UnderWallCoords.w;
			}
			break;
		}

		return V2ToUV(Results);
	}

	int GetTextureTypeAtPixel (float x, float y) {
		return worldM.GetTextureTypeAtPixel(x,y,BlockSize);
		//return 0;
	}

	public static float GetValueAtPixelParams (float x, float y, int blocksize, WorldManager wm) {
		return wm.GetHeightMap(x,y,blocksize);
		//return (float)noise.GetValue(x,0f,y)*(param.Amplitude/blocksize);
		//return Mathf.PerlinNoise(x*worldParameters.Frequency,y*worldParameters.Frequency)*worldParameters.Amplitude/(blocksize);
	}

	float GetValueAtPixel (float x, float y) {
		return GetValueAtPixelParams(x,y,BlockSize,worldM);
		//return GetValueAtPixelParams(x,y,BlockSize,chunkNoise,cparams);
	}

	Vector2 vcalcule = Vector2.zero;

	Vector2 PixelToUV (int x, int y) {
		vcalcule.x = 0;
		vcalcule.y = 0;

		vcalcule.x = (float)x*0.001953125f; //0.001953125 = 1/512, 512 = size of the texture
		vcalcule.y = 1-((float)y*0.001953125f); //0.001953125 = 1/512, 512 = size of the texture
		return vcalcule;
	}

	Vector2 V2ToUV (Vector2 v) {
		vcalcule.x = 0;
		vcalcule.y = 0;

		vcalcule.x = (float)v.x*0.001953125f; //0.001953125 = 1/512, 512 = size of the texture
		vcalcule.y = 1-((float)v.y*0.001953125f); //0.001953125 = 1/512, 512 = size of the texture
		return vcalcule;
	}

	bool IsInBound (int v, int bSize, int index) {
		return (v-(Mathf.Floor((float)v/bSize)*4))==index;
	}
}

[System.Serializable]
public class WorldTexture {
	public string Name;

	public Vector4 TopCoords;
	public Vector4 UpperWallCoords;
	public Vector4 LowerWallCoords;
	public Vector4 UnderWallCoords;
}

[System.Serializable]
public class WorldParameters {
	public float Frequency;
	public float Amplitude;
	public float Lacunarity;
	public float Displacement;
	public float Persistence;
}

public class ChunkParameters {
	public ChunkParameters(int ChunkLOD, Vector2 Position, bool Collider) {
		ChunkPos = Position;
		LevelOfDetails = ChunkLOD;
		ColliderMesh = Collider;
	}
	
	public int LevelOfDetails = 0;
	public Vector2 ChunkPos = Vector2.zero;
	public bool ColliderMesh = false;

	public override bool Equals(object other) {
		if(other.GetType() != typeof(ChunkParameters))
			return false;

		return this.LevelOfDetails == ((ChunkParameters)other).LevelOfDetails && this.ChunkPos == ((ChunkParameters)other).ChunkPos && this.ColliderMesh == ((ChunkParameters)other).ColliderMesh;
	}

	public override int GetHashCode () {
		return base.GetHashCode();
	}
}

public class WorldManager {
	public WorldCreator creator;
	public Biome[] biomes;

	public List<LibNoise.Unity.Generator.RiggedMultifractal> RMnoises = new List<LibNoise.Unity.Generator.RiggedMultifractal>();
	public List<int> RMnoisesBiomeId = new List<int>();
	public List<LibNoise.Unity.Generator.Billow> Bnoises = new List<LibNoise.Unity.Generator.Billow>();
	public List<int> BnoisesBiomeId = new List<int>();
	public List<LibNoise.Unity.Generator.Perlin> Pnoises = new List<LibNoise.Unity.Generator.Perlin>();
	public List<int> PnoisesBiomeId = new List<int>();

	public WorldManager(WorldCreator worldCreator, Biome[] biomesBlueprint) {
		biomes = biomesBlueprint;
		creator = worldCreator;

		for(int b = 0; b < biomes.Length; b++) {
			switch(biomes[b].noiseType) {
			case Biome.NoiseType.Billow:
				BnoisesBiomeId.Add(b);
				Bnoises.Add(new LibNoise.Unity.Generator.Billow(biomes[b].Frequency,biomes[b].NoiseLacunarity,biomes[b].NoisePersistence,biomes[b].NoiseOctaves,worldCreator.Seed,LibNoise.Unity.QualityMode.High));
				break;
			case Biome.NoiseType.Perlin:
				PnoisesBiomeId.Add(b);
				Pnoises.Add(new LibNoise.Unity.Generator.Perlin(biomes[b].Frequency,biomes[b].NoiseLacunarity,biomes[b].NoisePersistence,biomes[b].NoiseOctaves,worldCreator.Seed,LibNoise.Unity.QualityMode.High));
				break;
			case Biome.NoiseType.RiggedMultifractal:
				RMnoisesBiomeId.Add(b);
				RMnoises.Add(new LibNoise.Unity.Generator.RiggedMultifractal(biomes[b].Frequency,biomes[b].NoiseLacunarity,biomes[b].NoiseOctaves,worldCreator.Seed,LibNoise.Unity.QualityMode.High));
				break;
			}
		}
	}

	float CurrentValue = 0f;
	float Rarity = 0f;
	float Temperature = 0f;
	float Humidity = 0f;
	float ClosestValue = 0f;
	int BClosestId = 0;

	int CurrentBiome = 0;

	public int GetBiomeAt (float x, float y, int blockSize) {
		//return Mathf.Clamp((int)(Mathf.PerlinNoise(x*0.005f,y*0.005f)*2),0,1);
		if(x > 16384) {
			return 0;
		} else {
			return 1;
		}
		//Find to biome that is the closeset to the environnement
		CurrentValue = 0f;
		Rarity = 0f;
		Temperature = 0f;
		Humidity = 0f;

		ClosestValue = 0f;
		BClosestId = 0;

		for(int b = 0; b < biomes.Length; b++) {
			//Rarity, Temperature Matching, Humidity Matching.
			Rarity = biomes[b].BiomeRarity*0.01f;
			Temperature = Distance(biomes[b].BiomeRequiredTemperature*0.01f,creator.GetTemperature(x/(float)creator.WorldCreationRation,y/(float)creator.WorldCreationRation));
			Humidity = Distance(biomes[b].BiomeRequiredHumidity*0.01f,creator.GetHumidity(x/(float)creator.WorldCreationRation,y/(float)creator.WorldCreationRation));
			CurrentValue = Mathf.Lerp(Rarity,Mathf.Lerp(Temperature,Humidity,0.5f),0.7f); //0.3 - Rarity, 0.7 - Other

			if(CurrentValue > ClosestValue) {
				ClosestValue = CurrentValue;
				BClosestId = b;
			}
		}

		return BClosestId;
	}

	public float Distance (float a, float b) {
		return Mathf.Abs(a-b);
	}

	public float GetHeightMap(float x, float y, int blockSize) {
		//return (float)noise.GetValue(x,0f,y)*(parameters.Amplitude/blockSize);
		//return creator.GetHeight(x/(float)creator.WorldCreationRation,y/(float)creator.WorldCreationRation)*(parameters.Amplitude/blockSize);

		CurrentBiome = GetBiomeAt(x,y,blockSize);

		switch(biomes[CurrentBiome].noiseType) {
		case Biome.NoiseType.Billow:
			return ((biomes[CurrentBiome].BiomeMinHeight)+((float)Bnoises[BnoisesBiomeId.IndexOf(CurrentBiome)].GetValue(x,0f,y)*biomes[CurrentBiome].NoiseAmplitude))/blockSize;
			break;
		case Biome.NoiseType.Perlin:
			if(PnoisesBiomeId.IndexOf(CurrentBiome) >= Pnoises.Count || CurrentBiome >= biomes.Length) {
				Debug.LogWarning("Index: " + PnoisesBiomeId.IndexOf(CurrentBiome) + ", Lenght: " + Pnoises.Count);
				return 0f;
			}
			return ((biomes[CurrentBiome].BiomeMinHeight)+((float)Pnoises[PnoisesBiomeId.IndexOf(CurrentBiome)].GetValue(x,0f,y)*biomes[CurrentBiome].NoiseAmplitude))/blockSize;
			break;
		case Biome.NoiseType.RiggedMultifractal:
			if(RMnoisesBiomeId.IndexOf(CurrentBiome) >= RMnoises.Count || CurrentBiome >= biomes.Length) {
				Debug.LogWarning("Index: " + RMnoisesBiomeId.IndexOf(CurrentBiome) + ", Lenght: " + RMnoises.Count);
				return 0f;
			}
			return ((biomes[CurrentBiome].BiomeMinHeight)+((float)RMnoises[RMnoisesBiomeId.IndexOf(CurrentBiome)].GetValue(x,0f,y)*biomes[CurrentBiome].NoiseAmplitude))/blockSize;
			break;
		}
		return 0f;
	}

	public float GetWaterHeight(float x, float y, int blockSize) {
		return -20f/blockSize;
	}

	public int GetTextureTypeAtPixel (float x, float y, int blockSize) {
		float Height = GetHeightMap(x,y,blockSize)*blockSize;
		if(Height > -17) {
			return 0;
		} else {
			return 2;
		}
	}
}
