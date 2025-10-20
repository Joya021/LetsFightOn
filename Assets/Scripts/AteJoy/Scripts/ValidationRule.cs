using System.Collections.Generic;
using UnityEngine;

public abstract class ValidationRule : ScriptableObject
{
    [System.NonSerialized]
    public string errorMessage;
    public abstract bool Validate(string code);
}
