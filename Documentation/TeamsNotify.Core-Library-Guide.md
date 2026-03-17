# TeamsNotify.Core — Developer Guide
> For .NET developers who want to send Teams messages directly from their application without shelling out to the CLI.

---

> **STUB — Complete this document once the application is finalised and all rough edges are resolved.**
> Sections marked `[TODO]` require working code and confirmed API surface before they can be written accurately.

---

## Overview

`TeamsNotify.Core` is a .NET library that wraps the Microsoft Graph API for sending messages to Microsoft Teams channels. It handles authentication via Client Credentials (app-only), team and channel resolution by name or ID, and message delivery.

Use this library if:
- You are building a .NET application that needs to send Teams notifications programmatically
- You want typed, awaitable API calls rather than shelling out to the `teams-notify` CLI
- You need to integrate Teams messaging into a larger service, background worker, or pipeline

---

## Requirements

- .NET 10 or later
- An Entra ID App Registration with:
  - `ChannelMessage.Send` application permission (admin consented)
  - A client secret

See [Setting Up an Entra ID App Registration](#setting-up-an-entra-id-app-registration) below.

---

## Installation

```bash
dotnet add package TeamsNotify.Core
```

---

## Setting Up an Entra ID App Registration

[TODO] Step-by-step walkthrough:
1. Create the App Registration in the Azure / Entra portal
2. Add the `ChannelMessage.Send` application permission
3. Grant admin consent
4. Create a client secret
5. Note down Tenant ID, Client ID, Client Secret

---

## Quick Start

[TODO] Minimal working example — send a message to a known channel by ID:

```csharp
// [TODO — fill in once API surface is confirmed]
```

---

## Authentication

`AuthService` accepts a `TeamsCredentials` record and returns a configured `GraphServiceClient`.

```csharp
// [TODO — fill in once AuthService.BuildGraphClientAsync() is implemented]
```

### `TeamsCredentials`

| Property | Type | Description |
|---|---|---|
| `TenantId` | `string` | Your Entra ID tenant GUID |
| `ClientId` | `string` | App Registration client ID |
| `ClientSecret` | `string` | App Registration client secret |

---

## Sending a Message

`GraphService.SendMessageAsync()` accepts a `SendMessageRequest` record.

```csharp
// [TODO — fill in once GraphService.SendMessageAsync() is implemented]
```

### `SendMessageRequest`

| Property | Type | Required | Description |
|---|---|---|---|
| `TeamId` | `string` | Yes | GUID of the target team |
| `ChannelId` | `string` | Yes | GUID of the target channel |
| `Body` | `string` | Yes | Message text or HTML |
| `IsHtml` | `bool` | No | Set `true` to send `Body` as HTML |
| `Subject` | `string?` | No | Optional subject line shown above the message |

### Sending plain text

```csharp
// [TODO]
```

### Sending HTML

```csharp
// [TODO]
```

### Sending with a subject line

```csharp
// [TODO]
```

---

## Resolving Team and Channel Names to IDs

`GraphService` can resolve a human-readable team or channel name to its Graph API GUID. Pass a GUID directly to skip the lookup.

```csharp
// [TODO — fill in once ResolveTeamIdAsync() and ResolveChannelIdAsync() are implemented]
```

---

## Listing Teams and Channels

```csharp
// [TODO — fill in once ListTeamsAsync() and ListChannelsAsync() are implemented]
```

---

## Error Handling

[TODO] Document exceptions thrown by `AuthService` and `GraphService`:
- Auth failure (invalid credentials, missing consent)
- Team or channel not found
- Graph API throttling (429) — SDK retry behaviour
- Graph API server errors (5xx)

---

## Dependency Injection

[TODO] Show how to wire `AuthService` and `GraphService` into `IServiceCollection` for use in ASP.NET Core, Worker Services, etc.

```csharp
// [TODO]
```

---

## Full Example

[TODO] End-to-end example: load credentials from configuration, build the client, resolve a channel by name, send a formatted HTML message.

```csharp
// [TODO]
```
