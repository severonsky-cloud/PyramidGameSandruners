using System.Collections.Generic;
using UnityEngine;

public partial class SandRunnersPrototype
{
    private sealed class CombatTargetMarker
    {
        public EnemyUnit enemy;
        public Transform transform;
        public string label;
        public float expireAt;
        public bool hostile;
    }

    private readonly List<CombatTargetMarker> combatTargetMarkers = new List<CombatTargetMarker>();
    private Material readableWarningMaterial;
    private Material readableHostileWarningMaterial;
    private Material readableSelectionMaterial;
    private Material readableHostileSelectionMaterial;

    private void EnsureCombatReadabilityMaterials()
    {
        EnsureBattleEffectMaterials();
        if (readableWarningMaterial != null)
            return;

        readableWarningMaterial = CreateMaterial("Readable Golden Impact Warning", new Color(1f, 0.72f, 0.12f, 0.42f));
        readableHostileWarningMaterial = CreateMaterial("Readable Hostile Impact Warning", new Color(1f, 0.08f, 0.035f, 0.48f));
        readableSelectionMaterial = CreateMaterial("Readable Golden Selection Bracket", new Color(0.35f, 1f, 0.42f, 0.92f));
        readableHostileSelectionMaterial = CreateMaterial("Readable Red Target Bracket", new Color(1f, 0.18f, 0.08f, 0.92f));
        ConfigureTransparent(readableWarningMaterial);
        ConfigureTransparent(readableHostileWarningMaterial);
        ConfigureTransparent(readableSelectionMaterial);
        ConfigureTransparent(readableHostileSelectionMaterial);
        SetEmission(readableWarningMaterial, new Color(1f, 0.62f, 0.14f, 1f), 1.4f);
        SetEmission(readableHostileWarningMaterial, new Color(1f, 0.08f, 0.025f, 1f), 1.8f);
        SetEmission(readableSelectionMaterial, new Color(0.18f, 1f, 0.28f, 1f), 1.2f);
        SetEmission(readableHostileSelectionMaterial, new Color(1f, 0.08f, 0.035f, 1f), 1.4f);
    }

    private void TrackCombatTarget(EnemyUnit enemy, string label, float duration = 4f)
    {
        if (enemy == null || enemy.transform == null)
            return;

        for (int i = 0; i < combatTargetMarkers.Count; i++)
        {
            CombatTargetMarker marker = combatTargetMarkers[i];
            if (marker.enemy == enemy)
            {
                marker.label = label;
                marker.expireAt = Time.time + duration;
                marker.transform = enemy.transform;
                return;
            }
        }

        CombatTargetMarker created = new CombatTargetMarker();
        created.enemy = enemy;
        created.transform = enemy.transform;
        created.label = label;
        created.expireAt = Time.time + duration;
        created.hostile = true;
        combatTargetMarkers.Add(created);
    }

    private Transform CreateReadableImpactWarning(Vector3 target, float radius, Color color, float life, string label, bool hostile)
    {
        EnsureCombatReadabilityMaterials();

        Transform root = new GameObject(hostile ? "Readable_Hostile_Impact_Warning" : "Readable_Golden_Impact_Warning").transform;
        root.position = new Vector3(target.x, GetPlayableGroundHeight(target) + 0.085f, target.z);
        Material material = hostile ? readableHostileWarningMaterial : readableWarningMaterial;
        CreateCylinder(root, "Warning_Radius_Disc", Vector3.zero, Quaternion.identity, new Vector3(radius * 2f, 0.018f, radius * 2f), material);
        CreateBox(root, "Warning_Cross_A", Vector3.up * 0.06f, Quaternion.identity, new Vector3(radius * 1.8f, 0.025f, 0.18f), material);
        CreateBox(root, "Warning_Cross_B", Vector3.up * 0.07f, Quaternion.Euler(0f, 90f, 0f), new Vector3(radius * 1.8f, 0.025f, 0.18f), material);
        CreatePointLight(root, "Warning_Glow", Vector3.up * 1.2f, color, hostile ? 1.1f : 0.72f, Mathf.Clamp(radius * 2.6f, 12f, 42f));

        TextMesh text = new GameObject("Warning_Label").AddComponent<TextMesh>();
        text.transform.SetParent(root, false);
        text.transform.localPosition = Vector3.up * 1.9f;
        text.transform.localRotation = Quaternion.Euler(62f, 0f, 0f);
        text.text = label;
        text.characterSize = Mathf.Clamp(radius * 0.055f, 0.42f, 0.86f);
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = color;

        Destroy(root.gameObject, life);
        return root;
    }

