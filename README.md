# 💼 Nova — Personal Wealth Intelligence Platform

A modular, self-hosted finance dashboard built with WinUI 3 for tracking net worth, visualizing asset distribution, and integrating real-time data from trading APIs. Designed for extensibility, performance, and aesthetic clarity.

<img width="1920" height="1032" alt="Nova dashboard" src="https://github.com/user-attachments/assets/62aaffe2-9b7c-4651-ad16-2b5a2d3c6c64" />

*Dummy data*

## 🚀 Features

- **Modular card-based layout** — adaptive grid with panels for accounts, investments/crypto, net worth, wealth distribution and recent activity.
- **Real-time asset data** — live positions from Trading212 and Kraken, cached to stay within rate limits.
- **Net worth & cashflow tracking** — every payment, income, transfer, interest payment and balance update is logged as an account event in PostgreSQL, along with the resulting net worth.
- **Forms for every event type** — create accounts and record payments, income, transfers, interest and manual balance updates (optionally back-dated).
- **Console client** — the same operations from the command line.

## 🧠 Tech Stack

| Layer   | Tools & Frameworks                               |
|---------|--------------------------------------------------|
| UI/UX   | WinUI 3, XAML, Syncfusion charts                  |
| Backend | C#, .NET 8, async/await                           |
| APIs    | Trading212, Kraken                                |
| Data    | PostgreSQL (Npgsql), JSON                         |

## 📁 Solution layout

| Project | Description |
|---------|-------------|
| `Nova` | Core library: `Account`/`AccountEvent` models, `Database/AccountManager` (PostgreSQL access), and the `APIs/` clients for Trading212 and Kraken. |
| `Nova.Client` | WinUI 3 desktop dashboard (`Controls/Panels`, `Forms`). |
| `Nova.ConsoleClient` | Command-line front end over the same library. |

## ⚙️ Setup

### Requirements

- Windows 10 (19041) or later, Visual Studio with the Windows App SDK workload
- .NET 8 SDK
- A PostgreSQL database with `accounts` and `account_events` tables
- API keys for Trading212 and Kraken (optional — only the investment panels use them)

### Configuration

All secrets are read from environment variables:

| Variable | Purpose |
|----------|---------|
| `NOVA-DatabaseConnectionString` | Npgsql connection string for the PostgreSQL database |
| `NOVA-Trading212APIKey` | Trading212 API key |
| `NOVA-KrakenAPIKey` | Kraken API key |
| `NOVA-KrakenAPISecret` | Kraken API secret |

### Run

- **Dashboard:** open `Nova.sln` in Visual Studio, set `Nova.Client` as the startup project (x64) and run.
- **Console:** `dotnet run --project Nova.ConsoleClient -- <command> [args]`

## 🖥️ Console commands

| Command | Arguments |
|---------|-----------|
| `accounts` | List accounts |
| `events` | `[accountId]` |
| `changes` | `[accountId]` |
| `create` | `<accountName> <accountProvider> <accountType> <initialBalance>` |
| `update` | `<accountId> <newBalance> [daysPrevious]` |
| `payment` | `<accountId> <amount> <payee> [daysPrevious]` |
| `income` | `<accountId> <amount> <source> [daysPrevious]` |
| `transfer` | `<fromAccountId> <toAccountId> <value> [daysPrevious]` |
| `interest` | `<accountId> <amount> [daysPrevious]` |
| `payees` | `<accountId>` |
| `help` | Show usage |

Account types: `Current`, `Savings`, `Investment`, `Asset`.
