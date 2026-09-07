namespace MisterPxl.Aegis
{
    /// <summary>Compatibility entry point for existing -executeMethod jobs.</summary>
    [System.Obsolete("Use Astra.Aegis.AegisCli.Run in new CI jobs.")]
    public static class AegisCli
    {
        public static void Run() => global::Astra.Aegis.AegisCli.Run();
    }
}
