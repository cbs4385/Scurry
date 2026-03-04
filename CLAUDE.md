# Scurry - Project Rules

## Debugging Policy
- **NEVER guess at the solution to an issue.** Always use logging to determine what happened and resolve based on evidence.
- All scripts must include extensive logging (Debug.Log) covering:
  - Method entry/exit with parameter values
  - State transitions and phase changes
  - All decisions and branch paths taken
  - Object references (null checks, instance IDs)
  - Card placements, movements, combat results
  - Resource collection, HP changes
  - Error conditions with full context
- Logs must be detailed enough that the **entire game state can be reconstructed** from log entries alone.
- Log format: `[ClassName] MethodName: description (key=value, key=value)`
- Use consistent prefixes per system for easy filtering.
