using ScreenChecker.Services;

namespace ScreenChecker.UnitTests.ServicesTests
{
    [TestFixture]
    public class AttemptCounterTests
    {
        [Test]
        public void Get_ForAPointWithoutAttempts_ReturnsZero()
        {
            Assert.That(new AttemptCounter().Get("Center"), Is.EqualTo(0));
        }

        [Test]
        public void Increment_CountsEveryPointOnItsOwn()
        {
            var counter = new AttemptCounter();

            counter.Increment("Center");
            counter.Increment("Center");
            counter.Increment("TopLeft");

            Assert.That((counter.Get("Center"), counter.Get("TopLeft")), Is.EqualTo((2, 1)));
        }

        [Test]
        public void Reset_ZeroesOnlyThePoint()
        {
            var counter = new AttemptCounter();
            counter.Increment("Center");
            counter.Increment("TopLeft");

            counter.Reset("Center");

            Assert.That((counter.Get("Center"), counter.Get("TopLeft")), Is.EqualTo((0, 1)));
        }

        [Test]
        public void Counters_AreNotSharedBetweenInstances()
        {
            var first = new AttemptCounter();
            first.Increment("Center");

            Assert.That(new AttemptCounter().Get("Center"), Is.EqualTo(0));
        }
    }
}
