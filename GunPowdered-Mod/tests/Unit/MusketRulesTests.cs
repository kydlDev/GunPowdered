using GunPowdered.Features.Musket.Domain;

namespace GunPowdered.Tests.Unit
{
    /// The Iron Musket's magazine (D-080 / D-081): up to 5 paper cartridges, reload below 5 and never above 5, one cartridge per shot.
    public class MusketRulesTests
    {
        // ---- D-198 (user 2026-11-28): the musket on a gamepad uses the game's own buttons

        [TestCase("Use", "JoyUse")]
        [TestCase("SecondaryAttack", "JoySecondaryAttack")]
        [TestCase(" Hide ", "JoyHide")]
        [TestCase("JoyLTrigger", "JoyLTrigger")]
        [TestCase("None", null)]
        [TestCase("none", null)]
        [TestCase("", null)]
        [TestCase(null, null)]
        public void PadBinding_GameButtonGetsTheJoyName_NoneHasNone(string button, string expected)
        {
            Assert.That(MusketRules.PadBinding(button), Is.EqualTo(expected));
        }

        [TestCase("Use", false, true)]
        [TestCase("Use", true, false)]
        [TestCase(" use ", true, false)]
        [TestCase("SecondaryAttack", true, true)]
        [TestCase("Hide", false, true)]
        [TestCase(null, true, true)]
        public void PadReloadAllowed_UseWhileHoveringIsTheGames(string button, bool hovering, bool expected)
        {
            Assert.That(MusketRules.PadReloadAllowed(button, hovering), Is.EqualTo(expected));
        }

        // ---- D-155 (user 2026-11-22)

        [TestCase(true, false, null)]
        [TestCase(false, false, MusketRules.ReloadInAir)]
        [TestCase(true, true, MusketRules.ReloadSwimming)]
        [TestCase(false, true, MusketRules.ReloadSwimming)]
        public void WhyNotReloadMoving_RunningFine_JumpingAndSwimmingRefused(bool onGround, bool swimming, string expected)
        {
            Assert.That(MusketRules.WhyNotReloadMoving(onGround, swimming), Is.EqualTo(expected));
        }

        [TestCase("Torch", true)]
        [TestCase("Shield", true)]
        [TestCase("OneHandedWeapon", false)]
        [TestCase("TwoHandedWeapon", false)]
        [TestCase("Tool", false)]
        [TestCase("Bow", false)]
        [TestCase(null, false)]
        public void FitsBesideLoweredMusket_OnlyTorchesAndShields(string itemType, bool fits)
        {
            Assert.That(MusketRules.FitsBesideLoweredMusket(itemType), Is.EqualTo(fits));
        }

