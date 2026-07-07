using System.Text;
using TMPro;
using UnityEngine;

namespace Othello.App
{
    /// <summary>
    /// Shows "&lt;number&gt; &lt;unit&gt;" in a TMP_InputField (e.g. "14 depth", "500 ms") while keeping the
    /// edit numeric-only: focusing the field shows just the number, typing accepts only digits, and
    /// leaving the field re-appends the unit. Readers can ignore the unit by parsing only the digits
    /// (see <see cref="ExtractDigits"/>).
    /// </summary>
    [RequireComponent(typeof(TMP_InputField))]
    public class UnitInputField : MonoBehaviour
    {
        [Tooltip("Suffix shown after the number, e.g. \"ms\" or \"depth\".")]
        [SerializeField] private string m_Unit = "";

        private TMP_InputField m_Field;
        private int m_Value;

        public int Value => m_Value;

        private void Awake()
        {
            m_Field = GetComponent<TMP_InputField>();
            m_Value = ExtractDigits(m_Field.text);
            // Standard content type so the display can hold the unit text; the validator keeps typing numeric.
            m_Field.contentType = TMP_InputField.ContentType.Standard;
            m_Field.onValidateInput += DigitsOnly;
            m_Field.onSelect.AddListener(OnSelect);
            m_Field.onDeselect.AddListener(OnDeselect);
            Format();
        }

        // While focused, show only the number so it edits cleanly.
        private void OnSelect(string _) => m_Field.SetTextWithoutNotify(m_Value.ToString());

        // When focus leaves, commit the number and put the unit back for display.
        private void OnDeselect(string text)
        {
            m_Value = ExtractDigits(text);
            Format();
        }

        private void Format()
            => m_Field.SetTextWithoutNotify(string.IsNullOrEmpty(m_Unit) ? m_Value.ToString() : $"{m_Value} {m_Unit}");

        private static char DigitsOnly(string text, int charIndex, char addedChar)
            => char.IsDigit(addedChar) ? addedChar : '\0';

        /// <summary>Parses the first run of digits out of a string, ignoring any unit text. Returns 0 if none.</summary>
        public static int ExtractDigits(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
            var sb = new StringBuilder(text.Length);
            foreach (var c in text)
                if (char.IsDigit(c))
                    sb.Append(c);
            return int.TryParse(sb.ToString(), out var value) ? value : 0;
        }
    }
}
