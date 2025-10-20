using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Puzzle/BrokenCode")]
public class BrokenCode : ScriptableObject
{
    public Sprite codeSprite;

    [Header("Hint Images (0–3)")]
    public List<Sprite> hintImages;

    [TextArea(5, 10)]
    public string defaultCodeTemplate;

    [Header("Validation Rules")]
    public List<ValidationRule> validationRules;

    [TextArea(5, 10)]
    public string issueDescription;

    [TextArea(5, 10)]
    public string playersAnswer;

    [TextArea(5, 10)]
    public string pointsToRemember;
}
