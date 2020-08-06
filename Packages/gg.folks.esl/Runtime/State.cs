namespace GGFolks.Esl {

using UnityEngine;

/// <summary>
/// A stored snapshot of simulation state.
/// </summary>
[CreateAssetMenu(fileName = "SimulationState", menuName = "ScriptableObjects/SimulationState")]
public class State : ScriptableObject {

  [Tooltip("The value of each state element.")]
  public double[] states;
}

}
