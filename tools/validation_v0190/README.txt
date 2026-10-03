Isolated C# regression harness

Program.cs contains 56 methods extracted from the v0.19.0 production source.
Minimal Player, ConfigEntry, Mathf, Rect and casting stubs replace engine calls.
The harness passed 71 assertions using .NET SDK 8.0.425.

To rerun with a .NET 8 SDK: dotnet run --project checks.csproj
This does not compile the full mod or simulate combat/Unity rendering.

Checks cover: shared Class nodes/Tiers, advancement, locks, point spending,
manual/automatic tiers, foreign branch rejection, permanent slots, saved bar
isolation, casting/cooldown routing, stale pending rejection, demotion and colors.
