# AKS Cluster Module

terraform {
  required_version = ">= 1.5.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.0"
    }
    helm = {
      source  = "hashicorp/helm"
      version = "~> 2.0"
    }
  }
}

# Local variables
locals {
  cluster_name = "${var.project_name}-${var.environment}-aks"
}

# Azure AD Service Principal for AKS
resource "azuread_application" "aks" {
  display_name = "${local.cluster_name}-sp"
}

resource "azuread_service_principal" "aks" {
  application_id = azuread_application.aks.application_id
}

resource "azuread_service_principal_password" "aks" {
  service_principal_id = azuread_service_principal.aks.id
  end_date_relative     = "8760h" # 1 year
}

# Log Analytics Workspace (if not provided)
resource "azurerm_log_analytics_workspace" "main" {
  count               = var.log_analytics_workspace_id == null ? 1 : 0
  name                = "${var.project_name}-${var.environment}-law"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = var.tags
}

# AKS Cluster
resource "azurerm_kubernetes_cluster" "main" {
  name                = local.cluster_name
  location            = var.location
  resource_group_name = var.resource_group_name
  dns_prefix          = "${var.project_name}-${var.environment}"
  kubernetes_version  = var.kubernetes_version

  default_node_pool {
    name           = "system"
    node_count     = var.system_node_count
    vm_size        = var.system_vm_size
    os_disk_size_gb = 30
    vnet_subnet_id = var.vnet_subnet_id
    zones          = ["1", "2", "3"]
  }

  identity {
    type = "SystemAssigned"
  }

  network_profile {
    network_plugin     = "azure"
    network_policy     = "calico"
    dns_service_ip     = "10.0.0.10"
    docker_bridge_cidr = "172.17.0.1/16"
    service_cidr       = "10.0.0.0/16"
  }

  addon_profile {
    oms_agent {
      enabled                    = true
      log_analytics_workspace_id = var.log_analytics_workspace_id != null ? var.log_analytics_workspace_id : azurerm_log_analytics_workspace.main[0].id
    }
    azure_policy {
      enabled = true
    }
    ingress_application_gateway {
      enabled = false # We'll manage AGW separately
    }
  }

  role_based_access_control {
    enabled = true
    azure_active_directory {
      managed = true
      admin_group_object_ids = [var.aks_admin_group_object_id]
    }
  }

  aci_connector_linux {
    enabled = false
  }

  auto_scaler_profile {
    balance_similar_node_groups      = true
    max_graceful_termination_sec    = 600
    new_pod_scale_up_delay          = "10s"
    scale_down_delay_after_add      = "10s"
    scale_down_unneeded_time        = "10m"
    scale_down_unready_time         = "10m"
    scale_down_utilization_threshold = "0.5"
    max_node_provisioning_time      = "15m"
  }

  tags = var.tags
}

# Additional Node Pool for Application Workloads
resource "azurerm_kubernetes_cluster_node_pool" "app" {
  name                = "app"
  kubernetes_cluster_id = azurerm_kubernetes_cluster.main.id
  vm_size             = var.vm_size
  node_count          = var.node_count
  os_disk_size_gb     = 50
  vnet_subnet_id      = var.vnet_subnet_id
  zones               = ["1", "2", "3"]
  
  node_labels = {
    "nodepool" = "app"
  }
  
  node_taints = [
    "workload=app:NoSchedule"
  ]

  upgrade_settings {
    max_surge = "33%"
  }

  depends_on = [azurerm_kubernetes_cluster.main]
}

# Kubernetes Provider Configuration
data "azurerm_kubernetes_cluster" "main" {
  name                = azurerm_kubernetes_cluster.main.name
  resource_group_name = var.resource_group_name
}

provider "kubernetes" {
  host                   = data.azurerm_kubernetes_cluster.main.kube_config.0.host
  username               = data.azurerm_kubernetes_cluster.main.kube_config.0.username
  password               = data.azurerm_kubernetes_cluster.main.kube_config.0.password
  client_certificate     = base64decode(data.azurerm_kubernetes_cluster.main.kube_config.0.client_certificate)
  client_key             = base64decode(data.azurerm_kubernetes_cluster.main.kube_config.0.client_key)
  cluster_ca_certificate = base64decode(data.azurerm_kubernetes_cluster.main.kube_config.0.cluster_ca_certificate)
}

