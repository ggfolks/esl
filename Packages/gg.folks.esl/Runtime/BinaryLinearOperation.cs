namespace GGFolks.Esl {

using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;

/// <summary>A binary linear operation (blop), which generalizes addition, subtraction, negation,
/// and, or, not... but not XOR, e.g.</summary>
public class BinaryLinearOperation : Expression {

  public Expression left;
  public Expression right;
  public readonly Vector<float> opCoefficients;
  public Vector<float> leftCoefficients;
  public Vector<float> rightCoefficients;
  public readonly Activation activation;

  public override int depth => _depth ?? (int)(_depth = 1 + Math.Max(leftDepth, rightDepth));
  public int leftDepth => left?.depth ?? 0;
  public int rightDepth => right?.depth ?? 0;

  public bool commutable => activation == Activation.Identity &&
    opCoefficients[1] == opCoefficients[2] && // symmetric operations (+, *, |, etc.)
    opCoefficients[3] == 0f &&
    leftCoefficients == null &&
    rightCoefficients == null;

  public BinaryLinearOperation (
      Expression left, Expression right,
      float lr, float l, float r, float c,
      Vector<float> leftCoefficients, Vector<float> rightCoefficients,
      Activation activation = Activation.Identity,
      Func<string> stringify = null) : base(stringify ?? (() =>
        $"BinaryLinearOperation({left}, {right}, {lr}f, {l}f, {r}f, {c}f, {activation})")) {
    this.left = left;
    this.right = right;
    opCoefficients = CreateVector.Dense<float>(4);
    opCoefficients[0] = lr;
    opCoefficients[1] = l;
    opCoefficients[2] = r;
    opCoefficients[3] = c;
    this.leftCoefficients = leftCoefficients;
    this.rightCoefficients = rightCoefficients;
    this.activation = activation;
  }

  public BinaryLinearOperation WithActivation (Activation activation, Func<string> stringify) {
    return new BinaryLinearOperation(
      left, right, opCoefficients[0], opCoefficients[1], opCoefficients[2], opCoefficients[3],
      leftCoefficients, rightCoefficients, activation, stringify);
  }

  public override Expression Compact () {
    left = left?.Compact();
    right = right?.Compact();
    var diff = leftDepth - rightDepth;
    if (diff < 0) { // put the deeper side on the left
      (left, right) = (right, left);
      (leftCoefficients, rightCoefficients) = (rightCoefficients, leftCoefficients);
      (opCoefficients[1], opCoefficients[2]) = (opCoefficients[2], opCoefficients[1]);
      diff = -diff;
    }
    // redistribute commutable operations to reduce depth
    if (
      diff >= 2 && commutable && left is BinaryLinearOperation lblop && lblop.commutable &&
      lblop.opCoefficients.Equals(opCoefficients)
    ) {
      var newLeftDepth = lblop.leftDepth;
      var newRightDepth = 1 + Math.Max(lblop.rightDepth, rightDepth);
      if (newLeftDepth - newRightDepth >= 0) {
        left = lblop.left;
        right = new BinaryLinearOperation(lblop.right, right,
          opCoefficients[0], opCoefficients[1], opCoefficients[2], opCoefficients[3], null, null);
      }
    }
    return this;
  }

  public override void DeduplicateChildren (
      Dictionary<Expression, Expression> uniqueInstances, Reference reference) {
    if (left != null) {
      left.Deduplicate(uniqueInstances, new Expression.Reference(
        reference.output, reference.depth + 1, reference.path, value => left = value));
    }
    if (right != null) {
      right.Deduplicate(uniqueInstances, new Expression.Reference(
        reference.output, reference.depth + 1,
        reference.path | (1UL << reference.depth), value => right = value));
    }
    operationCount = 5 + (left?.operationCount ?? 0) + (right?.operationCount ?? 0);
  }

  public override int GetHashCode () {
    return 31 * (31 * (31 * opCoefficients.GetHashCode() +
      (int)activation) + (left?.GetHashCode() ?? 0)) + (right?.GetHashCode() ?? 0);
  }

  public override bool Equals (object other) {
    return other is BinaryLinearOperation blop &&
      Object.Equals(left, blop.left) &&
      Object.Equals(right, blop.right) &&
      opCoefficients.Equals(blop.opCoefficients) &&
      Object.Equals(leftCoefficients, blop.leftCoefficients) &&
      Object.Equals(rightCoefficients, blop.rightCoefficients) &&
      activation == blop.activation;
  }

  private int? _depth;
}

}
