terraform {
  required_version = ">= 1.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.56"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {}
}

locals {
  project_name     = "codereview"
  environment      = var.environment
  location         = var.location
  resource_prefix  = "${local.project_name}-${local.environment}"
  tags = {
    Project     = local.project_name
    Environment = local.environment
    ManagedBy   = "terraform"
  }
}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = "${local.resource_prefix}-rg"
  location = local.location
  tags     = local.tags
}

# Random suffix for unique naming
resource "random_string" "suffix" {
  length  = 8
  special = false
  upper   = false
}

# Storage Account
resource "azurerm_storage_account" "main" {
  name                     = "${local.resource_prefix}st${random_string.suffix.result}"
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "GRS"
  min_tls_version         = "TLS1_2"

  tags = local.tags
}

# Storage Container for artifacts
resource "azurerm_storage_container" "artifacts" {
  name                  = "artifacts"
  storage_account_name   = azurerm_storage_account.main.name
  container_access_type = "private"
}

# App Service Plan
resource "azurerm_service_plan" "main" {
  name                = "${local.resource_prefix}-asp"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  os_type             = "Linux"
  sku_name            = var.app_service_sku

  tags = local.tags
}

# Web App for API
resource "azurerm_linux_web_app" "api" {
  name                = "${local.resource_prefix}-api"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  service_plan_id     = azurerm_service_plan.main.id

  site_config {
    always_on = true
    application_stack {
      docker_image     = "mcr.microsoft.com/azure-app-containers/default:latest"
      docker_image_tag = "latest"
    }
    ftps_state = "FtpsOnly"
  }

  app_settings = {
    "DOCKER_REGISTRY_SERVER_URL"          = "https://mcr.microsoft.com"
    "DOCKER_ENABLE_CUSTOM_IMAGE"           = "true"
    "WEBSITE_HTTPLOGGING_ENABLED"          = "true"
    "APPINSIGHTS_INSTRUMENTATIONKEY"       = azurerm_application_insights.main.instrumentation_key
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.main.connection_string
  }

  identity {
    type = "SystemAssigned"
  }

  tags = local.tags
}

# Application Insights
resource "azurerm_application_insights" "main" {
  name                = "${local.resource_prefix}-ai"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = "web"

  tags = local.tags
}

# Log Analytics Workspace
resource "azurerm_log_analytics_workspace" "main" {
  name                = "${local.resource_prefix}-law"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 30

  tags = local.tags
}

# Azure SQL Database
resource "azurerm_mssql_server" "main" {
  name                         = "${local.resource_prefix}-sql${random_string.suffix.result}"
  resource_group_name          = azurerm_resource_group.main.name
  location                     = azurerm_resource_group.main.location
  version                      = "12.0"
  administrator_login          = "sqladmin"
  administrator_login_password = random_password.sql_password.result
  minimum_tls_version          = "1.2"

  azure_ad_administrator {
    login_username = azuread_group.sql_admins.display_name
    object_id      = azuread_group.sql_admins.id
  }

  tags = local.tags
}

resource "random_password" "sql_password" {
  length  = 16
  special = true
}

resource "azurerm_mssql_database" "main" {
  name           = "${local.resource_prefix}-db"
  server_id      = azurerm_mssql_server.main.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  sku_name       = "S0"
  max_size_gb    = 50

  tags = local.tags
}

# SQL Firewall Rules
resource "azurerm_mssql_firewall_rule" "azure" {
  name                = "AllowAzureIPs"
  server_id           = azurerm_mssql_server.main.id
  start_ip_address    = "0.0.0.0"
  end_ip_address      = "0.0.0.0"
}

# Azure Cache for Redis
resource "azurerm_redis_cache" "main" {
  name                = "${local.resource_prefix}-redis${random_string.suffix.result}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  capacity            = 1
  family              = "C"
  sku_name            = "Basic"
  enable_non_ssl_port = false
  minimum_tls_version = "1.2"

  tags = local.tags
}

# Azure Service Bus
resource "azurerm_servicebus_namespace" "main" {
  name                = "${local.resource_prefix}-sb${random_string.suffix.result}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "Standard"
  zone_redundancy     = false

  tags = local.tags
}

resource "azurerm_servicebus_queue" "code_reviews" {
  name         = "code-reviews"
  namespace_id = azurerm_servicebus_namespace.main.id
}

