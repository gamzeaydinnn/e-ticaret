using System.Threading.Channels;
using System.Threading.Tasks;

namespace ECommerce.Core.Messaging
{
    public class SmsQueue
    {
        private readonly Channel<SmsJob> _channel;

        public SmsQueue()
        {
            _channel = Channel.CreateUnbounded<SmsJob>();
        }

        public ChannelReader<SmsJob> Reader => _channel.Reader;

        public ChannelWriter<SmsJob> Writer => _channel.Writer;

        public ValueTask EnqueueAsync(SmsJob job)
        {
            return _channel.Writer.WriteAsync(job);
        }
    }
}
