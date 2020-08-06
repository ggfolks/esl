namespace GGFolks.Esl {

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Base class for all expressions.</summary>
public abstract class Expression : IComparable<Expression> {

  /// <summary>Tracks a reference to a shared subexpression.</summary>
  public readonly struct Reference : IComparable<Reference> {

    public readonly int output;
    public readonly int depth;
    public readonly ulong path;
    public readonly Action<Expression> set;

    public Reference (int output, int depth, ulong path, Action<Expression> set) {
      this.output = output;
      this.depth = depth;
      this.path = path;
      this.set = set;
    }

    /// <summary>Checks whether the subtree rooted at this reference could contain the specified
    /// other reference.</summary>
    public bool SubtreeContains (Reference other) {
      return depth < other.depth && path == (other.path & (1UL << depth) - 1);
    }

    public int CompareTo (Reference other) {
      return other.depth - depth;
    }
  }

  public static implicit operator Expression(float value) => Constant(value);
  public static implicit operator Expression(bool value) => Constant(value ? 1f : 0f);

  public static Expression operator +(Expression a) => Identity(a);
  public static Expression operator -(Expression a) => Negate(a);

  public static Expression operator +(Expression a, Expression b) => Add(a, b);
  public static Expression operator -(Expression a, Expression b) => Subtract(a, b);

  public static Expression operator *(Expression a, float s) => Multiply(a, s);
  public static Expression operator *(Expression a, Expression b) => Multiply(a, b);

  public static Expression operator !(Expression a) => Not(a);

  public static Expression operator &(Expression a, Expression b) => And(a, b);
  public static Expression operator |(Expression a, Expression b) => Or(a, b);
  public static Expression operator ^(Expression a, Expression b) => Xor(a, b);

  public static Expression operator <(Expression a, Expression b) => LessThan(a, b);
  public static Expression operator <=(Expression a, Expression b) => LessThanOrEquals(a, b);
  public static Expression operator >(Expression a, Expression b) => GreaterThan(a, b);
  public static Expression operator >=(Expression a, Expression b) => GreaterThanOrEquals(a, b);

  public static Expression Lerp (Expression a, Expression b, Expression t) {
    return LerpUnclamped(a, b, Clamp01(t), () => $"Lerp({a}, {b}, {t})");
  }

  public static Expression LerpUnclamped (
      Expression a, Expression b, Expression t, Func<string> stringify = null) {
    return Add(a, (b - a) * t, stringify ?? (() => $"LerpUnclamped({a}, {b}, {t})"));
  }

  public static Expression Clamp01 (Expression expr) {
    return Clamp(expr, 0f, 1f, () => $"Clamp01({expr})");
  }

  public static Expression Clamp (
      Expression expr, Expression min, Expression max, Func<string> stringify = null) {
    return Min(Max(expr, min), max, stringify ?? (() => $"Clamp({expr}, {min}, {max})"));
  }

  public static Expression Min (Expression l, Expression r, Func<string> stringify = null) {
    return If(l < r, l, r, stringify ?? (() => $"Min({l}, {r})"));
  }

  public static Expression Max (Expression l, Expression r) {
    return If(l > r, l, r, () => $"Max({l}, {r})");
  }

  public static Expression Approximately (Expression a, Expression b, Expression epsilon = null) {
    return LessThan(Abs(a - b), epsilon ?? 0.000001f,
      () => $"Approximately({a}, {b}{(epsilon == null ? "" : $", {epsilon}")})");
  }

  public static Expression Abs (Expression expr) {
    return If(expr < 0f, -expr, expr, () => $"Abs({expr})");
  }

  public static Expression If (
      Expression expr, Expression thenExpr, Expression elseExpr, Func<string> stringify = null) {
    return Add(And(expr, thenExpr), NotAnd(expr, elseExpr), stringify ??
      (() => $"If({expr}, {thenExpr}, {elseExpr})"));
  }

  public static Expression LessThanOrEquals (Expression l, Expression r) {
    return Not(l > r, () => $"{l}<={r}");
  }

  public static Expression GreaterThanOrEquals (Expression l, Expression r) {
    return Not(l < r, () => $"{l}>={r}");
  }

  public static Expression Equals (Expression l, Expression r) {
    return Nor(l > r, l < r, () => $"Equals({l}, {r})");
  }

  public static Expression NotEquals (Expression l, Expression r) {
    return Or(l > r, l < r, () => $"NotEquals({l}, {r})");
  }

  public static Expression GreaterThan (Expression l, Expression r) {
    return BinaryStep(l - r, () => $"{l}>{r}");
  }

  public static Expression LessThan (Expression l, Expression r, Func<string> stringify = null) {
    return BinaryStep(r - l, stringify ?? (() => $"{l}<{r}"));
  }

  public static Expression BinaryStep (Expression expr, Func<string> stringify = null) {
    stringify = stringify ?? (() => $"BinaryStep({expr})");
    return expr is BinaryLinearOperation blop
      ? blop.WithActivation(Activation.BinaryStep, stringify)
      : new BinaryLinearOperation(expr, null, 0f, 1f, 0f, 0f,
          null, null, Activation.BinaryStep, stringify);
  }

  public static Expression Add (Expression l, Expression r, Func<string> stringify = null) {
    stringify = stringify ?? (() => $"{l}+{r}");
    return l is Terminal tl && r is Terminal tr && Model.building.simplifyExpressions
      ? (Expression)new Terminal(tl.inputCoefficients + tr.inputCoefficients, stringify)
      : new BinaryLinearOperation(l, r, 0f, 1f, 1f, 0f, null, null, Activation.Identity, stringify);
  }

  public static Expression Subtract (Expression l, Expression r) {
    Func<string> stringify = () => $"{l}-{r}";
    return l is Terminal tl && r is Terminal tr && Model.building.simplifyExpressions
      ? (Expression)new Terminal(tl.inputCoefficients - tr.inputCoefficients, stringify)
      : new BinaryLinearOperation(l, r, 0f, 1f, -1f, 0f,
          null, null, Activation.Identity, stringify);
  }

  public static Expression Multiply (Expression expr, float s) {
    Func<string> stringify = () => $"{expr}*{s}f";
    return expr is Terminal t && Model.building.simplifyExpressions
      ? (Expression)new Terminal(t.inputCoefficients * s, stringify)
      : new BinaryLinearOperation(
          expr, null, 0f, s, 0f, 0f, null, null, Activation.Identity, stringify);
  }

  public static Expression Xor (Expression l, Expression r) {
    return And(Or(l, r), Nand(l, r), () => $"{l}^{r}");
  }

  public static Expression And (Expression l, Expression r, Func<string> stringify = null) {
    return new BinaryLinearOperation(l, r, 1f, 0f, 0f, 0f, null, null, Activation.Identity,
      stringify ?? (() => $"{l}&{r}"));
  }

  public static Expression Or (Expression l, Expression r, Func<string> stringify = null) {
    return new BinaryLinearOperation(l, r, -1f, 1f, 1f, 0f, null, null, Activation.Identity,
      stringify ?? (() => $"{l}|{r}"));
  }

  public static Expression Nor (Expression l, Expression r, Func<string> stringify = null) {
    return new BinaryLinearOperation(l, r, 1f, -1f, -1f, 1f, null, null, Activation.Identity,
      stringify ?? (() => $"!({l}|{r})"));
  }

  public static Expression Nand (Expression l, Expression r) {
    return new BinaryLinearOperation(l, r, -1f, 0f, 0f, 1f, null, null, Activation.Identity,
      () => $"!({l}&{r})");
  }

  public static Expression NotAnd (Expression l, Expression r) {
    return new BinaryLinearOperation(l, r, -1f, 0f, 1f, 0f, null, null, Activation.Identity,
      () => $"!{l}&{r}");
  }

  public static Expression Multiply (Expression l, Expression r) {
    return new BinaryLinearOperation(l, r, 1f, 0f, 0f, 0f, null, null, Activation.Identity,
      () => $"{l}*{r}");
  }

  public static Expression Not (Expression expr, Func<string> stringify = null) {
    stringify = stringify ?? (() => $"!{expr}");
    return expr is Terminal t && Model.building.simplifyExpressions
      ? (Expression)new Terminal(Constant(1.0f).inputCoefficients - t.inputCoefficients, stringify)
      : new BinaryLinearOperation(expr, null, 0f, -1f, 0f, 1f,
          null, null, Activation.Identity, stringify);
  }

  public static Expression Identity (Expression expr) {
    Func<string> stringify = () => $"+{expr}";
    return expr is Terminal t && Model.building.simplifyExpressions
      ? (Expression)new Terminal(t.inputCoefficients, stringify)
      : new BinaryLinearOperation(expr, null, 0f, 1f, 0f, 0f,
          null, null, Activation.Identity, stringify);
  }

  public static Expression Negate (Expression expr) {
    Func<string> stringify = () => $"-{expr}";
    return expr is Terminal t && Model.building.simplifyExpressions
      ? (Expression)new Terminal(-t.inputCoefficients, stringify)
      : new BinaryLinearOperation(expr, null, 0f, -1f, 0f, 0f,
          null, null, Activation.Identity, stringify);
  }

  public static Terminal Constant (float value) {
    return Model.building.Constant(value);
  }

  public static Terminal Random (float min, float max, int source = 0) {
    return Model.building.Random(min, max, source);
  }

  public static Terminal Input (int input, Func<string> stringify = null) {
    return Model.building.Input(input, stringify);
  }

  public static Expression State (Expression expr) {
    return State(expr, 0, Model.building.states.Count);
  }

  public static Expression State (Expression expr, int start, int count) {
    var states = Enumerable.Range(start, count).Select<int, Expression>(State).ToList();
    return Index(states, 0, states.Count - 1, expr);
  }

  public static Expression Index (
      List<Expression> exprs, float first, float last, Expression index) {
    if (exprs.Count <= 1) return exprs[0];
    var leftCount = exprs.Count / 2;
    var scale = (last - first) / (exprs.Count - 1);
    return If(index < first + (leftCount - 0.5f) * scale,
      Index(exprs.GetRange(0, leftCount), first,
        first + (leftCount - 1) * scale, index),
      Index(exprs.GetRange(leftCount, exprs.Count - leftCount),
        first + leftCount * scale, last, index));
  }

  public static Expression IsIndex (Expression expr, int index, int count) {
    if (count <= 1) return true;
    if (index == 0) return expr <= 0.5f;
    if (index == count - 1) return expr > count - 1.5f;
    return expr > index - 0.5f & expr <= index + 0.5f;
  }

  public static Terminal State (int state) {
    return Model.building.State(state);
  }

  public static Terminal Fetch (int memory) {
    return Model.building.Fetch(memory);
  }

  public static Terminal Value (
      int input, float scale = 1f, float offset = 0f, Func<string> stringify = null) {
    return Model.building.Value(input, scale, offset, stringify);
  }

  public readonly Func<string> stringify;
  public readonly List<Reference> references = new List<Reference>();

  public abstract int depth { get; }

  public int savings => _savings ?? (int)(_savings = ComputeSavings());

  public int operationCount { get; protected set; } = 1;

  public Expression (Func<string> stringify) {
    this.stringify = stringify;
  }

  public abstract Expression Compact ();

  public void Deduplicate (
      Dictionary<Expression, Expression> uniqueInstances, Reference reference) {
    if (!uniqueInstances.TryGetValue(this, out var uniqueInstance)) {
      DeduplicateChildren(uniqueInstances, reference);
      uniqueInstances.Add(this, uniqueInstance = this);
    } else reference.set(uniqueInstance);
    uniqueInstance.references.Add(reference);
  }

  public void Collapse (Model model) {
    // process references list (already sorted by decreasing depth) directly
    for (var ii = 0; ii < references.Count; ii++) {
      var deepReference = references[ii];
      for (var jj = ii + 1; jj < references.Count; jj++) {
        var shallowReference = references[jj];
        if (shallowReference.SubtreeContains(deepReference)) {
          references.RemoveAt(jj--);
          Expression CreatePathExpression (int levels, ulong path) {
            if (levels == 1) {
              var coefficients = model.OutputCoefficients(deepReference.output);
              return (path & 1) == 0
                ? new BinaryLinearOperation(null, null, 0f, 1f, 0f, 0f, coefficients, null)
                : new BinaryLinearOperation(null, null, 0f, 0f, 1f, 0f, null, coefficients);
            }
            return (path & 1) == 0
              ? new BinaryLinearOperation(
                  CreatePathExpression(levels - 1, path >> 1), null, 0f, 1f, 0f, 0f, null, null)
              : new BinaryLinearOperation(
                  null, CreatePathExpression(levels - 1, path >> 1), 0f, 0f, 1f, 0f, null, null);
          }
          shallowReference.set(CreatePathExpression(
            deepReference.depth - shallowReference.depth,
            deepReference.path >> shallowReference.depth));
        }
      }
    }
  }

  public int CompareTo (Expression other) {
    return other.savings - savings;
  }

  public virtual void DeduplicateChildren (
    Dictionary<Expression, Expression> uniqueInstances, Reference reference) {}

  public override string ToString () {
    return stringify();
  }

  private int ComputeSavings () {
    // don't bother with terminals
    if (operationCount == 1) return 0;

    // sort references by decreasing depth
    references.Sort();

    // process copy
    var refs = new List<Reference>(references);
    var totalSavings = 0;
    for (var ii = 0; ii < refs.Count; ii++) {
      var deepReference = refs[ii];
      for (var jj = ii + 1; jj < refs.Count; jj++) {
        var shallowReference = refs[jj];
        if (shallowReference.SubtreeContains(deepReference)) {
          refs.RemoveAt(jj--);
          totalSavings += (operationCount - 1);
        }
      }
    }
    return totalSavings;
  }

  private int? _savings;
}

}
