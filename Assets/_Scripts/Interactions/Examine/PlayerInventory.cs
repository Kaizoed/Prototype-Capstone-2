using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShakySurvival.Interactions.Examine
{
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        // ── Events ──────────────────────────────────────────────
        public event Action<ExamineData> OnItemAdded;
        public event Action<ExamineData> OnItemRemoved;

        // ── Storage ─────────────────────────────────────────────
        [Header("Collected Items")]
        [Tooltip("Read-only at runtime – items the player has taken.")]
        [SerializeField] private List<ExamineData> collectedItems = new List<ExamineData>();

        public IReadOnlyList<ExamineData> CollectedItems => collectedItems;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[PlayerInventory] Duplicate instance destroyed.", this);
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ──────────────────────────────────────────

        public void AddItem(ExamineData itemData)
        {
            if (itemData == null)
            {
                Debug.LogWarning("[PlayerInventory] Tried to add a null item.");
                return;
            }

            collectedItems.Add(itemData);
            Debug.Log($"[PlayerInventory] Added: {itemData.objectName}");

            OnItemAdded?.Invoke(itemData);
        }

        public bool HasItem(ExamineData itemData)
        {
            return itemData != null && collectedItems.Contains(itemData);
        }

        public bool RemoveItem(ExamineData itemData)
        {
            if (itemData == null) return false;

            if (collectedItems.Remove(itemData))
            {
                Debug.Log($"[PlayerInventory] Removed: {itemData.objectName}");
                OnItemRemoved?.Invoke(itemData);
                return true;
            }

            return false;
        }
    }
}
