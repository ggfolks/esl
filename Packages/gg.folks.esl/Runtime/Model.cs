namespace GGFolks.Esl {

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;

/// <summary>
/// Generic all-singing, all-dancing simulation model.
/// </summary>
public class Model {

  public static Model building { get; private set; }

  public readonly System.Random random = new System.Random();
  public int randomElements { get; private set; }
  public int inputElements { get; private set; }
  public int outputElements { get; private set; }
  public List<double> states { get; private set; }
  public int inputCount { get; private set; }
  public int outputCount { get; private set; }
  public int inputInputOffset { get; private set; }
  public int stateInputOffset { get; private set; }
  public int contentsInputOffset { get; private set; }
  public int stateOutputOffset { get; private set; }
  public int positionOutputOffset { get; private set; }
  public int sizeOutputOffset { get; private set; }
  public int incrementOutputOffset { get; private set; }

  public Memory memory { get; private set; }
  public Vector<float> fetchResult { get; private set; }

  public Expression[] outputs { get; private set; }
  public bool simplifyExpressions { get; private set; }

  public Stepper stepper { get; private set; }

  public Model (BinaryReader reader) {
    var stream = reader.BaseStream;
    if (stream.Position == stream.Length) {
      Init(0, 1, 1, new List<double>());
      return;
    }
    Init(
      reader.ReadInt32(),
      reader.ReadInt32(),
      reader.ReadInt32(),
      new List<double>(reader.ReadInt32()),
      reader.ReadInt32(),
      reader.ReadInt32());
    stepper = Stepper.Read(reader, this);
  }

  public Model (
      int randomElements, int inputElements, int outputElements,
      List<double> states, int positionElements = 1, int contentElements = 1,
      bool simplifyExpressions = true) {
    Init(randomElements, inputElements, outputElements, states,
      positionElements, contentElements, simplifyExpressions);
  }

  public void StartBuilding () {
    building = this;
  }

  public void FinishBuilding () {
    building = null;

    // compact output expressions
    for (var output = 0; output < outputCount; output++) outputs[output].Compact();
    stepper = new LeafStepper(this);

    // deduplicate the output expressions, tracking references for subexpression replacement
    var uniqueExpressions = new Dictionary<Expression, Expression>();
    for (var output = 0; output < outputCount; output++) {
      var referenceOutput = output;
      outputs[output].Deduplicate(uniqueExpressions,
        new Expression.Reference(output, 0, 0L, expr => outputs[referenceOutput] = expr));
    }

    // put all the unique expressions into a heap sorted by savings
    var heap = new Heap<Expression>();
    foreach (var expr in uniqueExpressions.Values) heap.Add(expr);

    // collapse subexpressions from the heap until we reach ones with no savings
    while (heap.count > 0) {
      var subexpr = heap.TakeLowest();
      if (subexpr.savings == 0) break;
      Debug.Log($@"Collapsing subexpression [subexpr={subexpr},
        savings={subexpr.savings}, operationCount={subexpr.operationCount},
        references={subexpr.references.Count}].");
      subexpr.Collapse(this);
    }

    // configure the stepper with the output expressions
    for (var output = 0; output < outputCount; output++) {
      stepper = stepper.SetOutput(output, outputs[output]);
    }

    Debug.Log($@"Created stepper [operationCount={stepper.operationCount},
      uniqueExpressions={uniqueExpressions.Count}]");
  }

  public Terminal Constant (float value) {
    var inputCoefficients = CreateVector.Sparse<float>(inputCount);
    inputCoefficients[BiasOffset] = value;
    return new Terminal(inputCoefficients, () => $"Constant({value}f)");
  }

  public Terminal Random (float min, float max, int source = 0) {
    return Value(RandomOffset + source, max - min, min, () => $"Random({min}f, {max}f, {source})");
  }

  public Terminal Input (int input, Func<string> stringify = null) {
    return Value(inputInputOffset + input, 1f, 0f, stringify ?? (() => $"Input({input})"));
  }

  public Terminal State (int state) {
    return Value(stateInputOffset + state, 1f, 0f, () => $"State({state})");
  }

  public Terminal Fetch (int memory) {
    return Value(contentsInputOffset + memory, 1f, 0f, () => $"Fetch({memory})");
  }

  public Terminal Value (
      int input, float scale = 1f, float offset = 0f, Func<string> stringify = null) {
    var inputCoefficients = CreateVector.Sparse<float>(inputCount);
    inputCoefficients[input] = scale;
    inputCoefficients[BiasOffset] = offset;
    return new Terminal(inputCoefficients,
      stringify ?? (() => $"Value({input}, {scale}f, {offset}f)"));
  }

  public Vector<float> OutputCoefficients (int output) {
    var coefficients = CreateVector.Sparse<float>(outputCount);
    coefficients[output] = 1f;
    return coefficients;
  }

  public Vector<float> Step (Vector<float> inputVector, Vector<float> resultVector = null) {
    var combinedInputVector = CreateVector.Sparse<float>(inputCount);
    combinedInputVector[BiasOffset] = 1f;
    for (var ii = 0; ii < randomElements; ii++) {
      combinedInputVector[RandomOffset + ii] = (float)random.NextDouble();
    }
    inputVector.CopySubVectorTo(combinedInputVector, 0, inputInputOffset, inputElements);
    for (var ii = 0; ii < states.Count; ii++) {
      combinedInputVector[stateInputOffset + ii] = (float)states[ii];
    }
    fetchResult.CopySubVectorTo(combinedInputVector, 0, contentsInputOffset, fetchResult.Count);
    resultVector = stepper.Step(combinedInputVector, resultVector);
    for (var ii = 0; ii < states.Count; ii++) states[ii] = resultVector[stateOutputOffset + ii];
    fetchResult = memory.IncrementAndFetch(
      resultVector.SubVector(positionOutputOffset, memory.positionElements),
      resultVector.SubVector(sizeOutputOffset, memory.positionElements),
      resultVector.SubVector(incrementOutputOffset, memory.contentElements));

    return resultVector;
  }

  /// <summary>
  /// Serializes the rules to the supplied writer.
  /// </summary>
  public void Write (BinaryWriter writer) {
    writer.Write(randomElements);
    writer.Write(inputElements);
    writer.Write(outputElements);
    writer.Write(states.Count);
    writer.Write(memory.positionElements);
    writer.Write(memory.contentElements);
    stepper.Write(writer);
  }

  private void Init (
      int randomElements, int inputElements, int outputElements,
      List<double> states, int positionElements = 1, int contentElements = 1,
      bool simplifyExpressions = true) {
    this.randomElements = randomElements;
    this.inputElements = inputElements;
    this.outputElements = outputElements;
    this.states = states;
    inputCount = 1 + randomElements + inputElements + states.Count + contentElements;
    outputCount = outputElements + states.Count + positionElements * 2 + contentElements;
    inputInputOffset = 1 + randomElements;
    stateInputOffset = inputInputOffset + inputElements;
    contentsInputOffset = stateInputOffset + states.Count;
    stateOutputOffset = outputElements;
    positionOutputOffset = stateOutputOffset + states.Count;
    sizeOutputOffset = positionOutputOffset + positionElements;
    incrementOutputOffset = sizeOutputOffset + positionElements;

    memory = new Memory(positionElements, contentElements);
    fetchResult = CreateVector.Sparse<float>(contentElements);

    outputs = new Expression[outputCount];
    for (var output = 0; output < outputCount; output++) outputs[output] = Constant(0f);

    this.simplifyExpressions = simplifyExpressions;

    stepper = new LeafStepper(this);
  }

  private const int BiasOffset = 0;
  private const int RandomOffset = 1;
}

}
