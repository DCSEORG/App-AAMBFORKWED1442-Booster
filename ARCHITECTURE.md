# Azure Services Architecture Diagram

This document describes the Azure services created when deploying the Northwind App Modernization solution.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            Azure Resource Group                              │
│                          (rg-northwind-demo)                                │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                     User-Assigned Managed Identity                   │   │
│  │                        (mid-northwind-xxx)                           │   │
│  │                                                                      │   │
│  │  Used for secure authentication between Azure services               │   │
│  │  No passwords or connection strings required                         │   │
│  └──────────────────────────────┬───────────────────────────────────────┘   │
│                                 │                                           │
│           ┌─────────────────────┼─────────────────────┐                     │
│           │                     │                     │                     │
│           ▼                     ▼                     ▼                     │
│  ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐           │
│  │   App Service   │   │   Azure SQL     │   │  Azure OpenAI   │           │
│  │    (S1 SKU)     │   │   Database      │   │  (Sweden Central)│           │
│  │                 │   │   (Basic)       │   │                 │           │
│  │ • ASP.NET 8.0   │   │                 │   │ • GPT-4o Model  │           │
│  │ • Razor Pages   │   │ • Northwind DB  │   │ • Function      │           │
│  │ • REST APIs     │   │ • Entra ID Auth │   │   Calling       │           │
│  │ • Swagger UI    │   │ • Stored Procs  │   │                 │           │
│  │ • Chat UI       │   │                 │   │                 │           │
│  └────────┬────────┘   └────────┬────────┘   └────────┬────────┘           │
│           │                     │                     │                     │
│           │                     │                     │                     │
│           └─────────────────────┴─────────────────────┘                     │
│                                 │                                           │
│                                 ▼                                           │
│                    ┌─────────────────────────┐                              │
│                    │    AI Search (Basic)    │                              │
│                    │    (Optional - RAG)     │                              │
│                    └─────────────────────────┘                              │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Service Connections

```
┌──────────────┐                                    ┌──────────────┐
│              │  HTTP Requests (Port 443)          │              │
│    Users     │ ──────────────────────────────────>│  App Service │
│              │                                    │              │
└──────────────┘                                    └──────┬───────┘
                                                          │
                              ┌────────────────────────────┼────────────────────────────┐
                              │                            │                            │
                              ▼                            ▼                            ▼
                    ┌──────────────────┐       ┌──────────────────┐       ┌──────────────────┐
                    │                  │       │                  │       │                  │
                    │    Azure SQL     │       │   Azure OpenAI   │       │    AI Search     │
                    │                  │       │                  │       │   (RAG Index)    │
                    │  Stored Procs:   │       │  Chat Completion │       │                  │
                    │  • sp_GetAll...  │       │  Function Calling│       │                  │
                    │  • sp_Create...  │       │                  │       │                  │
                    │  • sp_Update...  │       │                  │       │                  │
                    │  • sp_Delete...  │       │                  │       │                  │
                    └──────────────────┘       └──────────────────┘       └──────────────────┘
```

## Deployment Scripts

| Script | Description |
|--------|-------------|
| `deploy.sh` | Deploys App Service, Managed Identity, and Azure SQL (no GenAI) |
| `deploy-with-chat.sh` | Full deployment including Azure OpenAI and AI Search |

## Security Model

- **Entra ID Only Authentication**: Azure SQL requires Azure AD authentication only
- **Managed Identity**: All service-to-service authentication uses managed identity
- **No Secrets in Code**: Connection strings use `Authentication=Active Directory Managed Identity`
- **Role Assignments**: Managed identity has `db_datareader`, `db_datawriter`, and `EXECUTE` permissions

## Resource Naming Convention

All resources use lowercase names with a unique suffix generated from `uniqueString(resourceGroup().id)`:

- App Service: `app-northwind-{uniqueSuffix}`
- SQL Server: `sql-northwind-{uniqueSuffix}`
- Managed Identity: `mid-northwind-{uniqueSuffix}`
- Azure OpenAI: `aoai-northwind-{uniqueSuffix}` (Sweden Central)
- AI Search: `search-northwind-{uniqueSuffix}` (Sweden Central)

## SKUs Used

| Service | SKU | Notes |
|---------|-----|-------|
| App Service Plan | S1 | Standard tier to avoid cold starts |
| Azure SQL Database | Basic | Development tier, 2GB max |
| Azure OpenAI | S0 | Standard cognitive services tier |
| AI Search | Basic | Development tier |
| GPT-4o Deployment | Standard (Capacity: 8) | Sweden Central region |
