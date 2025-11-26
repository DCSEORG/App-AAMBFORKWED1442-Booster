#!/bin/bash
# deploy-with-chat.sh - Deploy Northwind App with GenAI resources for Chat UI
# Run this script after: az login && az account set --subscription <your-subscription-id>

set -e

echo "=========================================="
echo "Northwind App - Full Deployment with GenAI"
echo "=========================================="

# Configuration - Update these values
RESOURCE_GROUP="rg-northwind-demo"
LOCATION="uksouth"

# Get current user's Azure AD info for SQL admin
echo "Getting Azure AD user information..."
ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)
ADMIN_LOGIN=$(az ad signed-in-user show --query userPrincipalName -o tsv)

echo "Admin Object ID: $ADMIN_OBJECT_ID"
echo "Admin Login: $ADMIN_LOGIN"

# Step 1: Create Resource Group
echo ""
echo "Step 1: Creating Resource Group..."
az group create --name $RESOURCE_GROUP --location $LOCATION

# Step 2: Deploy Infrastructure with GenAI resources
echo ""
echo "Step 2: Deploying Infrastructure (App Service, Managed Identity, SQL, GenAI)..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file infra/main.bicep \
  --parameters adminObjectId=$ADMIN_OBJECT_ID adminLogin=$ADMIN_LOGIN deployGenAI=true \
  --query "properties.outputs" -o json)

echo "Deployment completed. Extracting outputs..."

# Extract deployment outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
APP_SERVICE_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceUrl.value')
SQL_SERVER_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerName.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerFqdn.value')
SQL_DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlDatabaseName.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')

# GenAI outputs
OPENAI_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAIEndpoint.value')
OPENAI_MODEL_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAIModelName.value')
SEARCH_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.searchEndpoint.value')

echo ""
echo "Deployment Outputs:"
echo "  App Service: $APP_SERVICE_NAME"
echo "  App URL: $APP_SERVICE_URL"
echo "  SQL Server: $SQL_SERVER_FQDN"
echo "  Database: $SQL_DATABASE_NAME"
echo "  Managed Identity: $MANAGED_IDENTITY_NAME"
echo "  Managed Identity Client ID: $MANAGED_IDENTITY_CLIENT_ID"
echo "  OpenAI Endpoint: $OPENAI_ENDPOINT"
echo "  OpenAI Model: $OPENAI_MODEL_NAME"
echo "  Search Endpoint: $SEARCH_ENDPOINT"

# Step 3: Configure App Service Settings (including GenAI)
echo ""
echo "Step 3: Configuring App Service settings..."
az webapp config appsettings set \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "ConnectionStrings__DefaultConnection=Server=tcp:${SQL_SERVER_FQDN},1433;Database=${SQL_DATABASE_NAME};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" \
    "AZURE_CLIENT_ID=${MANAGED_IDENTITY_CLIENT_ID}" \
    "ManagedIdentityClientId=${MANAGED_IDENTITY_CLIENT_ID}" \
    "OpenAI__Endpoint=${OPENAI_ENDPOINT}" \
    "OpenAI__DeploymentName=${OPENAI_MODEL_NAME}" \
    "Search__Endpoint=${SEARCH_ENDPOINT}" \
    "GenAI__Enabled=true"

# Step 4: Wait for SQL Server to be ready
echo ""
echo "Step 4: Waiting 30 seconds for SQL Server to be fully ready..."
sleep 30

# Step 5: Add local IP to SQL firewall
echo ""
echo "Step 5: Adding local IP to SQL Server firewall..."
LOCAL_IP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --name "LocalDeployment" \
  --start-ip-address $LOCAL_IP \
  --end-ip-address $LOCAL_IP

# Step 6: Install Python dependencies
echo ""
echo "Step 6: Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity

# Step 7: Set environment variables and update script.sql for Python scripts
echo ""
echo "Step 7: Setting environment variables for Python scripts..."
export SQL_SERVER="${SQL_SERVER_FQDN}"
export SQL_DATABASE="${SQL_DATABASE_NAME}"

# Update script.sql with managed identity name (still needed for the SQL script content)
sed -i.bak "s/MANAGED-IDENTITY-NAME/${MANAGED_IDENTITY_NAME}/g" script.sql && rm -f script.sql.bak

# Step 8: Import database schema
echo ""
echo "Step 8: Importing database schema..."
SQL_SCRIPT_FILE="Database-Schema/database_schema.sql" python3 run-sql.py

# Step 9: Configure managed identity database roles
echo ""
echo "Step 9: Configuring managed identity database roles..."
SQL_SCRIPT_FILE="script.sql" python3 run-sql-dbrole.py

# Step 10: Create stored procedures
echo ""
echo "Step 10: Creating stored procedures..."
SQL_SCRIPT_FILE="stored-procedures.sql" python3 run-sql-stored-procs.py

# Step 11: Deploy application code
echo ""
echo "Step 11: Deploying application code..."
az webapp deploy \
  --resource-group $RESOURCE_GROUP \
  --name $APP_SERVICE_NAME \
  --src-path ./app.zip

echo ""
echo "=========================================="
echo "Full Deployment with GenAI Complete!"
echo "=========================================="
echo ""
echo "Application URL: ${APP_SERVICE_URL}/Index"
echo "Chat UI URL: ${APP_SERVICE_URL}/Chat"
echo ""
echo "NOTE: Navigate to ${APP_SERVICE_URL}/Index (not just the root URL)"
echo ""
echo "To run the app locally, update appsettings.json to use:"
echo "  Connection String: Authentication=Active Directory Default"
echo "  Add OpenAI settings from above"
echo "Then run: az login && dotnet run"
