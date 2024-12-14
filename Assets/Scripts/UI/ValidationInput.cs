using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class ValidationInput : MonoBehaviour
{
    public enum StatusType
    {
        Invalid,
        Loading,
        Valid,
    }

    public StatusType ValidStatus { get; protected set; }

    [SerializeField] protected TMP_InputField input;

    [SerializeField] protected Image validationOutline;
    [SerializeField] protected TMP_Text validationText;

    [SerializeField] protected Color validColor;
    [SerializeField] protected Color testingColor;
    [SerializeField] protected Color invalidColor;

    private void Start()
    {
        Validate();
    }

    public abstract void Validate();

    protected virtual void SetStatus(StatusType type, string message)
    {
        ValidStatus = type;
        Color validationColor = type == StatusType.Invalid ? invalidColor : type == StatusType.Loading ? testingColor : validColor;
        validationText.text = message;
        validationText.color = validationColor;
        validationOutline.color = validationColor;
    }
}