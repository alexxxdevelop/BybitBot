# BybitBot

A Windows desktop trading terminal for crypto perpetual futures that manages an unlimited number of exchange accounts from a single interface. One action is fanned out to every active account, position sizing is recalculated per account balance, and balances, open positions and PnL are aggregated across all accounts in near real time.

![Platform](https://img.shields.io/badge/platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET%20Framework-4.8-512BD4)
![Language](https://img.shields.io/badge/language-C%23-239120)
![UI](https://img.shields.io/badge/UI-WPF-2C3E50)

## Overview

Managing trades across many accounts through the exchanges' own web interfaces is slow and error prone. Every trade has to be repeated on each account, the order size recalculated for each balance and leverage, and take profit / stop loss set separately. On a volatile market, a delay of minutes produces different entry prices and direct losses.

BybitBot solves this by acting as a single control panel for synchronous trading: place, modify and close orders on all active accounts in one action, with identical risk parameters and per account sizing, while watching a consolidated view of the whole portfolio.

## Features

- **Multi exchange support** through a unified abstraction layer. Bybit (v5 API) and MEXC (Contract API) are implemented; adding another exchange means writing a single adapter.
- **Fan out execution.** A single order is broadcast to every active account. Quantity is computed as a percentage of each account's own balance and rounded to the instrument's lot step.
- **Position aggregation.** Positions and limit orders from all accounts are merged into one consolidated table with averaged entry price and summed realized / unrealized PnL.
- **Risk management.** Take profit and stop loss can be set either by absolute price or by ROI percentage, with live two way conversion between the two.
- **Real time updates.** A continuous asynchronous polling loop refreshes balances and positions roughly once per second, with a reentrancy guard that prevents overlapping update cycles.
- **PnL reporting.** Closed trade history is paginated up to a year back and aggregated into per account reports by day, week, month, year and custom date range, with automatic management fee (commission) calculation.
- **Order operations.** Market and limit orders, add to position, partial and full close by percentage, amend and cancel.
- **Application protection.** Hardware binding plus a remote access kill switch guard against unauthorized launch.

## Architecture

The core is a single exchange contract (`Bot`) that the rest of the application depends on. Concrete adapters implement it per exchange, so all trading logic is exchange agnostic and exchange specific details stay encapsulated.

```
MainWindow / Actions (WPF UI)
        |
        v
   Bot (abstract contract)
     /            \
  Bybit          Mexc        <- exchange adapters
     \            /
   REST APIs (HMAC-SHA256 signed)
```

Key implementation points:

| Concern | Approach |
| --- | --- |
| Heterogeneous APIs | Unified `Bot` contract; per exchange adapters (`Bybit`, `Mexc`) |
| Request signing | HMAC-SHA256 with each exchange's own canonical string |
| Clock drift | Server time fetched before every signed request to stay within `recvWindow` |
| Concurrency | Async polling loop with a busy flag; UI updates marshaled to the dispatcher thread |
| Aggregation | Positions / orders matched by symbol, side, leverage and order id, then merged |
| Money precision | `decimal` arithmetic throughout; quantity rounded to instrument lot step; invariant number formatting |
| Resilience | Per call error handling with retries; exchange errors surfaced to the UI log without crashing |
| History | Cursor based pagination in weekly windows, aggregated into period reports |

## Supported exchanges

| Exchange | API | Status |
| --- | --- | --- |
| Bybit | v5 (linear / USDT perpetuals) | Full: tickers, balance, positions, orders, place / amend / cancel, leverage, TP/SL, closed PnL history |
| MEXC | Contract API | Tickers, balance, place order |

## Tech stack

- C# / .NET Framework 4.8
- WPF (XAML) with custom-themed controls via `ResourceDictionary`
- Asynchronous programming with `async` / `await` and `Task`
- `HttpClient` against Bybit v5 and MEXC Contract REST APIs
- HMAC-SHA256 request signing
- Newtonsoft.Json for serialization
- Adapter pattern for exchange abstraction

## Getting started

### Prerequisites

- Windows
- .NET Framework 4.8 Developer Pack
- Visual Studio 2022 (or `dotnet` / `msbuild` with the WPF workload)

### Build

```powershell
git clone <repository-url>
cd BybitBot
msbuild BybitBot.sln /p:Configuration=Release
```

Or open `BybitBot.sln` in Visual Studio and build the `BybitBot` project.

> Note: the project references two external assemblies (`Libs.dll`, `Web.dll`) via `HintPath` in `BybitBot.csproj`. Adjust those paths to your local copies before building.

### Configuration

1. Launch the application.
2. Add one or more API keys (key, secret, label, commission, testnet flag) per exchange.
3. Toggle which accounts are active.
4. Set the order size as a percentage of balance and the leverage, then trade.

API keys and settings are persisted locally as `config.json`.

## Project structure

```
BybitBot/
  Models/
    Bot.cs        # abstract exchange contract
    Bybit.cs      # Bybit v5 adapter
    Mexc.cs       # MEXC Contract adapter
    _Models.cs    # data models (Api, Position, History, Ticker, ...)
  MainWindow.*    # main terminal: accounts, positions, history, trading
  Actions.*       # per position actions: TP/SL, add, close, amend, cancel
  ApiWindow.*     # add / edit API key dialog
  Resources/      # custom WPF control themes
```

## Disclaimer

This software interacts with live trading APIs and can place real orders with real funds. Trading crypto derivatives carries substantial risk. Use a testnet account first and trade at your own risk.
