using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace DurableAf {
    public static class DurableMaster {
        private static readonly HttpClient _client = HttpClientFactory.Create();
        // Use a unique name for the event within the task hub
        private const string EVENTNAME = "ExternalEvent_Processed";

        // Start this by executing http://localhost:7072/api/DurableMaster_HttpStart
        [FunctionName(nameof(DurableMaster_HttpStart))]
        public static async Task<HttpResponseMessage> DurableMaster_HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log) {
            
            // The instance id is the value of what will be used to resume the orchestration
            string instanceId = await starter.StartNewAsync(nameof(DurableMaster_Orchestration), null);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(DurableMaster_Orchestration))]
        public static async Task<bool> DurableMaster_Orchestration(
            [OrchestrationTrigger] DurableOrchestrationContext context, ILogger log) {
            try {
                // This activity will perform some call to an external action that will require a callback
                var result = await context.CallActivityAsync<bool>(nameof(DurableMaster_ExternalActionActivity), null);
                if (!context.IsReplaying) 
                    log.LogWarning($"Call http://localhost:7072/api/DurableMaster_Callback?eventName={EVENTNAME}&payload=I%20did%20it&instanceId={context.InstanceId} in 30 seconds or this orchestration will timeout.");
                // This will cause the durable function to halt until the external action raises the proper event, or 30 seconds elapses
                var resultFromExternalEvent = await context.WaitForExternalEvent<string>(EVENTNAME, TimeSpan.FromSeconds(30));
                // When the callback is received the orchestration will resume
                if (!context.IsReplaying) log.LogInformation(resultFromExternalEvent);
                return true;
            } catch (Exception ex) {
                log.LogInformation($"~Last Error: {ex.Message}");
                throw;
            } finally {
                log.LogInformation("~Finishing orchestration");
            }
        }

        [FunctionName(nameof(DurableMaster_ExternalActionActivity))]
        public static async Task<bool> DurableMaster_ExternalActionActivity([ActivityTrigger] string value, ILogger log) {
            try {
                // This action will kick off an external action, such as another Durable Function Orchestration in another task hub
                var response = await _client.PostAsJsonAsync("https://www.google.com", "somevalue");
                return response.IsSuccessStatusCode;
            } finally {
                log.LogInformation($"~Completed DurableMaster_ExternalActionActivity.");
            }
        }   

        // This Callback will be called from an external action, such as another Durable Function Orchestration. It must be in the same task hub.
        // The call will need to pass the instanceId, eventName and any payload that needs to be processed by the Master Orchestration.
        [FunctionName(nameof(DurableMaster_Callback))]
        public static async Task<IActionResult> DurableMaster_Callback(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log) {
            var instanceId = req.RequestUri.ParseQueryString().GetValues("instanceId").FirstOrDefault();
            var eventName = req.RequestUri.ParseQueryString().GetValues("eventName").FirstOrDefault();
            var payload = req.RequestUri.ParseQueryString().GetValues("payload").FirstOrDefault();
            // Function input comes from the request content.
            await starter.RaiseEventAsync(instanceId, eventName, payload);
            return new OkResult();
        }
    }
}