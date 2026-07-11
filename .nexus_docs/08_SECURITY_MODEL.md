# Security and Validation Model

## 1. Secret and Configuration Handling
To maintain strict security boundaries, no secrets are ever stored inside the source repository:
- All database, bridge, and telemetry settings are configured via environment variables, `.NET User Secrets`, or standard configuration providers.
- **Connection String Masking**: Logs and diagnostic reports always pass through a masking routine (`SecurityConfiguration.MaskConnectionString`) to hide usernames, passwords, or authentication keys before rendering.

## 2. Validation Boundaries
The platform enforces validation checks at all entry points:
- **`InputValidator`**: A high-performance, stateless validator that rejects malformed command payloads early.
- **Rules**:
  - Symbol names must match strict alphanumeric patterns (regex `^[A-Z0-9#\.]{3,10}$`).
  - Order sizes (Volume) must be strictly positive and bounded (e.g. maximum of 1000 lots) to prevent catastrophic "fat-finger" submissions.
  - Price parameters must be positive and bounded.
  - Account numbers must be clean strings of safe lengths.

## 3. Safe Defaults & Profiles
The engine defines four explicit profiles to control operational risks:
- **`Development`**: Local testing.
- **`Simulation`**: Paper trading with simulated feeds.
- **`PaperTrading`**: Live feed with simulated gateway fills.
- **`LiveTrading`**: Production mode.

**Live Execution Mode Flag (`IsLiveModeEnabled`)**: To prevent accidental live broker trading, this flag defaults to `false`. Real order routing to production MT5 gateways is completely disabled unless this flag is explicitly configured as `true` in a secure environment file.
