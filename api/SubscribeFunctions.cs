using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace TaiwoTech.Enterprise.TimeSleeper.Teaser.Api
{
    public class SubscribeFunctions(ILogger<SubscribeFunctions> logger, Tracer tracer)
    {
        internal static EmailAddress DefaultSender = new("DoNotReply@Eltee27.com", "Eltee - Do Not Reply");
        private const string EmailSubject = "Timesleeper - New Subscriber";
#if DEBUG
        internal static EmailAddress EmailRecipient = new("eltee.taiwo@gmail.com", "Eltee Taiwo");
#else
        internal static EmailAddress EmailRecipient = new("filmdirectorforhire@gmail.com", "Bisong Taiwo");
#endif
        private const string MessageBody = "Hey Bisong, \n\n You've received a new subscriber from the timesleeper teaser site. See the details below. \n Email: {0}";

        [Function(nameof(Subscribe))]
        public async Task<HttpResponseData> Subscribe(
            [HttpTrigger(AuthorizationLevel.Anonymous,  "post", Route = nameof(Subscribe))] HttpRequestData req, [FromBody] SubscriberPost post)
        {
            using var span = tracer.StartRootSpan(nameof(Subscribe), SpanKind.Server);

            span.SetAttribute(nameof(SubscriberPost.EmailAddress), post.EmailAddress);
            logger.LogInformation("C# HTTP trigger function processed a request.");

            var apiKey = Environment.GetEnvironmentVariable("SendGridApiKey")!;
            var client = new SendGridClient(apiKey);
            var messageBody = string.Format(MessageBody, post.EmailAddress);
            var msg = MailHelper.CreateSingleEmail(DefaultSender, EmailRecipient, EmailSubject, messageBody, messageBody);
            var mailResponse = await client.SendEmailAsync(msg);

            span.SetAttribute("error", !mailResponse.IsSuccessStatusCode);
            logger.Log(mailResponse.IsSuccessStatusCode ? LogLevel.Information : LogLevel.Error,
                "Attempted to send message to {emailAddress}. Status is {status}", post.EmailAddress, mailResponse.StatusCode.ToString());
                
            var response = req.CreateResponse(mailResponse.IsSuccessStatusCode ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync("Subscribed!");

            return response;
        }
    }
}