resource "azurerm_servicebus_topic" "events" {
  name         = "events"
  namespace_id = azurerm_servicebus_namespace.main.id
}

resource "azurerm_servicebus_topic_subscription" "api" {
  name               = "api-subscription"
  topic_id           = azurerm_servicebus_topic.events.id
  max_delivery_count = 10
}

# Azure Key Vault
resource "azurerm_key_vault" "main" {
  name                = "${local.resource_prefix}-kv${random_string.suffix.result}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  tenant_id           = data.azurerm_client_config.current.tenant_id
  sku_name            = "standard"
  purge_protection_enabled = false

  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id

    secret_permissions = [
      "Get",
      "List",
      "Set",
      "Delete",
      "Recover",
      "Purge"
    ]
  }

  tags = local.tags
}

# Key Vault Secrets
resource "azurerm_key_vault_secret" "sql_connection" {
  name         = "sql-connection-string"
  value        = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main.name};Persist Security Info=False;User ID=sqladmin;Password=${random_password.sql_password.result};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  key_vault_id = azurerm_key_vault.main.id
}

resource "azurerm_key_vault_secret" "redis_connection" {
  name         = "redis-connection-string"
  value        = "${azurerm_redis_cache.main.hostname}:6380,password=${azurerm_redis_cache.main.primary_access_key},ssl=True,abortConnect=False"
  key_vault_id = azurerm_key_vault.main.id
}

resource "azurerm_key_vault_secret" "service_bus" {
  name         = "service-bus-connection-string"
  value        = azurerm_servicebus_namespace.primary_connection_string
  key_vault_id = azurerm_key_vault.main.id
}

# Azure Container Registry
resource "azurerm_container_registry" "main" {
  name                = "${local.resource_prefix}cr${random_string.suffix.result}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "Basic"
  admin_enabled       = true

  tags = local.tags
}

# Virtual Network
resource "azurerm_virtual_network" "main" {
  name                = "${local.resource_prefix}-vnet"
  address_space       = ["10.0.0.0/16"]
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  tags = local.tags
}

# Subnets
resource "azurerm_subnet" "web" {
  name                 = "web"
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = ["10.0.1.0/24"]

  delegation {
    name = "web-app-delegation"
    service_delegation {
      name    = "Microsoft.Web/serverFarms"
      actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
    }
  }
}

resource "azurerm_subnet" "data" {
  name                 = "data"
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = ["10.0.2.0/24"]
}

# Network Security Group
resource "azurerm_network_security_group" "main" {
  name                = "${local.resource_prefix}-nsg"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  tags = local.tags
}

# Network Security Rules
resource "azurerm_network_security_rule" "allow_http" {
  name                        = "AllowHTTP"
  priority                    = 100
  direction                   = "Inbound"
  access                      = "Allow"
  protocol                    = "Tcp"
  source_port_range          = "*"
  destination_port_range     = "80"
  source_address_prefix      = "*"
  destination_address_prefix = "*"
  resource_group_name         = azurerm_resource_group.main.name
  network_security_group_name = azurerm_network_security_group.main.name
}

resource "azurerm_network_security_rule" "allow_https" {
  name                        = "AllowHTTPS"
  priority                    = 110
  direction                   = "Inbound"
  access                      = "Allow"
  protocol                    = "Tcp"
  source_port_range          = "*"
  destination_port_range     = "443"
  source_address_prefix      = "*"
  destination_address_prefix = "*"
  resource_group_name         = azurerm_resource_group.main.name
  network_security_group_name = azurerm_network_security_group.main.name
}

# Output values
output "resource_group_name" {
  value = azurerm_resource_group.main.name
}

output "api_url" {
  value = azurerm_linux_web_app.api.default_hostname
}

output "sql_server_name" {
  value     = azurerm_mssql_server.main.fully_qualified_domain_name
  sensitive = true
}

output "redis_hostname" {
  value = azurerm_redis_cache.main.hostname
}

output "service_bus_namespace" {
  value = azurerm_servicebus_namespace.main.name
}

output "key_vault_name" {
  value = azurerm_key_vault.main.name
}

output "container_registry_url" {
  value = azurerm_container_registry.main.login_server
}

output "application_insights_key" {
  value     = azurerm_application_insights.main.instrumentation_key
  sensitive = true
}
