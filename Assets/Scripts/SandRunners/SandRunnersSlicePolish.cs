using System.Collections.Generic;
using UnityEngine;

public partial class SandRunnersPrototype
{
    private readonly List<Transform> sliceWindmillBlades = new List<Transform>();
    private readonly List<Light> sliceResourceLights = new List<Light>();
    private readonly Dictionary<Light, float> sliceResourceBaseIntensity = new Dictionary<Light, float>();
    private readonly List<Transform> sliceMechanicalParts = new List<Transform>();
    private readonly Dictionary<Transform, Vector3> sliceMechanicalBasePositions = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, Quaternion> sliceMechanicalBaseRotations = new Dictionary<Transform, Quaternion>();
    private readonly List<GameObject> sliceDistanceDetails = new List<GameObject>();
    private bool slicePolishReady;
    private float slicePolishTimer;
    private float sliceWindAudioTimer;
    private float sliceLodTimer;

    private void InitializeSlicePolish()
    {
        if (slicePolishReady)
            return;

        sliceWindmillBlades.Clear();
        sliceResourceLights.Clear();
        sliceResourceBaseIntensity.Clear();
        sliceMechanicalParts.Clear();
        sliceMechanicalBasePositions.Clear();
        sliceMechanicalBaseRotations.Clear();
        sliceDistanceDetails.Clear();
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null)
                continue;
            if (candidate.name.Contains("Windmill_Blade"))
                sliceWindmillBlades.Add(candidate);

            bool mechanical = candidate.name.Contains("Mine_Conveyor") || candidate.name.Contains("Excavator_Boom") ||
                candidate.name.Contains("Excavator_Bucket") || candidate.name.Contains("Crane_Arm") ||
                candidate.name.Contains("Service_Indicator") || candidate.name.Contains("Deck_Light");
            if (mechanical)
            {
                sliceMechanicalParts.Add(candidate);
                sliceMechanicalBasePositions[candidate] = candidate.localPosition;
                sliceMechanicalBaseRotations[candidate] = candidate.localRotation;
            }

            if (candidate.name.Contains("Windmill_Blade") || candidate.name.Contains("Mine_Conveyor") ||
                candidate.name.Contains("Excavator_Bucket") || candidate.name.Contains("Crane_Arm") ||
                candidate.name.Contains("Navigation") || candidate.name.Contains("Service_Indicator") || candidate.name.Contains("Deck_Light"))
                sliceDistanceDetails.Add(candidate.gameObject);

            if (candidate.name.Contains("Mining_Beacon") || candidate.name.Contains("Worksite_Light") || candidate.name.Contains("Wind_Generator_Light"))
            {
                Light light = candidate.GetComponent<Light>();
                if (light != null)
                {
                    sliceResourceLights.Add(light);
                    sliceResourceBaseIntensity[light] = light.intensity;
                }
            }
        }

        Color neutralAmbient = new Color(0.43f, 0.39f, 0.34f, 1f);
        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, neutralAmbient, 0.32f);
        RenderSettings.fogColor = new Color(0.26f, 0.23f, 0.2f, 1f);
        RenderSettings.fogDensity = Mathf.Min(RenderSettings.fogDensity, 0.0011f);
        slicePolishReady = true;
    }

    private void UpdateSlicePolish(float dt)
    {
        if (!slicePolishReady)
            InitializeSlicePolish();

        slicePolishTimer += dt;
        sliceWindAudioTimer -= dt;
        sliceLodTimer -= dt;
        for (int i = sliceWindmillBlades.Count - 1; i >= 0; i--)
        {
            Transform blade = sliceWindmillBlades[i];
            if (blade == null)
            {
                sliceWindmillBlades.RemoveAt(i);
                continue;
            }
            blade.Rotate(Vector3.forward, 45f * dt, Space.Self);
        }

        for (int i = sliceMechanicalParts.Count - 1; i >= 0; i--)
        {
            Transform part = sliceMechanicalParts[i];
            if (part == null)
            {
                sliceMechanicalParts.RemoveAt(i);
                continue;
            }
            if (!part.gameObject.activeInHierarchy)
                continue;
            Vector3 basePosition = sliceMechanicalBasePositions.ContainsKey(part) ? sliceMechanicalBasePositions[part] : part.localPosition;
            Quaternion baseRotation = sliceMechanicalBaseRotations.ContainsKey(part) ? sliceMechanicalBaseRotations[part] : part.localRotation;
            if (part.name.Contains("Conveyor"))
                part.localPosition = basePosition + (part.localRotation * Vector3.forward) * (Mathf.Sin(slicePolishTimer * 2.6f + i) * 0.22f);
            else if (part.name.Contains("Excavator"))
                part.localRotation = baseRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(slicePolishTimer * 0.82f + i) * 8f);
            else if (part.name.Contains("Crane_Arm"))
                part.localRotation = baseRotation * Quaternion.Euler(0f, Mathf.Sin(slicePolishTimer * 0.34f + i) * 18f, 0f);
            else
                part.localRotation = baseRotation * Quaternion.Euler(0f, Mathf.Sin(slicePolishTimer * 1.6f + i) * 4f, 0f);
        }

        float pulse = 0.78f + Mathf.Sin(slicePolishTimer * 2.2f) * 0.22f;
        for (int i = sliceResourceLights.Count - 1; i >= 0; i--)
        {
            Light light = sliceResourceLights[i];
            if (light == null)
            {
                sliceResourceLights.RemoveAt(i);
                continue;
            }
            float baseIntensity = sliceResourceBaseIntensity.ContainsKey(light) ? sliceResourceBaseIntensity[light] : light.intensity;
            light.intensity = Mathf.Max(0.15f, baseIntensity * pulse);
        }

        if (sliceLodTimer <= 0f && mainCamera != null)
        {
            sliceLodTimer = 0.35f;
            Vector3 cameraPosition = mainCamera.transform.position;
            float detailDistanceSq = presentationBudget.detailCullDistance * presentationBudget.detailCullDistance;
            for (int i = sliceDistanceDetails.Count - 1; i >= 0; i--)
            {
                GameObject detail = sliceDistanceDetails[i];
                if (detail == null)
                {
                    sliceDistanceDetails.RemoveAt(i);
                    continue;
                }
                bool visible = Vector3.SqrMagnitude(detail.transform.position - cameraPosition) <= detailDistanceSq;
                if (detail.activeSelf != visible)
                    detail.SetActive(visible);
            }
            float lightDistanceSq = presentationBudget.lightCullDistance * presentationBudget.lightCullDistance;
            for (int i = 0; i < sliceResourceLights.Count; i++)
            {
                Light light = sliceResourceLights[i];
                if (light != null)
                    light.enabled = Vector3.SqrMagnitude(light.transform.position - cameraPosition) <= lightDistanceSq;
            }
        }

        if (sliceWindAudioTimer <= 0f && sliceWindmillBlades.Count > 0)
        {
            sliceWindAudioTimer = 8f;
            Transform source = sliceWindmillBlades[0];
            if (source != null)
                PlaySandRunnerSound(SandRunnerSound.WindGenerator, source.position, 0.18f);
        }
    }
}
