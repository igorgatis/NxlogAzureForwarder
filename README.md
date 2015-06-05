# NxlogAzureForwarder
Windows Service which forwards Nxlog logs to Azure Table Storage.

# Installation:
It can be installed in two ways:

## As Azure Plugin
Just copy `AzurePlugin\NxlogAzureForwarder` to `C:\Program Files\Microsoft SDKs\Azure\.NET SDK\v2.6\bin\plugins\NxlogAzureForwarder`.

Then add an `Import` to your `ServiceDefinition.csdef` file:

	<ServiceDefinition>
	  <WorkerRole>
		<Imports>
		  <Import moduleName="NxlogAzureForwarder" />
		</Imports>
	  </WorkerRole>
	</ServiceDefinition>

It will use storage account you provide in `Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString`. Logs are pushed to `NxlogForwardedLogs` Azure Table Storage.

## As server services
TODO, but the idea is that one can install NxlogAzureForwarder together with Nxlog on servers and workstations and have their logs shipped to Azure Table Storage.
