using KnockingTool.Models;

namespace KnockingTool.Services;

public static class SequenceRunner
{
    public static async Task RunNodeAsync(
        KnockNode node,
        Action<string> log,
        CancellationToken cancellationToken)
    {
        if (node.Steps.Count == 0)
        {
            log($"[{node.Name}] هیچ مرحله‌ای تعریف نشده است.");
            return;
        }

        log($"=== شروع ناکینگ: {node.Name} ({node.DestinationIp}) ===");

        var orderedSteps = node.Steps.OrderBy(s => s.Order).ToList();
        for (var index = 0; index < orderedSteps.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var step = orderedSteps[index];
            log($"  مرحله {index + 1}/{orderedSteps.Count}: {step.Protocol} ...");

            var (success, message) = await PacketSender.SendAsync(step, node.DestinationIp);
            log(success ? $"    ✓ {message}" : $"    ✗ {message}");

            if (index < orderedSteps.Count - 1 && step.DelayMs > 0)
            {
                log($"    انتظار {step.DelayMs}ms ...");
                await Task.Delay(step.DelayMs, cancellationToken);
            }
        }

        log($"=== پایان ناکینگ: {node.Name} ===");
        log(string.Empty);
    }
}
