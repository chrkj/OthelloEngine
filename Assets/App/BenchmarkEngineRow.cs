using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Othello.AI;

namespace Othello.App
{
    /// <summary>
    /// One editable engine row in the benchmark pool. Lives on a prefab the controller instantiates.
    /// Reads its widgets into an <see cref="EngineConfig"/> and shows only the controls that apply to
    /// the selected engine kind.
    /// </summary>
    public class BenchmarkEngineRow : MonoBehaviour
    {
        [Tooltip("Options must be, in order: Random, Minimax, MCTS")]
        [SerializeField] private TMP_Dropdown m_KindDropdown;
        [Tooltip("Options must be, in order: Sequential, RootParallel, TreeParallel, GpuParallel")]
        [SerializeField] private TMP_Dropdown m_VariantDropdown;
        [SerializeField] private TMP_InputField m_DepthInput;
        [SerializeField] private TMP_InputField m_IterationsInput;
        [SerializeField] private TMP_InputField m_TimeInput;
        [SerializeField] private Toggle m_MoveOrderingToggle;
        [SerializeField] private Toggle m_IterativeDeepeningToggle;
        [SerializeField] private Toggle m_ZobristToggle;
        [SerializeField] private Button m_RemoveButton;

        [Header("Shown/hidden per engine kind")]
        [SerializeField] private GameObject m_MinimaxGroup;
        [SerializeField] private GameObject m_MctsGroup;

        private Action<BenchmarkEngineRow> m_OnRemove;

        public void Init(Action<BenchmarkEngineRow> onRemove)
        {
            m_OnRemove = onRemove;
            if (m_RemoveButton != null)
                m_RemoveButton.onClick.AddListener(() => m_OnRemove?.Invoke(this));
            if (m_KindDropdown != null)
                m_KindDropdown.onValueChanged.AddListener(_ => UpdateVisibility());
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            var kind = m_KindDropdown != null ? (EngineKind)m_KindDropdown.value : EngineKind.Mcts;
            if (m_MinimaxGroup != null) m_MinimaxGroup.SetActive(kind == EngineKind.Minimax);
            if (m_MctsGroup != null) m_MctsGroup.SetActive(kind == EngineKind.Mcts);
        }

        public EngineConfig ToConfig()
        {
            return new EngineConfig
            {
                Kind = m_KindDropdown != null ? (EngineKind)m_KindDropdown.value : EngineKind.Mcts,
                MctsVariant = m_VariantDropdown != null ? (MctsType)m_VariantDropdown.value : MctsType.Sequential,
                Depth = ParseInt(m_DepthInput, 6),
                Iterations = ParseInt(m_IterationsInput, 5000),
                TimeLimitMs = ParseInt(m_TimeInput, 1000),
                MoveOrdering = m_MoveOrderingToggle != null && m_MoveOrderingToggle.isOn,
                IterativeDeepening = m_IterativeDeepeningToggle != null && m_IterativeDeepeningToggle.isOn,
                ZobristHashing = m_ZobristToggle != null && m_ZobristToggle.isOn
            };
        }

        // Reads the number out of the field, ignoring any unit text (e.g. "500 ms" -> 500).
        private static int ParseInt(TMP_InputField field, int fallback)
        {
            if (field == null)
                return fallback;
            var hasDigit = false;
            foreach (var c in field.text)
                if (char.IsDigit(c)) { hasDigit = true; break; }
            return hasDigit ? UnitInputField.ExtractDigits(field.text) : fallback;
        }
    }
}
