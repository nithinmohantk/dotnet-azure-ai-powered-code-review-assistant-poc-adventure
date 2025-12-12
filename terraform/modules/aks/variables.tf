variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

variable "location" {
  description = "Azure region for resources"
  type        = string
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
}

variable "project_name" {
  description = "Project name prefix"
  type        = string
}

variable "kubernetes_version" {
  description = "Kubernetes version for AKS cluster"
  type        = string
}

variable "node_count" {
  description = "Number of nodes in application node pool"
  type        = number
  default     = 3
}

variable "vm_size" {
  description = "VM size for application nodes"
  type        = string
  default     = "Standard_D2s_v3"
}

variable "system_node_count" {
  description = "Number of nodes in system node pool"
  type        = number
  default     = 1
}

variable "system_vm_size" {
  description = "VM size for system nodes"
  type        = string
  default     = "Standard_B2s"
}

variable "vnet_subnet_id" {
  description = "ID of the subnet for AKS"
  type        = string
}

variable "log_analytics_workspace_id" {
  description = "ID of existing Log Analytics Workspace (optional)"
  type        = string
  default     = null
}

variable "aks_admin_group_object_id" {
  description = "Object ID of Azure AD group for AKS admin access"
  type        = string
  default     = null
}

variable "grafana_admin_password" {
  description = "Admin password for Grafana"
  type        = string
  default     = "Admin123!"
  sensitive   = true
}

variable "container_registry_id" {
  description = "ID of the Azure Container Registry"
  type        = string
}

variable "key_vault_id" {
  description = "ID of the Azure Key Vault"
  type        = string
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}
