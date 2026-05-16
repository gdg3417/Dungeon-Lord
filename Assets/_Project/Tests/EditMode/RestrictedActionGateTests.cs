using DungeonBuilder.M0;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class RestrictedActionGateTests
    {
        [Test]
        public void Evaluate_BlocksWhenOffline()
        {
            IRestrictedActionGate gate = new RestrictedActionGateService();
            GateEvaluationResult result = gate.Evaluate(new GateEvaluationInput(
                RestrictedActionType.ResearchStart,
                isOnline: false,
                verificationPending: false
            ));

            Assert.IsFalse(result.Allowed);
            Assert.AreEqual("gate.error.offline_required", result.MessageKey);
        }

        [Test]
        public void Evaluate_BlocksWhenVerificationPending()
        {
            IRestrictedActionGate gate = new RestrictedActionGateService();
            GateEvaluationResult result = gate.Evaluate(new GateEvaluationInput(
                RestrictedActionType.ResearchComplete,
                isOnline: true,
                verificationPending: true
            ));

            Assert.IsFalse(result.Allowed);
            Assert.AreEqual("gate.error.verification_pending", result.MessageKey);
        }

        [Test]
        public void Evaluate_AllowsWhenOnlineAndVerified()
        {
            IRestrictedActionGate gate = new RestrictedActionGateService();
            GateEvaluationResult result = gate.Evaluate(new GateEvaluationInput(
                RestrictedActionType.Purchase,
                isOnline: true,
                verificationPending: false
            ));

            Assert.IsTrue(result.Allowed);
            Assert.AreEqual("gate.ok.allowed", result.MessageKey);
        }
    }
}
