using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using puck.core.Abstract;
namespace Tests
{
    [TestClass]
    public class TestSaveToRepo
    {
        [TestMethod]
        public void SaveRevision()
        {
            var mock = new Mock<I_Puck_Repository>();
            var repo = mock.Object;
            Assert.IsNotNull(repo);

        }
    }
}
