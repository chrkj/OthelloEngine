using System;
using System.Globalization;
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
        // Defaults applied to each newly added engine row.
        private const int DEFAULT_DEPTH = 14;
        private const int DEFAULT_ITERATIONS = 1000000000;
        private const int DEFAULT_TIME_MS = 1000;
        private const bool DEFAULT_HEURISTIC = true;
        private const float DEFAULT_EPSILON = 0.2f;

        [Tooltip("Options must be, in order: Random, Minimax, MCTS")]
        [SerializeField] private TMP_Dropdown m_KindDropdown;
        [Tooltip("Options must be, in order: Sequential, Root, Tree, Gpu")]
        [SerializeField] private TMP_Dropdown m_VariantDropdown;
        [SerializeField] private TMP_InputField m_DepthInput;
        [SerializeField] private TMP_InputField m_IterationsInput;
        [SerializeField] private TMP_InputField m_TimeInput;
        [SerializeField] private Toggle m_MoveOrderingToggle;
        [SerializeField] private Toggle m_IterativeDeepeningToggle;
        [SerializeField] private Toggle m_ZobristToggle;
        [Tooltip("MCTS: epsilon-greedy positional rollouts instead of uniform-random.")]
        [SerializeField] private Toggle m_HeuristicRolloutToggle;
        [Tooltip("MCTS: rollout epsilon, 0-1 (chance of a random move when heuristic is on).")]
        [SerializeField] private TMP_InputField m_RolloutEpsilonInput;
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
            ApplyDefaults();
            UpdateVisibility();
        }

        // Pre-fills a freshly added row with the standard benchmark defaults.
        private void ApplyDefaults()
        {
            SetNumber(m_DepthInput, DEFAULT_DEPTH);
            SetNumber(m_TimeInput, DEFAULT_TIME_MS);
            SetNumber(m_IterationsInput, DEFAULT_ITERATIONS);
            if (m_HeuristicRolloutToggle != null)
                m_HeuristicRolloutToggle.SetIsOnWithoutNotify(DEFAULT_HEURISTIC);
            if (m_RolloutEpsilonInput != null)
                m_RolloutEpsilonInput.SetTextWithoutNotify(DEFAULT_EPSILON.ToString("0.##", CultureInfo.InvariantCulture));
        }

        // Sets a number field, routing through UnitInputField when present so its unit display stays correct.
        private static void SetNumber(TMP_InputField field, int value)
        {
            if (field == null)
                return;
            var unit = field.GetComponent<UnitInputField>();
            if (unit != null)
                unit.SetValue(value);
            else
                field.SetTextWithoutNotify(value.ToString());
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
                Depth = ParseInt(m_DepthInput, DEFAULT_DEPTH),
                Iterations = ParseInt(m_IterationsInput, DEFAULT_ITERATIONS),
                TimeLimitMs = ParseInt(m_TimeInput, DEFAULT_TIME_MS),
                MoveOrdering = m_MoveOrderingToggle != null && m_MoveOrderingToggle.isOn,
                IterativeDeepening = m_IterativeDeepeningToggle != null && m_IterativeDeepeningToggle.isOn,
                ZobristHashing = m_ZobristToggle != null && m_ZobristToggle.isOn,
                UseHeuristicRollout = m_HeuristicRolloutToggle != null && m_HeuristicRolloutToggle.isOn,
                RolloutEpsilon = ParseFloat(m_RolloutEpsilonInput, DEFAULT_EPSILON)
            };
        }

        private static float ParseFloat(TMP_InputField field, float fallback)
        {
            if (field == null)
                return fallback;
            // Normalize a comma decimal separator so "0,2" and "0.2" both parse regardless of locale.
            var text = field.text.Replace(',', '.');
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;
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
