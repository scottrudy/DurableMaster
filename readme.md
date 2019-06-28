This project can be cloned and run from the command line using dotnet core and Azure Functions tools

### Prerequisites

1. [Install Dotnet 2.2](https://dotnet.microsoft.com/download)
2. [Install Azure Storage Emulator](https://azure.microsoft.com/en-us/downloads/)
3. [Install Azure Storage Explorer](https://azure.microsoft.com/en-us/features/storage-explorer/)
4. Install Function CLI
   * `npm install -g azure-functions-core-tools`

### Add `local.settings.json` to function project
```
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet"
    }
}
```

### Run function project (relies on running web project)

1. [Start Azure Storage Emulator](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator#start-and-initialize-the-storage-emulator)
2. Navigate to the IssueAf project directory
3. `func start`
4. Test the application with a get request to http://localhost:7072/api/OrderIssue_HttpStart (You will need the JSON response from the function)
5. Grab the response from the statusQueryGetUri in the JSON response from step 4.

### See problem 
Open Storage Explorer
Navigate to Local & Attached > Storage Accounts > Emulator > Tables > Durable Functions Hub Instances