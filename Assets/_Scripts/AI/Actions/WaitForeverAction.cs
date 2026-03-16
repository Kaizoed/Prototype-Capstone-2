using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace ShakySurvival.AI
{
    /// <summary>
    /// Behavior Graph Action — does nothing forever.
    /// Returns Running indefinitely, blocking this branch until
    /// the parent node switches away (e.g. a condition changes).
    ///
    /// Use under the IsEvacuating = True branch so the behavior tree
    /// stays idle while the RoomEvacuationCoordinator handles movement.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Wait Forever",
        description: "Does nothing and returns Running forever. Useful for gating a branch.",
        story: "[Agent] waits indefinitely",
        category: "Action/Utility",
        id: "ea1c0011000000000000000000000001")]
    public partial class WaitForeverAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        protected override Status OnStart() => Status.Running;
        protected override Status OnUpdate() => Status.Running;
    }
}
