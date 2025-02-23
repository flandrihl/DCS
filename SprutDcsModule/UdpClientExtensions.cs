using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;

namespace SprutDcsModule
{
    public static class UdpClientExtensions
    {
        /// <summary>
        /// Append checking a <see cref="System.Threading.CancellationToken"/>
        /// to this task with a <see cref="System.Net.Sockets.UdpReceiveResult"/>.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.OperationCanceledException"></exception>
        public static async Task<UdpReceiveResult> WithCancellation(this Task<UdpReceiveResult> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => tcs.TrySetResult(true)))
            {
                if (task != await Task.WhenAny(task, tcs.Task))
                    throw new OperationCanceledException(cancellationToken);
            }
            return await task;
        }
    }
}