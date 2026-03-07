using System;
using System.Collections.Generic;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using ShakySurvival.Cover;
using Action = Unity.Behavior.Action;

namespace ShakySurvival.AI
{
    /// <summary>
    /// Behavior Graph Action — finds the nearest REACHABLE table tagged "HideableTable"
    /// that is NOT on the Blackboard's blacklist.
    /// 
    /// Validates NavMesh reachability via <see cref="NavMesh.CalculatePath"/>
    /// so tables on different floors or behind missing NavMesh are skipped.
    /// 
    /// On success, writes the table's child "CoverPoint" Transform
    /// to the <see cref="TargetCoverPoint"/> Blackboard variable.
    /// 
    /// Editor Setup:
    ///   1. Tag every valid table with "HideableTable".
    ///   2. Under each table, add a child (any depth) whose name
    ///      contains "coverpoint" (case-insensitive) positioned
    ///      where the NPC should crouch beneath it.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Find Nearest Safe Table",
        description: "Searches for the nearest reachable HideableTable not on the blacklist and outputs its CoverPoint transform.",
        story: "[Agent] finds the nearest safe table, ignoring [BlacklistedTables]",
        category: "Action/Earthquake",
        id: "ea1c0003000000000000000000000001")]
    public partial class FindNearestSafeTableAction : Action
    {
        // ── Blackboard Variables ─────────────────────────────────
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<List<GameObject>> BlacklistedTables;

        /// <summary>OUTPUT — the transform beneath the chosen table.</summary>
        [SerializeReference] public BlackboardVariable<Transform> TargetCoverPoint;

        /// <summary>OUTPUT — the table GameObject itself (used for blacklisting later).</summary>
        [SerializeReference] public BlackboardVariable<GameObject> TargetTable;

        // Reusable path object to avoid allocations every frame.
        private NavMeshPath m_TempPath;

        // ─────────────────────────────────────────────────────────
        protected override Status OnStart()
        {
            if (Agent == null || Agent.Value == null)
            {
                LogFailure("Agent reference is null.");
                return Status.Failure;
            }

            // We need the NavMeshAgent to know where the NPC is on the NavMesh.
            NavMeshAgent navAgent = Agent.Value.GetComponentInChildren<NavMeshAgent>();
            if (navAgent == null || !navAgent.isOnNavMesh)
            {
                LogFailure("NavMeshAgent missing or not on NavMesh.");
                return Status.Failure;
            }

            // 1. Find all candidate tables in the scene.
            GameObject[] allTables = GameObject.FindGameObjectsWithTag("HideableTable");
            if (allTables.Length == 0)
            {
                LogFailure("No GameObjects with tag 'HideableTable' found in the scene.");
                return Status.Failure;
            }

            // 2. Grab the current blacklist (may be null or empty).
            List<GameObject> blacklist = BlacklistedTables?.Value;

            // 3. Allocate the reusable path object once.
            if (m_TempPath == null)
                m_TempPath = new NavMeshPath();

            // 4. Filter and find the nearest REACHABLE table.
            Vector3 agentPos = navAgent.transform.position;
            GameObject bestTable = null;
            Transform  bestCover = null;
            float      bestDist  = float.MaxValue;

            foreach (GameObject table in allTables)
            {
                if (table == null) continue;

                // Skip blacklisted tables.
                if (blacklist != null && blacklist.Contains(table))
                    continue;

                // Skip tables that are already occupied by another NPC or player.
                CoverSpot coverSpot = table.GetComponentInChildren<CoverSpot>();
                if (coverSpot != null && coverSpot.IsOccupied && coverSpot.Occupant != Agent.Value)
                {
                    Debug.Log($"[FindNearestSafeTable] '{table.name}' is occupied by {coverSpot.Occupant.name}. Skipping.");
                    continue;
                }

                // Find the CoverPoint child (recursive, case-insensitive).
                Transform cover = FindChildRecursive(table.transform, "coverpoint");
                if (cover == null)
                    continue;

                // ── NavMesh reachability check ───────────────────
                // Sample the cover position onto the NavMesh surface first.
                if (!NavMesh.SamplePosition(cover.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    Debug.Log($"[FindNearestSafeTable] '{table.name}' CoverPoint is not near a NavMesh surface. Skipping.");
                    continue;
                }

                // Calculate a path from the agent to the cover point.
                navAgent.CalculatePath(hit.position, m_TempPath);

                if (m_TempPath.status == NavMeshPathStatus.PathInvalid)
                {
                    Debug.Log($"[FindNearestSafeTable] '{table.name}' path is invalid. Skipping.");
                    continue;
                }

                // Accept PathPartial if it gets close enough — the CoverPoint is often
                // inside the table's NavMeshObstacle carve, so a full path isn't possible.
                // The scripted crawl handles the last stretch.
                if (m_TempPath.status == NavMeshPathStatus.PathPartial)
                {
                    Vector3[] corners = m_TempPath.corners;
                    if (corners.Length == 0) continue;

                    float endGap = Vector3.Distance(corners[corners.Length - 1], hit.position);
                    if (endGap > 2f)
                    {
                        Debug.Log($"[FindNearestSafeTable] '{table.name}' partial path too far ({endGap:F1}m). Skipping.");
                        continue;
                    }
                }

                // Use path length (walking distance) not straight-line distance.
                float dist = CalculatePathLength(m_TempPath);
                if (dist < bestDist)
                {
                    bestDist  = dist;
                    bestTable = table;
                    bestCover = cover;
                }
            }

            if (bestCover == null)
            {
                LogFailure("No reachable HideableTable found.");
                return Status.Failure;
            }

            Debug.Log($"[FindNearestSafeTable] Winner: '{bestTable.name}' — path distance {bestDist:F1}m.");

            // 5. Reserve the table.
            CoverSpot bestSpot = bestTable.GetComponentInChildren<CoverSpot>();
            if (bestSpot != null)
                bestSpot.TryOccupy(Agent.Value);

            // 6. Write the result to the Blackboard.
            if (TargetCoverPoint != null)
                TargetCoverPoint.Value = bestCover;
            else
                Debug.LogError("[FindNearestSafeTable] TargetCoverPoint BlackboardVariable is not linked!");

            if (TargetTable != null)
                TargetTable.Value = bestTable;
            else
                Debug.LogError("[FindNearestSafeTable] TargetTable BlackboardVariable is not linked!");

            return Status.Success;
        }

        // ── Helpers ──────────────────────────────────────────────

        /// <summary>
        /// Recursively searches all descendants for a child whose name
        /// contains <paramref name="nameLower"/> (case-insensitive).
        /// </summary>
        private static Transform FindChildRecursive(Transform parent, string nameLower)
        {
            foreach (Transform child in parent)
            {
                if (child.name.IndexOf(nameLower, StringComparison.OrdinalIgnoreCase) >= 0)
                    return child;

                Transform found = FindChildRecursive(child, nameLower);
                if (found != null)
                    return found;
            }
            return null;
        }

        /// <summary>
        /// Calculates the total walking distance along a NavMeshPath.
        /// </summary>
        private static float CalculatePathLength(NavMeshPath path)
        {
            Vector3[] corners = path.corners;
            float length = 0f;
            for (int i = 1; i < corners.Length; i++)
            {
                length += Vector3.Distance(corners[i - 1], corners[i]);
            }
            return length;
        }
    }
}
