using System;

namespace MisterPxl.Aegis.Tests
{
    public sealed class AegisRegressionRule : AegisRuleAsset
    {
        [NonSerialized] public Action OnEvaluate;
        public bool ThrowOnEvaluate;

        public override void Evaluate(AegisValidationContext context, IAegisFindingSink sink)
        {
            OnEvaluate?.Invoke();
            if (ThrowOnEvaluate)
                throw new InvalidOperationException("Regression rule failure.");

            sink.Add(CreateFinding("Regression finding.", code: "Aegis.Tests.Regression"));
        }
    }
}
