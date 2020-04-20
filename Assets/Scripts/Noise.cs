using System.Collections;
using UnityEngine;

public static class Noise {

  public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float scale) {
    float[,] noiseMap = new float[mapWidth, mapHeight];
    
    for (int i = 0; i < mapHeight; i++) {
      for (int j = 0; j < mapWidth; j++) {
        float sampleJ = j / scale;
        float sampleI = i / scale;
        
        float perlinValue = Mathf.PerlinNoise(sampleJ, sampleI);

        noiseMap[j, i] = perlinValue;
      }
    }
    return noiseMap;
  }

}
