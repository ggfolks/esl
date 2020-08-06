namespace GGFolks.Esl {

using MathNet.Numerics.LinearAlgebra;

/// <summary>Base class for simulation tests.</summary>
public abstract class SimulationTest {

  protected static Vector<float> Scalar (float value = 0f) {
    return Vector(value);
  }

  protected static Vector<float> Vector (params float[] values) {
    return CreateVector.DenseOfArray(values);
  }
}

}
