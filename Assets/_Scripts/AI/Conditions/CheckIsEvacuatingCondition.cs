using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace ShakySurvival.AI
{
    /// <summary>
    /// Behavior Graph Condition — returns true when the NPC is in
    /// the evacuation phase (post-earthquake line-up / safe-zone walk).
    ///
    /// Usage in the Behavior Graph editor:
    ///   Attach this condition (NEGATED) to the Idle Roam branch so the
    ///   NPC does NOT roam while the RoomEvacuationCoordinator is in control.
    ///   Link the "IsEvacuating" field to the Blackboard's IsEvacuating bool variable.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Is Evacuating",
        category: "Earthquake",
        story: "[Self] is evacuating [IsEvacuating]",
        id: "ea1c0008000000000000000000000001")]
    public partial class CheckIsEvacuatingCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<bool> IsEvacuating;

        public override bool IsTrue()
        {
            return IsEvacuating != null && IsEvacuating.Value;
        }
    }
}
