using System;

namespace Astra.Aegis
{
    /// <summary>
    /// Stable identity for a rule without an asset. Asset-backed rules keep their GUID.
    /// Declare explicitly before renaming a custom fallback rule used by profiles or suppressions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class AegisRuleIdentityAttribute : Attribute
    {
        public string Id { get; }
        public AegisRuleIdentityAttribute(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A rule identity is required.", nameof(id));
            Id = id;
        }
    }
}
