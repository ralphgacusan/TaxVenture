using System;

/// <summary>Radial input response shared by move and look sticks.</summary>
public static class JoystickInputMath
{
    public static float RemapMagnitude(float magnitude, float deadZone, float sensitivity)
    {
        deadZone = Math.Clamp(deadZone, 0f, 0.95f);
        magnitude = Math.Clamp(magnitude, 0f, 1f);
        if (magnitude <= deadZone) return 0f;
        return Math.Clamp((magnitude - deadZone) / (1f - deadZone) * Math.Clamp(sensitivity, 0f, 2f), 0f, 1f);
    }
}
