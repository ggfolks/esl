namespace GGFolks.Esl {

using System.IO;
using UnityEngine;

/// <summary>
/// A stored snapshot of simulation memory.
/// </summary>
[CreateAssetMenu(fileName = "SimulationStorage", menuName = "ScriptableObjects/SimulationStorage")]
public class Storage : ScriptableObject, ISerializationCallbackReceiver {

  /// <summary>The stored memory.</summary>
  public Memory memory;

  [Tooltip("The serialized storage contents.")]
  public byte[] serialized;

  // from ISerializationCallbackReceiver
  public void OnBeforeSerialize () {
    using (var stream = new MemoryStream())
    using (var writer = new BinaryWriter(stream)) {
      memory?.Write(writer);
      serialized = stream.ToArray();
    }
  }

  // from ISerializationCallbackReceiver
  public void OnAfterDeserialize () {
    using (var reader = new BinaryReader(new MemoryStream(serialized))) {
      memory = new Memory(reader);
    }
  }
}

}
