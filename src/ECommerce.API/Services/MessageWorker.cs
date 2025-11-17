using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ECommerce.Core.Messaging;
using ECommerce.Infrastructure.Services.Email;

namespace ECommerce.API.Services
{
    public class MessageWorker : IHostedService, IDisposable
    {
        private readonly MailQueue _mailQueue;
        private readonly SmsQueue _smsQueue;
        private readonly EmailSender _emailSender;
        private readonly ILogger<MessageWorker> _logger;
        private CancellationTokenSource? _cts;
        private Task? _emailTask;
        private Task? _smsTask;

        public MessageWorker(MailQueue mailQueue, SmsQueue smsQueue, EmailSender emailSender, ILogger<MessageWorker> logger)
        {
            _mailQueue = mailQueue;
            _smsQueue = smsQueue;
            _emailSender = emailSender;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _emailTask = Task.Run(() => ProcessEmailsAsync(_cts.Token));
            _smsTask = Task.Run(() => ProcessSmsAsync(_cts.Token));
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _cts?.Cancel();
                if (_emailTask != null) await Task.WhenAny(_emailTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));
                if (_smsTask != null) await Task.WhenAny(_smsTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while stopping MessageWorker");
            }
        }

        private async Task ProcessEmailsAsync(CancellationToken ct)
        {
            var reader = _mailQueue.Reader;
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var job = await reader.ReadAsync(ct);
                    var success = false;
                    while (job.Attempts < 3 && !success)
                    {
                        job.Attempts++;
                        try
                        {
                            var ok = await _emailSender.SendEmailAsync(job.To, job.Subject, job.Body, job.IsHtml);
                            if (ok) success = true;
                            else
                            {
                                _logger.LogWarning("Email send failed attempt {Attempt} for {To}", job.Attempts, job.To);
                                await Task.Delay(1000, ct);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Exception while sending email to {To}", job.To);
                            await Task.Delay(1000, ct);
                        }
                    }

                    if (!success)
                    {
                        _logger.LogError("Giving up sending email to {To} after {Attempts} attempts", job.To, job.Attempts);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in email processor loop");
                    await Task.Delay(1000, ct);
                }
            }
        }

        private async Task ProcessSmsAsync(CancellationToken ct)
        {
            var reader = _smsQueue.Reader;
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var job = await reader.ReadAsync(ct);
                    var success = false;
                    while (job.Attempts < 3 && !success)
                    {
                        job.Attempts++;
                        try
                        {
                            // For now, SMS is a console write. Replace with real provider if needed.
                            Console.WriteLine($"[SMS] To={job.PhoneNumber} Message={job.Message}");
                            success = true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Exception while sending SMS to {Phone}", job.PhoneNumber);
                            await Task.Delay(1000, ct);
                        }
                    }

                    if (!success)
                    {
                        _logger.LogError("Giving up sending SMS to {Phone} after {Attempts} attempts", job.PhoneNumber, job.Attempts);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in SMS processor loop");
                    await Task.Delay(1000, ct);
                }
            }
        }

        public void Dispose()
        {
            _cts?.Dispose();
        }
    }
}
