using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Base class for UI elements that extend basic input field functionality to include
/// input-checking and validation, both using text and color feedback
/// </summary>
public abstract class ValidationInput : MonoBehaviour
{
    /// <summary>
    /// Contains possible states for this input field's validation
    /// </summary>
    public enum StatusType
    {
        Invalid,    // Input does not pass validation
        Loading,    // Input is currently being checked
        Valid,      // Input passes validation
    }

    /// <summary>
    /// Current state of this input field's validation
    /// </summary>
    public StatusType ValidStatus { get; protected set; }

    // References
    [SerializeField] protected TMP_InputField input;
    [SerializeField] protected Image validationOutline;
    [SerializeField] protected TMP_Text validationText;

    // Outline colors corresponding to each StatusType
    [SerializeField] protected Color validColor;
    [SerializeField] protected Color testingColor;
    [SerializeField] protected Color invalidColor;

    private void Start()
    {
        Validate();
    }

    /// <summary>
    /// Called whenever the input text changes or initializes, intended to check the input
    /// and call SetStatus with any validation updates
    /// </summary>
    public abstract void Validate();

    /// <summary>
    /// Sets the validation status of this input field, updating colors and text feedback
    /// </summary>
    /// <param name="type">New validation status</param>
    /// <param name="message">Feedback message to display</param>
    protected virtual void SetStatus(StatusType type, string message)
    {
        ValidStatus = type;
        Color validationColor = type == StatusType.Invalid ? invalidColor : type == StatusType.Loading ? testingColor : validColor;
        validationText.text = message;
        validationText.color = validationColor;
        validationOutline.color = validationColor;
    }
}