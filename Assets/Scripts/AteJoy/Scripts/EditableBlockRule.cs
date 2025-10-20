using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Validation/EditableBlockRule")]
public class EditableBlockRule : ValidationRule
{
    [HideInInspector] public List<string> editableSnippets;

    public override bool Validate(string playerInput)
    {
        // Example logic: check if all editable snippets are present in the player's input
        foreach (string snippet in editableSnippets)
        {
            if (!playerInput.Contains(snippet))
                return false;
        }

        return true;
    }
}