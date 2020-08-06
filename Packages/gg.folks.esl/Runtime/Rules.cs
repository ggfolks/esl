namespace GGFolks.Esl {

using System.IO;
using UnityEngine;

/// <summary>
/// A stored snapshot of simulation rules.
/// </summary>
[CreateAssetMenu(fileName = "SimulationRules", menuName = "ScriptableObjects/SimulationRules")]
public class Rules : ScriptableObject, ISerializationCallbackReceiver {

  [Tooltip("The current state of the simulation.")]
  public State state;

  [Tooltip("The storage state of the simulation.")]
  public Storage storage;

  [Tooltip("The serialized rule contents.")]
  public byte[] serialized;

  /// <summary>The model containing the rules.</summary>
  public Model model;

  // from ISerializationCallbackReceiver
  public void OnBeforeSerialize () {
    using (var stream = new MemoryStream())
    using (var writer = new BinaryWriter(stream)) {
      model?.Write(writer);
      serialized = stream.ToArray();
    }
  }

  // from ISerializationCallbackReceiver
  public void OnAfterDeserialize () {
    using (var reader = new BinaryReader(new MemoryStream(serialized))) {
      model = new Model(reader);
    }
  }
}

}
