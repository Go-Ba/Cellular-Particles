using UnityEngine;

[CreateAssetMenu(menuName = "Element Data")]
public class ElementData : ScriptableObject
{
    [field: SerializeField] public string displayName { get; private set; }
    [field: SerializeField] public Color color { get; private set; } = Color.white;
    [field: SerializeField] public bool useGravity { get; private set; }
    [field: SerializeField] public int stackingHeight { get; private set; } = 1;
    [field: SerializeField] public int density { get; private set; } = 1000;
    [field: SerializeField] public bool corrodable { get; private set; }
    [field: SerializeField] public float corrosionChance { get; private set; }
    [field: SerializeField] public ElementData corrosionResult { get; private set; }
    [field: SerializeField] public MatterState state { get; private set;} 
}
public enum MatterState
{
    Solid,
    Liquid,
    Gas
}
