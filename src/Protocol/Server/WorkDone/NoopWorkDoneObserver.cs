using System;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace OmniSharp.Extensions.LanguageServer.Protocol.Server.WorkDone
{
    internal class NoopWorkDoneObserver : IWorkDoneObserver
    {
        public static NoopWorkDoneObserver Instance = new NoopWorkDoneObserver();

        private NoopWorkDoneObserver()
        {
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(WorkDoneProgress value)
        {
        }

        public ProgressToken WorkDoneToken { get; } = new ProgressToken("Noop");

        public void OnNext(string message, int? percentage, bool? cancellable)
        {
        }

        public void Dispose()
        {
        }
    }
}
