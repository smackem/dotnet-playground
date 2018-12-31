using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SingleThreadedServer.Test
{
    [TestClass]
    public class ResizeArrayTest
    {
        [TestMethod]
        public void Test()
        {
            var ra = new ResizeArray<int>();
            Assert.AreEqual(0, ra.Count);
            Assert.AreEqual(0, ra.ReadOnlyArray.Length);

            ra.Add(0);
            Assert.AreEqual(1024, ra.ReadOnlyArray.Length);
            Assert.AreEqual(1, ra.Count);
            Assert.AreEqual(0, ra.ReadOnlyArray[0]);

            for (int i = 1; i <= 1024; i++)
                ra.Add(i);

            Assert.AreEqual(1537, ra.ReadOnlyArray.Length);
            Assert.AreEqual(1025, ra.Count);

            for (int i = 0; i < 16 * 1024; i++)
                ra.Add(1025 + i);

            Assert.IsTrue(ra.ReadOnlyArray.Length > ra.Count);

            for (int i = 0; i < ra.Count; i++)
                Assert.AreEqual(i, ra.ReadOnlyArray[i]);
        }
    }
}
