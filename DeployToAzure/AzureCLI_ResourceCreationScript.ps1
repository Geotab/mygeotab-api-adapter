<#
This PowerShell script creates a new Azure resource group and uses a template to create a deployment group within and deploy the MyGeotab API Adapter to that deployment group.
#>

# Create an Azure resource group, specifying the name and location (default name is 'MyGeotabAPIAdapter' and location is 'canadacentral').
az group create --name MyGeotabAPIAdapter --location canadacentral

# Create a deployment group within the new resource group using the template file named 'ARM_Template.json'. The template parameters are populated with values defined in the corresponding 'ARM_TemplateParameterValues.json' file.
az deployment group create -g MyGeotabAPIAdapter --template-file ARM_Template.json --parameters ARM_TemplateParameterValues.json --verbose