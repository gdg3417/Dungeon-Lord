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
        internal bool AllowLocalMvpResearch { get; }

        public GateEvaluationInput(RestrictedActionType actionType, bool isOnline, bool verificationPending)
            : this(actionType, isOnline, verificationPending, false)
        {
        }

        internal GateEvaluationInput(RestrictedActionType actionType, bool isOnline, bool verificationPending,
            bool allowLocalMvpResearch)
        {
            ActionType = actionType;
            IsOnline = isOnline;
            VerificationPending = verificationPending;
            AllowLocalMvpResearch = allowLocalMvpResearch;
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
            bool localMvpResearch = input.AllowLocalMvpResearch &&
                (input.ActionType == RestrictedActionType.ResearchStart || input.ActionType == RestrictedActionType.ResearchComplete);
            if (!input.IsOnline && !localMvpResearch)
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
