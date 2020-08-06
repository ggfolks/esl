namespace GGFolks.Esl {

using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A Unity component containing a simulation model.
/// </summary>
[AddComponentMenu("GGFolks/Simulation/Simulation")]
public class Simulation : MonoBehaviour {

  [Tooltip("The current rules of the simulation.")]
  public Rules rules;

  [Tooltip("Output connections to float properties.")]
  public FloatEvent[] floatOutputs;

  [Tooltip("Output connections to bool properties.")]
  public BoolEvent[] boolOutputs;

  [Tooltip("Output connections to Vector2 properties.")]
  public Vector2Event[] vector2Outputs;

  [Tooltip("Output connections to Vector3 properties.")]
  public Vector3Event[] vector3Outputs;
}

[Serializable]
public class FloatEvent : UnityEvent<float> {}

[Serializable]
public class BoolEvent : UnityEvent<bool> {}

[Serializable]
public class Vector2Event : UnityEvent<Vector2> {}

[Serializable]
public class Vector3Event : UnityEvent<Vector3> {}

}
