using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SandRunnersExpeditionRouteReadability : MonoBehaviour
{
    private const float StartX = -627f;
    private const float StartZ = -589f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (Object.FindFirstObjectByType<SandRunnersExpeditionRouteReadability>() != null) return;
        GameObject root = new GameObject("SandRunners Expedition Route Readability");
        Object.DontDestroyOnLoad(root);
        root.AddComponent<SandRunnersExpeditionRouteReadability>();
        CreateRouteBeacon(root.transform, "Route beacon - Old Wall", new Vector3(StartX + 235f, 9f, StartZ + 210f), new Color(0.9f, 0.32f, 0.12f));
        CreateRouteBeacon(root.transform, "Route beacon - Wind Pass", new Vector3(StartX + 300f, 9f, StartZ + 285f), new Color(0.18f, 0.66f, 0.95f));
        CreateRouteBeacon(root.transform, "Route beacon - Safe Canyon", new Vector3(StartX + 305f, 9f, StartZ + 180f), new Color(0.22f, 0.8f, 0.42f));
    }

    private static void CreateRouteBeacon(Transform parent, string beaconName, Vector3 position, Color color)
    {
        GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beacon.name = beaconName;
        beacon.transform.SetParent(parent, true);
        beacon.transform.position = position;
        beacon.transform.localScale = new Vector3(2.2f, 9f, 2.2f);
        Collider collider = beacon.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);
        Renderer renderer = beacon.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (renderer != null && shader != null)
        {
            Material material = new Material(shader) { color = color };
            renderer.material = material;
        }
    }

    private void OnGUI()
    {
        // Route details are owned by the contextual notification rail.
    }

}