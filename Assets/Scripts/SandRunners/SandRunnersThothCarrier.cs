using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class SandRunnersPrototype
{
    private enum ThothSlotState
    {
        Empty,
        Producing,
        Ready,
        Launching,
        Active,
        Returning,
        Landing,
        Servicing
    }

    private enum ThothAircraftClass
    {
        SunLancer,
        TombBomber,
        IbisGunship,
        ChronoKite
    }

    private sealed class ThothCarrierSlot
    {
        public int index;
        public bool heavy;
        public ThothAircraftClass aircraftClass;
        public string label;
        public ThothSlotState state;
        public float progress;
        public float duration;
        public RunnerUnit aircraft;
        public AirWingState wing;
    }

    private readonly List<ThothCarrierSlot> thothCarrierSlots = new List<ThothCarrierSlot>();
    private Transform thothCarrierRoot;
    private AirBaseState thothCarrierBase;
    private bool thothCarrierReady;
    private GameObject thothCarrierPanelObject;
    private Text thothCarrierStatusText;
    private readonly List<StrategicCanvasButton> thothCarrierButtons = new List<StrategicCanvasButton>();

    private void UpdateThothCarrier(float dt)
    {
        EnsureThothCarrier();
        if (!thothCarrierReady)
            return;

        UpdateThothCarrierProduction(dt);
        UpdateThothCarrierSlotStates();
        RouteDamagedAircraftToThoth();
        UpdateThothCarrierUi();
    }

    private void EnsureThothCarrier()
    {
        if (thothCarrierReady && thothCarrierRoot != null)
            return;

        RunnerUnit thoth = null;
        for (int i = 0; i < runners.Count; i++)
        {
            RunnerUnit candidate = runners[i];
            if (candidate != null && candidate.transform != null && candidate.displayName.Contains("Thoth"))
            {
                thoth = candidate;
                break;
            }
        }
        if (thoth == null)
            return;

        thothCarrierRoot = thoth.transform;
        thothCarrierBase = FindAviationBaseForCarrier(thothCarrierRoot);
        if (thothCarrierBase == null)
        {
            thothCarrierBase = new AirBaseState();
            thothCarrierBase.root = thothCarrierRoot;
            thothCarrierBase.carrier = true;
            thothCarrierBase.launchSlots = 2;
            thothCarrierBase.serviceSlots = 2;
            aviationBases.Add(thothCarrierBase);
        }

        if (thothCarrierSlots.Count == 0)
        {
            for (int i = 0; i < 8; i++)
            {
                ThothCarrierSlot slot = new ThothCarrierSlot();
                slot.index = i;
                slot.heavy = i >= 2;
                slot.aircraftClass = slot.heavy ? ThothAircraftClass.TombBomber : ThothAircraftClass.SunLancer;
                slot.label = slot.heavy ? "TOMB BOMBER " + (i - 1) : "SUN LANCER " + (i + 1);
                slot.state = ThothSlotState.Empty;
                thothCarrierSlots.Add(slot);
            }
        }

        thothCarrierReady = true;
        BuildThothCarrierDeckLights();
    }

    private AirBaseState FindAviationBaseForCarrier(Transform root)
    {
        for (int i = 0; i < aviationBases.Count; i++)
        {
            AirBaseState baseState = aviationBases[i];
            if (baseState != null && baseState.carrier && baseState.root == root)
                return baseState;
        }
        return null;
    }

    private void BuildThothCarrierDeckLights()
    {
        int lightCount = Mathf.Max(4, thothCarrierSlots.Count);
        for (int i = 0; i < lightCount; i++)
        {
            string lightName = "Thoth_Deck_Status_Light_" + i;
            if (thothCarrierRoot.Find(lightName) != null)
                continue;
            int col = i % 4;
            int row = i / 4;
            float x = (col - 1.5f) * 1.35f;
            float z = row == 0 ? 0.4f : -0.8f;
            CreatePointLight(thothCarrierRoot, lightName, new Vector3(x, 1.08f, z),
                i >= 4 ? new Color(0.7f, 0.35f, 1f, 1f) : new Color(0.2f, 0.82f, 1f, 1f), 0.35f, 5f);
        }
        CreatePointLight(thothCarrierRoot, "Thoth_Service_Bay_Light_Left", new Vector3(-0.75f, 0.9f, -1.2f), new Color(0.25f, 0.95f, 1f, 1f), 0.45f, 6f);
        CreatePointLight(thothCarrierRoot, "Thoth_Service_Bay_Light_Right", new Vector3(0.75f, 0.9f, -1.2f), new Color(0.25f, 0.95f, 1f, 1f), 0.45f, 6f);
    }

    private void UpdateThothCarrierProduction(float dt)
    {
        for (int i = 0; i < thothCarrierSlots.Count; i++)
        {
            ThothCarrierSlot slot = thothCarrierSlots[i];
            if (slot.state != ThothSlotState.Producing)
                continue;

            slot.progress += dt;
            if (slot.progress < slot.duration)
                continue;

            slot.aircraft = SpawnThothCarrierAircraft(slot);
            slot.progress = slot.duration;
            slot.state = ThothSlotState.Ready;
            PlaySandRunnerSound(SandRunnerSound.ProductionComplete, thothCarrierRoot.position, 0.8f);
            lastEvent = slot.label + " ready in Thoth's carrier bay. Select the carrier and press Launch.";
        }
    }

    private RunnerUnit SpawnThothCarrierAircraft(ThothCarrierSlot slot)
    {
        Vector3 position = GetThothDeckPosition(slot.index);
        switch (slot.aircraftClass)
        {
            case ThothAircraftClass.TombBomber:
                return CreateVehicleUnit("Thoth_Tomb_Bomber_" + slot.index, position, thothCarrierRoot.rotation,
                    new Vector3(1.28f, 0.76f, 1.55f), siegeUnitMaterial, 230f, 10.8f, 72f, 42f, slot.label, true, 11f);
            case ThothAircraftClass.IbisGunship:
                return CreateVehicleUnit("Thoth_Ibis_Gunship_" + slot.index, position, thothCarrierRoot.rotation,
                    new Vector3(1.18f, 0.82f, 1.38f), controlledMaterial, 260f, 9.6f, 46f, 34f, slot.label, true, 9.5f);
            case ThothAircraftClass.ChronoKite:
                return CreateVehicleUnit("Thoth_Chrono_Kite_" + slot.index, position, thothCarrierRoot.rotation,
                    new Vector3(1.05f, 0.46f, 1.7f), pyramidGlowMaterial, 150f, 18.2f, 28f, 38f, slot.label, true, 12f);
            default:
                return CreateVehicleUnit("Thoth_Sun_Lancer_" + slot.index, position, thothCarrierRoot.rotation,
                    new Vector3(0.92f, 0.55f, 1.22f), runnerMaterial, 120f, 16.8f, 30f, 34f, slot.label, true, 8f);
        }
    }

    private Vector3 GetThothDeckPosition(int index)
    {
        int col = index % 4;
        int row = index / 4;
        float x = (col - 1.5f) * 1.35f;
        float z = row == 0 ? 0.55f : -0.85f;
        return thothCarrierRoot.TransformPoint(new Vector3(x, 1.25f, z));
    }

    private Vector3 GetThothServicePosition(int serviceIndex)
    {
        float x = serviceIndex == 0 ? -0.75f : 0.75f;
        return thothCarrierRoot.TransformPoint(new Vector3(x, 1.65f, -1.15f));
    }

    private void UpdateThothCarrierSlotStates()
    {
        for (int i = 0; i < thothCarrierSlots.Count; i++)
        {
            ThothCarrierSlot slot = thothCarrierSlots[i];
            if (slot.aircraft == null || slot.aircraft.transform == null || slot.aircraft.health <= 0f)
            {
                slot.aircraft = null;
                slot.wing = null;
                if (slot.state != ThothSlotState.Producing)
                    slot.state = ThothSlotState.Empty;
                continue;
            }

            slot.wing = FindAirWing(slot.aircraft.squad);
            if (slot.wing == null)
                continue;

            slot.wing.baseState = thothCarrierBase;
            if (slot.state == ThothSlotState.Ready)
            {
                ParkThothAircraft(slot);
                continue;
            }
            if (slot.state == ThothSlotState.Launching && slot.wing.state == AirUnitState.Launching)
                continue;
            if (slot.state == ThothSlotState.Servicing && slot.wing.state == AirUnitState.Formation)
            {
                if (slot.aircraft.squad.autoEngage)
                {
                    LaunchThothSlot(slot);
                    lastEvent = slot.label + " serviced and automatically relaunched under " +
                        GetSquadDoctrineLabel(slot.aircraft.squad.doctrine) + " doctrine.";
                }
                else
                {
                    slot.state = ThothSlotState.Ready;
                    ParkThothAircraft(slot);
                }
                continue;
            }
            if (slot.wing.state == AirUnitState.Returning || slot.wing.state == AirUnitState.BreakOff)
                slot.state = ThothSlotState.Returning;
            else if (slot.wing.state == AirUnitState.Landing)
                slot.state = ThothSlotState.Landing;
            else if (slot.wing.state == AirUnitState.Repairing || slot.wing.state == AirUnitState.Rearming)
                slot.state = ThothSlotState.Servicing;
            else
                slot.state = ThothSlotState.Active;
        }
    }

    private void ParkThothAircraft(ThothCarrierSlot slot)
    {
        if (slot == null || slot.aircraft == null || slot.aircraft.transform == null || slot.wing == null)
            return;

        slot.wing.state = AirUnitState.HangarReady;
        slot.wing.mission = AirMission.Hold;
        slot.wing.endurance = slot.wing.maxEndurance;
        slot.wing.ammo = slot.wing.maxAmmo;
        slot.aircraft.squad.orderType = OrderType.None;
        slot.aircraft.squad.attackTarget = null;
        slot.aircraft.transform.position = GetThothDeckPosition(slot.index);
        slot.aircraft.transform.rotation = thothCarrierRoot.rotation;
    }

    private void RouteDamagedAircraftToThoth()
    {
        if (thothCarrierRoot == null)
            return;

        int serviceCount = 0;
        for (int i = 0; i < aviationWings.Count; i++)
        {
            AirWingState wing = aviationWings[i];
            if (wing != null && wing.baseState == thothCarrierBase && wing.state != AirUnitState.Formation && wing.state != AirUnitState.AttackRun)
                serviceCount++;
        }
        if (serviceCount >= 2)
            return;

        for (int i = 0; i < aviationWings.Count; i++)
        {
            AirWingState wing = aviationWings[i];
            if (wing == null || wing.squad == null || wing.baseState == thothCarrierBase)
                continue;
            if (GetAirWingHealthRatio(wing) > 0.48f && wing.ammo > 0)
                continue;
            if (FlatDistance(GetAirWingCentroid(wing), thothCarrierRoot.position) > 90f)
                continue;

            wing.baseState = thothCarrierBase;
            wing.state = AirUnitState.Returning;
            wing.mission = AirMission.ReturnToBase;
            serviceCount++;
            PlaySandRunnerSound(SandRunnerSound.AircraftReturn, thothCarrierRoot.position, 0.45f);
            if (serviceCount >= 2)
                break;
        }
    }

    private void HandleThothSlotAction(int index)
    {
        EnsureThothCarrier();
        if (!thothCarrierReady || index < 0 || index >= thothCarrierSlots.Count)
            return;

        ThothCarrierSlot slot = thothCarrierSlots[index];
        if (slot.state == ThothSlotState.Empty)
        {
            float sandCost = 110f;
            float goldCost = 140f;
            float windCost = 22f;
            if (slot.aircraftClass == ThothAircraftClass.TombBomber)
            {
                sandCost = 170f; goldCost = 195f; windCost = 32f;
            }
            else if (slot.aircraftClass == ThothAircraftClass.IbisGunship)
            {
                sandCost = 145f; goldCost = 210f; windCost = 30f;
            }
            else if (slot.aircraftClass == ThothAircraftClass.ChronoKite)
            {
                sandCost = 90f; goldCost = 230f; windCost = 42f;
            }
            if (!TrySpendResources(sandCost, goldCost, windCost, slot.label))
                return;
            slot.state = ThothSlotState.Producing;
            slot.progress = 0f;
            slot.duration = slot.aircraftClass == ThothAircraftClass.TombBomber ? 18f :
                slot.aircraftClass == ThothAircraftClass.IbisGunship ? 16f :
                slot.aircraftClass == ThothAircraftClass.ChronoKite ? 15f : 13f;
            lastEvent = slot.label + " production started in Thoth's carrier.";
            PlaySandRunnerSound(SandRunnerSound.ProductionBuild, thothCarrierRoot.position, 0.55f);
        }
        else if (slot.state == ThothSlotState.Ready)
        {
            LaunchThothSlot(slot);
        }
        else
        {
            lastEvent = slot.label + " is currently " + slot.state + ".";
        }
    }

    private void LaunchThothSlot(ThothCarrierSlot slot)
    {
        if (slot == null || slot.aircraft == null || slot.aircraft.squad == null)
            return;
        slot.state = ThothSlotState.Launching;
        if (slot.wing == null)
            slot.wing = FindAirWing(slot.aircraft.squad);
        if (slot.wing != null)
        {
            slot.wing.baseState = thothCarrierBase;
            slot.wing.state = AirUnitState.Launching;
            slot.wing.stateTimer = 2.2f;
            slot.wing.mission = AirMission.Move;
            slot.wing.endurance = slot.wing.maxEndurance;
        }
        slot.aircraft.squad.autoEngage = true;
        slot.aircraft.squad.doctrineTarget = null;
        slot.aircraft.squad.nextDoctrineScanTime = 0f;
        slot.aircraft.squad.orderType = OrderType.Move;
        slot.aircraft.squad.orderPosition = thothCarrierRoot.position + thothCarrierRoot.forward * 25f + Vector3.up * 14f;
        PlaySandRunnerSound(SandRunnerSound.AircraftTakeoff, GetThothDeckPosition(slot.index), 0.72f);
        lastEvent = slot.label + " launching from Thoth's flight deck.";
    }

    private void InitializeThothCarrierUi()
    {
        if (thothCarrierPanelObject != null || strategicCanvas == null)
            return;

        RectTransform root = strategicCanvas.GetComponent<RectTransform>();
        RectTransform panel = CreatePanel(root, "Thoth_Carrier_Panel", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 340f), new Vector2(510f, 202f), UiGlassDeep);
        thothCarrierPanelObject = panel.gameObject;
        thothCarrierStatusText = CreateText(panel, "Thoth_Status", "THOTH'S EMBRACE // MOBILE AIRBASE", 12, FontStyle.Bold, new Vector2(12f, -8f), new Vector2(480f, 26f), UiGold);
        for (int i = 0; i < 8; i++)
        {
            int captured = i;
            StrategicCanvasButton button = CreateButton(panel, "Thoth_Slot_" + i, "Slot " + (i + 1), new Vector2(12f + (i % 2) * 244f, -38f - (i / 2) * 38f), new Vector2(232f, 32f), () => HandleThothSlotAction(captured));
            thothCarrierButtons.Add(button);
        }
        thothCarrierPanelObject.SetActive(false);
    }

    private void UpdateThothCarrierUi()
    {
        if (thothCarrierPanelObject == null)
            return;

        bool selected = commandCursorMode && selectedStrategicTransform == thothCarrierRoot && thothCarrierReady;
        if (thothCarrierPanelObject.activeSelf != selected)
            thothCarrierPanelObject.SetActive(selected);
        if (!selected)
            return;

        for (int i = 0; i < thothCarrierButtons.Count; i++)
            thothCarrierButtons[i].button.gameObject.SetActive(i < thothCarrierSlots.Count);

        int active = 0;
        int servicing = 0;
        for (int i = 0; i < thothCarrierSlots.Count; i++)
        {
            ThothCarrierSlot slot = thothCarrierSlots[i];
            if (slot.state == ThothSlotState.Active)
                active++;
            if (slot.state == ThothSlotState.Servicing || slot.state == ThothSlotState.Landing || slot.state == ThothSlotState.Returning)
                servicing++;
            string progress = slot.state == ThothSlotState.Producing ? " " + Mathf.RoundToInt(slot.progress / Mathf.Max(0.1f, slot.duration) * 100f) + "%" : string.Empty;
            SetButtonText(thothCarrierButtons[i], slot.label + " | " + slot.state + progress);
        }
        if (thothCarrierStatusText != null)
            thothCarrierStatusText.text = (thothCarrierSlots.Count >= 8 ? "BLESSING OF THOTH" : "THOTH'S EMBRACE") +
                " // " + active + "/" + thothCarrierSlots.Count + " ACTIVE // SERVICE " +
                Mathf.Min(thothCarrierBase != null ? thothCarrierBase.serviceSlots : 2, servicing) +
                " // wings inherit squad doctrine and auto-relaunch after service";
    }

    private void UpgradeThothCarrierToBlessing(Transform transformedRoot, ResourceKind resourceKind)
    {
        if (transformedRoot == null)
            return;

        EnsureThothCarrier();
        if (thothCarrierRoot != transformedRoot)
            thothCarrierRoot = transformedRoot;

        string[] names =
        {
            "SUN LANCER 1", "SUN LANCER 2",
            "TOMB BOMBER 1", "TOMB BOMBER 2",
            "IBIS GUNSHIP 1", "IBIS GUNSHIP 2",
            "CHRONO KITE 1", "CHRONO KITE 2"
        };
        ThothAircraftClass[] classes =
        {
            ThothAircraftClass.SunLancer, ThothAircraftClass.SunLancer,
            ThothAircraftClass.TombBomber, ThothAircraftClass.TombBomber,
            ThothAircraftClass.IbisGunship, ThothAircraftClass.IbisGunship,
            ThothAircraftClass.ChronoKite, ThothAircraftClass.ChronoKite
        };

        while (thothCarrierSlots.Count < 8)
            thothCarrierSlots.Add(new ThothCarrierSlot());

        for (int i = 0; i < 8; i++)
        {
            ThothCarrierSlot slot = thothCarrierSlots[i];
            slot.index = i;
            slot.aircraftClass = classes[i];
            slot.heavy = classes[i] == ThothAircraftClass.TombBomber ||
                classes[i] == ThothAircraftClass.IbisGunship;
            slot.label = names[i] + " // " + GetResourceKitName(resourceKind);
        }

        if (thothCarrierBase != null)
        {
            thothCarrierBase.launchSlots = 4;
            thothCarrierBase.serviceSlots = 4;
        }
        BuildThothCarrierDeckLights();
        lastEvent = "Blessing of Thoth unfolded eight flight decks and four experimental aircraft classes.";
    }
}