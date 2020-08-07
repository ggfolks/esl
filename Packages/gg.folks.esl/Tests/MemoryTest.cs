namespace GGFolks.Esl {

using NUnit.Framework;

/// <summary>Tests the sparse memory model.</summary>
[TestFixture]
public class MemoryTest : SimulationTest {

  [Test]
  public void TestAsScalar () {
    var memory = new Memory(1, 1);

    // starts as zero: add zero, returns zero
    var result = memory.IncrementAndFetch(Scalar(0f), Scalar(0f), Scalar(0f));
    Assert.AreEqual(0f, result[0]);

    // add one, returns one
    result = memory.IncrementAndFetch(Scalar(0f), Scalar(0f), Scalar(1f));
    Assert.AreEqual(1f, result[0]);

    // add zero, still returns one
    result = memory.IncrementAndFetch(Scalar(0f), Scalar(0f), Scalar(0f));
    Assert.AreEqual(1f, result[0]);

    // add one, returns two
    result = memory.IncrementAndFetch(Scalar(0f), Scalar(0f), Scalar(1f));
    Assert.AreEqual(2f, result[0]);

    // add -2, returns zero
    result = memory.IncrementAndFetch(Scalar(0f), Scalar(0f), Scalar(-2f));
    Assert.AreEqual(0f, result[0]);
  }

  [Test]
  public void TestAsBinaryTree () {
    var memory = new Memory(1, 1);

    // starts as a single leaf node (depth 1)
    Assert.AreEqual(1, memory.root.depth);

    // add one to the left side, we split and return 1
    var result = memory.IncrementAndFetch(Scalar(-0.25f), Scalar(-1f), Scalar(1f));
    Assert.AreEqual(2, memory.root.depth);
    Assert.AreEqual(1f, result[0]);

    // add zero to left side, still returns 1
    result = memory.IncrementAndFetch(Scalar(-0.25f), Scalar(-1f), Scalar(0f));
    Assert.AreEqual(1f, result[0]);

    // reading the parent node returns 0.5
    result = memory.IncrementAndFetch(Scalar(0f), Scalar(0f), Scalar(0f));
    Assert.AreEqual(0.5f, result[0]);

    // reading the right node returns 0.25 (bleedover)
    result = memory.IncrementAndFetch(Scalar(0.25f), Scalar(-1f), Scalar(0f));
    Assert.AreEqual(0.25f, result[0]);

    // add 0.5 to the root node, returns 1
    result = memory.IncrementAndFetch(Scalar(0f), Scalar(0f), Scalar(0.5f));
    Assert.AreEqual(1f, result[0]);

    // now the left node is increased
    result = memory.IncrementAndFetch(Scalar(-0.25f), Scalar(-1f), Scalar(0));
    Assert.AreEqual(1.25f, result[0]);

    // and so is the right
    result = memory.IncrementAndFetch(Scalar(0.25f), Scalar(-1f), Scalar(0));
    Assert.AreEqual(0.5f, result[0]);

    // force a 2x expansion, adding 1 to the left-left side
    result = memory.IncrementAndFetch(Scalar(-1f + 0.125f), Scalar(-2f), Scalar(1f));
    Assert.AreEqual(4, memory.root.depth);
    Assert.AreEqual(1.21875f, result[0]);

    // set that back to zero
    result = memory.IncrementAndFetch(Scalar(-1f + 0.125f), Scalar(-2f), Scalar(-1.21875f));
    Assert.AreEqual(0f, result[0]);

    // still zero?
    result = memory.IncrementAndFetch(Scalar(-1f + 0.125f), Scalar(-2f), Scalar(0f));
    Assert.AreEqual(0f, result[0]);
  }
}

}
