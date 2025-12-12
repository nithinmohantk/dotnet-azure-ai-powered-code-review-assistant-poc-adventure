terraform {
  required_version = ">= 1.5.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    azapi = {
      source  = "Azure/azapi"
      version = "~> 1.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }

  backend "azurerm" {
    resource_group_name  = "tfstate-rg"
    storage_account_name = "tfstateterraformstorage"
    container_name       = "tfstate"
    key                  = "codereview-assistant.tfstate"
  }
}

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}

# Local variables for naming conventions
locals {
  project_name     = "codereview"
  environment      = var.environment
  location         = var.location
  resource_suffix  = "${local.project_name}-${local.environment}"
  
  tags = {
    Project     = local.project_name
    Environment = local.environment
    ManagedBy   = "Terraform"
    Owner       = var.owner_email
  }
}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = "${local.resource_suffix}-rg"
  location = local.location
  tags     = local.tags
}

# Random resources for unique naming
resource "random_string" "storage_suffix" {
  length  = 8
  special = false
  upper   = false
}

# Networking Module
module "networking" {
  source              = "./modules/networking"
  resource_group_name = azurerm_resource_group.main.name
  location            = local.location
  environment         = local.environment
  project_name        = local.project_name
  address_space       = var.vnet_address_space
  subnet_prefixes     = var.subnet_prefixes
  tags                = local.tags
}

# AKS Module
module "aks" {
  source                 = "./modules/aks"
  resource_group_name    = azurerm_resource_group.main.name
  location               = local.location
  environment            = local.environment
  project_name           = local.project_name
  kubernetes_version    = var.kubernetes_version
  node_count             = var.node_count
  vm_size                = var.vm_size
  vnet_subnet_id         = module.networking.aks_subnet_id
  log_analytics_workspace_id = module.monitoring.log_analytics_workspace_id
  tags                   = local.tags

  depends_on = [module.networking, module.monitoring]
}

# Application Insights Module
module "application_insights" {
  source              = "./modules/application-insights"
  resource_group_name = azurerm_resource_group.main.name
  location            = local.location
  environment         = local.environment
  project_name        = local.project_name
  tags                = local.tags
}

# Key Vault Module
module "key_vault" {
  source              = "./modules/key-vault"
  resource_group_name = azurerm_resource_group.main.name
  location            = local.location
  environment         = local.environment
  project_name        = local.project_name
  tenant_id           = data.azurerm_client_config.current.tenant_id
  object_id           = data.azurerm_client_config.current.object_id
  tags                = local.tags
}

# Cosmos DB Module
module "cosmos_db" {
  source              = "./modules/cosmos-db"
  resource_group_name = azurerm_resource_group.main.name
  location            = local.location
  environment         = local.environment
  project_name        = local.project_name
  tags                = local.tags
}

# Service Bus Module
module "service_bus" {
  source              = "./modules/service-bus"
  resource_group_name = azurerm_resource_group.main.name
  location            = local.location
  environment         = local.environment
  project_name        = local.project_name
  tags                = local.tags
}

# Redis Cache Module
module "redis_cache" {
  source              = "./modules/redis-cache"
  resource_group_name = azurerm_resource_group.main.name
  location            = local.location
  environment         = local.environment
  project_name        = local.project_name
  vnet_subnet_id      = module.networking.redis_subnet_id
  tags                = local.tags
}

# Monitoring Module
module "monitoring" {
  source              = "./modules/monitoring"
  resource_group_name = azurerm_resource_group.main.name
  location            = local.location
  environment         = local.environment
  project_name        = local.project_name
  tags                = local.tags
}

# Container Registry Module
module "container_registry" {
  source              = "./modules/container-registry"
  resource_group_name = azurerm_resource_group.main.name
  location            = local.location
  environment         = local.environment
  project_name        = local.project_name
  tags                = local.tags
}

# SQL Database Module
module "sql_database" {
  source              = "./modules/sql-database"
  resource_group_name = azurerm_resource_group.main.name
  location            = local.location
  environment         = local.environment
  project_name        = local.project_name
  vnet_subnet_id      = module.networking.sql_subnet_id
  tags                = local.tags
}

# Data sources
data "azurerm_client_config" "current" {}

# Outputs
output "resource_group_name" {
  value = azurerm_resource_group.main.name
}

output "aks_cluster_name" {
  value = module.aks.aks_cluster_name
}

output "key_vault_name" {
  value = module.key_vault.key_vault_name
}

output "cosmos_db_endpoint" {
  value = module.cosmos_db.cosmos_db_endpoint
}

output "service_bus_namespace" {
  value = module.service_bus.service_bus_namespace
}

output "redis_cache_name" {
  value = module.redis_cache.redis_cache_name
}

output "container_registry_url" {
  value = module.container_registry.container_registry_login_server
}

output "sql_database_server_fqdn" {
  value = module.sql_database.sql_server_fqdn
}

output "application_insights_app_id" {
  value = module.application_insights.application_insights_app_id
}
