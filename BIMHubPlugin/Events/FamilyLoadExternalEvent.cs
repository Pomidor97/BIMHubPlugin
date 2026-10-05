using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BIMHubPlugin.Services;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BIMHubPlugin.Events
{
    public sealed class FamilyLoadResult
    {
        public bool Success { get; }
        public string Message { get; }
        public FamilyLoadResult(bool success, string message) { Success = success; Message = message; }
    }

    // Created once in IExternalApplication.OnStartup, never from a WPF dispatcher callback.
    public sealed class FamilyLoadExternalEvent : IExternalEventHandler, IDisposable
    {
        private readonly object _gate = new object();
        private readonly ExternalEvent _event;
        private Request _pending;
        private bool _disposed;
        public FamilyLoadExternalEvent() => _event = ExternalEvent.Create(this);

        public Task<FamilyLoadResult> LoadAsync(string path, string name, Document document, bool showDialog, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            lock (_gate)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(FamilyLoadExternalEvent));
                if (_pending != null) throw new InvalidOperationException("Предыдущая загрузка семейства ещё выполняется.");
                var request = new Request(path, name, document, showDialog, ct);
                _pending = request;
                try
                {
                    var status = _event.Raise();
                    if (status != ExternalEventRequest.Accepted && status != ExternalEventRequest.Pending)
                        throw new InvalidOperationException("Revit не принял запрос загрузки. Повторите после завершения текущей команды.");
                    return request.Completion.Task;
                }
                catch { _pending = null; throw; }
            }
        }

        public void Execute(UIApplication app)
        {
            Request request;
            lock (_gate) { request = _pending; }
            if (request == null) return;
            try
            {
                request.Token.ThrowIfCancellationRequested();
                var doc = app.ActiveUIDocument?.Document;
                if (doc == null || !doc.IsValidObject || !doc.Equals(request.Document))
                    throw new InvalidOperationException("Активный документ изменился. Повторите загрузку в нужном проекте.");
                if (doc.IsReadOnly || doc.IsModifiable)
                    throw new InvalidOperationException("Документ сейчас недоступен для изменения.");
                if (!File.Exists(request.Path)) throw new FileNotFoundException("Файл семейства не найден.");
                using (var transaction = new Transaction(doc, "Загрузка семейства BIMHub"))
                {
                    transaction.Start();
                    var loaded = doc.LoadFamily(request.Path, new FamilyLoadOptions(request.ShowDialog), out var family);
                    if (!loaded || family == null)
                    {
                        transaction.RollBack();
                        request.Completion.TrySetResult(new FamilyLoadResult(false, "Загрузка отменена или семейство уже актуально."));
                    }
                    else
                    {
                        var status = transaction.Commit();
                        request.Completion.TrySetResult(new FamilyLoadResult(status == TransactionStatus.Committed,
                            status == TransactionStatus.Committed ? "Семейство '" + (request.Name ?? family.Name) + "' загружено." : "Revit отменил транзакцию загрузки."));
                    }
                }
            }
            catch (OperationCanceledException) { request.Completion.TrySetCanceled(); }
            catch (Exception ex)
            {
                SimpleLogger.Error("Revit family load failed", ex);
                request.Completion.TrySetResult(new FamilyLoadResult(false, ex.Message));
            }
            finally { lock (_gate) { if (ReferenceEquals(_pending, request)) _pending = null; } }
        }

        public string GetName() => "BIMHub family loader";
        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed) return;
                _disposed = true;
                _pending?.Completion.TrySetCanceled();
                _pending = null;
                _event.Dispose();
            }
        }
        private sealed class Request
        {
            public string Path { get; }
            public string Name { get; }
            public Document Document { get; }
            public bool ShowDialog { get; }
            public CancellationToken Token { get; }
            public TaskCompletionSource<FamilyLoadResult> Completion { get; } = new TaskCompletionSource<FamilyLoadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            public Request(string path, string name, Document document, bool showDialog, CancellationToken token)
            { Path = path; Name = name; Document = document; ShowDialog = showDialog; Token = token; }
        }
    }
}
