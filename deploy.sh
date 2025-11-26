#!/bin/bash
# deploy.sh - Deploy Northwind App Modernization (without GenAI resources)
# Run this script after: az login && az account set --subscription <your-subscription-id>

set -e

echo "=========================================="
echo "Northwind App Modernization - Deployment"
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

# Step 2: Deploy Infrastructure
echo ""
echo "Step 2: Deploying Infrastructure (App Service, Managed Identity, SQL)..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file infra/main.bicep \
  --parameters adminObjectId=$ADMIN_OBJECT_ID adminLogin=$ADMIN_LOGIN deployGenAI=false \
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

echo ""
echo "Deployment Outputs:"
echo "  App Service: $APP_SERVICE_NAME"
echo "  App URL: $APP_SERVICE_URL"
echo "  SQL Server: $SQL_SERVER_FQDN"
echo "  Database: $SQL_DATABASE_NAME"
echo "  Managed Identity: $MANAGED_IDENTITY_NAME"
echo "  Managed Identity Client ID: $MANAGED_IDENTITY_CLIENT_ID"

# Step 3: Configure App Service Settings
echo ""
echo "Step 3: Configuring App Service settings..."
az webapp config appsettings set \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "ConnectionStrings__DefaultConnection=Server=tcp:${SQL_SERVER_FQDN},1433;Database=${SQL_DATABASE_NAME};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" \
    "AZURE_CLIENT_ID=${MANAGED_IDENTITY_CLIENT_ID}" \
    "ManagedIdentityClientId=${MANAGED_IDENTITY_CLIENT_ID}"

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

# Step 7: Update Python scripts with actual server/database values
echo ""
echo "Step 7: Updating Python scripts with deployment values..."
sed -i.bak "s/example.database.windows.net/${SQL_SERVER_FQDN}/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/database_name/${SQL_DATABASE_NAME}/g" run-sql.py && rm -f run-sql.py.bak

sed -i.bak "s/example.database.windows.net/${SQL_SERVER_FQDN}/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/database_name/${SQL_DATABASE_NAME}/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak

sed -i.bak "s/example.database.windows.net/${SQL_SERVER_FQDN}/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak
sed -i.bak "s/database_name/${SQL_DATABASE_NAME}/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak

# Update script.sql with managed identity name
sed -i.bak "s/MANAGED-IDENTITY-NAME/${MANAGED_IDENTITY_NAME}/g" script.sql && rm -f script.sql.bak

# Step 8: Import database schema
echo ""
echo "Step 8: Importing database schema..."
python3 run-sql.py

# Step 9: Configure managed identity database roles
echo ""
echo "Step 9: Configuring managed identity database roles..."
python3 run-sql-dbrole.py

# Step 10: Create stored procedures
echo ""
echo "Step 10: Creating stored procedures..."
python3 run-sql-stored-procs.py

# Step 11: Deploy application code
echo ""
echo "Step 11: Deploying application code..."
az webapp deploy \
  --resource-group $RESOURCE_GROUP \
  --name $APP_SERVICE_NAME \
  --src-path ./app.zip

echo ""
echo "=========================================="
echo "Deployment Complete!"
echo "=========================================="
echo ""
echo "Application URL: ${APP_SERVICE_URL}/Index"
echo ""
echo "NOTE: Navigate to ${APP_SERVICE_URL}/Index (not just the root URL)"
echo ""
echo "To run the app locally, update appsettings.json connection string to use:"
echo "  Authentication=Active Directory Default"
echo "Then run: az login && dotnet run"
echo ""
echo "To deploy GenAI resources for the chat UI, run: ./deploy-with-chat.sh"
