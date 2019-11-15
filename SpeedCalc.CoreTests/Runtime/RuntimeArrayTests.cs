using SpeedCalc.Core.Runtime;

using System.Linq;

using Xunit;

namespace SpeedCalc.CoreTests.Runtime
{
    public class RuntimeArrayTests
    {
        [Fact]
        public void ArrayInitializesAsEmpty()
        {
            var array = new RuntimeArray<byte>();

            Assert.Equal(0, array.Count);
            Assert.True(array.Capacity > 0);
            Assert.Empty(array);
        }

        [Fact]
        public void ArrayContainsValuesAfterWrite()
        {
            var array = new RuntimeArray<int>();
            array.Write(1);
            array.Write(2);
            array.Write(3);

            Assert.Equal(3, array.Count);

            Assert.Equal(1, array[0]);
            Assert.Equal(2, array[1]);
            Assert.Equal(3, array[2]);

            Assert.Equal(new[] { 1, 2, 3 }, array);
        }

        [Fact]
        public void ArrayContainsChangedValuesAfterIndexedSet()
        {
            var array = new RuntimeArray<int>();
            array.Write(1);

            Assert.Equal(1, array[0]);

            array[0] = 100;

            Assert.Equal(100, array[0]);
        }

        [Fact]
        public void ArrayGrowsWhenWritingAtCapacity()
        {
            var array = new RuntimeArray<int>();
            var observedDefaultCapacity = array.Capacity;

            for (int i = 0; i < observedDefaultCapacity; i++)
                array.Write(i);

            Assert.Equal(observedDefaultCapacity, array.Capacity);

            array.Write(100);

            Assert.True(array.Capacity > observedDefaultCapacity);
        }

        [Fact]
        public void ArrayContainsDataAfterGrowth()
        {
            var array = new RuntimeArray<int>();

            var itemCountToWrite = 4 * array.Capacity;
            for (int i = 0; i < itemCountToWrite; i++)
                array.Write(i);

            Assert.Equal(Enumerable.Range(0, itemCountToWrite), array);
        }
    }
}
