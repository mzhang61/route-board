targetScope = 'resourceGroup'

@description('Deploy staging and production to separate resource groups.')
@allowed(['staging', 'production'])
param environment string
param location string = 'westus'
param administrator string = 'routeboard'
@description('Object ID of the operator allowed to provision environment secrets.')
param operatorObjectId string
@description('Public key only. Never pass a private SSH key.')
param sshPublicKey string
@description('A single administrator IPv4 address in /32 CIDR form.')
param administratorCidr string
@allowed(['Standard_B2pts_v2', 'Standard_B2pls_v2'])
param vmSize string = 'Standard_B2pts_v2'

var prefix = 'routeboard-${environment}'
var tags = { project: 'routeboard', environment: environment }

resource firewall 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: '${prefix}-firewall'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'https'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: 'Internet'
          destinationAddressPrefix: '*'
        }
      }
      {
        name: 'http-certificate-and-redirect'
        properties: {
          priority: 110
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '80'
          sourceAddressPrefix: 'Internet'
          destinationAddressPrefix: '*'
        }
      }
      {
        name: 'administrator-ssh'
        properties: {
          priority: 120
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '22'
          sourceAddressPrefix: administratorCidr
          destinationAddressPrefix: '*'
        }
      }
    ]
  }
}
resource network 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: '${prefix}-network'
  location: location
  tags: tags
  properties: {
    addressSpace: { addressPrefixes: ['10.40.0.0/16'] }
    subnets: [{ name: 'application', properties: { addressPrefix: '10.40.1.0/24' } }]
  }
}
resource address 'Microsoft.Network/publicIPAddresses@2024-05-01' = {
  name: '${prefix}-address'
  location: location
  tags: tags
  sku: { name: 'Standard' }
  properties: { publicIPAllocationMethod: 'Static' }
}
resource adapter 'Microsoft.Network/networkInterfaces@2024-05-01' = {
  name: '${prefix}-adapter'
  location: location
  tags: tags
  properties: {
    networkSecurityGroup: { id: firewall.id }
    ipConfigurations: [{
      name: 'primary'
      properties: {
        privateIPAllocationMethod: 'Dynamic'
        subnet: { id: '${network.id}/subnets/application' }
        publicIPAddress: { id: address.id }
      }
    }]
  }
}
resource host 'Microsoft.Compute/virtualMachines@2024-07-01' = {
  name: '${prefix}-host'
  location: location
  tags: tags
  identity: { type: 'SystemAssigned' }
  properties: {
    hardwareProfile: { vmSize: vmSize }
    storageProfile: {
      imageReference: {
        publisher: 'Canonical'
        offer: 'ubuntu-24_04-lts'
        sku: 'server-arm64'
        version: '24.04.202610250'
      }
      osDisk: {
        createOption: 'FromImage'
        diskSizeGB: 32
        managedDisk: { storageAccountType: 'StandardSSD_LRS' }
        deleteOption: 'Detach'
      }
    }
    osProfile: {
      customData: base64(loadTextContent('cloud-init.yaml'))
      computerName: prefix
      adminUsername: administrator
      linuxConfiguration: {
        disablePasswordAuthentication: true
        ssh: {
          publicKeys: [{ path: '/home/${administrator}/.ssh/authorized_keys', keyData: sshPublicKey }]
        }
      }
    }
    networkProfile: { networkInterfaces: [{ id: adapter.id }] }
    diagnosticsProfile: { bootDiagnostics: { enabled: true } }
  }
}
output publicAddress string = address.properties.ipAddress
output identityPrincipalId string = host.identity.principalId

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'rb${uniqueString(resourceGroup().id)}${environment == 'staging' ? 's' : 'p'}'
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enablePurgeProtection: true
    publicNetworkAccess: 'Enabled'
  }
}
resource hostSecretsReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, host.id, 'secrets-reader')
  scope: vault
  properties: {
    principalId: host.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
  }
}
resource operatorSecretsWriter 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, operatorObjectId, 'secrets-officer')
  scope: vault
  properties: {
    principalId: operatorObjectId
    principalType: 'User'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7')
  }
}
output vaultName string = vault.name
