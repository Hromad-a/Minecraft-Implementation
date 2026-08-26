using UnityEngine;

public class World : MonoBehaviour
{
    [SerializeField] WorldSettings worldSettings;
    WorldData worldData;

    void Start()
    {
        worldData = new WorldData(worldSettings.Seed, worldSettings.YSize);
    }
}
