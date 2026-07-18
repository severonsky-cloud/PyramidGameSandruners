using System.Collections.Generic;
using UnityEngine;

public partial class SandRunnersPrototype
{
    private sealed class ConvoyEscortPresentation
    {
        public readonly List<Transform> boats = new List<Transform>();
        public float fireTimer;
    }

    private sealed class CargoFlyerPresentation
    {
        public Transform body;
        public readonly List<Transform> rotors = new List<Transform>();
        public float phase;
        public float fireTimer;
    }

    private readonly Dictionary<MineConvoyState, ConvoyEscortPresentation> mineConvoyEscorts = new();
    private readonly Dictionary<ResourceCargo, CargoFlyerPresentation> heavyCargoFlyers = new();

    private void AttachMineConvoyEscorts(MineConvoyState convoy, int level)
    {
        if (convoy == null || convoy.root == null || mineConvoyEscorts.ContainsKey(convoy)) return;
        ConvoyEscortPresentation p = new();
        int count = SandRunnersResourceDevelopmentRules.ConvoyEscortCount(level);
        for (int i = 0; i < count; i++) p.boats.Add(CreateEscortBoat(convoy.root.position, i));
        mineConvoyEscorts.Add(convoy, p);
    }

    private Transform CreateEscortBoat(Vector3 position, int index)
    {
        Transform root = new GameObject("SR_Automatic_Flying_Gunboat_Escort_" + index).transform;
        root.position = position + Vector3.up * 5f;
        LogisticsPart(root, "Armored_Keel", PrimitiveType.Cube, Vector3.zero, new Vector3(1.4f, .28f, 2.7f), pyramidDarkArmorMaterial);
        LogisticsPart(root, "Golden_Deck", PrimitiveType.Cube, new Vector3(0f, .34f, -.15f), new Vector3(1.05f, .22f, 1.75f), pyramidGoldMaterial);
        LogisticsPart(root, "Black_Glass_Bridge", PrimitiveType.Cube, new Vector3(0f, .62f, -.35f), new Vector3(.62f, .28f, .72f), pyramidHangarGlassMaterial);
        LogisticsPart(root, "Port_Lift_Pod", PrimitiveType.Cylinder, new Vector3(-.95f, 0f, .15f), new Vector3(.34f, .12f, .34f), pyramidGoldMaterial);
        LogisticsPart(root, "Starboard_Lift_Pod", PrimitiveType.Cylinder, new Vector3(.95f, 0f, .15f), new Vector3(.34f, .12f, .34f), pyramidGoldMaterial);
        LogisticsPart(root, "Autocannon", PrimitiveType.Cylinder, new Vector3(0f, .68f, .65f), new Vector3(.12f, .72f, .12f), pyramidDarkArmorMaterial, Quaternion.Euler(90f, 0f, 0f));
        return root;
    }

    private void UpdateMineConvoyEscorts(MineConvoyState convoy, float dt)
    {
        if (convoy == null || convoy.root == null || !mineConvoyEscorts.TryGetValue(convoy, out ConvoyEscortPresentation p)) return;
        p.fireTimer = Mathf.Max(0f, p.fireTimer - dt);
        for (int i = 0; i < p.boats.Count; i++)
        {
            Transform boat = p.boats[i];
            if (boat == null) continue;
            float side = i == 0 ? -1f : 1f;
            Vector3 goal = convoy.root.position + convoy.root.right * side * 4.5f - convoy.root.forward * 1.8f + Vector3.up * 5f;
            boat.position = Vector3.Lerp(boat.position, goal, 1f - Mathf.Exp(-5f * dt));
            if (convoy.root.forward.sqrMagnitude > .1f)
                boat.rotation = Quaternion.Slerp(boat.rotation, Quaternion.LookRotation(convoy.root.forward), 1f - Mathf.Exp(-6f * dt));
        }

        EnemyUnit target = FindNearestEnemy(convoy.root.position, balanceProfile.mineConvoyEscortRange);
        if (target == null || target.transform == null || p.fireTimer > 0f || p.boats.Count == 0) return;
        p.fireTimer = .32f;
        target.health -= balanceProfile.mineConvoyEscortDamage;
        Transform gun = p.boats[Random.Range(0, p.boats.Count)];
        if (gun != null)
        {
            Vector3 muzzle = gun.position + gun.forward * 1.5f + Vector3.up * .35f;
            CreateWeaponTracer(muzzle, target.transform.position + Vector3.up * .7f, new Color(1f, .72f, .18f), .04f, .12f);
            CreateWeaponFlash(muzzle, .16f, new Color(1f, .72f, .18f));
        }
    }

    private void RetireMineConvoyEscorts(MineConvoyState convoy)
    {
        if (convoy == null || !mineConvoyEscorts.TryGetValue(convoy, out ConvoyEscortPresentation p)) return;
        foreach (Transform boat in p.boats) if (boat != null) Destroy(boat.gameObject);
        mineConvoyEscorts.Remove(convoy);
    }

