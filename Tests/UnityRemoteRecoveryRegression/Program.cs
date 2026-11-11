using System;

int checks = 0;
void Check(bool ok, string label)
{
    checks++;
    if (!ok) throw new Exception(label);
}
const string usb = "List of devices attached\nphoneA device usb:0-1 product:test model:Phone transport_id:5\n";
Check(UnityRemoteForwarding.UsbDevice(usb, out _) == "phoneA", "Authorized USB phone selected");
foreach (string disconnected in new[] { "", "List of devices attached\n", "phoneA unauthorized usb:0-1", "phoneA offline usb:0-1", "emulator-5554 device product:test", "192.0.2.1:5555 device product:test", "bad;command device usb:0-1" })
    Check(UnityRemoteForwarding.UsbDevice(disconnected, out string reason) == null && !string.IsNullOrEmpty(reason), "No auto-forward for absent/unauthorized/offline/non-USB/unsafe device");
Check(UnityRemoteForwarding.UsbDevice(usb + "phoneB device usb:0-2\n", out _) == null, "Multiple USB phones require a choice");
Check(UnityRemoteForwarding.UsbDevice(usb + "emulator-5554 device product:test\n", out _) == null, "Phone plus emulator is not guessed");
Check(UnityRemoteForwarding.UsbDevice(usb + "phoneB unauthorized usb:0-2\n", out _) == null, "Unauthorized second device does not get bypassed");
Check(UnityRemoteForwarding.Mapping("", "phoneA") == 0, "Original failure: no forward after restart must request repair");
Check(UnityRemoteForwarding.Mapping("phoneA tcp:7201 tcp:7201\n", "phoneA") == 1, "Correct forward needs no mutation");
Check(UnityRemoteForwarding.Mapping("phoneB tcp:7201 tcp:7201\n", "phoneA") == -1, "Another device's mapping is protected");
Check(UnityRemoteForwarding.Mapping("phoneA tcp:7201 tcp:9999\n", "phoneA") == -1, "Another service on Remote's port is protected");
Check(UnityRemoteForwarding.Mapping("phoneA tcp:8080 tcp:8080\r\n", "phoneA") == 0, "Unrelated forwarding is retained");
// Repeat the real missing -> restored -> disconnected -> reconnected condition.
for (int cycle = 0; cycle < 100; cycle++)
{
    Check(UnityRemoteForwarding.Mapping("", "phoneA") == 0, "Restart/replug always detects lost forward");
    Check(UnityRemoteForwarding.Mapping("phoneA tcp:7201 tcp:7201\n", "phoneA") == 1, "Successful repair settles idempotently");
    Check(UnityRemoteForwarding.UsbDevice("List of devices attached\n", out _) == null, "Disconnect pauses repair");
    Check(UnityRemoteForwarding.UsbDevice(usb, out _) == "phoneA", "Reconnect can be selected afresh");
}
Console.WriteLine($"PASS: {checks} Unity Remote recovery assertions (actual managed ADB-output decisions; not a video/touch test).");
