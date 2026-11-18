using System;
using System.Collections.Generic;
using GunPowdered.Features.Example.Application;
using GunPowdered.Features.Example.Domain;
using GunPowdered.Features.Example.Execution;
using KydlCore.Core;

namespace GunPowdered.Tests.Unit
{
    /// The template's example command slice, handler and executor directly (the integration test drives it through the dispatcher; Stryker only
    /// sees the unit tests, T-008).
    public class ExampleCommandUnitTests
    {
        sealed class Owner : IOwnership { public bool IsOwner { get; set; } public long OwnerId { get { return 1; } } }

        [Test]
        public void Command_KeepsTheRequestedValue()
        {
            Assert.That(new SetExampleValueCommand(7.5f).Requested, Is.EqualTo(7.5f));
        }

        [Test]
        public void Executor_AsOwner_AppliesTheValue()
        {
            var applied = new List<float>();
            var r = new ExampleValueExecutor(new Owner { IsOwner = true }, applied.Add).Apply(4f);
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Success));
            Assert.That(r.Reason, Is.EqualTo(ExampleValueExecutor.Applied));
            Assert.That(applied, Is.EqualTo(new[] { 4f }));
        }

        [Test]
        public void Executor_NotTheOwner_AppliesNothing()
        {
            var applied = new List<float>();
            var r = new ExampleValueExecutor(new Owner { IsOwner = false }, applied.Add).Apply(4f);
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Rejected));
            Assert.That(r.Reason, Is.EqualTo(ExampleValueExecutor.NotOwner));
            Assert.That(applied, Is.Empty);
        }

        [Test]
        public void Executor_WithoutOwnerOrAction_Throws()
        {
            var noOwner = Assert.Throws<ArgumentNullException>(() => new ExampleValueExecutor(null, v => { }));
            var noAction = Assert.Throws<ArgumentNullException>(() => new ExampleValueExecutor(new Owner(), null));
            Assert.That(noOwner.ParamName, Is.EqualTo("owner"));
            Assert.That(noAction.ParamName, Is.EqualTo("apply"));
        }

        [Test]
        public void Handler_WithoutExecutor_Throws()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new SetExampleValueHandler(null, null));
            Assert.That(ex.ParamName, Is.EqualTo("executor"));
        }

        [Test]
        public void Handler_AValidValue_IsDecidedThenApplied()
        {
            var applied = new List<float>();
            var traced = new List<string>();
            var handler = new SetExampleValueHandler(new ExampleValueExecutor(new Owner { IsOwner = true }, applied.Add), traced.Add);
            var r = handler.Handle(new SetExampleValueCommand(12f));
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Success));
            Assert.That(applied, Is.EqualTo(new[] { ExampleRules.Max }));
        }

        [Test]
        public void Handler_ARejectedValue_IsNotApplied()
        {
            var applied = new List<float>();
            var handler = new SetExampleValueHandler(new ExampleValueExecutor(new Owner { IsOwner = true }, applied.Add), null);
            var r = handler.Handle(new SetExampleValueCommand(float.NaN));
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Rejected));
            Assert.That(applied, Is.Empty);
        }

        [Test]
        public void Handler_NoCommand_IsRejectedAndNothingApplied()
        {
            var applied = new List<float>();
            var handler = new SetExampleValueHandler(new ExampleValueExecutor(new Owner { IsOwner = true }, applied.Add), null);
            var r = handler.Handle(null);
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Rejected));
            Assert.That(r.Reason, Does.Contain(SetExampleValueHandler.NoCommand));
            Assert.That(applied, Is.Empty);
        }

        [Test]
        public void Handler_TracesItsPipeline()
        {
            var traced = new List<string>();
            var handler = new SetExampleValueHandler(new ExampleValueExecutor(new Owner { IsOwner = true }, v => { }), traced.Add);
            handler.Handle(new SetExampleValueCommand(3f));
            Assert.That(string.Join("|", traced), Does.Contain("SetExampleValue: 1 checks passed").And.Contain(ExampleRules.InRange));
        }
    }
}
