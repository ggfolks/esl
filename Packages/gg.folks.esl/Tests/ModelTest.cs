namespace GGFolks.Esl {

using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using NUnit.Framework;

using static Expression;

/// <summary>Tests the simulation model.</summary>
[TestFixture]
public class ModelTest : SimulationTest {

  [Test]
  public void TestAsStateless () {
    // create a model that simply adds one to its input
    var model = new Model(0, 1, 1, new List<double>());
    model.StartBuilding();
    try {
      model.outputs[0] = Input(0) + 1f;
    } finally {
      model.FinishBuilding();
    }
    var result = model.Step(Scalar(1f));
    Assert.AreEqual(2f, result[0]);

    // create a model that determines whether its inputs (as x and y) lie within a unit circle,
    // returning 3 if so and 7 if not
    model = new Model(0, 2, 1, new List<double>());
    model.StartBuilding();
    try {
      model.outputs[0] = If(Input(0) * Input(0) + Input(1) * Input(1) < 1f, 3f, 7f);
    } finally {
      model.FinishBuilding();
    }
    result = model.Step(Vector(0f, 0f));
    Assert.AreEqual(3f, result[0]);
    result = model.Step(Vector(2f, -9f));
    Assert.AreEqual(7f, result[0]);
    result = model.Step(Vector(0.25f, 0.25f));
    Assert.AreEqual(3f, result[0]);
  }

  [Test]
  public void TestAsBallistic () {
    // create a model that simulates a ballistic trajectory in one dimension using Euler steps of
    // durations determined by the input.  the state consists of the position (0) velocity (1)
    var model = new Model(0, 1, 1, new List<double> { 0.0, 9.8 });
    model.StartBuilding();
    try {
      model.outputs[0] = State(0);
      model.outputs[model.stateOutputOffset] = State(0) + State(1) * Input(0);
      model.outputs[model.stateOutputOffset + 1] = State(1) - 9.8f * Input(0);
    } finally {
      model.FinishBuilding();
    }

    var result = model.Step(Scalar(0.01f));
    Assert.AreEqual(0f, result[0]); // starts at zero
    for (var ii = 0; ii < 100; ii++) result = model.Step(Scalar(0.01f));
    Assert.Greater(result[0], 4.5f);
    Assert.Less(result[0], 5.0f); // reaches 4.9
    for (var ii = 0; ii < 200; ii++) result = model.Step(Scalar(0.01f));
    Assert.Greater(result[0], -15f);
    Assert.Less(result[0], -14f); // drops to -14.7
  }

  [Test]
  public void TestAsFiniteStateMachine () {
    // create a model that uses two state elements to represent a finite state machine with four
    // states (corresponding to when the elements are 00, 01, 10, or 11) that counts successive
    // true values (up to three), providing the count as of the previous step as an output
    var model = new Model(0, 1, 1, new List<double> { 0.0, 0.0 });
    model.StartBuilding();
    try {
      model.outputs[0] = State(0) * 2f + State(1);
      model.outputs[model.stateOutputOffset] = Input(0) & (State(0) | State(1));
      model.outputs[model.stateOutputOffset + 1] = Input(0) & (State(0) | !State(1));
    } finally {
      model.FinishBuilding();
    }

    var result = model.Step(Scalar(0f));
    Assert.AreEqual(0f, result[0]); // starts at zero
    result = model.Step(Scalar(1f));
    Assert.AreEqual(0f, result[0]); // after first consecutive "true," still zero
    result = model.Step(Scalar(1f));
    Assert.AreEqual(1f, result[0]); // after second, we have counted the first
    result = model.Step(Scalar(1f));
    Assert.AreEqual(2f, result[0]); // after third, we have counted the second
    result = model.Step(Scalar(0f));
    Assert.AreEqual(3f, result[0]); // we have counted the third, but the count is reset
    result = model.Step(Scalar(0f));
    Assert.AreEqual(0f, result[0]); // now it returns zero again
  }

  [Test]
  public void TestAsPushdownAutomaton () {
    // create a model that uses one state element (a stack pointer) and sparse memory that pushes
    // a value (1) onto the stack when the input is true and pops one off when the input is false,
    // returning as an output whether the stack was valid (that is, we haven't popped more than
    // we've pushed) as of the conclusion of the last step
    var model = new Model(0, 1, 1, new List<double> { 0.0 });
    model.StartBuilding();
    try {
      model.outputs[0] = Fetch(0) >= 0f;
      model.outputs[model.stateOutputOffset] = State(0) + Input(0) * 2f - 1f;
      model.outputs[model.positionOutputOffset] = State(0) + Input(0) - 1f;
      model.outputs[model.incrementOutputOffset] = Input(0) * 2f - 1f;
    } finally {
      model.FinishBuilding();
    }

    Vector<float> result;
    for (var ii = 0; ii < 10; ii++) { // push ten items onto the stack
      result = model.Step(Scalar(1f));
      Assert.AreEqual(1f, result[0]);
    }
    for (var ii = 0; ii < 11; ii++) { // pop eleven items off the stack
      result = model.Step(Scalar(0f));
      Assert.AreEqual(1f, result[0]);
    }
    result = model.Step(Scalar(1f)); // now, whatever we do here, the result will be false
    Assert.AreEqual(0f, result[0]);
  }

  [Test]
  public void TestAsTuringMachine () {
    // create a model that executes a stored program to compute the 7th Fibonacci number.  the state
    // consists of a program counter and four "register" variables: previous, current, ii, and tmp.
    // at each step, the model outputs the value of "current," which should not exceed the correct
    // answer.  the operations consist of an operator (0 = noop, 1 = move, 2 = add, 3 = decrement
    // and jump if greater than zero) and two operands (dest register index and either src register
    // index (move/add) or jump offset (djgz))
    var model = new Model(0, 1, 1, new List<double> { -0.5, 0.0, 1.0, 7.0, 0.0 }, 1, 3);
    model.StartBuilding();
    try {
      model.outputs[0] = State(2);
      var pc = State(0) +
        If(Fetch(0) > 2.5f & State(Fetch(1), 1, 4) > 1f, Fetch(2), 1f);
      model.outputs[model.stateOutputOffset] = pc;
      for (var ii = 1; ii <= 4; ii++) {
        model.outputs[model.stateOutputOffset + ii] = If(
          IsIndex(Fetch(1), ii - 1, 4),
          Index(
            new List<Expression> {
              State(ii),
              State(Fetch(2), 1, 4),
              State(ii) + State(Fetch(2), 1, 4),
              State(ii) - 1f,
            },
            0f, 3f, Fetch(0)),
          State(ii)
        );
      }
      model.outputs[model.positionOutputOffset] = pc;
    } finally {
      model.FinishBuilding();
    }

    // store our program in memory
    model.memory.Set(Scalar(2f), Scalar(2f), 0, new [] {
      Vector(1f, 3f, 0f), // top: tmp = previous
      Vector(1f, 0f, 1f), // previous = current
      Vector(2f, 1f, 3f), // current += tmp
      Vector(3f, 2f, -3f), // if (ii-- > 0) goto top
    });

    for (var ii = 0; ii < 30; ii++) model.Step(Scalar(0f));
    var result = model.Step(Scalar(0f));
    Assert.AreEqual(21f, result[0]);
  }
}

}
