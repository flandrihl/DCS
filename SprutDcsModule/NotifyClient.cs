using System;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Threading;

namespace SprutDcsModule
{
    public class NotifyClient : IDisposable
    {
        #region Events

        /// <summary>
        /// Occurs when [receve data handler].
        /// </summary>
        public event EventHandler<byte[]> ReceveDataHandler;

        /// <summary>
        /// Occurs when [sending exception handler].
        /// </summary>
        public event EventHandler<Exception> SendingExceptionHandler;

        /// <summary>
        /// Occurs when [received exception handler].
        /// </summary>
        public event EventHandler<Exception> ReceivedExceptionHandler;

        #endregion Events

        #region Fields

        private UdpClient _udpClient;
        private IPEndPoint _ipEndPoint;
        private bool _isRunning = false;
        private Task _listeningTask;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed = false;

        #endregion Fields

        #region Finalizator

        /// <summary>
        /// Finalizes an instance of the <see cref="NotifyClient"/> class.
        /// </summary>
        ~NotifyClient()
        {
            Dispose(false);
        }

        ///<inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                Disposing();

                if (_isRunning)
                {
                    StopAsync()
                        .GetAwaiter()
                        .GetResult();
                }

                _udpClient?.Dispose();
                _cancellationTokenSource?.Dispose();
            }

            Disposed();

            _disposed = true;
        }

        /// <summary>
        /// Frees up managed resources.
        /// </summary>
        protected virtual void Disposing() { }

        /// <summary>
        /// Releases unmanaged resources (if any).
        /// </summary>
        protected virtual void Disposed() { }

        #endregion Finalizator

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this instance is rinning.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is rinning; otherwise, <c>false</c>.
        /// </value>
        public bool IsRinning => _isRunning;
        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => _isRunning;

        #endregion Properties

        #region Processing

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start(int port, string multicastAddress = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NotifyClient));

            if (_isRunning)
                throw new InvalidOperationException("Notifier is already running.");

            var useMulticast = false;
            var ipAddress = IPAddress.Broadcast;
            if (!string.IsNullOrEmpty(multicastAddress))
                useMulticast = IPAddress.TryParse(multicastAddress, out ipAddress);

            _ipEndPoint = new IPEndPoint(ipAddress, port);
            if (_udpClient == null || _udpClient.Client == null || !_udpClient.Client.Connected)
            {
                _udpClient = new UdpClient(port);

                if (useMulticast)
                    _udpClient.JoinMulticastGroup(_ipEndPoint.Address);
                else
                    _udpClient.EnableBroadcast = true;
            }
            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();

            _listeningTask = ListenAsync(_cancellationTokenSource.Token);
            Console.WriteLine("Running notifier async.");
        }

        /// <summary>
        /// Stops the asynchronous.
        /// </summary>
        public async Task StopAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NotifyClient));

            if (!_isRunning) return;

            _isRunning = false;
            _cancellationTokenSource.Cancel();

            await _listeningTask;

            _udpClient.Close();
            Console.WriteLine("Notifier service stopped.");
        }

        /// <summary>
        /// Sends the data asynchronous.
        /// </summary>
        /// <param name="data">The data.</param>
        public async Task SendDataAsync(byte[] data)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NotifyClient));

            if (!_isRunning || _udpClient == null)
            {
                Console.WriteLine("Notifier is not running or UdpClient is not initialized.");
                return;
            }

            try
            {
                await _udpClient.SendAsync(data, data.Length, _ipEndPoint);
            }
            catch (Exception ex)
            {
                SendingExceptionHandler?.Invoke(this, ex);
            }
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                if (_disposed) return;

                try
                {
                    var result = await _udpClient
                        .ReceiveAsync()
                        .WithCancellation(cancellationToken);
                    var data = result.Buffer;

                    ReceveDataHandler?.Invoke(this, data);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Listening stopped.");
                }
                catch (Exception ex)
                {
                    ReceivedExceptionHandler?.Invoke(this, ex);
                }
            }
        }

        #endregion Processing
    }
}