# NxlogAzureForwarder
Service which forwards Nxlog logs to Azure Table Storage.

Use this application to pump logs from Azure VMs and Cloud Service, as well as from on-premise Servers and workstations.

# Installation:
It can be installed in two ways:

## As Azure Plugin
Make plugin available to VS by copying `AzurePlugin\NxlogAzureForwarder` to `C:\Program Files\Microsoft SDKs\Azure\.NET SDK\v2.6\bin\plugins\NxlogAzureForwarder`.

Then add an `Import` to your `ServiceDefinition.csdef` file:

	<ServiceDefinition>
	  <WorkerRole>
		<Imports>
		  <Import moduleName="NxlogAzureForwarder" />
		</Imports>
	  </WorkerRole>
	</ServiceDefinition>

NxlogAzureForwarder plugin will use storage account you provide in `Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString`. Logs are pushed to `NxlogForwardedLogs` Azure Table Storage.

## As server services
TODO, but the idea is that one can install NxlogAzureForwarder together with Nxlog on servers and workstations and have their logs shipped to Azure Table Storage.

# TODO
* Create NSIS installer for both Nxlog and NxlogAzureForwarder.exe.
* Create installer ready to be used by on-premise servers and workstations.
* Add support for nxlog.conf extensions, so nxlog can be customized by application.