# Helm Provider Configuration
provider "helm" {
  kubernetes {
    host                   = data.azurerm_kubernetes_cluster.main.kube_config.0.host
    username               = data.azurerm_kubernetes_cluster.main.kube_config.0.username
    password               = data.azurerm_kubernetes_cluster.main.kube_config.0.password
    client_certificate     = base64decode(data.azurerm_kubernetes_cluster.main.kube_config.0.client_certificate)
    client_key             = base64decode(data.azurerm_kubernetes_cluster.main.kube_config.0.client_key)
    cluster_ca_certificate = base64decode(data.azurerm_kubernetes_cluster.main.kube_config.0.cluster_ca_certificate)
  }
}

# Namespaces
resource "kubernetes_namespace" "codereview" {
  metadata {
    name = "codereview"
    labels = {
      name        = "codereview"
      environment = var.environment
    }
  }
}

resource "kubernetes_namespace" "codereview_monitoring" {
  metadata {
    name = "codereview-monitoring"
    labels = {
      name        = "codereview-monitoring"
      environment = var.environment
    }
  }
}

# Deploy Prometheus and Grafana
resource "helm_release" "prometheus" {
  name       = "prometheus"
  repository = "https://prometheus-community.github.io/helm-charts"
  chart      = "kube-prometheus-stack"
  namespace  = kubernetes_namespace.codereview_monitoring.metadata[0].name
  version    = "45.0.0"

  values = [
    <<-EOT
    prometheus:
      prometheusSpec:
        storageSpec:
          volumeClaimTemplate:
            spec:
              storageClassName: default
              accessModes: ["ReadWriteOnce"]
              resources:
                requests:
                  storage: 50Gi
    grafana:
      adminPassword: "${var.grafana_admin_password}"
      persistence:
        enabled: true
        storageClassName: default
        size: 10Gi
    alertmanager:
      enabled: true
      persistentVolume:
        enabled: true
        storageClassName: default
        size: 2Gi
    EOT
  ]

  depends_on = [azurerm_kubernetes_cluster.main]
}

# Deploy Ingress Controller
resource "helm_release" "nginx_ingress" {
  name       = "nginx-ingress"
  repository = "https://kubernetes.github.io/ingress-nginx"
  chart      = "ingress-nginx"
  namespace  = kubernetes_namespace.codereview.metadata[0].name
  version    = "4.7.0"

  set {
    name  = "controller.replicaCount"
    value = 2
  }

  set {
    name  = "controller.nodeSelector.nodepool"
    value = "app"
  }

  set {
    name  = "controller.tolerations[0].key"
    value = "workload"
  }

  set {
    name  = "controller.tolerations[0].operator"
    value = "Equal"
  }

  set {
    name  = "controller.tolerations[0].value"
    value = "app"
  }

  set {
    name  = "controller.tolerations[0].effect"
    value = "NoSchedule"
  }

  depends_on = [azurerm_kubernetes_cluster.main]
}

# Role assignments for AKS
resource "azurerm_role_assignment" "aks_acr_pull" {
  scope                = var.container_registry_id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_kubernetes_cluster.main.identity[0].principal_id
}

resource "azurerm_role_assignment" "aks_key_vault_secrets" {
  scope                = var.key_vault_id
  role_definition_name = "Secrets Officer"
  principal_id         = azurerm_kubernetes_cluster.main.identity[0].principal_id
}

# Outputs
output "aks_cluster_name" {
  value = azurerm_kubernetes_cluster.main.name
}

output "aks_cluster_id" {
  value = azurerm_kubernetes_cluster.main.id
}

output "kube_config" {
  value     = data.azurerm_kubernetes_cluster.main.kube_config_raw
  sensitive = true
}

output "client_key" {
  value     = data.azurerm_kubernetes_cluster.main.kube_config.0.client_key
  sensitive = true
}

output "client_certificate" {
  value     = data.azurerm_kubernetes_cluster.main.kube_config.0.client_certificate
  sensitive = true
}

output "cluster_ca_certificate" {
  value     = data.azurerm_kubernetes_cluster.main.kube_config.0.cluster_ca_certificate
  sensitive = true
}

output "host" {
  value = data.azurerm_kubernetes_cluster.main.kube_config.0.host
}

output "username" {
  value = data.azurerm_kubernetes_cluster.main.kube_config.0.username
}

output "password" {
  value     = data.azurerm_kubernetes_cluster.main.kube_config.0.password
  sensitive = true
}
