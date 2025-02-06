using Moq;

namespace TestsnPreCredit
{
    public class StrictMock<T> : Mock<T> where T : class
    {
        public StrictMock() : base(MockBehavior.Strict)
        {
        }
    }
}
