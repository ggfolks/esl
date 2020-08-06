namespace GGFolks.Esl {

using System;
using MathNet.Numerics.LinearAlgebra;

/// <summary>A terminal node in the AST: any linear combination of the inputs.</summary>
public class Terminal : Expression {

  public readonly Vector<float> inputCoefficients;

  public override int depth => 0;

  public Terminal (Vector<float> inputCoefficients, Func<string> stringify = null) : base(
      stringify ?? (() => $"Terminal({inputCoefficients})")) {
    this.inputCoefficients = inputCoefficients;
  }

  public override Expression Compact () {
    var emptyVector = CreateVector.Sparse<float>(inputCoefficients.Count);
    return inputCoefficients.Equals(emptyVector) ? null : this;
  }

  public override int GetHashCode () {
    return inputCoefficients.GetHashCode();
  }

  public override bool Equals (object other) {
    return other is Terminal to && inputCoefficients.Equals(to.inputCoefficients);
  }
}

}
