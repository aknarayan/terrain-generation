using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour {

  public enum DrawMode {
    NoiseMap,
    ColourMap,
    Mesh
  }

  public DrawMode drawMode;

  public const int mapChunkSize = 241; // 240 is a multiple of all even numbers in range [1, 12]
  [Range(0, 6)]
  public int editorPreviewLOD;
  public float noiseScale;

  public int octaves;
  [Range(0, 1)]
  public float persistence;
  public float lacunarity;

  public int seed;
  public Vector2 offset;

  public float meshHeightMultiplier;
  public AnimationCurve meshHeightCurve;

  public bool autoUpdate;

  public TerrainType[] regions;

  Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
  Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

  public void DrawMapInEditor() {
    MapData mapData = GenerateMapData();

    float[,] noiseMap = mapData.heightMap;
    Color[] colourMap = mapData.colourMap;

    MapDisplay display = FindObjectOfType<MapDisplay>();
    if (drawMode == DrawMode.NoiseMap) {
      display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
    } else if (drawMode == DrawMode.ColourMap) {
      display.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize));
    } else if (drawMode == DrawMode.Mesh) {
      display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD), TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize));
    }
  }

  public void RequestMapData(Action<MapData> callback) {
    ThreadStart threadStart = delegate {
      MapDataThread(callback);
    };

    new Thread(threadStart).Start();
  }

  void MapDataThread(Action<MapData> callback) {
    MapData mapData = GenerateMapData();
    lock (mapDataThreadInfoQueue) {
      mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
    }
  }

  public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
    ThreadStart threadStart = delegate {
      MeshDataThread(mapData, lod, callback);
    };

    new Thread(threadStart).Start();
  }

  void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
    MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
    lock (meshDataThreadInfoQueue) {
      meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
    }
  }

  private void Update() {
    if (mapDataThreadInfoQueue.Count > 0) {
      for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
        MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
        threadInfo.callback(threadInfo.parameter);
      }
    }

    if (meshDataThreadInfoQueue.Count > 0) {
      for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
        MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
        threadInfo.callback(threadInfo.parameter);
      }
    }
  }

  MapData GenerateMapData() {
    float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistence, lacunarity, offset);

    Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
    for (int y = 0; y < mapChunkSize; y++) {
      for (int x = 0; x < mapChunkSize; x++) {
        float currentHeight = noiseMap[x, y];
        foreach (TerrainType region in regions) {
          if (currentHeight <= region.height) {
            colourMap[y * mapChunkSize + x] = region.colour;
            break;
          }
        }
      }
    }

    return new MapData(noiseMap, colourMap);
  }

  private void OnValidate() {
    if (lacunarity < 1) {
      lacunarity = 1;
    }

    if (octaves < 0) {
      octaves = 0;
    }
  }

  struct MapThreadInfo<T> {
    public readonly Action<T> callback;
    public readonly T parameter;

    public MapThreadInfo(Action<T> callback, T parameter) {
      this.callback = callback;
      this.parameter = parameter;
    }
  }

}

[System.Serializable]
public struct TerrainType {
  public string name;
  public float height;
  public Color colour;
}

public struct MapData {
  public readonly float[,] heightMap;
  public readonly Color[] colourMap;

  public MapData(float[,] heightMap, Color[] colourMap) {
    this.heightMap = heightMap;
    this.colourMap = colourMap;
  }
}
