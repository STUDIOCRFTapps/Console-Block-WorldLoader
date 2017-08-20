using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class WorldLoader : MonoBehaviour {

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

	Vector2 OldChunkPos;
	Vector2 NewChunkPos;

	public Biome[] biomes;

	List<ChunkParameters> NewChunkRequirement;

	List<Vector2> DoNotRecreate = new List<Vector2>();
	List<int> GarbadgeChunk = new List<int>();
	Chunk[] ChunkList;

	void Start () {
		//Prepare world reference map, it's useless for you
		worldCreator.Execute();

		//Prepare the biome setup
		worldManager = new WorldManager(worldCreator,biomes);
		OldChunkPos = /*calculate player pos in chunk scale (16x16)*/;
		PlayerBlockPos = /*calculate player pos in units*/;

		//create new chunk
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

		//Collision stuff

		//Verify if the player moved
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

		Action Preparing = () => {
			NewChunkRequirement = new List<ChunkParameters>();

			//Determine what chunk LOD/Pos are require
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

			//Prepare the obj pool list
			GarbadgeChunk.Clear();

			//Prepare the list of chunk that should be update
			DoNotRecreate.Clear();
			NewChunk = new Chunk[NewChunkRequirement.Count];
		};

		//Execute thread
		PreparingThread = new Thread(new ThreadStart(Preparing));
		PreparingThread.Start();
		yield return new WaitUntil(() => !PreparingThread.IsAlive);

		//Put in the pool (garbage) what's usless and prevent from recreating what's good.
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

		//Execute thread
		CleaningThread = new Thread(new ThreadStart(Cleaning));
		CleaningThread.Start();
		yield return new WaitUntil(() => !CleaningThread.IsAlive);

		//There's no way to describ that sh*t
		Action Generating = () => {

			//Loop bettween all chunk requirements
			for(int i = 0; i < NewChunkRequirement.Count; i++) {

				//If it shouldn't be recreated, do nothing, else continue
				bool DoNotRecreateGate = DoNotRecreate.Contains(NewChunkRequirement[i].ChunkPos);
				if(!DoNotRecreateGate) {
					//(The chunk should be recreated)

					//Verify if there's garbage available :D
					if(GarbadgeChunk.Count <= 0) {
						break;
					}

					//Take the garbage chunk from the pool
					int c = GarbadgeChunk[0];
					GarbadgeChunk.RemoveAt(0);

					int c2 = i;

					//Recondition it by moving the postion/changing the LOD level/changing the scale
					ChunkList[c].ReconditionMesh(NewChunkRequirement[c2].ChunkPos,Mathf.RoundToInt(Mathf.Pow(2,NewChunkRequirement[c2].LevelOfDetails)),NewChunkRequirement[c2].ColliderMesh);

					//Prepare verts, tris and all taht stuff
					ChunkList[c].PrepareGeneration();

					//Declare a action that will create the mesh with what's been prepared earlier
					Action GenerateChunk = () => {
						ChunkList[c].GenerateWorldObject();

					};

					//Store it for later
					PrepareActions.Add(GenerateChunk);
				} else {
					// Why is this here?
				}
			}
		};

		//Execute the thread
		GeneratingThread = new Thread(new ThreadStart(Generating));
		GeneratingThread.Start();
		yield return new WaitUntil(() => !GeneratingThread.IsAlive);

		//Remeber that "PrepareActions" list form earlier? Here whe execute it (oldest to newest)
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

//CHUNK THIGHNY
public class Chunk {
	blablabla
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

	//Create noise and a list to remember wich biome has what noise
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
		return Mathf.Clamp((int)(Mathf.PerlinNoise(x*0.005f,y*0.005f)*2),0,1);
	}

	public float Distance (float a, float b) {
		return Mathf.Abs(a-b);
	}

	//GOOD LUCK TRYING TO FIGURE OUT HOW IT WORKS
	public float GetHeightMap(float x, float y, int blockSize) {
		//return (float)noise.GetValue(x,0f,y)*(parameters.Amplitude/blockSize);
		//return creator.GetHeight(x/(float)creator.WorldCreationRation,y/(float)creator.WorldCreationRation)*(parameters.Amplitude/blockSize);

		CurrentBiome = GetBiomeAt(x,y,blockSize);

		switch(biomes[CurrentBiome].noiseType) {
		case Biome.NoiseType.Billow:
			return ABunchOfNoiseCalculation;
			break;
		case Biome.NoiseType.Perlin:
			return ABunchOfNoiseCalculation;
			break;
		case Biome.NoiseType.RiggedMultifractal:
			return ABunchOfNoiseCalculation;
			break;
		}
		return 0f;
	}

	//USLESS
	public float GetWaterHeight(float x, float y, int blockSize) {
		return -20f/blockSize;
	}

	//USLESS
	public int GetTextureTypeAtPixel (float x, float y, int blockSize) {
		float Height = GetHeightMap(x,y,blockSize)*blockSize;
		if(Height > -17) {
			return 0;
		} else {
			return 2;
		}
	}
}
