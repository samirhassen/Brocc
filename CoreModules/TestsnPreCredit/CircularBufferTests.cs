using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTech.Services.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestsnPreCredit
{
    [TestClass]
    public class CircularBufferTests
    {
        public class Item
        {
            public string Text { get; set; }
        }

        [TestMethod]
        public void CircularBufferMaintainsSizeAndOrder()
        {
            var r = new RingBuffer<Item>(10);
            for (var i = 1; i <= 100; ++i)
            {
                r.Add(new Item { Text = $"{i}" });
            }

            Assert.AreEqual(string.Join(",", Enumerable.Range(91, 10)), string.Join(",", r.Select(x => x.Text).ToList()));
        }

        [TestMethod]
        public void CircularBufferIsThreadSafe()
        {

            var r = new RingBuffer<Item>(10);
            var allTasks = new List<Task>();
            for (var i = 1; i < 100; ++i)
            {
                var innerI = i;
                if (innerI % 2 == 0)
                {
                    allTasks.Add(
                        Task.Factory.StartNew(() =>
                        {
                            r.Add(new Item { Text = $"{innerI}" });
                        }));
                }
                else
                {
                    allTasks.Add(
                        Task.Factory.StartNew(() =>
                        {
                            var items = r.ToList();
                            var count = items.Count;
                            Assert.IsTrue(r.Count() <= 10, $"Size over tolerance. {count} > 10");
                        }));
                }
            }

            Task.WaitAll(allTasks.ToArray()); ;
        }
    }
}
