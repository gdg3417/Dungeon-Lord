namespace DungeonBuilder.M0
{
    public enum RestrictedActionType
    {
        ResearchStart = 0,
        ResearchComplete = 1,
        Purchase = 2,
        EventClaim = 3
    }

    public readonly struct GateEvaluationInput
    {
        public RestrictedActionType ActionType { get; }
        public bool IsOnline { get; }
        public bool VerificationPending { get; }

        public GateEvaluationInput(RestrictedActionType actionType, bool isOnline, bool verificationPending)
        {
            ActionType = actionType;
            IsOnline = isOnline;
            VerificationPending = verificationPending;
        }
    }

    public readonly struct GateEvaluationResult
    {
        public bool Allowed { get; }
        public string MessageKey { get; }

        public GateEvaluationResult(bool allowed, string messageKey)
        {
            Allowed = allowed;
            MessageKey = messageKey;
        }
    }

    public interface IRestrictedActionGate
    {
        GateEvaluationResult Evaluate(GateEvaluationInput input);
    }

    public sealed class RestrictedActionGateService : IRestrictedActionGate
    {
        public GateEvaluationResult Evaluate(GateEvaluationInput input)
        {
            if (!input.IsOnline)
            {
                return new GateEvaluationResult(false, "gate.error.offline_required");
            }

            if (input.VerificationPending)
            {
                return new GateEvaluationResult(false, "gate.error.verification_pending");
            }

            return new GateEvaluationResult(true, "gate.ok.allowed");
        }
    }
}
