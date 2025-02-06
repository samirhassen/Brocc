namespace NTech.Core.Host.IntegrationTests
{
    internal static class TestExtensions
    {
        public static void AssertTrue(this bool source, string? message = null)
        {
            Assert.That(source, Is.EqualTo(true), message);
        }

        public static void AssertFalse(this bool source, string? message = null)
        {
            Assert.That(source, Is.EqualTo(true), message);
        }
    }
}
