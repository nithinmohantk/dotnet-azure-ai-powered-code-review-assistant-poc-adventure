# Virtual Network
resource "azurerm_virtual_network" "main" {
  name                = "${var.project_name}-${var.environment}-vnet"
  location            = var.location
  resource_group_name = var.resource_group_name
  address_space       = var.address_space
  tags                = var.tags
}

# Subnets
resource "azurerm_subnet" "aks" {
  name                 = "aks-subnet"
  virtual_network_name = azurerm_virtual_network.main.name
  resource_group_name  = var.resource_group_name
  address_prefixes     = [var.subnet_prefixes.aks_subnet]
}

resource "azurerm_subnet" "redis" {
  name                 = "redis-subnet"
  virtual_network_name = azurerm_virtual_network.main.name
  resource_group_name  = var.resource_group_name
  address_prefixes     = [var.subnet_prefixes.redis_subnet]
}

resource "azurerm_subnet" "sql" {
  name                 = "sql-subnet"
  virtual_network_name = azurerm_virtual_network.main.name
  resource_group_name  = var.resource_group_name
  address_prefixes     = [var.subnet_prefixes.sql_subnet]
}

# Network Security Groups
resource "azurerm_network_security_group" "aks" {
  name                = "${var.project_name}-${var.environment}-aks-nsg"
  location            = var.location
  resource_group_name = var.resource_group_name
  tags                = var.tags
}

resource "azurerm_subnet_network_security_group_association" "aks" {
  subnet_id                 = azurerm_subnet.aks.id
  network_security_group_id = azurerm_network_security_group.aks.id
}

# Outputs
output "vnet_id" {
  value = azurerm_virtual_network.main.id
}

output "aks_subnet_id" {
  value = azurerm_subnet.aks.id
}

output "redis_subnet_id" {
  value = azurerm_subnet.redis.id
}

output "sql_subnet_id" {
  value = azurerm_subnet.sql.id
}
