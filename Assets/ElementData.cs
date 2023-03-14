using UnityEngine;

[CreateAssetMenu(menuName = "Element Data")]
public class ElementData : ScriptableObject
{
    [field: SerializeField] public string displayName { get; private set; }
    [field: SerializeField] public Color color { get; private set; } = Color.white;
    [field: SerializeField] public bool useGravity { get; private set; }
    [field: SerializeField] public int stackingHeight { get; private set; } = 1;
    [field: SerializeField] public int density { get; private set; } = 1000;
    [field: SerializeField] public bool corrodable { get; private set; } //can this be corroded
    [field: SerializeField] public ElementData corrosionResult { get; private set; } //what is produced when this is corroded
    [field: SerializeField] public float corrosionChance { get; private set; } //chance of corroding something else
    [field: SerializeField] public float flammability { get; private set; }
    [field: SerializeField] public ElementData burnResult { get; private set; } //what is produced when this is burned
    [field: SerializeField] public MatterState state { get; private set;} 
}
public enum MatterState
{
    Solid,
    Liquid,
    Gas
}
