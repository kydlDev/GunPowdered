using GunPowdered.Features.Musket.Domain;

namespace GunPowdered.Tests.Unit
{
    /// <summary>
    /// The authored musket poses (D-092..D-094, approved by the user after the pictures and the in-game look) are data, not rules: these tests pin them, so a
    /// changed number - a typo, a slipped sign - fails here instead of bending an arm in game. Changing a pose on purpose means updating the fingerprint below
    /// in the same commit (the numbers come from _agent/lab/gen_musket_poses.py). Fingerprint = sum of value x (index + 1) in double: any single non-zero value
    /// that changes sign or size moves it (a11gate Stryker: 187 surviving sign mutants in MusketPoses.cs before these tests).
    /// </summary>
    public class MusketPosesTests
    {
        const double Exact = 1e-6;

        static double Fingerprint(float[] values)
        {
            double sum = 0d;
            for (int i = 0; i < values.Length; i++) sum += (double)values[i] * (i + 1);
            return sum;
        }

        [Test]
        public void EveryPose_HoldsOneValuePerMuscle()
        {
            Assert.Multiple(() =>
            {
                Assert.That(MusketPoses.Hold, Has.Length.EqualTo(MusketPoses.Count));
                Assert.That(MusketPoses.Recoil, Has.Length.EqualTo(MusketPoses.Count));
                Assert.That(MusketPoses.Carry, Has.Length.EqualTo(MusketPoses.Count));
                Assert.That(MusketPoses.ReloadLow, Has.Length.EqualTo(MusketPoses.Count));
                Assert.That(MusketPoses.ReloadHigh, Has.Length.EqualTo(MusketPoses.Count));
                Assert.That(MusketPoses.GripHold, Has.Length.EqualTo(6));
                Assert.That(MusketPoses.GripReload, Has.Length.EqualTo(6));
            });
        }

        [Test]
        public void ApprovedPoses_AreUnchanged()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Fingerprint(MusketPoses.Hold), Is.EqualTo(-271.02750508859754).Within(Exact), "Hold");
                Assert.That(Fingerprint(MusketPoses.Recoil), Is.EqualTo(-268.78080524504185).Within(Exact), "Recoil");
                Assert.That(Fingerprint(MusketPoses.Carry), Is.EqualTo(-328.1456043869257).Within(Exact), "Carry");
                Assert.That(Fingerprint(MusketPoses.ReloadLow), Is.EqualTo(-835.75360575318336).Within(Exact), "ReloadLow");
                Assert.That(Fingerprint(MusketPoses.ReloadHigh), Is.EqualTo(-821.17580637335777).Within(Exact), "ReloadHigh");
            });
        }

        [Test]
        public void ApprovedGrips_AreUnchanged()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Fingerprint(MusketPoses.GripHold), Is.EqualTo(870.82741480693221).Within(Exact), "GripHold");
                Assert.That(Fingerprint(MusketPoses.GripReload), Is.EqualTo(2410.5365761439316).Within(Exact), "GripReload");
            });
        }
    }
}
