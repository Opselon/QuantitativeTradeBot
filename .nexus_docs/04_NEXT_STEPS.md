# Nexus Trading Engine - Next Steps

## Immediate Next Steps (Phase 1)
1. **Initialize the C# Solution & Projects**:
   - Generate directories: `src/` and `tests/`.
   - Setup project references and package configurations for the solution.
2. **Build the Domain Models in `Nexus.Core`**:
   - Write fully detailed and robust implementation of `Symbol`, `Tick` (readonly struct), `Bar`, `Order`, `Position`, and `Account` domain entities.
   - Design strategic interfaces (`IStrategy`, `IRiskManager`, `ITrailingManager`).
3. **Verify Implementation**:
   - Write unit tests in `Nexus.Tests.Unit` for newly created core domain objects.
   - Execute `dotnet build` and `dotnet test` to ensure robust foundations.

## Medium-Term Roadmap
- **Phase 2**: PostgreSQL DB, migrations, and binary-copy bulk insertion.
- **Phase 3**: MetaTrader 5 Bridge integration via high-speed IPC (gRPC or local domain sockets/named pipes).
- **Phase 4**: Strategy Engine construction with GoldScalperM1 and EmaCrossover alongside dynamic trailing stops.
- **Phase 5**: WPF client UI using MVVM Community Toolkit.
- **Phase 6**: Testcontainers PostgreSQL integration.
