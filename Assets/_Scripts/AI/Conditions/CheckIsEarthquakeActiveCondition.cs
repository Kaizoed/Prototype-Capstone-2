using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace ShakySurvival.AI
{
    /// <summary>
    /// Behavior Graph Condition — returns true when the earthquake is active
    /// (after the reaction delay has elapsed).
    /// 
    /// Usage: Attach to a Branch node. Link "IsEarthquakeActive" to the
    /// matching Blackboard boolean.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Is Earthquake Active",
        category: "Earthquake",
        story: "is earthquake active [IsEarthquakeActive]",
        id: "ea1c0007000000000000000000000001")]
    public partial class CheckIsEarthquakeActiveCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<bool> IsEarthquakeActive;

        public override bool IsTrue()
        {
            return IsEarthquakeActive != null && IsEarthquakeActive.Value;
        }
    }
}
