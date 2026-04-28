# BookMyShow – Azure Function App

Migration of the MuleSoft **BookMyShow** application to an **Azure Functions** (Python v2 programming model) app backed by **Azure Database for MySQL Flexible Server**.

---

## Project structure

```
.
├── bicep/
│   ├── main.bicep               # Bicep template – all Azure resources
│   └── parameters.example.json  # Parameters file template
├── mulesoft/                    # Original MuleSoft source (reference only)
└── src/
    ├── function_app.py          # Azure Functions HTTP triggers
    ├── requirements.txt         # Python dependencies
    ├── host.json                # Functions host configuration
    ├── local.settings.json.example  # Local settings template (copy → local.settings.json)
    ├── db_init.sql              # DDL + seed data for MySQL
    └── test_function_app.py     # Unit tests (no live DB required)
```

---

## API endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET`  | `/api/movies` | List all movies with available seats |
| `POST` | `/api/movies/{m_id}?no_tickets=N` | Book N tickets for movie `m_id` |

### Pricing tiers (from original MuleSoft logic)

| Tickets | Unit price |
|---------|-----------|
| 1 – 5   | 100       |
| 6 – 10  | 90        |
| 11+     | 80        |

---

## Local development

### Prerequisites

- Python 3.11+
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- A MySQL server (local or cloud) with the schema initialised from `src/db_init.sql`

### Setup

```bash
# 1. Install Python dependencies
cd src
pip install -r requirements.txt

# 2. Create local settings
cp local.settings.json.example local.settings.json
# Edit local.settings.json and fill in your DB_HOST, DB_USER, DB_PASSWORD, DB_NAME

# 3. Initialise the database
mysql -h <host> -u <user> -p <database> < db_init.sql

# 4. Start the function app
func start
```

### Run unit tests (no live DB required)

```bash
cd src
pip install pytest
python -m pytest test_function_app.py -v
```

---

## Deploy to Azure

### 1. Provision infrastructure with Bicep

```bash
# Copy and fill in the parameters file
cp bicep/parameters.example.json bicep/parameters.json

az group create --name rg-bookmyshow --location eastus

az deployment group create \
  --resource-group rg-bookmyshow \
  --template-file bicep/main.bicep \
  --parameters @bicep/parameters.json
```

### 2. Deploy the function code

```bash
cd src
func azure functionapp publish <functionAppName>
```

> The function app name is printed as an output of the Bicep deployment.

### 3. Initialise the MySQL database

Connect to the Azure MySQL Flexible Server created by Bicep and run:

```bash
mysql -h <mysqlServerFqdn> -u <adminLogin> -p bookmyshow < src/db_init.sql
```

---

## Environment variables

| Variable | Description |
|----------|-------------|
| `DB_HOST` | MySQL server hostname |
| `DB_PORT` | MySQL port (default `3306`) |
| `DB_USER` | MySQL admin login |
| `DB_PASSWORD` | MySQL admin password |
| `DB_NAME` | Database name (default `bookmyshow`) |

These are automatically set by the Bicep template in the Function App's application settings.
