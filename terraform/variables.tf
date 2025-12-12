variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "dev"
  
  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be one of: dev, staging, prod."
  }
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "East US"
  
  validation {
    condition     = contains(["East US", "West US", "Central US", "North Europe", "West Europe"], var.location)
    error_message = "Location must be a valid Azure region."
  }
}

variable "owner_email" {
  description = "Email of the resource owner"
  type        = string
  default     = "admin@example.com"
  
  validation {
    condition     = can(regex("^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$", var.owner_email))
    error_message = "Owner email must be a valid email address."
  }
}

variable "vnet_address_space" {
  description = "Address space for the virtual network"
  type        = list(string)
  default     = ["10.0.0.0/16"]
  
  validation {
    condition     = length(var.vnet_address_space) > 0
    error_message = "VNet address space cannot be empty."
  }
}

variable "subnet_prefixes" {
  description = "Address prefixes for subnets"
  type        = object({
    aks_subnet     = string
    redis_subnet   = string
    sql_subnet     = string
    app_gateway    = string
    bastion        = string
  })
  default = {
    aks_subnet     = "10.0.1.0/24"
    redis_subnet   = "10.0.2.0/24"
    sql_subnet     = "10.0.3.0/24"
    app_gateway    = "10.0.4.0/24"
    bastion        = "10.0.5.0/24"
  }
  
  validation {
    condition = alltrue([
      can(cidrhost(var.subnet_prefixes.aks_subnet, 0)),
      can(cidrhost(var.subnet_prefixes.redis_subnet, 0)),
      can(cidrhost(var.subnet_prefixes.sql_subnet, 0)),
      can(cidrhost(var.subnet_prefixes.app_gateway, 0)),
      can(cidrhost(var.subnet_prefixes.bastion, 0))
    ])
    error_message = "All subnet prefixes must be valid CIDR blocks."
  }
}

variable "kubernetes_version" {
  description = "Kubernetes version for AKS cluster"
  type        = string
  default     = "1.28.0"
  
  validation {
    condition     = can(regex("^1\\.[0-9]+\\.[0-9]+$", var.kubernetes_version))
    error_message = "Kubernetes version must be in format x.y.z."
  }
}

variable "node_count" {
  description = "Number of nodes in AKS cluster"
  type        = number
  default     = 3
  
  validation {
    condition     = var.node_count >= 1 && var.node_count <= 100
    error_message = "Node count must be between 1 and 100."
  }
}

variable "vm_size" {
  description = "VM size for AKS nodes"
  type        = string
  default     = "Standard_D2s_v3"
  
  validation {
    condition     = contains(["Standard_B2s", "Standard_D2s_v3", "Standard_D4s_v3", "Standard_D8s_v3"], var.vm_size)
    error_message = "VM size must be a valid AKS node size."
  }
}

variable "enable_monitoring" {
  description = "Enable Azure Monitor integration"
  type        = bool
  default     = true
}

variable "enable_application_insights" {
  description = "Enable Application Insights"
  type        = bool
  default     = true
}

variable "enable_log_analytics" {
  description = "Enable Log Analytics Workspace"
  type        = bool
  default     = true
}

variable "cosmos_db_consistency_level" {
  description = "Cosmos DB consistency level"
  type        = string
  default     = "Session"
  
  validation {
    condition     = contains(["Strong", "BoundedStaleness", "Session", "ConsistentPrefix", "Eventual"], var.cosmos_db_consistency_level)
    error_message = "Consistency level must be one of: Strong, BoundedStaleness, Session, ConsistentPrefix, Eventual."
  }
}

variable "service_bus_sku" {
  description = "Service Bus SKU"
  type        = string
  default     = "Standard"
  
  validation {
    condition     = contains(["Basic", "Standard", "Premium"], var.service_bus_sku)
    error_message = "Service Bus SKU must be one of: Basic, Standard, Premium."
  }
}

variable "redis_sku" {
  description = "Redis Cache SKU"
  type        = string
  default     = "Standard"
  
  validation {
    condition     = contains(["Basic", "Standard", "Premium"], var.redis_sku)
    error_message = "Redis SKU must be one of: Basic, Standard, Premium."
  }
}

variable "redis_capacity" {
  description = "Redis Cache capacity"
  type        = number
  default     = 1
  
  validation {
    condition     = contains([0, 1, 2, 3, 4, 5, 6], var.redis_capacity)
    error_message = "Redis capacity must be between 0 and 6."
  }
}

variable "sql_database_sku" {
  description = "SQL Database SKU"
  type        = string
  default     = "S2"
  
  validation {
    condition     = contains(["Basic", "S0", "S1", "S2", "S3", "S4", "S6", "S7", "S9", "S12"], var.sql_database_sku)
    error_message = "SQL Database SKU must be a valid Azure SQL Database SKU."
  }
}

variable "container_registry_sku" {
  description = "Container Registry SKU"
  type        = string
  default     = "Standard"
  
  validation {
    condition     = contains(["Basic", "Standard", "Premium"], var.container_registry_sku)
    error_message = "Container Registry SKU must be one of: Basic, Standard, Premium."
  }
}

variable "tags" {
  description = "Additional tags for resources"
  type        = map(string)
  default     = {}
}

variable "enable_private_endpoints" {
  description = "Enable private endpoints for Azure services"
  type        = bool
  default     = true
}

variable "enable_dns_zones" {
  description = "Enable private DNS zones"
  type        = bool
  default     = true
}
