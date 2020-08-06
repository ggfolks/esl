namespace GGFolks.Esl {

using System;
using System.IO;
using MathNet.Numerics.LinearAlgebra;

public class InternalStepper : Stepper {

  public Stepper left { get; private set; }
  public Stepper right { get; private set; }
  public readonly Vector<float>[] opCoefficients;
  public readonly Vector<float>[] leftCoefficients;
  public readonly Vector<float>[] rightCoefficients;
  public readonly Vector<float>[] activationCoefficients;

  public override int operationCount =>
    (left == null ? 0 : left.operationCount + model.outputCount) +
    (right == null ? 0 : right.operationCount + model.outputCount) +
    model.outputCount * 2 + 5;

  public InternalStepper (Model model, Vector<float>[] inputCoefficients, BinaryReader reader)
      : base(model, inputCoefficients) {
    left = reader.ReadBoolean() ? Stepper.Read(reader, model) : null;
    right = reader.ReadBoolean() ? Stepper.Read(reader, model) : null;
    opCoefficients = BinaryUtil.ReadVectorArray(reader, 4, model.outputCount);
    leftCoefficients = BinaryUtil.ReadVectorArray(reader, model.outputCount, model.outputCount);
    rightCoefficients = BinaryUtil.ReadVectorArray(reader, model.outputCount, model.outputCount);
    activationCoefficients = BinaryUtil.ReadVectorArray(
      reader, model.outputCount, Enum.GetValues(typeof(Activation)).Length);
  }

  public InternalStepper (Model model, Vector<float>[] inputCoefficients)
      : base(model, inputCoefficients) {
    opCoefficients = new [] {
      CreateVector.Sparse<float>(model.outputCount), // lr
      CreateVector.Sparse<float>(model.outputCount), // l
      CreateVector.Sparse<float>(model.outputCount), // r
      CreateVector.Sparse<float>(model.outputCount), // c
    };
    leftCoefficients = new Vector<float>[model.outputCount];
    rightCoefficients = new Vector<float>[model.outputCount];
    activationCoefficients = new Vector<float>[model.outputCount];

    for (var output = 0; output < model.outputCount; output++) {
      leftCoefficients[output] = model.OutputCoefficients(output);
      rightCoefficients[output] = model.OutputCoefficients(output);
      activationCoefficients[output] = ActivationCoefficients(Activation.Identity);
    }
  }

  public override Stepper SetOutput (int output, Expression value) {
    base.SetOutput(output, value);
    if (value is BinaryLinearOperation blop) {
      left = (left ?? new LeafStepper(model)).SetOutput(output, blop.left);
      right = (right ?? new LeafStepper(model)).SetOutput(output, blop.right);
      opCoefficients[0][output] = blop.opCoefficients[0];
      opCoefficients[1][output] = blop.opCoefficients[1];
      opCoefficients[2][output] = blop.opCoefficients[2];
      opCoefficients[3][output] = blop.opCoefficients[3];
      blop.leftCoefficients?.CopyTo(leftCoefficients[output]);
      blop.rightCoefficients?.CopyTo(rightCoefficients[output]);
      activationCoefficients[output] = ActivationCoefficients(blop.activation);
    }
    return this;
  }

  public override Vector<float> Step (
      Vector<float> inputVector, Vector<float> resultVector = null) {
    resultVector = resultVector ?? CreateVector.Sparse<float>(model.outputCount);

    var leftVector = CreateVector.Sparse<float>(model.outputCount);
    if (left != null) {
      var outputVector = left.Step(inputVector);
      for (var output = 0; output < model.outputCount; output++) {
        leftVector[output] = leftCoefficients[output].DotProduct(outputVector);
      }
    }
    var rightVector = CreateVector.Sparse<float>(model.outputCount);
    if (right != null) {
      var outputVector = right.Step(inputVector);
      for (var output = 0; output < model.outputCount; output++) {
        rightVector[output] = rightCoefficients[output].DotProduct(outputVector);
      }
    }

    var productVector = leftVector.PointwiseMultiply(rightVector);
    var constantVector = CreateVector.Sparse<float>(model.outputCount, 1);

    productVector.PointwiseMultiply(opCoefficients[0], productVector);
    leftVector.PointwiseMultiply(opCoefficients[1], leftVector);
    rightVector.PointwiseMultiply(opCoefficients[2], rightVector);
    constantVector.PointwiseMultiply(opCoefficients[3], constantVector);

    var identityVector = productVector.Add(leftVector);
    identityVector.Add(rightVector, identityVector);
    identityVector.Add(constantVector, identityVector);

    var binaryStepVector = identityVector.PointwiseSign();
    binaryStepVector.PointwiseMaximum(0f, binaryStepVector);

    var activatedVectors = new [] { identityVector, binaryStepVector };

    var activatedVector = CreateVector.Sparse<float>(activatedVectors.Length);
    for (int output = 0; output < model.outputCount; output++) {
      for (int activation = 0; activation < activatedVectors.Length; activation++) {
        activatedVector[activation] = activatedVectors[activation][output];
      }
      resultVector[output] = activationCoefficients[output].DotProduct(activatedVector) +
        inputCoefficients[output].DotProduct(inputVector);
    }

    return resultVector;
  }

  public override void Write (BinaryWriter writer) {
    base.Write(writer);
    writer.Write(true);
    if (left == null) writer.Write(false);
    else {
      writer.Write(true);
      left.Write(writer);
    }
    if (right == null) writer.Write(false);
    else {
      writer.Write(true);
      right.Write(writer);
    }
    BinaryUtil.WriteVectorArray(writer, opCoefficients);
    BinaryUtil.WriteVectorArray(writer, leftCoefficients);
    BinaryUtil.WriteVectorArray(writer, rightCoefficients);
    BinaryUtil.WriteVectorArray(writer, activationCoefficients);
  }

  private static Vector<float> ActivationCoefficients (Activation activation) {
    var coefficients = CreateVector.Sparse<float>(Enum.GetValues(typeof(Activation)).Length);
    coefficients[(int)activation] = 1f;
    return coefficients;
  }
}

}
