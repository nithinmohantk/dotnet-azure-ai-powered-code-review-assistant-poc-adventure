terraform {
  required_providers {
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 3.7"
    }
  }
}

data "azurerm_client_config" "current" {}

# Azure AD Group for SQL Administrators
resource "azuread_group" "sql_admins" {
  display_name     = var.sql_admin_group_name
  security_enabled = true
  owners           = [data.azurerm_client_config.current.object_id]

  members = [data.azurerm_client_config.current.object_id]
}

# Azure AD Group for Application Administrators
resource "azuread_group" "app_admins" {
  display_name     = "CodeReview-App-Admins"
  security_enabled = true
  owners           = [data.azurerm_client_config.current.object_id]

  members = [data.azurerm_client_config.current.object_id]
}

# Azure AD Group for Developers
resource "azuread_group" "developers" {
  display_name     = "CodeReview-Developers"
  security_enabled = true
  owners           = [data.azurerm_client_config.current.object_id]

  members = [data.azurerm_client_config.current.object_id]
}

# Azure AD Service Principal for the Application
resource "azuread_service_principal" "app" {
  application_id = azuread_application.app.application_id
  owners         = [data.azurerm_client_config.current.object_id]
}

# Azure AD Application
resource "azuread_application" "app" {
  display_name = "${local.resource_prefix}-app"
  owners       = [data.azurerm_client_config.current.object_id]

  web {
    redirect_uris = ["https://${local.resource_prefix}-api.azurewebsites.net/.auth/login/aad/callback"]
    implicit_grant {
      access_token_issuance_enabled = true
      id_token_issuance_enabled     = true
    }
  }

  required_resource_access {
    resource_app_id = "00000003-0000-0000-c000-000000000000" # Microsoft Graph

    resource_access {
      id   = "e1fe6dd8-ba31-4d61-89e7-88639da4683d" # User.Read
      type = "Scope"
    }

    resource_access {
      id   = "06da0dbc-49e2-44d2-8312-53f166ab648a" # Directory.Read.All
      type = "Scope"
    }
  }

  fallback_public_client_enabled = true
}

# Azure AD Client Secret
resource "azuread_application_password" "app" {
  application_object_id = azuread_application.app.object_id
  display_name          = "${local.resource_prefix}-app-secret"
}

# Role Assignments
resource "azurerm_role_assignment" "api_key_vault" {
  scope                = azurerm_key_vault.main.id
  role_definition_name  = "Key Vault Secrets User"
  principal_id         = azuread_service_principal.app.object_id
}

resource "azurerm_role_assignment" "api_acr_pull" {
  scope                = azurerm_container_registry.main.id
  role_definition_name  = "AcrPull"
  principal_id         = azuread_service_principal.app.object_id
}

resource "azurerm_role_assignment" "api_service_bus" {
  scope                = azurerm_servicebus_namespace.main.id
  role_definition_name  = "Azure Service Bus Data Receiver"
  principal_id         = azuread_service_principal.app.object_id
}

resource "azurerm_role_assignment" "api_sql_db" {
  scope                = azurerm_mssql_database.main.id
  role_definition_name  = "SQL DB Contributor"
  principal_id         = azuread_service_principal.app.object_id
}

# Key Vault Access Policies for Service Principal
resource "azurerm_key_vault_access_policy" "app" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azuread_service_principal.app.object_id

  secret_permissions = [
    "Get",
    "List"
  ]
}

# Store Application Credentials in Key Vault
resource "azurerm_key_vault_secret" "app_client_id" {
  name         = "app-client-id"
  value        = azuread_application.app.application_id
  key_vault_id = azurerm_key_vault.main.id
}

resource "azurerm_key_vault_secret" "app_client_secret" {
  name         = "app-client-secret"
  value        = azuread_application_password.app.value
  key_vault_id = azurerm_key_vault.main.id
}

resource "azurerm_key_vault_secret" "app_tenant_id" {
  name         = "app-tenant-id"
  value        = data.azurerm_client_config.current.tenant_id
  key_vault_id = azurerm_key_vault.main.id
}
