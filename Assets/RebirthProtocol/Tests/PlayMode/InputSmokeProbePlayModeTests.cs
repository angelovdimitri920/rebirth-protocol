using System.Collections;
using NUnit.Framework;
using RebirthProtocol.Bootstrap;
using UnityEngine;
using UnityEngine.TestTools;

namespace RebirthProtocol.Tests.PlayMode
{
    public sealed class InputSmokeProbePlayModeTests
    {
        [UnityTest]
        public IEnumerator ProbeCanAcceptNormalizedMoveInput()
        {
            var gameObject = new GameObject("probe");
            var probe = gameObject.AddComponent<InputSmokeProbe>();

            probe.ApplyMoveForTests(new Vector2(4f, 0f), true);

            Assert.That(probe.MoveInput.magnitude, Is.LessThanOrEqualTo(1.001f));
            Assert.That(probe.DashPressed, Is.True);
            Object.Destroy(gameObject);
            yield return null;
        }
    }
}
