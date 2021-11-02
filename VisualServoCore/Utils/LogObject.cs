using System;

namespace VisualServoCore
{
    public record LogObject<T>(
        DateTimeOffset Time,
        T Steer,
        T Speed
    );
}
