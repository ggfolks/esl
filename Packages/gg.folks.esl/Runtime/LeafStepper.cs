namespace GGFolks.Esl {

using System.IO;
using MathNet.Numerics.LinearAlgebra;

public class LeafStepper : Stepper {

  public override int operationCount => model.outputCount;

  public LeafStepper (Model model, Vector<float>[] inputCoefficients = null)
    : base(model, inputCoefficients) {}

  public override Stepper SetOutput (int output, Expression value) {
    return value is BinaryLinearOperation
      ? new InternalStepper(model, inputCoefficients).SetOutput(output, value)
      : base.SetOutput(output, value);
  }

  public override Vector<float> Step (Vector<float> inputVector, Vector<float> resultVector) {
    resultVector = resultVector ?? CreateVector.Sparse<float>(model.outputCount);
    for (var output = 0; output < model.outputCount; output++) {
      resultVector[output] = inputCoefficients[output].DotProduct(inputVector);
    }
    return resultVector;
  }

  public override void Write (BinaryWriter writer) {
    base.Write(writer);
    writer.Write(false);
  }
}

}
