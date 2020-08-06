namespace GGFolks.Esl {

using System;
using System.Collections.Generic;
using System.IO;
using MathNet.Numerics.LinearAlgebra;

/// <summary>Sparse memory model.</summary>
public class Memory {

  /// <summary>Base class for nodes in the sparse tree.</summary>
  public abstract class Node {

    public static Node Read (
        BinaryReader reader, Vector<float> position, Vector<float> size, int contentElements) {
      var value = BinaryUtil.ReadVector(reader, contentElements);
      var splitIndex = reader.ReadInt32();
      if (splitIndex == 0) return new LeafNode(position, size, value);
      splitIndex--;
      var halfSize = size[splitIndex] * 0.5f;
      var childSize = CreateVector.SparseOfVector(size);
      childSize[splitIndex] = halfSize;
      var leftPosition = CreateVector.SparseOfVector(position);
      leftPosition[splitIndex] -= halfSize * 0.5f;
      var left = Read(reader, leftPosition, childSize, contentElements);
      var rightPosition = CreateVector.SparseOfVector(position);
      rightPosition[splitIndex] += halfSize * 0.5f;
      var right = Read(reader, rightPosition, childSize, contentElements);
      return new InternalNode(position, size, value, splitIndex, left, right);
    }

    public readonly Vector<float> position;
    public readonly Vector<float> size;
    public readonly Vector<float> value;

    public abstract int depth { get; }

    public Node (Vector<float> position, Vector<float> size, Vector<float> value) {
      this.position = position;
      this.size = size;
      this.value = value;
    }

    public virtual void Clear () {
      value.Clear();
    }

    public bool Intersects (Vector<float> position, Vector<float> size, int index) {
      var pointCenter = position[index];
      var pointExtent = size[index] * 0.5f;
      var pointMin = pointCenter - pointExtent;
      var pointMax = pointCenter + pointExtent;
      var thisCenter = this.position[index];
      var thisExtent = this.size[index] * 0.5f;
      var thisMin = thisCenter - thisExtent;
      var thisMax = thisCenter + thisExtent;
      return pointMax > thisMin && pointMin < thisMax;
    }

    public Node MaybeExpand (Vector<float> position, Vector<float> size) {
      // see if we have to expand on any axis
      var maxDiff = 0f;
      var maxIndex = -1;
      for (var ii = 0; ii < size.Count; ii++) {
        var pointCenter = position[ii];
        var pointExtent = size[ii] * 0.5f;
        var pointMin = pointCenter - pointExtent;
        var pointMax = pointCenter + pointExtent;
        var thisCenter = this.position[ii];
        var thisExtent = this.size[ii] * 0.5f;
        var thisMin = thisCenter - thisExtent;
        var thisMax = thisCenter + thisExtent;
        var diff = Math.Max(thisMin - pointMin, pointMax - thisMax);
        if (diff > maxDiff) (maxDiff, maxIndex) = (diff, ii);
      }
      if (maxIndex < 0) return this;

      var maxIndexSize = this.size[maxIndex];

      var expandedSize = this.size.Clone();
      expandedSize[maxIndex] = maxIndexSize * 2f;
      var contractedSize = this.size.Clone();
      contractedSize[maxIndex] = maxIndexSize * 0.5f;

      var leftPosition = this.position.Clone();
      leftPosition[maxIndex] -= maxIndexSize * 0.5f;
      var leftLeftPosition = leftPosition.Clone();
      leftLeftPosition[maxIndex] -= maxIndexSize * 0.25f;
      var left = new InternalNode(
        leftPosition, this.size, value.Multiply(0.5f), maxIndex,
        new LeafNode(leftLeftPosition, contractedSize, CreateVector.Sparse<float>(value.Count)),
        SplitLeft(maxIndex));

      var rightPosition = this.position.Clone();
      rightPosition[maxIndex] += maxIndexSize * 0.5f;
      var rightRightPosition = rightPosition.Clone();
      rightRightPosition[maxIndex] += maxIndexSize * 0.25f;
      var right = new InternalNode(
        rightPosition, this.size, value.Multiply(0.5f), maxIndex,
        SplitRight(maxIndex),
        new LeafNode(rightRightPosition, contractedSize,
          CreateVector.Sparse<float>(value.Count)));

      return new InternalNode(
        this.position, expandedSize, value.Clone(),
        maxIndex, left, right)
          .MaybeExpand(position, size);
    }

    public abstract Node Set (
      Vector<float> position, Vector<float> size, int axis, Vector<float>[] values, bool root);

    public abstract Node IncrementAndFetch (
      Vector<float> position, Vector<float> size, Vector<float> increment,
      Vector<float> result, bool root);

    public abstract Node SplitLeft (int splitIndex);
    public abstract Node SplitRight (int splitIndex);

    public virtual void Write (BinaryWriter writer) {
      BinaryUtil.WriteVector(writer, value);
    }

    protected void SetScaled (
        Vector<float> position, Vector<float> size, int axis, Vector<float>[] values, bool root) {
      var left = position[axis] - size[axis] * 0.5f;
      var scale = values.Length / size[axis];
      var index = (this.position[axis] - left) * scale - 0.5f;
      if (index <= 0f) values[0].CopyTo(value);
      else if (index >= values.Length - 1) values[values.Length - 1].CopyTo(value);
      else {
        var truncated = (int)index;
        var fraction = index - truncated;
        values[truncated + 1].CopyTo(value);
        value.Subtract(values[truncated], value);
        value.Multiply(fraction, value);
        value.Add(values[truncated], value);
      }
      if (!root) value.Multiply(4f / 3f, value);
    }

    protected void AddScaledAndFetch (
        Vector<float> position, Vector<float> size, Vector<float> increment,
        Vector<float> result, bool root) {
      var coverage = 1f;
      for (var ii = 0; ii < size.Count; ii++) {
        var pointCenter = position[ii];
        var pointExtent = size[ii] * 0.5f;
        var pointMin = pointCenter - pointExtent;
        var pointMax = pointCenter + pointExtent;
        var thisCenter = this.position[ii];
        var thisSize = this.size[ii];
        var thisExtent = thisSize * 0.5f;
        var thisMin = thisCenter - thisExtent;
        var thisMax = thisCenter + thisExtent;
        var intersectionMin = Math.Max(pointMin, thisMin);
        var intersectionMax = Math.Min(pointMax, thisMax);
        coverage *= (intersectionMax - intersectionMin) / thisSize;
      }
      var scaledIncrement = increment.Multiply(coverage);
      value.Add(scaledIncrement, value);
      // for non-root nodes, we subtract what we assume the parent added
      var scaledValue = value.Multiply(coverage * (root ? 1f : 0.75f));
      result.Add(scaledValue, result);
    }
  }

  /// <summary>A non-leaf node.</summary>
  public class InternalNode : Node {

    public readonly int splitIndex;
    public Node left { get; private set; }
    public Node right { get; private set; }

    public override int depth => 1 + Math.Max(left.depth, right.depth);

    public InternalNode (
      Vector<float> position, Vector<float> size, Vector<float> value,
      int splitIndex, Node left, Node right)
        : base(position, size, value) {
      this.splitIndex = splitIndex;
      this.left = left;
      this.right = right;
    }

    public override void Clear () {
      base.Clear();
      left.Clear();
      right.Clear();
    }

    public override Node Set (
        Vector<float> position, Vector<float> size, int axis, Vector<float>[] values, bool root) {
      // see if we have to pass down to children
      var ratios = size.PointwiseDivide(this.size);
      ratios[axis] /= values.Length;
      if (ratios.Minimum() > 0.5f) {
        SetScaled(position, size, axis, values, root);
        left.Clear();
        right.Clear();
        return this;
      }

      if (left.Intersects(position, size, splitIndex)) {
        left = left.Set(position, size, axis, values, false);
      }
      if (right.Intersects(position, size, splitIndex)) {
        right = right.Set(position, size, axis, values, false);
      }
      value.Clear();

      return this;
    }

    public override Node IncrementAndFetch (
        Vector<float> position, Vector<float> size, Vector<float> increment,
        Vector<float> result, bool root) {
      // add the increment scaled by coverage to our value
      AddScaledAndFetch(position, size, increment, result, root);

      // see if we have to pass down to children
      var ratios = size.PointwiseDivide(this.size);
      if (ratios.Minimum() > 0.5f) return this;

      if (left.Intersects(position, size, splitIndex)) {
        left = left.IncrementAndFetch(position, size, increment, result, false);
      }
      if (right.Intersects(position, size, splitIndex)) {
        right = right.IncrementAndFetch(position, size, increment, result, false);
      }

      return this;
    }

    public override Node SplitLeft (int splitIndex) {
      if (splitIndex == this.splitIndex) return left;
      var splitIndexSize = size[splitIndex];
      var contractedSize = size.Clone();
      contractedSize[splitIndex] = splitIndexSize * 0.5f;
      var leftPosition = position.Clone();
      leftPosition[splitIndex] -= splitIndexSize * 0.25f;
      return new InternalNode(
        leftPosition, contractedSize, value.Multiply(0.5f), this.splitIndex,
        left.SplitLeft(splitIndex),
        right.SplitLeft(splitIndex)
      );
    }

    public override Node SplitRight (int splitIndex) {
      if (splitIndex == this.splitIndex) return right;
      var splitIndexSize = size[splitIndex];
      var contractedSize = size.Clone();
      contractedSize[splitIndex] = splitIndexSize * 0.5f;
      var rightPosition = position.Clone();
      rightPosition[splitIndex] += splitIndexSize * 0.25f;
      return new InternalNode(
        rightPosition, contractedSize, value.Multiply(0.5f), this.splitIndex,
        left.SplitRight(splitIndex),
        right.SplitRight(splitIndex)
      );
    }

    public override void Write (BinaryWriter writer) {
      base.Write(writer);
      writer.Write(splitIndex + 1);
      left.Write(writer);
      right.Write(writer);
    }
  }

  /// <summary>A terminal node.</summary>
  public class LeafNode : Node {

    public override int depth => 1;

    public LeafNode (Vector<float> position, Vector<float> size, Vector<float> value) :
      base(position, size, value) {}

    public override Node Set (
        Vector<float> position, Vector<float> size, int axis, Vector<float>[] values, bool root) {
      // see if we have to subdivide
      var subdivided = MaybeSubdivide(size, size[axis] / (values.Length * this.size[axis]) <= 0.5f);
      if (subdivided != this) {
        return subdivided.Set(position, size, axis, values, root);
      }

      // if not, interpolate between the closest two values
      SetScaled(position, size, axis, values, root);

      return this;
    }

    public override Node IncrementAndFetch (
        Vector<float> position, Vector<float> size, Vector<float> increment,
        Vector<float> result, bool root) {
      // see if we have to subdivide
      var subdivided = MaybeSubdivide(size);
      if (subdivided != this) {
        return subdivided.IncrementAndFetch(position, size, increment, result, root);
      }

      // if not, we add the increment scaled by coverage to our value
      AddScaledAndFetch(position, size, increment, result, root);

      return this;
    }

    public Node MaybeSubdivide (Vector<float> size, bool force = false) {
      var ratios = size.PointwiseDivide(this.size);
      if (!force && ratios.Minimum() > 0.5f) return this;
      var minIndex = ratios.MinimumIndex();
      return new InternalNode(
        this.position, this.size, value,
        minIndex, SplitLeft(minIndex), SplitRight(minIndex));
    }

    public override Node SplitLeft (int splitIndex) {
      var splitIndexSize = size[splitIndex];
      var contractedSize = size.Clone();
      contractedSize[splitIndex] = splitIndexSize * 0.5f;
      var leftPosition = position.Clone();
      leftPosition[splitIndex] -= splitIndexSize * 0.25f;
      return new LeafNode(leftPosition, contractedSize, value.Multiply(0.5f));
    }

    public override Node SplitRight (int splitIndex) {
      var splitIndexSize = size[splitIndex];
      var contractedSize = size.Clone();
      contractedSize[splitIndex] = splitIndexSize * 0.5f;
      var rightPosition = position.Clone();
      rightPosition[splitIndex] += splitIndexSize * 0.25f;
      return new LeafNode(rightPosition, contractedSize, value.Multiply(0.5f));
    }

    public override void Write (BinaryWriter writer) {
      base.Write(writer);
      writer.Write(0);
    }
  }

  /// <summary>The number of elements in our position/size vectors.</summary>
  public int positionElements { get; private set; }

  /// <summary>The number of elements in our value vectors,</summary>
  public int contentElements { get; private set; }

  /// <summary> The root node of the memory tree.</summary>
  public Node root { get; private set; }

  public Memory (BinaryReader reader) {
    var stream = reader.BaseStream;
    if (stream.Position == stream.Length) {
      Init(1, 1);
      return;
    }
    positionElements = reader.ReadInt32();
    contentElements = reader.ReadInt32();
    var size = BinaryUtil.ReadVector(reader, positionElements);
    root = Node.Read(reader, CreateVector.Sparse<float>(size.Count), size, contentElements);
  }

  public Memory (int positionElements, int contentElements) {
    Init(positionElements, contentElements);
  }

  /// <summary>
  /// Sets a series of values simultaneously.
  /// </summary>
  public void Set (
      Vector<float> position, Vector<float> size, int axis, Vector<float>[] values) {
    var pow2Size = CreateVector.Sparse<float>(positionElements, 2f).PointwisePower(size);
    root = root.MaybeExpand(position, pow2Size).Set(position, pow2Size, axis, values, true);
  }

  /// <summary>
  /// Increments the values at the specified address by the provided amounts in the supplied sizes.
  /// <summary>
  public Vector<float> IncrementAndFetch (
      Vector<float> position, Vector<float> size, Vector<float> increment) {
    var result = CreateVector.Sparse<float>(contentElements);
    var pow2Size = CreateVector.Sparse<float>(positionElements, 2f).PointwisePower(size);
    root = root.MaybeExpand(position, pow2Size)
      .IncrementAndFetch(position, pow2Size, increment, result, true);
    return result;
  }

  /// <summary>
  /// Serializes the memory to the supplied writer.
  /// </summary>
  public void Write (BinaryWriter writer) {
    writer.Write(positionElements);
    writer.Write(contentElements);
    BinaryUtil.WriteVector(writer, root.size);
    root.Write(writer);
  }

  private void Init (int positionElements, int contentElements) {
    this.positionElements = positionElements;
    this.contentElements = contentElements;
    root = new LeafNode(
      CreateVector.Sparse<float>(positionElements),
      CreateVector.Sparse<float>(positionElements, 1f),
      CreateVector.Sparse<float>(contentElements));
  }
}

}
