namespace nTest.RandomDataSource
{
    public interface IRandomnessSource
    {
        decimal NextDecimal(decimal from, decimal to);
        double NextDouble();
        int NextIntBetween(int from, int to);
        bool ShouldHappenWithProbability(decimal p);
        T OneOf<T>(params T[] args);
    }
}