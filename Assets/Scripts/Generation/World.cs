using UnityEngine;

public class World : MonoBehaviour
{
    [SerializeField] WorldSettings worldSettings;
    WorldData worldData;
    public WorldData WorldData {get {return worldData;}}
    public WorldSettings WorldSettings { get{return worldSettings;}}

    void Awake()
    {
        worldData = WorldGenerator.GenerateWorld(worldSettings, worldSettings.XSize, worldSettings.YSize, worldSettings.ZSize, worldSettings.Seed);
    }
}
