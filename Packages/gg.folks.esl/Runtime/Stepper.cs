namespace GGFolks.Esl {

using System.IO;
using MathNet.Numerics.LinearAlgebra;

public abstract class Stepper {

  public static Stepper Read (BinaryReader reader, Model model) {
    var inputCoefficients = new Vector<float>[model.outputCount];
    for (var ii = 0; ii < model.outputCount; ii++) {
      inputCoefficients[ii] = BinaryUtil.ReadVector(reader, model.inputCount);
    }
    return reader.ReadBoolean()
      ? (Stepper)new InternalStepper(model, inputCoefficients, reader)
      : new LeafStepper(model, inputCoefficients);
  }

  public readonly Model model;

  public readonly Vector<float>[] inputCoefficients;

  public abstract int operationCount { get; }

  public Stepper (Model model, Vector<float>[] inputCoefficients = null) {
    this.model = model;
    if (inputCoefficients != null) this.inputCoefficients = inputCoefficients;
    else {
      this.inputCoefficients = new Vector<float>[model.outputCount];
      for (var output = 0; output < model.outputCount; output++) {
        this.inputCoefficients[output] = CreateVector.Sparse<float>(model.inputCount);
      }
    }
  }

  public virtual Stepper SetOutput (int output, Expression value) {
    if (value is Terminal terminal) terminal.inputCoefficients.CopyTo(inputCoefficients[output]);
    return this;
  }

  public abstract Vector<float> Step (Vector<float> inputVector, Vector<float> resultVector = null);

  public virtual void Write (BinaryWriter writer) {
    BinaryUtil.WriteVectorArray(writer, inputCoefficients);
  }
}

}
