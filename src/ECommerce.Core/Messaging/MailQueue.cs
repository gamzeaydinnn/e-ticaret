using System.Threading.Channels;
using System.Threading.Tasks;

namespace ECommerce.Core.Messaging
{
    public class MailQueue
    {
        private readonly Channel<EmailJob> _channel;

        public MailQueue()
        {
            _channel = Channel.CreateUnbounded<EmailJob>();
        }

        public ChannelReader<EmailJob> Reader => _channel.Reader;

        public ChannelWriter<EmailJob> Writer => _channel.Writer;

        public ValueTask EnqueueAsync(EmailJob job)
        {
            return _channel.Writer.WriteAsync(job);
        }
    }
}
