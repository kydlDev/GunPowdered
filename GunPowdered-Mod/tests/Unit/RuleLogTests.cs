using System.Collections.Generic;
using GunPowdered.Core;

namespace GunPowdered.Tests.Unit
{
    /// D-147 / PROJECT_RULES 38: the per-frame log guard RuleLog.Due.
    [NonParallelizable]
    public class RuleLogTests
    {
        readonly List<string> _written = new List<string>();
        bool _debug;

        [SetUp]
        public void Connect()
        {
            _written.Clear();
            _debug = true;
            RuleLog.Sink = _written.Add;
            RuleLog.DebugOn = () => _debug;
        }

        [TearDown]
        public void Disconnect()
        {
            RuleLog.Sink = null;
            RuleLog.DebugOn = null;
        }

        [Test]
        public void Due_FirstCallTrue_ThenFalseInsideTheWindow()
        {
            string key = "RuleLogTests.window." + System.Guid.NewGuid();
            Assert.That(RuleLog.Due(key, 60d), Is.True, "the first call is due");
            Assert.That(RuleLog.Due(key, 60d), Is.False, "inside the window it is not");
        }

        [Test]
        public void Due_ZeroSeconds_AlwaysDue()
        {
            string key = "RuleLogTests.zero." + System.Guid.NewGuid();
            Assert.That(RuleLog.Due(key, 0d), Is.True);
            Assert.That(RuleLog.Due(key, 0d), Is.True);
        }

        [Test]
        public void Due_DebugOff_NeverDue()
        {
            _debug = false;
            Assert.That(RuleLog.Due("RuleLogTests.off." + System.Guid.NewGuid(), 0d), Is.False);
        }

        [Test]
        public void Due_DoesNotUseUpTheEverySlotOfTheSameKey()
        {
            string key = "RuleLogTests.every." + System.Guid.NewGuid();
            Assert.That(RuleLog.Due(key, 60d), Is.True);
            Assert.That(RuleLog.Every(key, 60d, "the line"), Is.True, "the guarded Every still writes");
            Assert.That(_written, Is.EqualTo(new[] { "the line" }));
        }

        [Test]
        public void Due_Unconnected_NeverDue()
        {
            RuleLog.Sink = null;
            Assert.That(RuleLog.Due("RuleLogTests.none." + System.Guid.NewGuid(), 0d), Is.False);
        }
    }
}
