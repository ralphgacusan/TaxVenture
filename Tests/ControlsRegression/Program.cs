using System;

int checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new Exception(message);
}

Check(JoystickInputMath.RemapMagnitude(0f, 0.12f, 1f) == 0f, "Neutral stick must be zero.");
Check(JoystickInputMath.RemapMagnitude(0.12f, 0.12f, 1f) == 0f, "Dead-zone boundary must not drift.");
Check(JoystickInputMath.RemapMagnitude(1f, 0.12f, 1f) == 1f, "Full deflection must reach full speed.");
float smallInput = JoystickInputMath.RemapMagnitude(0.3f, 0.12f, 1f);
Check(smallInput > 0f && smallInput < 0.3f, "Small movement must remain analog, not full speed.");
Check(JoystickInputMath.RemapMagnitude(0.8f, 0.12f, 0.5f) <
      JoystickInputMath.RemapMagnitude(0.8f, 0.12f, 1f), "Sensitivity must affect response.");

foreach (float deadZone in new[] { 0f, 0.1f, 0.12f, 0.5f, 0.95f })
foreach (float sensitivity in new[] { 0f, 0.1f, 1f, 2f })
{
    float previous = 0f;
    for (int step = -10; step <= 110; step++)
    {
        float input = step / 100f;
        float result = JoystickInputMath.RemapMagnitude(input, deadZone, sensitivity);
        Check(float.IsFinite(result) && result >= 0f && result <= 1f, "Response must be finite and bounded.");
        Check(result >= previous, "Increasing deflection must never reduce speed.");
        if (input <= deadZone) Check(result == 0f, "Inside the dead zone must be exactly zero.");
        previous = result;
    }
}
Check(JoystickInputMath.RemapMagnitude(1f, 2f, 5f) == 1f, "Out-of-range settings must stay bounded.");
Console.WriteLine($"PASS: {checks} controls response assertions (production JoystickInputMath). Unity pointer/UI/camera tests remain manual.");
