namespace GGFolks.Esl {

using System.IO;
using MathNet.Numerics.LinearAlgebra;

/// <summary>Utility methods relating to encoding/decoding.</summary>
public static class BinaryUtil {

  /// <summary>Writes a vector array without length information.</summary>
  public static void WriteVectorArray (BinaryWriter writer, Vector<float>[] values) {
    foreach (var value in values) WriteVector(writer, value);
  }

  /// <summary>Writes a vector without length information.</summary>
  public static void WriteVector (BinaryWriter writer, Vector<float> value) {
    for (var ii = 0; ii < value.Count; ii++) writer.Write(value[ii]);
  }

  /// <summary>Reads a vector array of preestablished length.</summary>
  public static Vector<float>[] ReadVectorArray (BinaryReader reader, int count, int elements) {
    var values = new Vector<float>[count];
    for (var ii = 0; ii < values.Length; ii++) values[ii] = ReadVector(reader, elements);
    return values;
  }

  /// <summary>Reads a vector of preestablished length.</summary>
  public static Vector<float> ReadVector (BinaryReader reader, int elements) {
    var value = CreateVector.Sparse<float>(elements);
    for (var ii = 0; ii < value.Count; ii++) value[ii] = reader.ReadSingle();
    return value;
  }
}

}
