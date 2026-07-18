using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class SandRunnersImportedModelTests
{
    [TestCase("SR_MineConvoyTruck")]
    [TestCase("SR_MineEngineerRover")]
    public void ImportedVehicleProvidesThreeBuildSafeLods(string baseName)
    {
        const string root = "SandRunners/Models/ImportedCandidates/";
        for (int i = 0; i < 3; i++)
        {
            GameObject model = Resources.Load<GameObject>(root + baseName + "_LOD" + i);
            Assert.That(model, Is.Not.Null, baseName + " missing LOD" + i);
            Assert.That(model.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThan(0));
        }
    }

    [Test]
    public void ImportedVehicleScaleNormalizesHorizontalLength()
    {
        Type rules = Type.GetType("SandRunnersImportedModelRules, Assembly-CSharp");
        Assert.That(rules, Is.Not.Null);
        MethodInfo method = rules.GetMethod("NormalizeScale",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        float scale = (float)method.Invoke(null,
            new object[] { 8f, new Vector3(0.43f, 0.49f, 1f) });
        Assert.That(scale, Is.EqualTo(8f).Within(0.001f));
    }
}