    private void ConfigureHeavyCargoFlyer(ResourceCargo cargo)
    {
        if (cargo == null || cargo.transform == null || heavyCargoFlyers.ContainsKey(cargo)) return;
        CargoFlyerPresentation p = new();
        p.body = new GameObject("Heavy_Cargo_Flyer_Visual").transform;
        p.body.SetParent(cargo.transform, false);
        p.phase = Random.value * Mathf.PI * 2f;
        LogisticsPart(p.body, "Armored_Fuselage", PrimitiveType.Cube, Vector3.zero, new Vector3(3.2f, .72f, 5.2f), pyramidDarkArmorMaterial);
        LogisticsPart(p.body, "Golden_Spine", PrimitiveType.Cube, new Vector3(0f, .72f, -.25f), new Vector3(1.45f, .42f, 3.45f), pyramidGoldMaterial);
        LogisticsPart(p.body, "Black_Glass_Bridge", PrimitiveType.Cube, new Vector3(0f, 1.05f, 1.25f), new Vector3(1.15f, .48f, 1.15f), pyramidHangarGlassMaterial);
        LogisticsPart(p.body, "Port_Wing", PrimitiveType.Cube, new Vector3(-3.25f, 0f, -.25f), new Vector3(3.3f, .18f, 1.65f), pyramidDarkArmorMaterial, Quaternion.Euler(0f, 0f, -5f));
        LogisticsPart(p.body, "Starboard_Wing", PrimitiveType.Cube, new Vector3(3.25f, 0f, -.25f), new Vector3(3.3f, .18f, 1.65f), pyramidDarkArmorMaterial, Quaternion.Euler(0f, 0f, 5f));
        for (int i = 0; i < 3; i++)
            LogisticsPart(p.body, "Cargo_Module_" + i, PrimitiveType.Cube, new Vector3((i - 1) * 1.05f, -.65f, -.65f), new Vector3(.9f, .68f, 1.55f), pyramidGoldMaterial);

        Vector3[] points = { new(-4.35f,.15f,.55f), new(4.35f,.15f,.55f), new(-4.35f,.15f,-1.25f), new(4.35f,.15f,-1.25f) };
        for (int i = 0; i < points.Length; i++)
        {
            Transform rotor = LogisticsPart(p.body, "Lift_Rotor_" + i, PrimitiveType.Cylinder, points[i], new Vector3(.82f,.08f,.82f), pyramidGoldMaterial);
            LogisticsPart(rotor, "Rotor_Blade", PrimitiveType.Cube, Vector3.zero, new Vector3(2.2f,.035f,.13f), pyramidDarkArmorMaterial);
            p.rotors.Add(rotor);
        }
        LogisticsPart(p.body, "Dorsal_30mm_Left", PrimitiveType.Cylinder, new Vector3(-.38f,1.18f,.2f), new Vector3(.12f,.85f,.12f), pyramidDarkArmorMaterial, Quaternion.Euler(90f,0f,0f));
        LogisticsPart(p.body, "Dorsal_30mm_Right", PrimitiveType.Cylinder, new Vector3(.38f,1.18f,.2f), new Vector3(.12f,.85f,.12f), pyramidDarkArmorMaterial, Quaternion.Euler(90f,0f,0f));
        cargo.transform.gameObject.AddComponent<SphereCollider>().radius = 4.5f;
        heavyCargoFlyers.Add(cargo, p);
    }

    private void UpdateHeavyCargoFlyer(ResourceCargo cargo, float dt)
    {
        if (cargo == null || cargo.transform == null || !heavyCargoFlyers.TryGetValue(cargo, out CargoFlyerPresentation p)) return;
        p.phase += dt * 2.2f;
        if (p.body != null) p.body.localPosition = Vector3.up * (Mathf.Sin(p.phase) * .12f);
        foreach (Transform rotor in p.rotors) if (rotor != null) rotor.Rotate(Vector3.up, 720f * dt, Space.Self);
        p.fireTimer = Mathf.Max(0f, p.fireTimer - dt);
        EnemyUnit target = FindNearestEnemy(cargo.transform.position, balanceProfile.cargoFlyerDefenseRange);
        if (target == null || target.transform == null || p.fireTimer > 0f) return;
        p.fireTimer = .28f;
        target.health -= balanceProfile.cargoFlyerDefenseDamage;
        Vector3 muzzle = cargo.transform.position + cargo.transform.forward * 1.1f + Vector3.up * 1.4f;
        CreateWeaponTracer(muzzle, target.transform.position + Vector3.up * .8f, new Color(1f,.75f,.2f), .055f, .14f);
        CreateWeaponFlash(muzzle, .2f, new Color(1f,.72f,.18f));
    }

    private void ReleaseHeavyCargoFlyer(ResourceCargo cargo)
    {
        if (cargo != null) heavyCargoFlyers.Remove(cargo);
    }

    private Transform LogisticsPart(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Material material, Quaternion? rotation = null)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = rotation ?? Quaternion.identity;
        part.transform.localScale = scale;
        if (material != null && part.TryGetComponent(out Renderer renderer))
            renderer.sharedMaterial = material;
        if (part.TryGetComponent(out Collider collider)) Destroy(collider);
        return part.transform;
    }
}