using System.Collections.Generic;
using UnityEngine;

public partial class SandRunnersPrototype
{
    private sealed class DoctrineZoneVisual
    {
        public UnitSquad squad;
        public GameObject root;
        public LineRenderer ring;
        public LineRenderer tether;
        public TextMesh label;
    }

    private readonly List<DoctrineZoneVisual> doctrineZoneVisuals = new List<DoctrineZoneVisual>();
    private Material doctrineLineMaterial;
    private float doctrinePresentationTimer;

    private void UpdateSquadDoctrinePresentation(float dt)
    {
        doctrinePresentationTimer -= dt;
        if (doctrinePresentationTimer > 0f)
            return;
        doctrinePresentationTimer = 0.1f;

        EnsureDoctrineLineMaterial();
        for (int i = doctrineZoneVisuals.Count - 1; i >= 0; i--)
        {
            DoctrineZoneVisual visual = doctrineZoneVisuals[i];
            if (visual == null || visual.squad == null || !unitSquads.Contains(visual.squad))
            {
                if (visual != null && visual.root != null)
                    Destroy(visual.root);
                doctrineZoneVisuals.RemoveAt(i);
            }
        }

        for (int i = 0; i < unitSquads.Count; i++)
        {
            UnitSquad squad = unitSquads[i];
            if (squad == null)
                continue;

            DoctrineZoneVisual visual = FindDoctrineZoneVisual(squad);
            bool visible = commandCursorMode && squad.selected;
            if (!visible)
            {
                if (visual != null && visual.root != null)
                    visual.root.SetActive(false);
                continue;
            }

            if (visual == null)
                visual = CreateDoctrineZoneVisual(squad);
            visual.root.SetActive(true);
            UpdateDoctrineZoneVisual(visual);
        }
    }

    private void EnsureDoctrineLineMaterial()
    {
        if (doctrineLineMaterial != null)
            return;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        doctrineLineMaterial = new Material(shader);
        doctrineLineMaterial.name = "Doctrine Zone Shared";
    }

    private DoctrineZoneVisual FindDoctrineZoneVisual(UnitSquad squad)
    {
        for (int i = 0; i < doctrineZoneVisuals.Count; i++)
        {
            if (doctrineZoneVisuals[i] != null && doctrineZoneVisuals[i].squad == squad)
                return doctrineZoneVisuals[i];
        }
        return null;
    }

    private DoctrineZoneVisual CreateDoctrineZoneVisual(UnitSquad squad)
    {
        DoctrineZoneVisual visual = new DoctrineZoneVisual();
        visual.squad = squad;
        visual.root = new GameObject("Doctrine_Zone_" + squad.id);

        GameObject ringObject = new GameObject("Doctrine_Radius");
        ringObject.transform.SetParent(visual.root.transform, false);
        visual.ring = ringObject.AddComponent<LineRenderer>();
        ConfigureDoctrineLine(visual.ring, true);
        visual.ring.positionCount = 65;

        GameObject tetherObject = new GameObject("Doctrine_Tether");
        tetherObject.transform.SetParent(visual.root.transform, false);
        visual.tether = tetherObject.AddComponent<LineRenderer>();
        ConfigureDoctrineLine(visual.tether, false);
        visual.tether.positionCount = 2;

        GameObject labelObject = new GameObject("Doctrine_Label");
        labelObject.transform.SetParent(visual.root.transform, false);
        visual.label = labelObject.AddComponent<TextMesh>();
        visual.label.fontSize = 42;
        visual.label.characterSize = 0.22f;
        visual.label.anchor = TextAnchor.LowerCenter;
        visual.label.alignment = TextAlignment.Center;
        visual.label.fontStyle = FontStyle.Bold;

        doctrineZoneVisuals.Add(visual);
        return visual;
    }

    private void ConfigureDoctrineLine(LineRenderer line, bool loop)
    {
        line.sharedMaterial = doctrineLineMaterial;
        line.useWorldSpace = true;
        line.loop = loop;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Tile;
        line.numCornerVertices = 2;
        line.numCapVertices = 2;
        line.widthMultiplier = loop ? 0.42f : 0.18f;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
    }

    private void UpdateDoctrineZoneVisual(DoctrineZoneVisual visual)
    {
        UnitSquad squad = visual.squad;
        Vector3 anchor = GetSquadDoctrineAnchor(squad);
        float radius = squad.doctrine == SquadDoctrine.EscortPyramid ? 24f : squad.doctrineRadius;
        Color color = squad.doctrine == SquadDoctrine.SearchAndDestroy
            ? new Color(1f, 0.32f, 0.12f, 1f)
            : squad.doctrine == SquadDoctrine.HoldArea
                ? new Color(1f, 0.76f, 0.23f, 1f)
                : new Color(0.2f, 0.86f, 1f, 1f);

        float pulse = 0.68f + Mathf.Sin(Time.time * 3.6f + squad.id) * 0.22f;
        Color ringColor = new Color(color.r, color.g, color.b, pulse);
        visual.ring.startColor = ringColor;
        visual.ring.endColor = ringColor;
        visual.tether.startColor = new Color(color.r, color.g, color.b, 0.9f);
        visual.tether.endColor = new Color(color.r, color.g, color.b, 0.25f);
        visual.label.color = color;

        for (int i = 0; i <= 64; i++)
        {
            float angle = i / 64f * Mathf.PI * 2f;
            Vector3 point = anchor + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            point.y = GetPlayableGroundHeight(point) + 0.32f;
            visual.ring.SetPosition(i, point);
        }

        Vector3 squadPoint = anchor;
        if (TryGetSquadCentroid(squad, out Vector3 centroid))
            squadPoint = centroid;
        squadPoint.y += 1.1f;

        Vector3 destination = anchor;
        if (IsLiveDoctrineTarget(squad.doctrineTarget))
            destination = squad.doctrineTarget.transform.position;
        destination.y = GetPlayableGroundHeight(destination) + 1.25f;
        visual.tether.SetPosition(0, squadPoint);
        visual.tether.SetPosition(1, destination);

        Vector3 labelPosition;
        if (squad.doctrine == SquadDoctrine.EscortPyramid)
            labelPosition = squadPoint + Vector3.up * 3.6f;
        else
        {
            labelPosition = anchor;
            labelPosition.y = GetPlayableGroundHeight(labelPosition) + 4.2f;
        }
        visual.label.transform.position = labelPosition;
        visual.label.text = GetSquadDoctrineLabel(squad.doctrine) + "\n" +
            (IsLiveDoctrineTarget(squad.doctrineTarget) ? "TARGET ACQUIRED" : "AUTONOMOUS");
        if (mainCamera != null)
            visual.label.transform.rotation = mainCamera.transform.rotation;
    }
}