    private void DrawSelectedAttackTargetMarkers()
    {
        for (int i = 0; i < selectedSquads.Count; i++)
        {
            UnitSquad squad = selectedSquads[i];
            if (squad == null || squad.attackTarget == null || squad.attackTarget.transform == null)
                continue;

            EnemyUnit enemy = squad.attackTarget;
            DrawReadableWorldMarker(enemy.transform.position + Vector3.up * 2.6f, enemy.health / Mathf.Max(1f, enemy.maxHealth), "ATTACK: " + enemy.transform.name.Replace('_', ' '), new Color(1f, 0.16f, 0.08f, 0.96f), true);
        }
    }

    private void DrawTrackedCombatTargets()
    {
        for (int i = combatTargetMarkers.Count - 1; i >= 0; i--)
        {
            CombatTargetMarker marker = combatTargetMarkers[i];
            if (marker == null || marker.enemy == null || marker.transform == null || marker.enemy.health <= 0f || Time.time > marker.expireAt)
            {
                combatTargetMarkers.RemoveAt(i);
                continue;
            }

            Color color = marker.hostile ? new Color(1f, 0.18f, 0.08f, 0.92f) : new Color(0.35f, 1f, 0.42f, 0.92f);
            DrawReadableWorldMarker(marker.transform.position + Vector3.up * 2.35f, marker.enemy.health / Mathf.Max(1f, marker.enemy.maxHealth), marker.label, color, true);
        }
    }

    private void DrawReadableWorldMarker(Vector3 world, float ratio, string label, Color color, bool bracket)
    {
        if (mainCamera == null)
            return;

        Vector3 screen = mainCamera.WorldToScreenPoint(world);
        if (screen.z <= 0f)
            return;

        float scale = Mathf.Clamp(85f / Mathf.Max(28f, screen.z), 0.72f, 1.4f);
        float width = 110f * scale;
        float height = 48f * scale;
        float x = screen.x - width * 0.5f;
        float y = Screen.height - screen.y - height * 0.5f;

        bool important = label.Contains("GUSTAV") || label.Contains("FORT") || label.Contains("CASTLE") || label.Contains("MANDAR");

        if (bracket)
        {
            float corner = (important ? 24f : 18f) * scale;
            float thick = Mathf.Max(2.5f, (important ? 4f : 3f) * scale);
            Color bracketColor = important ? Color.Lerp(color, Color.white, 0.3f) : color;
            DrawGuiRect(new Rect(x, y, corner, thick), bracketColor);
            DrawGuiRect(new Rect(x, y, thick, corner), bracketColor);
            DrawGuiRect(new Rect(x + width - corner, y, corner, thick), bracketColor);
            DrawGuiRect(new Rect(x + width - thick, y, thick, corner), bracketColor);
            DrawGuiRect(new Rect(x, y + height - thick, corner, thick), bracketColor);
            DrawGuiRect(new Rect(x, y + height - corner, thick, corner), bracketColor);
            DrawGuiRect(new Rect(x + width - corner, y + height - thick, corner, thick), bracketColor);
            DrawGuiRect(new Rect(x + width - thick, y + height - corner, thick, corner), bracketColor);
        }

        float barY = y + height + 5f;
        float barWidth = important ? width + 10f : width - 14f;
        DrawGuiRect(new Rect(x + 7f, barY, barWidth, 7f), new Color(0f, 0f, 0f, 0.72f));
        float hpWidth = (barWidth - 4f) * Mathf.Clamp01(ratio);
        DrawGuiRect(new Rect(x + 9f, barY + 2f, hpWidth, 3f), color);
        GUI.color = color;
        GUI.Label(new Rect(x - 18f, y - (important ? 22f : 19f), width + 36f, 20f), label);
        GUI.color = Color.white;
    }

    private void DrawGuiRect(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
