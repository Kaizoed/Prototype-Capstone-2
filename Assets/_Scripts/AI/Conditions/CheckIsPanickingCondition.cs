using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace ShakySurvival.AI
{
    /// <summary>
    /// Behavior Graph Condition — returns true when the NPC is panicking.
    /// 
    /// Usage in the Behavior Graph editor:
    ///   Attach this condition to a Conditional / Switch node.
    ///   Link the "IsPanicking" field to the Blackboard's IsPanicking bool variable.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Is Panicking",
        category: "Earthquake",
        story: "[Self] is panicking [IsPanicking]",
        id: "ea1c0001000000000000000000000001")]
    public partial class CheckIsPanickingCondition : Condition
    {
        // Linked to the Blackboard's "IsPanicking" variable.
        [SerializeReference] public BlackboardVariable<bool> IsPanicking;

        public override bool IsTrue()
        {
            return IsPanicking != null && IsPanicking.Value;
        }
    }
}