        [Test]
        public void AimFirst_NamesTheKey()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(MusketRules.AimFirst("mouse wheel button"), Is.EqualTo("Raise the Iron Musket first (mouse wheel button)"));
                Assert.That(MusketRules.AimFirst(""), Is.EqualTo(MusketRules.NotAimed));
            }
        }

        [Test]
        public void BlockStats_AreTheUsersNumbers()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(MusketRules.BlockPower, Is.EqualTo(10f), "block armour 10 at skill 0 (the game makes it 15 at skill 100)");
                Assert.That(MusketRules.BlockPower * 1.5f, Is.EqualTo(15f), "x1.5 at Blocking 100");
                Assert.That(MusketRules.BlockPowerPerLevel, Is.Zero);
                Assert.That(MusketRules.ParryBonus, Is.EqualTo(1.5f));
                Assert.That(MusketRules.ParryAdrenaline, Is.EqualTo(10f));
                Assert.That(MusketRules.BlockAdrenaline, Is.EqualTo(2f));
                Assert.That(MusketRules.MovementModifier, Is.Zero, "no movement speed reduction");
            }
        }

        [Test]
        public void Capacity_IsFive()
        {
            Assert.That(MusketRules.Capacity, Is.EqualTo(5));
        }

        [TestCase(0, 10, 5)]
        [TestCase(0, 3, 3)]
        [TestCase(2, 10, 3)]
        [TestCase(2, 1, 1)]
        [TestCase(4, 9, 1)]
        [TestCase(5, 9, 0)]
        [TestCase(0, 0, 0)]
        [TestCase(3, -2, 0)]
        public void CartridgesToLoad_FillsUpToFive_FromWhatIsCarried(int loaded, int carried, int expected)
        {
            Assert.That(MusketRules.CartridgesToLoad(loaded, carried), Is.EqualTo(expected));
        }

        [TestCase(0, 99)]
        [TestCase(1, 99)]
        [TestCase(2, 4)]
        [TestCase(4, 1)]
        [TestCase(4, 50)]
        public void AfterReload_NeverGoesAboveFive(int loaded, int carried)
        {
            Assert.That(MusketRules.AfterReload(loaded, carried), Is.LessThanOrEqualTo(5));
            Assert.That(MusketRules.AfterReload(loaded, carried), Is.GreaterThanOrEqualTo(loaded));
        }

        [Test]
        public void AfterReload_FromTwoWithTenCarried_IsFive()
        {
            Assert.That(MusketRules.AfterReload(2, 10), Is.EqualTo(5));
            Assert.That(MusketRules.AfterReload(0, 3), Is.EqualTo(3));
        }

        [Test]
        public void Reload_IsRefusedWhenFull_OrWithoutCartridges()
        {
            Assert.That(MusketRules.WhyNotReload(5, 10), Is.EqualTo(MusketRules.Full));
            Assert.That(MusketRules.WhyNotReload(2, 0), Is.EqualTo(MusketRules.NoCartridges));
            Assert.That(MusketRules.WhyNotReload(4, 1), Is.Null, "one short of full is still allowed");
            Assert.That(MusketRules.WhyNotReload(0, 1), Is.Null);
            Assert.That(MusketRules.WhyNotReload(5, 0), Is.EqualTo(MusketRules.Full), "full wins over missing cartridges");
        }

        [Test]
        public void Fire_NeedsALoadedCartridge()
        {
            Assert.That(MusketRules.WhyNotFire(0), Is.EqualTo(MusketRules.Empty));
            Assert.That(MusketRules.WhyNotFire(1), Is.Null);
            Assert.That(MusketRules.WhyNotFire(5), Is.Null);
        }

        [Test]
        public void AfterShot_UsesOneCartridge_NeverGoesBelowZero()
        {
            Assert.That(MusketRules.AfterShot(5), Is.EqualTo(4));
            Assert.That(MusketRules.AfterShot(1), Is.EqualTo(0));
            Assert.That(MusketRules.AfterShot(0), Is.EqualTo(0));
        }

        [Test]
        public void FiveShotsEmptyAFullMusket_AndASixthIsRefused()
        {
            int loaded = MusketRules.Capacity;
            for (int i = 0; i < 5; i++)
            {
                Assert.That(MusketRules.WhyNotFire(loaded), Is.Null, "shot " + (i + 1));
                loaded = MusketRules.AfterShot(loaded);
            }
            Assert.That(loaded, Is.EqualTo(0));
            Assert.That(MusketRules.WhyNotFire(loaded), Is.EqualTo(MusketRules.Empty));
        }

        [TestCase(-3, 0)]
        [TestCase(0, 0)]
        [TestCase(3, 3)]
        [TestCase(5, 5)]
        [TestCase(9, 5)]
        public void ClampLoaded_KeepsAStoredLoadInsideTheMagazine(int stored, int expected)
        {
            Assert.That(MusketRules.ClampLoaded(stored), Is.EqualTo(expected));
        }

        [Test]
        public void ADamagedStoredLoad_CannotBeReloadedAboveFive()
        {
            Assert.That(MusketRules.CartridgesToLoad(9, 10), Is.EqualTo(0));
            Assert.That(MusketRules.WhyNotReload(9, 10), Is.EqualTo(MusketRules.Full));
            Assert.That(MusketRules.AfterReload(-2, 10), Is.EqualTo(5));
        }

        // D-095: firing and the timed reload
        [TestCase(10f, 9.3f, false)]
        [TestCase(10f, 9.2f, true)]
        [TestCase(10f, -99f, true)]
        [TestCase(10f, 10f, false)]
        public void ShotInterval_AllowsTheNextShotOnlyAfter08Seconds(float now, float lastShot, bool expected)
        {
            Assert.That(MusketRules.ShotIntervalOver(now, lastShot), Is.EqualTo(expected));
        }

        [TestCase(5f, 5f, 0)]
        [TestCase(5f, 4f, 0)]
        [TestCase(5f, 6.19f, 0)]
        [TestCase(5f, 6.2f, 1)]
        [TestCase(5f, 8.6f, 3)]
        [TestCase(5f, 11f, 5)]
        public void CartridgesDue_OnePerFinishedStroke(float started, float now, int expected)
        {
            Assert.That(MusketRules.CartridgesDue(started, now), Is.EqualTo(expected));
        }

        [Test]
        public void ReloadTiming_MatchesTheRamrodStroke()
        {
            Assert.That(MusketRules.ReloadSecondsPerCartridge, Is.EqualTo(MusketPoseRules.ReloadSecondsPerCartridge));
        }

        [TestCase("3", 3)]
        [TestCase("0", 0)]
        [TestCase("7", 5)]
        [TestCase("-1", 0)]
        [TestCase("", 0)]
        [TestCase(null, 0)]
        [TestCase("x", 0)]
        public void ParseLoaded_ReadsTheStoredLoadSafely(string stored, int expected)
        {
            Assert.That(MusketRules.ParseLoaded(stored), Is.EqualTo(expected));
        }

        [TestCase(0.25f, 50f, 0f, 25f)]
        [TestCase(0.25f, 50f, -0.5f, 50f)]
        [TestCase(0.25f, 50f, 0.3f, 25f)]
        [TestCase(0f, 50f, 0f, 0f)]
        [TestCase(0.25f, 0f, 0f, 0f)]
        [TestCase(0.25f, 50f, -1f, 0f)]
        public void RecoilPushForce_GivesTheGameTheForceForTheShove(float metres, float mass, float modifier, float expected)
        {
            Assert.That(MusketRules.RecoilPushForce(metres, mass, modifier), Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void RecoilPushForce_RoundTripsThroughTheGameFormula()
        {
            // the game: push = F x clamp01(1 + modifier) / mass x 2.5, slide ~ push / 5 m
            float force = MusketRules.RecoilPushForce(0.25f, 72f, -0.1f);
            float push = force * 0.9f / 72f * 2.5f;
            Assert.That(push / 5f, Is.EqualTo(0.25f).Within(0.0001f));
        }

        [TestCase(3, "3/5")]
        [TestCase(0, "0/5")]
        [TestCase(9, "5/5")]
        [TestCase(-2, "0/5")]
        public void LoadText_IsLoadedOverCapacity(int loaded, string expected)
        {
            Assert.That(MusketRules.LoadText(loaded), Is.EqualTo(expected));
        }

        [Test]
        public void FiveShots_EmptyTheMusket_ThenFiringIsRefused()
        {
            int loaded = MusketRules.AfterReload(0, 9);
            for (int i = 0; i < 5; i++)
            {
                Assert.That(MusketRules.WhyNotFire(loaded), Is.Null);
                loaded = MusketRules.AfterShot(loaded);
            }
            Assert.That(loaded, Is.EqualTo(0));
            Assert.That(MusketRules.WhyNotFire(loaded), Is.EqualTo(MusketRules.Empty));
        }
    }
}
